# Status: Dev-Frontend

## Current: ACTIVE (Phase 7 Loop)

## Last Update: 2026-04-02

## Current Task
Phase 7 + Phaser parity polish. Loop — waiting for external changes.

## Progress
- [x] Phase 6 — 12 UI panels + SkillVFX
- [x] Phase 6 Loop — null guards, effects (DamageText, SkillVFX, CameraShake), potion hotkeys
- [x] Phase 7 Step 1 — BootSceneController (53a48a8)
- [x] Phase 7 Step 2 — MainMenuController (53a48a8)
- [x] Phase 7 Step 3 — UI Refresh via GameManager.Instance (53a48a8)
- [x] Phase 7 Step 4 — HUD cooldown/dodge/potion visualization (53a48a8)
- [x] Loop: Stat allocation UI (STR/DEX/WIS/LUC + [+] buttons) (bbdc4ea)
- [x] Loop: H=history, TAB=minimap hotkeys (bbdc4ea)
- [x] Loop: Skill tooltip on HUD hover (e6e8d04)
- [x] Loop: levelGoldText in InventoryUI (9c68785)
- [x] Loop: Simplified HUD cooldowns via GetCooldowns() API (41fcbc0)
- [x] Loop: Buff duration indicators on skill bar (0095244)

## File Inventory (16 files)
### UI (14 files)
UIManager, HUD, InventoryUI, ShopUI, CraftingUI, EnhanceUI, SkillTreeUI,
QuestUI, DialogueUI, NpcProfilePanel, NpcQuestPanel, PauseMenuUI,
BootSceneController, MainMenuController

### Effects (3 files)
SkillVFX, DamageText, CameraShake

### Support classes (in InventoryUI.cs)
EquipSlotUI, InventorySlotUI

### Support classes (in SkillTreeUI.cs)
SkillRowUI

## Integration Notes for Director
- ~~UIManager.OnUseHpPotion / OnUseMpPotion~~ → Wired by Director (7479934)
- CameraShake → replace with CinemachineImpulseSource when installed
- Death marker system not yet implemented (needs cross-role coordination)

## Issues
None
