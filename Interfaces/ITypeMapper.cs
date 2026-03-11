using ProtoBufferParser.Models;

namespace ProtoBufferParser.Interfaces;

/// <summary>
/// Defines the contract for mapping Proto3 AST types to target platform types.
/// </summary>
public interface ITypeMapper
{
    /// <summary>
    /// Maps a field's Proto3 type to the target platform type string.
    /// Handles primitives, messages, enums, repeated fields, and map fields.
    /// </summary>
    /// <param name="field">The AST field node to map.</param>
    /// <returns>The target platform type string (e.g., "int32", "FString", "TArray&lt;FPlayerProto&gt;").</returns>
    string MapFieldType(FieldNode field);

    /// <summary>
    /// Converts a Proto3 type name to the target platform type name.
    /// </summary>
    /// <param name="protoTypeName">The original Proto3 type name.</param>
    /// <param name="isEnum">Whether the type is an enum.</param>
    /// <returns>The converted type name (e.g., "FPlayerProto" or "EStatusProto").</returns>
    string ConvertToTypeName(string protoTypeName, bool isEnum = false);

    /// <summary>
    /// Gets the output file name (without extension) for a given type name.
    /// </summary>
    /// <param name="typeName">The (possibly flattened) type name.</param>
    /// <returns>The output file name (e.g., "MEPlayerProto").</returns>
    string GetOutputFileName(string typeName);

    /// <summary>
    /// Converts a Proto3 field name to the target platform field name.
    /// </summary>
    /// <param name="protoFieldName">The Proto3 field name (typically snake_case).</param>
    /// <returns>The converted field name (e.g., "PlayerName").</returns>
    string ConvertFieldName(string protoFieldName);

    /// <summary>
    /// Converts a Proto3 enum value name to the target platform enum value name.
    /// </summary>
    /// <param name="protoEnumValueName">The Proto3 enum value name (typically UPPER_SNAKE_CASE).</param>
    /// <returns>The converted enum value name (e.g., "ActiveState").</returns>
    string ConvertEnumValueName(string protoEnumValueName);

    /// <summary>
    /// Checks whether a Proto3 type name is a known primitive/scalar type.
    /// </summary>
    /// <param name="protoTypeName">The Proto3 type name to check.</param>
    /// <returns>True if the type is a primitive scalar type.</returns>
    bool IsPrimitiveType(string protoTypeName);
}
