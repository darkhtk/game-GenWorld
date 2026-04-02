# SPEC-S-003: Scene Transition Safety

> **Priority:** Robustness
> **Entry point:** All scene transitions (BootScene → MainMenuScene → GameScene)

---

## Current State
- Scene transitions likely use `SceneManager.LoadScene()`
- Singletons (GameManager, AudioManager, etc.) may have Awake/Start ordering issues
- Null references possible during transition when objects aren't yet initialized

## Requirements

### 1. Null Reference Audit
- Search ALL MonoBehaviour scripts for:
  - `GameManager.Instance` access in `Awake()` — potential null if GM not yet initialized
  - `FindObjectOfType<>()` calls — fragile across scene loads
  - Static references cleared on scene reload
- Add null guards where needed

### 2. Initialization Order
- Ensure singletons initialize in correct order via Script Execution Order or `[DefaultExecutionOrder]`:
  1. GameManager (first)
  2. AudioManager
  3. UIManager / HUD
  4. Other systems
- Or use lazy initialization pattern (`Instance ??= this`)

### 3. Scene Load Safety
- Wrap `SceneManager.LoadScene()` calls:
  - Disable input during transition
  - Show loading screen (R-033 already implemented)
  - Clean up EventBus subscriptions from previous scene

### 4. DontDestroyOnLoad Check
- Verify all persistent objects use `DontDestroyOnLoad` correctly
- Prevent duplicate singletons on scene reload

## Files to Audit
- `Assets/Scripts/Core/GameManager.cs` — singleton + scene management
- All `*Manager.cs` files — singleton patterns
- All `*UI.cs` files — references to managers in Awake/Start

## Verification
- Rapid scene switching (MainMenu ↔ Game ↔ MainMenu) — no null reference errors
- Fresh game start after returning to main menu — all systems reinitialize correctly
- Check Unity console for null reference warnings during transitions
