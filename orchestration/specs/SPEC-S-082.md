# SPEC-S-082: UIManager Panel Duplicate Open Prevention

## Summary
Prevent multiple exclusive UI panels from being open simultaneously.
When opening one panel, auto-close others to avoid input conflicts and visual overlap.

## Current State
- `UIManager.cs` manages 10+ panels via SerializeField references (no instantiation risk).
- Each panel has independent `Toggle()` → `IsOpen` check.
- **Problem:** Player can open Inventory (I) + SkillTree (K) + Quest (J) simultaneously.
- HideAll() exists but only triggers on Escape; no mutual exclusion on individual Toggle.

## Requirements

### R1: Mutual Exclusion for Exclusive Panels
- When opening any of: Inventory, Shop, Crafting, Enhance, SkillTree, Quest → close all other exclusive panels first.
- **Non-exclusive (always visible):** HUD, Dialogue (has own _dialogueOpen gate).
- PauseMenu is exclusive by nature (blocks Update).

### R2: Implementation Approach
1. In `UIManager.Update()`, before each Toggle call, invoke `CloseExclusivePanels()` (skipping the one about to open).
2. Or: Add a `ShowExclusive(panel)` helper that hides others then shows the target.

### R3: External Callers (NPC interactions)
- `Shop.Open()`, `Crafting.Open()`, `Enhance.Open()` called from `GameUIWiring` / NPC interaction.
- These should also route through the mutual exclusion gate.

## Entry Point
- Keyboard shortcuts in `UIManager.Update()` (I/K/J keys).
- NPC interaction → `GameUIWiring` → `Shop.Open()` / `Crafting.Open()` etc.

## Data Structure
- No new data. Uses existing `IsOpen` booleans on each panel.

## Save Integration
- None. UI state is transient.

## Acceptance Criteria
- [ ] Only one exclusive panel open at a time.
- [ ] Opening a new panel auto-closes the previous one.
- [ ] External callers (NPC shop/craft) also respect exclusion.
- [ ] Escape still closes all panels as before.
