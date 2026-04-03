# SPEC-S-048: SkillSystem Data Integrity

> **Priority:** P2
> **Category:** Stabilize / Defensive Loading
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

skills.json loading lacks field-level validation. Missing or wrong-type fields can cause silent fallback to defaults or null reference exceptions at runtime.

## Current State

### Loading Path
1. `DataManager.LoadJson<SkillsData>()` (DataManager.cs:54-59)
2. Newtonsoft.Json deserializes into `SkillDef` structs
3. `SkillSystem` constructor builds `Dictionary<string, SkillDef>` from `SkillsData.skills`

### Existing Defenses
- `DataManager.ValidateData()` patches Items/Monsters but **does NOT validate Skills**
- `SkillDef.scaling` uses null coalescing (`?? DefaultScaling`) — silent fallback
- `_defs.TryGetValue()` for safe lookup

### Known Gaps
1. **id field null/empty** — dictionary key becomes null, subsequent lookups fail silently
2. **No type validation** — `damage: "abc"` silently converts to 0f or throws
3. **Missing required fields** — `mpCost`, `cooldown`, `damage` have no default enforcement
4. **ValidateData() skips skills** — Items/Monsters get patched, skills don't

## Required Changes

### 1. Add ValidateSkills() in DataManager
- After loading `SkillsData`, iterate all entries
- Skip entries where `id` is null or empty (log warning)
- Ensure numeric fields have sensible defaults:
  - `mpCost` default: 0
  - `cooldown` default: 1000 (ms)
  - `damage` default: 1f
  - `requiredLevel` default: 1
  - `requiredPoints` default: 1
- Log patched fields at Warning level

### 2. Call ValidateSkills() from ValidateData()
- Insert after existing Items/Monsters validation block
- Pattern: same as ValidateMonsters()

## Affected Files
- `Assets/Scripts/Data/DataManager.cs` — add ValidateSkills()
- `Assets/Scripts/Data/SkillDef.cs` — (read-only reference for field names)

## Entry Point
- `GameManager.Start()` → `DataManager.Load()` → `ValidateData()`

## Test Plan
- Unit test: Load skills.json with missing `id` field — expect skip + warning log
- Unit test: Load skills.json with `damage: null` — expect patched to 1f
- Unit test: Load valid skills.json — expect no warnings

## Data Structure Reference

```json
{
  "skills": [{
    "id": "string (REQUIRED)",
    "name": "string",
    "tree": "melee|ranged|magic",
    "requiredPoints": "int (default 1)",
    "requiredLevel": "int (default 1)",
    "mpCost": "int (default 0)",
    "cooldown": "float (default 1000)",
    "damage": "float (default 1)",
    "range": "float",
    "aoe": "float",
    "scaling": { "damage": float, "aoe": float, "duration": float, "buff": float }
  }]
}
```

## Save Integration
- None (read-only data)
