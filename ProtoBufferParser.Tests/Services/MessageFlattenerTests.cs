using ProtoBufferParser.Models;
using ProtoBufferParser.Services;

namespace ProtoBufferParser.Tests.Services;

/// <summary>
/// Tests for <see cref="MessageFlattener"/>.
/// Verifies nested message/enum flattening logic.
/// </summary>
public class MessageFlattenerTests
{
    private readonly MessageFlattener _flattener = new();

    [Fact]
    public void Flatten_TopLevelMessageOnly_SetsFullNameToName()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new() { Name = "Player", FullName = "Player" }
            }
        };

        _flattener.Flatten(file);

        Assert.Single(file.Messages);
        Assert.Equal("Player", file.Messages[0].FullName);
        Assert.Equal("Player", file.Messages[0].ProtocTypeName);
    }

    [Fact]
    public void Flatten_SingleNestedMessage_FlattensToConcatenatedName()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "Outer",
                    NestedMessages = new List<MessageNode>
                    {
                        new() { Name = "Inner" }
                    }
                }
            }
        };

        _flattener.Flatten(file);

        Assert.Equal(2, file.Messages.Count);
        Assert.Equal("Outer", file.Messages[0].FullName);
        Assert.Equal("Outer", file.Messages[0].ProtocTypeName);
        Assert.Equal("OuterInner", file.Messages[1].FullName);
        Assert.Equal("Outer_Inner", file.Messages[1].ProtocTypeName);
    }

    [Fact]
    public void Flatten_DeeplyNestedMessage_FlattensFully()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "A",
                    NestedMessages = new List<MessageNode>
                    {
                        new()
                        {
                            Name = "B",
                            NestedMessages = new List<MessageNode>
                            {
                                new() { Name = "C" }
                            }
                        }
                    }
                }
            }
        };

        _flattener.Flatten(file);

        Assert.Equal(3, file.Messages.Count);
        Assert.Equal("A", file.Messages[0].FullName);
        Assert.Equal("A", file.Messages[0].ProtocTypeName);
        Assert.Equal("AB", file.Messages[1].FullName);
        Assert.Equal("A_B", file.Messages[1].ProtocTypeName);
        Assert.Equal("ABC", file.Messages[2].FullName);
        Assert.Equal("A_B_C", file.Messages[2].ProtocTypeName);
    }

    [Fact]
    public void Flatten_NestedEnum_MovesToTopLevelWithCorrectFullName()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "Player",
                    NestedEnums = new List<EnumNode>
                    {
                        new()
                        {
                            Name = "Rank",
                            Values = new List<EnumValueNode>
                            {
                                new() { Name = "RANK_UNKNOWN", Value = 0 }
                            }
                        }
                    }
                }
            }
        };

        _flattener.Flatten(file);

        // Enum should be moved to top-level
        Assert.Single(file.Enums);
        Assert.Equal("PlayerRank", file.Enums[0].FullName);

        // Nested enums should be cleared from message
        Assert.Empty(file.Messages[0].NestedEnums);
    }

    [Fact]
    public void Flatten_TopLevelEnum_SetsFullNameToName()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Enums = new List<EnumNode>
            {
                new() { Name = "Status" }
            }
        };

        _flattener.Flatten(file);

        Assert.Single(file.Enums);
        Assert.Equal("Status", file.Enums[0].FullName);
    }

    [Fact]
    public void Flatten_MultipleNestedMessages_ClearsNestedCollections()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "Parent",
                    NestedMessages = new List<MessageNode>
                    {
                        new() { Name = "ChildA" },
                        new() { Name = "ChildB" }
                    }
                }
            }
        };

        _flattener.Flatten(file);

        // Parent + ChildA + ChildB = 3
        Assert.Equal(3, file.Messages.Count);
        Assert.Equal("Parent", file.Messages[0].FullName);
        Assert.Equal("Parent", file.Messages[0].ProtocTypeName);
        Assert.Equal("ParentChildA", file.Messages[1].FullName);
        Assert.Equal("Parent_ChildA", file.Messages[1].ProtocTypeName);
        Assert.Equal("ParentChildB", file.Messages[2].FullName);
        Assert.Equal("Parent_ChildB", file.Messages[2].ProtocTypeName);

        // Parent should have its nested messages cleared
        Assert.Empty(file.Messages[0].NestedMessages);
    }

    [Fact]
    public void Flatten_MixedTopLevelAndNestedEnums_CombinesAll()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "Player",
                    NestedEnums = new List<EnumNode>
                    {
                        new() { Name = "Rank" }
                    }
                }
            },
            Enums = new List<EnumNode>
            {
                new() { Name = "GameState" }
            }
        };

        _flattener.Flatten(file);

        Assert.Equal(2, file.Enums.Count);
        Assert.Contains(file.Enums, e => e.FullName == "GameState");
        Assert.Contains(file.Enums, e => e.FullName == "PlayerRank");
    }

    [Fact]
    public void Flatten_EmptyFile_NoErrors()
    {
        var file = new ProtoFileNode { FileName = "empty.proto" };

        _flattener.Flatten(file);

        Assert.Empty(file.Messages);
        Assert.Empty(file.Enums);
    }

    [Fact]
    public void Flatten_PreservesFieldsOnFlattenedMessages()
    {
        var file = new ProtoFileNode
        {
            FileName = "test.proto",
            Messages = new List<MessageNode>
            {
                new()
                {
                    Name = "Outer",
                    Fields = new List<FieldNode>
                    {
                        new() { Name = "id", Type = "int32", FieldNumber = 1 }
                    },
                    NestedMessages = new List<MessageNode>
                    {
                        new()
                        {
                            Name = "Inner",
                            Fields = new List<FieldNode>
                            {
                                new() { Name = "value", Type = "string", FieldNumber = 1 }
                            }
                        }
                    }
                }
            }
        };

        _flattener.Flatten(file);

        Assert.Single(file.Messages[0].Fields);
        Assert.Equal("id", file.Messages[0].Fields[0].Name);

        Assert.Single(file.Messages[1].Fields);
        Assert.Equal("value", file.Messages[1].Fields[0].Name);
    }
}
