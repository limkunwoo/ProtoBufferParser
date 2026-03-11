using ProtoBufferParser.Utilities;

namespace ProtoBufferParser.Tests.Utilities;

/// <summary>
/// Unit tests for NamingHelper utility.
/// </summary>
public class NamingHelperTests
{
    // ===== ToPascalCase =====

    [Theory]
    [InlineData("player_name", "PlayerName")]
    [InlineData("id", "Id")]
    [InlineData("is_active", "IsActive")]
    [InlineData("user_id", "UserId")]
    [InlineData("first_name", "FirstName")]
    [InlineData("MAX_HP", "MaxHp")]
    public void ToPascalCase_SnakeCase_ConvertsProperly(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToPascalCase(input));
    }

    [Theory]
    [InlineData("playerName", "PlayerName")]
    [InlineData("firstName", "FirstName")]
    [InlineData("isActive", "IsActive")]
    public void ToPascalCase_CamelCase_ConvertsProperly(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToPascalCase(input));
    }

    [Theory]
    [InlineData("Player", "Player")]
    [InlineData("GameSession", "GameSession")]
    [InlineData("PlayerInfo", "PlayerInfo")]
    public void ToPascalCase_AlreadyPascalCase_Unchanged(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToPascalCase(input));
    }

    [Theory]
    [InlineData("UNKNOWN", "Unknown")]
    [InlineData("ACTIVE_STATE", "ActiveState")]
    [InlineData("STATUS_UNKNOWN", "StatusUnknown")]
    [InlineData("GAME_READY", "GameReady")]
    public void ToPascalCase_UpperSnakeCase_ConvertsProperly(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToPascalCase(input));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToPascalCase_NullOrEmpty_ReturnsAsIs(string? input, string? expected)
    {
        Assert.Equal(expected, NamingHelper.ToPascalCase(input!));
    }

    // ===== ToUnrealFieldName =====

    [Theory]
    [InlineData("player_name", "PlayerName")]
    [InlineData("id", "Id")]
    [InlineData("is_active", "IsActive")]
    [InlineData("session_id", "SessionId")]
    public void ToUnrealFieldName_ConvertsSnakeCaseToPascalCase(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToUnrealFieldName(input));
    }

    // ===== ToUnrealEnumValueName =====

    [Theory]
    [InlineData("UNKNOWN", "Unknown")]
    [InlineData("ACTIVE", "Active")]
    [InlineData("ACTIVE_STATE", "ActiveState")]
    [InlineData("STATUS_UNKNOWN", "StatusUnknown")]
    public void ToUnrealEnumValueName_ConvertsUpperSnakeCaseToPascalCase(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToUnrealEnumValueName(input));
    }

    // ===== ToUnrealStructName =====

    [Theory]
    [InlineData("Player", "FPlayerProto")]
    [InlineData("GameSession", "FGameSessionProto")]
    [InlineData("player_info", "FPlayerInfoProto")]
    [InlineData("PLAYER_INFO", "FPlayerInfoProto")]
    public void ToUnrealStructName_AppliesFPrefixAndProtoSuffix(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToUnrealStructName(input));
    }

    // ===== ToUnrealEnumName =====

    [Theory]
    [InlineData("Status", "EStatusProto")]
    [InlineData("PlayerState", "EPlayerStateProto")]
    [InlineData("GAME_MODE", "EGameModeProto")]
    public void ToUnrealEnumName_AppliesEPrefixAndProtoSuffix(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToUnrealEnumName(input));
    }

    // ===== ToUnrealTypeName =====

    [Fact]
    public void ToUnrealTypeName_Message_UsesFPrefix()
    {
        Assert.Equal("FPlayerProto", NamingHelper.ToUnrealTypeName("Player", isEnum: false));
    }

    [Fact]
    public void ToUnrealTypeName_Enum_UsesEPrefix()
    {
        Assert.Equal("EStatusProto", NamingHelper.ToUnrealTypeName("Status", isEnum: true));
    }

    // ===== ToOutputFileName =====

    [Theory]
    [InlineData("Player", "MEPlayerProto")]
    [InlineData("GameSession", "MEGameSessionProto")]
    [InlineData("PlayerInventory", "MEPlayerInventoryProto")]
    public void ToOutputFileName_AppliesMEPrefixAndProtoSuffix(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToOutputFileName(input));
    }

    // ===== FlattenNestedName =====

    [Fact]
    public void FlattenNestedName_TwoParts_ConcatenatesPascalCase()
    {
        Assert.Equal("PlayerInventory", NamingHelper.FlattenNestedName("Player", "Inventory"));
    }

    [Fact]
    public void FlattenNestedName_ThreeParts_ConcatenatesPascalCase()
    {
        Assert.Equal("PlayerInventoryItem", NamingHelper.FlattenNestedName("Player", "Inventory", "Item"));
    }

    [Fact]
    public void FlattenNestedName_WithSnakeCase_ConvertsToPascalCase()
    {
        Assert.Equal("PlayerGameState", NamingHelper.FlattenNestedName("Player", "game_state"));
    }

    // ===== ToProtocHeaderName =====

    [Theory]
    [InlineData("player.proto", "player.pb.h")]
    [InlineData("game_data.proto", "game_data.pb.h")]
    [InlineData("common", "common.pb.h")]
    public void ToProtocHeaderName_ConvertsToPbH(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ToProtocHeaderName(input));
    }

    // ===== ImportPathToInclude =====

    [Theory]
    [InlineData("common.proto", "MECommonProto.h")]
    [InlineData("game/player.proto", "MEPlayerProto.h")]
    [InlineData("game_data.proto", "MEGameDataProto.h")]
    public void ImportPathToInclude_ConvertsToUnrealInclude(string input, string expected)
    {
        Assert.Equal(expected, NamingHelper.ImportPathToInclude(input));
    }
}
