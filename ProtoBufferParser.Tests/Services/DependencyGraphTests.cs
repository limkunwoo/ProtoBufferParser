using ProtoBufferParser.Exceptions;
using ProtoBufferParser.Models;
using ProtoBufferParser.Services;
using ProtoBufferParser.Tests.Helpers;

namespace ProtoBufferParser.Tests.Services;

/// <summary>
/// Tests for <see cref="DependencyGraph"/>.
/// Verifies topological sort and circular dependency detection.
/// </summary>
public class DependencyGraphTests
{
    private readonly MockLogger _logger = new();

    private static ProtoFileInfo MakeFile(string name) => new()
    {
        FileName = name,
        FilePath = $"/fake/{name}.proto",
        Content = ""
    };

    [Fact]
    public void TopologicalSort_NoDependencies_ReturnsAllFiles()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("a"));
        graph.AddFile(MakeFile("b"));
        graph.AddFile(MakeFile("c"));

        var result = graph.TopologicalSort();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.FileName == "a");
        Assert.Contains(result, f => f.FileName == "b");
        Assert.Contains(result, f => f.FileName == "c");
    }

    [Fact]
    public void TopologicalSort_LinearDependencies_ReturnsCorrectOrder()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("a"));
        graph.AddFile(MakeFile("b"));
        graph.AddFile(MakeFile("c"));

        // c depends on b, b depends on a => order: a -> b -> c
        graph.AddDependency("c", "b");
        graph.AddDependency("b", "a");

        var result = graph.TopologicalSort();

        Assert.Equal(3, result.Count);

        var indexA = result.ToList().FindIndex(f => f.FileName == "a");
        var indexB = result.ToList().FindIndex(f => f.FileName == "b");
        var indexC = result.ToList().FindIndex(f => f.FileName == "c");

        Assert.True(indexA < indexB, "a should come before b");
        Assert.True(indexB < indexC, "b should come before c");
    }

    [Fact]
    public void TopologicalSort_DiamondDependency_ReturnsValidOrder()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("common"));
        graph.AddFile(MakeFile("player"));
        graph.AddFile(MakeFile("item"));
        graph.AddFile(MakeFile("game"));

        // game depends on player and item; both depend on common
        graph.AddDependency("game", "player");
        graph.AddDependency("game", "item");
        graph.AddDependency("player", "common");
        graph.AddDependency("item", "common");

        var result = graph.TopologicalSort();

        Assert.Equal(4, result.Count);

        var indexCommon = result.ToList().FindIndex(f => f.FileName == "common");
        var indexPlayer = result.ToList().FindIndex(f => f.FileName == "player");
        var indexItem = result.ToList().FindIndex(f => f.FileName == "item");
        var indexGame = result.ToList().FindIndex(f => f.FileName == "game");

        Assert.True(indexCommon < indexPlayer, "common before player");
        Assert.True(indexCommon < indexItem, "common before item");
        Assert.True(indexPlayer < indexGame, "player before game");
        Assert.True(indexItem < indexGame, "item before game");
    }

    [Fact]
    public void TopologicalSort_CircularDependency_ThrowsCircularDependencyException()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("a"));
        graph.AddFile(MakeFile("b"));

        // a depends on b, b depends on a => circular
        graph.AddDependency("a", "b");
        graph.AddDependency("b", "a");

        Assert.Throws<CircularDependencyException>(() => graph.TopologicalSort());
    }

    [Fact]
    public void TopologicalSort_ThreeWayCircle_ThrowsCircularDependencyException()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("a"));
        graph.AddFile(MakeFile("b"));
        graph.AddFile(MakeFile("c"));

        graph.AddDependency("a", "b");
        graph.AddDependency("b", "c");
        graph.AddDependency("c", "a");

        Assert.Throws<CircularDependencyException>(() => graph.TopologicalSort());
    }

    [Fact]
    public void TopologicalSort_SingleFile_ReturnsSingleFile()
    {
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("solo"));

        var result = graph.TopologicalSort();

        Assert.Single(result);
        Assert.Equal("solo", result[0].FileName);
    }

    [Fact]
    public void TopologicalSort_ExternalDependencyIgnored_DoesNotBreak()
    {
        // If a file depends on something not in the graph, it should still work
        var graph = new DependencyGraph(_logger);
        graph.AddFile(MakeFile("a"));

        // "a" depends on "external" which is not in the graph
        graph.AddDependency("a", "external");

        var result = graph.TopologicalSort();

        Assert.Single(result);
        Assert.Equal("a", result[0].FileName);
    }

    [Fact]
    public void BuildFromFiles_ResolvesImportsCorrectly()
    {
        var files = new List<ProtoFileInfo>
        {
            MakeFile("player"),
            MakeFile("common")
        };

        var parsedFiles = new List<ProtoFileNode>
        {
            new()
            {
                FileName = "player.proto",
                Imports = new List<ImportNode>
                {
                    new() { Path = "common.proto" }
                }
            },
            new()
            {
                FileName = "common.proto",
                Imports = new List<ImportNode>()
            }
        };

        var graph = DependencyGraph.BuildFromFiles(files, parsedFiles, _logger);
        var result = graph.TopologicalSort();

        Assert.Equal(2, result.Count);
        var indexCommon = result.ToList().FindIndex(f => f.FileName == "common");
        var indexPlayer = result.ToList().FindIndex(f => f.FileName == "player");
        Assert.True(indexCommon < indexPlayer, "common should come before player");
    }

    [Fact]
    public void BuildFromFiles_ExternalImport_LogsWarning()
    {
        var files = new List<ProtoFileInfo>
        {
            MakeFile("player")
        };

        var parsedFiles = new List<ProtoFileNode>
        {
            new()
            {
                FileName = "player.proto",
                Imports = new List<ImportNode>
                {
                    new() { Path = "google/protobuf/timestamp.proto" }
                }
            }
        };

        var graph = DependencyGraph.BuildFromFiles(files, parsedFiles, _logger);
        var result = graph.TopologicalSort();

        Assert.Single(result);
        Assert.Contains(_logger.WarningMessages, w => w.Contains("not found in input directory"));
    }
}
