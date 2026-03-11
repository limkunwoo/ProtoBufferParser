using System.Text;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Services;

/// <summary>
/// Generates Unreal C++ enum header (.h) and implementation (.cpp) files
/// from an EnumNode AST.
/// </summary>
public sealed class EnumTemplate
{
    private readonly ITypeMapper _typeMapper;

    public EnumTemplate(ITypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);
        _typeMapper = typeMapper;
    }

    /// <summary>
    /// Generates the header (.h) content for an enum.
    /// </summary>
    /// <param name="enumNode">The enum node from the AST.</param>
    /// <returns>The header file content string.</returns>
    public string GenerateHeader(EnumNode enumNode)
    {
        ArgumentNullException.ThrowIfNull(enumNode);

        var enumName = _typeMapper.ConvertToTypeName(enumNode.FullName, isEnum: true);
        var fileName = _typeMapper.GetOutputFileName(enumNode.FullName);

        var sb = new StringBuilder();

        // #pragma once
        sb.AppendLine("#pragma once");
        sb.AppendLine();

        // Includes
        sb.AppendLine("#include \"CoreMinimal.h\"");
        sb.AppendLine($"#include \"{fileName}.generated.h\"");
        sb.AppendLine();

        // UENUM declaration
        sb.AppendLine("UENUM(BlueprintType)");
        sb.AppendLine($"enum class {enumName} : uint8");
        sb.AppendLine("{");

        // Enum values
        for (int i = 0; i < enumNode.Values.Count; i++)
        {
            var enumValue = enumNode.Values[i];
            var valueName = _typeMapper.ConvertEnumValueName(enumValue.Name);
            var displayName = valueName;
            var separator = (i < enumNode.Values.Count - 1) ? "," : "";

            sb.AppendLine($"    {valueName} = {enumValue.Value} UMETA(DisplayName = \"{displayName}\"){separator}");
        }

        sb.AppendLine("};");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the implementation (.cpp) content for an enum.
    /// Enums typically don't need .cpp files, but this generates a minimal one
    /// with just the include for consistency.
    /// </summary>
    /// <param name="enumNode">The enum node from the AST.</param>
    /// <param name="headerFileName">The header file name (with extension).</param>
    /// <returns>The cpp file content string, or null if no .cpp is needed.</returns>
    public string? GenerateCpp(EnumNode enumNode, string headerFileName)
    {
        // Enums don't need implementation files — they are fully defined in the header.
        return null;
    }
}
