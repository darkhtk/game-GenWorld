# SPEC-S-075: MonsterController 사망 상태 피격 방지

> **우선순위:** P2 (Stabilize)
> **파일:** `Assets/Scripts/Entities/MonsterController.cs`

---

## 요약

`MonsterController.TakeDamage()`가 `IsDead` 상태에서 호출될 때 조기 반환하여, 사망한 몬스터에게 추가 피해/이펙트가 적용되지 않도록 방어 코드를 확인 및 보강한다.

## 현재 상태

### TakeDamage 가드 (L203-213)

```csharp
public bool TakeDamage(int dmg)
{
    if (IsDead || DeathProcessed) return false;   // L205 — 가드 존재
    if (IsReturning) dmg = Mathf.Max(1, dmg / (int)ReturnDamageReduction);
    Hp -= dmg;
    if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
    FlashWhite();
    if (Hp <= 0) { PlayAnimation("die"); return true; }
    PlayAnimation("hit");
    return false;
}
```

**L205에 `IsDead || DeathProcessed` 가드가 이미 존재한다.** 기본적인 방어는 되어 있으나, 아래 잠재적 문제가 남아 있다.

### 문제 1: DoT 데미지가 TakeDamage를 우회 (L99-107)

```csharp
// UpdateAI 내부 (L99-107)
float dotDmg = Effects.TickDot(nowMs);
if (dotDmg > 0)
{
    Hp -= Mathf.RoundToInt(dotDmg);                    // TakeDamage 미경유, 직접 차감
    if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
    if (Hp <= 0) { PlayAnimation("die"); return; }
}
```

- DoT으로 `Hp`가 0 이하가 되면 `PlayAnimation("die")` 후 `return`하지만, `DeathProcessed`는 설정되지 않는다.
- `DeathProcessed`는 `GameManager` 루프(L147-151)에서 다음 프레임에 설정되므로, **같은 프레임 내에서** 먼저 DoT이 Hp를 0으로 만들고 이후 `TakeDamage`가 호출되면, `IsDead` 가드(`Hp <= 0`)가 잡아주긴 한다.
- 그러나 DoT으로 사망 시 `_onMonsterDeath` 콜백이 호출되지 않아 보상 누락 가능성이 있다 (이것은 별도 태스크 범위).

### 문제 2: 사망 직후 같은 프레임 다중 히트

`PerformAutoAttack`(L94-118)에서 몬스터 리스트를 역순 순회하며, 각 몬스터에 대해 `m.IsDead` 체크(L97) 후 `TakeDamage`를 호출한다. **단일 몬스터에 1회만 호출**되므로 동일 프레임 melee 중복은 없다.

`DealDamageToMonster`(L358-380)에서도 `m.IsDead` 체크(L360)가 호출 전에 있다.

`Projectile.OnTriggerEnter2D`(L136-154)에서도 `monster.IsDead` 체크(L141)가 있다.

**결론: 호출부 3곳 모두 호출 전 가드가 있고, TakeDamage 자체에도 가드가 있어 이중 방어 상태이다.**

## 수정 방안

현재 코드는 기본 방어가 되어 있으나, stabilize 단계에서 다음을 보강한다:

### 수정 1: TakeDamage 가드 로그 추가 (선택)

```csharp
public bool TakeDamage(int dmg)
{
    if (IsDead || DeathProcessed)
    {
        Debug.LogWarning($"[MonsterController] TakeDamage called on dead monster: {Def?.name}");
        return false;
    }
    // ... rest unchanged
}
```

> 디버그 빌드에서만 유의미. Release에서는 `#if UNITY_EDITOR` 또는 `[Conditional]`로 감싸거나 제거.

### 수정 2: DoT 사망 경로 정규화 (권장)

`UpdateAI` 내 DoT 데미지 처리를 `TakeDamage`를 경유하도록 변경:

```csharp
// Before (L102-107)
float dotDmg = Effects.TickDot(nowMs);
if (dotDmg > 0)
{
    Hp -= Mathf.RoundToInt(dotDmg);
    if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
    if (Hp <= 0) { PlayAnimation("die"); return; }
}

// After
float dotDmg = Effects.TickDot(nowMs);
if (dotDmg > 0)
{
    bool dead = TakeDamage(Mathf.RoundToInt(dotDmg));
    if (dead) return;
}
```

이렇게 하면:
- `IsDead` / `DeathProcessed` 가드를 자동 적용
- HP bar 갱신, FlashWhite, die/hit 애니메이션 모두 TakeDamage 내부에서 일관 처리
- 단, `IsReturning` 감쇠가 DoT에도 적용된다는 부작용이 있으므로, DoT용 오버로드를 추가하거나 `ignoreDmgReduction` 파라미터를 도입할 수 있다:

```csharp
public bool TakeDamage(int dmg, bool ignoreReduction = false)
{
    if (IsDead || DeathProcessed) return false;
    if (!ignoreReduction && IsReturning)
        dmg = Mathf.Max(1, dmg / (int)ReturnDamageReduction);
    Hp -= dmg;
    if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
    FlashWhite();
    if (Hp <= 0) { PlayAnimation("die"); return true; }
    PlayAnimation("hit");
    return false;
}
```

DoT 호출: `TakeDamage(Mathf.RoundToInt(dotDmg), ignoreReduction: true)`

## 연동 경로

| 호출부 | 파일:줄 | 호출 전 IsDead 가드 |
|--------|---------|---------------------|
| `PerformAutoAttack` (melee) | `CombatManager.cs:97, 113` | O (L97) |
| `DealDamageToMonster` (skill) | `CombatManager.cs:360, 363` | O (L360) |
| `Projectile.OnTriggerEnter2D` | `Projectile.cs:141, 147` | O (L141) |
| `UpdateAI` DoT (직접 Hp 차감) | `MonsterController.cs:102-107` | X (TakeDamage 미경유) |

## 테스트 방안

### EditMode 단위 테스트 (`Assets/Tests/EditMode/`)

```csharp
[Test]
public void TakeDamage_WhenDead_ReturnsEarlyNoDamage()
{
    // Arrange: monster with Hp = 0
    var monster = CreateTestMonster(hp: 10);
    monster.Hp = 0; // force dead
    
    // Act
    bool result = monster.TakeDamage(5);
    
    // Assert
    Assert.IsFalse(result);
    Assert.AreEqual(0, monster.Hp); // Hp unchanged (not -5)
}

[Test]
public void TakeDamage_WhenDeathProcessed_ReturnsEarlyNoDamage()
{
    var monster = CreateTestMonster(hp: 10);
    monster.Hp = 0;
    monster.DeathProcessed = true;
    
    bool result = monster.TakeDamage(5);
    
    Assert.IsFalse(result);
    Assert.AreEqual(0, monster.Hp);
}

[Test]
public void TakeDamage_KillsMonster_ReturnsTrue()
{
    var monster = CreateTestMonster(hp: 10);
    
    bool result = monster.TakeDamage(15);
    
    Assert.IsTrue(result);
    Assert.IsTrue(monster.IsDead);
}

[Test]
public void TakeDamage_AfterKill_SecondCallBlocked()
{
    var monster = CreateTestMonster(hp: 10);
    monster.TakeDamage(15); // kills
    
    bool result = monster.TakeDamage(5); // should be blocked
    
    Assert.IsFalse(result);
}
```

### PlayMode 수동 테스트

1. 몬스터를 죽인 직후 같은 위치에서 연타 공격 -- 추가 데미지 숫자가 뜨지 않는지 확인
2. DoT(독 등) 상태에서 몬스터 사망 시 이후 DoT tick에서 추가 HP 감소가 없는지 확인
3. 관통(piercing) 투사체가 사망한 몬스터를 통과할 때 `OnHitMonster` 콜백이 발생하지 않는지 확인

## 세이브 연동

해당 없음. 몬스터 상태는 세이브에 포함되지 않음.
