# SPEC-S-001: Save File Corruption Recovery

> **Priority:** Robustness
> **Entry point:** Automatic — SaveSystem.Load() at game start

---

## Current State
- `Assets/Scripts/Systems/SaveSystem.cs`
- Single save file: `Application.persistentDataPath/rpg_save.json`
- Backup: `.bak` created on migration or load failure
- No checksum verification — corrupted JSON silently fails

## Requirements

### 1. Checksum Generation (Save)
- In `SaveSystem.Save()`, after serializing JSON:
  - Compute SHA256 hash of the JSON string
  - Store hash in envelope: `envelope["checksum"] = hash`
- Auto-create backup of previous save before overwriting

### 2. Checksum Verification (Load)
- In `SaveSystem.Load()`, after reading file:
  - Extract `checksum` from envelope
  - Compute SHA256 of `data` field JSON
  - If mismatch → log warning + attempt backup restore

### 3. Backup Auto-Restore
- If checksum fails OR JSON parse fails:
  1. Check if `.bak` exists and is valid
  2. If valid → restore from backup, log warning to player
  3. If backup also corrupted → return null (new game)

### 4. Rolling Backups
- Keep last 3 backups: `.bak`, `.bak.1`, `.bak.2`
- Rotate on each save

## Files to Modify
- `Assets/Scripts/Systems/SaveSystem.cs` — checksum + rolling backups
- No UI changes needed (auto-recovery)

## Verification
- Corrupt save file manually → game restores from backup
- Delete backup → game starts fresh (no crash)
- Normal save/load cycle preserves checksum
