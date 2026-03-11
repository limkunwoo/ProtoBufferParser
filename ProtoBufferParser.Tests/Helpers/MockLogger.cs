using ProtoBufferParser.Interfaces;

namespace ProtoBufferParser.Tests.Helpers;

/// <summary>
/// Mock logger that captures log messages for test assertions.
/// </summary>
public sealed class MockLogger : ILogger
{
    public List<string> InfoMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<string> ErrorMessages { get; } = new();
    public List<string> VerboseMessages { get; } = new();

    public void LogInfo(string message) => InfoMessages.Add(message);
    public void LogWarning(string message) => WarningMessages.Add(message);
    public void LogError(string message) => ErrorMessages.Add(message);
    public void LogVerbose(string message) => VerboseMessages.Add(message);
}
