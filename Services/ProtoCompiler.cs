using System.Text.RegularExpressions;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;
using ProtoBufferParser.Parsers;

namespace ProtoBufferParser.Services;

/// <summary>
/// Main orchestrator that drives the full proto-to-Unreal compilation pipeline.
/// </summary>
public sealed class ProtoCompiler
{
    private readonly CompilerOptions _options;
    private readonly ILogger _logger;
    private readonly FileScanner _scanner;
    private readonly AntlrProtoParser _parser;
    private readonly MessageFlattener _flattener;
    private readonly UnrealTypeMapper _typeMapper;
    private readonly UnrealCodeGenerator _codeGenerator;

    public ProtoCompiler(CompilerOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _scanner = new FileScanner(logger);
        _parser = new AntlrProtoParser(logger);
        _flattener = new MessageFlattener();
        _typeMapper = new UnrealTypeMapper();
        _codeGenerator = new UnrealCodeGenerator(_typeMapper, logger);
    }

    /// <summary>
    /// Runs the full compilation pipeline: scan → parse → flatten → generate → write.
    /// </summary>
    /// <returns>The number of generated files.</returns>
    public int Compile()
    {
        _logger.LogInfo("ProtoBufferParser starting...");
        _logger.LogInfo($"Input:  {_options.InputDirectory}");
        _logger.LogInfo($"Output: {_options.OutputDirectory}");

        // Validate input directory exists
        if (!Directory.Exists(_options.InputDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Input directory not found: {_options.InputDirectory}");
        }

        // 1. Scan for .proto files
        _logger.LogInfo("Scanning for .proto files...");
        var files = _scanner.ScanDirectory(_options.InputDirectory).ToList();

        if (files.Count == 0)
        {
            _logger.LogWarning("No .proto files found. Exiting.");
            return 0;
        }

        // 2. Extract imports (regex) + topological sort
        _logger.LogInfo("Resolving dependencies...");
        var graph = new DependencyGraph(_logger);

        foreach (var file in files)
        {
            graph.AddFile(file);
        }

        foreach (var file in files)
        {
            var imports = ExtractImports(file.Content);
            foreach (var importPath in imports)
            {
                var importedFileName = Path.GetFileNameWithoutExtension(importPath);
                graph.AddDependency(file.FileName, importedFileName);
                _logger.LogVerbose($"  Dependency: {file.FileName} -> {importedFileName}");
            }
        }

        var orderedFiles = graph.TopologicalSort();
        _logger.LogInfo($"Compilation order: {string.Join(" -> ", orderedFiles.Select(f => f.FileName))}");

        // 3. Prepare output directory
        if (!Directory.Exists(_options.OutputDirectory))
        {
            Directory.CreateDirectory(_options.OutputDirectory);
            _logger.LogInfo($"Created output directory: {_options.OutputDirectory}");
        }

        // 4. Compile in topological order: parse → flatten → codegen → write
        var totalFiles = 0;

        foreach (var orderedFile in orderedFiles)
        {
            _logger.LogInfo($"Compiling {orderedFile.FileName}.proto...");

            // Parse
            ProtoFileNode ast;
            try
            {
                ast = _parser.Parse(orderedFile.Content, orderedFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"  Failed to parse {orderedFile.FileName}.proto: {ex.Message}");
                throw;
            }

            // Register parsed types for cross-file references
            _parser.RegisterParsedFile(ast);

            // Flatten nested messages/enums
            _flattener.Flatten(ast);

            // Generate code
            var result = _codeGenerator.Generate(ast);

            // Write output files
            foreach (var generatedFile in result.AllFiles)
            {
                var filePath = Path.Combine(_options.OutputDirectory, generatedFile.FileName);
                File.WriteAllText(filePath, generatedFile.Content);
                _logger.LogInfo($"  Written: {generatedFile.FileName}");
                totalFiles++;
            }
        }

        _logger.LogInfo($"Done! Generated {totalFiles} files in '{_options.OutputDirectory}'");
        return totalFiles;
    }

    /// <summary>
    /// Extracts import paths from proto content using regex (no parsing required).
    /// </summary>
    private static List<string> ExtractImports(string content)
    {
        var imports = new List<string>();
        var regex = new Regex(@"import\s+(?:public\s+|weak\s+)?""([^""]+)""\s*;");

        foreach (Match match in regex.Matches(content))
        {
            imports.Add(match.Groups[1].Value);
        }

        return imports;
    }
}
