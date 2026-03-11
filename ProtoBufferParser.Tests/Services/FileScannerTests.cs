using ProtoBufferParser.Services;
using ProtoBufferParser.Tests.Helpers;

namespace ProtoBufferParser.Tests.Services;

/// <summary>
/// Tests for <see cref="FileScanner"/>.
/// Uses temporary directories with real .proto files.
/// </summary>
public class FileScannerTests : IDisposable
{
    private readonly MockLogger _logger = new();
    private readonly FileScanner _scanner;
    private readonly string _tempDir;

    public FileScannerTests()
    {
        _scanner = new FileScanner(_logger);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProtoParserTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void ScanDirectory_WithProtoFiles_ReturnsAll()
    {
        File.WriteAllText(Path.Combine(_tempDir, "player.proto"), "syntax = \"proto3\";");
        File.WriteAllText(Path.Combine(_tempDir, "item.proto"), "syntax = \"proto3\";");

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, f => f.FileName == "player");
        Assert.Contains(results, f => f.FileName == "item");
    }

    [Fact]
    public void ScanDirectory_ReturnsCorrectContent()
    {
        var content = "syntax = \"proto3\";\nmessage Test {}";
        File.WriteAllText(Path.Combine(_tempDir, "test.proto"), content);

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Single(results);
        Assert.Equal(content, results[0].Content);
        Assert.Equal("test", results[0].FileName);
    }

    [Fact]
    public void ScanDirectory_RecursiveSearch_FindsSubdirectoryFiles()
    {
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_tempDir, "root.proto"), "syntax = \"proto3\";");
        File.WriteAllText(Path.Combine(subDir, "nested.proto"), "syntax = \"proto3\";");

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ScanDirectory_NoProtoFiles_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "not a proto file");

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ScanDirectory_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var fakePath = Path.Combine(_tempDir, "nonexistent");

        Assert.Throws<DirectoryNotFoundException>(() =>
            _scanner.ScanDirectory(fakePath).ToList());
    }

    [Fact]
    public void ScanDirectory_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _scanner.ScanDirectory(null!).ToList());
    }

    [Fact]
    public void ScanDirectory_ResultsAreSorted()
    {
        File.WriteAllText(Path.Combine(_tempDir, "z_last.proto"), "syntax = \"proto3\";");
        File.WriteAllText(Path.Combine(_tempDir, "a_first.proto"), "syntax = \"proto3\";");
        File.WriteAllText(Path.Combine(_tempDir, "m_middle.proto"), "syntax = \"proto3\";");

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal("a_first", results[0].FileName);
        Assert.Equal("m_middle", results[1].FileName);
        Assert.Equal("z_last", results[2].FileName);
    }

    [Fact]
    public void ScanDirectory_SetsFilePathCorrectly()
    {
        var filePath = Path.Combine(_tempDir, "player.proto");
        File.WriteAllText(filePath, "syntax = \"proto3\";");

        var results = _scanner.ScanDirectory(_tempDir).ToList();

        Assert.Single(results);
        Assert.Equal(filePath, results[0].FilePath);
    }
}
