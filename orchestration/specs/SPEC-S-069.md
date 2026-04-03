# SPEC-S-069: Projectile 풀 반환 시 null 콜백 방어

> **우선순위:** P3
> **태그:** 🔧 안정성 개선
> **관련 파일:**
> - `Assets/Scripts/Entities/Projectile.cs`
> - `Assets/Scripts/Core/ObjectPool.cs`
> - `Assets/Scripts/Systems/ActionRunner.cs`
> - `Assets/Scripts/Systems/CombatManager.cs`

## 배경

`Projectile`은 오브젝트 풀(`ObjectPool<Projectile>`)로 관리된다. 투사체가 목표에 도달하거나 몬스터에 충돌하면 `ReturnToPool()`을 호출해 풀에 반환하고 콜백(`OnHitMonster`, `OnArrive`)을 null로 초기화한다.

그러나 Unity 물리 엔진은 동일 프레임에 `OnTriggerEnter2D`를 여러 번 발생시킬 수 있다. non-piercing 투사체는 첫 번째 충돌에서 `OnHitMonster?.Invoke()` → `ReturnToPool()`을 순서대로 실행하지만, 같은 프레임 내에 이미 큐잉된 두 번째 `OnTriggerEnter2D`가 `_arrived = true` 검사를 통과하기 전에 콜백이 실행될 경우, 풀에 반환된 인스턴스의 콜백이 재호출될 수 있다.

piercing 투사체(`_piercing = true`)는 `ReturnToPool()`을 호출하지 않으므로 이 경로는 안전하지만, 동일 몬스터 중복 타격은 `_hitIds`로 막고 있다.

또한 `OnArrive` 경로에서는 `OnArrive?.Invoke(_to)` 직후 `ReturnToPool()`을 호출한다. 콜백 내부에서 `proj` 참조를 캡처해 사용하는 람다(예: `ActionRunner.cs:282`)가 실행 완료되기 전에 풀이 인스턴스를 재사용하면 stale 참조 문제가 발생할 수 있다.

## 현재 동작

```
OnTriggerEnter2D() 흐름 (non-piercing):
  1. _arrived 검사 → false (통과)
  2. OnHitMonster?.Invoke(monster)    ← 콜백 실행
  3. _arrived = true
  4. ReturnToPool()                   ← OnHitMonster = null, 풀 반환

문제 시나리오:
  - 같은 프레임에 두 번째 OnTriggerEnter2D 발생
  - 단계 1에서 _arrived가 아직 false → 통과
  - OnHitMonster?.Invoke() 재실행 → 동일 몬스터에 이중 피해 또는 null 역참조
```

`ReturnToPool()`은 콜백을 null로 초기화한 뒤 `_pool.Return(this)`를 호출한다. 그러나 콜백 null 초기화와 `Return()` 사이의 순간, 또는 `Invoke()` 후 `Return()` 전에 또 다른 물리 콜백이 끼어들 수 있다.

## 목표 동작

- `_arrived = true` 세팅을 콜백 실행 **전**으로 이동하여, 동일 프레임 내 중복 물리 이벤트가 콜백을 이중 호출하는 것을 방지한다.
- `OnHitMonster` 및 `OnArrive`를 로컬 변수에 캡처한 뒤 필드를 null로 초기화하고 로컬 변수로 호출(Capture-then-clear 패턴)하여, 콜백 실행 중 풀 반환이 일어나도 stale 참조를 방지한다.
- non-piercing 충돌의 경우 `OnHitMonster` 호출과 `ReturnToPool()` 사이에 `_arrived` 플래그가 이미 설정되어 있어 재진입이 차단된다.

## 구현 상세

### 변경 1: `OnTriggerEnter2D` — `_arrived` 플래그를 콜백 전으로 이동 + Capture-then-clear

```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if (_arrived) return;

    var monster = other.GetComponent<MonsterController>();
    if (monster == null || monster.IsDead) return;

    int id = monster.GetInstanceID();
    if (_hitIds.Contains(id)) return;
    _hitIds.Add(id);

    // For non-piercing: set _arrived before invoking so that any
    // physics callbacks queued in the same frame see _arrived = true.
    if (!_piercing)
        _arrived = true;

    var cb = OnHitMonster;   // capture
    OnHitMonster = null;     // clear before invoke to prevent reuse after pool return
    cb?.Invoke(monster);

    if (!_piercing)
        ReturnToPool();
}
```

### 변경 2: `Update` — `OnArrive` Capture-then-clear

```csharp
if (_traveled >= _totalDist)
{
    _arrived = true;
    var cb = OnArrive;   // capture
    OnArrive = null;     // clear before invoke
    cb?.Invoke(_to);
    ReturnToPool();
}
```

### 변경 3: `ReturnToPool` — 방어적 null 초기화 유지 (현행 유지, 이중 안전망)

```csharp
void ReturnToPool()
{
    OnHitMonster = null;
    OnArrive = null;
    if (_pool != null)
        _pool.Return(this);
    else
        Destroy(gameObject);
}
```

`ReturnToPool()`의 null 초기화는 그대로 유지한다. 위 변경 1/2에서 이미 null 처리하지만, `ReturnToPool()`이 다른 경로(타임아웃)에서도 호출되므로 이중 안전망으로 남긴다.

### 변경 4: `Update` 타임아웃 경로 — Capture-then-clear 적용

```csharp
if (Time.time - _spawnTime > Timeout)
{
    _arrived = true;
    ReturnToPool();   // OnHitMonster/OnArrive 이미 null이 될 것이지만 명시적으로 ReturnToPool 유지
    return;
}
```

타임아웃 경로는 `OnArrive`를 호출하지 않으므로 현행 그대로 유지한다.

## 호출 진입점

| 경로 | 파일 | 설명 |
|------|------|------|
| `FireMonsterProjectile()` | `CombatManager.cs:164` | `proj.OnArrive` 설정, 플레이어에게 피해 |
| `HandleProjectile()` | `ActionRunner.cs:268` | `proj.OnHitMonster` + `proj.OnArrive` 설정, 스킬 피해/onHit 체인 |
| `Update()` (이동 완료) | `Projectile.cs:129` | `OnArrive` 호출 후 풀 반환 |
| `OnTriggerEnter2D()` (충돌) | `Projectile.cs:136` | `OnHitMonster` 호출 후 풀 반환(non-piercing) |
| `Update()` (타임아웃) | `Projectile.cs:117` | 콜백 없이 풀 반환 |

## 데이터 구조

변경 없음. `public Action<MonsterController> OnHitMonster`와 `public Action<Vector2> OnArrive` 필드 자체는 유지하며, 초기화 순서만 변경한다.

## 세이브 연동

없음. 투사체는 런타임 전용 오브젝트이며 세이브/로드 시스템과 무관하다.

## 검증 항목

- [ ] non-piercing 투사체가 몬스터에 명중 시 `_arrived`가 `OnHitMonster.Invoke()` 전에 `true`로 설정된다
- [ ] 같은 프레임에 두 번째 `OnTriggerEnter2D`가 발생해도 `_arrived = true` 검사로 조기 반환된다
- [ ] `OnHitMonster` 콜백이 한 번만 실행된다 (동일 몬스터에 이중 피해 없음)
- [ ] piercing 투사체는 `_hitIds`로 동일 몬스터 중복 타격이 차단되며, 동작 변화 없음
- [ ] `OnArrive` 콜백은 풀 반환 전에 로컬 캡처 후 실행되어, 반환된 인스턴스가 콜백에서 참조되지 않는다
- [ ] 타임아웃 경로에서 풀 반환이 정상 작동한다
- [ ] `Init()` 재호출 시 `OnHitMonster = null`, `OnArrive = null` 초기화가 정상 동작한다
- [ ] `ObjectPool.Return()` → `Get()` 사이클 후 재사용된 인스턴스에 이전 콜백이 남아있지 않다

## Test Plan

EditMode 단위 테스트 (`Assets/Tests/EditMode/ProjectilePoolTests.cs`):

1. **`NonPiercing_Hit_ArrivedFlagSetBeforeCallback`**
   - non-piercing `Projectile`의 `OnTriggerEnter2D` 등가 로직을 직접 호출
   - `OnHitMonster` 콜백 내부에서 `_arrived` 필드값을 읽어 `true`인지 단언

2. **`NonPiercing_DoublePhysicsCallback_CallbackInvokedOnce`**
   - `OnTriggerEnter2D`를 같은 몬스터에 대해 연속 두 번 호출
   - `OnHitMonster` 호출 횟수가 1회임을 단언

3. **`OnArrive_CapturedBeforePoolReturn`**
   - `OnArrive` 콜백 내에서 `proj` 인스턴스가 아직 풀에 반환되지 않은 상태임을 단언
   - (콜백 내에서 `gameObject.activeSelf == true`인지 확인)

4. **`ReturnToPool_ClearsAllCallbacks`**
   - `Projectile.Get()` → `Init()` → `ReturnToPool()` 순서 실행
   - 반환 후 `OnHitMonster`와 `OnArrive`가 모두 `null`임을 단언

5. **`Reinit_AfterPoolReturn_NoStaleCallbacks`**
   - 풀에서 꺼내 콜백 설정 → 반환 → 다시 꺼내 `Init()` → 콜백이 null임을 단언
