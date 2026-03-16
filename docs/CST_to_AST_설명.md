# CST → AST 변환: Parse Tree에서 Abstract Syntax Tree까지

Protobuf3.g4 파서가 생성하는 Parse Tree(CST)를 Abstract Syntax Tree(AST)로 변환하는 전 과정을 단계별로 설명합니다.

---

## 1. 파싱 파이프라인 전체 흐름

### 질문
소스 코드 한 줄이 최종 코드 생성까지 어떤 단계를 거치는가?

### 답변
파싱 파이프라인은 다음 5단계로 구성됩니다.

**입력 예시:**
```protobuf
repeated string name = 1;
```

**단계별 변환:**

| 단계 | 출력 | 담당 |
|------|------|------|
| ① 소스 텍스트 | `repeated string name = 1;` | 원본 |
| ② 토큰 스트림 | `REPEATED STRING IDENTIFIER EQ INT_LIT SEMI` | ANTLR Lexer |
| ③ Parse Tree (CST) | 문법 규칙 트리 (모든 토큰 포함) | ANTLR Parser |
| ④ AST | 의미 있는 노드만 남긴 트리 | Visitor 패턴 |
| ⑤ 코드 출력 | `UPROPERTY() TArray<FString> Name;` | Code Generator |

중간 단계인 ③→④ 변환이 이 문서의 핵심 주제입니다. ANTLR이 자동 생성하는 CST는 문법의 구조를 그대로 반영하므로 중간 노드가 많고 구두점도 포함됩니다. AST는 이를 압축해 **의미 단위**만 남깁니다.

## 2. CST란 무엇인가

### 질문
ANTLR이 생성하는 Parse Tree(CST)는 정확히 어떤 구조인가?

### 답변
CST(Concrete Syntax Tree)는 **문법 규칙 하나하나가 모두 노드**로 표현된 트리입니다.

`repeated string name = 1;` 를 파싱하면 아래 구조가 생성됩니다.

```
field
├── fieldLabel
│   └── REPEATED          ← 터미널 토큰
├── type_
│   └── STRING            ← 터미널 토큰
├── fieldName
│   └── ident
│       └── IDENTIFIER("name")
├── EQ                    ← "=" 토큰도 노드
├── fieldNumber
│   └── intLit
│       └── INT_LIT("1")
└── SEMI                  ← ";" 토큰도 노드
```

**CST의 특징:**
- `fieldName → ident → IDENTIFIER` 처럼 **의미 없는 중간 래퍼 규칙**이 체인으로 존재
- `EQ(=)`, `SEMI(;)` 같은 **구두점 토큰**이 노드로 남음
- `fieldLabel`처럼 **단일 자식만 가진 규칙 노드**가 많음
- 문법 규칙 이름(`fieldNumber`, `intLit`)이 그대로 노드 타입이 됨

**왜 이런 구조인가?**
ANTLR은 `.g4` 파일의 규칙 구조를 **기계적으로** 트리로 변환합니다. 가독성보다 정확성이 우선입니다.

## 3. AST란 무엇인가

### 질문
AST는 CST와 어떻게 다르며, 어떤 정보만 남기는가?

### 답변
AST(Abstract Syntax Tree)는 **의미 있는 정보만 추출**해 재구성한 트리입니다.

같은 `repeated string name = 1;` 의 AST:

```
FieldNode
├── label:    REPEATED
├── type:     STRING
├── name:     "name"
└── number:   1
```

**CST 대비 무엇이 제거되었나:**

| 제거 대상 | 예시 | 이유 |
|-----------|------|------|
| 구두점 토큰 | `EQ`, `SEMI` | 구조가 이미 위치로 표현됨 |
| 단순 래퍼 규칙 | `fieldName → ident → IDENTIFIER` | 중간 단계가 정보를 추가하지 않음 |
| 단일 자식 체인 | `fieldNumber → intLit → INT_LIT` | 리프 값만 있으면 충분 |

**무엇이 보존되는가:**
- 필드의 **레이블** (optional / repeated / 없음)
- 필드의 **타입** (string, int32, MyMessage 등)
- 필드의 **이름** (식별자 문자열)
- 필드의 **번호** (정수값)
- 필드 **옵션** 목록 (있는 경우)

AST 노드는 개발자가 직접 설계한 C# 클래스(`FieldNode`, `MessageNode` 등)이며, 이후 코드 생성기가 이 노드를 순회하여 C++ 코드를 출력합니다.

## 4. Visitor 패턴으로 CST 순회하기

### 질문
CST를 어떻게 순회해서 AST를 만드는가? Visitor 패턴이란?

### 답변
ANTLR은 `Protobuf3BaseVisitor<T>` 클래스를 자동 생성합니다. 이를 상속받아 각 규칙 노드에 대한 처리 메서드를 구현하면 됩니다.

**기본 구조:**
```csharp
public class AstBuilder : Protobuf3BaseVisitor<AstNode>
{
    // CST의 'field' 규칙 노드를 방문할 때 호출됨
    public override AstNode VisitField(Protobuf3Parser.FieldContext ctx)
    {
        // 1. 자식 노드에서 필요한 정보 추출
        var label  = ctx.fieldLabel()?.GetText();   // "repeated" or null
        var type   = ctx.type_().GetText();          // "string"
        var name   = ctx.fieldName().GetText();      // "name"
        var number = int.Parse(ctx.fieldNumber().GetText()); // 1

        // 2. AST 노드 생성 및 반환
        return new FieldNode(label, type, name, number);
    }

    public override AstNode VisitMessageDef(Protobuf3Parser.MessageDefContext ctx)
    {
        var msgName = ctx.messageName().GetText();

        // 재귀적으로 자식 요소들을 방문
        var elements = ctx.messageBody()
                          .messageElement()
                          .Select(e => Visit(e))
                          .Where(n => n != null)
                          .ToList();

        return new MessageNode(msgName, elements);
    }
}
```

**Visitor 패턴의 핵심 원칙:**
- 각 `Visit*` 메서드는 해당 규칙 노드 **1개**를 처리하고 AST 노드 **1개**를 반환
- 자식 노드 처리는 `Visit(child)` 재귀 호출로 위임
- **구두점 토큰은 무시** — `ctx.EQ()`, `ctx.SEMI()` 는 호출하지 않음
- 단순 래퍼는 **통과(pass-through)** — `VisitFieldName`을 별도 구현하지 않고 `GetText()`로 바로 추출

**Visitor vs Listener:**

| | Visitor | Listener |
|---|---|---|
| 반환값 | 있음 (AST 노드) | 없음 |
| 제어권 | 직접 자식 호출 | ANTLR 워크가 자동 호출 |
| 용도 | AST 구축, 값 계산 | 이벤트 처리, 통계 |
| 선택 시점 | 트리를 **변환**할 때 | 트리를 **탐색**할 때 |

## 5. 변환 과정 단계별 추적

### 질문
message 블록 전체가 CST에서 AST로 변환되는 과정을 단계별로 추적하면?

### 답변
아래 proto 코드를 예시로 전 과정을 추적합니다.

**입력:**
```protobuf
message Player {
    string name  = 1;
    int32  level = 2;
}
```

**① Lexer 출력 — 토큰 스트림:**
```
MESSAGE  IDENTIFIER("Player")  LC
  STRING  IDENTIFIER("name")   EQ  INT_LIT("1")  SEMI
  INT32   IDENTIFIER("level")  EQ  INT_LIT("2")  SEMI
RC  EOF
```

**② Parser 출력 — CST (Parse Tree):**
```
messageDef
├── MESSAGE
├── messageName
│   └── ident
│       └── IDENTIFIER("Player")
├── doMessageNameDef          ← 시맨틱 액션 규칙 (빈 노드)
└── messageBody
    ├── LC
    ├── doEnterBlock          ← 시맨틱 액션 규칙
    ├── messageElement[0]
    │   └── field
    │       ├── type_
    │       │   └── STRING
    │       ├── fieldName → ident → IDENTIFIER("name")
    │       ├── EQ
    │       ├── fieldNumber → intLit → INT_LIT("1")
    │       └── SEMI
    ├── messageElement[1]
    │   └── field
    │       ├── type_
    │       │   └── INT32
    │       ├── fieldName → ident → IDENTIFIER("level")
    │       ├── EQ
    │       ├── fieldNumber → intLit → INT_LIT("2")
    │       └── SEMI
    ├── RC
    └── doExitBlock           ← 시맨틱 액션 규칙
```

**③ Visitor 순회 — 변환 중:**

`VisitMessageDef` 호출 →
- `messageName.GetText()` = `"Player"` 추출
- `messageBody.messageElement()` 반복 →
  - `VisitField` 호출 × 2회
  - 각각 `FieldNode` 반환

**④ AST 결과:**
```
MessageNode
├── name: "Player"
└── fields: [
      FieldNode(label=null, type="string", name="name",  number=1),
      FieldNode(label=null, type="int32",  name="level", number=2)
    ]
```

CST의 29개 노드가 AST에서는 **3개**로 압축됩니다.

## 6. 타입 판별 — 시맨틱 프레디킷의 역할

### 질문
`messageType`과 `enumType`은 어떻게 구분하는가? 2-Pass 파싱이란?

### 답변
proto3에서 사용자 정의 타입(`MyMessage`, `MyEnum`)은 Lexer 단계에서 키워드와 식별자를 구분할 수 없습니다. 이를 해결하기 위해 **2-Pass 파싱**을 사용합니다.

**문제:**
```protobuf
Status status = 1;   // Status가 enum인지 message인지 모름
```

**1st Pass — 심볼 수집:**
```
Protobuf3.g4의 doMessageNameDef / doEnumNameDef 액션이 호출됨
→ _messageTypes = { "Player", "Item", ... }
→ _enumTypes    = { "Status", "Rarity", ... }
```

**2nd Pass — 타입 판별:**
```antlr
type_
    : ...
    | { this.IsNotKeyword() }? messageType   ← IsMessageType_() 검사
    | { this.IsNotKeyword() }? enumType      ← IsEnumType_() 검사
    ;
```

`IsMessageType_()` / `IsEnumType_()` 는 현재 토큰이 심볼 테이블에 있는지 검사하는 **시맨틱 프레디킷**입니다.

**AST에서의 표현:**
```
// CST
type_
└── messageType
    └── messageName → ident → IDENTIFIER("Player")

// AST
FieldNode.type = TypeRef { kind: MESSAGE, name: "Player" }
```

Visitor는 `VisitMessageType` / `VisitEnumType` 을 구분해 처리하여 AST 노드에 `kind` 정보를 부여합니다.

## 7. AST에서 Unreal Engine C++ 코드 생성

### 질문
완성된 AST를 어떻게 Unreal Engine C++ 코드로 변환하는가?

### 답변
AST 노드를 순회하는 별도 `CodeGenerator` 클래스가 각 노드 타입에 맞는 C++ 코드를 출력합니다.

**타입 매핑 테이블:**

| proto3 타입 | Unreal C++ 타입 |
|-------------|----------------|
| `string` | `FString` |
| `int32` | `int32` |
| `int64` | `int64` |
| `float` | `float` |
| `double` | `double` |
| `bool` | `bool` |
| `bytes` | `TArray<uint8>` |
| `repeated T` | `TArray<T>` |
| `map<K, V>` | `TMap<K, V>` |
| `MessageType` | `F{MessageType}` (구조체) |
| `EnumType` | `E{EnumType}` (enum class) |

**코드 생성 흐름:**
```csharp
class CodeGenerator : IAstVisitor
{
    string Visit(MessageNode msg)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"USTRUCT(BlueprintType)");
        sb.AppendLine($"struct F{msg.Name} {{");
        sb.AppendLine($"    GENERATED_BODY()");
        foreach (var field in msg.Fields)
            sb.AppendLine($"    {Visit(field)}");
        sb.AppendLine($"}};");
        return sb.ToString();
    }

    string Visit(FieldNode field)
    {
        var ueType = MapType(field.Type, field.Label);
        return $"UPROPERTY(BlueprintReadWrite) {ueType} {ToPascalCase(field.Name)};";
    }
}
```

**최종 출력 예시:**

입력:
```protobuf
message Player {
    string name  = 1;
    int32  level = 2;
    repeated string inventory = 3;
}
```

출력:
```cpp
USTRUCT(BlueprintType)
struct FPlayer {
    GENERATED_BODY()

    UPROPERTY(BlueprintReadWrite) FString Name;
    UPROPERTY(BlueprintReadWrite) int32 Level;
    UPROPERTY(BlueprintReadWrite) TArray<FString> Inventory;
};
```

**전체 파이프라인 한눈에:**
```
.proto 파일
  ↓  [ANTLR Lexer]
Token Stream
  ↓  [ANTLR Parser + 2-Pass]
CST (Parse Tree)         ← ANTLR이 자동 생성
  ↓  [AstBuilder Visitor]
AST (추상 구문 트리)      ← 개발자가 설계
  ↓  [CodeGenerator]
Unreal Engine C++ 코드   ← 최종 출력
```
