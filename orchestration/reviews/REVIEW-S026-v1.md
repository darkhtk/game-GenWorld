# REVIEW-S026-v1: NPC 이동 재개 실패 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-026 NPC 이동 재개 실패
> **스펙:** SPEC-S-026
> **커밋:** fc20bb9
> **판정:** ❌ NEEDS_WORK

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ❌ | 상태 정리 불완전 — 아래 상세 |
| 기존 코드 호환 | ⚠️ | `_dialogueOpen` 상태 누수 |
| 아키텍처 패턴 | ⚠️ | SPEC 권장 CloseDialogue() 중앙 메서드 미적용 |
| 테스트 커버리지 | ⚠️ | 미작성 |

### 변경 분석 (코드 직접 확인)

**GameManager.cs:180-185 (수정된 코드):**
```csharp
if (dlg == null)
{
    nearest.ResumeMoving();
    player.Frozen = false;
    return;
}
```

**문제: 상태 정리 누락**

`dlg == null` 조기 반환 이전에 이미 3가지 상태가 변경된다 (lines 172-175):

```csharp
nearest.StopMoving();              // ✅ 수정에서 ResumeMoving()으로 복구
player.Frozen = true;              // ✅ 수정에서 false로 복구
uiManager.SetDialogueOpen(true);   // ❌ 복구 안 됨 → _dialogueOpen = true 잔류
_dialogueNpc = nearest;            // ❌ 복구 안 됨 → stale reference 잔류
```

**`_dialogueOpen = true` 잔류의 영향:**

`UIManager.Update()` (line 37):
```csharp
void Update()
{
    if (_dialogueOpen) return;  // ← 모든 키보드 입력 차단!
    // I(인벤토리), K(스킬트리), J(퀘스트), ESC 등 전부 무시
}
```

→ `_dialogueOpen`이 true로 남으면 플레이어가 인벤토리, 스킬트리, 퀘스트, 일시정지 메뉴를 열 수 없다. **다른 형태의 소프트락이 발생한다.**

**`_dialogueNpc` 잔류의 영향:**

`_dialogueNpc`은 line 216의 `OnClose` 콜백에서만 null로 초기화된다. `dlg`가 null이면 `OnClose`가 설정되지 않으므로 영원히 stale reference로 남는다. 직접적인 크래시 원인은 아니지만 방어적이지 않다.

### 필요한 수정

```csharp
if (dlg == null)
{
    nearest.ResumeMoving();
    player.Frozen = false;
    uiManager.SetDialogueOpen(false);  // 추가 필요
    _dialogueNpc = null;                // 추가 필요
    return;
}
```

### SPEC 대비 구현 범위

SPEC-S-026은 4가지 실패 경로를 식별:

| 경로 | SPEC | 구현 |
|------|------|------|
| dlg == null 조기 반환 | ✅ 식별 | ⚠️ 부분 수정 (상태 누수) |
| WireUICallbacks 초기 콜백 | ✅ 식별 | ❌ 미수정 |
| UIManager.HideAll() | ✅ 식별 | ❌ 미수정 |
| SceneManager.LoadScene | ✅ 식별 | ❌ 미수정 |

현재 커밋은 가장 긴급한 경로(dlg null)만 수정했으나, 해당 수정 자체도 상태 정리가 불완전하다. SPEC 권장 `CloseDialogue()` 중앙 메서드를 도입하면 이 모든 경로를 안전하게 처리할 수 있다.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 체인 | ❌ | `_dialogueOpen=true` 잔류 → UIManager 키보드 차단 |
| 패널 열기/닫기 | ❌ | ESC, I, K, J 전부 무반응 |
| 데이터 바인딩 | ✅ | 영향 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 정상 대화 (dlg 존재) | 기존과 동일 — 조기 반환 불통과 |
| dlg==null 상태에서 NPC 대화 시도 | NPC 패트롤 재개 ✅, 플레이어 이동 가능 ✅, **BUT** 인벤토리/스킬트리/퀘스트/ESC 키 무반응 ❌ |
| dlg==null 후 다시 NPC 대화 시도 | `_dialogueNpc`이 이전 NPC를 가리킴 — 직접 영향은 없으나 불필요한 잔류 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC한테 말 걸었는데 대화창 안 뜨면 그건 뭔가 이상한 거긴 한데, 적어도 캐릭터가 움직이는 건 좋다. 근데 그 뒤에 인벤토리도 안 열리면? 그건 게임 고장난 줄 안다.

### ⚔️ 코어 게이머
방어 코드의 의도는 맞다. `StopMoving()` 후 `ResumeMoving()` 대칭이 핵심이고 그건 해결했다. 하지만 `SetDialogueOpen(true)` → `SetDialogueOpen(false)` 대칭이 깨졌다. 상태 변경은 반드시 쌍으로 복구해야 한다. `try-finally` 패턴이나 중앙 정리 메서드가 이 문제를 근본적으로 해결한다.

### 🎨 UX/UI 디자이너
`_dialogueOpen`이 잔류하면 UIManager가 모든 키 입력을 무시한다. 사용자가 대화 UI가 없는 상태에서 아무 키도 안 먹는 경험을 하게 되며, "UI가 먹통이다"는 가장 심각한 UX 실패 중 하나다.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| NPC 이동 복구 | ✅ `ResumeMoving()` 호출 |
| 플레이어 이동 복구 | ✅ `Frozen = false` |
| 대화 상태 정리 | ❌ `SetDialogueOpen(false)` 누락 → UI 입력 차단 |
| NPC 참조 정리 | ❌ `_dialogueNpc = null` 누락 |
| 중복 진입 방어 | ⚠️ `_dialogueOpen=true`로 인해 UIManager는 차단되지만 GameManager의 TryInteractNPC는 독립적 |
| SPEC 수용 기준 #1 | N/A | 정상 종료 경로 — 이번 수정 범위 외 |
| SPEC 수용 기준 #5 | ✅ | `_dialogueNpc=null`일 때 CloseDialogue 안전성 — 미구현 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ❌ 상태 정리 불완전 — `_dialogueOpen`, `_dialogueNpc` 미복구 |
| 기존 호환성 | ⚠️ 정상 경로 영향 없으나, dlg null 시 UI 입력 차단 소프트락 |
| 코드 품질 | ⚠️ 의도는 올바르나 상태 대칭 누락 |

**결론:** ❌ **NEEDS_WORK**

**필수 수정:**
1. `dlg == null` 조기 반환 블록에 `uiManager.SetDialogueOpen(false)` 추가
2. `dlg == null` 조기 반환 블록에 `_dialogueNpc = null` 추가

**권장 개선 (후속 태스크 가능):**
- SPEC 권장 `CloseDialogue()` 중앙 메서드 도입으로 모든 종료 경로 통합
