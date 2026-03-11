using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Services;

/// <summary>
/// Scans a directory for .proto files and returns file information.
/// </summary>
public sealed class FileScanner
{
    private readonly ILogger _logger;

    public FileScanner(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scans the specified directory (recursively) for .proto files.
    /// </summary>
    /// <param name="directoryPath">The root directory to scan.</param>
    /// <returns>An enumerable of <see cref="ProtoFileInfo"/> for each .proto file found.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public IEnumerable<ProtoFileInfo> ScanDirectory(string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {directoryPath}");
        }

        var protoFiles = Directory.GetFiles(directoryPath, "*.proto", SearchOption.AllDirectories);
        _logger.LogInfo($"Found {protoFiles.Length} .proto file(s) in '{directoryPath}'");

        foreach (var filePath in protoFiles.OrderBy(f => f))
        {
            var fileInfo = new ProtoFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileNameWithoutExtension(filePath),
                Content = File.ReadAllText(filePath)
            };

            _logger.LogVerbose($"  Scanned: {fileInfo.FileName}.proto");
            yield return fileInfo;
        }
    }
}
