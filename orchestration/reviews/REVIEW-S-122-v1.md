# REVIEW-S-122-v1: 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1) [깊은 리뷰]

**Task:** S-122 (Phase 1: 자산 + 컴포넌트 + 테스트, Phase 2 = S-148 부착)
**리뷰 일시:** 2026-04-30 (Client 3회차)
**대상 커밋:** 4b3714b — feat(S-122): UI button hover/click SFX 통일 (Phase 1)
**리뷰 모드:** [깊은 리뷰] — 5/N 깊은 리뷰 cadence (S-101-v3 / S-084-v2 / S-120-v1 / S-121-v1 다음). SPEC vs 구현 항목별 대조 + Phase 분할 정합성 + 누락 상수 영향 분석.
**SPEC 참조:** specs/SPEC-S-122.md (작성됨, 11개 절)

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `Assets/Audio/Generated/sfx_ui_hover.wav` (신규, 5336 bytes) + .meta | 60ms 1800→1500Hz subtle chirp + HP noise tail, peak 0.55 (`gen_ui_hover_sfx.py` seed 20122) |
| `Assets/Resources/Audio/SFX/sfx_ui_hover.wav` (사본) + .meta | Resources.Load 호환 (S-117 fixup 교훈 동시 적용 — Generated만 두면 런타임 no-op) |
| `Assets/Scripts/Core/GameConfig.cs:68-78` | `UI` 정적 클래스 신규 — `ButtonHoverSfxName="sfx_ui_hover"`, `ButtonClickSfxName="sfx_click"`, `ButtonHoverSfxVolume=0.55f`, `ButtonClickSfxVolume=0.85f`, `ButtonSfxEnabled=true` |
| `Assets/Scripts/UI/UIButtonSfx.cs` (신규, 64라인) | `[RequireComponent(Button)] [DisallowMultipleComponent]` MonoBehaviour. `IPointerEnterHandler`/`IPointerClickHandler`. `interactable` 가드, per-instance override 4필드 (이름/볼륨×hover/click) + `playOnHover`/`playOnClick` 토글. `AudioManager.Instance?.PlaySFXScaled` 호출 |
| `Assets/Tests/EditMode/UIButtonSfxConfigTests.cs` (신규, 45라인) | 5 테스트: HoverName / ClickName(reused) / HoverVolume(≤0.6 subtle) / ClickVolume>HoverVolume / Enabled=true |

비변경 (의도적 deferred):
- 기존 prefab/씬에 `UIButtonSfx` 부착 0건 — Phase 2 (S-148 신규 RESERVE) 분할.
- `UIButtonAttachUtility.cs` (Editor menu item) 미생성 — S-148.
- 기존 OnClick PlaySFX 라인 자동 제거 미수행 — S-148.

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| `sfx_ui_hover.wav` Resources 배치 | ✅ | Generated + Resources/Audio/SFX 양쪽. `AudioManager.GetClip` Resources.Load 경로 정상 작동 (S-117 교훈 적용) |
| `.meta` 2개 (Generated + Resources) GUID 안정 | ✅ | 23라인 표준 AudioImporter |
| `UIButtonSfx` 어셈블리 배치 | ✅ | `Assets/Scripts/UI/` — UI 어셈블리 |
| `[RequireComponent(Button)]` | ✅ | Unity가 prefab/씬 부착 시 Button 자동 추가 |
| `[DisallowMultipleComponent]` | ✅ | 중복 부착 시 Unity 에디터 차단 — Phase 2 utility 멱등성 보장 |
| EventSystem 의존 | ⚠️ | `IPointerEnter/Click`는 EventSystem 없으면 발화 안 됨. SPEC §10 "EventSystem 부재 씬" 리스크 명시. Phase 2 utility에서 점검 권장 |

### 검증 2: 코드 추적 (SPEC vs 구현 대조)
| SPEC 항목 | SPEC 명세 | 구현 결과 | 평가 |
|---------|---------|---------|------|
| §2 `HoverClipId` | `"sfx_ui_hover"` | `GameConfig.UI.ButtonHoverSfxName="sfx_ui_hover"` | ✅ |
| §2 `ClickClipId` | `"sfx_click"` | `GameConfig.UI.ButtonClickSfxName="sfx_click"` (재사용) | ✅ — 자산 비대 회피 |
| §2 `HoverVolume` | **0.45** | **0.55** | ⚠️ — 0.10 상향. SPEC 추정값. PlayMode 청취 후 조정 의도. 테스트가 ≤0.6으로 가드 — 합리적 |
| §2 `HoverPitchVar` | **±0.06** (단조 방지) | **❌ 미구현** | ⚠️ **SPEC 결손** — 빠른 호버 반복 시 단조감. P3 follow-up 후보 |
| §2 `HoverDebounce` | **0.06s** (6프레임 재진입 무시) | **❌ 미구현** | ⚠️ **SPEC 결손 + §10 명시 리스크 미해결** — 빠른 마우스 슬라이드 → 호버 SFX 폭주 가능. 이는 §10에서 직접 거명한 위험. P2 follow-up 권장 |
| §2 `MutePrefabsContaining` | `["LoadingScreen","BootScene"]` | **❌ 미구현** | ⚠️ Phase 1은 부착 0이므로 실효 영향 X. Phase 2(S-148) utility에서 처리 필수 |
| §3 `UIButtonAttachUtility.cs` | Menu Item 신규 | **❌ Phase 2 deferred (S-148)** | ⚠️ 분할 명시 — 합리적 (대량 prefab/씬 변경 회피) |
| §3 prefab/씬 일괄 부착 | utility 1회 실행 | **❌ Phase 2 deferred** | ⚠️ Phase 1만으로는 사용자 청취상 무변화 — 의도된 분할 |
| §3 OnClick PlaySFX 라인 자동 제거 | utility | **❌ Phase 2 deferred** | ⚠️ 미수행 시 이중 재생 위험 (§10). Phase 2에서 grep 검증 필수 |
| §3 `AudioManager.PlaySFX(string, float volumeScale)` 오버로드 | 신규 또는 기존 | **`PlaySFXScaled(string, float)` 별도 메서드** (S-121 추가) | ✅ — 기존 `PlaySFX(string, float pitchVariation)` 시그니처 충돌 회피. SPEC 가정보다 안전 |
| §4 const 인스펙터 노출 X | 모두 const | **`SerializeField` 4개 + 토글 2개로 확장** | ⚠️ SPEC 변경 — per-instance override 추가 (PauseMenuUI 등 자체 SFX 재생 버튼 회피용). 합리적 확장이나 SPEC §4와 차이 명시 필요 |
| §7 EditMode 테스트 4건 | (1) PlaysHoverClip, (2) Debounced, (3) PlaysClickClip, (4) DoesNotBlockButtonOnClick | **5건 config-only** (Name×2 + Volume×2 + Enabled×1) | ❌ **SPEC §7 행동 검증 0건** — singleton DI 부재 사유 테스트 파일 주석 명시 (S-121 동일 패턴). DoD #4 부분 충족 |
| §11 DoD 1 자산 + .meta | 필수 | ✅ | hover만 (click은 기존 재사용) |
| §11 DoD 2 `UIButton.cs` + `UIButtonAttachUtility.cs` | 둘 다 | ⚠️ UIButton만 (`UIButtonSfx`로 명명, AttachUtility 없음) | Phase 2 deferred |
| §11 DoD 3 utility 실행 + diff 검토 | 필수 | ❌ Phase 2 | |
| §11 DoD 4 EditMode 4건 GREEN | 필수 | ⚠️ 5건 config-only (SPEC §7 행동 검증 0건) | 트레이드오프 transparent |
| §11 DoD 5 PlayMode 수동 검증 | 필수 | ❌ 부착 0 → 검증 불가 | Phase 2 후 |
| §11 DoD 6 MutePrefabs 무음 유지 | 필수 | ❌ Phase 2 | |

**총평:** SPEC 11개 절 중 §1/§2(부분)/§3(부분)/§4(확장)/§7(deferred)/§11(부분) 충족. **§2의 HoverPitchVar/HoverDebounce 누락은 SPEC 명시 리스크와 직결** — Phase 2에서 부착되면 즉시 표면화. Phase 분할 자체는 합리적(대량 prefab/씬 diff + 이중 재생 위험 회피).

### 검증 3: UI 추적
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| Button.onClick과 OnPointerClick 충돌 | ✅ | 서로 다른 인터페이스 분기, EventSystem이 둘 다 호출. SPEC §3 명시 |
| `_button.interactable` 가드 | ✅ | 비활성 버튼 호버/클릭 시 SFX 미재생 — 정상 UX |
| per-instance override 의도 | ✅ | PauseMenuUI 닫기버튼처럼 자체 sfx_menu_close 트리거하는 곳은 `playOnClick=false` 가능 — Phase 2 부착 정책 시 활용 |
| Phase 1 사용자 청취 영향 | ✅ | 0건 (부착 X) — 의도적 안전 분할 |

### 검증 4: 사용자 시나리오
| 시나리오 | 예상 결과 | 평가 |
|---------|---------|------|
| Phase 1 단독 빌드 | 사용자 청취 변화 0 | ✅ 의도 일치 |
| Phase 2(S-148) 부착 후 메인메뉴 호버 | sfx_ui_hover 재생 | 준비됨 |
| 빠른 마우스 슬라이드 (10버튼/0.5s) | **debounce 부재 → 10회 PlaySFX** | ❌ §10 리스크 표면화 — Phase 2 전 follow-up 필수 |
| LoadingScreen 진입 시 | **MutePrefabs 부재 → 호버 시 sfx 재생 (의도 외)** | ❌ Phase 2 utility에서 처리 필수 |
| OnClick 인스펙터에 PlaySFX 연결된 버튼 | **이중 재생 위험** | ❌ Phase 2 utility 자동 제거 필수 |
| 옵션창 SFX 볼륨 0 | AudioManager 마스터 볼륨 곱 → 무음 | ✅ |

---

## 페르소나 리뷰

### 🎮 하늘 (캐주얼 게이머)
- Phase 1 단독으로는 게임이 변한 게 없어. 어차피 들리는 거 0이라서 평가할 게 없네 — 안 깨졌으니 됐다.
- Phase 2 들어가면 빠른 마우스로 인벤 메뉴 훑을 때 사운드 따다다다닥 나면 짜증날 듯. **debounce 꼭 넣어줘.**
- 호버 사운드 0.55면 화면 한쪽 누르고 있을 때 너무 안 들리진 않을까? 0.45가 SPEC인데 0.55로 올린 이유가 청취 후 조정인 거면 OK. 일단 Phase 2 청취 후 결정.

### ⚔️ 태현 (코어 RPG 게이머)
- 분할이 옳다. 4b3714b 하나에 prefab 200개 + 씬 30개 부착 + OnClick 자동 제거가 다 들어왔다면 review 불가능했을 것. **Phase 1 = 컴포넌트 + 자산만, Phase 2 = 부착 + 마이그레이션** 이라는 분할은 표준적 폴리시 PR 패턴이다.
- per-instance override 4필드 + playOn 토글 2개 추가가 SPEC §4(const only)에서 벗어나지만 **PauseMenuUI/MainMenuController 같은 자체 SFX 재생 버튼**이 코드베이스에 이미 존재하므로 override 없으면 Phase 2에서 이중 재생 폭탄 — **SPEC 의도 외 확장이 합리적이다.** SPEC 갱신 권장.
- `PlaySFXScaled` 사용은 S-121 신규 메서드 재사용. signature 충돌 회피 정합. ⚔️

### 🎨 수아 (UX/UI 디자이너)
- **HoverPitchVar 누락이 가장 아쉽다.** 동일 hover SFX가 단조롭게 반복되면 청각 피로 가속 — SPEC §2가 ±0.06으로 적절히 명시했는데 구현이 누락. 후속 ticket로 반드시 챙겨야 한다.
- HoverDebounce 0.06s 누락은 **사용자 입력 폭주 방지** 관점에서 더 큰 문제. SPEC §10에서 직접 risk로 적었는데 구현 안 함 → Phase 2 부착 즉시 표면화 보장. **P2 follow-up 강력 권장.**
- 0.55 호버 볼륨은 0.45보다 살짝 높은데 sfx_ui_hover 60ms peak 0.55 자산 자체가 가벼운 톤이면 합쳐서 적절할 가능성. PlayMode에서 청취 후 결정.
- 테스트가 click>hover를 명시적으로 단언한 점은 좋다 — affordance feedback 의도가 코드로 박혀있다.

### 🔍 준혁 (QA 엔지니어)
- `[RequireComponent(Button)] [DisallowMultipleComponent]` 안전성 OK. Awake에서 GetComponent<Button>() 1회. 런타임 NRE 가능성 0.
- `_button != null && !_button.interactable` 가드 — 비활성 버튼 호버 SFX 차단 정상.
- **NRE 시나리오:** AudioManager.Instance가 null인 게임 부팅 직전에 EventSystem이 활성된 상태로 hover 진입 시 → `Instance?.PlaySFXScaled` null-conditional로 안전. ✅
- **테스트 5건 config-only는 행동 검증 0건.** singleton DI 부재로 PlayMode 의존 — S-121과 동일 패턴, 일관성 OK이나 PlayMode 안 돌리면 회귀 잡을 수 없음. PlayMode 자동화 시퀀스 필요 (별도 ticket).
- **debounce 부재 = 메모리 측면 안전** (PlaySFX는 OneShot으로 AudioSource 풀에서 처리). 하지만 청각 피로/UX 측면에서 critical. 별도 follow-up.

---

## SPEC 결손 사항 (RESERVE 신규 등재 권장)

| 신규 ID 후보 | 항목 | 우선순위 | 사유 |
|---|---|---|---|
| (S-149?) | `UIButtonSfx` Hover debounce 0.06s 추가 | P2 | SPEC-S-122 §2/§10 명시. Phase 2 부착 직전 필수 — 호버 폭주 방지 |
| (S-150?) | `UIButtonSfx` Hover pitch variation ±0.06 | P3 | SPEC §2 명시. 단조 방지 |
| (S-151?) | `MutePrefabsContaining` 가드 (LoadingScreen/BootScene) | P3 | SPEC §2/§11 명시. Phase 2 utility에서 처리 가능하나 코드 측 명시적 가드도 권장 |
| (S-152?) | `UIButtonSfx` 행동 PlayMode 테스트 (Phase 2 부착 후) | P3 | SPEC §7. Mock AudioManager 또는 PlayMode 자동화 |

→ Coordinator가 RESERVE에 흡수 권장. S-148(Phase 2 부착)이 진행되기 **전에** S-149(debounce)는 처리 필수.

---

## 결론

**✅ APPROVE — Phase 1 분할 합리적, SPEC §2 결손은 Phase 2 진입 전 follow-up 필수**

- Phase 1 단독으로 사용자 청취 영향 0 → 회귀 위험 0 → 분할 PR로 안전.
- SPEC vs 구현 대조 시 §2의 `HoverPitchVar`/`HoverDebounce`/`MutePrefabsContaining` 누락이 SPEC §10의 명시적 risk와 직결되나 Phase 2 시점까지 follow-up 가능.
- `PlaySFXScaled` 사용은 SPEC §3 가정(`PlaySFX(string,float volumeScale)` 오버로드)보다 안전(시그니처 충돌 회피).
- SerializeField override 추가는 SPEC §4(const only)에서 벗어나지만 코드베이스 실제 사용처(자체 SFX 버튼) 고려 시 합리적 확장. SPEC 갱신 권장.
- 테스트가 config-only인 점은 S-121과 동일 패턴, singleton DI 부재 트레이드오프 transparent.

**4/4 페르소나 만장일치 APPROVE** — 단, Phase 2(S-148) 진입 전 RESERVE follow-up (debounce/pitch/MutePrefabs) 처리 권장 명시.

**[깊은 리뷰] 5/N** — 다음 깊은 리뷰는 6번째 리뷰 후보에 적용.
