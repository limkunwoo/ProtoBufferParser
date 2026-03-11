namespace ProtoBufferParser.Models;

/// <summary>
/// Represents a Protocol Buffer file (.proto).
/// </summary>
public class ProtoFileNode : AstNode
{
    public string FileName { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public List<ImportNode> Imports { get; set; } = new();
    public List<MessageNode> Messages { get; set; } = new();
    public List<EnumNode> Enums { get; set; } = new();
}

/// <summary>
/// Represents an import statement.
/// </summary>
public class ImportNode : AstNode
{
    public string Path { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsWeak { get; set; }
}
