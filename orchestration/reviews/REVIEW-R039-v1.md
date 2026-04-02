# REVIEW-R039-v1: Settings UI (Steam) [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** R-039 Settings UI
> **스펙:** SPEC-R-039
> **판정:** ❌ NEEDS_WORK

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 | 비고 |
|---|---------|------|------|
| 1 | 3개 탭 간 전환 정상 동작 | ✅ | `SwitchTab(int idx)` — 0/1/2 인덱스로 정상 전환 |
| 2 | 드롭다운에 모니터 지원 해상도 동적 표시 | ❌ | 드롭다운 population 코드 없음. SettingsManager는 4개 고정 해상도만 보유 |
| 3 | "적용" 클릭 시 SettingsManager 반영 + 저장 | ⚠️ | `SaveAll()` 호출하지만 `SettingsManager.Apply()` 미호출 — 오디오 반영 누락 가능 |
| 4 | "취소" 클릭 시 변경 전 상태 복원 | ❌ | `Cancel()` 메서드 자체가 없음. `Close()`만 존재하며 값 복원 없음 |
| 5 | 해상도 변경 시 15초 카운트다운 확인 팝업 | ❌ | 미구현 |
| 6 | 키 바인딩 재설정 (클릭→감지→중복검사) | ❌ | Controls 탭에 autoPotionToggle만 존재. 키 리바인딩 로직 없음 |
| 7 | 메인 메뉴 + 인게임 양쪽 접근 가능 | ⚠️ | `MainMenuController`에 SettingsUI 연동 코드 없음 |

---

## 코드 직접 검증 [깊은 리뷰]

**파일:** `Assets/Scripts/UI/SettingsUI.cs` (98줄)

### 구조적 문제
1. **pendingSettings 패턴 미사용** — 스펙은 `pendingSettings`로 임시 보관 후 Apply/Cancel 패턴을 요구하나, 현재 UI 위젯 값을 직접 SettingsManager에 쓰고 있음. Cancel 시 이전 값 복원 불가.
2. **Apply() 후 SettingsManager.Apply() 미호출** — `SaveAll()`만 호출하여 PlayerPrefs에 저장하지만, `Apply()`(오디오 볼륨 AudioManager 반영)를 호출하지 않아 실제 오디오 적용이 안 될 수 있음.
3. **Controls 탭 빈약** — autoPotionToggle만 있고 키 리바인딩 UI 요소 없음.

### 누락된 기능
- 해상도 드롭다운 `Options` 채우기 (`AddOptions` / `ClearOptions`)
- 해상도 변경 확인 카운트다운 (Coroutine/타이머)
- Cancel 버튼 + 리스너
- 키 리바인딩 슬롯 UI + 키 감지 + 중복 검사

---

## 페르소나별 리뷰

### 🎮 캐주얼 게이머
해상도를 잘못 바꿨을 때 15초 확인 팝업이 없으면 화면이 까맣게 되고 게임을 강제종료해야 할 수 있다. "취소" 버튼도 없으니 실수로 적용한 설정을 되돌릴 방법이 없다. 불편하다.

### ⚔️ 코어 게이머
키 리바인딩이 안 되는 건 심각한 문제. 조작 커스터마이징은 기본이다. Controls 탭이 자동 포션 토글 하나로는 "조작 탭"이라 부르기 어렵다.

### 🎨 UX/UI 디자이너
Apply 후 피드백 없이 바로 Close()하는 건 UX적으로 좋지 않다. 적용 성공 피드백(토스트/애니메이션)이 필요. Cancel 플로우 부재는 설계 결함.

### 🔍 QA 엔지니어
`Apply()` 내 `SettingsManager.Apply()` 미호출은 버그. 해상도 드롭다운에 옵션이 안 채워지면 런타임 IndexOutOfRange 가능. 해상도 확인 팝업 없으면 사용자가 지원 안 되는 해상도로 전환 시 복구 불가.

---

## 검증 체계

### 검증 1: 엔진 검증
- ⚠️ SettingsUI 프리팹 미확인 (씬/프리팹에서 SettingsUI 컴포넌트 참조 미발견)
- ✅ 컴파일은 통과

### 검증 2: 코드 추적
- ❌ SPEC 명세 미충족 (7개 중 2개만 완전 충족)
- ⚠️ `SettingsManager.Apply()` 미호출 — 오디오 반영 누락
- ❌ pendingSettings 패턴 미적용

### 검증 3: UI 추적
- ❌ Cancel 버튼 없음 → 되돌리기 불가
- ❌ 해상도 확인 팝업 없음
- ❌ 키 리바인딩 UI 없음

### 검증 4: 플레이 시나리오
- ❌ 해상도 변경 → 확인 없이 적용 → 화면 복구 불가 시나리오

---

## 최종 판정: **❌ NEEDS_WORK**

### 필수 수정사항
1. `Cancel()` 구현 + 이전 값 복원 로직
2. `Apply()` 내 `SettingsManager.Apply()` 호출 추가
3. 해상도 드롭다운 동적 population
4. 해상도 변경 15초 확인 카운트다운 팝업
5. 키 리바인딩 UI + 키 감지 + 중복 검사 (Controls 탭)
6. MainMenuController에서 SettingsUI.Open() 연동
