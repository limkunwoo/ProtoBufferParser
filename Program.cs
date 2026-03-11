using System.CommandLine;
using ProtoBufferParser.Models;
using ProtoBufferParser.Services;

// ============================================================
// CLI Entry Point
// ============================================================

var inputOption = new Option<string>(
    aliases: new[] { "--input-dir", "-i" },
    description: "Input directory containing .proto files")
{
    IsRequired = true
};

var outputOption = new Option<string>(
    aliases: new[] { "--output-dir", "-o" },
    description: "Output directory for generated .h and .cpp files")
{
    IsRequired = true
};

var verboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Enable verbose logging");

var rootCommand = new RootCommand("ProtoBufferParser - Convert .proto files to Unreal Engine C++ structs")
{
    inputOption,
    outputOption,
    verboseOption
};

rootCommand.SetHandler((inputDir, outputDir, verbose) =>
{
    // Resolve to absolute paths
    inputDir = Path.GetFullPath(inputDir);
    outputDir = Path.GetFullPath(outputDir);

    var options = new CompilerOptions
    {
        InputDirectory = inputDir,
        OutputDirectory = outputDir,
        Verbose = verbose
    };

    var logger = new ConsoleLogger(options.Verbose);

    try
    {
        var compiler = new ProtoCompiler(options, logger);
        compiler.Compile();
    }
    catch (Exception ex)
    {
        logger.LogError($"Compilation failed: {ex.Message}");
        if (verbose)
        {
            logger.LogError(ex.StackTrace ?? "");
        }
        Environment.Exit(1);
    }
},
inputOption, outputOption, verboseOption);

return await rootCommand.InvokeAsync(args);
