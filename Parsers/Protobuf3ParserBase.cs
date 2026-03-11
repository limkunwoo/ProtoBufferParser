using Antlr4.Runtime;

namespace ProtoBufferParser.Parsers;

/// <summary>
/// Base parser class for Protobuf3 grammar.
/// Provides two-pass parsing with symbol table support.
/// </summary>
public abstract class Protobuf3ParserBase : Parser
{
    private readonly HashSet<string> _messageTypes = new();
    private readonly HashSet<string> _enumTypes = new();
    private readonly Stack<string> _scopeStack = new();
    private int _currentBlockDepth = 0;
    
    /// <summary>
    /// When true, IsMessageType_() and IsEnumType_() always return true.
    /// Used during the first pass so that custom type references don't cause
    /// parse errors before message/enum names have been collected.
    /// </summary>
    private bool _isFirstPass = false;
    
    protected Protobuf3ParserBase(ITokenStream input) : base(input)
    {
    }
    
    /// <summary>
    /// Performs the first pass to build symbol table, then rewinds the input stream.
    /// Called by the generated twoPassParse rule action: { this.DoRewind(); }
    /// </summary>
    /// <remarks>
    /// Uses a separate parser instance for the first pass to avoid corrupting
    /// the current parser's internal state (calling Reset() on the same instance
    /// would null out _ctx, causing NRE in ExitRule() of twoPassParse).
    /// </remarks>
    protected void DoRewind()
    {
        // Get the token stream
        var tokenStream = (ITokenStream)InputStream;
        if (tokenStream is not CommonTokenStream commonStream)
        {
            return;
        }

        // First pass: use a SEPARATE parser instance to build symbol table
        // We cannot call proto() + Reset() on this instance because Reset()
        // would null out _ctx, causing NRE when twoPassParse's finally block
        // calls ExitRule().
        try
        {
            commonStream.Seek(0);
            var firstPassParser = new Protobuf3Parser(commonStream);
            firstPassParser.RemoveErrorListeners(); // Silence errors on first pass
            firstPassParser.EnableFirstPassMode();  // Allow all type references in first pass
            firstPassParser.proto(); // Parse to discover message/enum names
            
            // Copy symbol table from first pass parser
            CopySymbolTableFrom(firstPassParser);
        }
        catch
        {
            // Ignore errors in first pass - we only need to collect names
        }

        // Rewind token stream to beginning for the second pass
        commonStream.Seek(0);
        
        // Reset scope tracking for clean second pass
        _scopeStack.Clear();
        _currentBlockDepth = 0;
    }
    
    /// <summary>
    /// Enables first-pass mode where IsMessageType_() and IsEnumType_() always return true.
    /// This allows the parser to fully parse the input without needing a pre-built symbol table.
    /// </summary>
    internal void EnableFirstPassMode()
    {
        _isFirstPass = true;
    }

    /// <summary>
    /// Registers an external message type name (from an imported file) into the symbol table.
    /// This allows the parser to recognize types defined in other .proto files.
    /// Must be called before twoPassParse() — registered types survive DoRewind()
    /// because CopySymbolTableFrom() only adds to the existing set.
    /// </summary>
    public void RegisterExternalMessageType(string name)
    {
        _messageTypes.Add(name);
    }

    /// <summary>
    /// Registers an external enum type name (from an imported file) into the symbol table.
    /// This allows the parser to recognize enum types defined in other .proto files.
    /// Must be called before twoPassParse().
    /// </summary>
    public void RegisterExternalEnumType(string name)
    {
        _enumTypes.Add(name);
    }
    
    /// <summary>
    /// Copies the symbol table (message and enum type names) from another parser base instance.
    /// </summary>
    internal void CopySymbolTableFrom(Protobuf3ParserBase other)
    {
        foreach (var msgType in other._messageTypes)
        {
            _messageTypes.Add(msgType);
        }
        foreach (var enumType in other._enumTypes)
        {
            _enumTypes.Add(enumType);
        }
    }
    
    /// <summary>
    /// Proto parsing method (overridden by generated parser).
    /// Must match the generated parser's proto() signature.
    /// </summary>
    public virtual Protobuf3Parser.ProtoContext proto()
    {
        // This will be overridden by the generated Protobuf3Parser class
        return null!;
    }
    
    /// <summary>
    /// Registers a message name in the symbol table.
    /// Called by grammar action: { DoMessageNameDef_(); }
    /// The action fires in an epsilon rule after messageName was parsed,
    /// so the name is the previous token, not CurrentToken (which is already lookahead).
    /// </summary>
    protected void DoMessageNameDef_()
    {
        var tokenStream = (ITokenStream)InputStream;
        var nameToken = tokenStream.Lt(-1);
        var messageName = nameToken?.Text ?? string.Empty;
        var fullName = GetFullScopedName(messageName);
        _messageTypes.Add(fullName);
    }
    
    /// <summary>
    /// Registers an enum name in the symbol table.
    /// Called by grammar action: { DoEnumNameDef_(); }
    /// The action fires in an epsilon rule after enumName was parsed,
    /// so the name is the previous token, not CurrentToken (which is already lookahead).
    /// </summary>
    protected void DoEnumNameDef_()
    {
        var tokenStream = (ITokenStream)InputStream;
        var nameToken = tokenStream.Lt(-1);
        var enumName = nameToken?.Text ?? string.Empty;
        var fullName = GetFullScopedName(enumName);
        _enumTypes.Add(fullName);
    }
    
    /// <summary>
    /// Registers a service name in the symbol table.
    /// Called by grammar action: { DoServiceNameDef_(); }
    /// </summary>
    protected void DoServiceNameDef_()
    {
        var tokenStream = (ITokenStream)InputStream;
        var nameToken = tokenStream.Lt(-1);
        var serviceName = nameToken?.Text ?? string.Empty;
        var fullName = GetFullScopedName(serviceName);
        // Services are not used in our Unreal Engine code generation
    }
    
    /// <summary>
    /// Processes an import statement.
    /// Called by grammar action: { DoImportStatement_(); }
    /// </summary>
    protected void DoImportStatement_()
    {
        // Import statements are recorded for dependency tracking
        // The actual import path will be extracted during AST building
    }
    
    /// <summary>
    /// Enters a new scope block (for nested messages/enums).
    /// Called by grammar action: { DoEnterBlock_(); }
    /// </summary>
    protected void DoEnterBlock_()
    {
        _currentBlockDepth++;
    }
    
    /// <summary>
    /// Exits the current scope block.
    /// Called by grammar action: { DoExitBlock_(); }
    /// </summary>
    protected void DoExitBlock_()
    {
        _currentBlockDepth--;
        if (_scopeStack.Count > 0)
        {
            _scopeStack.Pop();
        }
    }
    
    /// <summary>
    /// Checks if the current identifier is a known message type.
    /// Used as semantic predicate: {IsMessageType_()}?
    /// In first-pass mode, always returns true to allow collecting all names.
    /// </summary>
    protected bool IsMessageType_()
    {
        if (_isFirstPass) return true;
        var typeName = GetPotentialTypeName();
        return _messageTypes.Contains(typeName) || _messageTypes.Contains($".{typeName}");
    }
    
    /// <summary>
    /// Checks if the current identifier is a known enum type.
    /// Used as semantic predicate: {IsEnumType_()}?
    /// In first-pass mode, always returns true to allow collecting all names.
    /// </summary>
    protected bool IsEnumType_()
    {
        if (_isFirstPass) return true;
        var typeName = GetPotentialTypeName();
        return _enumTypes.Contains(typeName) || _enumTypes.Contains($".{typeName}");
    }
    
    /// <summary>
    /// Checks if the current token is NOT a keyword.
    /// Used as semantic predicate: {IsNotKeyword()}?
    /// </summary>
    protected bool IsNotKeyword()
    {
        if (CurrentToken == null) return true;
        
        var keywords = new[]
        {
            "syntax", "import", "weak", "public", "package", "option",
            "repeated", "optional", "oneof", "map", "reserved", "to", "max",
            "enum", "message", "service", "rpc", "returns", "stream",
            "int32", "int64", "uint32", "uint64", "sint32", "sint64",
            "fixed32", "fixed64", "sfixed32", "sfixed64",
            "bool", "string", "double", "float", "bytes"
        };
        
        return !keywords.Contains(CurrentToken.Text);
    }
    
    /// <summary>
    /// Gets the full scoped name for a type.
    /// </summary>
    private string GetFullScopedName(string name)
    {
        if (_scopeStack.Count == 0)
        {
            return name;
        }
        
        return string.Join(".", _scopeStack.Reverse()) + "." + name;
    }
    
    /// <summary>
    /// Gets the potential type name from current context.
    /// </summary>
    private string GetPotentialTypeName()
    {
        return CurrentToken?.Text ?? string.Empty;
    }
}
