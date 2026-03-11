using ProtoBufferParser.Models;
using ProtoBufferParser.Services;

namespace ProtoBufferParser.Tests.Services;

public sealed class StructTemplateTests
{
    private readonly UnrealTypeMapper _typeMapper = new();

    private static MessageNode CreateSimpleMessage(string name = "Player", List<FieldNode>? fields = null)
    {
        return new MessageNode
        {
            Name = name,
            FullName = name,
            ProtocTypeName = name,
            Fields = fields ?? new List<FieldNode>
            {
                new() { Name = "id", Type = "int32", FieldNumber = 1 },
                new() { Name = "name", Type = "string", FieldNumber = 2 }
            }
        };
    }

    // ========== Header Generation ==========

    [Fact]
    public void GenerateHeader_ContainsPragmaOnce()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("#pragma once", result);
    }

    [Fact]
    public void GenerateHeader_ContainsCoreMinimalInclude()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("#include \"CoreMinimal.h\"", result);
    }

    [Fact]
    public void GenerateHeader_ContainsGeneratedInclude()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("#include \"MEPlayerProto.generated.h\"", result);
    }

    [Fact]
    public void GenerateHeader_ContainsDependencyIncludes()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();
        var deps = new List<string> { "MEItemProto", "MEStatusProto" };

        var result = template.GenerateHeader(message, deps);

        Assert.Contains("#include \"MEItemProto.h\"", result);
        Assert.Contains("#include \"MEStatusProto.h\"", result);
    }

    [Fact]
    public void GenerateHeader_ContainsUStructMacro()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("USTRUCT(BlueprintType)", result);
    }

    [Fact]
    public void GenerateHeader_ContainsCorrectStructName()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("struct FPlayerProto", result);
    }

    [Fact]
    public void GenerateHeader_ContainsGeneratedBody()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("GENERATED_BODY()", result);
    }

    [Fact]
    public void GenerateHeader_ContainsDefaultConstructor()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("FPlayerProto() = default;", result);
    }

    [Fact]
    public void GenerateHeader_ContainsMarshalingConstructorDeclaration()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("explicit FPlayerProto(const ::Player& proto);", result);
    }

    [Fact]
    public void GenerateHeader_ContainsUPropertyForEachField()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = \"Proto\")", result);
        Assert.Contains("int32 Id;", result);
        Assert.Contains("FString Name;", result);
    }

    [Fact]
    public void GenerateHeader_CustomOptions_AppliesPropertySpecifier()
    {
        var options = new CodeGeneratorOptions
        {
            PropertySpecifier = "VisibleAnywhere",
            GenerateBlueprintReadWrite = false,
            CategoryName = "Network"
        };
        var template = new StructTemplate(_typeMapper, options);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("UPROPERTY(VisibleAnywhere, Category = \"Network\")", result);
        Assert.DoesNotContain("BlueprintReadWrite", result);
    }

    // ========== CPP Generation — Primitive Types ==========

    [Fact]
    public void GenerateCpp_ContainsSelfInclude()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("#include \"MEPlayerProto.h\"", result);
    }

    [Fact]
    public void GenerateCpp_ContainsProtocHeaderInclude()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("#include \"player.pb.h\"", result);
    }

    [Fact]
    public void GenerateCpp_ContainsConstructorSignature()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("FPlayerProto::FPlayerProto(const ::Player& proto)", result);
    }

    [Fact]
    public void GenerateCpp_PrimitiveInt_DirectAssignment()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "id", Type = "int32", FieldNumber = 1 }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("Id = proto.id();", result);
    }

    [Fact]
    public void GenerateCpp_PrimitiveBool_DirectAssignment()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "is_active", Type = "bool", FieldNumber = 1 }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("IsActive = proto.is_active();", result);
    }

    [Fact]
    public void GenerateCpp_PrimitiveFloat_DirectAssignment()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "score", Type = "float", FieldNumber = 1 }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("Score = proto.score();", result);
    }

    // ========== CPP Generation — String ==========

    [Fact]
    public void GenerateCpp_StringField_UsesUtf8Conversion()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "user_name", Type = "string", FieldNumber = 1 }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("UserName = FString(UTF8_TO_TCHAR(proto.user_name().c_str()));", result);
    }

    // ========== CPP Generation — Bytes ==========

    [Fact]
    public void GenerateCpp_BytesField_UsesMemcpyPattern()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Data", new List<FieldNode>
        {
            new() { Name = "content", Type = "bytes", FieldNumber = 1 }
        });

        var result = template.GenerateCpp(message, "MEDataProto.h", "data.proto");

        Assert.Contains("const std::string& _bytes_content = proto.content();", result);
        Assert.Contains("Content.SetNum(_bytes_content.size());", result);
        Assert.Contains("FMemory::Memcpy(Content.GetData(), _bytes_content.data(), _bytes_content.size());", result);
    }

    // ========== CPP Generation — Enum ==========

    [Fact]
    public void GenerateCpp_EnumField_UsesStaticCast()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "status", Type = "Status", FieldNumber = 1, IsEnum = true }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("Status = static_cast<EStatusProto>(proto.status());", result);
    }

    // ========== CPP Generation — Message (nested struct) ==========

    [Fact]
    public void GenerateCpp_MessageField_CallsMarshalingConstructor()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("GameSession", new List<FieldNode>
        {
            new() { Name = "host", Type = "Player", FieldNumber = 1, IsEnum = false }
        });

        var result = template.GenerateCpp(message, "MEGameSessionProto.h", "game.proto");

        Assert.Contains("Host = FPlayerProto(proto.host());", result);
    }

    // ========== CPP Generation — Repeated Fields ==========

    [Fact]
    public void GenerateCpp_RepeatedPrimitive_UsesReserveAndLoop()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Inventory", new List<FieldNode>
        {
            new() { Name = "item_ids", Type = "int32", FieldNumber = 1, IsRepeated = true }
        });

        var result = template.GenerateCpp(message, "MEInventoryProto.h", "inventory.proto");

        Assert.Contains("ItemIds.Reserve(proto.item_ids_size());", result);
        Assert.Contains("for (int i = 0; i < proto.item_ids_size(); ++i)", result);
        Assert.Contains("ItemIds.Add(proto.item_ids(i));", result);
    }

    [Fact]
    public void GenerateCpp_RepeatedString_UsesUtf8InLoop()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Config", new List<FieldNode>
        {
            new() { Name = "tags", Type = "string", FieldNumber = 1, IsRepeated = true }
        });

        var result = template.GenerateCpp(message, "MEConfigProto.h", "config.proto");

        Assert.Contains("Tags.Reserve(proto.tags_size());", result);
        Assert.Contains("Tags.Add(FString(UTF8_TO_TCHAR(proto.tags(i).c_str())));", result);
    }

    [Fact]
    public void GenerateCpp_RepeatedMessage_CallsConstructorInLoop()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Guild", new List<FieldNode>
        {
            new() { Name = "members", Type = "Player", FieldNumber = 1, IsRepeated = true }
        });

        var result = template.GenerateCpp(message, "MEGuildProto.h", "guild.proto");

        Assert.Contains("Members.Reserve(proto.members_size());", result);
        Assert.Contains("Members.Add(FPlayerProto(proto.members(i)));", result);
    }

    [Fact]
    public void GenerateCpp_RepeatedEnum_UsesStaticCastInLoop()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "roles", Type = "Role", FieldNumber = 1, IsRepeated = true, IsEnum = true }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("Roles.Reserve(proto.roles_size());", result);
        Assert.Contains("Roles.Add(static_cast<ERoleProto>(proto.roles(i)));", result);
    }

    // ========== CPP Generation — Map Fields ==========

    [Fact]
    public void GenerateCpp_MapStringToInt_UsesStructuredBindings()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Config", new List<FieldNode>
        {
            new()
            {
                Name = "settings",
                Type = "map",
                FieldNumber = 1,
                IsMap = true,
                MapKeyType = "string",
                MapValueType = "int32"
            }
        });

        var result = template.GenerateCpp(message, "MEConfigProto.h", "config.proto");

        Assert.DoesNotContain("Settings.Reserve(", result);
        Assert.Contains("for (const auto& [key, value] : proto.settings())", result);
        Assert.Contains("Settings.Add(FString(UTF8_TO_TCHAR(key.c_str())), value);", result);
    }

    [Fact]
    public void GenerateCpp_MapIntToString_UsesDirectKeyAndStringValue()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Lookup", new List<FieldNode>
        {
            new()
            {
                Name = "names",
                Type = "map",
                FieldNumber = 1,
                IsMap = true,
                MapKeyType = "int32",
                MapValueType = "string"
            }
        });

        var result = template.GenerateCpp(message, "MELookupProto.h", "lookup.proto");

        Assert.Contains("Names.Add(key, FString(UTF8_TO_TCHAR(value.c_str())));", result);
    }

    [Fact]
    public void GenerateCpp_MapStringToMessage_UsesConstructorForValue()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Registry", new List<FieldNode>
        {
            new()
            {
                Name = "players",
                Type = "map",
                FieldNumber = 1,
                IsMap = true,
                MapKeyType = "string",
                MapValueType = "Player",
                MapValueIsEnum = false
            }
        });

        var result = template.GenerateCpp(message, "MERegistryProto.h", "registry.proto");

        Assert.Contains("Players.Add(FString(UTF8_TO_TCHAR(key.c_str())), FPlayerProto(value));", result);
    }

    [Fact]
    public void GenerateCpp_MapStringToEnum_UsesStaticCastForValue()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Config", new List<FieldNode>
        {
            new()
            {
                Name = "modes",
                Type = "map",
                FieldNumber = 1,
                IsMap = true,
                MapKeyType = "string",
                MapValueType = "GameMode",
                MapValueIsEnum = true
            }
        });

        var result = template.GenerateCpp(message, "MEConfigProto.h", "config.proto");

        Assert.Contains("Modes.Add(FString(UTF8_TO_TCHAR(key.c_str())), static_cast<EGameModeProto>(value));", result);
    }

    // ========== CPP Generation — Complex Mixed Message ==========

    [Fact]
    public void GenerateCpp_ComplexMessage_GeneratesAllFieldTypes()
    {
        var template = new StructTemplate(_typeMapper);
        var message = new MessageNode
        {
            Name = "Player",
            FullName = "Player",
            ProtocTypeName = "Player",
            Fields = new List<FieldNode>
            {
                new() { Name = "id", Type = "int32", FieldNumber = 1 },
                new() { Name = "username", Type = "string", FieldNumber = 2 },
                new() { Name = "is_active", Type = "bool", FieldNumber = 3 },
                new() { Name = "status", Type = "Status", FieldNumber = 4, IsEnum = true },
                new() { Name = "guild", Type = "Guild", FieldNumber = 5 },
                new() { Name = "scores", Type = "float", FieldNumber = 6, IsRepeated = true },
                new() { Name = "items", Type = "Item", FieldNumber = 7, IsRepeated = true },
                new()
                {
                    Name = "stats", Type = "map", FieldNumber = 8,
                    IsMap = true, MapKeyType = "string", MapValueType = "int32"
                }
            }
        };

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        // Primitive
        Assert.Contains("Id = proto.id();", result);
        // String
        Assert.Contains("Username = FString(UTF8_TO_TCHAR(proto.username().c_str()));", result);
        // Bool
        Assert.Contains("IsActive = proto.is_active();", result);
        // Enum
        Assert.Contains("Status = static_cast<EStatusProto>(proto.status());", result);
        // Message
        Assert.Contains("Guild = FGuildProto(proto.guild());", result);
        // Repeated primitive
        Assert.Contains("Scores.Reserve(proto.scores_size());", result);
        Assert.Contains("Scores.Add(proto.scores(i));", result);
        // Repeated message
        Assert.Contains("Items.Reserve(proto.items_size());", result);
        Assert.Contains("Items.Add(FItemProto(proto.items(i)));", result);
        // Map — no Reserve for TMap
        Assert.DoesNotContain("Stats.Reserve(", result);
        Assert.Contains("Stats.Add(FString(UTF8_TO_TCHAR(key.c_str())), value);", result);
    }

    // ========== Null Argument Checks ==========

    [Fact]
    public void GenerateHeader_NullMessage_ThrowsArgumentNullException()
    {
        var template = new StructTemplate(_typeMapper);

        Assert.Throws<ArgumentNullException>(() =>
            template.GenerateHeader(null!, new List<string>()));
    }

    [Fact]
    public void GenerateCpp_NullMessage_ThrowsArgumentNullException()
    {
        var template = new StructTemplate(_typeMapper);

        Assert.Throws<ArgumentNullException>(() =>
            template.GenerateCpp(null!, "test.h", "test.proto"));
    }

    [Fact]
    public void Constructor_NullTypeMapper_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StructTemplate(null!));
    }

    // ========== Package Namespace Support ==========

    [Fact]
    public void GenerateHeader_WithPackage_UsesPackageNamespace()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>(), "game.data");

        Assert.Contains("explicit FPlayerProto(const game::data::Player& proto);", result);
    }

    [Fact]
    public void GenerateHeader_WithoutPackage_UsesGlobalNamespace()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateHeader(message, new List<string>(), "");

        Assert.Contains("explicit FPlayerProto(const ::Player& proto);", result);
    }

    [Fact]
    public void GenerateCpp_WithPackage_UsesPackageNamespace()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage();

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto", "game.data");

        Assert.Contains("FPlayerProto::FPlayerProto(const game::data::Player& proto)", result);
    }

    // ========== Nested Message ProtocTypeName ==========

    [Fact]
    public void GenerateHeader_NestedMessage_UsesUnderscoreProtocTypeName()
    {
        var template = new StructTemplate(_typeMapper);
        var message = new MessageNode
        {
            Name = "Stats",
            FullName = "PlayerStats",
            ProtocTypeName = "Player_Stats",
            Fields = new List<FieldNode>
            {
                new() { Name = "health", Type = "float", FieldNumber = 1 }
            }
        };

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("explicit FPlayerStatsProto(const ::Player_Stats& proto);", result);
    }

    [Fact]
    public void GenerateCpp_NestedMessage_UsesUnderscoreProtocTypeName()
    {
        var template = new StructTemplate(_typeMapper);
        var message = new MessageNode
        {
            Name = "Stats",
            FullName = "PlayerStats",
            ProtocTypeName = "Player_Stats",
            Fields = new List<FieldNode>
            {
                new() { Name = "health", Type = "float", FieldNumber = 1 }
            }
        };

        var result = template.GenerateCpp(message, "MEPlayerStatsProto.h", "game.proto");

        Assert.Contains("FPlayerStatsProto::FPlayerStatsProto(const ::Player_Stats& proto)", result);
    }

    [Fact]
    public void GenerateCpp_NestedMessageWithPackage_UsesBothNamespaceAndUnderscore()
    {
        var template = new StructTemplate(_typeMapper);
        var message = new MessageNode
        {
            Name = "Stats",
            FullName = "PlayerStats",
            ProtocTypeName = "Player_Stats",
            Fields = new List<FieldNode>
            {
                new() { Name = "health", Type = "float", FieldNumber = 1 }
            }
        };

        var result = template.GenerateCpp(message, "MEPlayerStatsProto.h", "game.proto", "game");

        Assert.Contains("FPlayerStatsProto::FPlayerStatsProto(const game::Player_Stats& proto)", result);
    }

    // ========== Optional Field — Header (TOptional) ==========

    [Fact]
    public void GenerateHeader_OptionalPrimitive_UsesTOptional()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "party_id", Type = "int32", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("TOptional<int32> PartyId;", result);
    }

    [Fact]
    public void GenerateHeader_OptionalString_UsesTOptional()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "guild_name", Type = "string", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("TOptional<FString> GuildName;", result);
    }

    [Fact]
    public void GenerateHeader_OneOfMessage_UsesTOptional()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("CombatLog", new List<FieldNode>
        {
            new() { Name = "damage", Type = "DamageInfo", FieldNumber = 1, IsOneOf = true, OneOfGroupName = "event" }
        });

        var result = template.GenerateHeader(message, new List<string>());

        Assert.Contains("TOptional<FDamageInfoProto> Damage;", result);
    }

    // ========== Optional Field — CPP Marshaling (has_xxx) ==========

    [Fact]
    public void GenerateCpp_OptionalPrimitive_UsesHasCheck()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "party_id", Type = "int32", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateCpp(message, "MECharacterProto.h", "character.proto");

        Assert.Contains("if (proto.has_party_id())", result);
        Assert.Contains("PartyId = proto.party_id();", result);
    }

    [Fact]
    public void GenerateCpp_OptionalString_UsesHasCheckAndUtf8()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "guild_name", Type = "string", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateCpp(message, "MECharacterProto.h", "character.proto");

        Assert.Contains("if (proto.has_guild_name())", result);
        Assert.Contains("GuildName = FString(UTF8_TO_TCHAR(proto.guild_name().c_str()));", result);
    }

    [Fact]
    public void GenerateCpp_OptionalBytes_UsesHasCheckAndMemcpy()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Data", new List<FieldNode>
        {
            new() { Name = "payload", Type = "bytes", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateCpp(message, "MEDataProto.h", "data.proto");

        Assert.Contains("if (proto.has_payload())", result);
        Assert.Contains("const std::string& _bytes_payload = proto.payload();", result);
        Assert.Contains("TArray<uint8> _temp_payload;", result);
        Assert.Contains("_temp_payload.SetNum(_bytes_payload.size());", result);
        Assert.Contains("FMemory::Memcpy(_temp_payload.GetData(), _bytes_payload.data(), _bytes_payload.size());", result);
        Assert.Contains("Payload = MoveTemp(_temp_payload);", result);
    }

    [Fact]
    public void GenerateCpp_OptionalEnum_UsesHasCheckAndStaticCast()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "rank", Type = "Rank", FieldNumber = 1, IsOptional = true, IsEnum = true }
        });

        var result = template.GenerateCpp(message, "MECharacterProto.h", "character.proto");

        Assert.Contains("if (proto.has_rank())", result);
        Assert.Contains("Rank = static_cast<ERankProto>(proto.rank());", result);
    }

    [Fact]
    public void GenerateCpp_OptionalMessage_UsesHasCheckAndConstructor()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Character", new List<FieldNode>
        {
            new() { Name = "stats", Type = "Stats", FieldNumber = 1, IsOptional = true, IsEnum = false }
        });

        var result = template.GenerateCpp(message, "MECharacterProto.h", "character.proto");

        Assert.Contains("if (proto.has_stats())", result);
        Assert.Contains("Stats = FStatsProto(proto.stats());", result);
    }

    [Fact]
    public void GenerateCpp_OptionalBool_UsesHasCheck()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Player", new List<FieldNode>
        {
            new() { Name = "premium", Type = "bool", FieldNumber = 1, IsOptional = true }
        });

        var result = template.GenerateCpp(message, "MEPlayerProto.h", "player.proto");

        Assert.Contains("if (proto.has_premium())", result);
        Assert.Contains("Premium = proto.premium();", result);
    }

    // ========== OneOf Field — CPP Marshaling (has_xxx) ==========

    [Fact]
    public void GenerateCpp_OneOfMessage_UsesHasCheckAndConstructor()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("CombatLog", new List<FieldNode>
        {
            new() { Name = "damage", Type = "DamageInfo", FieldNumber = 1, IsOneOf = true, OneOfGroupName = "event" }
        });

        var result = template.GenerateCpp(message, "MECombatLogProto.h", "combat.proto");

        Assert.Contains("if (proto.has_damage())", result);
        Assert.Contains("Damage = FDamageInfoProto(proto.damage());", result);
    }

    [Fact]
    public void GenerateCpp_OneOfString_UsesHasCheckAndUtf8()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Event", new List<FieldNode>
        {
            new() { Name = "text_data", Type = "string", FieldNumber = 1, IsOneOf = true, OneOfGroupName = "payload" }
        });

        var result = template.GenerateCpp(message, "MEEventProto.h", "event.proto");

        Assert.Contains("if (proto.has_text_data())", result);
        Assert.Contains("TextData = FString(UTF8_TO_TCHAR(proto.text_data().c_str()));", result);
    }

    [Fact]
    public void GenerateCpp_OneOfPrimitive_UsesHasCheck()
    {
        var template = new StructTemplate(_typeMapper);
        var message = CreateSimpleMessage("Event", new List<FieldNode>
        {
            new() { Name = "int_data", Type = "int32", FieldNumber = 2, IsOneOf = true, OneOfGroupName = "payload" }
        });

        var result = template.GenerateCpp(message, "MEEventProto.h", "event.proto");

        Assert.Contains("if (proto.has_int_data())", result);
        Assert.Contains("IntData = proto.int_data();", result);
    }

    // ========== Mixed regular + optional + oneof fields ==========

    [Fact]
    public void GenerateCpp_MixedFields_RegularUsesDirectOptionalUsesHasCheck()
    {
        var template = new StructTemplate(_typeMapper);
        var message = new MessageNode
        {
            Name = "Character",
            FullName = "Character",
            ProtocTypeName = "Character",
            Fields = new List<FieldNode>
            {
                new() { Name = "name", Type = "string", FieldNumber = 1 },
                new() { Name = "guild_name", Type = "string", FieldNumber = 2, IsOptional = true },
                new() { Name = "damage", Type = "DamageInfo", FieldNumber = 3, IsOneOf = true, OneOfGroupName = "event" }
            }
        };

        var result = template.GenerateCpp(message, "MECharacterProto.h", "character.proto");

        // Regular field — no has_xxx
        Assert.Contains("Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));", result);
        Assert.DoesNotContain("has_name()", result);

        // Optional field — has_xxx
        Assert.Contains("if (proto.has_guild_name())", result);
        Assert.Contains("GuildName = FString(UTF8_TO_TCHAR(proto.guild_name().c_str()));", result);

        // OneOf field — has_xxx
        Assert.Contains("if (proto.has_damage())", result);
        Assert.Contains("Damage = FDamageInfoProto(proto.damage());", result);
    }
}
