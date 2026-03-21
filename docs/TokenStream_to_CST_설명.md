# 토큰 스트림 → CST 구축: 렉싱에서 Parse Tree까지

소스 코드가 토큰으로 분해되고, 파서가 토큰을 소비하며 CST(Parse Tree)를 조립하는 전 과정을 단계별로 설명합니다.

---

## 1. 소스 텍스트에서 토큰 스트림까지 — 렉싱이란?

### 질문
.proto 소스 파일이 파서에 도달하기 전에 어떤 변환을 거치는가?

### 답변
렉싱(Lexing)은 소스 텍스트를 **토큰(Token)** 의 연속인 **토큰 스트림**으로 변환하는 과정입니다.

**파이프라인:**
```
소스 텍스트 (.proto 파일)
  ↓  AntlrInputStream — 문자열을 문자 단위로 읽을 수 있는 스트림으로 변환
  ↓  Protobuf3Lexer — 문자 패턴을 매칭하여 토큰을 생성
  ↓  CommonTokenStream — 토큰을 버퍼링하고 채널별로 필터링
  ↓  Protobuf3Parser — 토큰을 소비하며 CST 구축
```

**예시 — `message Player { string name = 1; int32 level = 2; }` 를 렉싱하면:**

```
t0:  MESSAGE      "message"     ← 키워드
t1:  IDENTIFIER   "Player"      ← 식별자
t2:  LC           "{"           ← 심볼
t3:  STRING       "string"      ← 키워드 (타입)
t4:  IDENTIFIER   "name"        ← 식별자
t5:  EQ           "="           ← 심볼
t6:  INT_LIT      "1"           ← 리터럴
t7:  SEMI         ";"           ← 심볼
t8:  INT32        "int32"       ← 키워드 (타입)
t9:  IDENTIFIER   "level"       ← 식별자
t10: EQ           "="           ← 심볼
t11: INT_LIT      "2"           ← 리터럴
t12: SEMI         ";"           ← 심볼
t13: RC           "}"           ← 심볼
     EOF                        ← 스트림 종료
```

14개의 토큰이 생성됩니다. 공백과 줄바꿈은 `skip` 처리되어 토큰 스트림에 나타나지 않습니다.

**코드에서의 흐름 (`AntlrProtoParser.Parse`):**
```csharp
var inputStream  = new AntlrInputStream(content);        // 문자 스트림
var lexer        = new Protobuf3Lexer(inputStream);       // 렉서 생성
var tokenStream  = new CommonTokenStream(lexer);          // 토큰 버퍼링
var parser       = new Protobuf3Parser(tokenStream);      // 파서 생성
```

## 2. 토큰의 분류 — 키워드·심볼·리터럴·히든 채널

### 질문
Protobuf3 렉서가 생성하는 토큰은 어떤 종류가 있으며, 파서에 보이지 않는 토큰도 있는가?

### 답변
`Protobuf3.g4`는 약 60개의 토큰 규칙을 정의하며, 5개 카테고리로 분류됩니다.

**① 키워드 (27개)** — 예약어로 식별자보다 우선 매칭

| 분류 | 토큰 |
|------|------|
| 구조 | `SYNTAX`, `IMPORT`, `WEAK`, `PUBLIC`, `PACKAGE`, `OPTION` |
| 필드 수식 | `OPTIONAL`, `REPEATED` |
| 복합 타입 | `ONEOF`, `MAP`, `ENUM`, `MESSAGE` |
| 스칼라 타입 | `INT32`, `INT64`, `UINT32`, `UINT64`, `SINT32`, `SINT64`, `FIXED32`, `FIXED64`, `SFIXED32`, `SFIXED64`, `BOOL`, `STRING`, `DOUBLE`, `FLOAT`, `BYTES` |
| 기타 | `RESERVED`, `TO`, `MAX`, `SERVICE`, `EXTEND`, `RPC`, `STREAM`, `RETURNS` |

**② 심볼 (15개)** — 구두점과 연산자

`SEMI(;)`, `EQ(=)`, `LP(()`, `RP())`, `LB([)`, `RB(])`, `LC({)`, `RC(})`, `LT(<)`, `GT(>)`, `DOT(.)`, `COMMA(,)`, `COLON(:)`, `PLUS(+)`, `MINUS(-)`

**③ 리터럴 (5개)** — 값을 나타내는 토큰

`INT_LIT`, `FLOAT_LIT`, `STR_LIT`, `BOOL_LIT`, `PROTO3_LIT`

**④ 식별자 (1개)**

`IDENTIFIER` — 키워드가 아닌 모든 이름. 렉서 규칙 순서상 키워드 규칙이 먼저 선언되므로, `message`는 `IDENTIFIER`가 아니라 `MESSAGE`로 매칭됩니다.

**⑤ 히든 채널 & 스킵 (3개)** — 파서에 보이지 않는 토큰

| 토큰 | 패턴 | 처리 | 차이점 |
|------|------|------|--------|
| `WS` | `[ \t\r\n]+` | `-> skip` | **완전 폐기** — 토큰 스트림에 존재하지 않음 |
| `LINE_COMMENT` | `// ...` | `-> channel(HIDDEN)` | 토큰 스트림에 **보존**, 파서만 무시 |
| `COMMENT` | `/* ... */` | `-> channel(HIDDEN)` | 토큰 스트림에 **보존**, 파서만 무시 |

**`skip` vs `channel(HIDDEN)` 의 실질적 차이:**
- `skip`: 토큰이 아예 생성되지 않음. 복원 불가.
- `channel(HIDDEN)`: 토큰이 생성되어 스트림에 존재하지만 파서의 기본 채널(0)에서 제외됨. 주석 추출 등에 활용 가능.

## 3. 파서의 CST 조립 원리 — 규칙 진입과 퇴장

### 질문
파서가 토큰 스트림을 소비하면서 CST(Parse Tree)를 어떻게 조립하는가?

### 답변
ANTLR 파서는 **재귀 하강(Recursive Descent)** 방식으로 동작합니다. `.g4` 문법의 각 규칙이 하나의 C# 메서드로 변환되며, 규칙 진입 시 CST 노드를 생성하고 퇴장 시 부모에 붙입니다.

**핵심 메커니즘:**

```csharp
// 자동 생성된 Protobuf3Parser.cs (간략화)
public MessageDefContext messageDef()
{
    var _localctx = new MessageDefContext(Context, State);  // ① CST 노드 생성
    EnterRule(_localctx, ...);                              // ② 스택에 push

    Match(MESSAGE);            // ③ 토큰 소비 → TerminalNode 추가
    messageName();             // ④ 하위 규칙 호출 → 자식 Context 생성
    doMessageNameDef();        // ⑤ 시맨틱 액션 (빈 규칙)
    messageBody();             // ⑥ 하위 규칙 호출

    ExitRule();                // ⑦ 스택에서 pop, 부모에 자식으로 연결
    return _localctx;
}
```

**각 단계의 역할:**

| 단계 | 동작 | CST 효과 |
|------|------|----------|
| ① `new Context()` | 규칙 이름으로 노드 객체 생성 | 빈 Context 노드 생성 |
| ② `EnterRule()` | 현재 Context를 파서 스택에 push | 파싱 컨텍스트 설정 |
| ③ `Match(TOKEN)` | 토큰 하나를 소비, `TerminalNode` 생성 | 현재 Context의 자식으로 추가 |
| ④ 하위 규칙 호출 | 재귀적으로 같은 과정 반복 | 하위 Context가 자식 트리로 생성 |
| ⑤ 시맨틱 액션 | 심볼 테이블 등록 등 부수 효과 | CST에 빈 노드 추가 (토큰 소비 없음) |
| ⑦ `ExitRule()` | 스택에서 pop | Context가 부모의 자식으로 확정 |

**결과적으로 CST는:**
- 문법 규칙마다 하나의 `Context` 노드 (내부 노드)
- 매칭된 토큰마다 하나의 `TerminalNode` (리프 노드)
- 문법 구조를 **그대로** 반영하는 트리

## 4. Context 클래스 — CST 노드의 정체

### 질문
CST의 각 노드는 어떤 타입이며, 자식 노드에 어떻게 접근하는가?

### 답변
ANTLR은 `.g4` 파일의 파서 규칙 하나당 하나의 `Context` 클래스를 자동 생성합니다. `Protobuf3.g4`에는 59개의 파서 규칙이 있으므로, 59개의 Context 클래스가 생성됩니다.

**대표적인 Context 클래스:**

| 규칙 | 클래스 | 주요 접근자 |
|------|--------|------------|
| `messageDef` | `MessageDefContext` | `.MESSAGE()`, `.messageName()`, `.messageBody()` |
| `field` | `FieldContext` | `.type_()`, `.fieldName()`, `.fieldNumber()`, `.EQ()`, `.SEMI()` |
| `type_` | `Type_Context` | `.STRING()`, `.INT32()`, `.messageType()`, `.enumType()` |
| `messageName` | `MessageNameContext` | `.ident()` |
| `ident` | `IdentContext` | `.IDENTIFIER()` |

**접근자 패턴 — 단수 vs 복수:**

```csharp
// 단수 접근자: 최대 1개 자식 (null 가능)
ctx.messageName()         // MessageNameContext? 반환
ctx.fieldLabel()          // null이면 레이블 없음 (proto3 기본)
ctx.MESSAGE()             // ITerminalNode 반환

// 복수 접근자: 0개 이상 자식 (빈 배열 가능)
ctx.messageElement()      // MessageElementContext[] 반환
ctx.field()               // FieldContext[] 반환 (messageBody 내)
```

**`TerminalNode` vs `Context`:**
- `TerminalNode`: 토큰 하나를 감싼 리프 노드. `.Symbol.Text`로 원본 텍스트, `.Symbol.Type`으로 토큰 타입 접근.
- `Context`: 규칙 하나를 나타내는 내부 노드. 타입 기반 접근자로 자식에 접근.
- `GetText()`: Context의 모든 하위 토큰 텍스트를 연결해 반환. `ctx.messageName().GetText()` → `"Player"`.

## 5. 2-Pass 파싱 — 전방 참조 해결

### 질문
proto3에서 아직 선언되지 않은 타입을 사용하면 어떻게 되는가? 파서는 이를 어떻게 해결하는가?

### 답변
proto3는 **전방 참조**를 허용합니다. 파일 뒷부분에 정의된 메시지 타입을 앞에서 사용할 수 있습니다.

**문제 상황:**
```protobuf
message Player {
    Inventory items = 1;      // Inventory가 아직 정의되지 않음!
}

message Inventory {           // 여기서 정의됨
    repeated Item entries = 1;
}
```

파서가 `Inventory`를 처음 만났을 때, 이것이 **메시지 타입인지 열거형인지 알 수 없습니다.** `type_` 규칙의 `messageType` / `enumType` 분기를 선택할 수 없습니다.

**해결: 2-Pass 파싱**

```
 ┌─────────────────────────────────────────────────┐
 │         DoRewind() 내부 동작                      │
 │                                                   │
 │  ┌─── Pass 1: 심볼 수집 ───┐                      │
 │  │ • Seek(0) — 토큰 스트림 처음으로               │
 │  │ • 별도 파서 인스턴스 생성                       │
 │  │ • _isFirstPass = true                          │
 │  │   → 모든 시맨틱 프레디킷 무조건 true            │
 │  │ • 에러 리스너 제거 (조용히)                     │
 │  │ • proto() 호출 → 전체 문법 순회                │
 │  │ • doMessageNameDef 액션 →                      │
 │  │   _messageTypes에 등록                         │
 │  │ • doEnumNameDef 액션 →                         │
 │  │   _enumTypes에 등록                            │
 │  └─────────────────────────┘                      │
 │           │ CopySymbolTableFrom()                  │
 │           ▼                                        │
 │  ┌─── Pass 2: 실제 파싱 ───┐                      │
 │  │ • Seek(0) — 토큰 스트림 다시 처음으로           │
 │  │ • _isFirstPass = false                         │
 │  │   → 시맨틱 프레디킷이 심볼 테이블 검사          │
 │  │ • proto() 호출 → CST 생성                      │
 │  └─────────────────────────┘                      │
 └─────────────────────────────────────────────────┘
```

**왜 별도 파서 인스턴스를 만드는가?**

`Reset()`을 현재 파서에 호출하면 `_ctx`가 `null`이 되어 `ExitRule()`에서 `NullReferenceException`이 발생합니다. 별도 인스턴스로 Pass 1을 수행하면 현재 파서의 상태를 보존할 수 있습니다.

**심볼 등록 메커니즘:**
```csharp
// Protobuf3.g4의 epsilon 규칙
doMessageNameDef : { this.DoMessageNameDef_(); } ;

// Protobuf3ParserBase.cs
protected void DoMessageNameDef_()
{
    // tokenStream.Lt(-1) = 직전에 매칭된 토큰 = 메시지 이름
    var name = tokenStream.Lt(-1).Text;
    var fullName = GetFullScopedName(name);  // 중첩 시 "Outer.Inner"
    _messageTypes.Add(fullName);
}
```

**교차 파일 타입 등록:**

`AntlrProtoParser`는 파일을 의존 순서대로 파싱합니다. 파일 A를 파싱한 후 `RegisterParsedFile(ast)`로 A의 타입들을 수집하고, 파일 B 파싱 전에 `RegisterExternalMessageType()` / `RegisterExternalEnumType()`으로 파서에 주입합니다.

## 6. 시맨틱 프레디킷 — type_ 규칙의 분기 판단

### 질문
파서가 `type_` 규칙에서 사용자 정의 타입의 종류(message vs enum)를 어떻게 판단하는가?

### 답변
`type_` 규칙은 스칼라 타입(string, int32 등)과 사용자 정의 타입(messageType, enumType)을 모두 포함합니다. 스칼라 타입은 고유 키워드 토큰이 있어 분기가 자명하지만, 사용자 정의 타입은 모두 `IDENTIFIER` 토큰이므로 **시맨틱 프레디킷**으로 판단합니다.

**문법 정의:**
```antlr
type_
    : DOUBLE | FLOAT | INT32 | INT64 | ... | STRING | BYTES
    | { this.IsNotKeyword() }? messageType
    | { this.IsNotKeyword() }? enumType
    ;
```

**세 가지 프레디킷:**

| 프레디킷 | 검사 내용 | 반환 조건 |
|----------|----------|----------|
| `IsNotKeyword()` | 현재 토큰이 예약어 목록(33개)에 없는지 | 키워드가 아니면 `true` |
| `IsMessageType_()` | 현재 토큰이 `_messageTypes` 집합에 있는지 | Pass 1에서는 항상 `true`, Pass 2에서 실제 검사 |
| `IsEnumType_()` | 현재 토큰이 `_enumTypes` 집합에 있는지 | Pass 1에서는 항상 `true`, Pass 2에서 실제 검사 |

**분기 판단 흐름:**
```
현재 토큰이 "Status"일 때:
  1. IsNotKeyword() → true (예약어 아님)
  2. IsMessageType_() → _messageTypes에 "Status" 있는가?
     - 있으면 → messageType 분기 선택
     - 없으면 → 다음 대안(enumType) 시도
  3. IsEnumType_() → _enumTypes에 "Status" 있는가?
     - 있으면 → enumType 분기 선택
     - 없으면 → 파싱 에러
```

**Pass 1에서는 왜 항상 true인가?**

Pass 1의 목적은 심볼 수집이지 정확한 트리 생성이 아닙니다. 모든 분기를 통과시켜 문법 전체를 순회해야 `doMessageNameDef` / `doEnumNameDef` 액션이 실행되어 심볼 테이블이 채워집니다.

```csharp
// Protobuf3ParserBase.cs
protected bool IsMessageType_()
{
    if (_isFirstPass) return true;  // Pass 1: 무조건 통과
    var name = CurrentToken?.Text;
    return name != null && _messageTypes.Contains(name);
}
```

## 7. 단계별 추적 — 14개 토큰이 CST가 되기까지

### 질문
`message Player { string name = 1; int32 level = 2; }` 의 14개 토큰이 CST로 조립되는 과정을 단계별로 추적하면?

### 답변
파서가 토큰을 하나씩(때로는 동시에) 소비하면서 CST 노드를 생성하는 13단계(0~12)를 추적합니다.

**입력 토큰 스트림:**
```
t0:MESSAGE  t1:ID("Player")  t2:LC  t3:STRING  t4:ID("name")  t5:EQ
t6:INT_LIT("1")  t7:SEMI  t8:INT32  t9:ID("level")  t10:EQ
t11:INT_LIT("2")  t12:SEMI  t13:RC
```

**완성될 CST (19개 노드):**
```
n0:messageDef
├── n1:MESSAGE
├── n2:messageName
│   └── n3:ID("Player")
├── n4:messageBody
│   ├── n5:LC
│   ├── n6:field[0]
│   │   ├── n7:type_:STRING
│   │   ├── n8:fieldName:ID("name")
│   │   ├── n9:EQ
│   │   ├── n10:fieldNumber:INT_LIT("1")
│   │   └── n11:SEMI
│   ├── n12:field[1]
│   │   ├── n13:type_:INT32
│   │   ├── n14:fieldName:ID("level")
│   │   ├── n15:EQ
│   │   ├── n16:fieldNumber:INT_LIT("2")
│   │   └── n17:SEMI
│   └── n18:RC
```

**단계별 진행:**

| 단계 | 소비 토큰 | 생성 CST 노드 | 파서 동작 |
|------|----------|---------------|----------|
| 0 | — | — | 초기 상태 — 파서 시작 전 |
| 1 | — | n0:messageDef | `messageDef()` 진입 — Context 노드 생성 |
| 2 | t0:MESSAGE | n1:MESSAGE | `Match(MESSAGE)` — 키워드 소비, TerminalNode 추가 |
| 3 | t1:ID("Player") | n2:messageName, n3:ID | `messageName()` → `ident()` → `Match(IDENTIFIER)` |
| 4 | t2:LC | n4:messageBody, n5:LC | `messageBody()` 진입, `Match(LC)` |
| 5 | t3:STRING | n6:field[0], n7:type_:STRING | `field()` 진입, `type_()` → `Match(STRING)` |
| 6 | t4:ID, t5:EQ | n8:fieldName, n9:EQ | `fieldName()` → `Match(ID)`, `Match(EQ)` |
| 7 | t6:INT_LIT("1") | n10:fieldNumber:INT_LIT | `fieldNumber()` → `Match(INT_LIT)` |
| 8 | t7:SEMI | n11:SEMI | `Match(SEMI)` — field[0] 완성, `ExitRule()` |
| 9 | t8:INT32 | n12:field[1], n13:type_:INT32 | 다음 `field()` 진입, `type_()` → `Match(INT32)` |
| 10 | t9:ID, t10:EQ | n14:fieldName, n15:EQ | `fieldName()` → `Match(ID)`, `Match(EQ)` |
| 11 | t11:INT_LIT("2") | n16:fieldNumber:INT_LIT | `fieldNumber()` → `Match(INT_LIT)` |
| 12 | t12:SEMI, t13:RC | n17:SEMI, n18:RC | `Match(SEMI)` — field[1] 완성, `Match(RC)` — messageBody 완성 |

아래 인터랙티브 스테퍼로 각 단계를 직접 진행하며 왼쪽의 토큰 소비와 오른쪽의 CST 성장을 관찰할 수 있습니다.
