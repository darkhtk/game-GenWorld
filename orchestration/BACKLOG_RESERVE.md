# Backlog Reserve

> **최종 갱신:** 2026-04-03 (Supervisor 루프 #20)
> **방향:** stabilize — 안정성 > 개선 > 신규 기능

## 미완료 태스크

| # | ID | 태그 | 태스크 | 우선순위 | 상태 |
|---|-----|------|--------|---------|------|
| 1 | S-004 | 🔧 | MonsterController DoT 사망 시 킬 보상/제거 미처리 — MonsterSpawner.RemoveMonster 연결 필요 | P1 | 👀 |
| 2 | S-005 | 🔧 | InventorySystem.OccupiedSlots LINQ 할당 제거 — Count() → 수동 카운트 변환 | P2 | 👀 |
| 3 | S-006 | 🔧 | GameManager 분할 리팩토링 — 884줄 → 300줄 이하 (DialogueManager, SaveController 등 분리) | P2 | ⬜ |
| 4 | S-007 | 🔧 | CombatManager _cachedMonsters 참조 안정성 — stale reference 방어 | P2 | 👀 |
| 5 | S-008 | 🔧 | PlayerController _cachedCamera null 체크 — 씬 전환 시 Camera.main 무효화 대비 | P2 | ⬜ |
| 6 | S-010 | 🔧 | QuestSystem null 방어 강화 — quest 완료 보상에서 items null 체크 | P2 | ⬜ |
| 7 | S-011 | 🔧 | DataManager 로드 실패 시 기본값 폴백 — JSON 파싱 에러에서 빈 Dictionary 반환 | P2 | ⬜ |
| 8 | S-012 | 🔧 | AudioManager null 참조 일괄 방어 — Instance 패턴에 초기화 전 호출 방어 | P2 | ⬜ |
| 9 | S-013 | 🔧 | DamageText 풀링 검증 — 대량 몬스터 전투 시 텍스트 오브젝트 누수 확인 | P2 | ⬜ |
| 10 | S-014 | 🔧 | Projectile 풀링 검증 — Get() null 반환 시 호출부 방어 | P2 | ⬜ |
| 11 | S-015 | 🔧 | WorldMapGenerator null 방어 — Walkable 배열 미초기화 시 IndexOutOfRange 방지 | P2 | ⬜ |
| 12 | S-016 | 🔧 | SkillSystem 쿨다운 동기화 — TimeSystem과 스킬 쿨다운 시간 단위(ms vs s) 일관성 검증 | P2 | ⬜ |
| 13 | S-021 | 🔧 | 테스트 커버리지 확장 — CombatSystem, InventorySystem, SaveMigrations 단위 테스트 추가 | P3 | ⬜ |
| 14 | S-022 | 🔧 | EffectHolder.Tick 스레드 안전성 — 이펙트 순회 중 삭제 시 InvalidOperationException 방지 | P2 | ⬜ |
| 15 | S-023 | 🔧 | RegionTracker 경계 조건 — 맵 밖 좌표 입력 시 예외 방지 | P2 | ⬜ |
| 16 | S-024 | 🔧 | ComboSystem 타이머 정밀도 — nowMs float 정밀도 손실 검증 (큰 Time.time 값) | P3 | ⬜ |
| 17 | S-025 | 🔧 | DialogueCameraZoom 복원 보장 — 대화 중 강제 종료 시 카메라 줌 원복 | P3 | ⬜ |
| 18 | S-026 | 🔧 | NPC 이동 재개 실패 — 대화 종료 후 ResumeMoving 미호출 경로 확인 | P2 | ⬜ |

## 완료 태스크

| ID | 태스크 | 완료일 |
|----|--------|--------|
| S-002 | EventBus 구독 누수 방지 — 이미 GameManager.OnDestroy에 EventBus.Clear() 구현됨 | 2026-04-03 |
| S-003 | async fire-and-forget 방어 — InitAISafe, HandleDialogueResponse에 try-catch 구현됨 | 2026-04-03 |
| S-009 | MonsterController FlashWhite 코루틴 중복 방지 — StopCoroutine 가드 추가 | 2026-04-03 |
| S-017 | 몬스터 사망 이펙트 스프라이트 — vfx_monster_death.png 이미 존재 확인 | 2026-04-03 |
| S-018 | 누락 SFX placeholder — sfx_combo.wav 생성 (sfx_error 이미 존재) | 2026-04-03 |
| S-019 | 퀘스트 아이콘 에셋 — icon_quest_marker.png, icon_quest_complete.png 생성 | 2026-04-03 |
| S-020 | 상태이상 아이콘 — status_stealth.png 생성 (나머지 이미 존재) | 2026-04-03 |
| (이전 RESERVE 기록 소실 — 코루틴 사고로 파일 덮어씀) | — | — |
