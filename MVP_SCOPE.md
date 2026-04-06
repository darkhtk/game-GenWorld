# MVP Scope

## MVP Goal

Ship a Unity playable slice where the main game loop works end-to-end:

- enter the game
- move and fight
- interact with NPCs
- manage core UI flows
- save and resume progress

## Must Have

- `BootScene -> MainMenuScene -> GameScene` flow works reliably
- Player movement, targeting, combat, damage, and monster defeat work in `GameScene`
- Inventory, skill, quest, and dialogue flows can open and close without breaking input/state
- Save/load works for the current core progression loop
- Review queue for critical runtime systems is reduced enough that the team can trust the current slice

## Nice to Have

- Additional UI polish and quality-of-life feedback
- More VFX/SFX polish after the main loop is stable
- Extra content breadth once the current gameplay slice is trustworthy

## Out of Scope For This MVP

- Broad content expansion before the current Unity gameplay loop is stable
- Steam/release packaging work that does not unblock the playable slice
- Secondary polish tasks that do not improve the current runtime loop or review backlog

## Current Constraints

- The project is in resume mode with a heavy review queue
- Runtime stability issues must be resolved before adding more feature breadth
- The MVP should follow the existing Unity architecture instead of introducing major rewrites

## Validation

- A tester can launch the project and exercise the core gameplay loop without obvious blockers
- Core review items for high-value runtime systems are closed or meaningfully reduced
- The next backlog after this MVP can focus on expansion instead of repeated stabilization
