# SPEC-S-004: DoT Death Kill Reward / Removal

> **Priority:** P1
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None (S-002 independent)

## Problem

When a monster dies from DoT (poison, burn, etc.), `MonsterController.UpdateAI()` line ~105 only plays the "die" animation and returns early. The normal kill pipeline (`CombatManager` -> `OnMonsterKilled` callback -> `GameManager.OnMonsterKilled()`) is never invoked. This causes:

1. **No XP/gold reward** for the player
2. **No loot drop**
3. **No kill count update** (quests, achievements, statistics)
4. **No death VFX** (`vfx_monster_death`)
5. **Monster not removed from spawner** (`MonsterSpawner.RemoveMonster` never called) — stale reference remains in `_monsters` list
6. **No EventBus events** (`monster_killed`, achievement triggers)

## Current Flow (Normal Combat Kill)

```
CombatManager.ProcessCombat()
  -> monster.TakeDamage(dmg)  (returns true if killed)
  -> _onMonsterKilled(monster)  (callback)
  -> GameManager.OnMonsterKilled(monster)
     -> Death VFX
     -> XP, gold, loot
     -> Kill count, quests, achievements
     -> monsterSpawner.RemoveMonster(monster)
```

## Current Flow (DoT Kill — Broken)

```
MonsterController.UpdateAI()
  -> Effects.TickDot(now)
  -> Hp -= dotDmg
  -> if Hp <= 0: PlayAnimation("die"); return;
  // END — no reward, no removal, no events
```

## Required Fix

Connect DoT death to the same kill pipeline. Two options:

### Option A: Fire EventBus event (Preferred)
In `MonsterController.UpdateAI()`, after DoT kills monster:
```csharp
if (Hp <= 0)
{
    PlayAnimation("die");
    EventBus.Emit("monster_killed_by_dot", new { monster = this });
    return;
}
```
`CombatManager` or `GameManager` listens for this event and calls `OnMonsterKilled(monster)`.

### Option B: Direct callback
Store a kill callback reference on `MonsterController` (set during `CombatManager.Init`), call it on DoT death.

## Entry Point

- `Assets/Scripts/Entities/MonsterController.cs` — `UpdateAI()` DoT section (~line 99-106)
- `Assets/Scripts/Core/GameManager.cs` — `OnMonsterKilled()` (~line 764)
- `Assets/Scripts/Entities/MonsterSpawner.cs` — `RemoveMonster()` (~line 113)

## Acceptance Criteria

1. DoT kill triggers full reward pipeline (XP, gold, loot, kill count)
2. Monster is removed from spawner list
3. Death VFX plays at monster position
4. Quest/achievement kill tracking fires
5. No duplicate kill processing if normal hit and DoT coincide in same frame

## Data / Save Impact

None — kill rewards use existing systems. No save format change.

## Test

- Unit test: MonsterController DoT reduces HP to 0 -> kill callback invoked
- Integration: poison a monster, wait for DoT death, verify XP gain
