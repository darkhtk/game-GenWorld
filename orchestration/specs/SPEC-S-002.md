# SPEC-S-002: Multiple Save Slots

> **Priority:** Robustness
> **Entry point:** MainMenuScene → "Continue" / "New Game" buttons → Save Slot Selection Panel

---

## Current State
- Single save: `rpg_save.json` in `Application.persistentDataPath`
- No slot selection UI
- `SaveSystem.HasSave()` / `Save()` / `Load()` / `DeleteSave()` — all hardcoded to single path

## Requirements

### 1. SaveSystem Path Parameterization
- Add `static int ActiveSlot { get; set; }` (0, 1, 2)
- `SavePath` → `rpg_save_{ActiveSlot}.json`
- `BackupPath` → `rpg_save_{ActiveSlot}.json.bak`
- `HasSave(int slot)` overload
- `GetSlotInfo(int slot)` → returns `{ exists, timestamp, playerLevel, playTime }` for UI display

### 2. Save Slot Selection UI
- **File:** New `Assets/Scripts/UI/SaveSlotUI.cs`
- 3 slots displayed as cards:
  - Empty: "Empty Slot" + "New Game" button
  - Occupied: Player name, Level, Play time, Last saved date + "Load" / "Delete" buttons
- **Entry from MainMenuController:**
  - "Continue" → open SaveSlotUI → select slot → load
  - "New Game" → open SaveSlotUI → select empty slot → start

### 3. Steam Cloud
- `SteamCloudStorage.SaveToCloud()` already syncs — update filename to include slot index

## Files to Modify
- `Assets/Scripts/Systems/SaveSystem.cs` — slot parameterization
- `Assets/Scripts/UI/SaveSlotUI.cs` — new file
- `Assets/Scripts/UI/MainMenuController.cs` — wire slot selection
- `Assets/Scripts/Core/GameManager.cs` — `SaveGame()` / `LoadGame()` use `SaveSystem.ActiveSlot`

## Verification
- 3 independent save slots, each with correct data
- Delete slot → confirmation → slot cleared
- Steam cloud syncs all 3 slots
