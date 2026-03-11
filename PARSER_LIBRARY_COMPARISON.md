# C#/.NET 파서 라이브러리 비교 분석

Protocol Buffer IDL 파싱을 위한 .NET 파서 생성 도구 비교

---

## 목차

1. [Protocol Buffer IDL 개요](#1-protocol-buffer-idl-개요)
2. [라이브러리 상세 비교](#2-라이브러리-상세-비교)
3. [성능 및 복잡도 비교표](#3-성능-및-복잡도-비교표)
4. [추천 라이브러리](#4-추천-라이브러리)
5. [구현 예시](#5-구현-예시)

---

## 1. Protocol Buffer IDL 개요

### 1.1 Proto3 주요 문법 요소

```protobuf
syntax = "proto3";

// Package 선언
package example;

// Import 문
import "google/protobuf/timestamp.proto";

// Message 정의
message Person {
  string name = 1;                    // Scalar 타입
  int32 age = 2;
  repeated string emails = 3;         // Repeated 필드
  map<string, string> metadata = 4;   // Map 타입
  
  // Nested message
  message Address {
    string street = 1;
    string city = 2;
  }
  
  Address address = 5;
  Status status = 6;                  // Enum 타입
  
  // Oneof
  oneof test_oneof {
    string name_alt = 7;
    int32 id = 8;
  }
}

// Enum 정의
enum Status {
  STATUS_UNSPECIFIED = 0;
  STATUS_ACTIVE = 1;
  STATUS_INACTIVE = 2;
}

// Service 정의 (gRPC)
service UserService {
  rpc GetUser (GetUserRequest) returns (User);
  rpc ListUsers (ListUsersRequest) returns (stream User);
}
```

### 1.2 파싱 요구사항

**필수 요소:**
- Syntax 선언 (`proto2`, `proto3`, `edition`)
- Message 정의 (중첩 가능)
- Field 타입 (scalar, message, enum)
- Field 번호와 카디널리티 (`optional`, `required`, `repeated`)
- Enum 정의
- Package 선언
- Import 문
- 주석 처리 (C++/Java 스타일)

**선택 요소:**
- Service 정의 (gRPC)
- Options 정의
- Oneof 구문
- Map 타입
- Reserved 필드

---

## 2. 라이브러리 상세 비교

### 2.1 ANTLR4

#### 기본 정보
- **NuGet 패키지:** `Antlr4.Runtime.Standard`
- **최신 버전:** 4.13.1 (2023년 9월)
- **.NET 8.0 호환성:** ✅ (.NET Standard 2.0 지원)
- **라이선스:** BSD-3-Clause
- **GitHub Stars:** 18.8k
- **활발한 유지보수:** ✅ (활발함)

#### 장점
1. **산업 표준:** 가장 널리 사용되는 파서 생성기
2. **강력한 도구:** ANTLRWorks, 비주얼 디버거 제공
3. **풍부한 문법 저장소:** [grammars-v4](https://github.com/antlr/grammars-v4)에 Protocol Buffer 문법 존재
4. **다중 타겟 언어:** C++, C#, Java, Python, JavaScript 등 10개 언어 지원
5. **우수한 에러 처리:** 정교한 에러 리포팅 및 복구 메커니즘
6. **IDE 통합:** Visual Studio 확장 프로그램 사용 가능

#### 단점
1. **별도 빌드 단계 필요:** `.g4` 문법 파일을 C# 코드로 컴파일해야 함
2. **무거운 의존성:** 런타임 라이브러리 크기가 큼 (~344KB)
3. **학습 곡선:** 문법 작성 및 AST 처리에 시간 필요
4. **컴파일 타임 의존성:** protoc처럼 별도 컴파일러 필요

#### Protocol Buffer 파싱 적합성: ⭐⭐⭐⭐⭐

**매우 적합.** Protocol Buffer 문법이 이미 존재하며, 복잡한 중첩 구조와 다양한 타입을 처리하는데 최적화되어 있음.

#### 사용 예시

```csharp
using Antlr4.Runtime;

// 1. 렉서와 파서 생성
var inputStream = CharStreams.fromString(protoContent);
var lexer = new Proto3Lexer(inputStream);
var tokenStream = new CommonTokenStream(lexer);
var parser = new Proto3Parser(tokenStream);

// 2. 파스 트리 생성
var tree = parser.proto();

// 3. Visitor 또는 Listener 패턴으로 AST 순회
var visitor = new ProtoVisitor();
visitor.Visit(tree);
```

---

### 2.2 Irony

#### 기본 정보
- **NuGet 패키지:** `Irony`
- **최신 버전:** 1.5.3 (2024년 8월)
- **.NET 8.0 호환성:** ✅ (.NET Standard 2.0 지원)
- **라이선스:** MIT
- **GitHub Stars:** 554
- **활발한 유지보수:** ⚠️ (보통 - 연 1-2회 업데이트)

#### 장점
1. **순수 C# 구현:** 별도 빌드 도구 불필요
2. **코드 내 문법 정의:** C# 코드로 직접 문법 작성
3. **직관적 API:** Fluent API 스타일로 읽기 쉬움
4. **LALR 파서:** 성능이 우수한 LR 파서
5. **작은 의존성:** 런타임 크기 작음 (~82KB)
6. **Grammar Explorer:** 내장 디버깅 도구 제공

#### 단점
1. **문법 작성이 코드 중심:** 별도 문법 파일이 아닌 C# 코드로 작성
2. **제한적 커뮤니티:** ANTLR에 비해 사용자 커뮤니티 작음
3. **문서 부족:** 공식 문서가 제한적
4. **에러 처리:** ANTLR에 비해 에러 리포팅 기능 약함

#### Protocol Buffer 파싱 적합성: ⭐⭐⭐⭐

**적합.** LALR 파서로 Protocol Buffer의 구조화된 문법을 효과적으로 처리 가능. 그러나 기존 문법 구현이 없어 처음부터 작성해야 함.

#### 사용 예시

```csharp
using Irony.Parsing;

// 1. 문법 정의
public class ProtoGrammar : Grammar
{
    public ProtoGrammar() : base(caseSensitive: true)
    {
        // Terminals
        var number = new NumberLiteral("number");
        var identifier = new IdentifierTerminal("identifier");
        var stringLit = new StringLiteral("string", "\"");
        
        // Non-terminals
        var proto = new NonTerminal("proto");
        var message = new NonTerminal("message");
        var field = new NonTerminal("field");
        
        // Rules
        proto.Rule = "syntax" + "=" + stringLit + ";" + message;
        message.Rule = "message" + identifier + "{" + field + "}";
        field.Rule = identifier + identifier + "=" + number + ";";
        
        Root = proto;
    }
}

// 2. 파싱
var grammar = new ProtoGrammar();
var parser = new Parser(grammar);
var parseTree = parser.Parse(protoContent);
```

---

### 2.3 Sprache

#### 기본 정보
- **NuGet 패키지:** `Sprache`
- **최신 버전:** 2.3.1 (2020년 9월)
- **.NET 8.0 호환성:** ✅ (.NET Standard 1.0 지원)
- **라이선스:** MIT
- **GitHub Stars:** 1.1k (추정)
- **활발한 유지보수:** ❌ (2020년 이후 업데이트 없음)

#### 장점
1. **경량:** 매우 작은 라이브러리 크기 (~227KB)
2. **파서 콤비네이터:** 함수형 프로그래밍 스타일
3. **순수 C#:** 외부 도구 불필요
4. **LINQ 친화적:** 쿼리 구문 사용 가능
5. **간단한 문법:** 작은 DSL에 적합

#### 단점
1. **성능:** 재귀 하강 파서로 큰 파일에 느림
2. **유지보수 중단:** 최근 4년간 업데이트 없음
3. **복잡한 문법에 부적합:** Protocol Buffer처럼 중첩이 많은 문법에 비효율적
4. **에러 메시지:** 불명확한 에러 리포팅

#### Protocol Buffer 파싱 적합성: ⭐⭐

**부적합.** 간단한 DSL용이며, Protocol Buffer의 복잡한 중첩 구조 처리에 성능 문제 발생 가능. 유지보수도 중단됨.

#### 사용 예시

```csharp
using Sprache;

// 파서 정의
Parser<string> Identifier =
    from first in Parse.Letter
    from rest in Parse.LetterOrDigit.Many()
    select new string(first.Concat(rest).ToArray());

Parser<int> FieldNumber =
    from num in Parse.Number
    select int.Parse(num);

Parser<Field> Field =
    from type in Identifier
    from name in Identifier
    from eq in Parse.Char('=')
    from num in FieldNumber
    from semi in Parse.Char(';')
    select new Field(type, name, num);

// 파싱
var result = Field.Parse("string name = 1;");
```

---

### 2.4 Superpower

#### 기본 정보
- **NuGet 패키지:** `Superpower`
- **최신 버전:** 3.1.0 (2025년 6월)
- **.NET 8.0 호환성:** ✅ (직접 타겟팅)
- **라이선스:** Apache-2.0
- **GitHub Stars:** 1.3k
- **활발한 유지보수:** ✅ (매우 활발)

#### 장점
1. **Sprache 개선판:** Sprache의 성능과 에러 처리 개선
2. **토크나이저 기반:** 렉싱 단계 분리로 성능 향상
3. **우수한 에러 메시지:** 명확한 위치 정보 제공
4. **경량:** 상대적으로 작은 크기 (~173KB)
5. **현대적 C#:** 최신 .NET 기능 활용

#### 단점
1. **문서 부족:** 공식 문서가 제한적
2. **커뮤니티 규모:** ANTLR보다 작은 커뮤니티
3. **학습 곡선:** 토크나이저 개념 이해 필요

#### Protocol Buffer 파싱 적합성: ⭐⭐⭐⭐

**적합.** 토크나이저 기반으로 Protocol Buffer의 복잡한 구조를 효율적으로 처리 가능. 성능도 우수.

#### 사용 예시

```csharp
using Superpower;
using Superpower.Parsers;

// 토크나이저 정의
enum ProtoToken
{
    Identifier,
    Number,
    String,
    Message,
    // ...
}

var tokenizer = new TokenizerBuilder<ProtoToken>()
    .Match(Identifier.CStyle, ProtoToken.Identifier)
    .Match(Numerics.Integer, ProtoToken.Number)
    .Match(QuotedString.CStyle, ProtoToken.String)
    .Match(Span.EqualTo("message"), ProtoToken.Message)
    .Build();

// 파서 정의
var messageParser =
    from msg in Token.EqualTo(ProtoToken.Message)
    from name in Token.EqualTo(ProtoToken.Identifier)
    from body in Parse.Ref(() => messageBody)
    select new Message(name, body);

// 파싱
var tokens = tokenizer.Tokenize(protoContent);
var result = messageParser.Parse(tokens);
```

---

### 2.5 Pidgin

#### 기본 정보
- **NuGet 패키지:** `Pidgin`
- **최신 버전:** 3.5.1 (2025년 10월)
- **.NET 8.0 호환성:** ✅ (직접 타겟팅)
- **라이선스:** MIT
- **GitHub Stars:** 1.1k
- **활발한 유지보수:** ✅ (매우 활발)

#### 장점
1. **최고 성능:** 파서 콤비네이터 중 가장 빠름
2. **메모리 효율:** 최소 GC 압력
3. **타입 안전:** 강력한 타입 시스템
4. **우수한 문서:** 풍부한 예제와 튜토리얼
5. **연산자 우선순위:** 표현식 파싱 도구 내장

#### 단점
1. **학습 곡선:** 고급 API 이해 필요
2. **커뮤니티 규모:** 중간 수준
3. **벤치마크 중심:** 복잡한 문법 예시 부족

#### Protocol Buffer 파싱 적합성: ⭐⭐⭐⭐

**적합.** 높은 성능과 타입 안전성으로 Protocol Buffer 파싱에 적합. 특히 대용량 파일 처리에 유리.

#### 사용 예시

```csharp
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

// 파서 정의
var identifier = Letter.Then(LetterOrDigit.ManyString(), (h, t) => h + t);

var fieldNumber = Digit.AtLeastOnceString().Select(int.Parse);

var field =
    from type in identifier
    from _ in Whitespace
    from name in identifier
    from __ in Char('=')
    from num in fieldNumber
    from ___ in Char(';')
    select new Field(type, name, num);

// 파싱
var result = field.ParseOrThrow("string name = 1;");
```

---

### 2.6 GOLD Parser

#### 기본 정보
- **NuGet 패키지:** 없음 (수동 설치)
- **최신 버전:** 5.2 (2014년)
- **.NET 8.0 호환성:** ⚠️ (레거시 .NET Framework)
- **라이선스:** Modified zlib/libpng
- **활발한 유지보수:** ❌ (거의 중단)

#### 장점
1. **GUI 도구:** GOLD Parser Builder로 문법 시각화
2. **LALR 파서:** 효율적인 파싱
3. **BNF 문법:** 표준 문법 표기법 사용

#### 단점
1. **사실상 중단:** 2014년 이후 업데이트 없음
2. **.NET Framework 전용:** 최신 .NET Core/5+ 미지원
3. **커뮤니티 소멸:** 거의 사용되지 않음
4. **도구 낡음:** Windows Forms 기반 도구

#### Protocol Buffer 파싱 적합성: ⭐

**부적합.** 유지보수 중단 및 최신 .NET 미지원으로 권장하지 않음.

---

### 2.7 GPLex/GPPG

#### 기본 정보
- **NuGet 패키지:** 없음
- **최신 버전:** 1.3 (2012년)
- **.NET 8.0 호환성:** ❌ (레거시만 지원)
- **라이선스:** BSD
- **활발한 유지보수:** ❌ (중단)

#### 장점
1. **Lex/Yacc 스타일:** 전통적 Unix 도구와 유사
2. **성숙한 알고리즘:** 검증된 LALR 구현

#### 단점
1. **프로젝트 중단:** 10년 이상 업데이트 없음
2. **빌드 도구 문제:** 최신 MSBuild와 호환 문제
3. **문서 부족:** 거의 없음
4. **커뮤니티 없음:** 사용자 거의 없음

#### Protocol Buffer 파싱 적합성: ⭐

**부적합.** 완전히 사장된 프로젝트. 사용 권장하지 않음.

---

### 2.8 수동 파싱 (Recursive Descent Parser)

#### 기본 정보
- **의존성:** 없음
- **복잡도:** 중간~높음
- **.NET 8.0 호환성:** ✅ (순수 C#)
- **활발한 유지보수:** N/A (직접 작성)

#### 장점
1. **완전한 제어:** 모든 세부사항 통제 가능
2. **의존성 없음:** 외부 라이브러리 불필요
3. **최적화 가능:** 특정 사용 사례에 최적화
4. **디버깅 용이:** 코드 흐름 완전히 이해
5. **크기:** 가장 작은 바이너리 크기

#### 단점
1. **개발 시간:** 처음부터 모든 것을 구현해야 함
2. **버그 위험:** 테스트와 검증에 많은 시간 필요
3. **유지보수:** 문법 변경 시 많은 수정 필요
4. **에러 처리:** 정교한 에러 리포팅 구현 어려움

#### Protocol Buffer 파�ing 적합성: ⭐⭐⭐

**가능하지만 권장하지 않음.** Protocol Buffer 문법은 비교적 단순하지만, 수동으로 작성하면 시간이 많이 소요됨. 라이브러리 사용 권장.

#### 사용 예시

```csharp
public class ProtoParser
{
    private readonly Tokenizer _tokenizer;
    private Token _currentToken;
    
    public ProtoFile ParseProto(string content)
    {
        _tokenizer = new Tokenizer(content);
        _currentToken = _tokenizer.NextToken();
        
        var protoFile = new ProtoFile();
        
        // syntax "proto3";
        Expect(TokenType.Syntax);
        Expect(TokenType.String);
        Expect(TokenType.Semicolon);
        
        // message definitions
        while (_currentToken.Type != TokenType.EOF)
        {
            if (_currentToken.Type == TokenType.Message)
            {
                protoFile.Messages.Add(ParseMessage());
            }
            else if (_currentToken.Type == TokenType.Enum)
            {
                protoFile.Enums.Add(ParseEnum());
            }
            else
            {
                throw new ParseException($"Unexpected token: {_currentToken}");
            }
        }
        
        return protoFile;
    }
    
    private Message ParseMessage()
    {
        Expect(TokenType.Message);
        var name = Expect(TokenType.Identifier).Value;
        Expect(TokenType.LeftBrace);
        
        var message = new Message(name);
        
        while (_currentToken.Type != TokenType.RightBrace)
        {
            message.Fields.Add(ParseField());
        }
        
        Expect(TokenType.RightBrace);
        return message;
    }
    
    private Field ParseField()
    {
        // field_type field_name = field_number;
        var type = Expect(TokenType.Identifier).Value;
        var name = Expect(TokenType.Identifier).Value;
        Expect(TokenType.Equals);
        var number = int.Parse(Expect(TokenType.Number).Value);
        Expect(TokenType.Semicolon);
        
        return new Field(type, name, number);
    }
    
    private Token Expect(TokenType type)
    {
        if (_currentToken.Type != type)
        {
            throw new ParseException($"Expected {type}, got {_currentToken.Type}");
        }
        
        var token = _currentToken;
        _currentToken = _tokenizer.NextToken();
        return token;
    }
}
```

---

## 3. 성능 및 복잡도 비교표

| 라이브러리 | 학습 곡선 | 파싱 성능 | 메모리 효율 | 에러 처리 | 문서 품질 | Proto 적합성 | .NET 8 지원 | 유지보수 |
|-----------|---------|----------|-----------|----------|----------|------------|-----------|---------|
| **ANTLR4** | 중간 | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ | ✅ 활발 |
| **Irony** | 낮음 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ⚠️ 보통 |
| **Sprache** | 낮음 | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ✅ | ❌ 중단 |
| **Superpower** | 중간 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ✅ 활발 |
| **Pidgin** | 중간 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ✅ 활발 |
| **GOLD Parser** | 중간 | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐ | ❌ | ❌ 중단 |
| **GPLex/GPPG** | 높음 | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐ | ⭐ | ❌ | ❌ 중단 |
| **수동 파싱** | 높음 | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐ ~ ⭐⭐⭐⭐⭐ | N/A | ⭐⭐⭐ | ✅ | N/A |

### 성능 벤치마크 (추정값)

| 라이브러리 | 100KB Proto 파일 | 1MB Proto 파일 | 메모리 할당 |
|-----------|----------------|---------------|-----------|
| **ANTLR4** | ~50ms | ~500ms | 중간 |
| **Irony** | ~40ms | ~400ms | 낮음 |
| **Sprache** | ~100ms | ~1000ms | 높음 |
| **Superpower** | ~30ms | ~300ms | 낮음 |
| **Pidgin** | ~20ms | ~200ms | 매우 낮음 |
| **수동 파싱** | ~15ms | ~150ms | 매우 낮음 |

---

## 4. 추천 라이브러리

### 🥇 1위: **ANTLR4**

**추천 이유:**
- ✅ **Protocol Buffer 문법이 이미 존재** (grammars-v4 저장소)
- ✅ **산업 표준**으로 검증된 안정성
- ✅ **강력한 도구**와 IDE 통합
- ✅ **우수한 에러 처리**
- ✅ **활발한 커뮤니티**와 지속적 업데이트
- ✅ **다중 언어 지원** (향후 타겟 언어 확장 가능)

**사용 시나리오:**
- 기업용 제품급 파서 필요
- 상세한 에러 메시지 필요
- Protocol Buffer 문법 전체 지원 필요
- 향후 gRPC 서비스 정의까지 확장 계획

**설치:**
```bash
dotnet add package Antlr4.Runtime.Standard
# 별도로 ANTLR4 도구 설치 필요
```

**시작 가이드:**
1. [grammars-v4/protobuf3](https://github.com/antlr/grammars-v4/tree/master/protobuf3)에서 문법 다운로드
2. ANTLR4 도구로 C# 코드 생성
3. Visitor 또는 Listener 패턴으로 AST 처리

---

### 🥈 2위: **Pidgin**

**추천 이유:**
- ✅ **최고 성능**과 메모리 효율
- ✅ **순수 C#** - 별도 빌드 도구 불필요
- ✅ **타입 안전**한 파서 구현
- ✅ **활발한 유지보수**
- ✅ **작은 의존성**

**사용 시나리오:**
- 성능이 최우선 (대용량 파일 처리)
- 간단한 빌드 프로세스 선호
- C# 코드 내에서 파서 정의 선호
- Protocol Buffer의 서브셋만 파싱

**설치:**
```bash
dotnet add package Pidgin
```

**시작 가이드:**
1. 파서 콤비네이터 패턴 학습
2. 점진적으로 문법 구현
3. 단위 테스트로 검증

---

### 🥉 3위: **Superpower**

**추천 이유:**
- ✅ **균형잡힌 성능**과 사용성
- ✅ **우수한 에러 메시지**
- ✅ **토크나이저 기반**으로 명확한 구조
- ✅ **활발한 유지보수**
- ✅ **.NET 8 네이티브 지원**

**사용 시나리오:**
- Pidgin보다 쉬운 API 선호
- 토크나이저와 파서 분리 원함
- 명확한 에러 메시지 필요
- 중간 복잡도 프로젝트

**설치:**
```bash
dotnet add package Superpower
```

**시작 가이드:**
1. 토크나이저 정의로 시작
2. 파서 정의 작성
3. 테스트 주도 개발

---

## 5. 구현 예시

### 5.1 ANTLR4 기반 Protocol Buffer 파서

#### 단계 1: ANTLR4 설치

```bash
# NuGet 패키지 설치
dotnet add package Antlr4.Runtime.Standard

# ANTLR4 도구 설치 (Java 필요)
# https://www.antlr.org/download.html
```

#### 단계 2: Protocol Buffer 문법 다운로드

```bash
# grammars-v4 저장소에서 protobuf3 문법 다운로드
git clone https://github.com/antlr/grammars-v4.git
cd grammars-v4/protobuf3
```

#### 단계 3: C# 코드 생성

```bash
# .g4 파일에서 C# 코드 생성
java -jar antlr-4.13.1-complete.jar -Dlanguage=CSharp Protobuf3.g4
```

#### 단계 4: Visitor 구현

```csharp
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ProtoBufferParser.Parsers
{
    public class ProtoFile
    {
        public string Syntax { get; set; } = "proto3";
        public string? Package { get; set; }
        public List<string> Imports { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
        public List<Enum> Enums { get; set; } = new();
    }

    public class Message
    {
        public string Name { get; set; } = string.Empty;
        public List<Field> Fields { get; set; } = new();
        public List<Message> NestedMessages { get; set; } = new();
    }

    public class Field
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public bool IsRepeated { get; set; }
        public bool IsOptional { get; set; }
    }

    public class Enum
    {
        public string Name { get; set; } = string.Empty;
        public List<EnumValue> Values { get; set; } = new();
    }

    public class EnumValue
    {
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    public class ProtoVisitor : Protobuf3BaseVisitor<object>
    {
        private readonly ProtoFile _protoFile = new();

        public ProtoFile GetProtoFile() => _protoFile;

        public override object VisitProto(Protobuf3Parser.ProtoContext context)
        {
            // Visit syntax
            var syntaxContext = context.syntax();
            if (syntaxContext != null)
            {
                _protoFile.Syntax = syntaxContext.GetText()
                    .Replace("syntax=", "")
                    .Replace("\"", "")
                    .Replace(";", "")
                    .Trim();
            }

            // Visit package
            var packageContext = context.packageStatement();
            if (packageContext != null)
            {
                _protoFile.Package = packageContext.fullIdent().GetText();
            }

            // Visit imports
            foreach (var importContext in context.importStatement())
            {
                var importPath = importContext.strLit().GetText()
                    .Replace("\"", "");
                _protoFile.Imports.Add(importPath);
            }

            // Visit top-level definitions
            foreach (var topLevelDef in context.topLevelDef())
            {
                if (topLevelDef.messageDef() != null)
                {
                    _protoFile.Messages.Add(VisitMessageDef(topLevelDef.messageDef()));
                }
                else if (topLevelDef.enumDef() != null)
                {
                    _protoFile.Enums.Add(VisitEnumDef(topLevelDef.enumDef()));
                }
            }

            return _protoFile;
        }

        private Message VisitMessageDef(Protobuf3Parser.MessageDefContext context)
        {
            var message = new Message
            {
                Name = context.messageName().GetText()
            };

            foreach (var element in context.messageBody().messageElement())
            {
                if (element.field() != null)
                {
                    message.Fields.Add(VisitField(element.field()));
                }
                else if (element.messageDef() != null)
                {
                    message.NestedMessages.Add(VisitMessageDef(element.messageDef()));
                }
            }

            return message;
        }

        private Field VisitField(Protobuf3Parser.FieldContext context)
        {
            var field = new Field
            {
                Type = context.type_().GetText(),
                Name = context.fieldName().GetText(),
                Number = int.Parse(context.fieldNumber().GetText())
            };

            // Check for repeated/optional
            if (context.GetText().StartsWith("repeated"))
            {
                field.IsRepeated = true;
            }
            else if (context.GetText().StartsWith("optional"))
            {
                field.IsOptional = true;
            }

            return field;
        }

        private Enum VisitEnumDef(Protobuf3Parser.EnumDefContext context)
        {
            var enumDef = new Enum
            {
                Name = context.enumName().GetText()
            };

            foreach (var element in context.enumBody().enumElement())
            {
                if (element.enumField() != null)
                {
                    var enumValue = new EnumValue
                    {
                        Name = element.enumField().ident().GetText(),
                        Number = int.Parse(element.enumField().intLit().GetText())
                    };
                    enumDef.Values.Add(enumValue);
                }
            }

            return enumDef;
        }
    }

    public class ProtoParser
    {
        public static ProtoFile Parse(string protoContent)
        {
            ArgumentNullException.ThrowIfNull(protoContent);

            // 1. 입력 스트림 생성
            var inputStream = CharStreams.fromString(protoContent);

            // 2. 렉서 생성
            var lexer = new Protobuf3Lexer(inputStream);

            // 3. 토큰 스트림 생성
            var tokenStream = new CommonTokenStream(lexer);

            // 4. 파서 생성
            var parser = new Protobuf3Parser(tokenStream);

            // 5. 파스 트리 생성
            var tree = parser.proto();

            // 6. Visitor로 AST 순회
            var visitor = new ProtoVisitor();
            visitor.Visit(tree);

            return visitor.GetProtoFile();
        }
    }
}
```

#### 단계 5: 사용 예시

```csharp
using ProtoBufferParser.Parsers;

var protoContent = @"
syntax = ""proto3"";

package example;

import ""google/protobuf/timestamp.proto"";

message Person {
  string name = 1;
  int32 age = 2;
  repeated string emails = 3;
  
  enum PhoneType {
    MOBILE = 0;
    HOME = 1;
    WORK = 2;
  }
  
  message PhoneNumber {
    string number = 1;
    PhoneType type = 2;
  }
  
  repeated PhoneNumber phones = 4;
}
";

try
{
    var protoFile = ProtoParser.Parse(protoContent);
    
    Console.WriteLine($"Syntax: {protoFile.Syntax}");
    Console.WriteLine($"Package: {protoFile.Package}");
    Console.WriteLine($"Messages: {protoFile.Messages.Count}");
    
    foreach (var message in protoFile.Messages)
    {
        Console.WriteLine($"\nMessage: {message.Name}");
        foreach (var field in message.Fields)
        {
            var modifier = field.IsRepeated ? "repeated " : "";
            Console.WriteLine($"  {modifier}{field.Type} {field.Name} = {field.Number}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Parse error: {ex.Message}");
}
```

---

### 5.2 Pidgin 기반 간단한 파서

```csharp
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace ProtoBufferParser.Parsers
{
    public static class PidginProtoParser
    {
        // 기본 파서
        private static readonly Parser<char, Unit> Whitespaces =
            Whitespace.SkipMany();

        private static readonly Parser<char, string> Identifier =
            Token(c => char.IsLetter(c) || c == '_')
                .Then(Token(c => char.IsLetterOrDigit(c) || c == '_').ManyString(),
                    (first, rest) => first + rest);

        private static readonly Parser<char, int> Number =
            Digit.AtLeastOnceString()
                .Select(int.Parse)
                .Before(Whitespaces);

        private static readonly Parser<char, string> StringLiteral =
            Char('"')
                .Then(Token(c => c != '"').ManyString())
                .Before(Char('"'))
                .Before(Whitespaces);

        // Syntax 파서
        private static readonly Parser<char, string> Syntax =
            String("syntax")
                .Before(Whitespaces)
                .Before(Char('='))
                .Before(Whitespaces)
                .Then(StringLiteral)
                .Before(Char(';'))
                .Before(Whitespaces);

        // Package 파서
        private static readonly Parser<char, string> Package =
            String("package")
                .Before(Whitespaces)
                .Then(Identifier)
                .Before(Char(';'))
                .Before(Whitespaces);

        // Field 파서
        private static readonly Parser<char, Field> Field =
            from type in Identifier.Before(Whitespaces)
            from name in Identifier.Before(Whitespaces)
            from _ in Char('=').Before(Whitespaces)
            from number in Number
            from __ in Char(';').Before(Whitespaces)
            select new Field(type, name, number);

        // Message 파서
        private static readonly Parser<char, Message> Message =
            from _ in String("message").Before(Whitespaces)
            from name in Identifier.Before(Whitespaces)
            from __ in Char('{').Before(Whitespaces)
            from fields in Field.Many()
            from ___ in Char('}').Before(Whitespaces)
            select new Message(name, fields.ToList());

        // Proto 파일 파서
        public static readonly Parser<char, ProtoFile> Proto =
            from syntax in Syntax.Optional()
            from package in Package.Optional()
            from messages in Message.Many()
            select new ProtoFile
            {
                Syntax = syntax.HasValue ? syntax.Value : "proto3",
                Package = package.HasValue ? package.Value : null,
                Messages = messages.ToList()
            };

        public static ProtoFile Parse(string input)
        {
            return Proto.ParseOrThrow(input);
        }
    }
}
```

---

## 6. 결론 및 권장사항

### 프로젝트 규모별 권장 라이브러리

#### 소규모 프로젝트 (개인/학습용)
- **추천:** Pidgin 또는 Superpower
- **이유:** 빠른 시작, 간단한 설정, 순수 C#

#### 중규모 프로젝트 (팀/상업용)
- **추천:** ANTLR4
- **이유:** 검증된 안정성, 풍부한 도구, 커뮤니티 지원

#### 대규모 프로젝트 (엔터프라이즈)
- **추천:** ANTLR4
- **이유:** 프로덕션 레벨 에러 처리, 유지보수성, 확장성

### 최종 권장사항

**ProtoBufferParser 프로젝트를 위한 최적의 선택:**

🏆 **ANTLR4를 1순위로 권장합니다.**

**이유:**
1. Protocol Buffer 문법이 이미 검증되어 존재함
2. Unreal Engine C++ 코드 생성까지 확장 가능
3. 명확한 에러 메시지로 디버깅 용이
4. 팀 프로젝트에 적합한 문서화

**대안:**
- 성능이 최우선이고 간단한 프로토 파일만 처리한다면 **Pidgin**
- 빠른 프로토타입이 필요하다면 **Superpower**

### 다음 단계

1. ✅ ANTLR4 설치 및 환경 설정
2. ✅ Protocol Buffer 문법 파일 다운로드
3. ✅ 기본 파서 구현
4. ✅ Unreal Engine C++ 코드 생성기 구현
5. ✅ 단위 테스트 작성
6. ✅ 샘플 .proto 파일 테스트
