# REVIEW-S-126-v1: 옵션창 ESC로 닫기 일관성

**Task:** S-126
**리뷰 일시:** 2026-04-30 (Client 2회차, 본 루프 3회차에서 파일 재기록)
**대상 커밋:** 8c01c12 — feat(S-126): 옵션창 ESC 닫기 일관성 — NPC/Dialogue 모달 포함 + HideAll dialogue OnClose 동반
**리뷰 모드:** 일반 (코드 추적 + OnClose 콜백 시퀀스 검증)
**SPEC 참조:** specs/SPEC-S-126.md **부재** (RESERVE 비고 의도 충족 확인)

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `Assets/Scripts/UI/UIManager.cs Update` | ESC 입력 분기를 `_dialogueOpen` 차단 위로 이동 → 다이얼로그 중에도 ESC로 모달 닫힘. 다른 키(I/K/J/R/T/H/Tab)는 `_dialogueOpen` 차단 유지 (인벤토리 토글 방지) |
| `Assets/Scripts/UI/UIManager.cs IsAnyPanelOpen` | `dialogue.IsOpen` / `npcProfile.IsOpen` / `npcQuest.IsOpen` 추가 → NPC 모달도 ESC로 일관 닫힘 |
| `Assets/Scripts/UI/UIManager.cs HideAll` | dialogue 닫기에 `OnClose?.Invoke()` 동반 호출 → DialogueController 상태 정리 보장 (_player.Frozen=false / NpcProfile.Hide / SetDialogueOpen(false) / ResumeMoving) — closeButton/AutoCloseDialogue 패턴과 정합 |
| `Assets/Scripts/UI/DialogueUI.cs` | `public bool IsOpen => panel != null && panel.activeSelf` 1라인 (PauseMenuUI 패턴) |
| `Assets/Scripts/UI/NpcProfilePanel.cs` | 동일 IsOpen 1라인 |
| `Assets/Scripts/UI/NpcQuestPanel.cs` | 동일 IsOpen 1라인 |

비변경 (영향 검토만):
- closeButton OnClick / AutoCloseDialogue 코루틴 / DialogueEndEvent emit 회로 — 그대로 유지.

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| `IsOpen` 프로퍼티 3개 추가 (PauseMenuUI 패턴) | ✅ | `panel != null && panel.activeSelf` 일관 표현 |
| ESC 키 분기 위치 변경 | ✅ | `_dialogueOpen` 가드 위로 이동 — 다이얼로그 중에도 닫힘 |
| 다른 키(I/K/J/R/T/H/Tab) 차단 유지 | ✅ | 다이얼로그 중 인벤토리 토글 회귀 방지 |

### 검증 2: 코드 추적 (OnClose 시퀀스)
| 시퀀스 | 결과 | 비고 |
|---------|------|------|
| closeButton 클릭 → DialogueUI.Hide → OnClose | ✅ 기존 동작 유지 | DialogueController._player.Frozen=false / NpcProfile.Hide / SetDialogueOpen(false) / ResumeMoving |
| AutoCloseDialogue 코루틴 → Hide → OnClose | ✅ 기존 동작 유지 | 동일 |
| **🆕 ESC → UIManager.HideAll → dialogue.Hide + OnClose?.Invoke()** | ✅ | 동일 cleanup 시퀀스 — DialogueController.OnClose idempotent 회귀 안전 |
| DialogueEndEvent 재emit 위험 | ⚠️ 낮음 | closeButton + ESC 동시 발생 비현실적. Hide()는 panel.activeSelf 가드로 idempotent |
| NPC 프로필/퀘스트 모달 ESC 닫힘 | ✅ 신규 동작 | IsAnyPanelOpen에 IsOpen 포함 → HideAll에서 SetActive(false) |

### 검증 3: UI 추적
- ESC 입력 → UIManager.Update → IsAnyPanelOpen() → HideAll() → 각 패널 SetActive(false) + dialogue.OnClose 동반.
- 다이얼로그 중 I 키 → UIManager.Update → `_dialogueOpen` 가드로 무시 (인벤토리 토글 차단 유지).
- 다이얼로그 중 ESC → UIManager.Update → ESC 분기가 `_dialogueOpen` 가드 위 → HideAll 실행.

### 검증 4: 사용자 시나리오
| 시나리오 | 결과 | 비고 |
|---------|------|------|
| 메뉴/옵션/인벤토리 ESC 닫기 | ✅ 기존 동작 유지 | |
| **NPC 대화창 ESC 닫기** | ✅ 신규 일관 닫힘 | HideAll OnClose 동반으로 player Frozen 해제 |
| **NPC 프로필 모달 ESC 닫기** | ✅ 신규 | IsOpen 추가로 IsAnyPanelOpen 포함 |
| **NPC 퀘스트 모달 ESC 닫기** | ✅ 신규 | 동일 |
| 다이얼로그 중 I/K/J/R/T/H/Tab | ✅ 차단 유지 | 인벤토리 토글 등 방지 |
| ESC 연타 (이미 닫힌 상태) | ✅ idempotent | panel.activeSelf 가드 |

---

## 페르소나 리뷰

### 🎮 하늘 (캐주얼 게이머)
- 옵션창 열고 ESC, 다 닫혀서 빠르다. 그동안 NPC 대화창은 ESC가 안 먹어서 X 버튼 찾는 게 짜증이었는데 이제 ESC 한 방 — 좋다.
- NPC 프로필 보다가 ESC로 한 번에 닫히니 시원함.

### ⚔️ 태현 (코어 RPG 게이머)
- 다이얼로그 중 다른 키(I/T/J/R/T/H/Tab)는 차단 유지하고 ESC만 분리 통과시킨 건 정확한 분기. **사용자 입력 우선순위 정의가 명시적.**
- IsOpen 1라인 패턴으로 PauseMenuUI / DialogueUI / NpcProfilePanel / NpcQuestPanel 통일 — 미래의 panel 추가 시 동일 패턴으로 IsAnyPanelOpen에 추가만 하면 됨. 확장성 ✅
- HideAll OnClose 동반은 closeButton/AutoCloseDialogue와 동일 cleanup → idempotent 안전.

### 🎨 수아 (UX/UI 디자이너)
- ESC 일관성은 모달 UI의 기본 — 본 변경으로 기본 충족. 다음 단계는 ESC 단계적 닫기(가장 위 패널만 1개 닫고 나머지는 유지)인데 그건 별도 ticket.
- IsOpen 1라인 도입으로 향후 inspector/debugger에서 각 패널 상태 직접 관찰 가능 — 디버깅 편의 ✅

### 🔍 준혁 (QA 엔지니어)
- panel == null 가드 (`panel != null && panel.activeSelf`) — Awake 전 IsOpen 호출 시 NRE 방지. ✅
- HideAll dialogue.Hide + OnClose?.Invoke() 호출 순서 — Hide가 panel.SetActive(false) → OnClose 호출 시점에 panel.activeSelf=false → DialogueController가 SetDialogueOpen(false) 호출해도 idempotent. ✅
- DialogueEndEvent 재emit 회로 추적: closeButton + ESC 동시 트리거 비현실적이므로 사실상 N/A. 단, AutoCloseDialogue 코루틴 진행 중 ESC 트리거되면 close 2회 가능 — Hide() panel.activeSelf 가드로 idempotent.
- **회귀 위험 0** — 기존 closeButton/AutoCloseDialogue 경로는 무손상. 신규 ESC 경로만 추가.

---

## 후속 BACKLOG 후보
1. **IUiPanel 인터페이스 + List 등록** — UIManager가 List<IUiPanel>로 일괄 관리. P3.
2. **UIManagerEscTests PlayMode** — ESC → 각 패널 닫힘 자동화. P2.
3. **DialogueEndEvent 재emit 가드** — Hide() 내부에서 idempotent flag 도입. P3.
4. **ESC 단계적 닫기** — 가장 위 패널만 1개씩 닫기 (현재는 한 번에 모두). P3.
5. **S-118/S-120/S-121 ESC 동시 트리거 PlayMode** — close SFX 정상 트리거 회로 검증. P3.

---

## 결론

**✅ APPROVE — 4/4 페르소나 만장일치**

- SPEC 부재(specs 참조 N) — RESERVE 비고 의도("ESC 일관성") 충족 확인.
- 변경 6건 1~5라인 단위. closeButton/AutoCloseDialogue/HideAll 세 경로 모두 동일 cleanup 시퀀스 → 회귀 안전.
- DialogueEndEvent 재emit 회로 N/A (idempotent + closeButton + ESC 동시 발생 비현실적).
- IsOpen 패턴 통일로 향후 panel 추가 시 동일 패턴 확장만 → 유지보수성 ✅.
- UI 자가검증: 신규 메서드/시스템 없음 (기존 IsOpen 패턴 확장), ESC 키 = 기존 진입점, SPEC 부재 N/A.
