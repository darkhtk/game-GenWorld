# GenWorld Pre-Release QA Checklist

## Steam API
- [ ] SteamManager initializes without crash
- [ ] Offline mode works when Steam not running
- [ ] Cloud save uploads/downloads correctly
- [ ] Steam achievements unlock on correct triggers
- [ ] Steam overlay (Shift+Tab) works in-game

## Core Gameplay
- [ ] Game starts from boot scene without errors
- [ ] Player movement (WASD) responsive
- [ ] Auto-attack (left click) hits monsters
- [ ] Skills 1-6 fire correctly
- [ ] Dodge (Ctrl/Space) provides i-frames
- [ ] Monster AI: patrol, chase, attack, return
- [ ] Loot drops on monster death
- [ ] XP/Gold floating text on kill

## Save/Load
- [ ] Manual save (Esc → Save) works
- [ ] Auto-save on region change
- [ ] Load restores player position/level/inventory
- [ ] Save version migration (old saves load)

## UI
- [ ] Inventory (I): open/close, equip, filter, sort
- [ ] Equipment compare popup on equip click
- [ ] Skill tooltips on hover
- [ ] Quest tracker (V toggle) shows active quests
- [ ] Minimap (M zoom toggle) shows entities
- [ ] Achievement panel (J) shows progress
- [ ] Settings (Esc → Settings) volume/resolution
- [ ] Keybindings (F1) display

## Performance
- [ ] Stable 60fps in village area
- [ ] No GC spikes during combat (object pooling)
- [ ] Monster culling at distance (50 tiles)
- [ ] Build size < 500MB

## Build
- [ ] Windows x64 build runs standalone
- [ ] No console errors on fresh start
- [ ] Resolution changes apply correctly
- [ ] Fullscreen toggle works
