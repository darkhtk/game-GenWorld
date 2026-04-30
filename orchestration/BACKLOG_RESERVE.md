# Backlog Reserve

> **최종 갱신:** 2026-04-30 (Coordinator 9회차 — S-122 APPROVE 흡수 ✅ + S-127 APPROVE 흡수 ✅ + S-128 Developer 9회차 In Review 진입 ~~취소선~~/👀 정리 + S-123 Supervisor 8회차 mid-flight ~~취소선~~/👀 유지. **Client S-122 [깊은 리뷰] 후속 4건 신규 등재(S-149~S-152)**. 잔여 24건(🎨 4 + 🐛 20))
> **방향:** polish (UX/UI > 비주얼/오디오 > 성능)
> **태그 규약:** `🎨` = 에셋(스프라이트/SFX/BGM/이펙트) 동반 태스크. 감독관이 우선 픽업.

---

## 🎨 에셋 동반 태스크 (감독관 우선)

| ID    | 태스크                                                   | 우선순위 | 영역      | 비고                                                  |
| ----- | ----------------------------------------------------- | ---- | ------- | --------------------------------------------------- |
| ~~S-114~~ | ~~🎨 회피 모션 잔상 이펙트 스프라이트 추가~~ ✅ 2026-04-30          | P2   | VFX     | DONE — vfx_dodge_trail 4f sprite + DodgeVFX ghost layer (commit 6ab7a5c) |
| ~~S-115~~ | ~~🎨 데미지 텍스트 폰트 아웃라인/그림자 강화~~ ✅ 2026-04-30        | P2   | UI      | DONE — outline 0.2 black + Underlay 드롭섀도우 (DamageText.cs)             |
| ~~S-116~~ | ~~🎨 스킬 쿨다운 회복 SFX 누락~~ ✅ 2026-04-30                | P2   | SFX     | DONE — sfx_cooldown_ready.wav (220ms 1320Hz 벨톤) + HUD.UpdateCooldowns transition trigger |
| ~~S-117~~ | ~~🎨 몬스터 처치 시 골드 드롭 사운드 (등급별 3종)~~ ✅ 2026-04-30 | P3 | SFX | DONE — sfx_coin_small.wav (150ms 단발), sfx_coin_pile.wav (280ms 3-pluck), sfx_coin_burst.wav (500ms arpeggio+sparkle) + CombatRewardHandler def.rank 분기 |
| ~~S-118~~ | ~~🎨 아이템 획득 팝업 BGM 더킹 (-6dB, 0.4초)~~ ✅ 2026-04-30 | P3   | Audio   | DONE — AudioManager.DuckBGM(-6f, 0.4f) coroutine + CombatRewardHandler drops>0 호출. AudioMixer 미사용으로 BGM source volume × _duckMultiplier 직접 조작, 0.08s fade in/hold/0.08s fade out |
| ~~S-119~~ | ~~🎨 레벨업 파티클 이펙트~~ ✅ 2026-04-30                  | P2   | VFX     | DONE — vfx_levelup_burst 8f sprite + LevelUpVFX 16-spark 방사 (1.0s) |
| ~~S-120~~ | ~~🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s)~~ ✅ 2026-04-30          | P3   | Audio   | DONE — GameConfig.Audio 신규(BossRegionIds/BgmTransitionBossEnter=1.5s/Default=1.0s/CrossfadeDualSource=true) + AudioManager.bgmSourceB + CrossfadeBGMDual(동시 ramp, 무음 갭 X) + GameManager.PlayRegionBGM fadeTime 분기. DuckBGM과 충돌 없음(크로스페이드 활성 시 _duckMultiplier만 갱신, 매 프레임 BgmTargetVolume 재계산). EditMode 7건. |
| ~~S-121~~ | ~~🎨 NPC 대화 시작/종료 SFX~~ ✅ 2026-04-30                  | P3   | SFX     | DONE — sfx_dialogue_open.wav (200ms paper-flip) + sfx_dialogue_close.wav (180ms book-thud) Generated+Resources 양쪽 배치, GameConfig.Audio 5상수, AudioManager.PlaySFXScaled 신규 (volumeScale 오버로드), DialogueUI.Show/Hide 훅. 부수: S-117 코인 SFX Resources 누락 fixup(런타임 PlaySFX no-op 버그). EditMode `DialogueAudioConfigTests` 5건. |
| ~~S-122~~ | ~~🎨 UI 버튼 호버/클릭 SFX 통일 (UIButton 컴포넌트)~~ ✅ 2026-04-30 (Phase 1) | P2   | UI/SFX  | DONE — REVIEW-S-122-v1 APPROVE 4/4 페르소나 만장일치 [깊은 리뷰 5/N] / 4b3714b (BOARD #14 ✅ 이동 완료). Phase 1 자산+컴포넌트+테스트. **Phase 2 = S-148** 기존 Button 일괄 부착(Editor tooling). Client [깊은 리뷰] SPEC §2/§10 결손 3건 → **S-149/S-150/S-151/S-152 신규 RESERVE 등재**(S-149 debounce는 S-148 진입 전 필수). |
| ~~S-123~~ | ~~🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3)~~ ✅ 2026-04-30                       | P3   | UI      | DONE — REVIEW-S-123-v1 APPROVE 4/4 페르소나 만장일치 / a10603c (BOARD #17 ✅ 이동 완료). `GameConfig.UI.InventorySlotEmptyAlpha=0.3f` / `InventorySlotFilledAlpha=1.0f` + `InventorySlotUI.ApplyEmptyAlpha(bool)` 헬퍼 + Clear()/SetItem() 훅. 부수: gradeFrameImage.enabled=false 회귀 방지. EditMode 6건. |
| S-140 | 🎨 보스 처치 시 화면 흔들림 + 승리 코드 SFX (sfx_boss_die_chord 1.2s) | P2   | VFX/SFX | 보스 처치가 일반 몬스터와 동일 피드백. CinemachineImpulse + 코드 진행 SFX |
| S-141 | 🎨 포션 사용 SFX 종류별 분리 (sfx_potion_hp/mp/buff 3종 250ms)        | P3   | SFX     | 현재 sfx_potion 단일. ItemUseSystem.Use 분기에서 itemDef.subtype 기반 호출 |
| S-142 | 🎨 NPC 대화 시작 시 BGM 페이드 다운 (-3dB, 유지, Hide 시 복원)         | P3   | Audio   | 음성/효과 가독성. AudioManager에 SetBGMDuck(float dB, bool sustain) 추가 |
| S-143 | 🎨 발걸음 SFX 지형별 분리 (grass/stone/wood, 0.4s 간격)                | P3   | SFX     | 현재 발걸음 SFX 없음. PlayerController.Update 이동 distance 누적 + Tilemap 지형 lookup |

## 🐛 코드 품질 / UX

| ID    | 태스크                                                  | 우선순위 | 영역         | 비고                                                |
| ----- | --------------------------------------------------- | ---- | ---------- | ------------------------------------------------- |
| ~~S-124~~ | ~~인벤토리 드래그 앤 드롭 시각 피드백~~ ✅ 2026-04-30 | P2   | UI         | DONE — REVIEW-S-124-v1 APPROVE / ee545e1 (BOARD ✅ 이동 완료) |
| ~~S-125~~ | ~~SkillTree 미해금 노드 해금 조건 결합 표시~~ ✅ 2026-04-30      | P2   | UI         | DONE — REVIEW-S-125-v1 APPROVE / 8bd46ba (BOARD #10 ✅ 이동 완료). SkillRowUI.UpdateState 잠금 분기에서 레벨/포인트 deficit 결합 표기 (`Lv.5+ -2pt`). 선행 스킬은 SkillDef 미정의 → 향후 별도 SPEC. |
| ~~S-126~~ | ~~옵션창 ESC로 닫기 일관성~~ 👀 2026-04-30                          | P2   | UI         | In Review (Developer 7회차) — UIManager ESC 분기 재구조화 + IsAnyPanelOpen NPC/Dialogue 포함 + HideAll dialogue OnClose 동반 호출. SPEC 부재(specs 참조 N) → Coordinator 후속 SPEC 작성 검토. |
| ~~S-127~~ | ~~미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)~~ ✅ 2026-04-30                        | P3   | UI         | DONE — REVIEW-S-127-v1 APPROVE 4/4 페르소나 만장일치 / c88f1c2 (BOARD #15 ✅ 이동 완료). `MinimapUI.ClassifyNpcMarkerColor` 분류 우선순위(open_shop/Quest/Default) + `ApplyNpcMarkerColor` private static + 7건 테스트. 후속 5건 BACKLOG 후보(색맹 대응 마커 모양/Image 캐싱/퀘스트 상태별 색상/시각 거리 향상/PlayMode 자동화) — 다음 idle 회차 등재 검토. |
| ~~S-128~~ | ~~퀘스트 추적 UI 진행률 바 (현재 숫자만)~~ 👀 2026-04-30                            | P2   | UI         | In Review (Developer 9회차) — `QuestProgressBarBuilder.Build(progress, required) → string` static + GameConfig.UI 5상수 추가(Cells=10/MetColor/UnmetColor/BgColor/Enabled) + QuestUI.AddActiveEntry requirements/killRequirements 두 라인 게이지 삽입. SPEC-S-128 §4 와이어프레임 충실. EditMode 5건. specs 참조 Y. |
| S-129 | GameManager 싱글톤 null 체크 강화 (Awake 순서 의존)             | P2   | Core       | 씬 전환 직후 NRE 산발                                   |
| S-130 | InputManager 입력 폴링 GC 알로크 측정/제거                     | P3   | Performance| Update 마다 GetKey 다중 호출. 캐싱 + 1회만                  |
| S-131 | EnemySpawner 풀 검증 (씬 전환 후 미반환 추적)                   | P2   | Systems    | 메모리 누수 의심                                         |
| S-132 | SaveSystem 비동기 저장 진행 표시 (스피너 + 완료 토스트)              | P2   | UI/Core    | 길게 걸릴 때 멈춘 듯 보임                                  |
| S-133 | ItemDatabase 아이템 ID 충돌 검증 (시작 시 1회)                  | P3   | Data       | JSON 추가 시 사일런트 덮어쓰기 위험                            |
| S-134 | Tilemap 청크 비활성화 거리 조정 (현재 너무 가까움)                   | P3   | Performance| LOD 거리 측정 후 +30%                                  |
| S-135 | ParticleSystem 풀 사이즈 측정 → 보스전 동시 50+ 처리              | P3   | Performance| 보스 페이즈 전환 시 인스턴스화 스파이크                            |
| S-136 | `MonsterAttackPatternSelector` 정의 위치 추적/누락 보정              | P2   | Systems    | REVIEW-S-101-v3 켄지 메모. MonsterController.cs:307 호출되나 클래스 정의 grep 미검출 — Unity Editor 컴파일 후 누락 확인 시 신규 클래스 작성 또는 호출부 정리 |
| S-137 | 인벤토리 드래그 중 강제 닫힘 시 CancelDrag 안전망                    | P3   | UI         | REVIEW-S-124-v1 켄지 메모. 드래그 중 InventoryUI.Hide() 또는 씬 전환 시 dragIcon 잔존 위험 — OnDisable에서 CancelDrag 호출 보장 |
| S-138 | InventoryUI ghost alpha const 분리 (`InventorySlotGhostAlpha=0.3f`)   | P3   | UI         | REVIEW-S-124-v1 하루카 메모. 매직 넘버 0.3f 직접 박혀있음 — `GameConfig.UI` 또는 InventorySlotUI const로 단일화 |
| S-139 | 인벤토리 드롭 성공 시 pop 애니메이션 (scale 1.0→1.15→1.0, 0.12s)        | P3   | UI         | REVIEW-S-124-v1 하루카 제안. 드롭 직후 시각적 컨펌 부족 — DOTween 또는 코루틴 한 번 |
| S-144 | `MonsterSpawner.SpawnEventMonster` IsNullOrEmpty(eventId) 가드 + SkillVFX/AudioManager 호출 통합 | P3 | Systems | REVIEW-S-084-v2 ⚔️ 켄지 권고. 현재 `SpawnEventMonster(def, pos, eventId)`는 eventId 검증 없이 그대로 EventOriginId에 대입 → null/empty가 들어오면 untagged 몬스터와 식별 불가. 진입점에서 `string.IsNullOrEmpty(eventId)` 가드 + 같은 시점 SkillVFX/AudioManager.PlaySFX 트리거 통합(현재 분산). |
| S-145 | `WorldEventEndEvent_PreservesUntaggedMonsters` 회귀 테스트 + `[SetUp] EventBus.Clear()` | P3 | Tests | REVIEW-S-084-v2 🔍 QA 권고. EditMode `WorldEventCleanupTests`에 untagged-preservation 직접 단언 부재 — eventId="raid_001" 정리 시 EventOriginId=null 몬스터가 보존되는지 명시 단언 추가. + `[SetUp]` 에 `EventBus.Clear()` 호출하여 테스트 간 핸들러 누수 방지. |
| S-146 | invasion / elite_spawn / merchant 핸들러 SPEC 작성 → SpawnEventMonster 호출처 연결 | P2 | Systems/Spec | REVIEW-S-084-v2 🎮/⚔️ 권고. S-084 Phase 2가 cleanup 인프라만 깔고 spawn 측 프로덕션 호출처 0. WorldEventSystem이 emit하는 type별 핸들러(spawn 위치/수량/난이도 보정) SPEC 작성 후 `SpawnEventMonster` 연결. SPEC-S-146.md 신규 필요 — Coordinator 선제 작성 후보. |
| S-147 | `RegionManager.SwitchRegion` / `SaveSystem.OnSceneUnload` → `WorldEventSystem.ForceEndActiveEvent` 와이어링 | P3 | Systems | REVIEW-S-084-v2 🔍 QA 권고. Phase 1에서 `ForceEndActiveEvent` 인프라 완성됐으나 호출처는 자연 만료(`EndEvent`)뿐 — 씬 전환/세이브 로드 시점에 강제 종료 호출 없음 → 잔존 이벤트 몬스터가 다음 씬으로 이월될 잠재 리스크. 1라인씩 훅 추가. |
| S-148 | 🎨 UIButtonSfx 일괄 부착 (S-122 Phase 2) — Editor 자동 부착 스크립트 | P2 | UI/Tooling | S-122 Phase 1 후속. `UIButtonSfx` 컴포넌트는 만들었으나 어느 Button 에도 부착 안 됨 → 사용자 청취상 무변화. Editor 스크립트 작성 필요: 모든 .prefab + .unity 스캔, `Button` 있고 `UIButtonSfx` 없는 모든 곳에 부착 + asset 변경 보존. 시스템성 Button(이미 sfx_cancel/sfx_menu_close 직접 호출하는 곳: PauseMenuUI 닫기버튼, MainMenuController 등)은 `playOnClick=false` 설정해 중복 재생 회피. 검수 후 PR 분리. **선결 조건: S-149 우선 처리(SPEC §10 risk).** |
| S-149 | UIButtonSfx HoverDebounce 0.06s (마우스 호버 폭주 방지) | P2 | UI/Audio | REVIEW-S-122-v1 [깊은 리뷰] 결손 발견 — SPEC-S-122 §10 명시 risk 직결("호버 SFX 폭주 시 청각 피로"). `UIButtonSfx.OnPointerEnter`에 `Time.unscaledTime - _lastHoverPlayTime < HoverDebounce` 가드 추가 + `GameConfig.UI.ButtonHoverDebounce=0.06f` 신규 상수. **S-148 Phase 2 진입 전 필수 처리** — 부착 후 무가드 발화 시 음향 피로 즉시 발생. EditMode 2건(debounce 차단/통과) + 매뉴얼 PlayMode 1건(빠른 호버 이동). |
| S-150 | UIButtonSfx HoverPitchVar ±0.06 (단조 방지) | P3 | UI/Audio | REVIEW-S-122-v1 [깊은 리뷰] 결손 발견 — SPEC-S-122 §2 명시("호버 단조 회피"). 현재 `PlaySFXScaled(string,float)`은 pitch 변동 미지원. `AudioManager.PlaySFXScaledPitched(string name, float volume, float pitchOffset)` 신규 오버로드 + 임시 `AudioSource.pitch = 1f + Random.Range(-0.06f, 0.06f)` 적용 후 복원(또는 OneShot 비호환 시 별도 풀). `GameConfig.UI.ButtonHoverPitchVar=0.06f`. EditMode 2건(범위 단언). |
| S-151 | UIButtonSfx MutePrefabsContaining 가드 (LoadingScreen/BootScene 회피) | P3 | UI/Audio | REVIEW-S-122-v1 [깊은 리뷰] 결손 발견 — SPEC-S-122 §3 명시("진입 화면에서 사용자가 의도하지 않은 호버 발화 회피"). `GameConfig.UI.MutePrefabsContaining=new[]{"Loading","Boot","Splash"}` 신규 + `UIButtonSfx.OnEnable`에서 부모 prefab name 검사 → 매칭 시 자동 비활성. EditMode 3건(매칭/비매칭/대소문자). |
| S-152 | UIButtonSfx PlayMode 행동 테스트 (S-148 부착 후) | P3 | UI/Tests | REVIEW-S-122-v1 [깊은 리뷰] cadence 메모. EditMode `UIButtonSfxConfigTests` 5건은 상수 검증만 — 실제 `OnPointerEnter` → `AudioManager.PlaySFXScaled` 호출 카운트는 PlayMode에서만 검증 가능(MonoBehaviour 메시지 + 싱글톤 초기화). PlayMode 4건: hover 1회/click 1회/interactable=false 무발화/debounce 차단. **S-148 부착 완료 후 진입.** |

---

## ❌ Rejected 재작업 (BOARD에서 회수)

> 현재 비어 있음. (S-101은 BOARD ❌ Rejected에서 직접 처리 중, S-084는 BOARD 🔧 In Progress 보류 상태 — RESERVE 중복 등재 제거됨.)

---

## 작성 가이드

- 신규 항목 ID는 `S-XXX` 연속 (현재 S-147까지). 다음 번호 S-148부터.
- `🎨` 태그는 신규 스프라이트/SFX/BGM/이펙트 자산이 필요한 태스크에만.
- 우선순위: `high` > `P1` > `P2` > `P3`.
- BOARD.md로 승격 시 이 표에서 제거하고 BOARD ❌/🔧/📋 컬럼에 등록.
