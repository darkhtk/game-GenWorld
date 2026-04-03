# REVIEW-S047-v1: DialogueSystem 동시 대화 방지

> **리뷰 일시:** 2026-04-03
> **태스크:** S-047 DialogueSystem 동시 대화 방지
> **스펙:** SPEC-S-047
> **커밋:** fix: S-047 DialogueController _inDialogue 가드 — 동시 대화 재진입 방지 (7146841)
> **판정:** ✅ APPROVE (경미 권고 1건)

---

## 변경 요약

**변경 파일:** 1개 (`Assets/Scripts/Core/DialogueController.cs`) — 코드 변경 +7줄

1. **`_inDialogue` private 필드 추가** (line 23): `bool _inDialogue;`
2. **`InDialogue` public 읽기 전용 프로퍼티 추가** (line 26): `public bool InDialogue => _inDialogue;`
3. **`TryInteract()` 진입 가드** (line 80): `if (_inDialogue) return;` — 대화 중 재진입 즉시 차단
4. **대화 시작 시 플래그 설정** (line 96): `_inDialogue = true;`
5. **UI null 조기 반환 경로에서 플래그 해제** (line 111): `_inDialogue = false;`
6. **정상 대화 종료(`OnClose`) 경로에서 플래그 해제** (line 144): `_inDialogue = false;`

**관리 파일:** 2개 (`orchestration/BACKLOG_RESERVE.md`, `orchestration/BOARD.md`) — 상태 갱신

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | DialogueController는 순수 C# 클래스, 새 씬 참조 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### 경로 A: 정상 대화 시작 → 종료

**호출 체인:** `GameManager.Update()` → `Input.GetKeyDown(KeyCode.F)` → `DialogueController.TryInteract()` → NPC 탐색 → `_inDialogue = true` (line 96) → 대화 진행 → 사용자 대화창 닫기 → `dlg.OnClose` → `_inDialogue = false` (line 144)

- `_inDialogue` 시작 시 `true`, 종료 시 `false`. **PASS**

### 경로 B: 대화 중 F키 재입력

**기존 가드 1:** `GameManager.Update()` (line 117): `if (player.Frozen) return;` — TryInteract() 직전에 Frozen이 true이므로 F키 입력 자체가 도달하지 않음
**기존 가드 2:** `UIManager.IsInputBlocked()` (line 87): `_dialogueOpen` true 시 입력 차단 (GameManager line 144)
**신규 가드 3:** `TryInteract()` (line 80): `if (_inDialogue) return;` — 방어적 추가 계층

3중 가드 구조: `Frozen` → `IsInputBlocked` → `_inDialogue`. **PASS**

### 경로 C: dlg == null 조기 반환

`_uiManager.Dialogue` null 시 (line 105): NPC 정지 복원, 플레이어 해동, UI 상태 복원, `_dialogueNpc = null`, `_inDialogue = false`. 상태 완전 롤백. **PASS**

### 경로 D: HandleDialogueResponse 예외 발생

`HandleDialogueResponse` catch 블록 (lines 205-211): `_dialogueGenerating = false`, 로딩 숨김, 에러 로그 출력. 그러나 **`_inDialogue`는 catch에서 직접 해제하지 않음.**

**분석:** 이 경로에서 대화 패널은 여전히 열려 있고 (`dlg.AppendLog("System", "...", "#999999")`), 사용자가 대화창을 닫으면 `OnClose` 콜백이 호출되어 `_inDialogue = false`가 실행됨. 따라서 **데드락 발생하지 않음.** 다만, `HandleDialogueResponse`가 연속 호출되며 예외가 반복되면 대화 UI가 "..." 메시지로 채워질 수 있으나, 이는 기존 동작이며 S-047 범위 외.

**잠재적 위험:** 예외 발생 시 `dlg`가 null이면 (예: 씬 전환으로 UI 파괴) `dlg?.AppendLog()` no-op이고, 대화 UI가 없으므로 `OnClose` 호출 불가 → `_inDialogue`가 영구 true 잔존 가능. 단, 씬 전환 시 `DialogueController` 인스턴스 자체가 새로 생성되므로 실질적 문제 없음. **PASS (경미 권고)**

### 경로 E: AutoCloseDialogue 코루틴

`AutoCloseDialogue` (lines 214-223): 1.5초 후 `dlg.Hide()` → `dlg.OnClose?.Invoke()` → `_inDialogue = false`. 정상 해제. **PASS**

### 기존 시스템 호환성 검증

| 시스템 | 안전성 | 비고 |
|--------|--------|------|
| GameManager.Update() | ✅ | `player.Frozen` + `IsInputBlocked()` 기존 가드 유지, TryInteract 호출 변경 없음 |
| UIManager._dialogueOpen | ✅ | `SetDialogueOpen(true/false)`와 `_inDialogue` 독립 관리, 양쪽 동기화됨 |
| EventBus (DialogueStartEvent/EndEvent) | ✅ | 이벤트 발행 위치 변경 없음 |
| DialogueCameraZoom | ✅ | EventBus 리스닝, 발행 순서 변경 없음 |
| PlayerController.Frozen | ✅ | 기존 set/reset 경로 유지 |
| AIManager (대화 생성) | ✅ | HandleDialogueResponse async 호출 변경 없음 |

### InDialogue 프로퍼티 사용

`InDialogue` 프로퍼티는 현재 코드베이스에서 사용하는 곳 없음. 향후 외부 시스템(예: trigger collider 기반 자동 대화, InputSystem 가드)에서 활용 가능. **스펙 "선택적" 항목 구현 완료.**

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 대화 패널 열기 | ✅ | `dlg.Show()` 호출 위치 변경 없음 |
| 대화 패널 닫기 | ✅ | `OnClose` 콜백에서 `_inDialogue = false` 확인 |
| 대화 중 다른 UI 패널 | ✅ | `IsInputBlocked()` 기존 가드가 대화 중 다른 패널 입력도 차단 |
| 대화 로그 표시 | ✅ | `dlg.ClearLog()` + `AppendLog()` 기존 동작 유지 |
| 퀘스트 제안 UI | ✅ | `ShowQuestProposal()` 호출 경로 변경 없음 |
| 액션 버튼 | ✅ | `SetActionButtons()` 호출 경로 변경 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| NPC 근처에서 F키 1회 | 정상 대화 시작. `_inDialogue = true` |
| 대화 중 F키 재입력 | `player.Frozen` → Update early return으로 TryInteract 미도달. 만약 직접 호출 시 `_inDialogue` 가드로 차단 |
| 대화 정상 종료 후 같은 NPC 재대화 | `_inDialogue = false` 해제 후 정상 재진입 |
| 대화 정상 종료 후 다른 NPC 대화 | 동일. `_dialogueNpc = null` + `_inDialogue = false` 후 새 NPC 정상 탐색 |
| NPC 범위 밖에서 F키 | `nearest == null` → return. `_inDialogue` 변경 없음 |
| dlg == null (UI 미초기화) | NPC 정지 복원, Frozen 해제, `_inDialogue = false`. 완전 롤백 |
| AI 응답 중 예외 발생 | 에러 로그 + 대화 UI에 "..." 표시. 사용자가 대화창 닫으면 `_inDialogue = false` |
| AutoCloseDialogue 경로 | 1.5초 후 `OnClose` 자동 호출 → `_inDialogue = false` |
| Trigger collider 기반 대화 (향후) | `_inDialogue` 가드가 동시 대화 방지. 스펙 의도 충족 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"NPC랑 대화할 때 F키를 연타하는 버릇이 있는데, 대화가 겹치면 혼란스러울 뻔했다. 이제 대화 중이면 무시해준다니 안심. 대화 종료 후 바로 다시 말 걸 수 있는 것도 확인됐고, 체감상 달라지는 건 없는데 뒤에서 튼튼해진 느낌."

### ⚔️ 코어 게이머
"상점 NPC 대화 중 실수로 옆 퀘스트 NPC까지 동시 트리거되는 게 RPG에서 흔한 버그인데, 선제적으로 막아둔 건 좋다. 향후 auto-interact나 proximity trigger 추가해도 안전. 멀티 레이어 가드(Frozen + IsInputBlocked + _inDialogue)는 과하다 싶지만 방어적 코딩 관점에서 수용."

### 🎨 UX/UI 디자이너
"사용자 입장에서 변화 없음. 대화 시작/종료 이벤트 순서 유지되어 DialogueCameraZoom 줌인/줌아웃 전환도 정상. 대화 UI 상태와 `_inDialogue` 동기화가 보장됨. 시각적 회귀 없음."

### 🔍 QA 엔지니어

| 체크 | 결과 | 비고 |
|------|------|------|
| `_inDialogue` private 필드 존재 | ✅ | line 23 |
| `InDialogue` public 프로퍼티 존재 | ✅ | line 26, expression-bodied |
| TryInteract() 진입부 `_inDialogue` 가드 | ✅ | line 80, 메서드 첫 번째 문장 |
| 대화 시작 시 `_inDialogue = true` | ✅ | line 96, NPC 발견 후 즉시 설정 |
| 정상 종료(OnClose)에서 해제 | ✅ | line 144, `_inDialogue = false` |
| dlg null 조기 반환에서 해제 | ✅ | line 111, `_inDialogue = false` |
| HandleDialogueResponse catch에서 해제 | ⚠️ | 직접 해제 없음 — OnClose 경유 필요. 실질적 문제 없음 (경미 권고) |
| `_dialogueNpc` null 안전 | ✅ | TryInteract 내 `nearest == null` 체크 후 할당. null 상태 진입 없음 |
| GameManager 호환성 | ✅ | `player.Frozen` + `IsInputBlocked()` 기존 가드 유지 |
| UIManager._dialogueOpen 동기화 | ✅ | `SetDialogueOpen()` 호출 위치 변경 없음 |
| EventBus 이벤트 발행 순서 | ✅ | Start/End 이벤트 위치 변경 없음 |
| DialogueCameraZoom 호환 | ✅ | 이벤트 기반, 영향 없음 |
| AutoCloseDialogue 코루틴 | ✅ | OnClose 호출 → _inDialogue 해제 |
| 단위 테스트 | ℹ️ | 없음 — DialogueController는 순수 C# 클래스지만 많은 외부 의존성으로 모킹 필요. 현재 수준에서 수용 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 스펙 전 항목 충족 (필수 3건 + 선택 1건) |
| 기존 호환성 | ✅ GameManager, UIManager, EventBus, DialogueCameraZoom 전부 안전 |
| 코드 품질 | ✅ 최소 변경(+7줄), 기존 패턴 준수, 방어적 코딩 |
| 예외 안전성 | ⚠️ HandleDialogueResponse catch에서 `_inDialogue` 직접 해제 없음 — OnClose 경유로 실질적 데드락 없으나, `_inDialogue = false`를 catch/finally에 추가하면 더 견고 |

**경미 권고 (APPROVE 유지, 차후 개선 권장):**
- `HandleDialogueResponse` catch 블록에 `_inDialogue = false` 추가 또는 try-finally 패턴 적용 검토. 현재 코드에서는 OnClose 콜백이 최종 해제를 담당하므로 실질적 문제 없으나, 향후 대화 UI 없이 직접 호출하는 경로가 추가되면 `_inDialogue` 영구 잠금 위험 존재.

**결론:** ✅ **APPROVE** — 스펙에 정확히 부합하는 최소한의 방어적 구현. `_inDialogue` 플래그가 TryInteract 진입부에서 재진입을 차단하고, 정상 종료/조기 반환 두 경로 모두에서 해제됨. 기존 3중 가드(Frozen + IsInputBlocked + _inDialogue) 체계와 자연스럽게 통합. 향후 trigger 기반 자동 대화 진입점 추가 시에도 안전성 확보. 회귀 없음.
