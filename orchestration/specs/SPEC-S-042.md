# SPEC-S-042: SaveSystem 동시 저장 경합 방지

> **Priority:** P1
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## 목적

`SaveSystem.Save()`는 auto-save (RegionVisitEvent 기반)와 manual save (PauseMenuUI 저장 버튼) 양쪽에서 호출된다. 두 경로 모두 `EventBus.Emit(new SaveEvent())`를 통해 `GameManager.SaveGame() -> SaveSystem.Save()`로 진입하며, 동일 프레임 또는 연속 프레임에서 동시에 트리거되면 다음 문제가 발생할 수 있다:

1. `RotateBackups()`가 두 번 실행되어 백업 슬롯이 하나 건너뛰어짐
2. 첫 번째 `File.WriteAllText()`가 완료되기 전에 두 번째가 시작되면 파일 손상
3. `SteamCloudStorage.SaveToCloud()`가 중복 호출되어 불필요한 네트워크 비용 발생

현재 `SaveSystem`는 `static` 클래스이며 어떤 형태의 재진입 방지(lock, `_isSaving` 가드)도 없다.

## 현재 상태

### 호출 경로 1: Auto-Save (RegionVisitEvent)

```
GameManager.Update() -> RegionTracker.UpdatePlayerRegion()
  -> EventBus.Emit(RegionVisitEvent)
  -> GameManager (listener, line ~753)
    -> 30s throttle check (Time.time - _lastAutoSaveTime < 30f)
    -> EventBus.Emit(new SaveEvent())
    -> GameManager (listener, line ~775) -> SaveGame() -> SaveSystem.Save()
```

30초 throttle이 있지만 `SaveEvent` 자체에 대한 중복 방지는 없다.

### 호출 경로 2: Manual Save (PauseMenuUI)

```
PauseMenuUI.DoSave()
  -> OnSaveRequested?.Invoke()
  -> GameManager (line ~704): pm.OnSaveRequested = () => EventBus.Emit(new SaveEvent())
  -> GameManager (listener, line ~775) -> SaveGame() -> SaveSystem.Save()
```

### 경합 시나리오

플레이어가 지역 경계를 넘는 동시에 ESC -> Save 버튼을 누르면:
- `RegionVisitEvent` listener와 `PauseMenuUI.DoSave()` 모두 같은 프레임에서 `SaveEvent`를 emit
- `SaveEvent` listener가 두 번 실행 -> `SaveSystem.Save()` 두 번 호출
- `RotateBackups()`에서 `File.Move()` / `File.Copy()` 경합 가능

### SaveSystem.Save() 코드 (현재)

```csharp
public static void Save(SaveData data)
{
    try
    {
        data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // ... serialize, checksum ...
        RotateBackups();           // <-- File.Delete, File.Move, File.Copy
        File.WriteAllText(SavePath, json);  // <-- Write
        SteamCloudStorage.SaveToCloud(...)  // <-- Network
    }
    catch (Exception e) { ... }
}
```

재진입 가드 없음. `RotateBackups()`와 `File.WriteAllText()` 사이에 다른 `Save()` 호출이 끼어들 수 있다.

## 검증 항목

- [ ] `SaveSystem.Save()`에 `_isSaving` static bool 가드가 존재하는지 확인
- [ ] 가드가 없으면 `Save()` 진입 시 `_isSaving == true`이면 즉시 리턴 + 로그 출력
- [ ] `Save()` 완료 후 (정상/예외 모두) `_isSaving`이 `false`로 복원되는지 확인 (`try/finally`)
- [ ] `GameManager.SaveGame()`에서 `SaveSystem.Save()` 호출 전 추가 throttle이 있는지 확인
- [ ] Auto-save 30초 throttle이 `SaveEvent` emit이 아닌 `SaveSystem.Save()` 진입 기준인지 확인
- [ ] 수동 저장 직후 자동 저장이 무시되는지 (또는 그 반대) 확인

## 수정 방안 (필요 시)

### 1. SaveSystem에 `_isSaving` 가드 추가

```csharp
public static class SaveSystem
{
    static bool _isSaving;

    public static void Save(SaveData data)
    {
        if (_isSaving)
        {
            Debug.LogWarning("[SaveSystem] Save already in progress, skipping");
            return;
        }
        _isSaving = true;
        try
        {
            // ... existing logic ...
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }
}
```

### 2. GameManager 측 throttle 강화

`SaveGame()`에 자체 throttle 추가 (auto-save/manual 구분 없이):

```csharp
void SaveGame()
{
    float now = Time.time;
    if (now - _lastSaveTime < 1f)
    {
        Debug.Log("[GameManager] Save throttled (< 1s since last save)");
        return;
    }
    _lastSaveTime = now;
    // ... existing save logic ...
}
```

### 3. `_lastAutoSaveTime` 갱신 위치 수정

현재 `_lastAutoSaveTime`은 `RegionVisitEvent` listener 안에서만 갱신된다. Manual save도 이 타이머를 리셋해야 auto-save가 직후에 중복 트리거되지 않는다:

```csharp
void SaveGame()
{
    _lastAutoSaveTime = Time.time;  // Manual save also resets auto-save timer
    // ...
}
```

## 호출 진입점

| 트리거 | 경로 |
|--------|------|
| Auto-save | `RegionVisitEvent` -> `SaveEvent` -> `GameManager.SaveGame()` -> `SaveSystem.Save()` |
| Manual save | `PauseMenuUI.DoSave()` -> `SaveEvent` -> `GameManager.SaveGame()` -> `SaveSystem.Save()` |

## 연동 시스템

| 시스템 | 역할 |
|--------|------|
| `GameManager` | `SaveGame()` 호출자, `SaveData` 조립 |
| `SaveSystem` | 파일 I/O, 백업 로테이션, 체크섬 |
| `SteamCloudStorage` | 클라우드 동기화 |
| `PauseMenuUI` | Manual save 버튼 |
| `EventBus` | `SaveEvent` / `RegionVisitEvent` 전달 |
| `HUD` | Save indicator 표시 |

## 데이터 구조

### SaveData (Assets/Scripts/Data/StatTypes.cs)

```csharp
public class SaveData
{
    public float playerX, playerY;
    public int level, xp, gold, skillPoints, statPoints;
    public int hp, mp;
    public ItemInstance[] inventory;
    public Dictionary<string, ItemInstance> equipment;
    // ... etc
    public long timestamp;
}
```

### Save file format (rpg_save.json)

```json
{
    "version": 1,
    "checksum": "sha256...",
    "data": { ... SaveData ... }
}
```

Backup files: `rpg_save.bak1.json` ~ `rpg_save.bak3.json` (rotating)

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/SaveSystem.cs` | `_isSaving` 가드 추가 (`try/finally` 패턴) |
| `Assets/Scripts/Core/GameManager.cs` | `SaveGame()`에 최소 간격 throttle 추가, manual save 시 `_lastAutoSaveTime` 리셋 |

## Acceptance Criteria

1. `SaveSystem.Save()`가 이미 실행 중이면 두 번째 호출이 무시되고 경고 로그 출력
2. `Save()` 예외 발생 시 `_isSaving`이 반드시 `false`로 복원 (deadlock 방지)
3. Manual save 직후 30초 이내 auto-save가 스킵됨
4. Auto-save 직후 manual save는 정상 동작 (throttle 1초 이상 경과 시)
5. 저장 완료 후 HUD save indicator 정상 표시
6. 기존 save/load 기능에 regression 없음

## 테스트

### Edit Mode Unit Test

- `SaveSystem.Save()` 내부에서 `_isSaving`이 `true`인 상태를 시뮬레이션하고 두 번째 호출이 무시되는지 검증
- `Save()` 예외 후 `_isSaving`이 `false`로 복원되는지 검증

### Play Mode / 수동 테스트

1. 지역 이동 중 ESC -> Save 버튼 연타 -> 파일 손상 없음 확인
2. Auto-save 직후 (30초 throttle 내) manual save 시도 -> 정상 저장 확인
3. Manual save 직후 지역 이동 -> auto-save 스킵 확인 (로그로 검증)
