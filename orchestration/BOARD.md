# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator 9회차 — Client 4회차+5회차 APPROVE 흡수 5건: **S-122** ✅(REVIEW-S-122-v1, [깊은 리뷰 5/N], 4/4 만장일치 / 4b3714b). **S-127** ✅(REVIEW-S-127-v1, 4/4 만장일치 / c88f1c2). **S-128** ✅(REVIEW-S-128-v1, [깊은 리뷰 6/N], 4/4 만장일치 / f376fb5). **S-123** ✅(REVIEW-S-123-v1, 4/4 만장일치 / a10603c). REVIEW-S-121-v1 / REVIEW-S-126-v1 회복 작성도 흡수. 헤더 카운트 Done +14 → +18(S-122/S-127/S-128/S-123 일괄 이동), In Review 2 → 0. Client S-122 [깊은 리뷰] 후속 4건 RESERVE 등재(S-149 debounce P2 / S-150 pitch P3 / S-151 MutePrefabs P3 / S-152 PlayMode P3 — S-149는 S-148 진입 전 필수 명시). DISCUSS-001 옵션 A 검증 3차 사례(Coordinator 헤더 갱신 vs Developer 9회차 + Supervisor 8회차 + Client 5회차 동시 진입 → 충돌 0건, 4-way 최대 동시성 검증). SPEC-S-129 GameManager 싱글톤 null 체크 강화(P2, Core) 선제 작성. 다음 단계: 백로그 100% 정리 — S-129/S-131/S-132 P2 + S-148 Phase 2 큐 재충전.)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+18, In Progress 0건, In Review 0건, Rejected 0건)
> **📌 Client 리뷰 대기:** **현재 0건** — Client 4회차+5회차에서 4건 일괄 APPROVE 흡수 완료(S-122/S-127/S-128/S-123). 다음 픽업 후보(RESERVE 🐛 P2 상위): **S-129** GameManager 싱글톤 null 체크 강화(SPEC-S-129 작성 완료) / **S-131** EnemySpawner 풀 검증(메모리 누수 의심) / **S-132** SaveSystem 비동기 저장 진행 표시(스피너+토스트) / **S-136** MonsterAttackPatternSelector 정의 위치(REVIEW-S-101-v3 후속) / **S-148** UIButtonSfx 일괄 부착(S-122 Phase 2 — **선결 조건 S-149** SPEC §10 risk debounce 우선). *S-122 [깊은 리뷰] follow-up RESERVE 신규 4건:* **S-149** HoverDebounce P2(S-148 진입 전 필수) / **S-150** HoverPitchVar P3 / **S-151** MutePrefabs P3 / **S-152** PlayMode 행동 테스트 P3. *S-127 follow-up 5건 BACKLOG 후보:* 마커 모양 차별화(색맹) / Image 캐싱 / 퀘스트 상태별 색상 / 시각 거리 향상 / PlayMode 자동화 — 다음 idle 회차 RESERVE 등재 검토. *S-084 Phase 2 후속(RESERVE 잔존):* **S-144** P3 / **S-145** P3 / **S-146** P2(SPEC-S-146.md 작성 완료) / **S-147** P3. *S-101 v3 후속:* S-136 P2. *S-124 후속:* S-137/S-138/S-139. *S-118 후속:* DuckBGM API 재사용(S-140/S-142). *DISCUSS-001:* BOARD 헤더 쓰기 권한 분리(옵션 A) — 4개 에이전트 만장일치 합의, **본 루프(Coordinator 9회차)가 옵션 A 검증 3차 사례 + 4-way 동시성 무충돌 검증**(Coordinator 헤더 갱신 vs Developer 9회차 + Supervisor 8회차 + Client 5회차 동시 진입). 사용자 프롬프트 갱신 대기.

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
| 14  | S-122: 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1)               | P2   | ✅  | APPROVE (REVIEW-S-122-v1, [깊은 리뷰 5/N], 4/4 페르소나 만장일치) — 4b3714b |
| 15  | S-127: 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)                              | P3   | ✅  | APPROVE (REVIEW-S-127-v1, 4/4 페르소나 만장일치) — c88f1c2 |
| 16  | S-128: QuestUI 진행률 바 (requirements/killRequirements)                              | P2   | ✅  | APPROVE (REVIEW-S-128-v1, [깊은 리뷰 6/N], 4/4 페르소나 만장일치) — f376fb5 + 559836b(missed UI consts fixup) |
| 17  | S-123: 🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3)                              | P3   | ✅  | APPROVE (REVIEW-S-123-v1, 4/4 페르소나 만장일치) — a10603c |

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
| S-122 🎨 UI 버튼 호버/클릭 SFX 통일 (Phase 1) | P2 | 2026-04-30 | APPROVE / 4b3714b (REVIEW-S-122-v1, [깊은 리뷰 5/N], 4/4 페르소나 만장일치) — Phase 1 자산+컴포넌트+테스트. `sfx_ui_hover.wav`(60ms peak 0.55) Generated+Resources 양쪽 + GameConfig.UI 5상수 + UIButtonSfx 컴포넌트 + UIButtonSfxConfigTests 5건. **Phase 2 = S-148 RESERVE** — 기존 Button 일괄 부착(Editor tooling). Client [깊은 리뷰 5/N] SPEC §2/§10 결손 3건(HoverDebounce/HoverPitchVar/MutePrefabsContaining) → S-149~S-152 RESERVE 신규 등재(S-149는 S-148 진입 전 필수). SPEC-S-122.md §1~§11 항목별 대조. |
| S-128 QuestUI 진행률 바 (requirements/killRequirements) | P2 | 2026-04-30 | APPROVE / f376fb5 + 559836b (REVIEW-S-128-v1, [깊은 리뷰 6/N], 4/4 페르소나 만장일치) — `QuestProgressBarBuilder.Build(progress, required) → string` static + GameConfig.UI 5상수(Cells=10/MetColor/UnmetColor/BgColor/Enabled) + QuestUI.AddActiveEntry requirements/killRequirements 두 라인 게이지 삽입. SPEC-S-128 §4 와이어프레임 충실. EditMode `QuestProgressBarTests` 5건(Zero/Half/Met/Over-met clamp/Required zero guard). prefab 변경 0건(TMP 단일 노드 Rich Text). 559836b는 f376fb5에서 누락된 GameConfig.UI consts 보강 fixup. specs 참조 Y. |
| S-123 🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3) | P3 | 2026-04-30 | APPROVE / a10603c (REVIEW-S-123-v1, 4/4 페르소나 만장일치) — `GameConfig.UI.InventorySlotEmptyAlpha=0.3f` / `InventorySlotFilledAlpha=1.0f` + `InventorySlotUI.ApplyEmptyAlpha(bool)` 헬퍼 + `Clear()`/`SetItem()` 훅. 자산 신규 0(기존 `Sprites/UI/slot_bg` 알파만). 부수: `Clear()`에 `gradeFrameImage.enabled=false` 1라인(stale 등급 프레임 잔존 회귀 방지). EditMode `InventorySlotEmptyAlphaTests` 6건(상수 4 + 동작 2). SPEC 부재(specs 참조 N). |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
