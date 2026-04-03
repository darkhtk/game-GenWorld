# SPEC-S-064: DialogueUI Coroutine Reentry Prevention

> **Priority:** P2 | **Tag:** Bug Fix | **Status:** Draft

## Problem

`DialogueUI.ShowLoading(true)` (line 215) overwrites `_loadingCoroutine` reference without stopping the previous coroutine. If called twice before `ShowLoading(false)`, the first coroutine becomes orphaned and runs indefinitely.

Additionally, `Show()` (line 90) does not call `Hide()` first, so rapid NPC interactions can leak UI state.

## Root Cause

- `ShowLoading(true)` assigns `_loadingCoroutine = StartCoroutine(...)` without checking/stopping the existing one.
- `DialogueController.TryInteract()` has an `_inDialogue` guard (line 80) but edge cases (e.g., dialogue auto-close + immediate re-open) can bypass it.

## Scope

### Files to Modify
- `Assets/Scripts/UI/DialogueUI.cs`

### Changes

1. **ShowLoading guard** — Before `StartCoroutine(LoadingAnimation())`, check if `_loadingCoroutine != null` and `StopCoroutine(_loadingCoroutine)` first.
2. **Show() cleanup** — At the top of `Show()`, call `Hide()` if the panel is already active to reset state cleanly.
3. **Unit test** — Verify that calling `ShowLoading(true)` twice does not leak coroutines.

## Data Structure

No data changes.

## UI/UX Impact

None visible — purely defensive fix.

## Integration Points

- `DialogueController.HandleDialogueResponse()` calls `ShowLoading(true/false)`
- `DialogueController.TryInteract()` calls `Show()`

## Validation Checklist

- [ ] `ShowLoading(true)` called twice in succession: first coroutine is stopped
- [ ] `Show()` called while already visible: previous state cleaned up
- [ ] Normal dialogue flow unchanged (single NPC interaction, AI response, close)
- [ ] No orphaned `LoadingAnimation` coroutines after dialogue ends
