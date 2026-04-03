# SPEC-S-087: RegionManager Scene Transition Input Blocking

## Summary
Block PlayerController input during scene/region transitions to prevent movement commands
queuing during loading, which can cause teleport-like jumps on load completion.

## Current State
- `RegionTracker` tracks current region by player position (no scene reload).
- Scene transitions use `SceneManager.LoadScene` via `GameManager`.
- `UIManager.IsInputBlocked()` exists but only checks panels/dialogue.
- **Problem:** During async scene load or region fade, player can still move via `PlayerController.Update()`.

## Requirements

### R1: Transition Flag
- Add `bool IsTransitioning` to `GameManager` (or a dedicated `SceneTransitionController`).
- Set `true` before scene load starts, `false` after scene load completes.

### R2: Input Gate in PlayerController
- `PlayerController.Update()` should early-return when `GameManager.IsTransitioning` is true.
- Alternatively, wire through `UIManager.IsInputBlocked()` by adding transition check.

### R3: Visual Feedback (Optional)
- If a loading screen exists, it already blocks input visually.
- Ensure Rigidbody2D velocity is zeroed when transition starts.

## Entry Point
- `GameManager` scene load path (OnSceneLoadedCleanup already exists).
- Any portal/region-change trigger that initiates loading.

## Data Structure
- Single `bool _isTransitioning` field on GameManager.

## Save Integration
- AutoSave already has combat-check (S-054). Transition flag should also block auto-save.

## Acceptance Criteria
- [ ] Player cannot move during scene transitions.
- [ ] Rigidbody2D velocity zeroed at transition start.
- [ ] Input resumes after scene load completes.
- [ ] AutoSave skips during transitions.
