# SPEC-B-008: Camera Z Position Fix

> **Priority:** P0 (user bug report)
> **Symptom:** Camera Z value must be negative for map/characters to be visible.

---

## Key Files

| Component | File | Line |
|-----------|------|------|
| Camera follow | `Assets/Scripts/Core/GameManager.cs` | 165-171 |
| Dialogue zoom | `Assets/Scripts/Effects/DialogueCameraZoom.cs` | — |
| Scene setup | Check MainScene/GameScene `.unity` file | — |

## Diagnostic Points

### 1. Camera Z Position in GameManager
- **GameManager.cs:165-171:**
  ```
  var cam = Camera.main;
  float z = cam.transform.position.z;
  cam.transform.position = new Vector3(pos.x, pos.y, z);
  ```
- The camera follow code preserves existing Z value. If the camera's initial Z is 0 in the scene, sprites (at Z=0) won't render because orthographic camera needs to be "behind" them (Z < 0 in Unity 2D).

### 2. Expected Camera Z
- For Unity 2D with orthographic camera:
  - Camera Z should be **-10** (Unity default) or any negative value.
  - Sprites at Z=0 → camera at Z=-10 → camera looks "forward" (+Z) and sees sprites.
- If camera Z = 0, camera is at same Z as sprites → nothing visible → grey screen.

### 3. Where to Fix
**Option A: Scene file (preferred)**
- Open the main game scene `.unity` file.
- Find the Main Camera object → set `m_LocalPosition.z` to `-10`.
- This is the cleanest fix — no code change needed.

**Option B: Code initialization**
- In GameManager or scene setup script, add:
  ```csharp
  var cam = Camera.main;
  if (cam != null && cam.transform.position.z >= 0)
      cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, -10f);
  ```

### 4. Related Issues
- **B-004 (minimap):** Minimap camera also needs Z < 0 to see content.
- **DialogueCameraZoom:** Only modifies `orthographicSize`, not Z — should be fine.

## Fix Direction
1. Check current camera Z in scene file (grep for `m_LocalPosition` in Main Camera).
2. Set Z to -10 in scene or code.
3. Verify minimap camera Z separately.

## Verification
- Map tiles, player, NPCs, monsters all visible immediately on scene load.
- Camera follow works correctly (X/Y follow player, Z stays at -10).
- Minimap camera also renders correctly.
