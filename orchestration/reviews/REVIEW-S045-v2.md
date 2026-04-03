# REVIEW-S045-v2: QuestSystem killProgress Save/Restore

> **리뷰 일시:** 2026-04-03
> **태스크:** S-045 QuestSystem killProgress 저장/복원
> **스펙:** SPEC-S-045
> **v1 판정:** NEEDS_WORK (테스트 누락)
> **v2 판정:** ✅ APPROVE

---

## 변경 요약

**변경 파일 (4개):**

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/QuestSystem.cs` | `Serialize()` 반환값에 killProgress 추가 (3-tuple); `Restore()` 파라미터에 killProgress 추가 (null 가드 + 딥카피) |
| `Assets/Scripts/Core/SaveController.cs` | `Save()` 3-tuple 분해 → `QuestSaveData.killProgress`에 저장; `Load()` killProgress 전달 |
| `Assets/Scripts/Data/StatTypes.cs` | `QuestSaveData`에 `Dictionary<string, Dictionary<string, int>> killProgress` 필드 추가 |
| `Assets/Tests/EditMode/QuestSystemTests.cs` | **[v2 신규]** `SerializeRestore_PreservesKillProgress` + `Restore_NullKillProgress_DoesNotCrash` 테스트 2건 추가 |

**범위:** 직렬화/역직렬화 경로 변경 + 핵심 동작 테스트 추가. 게임플레이 로직 변경 없음.

---

## v1 지적사항 해소 확인

| v1 필수 조치 | v2 상태 | 비고 |
|-------------|---------|------|
| 1. 킬 진행률 serialize/restore 왕복 테스트 | ✅ 해소 | `SerializeRestore_PreservesKillProgress` (line 175-195) |
| 2. null killProgress restore 시 크래시 없음 테스트 | ✅ 해소 | `Restore_NullKillProgress_DoesNotCrash` (line 197-208) |
| 3. (선택) SaveSystemTests killProgress 포함 | ⚠️ 미적용 | 선택사항이므로 차단 요인 아님 |

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 순수 C# 직렬화 로직 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

- `QuestSaveData`에 `[Serializable]` 어트리뷰트 확인됨. Newtonsoft.Json의 `Dictionary<string, Dictionary<string, int>>` 네이티브 직렬화 지원.
- 기존 세이브 파일에 `killProgress` 필드 없으면 `null`로 역직렬화 (Newtonsoft.Json 표준 동작).

## 검증 2: 코드 추적

### Serialize() — killProgress 반환 (QuestSystem.cs:153-154)

```csharp
public (string[] active, string[] completed, Dictionary<string, Dictionary<string, int>> killProgress) Serialize() =>
    (_active.Keys.ToArray(), _completed.ToArray(), new Dictionary<string, Dictionary<string, int>>(_killProgress));
```

- 외부 Dictionary 방어적 복사. 내부 Dictionary는 공유 참조이나 즉시 JSON 직렬화되므로 mutation 위험 없음. **PASS**

### Restore() — killProgress 복원 (QuestSystem.cs:156-172)

```csharp
_killProgress.Clear();
if (data.killProgress != null)
    foreach (var kvp in data.killProgress)
        _killProgress[kvp.Key] = new Dictionary<string, int>(kvp.Value);
```

- `_killProgress.Clear()` 후 복원 — stale 데이터 방지. **PASS**
- null 가드 — 하위 호환 (구 세이브 로드 시 크래시 없음). **PASS**
- 내부 Dictionary 딥카피 — 역직렬화된 데이터와 분리. **PASS**

### SaveController.Save() (SaveController.cs:10-27)

```csharp
var (active, completed, killProgress) = quests.Serialize();
...
questState = new QuestSaveData { active = active, completed = completed, killProgress = killProgress },
```

- 3-tuple 분해 → `QuestSaveData` 생성. killProgress 포함. **PASS**

### SaveController.Load() (SaveController.cs:57)

```csharp
if (save.questState != null) quests.Restore((save.questState.active, save.questState.completed, save.questState.killProgress));
```

- `questState` null 체크 유지. `killProgress`가 null이면 Restore 내부 null 가드에서 처리. **PASS**

### QuestSaveData 필드 (StatTypes.cs:147-153)

```csharp
[Serializable]
public class QuestSaveData
{
    public string[] active;
    public string[] completed;
    public Dictionary<string, Dictionary<string, int>> killProgress;
}
```

- `[Serializable]` 어트리뷰트 존재. Newtonsoft.Json 호환. **PASS**

### 하위 호환 경로

- 구 세이브 JSON에 `killProgress` 필드 없음 → 역직렬화 시 `null` → `Restore()` null 가드 → `_killProgress` 빈 dict 유지 → 크래시 없음. **PASS**

### 완료된 퀘스트 killProgress 제거 (QuestSystem.cs:61)

- `CompleteQuest()`에서 `_killProgress.Remove(questId)` 호출 (S-045 이전부터 존재). 메모리 누수 없음. **PASS**

### 테스트 커버리지 (v2 신규)

#### `SerializeRestore_PreservesKillProgress` (line 175-195)

- kill 퀘스트 설정 → accept → wolf 2마리 킬 이벤트 → Serialize
- 직렬화 데이터 검증: `data.killProgress["kill_wolves"]["wolf"] == 2`
- 새 QuestSystem 생성 → Restore → `GetKillProgress("kill_wolves", "wolf") == 2` 검증
- active 퀘스트 복원도 동시 검증: `GetActiveQuests().Length == 1`
- **왕복 (roundtrip) 테스트 완성. PASS**

#### `Restore_NullKillProgress_DoesNotCrash` (line 197-208)

- killProgress에 명시적 `null` 전달하여 ValueTuple 생성
- Restore 호출 → 크래시 없음 확인
- active 퀘스트 정상 복원 검증 (1개)
- `GetKillProgress` 미존재 퀘스트 → 0 반환 검증
- **null 안전성 테스트 완성. PASS**

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| HUD 퀘스트 트래커 | ✅ | `GetKillProgress()` → `_killProgress` 읽기 — 로드 후 정확한 값 표시 |
| 패널 열기/닫기 | N/A | UI 변경 없음 |
| 데이터 바인딩 | ✅ | 기존 바인딩 경로 유지 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 킬 퀘스트 진행 중 저장 → 로드 | 킬 카운트 유지 (예: 늑대 2/3 → 로드 후 2/3) |
| 구 세이브(killProgress 없음) 로드 | 크래시 없음, 킬 카운트 0으로 시작 |
| 퀘스트 완료 후 저장 → 로드 | killProgress에서 완료 퀘스트 제거됨 (메모리 누수 없음) |
| 복수 킬 퀘스트 동시 진행 | 각 퀘스트 독립적으로 진행률 유지 |
| 저장 → 추가 킬 → 로드 | 저장 시점 카운트로 정확히 복원 (추가 킬 무시) |

---

## 페르소나 리뷰

### 캐주얼 게이머
"늑대 잡은 거 저장하고 나중에 다시 들어오면 그대로 남아있다고? 당연히 그래야지. 전에는 리셋돼서 처음부터 다시 잡아야 했는데, 이제 정상적으로 동작한다. 아무 문제 없다."

### 코어 게이머
"50마리 킬 퀘스트 중 45마리 잡고 저장했는데 복원되면 당연히 45에서 이어져야 한다. Dictionary-of-Dictionaries 구조라 복수 퀘스트도 독립적으로 관리되고, 왕복 테스트도 있으니 리팩토링 시에도 안전하다. v1에서 지적한 테스트가 추가됐으니 만족."

### UX/UI 디자이너
"UI 변경 없음. HUD 퀘스트 트래커가 `GetKillProgress()` 사용 중이고, 로드 후에도 정확한 진행률 표시. 사용자가 기대하는 '진행률은 저장됨' 멘탈 모델과 실제 동작이 일치."

### QA 엔지니어

| # | 체크 항목 | 결과 | 비고 |
|---|----------|------|------|
| 1 | Serialize()에 killProgress 포함 | ✅ | 방어적 복사 |
| 2 | Restore()에서 killProgress 복원 | ✅ | null 가드 + 딥카피 |
| 3 | SaveController.Save() 전달 | ✅ | 3-tuple 분해 |
| 4 | SaveController.Load() 전달 | ✅ | questState.killProgress |
| 5 | QuestSaveData 필드 추가 | ✅ | StatTypes.cs |
| 6 | 하위 호환 (null → 빈 dict) | ✅ | Restore() null 가드 |
| 7 | 완료 퀘스트 killProgress 제거 | ✅ | CompleteQuest() 기존 코드 |
| 8 | JSON 왕복 (중첩 Dictionary) | ✅ | Newtonsoft.Json 네이티브 지원 |
| 9 | killProgress 왕복 단위 테스트 | ✅ | **v2에서 추가됨** — `SerializeRestore_PreservesKillProgress` |
| 10 | null killProgress 안전 테스트 | ✅ | **v2에서 추가됨** — `Restore_NullKillProgress_DoesNotCrash` |
| 11 | SaveSystemTests killProgress 포함 | ⚠️ | 선택사항 — 미적용이나 차단 아님 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 스펙 전 항목 충족 |
| 기존 호환성 | ✅ null 폴백으로 구 세이브 호환 |
| 코드 품질 | ✅ 최소 변경, 방어적 프로그래밍, 딥카피 |
| 아키텍처 | ✅ 기존 패턴 유지 (ValueTuple 확장) |
| 테스트 커버리지 | ✅ 핵심 왕복 + null 안전 테스트 추가 |
| v1 지적사항 해소 | ✅ 필수 2건 모두 해소 |

**결론:** ✅ **APPROVE** — v1에서 정확하고 최소한으로 구현된 코드에 v2에서 핵심 테스트 2건이 추가됨. `SerializeRestore_PreservesKillProgress`는 킬 이벤트 발생 → 직렬화 → 새 인스턴스에 복원 → 진행률 유지를 완전히 검증. `Restore_NullKillProgress_DoesNotCrash`는 구 세이브 하위 호환을 검증. 선택사항인 SaveSystemTests killProgress 포함은 미적용이나 기능 안전성에 영향 없음. 승인.
