# SPEC-S-074: MonsterSpawner _nightPoolBuffer stale 데이터

> **태스크:** S-074
> **우선순위:** P3
> **태그:** 🔧 안정성 개선
> **관련 파일:**
> - `Assets/Scripts/Entities/MonsterSpawner.cs`
> - `Assets/Tests/EditMode/MonsterSpawnerTests.cs` (테스트 추가)

## 배경

`MonsterSpawner._nightPoolBuffer`는 야간 전환 시 낮 몬스터 풀(`monsterIds`)과 야간 전용 몬스터 풀(`nightMonsterIds`)을 합쳐 저장하는 인스턴스 수준의 `List<string>` 버퍼다. Client 리뷰에서 이 버퍼가 낮→밤 전환 시에만 채워지고, **밤→낮 전환 시에는 Clear되지 않아** 이전 야간 데이터가 잔존함이 발견되었다.

## 현재 동작

`SpawnForRegion()` (MonsterSpawner.cs, line 48-53):

```csharp
if (isNight && region.nightMonsterIds != null && region.nightMonsterIds.Length > 0)
{
    _nightPoolBuffer.Clear();
    _nightPoolBuffer.AddRange(region.monsterIds);
    _nightPoolBuffer.AddRange(region.nightMonsterIds);
}
```

그 아래 스폰 루프 (line 57-59):

```csharp
string monsterId = _nightPoolBuffer.Count > 0
    ? _nightPoolBuffer[Random.Range(0, _nightPoolBuffer.Count)]
    : monsterPool[Random.Range(0, monsterPool.Length)];
```

**문제:** `isNight == false` 이거나 `nightMonsterIds`가 비어 있는 경우, `_nightPoolBuffer`를 Clear하지 않는다. 결과적으로 직전 야간 호출에서 채워진 데이터가 남아 있는 상태로 스폰 루프가 `_nightPoolBuffer.Count > 0` 조건을 만족하게 되어, **낮 지역에서도 야간 전용 몬스터가 스폰**될 수 있다.

발생 경로:

1. 야간에 `SpawnForRegion()` 호출 → `_nightPoolBuffer` 채워짐
2. 낮으로 전환 후 `SpawnForRegion()` 재호출 (`isNight == false`)
3. `_nightPoolBuffer.Clear()` 분기를 건너뜀
4. 스폰 루프가 stale buffer 기반으로 야간 몬스터를 섞어 스폰

추가 발생 경로:

- 야간(`isNight == true`)이나 대상 region에 `nightMonsterIds`가 null/빈 배열인 경우에도 Clear 없이 진행. 이전 region 야간 데이터가 잔존하면 동일 문제 발생.

**위험도:** P3 — 재현 조건(야간 → 낮 전환 후 동일 세션에서 스폰 재호출)이 정규 플레이 중 반드시 발생하며, 낮 지역에 야간 몬스터가 섞이면 밸런스 및 퀘스트 킬 카운트에 영향을 줄 수 있다.

## 목표 동작

`SpawnForRegion()` 진입 시 항상 `_nightPoolBuffer`를 Clear하고, 야간+nightMonsterIds 조건이 충족될 때만 채운다. 스폰 루프는 버퍼 유무를 신뢰할 수 있어야 한다.

## 구현 상세

`Assets/Scripts/Entities/MonsterSpawner.cs`의 `SpawnForRegion()` 내 nightPool 구성 로직을 아래와 같이 수정한다.

**현재 코드 (line 47-53):**

```csharp
string[] monsterPool = region.monsterIds;
if (isNight && region.nightMonsterIds != null && region.nightMonsterIds.Length > 0)
{
    _nightPoolBuffer.Clear();
    _nightPoolBuffer.AddRange(region.monsterIds);
    _nightPoolBuffer.AddRange(region.nightMonsterIds);
}
```

**수정 코드:**

```csharp
string[] monsterPool = region.monsterIds;
_nightPoolBuffer.Clear();
if (isNight && region.nightMonsterIds != null && region.nightMonsterIds.Length > 0)
{
    _nightPoolBuffer.AddRange(region.monsterIds);
    _nightPoolBuffer.AddRange(region.nightMonsterIds);
}
```

변경점: `_nightPoolBuffer.Clear()`를 `if` 블록 밖으로 이동하여 조건에 관계없이 항상 먼저 실행한다. `AddRange` 두 줄은 조건부로 유지된다.

## 호출 진입점

`SpawnForRegion()`은 두 경로에서 호출된다:

| 호출 위치 | 조건 |
|-----------|------|
| `GameManager.SpawnInitialRegion()` (line 265) | 게임 초기화 시 1회 |
| `GameManager.HandleRegionTransition()` (line 238) | 플레이어가 새 region으로 진입 시 |

야간 전환(`TimeSystem.Period`가 `"night"`로 바뀜) 자체는 `GameManager.Update()`에서 `TimeSystem.Update()`를 호출하여 감지되나, 현재 **period 변경 시 `SpawnForRegion()` 재호출 로직이 없다.** 따라서 야간→낮 전환 후 stale buffer는 다음 region 이동 시 `SpawnForRegion()` 재호출 때 발현된다. 이 spec의 수정으로 해당 시점에 버퍼가 올바르게 정리된다.

> **참고:** 야간 전환 시 즉시 스폰 풀을 갱신하는 기능(period 변경 → 현재 region respawn)은 별도 태스크(S-075 등)로 분리 검토한다. 본 태스크는 stale 데이터 정리에만 집중한다.

## 데이터 구조

변경 없음. `_nightPoolBuffer`(`List<string>`)는 인스턴스 내 임시 버퍼로 직렬화/저장 대상이 아니다.

```csharp
// MonsterSpawner.cs line 10 — 변경 없음
readonly List<string> _nightPoolBuffer = new();
```

## 세이브 연동

영향 없음. `_nightPoolBuffer`는 런타임 전용이며 저장/복원 대상이 아니다. `SaveController`나 `QuestSystem` 연동 없음.

## 검증 항목

- [ ] 야간에 spawnForRegion 호출 후, 낮으로 전환하여 spawnForRegion 재호출 시 `_nightPoolBuffer.Count == 0`
- [ ] 낮 상태에서 스폰된 몬스터 목록이 `region.monsterIds`에서만 선택됨
- [ ] 야간 + `nightMonsterIds` 있는 region: `_nightPoolBuffer`에 `monsterIds + nightMonsterIds` 합집합 존재
- [ ] 야간 + `nightMonsterIds` null/빈 region: `_nightPoolBuffer.Count == 0` (monsterPool 폴백 사용)
- [ ] 기존 `ClearAllMonsters()` 동작 변화 없음
- [ ] 연속 region 전환 시 (낮→밤→낮) 각 호출 후 버퍼 상태가 기대값과 일치

## Test Plan

`Assets/Tests/EditMode/MonsterSpawnerTests.cs` (신규 또는 기존 파일에 추가):

```csharp
// 1. 야간 호출 후 낮 재호출 시 버퍼 비어 있음
[Test]
public void SpawnForRegion_AfterNightThenDay_NightPoolBufferIsEmpty()
// Setup: isNight=true → SpawnForRegion; isNight=false → SpawnForRegion
// Assert: _nightPoolBuffer.Count == 0 (reflection 또는 internal accessor 사용)

// 2. 야간 + nightMonsterIds 있는 경우 버퍼에 합집합 존재
[Test]
public void SpawnForRegion_NightWithNightMonsterIds_BufferContainsBothPools()
// Assert: buffer contains all monsterIds and nightMonsterIds entries

// 3. 야간 + nightMonsterIds 없는 경우 버퍼 비어 있음 (폴백 사용)
[Test]
public void SpawnForRegion_NightWithNoNightMonsterIds_BufferIsEmpty()
// Assert: buffer.Count == 0

// 4. 낮 상태에서 버퍼 비어 있음
[Test]
public void SpawnForRegion_DayTime_BufferIsEmpty()
// Assert: buffer.Count == 0
```

> MonsterSpawner는 MonoBehaviour이므로 EditMode 테스트에서 `new GameObject().AddComponent<MonsterSpawner>()` 또는 `ScriptableObject` 방식으로 인스턴스 생성. `TimeSystem.Period`는 `GameManager.Instance`에서 읽으므로 테스트용 mock/stub 필요 또는 GameManager 없이 `isNight` 값을 직접 주입할 수 있도록 `SpawnForRegion`에 optional 파라미터(`bool? overrideIsNight = null`) 추가를 검토한다.
