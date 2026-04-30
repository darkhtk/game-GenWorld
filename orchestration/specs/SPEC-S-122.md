# SPEC-S-122: 🎨 UI 버튼 호버/클릭 SFX 통일 (UIButton 컴포넌트)

> **작성:** 2026-04-30 (Coordinator)
> **출처:** BACKLOG_RESERVE.md S-122 (🎨 UI/SFX, P2)
> **우선순위:** P2 (RESERVE 🎨 P2 최상단)
> **상태:** Spec Drafted → 구현 대기 (Supervisor 🎨 픽업 또는 Dev-Frontend)
> **호출 진입점:** Unity `Button` 컴포넌트가 부착된 모든 UI 오브젝트 — 신규 `UIButton` 컴포넌트가 부모 자동 부착, `OnPointerEnter`/`OnPointerClick`에서 `AudioManager.PlaySFX` 호출

---

## 1. 문제 정의

현재 일부 UI 버튼만 클릭 사운드(`sfx_click`)가 있고 다수는 무음. 호버 사운드는 존재하지 않음. 결과:
1. 버튼 종류별 사운드 일관성 부재 (사용자 입력 피드백 불일치 → 직관성 저하).
2. 호버 사운드 부재 → 키보드/게임패드 포커스 이동 시 청각 피드백 없음.
3. 신규 UI 추가 시 매번 OnClick 인스펙터에서 `AudioManager.PlaySFX("sfx_click")`를 수동 연결 — 누락 빈발.

목표:
- 공통 `UIButton` MonoBehaviour 컴포넌트 신설.
- 인스펙터 의존성 없이 `Button` 자동 감지 → 호버/클릭 SFX 자동 발사.
- 모든 기존 prefab/UI 씬에 일괄 부착 (스크립트 또는 menu item 자동 처리).

---

## 2. 수치/상수 (단일 소스)

| 상수                          | 값       | 위치                  | 비고                                  |
| --------------------------- | ------- | ------------------- | ----------------------------------- |
| `UIButtonSfx.HoverClipId`   | `"sfx_ui_hover"`  | `UIButton` (const)   | 신규 wav 필요                            |
| `UIButtonSfx.ClickClipId`   | `"sfx_click"`     | `UIButton` (const)   | 기존 활용                                |
| `UIButtonSfx.HoverVolume`   | **0.45**          | `UIButton` (const)   | 호버는 자주 발생 → 작게                       |
| `UIButtonSfx.HoverPitchVar` | **±0.06**         | `UIButton` (const)   | 단조 방지                                |
| `UIButtonSfx.HoverDebounce` | **0.06s**         | `UIButton` (const)   | 동일 버튼 재진입 6프레임 내 무시                  |
| `UIButtonSfx.MutePrefabsContaining` | `["LoadingScreen","BootScene"]` | `UIButton` (const) | 자동 부착 제외 패턴 (스플래시는 무음 유지) |

---

## 3. 연동 경로 (변경 범위)

| 파일/위치                                          | 변경 내용                                                                  |
| --------------------------------------------- | ---------------------------------------------------------------------- |
| `Assets/Resources/Audio/SFX/sfx_ui_hover.wav` | **신규** 호버 톤 (60ms, 1800Hz 미세 클릭 + 짧은 페이드)                                |
| `Assets/Scripts/UI/UIButton.cs`               | **신규** MonoBehaviour. `RequireComponent(Button)`. `IPointerEnterHandler`/`IPointerClickHandler` 구현 |
| `Assets/Editor/UIButtonAttachUtility.cs`      | **신규** Menu Item: "Tools/UI/Attach UIButton To All Buttons" — 씬/Prefab 일괄 부착 |
| `Assets/Prefabs/UI/*.prefab`                  | 모든 `Button` 부모에 `UIButton` 컴포넌트 1회 추가 (utility로 처리 → 수동 편집 0)             |
| `Assets/Scenes/*.unity` (UI 포함 씬)             | 동일 (utility로 처리)                                                        |

비변경 (영향 검토만):
- 기존 `OnClick.AddListener` 콜백 — `UIButton`은 Button과 병렬 동작 (이벤트 컨플릭트 없음).
- 기존 인스펙터에 `AudioManager.PlaySFX("sfx_click")` 연결된 OnClick — `UIButton`이 따로 발사하므로 **이중 재생 위험**. 마이그레이션 단계에서 utility가 OnClick 항목에서 `AudioManager.PlaySFX` 라인 자동 제거.

---

## 4. 데이터 구조

### UIButton 핵심 필드 (모두 const, 인스펙터 노출 없음)

```csharp
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    const string HoverClipId = "sfx_ui_hover";
    const string ClickClipId = "sfx_click";
    const float  HoverVolume = 0.45f;
    const float  HoverPitchVar = 0.06f;
    const float  HoverDebounce = 0.06f;

    static float _lastHoverTime;
    static GameObject _lastHoverGo;
}
```

`AudioManager.PlaySFX`는 마스터 SFX 볼륨을 곱한 결과를 사용 — `HoverVolume` 0.45는 추가 어테뉴에이션 (PlaySFX 신호 1× → PlaySFX 시그니처 확장 필요 시 §3 참조).

### sfx_ui_hover.wav 스펙

- 60ms mono 16-bit 22050Hz
- 1800Hz 사인 + 짧은 진폭 envelope (10ms attack / 50ms decay)
- 정점 -3dB, RMS -10dB

---

## 5. UI / 와이어프레임

**UI 변경 없음** (시각적). 기존 모든 Button GameObject에 `UIButton` 컴포넌트가 자동 부착되어 호버/클릭 시 SFX만 추가됨.

```
사용자 마우스 이동 → Button hover
  → IPointerEnter 발화
    → UIButton.OnPointerEnter
      → static debounce 체크 (같은 버튼 0.06s 내 재진입 무시)
      → AudioManager.PlaySFX("sfx_ui_hover", 0.06)

사용자 클릭 → Button click
  → IPointerClick 발화 (Button.OnClick과 병렬)
    → UIButton.OnPointerClick
      → AudioManager.PlaySFX("sfx_click", 0)
```

키보드/게임패드 포커스: Unity EventSystem `Selected` 변경에 추가 hook 필요 시 v2에서 처리. v1은 마우스 호버만 담당.

---

## 6. 세이브 연동

**없음.** SFX 음량 자체는 기존 `_sfxVolume` 마스터 슬라이더(이미 세이브됨)에 종속.

---

## 7. 테스트 계획

### EditMode (NUnit)

1. `UIButtonTest.OnPointerEnter_PlaysHoverClip()` — Mock AudioManager, hover 진입 시 `sfx_ui_hover` 1회 재생 검증.
2. `UIButtonTest.OnPointerEnter_DebouncedWithin60ms_NoDoublePlay()` — 동일 버튼 30ms 간격 두 번 진입 → 1회만 재생.
3. `UIButtonTest.OnPointerClick_PlaysClickClip()` — 클릭 시 `sfx_click` 1회.
4. `UIButtonTest.OnPointerClick_DoesNotBlockButtonOnClick()` — 기존 `Button.onClick` 콜백이 정상 호출되는지 확인.

### PlayMode 수동
- 메인메뉴 → 옵션 → 인벤토리 → 크래프팅 → 다이얼로그 모든 버튼 호버/클릭 시 사운드 일관 재생 확인.
- 빠른 마우스 슬라이드 (10개 버튼 위 0.5s 동안 통과) → 호버 사운드 폭주 없음 (debounce 검증).
- `LoadingScreen`/`BootScene` 진입 시 무음 유지.

---

## 8. 호출 진입점 (재명시)

| 진입점                              | 트리거 조건                                       |
| -------------------------------- | -------------------------------------------- |
| `UIButton.OnPointerEnter`        | EventSystem이 마우스/게임패드 호버 감지                  |
| `UIButton.OnPointerClick`        | Unity Button 클릭 (Submit/마우스 좌클릭)             |
| `AudioManager.PlaySFX(clipName, pitchVariation)` | UIButton 내부에서 hover/click 분기로 호출 |

신규 UI 진입점 없음 — 기존 Button GameObject 위에 컴포넌트 자동 부착.

---

## 9. 작업 분담

| 영역 | 담당 | 산출물 |
| --- | --- | --- |
| `sfx_ui_hover.wav` 생성 | Asset/QA (Supervisor 🎨 픽업) | 60ms 1800Hz 호버 톤 |
| `UIButton.cs` | Dev-Frontend | MonoBehaviour + 인터페이스 구현 |
| `UIButtonAttachUtility.cs` | Dev-Frontend | Editor menu item — 씬/Prefab 자동 부착 + OnClick PlaySFX 라인 제거 |
| Prefab/씬 일괄 부착 실행 | Asset/QA | utility 1회 실행 → diff 확인 |
| EditMode 테스트 4건 | Dev-Frontend | NUnit 테스트 |

---

## 10. 리스크

- **기존 OnClick 인스펙터에 AudioManager.PlaySFX("sfx_click") 연결된 항목 → 이중 재생.** Utility가 자동 제거하지만 비표준 표기(`PlaySFX("ui_click")` 등)는 수동 처리 필요. PR 전에 grep으로 잔존 검증.
- **호버 사운드 폭주:** 빠른 마우스 이동 시 0.06s debounce 부족 가능성. 실측 후 0.08~0.1s까지 조정.
- **EventSystem 부재 씬:** 자동 부착 utility는 EventSystem 미존재 씬에서 경고 로그만 — `EventSystemEnsurer.cs` 활용으로 BootScene 제외 모든 UI 씬에 EventSystem 보장.
- **포커스 이동(키보드 Tab/방향키)에서 호버 SFX 미발화:** v1은 의도적으로 미지원. v2에서 `EventSystem.current.currentSelectedGameObject` 폴링으로 추가.

---

## 11. 완료 조건 (DoD)

1. `sfx_ui_hover.wav` 생성 + Resources/Audio/SFX 배치 + meta.
2. `UIButton.cs` + `UIButtonAttachUtility.cs` 신규.
3. Utility 실행 → 모든 UI prefab/씬에 `UIButton` 부착 + 기존 OnClick PlaySFX 라인 제거 diff 검토.
4. EditMode 테스트 4건 GREEN.
5. PlayMode 수동 검증 — 메인메뉴/인벤토리/다이얼로그/옵션 호버·클릭 사운드 일관, debounce 동작 확인.
6. `MutePrefabsContaining` 패턴 (LoadingScreen/BootScene) 무음 유지 확인.
