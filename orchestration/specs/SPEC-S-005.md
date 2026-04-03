# SPEC-S-005: InventorySystem OccupiedSlots LINQ Allocation Removal

> **Priority:** P2
> **Tag:** Performance (Stabilize)
> **Depends on:** None

## Problem

`InventorySystem.OccupiedSlots` property uses LINQ `Count()`:

```csharp
// Assets/Scripts/Systems/InventorySystem.cs:8
public int OccupiedSlots => Slots.Count(s => s != null);
```

`Count()` with a predicate on an array allocates an `IEnumerator` + closure on each call. If `OccupiedSlots` is accessed in hot paths (UI update, item add/remove validation), this generates GC pressure every frame.

## Required Fix

Replace LINQ with a manual count loop or, preferably, a cached counter:

### Option A: Manual count (minimal change)
```csharp
public int OccupiedSlots
{
    get
    {
        int count = 0;
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != null) count++;
        return count;
    }
}
```

### Option B: Cached counter (better perf if called frequently)
Maintain a `_occupiedCount` field, increment on Add, decrement on Remove. Property becomes a simple field read.

## Entry Point

- `Assets/Scripts/Systems/InventorySystem.cs` — `OccupiedSlots` property (line 8)
- Callers: search for `OccupiedSlots` usage across UI and system code

## Acceptance Criteria

1. No LINQ `Count()` call in `OccupiedSlots`
2. Return value identical to previous behavior
3. Zero allocation per call
4. Existing tests pass (if any)

## Data / Save Impact

None.

## Test

- Unit test: Add N items, verify OccupiedSlots == N. Remove M items, verify OccupiedSlots == N-M.
