# Orchestration Board

> **최종 업데이트:** 2026-04-03
> **프로젝트:** GenWorld
> **현재 상태:** 안정화 (Stabilize) — 견고성 강화 진행 중

---

## 로드맵

| # | 태스크 | 우선순위 | 상태 | 비고 |
|---|--------|---------|------|------|

---

## ❌ Rejected (최우선 수정)

| 태스크 | 사�� | REVIEW | 비고 |
|--------|------|--------|------|

## 🔧 In Progress

| 태스크 | 담당 | 시작일 | 비고 |
|--------|------|--------|------|

## 👀 In Review

| 태스크 | 완료일 | 결과 | 비고 |
|--------|--------|------|------|
| S-079 EffectHolder 버프 스택 상한 |  | ⏳ | maxStack + DefaultMaxStack + GetStackCount + 8 tests |
| S-087 RegionManager 씬 전환 중 입력 차단 |  | ⏳ | SPEC-S-087 |
| S-091 UIManager 씬 전환 시 열린 패널 일괄 닫기 |  | ⏳ | SPEC-S-091 |
| S-083 AudioManager BGM 동시 변경 안정성 |  | ⏳ | P3 |
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정 |  | ⏳ | high |
| S-102 게임 핵심 시스템 런타임 추적 및 안정성 개선 |  | ⏳ | high |
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 |  | ⏳ | P3 |
| S-085 NPC 대화 종료 직후 재진입 방지 |  | ⏳ | P3 |
| S-086 SkillBar 쿨다운 시각 동기화 |  | ⏳ | P3 |
| S-092 DialogueUI 긴 텍스트 오버플로 처리 |  | ⏳ | P3 |
| S-094 CraftingUI 재료 부족 항목별 표시 |  | ⏳ | P3 |
| S-096 MonsterController 공격 범위 외 데미지 차단 |  | ⏳ | ⏳ |
| S-097 SaveSystem 슬롯 삭제 확인 팝업 | 2026-04-04 | ⏳ | P3 |

## ✅ Done

| 태스크 | 완료일 | 비고 |
|--------|--------|------|
| S-078 DialogueSystem AI 응답 타임아웃 |  | REVIEW-S078-v1 ✅ APPROVE |
| S-077 SaveSystem 슬롯 데이터 무결성 검증 |  | REVIEW-S077-v1 ✅ APPROVE |
| S-076 CombatManager 동시 공격 순차 처리 |  | REVIEW-S076-v1 ✅ APPROVE |
| S-069 Projectile 풀 반환 null 콜백 방어 |  | REVIEW-S069-v1 ✅ APPROVE |
| S-053 PlayerController 벽 끼임 방지 |  | REVIEW-S053-v1 ✅ APPROVE |
| S-055 UI 해상도 대응 |  | REVIEW-S055-v1 ✅ APPROVE |
| S-074 MonsterSpawner nightPoolBuffer stale 데이터 |  | Supervisor — Clear 위치 이동 + Def null 방어 ✅ |
| S-075 MonsterController 사망 상태 피격 방지 |  | Supervisor — TakeDamage 조기 반환 ✅ |
| S-073 TimeSystem 기간 전환 로그 스팸 |  | Supervisor — _lastPeriod 추적 중복 제거 ✅ |
| S-072 상태이상 아이콘 추가 |  | Supervisor — 7종 아이콘 생성 ✅ |
| S-071 ShopUI Destroy GC 스파이크 |  | Supervisor — SetActive(false) 풀링 ✅ |
| S-070 ShopUI/InventoryUI 아이콘 폴백 |  | Supervisor — placeholder 스프라이트 ✅ |
| S-068 QuestUI 빈 목록 안내 |  | Supervisor — placeholder 텍스트 ✅ |
| S-067 SkillTreeUI 잠긴 스킬 사유 표시 |  | Supervisor — 레벨/포인트 분리 표시 ✅ |
| S-066 CraftingSystem LINQ 잔여 제거 |  | Supervisor — 수동 루프 교체 ✅ |
| S-063 EnhanceUI 강화 전 확인 팝업 |  | Supervisor — 파괴 확률 경고 ✅ |
| S-062 ShopUI 구매 실패 피드백 |  | Supervisor — 골드 부족/인벤 풀 사유 ✅ |
| S-059 AudioManager 클립 캐시 메모리 누수 |  | Supervisor 감사 — LRU/씬 전환 정리 ✅ |
| S-052 EventBus 이벤트 순서 안정성 |  | 검증 완료 — 자가진행 (🔧 검증) |
| S-049 ObjectPool 최대 크기 제한 |  | 검증 완료 — 자가진행 (🔧 검증) |
| S-041 NPC 호감도 데이터 저장 |  | 검증 완료 — 자가진행 (🔧 검증) |
| S-040 CombatManager 타겟팅 범위 |  | REVIEW-S040-v1 ✅ APPROVE |
| S-051 SceneTransition 메모리 누수 v2 |  | REVIEW-S051-v2 ✅ APPROVE |
| S-061 QuestSystem killProgress 고아 항목 |  | REVIEW-S061-v1 ✅ APPROVE |
| S-064 DialogueUI 코루틴 중복 실행 |  | REVIEW-S064-v1 ✅ APPROVE |
| S-065 EffectHolder DoT 중복 적용 |  | REVIEW-S065-v1 ✅ APPROVE |
| R-001 몬스터 디스폰/컬링 시스템 |  | REVIEW-R001-v1 ✅ |
| R-002 오브젝트 풀링 |  | REVIEW-R002-v2 ✅ |
| R-003 세이브 버전 마이그레이션 |  | REVIEW-R003-v1 ✅ |
| R-004 JSON 파싱 복구 |  | REVIEW-R004-v1 ✅ |
| R-005 CombatManager null 방어 |  | REVIEW-R005-v1 ✅ |
| R-006 리전 전환 자동 저장 |  | REVIEW-R006-v1 ✅ |
| R-007 어그로/리쉬 개선 |  | REVIEW-R007-v1 ✅ |
| R-008 조건부 대화 분기 |  | REVIEW-R008-v1 ✅ |
| R-009 스킬 콤보 시스템 |  | REVIEW-R009-v1 ✅ |
| R-010 회피 시각 피드백 |  | REVIEW-R010-v1 ✅ |
| R-011 아이템/스킬 툴팁 |  | REVIEW-R011-v1 ✅ |
| R-012 HUD 버프/디버프 아이콘 |  | REVIEW-R012-v1 ✅ |
| R-013 인벤토리 필터/정렬 |  | REVIEW-R013-v2 ✅ |
| R-014 퀘스트 추적 HUD 위젯 |  | REVIEW-R014-v1 ✅ |
| R-015 몬스터 HP 바 |  | REVIEW-R015-v1 ✅ |
| R-016 장비 비교 팝업 |  | REVIEW-R016-v1 ✅ |
| R-027 AnimationDef |  | REVIEW-R027-v2 ✅ |
| R-028 AnimationPreviewUI |  | REVIEW-R028-v1 ✅ |
| R-029 PlayerAnimator 검증 |  | REVIEW-R029-v1 ✅ |
| R-030 Monster 애니메이션 검증 |  | REVIEW-R030-v1 ✅ |
| R-031 NPC 애니메이션 검증 |  | REVIEW-R031-v1 ✅ |
| R-032 스킬 이펙트 애니메이션 검증 |  | REVIEW-R032-v1 ✅ |
| R-017 사운드/음악 시스템 |  | REVIEW-R017-v1 ✅ |
| R-018 미니맵 UI |  | REVIEW-R018-v1 ✅ |
| R-019 NPC 일과 시스템 |  | REVIEW-R019-v1 ✅ |
| R-020 NPC 호감도 이벤트 |  | REVIEW-R020-v1 ✅ |
| R-021 주/야간 사이클 |  | REVIEW-R021-v1 ✅ |
| R-022 업적 시스템 |  | REVIEW-R022-v1 ✅ |
| R-023 장비 강화 실패/파괴 |  | REVIEW-R023-v1 ✅ |
| R-024 월드 이벤트 시스템 |  | REVIEW-R024-v1 ✅ |
| R-037 XP 플로팅 텍스트 |  | REVIEW-R037-038-041-v1 ✅ |
| R-038 골드 플로팅 텍스트 |  | REVIEW-R037-038-041-v1 ✅ |
| R-041 지역 진입 알림 |  | REVIEW-R037-038-041-v1 ✅ |
| R-034 설정 메뉴 |  | REVIEW-R034-035-042-v1 ✅ |
| R-035 키바인딩 표시 |  | REVIEW-R034-035-042-v1 ✅ |
| R-036 사망 화면 |  | REVIEW-R036-039-v1 ✅ |
| R-039 자동 포션 |  | REVIEW-R036-039-v1 ✅ |
| R-042 게임 통계 |  | REVIEW-R034-035-042-v1 ✅ |
| R-033 로딩 화면 |  | REVIEW-R033-040-v1 ✅ |
| R-040 NPC 카메라 줌인 |  | REVIEW-R033-040-v1 ✅ |
| R-035 Steamworks SDK |  | REVIEW-R035-v1 ✅ (Steam) |
| R-036 Steam 클라우드 |  | REVIEW-R036-v1 ✅ (Steam) |
| R-037 Steam 업적 |  | REVIEW-R037-v1 ✅ (Steam) |
| R-038 SettingsManager |  | REVIEW-R038-v1 ✅ (Steam) |
| B-001 몬스터 이동 불가 |  | REVIEW-B001-008-v1 ✅ |
| B-002 NPC 제자리 흔들림 |  | REVIEW-B001-008-v1 ✅ |
| B-003 NPC 대화 불가 |  | REVIEW-B001-008-v1 ✅ |
| B-004 미니맵 회색 빈 화면 |  | REVIEW-B001-008-v1 ✅ |
| B-005 스킬트리 아이콘 |  | REVIEW-B001-008-v1 ✅ |
| B-006 Ollama AI 연동 |  | REVIEW-B001-008-v1 ✅ |
| B-007 인벤토리 아이콘 |  | REVIEW-B001-008-v1 ✅ |
| B-008 카메라 Z값 |  | REVIEW-B001-008-v1 ✅ |
| R-039 Settings UI v2 |  | REVIEW-R039-v2 ✅ (Steam) |
| R-040 SteamPipe v2 |  | REVIEW-R040-v2 ✅ (Steam) |
| R-041 QA 체크리스트 v2 |  | REVIEW-R041-v2 ✅ (Steam) |
| V-001 HUD 바 스타일링 |  | 자가진행 ✅ (폴리시) |
| V-003 스킬바 아이콘 표시 |  | 자가진행 ✅ (폴리시) |
| V-010 카메라 줌 조절 |  | 자가진행 ✅ (폴리시) |
| V-011 데미지 색상 분류 |  | 자가진행 ✅ (폴리시) |
| V-007 몬스터/NPC 이름표 |  | 자가진행 ✅ (폴리시) |
| V-009 UI 패널 애니메이션 |  | 자가진행 ✅ (폴리시) |
| BF-009 LevelSystem→StatsSystem |  | 빌드 에러 수정 ✅ |
| V-002 인벤토리 그리드 미관 |  | 자가진행 ✅ (폴리시) |
| V-004 대화 UI 포트레이트 |  | 자가진행 ✅ (폴리시) |
| V-005 메인 메뉴 배경 |  | 자가진행 ✅ (폴리시) |
| V-006 부트 스플래시 로고 |  | 자가진행 ✅ (폴리시) |
| V-008 파티클/이벤트 VFX |  | 자가진행 ✅ (폴리시) |
| V-012 타일맵 경계 블렌딩 |  | 자가진행 ✅ (폴리시) |
| S-001 세이브 파일 손상 복구 |  | REVIEW-S001-v1 ✅ |
| S-002 EventBus 구독 누수 방지 |  | 이미 구현됨 확인 ✅ |
| S-003 async fire-and-forget 방어 |  | 이미 구현됨 확인 ✅ |
| S-009 FlashWhite 코루틴 중복 방지 |  | StopCoroutine 가드 추가 ✅ |
| S-017 몬스터 사망 이펙트 확인 |  | 이미 존재 확인 ✅ |
| S-018 누락 SFX placeholder |  | sfx_combo.wav 생성 ✅ |
| S-019 퀘스트 아이콘 에셋 |  | icon_quest_marker/complete 생성 ✅ |
| S-020 상태이상 아이콘 |  | status_stealth 생성 ✅ |
| S-005 LINQ 할당 제거 |  | Where/ToList→수동 루프 ✅ |
| S-007 CombatManager stale ref 방어 |  | _pendingKills 지연 처리 ✅ |
| S-010 QuestSystem null 방어 |  | CompleteQuest rewards null 기본값 ✅ |
| S-004 DoT 사망 킬 보상 미처리 |  | REVIEW-S004-v1 ✅ |
| S-008 PlayerController 카메라 null 방어 |  | REVIEW-S005-S007-S008-S010-v1 ✅ |
| S-011 DataManager 로드 실패 폴백 |  | REVIEW-S011-v1 ✅ |
| S-022 EffectHolder.Tick 안전성 |  | REVIEW-S022-v1 ✅ |
| S-013 DamageText 풀링 검증 |  | 버그 없음 확인 ✅ |
| S-014 Projectile 풀링 검증 |  | 버그 없음 확인 ✅ |
| S-015 WorldMapGenerator null 방어 |  | null/empty regions 가드 ✅ |
| S-016 SkillSystem 쿨다운 동기화 |  | ms 단위 일관성 확인 ✅ |
| S-024 ComboSystem 타이머 정밀도 |  | float 정밀도 충분 확인 ✅ |
| S-023 RegionTracker 경계 조건 |  | REVIEW-S023-v1 ✅ |
| S-025 DialogueCameraZoom 복원 보장 |  | REVIEW-S025-v1 ✅ |
| S-033 LootTable 빈 드롭 |  | null 체크 이미 존재 ✅ |
| S-036 AchievementSystem 중복 방지 |  | _completed.Contains 이미 존재 ✅ |
| S-029 인벤토리 오버플로우 알림 |  | REVIEW-S029-v1 ✅ |
| S-030 UIManager null 방어 |  | 모든 참조 null 체크 이미 존재 ✅ |
| S-032 TimeSystem 일시정지 |  | Time.deltaTime 기반 자동 정지 ✅ |
| S-034 DialogueSystem 선택지 |  | foreach 기반, 인덱스 접근 없음 ✅ |
| S-037 BuffSystem 만료 |  | EffectHolder.Tick 만료 제거 정상 ✅ |
| S-026 NPC 이동 재개 v2 |  | REVIEW-S026-v2 ✅ |
| S-039 UI SFX 누락 수정 |  | 8개 UI에 PlaySFX 추가 ✅ |
| S-006 GameManager 분할 리팩토링 |  | REVIEW-S006-v1 ✅ APPROVE |
| S-021 테스트 커버리지 확장 |  | REVIEW-S021-v1 ✅ APPROVE |
| S-042 SaveSystem 동시 저장 경합 방지 |  | Supervisor 감사 — CRITICAL 수정 ✅ |
| S-043 CombatRewardHandler 중복 보상 방어 |  | Supervisor 감사 — MEDIUM 수정 ✅ |
| S-044 장비 교체 시 스탯 복원 |  | Supervisor 감사 — 버그 없음 확인 ✅ |
| S-038 WorldEvent 동시 실행 방지 |  | REVIEW-S038 ✅ APPROVE |
| S-046 MonsterSpawner 리전 전환 클린업 |  | REVIEW-S046-v1 ✅ APPROVE |
| S-047 DialogueSystem 동시 대화 방지 |  | REVIEW-S047-v1 ✅ APPROVE |
| S-050 InputSystem UI/게임 입력 분리 |  | REVIEW-S050-v1 ✅ APPROVE |
| S-045 QuestSystem 진행률 저장 v2 |  | REVIEW-S045-v2 ✅ APPROVE |
| S-054 AutoSave 전투 중 저장 방지 |  | REVIEW-S054-v1 ✅ APPROVE |
| S-056 GameManager 초기화 순서 |  | REVIEW-S056-v1 ✅ APPROVE |
| S-048 SkillSystem 데이터 무결성 v2 |  | REVIEW-S048-v2 ✅ APPROVE |

## 📋 Backlog

| 태스크 | 우선순위 | 비고 |
|--------|---------|------|
| S-098 AchievementUI 알림 큐 중복 방지 | P3 | P3 |
 P3 |
