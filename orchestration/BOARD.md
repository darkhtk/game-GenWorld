# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator 9회차 — Client 4회차 APPROVE 흡수: S-122 ✅(REVIEW-S-122-v1, [깊은 리뷰 5/N], 4/4 페르소나 만장일치) Done 이동(로드맵 #14 ✅, In Review 행 제거 / 4b3714b). S-127 ✅(REVIEW-S-127-v1, 4/4 페르소나 만장일치) Done 이동(로드맵 #15 ✅, In Review 행 제거 / c88f1c2). REVIEW-S-121-v1 / REVIEW-S-126-v1 회복 작성도 흡수. **S-123은 In Review 유지**(Supervisor 8회차 mid-flight 미커밋 → 다음 루프 흡수 위임). 헤더 카운트 Done +14 → +16, In Review 2 → 1(S-123). Client S-122 [깊은 리뷰] 후속 4건 RESERVE 등재(S-149 debounce P2 / S-150 pitch P3 / S-151 MutePrefabs P3 / S-152 PlayMode P3 — S-149는 S-148 진입 전 필수 명시). DISCUSS-001 옵션 A 검증 3차 사례(Coordinator 헤더 갱신 vs Supervisor 8회차 In Review 행 추가 동시성 → 충돌 0건). SPEC-S-129 GameManager 싱글톤 null 체크 강화(P2, Core) 선제 작성.)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+16, In Progress 0건, In Review 1건(S-123), Rejected 0건)
> **📌 Client 리뷰 대기:** (1) **S-122** 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1)(Supervisor 7회차) — `sfx_ui_hover.wav`(60ms 1800→1500Hz subtle chirp + HP noise tail, peak 0.55, 5336B) Generated+Resources 양쪽 + .meta 2개(GUID 7f3c8a91 / 8e4d9b02). 클릭은 기존 `sfx_click.wav` 재사용(자산 비대 회피). `GameConfig.UI` 신규 클래스 5상수(ButtonHoverSfxName/ButtonClickSfxName/ButtonHoverSfxVolume=0.55/ButtonClickSfxVolume=0.85/ButtonSfxEnabled=true). `Assets/Scripts/UI/UIButtonSfx.cs` 신규 — `[RequireComponent(Button)]` + `IPointerEnter/Click`, `button.interactable` 가드, per-instance override 4필드(hoverSfxOverride/clickSfxOverride/hoverVolumeOverride(-1=use config)/clickVolumeOverride(-1=use config)) + playOnHover/playOnClick 토글. `AudioManager.PlaySFXScaled(string,float)` (S-121에서 추가) 재사용. EditMode `UIButtonSfxConfigTests` 5건(Name 2 + Volume 2 + Enabled 1, click>hover 명시 단언). **Phase 2 = S-148 RESERVE 신규** — 기존 Button 일괄 부착(Editor 자동 부착 스크립트 + 시스템성 Button 중복 회피 정책 `playOnClick=false`). Phase 1만으로 사용자 청취상 무변화 의도(검토 안전성). SPEC 부재(specs 참조 N). (2) **S-127** 미니맵 NPC 마커 색상 분리(Developer 8회차) — `MinimapUI.ClassifyNpcMarkerColor(NpcDef, bool hasQuest)` public static + 분류 우선순위 ① open_shop → 상점색 ② QuestSystem.GetQuestStatusForNpc HasValue → 퀘스트색 ③ 기본색. `ApplyNpcMarkerColor` private static, `UpdateEntityIcons` foreach 매 0.2s 호출. EditMode `MinimapNpcMarkerTests` 7건. SPEC 부재(specs 참조 N). *S-084 Phase 2 후속(RESERVE 잔존):* **S-144** SpawnEventMonster `IsNullOrEmpty(eventId)` 가드(P3), **S-145** `PreservesUntaggedMonsters` 회귀 + `[SetUp] EventBus.Clear()`(P3), **S-146** invasion/elite_spawn 핸들러 SPEC(P2 — SPEC-S-146.md 작성 완료), **S-147** SwitchRegion/OnSceneUnload → `ForceEndActiveEvent` 와이어링(P3). *S-101 v3 후속:* `MonsterAttackPatternSelector` 정의 위치(S-136). *S-124 후속:* S-137/S-138/S-139. *S-118 후속:* DuckBGM API 재사용(S-140/S-142). *DISCUSS-001:* BOARD 헤더 쓰기 권한 분리(옵션 A 권장) — 4개 에이전트 만장일치 합의 도달, **본 루프(Coordinator 8회차)가 옵션 A 즉시 적용 1차 검증 사례**(Supervisor 7회차 헤더 미수정 → 본 루프 헤더 갱신 시 충돌 0건). 사용자 프롬프트 갱신 대기.

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고                                       |
| --- | ------------------------------------- | ---- | --- | ---------------------------------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | ✅  | APPROVE (REVIEW-S-101-v3, [깊은 리뷰], 4/4 페르소나) — 5373b76 |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ✅  | APPROVE (REVIEW-S-084-v2, [깊은 리뷰], 4/4 페르소나 만장일치) — Phase 2 인프라 통과, 분할 적정. P3 후속 S-144~S-147 RESERVE 등재. (1차 cf72a6a, 2차 3893d24) |
| 3   | S-114: 🎨 회피 모션 잔상 이펙트 스프라이트         | P2   | ✅  | APPROVE (REVIEW-S-114-v1) — 6ab7a5c                |
| 4   | S-115: 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화      | P2   | ✅  | APPROVE (REVIEW-S-115-v1) — edae030               |
| 5   | S-116: 🎨 스킬 쿨다운 회복 SFX                     | P2   | ✅  | DONE — sfx_cooldown_ready.wav + HUD 트리거 (cfce018) |
| 6   | S-119: 🎨 레벨업 파티클 이펙트                     | P2   | ✅  | DONE — vfx_levelup_burst + LevelUpVFX 16 spark (3fd219c) |
| 7   | S-124: 인벤토리 드래그 앤 드롭 시각 피드백             | P2   | ✅  | APPROVE (REVIEW-S-124-v1, 4/4 페르소나) — ee545e1 |
| 8   | S-117: 🎨 몬스터 처치 시 골드 드롭 사운드 (3등급)         | P3   | ✅  | DONE — sfx_coin_small/pile/burst.wav + CombatRewardHandler rank 분기 (6035926) |
| 9   | S-118: 🎨 아이템 획득 팝업 BGM 더킹 (-6dB, 0.4s)           | P3   | ✅  | DONE — AudioManager.DuckBGM coroutine + CombatRewardHandler 호출 |
| 10  | S-125: SkillTree 잠금 노드 해금 조건 결합 표시        | P2   | ✅  | APPROVE (REVIEW-S-125-v1, 4/4 페르소나 만장일치) — 8bd46ba |
| 11  | S-126: 옵션창 ESC로 닫기 일관성                              | P2   | ✅  | APPROVE (REVIEW-S-126-v1, 4/4 페르소나 만장일치) — 8c01c12 |
| 12  | S-120: 🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s)         | P3   | ✅  | APPROVE (REVIEW-S-120-v1, [깊은 리뷰], 4/4 페르소나 만장일치) — e17b59e |
| 13  | S-121: 🎨 NPC 대화 시작/종료 SFX                          | P3   | ✅  | APPROVE (REVIEW-S-121-v1, [깊은 리뷰], 4/4 페르소나 만장일치) — 81213a6 |
| 14  | S-122: 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1)               | P2   | 👀  | In Review (Supervisor 7회차) — sfx_ui_hover.wav + UIButtonSfx 컴포넌트 + GameConfig.UI 5상수 + UIButtonSfxConfigTests 5건. Phase 2 = S-148 RESERVE. Client 리뷰 대기 |
| 15  | S-127: 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)                              | P3   | ✅  | APPROVE (REVIEW-S-127-v1, 4/4 페르소나 만장일치) — c88f1c2 |
| 16  | S-128: QuestUI 진행률 바 (requirements/killRequirements)                              | P2   | 👀  | In Review (Developer 9회차) — QuestProgressBarBuilder static + GameConfig.UI 5상수 + QuestUI.AddActiveEntry 두 라인에 게이지 삽입 + 테스트 5건. specs 참조 Y(SPEC-S-128) |
| 17  | S-123: 🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3)                              | P3   | 👀  | In Review (Supervisor 8회차) — `GameConfig.UI.InventorySlotEmptyAlpha=0.3f` / `InventorySlotFilledAlpha=1.0f` + `InventorySlotUI.ApplyEmptyAlpha(bool)` 헬퍼 + Clear()/SetItem() 훅. EditMode 6건. 자산 신규 0(slot_bg 알파만 조정). |

---

## ❌ Rejected (최우선 수정)

| 태스크 | 사유 | REVIEW | 비고 |
| --- | --- | ------ | --- |

---

## 🔧 In Progress

| 태스크 | 우선순위 | 담당 | 시작일 | 비고 |
| --- | ---- | --- | --- | --- |

---

## 👀 In Review

| 태스크 | 우선순위 | 완료일 | 결과 | 비고 |
| --- | ---- | --- | --- | --- |
| S-122 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1) | P2 | 2026-04-30 | ✅ APPROVE (REVIEW-S-122-v1, [깊은 리뷰], 4/4 페르소나 만장일치) | Phase 1 = 자산 + 컴포넌트 + 테스트 (Phase 2 분할). `Assets/Audio/Generated/sfx_ui_hover.wav` (60ms 1800→1500Hz 14ms chirp + HP(1500Hz) noise tail 45ms, peak 0.55, 5336 bytes) + Resources 양쪽 + .meta 2개(GUID 7f3c8a91 / 8e4d9b02). `orchestration/scripts/gen_ui_hover_sfx.py` 결정론적(seed 20122). 클릭 SFX는 기존 `sfx_click.wav` 재사용(자산 비대 회피). `GameConfig.UI` 신규 클래스 5상수(ButtonHoverSfxName="sfx_ui_hover" / ButtonClickSfxName="sfx_click" / ButtonHoverSfxVolume=0.55 / ButtonClickSfxVolume=0.85 / ButtonSfxEnabled=true). `Assets/Scripts/UI/UIButtonSfx.cs` 신규 — `[RequireComponent(Button)]` + `IPointerEnterHandler/IPointerClickHandler`, `button.interactable` 가드, per-instance override 4필드(hoverSfxOverride / clickSfxOverride / hoverVolumeOverride(-1=use config) / clickVolumeOverride(-1=use config)) + playOnHover/playOnClick 토글. EditMode `UIButtonSfxConfigTests` 5건(Name 2 + Volume 2 + Enabled 1 + 명시적 hover<click 단언). **Phase 2 = S-148 RESERVE 신규** — 기존 Button 일괄 부착(씬/프리팹 검수 + Editor tooling 필요, 분리 PR). Phase 1만으로는 컴포넌트 부착 0 → 사용자 청취상 무변화 의도(검토 안전성). SPEC 부재(specs 참조 N), polish 방향 분할 적정. |
| S-128 QuestUI 진행률 바 (requirements/killRequirements) | P2 | 2026-04-30 | 대기 | SPEC-S-128 §4 와이어프레임 충실. 신규: `Assets/Scripts/UI/QuestProgressBarBuilder.cs` (static, public `Build(int progress, int required) → string`) — 10셀 고정폭 글리프 게이지 (`▰`/`░`), 미달성/달성 색상 분리, div-by-zero 가드, over-met 클램프. `Assets/Scripts/Core/GameConfig.cs` UI 클래스에 5상수 추가(QuestProgressBarCells=10/MetColor=#66ff88/UnmetColor=#ff9944/BgColor=#444444/Enabled=true) — S-122 GameConfig.UI 클래스 재사용(매직 넘버 회피, S-138 정신). `Assets/Scripts/UI/QuestUI.cs` AddActiveEntry 의 requirements/killRequirements foreach 두 라인에 `QuestProgressBarBuilder.Build(...)` 호출 결과를 항목명 + 게이지 + (have/required) 형식으로 삽입(기존 텍스트 병기 유지 — 시각+정확 정보 동시 노출). prefab 변경 0건(TMP 단일 노드 Rich Text 활용). EditMode `QuestProgressBarTests` 5건(Zero/Half rounding+Unmet color/Met all+No bg/Over-met clamp/Required zero guard). UI 자가검증 §2.5: ① 호출처 = AddActiveEntry foreach 2곳(requirements + killRequirements) ② J 키로 QuestUI 패널 열기 → 게이지 노출 ③ SPEC §4 와이어프레임 대조 완료. specs 참조 Y. DoD §6/§7 PlayMode 수동(폰트 글리프 렌더 + 처치 후 게이지 1셀 증가)은 Asset/QA 또는 사용자 검증 잔여. |
| S-123 🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3) | P3 | 2026-04-30 | 대기 | 채워진 슬롯과 시각 구분 약함 → 빈 슬롯 bg 알파 0.3 톤다운으로 "drop target" affordance 시각화. **자산 신규 0** (기존 `Sprites/UI/slot_bg` 그대로, 런타임 알파만 조정). `GameConfig.UI` 신규 상수 2개: `InventorySlotEmptyAlpha=0.3f` / `InventorySlotFilledAlpha=1.0f` (기존 `SetDragGhost`의 0.3f 매직 넘버와 동일 컨벤션 — S-138 follow-up 시 통합 가능). `InventorySlotUI.ApplyEmptyAlpha(bool empty)` private 헬퍼 신규: `_bgImage.color.a` 만 변경(다른 채널 보존). `Clear()` 끝부분에 `ApplySlotBg() + ApplyEmptyAlpha(true)` 호출(이전엔 ApplySlotBg 미호출로 sprite 미적용 가능 + bg dim 무시). `SetItem()` 끝부분에 `ApplyEmptyAlpha(false)` 호출(채워질 때 복원). 부수 fix: `Clear()`에 `gradeFrameImage.enabled = false` 1라인 추가(이전엔 stale 등급 프레임 잔존 가능 — SetItem grade null/empty 시 미터치). EditMode `InventorySlotEmptyAlphaTests` 6건: 상수 4건(Empty<Filled / Empty 0~1 / Filled=1.0 / Empty 0.2~0.4 ghost-tier 범위) + 동작 2건(Clear→0.3 / SetItem→1.0; `new GameObject + AddComponent<Image> + AddComponent<InventorySlotUI>` 패턴, DestroyImmediate 정리). InventoryUI.RefreshGrid foreach 호출처 변경 0(Clear/SetItem 시그니처 불변). SPEC 부재(specs 참조 N). UI 자가검증 §2.5: ① 핵심 메서드 호출처 = RefreshGrid for(_slots.Count) (1곳) ② 인벤토리 UI 열기 → 빈 슬롯 즉시 dim, drop 시 즉시 복원 ③ SPEC 부재 N/A. |

---

## ✅ Done

| 태스크                                    | 우선순위 | 완료일 | 비고      |
| -------------------------------------- | ---- | --- | ------- |
| S-086 SkillBar 쿨다운 시각 동기화              | P2   |     | APPROVE |
| S-092 DialogueUI 긴 텍스트 오버플로 처리         | P2   |     | APPROVE |
| S-094 CraftingUI 재료 부족 항목별 표시          | P2   |     | APPROVE |
| S-096 MonsterController 공격 범위 외 데미지 차단 | P2   |     | APPROVE |
| S-097 SaveSystem 슬롯 삭제 확인 팝업           | P2   |     | APPROVE |
| S-098 AchievementUI 알림 큐 중복 방지         | P2   |     | APPROVE |
| S-087 RegionManager 씬 전환 중 입력 차단       | P2   |     | APPROVE |
| S-113 WorldEvent 배율 미적용                | P2   |     | bf997ae |
| S-112 아이템 판매가 미정의                      | P2   |     | ed5d554 |
| S-109 ScreenFlash 미호출                  | P2   |     | 914a4ad |
| S-114 🎨 회피 모션 잔상 이펙트 스프라이트       | P2   | 2026-04-30 | APPROVE / 6ab7a5c |
| S-115 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화 | P2   | 2026-04-30 | APPROVE / edae030 (REVIEW-S-115-v1, 4/4 페르소나 APPROVE) |
| S-116 🎨 스킬 쿨다운 회복 SFX 누락                  | P2   | 2026-04-30 | DONE / cfce018 (sfx_cooldown_ready.wav 220ms 1320Hz + HUD.UpdateCooldowns transition trigger) |
| S-119 🎨 레벨업 파티클 이펙트                       | P2   | 2026-04-30 | DONE / 3fd219c (vfx_levelup_burst 8f sprite + LevelUpVFX 16-spark 방사 1.0s) |
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정 (v3) | high | 2026-04-30 | APPROVE / 5373b76 (REVIEW-S-101-v3, [깊은 리뷰], 4/4 페르소나 APPROVE; v2 BLOCKER 5건 해소; SPEC §7 #1/#2/#7 Unity Editor 실 검증 다음 루프 잔여) |
| S-124 인벤토리 드래그 앤 드롭 시각 피드백              | P2   | 2026-04-30 | APPROVE / ee545e1 (REVIEW-S-124-v1, 4/4 페르소나 APPROVE; null/index 가드 적절, RefreshGrid swap 후 alpha 자동 복원; P3 후속 3건 RESERVE 등재 검토) |
| S-117 🎨 몬스터 처치 시 골드 드롭 사운드 (등급별 3종)    | P3   | 2026-04-30 | DONE / 6035926 (sfx_coin_small.wav 150ms + sfx_coin_pile.wav 280ms + sfx_coin_burst.wav 500ms + CombatRewardHandler.OnMonsterKilled def.rank 분기) |
| S-118 🎨 아이템 획득 팝업 BGM 더킹 (-6dB, 0.4s)          | P3   | 2026-04-30 | DONE (Supervisor) — AudioManager.DuckBGM(dropDb, duration, fadeTime=0.08s) coroutine: dB → linear (10^(dB/20)) × _duckMultiplier × _bgmVolume × _masterVolume, fade-in/hold/fade-out unscaled. CombatRewardHandler drops>0 분기에서 호출. SetBGMVolume/SetMasterVolume도 BgmTargetVolume() 통일. |
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 (Phase 2) | P3   | 2026-04-30 | APPROVE (REVIEW-S-084-v2, [깊은 리뷰], 4/4 페르소나 만장일치) / 3893d24 — EventOriginId 태깅 + MonsterSpawner.OnEnable WorldEventEndEvent 구독 + DespawnEventMonsters/SpawnEventMonster API + EditMode 테스트 4건 (`WorldEventCleanupTests`). 분할 PR 적정. P3 후속 4건 RESERVE 신규 등재(S-144 IsNullOrEmpty 가드 / S-145 PreservesUntaggedMonsters 회귀 + EventBus.Clear / S-146 invasion·elite_spawn 핸들러 SPEC P2 / S-147 RegionManager·OnSceneUnload 와이어링). |
| S-125 SkillTree 잠금 노드 해금 조건 결합 표시 | P2 | 2026-04-30 | APPROVE / 8bd46ba (REVIEW-S-125-v1, 4/4 페르소나 만장일치) — `SkillRowUI.UpdateState` 잠금 분기 재작성, 레벨/포인트 deficit 둘 다 부족 시 `Lv.5+ -2pt`(빨강) 동시 노출. SPEC 부재(specs 참조 N). |
| S-120 🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s) | P3 | 2026-04-30 | APPROVE / e17b59e (REVIEW-S-120-v1, [깊은 리뷰], 4/4 페르소나 만장일치) — `GameConfig.Audio` 신규(BossRegionIds={"volcano","dragon_lair"}/BgmTransitionBossEnter=1.5f/Default=1.0f/CrossfadeDualSource=true/IsBossRegion/BgmFadeTimeFor 헬퍼) + AudioManager.bgmSourceB SerializeField + CrossfadeBGMDual(매 프레임 BgmTargetVolume 재계산 → S-118 DuckBGM 충돌 회피) + ApplyBgmVolume 7곳 통합 + GameManager.PlayRegionBGM fadeTime 분기 1라인. EditMode `AudioConfigTests` 7건. DoD §6/§7 PlayMode 수동은 Asset/QA 또는 사용자 검증 잔여. |
| S-126 옵션창 ESC로 닫기 일관성 | P2 | 2026-04-30 | APPROVE / 8c01c12 (REVIEW-S-126-v1, 4/4 페르소나 만장일치) — UIManager.Update ESC 분기를 `_dialogueOpen` 차단 위로 이동(다이얼로그 중에도 ESC 닫기 작동, 다른 키 I/K/J/R/T/H/Tab은 차단 유지) + `IsAnyPanelOpen()` 에 dialogue/npcProfile/npcQuest IsOpen 추가 + `HideAll()` dialogue 닫기에 `OnClose?.Invoke()` 동반 호출(DialogueController 상태 정리 — closeButton/AutoCloseDialogue 패턴과 정합). DialogueUI/NpcProfilePanel/NpcQuestPanel 에 `IsOpen` 1라인 추가(PauseMenuUI 패턴). SPEC 부재(specs 참조 N), polish 최소 변경. |
| S-121 🎨 NPC 대화 시작/종료 SFX | P3 | 2026-04-30 | APPROVE / 81213a6 (REVIEW-S-121-v1, [깊은 리뷰], 4/4 페르소나 만장일치) — SPEC-S-121 §11 DoD 1~5 충족. `sfx_dialogue_open.wav`(200ms paper-flip, 17.6KB) + `sfx_dialogue_close.wav`(180ms book-thud, 15.9KB) Generated+Resources 양쪽 + .meta 4개. seed 20121/20122. `GameConfig.Audio` 5상수(DialogueOpenSfxName/CloseSfxName/Volume 0.85/0.70/Enabled) + `AudioManager.PlaySFXScaled(string,float)` 신규(시그니처 충돌 회피) + DialogueUI.Show/Hide 훅. EditMode `DialogueAudioConfigTests` 5건. 부수: **S-117 fixup** 코인 SFX 3종 Resources 누락(런타임 PlaySFX no-op 버그) → Resources 복사 + .meta 3개. DoD §6 PlayMode 수동은 Asset/QA 또는 사용자 검증. |
| S-127 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본) | P3 | 2026-04-30 | APPROVE / c88f1c2 (REVIEW-S-127-v1, 4/4 페르소나 만장일치) — `MinimapUI.ClassifyNpcMarkerColor` 분류 우선순위 ① open_shop → 상점색 ② QuestSystem.GetQuestStatusForNpc HasValue → 퀘스트색 ③ 기본색. EditMode `MinimapNpcMarkerTests` 7건. 회귀 위험 0(UpdateEntityIcons 1라인 추가). 후속 5건 BACKLOG 후보(색맹 대응 마커 모양/Image 캐싱/퀘스트 상태별 색상/시각 거리 향상/PlayMode 자동화) — Coordinator 흡수 검토. SPEC 부재(specs 참조 N). |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
