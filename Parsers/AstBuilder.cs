using Antlr4.Runtime;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Parsers;

/// <summary>
/// Visitor that converts ANTLR4 parse tree into custom AST nodes.
/// Handles messages, enums, fields, map fields, oneof fields, and imports.
/// </summary>
public sealed class AstBuilder : Protobuf3BaseVisitor<AstNode?>
{
    private readonly string _fileName;
    private readonly ILogger _logger;

    public AstBuilder(string fileName, ILogger logger)
    {
        _fileName = fileName;
        _logger = logger;
    }

    /// <summary>
    /// Entry point: builds a <see cref="ProtoFileNode"/> from a <see cref="Protobuf3Parser.ProtoContext"/>.
    /// </summary>
    public ProtoFileNode BuildAst(Protobuf3Parser.ProtoContext context)
    {
        var fileNode = new ProtoFileNode
        {
            FileName = _fileName,
            Location = CreateLocation(context)
        };

        // Package (grammar allows multiple packageStatement, but typically 0 or 1)
        var packageStatements = context.packageStatement();
        if (packageStatements.Length > 0)
        {
            fileNode.Package = packageStatements[0].fullIdent().GetText();
        }

        // Imports
        foreach (var importCtx in context.importStatement())
        {
            var importNode = VisitImportStatement(importCtx);
            if (importNode != null)
            {
                fileNode.Imports.Add(importNode);
            }
        }

        // Top-level definitions (messages, enums; services/extends are ignored)
        foreach (var topLevelCtx in context.topLevelDef())
        {
            if (topLevelCtx.messageDef() != null)
            {
                var messageNode = VisitMessageDef(topLevelCtx.messageDef());
                if (messageNode != null)
                {
                    fileNode.Messages.Add(messageNode);
                }
            }
            else if (topLevelCtx.enumDef() != null)
            {
                var enumNode = VisitEnumDef(topLevelCtx.enumDef());
                if (enumNode != null)
                {
                    fileNode.Enums.Add(enumNode);
                }
            }
            // serviceDef and extendDef are intentionally ignored
        }

        return fileNode;
    }

    /// <summary>
    /// Visits an import statement and creates an <see cref="ImportNode"/>.
    /// </summary>
    public new ImportNode VisitImportStatement(Protobuf3Parser.ImportStatementContext context)
    {
        var path = context.strLit().GetText();
        // Remove surrounding quotes (single or double)
        path = path.Trim('"', '\'');

        return new ImportNode
        {
            Path = path,
            IsPublic = context.PUBLIC() != null,
            IsWeak = context.WEAK() != null,
            Location = CreateLocation(context)
        };
    }

    /// <summary>
    /// Visits a message definition and creates a <see cref="MessageNode"/>.
    /// Handles fields, nested messages, nested enums, map fields, and oneof fields.
    /// </summary>
    public new MessageNode VisitMessageDef(Protobuf3Parser.MessageDefContext context)
    {
        var name = context.messageName().GetText();
        var message = new MessageNode
        {
            Name = name,
            FullName = name, // Will be updated by MessageFlattener for nested messages
            Location = CreateLocation(context)
        };

        var body = context.messageBody();
        foreach (var element in body.messageElement())
        {
            if (element.field() != null)
            {
                var fieldNode = VisitFieldContext(element.field());
                if (fieldNode != null)
                {
                    message.Fields.Add(fieldNode);
                }
            }
            else if (element.mapField() != null)
            {
                var mapFieldNode = VisitMapFieldContext(element.mapField());
                if (mapFieldNode != null)
                {
                    message.Fields.Add(mapFieldNode);
                }
            }
            else if (element.oneof() != null)
            {
                // Oneof fields are flattened into the message's field list
                var oneofFields = VisitOneofContext(element.oneof());
                message.Fields.AddRange(oneofFields);
            }
            else if (element.messageDef() != null)
            {
                var nestedMessage = VisitMessageDef(element.messageDef());
                if (nestedMessage != null)
                {
                    message.NestedMessages.Add(nestedMessage);
                }
            }
            else if (element.enumDef() != null)
            {
                var nestedEnum = VisitEnumDef(element.enumDef());
                if (nestedEnum != null)
                {
                    message.NestedEnums.Add(nestedEnum);
                }
            }
            // optionStatement, reserved, extendDef, emptyStatement are ignored
        }

        // Validate for duplicate field numbers
        ValidateFieldNumbers(message);

        return message;
    }

    /// <summary>
    /// Visits a regular field (non-map, non-oneof).
    /// Grammar: fieldLabel? type_ fieldName EQ fieldNumber (LB fieldOptions RB)? SEMI
    /// </summary>
    private FieldNode VisitFieldContext(Protobuf3Parser.FieldContext context)
    {
        var field = new FieldNode
        {
            Name = context.fieldName().GetText(),
            FieldNumber = ParseFieldNumber(context.fieldNumber()),
            Location = CreateLocation(context)
        };

        // Check field label (optional/repeated)
        var label = context.fieldLabel();
        if (label != null)
        {
            if (label.REPEATED() != null)
            {
                field.IsRepeated = true;
            }
            else if (label.OPTIONAL() != null)
            {
                field.IsOptional = true;
            }
        }

        // Extract type and determine if it's an enum reference
        field.Type = ExtractTypeName(context.type_());
        field.IsEnum = IsEnumTypeReference(context.type_());

        return field;
    }

    /// <summary>
    /// Visits a map field definition.
    /// Grammar: MAP LT keyType COMMA type_ GT mapName EQ fieldNumber ...
    /// </summary>
    private FieldNode VisitMapFieldContext(Protobuf3Parser.MapFieldContext context)
    {
        return new FieldNode
        {
            Name = context.mapName().GetText(),
            FieldNumber = ParseFieldNumber(context.fieldNumber()),
            IsMap = true,
            MapKeyType = context.keyType().GetText(),
            MapValueType = ExtractTypeName(context.type_()),
            MapValueIsEnum = IsEnumTypeReference(context.type_()),
            Type = "map",
            Location = CreateLocation(context)
        };
    }

    /// <summary>
    /// Visits a oneof definition and returns all its fields.
    /// Each field gets IsOneOf = true and the oneof group name preserved.
    /// In our Unreal mapping, oneof fields are mapped to TOptional&lt;T&gt;.
    /// </summary>
    private List<FieldNode> VisitOneofContext(Protobuf3Parser.OneofContext context)
    {
        var fields = new List<FieldNode>();
        var oneofGroupName = context.oneofName().GetText();

        foreach (var oneofFieldCtx in context.oneofField())
        {
            var field = new FieldNode
            {
                Name = oneofFieldCtx.fieldName().GetText(),
                FieldNumber = ParseFieldNumber(oneofFieldCtx.fieldNumber()),
                Type = ExtractTypeName(oneofFieldCtx.type_()),
                IsEnum = IsEnumTypeReference(oneofFieldCtx.type_()),
                IsOneOf = true,
                OneOfGroupName = oneofGroupName,
                Location = CreateLocation(oneofFieldCtx)
            };

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    /// Visits an enum definition and creates an <see cref="EnumNode"/>.
    /// </summary>
    public new EnumNode VisitEnumDef(Protobuf3Parser.EnumDefContext context)
    {
        var name = context.enumName().GetText();
        var enumNode = new EnumNode
        {
            Name = name,
            FullName = name, // Will be updated by MessageFlattener for nested enums
            Location = CreateLocation(context)
        };

        var body = context.enumBody();
        foreach (var element in body.enumElement())
        {
            if (element.enumField() != null)
            {
                var valueNode = VisitEnumFieldContext(element.enumField());
                if (valueNode != null)
                {
                    enumNode.Values.Add(valueNode);
                }
            }
            // optionStatement, reserved, emptyStatement are ignored
        }

        return enumNode;
    }

    /// <summary>
    /// Visits an enum field (value definition).
    /// Grammar: ident EQ (MINUS)? intLit enumValueOptions? SEMI
    /// </summary>
    private EnumValueNode VisitEnumFieldContext(Protobuf3Parser.EnumFieldContext context)
    {
        var name = context.ident().GetText();
        var value = int.Parse(context.intLit().GetText());

        // Handle negative values
        if (context.MINUS() != null)
        {
            value = -value;
        }

        return new EnumValueNode
        {
            Name = name,
            Value = value,
            Location = CreateLocation(context)
        };
    }

    /// <summary>
    /// Extracts the type name from a type_ context.
    /// Handles primitive types, message types, and enum types.
    /// </summary>
    private static string ExtractTypeName(Protobuf3Parser.Type_Context context)
    {
        // If it's a message type reference
        if (context.messageType() != null)
        {
            return context.messageType().GetText();
        }

        // If it's an enum type reference
        if (context.enumType() != null)
        {
            return context.enumType().GetText();
        }

        // Primitive type - GetText() returns the keyword text (e.g., "int32", "string", etc.)
        return context.GetText();
    }

    /// <summary>
    /// Determines whether the type_ context references an enum type.
    /// </summary>
    private static bool IsEnumTypeReference(Protobuf3Parser.Type_Context context)
    {
        return context.enumType() != null;
    }

    /// <summary>
    /// Parses a field number from a fieldNumber context.
    /// </summary>
    private static int ParseFieldNumber(Protobuf3Parser.FieldNumberContext context)
    {
        return int.Parse(context.GetText());
    }

    /// <summary>
    /// Validates that a message has no duplicate field numbers.
    /// </summary>
    private void ValidateFieldNumbers(MessageNode message)
    {
        var seenNumbers = new HashSet<int>();
        foreach (var field in message.Fields)
        {
            if (!seenNumbers.Add(field.FieldNumber))
            {
                _logger.LogWarning(
                    $"{_fileName}: Duplicate field number {field.FieldNumber} " +
                    $"in message '{message.Name}' for field '{field.Name}'");
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="SourceLocation"/> from a parser rule context.
    /// </summary>
    private SourceLocation CreateLocation(ParserRuleContext context)
    {
        return new SourceLocation
        {
            FileName = _fileName,
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }
}
