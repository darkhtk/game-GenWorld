# GenWorld Pre-Release QA Checklist

## Steam API
- [ ] SteamManager initializes without crash
- [ ] Offline mode works when Steam not running (no crash, offline fallback)
- [ ] Cloud save uploads/downloads correctly
- [ ] Cloud save conflict resolution UI works when PC A and PC B have different saves
- [ ] Steam achievements unlock on correct triggers
- [ ] At least 5 achievements: unlock → verify on Steam overlay → reset via SteamCMD → re-unlock
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
- [ ] Stable 60fps in village area (30fps minimum on min-spec hardware)
- [ ] No GC spikes during combat (object pooling)
- [ ] Monster culling at distance (50 tiles)
- [ ] 1-hour continuous play: memory increase < 100MB (check Unity Profiler)
- [ ] Build size < 500MB; remove unused assets (check Editor Log for large files)

## Build
- [ ] Windows x64 build runs standalone
- [ ] No console errors on fresh start
- [ ] Release build has no Debug.Log output in Player.log (or minimal)
- [ ] No debug UI visible (F9 animation preview, Shift+P stats — disabled in release)
- [ ] Resolution changes apply correctly
- [ ] Resolution change shows 15-second confirmation popup
- [ ] Fullscreen toggle works
- [ ] steam_appid.txt NOT included in build output

## Store / Legal
- [ ] Steamworks Partner site: store page has all required fields (description, screenshots, system req)
- [ ] Store screenshots match actual gameplay
- [ ] EULA / privacy policy prepared and linked (if applicable)
- [ ] Content rating / age gate configured correctly
