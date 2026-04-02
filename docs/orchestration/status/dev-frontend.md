# Status: Dev-Frontend

## Current: ACTIVE (Phase 7 Loop)

## Last Update: 2026-04-02

## Current Task
Phase 7 + Phaser parity polish. Loop.

## Progress
- [x] Phase 6 — 12 UI panels + SkillVFX
- [x] Phase 6 Loop — null guards, effects (DamageText, SkillVFX, CameraShake), potion hotkeys
- [x] Phase 7 Step 1 — BootSceneController (53a48a8)
- [x] Phase 7 Step 2 — MainMenuController (53a48a8)
- [x] Phase 7 Step 3 — UI Refresh via GameManager.Instance (53a48a8)
- [x] Phase 7 Step 4 — HUD cooldown/dodge/potion visualization (53a48a8)
- [x] Loop: Stat allocation UI in InventoryUI (STR/DEX/WIS/LUC + buttons) (bbdc4ea)
- [x] Loop: H=history toggle, TAB=minimap toggle hotkeys (bbdc4ea)
- [x] Loop: Skill tooltip on HUD hover (name/dmg/range/cost/cd) (e6e8d04)
- [x] Loop: levelGoldText wiring in InventoryUI (9c68785)

## Hotkey Map (complete)
| Key | Action |
|-----|--------|
| I | Inventory toggle |
| K | Skill tree toggle |
| J | Quest log toggle |
| H | History log toggle |
| TAB | Minimap toggle |
| Esc | Close panels / Pause |
| R | HP potion |
| T | MP potion |
| 1-6 | Skills (GameManager) |
| E | Interact (PlayerController) |

## Integration Notes for Director
- UIManager.OnUseHpPotion / OnUseMpPotion need wiring in GameManager
- CameraShake → replace with CinemachineImpulseSource when installed
- Death marker system not yet implemented (needs cross-role coordination)

## Issues
None
