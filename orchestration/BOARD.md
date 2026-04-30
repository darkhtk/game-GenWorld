# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator 7회차 — S-120 APPROVE → Done 이동(로드맵 #12 ✅, In Review 표 행 제거, Done 표 추가 / commit e17b59e). S-121 신규 In Review #13 등재(Supervisor, Client 리뷰 대기). S-125 #10 + S-126 #11 + Done 흡수는 Developer 처리 정합 확인. 헤더 카운트 +10 → +12 Done 정정. SPEC-S-146 선제 작성 진입.)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+12, In Progress 0건, In Review 2건, Rejected 0건)
> **📌 Client 리뷰 대기:** (1) **S-126** 옵션창 ESC로 닫기 일관성(Developer 7회차) — UIManager.Update ESC 분기를 `_dialogueOpen` 차단 위로 이동 → 다이얼로그 중에도 ESC 닫기 작동(다른 키 I/K/J/R/T/H/Tab은 차단 유지). `IsAnyPanelOpen()` 에 dialogue/npcProfile/npcQuest IsOpen 추가 → NPC 모달 ESC 일관 닫힘. `HideAll()` 의 dialogue 닫기는 `OnClose?.Invoke()` 동반 호출 → DialogueController 상태 정리(_player.Frozen=false, NpcProfile.Hide, SetDialogueOpen(false), ResumeMoving). DialogueUI/NpcProfilePanel/NpcQuestPanel 에 `IsOpen` 1라인 추가(PauseMenuUI 패턴). SPEC 부재(specs 참조 N). 기능 변경 최소(polish 방향) 기존 IsOpen 패턴 확장만. (2) **S-121** 🎨 NPC 대화 시작/종료 SFX(Supervisor) — SPEC-S-121 §11 DoD 1~5 충족 주장. `sfx_dialogue_open.wav`(200ms paper-flip)+`sfx_dialogue_close.wav`(180ms book-thud) Generated+Resources 양쪽 + .meta 4개. `GameConfig.Audio` 5상수 + `AudioManager.PlaySFXScaled(string,float)` 신규(기존 `PlaySFX(string,float pitchVariation)` 충돌 회피용 별도 메서드). DialogueUI Show/Hide 호출 1라인씩. EditMode `DialogueAudioConfigTests` 5건. 부수: S-117 코인 SFX 3종 Resources 누락 fixup(런타임 PlaySFX no-op 버그 수정). *S-084 Phase 2 후속(RESERVE 신규 등재):* **S-144** SpawnEventMonster `IsNullOrEmpty(eventId)` 가드(P3), **S-145** `PreservesUntaggedMonsters` 회귀 + `[SetUp] EventBus.Clear()`(P3), **S-146** invasion/elite_spawn 핸들러 SPEC(P2 — Coordinator 7회차 SPEC 선제 작성 진입), **S-147** SwitchRegion/OnSceneUnload → `ForceEndActiveEvent` 와이어링(P3). *S-101 v3 후속:* `MonsterAttackPatternSelector` 정의 위치(S-136). *S-124 후속:* S-137/S-138/S-139. *S-118 후속:* DuckBGM API 재사용(S-140/S-142). *DISCUSS-001:* BOARD 헤더 쓰기 권한 분리(옵션 A 권장) — 7회차에 동시 편집 충돌 2회 재발(Developer #11 추가 vs Coordinator 헤더 갱신), 응답 우선순위 상향.

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
| 11  | S-126: 옵션창 ESC로 닫기 일관성                              | P2   | 👀  | In Review (Developer 7회차) — UIManager ESC 분기 재구조화 + IsAnyPanelOpen NPC/Dialogue 포함 + HideAll dialogue OnClose 동반 호출 |
| 12  | S-120: 🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s)         | P3   | ✅  | APPROVE (REVIEW-S-120-v1, [깊은 리뷰], 4/4 페르소나 만장일치) — e17b59e |
| 13  | S-121: 🎨 NPC 대화 시작/종료 SFX                          | P3   | 👀  | In Review (Supervisor) — sfx_dialogue_open/close.wav + DialogueUI Show/Hide 훅 + DialogueAudioConfigTests 5건. Client 리뷰 대기 |

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
| S-121 🎨 NPC 대화 시작/종료 SFX | P3 | 2026-04-30 | 대기 | SPEC-S-121 §11 DoD 1~5 충족. `Assets/Audio/Generated/sfx_dialogue_open.wav` (200ms paper-flip, 17.6KB) + `sfx_dialogue_close.wav` (180ms book-thud, 15.9KB) Generated+Resources 양쪽 배치 + .meta 4개. `orchestration/scripts/gen_dialogue_sfx.py` 결정론적(seed 20121/20122). `GameConfig.Audio` 5상수 추가(DialogueOpenSfxName/CloseSfxName/Volume 0.85/0.70/Enabled). `AudioManager.PlaySFXScaled(string,float)` 신규 — 기존 `PlaySFX(string,float pitchVariation)` 시그니처 충돌 회피용 별도 메서드. `DialogueUI.Show` panel.SetActive 직후/`Hide` panel.SetActive(false) 직전에 PlaySFXScaled 호출. EditMode `DialogueAudioConfigTests` 5건(Name 2 + Volume 2 + Enabled 1). 부수: **S-117 fixup** — 코인 SFX 3종(sfx_coin_small/pile/burst) Resources 누락(`Assets/Resources/Audio/SFX/`)으로 런타임 PlaySFX no-op 버그 → Resources 복사 + .meta 3개 추가. DoD §6 PlayMode 수동은 Asset/QA 또는 사용자 검증. |
| S-126 옵션창 ESC로 닫기 일관성 | P2 | 2026-04-30 | 대기 | UIManager.Update ESC 분기를 `_dialogueOpen` 차단 위로 이동 → 다이얼로그 중에도 ESC 닫기 작동(다른 키 I/K/J/R/T/H/Tab은 차단 유지). `IsAnyPanelOpen()` 에 `dialogue.IsOpen`/`npcProfile.IsOpen`/`npcQuest.IsOpen` 추가 → NPC 모달도 ESC로 일관 닫힘. `HideAll()` 의 dialogue 닫기는 `OnClose?.Invoke()` 동반 호출하여 DialogueController 상태 정리(_player.Frozen=false, NpcProfile.Hide, SetDialogueOpen(false), ResumeMoving) 보장 — closeButton/AutoCloseDialogue 패턴과 정합. DialogueUI/NpcProfilePanel/NpcQuestPanel 에 `public bool IsOpen => panel != null && panel.activeSelf` 1라인 추가(PauseMenuUI 패턴 동일). SPEC 부재(specs 참조 N). RESERVE 비고의 "InputManager 메뉴 스택" 미사용 — 기능 변경 최소(polish 방향) 기존 IsOpen 패턴 확장만. UI 자가검증 §2.5: 신규 메서드/시스템 없음(기존 IsOpen 패턴 확장), ESC 키 = 기존 진입점, SPEC 부재 N/A. |

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

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
