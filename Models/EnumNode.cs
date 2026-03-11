namespace ProtoBufferParser.Models;

/// <summary>
/// Represents an enum definition in a Protocol Buffer file.
/// </summary>
public class EnumNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // After flattening
    public List<EnumValueNode> Values { get; set; } = new();
}

/// <summary>
/// Represents an enum value.
/// </summary>
public class EnumValueNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
