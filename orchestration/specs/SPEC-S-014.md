# SPEC-S-014: Projectile Pool Exhaustion Defense

> **Priority:** P2
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## Problem

`ObjectPool<T>.Get()` (line 53)은 풀이 소진되고 `_totalCreated >= _maxSize`일 때 `null`을 반환한다.

현재 Projectile 풀 설정: `initialSize=20, maxSize=50`. 다수의 몬스터가 동시에 원거리 공격하거나, fan/radial/scatter 패턴 스킬이 다량의 투사체를 생성하면 50개 한도를 초과할 수 있다.

### 호출부 현황

| 호출 위치 | null 방어 | 비고 |
|-----------|----------|------|
| `CombatManager.FireMonsterProjectile()` (line 132) | `if (proj == null) return;` | 방어됨 -- 몬스터 공격이 조용히 무시됨 |
| `ActionRunner.HandleSpawnProjectile()` (line 268) | `if (proj == null) continue;` | 방어됨 -- 스킬 투사체가 조용히 무시됨 |
| `DamageText.Spawn()` (line 40) | `if (dt != null)` | 방어됨 (별도 풀, maxSize=80) |
| `DamageText.SpawnText()` (line 49) | `if (dt != null)` | 방어됨 (별도 풀, maxSize=80) |

**겉보기에 방어가 되어 있지만, 실제 문제점은 다음과 같다:**

1. **조용한 실패 (Silent failure)** -- 투사체가 생성되지 않아도 로그가 없어서 디버깅이 불가능하다. 플레이어 스킬이 발동되었으나 투사체가 날아가지 않으면 버그로 인식된다.
2. **몬스터 원거리 공격 무시** -- 풀 소진 시 몬스터의 원거리 공격이 무시되어 난이도가 비정상적으로 낮아진다.
3. **maxSize 하드코딩** -- Projectile 풀의 maxSize(50)가 최악 시나리오(radial 8발 x 다수 스킬 + 몬스터 원거리)에 대한 근거 없이 설정되어 있다.
4. **Return 누락 가능성** -- `Projectile.ReturnToPool()`이 호출되기 전에 GameObject가 외부에서 Destroy되면 `_totalCreated`가 감소하지 않아 풀이 영구적으로 축소된다.

## Current Flow

```
Projectile.Get()
  -> EnsurePool()
  -> _pool.Get()
     -> Queue empty + _totalCreated >= 50
     -> return null   // silent drop

CombatManager.FireMonsterProjectile():
  var proj = Projectile.Get();
  if (proj == null) return;   // monster attack silently lost

ActionRunner.HandleSpawnProjectile():
  var proj = Projectile.Get();
  if (proj == null) continue; // player skill shot silently lost
```

## Required Fix

### 1. ObjectPool: null 반환 시 경고 로그 추가

```csharp
// ObjectPool.Get() -- line 52 부근
if (_totalCreated >= _maxSize)
{
    Debug.LogWarning($"[ObjectPool] Pool exhausted ({_maxSize}). Consider increasing maxSize.");
    return null;
}
```

### 2. Projectile 풀 maxSize 상향 또는 자동 확장

**Option A (권장): maxSize 상향 + 경고**

```csharp
// Projectile.EnsurePool() -- maxSize를 100으로 상향
_pool = new ObjectPool<Projectile>(..., initialSize: 20, maxSize: 100);
```

**Option B: ObjectPool에 auto-expand 모드 추가**

```csharp
public ObjectPool(Func<T> factory, Transform parent, int initialSize, int maxSize, bool autoExpand = false)
```
`autoExpand=true`이면 maxSize 초과 시에도 새 인스턴스를 생성하되 경고 로그를 남긴다.

### 3. Return 누락에 대한 방어

`ObjectPool.Get()`의 while 루프(line 33-42)에서 이미 파괴된 오브젝트를 `_totalCreated--`로 감소시키고 있으므로, 이 부분은 기존 코드가 올바르게 처리한다. 추가로 `Projectile.OnDestroy()`에서 풀 카운터를 보정하면 더 안전하다:

```csharp
// Projectile.cs
void OnDestroy()
{
    // Pool won't know this instance is gone; the while loop in Get()
    // handles it by decrementing _totalCreated when dequeuing null.
    // No additional action needed, but avoid external Destroy calls.
}
```

### 4. 호출부 로그 보강

```csharp
// CombatManager.FireMonsterProjectile()
var proj = Projectile.Get();
if (proj == null)
{
    Debug.LogWarning("[CombatManager] Projectile pool exhausted; monster ranged attack dropped.");
    return;
}

// ActionRunner.HandleSpawnProjectile()
var proj = Projectile.Get();
if (proj == null)
{
    Debug.LogWarning("[ActionRunner] Projectile pool exhausted; skill projectile dropped.");
    continue;
}
```

## Entry Point

- `Assets/Scripts/Core/ObjectPool.cs` -- `Get()` (line 31-53)
- `Assets/Scripts/Entities/Projectile.cs` -- `EnsurePool()` (line 27-45), `Get()` (line 47-51)
- `Assets/Scripts/Systems/CombatManager.cs` -- `FireMonsterProjectile()` (line 126-147)
- `Assets/Scripts/Systems/ActionRunner.cs` -- `HandleSpawnProjectile()` (line 211-288)
- `Assets/Scripts/Effects/DamageText.cs` -- `Spawn()` / `SpawnText()` (line 35-52)

## Acceptance Criteria

1. `ObjectPool.Get()`이 null을 반환할 때 `Debug.LogWarning` 출력
2. Projectile 풀의 maxSize가 실전 시나리오를 커버할 수 있는 값으로 조정 (100 이상)
3. `CombatManager`, `ActionRunner`의 null 분기에서 경고 로그 출력
4. 풀 소진 상황에서 NullReferenceException 없음 (기존 null guard 유지)
5. 기존 테스트 통과

## Data / Save Impact

None. Runtime pool 설정 변경만 해당. 저장 포맷 영향 없음.

## Test

- EditMode 단위 테스트: `ObjectPool<T>`를 maxSize=2로 생성, 3번 `Get()` 호출 -- 3번째에서 null 반환 확인
- EditMode 단위 테스트: `Get()` 후 `Return()`, 다시 `Get()` -- 재사용 확인, null 아님
- EditMode 단위 테스트: 풀에서 꺼낸 오브젝트를 Destroy한 뒤 `Get()` -- `_totalCreated` 보정 후 새 인스턴스 생성 확인
- PlayMode 수동 검증: radial 패턴 스킬(8발) + 몬스터 원거리 다수 동시 공격 -- 모든 투사체 정상 생성, 콘솔에 경고 없음
