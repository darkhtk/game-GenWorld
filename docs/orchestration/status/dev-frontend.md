# Status: Dev-Frontend

## Current: DONE (Phase 7)

## Last Update: 2026-04-02

## Current Task
All steps + Phaser parity complete. Cinemachine installed.

## Progress
- [x] Phase 6 — 12 UI panels + SkillVFX
- [x] Phase 6 Loop — null guards, effects (DamageText, SkillVFX, CameraShake), potion hotkeys
- [x] Phase 7 Step 1 — BootSceneController (53a48a8)
- [x] Phase 7 Step 2 — MainMenuController (53a48a8)
- [x] Phase 7 Step 3 — UI Refresh via GameManager.Instance (53a48a8)
- [x] Phase 7 Step 4 — HUD cooldown/dodge/potion visualization (53a48a8)
- [x] Loop: Stat allocation, hotkeys, skill tooltip, buff durations, levelGoldText
- [x] Loop: Cinemachine 3.1.2 설치 + CameraShake ImpulseSource 마이그레이션 (37d4910, b76ab9c)

## File Inventory (17 files)
### UI (14): UIManager, HUD, InventoryUI, ShopUI, CraftingUI, EnhanceUI, SkillTreeUI, QuestUI, DialogueUI, NpcProfilePanel, NpcQuestPanel, PauseMenuUI, BootSceneController, MainMenuController
### Effects (3): SkillVFX, DamageText, CameraShake
### Support: EquipSlotUI, InventorySlotUI (in InventoryUI.cs), SkillRowUI (in SkillTreeUI.cs)

## Integration Notes for Director
- Cinemachine 설치 완료 — Scene에 CinemachineCamera + ImpulseListener 세팅 필요 (Editor)
- Death marker system 미구현 (cross-role)

## Issues
None
