# Project Brief

## One-line Summary

GenWorld is a Unity 2D port of the Phaser 3 TypeScript RPG `testgame2`, rebuilt as a playable top-down fantasy RPG in C#.

## Why This Exists

- Preserve the original game's core RPG loop in a Unity-native runtime.
- Create a maintainable Unity project structure for gameplay, UI, save/load, and orchestration-driven delivery.
- Reach a playable slice quickly, then expand content and polish on top of a stable base.

## Primary User

- Internal developer and QA users validating the Unity port.
- Early players testing a compact fantasy RPG loop with combat, NPC interaction, inventory, and progression.

## Core Experience

- Start from `BootScene` and `MainMenuScene`, then enter `GameScene`.
- Move the player through the world, fight monsters, use skills, collect loot, and interact with NPC dialogue.
- Open core UI flows such as inventory, quests, crafting, enhancement, and pause/settings without breaking the runtime loop.

## Success Criteria

- The Unity build reproduces the original game's core playable loop end-to-end.
- Core systems stay stable enough that review items can drain instead of re-opening the same runtime bugs.
- The project can move from review-heavy stabilization into intentional feature expansion.

## Current Product Shape

- Engine: Unity 2D / URP / C#
- Major systems: GameManager, CombatManager, SkillSystem, InventorySystem, QuestSystem, SaveSystem, AI/NPC dialogue
- Current operating mode: resume from an active orchestration board

## Open Questions

- Which features are strictly required for the first public or internal playable milestone?
- Which systems should stay in stabilization mode versus move back into feature delivery?
