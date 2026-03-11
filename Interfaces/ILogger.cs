namespace ProtoBufferParser.Interfaces;

/// <summary>
/// Interface for logging messages during compilation.
/// </summary>
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogVerbose(string message);
}
