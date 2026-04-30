# Backlog Reserve

> **최종 갱신:** 2026-04-30 (Supervisor — S-120 DONE: AudioManager dual-source CrossfadeBGMDual + GameConfig.Audio + GameManager fadeTime 분기 / EditMode 7건)
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
| S-121 | 🎨 NPC 대화 시작/종료 SFX                                    | P3   | SFX     | DialogueUI Show/Hide 진입점에 훅                        |
| S-122 | 🎨 UI 버튼 호버/클릭 SFX 통일 (UIButton 컴포넌트)                  | P2   | UI/SFX  | 일부 버튼만 사운드 있음. 공통 UIButton 만들고 일괄 부착                |
| S-123 | 🎨 인벤토리 빈 슬롯 그래픽 톤다운 (alpha 0.3)                       | P3   | UI      | 채워진 슬롯과 시각 구분 약함                                    |
| S-140 | 🎨 보스 처치 시 화면 흔들림 + 승리 코드 SFX (sfx_boss_die_chord 1.2s) | P2   | VFX/SFX | 보스 처치가 일반 몬스터와 동일 피드백. CinemachineImpulse + 코드 진행 SFX |
| S-141 | 🎨 포션 사용 SFX 종류별 분리 (sfx_potion_hp/mp/buff 3종 250ms)        | P3   | SFX     | 현재 sfx_potion 단일. ItemUseSystem.Use 분기에서 itemDef.subtype 기반 호출 |
| S-142 | 🎨 NPC 대화 시작 시 BGM 페이드 다운 (-3dB, 유지, Hide 시 복원)         | P3   | Audio   | 음성/효과 가독성. AudioManager에 SetBGMDuck(float dB, bool sustain) 추가 |
| S-143 | 🎨 발걸음 SFX 지형별 분리 (grass/stone/wood, 0.4s 간격)                | P3   | SFX     | 현재 발걸음 SFX 없음. PlayerController.Update 이동 distance 누적 + Tilemap 지형 lookup |

## 🐛 코드 품질 / UX

| ID    | 태스크                                                  | 우선순위 | 영역         | 비고                                                |
| ----- | --------------------------------------------------- | ---- | ---------- | ------------------------------------------------- |
| ~~S-124~~ | ~~인벤토리 드래그 앤 드롭 시각 피드백~~ ✅ 2026-04-30 | P2   | UI         | DONE — REVIEW-S-124-v1 APPROVE / ee545e1 (BOARD ✅ 이동 완료) |
| S-125 | SkillTree 미해금 노드 툴팁에 해금 조건 명시                       | P2   | UI         | 현재 "Locked"만 표시. 레벨/포인트/선행 스킬 표기                  |
| S-126 | 옵션창 ESC로 닫기 일관성 (현재 메뉴별 상이)                          | P2   | UI         | InputManager 메뉴 스택 활용                             |
| S-127 | 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)                        | P3   | UI         | MinimapIcon 컴포넌트에 type enum 추가                    |
| S-128 | 퀘스트 추적 UI 진행률 바 (현재 숫자만)                            | P2   | UI         | "3/10 처치" → 게이지 바 추가                              |
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

---

## ❌ Rejected 재작업 (BOARD에서 회수)

> 현재 비어 있음. (S-101은 BOARD ❌ Rejected에서 직접 처리 중, S-084는 BOARD 🔧 In Progress 보류 상태 — RESERVE 중복 등재 제거됨.)

---

## 작성 가이드

- 신규 항목 ID는 `S-XXX` 연속 (현재 S-143까지). 다음 번호 S-144부터.
- `🎨` 태그는 신규 스프라이트/SFX/BGM/이펙트 자산이 필요한 태스크에만.
- 우선순위: `high` > `P1` > `P2` > `P3`.
- BOARD.md로 승격 시 이 표에서 제거하고 BOARD ❌/🔧/📋 컬럼에 등록.
