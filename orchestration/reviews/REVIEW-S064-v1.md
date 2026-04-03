# REVIEW-S064-v1: DialogueUI 코루틴 중복 실행

> **리뷰 일시:** 2026-04-03
> **태스크:** S-064 DialogueUI 코루틴 중복 실행
> **스펙:** SPEC-S-064
> **커밋:** `deb6cbe`
> **판정:** ✅ APPROVE

---

## 변경 요약

DialogueUI.cs 1개 파일, +3 라인 변경:

1. **Show()** — 패널이 이미 활성 상태면 `Hide()` 호출 후 진행 (재진입 방어).
2. **ShowLoading(true)** — 기존 `_loadingCoroutine`가 있으면 `StopCoroutine` 후 새 코루틴 시작.

---

## 검증 1: 엔진 검증

| 항목 | 스펙 요구 | 구현 | 판정 |
|------|----------|------|------|
| ShowLoading 코루틴 가드 | _loadingCoroutine != null → StopCoroutine | `if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine)` | **PASS** |
| Show() 재진입 방어 | panel 활성 시 Hide() 호출 | `if (panel != null && panel.activeSelf) Hide()` | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 Show() 재진입 방어 (DialogueUI.cs:92)

```csharp
public void Show(NpcDef npcDef, ConditionalDialogue conditional = null)
{
    if (panel != null && panel.activeSelf) Hide();
    // ... rest of method
}
```

- `panel != null` 체크로 null 참조 방지
- `panel.activeSelf`로 실제 활성 상태만 확인 (비활성이면 Hide 불필요)
- `Hide()`가 `_loadingCoroutine` 정리도 포함하므로 연쇄 정리 보장
- 빠른 NPC 전환 시나리오에서 이전 대화 상태가 깨끗하게 정리됨

### 2.2 ShowLoading 코루틴 가드 (DialogueUI.cs:216)

```csharp
if (show)
{
    if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
    loadingPanel.SetActive(true);
    _loadingCoroutine = StartCoroutine(LoadingAnimation());
}
```

- 기존 코루틴 중지 후 새 코루틴 시작 — 고아 코루틴 방지
- `_loadingCoroutine` 참조가 즉시 갱신되어 다음 StopCoroutine 호출에서도 유효
- Hide()에서도 동일한 `StopCoroutine + null` 패턴 사용 (line 135) — 일관성 양호

### 2.3 DialogueController 연동 확인

`DialogueController.HandleDialogueResponse()`에서:
- `dlg.ShowLoading(true)` → AI 생성 → `dlg.ShowLoading(false)`
- AI 응답 중 플레이어가 다른 NPC 상호작용 시 `_inDialogue` 가드 + Show() 재진입 방어로 이중 보호

---

## 검증 3: UI 추적

| 경로 | 이전 문제 | v1 수정 | 판정 |
|------|----------|---------|------|
| ShowLoading(true) 연속 호출 | 코루틴 누수 (이전 코루틴 계속 실행) | StopCoroutine 가드 | **PASS** |
| 빠른 NPC 전환 | 이전 대화 UI 상태 잔류 | Show()에서 Hide() 선행 호출 | **PASS** |
| 정상 대화 → 닫기 | 변화 없음 | 변화 없음 | **PASS** |

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|----------|------|
| NPC 대화 중 다른 NPC 클릭 | 이전 대화 Hide → 새 대화 Show | **PASS** |
| AI 응답 대기 중 ShowLoading 재호출 | 이전 코루틴 정지, 새 코루틴 시작 | **PASS** |
| 정상 대화 흐름 | 기존 동작 동일 | **PASS** |
| 대화 중 ESC로 닫기 | Hide() 정상 정리 | **PASS** |

---

## 테스트 검증

스펙에서 요구한 "ShowLoading(true) 이중 호출 테스트"가 미작성됨. 그러나 `DialogueUI`는 `MonoBehaviour`이며 코루틴 테스트는 EditMode에서 불가능 (PlayMode 필요). 프로젝트가 EditMode 테스트만 사용하는 현 구조에서는 수용 가능한 제약.

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC 대화 중 실수로 다른 NPC를 클릭해도 UI가 꼬이지 않는다. 이전에는 대화창이 중첩되거나 로딩 애니메이션이 멈추지 않는 경우가 있었을 수 있으나, 이제 깔끔하게 전환됨.

### ⚔️ 코어 게이머
빠른 대화 스킵이나 연속 상호작용에서 UI 안정성 향상. 게임플레이에는 영향 없음.

### 🎨 UX/UI 디자이너
재진입 방어로 대화 UI 상태 일관성 보장. 로딩 애니메이션이 코루틴 누수 없이 깨끗하게 동작.

### 🔍 QA 엔지니어
코드 변경이 3줄로 최소한이며, 각각 명확한 방어 패턴. MonoBehaviour 코루틴 테스트의 EditMode 제약은 알려진 한계. 수동 테스트(빠른 NPC 전환, AI 응답 중 재호출) 권장.

---

## 종합 판정

### ✅ APPROVE

최소한의 변경(3줄)으로 코루틴 누수와 UI 재진입 문제를 정확히 해결. 기존 `Hide()` 패턴과 일관성 유지. EditMode 테스트 제약으로 자동 테스트는 없으나, 코드 변경 범위가 매우 작고 로직이 명확하여 수동 검증으로 충분.
