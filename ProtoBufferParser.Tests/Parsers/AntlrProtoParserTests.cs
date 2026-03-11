using ProtoBufferParser.Parsers;
using ProtoBufferParser.Tests.Helpers;

namespace ProtoBufferParser.Tests.Parsers;

/// <summary>
/// Integration tests for <see cref="AntlrProtoParser"/>.
/// Tests the full pipeline: Lexer -> Two-pass Parser -> AST Builder.
/// </summary>
public class AntlrProtoParserTests
{
    private readonly MockLogger _logger = new();
    private readonly AntlrProtoParser _parser;

    public AntlrProtoParserTests()
    {
        _parser = new AntlrProtoParser(_logger);
    }

    [Fact]
    public void Parse_SimpleMessage_ReturnsCorrectAst()
    {
        var proto = """
            syntax = "proto3";
            message Player {
                string user_name = 1;
                int32 level = 2;
                bool is_active = 3;
            }
            """;

        var result = _parser.Parse(proto, "player.proto");

        Assert.Equal("player.proto", result.FileName);
        Assert.Single(result.Messages);
        Assert.Empty(result.Enums);

        var msg = result.Messages[0];
        Assert.Equal("Player", msg.Name);
        Assert.Equal(3, msg.Fields.Count);

        Assert.Equal("user_name", msg.Fields[0].Name);
        Assert.Equal("string", msg.Fields[0].Type);
        Assert.Equal(1, msg.Fields[0].FieldNumber);

        Assert.Equal("level", msg.Fields[1].Name);
        Assert.Equal("int32", msg.Fields[1].Type);
        Assert.Equal(2, msg.Fields[1].FieldNumber);

        Assert.Equal("is_active", msg.Fields[2].Name);
        Assert.Equal("bool", msg.Fields[2].Type);
        Assert.Equal(3, msg.Fields[2].FieldNumber);
    }

    [Fact]
    public void Parse_MessageWithRepeatedField_SetsIsRepeated()
    {
        var proto = """
            syntax = "proto3";
            message Inventory {
                repeated string items = 1;
            }
            """;

        var result = _parser.Parse(proto, "inventory.proto");

        var field = Assert.Single(result.Messages[0].Fields);
        Assert.Equal("items", field.Name);
        Assert.True(field.IsRepeated);
        Assert.False(field.IsOptional);
    }

    [Fact]
    public void Parse_MessageWithOptionalField_SetsIsOptional()
    {
        var proto = """
            syntax = "proto3";
            message Profile {
                optional string nickname = 1;
            }
            """;

        var result = _parser.Parse(proto, "profile.proto");

        var field = Assert.Single(result.Messages[0].Fields);
        Assert.Equal("nickname", field.Name);
        Assert.True(field.IsOptional);
        Assert.False(field.IsRepeated);
    }

    [Fact]
    public void Parse_MessageWithMapField_SetsMapProperties()
    {
        var proto = """
            syntax = "proto3";
            message Config {
                map<string, int32> settings = 1;
            }
            """;

        var result = _parser.Parse(proto, "config.proto");

        var field = Assert.Single(result.Messages[0].Fields);
        Assert.Equal("settings", field.Name);
        Assert.True(field.IsMap);
        Assert.Equal("string", field.MapKeyType);
        Assert.Equal("int32", field.MapValueType);
    }

    [Fact]
    public void Parse_MessageWithOneofField_FlattensAsOptional()
    {
        var proto = """
            syntax = "proto3";
            message Event {
                oneof payload {
                    string text_data = 1;
                    int32 int_data = 2;
                }
            }
            """;

        var result = _parser.Parse(proto, "event.proto");

        var msg = result.Messages[0];
        Assert.Equal(2, msg.Fields.Count);

        Assert.Equal("text_data", msg.Fields[0].Name);
        Assert.True(msg.Fields[0].IsOneOf);
        Assert.False(msg.Fields[0].IsOptional);
        Assert.Equal("payload", msg.Fields[0].OneOfGroupName);
        Assert.Equal("string", msg.Fields[0].Type);

        Assert.Equal("int_data", msg.Fields[1].Name);
        Assert.True(msg.Fields[1].IsOneOf);
        Assert.False(msg.Fields[1].IsOptional);
        Assert.Equal("payload", msg.Fields[1].OneOfGroupName);
        Assert.Equal("int32", msg.Fields[1].Type);
    }

    [Fact]
    public void Parse_EnumDefinition_ReturnsCorrectValues()
    {
        var proto = """
            syntax = "proto3";
            enum Status {
                STATUS_UNKNOWN = 0;
                STATUS_ACTIVE = 1;
                STATUS_INACTIVE = 2;
            }
            """;

        var result = _parser.Parse(proto, "status.proto");

        Assert.Empty(result.Messages);
        var enumNode = Assert.Single(result.Enums);
        Assert.Equal("Status", enumNode.Name);
        Assert.Equal(3, enumNode.Values.Count);

        Assert.Equal("STATUS_UNKNOWN", enumNode.Values[0].Name);
        Assert.Equal(0, enumNode.Values[0].Value);
        Assert.Equal("STATUS_ACTIVE", enumNode.Values[1].Name);
        Assert.Equal(1, enumNode.Values[1].Value);
        Assert.Equal("STATUS_INACTIVE", enumNode.Values[2].Name);
        Assert.Equal(2, enumNode.Values[2].Value);
    }

    [Fact]
    public void Parse_NestedMessage_CreatesNestedStructure()
    {
        var proto = """
            syntax = "proto3";
            message Outer {
                string name = 1;
                message Inner {
                    int32 value = 1;
                }
                Inner detail = 2;
            }
            """;

        var result = _parser.Parse(proto, "nested.proto");

        var outer = Assert.Single(result.Messages);
        Assert.Equal("Outer", outer.Name);
        Assert.Equal(2, outer.Fields.Count);

        var inner = Assert.Single(outer.NestedMessages);
        Assert.Equal("Inner", inner.Name);
        Assert.Single(inner.Fields);
        Assert.Equal("value", inner.Fields[0].Name);
    }

    [Fact]
    public void Parse_NestedEnum_CreatesNestedStructure()
    {
        var proto = """
            syntax = "proto3";
            message Player {
                enum Rank {
                    RANK_UNKNOWN = 0;
                    RANK_BRONZE = 1;
                }
                Rank rank = 1;
            }
            """;

        var result = _parser.Parse(proto, "player.proto");

        var msg = Assert.Single(result.Messages);
        var nestedEnum = Assert.Single(msg.NestedEnums);
        Assert.Equal("Rank", nestedEnum.Name);
        Assert.Equal(2, nestedEnum.Values.Count);
    }

    [Fact]
    public void Parse_ImportStatement_ParsesCorrectly()
    {
        var proto = """
            syntax = "proto3";
            import "common.proto";
            import public "shared.proto";
            message Foo {
                int32 id = 1;
            }
            """;

        var result = _parser.Parse(proto, "foo.proto");

        Assert.Equal(2, result.Imports.Count);
        Assert.Equal("common.proto", result.Imports[0].Path);
        Assert.False(result.Imports[0].IsPublic);
        Assert.Equal("shared.proto", result.Imports[1].Path);
        Assert.True(result.Imports[1].IsPublic);
    }

    [Fact]
    public void Parse_PackageStatement_SetsPackage()
    {
        var proto = """
            syntax = "proto3";
            package game.proto;
            message Empty {}
            """;

        var result = _parser.Parse(proto, "empty.proto");

        Assert.Equal("game.proto", result.Package);
    }

    [Fact]
    public void Parse_MultipleMessages_ReturnsAll()
    {
        var proto = """
            syntax = "proto3";
            message Player {
                string name = 1;
            }
            message Item {
                int32 id = 1;
                string name = 2;
            }
            """;

        var result = _parser.Parse(proto, "multi.proto");

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("Player", result.Messages[0].Name);
        Assert.Equal("Item", result.Messages[1].Name);
    }

    [Fact]
    public void Parse_AllPrimitiveTypes_ParsesCorrectly()
    {
        var proto = """
            syntax = "proto3";
            message AllTypes {
                double d = 1;
                float f = 2;
                int32 i32 = 3;
                int64 i64 = 4;
                uint32 u32 = 5;
                uint64 u64 = 6;
                sint32 s32 = 7;
                sint64 s64 = 8;
                fixed32 f32 = 9;
                fixed64 f64 = 10;
                sfixed32 sf32 = 11;
                sfixed64 sf64 = 12;
                bool b = 13;
                string s = 14;
                bytes by = 15;
            }
            """;

        var result = _parser.Parse(proto, "alltypes.proto");

        var msg = result.Messages[0];
        Assert.Equal(15, msg.Fields.Count);
        Assert.Equal("double", msg.Fields[0].Type);
        Assert.Equal("float", msg.Fields[1].Type);
        Assert.Equal("int32", msg.Fields[2].Type);
        Assert.Equal("int64", msg.Fields[3].Type);
        Assert.Equal("uint32", msg.Fields[4].Type);
        Assert.Equal("uint64", msg.Fields[5].Type);
        Assert.Equal("sint32", msg.Fields[6].Type);
        Assert.Equal("sint64", msg.Fields[7].Type);
        Assert.Equal("fixed32", msg.Fields[8].Type);
        Assert.Equal("fixed64", msg.Fields[9].Type);
        Assert.Equal("sfixed32", msg.Fields[10].Type);
        Assert.Equal("sfixed64", msg.Fields[11].Type);
        Assert.Equal("bool", msg.Fields[12].Type);
        Assert.Equal("string", msg.Fields[13].Type);
        Assert.Equal("bytes", msg.Fields[14].Type);
    }

    [Fact]
    public void Parse_MessageTypeReference_ParsesAsType()
    {
        var proto = """
            syntax = "proto3";
            message Address {
                string city = 1;
            }
            message Player {
                Address home = 1;
                repeated Address offices = 2;
            }
            """;

        var result = _parser.Parse(proto, "ref.proto");

        var player = result.Messages[1];
        Assert.Equal("Address", player.Fields[0].Type);
        Assert.False(player.Fields[0].IsRepeated);

        Assert.Equal("Address", player.Fields[1].Type);
        Assert.True(player.Fields[1].IsRepeated);
    }

    [Fact]
    public void Parse_NullContent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!, "test.proto"));
    }

    [Fact]
    public void Parse_NullFileName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse("syntax = \"proto3\";", null!));
    }

    [Fact]
    public void Parse_InvalidSyntax_ThrowsProtoSyntaxException()
    {
        var proto = "this is not valid proto";
        Assert.Throws<Exceptions.ProtoSyntaxException>(() => _parser.Parse(proto, "bad.proto"));
    }

    [Fact]
    public void Parse_EmptyMessage_ReturnsEmptyFields()
    {
        var proto = """
            syntax = "proto3";
            message Empty {}
            """;

        var result = _parser.Parse(proto, "empty.proto");

        var msg = Assert.Single(result.Messages);
        Assert.Equal("Empty", msg.Name);
        Assert.Empty(msg.Fields);
    }

    [Fact]
    public void Parse_ComplexProto_IntegrationTest()
    {
        var proto = """
            syntax = "proto3";
            package game;
            
            import "common.proto";
            
            enum GameState {
                GAME_STATE_UNKNOWN = 0;
                GAME_STATE_LOBBY = 1;
                GAME_STATE_PLAYING = 2;
            }
            
            message Player {
                string name = 1;
                int32 level = 2;
                repeated string inventory = 3;
                
                enum Rank {
                    RANK_UNKNOWN = 0;
                    RANK_GOLD = 1;
                }
                Rank rank = 4;
                
                message Stats {
                    int32 wins = 1;
                    int32 losses = 2;
                }
                Stats stats = 5;
                
                oneof action {
                    string chat = 6;
                    int32 move = 7;
                }
                
                map<string, int32> attributes = 8;
                optional bool premium = 9;
            }
            """;

        var result = _parser.Parse(proto, "game.proto");

        Assert.Equal("game", result.Package);
        Assert.Single(result.Imports);
        Assert.Single(result.Enums); // GameState

        var player = result.Messages[0];
        Assert.Equal("Player", player.Name);

        // Fields: name, level, inventory, rank, stats, chat, move, attributes, premium = 9
        Assert.Equal(9, player.Fields.Count);

        // Repeated field
        Assert.True(player.Fields[2].IsRepeated);

        // Oneof fields (flattened with IsOneOf)
        Assert.True(player.Fields[5].IsOneOf); // chat
        Assert.True(player.Fields[6].IsOneOf); // move
        Assert.Equal("action", player.Fields[5].OneOfGroupName);
        Assert.Equal("action", player.Fields[6].OneOfGroupName);

        // Map field
        Assert.True(player.Fields[7].IsMap);

        // Optional field
        Assert.True(player.Fields[8].IsOptional);

        // Nested message & enum
        Assert.Single(player.NestedMessages);
        Assert.Equal("Stats", player.NestedMessages[0].Name);
        Assert.Single(player.NestedEnums);
        Assert.Equal("Rank", player.NestedEnums[0].Name);
    }
}
