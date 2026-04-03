# REVIEW-S061-v1: QuestSystem killProgress 고아 항목

> **리뷰 일시:** 2026-04-03
> **태스크:** S-061 QuestSystem killProgress 고아 항목
> **스펙:** SPEC-S-061
> **커밋:** `ed10a17`
> **판정:** ✅ APPROVE

---

## 변경 요약

3개 파일 변경 (+81 라인):

1. **GameEvents.cs** — `QuestAbandonEvent` 구조체 추가.
2. **QuestSystem.cs** — `AbandonQuest()` 메서드 추가, `Serialize()`/`Restore()`에 killProgress 고아 필터링 추가.
3. **QuestSystemTests.cs** — S-061 테스트 4건 추가.

---

## 검증 1: 엔진 검증

| 항목 | 스펙 요구 | 구현 | 판정 |
|------|----------|------|------|
| AbandonQuest() 구현 | _active + _killProgress 동시 제거 | `_active.Remove` + `_killProgress.Remove` + 이벤트 | **PASS** |
| Serialize 필터링 | 활성 퀘스트의 killProgress만 직렬화 | `_active.ContainsKey` 가드 | **PASS** |
| Restore 필터링 | 고아 killProgress 복원 차단 | `_active.ContainsKey` 가드 | **PASS** |
| QuestAbandonEvent | 포기 시 이벤트 발행 | `EventBus.Emit` | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 AbandonQuest (QuestSystem.cs:51-57)

```csharp
public bool AbandonQuest(string questId)
{
    if (!_active.Remove(questId)) return false;
    _killProgress.Remove(questId);
    EventBus.Emit(new QuestAbandonEvent { questId = questId });
    return true;
}
```

- `_active.Remove`가 false면 즉시 반환 — 존재하지 않는 퀘스트 포기 시도 차단
- `_killProgress.Remove`는 키 부재 시 false 반환 (예외 없음) — 안전
- 이벤트 발행으로 UI/로깅 연동 가능
- **불변성 보장:** `_killProgress.Keys ⊆ _active.Keys` 유지됨

### 2.2 Serialize 필터링 (QuestSystem.cs:161-170)

```csharp
var filtered = new Dictionary<string, Dictionary<string, int>>();
foreach (var kvp in _killProgress)
{
    if (_active.ContainsKey(kvp.Key))
        filtered[kvp.Key] = new Dictionary<string, int>(kvp.Value);
}
```

- 새 Dictionary 생성으로 원본 데이터 방어 — 외부 수정 방지
- 활성 퀘스트의 killProgress만 세이브 파일에 기록
- 기존 CompleteQuest()가 이미 `_killProgress.Remove`를 호출하므로 이중 방어

### 2.3 Restore 필터링 (QuestSystem.cs:186-189)

```csharp
if (_active.ContainsKey(kvp.Key))
    _killProgress[kvp.Key] = new Dictionary<string, int>(kvp.Value);
```

- 손상된 세이브 파일이나 제거된 퀘스트의 고아 데이터 차단
- `_active` 복원 후 `_killProgress` 복원 순서 — 올바른 의존성 순서

---

## 검증 3: UI 추적

| 경로 | 영향 | 판정 |
|------|------|------|
| QuestTrackerUI | killProgress 표시 — 고아 항목 제거로 UI 정합성 향상 | **PASS** |
| SaveSystem | Serialize() 호출 — 고아 데이터 세이브 차단 | **PASS** |
| 로드 화면 | Restore() 호출 — 고아 데이터 로드 차단 | **PASS** |

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|----------|------|
| 퀘스트 포기 후 저장/로드 | killProgress 없음, 깨끗한 상태 | **PASS** |
| 손상된 세이브 파일 로드 | 고아 killProgress 필터링됨 | **PASS** |
| 정상 퀘스트 진행 중 저장/로드 | killProgress 정상 유지 | **PASS** |
| 퀘스트 완료 후 저장/로드 | killProgress 이미 Remove됨 | **PASS** |

---

## 테스트 검증

| 테스트 | 검증 대상 | 판정 |
|--------|----------|------|
| AbandonQuest_RemovesKillProgress | 포기 시 killProgress 정리 | **PASS** |
| AbandonQuest_ReturnsFalse_ForInactiveQuest | 비활성 퀘스트 포기 방어 | **PASS** |
| Restore_FiltersOrphanKillProgress | 로드 시 고아 필터링 | **PASS** |
| Serialize_ExcludesOrphanKillProgress | 저장 시 고아 제외 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
퀘스트 포기 기능이 안전하게 동작한다. 포기한 퀘스트의 잔여 데이터가 세이브에 남지 않아 깔끔.

### ⚔️ 코어 게이머
장기 세이브 파일에 누적된 고아 데이터가 다음 저장/로드 사이클에서 자동 정리됨. 세이브 파일 비대화 방지.

### 🎨 UX/UI 디자이너
퀘스트 추적 UI에 고아 항목이 표시되지 않으므로 UI 정합성 향상.

### 🔍 QA 엔지니어
3중 방어(포기 시 즉시 제거, 직렬화 시 필터, 역직렬화 시 필터)로 고아 발생 경로를 완전히 차단. 테스트 4건이 모든 시나리오를 커버. 기존 테스트 회귀 없음.

---

## 종합 판정

### ✅ APPROVE

스펙의 3가지 요구사항(AbandonQuest, Serialize 필터, Restore 필터) 전부 충족. 3중 방어로 불변성(`_killProgress.Keys ⊆ _active.Keys`) 보장. 테스트 커버리지 양호. 기존 API 호환성 유지.
