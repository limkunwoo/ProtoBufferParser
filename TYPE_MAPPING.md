# ProtoBufferParser - Type Mapping Reference

## 개요

이 문서는 Protocol Buffer 3 (Proto3) 타입을 Unreal Engine C++ 타입으로 변환하는 규칙을 상세히 설명합니다.

---

## 1. 기본 타입 (Scalar Types)

### 1.1 정수 타입

| Proto3 타입 | Unreal C++ 타입 | 비고 |
|------------|----------------|------|
| `int32` | `int32` | 가변 길이 인코딩, 음수에 비효율적 |
| `int64` | `int64` | 가변 길이 인코딩, 음수에 비효율적 |
| `uint32` | `uint32` | 가변 길이 인코딩 |
| `uint64` | `uint64` | 가변 길이 인코딩 |
| `sint32` | `int32` | 가변 길이 인코딩, 음수에 효율적 (ZigZag) |
| `sint64` | `int64` | 가변 길이 인코딩, 음수에 효율적 (ZigZag) |
| `fixed32` | `uint32` | 항상 4바이트, 2^28보다 큰 값에 효율적 |
| `fixed64` | `uint64` | 항상 8바이트, 2^56보다 큰 값에 효율적 |
| `sfixed32` | `int32` | 항상 4바이트 |
| `sfixed64` | `int64` | 항상 8바이트 |

#### 예제
```protobuf
// Proto3
message Stats {
    int32 health = 1;
    uint32 score = 2;
    sint32 temperature = 3;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FStatsProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Health;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    uint32 Score;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Temperature;
};
```

### 1.2 부동소수점 타입

| Proto3 타입 | Unreal C++ 타입 | 비고 |
|------------|----------------|------|
| `float` | `float` | 32비트 부동소수점 |
| `double` | `double` | 64비트 부동소수점 |

#### 예제
```protobuf
// Proto3
message Position {
    float x = 1;
    float y = 2;
    double precise_coord = 3;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FPositionProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    float X;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    float Y;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    double PreciseCoord;
};
```

### 1.3 불리언 타입

| Proto3 타입 | Unreal C++ 타입 | 비고 |
|------------|----------------|------|
| `bool` | `bool` | true 또는 false |

#### 예제
```protobuf
// Proto3
message Flags {
    bool is_active = 1;
    bool can_fly = 2;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FFlagsProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    bool IsActive;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    bool CanFly;
};
```

### 1.4 문자열 및 바이트

| Proto3 타입 | Unreal C++ 타입 | 비고 |
|------------|----------------|------|
| `string` | `FString` | UTF-8 인코딩 문자열 |
| `bytes` | `TArray<uint8>` | 임의의 바이트 시퀀스 |

#### 예제
```protobuf
// Proto3
message UserInfo {
    string username = 1;
    string email = 2;
    bytes avatar_data = 3;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FUserInfoProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Username;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Email;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<uint8> AvatarData;
};
```

---

## 2. 복합 타입 (Composite Types)

### 2.1 Message (구조체)

Proto3의 `message`는 Unreal의 `USTRUCT`로 변환됩니다.

#### 네이밍 규칙
- **Prefix**: `F` (Unreal 구조체 규칙)
- **Suffix**: `Proto` (자동 생성 구분)
- **Case**: PascalCase

| Proto3 Message 이름 | Unreal Struct 이름 |
|--------------------|-------------------|
| `PlayerInfo` | `FPlayerInfoProto` |
| `player_info` | `FPlayerInfoProto` |
| `PLAYER_INFO` | `FPlayerInfoProto` |

#### 예제
```protobuf
// Proto3
message Player {
    int32 id = 1;
    string name = 2;
    float health = 3;
}

message GameSession {
    string session_id = 1;
    Player host = 2;
}
```

```cpp
// Unreal C++ - MEPlayerProto.h
USTRUCT(BlueprintType)
struct FPlayerProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Name;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    float Health;
};

// Unreal C++ - MEGameSessionProto.h
USTRUCT(BlueprintType)
struct FGameSessionProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString SessionId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FPlayerProto Host;  // 다른 메시지 타입 참조
};
```

### 2.2 Enum

Proto3의 `enum`은 Unreal의 `UENUM`으로 변환됩니다.

#### 네이밍 규칙
- **Prefix**: `E` (Unreal enum 규칙)
- **Suffix**: `Proto` (자동 생성 구분)
- **Case**: PascalCase
- **Enum Class**: `enum class` 사용 (타입 안전)

| Proto3 Enum 이름 | Unreal Enum 이름 |
|-----------------|-----------------|
| `Status` | `EStatusProto` |
| `PlayerState` | `EPlayerStateProto` |
| `GAME_MODE` | `EGameModeProto` |

#### Enum 값 네이밍
- Proto3: `UPPER_SNAKE_CASE` (관례)
- Unreal: `PascalCase`

| Proto3 Enum 값 | Unreal Enum 값 |
|---------------|---------------|
| `UNKNOWN` | `Unknown` |
| `ACTIVE_STATE` | `ActiveState` |
| `GAME_READY` | `GameReady` |

#### 예제
```protobuf
// Proto3
enum Status {
    UNKNOWN = 0;
    ACTIVE = 1;
    INACTIVE = 2;
    SUSPENDED = 3;
}

message Player {
    int32 id = 1;
    Status status = 2;
}
```

```cpp
// Unreal C++ - MEStatusProto.h
UENUM(BlueprintType)
enum class EStatusProto : uint8
{
    Unknown = 0 UMETA(DisplayName = "Unknown"),
    Active = 1 UMETA(DisplayName = "Active"),
    Inactive = 2 UMETA(DisplayName = "Inactive"),
    Suspended = 3 UMETA(DisplayName = "Suspended")
};

// Unreal C++ - MEPlayerProto.h
USTRUCT(BlueprintType)
struct FPlayerProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    EStatusProto Status;
};
```

### 2.3 Repeated Fields (배열)

Proto3의 `repeated` 필드는 Unreal의 `TArray<T>`로 변환됩니다.

#### 예제
```protobuf
// Proto3
message Inventory {
    repeated int32 item_ids = 1;
    repeated string item_names = 2;
    repeated Player players = 3;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FInventoryProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<int32> ItemIds;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<FString> ItemNames;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<FPlayerProto> Players;
};
```

### 2.4 Map Fields (맵)

Proto3의 `map<K, V>`는 Unreal의 `TMap<K, V>`로 변환됩니다.

#### 지원되는 Key 타입
- 정수 타입: `int32`, `int64`, `uint32`, `uint64`
- `bool`
- `string` (FString으로 변환)

#### 예제
```protobuf
// Proto3
message GameConfig {
    map<string, int32> settings = 1;
    map<int32, string> player_names = 2;
    map<string, Player> players_by_name = 3;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FGameConfigProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TMap<FString, int32> Settings;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TMap<int32, FString> PlayerNames;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TMap<FString, FPlayerProto> PlayersByName;
};
```

---

## 3. 중첩 메시지 (Nested Messages)

중첩된 메시지는 **Flat하게 풀어서** 별도의 구조체로 생성됩니다.

### 네이밍 규칙
부모 메시지와 자식 메시지를 언더스코어(`_`)로 연결합니다.

| Proto3 중첩 구조 | Unreal Struct 이름 |
|-----------------|-------------------|
| `Outer.Inner` | `FOuter_InnerProto` |
| `Player.Inventory.Item` | `FPlayer_Inventory_ItemProto` |

### 예제
```protobuf
// Proto3
message Player {
    int32 id = 1;
    
    message Inventory {
        repeated int32 item_ids = 1;
        
        message Item {
            int32 id = 1;
            string name = 2;
        }
        
        repeated Item items = 2;
    }
    
    Inventory inventory = 2;
}
```

```cpp
// Unreal C++ - 3개의 별도 구조체로 생성

// MEPlayer_Inventory_ItemProto.h
USTRUCT(BlueprintType)
struct FPlayer_Inventory_ItemProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Name;
};

// MEPlayer_InventoryProto.h
USTRUCT(BlueprintType)
struct FPlayer_InventoryProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<int32> ItemIds;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TArray<FPlayer_Inventory_ItemProto> Items;
};

// MEPlayerProto.h
USTRUCT(BlueprintType)
struct FPlayerProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FPlayer_InventoryProto Inventory;
};
```

---

## 4. 중첩 Enum

중첩된 Enum도 Flat하게 풀어집니다.

### 예제
```protobuf
// Proto3
message Player {
    enum Status {
        UNKNOWN = 0;
        ONLINE = 1;
        OFFLINE = 2;
    }
    
    int32 id = 1;
    Status status = 2;
}
```

```cpp
// Unreal C++ - 별도 파일로 생성

// MEPlayer_StatusProto.h
UENUM(BlueprintType)
enum class EPlayer_StatusProto : uint8
{
    Unknown = 0 UMETA(DisplayName = "Unknown"),
    Online = 1 UMETA(DisplayName = "Online"),
    Offline = 2 UMETA(DisplayName = "Offline")
};

// MEPlayerProto.h
USTRUCT(BlueprintType)
struct FPlayerProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    EPlayer_StatusProto Status;
};
```

---

## 5. 필드명 변환 규칙

### Proto3 → Unreal C++

| Proto3 스타일 | Unreal C++ 스타일 | 예제 |
|--------------|------------------|------|
| `snake_case` | `PascalCase` | `player_name` → `PlayerName` |
| `camelCase` | `PascalCase` | `playerName` → `PlayerName` |
| `UPPER_CASE` | `PascalCase` | `PLAYER_NAME` → `PlayerName` |

### 예제
```protobuf
// Proto3
message Example {
    int32 user_id = 1;
    string first_name = 2;
    bool is_active = 3;
    float MAX_HP = 4;
}
```

```cpp
// Unreal C++
USTRUCT(BlueprintType)
struct FExampleProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 UserId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString FirstName;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    bool IsActive;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    float MaxHp;
};
```

---

## 6. 파일명 규칙

### Proto3 파일명 → Unreal 파일명

| Proto3 파일 | Unreal 헤더 | Unreal 구현 |
|------------|-----------|-----------|
| `player.proto` | `MEPlayerProto.h` | `MEPlayerProto.cpp` |
| `player_info.proto` | `MEPlayerInfoProto.h` | `MEPlayerInfoProto.cpp` |
| `game_session.proto` | `MEGameSessionProto.h` | `MEGameSessionProto.cpp` |

**규칙:**
- 접두사: `ME` (Message/Enum 구분용)
- 접미사: `Proto` (자동 생성 구분)
- snake_case → PascalCase 변환

---

## 7. Import 처리

Proto3의 `import`는 Unreal C++의 `#include`로 변환됩니다.

### 예제
```protobuf
// common.proto
message Address {
    string street = 1;
    string city = 2;
}

// user.proto
import "common.proto";

message User {
    int32 id = 1;
    string name = 2;
    Address address = 3;
}
```

```cpp
// MEAddressProto.h
#pragma once

#include "CoreMinimal.h"
#include "MEAddressProto.generated.h"

USTRUCT(BlueprintType)
struct FAddressProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Street;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString City;
};

// MEUserProto.h
#pragma once

#include "CoreMinimal.h"
#include "MEAddressProto.h"  // Import 의존성
#include "MEUserProto.generated.h"

USTRUCT(BlueprintType)
struct FUserProto
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Name;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FAddressProto Address;
};
```

---

## 8. Proto3 미지원 기능

### Proto2 전용 기능 (지원 안 함)
- `required` 키워드
- `extensions`
- `groups`
- Default values (Proto3는 zero values 사용)

**참고**: Proto3의 `optional` 키워드는 지원됩니다 (TOptional로 변환). Proto2의 `optional`과는 다릅니다.

### 처리 방법
Proto2 문법이 감지되면 **컴파일 에러** 발생:
```
Error PC006: Unsupported Proto2 syntax detected in player.proto
Proto3 syntax is required. Please convert to Proto3.
```

---

## 9. 특수 케이스 처리

### 9.1 Reserved 필드/번호
```protobuf
message Example {
    reserved 2, 15, 9 to 11;
    reserved "foo", "bar";
    
    int32 id = 1;
}
```

**처리 방법**: Reserved 필드는 무시하고, 코드 생성하지 않음.

### 9.2 Oneof → TOptional

`oneof` 필드는 `TOptional<T>`로 래핑되어 생성됩니다. 각 필드에 `has_xxx()` 가드가 사용됩니다.

```protobuf
message Example {
    oneof test_oneof {
        string name = 1;
        int32 id = 2;
    }
}
```

자세한 내용은 **섹션 13. Optional / Oneof → TOptional**을 참고하세요.

### 9.3 Optional 필드 → TOptional

Proto3에서 `optional` 키워드가 붙은 필드는 `TOptional<T>`로 래핑됩니다.

```protobuf
message Example {
    optional string nickname = 1;
    optional int32 age = 2;
}
```

자세한 내용은 **섹션 13. Optional / Oneof → TOptional**을 참고하세요.

### 9.4 Any, Timestamp, Duration 등 Well-Known Types
```protobuf
import "google/protobuf/timestamp.proto";

message Event {
    google.protobuf.Timestamp created_at = 1;
}
```

**처리 방법**: 향후 지원 예정. 현재는 커스텀 매핑 필요.

---

## 10. 마샬링 (Marshaling) - Proto → Unreal 변환

모든 생성된 Unreal 구조체는 대응하는 Protocol Buffer 메시지 타입을 받아서 변환하는 **마샬링 생성자**를 제공합니다.

### 10.1 마샬링 생성자

```cpp
// 헤더 파일에 선언
USTRUCT(BlueprintType)
struct FPlayerProto
{
    GENERATED_BODY()
    
    // 기본 생성자
    FPlayerProto() = default;
    
    // 마샬링 생성자 (protoc로 생성된 C++ 클래스를 받음)
    explicit FPlayerProto(const proto::Player& proto);
    
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;
    
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Name;
};
```

**설명:**
- `proto::Player`는 Google Protocol Buffer 컴파일러(protoc)가 생성한 C++ 클래스
- `.proto` 파일에 `package proto;`가 있으면 네임스페이스가 `proto::`가 됨
- 패키지가 없으면 전역 네임스페이스에 생성됨

### 10.2 마샬링 구현 (CPP 파일)

#### 기본 타입 변환
```cpp
// MEPlayerProto.cpp
#include "MEPlayerProto.h"
#include "player.pb.h"  // protoc로 생성된 헤더

FPlayerProto::FPlayerProto(const proto::Player& proto)
{
    // int32, float, double, bool - getter로 접근
    Id = proto.id();
    
    // string → FString 변환
    Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));
}
```

**protoc 생성 코드 특징:**
- 모든 필드는 getter 함수로 접근: `proto.field_name()`
- repeated 필드는 `proto.field_name_size()`로 크기 확인
- map 필드는 `proto.field_name()`로 맵 접근

#### Repeated 필드 변환
```cpp
// message Inventory { repeated int32 item_ids = 1; }

FInventoryProto::FInventoryProto(const proto::Inventory& proto)
{
    // Repeated 필드 - size()로 크기 확인, getter로 접근
    ItemIds.Reserve(proto.item_ids_size());
    for (int i = 0; i < proto.item_ids_size(); ++i)
    {
        ItemIds.Add(proto.item_ids(i));
    }
}
```

#### Bytes 필드 변환
```cpp
// message Data { bytes content = 1; }

FDataProto::FDataProto(const proto::Data& proto)
{
    // bytes → TArray<uint8>
    const std::string& content = proto.content();
    Content.SetNum(content.size());
    FMemory::Memcpy(Content.GetData(), content.data(), content.size());
}
```

#### 메시지 타입 변환 (재귀적)
```cpp
// message GameSession { 
//     string session_id = 1;
//     Player host = 2; 
// }

FGameSessionProto::FGameSessionProto(const proto::GameSession& proto)
{
    SessionId = FString(UTF8_TO_TCHAR(proto.session_id().c_str()));
    
    // 중첩 메시지 - 재귀적으로 마샬링 생성자 호출
    Host = FPlayerProto(proto.host());
}
```

#### Map 필드 변환
```cpp
// message Config { map<string, int32> settings = 1; }

FConfigProto::FConfigProto(const proto::Config& proto)
{
    // Map 필드 - protoc 생성 코드는 google::protobuf::Map 사용
    // 주의: TMap에는 Reserve()가 없음
    for (const auto& [key, value] : proto.settings())
    {
        Settings.Add(FString(UTF8_TO_TCHAR(key.c_str())), value);
    }
}
```

#### Repeated 메시지 변환
```cpp
// message Guild { repeated Player members = 1; }

FGuildProto::FGuildProto(const proto::Guild& proto)
{
    Members.Reserve(proto.members_size());
    for (int i = 0; i < proto.members_size(); ++i)
    {
        Members.Add(FPlayerProto(proto.members(i)));
    }
}
```

#### Enum 변환
```cpp
// enum Status { UNKNOWN = 0; ACTIVE = 1; }
// message Player { 
//     int32 id = 1;
//     string name = 2;
//     Status status = 3; 
// }

FPlayerProto::FPlayerProto(const proto::Player& proto)
{
    Id = proto.id();
    Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));
    
    // Enum 변환 - protoc 생성 enum → Unreal enum
    // protoc는 enum을 int로 변환 가능
    Status = static_cast<EStatusProto>(proto.status());
}
```

### 10.3 사용 예제

```cpp
// protoc로 생성된 C++ 클래스를 사용
// player.proto를 protoc로 컴파일하면 player.pb.h가 생성됨
#include "player.pb.h"  // protoc 생성 파일
#include "MEPlayerProto.h"  // 우리 도구가 생성한 파일

// 네트워크에서 Protocol Buffer 객체 수신
proto::Player Player = ReceiveFromNetwork();

// Unreal 구조체로 변환 (마샬링)
FPlayerProto Proto(Player);

// Unreal Engine에서 사용
UE_LOG(LogTemp, Log, TEXT("Player: %s (ID: %d)"), *Proto.Name, Proto.Id);
```

### 10.4 복합 예제

```protobuf
// game_data.proto
syntax = "proto3";

package proto;  // C++에서 proto:: 네임스페이스가 됨

message Item {
    int32 id = 1;
    string name = 2;
    map<string, int32> stats = 3;
}

message Player {
    int32 id = 1;
    string username = 2;
    repeated Item inventory = 3;
}
```

```cpp
// MEPlayerProto.cpp
#include "MEPlayerProto.h"
#include "game_data.pb.h"  // protoc로 생성된 파일

FPlayerProto::FPlayerProto(const proto::Player& proto)
{
    // 기본 필드
    Id = proto.id();
    Username = FString(UTF8_TO_TCHAR(proto.username().c_str()));
    
    // Repeated 메시지
    Inventory.Reserve(proto.inventory_size());
    for (int i = 0; i < proto.inventory_size(); ++i)
    {
        Inventory.Add(FItemProto(proto.inventory(i)));
    }
}

// MEItemProto.cpp
FItemProto::FItemProto(const proto::Item& proto)
{
    Id = proto.id();
    Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));
    
    // Map 필드 - TMap에는 Reserve() 없음
    for (const auto& [key, value] : proto.stats())
    {
        Stats.Add(FString(UTF8_TO_TCHAR(key.c_str())), value);
    }
}
```

**사용 예제:**
```cpp
// 네트워크나 파일에서 protobuf 데이터 로드
proto::Player playerData = LoadPlayerData();

// Unreal 구조체로 변환
FPlayerProto unrealPlayer(playerData);

// Unreal Engine에서 사용
for (const FItemProto& item : unrealPlayer.Inventory)
{
    UE_LOG(LogTemp, Log, TEXT("Item: %s (ID: %d)"), *item.Name, item.Id);
}
```

---

## 11. 타입 매핑 요약표

| Proto3 | Unreal C++ | 비고 |
|--------|-----------|------|
| `int32` | `int32` | |
| `int64` | `int64` | |
| `uint32` | `uint32` | |
| `uint64` | `uint64` | |
| `sint32` | `int32` | |
| `sint64` | `int64` | |
| `fixed32` | `uint32` | |
| `fixed64` | `uint64` | |
| `sfixed32` | `int32` | |
| `sfixed64` | `int64` | |
| `float` | `float` | |
| `double` | `double` | |
| `bool` | `bool` | |
| `string` | `FString` | |
| `bytes` | `TArray<uint8>` | |
| `message Foo` | `struct FFooProto` | USTRUCT |
| `enum Bar` | `enum class EBarProto` | UENUM |
| `repeated T` | `TArray<T>` | |
| `map<K, V>` | `TMap<K, V>` | |
| `optional T` | `TOptional<T>` | has_xxx() 가드 사용 |
| `oneof { T }` | `TOptional<T>` | 각 필드별 has_xxx() 가드 |

---

## 12. UPROPERTY 속성

현재 모든 필드에 적용되는 UPROPERTY 속성:

```cpp
UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
```

### 향후 커스터마이징 가능 옵션 (GUI):
- `EditAnywhere` vs `VisibleAnywhere` vs `EditDefaultsOnly`
- `BlueprintReadWrite` vs `BlueprintReadOnly`
- `Category` 이름 변경
- `meta` 태그 추가 (ToolTip, ClampMin, ClampMax 등)

---

**작성일**: 2026-03-08  
**버전**: 1.1  
**상태**: 완료 (optional/oneof TOptional 지원 추가)

---

## 13. Optional / Oneof → TOptional 마샬링

Proto3의 `optional` 키워드 필드와 `oneof` 블록 내 필드는 모두 Unreal Engine의 `TOptional<T>`로 래핑됩니다. 마샬링 시 protoc이 생성하는 `has_xxx()` 메서드를 사용하여 값의 존재 여부를 확인합니다.

### 13.1 적용 규칙

| Proto3 구문 | Unreal C++ 타입 | has_xxx() 생성 | 비고 |
|------------|----------------|---------------|------|
| `optional int32 x = 1;` | `TOptional<int32>` | O | 모든 타입에 has 생성 |
| `optional string x = 1;` | `TOptional<FString>` | O | |
| `optional bytes x = 1;` | `TOptional<TArray<uint8>>` | O | MoveTemp 패턴 사용 |
| `optional Foo x = 1;` | `TOptional<FFooProto>` | O | 메시지 타입 |
| `optional Bar x = 1;` | `TOptional<EBarProto>` | O | enum 타입 |
| `oneof { int32 x; }` | `TOptional<int32>` | O | 각 필드가 개별 TOptional |
| `oneof { Foo x; }` | `TOptional<FFooProto>` | O | |

**주의**: `repeated` + `optional` 조합은 Proto3에서 허용되지 않으므로 TOptional 래핑이 적용되지 않습니다.

### 13.2 Optional 필드 예제

```protobuf
// Proto3
syntax = "proto3";
message Character {
    int32 id = 1;
    string name = 2;
    optional string guild_name = 3;
    optional int32 party_id = 4;
    optional bool is_premium = 5;
}
```

```cpp
// MECharacterProto.h
USTRUCT(BlueprintType)
struct FCharacterProto
{
    GENERATED_BODY()

    FCharacterProto() = default;
    explicit FCharacterProto(const ::Character& proto);

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    FString Name;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<FString> GuildName;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<int32> PartyId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<bool> IsPremium;
};
```

```cpp
// MECharacterProto.cpp
#include "MECharacterProto.h"
#include "character.pb.h"

FCharacterProto::FCharacterProto(const ::Character& proto)
{
    Id = proto.id();
    Name = FString(UTF8_TO_TCHAR(proto.name().c_str()));
    if (proto.has_guild_name())
    {
        GuildName = FString(UTF8_TO_TCHAR(proto.guild_name().c_str()));
    }
    if (proto.has_party_id())
    {
        PartyId = proto.party_id();
    }
    if (proto.has_is_premium())
    {
        IsPremium = proto.is_premium();
    }
}
```

### 13.3 Oneof 필드 예제

`oneof` 블록 내의 각 필드는 개별 `TOptional<T>`로 생성됩니다. protoc이 상호 배타성을 보장하므로, 런타임에는 최대 하나의 필드만 값이 설정됩니다.

```protobuf
// Proto3
syntax = "proto3";
message CombatLogEntry {
    int64 entry_id = 1;
    oneof event {
        DamageInfo damage = 2;
        SkillCastEvent skill_cast = 3;
        HealInfo heal = 4;
        string description = 5;
    }
}
```

```cpp
// MECombatLogEntryProto.h
USTRUCT(BlueprintType)
struct FCombatLogEntryProto
{
    GENERATED_BODY()

    FCharacterProto() = default;
    explicit FCombatLogEntryProto(const ::CombatLogEntry& proto);

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    int64 EntryId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<FDamageInfoProto> Damage;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<FSkillCastEventProto> SkillCast;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<FHealInfoProto> Heal;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Proto")
    TOptional<FString> Description;
};
```

```cpp
// MECombatLogEntryProto.cpp
#include "MECombatLogEntryProto.h"
#include "combat_log.pb.h"

FCombatLogEntryProto::FCombatLogEntryProto(const ::CombatLogEntry& proto)
{
    EntryId = proto.entry_id();
    if (proto.has_damage())
    {
        Damage = FDamageInfoProto(proto.damage());
    }
    if (proto.has_skill_cast())
    {
        SkillCast = FSkillCastEventProto(proto.skill_cast());
    }
    if (proto.has_heal())
    {
        Heal = FHealInfoProto(proto.heal());
    }
    if (proto.has_description())
    {
        Description = FString(UTF8_TO_TCHAR(proto.description().c_str()));
    }
}
```

### 13.4 타입별 마샬링 패턴

| 필드 타입 | optional/oneof 마샬링 패턴 |
|---|---|
| primitive (int32, float, bool 등) | `if (proto.has_xxx()) { Field = proto.xxx(); }` |
| string | `if (proto.has_xxx()) { Field = FString(UTF8_TO_TCHAR(proto.xxx().c_str())); }` |
| bytes | `if (proto.has_xxx()) { ... _temp_ + SetNum + Memcpy + MoveTemp }` |
| enum | `if (proto.has_xxx()) { Field = static_cast<EXxxProto>(proto.xxx()); }` |
| message | `if (proto.has_xxx()) { Field = FXxxProto(proto.xxx()); }` |

#### Optional Bytes 마샬링 상세

bytes 필드는 임시 변수를 사용하여 `MoveTemp`로 `TOptional`에 할당합니다:

```cpp
// optional bytes thumbnail = 1;
if (proto.has_thumbnail())
{
    const std::string& _bytes_thumbnail = proto.thumbnail();
    TArray<uint8> _temp_thumbnail;
    _temp_thumbnail.SetNum(_bytes_thumbnail.size());
    FMemory::Memcpy(_temp_thumbnail.GetData(), _bytes_thumbnail.data(), _bytes_thumbnail.size());
    Thumbnail = MoveTemp(_temp_thumbnail);
}
```

### 13.5 Unreal Engine에서 사용

```cpp
// TOptional 값 접근
FCharacterProto character(protoData);

// 값 존재 여부 확인
if (character.GuildName.IsSet())
{
    UE_LOG(LogTemp, Log, TEXT("Guild: %s"), *character.GuildName.GetValue());
}

// oneof - 최대 하나만 설정됨
FCombatLogEntryProto entry(combatProto);
if (entry.Damage.IsSet())
{
    // 데미지 이벤트 처리
}
else if (entry.Heal.IsSet())
{
    // 힐 이벤트 처리
}
```
