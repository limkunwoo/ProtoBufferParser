using ProtoBufferParser.Models;
using ProtoBufferParser.Services;

namespace ProtoBufferParser.Tests.Services;

/// <summary>
/// Unit tests for UnrealTypeMapper.
/// </summary>
public class UnrealTypeMapperTests
{
    private readonly UnrealTypeMapper _mapper = new();

    // ===== Primitive scalar types =====

    [Theory]
    [InlineData("int32", "int32")]
    [InlineData("int64", "int64")]
    [InlineData("uint32", "uint32")]
    [InlineData("uint64", "uint64")]
    [InlineData("sint32", "int32")]
    [InlineData("sint64", "int64")]
    [InlineData("fixed32", "uint32")]
    [InlineData("fixed64", "uint64")]
    [InlineData("sfixed32", "int32")]
    [InlineData("sfixed64", "int64")]
    [InlineData("float", "float")]
    [InlineData("double", "double")]
    [InlineData("bool", "bool")]
    [InlineData("string", "FString")]
    [InlineData("bytes", "TArray<uint8>")]
    public void MapFieldType_PrimitiveTypes_MapsCorrectly(string protoType, string expectedUnrealType)
    {
        var field = new FieldNode { Type = protoType };
        Assert.Equal(expectedUnrealType, _mapper.MapFieldType(field));
    }

    // ===== Repeated scalar types =====

    [Theory]
    [InlineData("int32", "TArray<int32>")]
    [InlineData("string", "TArray<FString>")]
    [InlineData("float", "TArray<float>")]
    [InlineData("bool", "TArray<bool>")]
    [InlineData("bytes", "TArray<TArray<uint8>>")]
    public void MapFieldType_RepeatedPrimitive_WrapsInTArray(string protoType, string expectedUnrealType)
    {
        var field = new FieldNode { Type = protoType, IsRepeated = true };
        Assert.Equal(expectedUnrealType, _mapper.MapFieldType(field));
    }

    // ===== Message type references =====

    [Fact]
    public void MapFieldType_MessageReference_AppliesFPrefixAndProtoSuffix()
    {
        var field = new FieldNode { Type = "Player", IsEnum = false };
        Assert.Equal("FPlayerProto", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_RepeatedMessage_WrapsInTArray()
    {
        var field = new FieldNode { Type = "Player", IsRepeated = true, IsEnum = false };
        Assert.Equal("TArray<FPlayerProto>", _mapper.MapFieldType(field));
    }

    // ===== Enum type references =====

    [Fact]
    public void MapFieldType_EnumReference_AppliesEPrefixAndProtoSuffix()
    {
        var field = new FieldNode { Type = "Status", IsEnum = true };
        Assert.Equal("EStatusProto", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_RepeatedEnum_WrapsInTArray()
    {
        var field = new FieldNode { Type = "Status", IsRepeated = true, IsEnum = true };
        Assert.Equal("TArray<EStatusProto>", _mapper.MapFieldType(field));
    }

    // ===== Map fields =====

    [Fact]
    public void MapFieldType_MapStringToInt_ReturnsTMap()
    {
        var field = new FieldNode
        {
            IsMap = true,
            MapKeyType = "string",
            MapValueType = "int32",
            Type = "map"
        };
        Assert.Equal("TMap<FString, int32>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_MapIntToString_ReturnsTMap()
    {
        var field = new FieldNode
        {
            IsMap = true,
            MapKeyType = "int32",
            MapValueType = "string",
            Type = "map"
        };
        Assert.Equal("TMap<int32, FString>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_MapStringToMessage_ReturnsTMapWithFPrefix()
    {
        var field = new FieldNode
        {
            IsMap = true,
            MapKeyType = "string",
            MapValueType = "Player",
            MapValueIsEnum = false,
            Type = "map"
        };
        Assert.Equal("TMap<FString, FPlayerProto>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_MapStringToEnum_ReturnsTMapWithEPrefix()
    {
        var field = new FieldNode
        {
            IsMap = true,
            MapKeyType = "string",
            MapValueType = "Status",
            MapValueIsEnum = true,
            Type = "map"
        };
        Assert.Equal("TMap<FString, EStatusProto>", _mapper.MapFieldType(field));
    }

    // ===== ConvertToTypeName =====

    [Theory]
    [InlineData("Player", false, "FPlayerProto")]
    [InlineData("GameSession", false, "FGameSessionProto")]
    [InlineData("Status", true, "EStatusProto")]
    [InlineData("PlayerState", true, "EPlayerStateProto")]
    public void ConvertToTypeName_AppliesCorrectPrefixAndSuffix(
        string protoType, bool isEnum, string expected)
    {
        Assert.Equal(expected, _mapper.ConvertToTypeName(protoType, isEnum));
    }

    // ===== GetOutputFileName =====

    [Theory]
    [InlineData("Player", "MEPlayerProto")]
    [InlineData("GameSession", "MEGameSessionProto")]
    [InlineData("PlayerInventory", "MEPlayerInventoryProto")]
    public void GetOutputFileName_AppliesMEPrefixAndProtoSuffix(string typeName, string expected)
    {
        Assert.Equal(expected, _mapper.GetOutputFileName(typeName));
    }

    // ===== ConvertFieldName =====

    [Theory]
    [InlineData("player_name", "PlayerName")]
    [InlineData("id", "Id")]
    [InlineData("is_active", "IsActive")]
    [InlineData("session_id", "SessionId")]
    public void ConvertFieldName_ConvertsToPascalCase(string protoFieldName, string expected)
    {
        Assert.Equal(expected, _mapper.ConvertFieldName(protoFieldName));
    }

    // ===== ConvertEnumValueName =====

    [Theory]
    [InlineData("UNKNOWN", "Unknown")]
    [InlineData("ACTIVE_STATE", "ActiveState")]
    [InlineData("STATUS_UNKNOWN", "StatusUnknown")]
    public void ConvertEnumValueName_ConvertsToPascalCase(string protoValue, string expected)
    {
        Assert.Equal(expected, _mapper.ConvertEnumValueName(protoValue));
    }

    // ===== IsPrimitiveType =====

    [Theory]
    [InlineData("int32", true)]
    [InlineData("string", true)]
    [InlineData("bytes", true)]
    [InlineData("float", true)]
    [InlineData("double", true)]
    [InlineData("bool", true)]
    [InlineData("Player", false)]
    [InlineData("Status", false)]
    [InlineData("map", false)]
    public void IsPrimitiveType_ReturnsCorrectResult(string protoType, bool expected)
    {
        Assert.Equal(expected, _mapper.IsPrimitiveType(protoType));
    }

    // ===== TOptional wrapping for optional fields =====

    [Fact]
    public void MapFieldType_OptionalPrimitive_WrapsTOptional()
    {
        var field = new FieldNode { Type = "int32", IsOptional = true };
        Assert.Equal("TOptional<int32>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OptionalString_WrapsTOptional()
    {
        var field = new FieldNode { Type = "string", IsOptional = true };
        Assert.Equal("TOptional<FString>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OptionalBytes_WrapsTOptional()
    {
        var field = new FieldNode { Type = "bytes", IsOptional = true };
        Assert.Equal("TOptional<TArray<uint8>>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OptionalEnum_WrapsTOptional()
    {
        var field = new FieldNode { Type = "Status", IsOptional = true, IsEnum = true };
        Assert.Equal("TOptional<EStatusProto>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OptionalMessage_WrapsTOptional()
    {
        var field = new FieldNode { Type = "Player", IsOptional = true, IsEnum = false };
        Assert.Equal("TOptional<FPlayerProto>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OptionalBool_WrapsTOptional()
    {
        var field = new FieldNode { Type = "bool", IsOptional = true };
        Assert.Equal("TOptional<bool>", _mapper.MapFieldType(field));
    }

    // ===== TOptional wrapping for oneof fields =====

    [Fact]
    public void MapFieldType_OneOfPrimitive_WrapsTOptional()
    {
        var field = new FieldNode { Type = "int32", IsOneOf = true, OneOfGroupName = "payload" };
        Assert.Equal("TOptional<int32>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OneOfString_WrapsTOptional()
    {
        var field = new FieldNode { Type = "string", IsOneOf = true, OneOfGroupName = "event" };
        Assert.Equal("TOptional<FString>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OneOfMessage_WrapsTOptional()
    {
        var field = new FieldNode { Type = "DamageInfo", IsOneOf = true, OneOfGroupName = "event", IsEnum = false };
        Assert.Equal("TOptional<FDamageInfoProto>", _mapper.MapFieldType(field));
    }

    [Fact]
    public void MapFieldType_OneOfEnum_WrapsTOptional()
    {
        var field = new FieldNode { Type = "Status", IsOneOf = true, OneOfGroupName = "event", IsEnum = true };
        Assert.Equal("TOptional<EStatusProto>", _mapper.MapFieldType(field));
    }

    // ===== Repeated fields should NOT be wrapped in TOptional =====

    [Fact]
    public void MapFieldType_RepeatedOptional_DoesNotWrapTOptional()
    {
        // repeated + optional is not valid in proto3, but ensure we don't double-wrap
        var field = new FieldNode { Type = "int32", IsRepeated = true, IsOptional = true };
        Assert.Equal("TArray<int32>", _mapper.MapFieldType(field));
    }
}
