namespace ProtoBufferParser.Models;

/// <summary>
/// Represents a Protocol Buffer message definition.
/// </summary>
public class MessageNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // After flattening: e.g., "OuterInner"
    
    /// <summary>
    /// The C++ type name that protoc generates for this message.
    /// For top-level messages, this is the same as Name (e.g., "Player").
    /// For nested messages, protoc uses underscores: "Player_Stats", "A_B_C".
    /// Set by MessageFlattener during flattening.
    /// </summary>
    public string ProtocTypeName { get; set; } = string.Empty;
    
    public List<FieldNode> Fields { get; set; } = new();
    public List<MessageNode> NestedMessages { get; set; } = new();
    public List<EnumNode> NestedEnums { get; set; } = new();
    public List<string> Dependencies { get; set; } = new(); // Other messages this depends on
}

/// <summary>
/// Represents a field in a message.
/// </summary>
public class FieldNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int FieldNumber { get; set; }
    public bool IsRepeated { get; set; }
    public bool IsOptional { get; set; }

    /// <summary>
    /// Whether this field belongs to a oneof group.
    /// Both IsOptional and IsOneOf fields are mapped to TOptional&lt;T&gt; in Unreal.
    /// </summary>
    public bool IsOneOf { get; set; }

    /// <summary>
    /// The name of the oneof group this field belongs to (e.g., "event").
    /// Empty string if the field is not part of a oneof group.
    /// </summary>
    public string OneOfGroupName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this field references an enum type (vs. a message or primitive type).
    /// Used by TypeMapper to apply the correct naming prefix (E vs F).
    /// </summary>
    public bool IsEnum { get; set; }
    
    // Map field properties
    public bool IsMap { get; set; }
    public string MapKeyType { get; set; } = string.Empty;
    public string MapValueType { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the map value type is an enum (for TMap value type naming).
    /// </summary>
    public bool MapValueIsEnum { get; set; }
}
