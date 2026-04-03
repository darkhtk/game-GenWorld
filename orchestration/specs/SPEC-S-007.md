# SPEC-S-007: CombatManager _cachedMonsters Stale Reference Defense

> **Priority:** P2
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## Problem

`CombatManager._cachedMonsters` (line 18) is a `List<MonsterController>` reference cached during `PerformAutoAttack()` (line 41) and `HandleMonsterAttacks()` (line 96). Later, `ExecuteSkill()` (line 221) reuses this cached list:

```csharp
var monsters = _cachedMonsters ?? new List<MonsterController>();
```

Between cache writes and reads, monsters can be killed, despawned, or destroyed. The null-coalescing check only protects against the list reference itself being null — it does **not** protect against destroyed/dead `MonsterController` entries inside the list. This causes:

1. **NullReferenceException** if a cached monster's GameObject was Destroyed
2. **Incorrect targeting** — skill hits a dead monster instead of skipping it
3. **Wasted computation** — iterating stale entries

## Current Flow

```
PerformAutoAttack() / HandleMonsterAttacks()
  -> _cachedMonsters = monsters;  // snapshot at call time
  ... (frames pass, monsters die/despawn) ...
ExecuteSkill()
  -> var monsters = _cachedMonsters;  // stale list
  -> foreach(m in monsters) { m.TakeDamage(...) }  // potential stale ref
```

## Required Fix

Filter out dead/destroyed entries before use in `ExecuteSkill()`:

```csharp
// Line ~221
var monsters = _cachedMonsters ?? new List<MonsterController>();
monsters.RemoveAll(m => m == null || m.IsDead);
```

Or better: don't cache at all — fetch a fresh list from the spawner each time, since `ExecuteSkill` is not called every frame:

```csharp
var monsters = MonsterSpawner.Instance?.GetActiveMonsters()
    ?? new List<MonsterController>();
```

## Entry Point

- `Assets/Scripts/Systems/CombatManager.cs` — `_cachedMonsters` field (line 18)
- `Assets/Scripts/Systems/CombatManager.cs` — `ExecuteSkill()` usage (line 221)
- `Assets/Scripts/Systems/CombatManager.cs` — `PerformAutoAttack()` cache write (line 41)
- `Assets/Scripts/Systems/CombatManager.cs` — `HandleMonsterAttacks()` cache write (line 96)

## Acceptance Criteria

1. No `NullReferenceException` when cached monster is destroyed between frames
2. Dead/despawned monsters are never targeted by skills
3. Existing combat tests pass
4. No performance regression (no per-frame allocation)

## Data / Save Impact

None.

## Test

- Unit test: Cache list with 3 monsters, destroy 1, call ExecuteSkill — verify only 2 hit
- Unit test: Cache list, despawn all monsters, call ExecuteSkill — verify no exception
