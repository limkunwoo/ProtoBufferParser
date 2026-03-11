using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;
using ProtoBufferParser.Utilities;

namespace ProtoBufferParser.Services;

/// <summary>
/// Maps Proto3 types to Unreal Engine C++ types.
/// Handles scalar types, message references, enum references,
/// repeated fields (TArray), and map fields (TMap).
/// </summary>
public sealed class UnrealTypeMapper : ITypeMapper
{
    /// <summary>
    /// Proto3 scalar type → Unreal C++ type mapping table.
    /// </summary>
    private static readonly Dictionary<string, string> PrimitiveTypeMap = new()
    {
        { "int32", "int32" },
        { "int64", "int64" },
        { "uint32", "uint32" },
        { "uint64", "uint64" },
        { "sint32", "int32" },
        { "sint64", "int64" },
        { "fixed32", "uint32" },
        { "fixed64", "uint64" },
        { "sfixed32", "int32" },
        { "sfixed64", "int64" },
        { "float", "float" },
        { "double", "double" },
        { "bool", "bool" },
        { "string", "FString" },
        { "bytes", "TArray<uint8>" }
    };

    /// <inheritdoc />
    public string MapFieldType(FieldNode field)
    {
        ArgumentNullException.ThrowIfNull(field);

        // Map fields: TMap<K, V>
        if (field.IsMap)
        {
            return MapMapFieldType(field);
        }

        // Determine base type first
        string baseType;

        // Primitive/scalar type
        if (PrimitiveTypeMap.TryGetValue(field.Type, out var primitiveType))
        {
            baseType = field.IsRepeated ? $"TArray<{primitiveType}>" : primitiveType;
        }
        else
        {
            // Message or Enum reference type
            var unrealTypeName = ConvertToTypeName(field.Type, field.IsEnum);
            baseType = field.IsRepeated ? $"TArray<{unrealTypeName}>" : unrealTypeName;
        }

        // Wrap in TOptional for optional/oneof fields (not repeated, not map)
        if ((field.IsOptional || field.IsOneOf) && !field.IsRepeated)
        {
            return $"TOptional<{baseType}>";
        }

        return baseType;
    }

    /// <inheritdoc />
    public string ConvertToTypeName(string protoTypeName, bool isEnum = false)
    {
        ArgumentNullException.ThrowIfNull(protoTypeName);

        return isEnum
            ? NamingHelper.ToUnrealEnumName(protoTypeName)
            : NamingHelper.ToUnrealStructName(protoTypeName);
    }

    /// <inheritdoc />
    public string GetOutputFileName(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);

        return NamingHelper.ToOutputFileName(typeName);
    }

    /// <inheritdoc />
    public string ConvertFieldName(string protoFieldName)
    {
        ArgumentNullException.ThrowIfNull(protoFieldName);

        return NamingHelper.ToUnrealFieldName(protoFieldName);
    }

    /// <inheritdoc />
    public string ConvertEnumValueName(string protoEnumValueName)
    {
        ArgumentNullException.ThrowIfNull(protoEnumValueName);

        return NamingHelper.ToUnrealEnumValueName(protoEnumValueName);
    }

    /// <inheritdoc />
    public bool IsPrimitiveType(string protoTypeName)
    {
        return PrimitiveTypeMap.ContainsKey(protoTypeName);
    }

    /// <summary>
    /// Maps a map field to TMap&lt;K, V&gt;.
    /// Key types are always primitive; value types can be primitive or custom.
    /// </summary>
    private string MapMapFieldType(FieldNode field)
    {
        var keyType = MapPrimitiveType(field.MapKeyType);
        var valueType = MapMapValueType(field.MapValueType, field.MapValueIsEnum);
        return $"TMap<{keyType}, {valueType}>";
    }

    /// <summary>
    /// Maps a map value type. Can be primitive or a custom (message/enum) type.
    /// </summary>
    private string MapMapValueType(string protoType, bool isEnum)
    {
        if (PrimitiveTypeMap.TryGetValue(protoType, out var primitiveType))
        {
            return primitiveType;
        }

        return ConvertToTypeName(protoType, isEnum);
    }

    /// <summary>
    /// Maps a Proto3 primitive type name to its Unreal equivalent.
    /// Used for map key types which are always primitives.
    /// </summary>
    private static string MapPrimitiveType(string protoType)
    {
        return PrimitiveTypeMap.TryGetValue(protoType, out var mapped) ? mapped : protoType;
    }
}
