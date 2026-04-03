# SPEC-S-050: InputSystem UI/Game Input Separation

> **Priority:** P2
> **Category:** Stabilize / Input Handling
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

Game input (movement, skills, NPC interaction) is NOT blocked when UI panels are open. Players can cast skills, attack, and interact with NPCs while inventory/shop/skillTree panels are displayed.

## Current State

### Input Architecture
- **No centralized InputManager** — input scattered across GameManager, CombatManager, UIManager
- `GameManager.Update()` checks `Input.GetKeyDown(KeyCode.F)` for NPC interaction (line ~152)
- `GameManager.HandleSkillInput()` checks `Input.GetKeyDown(KeyCode.Alpha1+i)` for skills (line ~180)
- `CombatManager.Update()` checks `Input.GetMouseButton(0)` for auto-attack (line ~45)
- `UIManager.Update()` checks I/K/J/Escape/Tab/R/T/H for UI toggles (lines 35-55)

### Existing (Partial) Blocking
- `UIManager._dialogueOpen` flag blocks **pause menu only**, not game input
- `PauseMenuUI.Toggle()` sets `Time.timeScale = 0f` — pauses physics but NOT input events
- `UIManager.IsAnyPanelOpen()` exists (line ~77) but is **never called** by GameManager/CombatManager

### Bugs (Reproducible)
1. Open inventory (I) → press skill key (1-4) → skill fires behind UI
2. Open shop → left-click Buy button → auto-attack also triggers
3. Open inventory → press F near NPC → NPC interaction triggers behind UI
4. Dialogue active → press skill key → skill fires during conversation

## Required Changes

### 1. Add UIManager.IsInputBlocked() method
```csharp
public bool IsInputBlocked()
{
    return _dialogueOpen || IsAnyPanelOpen();
}
```

### 2. Guard game input in GameManager.Update()
- At top of Update, check `UIManager.IsInputBlocked()` — early return for game input section
- UI toggle input (Escape, I, K, J) must still work (handled in UIManager.Update)

### 3. Guard auto-attack in CombatManager
- `PerformAutoAttack()` should check `UIManager.IsInputBlocked()` before processing click

### 4. Guard skill input in GameManager.HandleSkillInput()
- Check `UIManager.IsInputBlocked()` at method entry

## Affected Files
- `Assets/Scripts/UI/UIManager.cs` — add IsInputBlocked()
- `Assets/Scripts/Core/GameManager.cs` — guard NPC interaction + skill input
- `Assets/Scripts/Systems/CombatManager.cs` — guard auto-attack

## Entry Point
- Player presses any key/mouse button while UI panel is open
- Flow: GameManager.Update() → check UIManager.IsInputBlocked() → skip game input

## Test Plan
- Manual: Open inventory → try to cast skill → no effect
- Manual: Open shop → click inside shop → no auto-attack
- Manual: Close all panels → skills and attack work normally
- Unit test: IsInputBlocked() returns true when any panel open, false when all closed

## Save Integration
- None (input handling only)
