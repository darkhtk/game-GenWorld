# REVIEW-S045-v1: QuestSystem killProgress Save/Restore

> **리뷰 일시:** 2026-04-03
> **태스크:** S-045 QuestSystem killProgress 저장/복원
> **스펙:** SPEC-S-045
> **커밋:** `1533ec2` — fix: S-045 QuestSystem killProgress 저장/복원
> **판정:** ❌ NEEDS_WORK

---

## 변경 요약

**변경 파일 (3개):**

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/QuestSystem.cs` | `Serialize()` 반환값에 killProgress 추가; `Restore()` 파라미터에 killProgress 추가 (null 가드 포함) |
| `Assets/Scripts/Core/SaveController.cs` | `Save()` 3-tuple 분해; `Load()` killProgress 전달 |
| `Assets/Scripts/Data/StatTypes.cs` | `QuestSaveData`에 `killProgress` 필드 추가 |

**범위:** 최소한의 정확한 수정. 직렬화/역직렬화 경로만 변경. 게임플레이 로직 변경 없음.

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 순수 C# 직렬화 로직 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

- `QuestSaveData`에 `[Serializable]` 이미 존재. Newtonsoft.Json은 `Dictionary<string, Dictionary<string, int>>`를 네이티브로 처리.
- 기존 세이브 파일에 필드 없으면 `null`로 역직렬화 (표준 동작).

## 검증 2: 코드 추적

### Serialize() — killProgress 반환 (QuestSystem.cs:153-154)

```csharp
public (string[] active, string[] completed, Dictionary<string, Dictionary<string, int>> killProgress) Serialize() =>
    (_active.Keys.ToArray(), _completed.ToArray(), new Dictionary<string, Dictionary<string, int>>(_killProgress));
```

- 외부 dict 방어적 복사. 내부 dict는 공유 참조이나 즉시 JSON 직렬화되므로 안전. **PASS**

### Restore() — killProgress 복원 (QuestSystem.cs:156-172)

```csharp
_killProgress.Clear();
if (data.killProgress != null)
    foreach (var kvp in data.killProgress)
        _killProgress[kvp.Key] = new Dictionary<string, int>(kvp.Value);
```

- `_killProgress.Clear()` 후 복원 — stale 데이터 방지. **PASS**
- null 가드 — 하위 호환. **PASS**
- 내부 dict 딥카피 — 방어적 프로그래밍. **PASS**

### SaveController Save/Load 경로

- Save: 3-tuple 분해 → `QuestSaveData.killProgress`에 저장. **PASS**
- Load: `save.questState.killProgress` → `Restore()`에 전달. 기존 null 체크 유지. **PASS**

### QuestSaveData 필드 추가 (StatTypes.cs:148-153)

- `Dictionary<string, Dictionary<string, int>> killProgress` 필드 추가. Newtonsoft.Json 호환. **PASS**

### 하위 호환: null killProgress → 빈 dict 폴백

- 구 세이브: JSON에 필드 없음 → `null` → `Restore()` null 가드 → `_killProgress` 빈 상태 유지. **PASS**

### 완료된 퀘스트 killProgress 제거 (메모리 누수 방지)

- `CompleteQuest()`에서 `_killProgress.Remove(questId)` 호출 (S-045 이전부터 존재). **PASS**

### 테스트 커버리지

- **문제 발견**: killProgress serialize/restore 왕복 테스트 없음.
  - `SerializeRestore_PreservesState` (QuestSystemTests.cs:72) — active만 검증, killProgress 미포함.
  - `SaveAndLoad_PreservesQuestState` (SaveSystemTests.cs:155) — killProgress 미포함.
- **❌ NEEDS_WORK** — 핵심 변경에 대한 자동화 테스트 누락.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| HUD 퀘스트 트래커 | ✅ | `GetKillProgress()` → `_killProgress` 읽기 — 로드 후 정확한 값 표시 |
| 패널 열기/닫기 | N/A | UI 변경 없음 |
| 데이터 바인딩 | ✅ | 기존 바인딩 경로 유지 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 킬 퀘스트 진행 중 저장→로드 | 킬 카운트 유지 (예: 늑대 2/3 → 로드 후 2/3) |
| 구 세이브(killProgress 없음) 로드 | 크래시 없음, 킬 카운트 0으로 시작 |
| 퀘스트 완료 후 저장→로드 | killProgress에서 완료 퀘스트 제거됨 (메모리 누수 없음) |
| 복수 킬 퀘스트 동시 진행 | 각 퀘스트 독립적으로 진행률 유지 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"저장하고 다시 들어오면 늑대 잡은 거 리셋되는 거 너무 짜증났는데 이제 해결됐다고? 기본적인 건데 늦었지만 다행이다."

### ⚔️ 코어 게이머
"킬 퀘스트 50마리 중 40마리 잡고 저장했는데 리셋되면 게임 접는다. 코드 보니까 Dictionary-of-Dictionaries 구조로 복수 퀘스트도 문제없어 보인다. 근데 테스트가 없으면 나중에 리팩토링할 때 깨질 수 있다."

### 🎨 UX/UI 디자이너
"UI 변경 불필요. HUD가 이미 `GetKillProgress()` 사용 중이고, 이제 로드 후에도 올바른 값 반환. 사용자 멘탈 모델(진행률은 저장됨)과 실제 동작이 일치하게 됨."

### 🔍 QA 엔지니어

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
| 9 | killProgress 왕복 단위 테스트 | ❌ | **테스트 없음** |
| 10 | SaveSystemTests killProgress 포함 | ❌ | **테스트 없음** |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 스펙 5개 항목 모두 충족 |
| 기존 호환성 | ✅ null 폴백으로 구 세이브 호환 |
| 코드 품질 | ✅ 최소 변경, 방어적 프로그래밍 |
| 아키텍처 | ✅ 기존 패턴 유지 |
| 테스트 커버리지 | ❌ 핵심 변경에 대한 테스트 누락 |

**결론:** ❌ **NEEDS_WORK** — 코드 변경 자체는 정확하고 최소한이며 잘 구현됨. 스펙 전 항목 충족. 하위 호환 처리 완료. 그러나 strict 리뷰 레벨에서 핵심 동작에 대한 자동화 테스트가 필수.

**APPROVE 전 필수 조치:**
1. `QuestSystemTests`에 킬 진행률 serialize/restore 왕복 테스트 추가
2. `QuestSystemTests`에 null killProgress restore 시 크래시 없음 테스트 추가
3. (선택) `SaveSystemTests`의 `SaveAndLoad_PreservesQuestState`에 killProgress 포함
