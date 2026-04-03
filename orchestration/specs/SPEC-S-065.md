# SPEC-S-065: EffectHolder DoT Reapplication Verification

> **Priority:** P2 | **Tag:** Bug Fix | **Status:** Draft

## Problem

`EffectSystem.ApplyDot()` (line 55-64) unconditionally overwrites the existing DoT `ActiveEffect` when the same DoT is reapplied. Unlike `Apply()` for stun/slow (which extends duration), DoT has no reapplication logic — it silently resets the tick timer and damage, potentially losing accumulated state.

## Root Cause

- `_effects["dot"] = new ActiveEffect {...}` (line 58) replaces the entire object without checking for an existing entry.
- `stun` and `slow` have explicit duration-extension logic; `dot` was not given the same treatment.

## Scope

### Files to Modify
- `Assets/Scripts/Systems/EffectSystem.cs`

### Changes

1. **DoT reapplication policy** — When a DoT is reapplied:
   - If existing DoT has the same damage value: extend `expiresAt` to the later of (current, new).
   - If new DoT has higher damage: overwrite damage and extend duration.
   - If new DoT has lower damage: keep current damage, extend duration (stronger effect wins).
   - Preserve `_nextTick` so the tick rhythm is not reset mid-cycle.
2. **Unit test** — Verify:
   - Same DoT reapplied extends duration, keeps damage.
   - Stronger DoT overwrites damage.
   - Weaker DoT extends duration only.
   - `_nextTick` is not reset on reapplication.

## Data Structure

No schema changes. `ActiveEffect` struct used as-is.

## UI/UX Impact

Players will see consistent DoT behavior — reapplying the same DoT refreshes duration instead of resetting tick timing.

## Integration Points

- `ActionRunner.HandleApplyEffect()` (line 192-193) triggers `ApplyDot()`
- Skills with `effect: "dot"` in `skills.json`

## Validation Checklist

- [ ] DoT reapplied with same damage → duration extended, tick timing preserved
- [ ] DoT reapplied with higher damage → damage upgraded, duration extended
- [ ] DoT reapplied with lower damage → original damage kept, duration extended
- [ ] Single DoT application behavior unchanged
- [ ] No regression in stun/slow reapplication logic
