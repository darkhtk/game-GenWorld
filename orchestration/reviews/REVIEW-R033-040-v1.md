# REVIEW-R033+R040-v1: 로딩 화면 + NPC 카메라 줌

> **리뷰 일시:** 2026-04-02
> **태스크:** R-033 로딩 화면 + R-040 NPC 대화 카메라 줌
> **스펙:** 없음 (폴리시)
> **판정:** ✅ APPROVE

---

## R-033: 로딩 화면 (LoadingScreenUI.cs, 43행)

| 항목 | 결과 | 상세 |
|------|------|------|
| 싱글턴 Instance | ✅ | line 13 |
| Show/Hide | ✅ | panel SetActive |
| 프로그레스 바 | ✅ | progressBar.fillAmount |
| 퍼센트 텍스트 | ✅ | `{progress*100}%` |
| 상태 텍스트 | ✅ | statusText.text |
| SimulateLoading 코루틴 | ✅ | steps 배열 순회, 0.15초 간격 |

## R-040: NPC 대화 카메라 줌 (DialogueCameraZoom.cs, 48행)

| 항목 | 결과 | 상세 |
|------|------|------|
| EventBus 구독 | ✅ | DialogueStartEvent → ZoomIn, DialogueEndEvent → ZoomOut |
| Lerp 줌 | ✅ | `Mathf.Lerp(current, target, speed * dt)` |
| 원본 크기 복원 | ✅ | `_originalSize` 저장 + ZoomOut 복원 |
| zoomInSize/Speed SerializeField | ✅ | Inspector 조정 가능 |
| 줌 완료 감지 | ✅ | `Abs < 0.01f → _zooming = false` |
| null 방어 | ✅ | _cam null 체크 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
로딩 화면이 있으면 게임 시작할 때 뭔가 진행 중인 느낌! NPC 대화 시 카메라 줌인은 몰입감 좋겠다.

### 🔍 QA 엔지니어
- ZoomIn에서 `_originalSize = _cam.orthographicSize` 갱신 — ZoomOut이 정확한 크기로 복원 ✅
- Lerp 기반이라 자연스러운 전환 ✅
- _zooming flag로 불필요한 Lerp 중단 ✅

---

## 최종 판정

**✅ APPROVE**

로딩 화면: 싱글턴, 프로그레스 바+텍스트, SimulateLoading 코루틴. 카메라 줌: EventBus 구독, Lerp 전환, 원본 크기 복원. 모두 정확.
