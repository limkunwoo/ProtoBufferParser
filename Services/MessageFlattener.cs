using ProtoBufferParser.Models;

namespace ProtoBufferParser.Services;

/// <summary>
/// Flattens nested message and enum definitions within a <see cref="ProtoFileNode"/>.
/// Nested types like <c>Outer.Inner</c> become <c>OuterInner</c> at the top level.
/// Also updates field type references to use the flattened names.
/// </summary>
public sealed class MessageFlattener
{
    /// <summary>
    /// Flattens all nested messages and enums in a proto file to top-level definitions.
    /// After flattening, all messages and enums have their <c>FullName</c> set and
    /// nested collections are cleared. Also sets <c>ProtocTypeName</c> to match
    /// the C++ type name that protoc generates (using underscores for nesting).
    /// </summary>
    public void Flatten(ProtoFileNode fileNode)
    {
        // Flatten messages (recursive)
        var flattenedMessages = FlattenMessages(fileNode.Messages, parentName: "", parentProtocName: "");
        fileNode.Messages = flattenedMessages;

        // Flatten top-level enums (set FullName = Name for consistency)
        foreach (var enumNode in fileNode.Enums)
        {
            enumNode.FullName = enumNode.Name;
        }

        // Collect all flattened enums from nested messages into top-level
        var allEnums = new List<EnumNode>(fileNode.Enums);
        foreach (var message in fileNode.Messages)
        {
            allEnums.AddRange(message.NestedEnums);
            message.NestedEnums.Clear();
        }
        fileNode.Enums = allEnums;
    }

    /// <summary>
    /// Recursively flattens nested messages and sets their <c>FullName</c> and <c>ProtocTypeName</c>.
    /// Also updates field type references in parent messages to use the flattened names.
    /// </summary>
    /// <param name="messages">The list of messages to flatten.</param>
    /// <param name="parentName">The parent's full name for Unreal naming (empty for top-level).</param>
    /// <param name="parentProtocName">The parent's protoc type name for protoc C++ naming (empty for top-level).</param>
    /// <returns>A flat list of all messages with proper <c>FullName</c> and <c>ProtocTypeName</c> values.</returns>
    private List<MessageNode> FlattenMessages(List<MessageNode> messages, string parentName, string parentProtocName)
    {
        var result = new List<MessageNode>();

        foreach (var message in messages)
        {
            // Build full name: ParentChild (no underscore, no separator) — for Unreal naming
            var fullName = string.IsNullOrEmpty(parentName)
                ? message.Name
                : $"{parentName}{message.Name}";

            message.FullName = fullName;

            // Build protoc C++ type name: Parent_Child (underscore separator) — matches protoc output
            var protocTypeName = string.IsNullOrEmpty(parentProtocName)
                ? message.Name
                : $"{parentProtocName}_{message.Name}";

            message.ProtocTypeName = protocTypeName;

            // Build rename map: original short name → flattened FullName
            // for this message's nested types (messages + enums)
            var nestedTypeRenames = new Dictionary<string, string>();
            foreach (var nested in message.NestedMessages)
            {
                nestedTypeRenames[nested.Name] = $"{fullName}{nested.Name}";
            }
            foreach (var nestedEnum in message.NestedEnums)
            {
                nestedTypeRenames[nestedEnum.Name] = $"{fullName}{nestedEnum.Name}";
            }

            // Update field type references in this message to use flattened names
            foreach (var field in message.Fields)
            {
                if (nestedTypeRenames.TryGetValue(field.Type, out var newTypeName))
                {
                    field.Type = newTypeName;
                }

                // Also handle map value types
                if (field.IsMap && nestedTypeRenames.TryGetValue(field.MapValueType, out var newMapValueType))
                {
                    field.MapValueType = newMapValueType;
                }
            }

            result.Add(message);

            // Recursively flatten nested messages
            if (message.NestedMessages.Count > 0)
            {
                var flattened = FlattenMessages(message.NestedMessages, fullName, protocTypeName);
                result.AddRange(flattened);
                message.NestedMessages = new List<MessageNode>(); // Clear after flattening
            }

            // Update nested enum FullNames
            foreach (var nestedEnum in message.NestedEnums)
            {
                nestedEnum.FullName = $"{fullName}{nestedEnum.Name}";
            }
        }

        return result;
    }
}
