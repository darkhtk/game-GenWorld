# DEVELOPER Loop Log

**Last run:** 2026-04-30 (S-084 Phase 2 — EventOriginId 태깅 + Spawner 구독 정리 → In Review)
**Status:** S-084 In Review 제출

## 이번 루프 완료 작업

### S-084: WorldEventSystem 종료 잔존 오브젝트 정리 (Phase 2)
BOARD `🔧 In Progress` 본 작업 재개. 1차(cf72a6a)에서 추가한 ForceEndActiveEvent 인프라 위에 태깅+정리 코어를 얹는다.

| 영역 | 변경 |
|------|------|
| `Assets/Scripts/Entities/MonsterController.cs` | `string EventOriginId { get; set; }` 추가. null = 일반 리전 스폰, non-null = 해당 WorldEvent로 스폰됨. |
| `Assets/Scripts/Entities/MonsterSpawner.cs` | `OnEnable/OnDisable`에서 `WorldEventEndEvent` 구독/해제. `OnWorldEventEnded(e)` → `DespawnEventMonsters(e.id)` 호출. `DespawnEventMonsters(string)` — `_monsters` 순회하며 `EventOriginId == eventId`인 항목만 `Destroy` + `MonsterDespawnEvent` 발행 + 리스트에서 제거 (null/empty 가드). `SpawnEventMonster(def, pos, eventId)` — 향후 invasion/elite_spawn 핸들러가 호출할 진입점. Init 후 EventOriginId 태깅. |
| `Assets/Tests/EditMode/WorldEventCleanupTests.cs` | EditMode 테스트 4건 추가 — `EventOriginId` 기본값 null, 태그 일치만 정리, null/empty no-op, `WorldEventEndEvent` emit이 spawner 정리를 트리거. 기존 3건(System 측)과 합쳐 7건. |

**왜 이게 필요했나**: WorldEventSystem이 `invasion`/`elite_spawn`/`merchant` 타입 이벤트를 emit해도 정작 종료 시 잔존 오브젝트를 추적할 메커니즘이 없었음. 1차에서 `ForceEndActiveEvent` 경로를 만들었으니, 이제 정리 대상을 식별·제거할 인프라(`EventOriginId` + Spawner 구독)를 깔아야 향후 SPEC에서 실제 spawn 핸들러를 붙일 수 있음.

## 빌드 에러 점검
- `%LOCALAPPDATA%\Unity\Editor\Editor.log` `error CS` 검색: **0건** (마지막 컴파일 2026-04-28 12:13)
- `mcp-unity recompile_scripts` 호출 → 60s 큐 만료 (Unity Editor 미가동) — 사용자 측 Editor 가동 시 자동 검증.

## 자가 검증 (S-084 Phase 2)

### Step 2.5 UI 통합 자가 검증
1. **핵심 메서드 호출처**: `OnWorldEventEnded(WorldEventEndEvent)` — `MonsterSpawner.OnEnable`에서 `EventBus.On<WorldEventEndEvent>` 구독으로 자동 호출. 발행 측은 `WorldEventSystem.EndEvent` (자연 만료) + `ForceEndActiveEvent` (강제 종료, 1차에서 추가) — 프로덕션 호출처 ✓ (자동 트리거).
2. **UI**: WorldEvent는 게임 시간에 따라 자동 발생. 시작/종료 모두 `GameUIWiring`에서 HUD 히스토리 항목으로 노출 (`Event ended: <id>`) — 플레이어가 정리 시점을 시각적으로 인지 가능 ✓ (기존 UI 활용).
3. **SPEC 와이어프레임**: SPEC-S-084 미존재. **specs 참조: N (SPEC 부재).** 코디네이터 결정 시 후속 SPEC에서 invasion/elite_spawn 핸들러의 spawn 위치/수량 와이어프레임 명시 필요.

**알림**: `SpawnEventMonster(...)` 자체는 현재 프로덕션 호출처가 0이며 (테스트만 사용), 향후 SPEC 기반의 invasion/elite_spawn 핸들러가 추가되어야 실제 게임에서 관측 가능. 본 PR은 그 전까지 이미 동작하는 cleanup 측만 라이브로 두고, spawn 측은 API를 노출만 함. 클라이언트 리뷰에서 이 분할 적정성 판단 요청.

### Step 2.6 리뷰 필수 여부
- 새 시스템 추가 (EventOriginId 태깅 + Spawner 구독) → strict 리뷰 모드 + "새 시스템 추가" 규칙으로 **고객사 리뷰 필수**. 자가진행 금지.

## 다음 루프 계획
- S-084 Client 리뷰 결과 대기. APPROVE 시 Done 이동, NEEDS_WORK 시 Rejected → 수정.
- 새 픽업 후보 (BACKLOG_RESERVE 코드 품질/UX 섹션 최상단): S-125 SkillTree 미해금 노드 툴팁에 해금 조건 명시 → S-126 옵션창 ESC 일관성 → S-128 퀘스트 추적 UI 진행률 바 …
- Rejected 발생 시 그쪽이 최우선.
