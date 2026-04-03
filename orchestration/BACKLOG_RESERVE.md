# Backlog Reserve

> **최종 갱신:** 2026-04-03 (Supervisor — S-076~S-078 커밋 동기화, S-089/S-093/S-099 버그 수정, S-081/S-088/S-090/S-095 검증 완료)
> **방향:** stabilize — 안정성 > 개선 > 신규 기능

## 미완료 태스크

| # | ID | 태그 | 태스크 | 우선순위 | 상태 |
|---|-----|------|--------|---------|------|
| ~~1~~ | ~~S-021~~ | ~~🔧~~ | ~~테스트 커버리지 확장 — CombatSystem, InventorySystem, SaveMigrations 단위 테스트 추가~~ | ~~P3~~ | ✅ |
| ~~2~~ | ~~S-038~~ | ~~🔧~~ | ~~WorldEvent 동시 실행 방지 — 월드 이벤트 중복 발동 방어~~ | ~~P3~~ | ✅ |
| 3 | S-040 | 🔧 | CombatManager 타겟팅 범위 — REVIEW-S040-v1 ✅ APPROVE | P3 | ✅ |
| 4 | S-041 | 🔧 | NPC 호감도 데이터 저장 — 검증 완료, Restore clamp 방어 추가 | P3 | ✅ |
| 5 | S-042 | 🔧 | SaveSystem 동시 저장 경합 방지 — 자동 저장 중 수동 저장 요청 시 잠금/대기 처리 | P2 | ✅ |
| 6 | S-043 | 🔧 | CombatManager 동시 사망 보상 — 동시에 여러 몬스터 사망 시 XP/골드 누적 정확성 검증 | P2 | ✅ |
| 7 | S-044 | 🔧 | 장비 교체 시 스탯 복원 — 장비 해제/교체 시 이전 스탯 보너스 정확히 제거 확인 | P2 | ✅ |
| 8 | S-045 | 🔧 | QuestSystem 진행률 저장 — v2 APPROVE | P2 | ✅ |
| ~~9~~ | ~~S-046~~ | ~~🔧~~ | ~~MonsterSpawner 리전 전환 클린업 — APPROVE~~ | ~~P2~~ | ✅ |
| ~~10~~ | ~~S-047~~ | ~~🔧~~ | ~~DialogueSystem 동시 대화 방지 — APPROVE~~ | ~~P2~~ | ✅ |
| ~~11~~ | ~~S-048~~ | ~~🔧~~ | ~~SkillSystem 데이터 무결성 — REVIEW-S048-v2 APPROVE~~ | ~~P2~~ | ✅ |
| 12 | S-049 | 🔧 | ObjectPool 최대 크기 제한 — 검증 완료: maxSize 적정, null 처리/Return 중복 방지 완비 | P3 | ✅ |
| ~~13~~ | ~~S-050~~ | ~~🔧~~ | ~~InputSystem UI/게임 입력 분리 — APPROVE~~ | ~~P2~~ | ✅ |
| 14 | S-051 | 🔧 | SceneTransition 메모리 누수 — v2 APPROVE | P2 | ✅ |
| 15 | S-052 | 🔧 | EventBus 이벤트 순서 안정성 — 검증 완료: LIFO 순서, 전 핸들러 독립, using Linq 제거 | P3 | ✅ |
| 16 | S-053 | 🔧 | PlayerController 벽 끼임 방지 — CCD Continuous 설정 완료 (f80733e) | P3 | ✅ |
| 17 | S-054 | 🔧 | AutoSave 전투 중 저장 방지 — 전투 상태에서 자동 저장 스킵 (데이터 일관성) | P2 | ✅ |
| 18 | S-055 | 🔧 | UI 해상도 대응 — REVIEW-S055-v1 ✅ APPROVE (tooltip 오프셋 스케일링) | P3 | ✅ |
| 19 | S-056 | 🔧 | GameManager 초기화 순서 — Awake/Start 의존성 순서 보장 및 경합 확인 | P2 | ✅ |
| 20 | S-059 | 🔧 | AudioManager 클립 캐시 메모리 누수 — _clipCache 무한 성장 방지, LRU 또는 씬 전환 시 정리 | P2 | ✅ |
| 21 | S-060 | 🔧 | MinimapUI 텍스처 재생성 누수 — Init() 호출 시 이전 Texture2D 미파괴 확인 | P2 | ✅ |
| 22 | S-061 | 🔧 | QuestSystem killProgress 고아 항목 — 완료/포기된 퀘스트의 진행 데이터 잔류 정리 | P2 | ✅ |
| 23 | S-062 | 🔧 | ShopUI 구매 실패 피드백 — 골드 부족/인벤 풀 시 플레이어에게 사유 표시 | P3 | ✅ |
| 24 | S-063 | 🔧 | EnhanceUI 강화 전 확인 팝업 — 파괴 확률 경고 없이 바로 실행되는 문제 | P3 | ✅ |
| 25 | S-064 | 🔧 | DialogueUI 코루틴 중복 실행 — AI 응답 대기 중 재진입 시 이전 코루틴 미정리 | P2 | ✅ |
| 26 | S-065 | 🔧 | EffectHolder DoT 중복 적용 — 동일 DoT 재적용 시 기존 값 덮어쓰기 검증 | P2 | ✅ |
| 27 | S-066 | 🔧 | CraftingSystem LINQ 잔여 제거 — using System.Linq 제거, 수동 루프 교체 | P3 | ✅ |
| 28 | S-067 | 🔧 | SkillTreeUI 잠긴 스킬 사유 표시 — 레벨/포인트 부족 구분 없이 회색 표시되는 문제 | P3 | ✅ |
| 29 | S-068 | 🔧 | QuestUI 빈 목록 안내 — 퀘스트 없을 때 빈 화면 → placeholder 텍스트 표시 | P3 | ✅ |
| 30 | S-069 | 🔧 | Projectile 풀 반환 null 콜백 방어 — REVIEW-S069-v1 ✅ APPROVE | P3 | ✅ |
| 31 | S-070 | 🎨 | ShopUI/InventoryUI 아이템 아이콘 폴백 — 누락 아이콘 시 기본 placeholder 스프라이트 표시 | P3 | ✅ |
| 32 | S-071 | 🔧 | ShopUI Destroy 대량 호출 GC 스파이크 — ClearEntries()에서 풀링 또는 SetActive(false) 교체 | P3 | ✅ |
| 33 | S-072 | 🎨 | 상태이상 아이콘 추가 — burn/freeze/bleed 등 누락 상태 아이콘 생성 | P3 | ✅ |
| 34 | S-073 | 🔧 | TimeSystem 기간 전환 로그 스팸 — 기간(dawn/day/dusk/night) 변경 시 중복 로그 방지 | P3 | ✅ |
| 35 | S-074 | 🔧 | MonsterSpawner _nightPoolBuffer stale 데이터 — Clear()를 night 조건 밖으로 이동, Def null 방어 추가 | P3 | ✅ |
| 36 | S-075 | 🔧 | MonsterController 사망 상태 피격 방지 — TakeDamage에 IsDead/DeathProcessed 조기 반환 추가 | P2 | ✅ |
| 37 | S-076 | 🔧 | CombatManager 동시 공격 순차 처리 — player death guard + pendingKills 통합 (bd123e4) | P3 | ✅ |
| 38 | S-077 | 🔧 | SaveSystem 슬롯 데이터 무결성 검증 — SaveData.Validate() 로드 시 필드 검증 + 폴백 (fb8f223) | P2 | ✅ |
| 39 | S-078 | 🔧 | DialogueSystem AI 응답 타임아웃 — CancellationToken 30s + fallback + elapsed UI (3185a32) | P2 | ✅ |
| 40 | S-079 | 🔧 | EffectHolder 버프 스택 상한 — 동일 버프 무한 중첩 방지 (maxStack 도입 검토) | P3 | ⬜ |
| 41 | S-080 | 🔧 | PlayerController CCD 확인 — CollisionDetectionMode2D.Continuous 설정 검증 완료 | P3 | ✅ |
| 42 | S-081 | 🔧 | InventorySystem 중복 아이템 ID 병합 — 검증 완료: 스택 로직 정상 동작 | P3 | ✅ |
| 43 | S-082 | 🔧 | UIManager 패널 중복 열기 방지 — 8개 UI에 IsOpen 가드 추가 완료 | P2 | ✅ |
| 44 | S-083 | 🔧 | AudioManager BGM 동시 변경 안정성 — 연속 BGM 전환 요청 시 마지막 요청만 수행 검증 | P3 | ⬜ |
| 45 | S-084 | 🔧 | WorldEventSystem 종료 잔존 오브젝트 정리 — 이벤트 완료 후 스폰된 오브젝트 확실히 제거 확인 | P3 | ⬜ |
| 46 | S-085 | 🔧 | NPC 대화 종료 직후 재진입 방지 — Close 후 쿨다운 타이머로 즉시 재대화 차단 | P3 | ⬜ |
| 47 | S-086 | 🔧 | SkillBar 쿨다운 시각 동기화 — 스킬 사용 후 쿨다운 필 업데이트 정밀도 확인 | P3 | ⬜ |
| 48 | S-087 | 🔧 | RegionManager 씬 전환 중 입력 차단 — 로딩 중 PlayerController 입력 비활성 확인 | P2 | ⬜ |
| 49 | S-088 | 🔧 | CombatManager 빈 스킬 슬롯 사용 방어 — 검증 완료: UseSkill null 방어 + result.success 체크 정상 | P2 | ✅ |
| 50 | S-089 | 🔧 | InventorySystem RemoveItem 음수 수량 방어 — count <= 0 가드 추가 완료 | P2 | ✅ |
| 51 | S-090 | 🔧 | QuestSystem 중복 수락 방지 — 검증 완료: _active.ContainsKey 이미 구현됨 | P2 | ✅ |
| 52 | S-091 | 🔧 | UIManager 씬 전환 시 열린 패널 일괄 닫기 — 씬 전환 후 잔존 UI 패널 정리 확인 | P2 | ⬜ |
| 53 | S-092 | 🔧 | DialogueUI 긴 텍스트 오버플로 처리 — AI 응답이 텍스트 영역 초과 시 스크롤/잘림 확인 | P3 | ⬜ |
| 54 | S-093 | 🔧 | PlayerController 사망 후 입력 차단 — DeathScreenUI에서 Frozen=true 설정, Respawn에서 복원 | P2 | ✅ |
| 55 | S-094 | 🔧 | CraftingUI 재료 부족 항목별 표시 — 어떤 재료가 부족한지 개별 색상/텍스트로 표시 확인 | P3 | ⬜ |
| 56 | S-095 | 🔧 | EquipmentSystem 빈 슬롯 장착 해제 방어 — 검증 완료: TryGetValue+null 체크 이미 구현됨 | P2 | ✅ |
| 57 | S-096 | 🔧 | MonsterController 공격 범위 외 데미지 차단 — 거리 체크 없는 피해 적용 경로 확인 | P3 | ⬜ |
| 58 | S-097 | 🔧 | SaveSystem 슬롯 삭제 확인 팝업 — 세이브 삭제 시 확인 없이 즉시 삭제되는 문제 확인 | P3 | ⬜ |
| 59 | S-098 | 🔧 | AchievementUI 알림 큐 중복 방지 — 동시 달성 시 알림 겹침/누락 확인 | P3 | ⬜ |
| 60 | S-099 | 🔧 | HUD 상태바 음수 값 표시 방지 — Mathf.Clamp01 적용 (HP/MP/XP/Effect fill) | P2 | ✅ |

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
| S-066 | CraftingSystem LINQ 잔여 제거 — Where/FirstOrDefault/All → 수동 루프 + using Linq 제거 | 2026-04-03 |
| S-067 | SkillTreeUI 잠긴 스킬 사유 표시 — 레벨/포인트 부족 분리 표시 (빨간색) | 2026-04-03 |
| S-068 | QuestUI 빈 목록 안내 — "No active/completed quests" placeholder 추가 | 2026-04-03 |
| S-046 | MonsterSpawner 리전 전환 클린업 — REVIEW-S046-v1 APPROVE | 2026-04-03 |
| S-071 | ShopUI Destroy GC 스파이크 → SetActive(false) 풀링 교체 | 2026-04-03 |
| S-073 | TimeSystem 기간 전환 로그 스팸 → _lastPeriod 추적으로 중복 제거 | 2026-04-03 |
| S-048 | SkillSystem 데이터 무결성 v2 — REVIEW-S048-v2 APPROVE | 2026-04-03 |
| S-061 | QuestSystem killProgress 고아 항목 정리 (Developer commit) | 2026-04-03 |
| S-062 | ShopUI 구매 실패 피드백 — 골드 부족/인벤 풀 사유 표시 추가 | 2026-04-03 |
| S-063 | EnhanceUI 강화 전 확인 팝업 — 파괴 확률 경고 표시 | 2026-04-03 |
| S-064 | DialogueUI 코루틴 재진입 방지 (Developer commit) | 2026-04-03 |
| S-065 | EffectHolder DoT 재적용 로직 수정 (Developer commit) | 2026-04-03 |
| S-051 | SceneTransition 메모리 누수 v2 — REVIEW-S051-v2 APPROVE | 2026-04-03 |
