using ProtoBufferParser.Models;

namespace ProtoBufferParser.Exceptions;

/// <summary>
/// Base exception for all Protocol Buffer compilation errors.
/// </summary>
public class ProtoCompilationException : Exception
{
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    
    public ProtoCompilationException(string message) : base(message) { }
    
    public ProtoCompilationException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public ProtoCompilationException(string message, SourceLocation location, string errorCode = "PC000")
        : base(message)
    {
        FileName = location.FileName;
        Line = location.Line;
        Column = location.Column;
        ErrorCode = errorCode;
    }
    
    public override string Message => 
        string.IsNullOrEmpty(FileName)
            ? $"error {ErrorCode}: {base.Message}"
            : $"{FileName}({Line},{Column}): error {ErrorCode}: {base.Message}";
}

/// <summary>
/// Thrown when there is a syntax error in the .proto file.
/// </summary>
public class ProtoSyntaxException : ProtoCompilationException
{
    public ProtoSyntaxException(string message) : base(message)
    {
        ErrorCode = "PC001";
    }
    
    public ProtoSyntaxException(string message, SourceLocation location) 
        : base(message, location, "PC001") { }
    
    public ProtoSyntaxException(string fileName, int line, int column, string message)
        : base(message, new SourceLocation { FileName = fileName, Line = line, Column = column }, "PC001") { }
}

/// <summary>
/// Thrown when duplicate field numbers are detected.
/// </summary>
public class DuplicateFieldNumberException : ProtoCompilationException
{
    public DuplicateFieldNumberException(string message, SourceLocation location) 
        : base(message, location, "PC002") { }
}

/// <summary>
/// Thrown when an invalid type is referenced.
/// </summary>
public class InvalidTypeException : ProtoCompilationException
{
    public InvalidTypeException(string message, SourceLocation location) 
        : base(message, location, "PC003") { }
}

/// <summary>
/// Thrown when a circular import dependency is detected.
/// </summary>
public class CircularDependencyException : ProtoCompilationException
{
    public CircularDependencyException(string message) : base(message)
    {
        ErrorCode = "PC004";
    }
}

/// <summary>
/// Thrown when an imported file cannot be found.
/// </summary>
public class FileNotFoundException : ProtoCompilationException
{
    public FileNotFoundException(string filePath) 
        : base($"File not found: {filePath}")
    {
        ErrorCode = "PC005";
        FileName = filePath;
    }
}

/// <summary>
/// Thrown when Proto2 syntax is detected (only Proto3 is supported).
/// </summary>
public class UnsupportedProto2Exception : ProtoCompilationException
{
    public UnsupportedProto2Exception(string message, SourceLocation location) 
        : base(message, location, "PC006") { }
}
