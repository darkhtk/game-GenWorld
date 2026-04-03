# SPEC-S-091: UIManager Scene Transition Panel Cleanup

## Summary
Close all open UI panels when a scene transition occurs, preventing stale panel state
from leaking across scenes and avoiding visual glitches on re-entry.

## Current State
- `UIManager.HideAll()` exists and correctly closes all 10 managed panels.
- **Problem:** `HideAll()` is only called on Escape key press — never on scene transitions.
- `SceneManager.LoadScene("MainMenuScene")` in `GameUIWiring.cs:198` does not trigger panel cleanup.
- `SceneManager.LoadScene("GameScene")` in `MainMenuController.cs:62/69` similarly has no cleanup.
- Open panels retain references to GameManager, player data, inventory systems post-transition.

## Requirements

### R1: HideAll on Scene Exit
- Before `SceneManager.LoadScene()` is called, invoke `UIManager.HideAll()`.
- Two call sites:
  1. `GameUIWiring.cs:198` — Game → Main Menu transition
  2. Any future scene load paths

### R2: Defensive sceneLoaded Hook
- Subscribe `UIManager` to `SceneManager.sceneLoaded` as a safety net.
- On scene load, call `HideAll()` if any panel is open (handles edge cases where direct
  call was missed).
- Unsubscribe in `OnDestroy` to prevent leaks.

### R3: Dialogue State Reset
- Ensure `_dialogueOpen` flag is reset to `false` on scene transition.
- Already handled by `HideAll()` — verify it remains correct.

## Entry Point
- **Primary:** `GameUIWiring.cs` where `OnMainMenuRequested` lambda calls `SceneManager.LoadScene`.
- **Safety net:** `UIManager.Awake/OnEnable` subscribing to `SceneManager.sceneLoaded`.

## Data Structure
- No new data structures needed. Reuses existing `HideAll()` method.

## Save Integration
- Not directly related. AutoSave blocking (S-054/S-087) is independent.

## Acceptance Criteria
- [ ] All panels closed before scene transition begins.
- [ ] `_dialogueOpen` reset to `false`.
- [ ] No stale panel references after loading a new scene.
- [ ] `sceneLoaded` subscription cleaned up in `OnDestroy`.
- [ ] Escape key behavior unchanged (still calls `HideAll()`).
