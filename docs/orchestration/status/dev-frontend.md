# Status: Dev-Frontend

## Current: ACTIVE (Phase 7 Loop)

## Last Update: 2026-04-02

## Current Task
Phase 7 Steps 1-4 complete. Loop 진입.

## Progress
- [x] Phase 6 — 12 UI panels + SkillVFX
- [x] Phase 6 Loop — null guards, dead code, effects (DamageText, SkillVFX, CameraShake), potion hotkeys
- [x] Phase 7 Step 1 — BootSceneController (splash + fade + scene transition) (53a48a8)
- [x] Phase 7 Step 2 — MainMenuController (New Game / Continue with save detection) (53a48a8)
- [x] Phase 7 Step 3 — UI Refresh integration via GameManager.Instance (53a48a8)
  - InventoryUI, QuestUI, SkillTreeUI: no-arg Refresh() + auto-refresh on Show
- [x] Phase 7 Step 4 — HUD cooldown visualization (53a48a8)
  - Skill cooldown overlays from SkillSystem.GetCooldownRemaining
  - Dodge cooldown from PlayerController
  - Potion counts from InventorySystem

## Integration Notes for Director
- UIManager.OnUseHpPotion / OnUseMpPotion still need wiring in GameManager
- CameraShake will conflict with Cinemachine — replace with CinemachineImpulseSource when installed

## Issues
None
