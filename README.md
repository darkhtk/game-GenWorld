# GenWorld

A Unity 2D port of the Phaser 3 TypeScript RPG `testgame2`, rebuilt as a playable top-down fantasy RPG in C#.

## Overview

GenWorld preserves the original game's core RPG loop in a Unity-native runtime: movement, combat, skills, inventory, NPC dialogue, and progression. The codebase is structured for steady delivery — gameplay systems are pure C# classes, entities are MonoBehaviours, and UI lives under uGUI + TextMeshPro.

## Tech Stack

- **Engine**: Unity 6 (6000.3.9f1) with the Universal Render Pipeline
- **Language**: C# 9
- **Input**: Legacy Input System (`Input.GetKey`, `activeInputHandler: 2 / Both`)
- **UI**: uGUI + TextMeshPro
- **Tilemap**: Unity Tilemap + Tilemap Extras
- **Camera**: Cinemachine 2D
- **Serialization**: Newtonsoft.Json

## Repository Layout

```
Assets/
  Scripts/
    Core/        GameManager and shared bootstrap (read-only for most roles)
    Data/        Data models and JSON DTOs (read-only for most roles)
    Systems/     Pure C# gameplay systems (Combat, Skill, Inventory, Quest, Save, ...)
    AI/          NPC behaviour and dialogue
    Entities/    MonoBehaviour entities (player, monsters, interactables)
    UI/          Canvas-attached MonoBehaviour UI scripts
    Effects/     Visual effect scripts
    Map/         Tilemap and world wiring
  Scenes/        BootScene, MainMenuScene, GameScene
  Prefabs/       Reusable GameObjects (UI/ owned by frontend, others by Asset/QA)
  Art/           Sprites, animations, fonts
  StreamingAssets/Data/  Game data JSON, NPC profiles, lore (Markdown)
  Tests/EditMode/        NUnit edit-mode tests
docs/orchestration/      Role assignments, status, and reference docs
```

## Scene Flow

`BootScene → MainMenuScene → GameScene`

`BootScene` initializes managers, `MainMenuScene` handles entry/UI, and `GameScene` runs the playable loop.

## Getting Started

1. Install Unity **6000.3.9f1** via Unity Hub (Universal Render Pipeline template).
2. Clone the repository:
   ```bash
   git clone git@github.com:darkhtk/game-GenWorld.git
   ```
3. Open the project root in Unity Hub. The first import takes a few minutes while packages resolve.
4. Open `Assets/Scenes/BootScene.unity` and press **Play**.

### Running Tests

Edit-mode tests live in `Assets/Tests/EditMode/`. Run them from **Window → General → Test Runner → EditMode**, or via the Unity CLI:

```bash
Unity -batchmode -runTests -projectPath . -testPlatform editmode -logFile -
```

## Architecture Notes

- **Systems** are plain C# classes (not MonoBehaviour). Constructed with `new` or accessed statically.
- **Entities** are MonoBehaviours and use `[RequireComponent]` to declare their dependencies.
- **UI** scripts attach to children of a Canvas.
- **One class per file**, file size ≤ 300 lines preferred.
- **Naming**: PascalCase for types/methods/properties, camelCase for parameters/locals, `_camelCase` for private fields.
- **Comments and code in English.** Korean is reserved for game data JSON and player-facing UI text.

### Porting Constraints (Phaser → Unity)

- **Y axis** is flipped: Phaser uses Y-down, Unity uses Y-up. Convert tile coordinates carefully.
- **Time units**: Phaser milliseconds → Unity seconds. Convert at boundaries (`Time.time * 1000f` when interfacing with original timing).
- **Sprites**: 32×32 pixel art. Import settings — PPU = 32, Filter = Point, Compression = None.
- **Rigidbody2D** uses `linearVelocity` (Unity 6 naming), not `velocity`.

## Data Files

- Gameplay JSON: `Assets/StreamingAssets/Data/`
- NPC profiles (Markdown): `Assets/StreamingAssets/Data/npc-profiles/`
- Lore (Markdown): `Assets/StreamingAssets/Data/lore/`

Original data lives at `C:\sourcetree\testgame2\data\` and is copied — never edited — into the Unity project.

## Orchestration

Work is split across four parallel CLI roles. Each role owns specific folders and coordinates through `docs/orchestration/`.

| Role          | Owned Folders                                                                |
| ------------- | ---------------------------------------------------------------------------- |
| Director      | `Assets/Scripts/Core/GameManager.cs`, `docs/orchestration/`                  |
| Dev-Backend   | `Assets/Scripts/Systems/`, `Scripts/AI/`, `Scripts/Entities/`, `Tests/EditMode/` |
| Dev-Frontend  | `Assets/Scripts/UI/`, `Scripts/Effects/`, `Prefabs/UI/`                      |
| Asset/QA      | `Assets/Art/`, `StreamingAssets/`, `Scenes/`, `Prefabs/` (excluding `UI/`)   |
| Shared (RO)   | `Assets/Scripts/Core/`, `Assets/Scripts/Data/`                               |

### Workflow

- Before working: read your assignment in `docs/orchestration/assignments/<role>.md` and the relevant `docs/orchestration/reference/` docs.
- During work: only modify your owned folders. Watch `docs/orchestration/status/compile-status.md` for errors caused by your changes. If blocked, file a question in `docs/orchestration/questions/`.
- After working: update `docs/orchestration/status/<role>.md` with progress, then `git add` and `git commit` your owned files only.
- Always `git pull --rebase` before committing. Conflicts in another role's files must be reported to Director — never resolved unilaterally.

## Project Status

Currently in stabilization mode: draining the In-Review queue and fixing the highest-value runtime issues before widening scope. See `PROJECT_BRIEF.md`, `MVP_SCOPE.md`, and `CURRENT_GOAL.md` for the latest direction.

## License

Internal project — license terms TBD.
