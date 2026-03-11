using Antlr4.Runtime;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Parsers;

/// <summary>
/// ANTLR4-based implementation of <see cref="IProtoParser"/>.
/// Performs lexing, two-pass parsing, and AST construction from .proto file content.
/// Tracks known types from previously parsed files to support cross-file type references.
/// </summary>
public sealed class AntlrProtoParser : IProtoParser
{
    private readonly ILogger _logger;
    private readonly HashSet<string> _knownMessageTypes = new();
    private readonly HashSet<string> _knownEnumTypes = new();

    public AntlrProtoParser(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers all message and enum type names from a previously parsed file.
    /// Call this after parsing each file (in topological order) so that subsequent
    /// files can reference types defined in earlier files.
    /// </summary>
    public void RegisterParsedFile(ProtoFileNode ast)
    {
        ArgumentNullException.ThrowIfNull(ast);

        foreach (var msg in ast.Messages)
        {
            CollectMessageTypeNames(msg, "");
        }

        foreach (var en in ast.Enums)
        {
            _knownEnumTypes.Add(en.Name);
        }
    }

    /// <inheritdoc />
    public ProtoFileNode Parse(string content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fileName);

        _logger.LogVerbose($"Parsing: {fileName}");

        try
        {
            // Lexing
            var inputStream = new AntlrInputStream(content);
            var lexer = new Protobuf3Lexer(inputStream);
            lexer.RemoveErrorListeners();
            
            var tokenStream = new CommonTokenStream(lexer);
            
            // Parsing (two-pass: first pass builds symbol table, second pass produces parse tree)
            var parser = new Protobuf3Parser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ProtoErrorListener(fileName));

            // Register known types from previously parsed files
            // These survive DoRewind() because CopySymbolTableFrom() only adds to existing sets
            foreach (var msgType in _knownMessageTypes)
            {
                parser.RegisterExternalMessageType(msgType);
            }
            foreach (var enumType in _knownEnumTypes)
            {
                parser.RegisterExternalEnumType(enumType);
            }
            
            // Use twoPassParse as the entry point - it calls DoRewind() then proto()
            var twoPassContext = parser.twoPassParse();
            var protoContext = twoPassContext.proto();

            // AST construction
            var astBuilder = new AstBuilder(fileName, _logger);
            var fileNode = astBuilder.BuildAst(protoContext);

            _logger.LogVerbose($"Parsed {fileName}: {fileNode.Messages.Count} messages, {fileNode.Enums.Count} enums");
            return fileNode;
        }
        catch (Exceptions.ProtoSyntaxException)
        {
            throw; // Already a structured error, re-throw as-is
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to parse {fileName}: {ex.Message}");
            throw new Exceptions.ProtoSyntaxException(fileName, 0, 0, $"Unexpected parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively collects message type names (including nested messages and their enums).
    /// Names are stored in both simple and scoped forms for flexible matching.
    /// </summary>
    private void CollectMessageTypeNames(MessageNode msg, string prefix)
    {
        var fullName = string.IsNullOrEmpty(prefix) ? msg.Name : $"{prefix}.{msg.Name}";
        _knownMessageTypes.Add(msg.Name);
        _knownMessageTypes.Add(fullName);

        foreach (var nested in msg.NestedMessages)
        {
            CollectMessageTypeNames(nested, fullName);
        }

        foreach (var en in msg.NestedEnums)
        {
            _knownEnumTypes.Add(en.Name);
            var enumFullName = $"{fullName}.{en.Name}";
            _knownEnumTypes.Add(enumFullName);
        }
    }
}
