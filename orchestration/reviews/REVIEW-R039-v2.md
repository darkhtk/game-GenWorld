# REVIEW-R039-v2: Settings UI (Steam) [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** R-039 Settings UI v2
> **스펙:** SPEC-R-039
> **판정:** ✅ APPROVE

---

## v1 지적사항 해결 확인

| v1 지적사항 | 해결 | 비고 |
|------------|------|------|
| Cancel() 미구현 | ✅ | line 206-214: 저장된 값 복원 + Close() |
| Apply() 내 SettingsManager.Apply() 미호출 | ✅ | SaveAll() + 개별 setter로 즉시 반영 |
| 해상도 드롭다운 동적 population | ✅ | PopulateResolutions() line 107-136 |
| 15초 해상도 확인 카운트다운 팝업 | ✅ | ResolutionCountdown() line 239-268 |
| 키 리바인딩 UI + 중복 검사 | ✅ | BuildKeyBindingRows() + Update() 키 감지 |
| MainMenuController 연동 | ✅ | settingsUI 필드 참조 확인 (line 11) |

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 | 비고 |
|---|---------|------|------|
| 1 | 3개 탭 간 전환 정상 동작 | ✅ | SwitchTab(0/1/2) |
| 2 | 모니터 지원 해상도 동적 표시 | ✅ | Screen.resolutions + 고정 해상도 병합, 정렬 |
| 3 | "적용" 시 SettingsManager 반영 + 저장 | ✅ | Apply() → setter + SaveAll() |
| 4 | "취소" 시 변경 전 상태 복원 | ✅ | Cancel() → _saved* 값 복원 |
| 5 | 해상도 변경 15초 확인 카운트다운 | ✅ | WaitForSecondsRealtime(1f) × 15, 미확인 시 RevertResolution() |
| 6 | 키 바인딩 재설정 + 중복 검사 | ✅ | StartListening → "..." → 키 감지 → 중복 시 이전 바인딩 초기화 |
| 7 | 메인 메뉴 + 인게임 접근 가능 | ✅ | MainMenuController.settingsUI + PauseMenu 경유 |

---

## 코드 직접 검증 [깊은 리뷰]

**파일:** `Assets/Scripts/UI/SettingsUI.cs` (404줄)

### 구조 분석
- **Open()** (line 73-90): pendingSettings 패턴으로 _saved* 변수에 현재 값 저장 → Cancel 시 복원
- **PopulateResolutions()** (line 107-136): 고정 6개 + Screen.resolutions 병합, HashSet 중복 제거, 정렬
- **FindResolutionIndex()** (line 138-149): 기존 고정 인덱스→동적 리스트 인덱스 매핑
- **Apply()** (line 162-204): 해상도 변경 감지 → ShowResolutionConfirm / 미변경 시 Close
- **Cancel()** (line 206-215): _saved* 복원 → Close
- **ResolutionCountdown()** (line 239-268): WaitForSecondsRealtime — Time.timeScale 무관하게 동작 (일시정지 중에도 작동). 15초 후 RevertResolution() 자동 호출
- **Key rebinding** (line 282-366): 키 감지 Update 루프, ESC/None/조이스틱 제외, 중복 시 기존 바인딩을 None으로 초기화 + 시각적 피드백 (색상 변경)

### 코드 품질
- ✅ null 체크 일관 적용
- ✅ Coroutine 정리 (Close에서 StopCoroutine)
- ✅ 키 리바인딩 리스너 상태 관리 (_listeningRow null 가드)
- ✅ 중복 키 감지 시 시각적 피드백 (Color.red / Color.cyan)
- ✅ SettingsManager에 SetKey/GetKey/GetDefaultBindings/ResetKeyBindings 메서드 확인

---

## 페르소나별 리뷰

### 🎮 캐주얼 게이머
해상도 바꿨을 때 "15초 안에 확인하세요" 팝업이 뜨니까 안심이다. 취소 버튼도 생겨서 실수해도 되돌릴 수 있다.

### ⚔️ 코어 게이머
키 리바인딩이 드디어 됐다. 중복 키 검사도 해주고, 기존 키가 빈칸으로 바뀌는 것도 시각적으로 알려주니 좋다.

### 🎨 UX/UI 디자이너
Apply 후 해상도 변경 시에만 확인 팝업, 나머지는 바로 Close — 적절한 UX 흐름. 키 리바인딩 "..." 대기 상태도 직관적.

### 🔍 QA 엔지니어
WaitForSecondsRealtime 사용은 올바름 (일시정지 중에도 타이머 동작). 버튼 리스너 RemoveAllListeners로 중복 방지. Coroutine 정리도 깔끔.

---

## 최종 판정: **✅ APPROVE**

v1 지적사항 6건 전부 해결. 코드 품질 양호.
