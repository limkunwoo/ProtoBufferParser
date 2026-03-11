namespace ProtoBufferParser.Models;

/// <summary>
/// Configuration options for the proto-to-Unreal compiler pipeline.
/// </summary>
public sealed class CompilerOptions
{
    /// <summary>
    /// Input directory containing .proto files.
    /// </summary>
    public string InputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Output directory for generated .h and .cpp files.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable verbose logging.
    /// </summary>
    public bool Verbose { get; set; }
}
