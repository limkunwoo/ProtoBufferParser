using System.Text;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;
using ProtoBufferParser.Utilities;

namespace ProtoBufferParser.Services;

/// <summary>
/// Generates Unreal C++ struct header (.h) and implementation (.cpp) files
/// from a MessageNode AST, including marshaling constructors.
/// </summary>
public sealed class StructTemplate
{
    private readonly ITypeMapper _typeMapper;
    private readonly CodeGeneratorOptions _options;

    public StructTemplate(ITypeMapper typeMapper, CodeGeneratorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);
        _typeMapper = typeMapper;
        _options = options ?? new CodeGeneratorOptions();
    }

    /// <summary>
    /// Generates the header (.h) content for a struct.
    /// </summary>
    /// <param name="message">The flattened message node.</param>
    /// <param name="dependencies">List of dependency include file names (without .h).</param>
    /// <param name="package">The proto package name (e.g., "foo.bar"), empty if none.</param>
    /// <returns>The header file content string.</returns>
    public string GenerateHeader(MessageNode message, List<string> dependencies, string package = "")
    {
        ArgumentNullException.ThrowIfNull(message);

        var structName = _typeMapper.ConvertToTypeName(message.FullName, isEnum: false);
        var fileName = _typeMapper.GetOutputFileName(message.FullName);

        var sb = new StringBuilder();

        // #pragma once
        sb.AppendLine("#pragma once");
        sb.AppendLine();

        // Includes
        sb.AppendLine("#include \"CoreMinimal.h\"");

        foreach (var dep in dependencies)
        {
            sb.AppendLine($"#include \"{dep}.h\"");
        }

        sb.AppendLine($"#include \"{fileName}.generated.h\"");
        sb.AppendLine();

        // USTRUCT declaration
        sb.AppendLine("USTRUCT(BlueprintType)");
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");
        sb.AppendLine("    GENERATED_BODY()");
        sb.AppendLine();

        // Default constructor
        sb.AppendLine($"    {structName}() = default;");
        sb.AppendLine();

        // Marshaling constructor declaration
        var protoTypeName = BuildProtocFullTypeName(message.ProtocTypeName, package);
        sb.AppendLine($"    // Marshal from protobuf message");
        sb.AppendLine($"    explicit {structName}(const {protoTypeName}& proto);");

        // Fields
        foreach (var field in message.Fields)
        {
            sb.AppendLine();
            var fieldType = _typeMapper.MapFieldType(field);
            var fieldName = _typeMapper.ConvertFieldName(field.Name);
            var uPropertyAttr = BuildUPropertyAttribute();

            sb.AppendLine($"    {uPropertyAttr}");
            sb.AppendLine($"    {fieldType} {fieldName};");
        }

        sb.AppendLine("};");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the implementation (.cpp) content for a struct,
    /// including the full marshaling constructor.
    /// </summary>
    /// <param name="message">The flattened message node.</param>
    /// <param name="headerFileName">The header file name (with extension).</param>
    /// <param name="sourceProtoFileName">The source .proto file name for #include of .pb.h.</param>
    /// <param name="package">The proto package name (e.g., "foo.bar"), empty if none.</param>
    /// <returns>The cpp file content string.</returns>
    public string GenerateCpp(MessageNode message, string headerFileName, string sourceProtoFileName, string package = "")
    {
        ArgumentNullException.ThrowIfNull(message);

        var structName = _typeMapper.ConvertToTypeName(message.FullName, isEnum: false);
        var protocHeader = NamingHelper.ToProtocHeaderName(sourceProtoFileName);

        var sb = new StringBuilder();

        // Includes
        sb.AppendLine($"#include \"{headerFileName}\"");
        sb.AppendLine($"#include \"{protocHeader}\"");
        sb.AppendLine();

        // Marshaling constructor
        var protoTypeName = BuildProtocFullTypeName(message.ProtocTypeName, package);
        sb.AppendLine($"{structName}::{structName}(const {protoTypeName}& proto)");
        sb.AppendLine("{");

        foreach (var field in message.Fields)
        {
            GenerateFieldMarshaling(sb, field);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the marshaling code for a single field inside the constructor body.
    /// </summary>
    private void GenerateFieldMarshaling(StringBuilder sb, FieldNode field)
    {
        var fieldName = _typeMapper.ConvertFieldName(field.Name);
        var protoFieldName = ToProtoAccessorName(field.Name);

        // Optional and oneof fields use has_xxx() conditional marshaling
        if ((field.IsOptional || field.IsOneOf) && !field.IsRepeated && !field.IsMap)
        {
            GenerateOptionalFieldMarshaling(sb, field, fieldName, protoFieldName);
            return;
        }

        if (field.IsMap)
        {
            GenerateMapFieldMarshaling(sb, field, fieldName, protoFieldName);
        }
        else if (field.IsRepeated)
        {
            GenerateRepeatedFieldMarshaling(sb, field, fieldName, protoFieldName);
        }
        else if (field.Type == "string")
        {
            sb.AppendLine($"    {fieldName} = FString(UTF8_TO_TCHAR(proto.{protoFieldName}().c_str()));");
        }
        else if (field.Type == "bytes")
        {
            GenerateBytesFieldMarshaling(sb, fieldName, protoFieldName);
        }
        else if (field.IsEnum)
        {
            var enumTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: true);
            sb.AppendLine($"    {fieldName} = static_cast<{enumTypeName}>(proto.{protoFieldName}());");
        }
        else if (!_typeMapper.IsPrimitiveType(field.Type))
        {
            // Message type — recursive marshaling constructor
            var messageTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: false);
            sb.AppendLine($"    {fieldName} = {messageTypeName}(proto.{protoFieldName}());");
        }
        else
        {
            // Primitive numeric/bool — direct assignment
            sb.AppendLine($"    {fieldName} = proto.{protoFieldName}();");
        }
    }

    /// <summary>
    /// Generates marshaling code for optional/oneof fields using has_xxx() guard
    /// and TOptional assignment.
    /// </summary>
    private void GenerateOptionalFieldMarshaling(
        StringBuilder sb, FieldNode field, string fieldName, string protoFieldName)
    {
        sb.AppendLine($"    if (proto.has_{protoFieldName}())");
        sb.AppendLine("    {");

        if (field.Type == "string")
        {
            sb.AppendLine($"        {fieldName} = FString(UTF8_TO_TCHAR(proto.{protoFieldName}().c_str()));");
        }
        else if (field.Type == "bytes")
        {
            sb.AppendLine($"        const std::string& _bytes_{protoFieldName} = proto.{protoFieldName}();");
            sb.AppendLine($"        TArray<uint8> _temp_{protoFieldName};");
            sb.AppendLine($"        _temp_{protoFieldName}.SetNum(_bytes_{protoFieldName}.size());");
            sb.AppendLine($"        FMemory::Memcpy(_temp_{protoFieldName}.GetData(), _bytes_{protoFieldName}.data(), _bytes_{protoFieldName}.size());");
            sb.AppendLine($"        {fieldName} = MoveTemp(_temp_{protoFieldName});");
        }
        else if (field.IsEnum)
        {
            var enumTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: true);
            sb.AppendLine($"        {fieldName} = static_cast<{enumTypeName}>(proto.{protoFieldName}());");
        }
        else if (!_typeMapper.IsPrimitiveType(field.Type))
        {
            var messageTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: false);
            sb.AppendLine($"        {fieldName} = {messageTypeName}(proto.{protoFieldName}());");
        }
        else
        {
            // Primitive numeric/bool
            sb.AppendLine($"        {fieldName} = proto.{protoFieldName}();");
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Generates marshaling code for a repeated field.
    /// </summary>
    private void GenerateRepeatedFieldMarshaling(
        StringBuilder sb, FieldNode field, string fieldName, string protoFieldName)
    {
        sb.AppendLine($"    {fieldName}.Reserve(proto.{protoFieldName}_size());");
        sb.AppendLine($"    for (int i = 0; i < proto.{protoFieldName}_size(); ++i)");
        sb.AppendLine("    {");

        if (field.Type == "string")
        {
            sb.AppendLine($"        {fieldName}.Add(FString(UTF8_TO_TCHAR(proto.{protoFieldName}(i).c_str())));");
        }
        else if (field.Type == "bytes")
        {
            // Repeated bytes is unusual but handle it
            sb.AppendLine($"        const std::string& elem = proto.{protoFieldName}(i);");
            sb.AppendLine($"        TArray<uint8> temp;");
            sb.AppendLine($"        temp.SetNum(elem.size());");
            sb.AppendLine($"        FMemory::Memcpy(temp.GetData(), elem.data(), elem.size());");
            sb.AppendLine($"        {fieldName}.Add(MoveTemp(temp));");
        }
        else if (field.IsEnum)
        {
            var enumTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: true);
            sb.AppendLine($"        {fieldName}.Add(static_cast<{enumTypeName}>(proto.{protoFieldName}(i)));");
        }
        else if (!_typeMapper.IsPrimitiveType(field.Type))
        {
            var messageTypeName = _typeMapper.ConvertToTypeName(field.Type, isEnum: false);
            sb.AppendLine($"        {fieldName}.Add({messageTypeName}(proto.{protoFieldName}(i)));");
        }
        else
        {
            sb.AppendLine($"        {fieldName}.Add(proto.{protoFieldName}(i));");
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Generates marshaling code for a map field.
    /// </summary>
    private void GenerateMapFieldMarshaling(
        StringBuilder sb, FieldNode field, string fieldName, string protoFieldName)
    {
        sb.AppendLine($"    for (const auto& [key, value] : proto.{protoFieldName}())");
        sb.AppendLine("    {");

        var keyConversion = GenerateMapKeyConversion(field.MapKeyType);
        var valueConversion = GenerateMapValueConversion(field.MapValueType, field.MapValueIsEnum);

        sb.AppendLine($"        {fieldName}.Add({keyConversion}, {valueConversion});");

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Generates the bytes field marshaling (3-line pattern).
    /// </summary>
    private static void GenerateBytesFieldMarshaling(
        StringBuilder sb, string fieldName, string protoFieldName)
    {
        sb.AppendLine($"    const std::string& _bytes_{protoFieldName} = proto.{protoFieldName}();");
        sb.AppendLine($"    {fieldName}.SetNum(_bytes_{protoFieldName}.size());");
        sb.AppendLine($"    FMemory::Memcpy({fieldName}.GetData(), _bytes_{protoFieldName}.data(), _bytes_{protoFieldName}.size());");
    }

    /// <summary>
    /// Generates the conversion expression for a map key.
    /// Map keys in proto3 are always scalar types.
    /// </summary>
    private static string GenerateMapKeyConversion(string keyType)
    {
        return keyType == "string"
            ? "FString(UTF8_TO_TCHAR(key.c_str()))"
            : "key";
    }

    /// <summary>
    /// Generates the conversion expression for a map value.
    /// </summary>
    private string GenerateMapValueConversion(string valueType, bool isEnum)
    {
        if (valueType == "string")
        {
            return "FString(UTF8_TO_TCHAR(value.c_str()))";
        }

        if (valueType == "bytes")
        {
            // Bytes as map values is unusual; not commonly supported
            return "value";
        }

        if (isEnum)
        {
            var enumTypeName = _typeMapper.ConvertToTypeName(valueType, isEnum: true);
            return $"static_cast<{enumTypeName}>(value)";
        }

        if (!_typeMapper.IsPrimitiveType(valueType))
        {
            var messageTypeName = _typeMapper.ConvertToTypeName(valueType, isEnum: false);
            return $"{messageTypeName}(value)";
        }

        return "value";
    }

    /// <summary>
    /// Converts a Proto3 field name to the protoc-generated C++ accessor name (snake_case).
    /// protoc keeps field names as snake_case for getters.
    /// </summary>
    private static string ToProtoAccessorName(string protoFieldName)
    {
        // protoc uses the exact snake_case field name from the .proto file
        return protoFieldName.ToLowerInvariant();
    }

    /// <summary>
    /// Builds the UPROPERTY attribute string based on options.
    /// </summary>
    private string BuildUPropertyAttribute()
    {
        var parts = new List<string> { _options.PropertySpecifier };

        if (_options.GenerateBlueprintReadWrite)
        {
            parts.Add("BlueprintReadWrite");
        }

        parts.Add($"Category = \"{_options.CategoryName}\"");

        return $"UPROPERTY({string.Join(", ", parts)})";
    }

    /// <summary>
    /// Builds the fully-qualified C++ type name that protoc generates.
    /// If package is present, uses the package as namespace (e.g., "foo::bar::Player").
    /// If no package, uses global namespace (e.g., "::Player").
    /// For nested messages, protoc uses underscores (e.g., "::Player_Stats").
    /// </summary>
    private static string BuildProtocFullTypeName(string protocTypeName, string package)
    {
        if (string.IsNullOrEmpty(package))
        {
            return $"::{protocTypeName}";
        }

        var cppNamespace = package.Replace(".", "::");
        return $"{cppNamespace}::{protocTypeName}";
    }
}
