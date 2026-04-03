# Backlog Reserve

> **최종 갱신:** 2026-04-03 (Coordinator 동기화)
> **방향:** stabilize — 안정성 > 개선 > 신규 기능

## 미완료 태스크

| # | ID | 태그 | 태스크 | 우선순위 | 상태 |
|---|-----|------|--------|---------|------|
| 1 | S-006 | 🔧 | GameManager 분할 리팩토링 — 884줄 → 300줄 이하 (DialogueManager, SaveController 등 분리) | P2 | ⬜ |
| 2 | S-026 | 🔧 | NPC 이동 재개 실패 — 대화 종료 후 SetDialogueOpen/dialogueNpc 복구 누락 (NEEDS_WORK) | P2 | ❌ |
| 4 | S-021 | 🔧 | 테스트 커버리지 확장 — CombatSystem, InventorySystem, SaveMigrations 단위 테스트 추가 | P3 | ⬜ |
| 5 | S-025 | 🔧 | DialogueCameraZoom 복원 보장 — 대화 중 강제 종료 시 카메라 줌 원복 | P3 | ✅ |
| 6 | S-027 | 🔧 | MonsterSpawner 중복 스폰 방지 — 같은 위치 동시 스폰 시 겹침 처리 | P2 | ⬜ |
| 7 | S-028 | 🔧 | SaveSystem 자동 백업 검증 — 자동 저장 시 이전 세이브 백업 존재 확인 | P2 | ⬜ |
| 8 | S-029 | 🔧 | InventorySystem 슬롯 오버플로우 — 인벤 가득 찬 상태에서 아이템 획득 방어 | P2 | ⬜ |
| 9 | S-030 | 🔧 | UIManager null 참조 방어 — 패널 전환 중 null 참조 확인 | P2 | ⬜ |
| 10 | S-031 | 🎨 | 미니맵 아이콘 에셋 — NPC/몬스터/퀘스트 마커 아이콘 점검 및 누락분 생성 | P3 | ⬜ |
| 11 | S-032 | 🔧 | TimeSystem 일시정지 연동 — 게임 일시정지 시 TimeSystem 시간 경과 동기화 확인 | P2 | ⬜ |
| 12 | S-033 | 🔧 | LootTable 빈 드롭 방어 — 몬스터 드롭테이블 비어있을 때 null 방어 | P2 | ⬜ |
| 13 | S-034 | 🔧 | DialogueSystem 선택지 인덱스 범위 — 선택지 인덱스 out-of-range 방어 | P2 | ⬜ |
| 14 | S-035 | 🎨 | 장비 아이콘 누락 확인 — 장비 JSON 데이터 대비 아이콘 파일 존재 교차검증 | P3 | ⬜ |
| 15 | S-036 | 🔧 | AchievementSystem 중복 달성 방지 — 동일 업적 중복 해금 방어 | P2 | ⬜ |
| 16 | S-037 | 🔧 | BuffSystem 만료 처리 — 버프 만료 시 스탯 복원 누락 확인 | P2 | ⬜ |
| 17 | S-038 | 🔧 | WorldEvent 동시 실행 방지 — 월드 이벤트 중복 발동 방어 | P3 | ⬜ |
| 18 | S-039 | 🎨 | 누락 UI 사운드 확인 — 버튼 클릭/패널 열기 SFX 누락 리스트업 및 placeholder 생성 | P3 | ⬜ |
| 19 | S-040 | 🔧 | CombatManager 타겟팅 범위 — 화면 밖 몬스터 자동 타겟팅 방지 | P3 | ⬜ |
| 20 | S-041 | 🔧 | NPC 호감도 데이터 저장 — 세이브/로드 시 호감도 누락 확인 | P3 | ⬜ |

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
