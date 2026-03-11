using ProtoBufferParser.Models;
using ProtoBufferParser.Services;
using ProtoBufferParser.Tests.Helpers;

namespace ProtoBufferParser.Tests.Integration;

/// <summary>
/// End-to-end integration tests that exercise the full compilation pipeline
/// from .proto input to generated .h/.cpp output.
/// </summary>
public sealed class EndToEndTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _inputDir;
    private readonly string _outputDir;
    private readonly MockLogger _logger;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProtoTest_{Guid.NewGuid():N}");
        _inputDir = Path.Combine(_tempDir, "input");
        _outputDir = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(_inputDir);
        _logger = new MockLogger();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private CompilerOptions CreateOptions(bool verbose = false) => new()
    {
        InputDirectory = _inputDir,
        OutputDirectory = _outputDir,
        Verbose = verbose
    };

    private void WriteProto(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_inputDir, fileName), content);
    }

    // ----------------------------------------------------------------
    // Basic: Simple message
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_SimpleMessage_GeneratesHeaderAndCpp()
    {
        WriteProto("player.proto", """
            syntax = "proto3";
            message Player {
                int32 id = 1;
                string name = 2;
                float score = 3;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        Assert.Equal(2, count); // 1 header + 1 cpp
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEPlayerProto.h")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEPlayerProto.cpp")));
    }

    [Fact]
    public void Compile_SimpleMessage_HeaderContainsCorrectStructure()
    {
        WriteProto("player.proto", """
            syntax = "proto3";
            message Player {
                int32 id = 1;
                string name = 2;
                bool is_active = 3;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.h"));
        Assert.Contains("#pragma once", header);
        Assert.Contains("#include \"CoreMinimal.h\"", header);
        Assert.Contains("#include \"MEPlayerProto.generated.h\"", header);
        Assert.Contains("USTRUCT(BlueprintType)", header);
        Assert.Contains("struct FPlayerProto", header);
        Assert.Contains("GENERATED_BODY()", header);
        Assert.Contains("FPlayerProto() = default;", header);
        Assert.Contains("explicit FPlayerProto(const ::Player& proto);", header);
        Assert.Contains("int32 Id;", header);
        Assert.Contains("FString Name;", header);
        Assert.Contains("bool IsActive;", header);
        Assert.Contains("UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = \"Proto\")", header);
    }

    [Fact]
    public void Compile_SimpleMessage_CppContainsMarshalingConstructor()
    {
        WriteProto("player.proto", """
            syntax = "proto3";
            message Player {
                int32 id = 1;
                string name = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.cpp"));
        Assert.Contains("#include \"MEPlayerProto.h\"", cpp);
        Assert.Contains("#include \"player.pb.h\"", cpp);
        Assert.Contains("FPlayerProto::FPlayerProto(const ::Player& proto)", cpp);
        Assert.Contains("Id = proto.id();", cpp);
        Assert.Contains("Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));", cpp);
    }

    // ----------------------------------------------------------------
    // Enum
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_EnumOnly_GeneratesHeaderOnly()
    {
        WriteProto("status.proto", """
            syntax = "proto3";
            enum Status {
                UNKNOWN = 0;
                ACTIVE = 1;
                INACTIVE = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        Assert.Equal(1, count); // enum = header only
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEStatusProto.h")));
        Assert.False(File.Exists(Path.Combine(_outputDir, "MEStatusProto.cpp")));

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEStatusProto.h"));
        Assert.Contains("UENUM(BlueprintType)", header);
        Assert.Contains("enum class EStatusProto : uint8", header);
        Assert.Contains("Unknown = 0", header);
        Assert.Contains("Active = 1", header);
        Assert.Contains("Inactive = 2", header);
    }

    // ----------------------------------------------------------------
    // Enum field reference
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_MessageWithEnumField_UsesStaticCast()
    {
        WriteProto("item.proto", """
            syntax = "proto3";
            enum ItemType {
                ITEM_UNKNOWN = 0;
                ITEM_WEAPON = 1;
            }
            message Item {
                int32 id = 1;
                ItemType type = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEItemProto.cpp"));
        Assert.Contains("Type = static_cast<EItemTypeProto>(proto.type());", cpp);

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEItemProto.h"));
        Assert.Contains("EItemTypeProto Type;", header);
        Assert.Contains("#include \"MEItemTypeProto.h\"", header);
    }

    // ----------------------------------------------------------------
    // Repeated fields
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_RepeatedFields_UsesTArrayWithReserveLoop()
    {
        WriteProto("data.proto", """
            syntax = "proto3";
            message Data {
                repeated int32 scores = 1;
                repeated string tags = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEDataProto.cpp"));

        // repeated int32
        Assert.Contains("Scores.Reserve(proto.scores_size());", cpp);
        Assert.Contains("Scores.Add(proto.scores(i));", cpp);

        // repeated string
        Assert.Contains("Tags.Reserve(proto.tags_size());", cpp);
        Assert.Contains("Tags.Add(FString(UTF8_TO_TCHAR(proto.tags(i).c_str())));", cpp);

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEDataProto.h"));
        Assert.Contains("TArray<int32> Scores;", header);
        Assert.Contains("TArray<FString> Tags;", header);
    }

    // ----------------------------------------------------------------
    // Map fields
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_MapFields_UsesTMapWithStructuredBindings()
    {
        WriteProto("config.proto", """
            syntax = "proto3";
            message Config {
                map<string, int32> settings = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEConfigProto.cpp"));
        Assert.DoesNotContain("Settings.Reserve(", cpp);
        Assert.Contains("for (const auto& [key, value] : proto.settings())", cpp);
        Assert.Contains("Settings.Add(FString(UTF8_TO_TCHAR(key.c_str())), value);", cpp);

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEConfigProto.h"));
        Assert.Contains("TMap<FString, int32> Settings;", header);
    }

    // ----------------------------------------------------------------
    // Nested messages (flatten)
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_NestedMessage_FlattensWithCorrectProtocTypeName()
    {
        WriteProto("parent.proto", """
            syntax = "proto3";
            message Parent {
                message Child {
                    int32 value = 1;
                }
                Child child = 1;
                string name = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        // Parent + Child = 2 types, each with .h + .cpp = 4 files
        Assert.Equal(4, count);
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEParentProto.h")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEParentProto.cpp")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEParentChildProto.h")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEParentChildProto.cpp")));

        // Child uses Parent_Child protoc type
        var childCpp = File.ReadAllText(Path.Combine(_outputDir, "MEParentChildProto.cpp"));
        Assert.Contains("FParentChildProto::FParentChildProto(const ::Parent_Child& proto)", childCpp);

        // Parent references FParentChildProto
        var parentHeader = File.ReadAllText(Path.Combine(_outputDir, "MEParentProto.h"));
        Assert.Contains("FParentChildProto Child;", parentHeader);
        Assert.Contains("#include \"MEParentChildProto.h\"", parentHeader);

        var parentCpp = File.ReadAllText(Path.Combine(_outputDir, "MEParentProto.cpp"));
        Assert.Contains("Child = FParentChildProto(proto.child());", parentCpp);
    }

    // ----------------------------------------------------------------
    // Cross-file imports
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_CrossFileImports_ResolvesTypesAcrossFiles()
    {
        WriteProto("common.proto", """
            syntax = "proto3";
            message Vector3 {
                float x = 1;
                float y = 2;
                float z = 3;
            }
            """);

        WriteProto("transform.proto", """
            syntax = "proto3";
            import "common.proto";
            message Transform {
                Vector3 position = 1;
                Vector3 scale = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        // Vector3 (h+cpp) + Transform (h+cpp) = 4 files
        Assert.Equal(4, count);

        var transformHeader = File.ReadAllText(Path.Combine(_outputDir, "METransformProto.h"));
        Assert.Contains("#include \"MEVector3Proto.h\"", transformHeader);
        Assert.Contains("FVector3Proto Position;", transformHeader);
        Assert.Contains("FVector3Proto Scale;", transformHeader);

        var transformCpp = File.ReadAllText(Path.Combine(_outputDir, "METransformProto.cpp"));
        Assert.Contains("Position = FVector3Proto(proto.position());", transformCpp);
        Assert.Contains("Scale = FVector3Proto(proto.scale());", transformCpp);
    }

    // ----------------------------------------------------------------
    // Package → protoc namespace
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_WithPackage_UsesPackageNamespaceInProtocReference()
    {
        WriteProto("game.proto", """
            syntax = "proto3";
            package game;
            message Player {
                int32 id = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.h"));
        Assert.Contains("explicit FPlayerProto(const game::Player& proto);", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.cpp"));
        Assert.Contains("FPlayerProto::FPlayerProto(const game::Player& proto)", cpp);
    }

    [Fact]
    public void Compile_WithoutPackage_UsesGlobalNamespace()
    {
        WriteProto("player.proto", """
            syntax = "proto3";
            message Player {
                int32 id = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.h"));
        Assert.Contains("explicit FPlayerProto(const ::Player& proto);", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.cpp"));
        Assert.Contains("FPlayerProto::FPlayerProto(const ::Player& proto)", cpp);
    }

    // ----------------------------------------------------------------
    // Package + nested message
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_PackageWithNestedMessage_UsesBothNamespaceAndUnderscore()
    {
        WriteProto("game.proto", """
            syntax = "proto3";
            package game;
            message Player {
                message Stats {
                    int32 strength = 1;
                }
                Stats stats = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var childCpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerStatsProto.cpp"));
        Assert.Contains("FPlayerStatsProto::FPlayerStatsProto(const game::Player_Stats& proto)", childCpp);

        var childHeader = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerStatsProto.h"));
        Assert.Contains("explicit FPlayerStatsProto(const game::Player_Stats& proto);", childHeader);
    }

    // ----------------------------------------------------------------
    // bytes field
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_BytesField_UsesMemcpyPattern()
    {
        WriteProto("blob.proto", """
            syntax = "proto3";
            message Blob {
                bytes data = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEBlobProto.h"));
        Assert.Contains("TArray<uint8> Data;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEBlobProto.cpp"));
        Assert.Contains("const std::string& _bytes_data = proto.data();", cpp);
        Assert.Contains("Data.SetNum(_bytes_data.size());", cpp);
        Assert.Contains("FMemory::Memcpy(Data.GetData(), _bytes_data.data(), _bytes_data.size());", cpp);
    }

    // ----------------------------------------------------------------
    // Full multi-file E2E (samples equivalent)
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_MultiFileWithDependencies_GeneratesAllFiles()
    {
        WriteProto("common.proto", """
            syntax = "proto3";
            message Vector3 {
                float x = 1;
                float y = 2;
                float z = 3;
            }
            message Quaternion {
                float x = 1;
                float y = 2;
                float z = 3;
                float w = 4;
            }
            """);

        WriteProto("transform.proto", """
            syntax = "proto3";
            import "common.proto";
            message Transform {
                Vector3 position = 1;
                Quaternion rotation = 2;
                Vector3 scale = 3;
            }
            """);

        WriteProto("game_data.proto", """
            syntax = "proto3";
            import "transform.proto";
            enum ItemType {
                ITEM_UNKNOWN = 0;
                ITEM_WEAPON = 1;
            }
            message Item {
                int32 id = 1;
                string name = 2;
                ItemType type = 3;
                map<string, int32> stats = 4;
            }
            message Player {
                int32 id = 1;
                string username = 2;
                repeated Item inventory = 3;
                Transform spawn_point = 4;
                message Stats {
                    int32 strength = 1;
                    float health = 2;
                }
                Stats stats = 5;
            }
            message Guild {
                int32 id = 1;
                string name = 2;
                repeated Player members = 3;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(verbose: true), _logger);
        var count = compiler.Compile();

        // common: Vector3(h+cpp) + Quaternion(h+cpp) = 4
        // transform: Transform(h+cpp) = 2
        // game_data: ItemType(h) + Item(h+cpp) + Player(h+cpp) + PlayerStats(h+cpp) + Guild(h+cpp) = 9
        Assert.Equal(15, count);

        // Spot-check key files
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEVector3Proto.h")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "METransformProto.cpp")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEItemTypeProto.h")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEPlayerProto.cpp")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEPlayerStatsProto.cpp")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MEGuildProto.h")));

        // Verify nested message protoc type
        var statsCpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerStatsProto.cpp"));
        Assert.Contains("const ::Player_Stats& proto", statsCpp);

        // Verify cross-file type resolution
        var transformCpp = File.ReadAllText(Path.Combine(_outputDir, "METransformProto.cpp"));
        Assert.Contains("Position = FVector3Proto(proto.position());", transformCpp);

        // Verify enum static_cast
        var itemCpp = File.ReadAllText(Path.Combine(_outputDir, "MEItemProto.cpp"));
        Assert.Contains("static_cast<EItemTypeProto>(proto.type())", itemCpp);
    }

    // ----------------------------------------------------------------
    // Error handling
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_NonExistentInputDirectory_ThrowsDirectoryNotFoundException()
    {
        var options = new CompilerOptions
        {
            InputDirectory = Path.Combine(_tempDir, "does_not_exist"),
            OutputDirectory = _outputDir,
            Verbose = false
        };

        var compiler = new ProtoCompiler(options, _logger);
        Assert.Throws<DirectoryNotFoundException>(() => compiler.Compile());
    }

    [Fact]
    public void Compile_EmptyDirectory_ReturnsZero()
    {
        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        Assert.Equal(0, count);
        Assert.Contains(_logger.WarningMessages, m => m.Contains("No .proto files found"));
    }

    [Fact]
    public void Compile_OutputDirectoryCreatedAutomatically()
    {
        WriteProto("simple.proto", """
            syntax = "proto3";
            message Simple {
                int32 id = 1;
            }
            """);

        Assert.False(Directory.Exists(_outputDir));

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        Assert.True(Directory.Exists(_outputDir));
        Assert.True(File.Exists(Path.Combine(_outputDir, "MESimpleProto.h")));
    }

    // ----------------------------------------------------------------
    // Logging verification
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_VerboseMode_LogsDetailedOutput()
    {
        WriteProto("test.proto", """
            syntax = "proto3";
            message Test {
                int32 value = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(verbose: true), _logger);
        compiler.Compile();

        Assert.Contains(_logger.InfoMessages, m => m.Contains("ProtoBufferParser starting"));
        Assert.Contains(_logger.InfoMessages, m => m.Contains("Scanning for .proto files"));
        Assert.Contains(_logger.InfoMessages, m => m.Contains("Compiling test.proto"));
        Assert.Contains(_logger.InfoMessages, m => m.Contains("Done!"));
        Assert.True(_logger.VerboseMessages.Count > 0);
    }

    // ----------------------------------------------------------------
    // Auto-generated file header
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_GeneratedFiles_ContainAutoGeneratedHeader()
    {
        WriteProto("test.proto", """
            syntax = "proto3";
            message Test {
                int32 value = 1;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "METestProto.h"));
        Assert.Contains("AUTO-GENERATED from test", header);
        Assert.Contains("DO NOT MODIFY", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "METestProto.cpp"));
        Assert.Contains("AUTO-GENERATED from test", cpp);
        Assert.Contains("DO NOT MODIFY", cpp);
    }

    // ----------------------------------------------------------------
    // Optional fields → TOptional
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_OptionalFields_GeneratesTOptionalInHeader()
    {
        WriteProto("profile.proto", """
            syntax = "proto3";
            message Profile {
                int32 id = 1;
                string name = 2;
                optional string nickname = 3;
                optional int32 age = 4;
                optional bool is_premium = 5;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEProfileProto.h"));
        Assert.Contains("int32 Id;", header);
        Assert.Contains("FString Name;", header);
        Assert.Contains("TOptional<FString> Nickname;", header);
        Assert.Contains("TOptional<int32> Age;", header);
        Assert.Contains("TOptional<bool> IsPremium;", header);
    }

    [Fact]
    public void Compile_OptionalFields_GeneratesHasGuardInCpp()
    {
        WriteProto("profile.proto", """
            syntax = "proto3";
            message Profile {
                int32 id = 1;
                optional string nickname = 2;
                optional int32 age = 3;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEProfileProto.cpp"));

        // Regular field — no has_xxx guard
        Assert.Contains("Id = proto.id();", cpp);

        // Optional string — has_xxx + FString conversion
        Assert.Contains("if (proto.has_nickname())", cpp);
        Assert.Contains("Nickname = FString(UTF8_TO_TCHAR(proto.nickname().c_str()));", cpp);

        // Optional int32 — has_xxx + direct assignment
        Assert.Contains("if (proto.has_age())", cpp);
        Assert.Contains("Age = proto.age();", cpp);
    }

    [Fact]
    public void Compile_OptionalEnumField_GeneratesTOptionalWithStaticCast()
    {
        WriteProto("player.proto", """
            syntax = "proto3";
            enum Rank {
                RANK_UNKNOWN = 0;
                RANK_BRONZE = 1;
                RANK_SILVER = 2;
            }
            message Player {
                int32 id = 1;
                optional Rank rank = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.h"));
        Assert.Contains("TOptional<ERankProto> Rank;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEPlayerProto.cpp"));
        Assert.Contains("if (proto.has_rank())", cpp);
        Assert.Contains("Rank = static_cast<ERankProto>(proto.rank());", cpp);
    }

    [Fact]
    public void Compile_OptionalMessageField_GeneratesTOptionalWithConstructor()
    {
        WriteProto("game.proto", """
            syntax = "proto3";
            message Vector3 {
                float x = 1;
                float y = 2;
                float z = 3;
            }
            message Entity {
                int32 id = 1;
                optional Vector3 target_position = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEEntityProto.h"));
        Assert.Contains("TOptional<FVector3Proto> TargetPosition;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEEntityProto.cpp"));
        Assert.Contains("if (proto.has_target_position())", cpp);
        Assert.Contains("TargetPosition = FVector3Proto(proto.target_position());", cpp);
    }

    [Fact]
    public void Compile_OptionalBytesField_GeneratesTOptionalWithMoveTemp()
    {
        WriteProto("attachment.proto", """
            syntax = "proto3";
            message Attachment {
                int32 id = 1;
                optional bytes thumbnail = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEAttachmentProto.h"));
        Assert.Contains("TOptional<TArray<uint8>> Thumbnail;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEAttachmentProto.cpp"));
        Assert.Contains("if (proto.has_thumbnail())", cpp);
        Assert.Contains("const std::string& _bytes_thumbnail = proto.thumbnail();", cpp);
        Assert.Contains("_temp_thumbnail.SetNum(_bytes_thumbnail.size());", cpp);
        Assert.Contains("FMemory::Memcpy(_temp_thumbnail.GetData(), _bytes_thumbnail.data(), _bytes_thumbnail.size());", cpp);
        Assert.Contains("Thumbnail = MoveTemp(_temp_thumbnail);", cpp);
    }

    // ----------------------------------------------------------------
    // Oneof fields → TOptional
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_OneofFields_GeneratesTOptionalInHeader()
    {
        WriteProto("event.proto", """
            syntax = "proto3";
            message ClickEvent {
                int32 x = 1;
                int32 y = 2;
            }
            message KeyEvent {
                string key_code = 1;
            }
            message InputEvent {
                int32 timestamp = 1;
                oneof event {
                    ClickEvent click = 2;
                    KeyEvent key_press = 3;
                    string raw_input = 4;
                    int32 error_code = 5;
                }
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEInputEventProto.h"));
        Assert.Contains("int32 Timestamp;", header);
        Assert.Contains("TOptional<FClickEventProto> Click;", header);
        Assert.Contains("TOptional<FKeyEventProto> KeyPress;", header);
        Assert.Contains("TOptional<FString> RawInput;", header);
        Assert.Contains("TOptional<int32> ErrorCode;", header);
    }

    [Fact]
    public void Compile_OneofFields_GeneratesHasGuardInCpp()
    {
        WriteProto("event.proto", """
            syntax = "proto3";
            message ClickEvent {
                int32 x = 1;
                int32 y = 2;
            }
            message InputEvent {
                int32 timestamp = 1;
                oneof event {
                    ClickEvent click = 2;
                    string raw_input = 3;
                    int32 error_code = 4;
                }
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEInputEventProto.cpp"));

        // Regular field
        Assert.Contains("Timestamp = proto.timestamp();", cpp);

        // Oneof message — has_xxx + constructor
        Assert.Contains("if (proto.has_click())", cpp);
        Assert.Contains("Click = FClickEventProto(proto.click());", cpp);

        // Oneof string — has_xxx + FString
        Assert.Contains("if (proto.has_raw_input())", cpp);
        Assert.Contains("RawInput = FString(UTF8_TO_TCHAR(proto.raw_input().c_str()));", cpp);

        // Oneof int32 — has_xxx + direct
        Assert.Contains("if (proto.has_error_code())", cpp);
        Assert.Contains("ErrorCode = proto.error_code();", cpp);
    }

    // ----------------------------------------------------------------
    // Mixed: regular + optional + oneof in one message
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_MixedRegularOptionalOneof_GeneratesCorrectOutput()
    {
        WriteProto("notification.proto", """
            syntax = "proto3";
            message EmailData {
                string subject = 1;
                string body = 2;
            }
            message Notification {
                int32 id = 1;
                string title = 2;
                optional string subtitle = 3;
                optional int32 priority = 4;
                oneof content {
                    string text_message = 5;
                    EmailData email = 6;
                }
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MENotificationProto.h"));
        // Regular fields
        Assert.Contains("int32 Id;", header);
        Assert.Contains("FString Title;", header);
        // Optional fields
        Assert.Contains("TOptional<FString> Subtitle;", header);
        Assert.Contains("TOptional<int32> Priority;", header);
        // Oneof fields
        Assert.Contains("TOptional<FString> TextMessage;", header);
        Assert.Contains("TOptional<FEmailDataProto> Email;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MENotificationProto.cpp"));
        // Regular marshaling
        Assert.Contains("Id = proto.id();", cpp);
        Assert.Contains("Title = FString(UTF8_TO_TCHAR(proto.title().c_str()));", cpp);
        // Optional marshaling with has_xxx
        Assert.Contains("if (proto.has_subtitle())", cpp);
        Assert.Contains("if (proto.has_priority())", cpp);
        // Oneof marshaling with has_xxx
        Assert.Contains("if (proto.has_text_message())", cpp);
        Assert.Contains("if (proto.has_email())", cpp);
        Assert.Contains("Email = FEmailDataProto(proto.email());", cpp);
    }

    // ----------------------------------------------------------------
    // Optional + package namespace
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_OptionalWithPackage_UsesCorrectNamespace()
    {
        WriteProto("account.proto", """
            syntax = "proto3";
            package myapp.users;
            message Account {
                int32 id = 1;
                optional string display_name = 2;
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        compiler.Compile();

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEAccountProto.h"));
        Assert.Contains("explicit FAccountProto(const myapp::users::Account& proto);", header);
        Assert.Contains("TOptional<FString> DisplayName;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEAccountProto.cpp"));
        Assert.Contains("FAccountProto::FAccountProto(const myapp::users::Account& proto)", cpp);
        Assert.Contains("if (proto.has_display_name())", cpp);
    }

    // ----------------------------------------------------------------
    // Cross-file optional/oneof with imports
    // ----------------------------------------------------------------

    [Fact]
    public void Compile_CrossFileOptionalAndOneof_ResolvesTypesCorrectly()
    {
        WriteProto("common.proto", """
            syntax = "proto3";
            message Timestamp {
                int64 seconds = 1;
                int32 nanos = 2;
            }
            message Location {
                float lat = 1;
                float lng = 2;
            }
            """);

        WriteProto("activity.proto", """
            syntax = "proto3";
            import "common.proto";
            message Activity {
                int32 id = 1;
                optional Timestamp scheduled_at = 2;
                oneof details {
                    string description = 3;
                    Location meeting_point = 4;
                }
            }
            """);

        var compiler = new ProtoCompiler(CreateOptions(), _logger);
        var count = compiler.Compile();

        // Timestamp(h+cpp) + Location(h+cpp) + Activity(h+cpp) = 6
        Assert.Equal(6, count);

        var header = File.ReadAllText(Path.Combine(_outputDir, "MEActivityProto.h"));
        Assert.Contains("#include \"METimestampProto.h\"", header);
        Assert.Contains("#include \"MELocationProto.h\"", header);
        Assert.Contains("TOptional<FTimestampProto> ScheduledAt;", header);
        Assert.Contains("TOptional<FString> Description;", header);
        Assert.Contains("TOptional<FLocationProto> MeetingPoint;", header);

        var cpp = File.ReadAllText(Path.Combine(_outputDir, "MEActivityProto.cpp"));
        Assert.Contains("if (proto.has_scheduled_at())", cpp);
        Assert.Contains("ScheduledAt = FTimestampProto(proto.scheduled_at());", cpp);
        Assert.Contains("if (proto.has_description())", cpp);
        Assert.Contains("if (proto.has_meeting_point())", cpp);
        Assert.Contains("MeetingPoint = FLocationProto(proto.meeting_point());", cpp);
    }
}
