# ProtoBufferParser - Implementation Plan

## 개요

이 문서는 Protocol Buffer to Unreal Engine 컴파일러의 구현 계획과 단계별 작업 내용을 설명합니다.

## 전체 구현 일정

```
Phase 1: 기초 설정 (1-2일)
  └─ 프로젝트 구조, ANTLR4 설정, NuGet 패키지

Phase 2: 파싱 계층 (3-4일)
  └─ Input Handler, ANTLR4 통합, AST 구조

Phase 3: 타입 변환 (2-3일)
  └─ Type Mapper, 네이밍 규칙, Flatten 로직

Phase 4: 코드 생성 (3-4일)
  └─ 템플릿 엔진, 헤더/구현 생성

Phase 5: CLI 및 통합 (2일)
  └─ 명령줄 파싱, 전체 파이프라인 연결

Phase 6: 테스트 및 검증 (2-3일)
  └─ 단위 테스트, 통합 테스트, 예제 검증

총 예상 기간: 13-18일
```

---

## Phase 1: 기초 설정 및 환경 구성

### 목표
- 프로젝트 구조 생성
- ANTLR4 런타임 설치
- 기본 인프라 구축

### 작업 항목

#### 1.1 프로젝트 구조 생성
```
ProtoBufferParser/
├── CLI/
│   └── Program.cs
├── Core/
│   ├── InputHandler/
│   │   ├── IInputHandler.cs
│   │   ├── FileScanner.cs
│   │   └── DependencyGraph.cs
│   ├── Parser/
│   │   ├── IProtoParser.cs
│   │   └── AntlrProtoParser.cs
│   ├── Ast/
│   │   ├── AstNode.cs
│   │   ├── ProtoFileNode.cs
│   │   ├── MessageNode.cs
│   │   ├── FieldNode.cs
│   │   ├── EnumNode.cs
│   │   └── EnumValueNode.cs
│   ├── TypeMapping/
│   │   ├── ITypeMapper.cs
│   │   └── UnrealTypeMapper.cs
│   ├── CodeGeneration/
│   │   ├── ICodeGenerator.cs
│   │   ├── UnrealCodeGenerator.cs
│   │   └── Templates/
│   │       ├── StructTemplate.cs
│   │       ├── EnumTemplate.cs
│   │       └── FileHeaderTemplate.cs
│   └── OutputWriter/
│       ├── IOutputWriter.cs
│       └── FileSystemWriter.cs
├── Models/
│   ├── CompilerOptions.cs
│   ├── GeneratedFile.cs
│   └── ProtoFileInfo.cs
├── Utilities/
│   ├── ILogger.cs
│   ├── ConsoleLogger.cs
│   ├── ErrorReporter.cs
│   └── NamingHelper.cs
├── Exceptions/
│   ├── ProtoCompilationException.cs
│   ├── ProtoSyntaxException.cs
│   └── ProtoSemanticException.cs
└── Grammar/
    └── Proto3.g4 (ANTLR4 문법 파일)
```

**소요 시간**: 2-3시간

#### 1.2 NuGet 패키지 설치
```bash
# ANTLR4 런타임
dotnet add package Antlr4.Runtime.Standard --version 4.13.1

# 명령줄 파싱
dotnet add package System.CommandLine --version 2.0.0-beta4.22272.1

# 추가 유틸리티 (선택)
dotnet add package Humanizer.Core --version 2.14.1  # 이름 변환 (PascalCase 등)
```

**소요 시간**: 30분

#### 1.3 Proto3 ANTLR4 문법 다운로드
```bash
# GitHub에서 공식 Proto3 문법 다운로드
# https://github.com/antlr/grammars-v4/tree/master/protobuf3
```

문법 파일을 `Grammar/Proto3.g4`에 저장하고, ANTLR4 도구로 파서 생성:

```bash
# ANTLR4 도구 설치 (Java 필요)
# 또는 .NET ANTLR4 도구 사용
dotnet tool install -g Antlr4.CodeGenerator

# 파서 생성
antlr4 -Dlanguage=CSharp -visitor -no-listener Grammar/Proto3.g4 -o Core/Parser/Generated
```

**소요 시간**: 1-2시간

#### 1.4 기본 인터페이스 및 모델 정의
```csharp
// Models/CompilerOptions.cs
public class CompilerOptions
{
    public string InputDirectory { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public List<string> ImportPaths { get; set; } = new();
    public string? Namespace { get; set; }
    public string PropertySpecifier { get; set; } = "EditAnywhere";
    public bool Verbose { get; set; }
}

// Models/ProtoFileInfo.cs
public class ProtoFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Imports { get; set; } = new();
}

// Models/GeneratedFile.cs
public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string HeaderContent { get; set; } = string.Empty;
    public string CppContent { get; set; } = string.Empty;
}

// Utilities/ILogger.cs
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogVerbose(string message);
}
```

**소요 시간**: 2-3시간

---

## Phase 2: 파싱 계층 구현

### 목표
- .proto 파일 스캔 및 읽기
- ANTLR4 파서 통합
- AST 구조 생성

### 작업 항목

#### 2.1 Input Handler 구현
```csharp
// Core/InputHandler/FileScanner.cs
public class FileScanner
{
    public IEnumerable<ProtoFileInfo> ScanDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        
        var protoFiles = Directory.GetFiles(path, "*.proto", SearchOption.AllDirectories);
        
        foreach (var file in protoFiles)
        {
            yield return new ProtoFileInfo
            {
                FilePath = file,
                FileName = Path.GetFileNameWithoutExtension(file),
                Content = File.ReadAllText(file)
            };
        }
    }
}

// Core/InputHandler/DependencyGraph.cs
public class DependencyGraph
{
    private readonly Dictionary<string, ProtoFileInfo> _files = new();
    private readonly Dictionary<string, List<string>> _dependencies = new();
    
    public void AddFile(ProtoFileInfo file)
    {
        _files[file.FileName] = file;
        _dependencies[file.FileName] = new List<string>();
    }
    
    public void AddDependency(string from, string to)
    {
        if (!_dependencies.ContainsKey(from))
            _dependencies[from] = new List<string>();
        
        _dependencies[from].Add(to);
    }
    
    public IEnumerable<ProtoFileInfo> TopologicalSort()
    {
        // Kahn's algorithm 구현
        var inDegree = new Dictionary<string, int>();
        var queue = new Queue<string>();
        var result = new List<ProtoFileInfo>();
        
        // 진입 차수 계산
        foreach (var file in _files.Keys)
        {
            inDegree[file] = 0;
        }
        
        foreach (var deps in _dependencies.Values)
        {
            foreach (var dep in deps)
            {
                if (inDegree.ContainsKey(dep))
                    inDegree[dep]++;
            }
        }
        
        // 진입 차수가 0인 노드를 큐에 추가
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
                queue.Enqueue(kvp.Key);
        }
        
        // 위상 정렬
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(_files[current]);
            
            if (_dependencies.ContainsKey(current))
            {
                foreach (var dep in _dependencies[current])
                {
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                        queue.Enqueue(dep);
                }
            }
        }
        
        // 순환 의존성 검사
        if (result.Count != _files.Count)
        {
            throw new ProtoCompilationException("Circular dependency detected");
        }
        
        return result;
    }
}
```

**소요 시간**: 3-4시간

#### 2.2 ANTLR4 파서 통합
```csharp
// Core/Parser/AntlrProtoParser.cs
public class AntlrProtoParser : IProtoParser
{
    private readonly ILogger _logger;
    
    public AntlrProtoParser(ILogger logger)
    {
        _logger = logger;
    }
    
    public Proto3Parser.ProtoContext Parse(string content, string fileName)
    {
        try
        {
            var inputStream = new AntlrInputStream(content);
            var lexer = new Proto3Lexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new Proto3Parser(tokenStream);
            
            // 에러 리스너 추가
            var errorListener = new ProtoErrorListener(fileName);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            
            return parser.proto();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to parse {fileName}: {ex.Message}");
            throw;
        }
    }
}

// Core/Parser/ProtoErrorListener.cs
public class ProtoErrorListener : BaseErrorListener
{
    private readonly string _fileName;
    
    public ProtoErrorListener(string fileName)
    {
        _fileName = fileName;
    }
    
    public override void SyntaxError(
        TextWriter output,
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
```

**소요 시간**: 2-3시간

#### 2.3 AST 노드 정의 및 빌더
```csharp
// Core/Ast/AstBuilder.cs
public class AstBuilder : Proto3ParserBaseVisitor<AstNode>
{
    private readonly string _fileName;
    private readonly ILogger _logger;
    
    public ProtoFileNode BuildAst(Proto3Parser.ProtoContext context, string fileName)
    {
        _fileName = fileName;
        return VisitProto(context);
    }
    
    public override ProtoFileNode VisitProto(Proto3Parser.ProtoContext context)
    {
        var file = new ProtoFileNode
        {
            FileName = _fileName,
            Imports = new List<ImportNode>(),
            Messages = new List<MessageNode>(),
            Enums = new List<EnumNode>()
        };
        
        // Package 추출 (무시하지만 저장)
        var packageStmt = context.packageStatement();
        if (packageStmt != null)
        {
            file.Package = packageStmt.fullIdent().GetText();
        }
        
        // Imports 추출
        foreach (var importStmt in context.importStatement())
        {
            file.Imports.Add(new ImportNode
            {
                Path = importStmt.strLit().GetText().Trim('"'),
                IsPublic = importStmt.PUBLIC() != null
            });
        }
        
        // Top-level 정의 처리
        foreach (var topLevel in context.topLevelDef())
        {
            if (topLevel.messageDef() != null)
            {
                file.Messages.Add(VisitMessageDef(topLevel.messageDef()));
            }
            else if (topLevel.enumDef() != null)
            {
                file.Enums.Add(VisitEnumDef(topLevel.enumDef()));
            }
        }
        
        return file;
    }
    
    public MessageNode VisitMessageDef(Proto3Parser.MessageDefContext context)
    {
        var message = new MessageNode
        {
            Name = context.messageName().GetText(),
            Fields = new List<FieldNode>(),
            NestedMessages = new List<MessageNode>(),
            NestedEnums = new List<EnumNode>()
        };
        
        // 필드 추출
        foreach (var field in context.messageBody().field())
        {
            message.Fields.Add(VisitField(field));
        }
        
        // 중첩 메시지
        foreach (var nested in context.messageBody().messageDef())
        {
            message.NestedMessages.Add(VisitMessageDef(nested));
        }
        
        // 중첩 Enum
        foreach (var enumDef in context.messageBody().enumDef())
        {
            message.NestedEnums.Add(VisitEnumDef(enumDef));
        }
        
        return message;
    }
    
    public FieldNode VisitField(Proto3Parser.FieldContext context)
    {
        var field = new FieldNode
        {
            Name = context.fieldName().GetText(),
            FieldNumber = int.Parse(context.fieldNumber().GetText())
        };
        
        // Repeated 체크
        if (context.REPEATED() != null)
        {
            field.IsRepeated = true;
        }
        
        // 타입 추출
        var typeContext = context.type_();
        if (typeContext.messageType() != null)
        {
            // 메시지 타입
            field.Type = typeContext.messageType().GetText();
        }
        else if (typeContext.enumType() != null)
        {
            // Enum 타입
            field.Type = typeContext.enumType().GetText();
        }
        else
        {
            // 기본 타입
            field.Type = typeContext.GetText();
        }
        
        return field;
    }
    
    public EnumNode VisitEnumDef(Proto3Parser.EnumDefContext context)
    {
        var enumNode = new EnumNode
        {
            Name = context.enumName().GetText(),
            Values = new List<EnumValueNode>()
        };
        
        foreach (var field in context.enumBody().enumField())
        {
            enumNode.Values.Add(new EnumValueNode
            {
                Name = field.ident().GetText(),
                Value = int.Parse(field.intLit().GetText())
            });
        }
        
        return enumNode;
    }
}
```

**소요 시간**: 4-6시간

#### 2.4 중첩 메시지 Flatten 처리
```csharp
// Core/Ast/MessageFlattener.cs
public class MessageFlattener
{
    public List<MessageNode> FlattenMessages(List<MessageNode> messages, string parentName = "")
    {
        var result = new List<MessageNode>();
        
        foreach (var message in messages)
        {
            var fullName = string.IsNullOrEmpty(parentName) 
                ? message.Name 
                : $"{parentName}_{message.Name}";
            
            message.FullName = fullName;
            result.Add(message);
            
            // 중첩 메시지를 재귀적으로 Flatten
            if (message.NestedMessages.Count > 0)
            {
                var flattened = FlattenMessages(message.NestedMessages, fullName);
                result.AddRange(flattened);
                message.NestedMessages.Clear(); // 이미 Flatten했으므로 제거
            }
            
            // 중첩 Enum도 Flatten
            foreach (var nestedEnum in message.NestedEnums)
            {
                nestedEnum.FullName = $"{fullName}_{nestedEnum.Name}";
            }
        }
        
        return result;
    }
}
```

**소요 시간**: 2-3시간

---

## Phase 3: 타입 변환 계층 구현

### 목표
- Proto3 타입을 Unreal C++ 타입으로 매핑
- 네이밍 규칙 적용
- Map 타입 처리

### 작업 항목

#### 3.1 UnrealTypeMapper 구현
```csharp
// Core/TypeMapping/UnrealTypeMapper.cs
public class UnrealTypeMapper : ITypeMapper
{
    private static readonly Dictionary<string, string> _primitiveTypeMap = new()
    {
        { "int32", "int32" },
        { "int64", "int64" },
        { "uint32", "uint32" },
        { "uint64", "uint64" },
        { "sint32", "int32" },
        { "sint64", "int64" },
        { "fixed32", "uint32" },
        { "fixed64", "uint64" },
        { "sfixed32", "int32" },
        { "sfixed64", "int64" },
        { "float", "float" },
        { "double", "double" },
        { "bool", "bool" },
        { "string", "FString" },
        { "bytes", "TArray<uint8>" }
    };
    
    public string MapFieldType(FieldNode field)
    {
        // 기본 타입
        if (_primitiveTypeMap.TryGetValue(field.Type, out var primitiveType))
        {
            return field.IsRepeated ? $"TArray<{primitiveType}>" : primitiveType;
        }
        
        // Map 타입 (Proto3의 map<K,V>는 별도 처리 필요)
        if (field.IsMap)
        {
            var keyType = MapPrimitiveType(field.MapKeyType);
            var valueType = MapFieldType(new FieldNode { Type = field.MapValueType });
            return $"TMap<{keyType}, {valueType}>";
        }
        
        // 메시지/Enum 타입
        var typeName = ConvertToUnrealTypeName(field.Type, field.IsEnum);
        return field.IsRepeated ? $"TArray<{typeName}>" : typeName;
    }
    
    public string ConvertToUnrealTypeName(string protoName, bool isEnum = false)
    {
        var prefix = isEnum ? "E" : "F";
        var pascalName = ToPascalCase(protoName);
        return $"{prefix}{pascalName}Proto";
    }
    
    public string GetOutputFileName(string protoName)
    {
        var pascalName = ToPascalCase(protoName);
        return $"ME{pascalName}Proto";
    }
    
    private string ToPascalCase(string input)
    {
        // snake_case 또는 camelCase를 PascalCase로 변환
        return string.Join("", input.Split('_')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }
    
    private string MapPrimitiveType(string protoType)
    {
        return _primitiveTypeMap.TryGetValue(protoType, out var mapped) ? mapped : protoType;
    }
}
```

**소요 시간**: 3-4시간

#### 3.2 필드명 변환
```csharp
// Utilities/NamingHelper.cs
public static class NamingHelper
{
    public static string ToUnrealFieldName(string protoFieldName)
    {
        // Proto: player_name → Unreal: PlayerName
        return ToPascalCase(protoFieldName);
    }
    
    public static string ToUnrealEnumValue(string protoEnumValue)
    {
        // Proto: ACTIVE_STATUS → Unreal: ActiveStatus
        return ToPascalCase(protoEnumValue.ToLower());
    }
    
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        var parts = input.Split('_');
        return string.Join("", parts.Select(p => 
            char.ToUpper(p[0]) + p.Substring(1).ToLower()));
    }
}
```

**소요 시간**: 1-2시간

---

## Phase 4: 코드 생성 계층 구현

### 목표
- Unreal C++ 헤더 파일 생성
- Unreal C++ 구현 파일 생성
- USTRUCT, UENUM, UPROPERTY 매크로 생성

### 작업 항목

#### 4.1 템플릿 시스템 구현
```csharp
// Core/CodeGeneration/Templates/FileHeaderTemplate.cs
public class FileHeaderTemplate
{
    public string Generate(string sourceProtoFile)
    {
        return $@"// ----------------------------------------------
// AUTO-GENERATED from {sourceProtoFile}
// DO NOT MODIFY - Changes will be overwritten
// Generated by ProtoBufferParser
// ----------------------------------------------
";
    }
}

// Core/CodeGeneration/Templates/StructTemplate.cs
public class StructTemplate
{
    private readonly ITypeMapper _typeMapper;
    
    public string GenerateHeader(MessageNode message, List<string> dependencies)
    {
        var structName = _typeMapper.ConvertToUnrealTypeName(message.FullName);
        var fileName = _typeMapper.GetOutputFileName(message.FullName);
        
        var sb = new StringBuilder();
        
        // Header guard
        sb.AppendLine("#pragma once");
        sb.AppendLine();
        
        // Includes
        sb.AppendLine("#include \"CoreMinimal.h\"");
        
        // 의존성 includes
        foreach (var dep in dependencies)
        {
            var depFileName = _typeMapper.GetOutputFileName(dep);
            sb.AppendLine($"#include \"{depFileName}.h\"");
        }
        
        sb.AppendLine($"#include \"{fileName}.generated.h\"");
        sb.AppendLine();
        
        // USTRUCT 정의
        sb.AppendLine("USTRUCT(BlueprintType)");
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");
        sb.AppendLine("    GENERATED_BODY()");
        sb.AppendLine();
        
        // 필드들
        foreach (var field in message.Fields)
        {
            var fieldType = _typeMapper.MapFieldType(field);
            var fieldName = NamingHelper.ToUnrealFieldName(field.Name);
            
            sb.AppendLine($"    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = \"Proto\")");
            sb.AppendLine($"    {fieldType} {fieldName};");
            sb.AppendLine();
        }
        
        sb.AppendLine("};");
        
        return sb.ToString();
    }
    
    public string GenerateCpp(MessageNode message, string headerFileName)
    {
        var sb = new StringBuilder();
        
        // Include own header
        sb.AppendLine($"#include \"{headerFileName}.h\"");
        sb.AppendLine();
        
        // 현재는 빈 구현 (필요시 추가)
        sb.AppendLine("// Implementation file");
        sb.AppendLine("// Add custom implementations here if needed");
        
        return sb.ToString();
    }
}

// Core/CodeGeneration/Templates/EnumTemplate.cs
public class EnumTemplate
{
    private readonly ITypeMapper _typeMapper;
    
    public string GenerateHeader(EnumNode enumNode)
    {
        var enumName = _typeMapper.ConvertToUnrealTypeName(enumNode.FullName, isEnum: true);
        var fileName = _typeMapper.GetOutputFileName(enumNode.FullName);
        
        var sb = new StringBuilder();
        
        // Header guard
        sb.AppendLine("#pragma once");
        sb.AppendLine();
        
        // Includes
        sb.AppendLine("#include \"CoreMinimal.h\"");
        sb.AppendLine($"#include \"{fileName}.generated.h\"");
        sb.AppendLine();
        
        // UENUM 정의
        sb.AppendLine("UENUM(BlueprintType)");
        sb.AppendLine($"enum class {enumName} : uint8");
        sb.AppendLine("{");
        
        // Enum 값들
        for (int i = 0; i < enumNode.Values.Count; i++)
        {
            var enumValue = enumNode.Values[i];
            var valueName = NamingHelper.ToUnrealEnumValue(enumValue.Name);
            var displayName = enumValue.Name;
            
            sb.Append($"    {valueName} = {enumValue.Value} UMETA(DisplayName = \"{displayName}\")");
            
            if (i < enumNode.Values.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }
        
        sb.AppendLine("};");
        
        return sb.ToString();
    }
    
    public string GenerateCpp(EnumNode enumNode, string headerFileName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"#include \"{headerFileName}.h\"");
        sb.AppendLine();
        sb.AppendLine("// Enum implementation");
        
        return sb.ToString();
    }
}
```

**소요 시간**: 4-6시간

#### 4.2 UnrealCodeGenerator 구현
```csharp
// Core/CodeGeneration/UnrealCodeGenerator.cs
public class UnrealCodeGenerator : ICodeGenerator
{
    private readonly ITypeMapper _typeMapper;
    private readonly ILogger _logger;
    private readonly FileHeaderTemplate _headerTemplate;
    private readonly StructTemplate _structTemplate;
    private readonly EnumTemplate _enumTemplate;
    
    public UnrealCodeGenerator(ITypeMapper typeMapper, ILogger logger)
    {
        _typeMapper = typeMapper;
        _logger = logger;
        _headerTemplate = new FileHeaderTemplate();
        _structTemplate = new StructTemplate(typeMapper);
        _enumTemplate = new EnumTemplate(typeMapper);
    }
    
    public List<GeneratedFile> Generate(ProtoFileNode ast)
    {
        var files = new List<GeneratedFile>();
        
        _logger.LogVerbose($"Generating code for {ast.FileName}");
        
        // Enum 생성
        foreach (var enumNode in ast.Enums)
        {
            files.Add(GenerateEnum(enumNode, ast.FileName));
        }
        
        // Message (Struct) 생성
        foreach (var message in ast.Messages)
        {
            files.Add(GenerateStruct(message, ast.FileName, ast.Imports));
        }
        
        return files;
    }
    
    private GeneratedFile GenerateEnum(EnumNode enumNode, string sourceFile)
    {
        var fileName = _typeMapper.GetOutputFileName(enumNode.FullName);
        
        var headerContent = new StringBuilder();
        headerContent.Append(_headerTemplate.Generate(sourceFile));
        headerContent.AppendLine();
        headerContent.Append(_enumTemplate.GenerateHeader(enumNode));
        
        var cppContent = new StringBuilder();
        cppContent.Append(_headerTemplate.Generate(sourceFile));
        cppContent.AppendLine();
        cppContent.Append(_enumTemplate.GenerateCpp(enumNode, fileName));
        
        return new GeneratedFile
        {
            FileName = fileName,
            HeaderContent = headerContent.ToString(),
            CppContent = cppContent.ToString()
        };
    }
    
    private GeneratedFile GenerateStruct(MessageNode message, string sourceFile, List<ImportNode> imports)
    {
        var fileName = _typeMapper.GetOutputFileName(message.FullName);
        
        // Import된 타입 의존성 추출
        var dependencies = ExtractDependencies(message, imports);
        
        var headerContent = new StringBuilder();
        headerContent.Append(_headerTemplate.Generate(sourceFile));
        headerContent.AppendLine();
        headerContent.Append(_structTemplate.GenerateHeader(message, dependencies));
        
        var cppContent = new StringBuilder();
        cppContent.Append(_headerTemplate.Generate(sourceFile));
        cppContent.AppendLine();
        cppContent.Append(_structTemplate.GenerateCpp(message, fileName));
        
        return new GeneratedFile
        {
            FileName = fileName,
            HeaderContent = headerContent.ToString(),
            CppContent = cppContent.ToString()
        };
    }
    
    private List<string> ExtractDependencies(MessageNode message, List<ImportNode> imports)
    {
        var dependencies = new HashSet<string>();
        
        foreach (var field in message.Fields)
        {
            // 메시지 타입이면 의존성 추가 (기본 타입은 제외)
            if (!IsPrimitiveType(field.Type))
            {
                dependencies.Add(field.Type);
            }
        }
        
        return dependencies.ToList();
    }
    
    private bool IsPrimitiveType(string type)
    {
        var primitives = new[] { "int32", "int64", "uint32", "uint64", 
            "float", "double", "bool", "string", "bytes" };
        return primitives.Contains(type);
    }
}
```

**소요 시간**: 3-4시간

---

## Phase 5: CLI 및 전체 통합

### 목표
- 명령줄 인터페이스 구현
- 전체 파이프라인 연결
- 에러 처리 및 로깅

### 작업 항목

#### 5.1 CLI 구현 (System.CommandLine 사용)
```csharp
// CLI/Program.cs
using System.CommandLine;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputOption = new Option<string>(
            aliases: new[] { "--input-dir", "-i" },
            description: "Input directory containing .proto files")
        {
            IsRequired = true
        };
        
        var outputOption = new Option<string>(
            aliases: new[] { "--output-dir", "-o" },
            description: "Output directory for generated files")
        {
            IsRequired = true
        };
        
        var importPathsOption = new Option<string[]>(
            aliases: new[] { "--import-paths", "-I" },
            description: "Additional import search paths")
        {
            AllowMultipleArgumentsPerToken = true
        };
        
        var namespaceOption = new Option<string?>(
            aliases: new[] { "--namespace", "-n" },
            description: "C++ namespace for generated code");
        
        var propertySpecOption = new Option<string>(
            aliases: new[] { "--property-spec", "-p" },
            getDefaultValue: () => "EditAnywhere",
            description: "UPROPERTY specifier");
        
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging");
        
        var rootCommand = new RootCommand("ProtoBufferParser - Convert .proto files to Unreal C++ structs")
        {
            inputOption,
            outputOption,
            importPathsOption,
            namespaceOption,
            propertySpecOption,
            verboseOption
        };
        
        rootCommand.SetHandler(async (inputDir, outputDir, importPaths, ns, propSpec, verbose) =>
        {
            var options = new CompilerOptions
            {
                InputDirectory = inputDir,
                OutputDirectory = outputDir,
                ImportPaths = importPaths?.ToList() ?? new List<string>(),
                Namespace = ns,
                PropertySpecifier = propSpec,
                Verbose = verbose
            };
            
            await RunCompilerAsync(options);
        },
        inputOption, outputOption, importPathsOption, namespaceOption, propertySpecOption, verboseOption);
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static async Task RunCompilerAsync(CompilerOptions options)
    {
        var logger = new ConsoleLogger(options.Verbose);
        
        try
        {
            logger.LogInfo("ProtoBufferParser starting...");
            logger.LogInfo($"Input: {options.InputDirectory}");
            logger.LogInfo($"Output: {options.OutputDirectory}");
            
            var compiler = new ProtoCompiler(options, logger);
            await compiler.CompileAsync();
            
            logger.LogInfo("Compilation completed successfully!");
        }
        catch (ProtoCompilationException ex)
        {
            logger.LogError($"Compilation failed: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            logger.LogError($"Unexpected error: {ex.Message}");
            if (options.Verbose)
            {
                logger.LogError(ex.StackTrace ?? "");
            }
            Environment.Exit(1);
        }
    }
}
```

**소요 시간**: 2-3시간

#### 5.2 ProtoCompiler 메인 오케스트레이터
```csharp
// Core/ProtoCompiler.cs
public class ProtoCompiler
{
    private readonly CompilerOptions _options;
    private readonly ILogger _logger;
    private readonly FileScanner _scanner;
    private readonly AntlrProtoParser _parser;
    private readonly AstBuilder _astBuilder;
    private readonly MessageFlattener _flattener;
    private readonly ITypeMapper _typeMapper;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IOutputWriter _outputWriter;
    
    public ProtoCompiler(CompilerOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
        _scanner = new FileScanner();
        _parser = new AntlrProtoParser(logger);
        _astBuilder = new AstBuilder(logger);
        _flattener = new MessageFlattener();
        _typeMapper = new UnrealTypeMapper();
        _codeGenerator = new UnrealCodeGenerator(_typeMapper, logger);
        _outputWriter = new FileSystemWriter(_options.OutputDirectory, logger);
    }
    
    public async Task CompileAsync()
    {
        // 1. 파일 스캔
        _logger.LogInfo("Scanning for .proto files...");
        var files = _scanner.ScanDirectory(_options.InputDirectory).ToList();
        _logger.LogInfo($"Found {files.Count} .proto files");
        
        // 2. 의존성 그래프 구축
        _logger.LogVerbose("Building dependency graph...");
        var depGraph = BuildDependencyGraph(files);
        var orderedFiles = depGraph.TopologicalSort().ToList();
        
        // 3. 각 파일 컴파일
        var allGeneratedFiles = new List<GeneratedFile>();
        
        foreach (var file in orderedFiles)
        {
            _logger.LogInfo($"Compiling {file.FileName}.proto...");
            
            // 파싱
            var parseTree = _parser.Parse(file.Content, file.FileName);
            
            // AST 생성
            var ast = _astBuilder.BuildAst(parseTree, file.FileName);
            
            // 중첩 메시지 Flatten
            ast.Messages = _flattener.FlattenMessages(ast.Messages);
            
            // 코드 생성
            var generatedFiles = _codeGenerator.Generate(ast);
            allGeneratedFiles.AddRange(generatedFiles);
        }
        
        // 4. 파일 쓰기
        _logger.LogInfo($"Writing {allGeneratedFiles.Count} generated files...");
        await _outputWriter.WriteAllAsync(allGeneratedFiles);
        
        _logger.LogInfo("Done!");
    }
    
    private DependencyGraph BuildDependencyGraph(List<ProtoFileInfo> files)
    {
        var graph = new DependencyGraph();
        
        foreach (var file in files)
        {
            graph.AddFile(file);
            
            // Import 문 파싱 (간단한 정규식 사용)
            var imports = ExtractImports(file.Content);
            foreach (var import in imports)
            {
                var importFileName = Path.GetFileNameWithoutExtension(import);
                graph.AddDependency(file.FileName, importFileName);
            }
        }
        
        return graph;
    }
    
    private List<string> ExtractImports(string content)
    {
        var imports = new List<string>();
        var lines = content.Split('\n');
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("import"))
            {
                var match = Regex.Match(trimmed, @"import\s+""([^""]+)""");
                if (match.Success)
                {
                    imports.Add(match.Groups[1].Value);
                }
            }
        }
        
        return imports;
    }
}
```

**소요 시간**: 3-4시간

#### 5.3 OutputWriter 구현
```csharp
// Core/OutputWriter/FileSystemWriter.cs
public class FileSystemWriter : IOutputWriter
{
    private readonly string _outputDirectory;
    private readonly ILogger _logger;
    
    public FileSystemWriter(string outputDirectory, ILogger logger)
    {
        _outputDirectory = outputDirectory;
        _logger = logger;
    }
    
    public async Task WriteAllAsync(List<GeneratedFile> files)
    {
        // 출력 디렉토리 생성
        Directory.CreateDirectory(_outputDirectory);
        
        foreach (var file in files)
        {
            // 헤더 파일
            var headerPath = Path.Combine(_outputDirectory, $"{file.FileName}.h");
            await File.WriteAllTextAsync(headerPath, file.HeaderContent);
            _logger.LogVerbose($"Written: {headerPath}");
            
            // CPP 파일
            var cppPath = Path.Combine(_outputDirectory, $"{file.FileName}.cpp");
            await File.WriteAllTextAsync(cppPath, file.CppContent);
            _logger.LogVerbose($"Written: {cppPath}");
        }
    }
}
```

**소요 시간**: 1-2시간

---

## Phase 6: 테스트 및 검증

### 목표
- 단위 테스트 작성
- 통합 테스트 작성
- 예제 검증

### 작업 항목

#### 6.1 테스트 프로젝트 생성
```bash
dotnet new xunit -n ProtoBufferParser.Tests
dotnet sln add ProtoBufferParser.Tests/ProtoBufferParser.Tests.csproj
dotnet add ProtoBufferParser.Tests reference ProtoBufferParser/ProtoBufferParser.csproj
```

**소요 시간**: 30분

#### 6.2 단위 테스트 작성
```csharp
// Tests/TypeMapperTests.cs
public class TypeMapperTests
{
    private readonly UnrealTypeMapper _mapper = new();
    
    [Theory]
    [InlineData("int32", "int32")]
    [InlineData("string", "FString")]
    [InlineData("bytes", "TArray<uint8>")]
    public void MapPrimitiveType_ShouldReturnCorrectUnrealType(string protoType, string expected)
    {
        var field = new FieldNode { Type = protoType, IsRepeated = false };
        var result = _mapper.MapFieldType(field);
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void MapRepeatedField_ShouldReturnTArray()
    {
        var field = new FieldNode { Type = "int32", IsRepeated = true };
        var result = _mapper.MapFieldType(field);
        Assert.Equal("TArray<int32>", result);
    }
    
    [Theory]
    [InlineData("PlayerInfo", "FPlayerInfoProto")]
    [InlineData("player_info", "FPlayerInfoProto")]
    public void ConvertToUnrealTypeName_ShouldApplyNamingConvention(string input, string expected)
    {
        var result = _mapper.ConvertToUnrealTypeName(input);
        Assert.Equal(expected, result);
    }
}

// Tests/NamingHelperTests.cs
public class NamingHelperTests
{
    [Theory]
    [InlineData("player_name", "PlayerName")]
    [InlineData("user_id", "UserId")]
    [InlineData("HP", "Hp")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = NamingHelper.ToPascalCase(input);
        Assert.Equal(expected, result);
    }
}
```

**소요 시간**: 3-4시간

#### 6.3 통합 테스트
```csharp
// Tests/IntegrationTests/EndToEndTests.cs
public class EndToEndTests
{
    [Fact]
    public async Task CompileSimpleProto_ShouldGenerateCorrectFiles()
    {
        // Arrange
        var protoContent = @"
syntax = ""proto3"";

message Player {
    int32 id = 1;
    string name = 2;
}
";
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var inputDir = Path.Combine(tempDir, "input");
        var outputDir = Path.Combine(tempDir, "output");
        
        Directory.CreateDirectory(inputDir);
        await File.WriteAllTextAsync(Path.Combine(inputDir, "player.proto"), protoContent);
        
        var options = new CompilerOptions
        {
            InputDirectory = inputDir,
            OutputDirectory = outputDir,
            Verbose = false
        };
        
        // Act
        var compiler = new ProtoCompiler(options, new ConsoleLogger(false));
        await compiler.CompileAsync();
        
        // Assert
        Assert.True(File.Exists(Path.Combine(outputDir, "MEPlayerProto.h")));
        Assert.True(File.Exists(Path.Combine(outputDir, "MEPlayerProto.cpp")));
        
        var headerContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MEPlayerProto.h"));
        Assert.Contains("struct FPlayerProto", headerContent);
        Assert.Contains("int32 Id;", headerContent);
        Assert.Contains("FString Name;", headerContent);
        
        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
```

**소요 시간**: 4-5시간

---

## Phase 7: 문서화 및 마무리

### 작업 항목

#### 7.1 README.md 작성
- 프로젝트 소개
- 설치 방법
- 사용 예제
- CLI 옵션 설명

**소요 시간**: 2시간

#### 7.2 예제 프로젝트
- 샘플 .proto 파일 작성
- 생성된 출력 예제
- Unreal Engine 통합 가이드

**소요 시간**: 2시간

---

## 우선순위 및 의존성

```
Priority 1 (필수):
├── Phase 1: 기초 설정
├── Phase 2: 파싱 계층
├── Phase 3: 타입 변환
├── Phase 4: 코드 생성
└── Phase 5: CLI 통합

Priority 2 (중요):
├── Phase 6: 테스트
└── 기본 에러 처리

Priority 3 (향후):
├── GUI 도구
├── 고급 옵션
└── 성능 최적화
```

## 마일스톤

### Milestone 1: 기본 동작 (Phase 1-5)
- 단일 .proto 파일 → 단일 .h/.cpp 생성
- 기본 타입, Message, Enum 지원
- CLI 동작

### Milestone 2: 완전한 기능 (Phase 6)
- Import 처리
- 중첩 메시지 Flatten
- 테스트 커버리지 70%+

### Milestone 3: 프로덕션 준비 (Phase 7)
- 완전한 문서
- 예제 및 가이드
- 에러 처리 강화

---

**다음 단계**: Phase 1 시작 - ANTLR4 설정 및 프로젝트 구조 생성
