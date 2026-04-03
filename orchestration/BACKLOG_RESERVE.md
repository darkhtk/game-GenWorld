# Backlog Reserve

> **최종 갱신:** 2026-04-03 (Coordinator 동기화 — S-047 상태 복원, S-038 Done 확인)
> **방향:** stabilize — 안정성 > 개선 > 신규 기능

## 미완료 태스크

| # | ID | 태그 | 태스크 | 우선순위 | 상태 |
|---|-----|------|--------|---------|------|
| ~~1~~ | ~~S-021~~ | ~~🔧~~ | ~~테스트 커버리지 확장 — CombatSystem, InventorySystem, SaveMigrations 단위 테스트 추가~~ | ~~P3~~ | ✅ |
| ~~2~~ | ~~S-038~~ | ~~🔧~~ | ~~WorldEvent 동시 실행 방지 — 월드 이벤트 중복 발동 방어~~ | ~~P3~~ | ✅ |
| 3 | S-040 | 🔧 | CombatManager 타겟팅 범위 — 화면 밖 몬스터 자동 타겟팅 방지 | P3 | ⬜ |
| 4 | S-041 | 🔧 | NPC 호감도 데이터 저장 — 세이브/로드 시 호감도 누락 확인 | P3 | ⬜ |
| 5 | S-042 | 🔧 | SaveSystem 동시 저장 경합 방지 — 자동 저장 중 수동 저장 요청 시 잠금/대기 처리 | P2 | ✅ |
| 6 | S-043 | 🔧 | CombatManager 동시 사망 보상 — 동시에 여러 몬스터 사망 시 XP/골드 누적 정확성 검증 | P2 | ✅ |
| 7 | S-044 | 🔧 | 장비 교체 시 스탯 복원 — 장비 해제/교체 시 이전 스탯 보너스 정확히 제거 확인 | P2 | ✅ |
| 8 | S-045 | 🔧 | QuestSystem 진행률 저장 — 게임 종료/크래시 시 퀘스트 중간 진행 상태 보존 확인 | P2 | 👀 |
| 9 | S-046 | 🔧 | MonsterSpawner 리전 전환 클린업 — 리전 전환 시 이전 리전 몬스터 오브젝트 정리 확인 | P2 | 👀 |
| 10 | S-047 | 🔧 | DialogueSystem 동시 대화 방지 — 다중 NPC 동시 대화 시작 잠금 확인 | P2 | ⬜ |
| 11 | S-048 | 🔧 | SkillSystem 데이터 무결성 — skills.json 필드 누락/잘못된 타입 시 방어 로딩 | P2 | 👀 |
| 12 | S-049 | 🔧 | ObjectPool 최대 크기 제한 — 풀 무한 성장 방지 (maxSize 상한 도입 검토) | P3 | ⬜ |
| 13 | S-050 | 🔧 | InputSystem UI/게임 입력 분리 — UI 패널 열린 상태에서 게임 입력 차단 확인 | P2 | ⬜ |
| 14 | S-051 | 🔧 | SceneTransition 메모리 누수 — 씬 전환 후 이전 씬 리소스 해제 확인 | P2 | ⬜ |
| 15 | S-052 | 🔧 | EventBus 이벤트 순서 안정성 — 동일 이벤트 다중 핸들러 실행 순서 보장 검증 | P3 | ⬜ |
| 16 | S-053 | 🔧 | PlayerController 벽 끼임 방지 — 콜라이더 경계에서 플레이어 위치 보정 확인 | P3 | ⬜ |
| 17 | S-054 | 🔧 | AutoSave 전투 중 저장 방지 — 전투 상태에서 자동 저장 스킵 (데이터 일관성) | P2 | ⬜ |
| 18 | S-055 | 🔧 | UI 해상도 대응 — 다양한 해상도에서 UI 앵커/레이아웃 정상 동작 검증 | P3 | ⬜ |
| 19 | S-056 | 🔧 | GameManager 초기화 순서 — Awake/Start 의존성 순서 보장 및 경합 확인 | P2 | ⬜ |

## 완료 태스크

| ID | 태스크 | 완료일 |
|----|--------|--------|
| S-004 | MonsterController DoT 사망 시 킬 보상/제거 — REVIEW-S004-v1 APPROVE | 2026-04-03 |
| S-008 | PlayerController 카메라 null 방어 — REVIEW-S005-S007-S008-S010-v1 APPROVE | 2026-04-03 |
| S-011 | DataManager 로드 실패 폴백 — REVIEW-S011-v1 APPROVE | 2026-04-03 |
| S-012 | AudioManager null 방어 — 이미 ?./!=null 체크 완비 확인 | 2026-04-03 |
| S-013 | DamageText 풀링 누수 방어 — OnDisable 풀 반환 + _inUse 이중반환 방지 | 2026-04-03 |
| S-014 | Projectile 풀링 검증 — 버그 없음, null 체크+반환 확인 | 2026-04-03 |
| S-015 | WorldMapGenerator null 방어 — Generate null/empty regions 가드 추가 | 2026-04-03 |
| S-016 | SkillSystem 쿨다운 동기화 — ms 단위 일관성 검증 완료 | 2026-04-03 |
| S-022 | EffectHolder.Tick 안전성 — REVIEW-S022-v1 APPROVE | 2026-04-03 |
| S-005 | InventorySystem LINQ 할당 제거 — Where/ToList → 수동 루프 + using Linq 제거 | 2026-04-03 |
| S-007 | CombatManager _cachedMonsters stale ref 방어 — _pendingKills 지연 처리 | 2026-04-03 |
| S-010 | QuestSystem null 방어 강화 — CompleteQuest에서 def.rewards null 시 안전 기본값 | 2026-04-03 |
| S-002 | EventBus 구독 누수 방지 — 이미 GameManager.OnDestroy에 EventBus.Clear() 구현됨 | 2026-04-03 |
| S-003 | async fire-and-forget 방어 — InitAISafe, HandleDialogueResponse에 try-catch 구현됨 | 2026-04-03 |
| S-009 | MonsterController FlashWhite 코루틴 중복 방지 — StopCoroutine 가드 추가 | 2026-04-03 |
| S-017 | 몬스터 사망 이펙트 스프라이트 — vfx_monster_death.png 이미 존재 확인 | 2026-04-03 |
| S-018 | 누락 SFX placeholder — sfx_combo.wav 생성 | 2026-04-03 |
| S-019 | 퀘스트 아이콘 에셋 — icon_quest_marker.png, icon_quest_complete.png 생성 | 2026-04-03 |
| S-020 | 상태이상 아이콘 — status_stealth.png 생성 | 2026-04-03 |
| S-024 | ComboSystem 타이머 정밀도 — float 정밀도 충분 확인 (24h에도 1ms 유지) | 2026-04-03 |
| S-023 | RegionTracker 경계 조건 — REVIEW-S023-v1 APPROVE | 2026-04-03 |
| S-025 | DialogueCameraZoom 복원 보장 — REVIEW-S025-v1 APPROVE | 2026-04-03 |
| S-026 | NPC 이동 재개 v2 — REVIEW-S026-v2 APPROVE | 2026-04-03 |
| S-027 | MonsterSpawner 중복 스폰 — cosmetic only, 문제 없음 | 2026-04-03 |
| S-028 | SaveSystem 자동 백업 — S-001에서 이미 구현됨 | 2026-04-03 |
| S-029 | 인벤토리 오버플로우 알림 — REVIEW-S029-v1 APPROVE | 2026-04-03 |
| S-030 | UIManager null 방어 — 모든 참조 null 체크 이미 존재 | 2026-04-03 |
| S-031 | 미니맵 아이콘 에셋 — 5종 모두 존재 확인 (player/monster/npc/quest/portal) | 2026-04-03 |
| S-032 | TimeSystem 일시정지 — Time.deltaTime 기반 자동 정지 | 2026-04-03 |
| S-033 | LootTable 빈 드롭 — null 체크 이미 존재 | 2026-04-03 |
| S-034 | DialogueSystem 선택지 인덱스 — foreach 기반, 인덱스 접근 없음 | 2026-04-03 |
| S-035 | 장비 아이콘 누락 — 25종 전체 존재 확인 (spritesheet+individual) | 2026-04-03 |
| S-036 | AchievementSystem 중복 방지 — _completed.Contains 이미 존재 | 2026-04-03 |
| S-037 | BuffSystem 만료 — EffectHolder.Tick 만료 제거 정상 | 2026-04-03 |
| S-039 | UI SFX 누락 수정 — 8개 UI 스크립트에 PlaySFX 호출 추가 (menu_open/close, confirm, tab_switch, craft, enchant, coin) | 2026-04-03 |
| S-006 | GameManager 분할 리팩토링 — 928줄→308줄, 4개 클래스 분리. REVIEW-S006-v1 APPROVE | 2026-04-03 |
| S-057 | 스킬 아이콘 누락 점검 — skills.json 대비 교차검증 완료 | 2026-04-03 |
| S-058 | 몬스터 스프라이트 누락 점검 — monsters.json 대비 교차검증 완료 | 2026-04-03 |
| S-042 | SaveSystem 동시 저장 경합 방지 — _isSaving 잠금 + 원자적 파일 쓰기 (tmp→move) | 2026-04-03 |
| S-043 | CombatRewardHandler 중복 보상 방어 — DeathProcessed 조기 반환 추가 | 2026-04-03 |
