# ProtoBufferParser - Architecture Design

## 개요

이 문서는 Protocol Buffer IDL 파일(.proto)을 Unreal Engine C++ 구조체로 변환하는 컴파일러의 전체 아키텍처를 설명합니다.

## 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                        ProtoBufferParser                         │
│                     (CLI Application - .NET 8.0)                 │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. Input Handler                                                │
│  ────────────────                                                │
│  • 폴더 스캔 (.proto 파일 검색)                                     │
│  • 파일 읽기 및 유효성 검사                                          │
│  • 의존성 그래프 생성 (import 처리)                                  │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Lexer (Lexical Analysis)                                     │
│  ────────────────────────────                                    │
│  • ANTLR4 기반 토큰화                                              │
│  • Proto3 문법 인식                                                │
│  • 토큰 스트림 생성                                                 │
│                                                                   │
│  [ANTLR4 Proto3 Grammar 사용]                                     │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. Parser (Syntax Analysis)                                     │
│  ───────────────────────────                                     │
│  • ANTLR4 기반 구문 분석                                            │
│  • Parse Tree 생성                                                │
│  • 문법 오류 검출 및 리포팅                                          │
│                                                                   │
│  출력: ANTLR4 Parse Tree                                          │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  4. AST Builder (Abstract Syntax Tree)                           │
│  ─────────────────────────────────────                           │
│  • Parse Tree → AST 변환                                          │
│  • 의미 분석 (Semantic Analysis)                                   │
│  • 타입 검증                                                        │
│  • 스코프 해석                                                      │
│                                                                   │
│  AST 노드:                                                         │
│  ├── ProtoFileNode                                                │
│  ├── MessageNode                                                  │
│  ├── FieldNode                                                    │
│  ├── EnumNode                                                     │
│  ├── EnumValueNode                                                │
│  └── ImportNode                                                   │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. Type Mapper                                                   │
│  ──────────────                                                   │
│  • Proto3 타입 → Unreal C++ 타입 변환                              │
│  • 중첩 메시지 Flatten 처리                                         │
│  • 네이밍 규칙 적용:                                                │
│    - Message: FMessageNameProto                                   │
│    - Enum: EEnumNameProto                                         │
│    - Nested: FOuterInnerProto                                     │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  6. Code Generator                                                │
│  ─────────────────                                                │
│  • Unreal C++ 코드 생성                                            │
│  • 헤더(.h) 생성: USTRUCT, UENUM 정의                              │
│  • 구현(.cpp) 생성: 필요시 구현 코드                                 │
│  • 템플릿 기반 코드 생성                                             │
│                                                                   │
│  파일명 규칙:                                                       │
│  ├── MEMessageNameProto.h                                         │
│  └── MEMessageNameProto.cpp                                       │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  7. Output Writer                                                 │
│  ───────────────                                                  │
│  • 파일 시스템에 쓰기                                                │
│  • 폴더 구조 생성                                                   │
│  • 자동 생성 경고 헤더 추가                                          │
│  • Import 의존성 헤더 추가                                          │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │  출력: .h / .cpp 파일   │
                    └────────────────────────┘
```

## 주요 컴포넌트

### 1. CLI Layer (Program.cs)

**책임:**
- 명령줄 인자 파싱
- 옵션 처리 및 유효성 검사
- 전체 파이프라인 오케스트레이션
- 에러 처리 및 사용자 피드백

**인터페이스:**
```csharp
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = ParseCommandLineOptions(args);
        var compiler = new ProtoCompiler(options);
        return await compiler.CompileAsync();
    }
}
```

**CLI 옵션:**
```
--input-dir, -i      : 입력 .proto 파일 폴더 경로 (필수)
--output-dir, -o     : 출력 .h/.cpp 파일 폴더 경로 (필수)
--import-paths, -I   : Import 검색 경로 (여러 개 지정 가능)
--namespace, -n      : C++ 네임스페이스 (선택, 기본값: 없음)
--property-spec, -p  : UPROPERTY 속성 (선택, 기본값: EditAnywhere)
--verbose, -v        : 상세 로그 출력
--help, -h           : 도움말 표시
```

### 2. Input Handler

**책임:**
- 디렉토리 스캔 및 .proto 파일 찾기
- 파일 읽기 및 인코딩 처리
- Import 의존성 그래프 생성
- 컴파일 순서 결정 (위상 정렬)

**주요 클래스:**
```csharp
public class InputHandler
{
    public IEnumerable<ProtoFileInfo> ScanDirectory(string path);
    public DependencyGraph BuildDependencyGraph(IEnumerable<ProtoFileInfo> files);
    public IEnumerable<ProtoFileInfo> GetCompilationOrder(DependencyGraph graph);
}

public class ProtoFileInfo
{
    public string FilePath { get; set; }
    public string Content { get; set; }
    public List<string> Imports { get; set; }
}

public class DependencyGraph
{
    public void AddFile(ProtoFileInfo file);
    public void AddDependency(string from, string to);
    public IEnumerable<ProtoFileInfo> TopologicalSort();
}
```

### 3. Lexer & Parser (ANTLR4)

**책임:**
- Proto3 문법 기반 어휘/구문 분석
- Parse Tree 생성
- 문법 오류 검출 및 리포팅

**ANTLR4 통합:**
```csharp
public class ProtoParserService
{
    private readonly AntlrInputStream _input;
    private readonly Proto3Lexer _lexer;
    private readonly Proto3Parser _parser;
    
    public Proto3Parser.ProtoContext Parse(string content)
    {
        var inputStream = new AntlrInputStream(content);
        var lexer = new Proto3Lexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new Proto3Parser(tokenStream);
        
        // 에러 리스너 추가
        parser.AddErrorListener(new ProtoErrorListener());
        
        return parser.proto(); // 최상위 규칙
    }
}

public class ProtoErrorListener : BaseErrorListener
{
    public override void SyntaxError(
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new ProtoSyntaxException(
            $"Line {line}:{charPositionInLine} - {msg}");
    }
}
```

### 4. AST Builder

**책임:**
- ANTLR4 Parse Tree를 커스텀 AST로 변환
- 의미 분석 (타입 체크, 필드 번호 중복 검사 등)
- 중첩 메시지 Flatten 처리

**AST 노드 구조:**
```csharp
public abstract class AstNode
{
    public SourceLocation Location { get; set; }
}

public class ProtoFileNode : AstNode
{
    public string FileName { get; set; }
    public string Package { get; set; } // 무시하지만 파싱은 함
    public List<ImportNode> Imports { get; set; }
    public List<MessageNode> Messages { get; set; }
    public List<EnumNode> Enums { get; set; }
}

public class MessageNode : AstNode
{
    public string Name { get; set; }
    public List<FieldNode> Fields { get; set; }
    public List<MessageNode> NestedMessages { get; set; }
    public List<EnumNode> NestedEnums { get; set; }
    
    // Flatten 후 생성되는 전체 이름
    public string FullName { get; set; } // e.g., "OuterInnerProto"
}

public class FieldNode : AstNode
{
    public string Name { get; set; }
    public string Type { get; set; }
    public int FieldNumber { get; set; }
    public bool IsRepeated { get; set; }
    public bool IsMap { get; set; }
    public string MapKeyType { get; set; }
    public string MapValueType { get; set; }
}

public class EnumNode : AstNode
{
    public string Name { get; set; }
    public List<EnumValueNode> Values { get; set; }
    public string FullName { get; set; }
}

public class EnumValueNode : AstNode
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public class ImportNode : AstNode
{
    public string Path { get; set; }
    public bool IsPublic { get; set; }
}
```

**AST Visitor 패턴:**
```csharp
public class AstBuilder : Proto3ParserBaseVisitor<AstNode>
{
    public override ProtoFileNode VisitProto(Proto3Parser.ProtoContext context)
    {
        var file = new ProtoFileNode();
        
        // Package 파싱 (무시하지만 저장)
        if (context.packageStatement() != null)
        {
            file.Package = context.packageStatement().GetText();
        }
        
        // Messages 파싱
        foreach (var msgCtx in context.topLevelDef().Where(t => t.messageDef() != null))
        {
            file.Messages.Add(VisitMessageDef(msgCtx.messageDef()) as MessageNode);
        }
        
        // Enums 파싱
        foreach (var enumCtx in context.topLevelDef().Where(t => t.enumDef() != null))
        {
            file.Enums.Add(VisitEnumDef(enumCtx.enumDef()) as EnumNode);
        }
        
        return file;
    }
    
    public override MessageNode VisitMessageDef(Proto3Parser.MessageDefContext context)
    {
        var message = new MessageNode
        {
            Name = context.messageName().GetText(),
            Fields = new List<FieldNode>(),
            NestedMessages = new List<MessageNode>(),
            NestedEnums = new List<EnumNode>()
        };
        
        // 필드 파싱
        foreach (var fieldCtx in context.messageBody().field())
        {
            message.Fields.Add(VisitField(fieldCtx) as FieldNode);
        }
        
        // 중첩 메시지 파싱
        foreach (var nestedCtx in context.messageBody().messageDef())
        {
            message.NestedMessages.Add(VisitMessageDef(nestedCtx) as MessageNode);
        }
        
        return message;
    }
}
```

### 5. Type Mapper

**책임:**
- Proto3 타입을 Unreal C++ 타입으로 매핑
- 네이밍 규칙 적용
- 중첩 타입 Flatten

**타입 매핑 테이블:**
```csharp
public class TypeMapper
{
    private static readonly Dictionary<string, string> _typeMap = new()
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
    
    public string MapType(FieldNode field)
    {
        // 기본 타입
        if (_typeMap.ContainsKey(field.Type))
        {
            var baseType = _typeMap[field.Type];
            return field.IsRepeated ? $"TArray<{baseType}>" : baseType;
        }
        
        // Map 타입
        if (field.IsMap)
        {
            var keyType = MapPrimitiveType(field.MapKeyType);
            var valueType = MapType(field.MapValueType);
            return $"TMap<{keyType}, {valueType}>";
        }
        
        // 메시지 타입
        var messageName = ApplyNamingConvention(field.Type);
        return field.IsRepeated ? $"TArray<{messageName}>" : messageName;
    }
    
    public string ApplyNamingConvention(string name, bool isEnum = false)
    {
        var prefix = isEnum ? "E" : "F";
        var pascalName = ToPascalCase(name);
        return $"{prefix}{pascalName}Proto";
    }
    
    public string GetFileName(string messageName)
    {
        var pascalName = ToPascalCase(messageName);
        return $"ME{pascalName}Proto";
    }
    
    public string FlattenNestedName(string outerName, string innerName)
    {
        return $"{outerName}{innerName}";
    }
}
```

### 6. Code Generator

**책임:**
- Unreal C++ 헤더(.h) 및 구현(.cpp) 파일 생성
- 템플릿 기반 코드 생성
- USTRUCT, UENUM, UPROPERTY 매크로 생성
- Import 의존성 헤더 추가

**코드 생성기 구조:**
```csharp
public class UnrealCodeGenerator
{
    private readonly TypeMapper _typeMapper;
    private readonly CodeGeneratorOptions _options;
    
    public GeneratedFiles Generate(ProtoFileNode astRoot)
    {
        var files = new GeneratedFiles();
        
        // Enum 생성
        foreach (var enumNode in astRoot.Enums)
        {
            files.AddEnum(GenerateEnum(enumNode));
        }
        
        // Message (Struct) 생성
        foreach (var message in astRoot.Messages)
        {
            files.AddStruct(GenerateStruct(message));
        }
        
        return files;
    }
    
    private GeneratedEnum GenerateEnum(EnumNode enumNode)
    {
        var enumName = _typeMapper.ApplyNamingConvention(enumNode.Name, isEnum: true);
        var fileName = _typeMapper.GetFileName(enumNode.Name);
        
        return new GeneratedEnum
        {
            Name = enumName,
            HeaderFile = GenerateEnumHeader(enumNode, enumName, fileName),
            CppFile = GenerateEnumCpp(enumNode, enumName, fileName)
        };
    }
    
    private GeneratedStruct GenerateStruct(MessageNode message)
    {
        var structName = _typeMapper.ApplyNamingConvention(message.Name);
        var fileName = _typeMapper.GetFileName(message.Name);
        
        return new GeneratedStruct
        {
            Name = structName,
            HeaderFile = GenerateStructHeader(message, structName, fileName),
            CppFile = GenerateStructCpp(message, structName, fileName)
        };
    }
}
```

**헤더 템플릿 구조:**
```csharp
public class HeaderTemplate
{
    public string Generate(MessageNode message, string structName, string fileName)
    {
        var sb = new StringBuilder();
        
        // 자동 생성 경고
        sb.AppendLine("// ----------------------------------------------");
        sb.AppendLine($"// AUTO-GENERATED from {message.SourceFile}");
        sb.AppendLine("// DO NOT MODIFY - Changes will be overwritten");
        sb.AppendLine("// Generated by ProtoBufferParser");
        sb.AppendLine("// ----------------------------------------------");
        sb.AppendLine();
        
        // Header guard
        sb.AppendLine("#pragma once");
        sb.AppendLine();
        
        // Includes
        sb.AppendLine("#include \"CoreMinimal.h\"");
        
        // Import 의존성
        foreach (var import in message.Dependencies)
        {
            var importFileName = _typeMapper.GetFileName(import);
            sb.AppendLine($"#include \"{importFileName}.h\"");
        }
        
        sb.AppendLine($"#include \"{fileName}.generated.h\"");
        sb.AppendLine();
        
        // USTRUCT 정의
        sb.AppendLine("USTRUCT(BlueprintType)");
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");
        sb.AppendLine("    GENERATED_BODY()");
        sb.AppendLine();
        
        // 기본 생성자
        sb.AppendLine($"    {structName}() = default;");
        sb.AppendLine();
        
        // 마샬링 생성자 (Proto → Unreal)
        var protoTypeName = GetProtoTypeName(message.FullName);
        sb.AppendLine($"    // Marshal from protobuf message");
        sb.AppendLine($"    explicit {structName}(const {protoTypeName}& proto);");
        sb.AppendLine();
        
        // 필드들
        foreach (var field in message.Fields)
        {
            var fieldType = _typeMapper.MapType(field);
            var fieldName = ToPascalCase(field.Name);
            
            sb.AppendLine($"    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = \"Proto\")");
            sb.AppendLine($"    {fieldType} {fieldName};");
            sb.AppendLine();
        }
        
        sb.AppendLine("};");
        
        return sb.ToString();
    }
    
    private string GetProtoTypeName(string messageName)
    {
        // Proto 패키지명은 무시하고 메시지명만 사용
        return $"::{messageName}";
    }
}
```

**CPP 템플릿 구조 (마샬링 생성자 구현):**
```csharp
public class CppTemplate
{
    public string Generate(MessageNode message, string structName, string fileName)
    {
        var sb = new StringBuilder();
        
        // Header include
        sb.AppendLine($"#include \"{fileName}.h\"");
        sb.AppendLine($"#include \"{GetProtoHeaderName(message.FullName)}\"");
        sb.AppendLine();
        
        // 마샬링 생성자 구현
        var protoTypeName = GetProtoTypeName(message.FullName);
        sb.AppendLine($"{structName}::{structName}(const {protoTypeName}& proto)");
        sb.AppendLine("{");
        
        // 각 필드 마샬링
        foreach (var field in message.Fields)
        {
            sb.AppendLine($"    {GenerateFieldMarshaling(field)}");
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private string GenerateFieldMarshaling(FieldNode field)
    {
        var fieldName = ToPascalCase(field.Name);
        var protoFieldName = field.Name;
        
        // Repeated 필드
        if (field.IsRepeated)
        {
            return GenerateRepeatedFieldMarshaling(fieldName, protoFieldName, field.Type);
        }
        
        // Map 필드
        if (field.IsMap)
        {
            return GenerateMapFieldMarshaling(fieldName, protoFieldName, field);
        }
        
        // 기본 타입 또는 메시지 타입
        if (IsPrimitiveType(field.Type))
        {
            return GeneratePrimitiveMarshaling(fieldName, protoFieldName, field.Type);
        }
        else
        {
            // 메시지 타입 - 재귀적 마샬링 (protoc getter 사용)
            return $"{fieldName} = {GetUnrealTypeName(field.Type)}(proto.{protoFieldName}());";
        }
    }
    
    private string GeneratePrimitiveMarshaling(string fieldName, string protoFieldName, string type)
    {
        if (type == "string")
        {
            // string → FString 변환 (protoc getter 반환값은 const std::string&)
            return $"{fieldName} = FString(UTF8_TO_TCHAR(proto.{protoFieldName}().c_str()));";
        }
        else if (type == "bytes")
        {
            // bytes → TArray<uint8> 변환
            return $@"const std::string& bytes_{protoFieldName} = proto.{protoFieldName}();
    {fieldName}.SetNum(bytes_{protoFieldName}.size());
    FMemory::Memcpy({fieldName}.GetData(), bytes_{protoFieldName}.data(), bytes_{protoFieldName}.size());";
        }
        else
        {
            // 숫자, bool 타입 - protoc getter로 접근
            return $"{fieldName} = proto.{protoFieldName}();";
        }
    }
    
    private string GenerateRepeatedFieldMarshaling(string fieldName, string protoFieldName, string type)
    {
        var sb = new StringBuilder();
        // protoc는 repeated 필드를 _size() 함수와 인덱스 접근으로 제공
        sb.AppendLine($"{fieldName}.Reserve(proto.{protoFieldName}_size());");
        sb.AppendLine($"    for (int i = 0; i < proto.{protoFieldName}_size(); ++i)");
        sb.AppendLine("    {");
        
        if (IsPrimitiveType(type))
        {
            if (type == "string")
            {
                sb.AppendLine($"        {fieldName}.Add(FString(UTF8_TO_TCHAR(proto.{protoFieldName}(i).c_str())));");
            }
            else
            {
                sb.AppendLine($"        {fieldName}.Add(proto.{protoFieldName}(i));");
            }
        }
        else
        {
            // 메시지 타입 - protoc의 인덱스 getter 사용
            var unrealType = GetUnrealTypeName(type);
            sb.AppendLine($"        {fieldName}.Add({unrealType}(proto.{protoFieldName}(i)));");
        }
        
        sb.AppendLine("    }");
        return sb.ToString();
    }
    
    private string GenerateMapFieldMarshaling(string fieldName, string protoFieldName, FieldNode field)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{fieldName}.Reserve(proto.{protoFieldName}_size());");
        sb.AppendLine($"    for (const auto& [key, value] : proto.{protoFieldName}())");
        sb.AppendLine("    {");
        
        var keyConversion = field.MapKeyType == "string" 
            ? "FString(UTF8_TO_TCHAR(key.c_str()))" 
            : "key";
        
        string valueConversion;
        if (IsPrimitiveType(field.MapValueType))
        {
            valueConversion = field.MapValueType == "string" 
                ? "FString(UTF8_TO_TCHAR(value.c_str()))" 
                : "value";
        }
        else
        {
            // 메시지 타입
            var unrealType = GetUnrealTypeName(field.MapValueType);
            valueConversion = $"{unrealType}(value)";
        }
        
        sb.AppendLine($"        {fieldName}.Add({keyConversion}, {valueConversion});");
        sb.AppendLine("    }");
        return sb.ToString();
    }
    
    private string GetProtoHeaderName(string messageName)
    {
        // Proto 헤더 파일명 (예: player.proto → player.pb.h)
        return $"{messageName.ToLower()}.pb.h";
    }
    
    private string GetProtoTypeName(string messageName)
    {
        return $"::{messageName}";
    }
    
    private string GetUnrealTypeName(string protoTypeName)
    {
        var prefix = "F";
        var pascalName = ToPascalCase(protoTypeName);
        return $"{prefix}{pascalName}Proto";
    }
    
    private bool IsPrimitiveType(string type)
    {
        var primitives = new[] { "int32", "int64", "uint32", "uint64", 
            "float", "double", "bool", "string", "bytes" };
        return primitives.Contains(type);
    }
}
```

### 7. Output Writer

**책임:**
- 생성된 코드를 파일 시스템에 쓰기
- 디렉토리 구조 생성
- 파일 덮어쓰기 처리

```csharp
public class OutputWriter
{
    private readonly string _outputDirectory;
    
    public async Task WriteFilesAsync(GeneratedFiles files)
    {
        Directory.CreateDirectory(_outputDirectory);
        
        foreach (var file in files.AllFiles)
        {
            var fullPath = Path.Combine(_outputDirectory, file.FileName);
            await File.WriteAllTextAsync(fullPath, file.Content);
        }
    }
}
```

## 데이터 플로우

```
.proto 파일
    ↓
[Input Handler] → ProtoFileInfo (파일 정보 + 의존성)
    ↓
[ANTLR4 Lexer] → Token Stream
    ↓
[ANTLR4 Parser] → Parse Tree
    ↓
[AST Builder] → AST (ProtoFileNode, MessageNode, EnumNode 등)
    ↓
[Type Mapper] → Unreal 타입으로 변환된 AST
    ↓
[Code Generator] → GeneratedFiles (헤더 + 구현 코드 문자열)
    ↓
[Output Writer] → .h / .cpp 파일
```

## 에러 처리 전략

### 1. 컴파일 타임 에러
```csharp
public class ProtoCompilationException : Exception
{
    public string FileName { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string ErrorCode { get; set; }
    
    public override string Message => 
        $"{FileName}({Line},{Column}): error {ErrorCode}: {base.Message}";
}
```

### 2. 에러 종류
- **PC001**: Syntax Error (문법 오류)
- **PC002**: Duplicate Field Number (필드 번호 중복)
- **PC003**: Invalid Type (잘못된 타입)
- **PC004**: Circular Import (순환 의존성)
- **PC005**: File Not Found (파일 찾을 수 없음)
- **PC006**: Unsupported Proto2 Syntax (Proto2 문법 사용)

### 3. 로깅
```csharp
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogVerbose(string message);
}

public class ConsoleLogger : ILogger
{
    private readonly bool _verbose;
    
    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    
    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }
}
```

## 확장성 고려사항

### 1. 플러그인 시스템 (향후)
```csharp
public interface ICodeGeneratorPlugin
{
    string Name { get; }
    string TargetPlatform { get; } // "Unreal", "Unity", "Custom"
    GeneratedFiles Generate(ProtoFileNode ast);
}
```

### 2. 커스텀 옵션 (향후 GUI)
```csharp
public class CodeGeneratorOptions
{
    public string PropertySpecifier { get; set; } = "EditAnywhere";
    public bool GenerateBlueprintReadWrite { get; set; } = true;
    public string CategoryName { get; set; } = "Proto";
    public bool GenerateComments { get; set; } = true;
    public string StructPrefix { get; set; } = "F";
    public string StructSuffix { get; set; } = "_Proto";
    public string EnumPrefix { get; set; } = "E";
    public string EnumSuffix { get; set; } = "_Proto";
    public string FilePrefix { get; set; } = "ME";
    public string FileSuffix { get; set; } = "_Proto";
}
```

### 3. 멀티스레딩 (대량 파일 처리)
```csharp
public class ParallelCompiler
{
    public async Task CompileAllAsync(IEnumerable<ProtoFileInfo> files)
    {
        var tasks = files.Select(file => CompileFileAsync(file));
        await Task.WhenAll(tasks);
    }
}
```

## 테스트 전략

### 1. 단위 테스트
- TypeMapper 테스트 (타입 변환 검증)
- AstBuilder 테스트 (Parse Tree → AST 변환)
- CodeGenerator 테스트 (코드 생성 검증)

### 2. 통합 테스트
- End-to-End 테스트 (.proto → .h/.cpp)
- Import 의존성 테스트
- 중첩 메시지 Flatten 테스트

### 3. 테스트 케이스
- 기본 타입 필드
- Repeated 필드
- Map 필드
- 중첩 메시지
- Enum
- Import
- 복잡한 의존성 그래프

## 성능 목표

- 1000개 .proto 파일 처리: < 10초
- 단일 파일 처리: < 100ms
- 메모리 사용: < 500MB (대량 파일 처리 시)

## 다음 단계

1. ANTLR4 설치 및 Proto3 문법 통합
2. 기본 프로젝트 구조 생성
3. InputHandler 구현
4. AST 모델 정의
5. TypeMapper 구현
6. CodeGenerator 템플릿 작성
7. 테스트 케이스 작성
8. End-to-End 통합

---

**작성일**: 2026-03-08  
**버전**: 1.0  
**상태**: 설계 완료, 구현 대기
