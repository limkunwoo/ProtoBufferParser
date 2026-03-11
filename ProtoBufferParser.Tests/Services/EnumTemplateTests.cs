using ProtoBufferParser.Models;
using ProtoBufferParser.Services;

namespace ProtoBufferParser.Tests.Services;

public sealed class EnumTemplateTests
{
    private readonly UnrealTypeMapper _typeMapper = new();

    private static EnumNode CreateSimpleEnum(string name = "Status")
    {
        return new EnumNode
        {
            Name = name,
            FullName = name,
            Values = new List<EnumValueNode>
            {
                new() { Name = "UNKNOWN", Value = 0 },
                new() { Name = "ACTIVE", Value = 1 },
                new() { Name = "INACTIVE", Value = 2 }
            }
        };
    }

    [Fact]
    public void GenerateHeader_ContainsPragmaOnce()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("#pragma once", result);
    }

    [Fact]
    public void GenerateHeader_ContainsCoreMinimalInclude()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("#include \"CoreMinimal.h\"", result);
    }

    [Fact]
    public void GenerateHeader_ContainsGeneratedInclude()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("#include \"MEStatusProto.generated.h\"", result);
    }

    [Fact]
    public void GenerateHeader_ContainsUEnumMacro()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("UENUM(BlueprintType)", result);
    }

    [Fact]
    public void GenerateHeader_ContainsCorrectEnumClassName()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("enum class EStatusProto : uint8", result);
    }

    [Fact]
    public void GenerateHeader_ContainsAllValues()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("Unknown = 0", result);
        Assert.Contains("Active = 1", result);
        Assert.Contains("Inactive = 2", result);
    }

    [Fact]
    public void GenerateHeader_ContainsUMetaDisplayName()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("UMETA(DisplayName = \"Unknown\")", result);
        Assert.Contains("UMETA(DisplayName = \"Active\")", result);
        Assert.Contains("UMETA(DisplayName = \"Inactive\")", result);
    }

    [Fact]
    public void GenerateHeader_LastValueHasNoComma()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        // Last value (Inactive) should not have a trailing comma
        var lines = result.Split('\n');
        var inactiveLine = lines.First(l => l.Contains("Inactive = 2"));
        Assert.DoesNotContain(",", inactiveLine);
    }

    [Fact]
    public void GenerateHeader_NonLastValuesHaveComma()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateHeader(enumNode);

        var lines = result.Split('\n');
        var unknownLine = lines.First(l => l.Contains("Unknown = 0"));
        Assert.Contains(",", unknownLine);
    }

    [Fact]
    public void GenerateCpp_ReturnsNull()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = CreateSimpleEnum();

        var result = template.GenerateCpp(enumNode, "MEStatusProto.h");

        Assert.Null(result);
    }

    [Fact]
    public void GenerateHeader_FlattenedEnumName_UsesFullName()
    {
        var template = new EnumTemplate(_typeMapper);
        var enumNode = new EnumNode
        {
            Name = "Type",
            FullName = "PlayerType",
            Values = new List<EnumValueNode>
            {
                new() { Name = "UNKNOWN", Value = 0 }
            }
        };

        var result = template.GenerateHeader(enumNode);

        Assert.Contains("enum class EPlayerTypeProto : uint8", result);
        Assert.Contains("#include \"MEPlayerTypeProto.generated.h\"", result);
    }

    [Fact]
    public void GenerateHeader_NullEnumNode_ThrowsArgumentNullException()
    {
        var template = new EnumTemplate(_typeMapper);

        Assert.Throws<ArgumentNullException>(() =>
            template.GenerateHeader(null!));
    }

    [Fact]
    public void Constructor_NullTypeMapper_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EnumTemplate(null!));
    }
}
