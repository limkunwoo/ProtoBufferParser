namespace ProtoBufferParser.Models;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// Source location information for error reporting.
    /// </summary>
    public SourceLocation Location { get; set; } = new();
}

/// <summary>
/// Represents a source location in the .proto file.
/// </summary>
public class SourceLocation
{
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    
    public override string ToString() => $"{FileName}({Line},{Column})";
}
