# SPEC-S-146: invasion / elite_spawn 핸들러 → SpawnEventMonster 호출처 연결

> **작성:** 2026-04-30 (Coordinator, 7회차)
> **출처:** BACKLOG_RESERVE.md S-146 (Systems/Spec, P2 — S-084 Phase 2 후속)
> **우선순위:** P2 (S-084 Phase 2 인프라가 production 호출처 0인 상태 정상화)
> **상태:** Spec Drafted → 구현 대기 (Dev-Backend 픽업)
> **호출 진입점:**
> - **발화:** `WorldEventSystem.StartEvent(def, gameHour)` (`WorldEventSystem.cs:66`) → 기존 `EventBus.Emit(new WorldEventStartEvent { id, name, description, duration })` (line 83~87)
> - **신규 구독:** `WorldEventMonsterHandler.OnEnable()` → `EventBus.On<WorldEventStartEvent>(OnEventStart)` — 신규 MonoBehaviour, `GameUIWiring` 또는 `GameManager` 자식 오브젝트에 부착
> - **정리:** 기존 `MonsterSpawner.OnWorldEventEnded(WorldEventEndEvent e) => DespawnEventMonsters(e.id)` (`MonsterSpawner.cs:104`) — 변경 없음
> - **스폰 API:** 기존 `MonsterSpawner.SpawnEventMonster(MonsterDef def, Vector2 pos, string eventId)` (`MonsterSpawner.cs:126`, S-084 Phase 2 신규) — 변경 없음

---

## 1. 문제 정의

`WorldEventSystem.StartEvent` (line 66~89) 의 `switch (def.type)` 분기에 **`bonus_drops`만 처리** (GlobalDropMultiplier/GlobalGoldMultiplier 갱신). 나머지 3종은 `EventBus.Emit(WorldEventStartEvent)` + `Debug.Log`만 출력 → 실제 게임플레이 영향 없음:

| 이벤트 ID | type | 현재 동작 | 의도 동작 |
| --- | --- | --- | --- |
| `blood_moon` | `elite_spawn` | 메시지만 | **엘리트 몬스터 N마리 스폰** |
| `goblin_raid` | `invasion` | 메시지만 | **고블린 N마리 마을 침공** |
| `wandering_merchant` | `merchant` | 메시지만 | (별도 SPEC — NPC 스폰, 본 SPEC 범위 외 → S-148 분리) |

S-084 Phase 2에서 `MonsterSpawner.SpawnEventMonster(def, pos, eventId)` API + `EventOriginId` 태깅 + `WorldEventEndEvent` 자동 정리 인프라 완성. 그러나 **production 호출처 0건** → S-146으로 핸들러 연결.

목표:
- `elite_spawn`/`invasion` 2종 핸들러 구현 → `SpawnEventMonster` 호출 → 종료 시 `OnWorldEventEnded` 자동 정리.
- 핸들러는 별도 MonoBehaviour로 분리 (WorldEventSystem은 순수 C# 클래스, MonsterSpawner와 직접 결합 회피).

비목표:
- `merchant` 핸들러 (NPC 스폰 — 별도 SPEC-S-148로 분리. NPC 데이터/대화 트리/위치 명세 필요).
- 신규 이벤트 추가 (`WorldEventSystem.Init` 4종 그대로 유지).
- AI 패턴 변경 (엘리트는 `MonsterDef.rarity="elite"` 데이터로 표현, `MonsterController` 동작 변경 X).

---

## 2. 수치/상수 (단일 소스)

`GameConfig.WorldEvent` 정적 클래스 신규 (`Assets/Scripts/Core/GameConfig.cs`):

| 상수 | 값 | 비고 |
| --- | --- | --- |
| `EliteSpawnCount` | **3** | Blood Moon — 엘리트 몬스터 수 |
| `EliteSpawnRadiusMin` | **8f** | 플레이어 기준 최소 반경 (즉시 충돌 방지) |
| `EliteSpawnRadiusMax` | **12f** | 플레이어 기준 최대 반경 (시야 내 발견) |
| `EliteMonsterIds` | `{"orc_chief","skeleton_lord","spider_queen"}` | 엘리트 풀 — 1마리당 무작위 선택 |
| `InvasionSpawnCount` | **5** | Goblin Raid — 고블린 수 |
| `InvasionSpawnRadiusMin` | **5f** | 마을 입구 기준 최소 |
| `InvasionSpawnRadiusMax` | **10f** | 마을 입구 기준 최대 |
| `InvasionMonsterId` | `"goblin"` | 고블린 단일 (id, MonsterDatabase 기존) |
| `InvasionAnchorRegionId` | `"village"` | 마을 입구 — 다른 region 활성 시 핸들러 SKIP |
| `EventSpawnPoiTagPlayer` | `true` | true=플레이어 위치 기준, false=region anchor |

엘리트 몬스터 ID 3종은 RPG 표준 풀(MonsterDatabase 존재 가정 — 미존재 시 핸들러 fallback `MonsterDatabase.Get(id)?.IsValid`로 조용히 SKIP).

---

## 3. 연동 경로 (변경 범위)

### 3.1 신규 `WorldEventMonsterHandler.cs`
- 위치: `Assets/Scripts/Systems/WorldEventMonsterHandler.cs` (MonoBehaviour, GameManager 자식 오브젝트 또는 GameUIWiring 형제 오브젝트)
- 의존성: `MonsterSpawner` (SerializeField 또는 `FindFirstObjectByType`), `MonsterDatabase` (static lookup)

```csharp
public class WorldEventMonsterHandler : MonoBehaviour
{
    [SerializeField] MonsterSpawner spawner;

    void OnEnable()  { EventBus.On<WorldEventStartEvent>(OnEventStart); }
    void OnDisable() { EventBus.Off<WorldEventStartEvent>(OnEventStart); }

    void OnEventStart(WorldEventStartEvent e)
    {
        if (spawner == null) spawner = FindFirstObjectByType<MonsterSpawner>();
        if (spawner == null) return;

        // type 분기 — id에서 type 역추출은 모호하므로 id 직접 매칭
        switch (e.id)
        {
            case "blood_moon":     SpawnEliteWave(e.id);    break;
            case "goblin_raid":    SpawnInvasion(e.id);     break;
            // wandering_merchant: SPEC-S-148 (NPC 핸들러)
        }
    }

    void SpawnEliteWave(string eventId) { ... }
    void SpawnInvasion(string eventId)  { ... }
}
```

### 3.2 `GameConfig.cs` 추가
- `public static class WorldEvent { ... }` 정적 클래스 — §2 11상수 + 헬퍼 `IsEliteSpawnEventId(string)`/`IsInvasionEventId(string)` (id 매칭).

### 3.3 `WorldEventSystem.cs` — 변경 없음
- StartEvent의 `switch (def.type) { case "bonus_drops": ... }` 그대로. 핸들러는 EventBus 구독만으로 동작.
- 단, **부수: `EventOriginId` 일관 태깅을 위해 핸들러가 `e.id`를 그대로 `SpawnEventMonster(eventId)`에 전달** — `WorldEventEndEvent.id`와 1:1 일치 보장.

### 3.4 씬 와이어링 — `GameScene.unity`
- 신규 GameObject `WorldEventMonsterHandler` 1개 (Asset/QA 영역).
- Inspector `spawner` 필드: `MonsterSpawner` 인스턴스 드래그(또는 핸들러가 `FindFirstObjectByType` fallback).

---

## 4. UI 와이어프레임

UI 변경 없음. 본 SPEC은 시스템 레벨(스폰 로직)만 다룸. 이벤트 메시지 표시(HUD 토스트 등)는 기존 `WorldEventStartEvent` 구독자(`WorldEventToast` 등) 그대로 작동.

---

## 5. 데이터 구조

신규 데이터 파일 없음. 기존 `MonsterDatabase`의 monster id 풀 사용:
- `goblin` (RESERVE invasion 표준)
- `orc_chief`/`skeleton_lord`/`spider_queen` (엘리트 풀 — 미존재 시 핸들러가 조용히 SKIP, 로그 1회)

---

## 6. 세이브 연동

`MonsterController.EventOriginId` 태깅은 휘발(런타임) — 세이브 시 이벤트 몬스터는 **저장 대상에서 제외**가 정석. 후속 S-147 (`SaveSystem.OnSceneUnload` → `WorldEventSystem.ForceEndActiveEvent` 와이어링) 적용 후엔 세이브 시점에 자동 정리되어 자연스럽게 휘발.

본 SPEC 단독 적용 시: 세이브 → 로드 사이에 이벤트 활성이면 로드 후 잔존 가능. **S-147과 함께 머지 권장** (BOARD에 묶음 메모).

---

## 7. 테스트 (EditMode, NUnit)

**파일:** `Assets/Tests/EditMode/WorldEventHandlerTests.cs`

### 핵심 케이스
1. `OnEventStart_BloodMoonSpawnsThreeEliteMonsters` — `EventBus.Emit(new WorldEventStartEvent { id="blood_moon" })` 후 spawner.SpawnEventMonster 3회 호출(Mock spawner 또는 실 인스턴스 + count 검증).
2. `OnEventStart_GoblinRaidSpawnsFiveGoblins` — `id="goblin_raid"` → 5마리.
3. `OnEventStart_BonusDropsDoesNotSpawn` — `id="golden_hour"` → 스폰 0건 (분기 누락 검증).
4. `OnEventStart_WanderingMerchantDoesNotSpawn` — `id="wandering_merchant"` → 스폰 0건 (S-148 범위).
5. `OnEventEnd_DespawnsTaggedMonstersOnly` — Spawn 후 `EventBus.Emit(new WorldEventEndEvent { id="blood_moon" })` → 스폰된 3마리 제거, 일반 몬스터(`EventOriginId=null`) 보존. **(S-145와 중복 — S-145 우선 머지 시 본 케이스 SKIP)**
6. `OnEventStart_NullSpawnerNoCrash` — `spawner=null` → Debug.LogWarning, 예외 없음.

### `[SetUp]` / `[TearDown]`
- `EventBus.Clear()` (S-145 권고와 일관) — 테스트 간 핸들러 누수 방지.

---

## 8. 호환성 / 회귀 리스크

| 리스크 | 완화 |
| --- | --- |
| `WorldEventStartEvent` 기존 구독자(예: `WorldEventToast`) 와의 충돌 | 본 핸들러는 추가 구독만 — 기존 구독 변경 없음, 순서 무관 |
| `MonsterSpawner.SpawnEventMonster` 동시 다중 호출 시 위치 클러스터링 | `Random.insideUnitCircle * Range(min, max)` 로 분산, 최소 반경 가드(8f/5f) |
| 엘리트 몬스터 id가 `MonsterDatabase`에 없을 때 NRE | `MonsterDatabase.Get(id) == null` 시 조용히 SKIP + Debug.Log 1회 |
| 활성 이벤트 진행 중 새 이벤트 시작(WorldEventSystem.StartEvent line 68: `if (_activeEvent != null) EndEvent()`) | EndEvent → WorldEventEndEvent emit → MonsterSpawner.OnWorldEventEnded → 기존 몬스터 자동 정리 → 신규 핸들러가 새 이벤트로 스폰. 순서 보장 |
| Save/Load 사이 이벤트 잔존 | S-147 머지 시 자동 해소. 본 SPEC 단독 시 README/BOARD 메모 |

---

## 9. 분할 PR 권고

| Phase | 범위 | 검증 |
| --- | --- | --- |
| Phase A (본 SPEC) | `WorldEventMonsterHandler` 신규 + GameConfig.WorldEvent 상수 + GameScene 와이어링 + EditMode 6건 | invasion/elite 스폰 동작 |
| Phase B (S-148 후속) | `WorldEventNpcHandler` (merchant) — NPC 데이터/위치/대화 트리 별도 SPEC | merchant NPC 스폰 |
| Phase C (S-147 머지) | `RegionManager.SwitchRegion` / `SaveSystem.OnSceneUnload` → `WorldEventSystem.ForceEndActiveEvent` | 씬 전환/세이브 시 강제 정리 |

본 SPEC = Phase A. 단독 머지 가능, S-148/S-147은 별도 PR.

---

## 10. DoD (Definition of Done)

1. `WorldEventMonsterHandler.cs` 신규 (Systems 영역, MonoBehaviour, EventBus 구독/해제).
2. `GameConfig.cs`에 `WorldEvent` 정적 클래스 + §2 11상수 추가.
3. `GameScene.unity`에 `WorldEventMonsterHandler` GameObject 1개 추가, spawner 인스펙터 바인딩.
4. EditMode `WorldEventHandlerTests` 6건 GREEN.
5. PlayMode 수동:
   - village 활성 + 게임 시간 진행 → blood_moon 발화 → 엘리트 3마리 스폰 → 종료 시 자동 정리 (`EventOriginId="blood_moon"` 태깅 일관).
   - village 활성 + goblin_raid 발화 → 고블린 5마리 스폰 → 종료 시 자동 정리.
   - golden_hour/wandering_merchant 발화 → 스폰 없음, 메시지만.
6. 기존 `bonus_drops` (Golden Hour) 동작 회귀 없음 (`GlobalDropMultiplier=2f`/`GlobalGoldMultiplier=1.5f` 그대로).
7. 4-페르소나 리뷰 APPROVE.

---

## 11. 메모

- **S-144 (`SpawnEventMonster` IsNullOrEmpty(eventId) 가드)** 와의 순서: S-144 먼저 머지 시 본 SPEC 핸들러는 항상 `e.id` 비어있지 않음을 가정 가능 → 핸들러 측 추가 가드 불필요.
- **S-145 (`PreservesUntaggedMonsters` 회귀 + `[SetUp] EventBus.Clear()`)** 와의 순서: S-145가 본 SPEC §7 #5와 중복 → S-145 우선 머지 후 본 SPEC 테스트는 #5 SKIP.
- 본 SPEC 머지 후 RESERVE에서 S-146 ~~취소선~~ 처리, 후속 S-148(merchant 핸들러) 신규 등재 검토.
