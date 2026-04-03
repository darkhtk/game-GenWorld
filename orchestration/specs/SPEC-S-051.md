# SPEC-S-051: SceneTransition Memory Leak Check

> **Priority:** P2
> **Category:** Stabilize / Memory Management
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

Scene transitions (game ↔ main menu) may leak memory due to missing cleanup of pooled objects, event subscriptions, and data references.

## Current State

### Scene Loading
- `GameUIWiring` line ~197: `SceneManager.LoadScene("MainMenuScene")` — direct load, no cleanup
- `LoadingScreenUI` (43 lines): fade animation wrapper, no resource management

### Cleanup on Destroy
- `GameManager.OnDestroy()` calls `EventBus.Clear()` only
- No explicit pool cleanup, no system disposal, no asset unload

### ObjectPool (ObjectPool.cs, 65 lines)
- Queue-based generic pool
- `SetActive(false)` on return — objects stay in hierarchy
- No `Clear()` or `DisposeAll()` method
- No max size limit (related to S-049)

### Potential Leak Sources
1. **ObjectPool objects persist** — pooled GameObjects (DamageText, Projectile) in hierarchy not destroyed on scene unload
2. **EventBus.Clear() timing** — if new scene loads before old OnDestroy, new subscribers also cleared
3. **GameUIWiring callbacks** — lambda references to systems hold GC roots until scene fully unloads
4. **DataManager** — loaded JSON data (`File.ReadAllText`) stays in memory; no explicit release

## Required Changes

### 1. Verify Unity default cleanup is sufficient
- Unity auto-destroys all GameObjects on scene unload (non-DontDestroyOnLoad)
- **Check**: Are any game objects marked DontDestroyOnLoad inappropriately?
- **Check**: Does ObjectPool parent transform get destroyed with scene?

### 2. Add ObjectPool.Clear() method
```csharp
public void Clear()
{
    while (_available.Count > 0)
    {
        var obj = _available.Dequeue();
        if (obj != null) Object.Destroy((obj as MonoBehaviour)?.gameObject);
    }
}
```

### 3. Add GameManager cleanup before scene load
- Before `SceneManager.LoadScene()`, call pool Clear() for each pool
- Unsubscribe all event handlers (or rely on EventBus.Clear())
- Call `Resources.UnloadUnusedAssets()` after scene load

### 4. Verify EventBus.Clear() ordering
- Ensure EventBus.Clear() runs BEFORE new scene subscribes
- Current pattern: OnDestroy (old) → Awake/Start (new) — timing may interleave

## Affected Files
- `Assets/Scripts/Core/ObjectPool.cs` — add Clear() method
- `Assets/Scripts/Core/GameManager.cs` — add pre-scene-load cleanup
- `Assets/Scripts/Core/GameUIWiring.cs` — verify no retained references

## Entry Point
- Player clicks "Return to Main Menu" → GameUIWiring → SceneManager.LoadScene()
- Player dies → DeathScreenUI → respawn/menu transition

## Investigation Steps
1. Search for `DontDestroyOnLoad` in codebase — verify only appropriate objects persist
2. Profile memory before/after 5 scene transitions — check for growth
3. Check if DataManager singleton persists across scenes

## Test Plan
- Manual: Load game → return to menu → load game again 10 times → check memory
- Code review: Verify no DontDestroyOnLoad on pooled objects
- Unit test: ObjectPool.Clear() destroys all pooled instances

## Save Integration
- SaveSystem should NOT auto-save during scene transition cleanup (see S-054)
