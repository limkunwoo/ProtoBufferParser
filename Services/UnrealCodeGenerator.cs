using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Services;

/// <summary>
/// Orchestrates code generation for Unreal Engine C++ from a flattened Proto3 AST.
/// Combines FileHeaderTemplate, StructTemplate, and EnumTemplate to produce
/// complete header (.h) and implementation (.cpp) files.
/// </summary>
public sealed class UnrealCodeGenerator : ICodeGenerator
{
    private readonly ITypeMapper _typeMapper;
    private readonly ILogger _logger;
    private readonly FileHeaderTemplate _headerTemplate;
    private readonly StructTemplate _structTemplate;
    private readonly EnumTemplate _enumTemplate;

    public UnrealCodeGenerator(ITypeMapper typeMapper, ILogger logger, CodeGeneratorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);
        ArgumentNullException.ThrowIfNull(logger);

        _typeMapper = typeMapper;
        _logger = logger;
        _headerTemplate = new FileHeaderTemplate();
        _structTemplate = new StructTemplate(typeMapper, options);
        _enumTemplate = new EnumTemplate(typeMapper);
    }

    /// <inheritdoc />
    public GeneratedCodeResult Generate(ProtoFileNode ast)
    {
        ArgumentNullException.ThrowIfNull(ast);

        var result = new GeneratedCodeResult();

        _logger.LogInfo($"Generating code for {ast.FileName}...");

        // Generate enum types
        foreach (var enumNode in ast.Enums)
        {
            var enumOutput = GenerateEnum(enumNode, ast.FileName);
            result.Types.Add(enumOutput);
            _logger.LogVerbose($"  Generated enum: {enumOutput.TypeName}");
        }

        // Generate struct types
        foreach (var message in ast.Messages)
        {
            var structOutput = GenerateStruct(message, ast.FileName, ast.Imports, ast.Package);
            result.Types.Add(structOutput);
            _logger.LogVerbose($"  Generated struct: {structOutput.TypeName}");
        }

        _logger.LogInfo($"Code generation complete: {result.Types.Count} types generated.");

        return result;
    }

    /// <summary>
    /// Generates the header and (optional) cpp files for an enum type.
    /// </summary>
    private GeneratedTypeOutput GenerateEnum(EnumNode enumNode, string sourceFile)
    {
        var enumTypeName = _typeMapper.ConvertToTypeName(enumNode.FullName, isEnum: true);
        var fileName = _typeMapper.GetOutputFileName(enumNode.FullName);

        var fileHeader = _headerTemplate.Generate(sourceFile);
        var headerContent = _enumTemplate.GenerateHeader(enumNode);
        var cppContent = _enumTemplate.GenerateCpp(enumNode, $"{fileName}.h");

        var output = new GeneratedTypeOutput
        {
            TypeName = enumTypeName,
            HeaderFile = new GeneratedFile
            {
                FileName = $"{fileName}.h",
                Content = fileHeader + headerContent
            }
        };

        // Enums typically don't need a .cpp file
        if (cppContent != null)
        {
            output.CppFile = new GeneratedFile
            {
                FileName = $"{fileName}.cpp",
                Content = fileHeader + cppContent
            };
        }

        return output;
    }

    /// <summary>
    /// Generates the header and cpp files for a struct type.
    /// </summary>
    private GeneratedTypeOutput GenerateStruct(
        MessageNode message, string sourceFile, List<ImportNode> imports, string package)
    {
        var structTypeName = _typeMapper.ConvertToTypeName(message.FullName, isEnum: false);
        var fileName = _typeMapper.GetOutputFileName(message.FullName);

        var dependencies = ExtractDependencies(message, imports);
        var fileHeader = _headerTemplate.Generate(sourceFile);
        var headerContent = _structTemplate.GenerateHeader(message, dependencies, package);
        var cppContent = _structTemplate.GenerateCpp(message, $"{fileName}.h", sourceFile, package);

        return new GeneratedTypeOutput
        {
            TypeName = structTypeName,
            HeaderFile = new GeneratedFile
            {
                FileName = $"{fileName}.h",
                Content = fileHeader + headerContent
            },
            CppFile = new GeneratedFile
            {
                FileName = $"{fileName}.cpp",
                Content = fileHeader + cppContent
            }
        };
    }

    /// <summary>
    /// Extracts dependency include file names from a message's fields.
    /// Returns file names without extension (e.g., "MEPlayerProto").
    /// Only includes dependencies based on actual field types used by this message.
    /// </summary>
    private List<string> ExtractDependencies(MessageNode message, List<ImportNode> imports)
    {
        var dependencyFileNames = new HashSet<string>(StringComparer.Ordinal);

        // Collect non-primitive field types as dependencies
        foreach (var field in message.Fields)
        {
            if (field.IsMap)
            {
                // Check if map value type is a custom type
                if (!_typeMapper.IsPrimitiveType(field.MapValueType))
                {
                    var depFileName = _typeMapper.GetOutputFileName(field.MapValueType);
                    dependencyFileNames.Add(depFileName);
                }
            }
            else if (!_typeMapper.IsPrimitiveType(field.Type))
            {
                var depFileName = _typeMapper.GetOutputFileName(field.Type);
                dependencyFileNames.Add(depFileName);
            }
        }

        // Exclude self-reference
        var selfFileName = _typeMapper.GetOutputFileName(message.FullName);
        dependencyFileNames.Remove(selfFileName);

        return dependencyFileNames.OrderBy(n => n, StringComparer.Ordinal).ToList();
    }
}
