# SPEC-S-052: EventBus Handler Execution Order Stability

> **Priority:** P3
> **Category:** Stabilize / Event System
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

Verify that EventBus handler execution order is stable and predictable when multiple handlers subscribe to the same event type. Current implementation iterates in reverse order (LIFO), which could cause issues if handlers depend on execution sequence.

## Current State

### EventBus Architecture (`Assets/Scripts/Core/EventBus.cs`)
- Static class, `Dictionary<Type, List<Delegate>> _listeners`
- `On<T>()` appends handler to end of list (`list.Add(handler)`)
- `Off<T>()` removes handler via `list.Remove(handler)` (first match)
- `Emit<T>()` iterates **reverse** (i = list.Count - 1 to 0) — last subscribed fires first
- `Clear()` removes all handlers

### Potential Issues
1. **Reverse iteration order**: Later subscribers execute first. If handler A depends on state set by handler B, and B subscribed before A, A fires first — wrong order.
2. **Remove during iteration**: `Off<T>()` could be called from within a handler during `Emit<T>()`. Reverse iteration partially protects against this (removing higher-index items is safe), but removing a lower-index item shifts remaining indices.
3. **No priority system**: No way to specify handler priority if order matters.

### Known Event Types with Multiple Handlers
- `MonsterKilledEvent` — listened by CombatRewardHandler, QuestSystem, AchievementSystem
- `PlayerLevelUpEvent` — listened by HUD, AchievementSystem
- `ItemPickupEvent` — listened by InventorySystem, QuestSystem, HUD

## Required Verification

### 1. Audit multi-subscriber events
- List all event types with 2+ subscribers
- Verify no handler depends on another handler's side effects from the same event

### 2. Test remove-during-emit safety
- Subscribe handler A that unsubscribes handler B within the same Emit call
- Verify no crash or skipped handler

### 3. Document expected behavior
- If no order dependency found: document that order is LIFO and handlers must be independent
- If order dependency found: add priority parameter to `On<T>()` or switch to insertion-order (forward) iteration

## Affected Files
- `Assets/Scripts/Core/EventBus.cs` — potential iteration direction change or priority support

## Entry Point
- Any `EventBus.Emit<T>()` call with 2+ registered handlers

## Test Plan
- Unit test: Subscribe A, B, C → Emit → verify all three fire
- Unit test: Unsubscribe B during Emit → verify A and C still fire, no exception
- Code audit: confirm no handler depends on another handler's output from same event

## Save Integration
- None (event dispatch only)
