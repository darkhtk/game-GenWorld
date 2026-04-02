# SPEC-R001: 몬스터 디스폰/컬링 시스템

## 목적
플레이어에서 먼 몬스터를 자동 제거하여 메모리 누수를 방지하고 성능을 유지한다.

## 현재 상태
- `MonsterSpawner.cs`가 리전 기반으로 몬스터를 스폰하지만, 디스폰 로직이 없음.
- 플레이어가 이동하면 이전 리전의 몬스터가 계속 남아 메모리 증가.

## 구현 명세

### 수치
- **디스폰 거리:** 플레이어로부터 50 타일 (1600px at 32PPU)
- **체크 주기:** 2초마다 (coroutine)
- **유예 시간:** 스폰 후 5초간은 디스폰 면제 (스폰 즉시 삭제 방지)
- **전투 중 보호:** 플레이어가 최근 3초 이내 공격한 몬스터는 디스폰 면제

### 연동 경로
- **수정 파일:** `Assets/Scripts/Entities/MonsterSpawner.cs`
- **참조 파일:** `Assets/Scripts/Entities/MonsterController.cs` (디스폰 시 cleanup)
- **이벤트:** `EventBus` — `MonsterDespawnEvent` 추가 (LootSystem 등이 구독 가능)

### 데이터 구조
```csharp
// MonsterController에 추가
public float SpawnTime { get; set; }
public float LastHitByPlayerTime { get; set; }

// MonsterSpawner에 추가
private List<MonsterController> _activeMonsters = new();
private const float DESPAWN_DISTANCE = 50f; // tiles
private const float DESPAWN_CHECK_INTERVAL = 2f;
private const float SPAWN_GRACE_PERIOD = 5f;
private const float COMBAT_GRACE_PERIOD = 3f;
```

### 로직
1. MonsterSpawner가 `_activeMonsters` 리스트 관리.
2. 2초마다 코루틴으로 전체 리스트 순회.
3. 조건 충족 시 `Destroy(monster.gameObject)` + 리스트에서 제거.
4. 디스폰된 슬롯은 다음 스폰 사이클에서 자연스럽게 보충됨.

### 세이브 연동
- 디스폰된 몬스터는 세이브에 영향 없음 (몬스터는 리전 데이터에서 동적 생성).

## 호출 진입점
- `MonsterSpawner.Start()` 에서 디스폰 코루틴 자동 시작.
- 별도 UI 진입점 없음 (백그라운드 시스템).

## 테스트 항목
- [ ] 50타일 이상 떨어진 몬스터가 2초 내 제거되는지
- [ ] 스폰 후 5초 이내 몬스터는 제거되지 않는지
- [ ] 최근 공격한 몬스터는 보호되는지
- [ ] 디스폰 후 리전 재진입 시 정상 리스폰되는지
