# SPEC-S-077: SaveSystem 슬롯 데이터 무결성 검증 — 로드 시 필수 필드 누락 체크 + 기본값 폴백

> **Priority:** P2
> **Tag:** Enhancement (Stabilize)
> **Depends on:** S-042 (SaveSystem 동시 저장 경합 방지)

## 요약

`SaveSystem.Load()` → `SaveData` 역직렬화 후 필수 필드 누락/이상값에 대한 검증이 없다. JSON에서 필드가 빠지거나 비정상 값(level=0, hp=-1 등)이면 그대로 게임에 반영되어 플레이 불가 상태가 된다. 로드 직후 `SaveData.Validate()` 메서드로 필수 필드를 검증하고 안전한 기본값으로 폴백하는 방어 계층을 추가한다.

## 현재 상태

### SaveSystem.TryLoadFrom() (SaveSystem.cs:90-146)

```csharp
var result = data.ToObject<SaveData>();   // line 133
if (result == null)                        // line 134
{
    Debug.LogError(...);                   // line 136
    return null;                           // line 137
}
return result;                             // line 139
```

역직렬화 성공 여부(`null` 체크)만 확인한다. `result.level == 0`이든 `result.hp == -999`이든 그대로 반환한다.

### SaveController.Load() (SaveController.cs:30-68)

```csharp
var save = SaveSystem.Load();              // line 34
if (save == null) return;                  // line 35

player.transform.position = new Vector3(save.playerX, save.playerY, 0);  // line 37
state.Level = save.level;                  // line 38 — level=0이면 그대로 반영
state.Hp = save.hp;                        // line 43 — hp=0이면 사망 상태로 시작
state.Mp = save.mp;                        // line 44
```

일부 nullable 필드(`inventory`, `equipment`, `learnedSkills`, `npcBrains`, `questState`, `killCounts`, `bonusStats`)에 대해서는 `null` 체크가 있다(line 46-61). 그러나 값 타입 필드(`level`, `hp`, `mp`, `gold` 등)는 검증 없이 직접 대입한다.

### SaveData 구조 (StatTypes.cs:129-145)

모든 필드가 C# 기본값(int=0, float=0f, reference=null)으로 초기화된다. `level` 필드에 JSON 기본값이나 `[DefaultValue]` 어트리뷰트가 없어서, JSON에 `level` 키가 없으면 `level=0`으로 역직렬화된다.

### 위험 시나리오

| 시나리오 | 원인 | 결과 |
|----------|------|------|
| 오래된 세이브 파일 (필드 추가 전) | 마이그레이션 누락 | 새 필드가 기본값(0/null)으로 로드 |
| 수동 편집/치트 엔진 | 비정상 값 삽입 | `level=-1`, `hp=999999` 등 |
| 디스크 부분 손상 | JSON 일부 필드 소실 | 체크섬은 통과하나(파일 자체는 유효) 데이터 불완전 |
| 버전 마이그레이션 버그 | `SaveMigrations.Apply()`가 필드 누락 | 특정 필드만 0/null |

## 수정 방안

### 1. SaveData에 Validate() 메서드 추가 (StatTypes.cs)

```csharp
[Serializable]
public class SaveData
{
    // ... existing fields ...

    /// <summary>
    /// Validates and repairs loaded data. Returns list of applied fixes for logging.
    /// </summary>
    public List<string> Validate()
    {
        var fixes = new List<string>();

        // --- Value-type required fields ---
        if (level < 1)
        {
            fixes.Add($"level {level} -> 1");
            level = 1;
        }

        if (hp <= 0)
        {
            fixes.Add($"hp {hp} -> 50 (fallback)");
            hp = 50;
        }

        if (mp < 0)
        {
            fixes.Add($"mp {mp} -> 0");
            mp = 0;
        }

        if (gold < 0)
        {
            fixes.Add($"gold {gold} -> 0");
            gold = 0;
        }

        if (skillPoints < 0)
        {
            fixes.Add($"skillPoints {skillPoints} -> 0");
            skillPoints = 0;
        }

        if (statPoints < 0)
        {
            fixes.Add($"statPoints {statPoints} -> 0");
            statPoints = 0;
        }

        if (totalKills < 0)
        {
            fixes.Add($"totalKills {totalKills} -> 0");
            totalKills = 0;
        }

        // --- Reference-type fields: null -> safe default ---
        if (inventory == null)
        {
            fixes.Add("inventory null -> empty array");
            inventory = new ItemInstance[0];
        }

        if (equipment == null)
        {
            fixes.Add("equipment null -> default slots");
            equipment = new Dictionary<string, ItemInstance>
            {
                ["weapon"] = null, ["helmet"] = null,
                ["armor"] = null, ["boots"] = null, ["accessory"] = null
            };
        }

        if (learnedSkills == null)
        {
            fixes.Add("learnedSkills null -> empty dict");
            learnedSkills = new Dictionary<string, int>();
        }

        if (equippedSkills == null)
        {
            fixes.Add("equippedSkills null -> empty array");
            equippedSkills = new string[0];
        }

        if (killCounts == null)
        {
            fixes.Add("killCounts null -> empty dict");
            killCounts = new Dictionary<string, int>();
        }

        if (bonusStats == null)
        {
            fixes.Add("bonusStats null -> default zeros");
            bonusStats = new Dictionary<string, int>
            {
                ["str"] = 0, ["dex"] = 0, ["wis"] = 0, ["luc"] = 0
            };
        }

        // questState null is acceptable (new game) — no fix needed
        // npcBrains null is acceptable — no fix needed
        // firedTriggers null is acceptable — no fix needed

        return fixes;
    }
}
```

### 2. SaveSystem.TryLoadFrom()에 Validate() 호출 삽입 (SaveSystem.cs)

`data.ToObject<SaveData>()` 직후, `return result` 이전에 검증을 실행한다.

```csharp
// SaveSystem.cs TryLoadFrom(), after line 133
var result = data.ToObject<SaveData>();
if (result == null)
{
    Debug.LogError($"[SaveSystem] {path}: deserialized to null");
    return null;
}

var fixes = result.Validate();
if (fixes.Count > 0)
{
    Debug.LogWarning($"[SaveSystem] {path}: applied {fixes.Count} validation fix(es):\n  " +
                     string.Join("\n  ", fixes));
}

return result;
```

### 3. SaveController.Load()의 기존 null 체크 단순화 (SaveController.cs)

`Validate()`가 null 필드를 이미 안전한 기본값으로 채우므로, `SaveController.Load()`의 `if (save.inventory != null)`, `if (save.equipment != null)` 등의 가드를 제거하거나 유지한다. 방어적 프로그래밍 관점에서 **기존 null 체크는 유지**하되, 주석으로 `Validate()`가 이미 처리함을 표기한다.

```csharp
// SaveController.cs line 47 — Validate() already ensures non-null, kept as defensive check
if (save.inventory != null)
{
    ...
}
```

## 연동 경로

```
[게임 시작]
  GameManager.Start() (line 116)
    -> LoadGame() (line 322)
      -> SaveController.Load() (line 30)
        -> SaveSystem.Load() (line 69)
          -> TryLoadFrom() (line 90)
            -> JObject.ToObject<SaveData>() (line 133)
            -> ★ SaveData.Validate()  <-- 여기에 삽입
          -> return SaveData
        -> SaveController가 PlayerStats/Inventory 등에 반영

[일시정지 메뉴 / 자동 저장]
  SaveGame() (line 316)
    -> SaveController.Save() (line 6)
      -> SaveSystem.Save() (line 20)
```

로드는 `TryLoadFrom()` 한 곳에서만 일어나므로, 검증 로직 삽입 지점이 단일하다.

## 데이터 구조

### SaveData 필드 목록 및 기본값 폴백 규칙

| 필드 | 타입 | C# 기본값 | 게임 기본값 (폴백) | 검증 규칙 |
|------|------|-----------|-------------------|-----------|
| `playerX` | float | 0f | 0f (허용) | 검증 불요 — 좌표 0,0은 유효 |
| `playerY` | float | 0f | 0f (허용) | 검증 불요 |
| `level` | int | 0 | **1** | `< 1` -> 1 |
| `xp` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `gold` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `skillPoints` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `statPoints` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `hp` | int | 0 | **50** | `<= 0` -> 50 (사망 상태 방지) |
| `mp` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `inventory` | ItemInstance[] | null | `new ItemInstance[0]` | null -> 빈 배열 |
| `equipment` | Dict | null | 5슬롯 null dict | null -> 기본 슬롯 |
| `learnedSkills` | Dict | null | `new Dict()` | null -> 빈 dict |
| `equippedSkills` | string[] | null | `new string[0]` | null -> 빈 배열 |
| `npcBrains` | Dict | null | null (허용) | 검증 불요 — 새 게임과 동일 |
| `questState` | QuestSaveData | null | null (허용) | 검증 불요 |
| `killCounts` | Dict | null | `new Dict()` | null -> 빈 dict |
| `totalKills` | int | 0 | 0 (허용) | `< 0` -> 0 |
| `firedTriggers` | string[] | null | null (허용) | 검증 불요 |
| `bonusStats` | Dict | null | `{str:0,dex:0,wis:0,luc:0}` | null -> 기본 4키 |
| `timestamp` | long | 0 | 0 (허용) | 검증 불요 — 로드 시 참고용 |

### hp 폴백값 50 근거

`StatsSystem.ComputeBaseStats(level=1, ...)` 기준 `maxHp`가 약 50-100 범위. 정확한 값은 `RecalcStats()` 후 `Mathf.Min(hp, maxHp)`으로 클램프되므로, 50은 안전한 초기값이다. `SaveController.Load()` line 64에서 `state.RecalcStats()`가 호출되어 최종 보정이 일어난다.

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Data/StatTypes.cs` | `SaveData.Validate()` 메서드 추가 (~50줄) |
| `Assets/Scripts/Systems/SaveSystem.cs` | `TryLoadFrom()`에 `Validate()` 호출 + 경고 로그 삽입 (line 133 이후, ~6줄) |
| `Assets/Tests/EditMode/SaveSystemTests.cs` | 무결성 검증 테스트 추가 |

## 테스트 방안

### Edit Mode Unit Tests (SaveSystemTests.cs에 추가)

```csharp
[Test]
public void Validate_FixesZeroLevel()
{
    var data = new SaveData { level = 0 };
    var fixes = data.Validate();
    Assert.AreEqual(1, data.level);
    Assert.IsTrue(fixes.Count > 0);
}

[Test]
public void Validate_FixesNegativeGold()
{
    var data = new SaveData { level = 1, gold = -100 };
    var fixes = data.Validate();
    Assert.AreEqual(0, data.gold);
}

[Test]
public void Validate_FixesZeroHp()
{
    var data = new SaveData { level = 1, hp = 0 };
    var fixes = data.Validate();
    Assert.AreEqual(50, data.hp);
}

[Test]
public void Validate_PreservesValidData()
{
    var data = new SaveData
    {
        level = 10, hp = 200, mp = 50, gold = 500,
        inventory = new ItemInstance[5],
        equipment = new Dictionary<string, ItemInstance>()
    };
    var fixes = data.Validate();
    Assert.AreEqual(0, fixes.Count);
    Assert.AreEqual(10, data.level);
    Assert.AreEqual(200, data.hp);
}

[Test]
public void Validate_FillsNullCollections()
{
    var data = new SaveData
    {
        level = 1, hp = 100,
        inventory = null, equipment = null,
        learnedSkills = null, bonusStats = null
    };
    var fixes = data.Validate();
    Assert.IsNotNull(data.inventory);
    Assert.IsNotNull(data.equipment);
    Assert.IsNotNull(data.learnedSkills);
    Assert.IsNotNull(data.bonusStats);
    Assert.IsTrue(fixes.Count >= 4);
}

[Test]
public void Load_AppliesValidation_ToCorruptedValues()
{
    // Save valid data, then manually tamper JSON to set level=0
    SaveSystem.Save(new SaveData { level = 5, hp = 100 });
    string json = File.ReadAllText(_tempPath);
    var root = JObject.Parse(json);
    var dataObj = (JObject)root["data"];
    dataObj["level"] = 0;
    dataObj["hp"] = -10;
    // Recompute checksum for tampered data
    string dataJson = dataObj.ToString(Formatting.None);
    using var sha = SHA256.Create();
    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dataJson));
    var sb = new StringBuilder(hash.Length * 2);
    foreach (byte b in hash) sb.Append(b.ToString("x2"));
    root["checksum"] = sb.ToString();
    File.WriteAllText(_tempPath, root.ToString());

    var loaded = SaveSystem.Load();
    Assert.IsNotNull(loaded);
    Assert.AreEqual(1, loaded.level);   // fixed from 0
    Assert.AreEqual(50, loaded.hp);     // fixed from -10
}
```

### Play Mode / 수동 테스트

1. 세이브 파일(`rpg_save.json`)을 열어 `level`을 0으로 수동 변경 -> 로드 시 `level=1`로 보정되는지 확인 (콘솔 경고 로그)
2. `hp`를 음수로 변경 -> 로드 시 `hp=50`으로 보정 + `RecalcStats()` 후 정상 클램프 확인
3. `inventory`, `equipment` 키를 JSON에서 삭제 -> 로드 시 빈 기본값으로 정상 진행
4. 정상 세이브 파일 -> 로드 시 경고 로그 없음 (regression 확인)

## Acceptance Criteria

1. `SaveData.Validate()`가 `level < 1`, `hp <= 0`, `gold < 0` 등 비정상 값을 안전한 기본값으로 보정
2. null인 컬렉션 필드(`inventory`, `equipment`, `learnedSkills`, `equippedSkills`, `killCounts`, `bonusStats`)가 빈 기본값으로 채워짐
3. 보정이 발생하면 `[SaveSystem]` 태그로 경고 로그 출력 (보정 항목 명시)
4. 정상 데이터 로드 시 `Validate()`가 아무것도 변경하지 않음 (fixes.Count == 0)
5. 기존 save/load roundtrip 테스트 전체 통과 (regression 없음)
6. `TryLoadFrom()` 단일 지점에서만 검증이 실행되어 중복 검증 없음

## 세이브 연동: 직접 관련

- `SaveSystem.TryLoadFrom()` 내부에 검증 삽입 — 로드 경로의 유일한 출구점
- `SaveData.Validate()` — 데이터 클래스에 자기 검증 책임 부여 (SRP 유지)
- `SaveController.Load()` — 기존 null 체크 유지, Validate()와 이중 방어
- `SaveMigrations.Apply()` — 마이그레이션 후 Validate()가 실행되므로 마이그레이션이 필드를 누락해도 안전
