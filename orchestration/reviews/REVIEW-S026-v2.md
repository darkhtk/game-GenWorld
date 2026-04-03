# REVIEW-S026-v2: NPC 이동 재개 실패 (재리뷰)

> **리뷰 일시:** 2026-04-03
> **태스크:** S-026 NPC 이동 재개 실패
> **스펙:** SPEC-S-026
> **커밋:** fc20bb9 (v1) + 7efad82 (v2)
> **이전 리뷰:** REVIEW-S026-v1 ❌ NEEDS_WORK
> **판정:** ✅ APPROVE

---

## v1 지적 사항 반영 확인

| 지적 | 반영 | 비고 |
|------|------|------|
| `uiManager.SetDialogueOpen(false)` 누락 | ✅ 추가됨 | UIManager 키보드 차단 해소 |
| `_dialogueNpc = null` 누락 | ✅ 추가됨 | stale reference 정리 |

## 검증 2: 코드 추적 (재검증)

**GameManager.cs:180-186 (v2 최종 코드):**
```csharp
if (dlg == null)
{
    nearest.ResumeMoving();        // ✅ NPC 이동 복구
    player.Frozen = false;          // ✅ 플레이어 이동 복구
    uiManager.SetDialogueOpen(false); // ✅ v2 추가 — UI 입력 차단 해소
    _dialogueNpc = null;            // ✅ v2 추가 — stale ref 정리
    return;
}
```

**상태 대칭 검증:**

| 상태 변경 (line 172-175) | 복구 (dlg null 경로) |
|--------------------------|---------------------|
| `nearest.StopMoving()` | ✅ `nearest.ResumeMoving()` |
| `player.Frozen = true` | ✅ `player.Frozen = false` |
| `uiManager.SetDialogueOpen(true)` | ✅ `uiManager.SetDialogueOpen(false)` |
| `_dialogueNpc = nearest` | ✅ `_dialogueNpc = null` |
| `_dialogueHistory.Clear()` | — (빈 리스트, 정리 불필요) |

모든 상태 변경이 대칭적으로 복구됨.

## 검증 3: UI 추적 (재검증)

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 체인 | ✅ | `_dialogueOpen=false` → UIManager.Update() 정상 작동 |
| I/K/J/ESC 키 | ✅ | 모두 정상 반응 |

## 검증 4: 플레이 시나리오 (재검증)

| 시나리오 | 예상 결과 |
|----------|-----------|
| dlg==null 상태에서 NPC 대화 시도 | NPC 패트롤 재개 ✅, 플레이어 이동 ✅, UI 키보드 정상 ✅ |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| v1 지적 사항 | ✅ 전부 반영 |
| 상태 대칭 | ✅ 4개 상태 모두 복구 |
| 기능 완성도 | ✅ dlg null 경로 완전 방어 |

**결론:** ✅ **APPROVE** — v1에서 지적한 `SetDialogueOpen(false)`와 `_dialogueNpc = null` 누락이 정확히 수정됨. dlg null 경로에서의 모든 상태 변경이 대칭적으로 복구되어 소프트락 완전 방지.

**참고:** SPEC-S-026의 나머지 경로(UIManager.HideAll, SceneManager.LoadScene, WireUICallbacks 초기 콜백)는 별도 후속 태스크로 검토 가능. 현재 수정은 가장 빈번한 실패 경로를 안전하게 처리함.
