# Backlog Reserve

> **최종 갱신:** 2026-04-03 (Supervisor 루프 #19)
> **방향:** stabilize — 안정성 > 개선 > 신규 기능

## 미완료 태스크

| # | ID | 태그 | 태스크 | 우선순위 | 상태 |
|---|-----|------|--------|---------|------|
| 1 | S-002 | 🔧 | EventBus 구독 누수 방지 — GameManager OnDestroy에서 EventBus.Clear() 호출 | P1 | 👀 |
| 2 | S-003 | 🔧 | GameManager async fire-and-forget 방어 — AI.Init(), HandleDialogueResponse 예외 핸들링 | P1 | ⬜ |
| 3 | S-004 | 🔧 | MonsterController DoT 사망 시 킬 보상/제거 미처리 — MonsterSpawner.RemoveMonster 연결 필요 | P1 | ⬜ |
| 4 | S-005 | 🔧 | InventorySystem.OccupiedSlots LINQ 할당 제거 — Count() → 수동 카운트 변환 | P2 | ⬜ |
| 5 | S-006 | 🔧 | GameManager 분할 리팩토링 — 884줄 → 300줄 이하 (DialogueManager, SaveController 등 분리) | P2 | ⬜ |
| 6 | S-007 | 🔧 | CombatManager _cachedMonsters 참조 안정성 — stale reference 방어 | P2 | ⬜ |
| 7 | S-008 | 🔧 | PlayerController _cachedCamera null 체크 — 씬 전환 시 Camera.main 무효화 대비 | P2 | ⬜ |
| 8 | S-009 | 🔧 | MonsterController FlashWhite 코루틴 중복 방지 — 빠른 연타 히트 시 색상 깨짐 | P2 | ⬜ |
| 9 | S-010 | 🔧 | QuestSystem null 방어 강화 — quest 완료 보상에서 items null 체크 | P2 | ⬜ |
| 10 | S-011 | 🔧 | DataManager 로드 실패 시 기본값 폴백 — JSON 파싱 에러에서 빈 Dictionary 반환 | P2 | ⬜ |
| 11 | S-012 | 🔧 | AudioManager null 참조 일괄 방어 — Instance 패턴에 초기화 전 호출 방어 | P2 | ⬜ |
| 12 | S-013 | 🔧 | DamageText 풀링 검증 — 대량 몬스터 전투 시 텍스트 오브젝트 누수 확인 | P2 | ⬜ |
| 13 | S-014 | 🔧 | Projectile 풀링 검증 — Get() null 반환 시 호출부 방어 | P2 | ⬜ |
| 14 | S-015 | 🔧 | WorldMapGenerator null 방어 — Walkable 배열 미초기화 시 IndexOutOfRange 방지 | P2 | ⬜ |
| 15 | S-016 | 🔧 | SkillSystem 쿨다운 동기화 — TimeSystem과 스킬 쿨다운 시간 단위(ms vs s) 일관성 검증 | P2 | ⬜ |
| 16 | S-017 | 🎨 | 몬스터 사망 이펙트 스프라이트 — vfx_monster_death 에셋 존재 확인/생성 | P3 | ⬜ |
| 17 | S-018 | 🎨 | 누락 SFX placeholder 생성 — sfx_combo, sfx_error 등 빈 AudioClip 생성 | P3 | ⬜ |
| 18 | S-019 | 🎨 | 퀘스트 아이콘 에셋 — quest_marker, quest_complete 아이콘 생성 | P3 | ⬜ |
| 19 | S-020 | 🎨 | 상태이상 아이콘 — stun, slow, poison, mana_shield, stealth 아이콘 생성 | P3 | ⬜ |
| 20 | S-021 | 🔧 | 테스트 커버리지 확장 — CombatSystem, InventorySystem, SaveMigrations 단위 테스트 추가 | P3 | ⬜ |
| 21 | S-022 | 🔧 | EffectHolder.Tick 스레드 안전성 — 이펙트 순회 중 삭제 시 InvalidOperationException 방지 | P2 | ⬜ |
| 22 | S-023 | 🔧 | RegionTracker 경계 조건 — 맵 밖 좌표 입력 시 예외 방지 | P2 | ⬜ |
| 23 | S-024 | 🔧 | ComboSystem 타이머 정밀도 — nowMs float 정밀도 손실 검증 (큰 Time.time 값) | P3 | ⬜ |
| 24 | S-025 | 🔧 | DialogueCameraZoom 복원 보장 — 대화 중 강제 종료 시 카메라 줌 원복 | P3 | ⬜ |
| 25 | S-026 | 🔧 | NPC 이동 재개 실패 — 대화 종료 후 ResumeMoving 미호출 경로 확인 | P2 | ⬜ |

## 완료 태스크

| ID | 태스크 | 완료일 |
|----|--------|--------|
| (이전 RESERVE 기록 소실 — 코루틴 사고로 파일 덮어씀) | — | — |
