# SPEC-S-061: QuestSystem killProgress 고아 항목

> **태스크:** S-061
> **우선순위:** P2
> **태그:** 🔧

## 문제

`_killProgress` Dictionary에 더 이상 `_active`에 존재하지 않는 questId 키가 잔류할 수 있다.

발생 경로:

1. **포기(Abandon) 미구현:** 현재 QuestSystem에 AbandonQuest 메서드가 없다. 향후 포기 기능 추가 시 `_active.Remove(questId)`만 호출하고 `_killProgress.Remove(questId)`를 누락하면 고아 항목이 생긴다.
2. **Restore 무검증:** `Restore()`가 save 데이터의 killProgress를 그대로 복원한다 (line 169-171). active 목록에 없는 questId의 killProgress도 필터 없이 로드된다. 예: 세이브 파일 수동 편집, 퀘스트 정의 제거(패치), 또는 세이브 파일 손상 시 발생.
3. **Serialize 무필터:** `Serialize()`가 `_killProgress` 전체를 복사한다 (line 153-154). 고아 항목이 있으면 세이브에 영구 기록되어 로드마다 재잔류.

**영향:**
- 메모리 미미하지만 장기 플레이 시 축적 가능
- `GetKillProgress()`가 완료/제거된 퀘스트에 대해 비정상 값 반환 (혼동 가능)
- 세이브 파일 비대화 (killProgress 항목이 절대 줄지 않음)

## 목표

- `_killProgress`에 `_active`에 없는 questId가 존재하지 않도록 보장
- AbandonQuest 메서드 추가 시 killProgress 정리 포함
- Restore/Serialize 시 고아 항목 필터링

## 수치/제한

- `_killProgress.Keys`는 항상 `_active.Keys`의 부분집합이어야 함
- 성능: 정리 로직은 O(n) — killProgress 키 수 기준, 퀘스트 수가 적으므로 무시 가능

## 연동 경로

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Systems/QuestSystem.cs` | 핵심 수정 대상 |
| `Assets/Scripts/Core/SaveController.cs` | Save/Load 호출부 (변경 불필요) |
| `Assets/Scripts/Data/StatTypes.cs` | `QuestSaveData` 구조체 (변경 불필요) |
| `Assets/Tests/EditMode/QuestSystemTests.cs` | 테스트 추가 |

## 데이터 구조

```csharp
// QuestSystem.cs line 12
readonly Dictionary<string, Dictionary<string, int>> _killProgress = new();

// Key: questId (string)
// Value: Dictionary<monsterId, killCount>
//
// _active: Dictionary<string, QuestDef>  — 현재 진행 중인 퀘스트
// _completed: HashSet<string>            — 완료된 퀘스트
```

## 구현 방향

### 1. AbandonQuest 메서드 추가

```csharp
public bool AbandonQuest(string questId)
{
    if (!_active.Remove(questId)) return false;
    _killProgress.Remove(questId);
    EventBus.Emit(new QuestAbandonEvent { questId = questId });
    return true;
}
```

- `QuestAbandonEvent` 이벤트 클래스 신규 정의 필요 (StatTypes.cs 또는 별도 Events 파일)

### 2. Restore에 고아 필터 추가

`Restore()` 마지막에 killProgress 키 중 `_active`에 없는 것을 제거:

```csharp
// After restoring killProgress (line 171 이후)
var orphanKeys = new List<string>();
foreach (var key in _killProgress.Keys)
{
    if (!_active.ContainsKey(key))
        orphanKeys.Add(key);
}
foreach (var key in orphanKeys)
    _killProgress.Remove(key);
```

### 3. Serialize에 필터 적용 (방어적)

```csharp
public (...) Serialize()
{
    var filtered = new Dictionary<string, Dictionary<string, int>>();
    foreach (var kvp in _killProgress)
    {
        if (_active.ContainsKey(kvp.Key))
            filtered[kvp.Key] = new Dictionary<string, int>(kvp.Value);
    }
    return (_active.Keys.ToArray(), _completed.ToArray(), filtered);
}
```

## 호출 진입점

| 트리거 | 위치 |
|--------|------|
| 퀘스트 완료 | `CompleteQuest()` — 이미 `_killProgress.Remove()` 호출 중 (정상) |
| 퀘스트 포기 | `AbandonQuest()` — 신규 메서드, UI에서 호출 |
| 세이브 저장 | `Serialize()` — 필터링 적용 |
| 세이브 로드 | `Restore()` — 고아 항목 정리 |

## 세이브 연동

- S-045에서 killProgress 직렬화/복원을 추가함 (SPEC-S-045 참고)
- `QuestSaveData` 구조 변경 불필요 — 기존 `Dictionary<string, Dictionary<string, int>>` 그대로 사용
- Restore에서 필터링하므로 기존 세이브 파일의 고아 항목은 로드 시 자동 제거됨
- 필터링 후 재저장하면 세이브 파일에서도 영구 제거 (자연 정리)
- SaveData 버전 마이그레이션 불필요

## 검증 기준

- [ ] `AbandonQuest()` 호출 후 `_killProgress`에 해당 questId 키 없음
- [ ] `AbandonQuest()` 호출 후 `GetKillProgress()` 반환값 0
- [ ] Restore 시 active에 없는 killProgress 키가 자동 제거됨
- [ ] Serialize 시 active에 없는 killProgress 키가 포함되지 않음
- [ ] CompleteQuest 후 killProgress 정리 동작 변화 없음 (기존 동작 유지)
- [ ] 기존 QuestSystemTests 전체 통과
