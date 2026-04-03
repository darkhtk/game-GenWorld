# SPEC-S-010: QuestSystem Null Defense for Rewards

> **Priority:** P2
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## Problem

`QuestSystem` accesses quest reward data without null checks in two locations:

1. **`CompleteQuest()`** (line 68) returns `def.rewards` directly — if a QuestDef has `rewards: null`, the caller receives null and may dereference it.
2. **`GetScaledRewards()`** (line 146) accesses `quest.rewards.items` without checking if `quest.rewards` or `.items` is null.

If any quest definition in JSON omits the `rewards` field or has `rewards.items` as null, this causes a `NullReferenceException` at runtime.

## Current Flow

```csharp
// CompleteQuest() — line 68
return def.rewards;  // can be null

// GetScaledRewards() — line 146
items = quest.rewards.items  // NullRefException if rewards or items is null
```

## Required Fix

### In CompleteQuest()
```csharp
// Return empty reward instead of null
return def.rewards ?? new QuestReward();
```

### In GetScaledRewards()
```csharp
var items = quest.rewards?.items;
if (items == null || items.Count == 0)
    return new ScaledRewards();  // or empty list
```

### Data Validation (Optional)
Add a validation pass in `DataManager` or `QuestSystem.Init()` that warns about quest definitions with null rewards.

## Entry Point

- `Assets/Scripts/Systems/QuestSystem.cs` — `CompleteQuest()` return (line 68)
- `Assets/Scripts/Systems/QuestSystem.cs` — `GetScaledRewards()` (line 146)
- Quest JSON data: `Assets/StreamingAssets/Data/quests.json`

## Acceptance Criteria

1. `CompleteQuest()` never returns null
2. `GetScaledRewards()` never throws NullReferenceException
3. Quest with missing/null rewards completes gracefully (gives no items, logs warning)
4. Existing tests pass

## Data / Save Impact

None — defensive null checks only. No save format change.

## Test

- Unit test: CompleteQuest with quest that has null rewards — verify no exception, returns empty reward
- Unit test: GetScaledRewards with null items array — verify returns empty, no crash
