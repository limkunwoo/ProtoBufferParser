using Antlr4.Runtime;
using ProtoBufferParser.Exceptions;

namespace ProtoBufferParser.Parsers;

/// <summary>
/// Custom ANTLR error listener that converts ANTLR syntax errors
/// into <see cref="ProtoSyntaxException"/> for structured error reporting.
/// </summary>
public sealed class ProtoErrorListener : BaseErrorListener
{
    private readonly string _fileName;

    public ProtoErrorListener(string fileName)
    {
        _fileName = fileName;
    }

    public override void SyntaxError(
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new ProtoSyntaxException(_fileName, line, charPositionInLine, msg);
    }
}
