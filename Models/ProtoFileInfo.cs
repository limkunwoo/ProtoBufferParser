namespace ProtoBufferParser.Models;

/// <summary>
/// Represents information about a .proto file discovered by the file scanner.
/// </summary>
public sealed class ProtoFileInfo
{
    /// <summary>
    /// Full absolute path to the .proto file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File name without extension (e.g., "player" from "player.proto").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Raw text content of the .proto file.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
