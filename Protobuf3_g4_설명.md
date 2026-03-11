# Protobuf3.g4 문법 파일 상세 설명

> 이 문서는 `Parsers/Protobuf3.g4` 파일의 모든 규칙과 토큰에 대한 한국어 설명입니다.

---

## 목차

1. [헤더 & 옵션 (1~22줄)](#1-헤더--옵션)
2. [2-Pass 파싱 진입점 (26~28줄)](#2-2-pass-파싱-진입점)
3. [최상위 파일 구조 (30~31줄)](#3-최상위-파일-구조)
4. [syntax 선언 (36~38줄)](#4-syntax-선언)
5. [import 문 (42~44줄)](#5-import-문)
6. [package 선언 (48~50줄)](#6-package-선언)
7. [option 문 (54~61줄)](#7-option-문)
8. [필드 레이블 & 일반 필드 (64~83줄)](#8-필드-레이블--일반-필드)
9. [oneof 블록 (87~93줄)](#9-oneof-블록)
10. [map 필드 (97~114줄)](#10-map-필드)
11. [타입 참조 (118~136줄)](#11-타입-참조)
12. [reserved 선언 (140~154줄)](#12-reserved-선언)
13. [최상위 정의 종류 (158~163줄)](#13-최상위-정의-종류)
14. [enum 정의 (167~192줄)](#14-enum-정의)
15. [message 정의 (196~215줄)](#15-message-정의)
16. [extend 정의 (224~226줄)](#16-extend-정의)
17. [service & rpc 정의 (230~245줄)](#17-service--rpc-정의)
18. [리터럴 및 기본 표현식 (249~265줄)](#18-리터럴-및-기본-표현식)
19. [식별자 및 타입 참조 (269~330줄)](#19-식별자-및-타입-참조)
20. [Lexer 규칙 — 키워드 토큰 (334~480줄)](#20-lexer-규칙--키워드-토큰)
21. [Lexer 규칙 — 기호 토큰 (484~542줄)](#21-lexer-규칙--기호-토큰)
22. [Lexer 규칙 — 리터럴 토큰 (544~623줄)](#22-lexer-규칙--리터럴-토큰)
23. [Lexer 규칙 — 공백 & 주석 (626~636줄)](#23-lexer-규칙--공백--주석)
24. [keywords 파서 규칙 (638~675줄)](#24-keywords-파서-규칙)
25. [시맨틱 액션 전용 규칙 (677~685줄)](#25-시맨틱-액션-전용-규칙)
26. [전체 흐름 요약](#26-전체-흐름-요약)

---

## 1. 헤더 & 옵션

**줄 번호:** 1~22

```antlr
// SPDX-License-Identifier: Apache-2.0
```

- 오픈소스 라이선스 선언 (Apache 2.0)

```antlr
/**
 * A Protocol Buffers 3 grammar
 *
 * Original source: https://developers.google.com/protocol-buffers/docs/reference/proto3-spec
 * Original source is published under Apache License 2.0.
 *
 * Changes from the source above:
 * - rewrite to antlr
 * - extract some group to rule.
 *
 * @author anatawa12
 */
```

- 파일 전체에 대한 설명 블록
- proto3 공식 스펙을 ANTLR4 문법으로 재작성한 파일이며, 원작자는 anatawa12

```antlr
// $antlr-format alignTrailingComments true, columnLimit 150, ...
// $antlr-format allowShortRulesOnASingleLine false, ...
```

- ANTLR4 코드 포맷터(antlr-format)의 설정 지시자
- 생성 코드 정렬 스타일을 제어함. 실제 문법 동작과는 무관

```antlr
grammar Protobuf3;
```

- **문법 이름 선언**
- 이 파일이 `Protobuf3`라는 이름의 combined grammar(Lexer+Parser 통합)임을 선언
- ANTLR은 이 이름으로 `Protobuf3Lexer.cs`, `Protobuf3Parser.cs` 등을 생성함

```antlr
options {
    superClass = Protobuf3ParserBase;
}
```

- **파서 옵션**
- 생성될 `Protobuf3Parser` 클래스가 기본 `Antlr4.Runtime.Parser`를 상속하는 대신 수동 작성된 `Protobuf3ParserBase`를 상속하도록 지정
- 2-pass 파싱, 심볼 테이블, 시맨틱 프레디킷 메서드를 이 베이스 클래스에서 제공함

---

## 2. 2-Pass 파싱 진입점

**줄 번호:** 26~28

```antlr
twoPassParse
    : { this.DoRewind(); } proto
    ;
```

- **파싱 최상위 진입 규칙**
- `{ this.DoRewind(); }` — 파싱 시작 전 C# 코드를 실행하는 **시맨틱 액션**
- `DoRewind()`는 `Protobuf3ParserBase`에서 구현되며:
  1. **1st pass:** 별도 파서 인스턴스로 전체를 한번 파싱해 message/enum 이름을 심볼 테이블에 수집
  2. 토큰 스트림을 위치 0으로 **되감기(rewind)**
  3. **2nd pass:** 심볼 테이블을 활용해 타입을 정확히 구분하며 실제 파싱 수행
- 그 후 `proto` 규칙으로 실제 파싱 트리를 구성

---

## 3. 최상위 파일 구조

**줄 번호:** 30~31

```antlr
proto
    : syntax (importStatement | packageStatement | optionStatement | topLevelDef | emptyStatement_)* EOF
    ;
```

- **proto 파일 전체 구조 정의**
- `syntax` 선언이 반드시 먼저 오고, 이후 import/package/option/타입정의(message,enum,service) 또는 빈 문장(`;`)이 0회 이상 반복된 뒤 EOF로 끝남

---

## 4. syntax 선언

**줄 번호:** 36~38

```antlr
syntax
    : SYNTAX EQ (PROTO3_LIT_SINGLE | PROTO3_LIT_DOUBLE) SEMI
    ;
```

- `syntax = "proto3";` 또는 `syntax = 'proto3';` 구문을 인식
- `PROTO3_LIT_SINGLE`은 `"proto3"`, `PROTO3_LIT_DOUBLE`은 `'proto3'`에 대응하는 Lexer 토큰

---

## 5. import 문

**줄 번호:** 42~44

```antlr
importStatement
    : IMPORT (WEAK | PUBLIC)? strLit SEMI { this.DoImportStatement_(); }
    ;
```

- `import "other.proto";` / `import weak "..."` / `import public "..."` 구문 인식
- 마지막의 `{ this.DoImportStatement_(); }` 시맨틱 액션은 import된 파일명을 파서 베이스에 기록함
- `WEAK`/`PUBLIC` 수식어는 선택 사항(`?`)

---

## 6. package 선언

**줄 번호:** 48~50

```antlr
packageStatement
    : PACKAGE fullIdent SEMI
    ;
```

- `package foo.bar.baz;` 형태의 패키지 선언 인식
- `fullIdent`는 `.`으로 연결된 식별자 체인 (예: `google.protobuf`)

---

## 7. option 문

**줄 번호:** 54~61

```antlr
optionStatement
    : OPTION optionName EQ constant SEMI
    ;

optionName
    : fullIdent
    | LP fullIdent RP ( DOT fullIdent)?
    ;
```

- `option optimize_for = SPEED;` 같은 옵션 문 인식
- `optionName`은 단순 식별자(`optimize_for`) 또는 확장 옵션(`(google.api.http).get`) 형태 모두 허용
- `LP`/`RP`는 `(`/`)`

---

## 8. 필드 레이블 & 일반 필드

**줄 번호:** 64~83

```antlr
fieldLabel
    : OPTIONAL
    | REPEATED
    ;
```

- 필드 앞에 붙는 수식어. `optional` 또는 `repeated` (proto3에서는 생략 가능)

```antlr
field
    : fieldLabel? type_ fieldName EQ fieldNumber (LB fieldOptions RB)? SEMI
    ;
```

- 일반 필드 전체 구조
- 예: `repeated string name = 1 [json_name = "name"];`
- `LB`/`RB`는 `[`/`]` — 필드 옵션을 감싸는 대괄호. 선택 사항

```antlr
fieldOptions
    : fieldOption (COMMA fieldOption)*
    ;

fieldOption
    : optionName EQ constant
    ;
```

- `fieldOptions`: 필드 옵션 목록 (쉼표로 구분)
- `fieldOption`: 옵션 이름 = 값 쌍

```antlr
fieldNumber
    : intLit
    ;
```

- 필드 번호. 정수 리터럴만 허용

---

## 9. oneof 블록

**줄 번호:** 87~93

```antlr
oneof
    : ONEOF oneofName LC (optionStatement | oneofField | emptyStatement_)* RC
    ;

oneofField
    : type_ fieldName EQ fieldNumber (LB fieldOptions RB)? SEMI
    ;
```

- `oneof` 블록 인식
- 예: `oneof payload { string text = 1; bytes blob = 2; }`
- `oneofField`는 일반 `field`와 동일하지만 `fieldLabel`이 없음 (oneof 내부 필드는 레이블 불가)
- `LC`/`RC`는 `{`/`}`

---

## 10. map 필드

**줄 번호:** 97~114

```antlr
mapField
    : MAP LT keyType COMMA type_ GT mapName EQ fieldNumber (LB fieldOptions RB)? SEMI
    ;
```

- `map<string, MyMessage> items = 3;` 형태의 map 필드 인식
- `LT`/`GT`는 `<`/`>`
- `keyType`은 별도 규칙으로 제한됨

```antlr
keyType
    : INT32 | INT64 | UINT32 | UINT64 | SINT32 | SINT64
    | FIXED32 | FIXED64 | SFIXED32 | SFIXED64
    | BOOL | STRING
    ;
```

- **map의 키 타입 제한 목록**
- proto3 스펙에 따라 map 키는 정수/bool/string만 허용
- `float`, `double`, `bytes`, message 타입은 map 키로 사용 불가

---

## 11. 타입 참조

**줄 번호:** 118~136

```antlr
type_
    : DOUBLE | FLOAT | INT32 | INT64 | UINT32 | UINT64
    | SINT32 | SINT64 | FIXED32 | FIXED64 | SFIXED32 | SFIXED64
    | BOOL | STRING | BYTES
    | { this.IsNotKeyword() }? messageType
    | { this.IsNotKeyword() }? enumType
    ;
```

- **필드 타입 전체 목록.** 스칼라 타입(14종)과 사용자 정의 타입(message/enum)을 포함
- `{ this.IsNotKeyword() }?` — **시맨틱 프레디킷**: 현재 식별자가 예약 키워드가 아닐 때만 `messageType`/`enumType` 규칙 시도
- 2nd pass에서 `IsMessageType_()`/`IsEnumType_()`이 심볼 테이블을 참조해 message인지 enum인지 구분

---

## 12. reserved 선언

**줄 번호:** 140~154

```antlr
reserved
    : RESERVED (ranges | reservedFieldNames) SEMI
    ;

ranges
    : range_ (COMMA range_)*
    ;

range_
    : intLit (TO ( intLit | MAX))?
    ;

reservedFieldNames
    : strLit (COMMA strLit)*
    ;
```

- `reserved 2, 15, 9 to 11;` 또는 `reserved "foo", "bar";` 구문 인식
- 삭제된 필드 번호/이름을 예약하여 재사용 방지
- `range_`: 단일 숫자 또는 `9 to 11` / `9 to max` 범위 표현
- `MAX`는 최대 필드 번호(536870911)를 의미

---

## 13. 최상위 정의 종류

**줄 번호:** 158~163

```antlr
topLevelDef
    : messageDef
    | enumDef
    | extendDef
    | serviceDef
    ;
```

- proto 파일 최상위 레벨에서 정의 가능한 4가지 종류:
  - `messageDef` — message 타입 정의
  - `enumDef` — enum 타입 정의
  - `extendDef` — proto2 호환 extend 정의
  - `serviceDef` — gRPC 서비스 정의

---

## 14. enum 정의

**줄 번호:** 167~192

```antlr
enumDef
    : ENUM enumName doEnumNameDef enumBody
    ;

enumBody
    : LC doEnterBlock enumElement* RC doExitBlock
    ;

enumElement
    : optionStatement
    | enumField
    | reserved
    | emptyStatement_
    ;

enumField
    : ident EQ (MINUS)? intLit enumValueOptions? SEMI
    ;

enumValueOptions
    : LB enumValueOption (COMMA enumValueOption)* RB
    ;

enumValueOption
    : optionName EQ constant
    ;
```

- **enum 전체 구조**
- 예: `enum Status { UNKNOWN = 0; ACTIVE = 1; }`
- `doEnumNameDef` — enum 이름을 심볼 테이블에 등록하는 액션 규칙 (1st pass에서 사용)
- `doEnterBlock`/`doExitBlock` — `{`/`}` 진입/탈출 시 스코프 스택 관리
- `enumField`: `UNKNOWN = 0;` 또는 `DISABLED = -1;` 형태. 음수값 허용(`MINUS`?)
- `enumValueOptions`: 필드 옵션과 동일한 형태로 enum 값에도 옵션 부여 가능

---

## 15. message 정의

**줄 번호:** 196~215

```antlr
messageDef
    : MESSAGE messageName doMessageNameDef messageBody
    ;

messageBody
    : LC doEnterBlock messageElement* RC doExitBlock
    ;

messageElement
    : field
    | enumDef
    | messageDef
    | extendDef
    | optionStatement
    | oneof
    | mapField
    | reserved
    | emptyStatement_
    ;
```

- **message 전체 구조**
- 예: `message Player { string name = 1; int32 level = 2; }`
- `doMessageNameDef` — message 이름을 심볼 테이블에 등록
- `messageElement`: message 내부에 올 수 있는 요소 (중첩 message/enum 포함)
- 중첩 `messageDef`/`enumDef`가 허용되므로 **재귀적(recursive)** 문법 구조

---

## 16. extend 정의

**줄 번호:** 224~226

```antlr
extendDef
    : EXTEND messageType LC (field | emptyStatement_)* RC
    ;
```

- proto2 호환 `extend` 구문 인식
- proto3 스펙에는 없지만 `protoc`가 지원하므로 포함
- 기존 message 타입에 필드를 추가하는 proto2 확장 메커니즘

---

## 17. service & rpc 정의

**줄 번호:** 230~245

```antlr
serviceDef
    : SERVICE serviceName doServiceNameDef LC doEnterBlock serviceElement* RC doExitBlock
    ;

serviceElement
    : optionStatement
    | rpc
    | emptyStatement_
    ;

rpc
    : RPC rpcName LP (STREAM)? messageType RP RETURNS LP (STREAM)? messageType RP (
        LC ( optionStatement | emptyStatement_)* RC
        | SEMI
    )
    ;
```

- **gRPC 서비스 정의 구조**
- `doServiceNameDef` — 서비스 이름을 심볼 테이블에 등록
- `rpc`: 예: `rpc GetPlayer(PlayerRequest) returns (PlayerResponse);`
- `(STREAM)?`: 스트리밍 여부. `rpc Chat(stream ChatMsg) returns (stream ChatMsg)` 형태도 허용
- rpc 본문은 `{ option ... ; }` 형태 또는 단순 `;`로 끝낼 수 있음

---

## 18. 리터럴 및 기본 표현식

**줄 번호:** 249~265

```antlr
constant
    : { this.IsNotKeyword() }? fullIdent
    | (MINUS | PLUS)? intLit
    | (MINUS | PLUS)? floatLit
    | strLit
    | boolLit
    | blockLit
    ;

blockLit
    : LC (ident COLON constant)* RC
    ;

emptyStatement_
    : SEMI
    ;
```

- `constant`: 옵션 값으로 사용될 수 있는 모든 상수 표현. 식별자/정수/실수/문자열/bool/블록 리터럴
- `blockLit`: `{ key: value }` 형태의 구조체 리터럴 (proto 스펙 비표준이지만 테스트에 사용됨)
- `emptyStatement_`: 단순 `;` 하나. 빈 줄 대신 허용

---

## 19. 식별자 및 타입 참조

**줄 번호:** 269~330

```antlr
ident
    : IDENTIFIER
    | keywords
    ;

fullIdent
    : ident (DOT ident)*
    ;
```

- `ident`: 일반 식별자. proto에서는 키워드도 필드 이름 등으로 사용 가능하므로 `keywords` 대안을 포함
- `fullIdent`: `.`으로 연결된 복합 식별자. 예: `google.protobuf.Timestamp`

```antlr
messageName : ident ;
enumName    : ident ;
fieldName   : ident ;
oneofName   : ident ;
mapName     : ident ;
serviceName : ident ;
rpcName     : ident ;
```

- 각 문맥에서 이름 역할을 하는 규칙들. 모두 `ident`의 별칭
- **파스 트리에서 의미적으로 구분**되도록 별도 규칙으로 정의함 (컨텍스트 클래스가 별도 생성됨)

```antlr
messageType
    : { this.IsMessageType_() }? (DOT)? (ident DOT)* messageName
    ;

enumType
    : { this.IsEnumType_() }? (DOT)? (ident DOT)* enumName
    ;
```

- `messageType`: 2nd pass에서 `IsMessageType_()` 프레디킷이 true일 때만 매칭
- 심볼 테이블에 등록된 message 이름인지 검사
- 앞에 `.`(절대 경로) 또는 패키지 경로(`foo.bar.MyMsg`) 형태도 허용
- `enumType`: 동일하지만 enum 심볼 테이블 검사

```antlr
intLit   : INT_LIT ;
strLit   : STR_LIT | PROTO3_LIT_SINGLE | PROTO3_LIT_DOUBLE ;
boolLit  : BOOL_LIT ;
floatLit : FLOAT_LIT ;
```

- 각 리터럴 타입을 Lexer 토큰으로 매핑하는 Parser 규칙
- `strLit`은 일반 문자열뿐 아니라 `"proto3"`/`'proto3'` 특수 리터럴 토큰도 포함

---

## 20. Lexer 규칙 — 키워드 토큰

**줄 번호:** 334~480

### 구조적 키워드

| 토큰 | 리터럴 | 설명 |
|------|--------|------|
| `SYNTAX` | `'syntax'` | 프로토콜 버전 선언 키워드 |
| `IMPORT` | `'import'` | 외부 파일 가져오기 키워드 |
| `WEAK` | `'weak'` | 약한 import 수식어 |
| `PUBLIC` | `'public'` | 공개 import 수식어 |
| `PACKAGE` | `'package'` | 패키지 선언 키워드 |
| `OPTION` | `'option'` | 옵션 선언 키워드 |
| `OPTIONAL` | `'optional'` | 선택적 필드 레이블 |
| `REPEATED` | `'repeated'` | 반복 필드 레이블 (배열) |
| `ONEOF` | `'oneof'` | 택일 필드 그룹 키워드 |
| `MAP` | `'map'` | 맵 필드 키워드 |

### 스칼라 타입 키워드 (15종)

| 토큰 | 리터럴 | 설명 |
|------|--------|------|
| `INT32` | `'int32'` | 32비트 부호 있는 정수 (가변 길이 인코딩) |
| `INT64` | `'int64'` | 64비트 부호 있는 정수 (가변 길이 인코딩) |
| `UINT32` | `'uint32'` | 32비트 부호 없는 정수 |
| `UINT64` | `'uint64'` | 64비트 부호 없는 정수 |
| `SINT32` | `'sint32'` | 32비트 부호 있는 정수 (ZigZag 인코딩, 음수에 효율적) |
| `SINT64` | `'sint64'` | 64비트 부호 있는 정수 (ZigZag 인코딩) |
| `FIXED32` | `'fixed32'` | 항상 4바이트 부호 없는 정수 |
| `FIXED64` | `'fixed64'` | 항상 8바이트 부호 없는 정수 |
| `SFIXED32` | `'sfixed32'` | 항상 4바이트 부호 있는 정수 |
| `SFIXED64` | `'sfixed64'` | 항상 8바이트 부호 있는 정수 |
| `BOOL` | `'bool'` | 불리언 (true/false) |
| `STRING` | `'string'` | UTF-8 문자열 |
| `DOUBLE` | `'double'` | 64비트 부동소수점 |
| `FLOAT` | `'float'` | 32비트 부동소수점 |
| `BYTES` | `'bytes'` | 임의 바이트 시퀀스 |

### 나머지 예약 키워드

| 토큰 | 리터럴 | 설명 |
|------|--------|------|
| `RESERVED` | `'reserved'` | 필드 번호/이름 예약 |
| `TO` | `'to'` | reserved 범위 키워드 |
| `MAX` | `'max'` | 최대 필드 번호 (536870911) |
| `ENUM` | `'enum'` | enum 타입 정의 키워드 |
| `MESSAGE` | `'message'` | message 타입 정의 키워드 |
| `SERVICE` | `'service'` | gRPC 서비스 정의 키워드 |
| `EXTEND` | `'extend'` | proto2 호환 확장 키워드 |
| `RPC` | `'rpc'` | RPC 메서드 정의 키워드 |
| `STREAM` | `'stream'` | 스트리밍 RPC 수식어 |
| `RETURNS` | `'returns'` | RPC 반환 타입 키워드 |

### 특수 리터럴 토큰

| 토큰 | 리터럴 | 설명 |
|------|--------|------|
| `PROTO3_LIT_SINGLE` | `'"proto3"'` | syntax 선언 전용 (큰따옴표) |
| `PROTO3_LIT_DOUBLE` | `'\'proto3\''` | syntax 선언 전용 (작은따옴표) |

---

## 21. Lexer 규칙 — 기호 토큰

**줄 번호:** 484~542

| 토큰 | 문자 | 용도 |
|------|------|------|
| `SEMI` | `;` | 문장 종료 |
| `EQ` | `=` | 대입 (필드 번호, 옵션 값) |
| `LP` | `(` | 확장 옵션, rpc 파라미터 시작 |
| `RP` | `)` | 확장 옵션, rpc 파라미터 끝 |
| `LB` | `[` | 필드 옵션 시작 |
| `RB` | `]` | 필드 옵션 끝 |
| `LC` | `{` | 블록(message/enum/service 본문) 시작 |
| `RC` | `}` | 블록 끝 |
| `LT` | `<` | map 타입 꺾쇠 시작 |
| `GT` | `>` | map 타입 꺾쇠 끝 |
| `DOT` | `.` | 패키지 경로 구분자 |
| `COMMA` | `,` | 옵션/범위 구분자 |
| `COLON` | `:` | blockLit의 key:value 구분 |
| `PLUS` | `+` | 양수 부호 |
| `MINUS` | `-` | 음수 부호 |

---

## 22. Lexer 규칙 — 리터럴 토큰

**줄 번호:** 544~623

### 문자열 리터럴

```antlr
STR_LIT
    : ('\'' ( CHAR_VALUE)*? '\'')
    | ( '"' ( CHAR_VALUE)*? '"')
    ;
```

- 작은따옴표 또는 큰따옴표로 감싼 문자열
- `*?` = 비탐욕적(non-greedy) 매칭으로 가장 짧게 일치

### 문자열 내부 문자 (fragment)

```antlr
fragment CHAR_VALUE
    : HEX_ESCAPE       // 16진수 이스케이프 (\xHH)
    | OCT_ESCAPE       // 8진수 이스케이프 (\OOO)
    | CHAR_ESCAPE      // 문자 이스케이프 (\n, \t 등)
    | ~[\u0000\n\\]    // null, 줄바꿈, 백슬래시를 제외한 일반 문자
    ;
```

### 이스케이프 시퀀스 (fragment)

| 규칙 | 예시 | 설명 |
|------|------|------|
| `HEX_ESCAPE` | `\x1F` | `\\` + `x`/`X` + 16진수 2자리 |
| `OCT_ESCAPE` | `\077` | `\\` + 8진수 3자리 |
| `CHAR_ESCAPE` | `\n`, `\t`, `\\` | C 스타일 이스케이프 문자 (`a`,`b`,`f`,`n`,`r`,`t`,`v`,`\\`,`'`,`"`) |

### 불리언 리터럴

```antlr
BOOL_LIT : 'true' | 'false' ;
```

### 부동소수점 리터럴

```antlr
FLOAT_LIT
    : (DECIMALS DOT DECIMALS? EXPONENT? | DECIMALS EXPONENT | DOT DECIMALS EXPONENT?)
    | 'inf'
    | 'nan'
    ;

fragment EXPONENT : ('e' | 'E') (PLUS | MINUS)? DECIMALS ;
fragment DECIMALS : DECIMAL_DIGIT+ ;
```

- `1.0`, `.5e10`, `3.14E-2`, `inf`, `nan` 등을 인식
- `EXPONENT`: `e+10`, `E-3` 형태의 지수부
- `DECIMALS`: 1개 이상의 십진수 숫자

### 정수 리터럴

```antlr
INT_LIT
    : DECIMAL_LIT
    | OCTAL_LIT
    | HEX_LIT
    ;

fragment DECIMAL_LIT : ([1-9]) DECIMAL_DIGIT* ;   // 10진수 (123)
fragment OCTAL_LIT   : '0' OCTAL_DIGIT* ;         // 8진수 (0755), 0 단독 포함
fragment HEX_LIT     : '0' ('x' | 'X') HEX_DIGIT+ ;  // 16진수 (0xFF)
```

### 식별자

```antlr
IDENTIFIER : LETTER (LETTER | DECIMAL_DIGIT)* ;
```

- 영문자/언더스코어로 시작하고 영숫자/언더스코어가 이어지는 이름

### 문자 범위 fragment

| 규칙 | 범위 | 설명 |
|------|------|------|
| `LETTER` | `[A-Za-z_]` | 알파벳 대소문자 + 언더스코어 |
| `DECIMAL_DIGIT` | `[0-9]` | 십진수 숫자 |
| `OCTAL_DIGIT` | `[0-7]` | 8진수 숫자 |
| `HEX_DIGIT` | `[0-9A-Fa-f]` | 16진수 숫자 |

---

## 23. Lexer 규칙 — 공백 & 주석

**줄 번호:** 626~636

```antlr
WS : [ \t\r\n\u000C]+ -> skip ;
```

- **공백 처리.** 스페이스, 탭, CR, LF, Form Feed를 토큰 스트림에서 **완전히 제거(skip)**
- 파서는 공백을 전혀 보지 못함

```antlr
LINE_COMMENT : '//' ~[\r\n]* -> channel(HIDDEN) ;
COMMENT      : '/*' .*? '*/' -> channel(HIDDEN) ;
```

- **주석 처리**
- 한 줄 주석(`//`)과 블록 주석(`/* */`) 모두 **HIDDEN 채널**로 전송
- 파서에는 노출되지 않지만 토큰 스트림에는 보존됨 (필요 시 툴에서 접근 가능)

### `skip` vs `channel(HIDDEN)` 차이

| 지시자 | 동작 | 접근 가능 여부 |
|--------|------|----------------|
| `skip` | 토큰 스트림에서 완전 제거 | 불가 |
| `channel(HIDDEN)` | 파서에 안 보이지만 스트림에 보존 | 코드에서 접근 가능 |

---

## 24. keywords 파서 규칙

**줄 번호:** 638~675

```antlr
keywords
    : SYNTAX | IMPORT | WEAK | PUBLIC | PACKAGE | OPTION
    | OPTIONAL | REPEATED | ONEOF | MAP
    | INT32 | INT64 | UINT32 | UINT64 | SINT32 | SINT64
    | FIXED32 | FIXED64 | SFIXED32 | SFIXED64
    | BOOL | STRING | DOUBLE | FLOAT | BYTES
    | RESERVED | TO | MAX | ENUM | MESSAGE
    | SERVICE | EXTEND | RPC | STREAM | RETURNS
    | BOOL_LIT
    ;
```

- **모든 예약 키워드의 목록을 파서 규칙으로 묶은 것**
- `ident` 규칙에서 `IDENTIFIER | keywords`를 허용하는 이유:
  - proto3에서는 `message`, `string` 같은 키워드도 필드 이름으로 사용 가능하기 때문
- `IsNotKeyword()` 프레디킷이 타입 매칭 시 이 keywords 규칙을 참조하여 키워드를 식별자로 오인하지 않도록 방지

---

## 25. 시맨틱 액션 전용 규칙

**줄 번호:** 677~685

이 규칙들은 모두 **빈 파서 규칙에 C# 코드를 삽입**하는 ANTLR4 시맨틱 액션 패턴입니다.

```antlr
doEnterBlock     : { this.DoEnterBlock_(); } ;
```

- `{` 진입 시 스코프 스택에 현재 컨텍스트 push. 중첩 타입 이름 추적에 사용

```antlr
doEnumNameDef    : { this.DoEnumNameDef_(); } ;
```

- enum 이름 토큰 직후 호출
- 1st pass에서 enum 이름을 `_enumTypes` 심볼 테이블에 등록

```antlr
doExitBlock      : { this.DoExitBlock_(); } ;
```

- `}` 탈출 시 스코프 스택에서 pop. 중첩 스코프 복원

```antlr
doMessageNameDef : { this.DoMessageNameDef_(); } ;
```

- message 이름 토큰 직후 호출
- 1st pass에서 message 이름을 `_messageTypes` 심볼 테이블에 등록

```antlr
doServiceNameDef : { this.DoServiceNameDef_(); } ;
```

- service 이름 토큰 직후 호출
- 1st pass에서 service 이름을 심볼 테이블에 등록

### 시맨틱 액션이 사용되는 위치 정리

| 액션 규칙 | 호출 위치 | 역할 |
|-----------|-----------|------|
| `doMessageNameDef` | `messageDef` 내 이름 뒤 | message 이름 등록 |
| `doEnumNameDef` | `enumDef` 내 이름 뒤 | enum 이름 등록 |
| `doServiceNameDef` | `serviceDef` 내 이름 뒤 | service 이름 등록 |
| `doEnterBlock` | `enumBody`, `messageBody`, `serviceDef` 내 `{` 뒤 | 스코프 push |
| `doExitBlock` | `enumBody`, `messageBody`, `serviceDef` 내 `}` 뒤 | 스코프 pop |

---

## 26. 전체 흐름 요약

```
Protobuf3.g4
│
├── grammar Protobuf3             ← 이름 선언
├── options { superClass = ... }  ← 커스텀 베이스 클래스 지정
│
├── [Parser 규칙 - 소문자]
│   ├── twoPassParse              ← 진입점, DoRewind() 호출
│   ├── proto                     ← 파일 최상위 구조
│   ├── syntax/import/package/option  ← 헤더 선언들
│   ├── field/oneof/mapField      ← 필드 종류들
│   ├── enumDef/messageDef        ← 타입 정의 (재귀 포함)
│   ├── serviceDef/rpc            ← gRPC 서비스
│   ├── type_/messageType/enumType ← 타입 참조 (시맨틱 프레디킷)
│   └── ident/fullIdent/리터럴    ← 기본 표현식
│
├── [Lexer 규칙 - 대문자]
│   ├── 키워드: MESSAGE, ENUM, REPEATED ...
│   ├── 기호: SEMI, EQ, LC, RC ...
│   ├── 리터럴: INT_LIT, STR_LIT, FLOAT_LIT, BOOL_LIT
│   ├── IDENTIFIER
│   ├── fragment: 보조 규칙 (독립 토큰 불가)
│   └── WS(skip) / COMMENT(HIDDEN)
│
└── [시맨틱 액션 규칙]
    ├── doEnterBlock / doExitBlock     ← 스코프 스택 관리
    └── doMessageNameDef / doEnumNameDef / doServiceNameDef  ← 심볼 테이블 등록
```
