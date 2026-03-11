using ProtoBufferParser.Interfaces;

namespace ProtoBufferParser.Services;

/// <summary>
/// Console-based logger implementation.
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly bool _verbose;
    
    public ConsoleLogger(bool verbose = false)
    {
        _verbose = verbose;
    }
    
    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    
    public void LogWarning(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARNING] {message}");
        Console.ForegroundColor = originalColor;
    }
    
    public void LogError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"[ERROR] {message}");
        Console.ForegroundColor = originalColor;
    }
    
    public void LogVerbose(string message)
    {
        if (_verbose)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[VERBOSE] {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
