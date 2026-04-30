# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator 6회차 — Developer가 처리한 S-084 APPROVE → Done 흡수 정합성 확인, 헤더 카운트 +10 정정, S-084 후속 권고 4건 RESERVE 흡수 신규 등재 S-144~S-147)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+10, In Progress 0건, In Review 2건, Rejected 0건)
> **📌 Client 리뷰 대기:** (1) **S-120** 🎨 보스룸 BGM 1.5s 크로스페이드(Supervisor) — SPEC-S-120 §11 DoD 1~5 충족, working tree 미커밋(AudioManager/GameConfig/GameManager + AudioConfigTests.cs 신규) → Supervisor 커밋 후 PR. (2) **S-125** SkillTree 잠금 노드 deficit 결합 표시(Developer) — `SkillRowUI.UpdateState` 잠금 분기 재작성, 레벨/포인트 deficit 둘 다 부족 시 `Lv.5+ -2pt` 동시 노출. SPEC 부재(specs 참조 N). *S-084 Phase 2 후속(RESERVE 신규 등재):* **S-144** SpawnEventMonster `IsNullOrEmpty(eventId)` 가드 + SkillVFX/AudioManager 호출 통합(P3), **S-145** `WorldEventEndEvent_PreservesUntaggedMonsters` 회귀 테스트 + `[SetUp] EventBus.Clear()`(P3), **S-146** invasion/elite_spawn 핸들러 SPEC → SpawnEventMonster 호출처 연결(P2), **S-147** RegionManager.SwitchRegion/SaveSystem.OnSceneUnload → `WorldEventSystem.ForceEndActiveEvent` 와이어링(P3). *S-101 v3 후속:* Unity Editor 실 컴파일 + EditMode test 4건 검증, `MonsterAttackPatternSelector` 정의 위치 추적(S-136). *S-124 후속:* S-137/S-138/S-139. *S-118 후속:* DuckBGM API 재사용 (S-140 보스, S-142 NPC).

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고                                       |
| --- | ------------------------------------- | ---- | --- | ---------------------------------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | ✅  | APPROVE (REVIEW-S-101-v3, [깊은 리뷰], 4/4 페르소나) — 5373b76 |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ✅  | APPROVE (REVIEW-S-084-v2, [깊은 리뷰], 4/4 페르소나 만장일치) — Phase 2 인프라 통과, 분할 적정. P3 후속 S-140~S-143 RESERVE 등재. (1차 cf72a6a, 2차 3893d24) |
| 3   | S-114: 🎨 회피 모션 잔상 이펙트 스프라이트         | P2   | ✅  | APPROVE (REVIEW-S-114-v1) — 6ab7a5c                |
| 4   | S-115: 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화      | P2   | ✅  | APPROVE (REVIEW-S-115-v1) — edae030               |
| 5   | S-116: 🎨 스킬 쿨다운 회복 SFX                     | P2   | ✅  | DONE — sfx_cooldown_ready.wav + HUD 트리거 (cfce018) |
| 6   | S-119: 🎨 레벨업 파티클 이펙트                     | P2   | ✅  | DONE — vfx_levelup_burst + LevelUpVFX 16 spark (3fd219c) |
| 7   | S-124: 인벤토리 드래그 앤 드롭 시각 피드백             | P2   | ✅  | APPROVE (REVIEW-S-124-v1, 4/4 페르소나) — ee545e1 |
| 8   | S-117: 🎨 몬스터 처치 시 골드 드롭 사운드 (3등급)         | P3   | ✅  | DONE — sfx_coin_small/pile/burst.wav + CombatRewardHandler rank 분기 (6035926) |
| 9   | S-118: 🎨 아이템 획득 팝업 BGM 더킹 (-6dB, 0.4s)           | P3   | ✅  | DONE — AudioManager.DuckBGM coroutine + CombatRewardHandler 호출 |

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
| S-120 🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s) | P3 | 2026-04-30 | 대기 | SPEC-S-120 §11 DoD 1~5 충족. GameConfig.Audio 신규(BossRegionIds={"volcano","dragon_lair"}, BgmTransitionBossEnter=1.5f, Default=1.0f, BossExit=1.0f, CrossfadeDualSource=true, IsBossRegion/BgmFadeTimeFor 헬퍼) + AudioManager.bgmSourceB SerializeField + Awake CreateSource("BGM_B") + CrossfadeBGMDual(동시 ramp, 매 프레임 BgmTargetVolume 재계산 → S-118 DuckBGM 충돌 회피, 무음 갭 X) + ApplyBgmVolume(dual) 헬퍼로 SetBGMVolume/SetMasterVolume/LoadVolumeSettings/DuckRoutine 7곳 통합. GameManager.PlayRegionBGM fadeTime=BgmFadeTimeFor(region) 1라인. EditMode `AudioConfigTests` 7건(IsBossRegion 4 + BgmFadeTimeFor 2 + BossRegionIds 1). DoD §6/§7 PlayMode 수동은 Asset/QA 또는 사용자 검증. |
| S-125 SkillTree 잠금 노드 해금 조건 결합 표시 | P2 | 2026-04-30 | 대기 | `SkillRowUI.UpdateState` 잠금(canLearn=false) 분기 재작성. 레벨/포인트 deficit 둘 다 부족 → `Lv.5+ -2pt`(빨강) 동시 노출. 한쪽만 부족 → `Lv.5+` 또는 `-2pt`(기존 동작 유지). 양쪽 다 충족인데 canLearn=false인 비정상 케이스(playerLevel=0 등) → `<requiredPoints>pt` 빨강 fallback. SPEC 부재(specs 참조 N). 호출처 변경 없음(SkillTreeUI.Refresh→UpdateState 체인 그대로, 진입점 SkillTreeUI.Show/Toggle 기존 사용). 신규 메서드/시스템 없음 — 기존 분기 로직 정밀화. |

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
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 (Phase 2) | P3   | 2026-04-30 | APPROVE (REVIEW-S-084-v2, [깊은 리뷰], 4/4 페르소나 만장일치) / 3893d24 — EventOriginId 태깅 + MonsterSpawner.OnEnable WorldEventEndEvent 구독 + DespawnEventMonsters/SpawnEventMonster API + EditMode 테스트 4건 (`WorldEventCleanupTests`). 분할 PR 적정. P3 후속 4건 RESERVE 등재(S-140~S-143). |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
