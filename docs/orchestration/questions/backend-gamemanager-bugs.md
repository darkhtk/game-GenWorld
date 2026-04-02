# Report from Dev-Backend

## Date: 2026-04-02
## To: Director
## Status: RESOLVED

## Bug 1: AIManager.Init() never called
GameManager.cs:46 creates `AI = new AIManager()` but never calls `await AI.Init()`.
This means `AI.AiEnabled` is always false and all dialogue uses the offline fallback.
Ollama warm-up never happens.

**Fix:** Add `_ = AI.Init();` (fire-and-forget) or make Start async and await it:
```csharp
// Option A: fire-and-forget (simplest)
AI = new AIManager();
_ = AI.Init();

// Option B: async Start
async void Start() { ... await AI.Init(); ... }
```

## Bug 2: LevelUpEvent handler runs before PlayerState.Level is updated
In OnMonsterKilled (line 210+):
1. StatsSystem.AddXp increments `state.level` locally and emits LevelUpEvent
2. LevelUpEvent handler (line 161) calls `RecalcStats()` and `FullHeal()` — but `PlayerState.Level` is still the OLD level at this point
3. After AddXp returns, `PlayerState.Level = state.level` finally sets the new level

Result: RecalcStats and FullHeal use the old level's stats. The player briefly has correct HP for the old level, then the HUD shows new level but old maxHp.

**Fix:** Move the Level/Xp assignment BEFORE the event emit, or move RecalcStats/FullHeal out of the event handler and into OnMonsterKilled after the assignment.

## Bug 3 (Minor): Inventory restoration stacks items
LoadGame (line 294) uses `Inventory.AddItem()` in a loop, which stacks items. If the saved inventory had items spread across non-adjacent slots, the restored layout won't match the original. Consider direct slot assignment instead:
```csharp
for (int i = 0; i < save.inventory.Length && i < Inventory.MaxSlots; i++)
    Inventory.Slots[i] = save.inventory[i];
```
