# SPEC-S-043: CombatManager 동시 사망 보상 누락 방지

> **Priority:** P1
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## 목적

광역 공격(AoE 스킬, auto-attack arc)으로 같은 프레임에서 여러 몬스터가 동시에 사망할 때, 경험치(XP)와 골드 보상이 정확하게 누적되는지 검증한다. `OnMonsterKilled()` 콜백이 반복 호출될 때 `PlayerLevelState` 중간 값이 유실되거나 레벨업 판정이 누락되는 경합 조건을 방지한다.

## 현재 상태

### CombatManager: 동시 사망 수집 경로

**경로 1: PerformAutoAttack() (auto-attack)**

```csharp
// CombatManager.cs line 60-88
List<MonsterController> killed = null;
for (int i = monsters.Count - 1; i >= 0; i--)
{
    // ... damage calculation ...
    bool dead = m.TakeDamage(dmg);
    if (dead) { killed ??= new(); killed.Add(m); }
}
if (killed != null)
    foreach (var m in killed) _onMonsterDeath?.Invoke(m);
```

사망 몬스터를 `killed` 리스트에 모은 후 순차적으로 `_onMonsterDeath` 콜백을 호출한다. 콜백은 `GameManager.OnMonsterKilled()`이다.

**경로 2: ExecuteSkill() (skill attack)**

```csharp
// CombatManager.cs line 226-244
_pendingKills ??= new List<MonsterController>();
_pendingKills.Clear();
// ... skill execution (DealDamageToMonster adds to _pendingKills) ...
for (int i = 0; i < _pendingKills.Count; i++)
    _onMonsterDeath?.Invoke(_pendingKills[i]);
_pendingKills.Clear();
```

`_pendingKills` 리스트에 수집 후 순차 콜백. 동일 패턴.

**경로 3: DealDamageToMonster() (skill internal)**

```csharp
// CombatManager.cs line 325-348
bool dead = m.TakeDamage(dmg);
if (dead)
{
    if (_pendingKills != null)
        _pendingKills.Add(m);
    else
        _onMonsterDeath?.Invoke(m);  // <-- fallback: immediate invoke
}
```

`_pendingKills`가 null이면 즉시 콜백을 호출하는 fallback 경로가 있다. 이 경로는 `ExecuteSkill()` 외부에서 `DealDamageToMonster`가 호출될 경우에 활성화될 수 있다.

**경로 4: GameManager.Update() DoT deaths**

```csharp
// GameManager.cs line 129-137
for (int i = monsters.Count - 1; i >= 0; i--)
{
    var m = monsters[i];
    if (m != null && m.IsDead && !m.DeathProcessed)
    {
        m.DeathProcessed = true;
        OnMonsterKilled(m);
    }
}
```

DoT으로 죽은 몬스터를 프레임 시작 시 처리. 여러 DoT 사망이 같은 프레임에 발생하면 루프 내에서 순차 처리된다.

### GameManager.OnMonsterKilled() 보상 로직

```csharp
// GameManager.cs line 782-861
void OnMonsterKilled(MonsterController monster)
{
    // ... VFX ...
    var state = new PlayerLevelState
    {
        level = PlayerState.Level, xp = PlayerState.Xp,
        skillPoints = PlayerState.SkillPoints, statPoints = PlayerState.StatPoints
    };
    StatsSystem.AddXp(ref state, def.xp);
    PlayerState.Level = state.level;
    PlayerState.Xp = state.xp;
    PlayerState.SkillPoints = state.skillPoints;
    PlayerState.StatPoints = state.statPoints;

    // Level up handling...
    PlayerState.Gold += def.gold;
    // ... drops ...
}
```

**핵심 분석:** 매 호출마다 `PlayerState`에서 현재 값을 읽어 `PlayerLevelState`를 새로 생성하고, 결과를 다시 `PlayerState`에 반영한다. 이 패턴은 순차적 호출에서는 올바르다 -- 두 번째 호출 시 첫 번째의 결과가 이미 `PlayerState`에 반영되어 있기 때문이다.

**잠재적 문제:** 레벨업 판정 시 `RecalcStats()` + `FullHeal()` + `player.SetSpeed()`가 실행된다. 연속 킬에서 두 번 레벨업이 가능한 경우 (XP가 충분히 높은 몬스터 2마리 동시 사살), 첫 번째 레벨업의 `RecalcStats()`가 아직 HUD 업데이트 중인데 두 번째가 실행되면 HUD가 중간 상태를 표시할 수 있다. 그러나 이는 동일 프레임 내 동기 실행이므로 렌더링 전에 모두 완료된다.

## 검증 항목

- [ ] `PerformAutoAttack()`에서 2마리 이상 동시 사살 시 모든 XP가 누적되는지 확인
- [ ] `ExecuteSkill()` AoE로 3마리 이상 동시 사살 시 모든 gold가 누적되는지 확인
- [ ] 동시 사살로 2회 이상 레벨업이 필요한 경우 모든 레벨업이 처리되는지 확인
- [ ] `_pendingKills`가 null인 상태에서 `DealDamageToMonster()`가 호출되면 즉시 콜백이 실행되는지 확인
- [ ] DoT 사망 + auto-attack 사망이 같은 프레임에 혼합될 때 `DeathProcessed` 플래그로 중복 보상이 방지되는지 확인
- [ ] `monsterSpawner.RemoveMonster(monster)` 호출 후 동일 몬스터에 대한 재처리가 없는지 확인
- [ ] 동시 사살 시 모든 loot drop이 정상적으로 inventory에 추가되는지 확인
- [ ] `killed` 리스트(PerformAutoAttack)와 `_pendingKills` 리스트(ExecuteSkill)가 프레임 간 잔류하지 않는지 확인

## 수정 방안 (필요 시)

### 1. DeathProcessed 이중 체크 강화

`OnMonsterKilled()` 진입 시 `DeathProcessed` 확인:

```csharp
void OnMonsterKilled(MonsterController monster)
{
    if (monster == null) return;
    if (monster.DeathProcessed)
    {
        Debug.LogWarning($"[GameManager] Duplicate death callback for {monster.Def?.id}, skipping");
        return;
    }
    monster.DeathProcessed = true;
    // ... existing logic ...
}
```

현재 코드는 `DeathProcessed = true`를 설정하지만 진입 시 체크하지 않는다. `PerformAutoAttack()`에서 죽인 몬스터가 같은 프레임의 DoT 루프에서도 감지되면 중복 보상이 발생한다.

### 2. CombatManager killed 리스트에서 DeathProcessed 필터링

```csharp
// PerformAutoAttack - before callback loop
if (killed != null)
    foreach (var m in killed)
    {
        if (!m.DeathProcessed)
            _onMonsterDeath?.Invoke(m);
    }
```

### 3. DealDamageToMonster fallback 경로 제거 검토

`_pendingKills`가 null인 상태에서의 즉시 콜백은 `ExecuteSkill()` 외 호출자에 대한 fallback이다. 이 경로가 실제로 사용되는지 확인하고, 사용되지 않으면 방어 로그로 대체:

```csharp
if (dead)
{
    if (_pendingKills != null)
        _pendingKills.Add(m);
    else
    {
        Debug.LogWarning("[CombatManager] DealDamageToMonster: _pendingKills is null, invoking death directly");
        _onMonsterDeath?.Invoke(m);
    }
}
```

## 호출 진입점

| 트리거 | 경로 |
|--------|------|
| Auto-attack (LMB 홀드) | `CombatManager.PerformAutoAttack()` -> arc 범위 내 다중 적 타격 |
| Skill 사용 (1-5 키) | `CombatManager.ExecuteSkill()` -> AoE 스킬 -> `DealDamageToMonster()` |
| DoT 효과 | `MonsterController.UpdateAI()` -> HP <= 0 -> `GameManager.Update()` DoT loop |

## 연동 시스템

| 시스템 | 역할 |
|--------|------|
| `CombatManager` | 데미지 처리, 사망 감지, 콜백 호출 |
| `GameManager.OnMonsterKilled()` | XP/Gold/Loot 보상 지급, 레벨업 처리 |
| `StatsSystem.AddXp()` | XP 추가 및 레벨업 판정 (while 루프로 다중 레벨업 지원) |
| `LootSystem.RollDrops()` | 드롭 아이템 결정 |
| `InventorySystem.AddItem()` | 드롭 아이템 인벤토리 추가 |
| `MonsterSpawner.RemoveMonster()` | 사망 몬스터 제거 |
| `EventBus` | `MonsterKillEvent`, `GoldChangeEvent`, `LevelUpEvent` 전파 |
| `PlayerStats` | XP, Gold, Level 상태 저장 |
| `MonsterController` | `IsDead`, `DeathProcessed` 상태 관리 |

## 데이터 구조

### MonsterDef (JSON rewards)

```json
{
    "id": "slime_green",
    "xp": 15,
    "gold": 5,
    "drops": [
        { "itemId": "slime_gel", "chance": 0.3, "count": 1 }
    ]
}
```

### PlayerLevelState (transient struct)

```csharp
public struct PlayerLevelState
{
    public int level, xp, skillPoints, statPoints;
}
```

### StatsSystem.AddXp (level-up loop)

```csharp
public static void AddXp(ref PlayerLevelState state, int amount)
{
    state.xp += amount;
    while (state.xp >= GameConfig.XpForLevel(state.level))
    {
        state.xp -= GameConfig.XpForLevel(state.level);
        state.level++;
        // ...
    }
}
```

`while` 루프로 다중 레벨업 처리. 단일 호출 내에서는 안전하나, 연속 호출 간 `PlayerState` 동기화가 핵심.

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Core/GameManager.cs` | `OnMonsterKilled()` 진입 시 `DeathProcessed` 체크 추가 |
| `Assets/Scripts/Systems/CombatManager.cs` | `DealDamageToMonster()` fallback 경로에 방어 로그 추가 |

## Acceptance Criteria

1. AoE 공격으로 3마리 동시 사살 시 `XP = sum(monster.xp)` 정확히 누적
2. 동시 사살로 인한 Gold 합산이 정확함 (`Gold += def.gold` x N)
3. 동시 사살로 2회 레벨업 조건 충족 시 모든 레벨업 이벤트 발생
4. 동일 몬스터에 대한 `OnMonsterKilled()` 중복 호출 시 두 번째가 무시됨
5. DoT 사망과 직접 타격 사망이 같은 프레임에 혼합되어도 보상 정확
6. 모든 loot drop이 inventory에 추가 (overflow 시 로그만, 유실 없음)

## 테스트

### Edit Mode Unit Test

- `StatsSystem.AddXp()`를 연속 호출하여 XP 누적 및 다중 레벨업 검증
- `OnMonsterKilled()` 2회 연속 호출 시 Gold 합산 검증
- `DeathProcessed` 플래그가 중복 보상을 방지하는지 검증

### Play Mode / 수동 테스트

1. AoE 스킬로 슬라임 3마리 동시 사살 -> XP/Gold 로그 확인
2. 높은 XP 몬스터 2마리 동시 사살로 다중 레벨업 유도 -> 레벨 정확히 +2 확인
3. DoT 스킬 시전 후 auto-attack으로 다른 몬스터 사살 -> 같은 프레임 내 혼합 사망 -> 보상 정확 확인
