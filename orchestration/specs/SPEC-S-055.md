# SPEC-S-055: UI Resolution Adaptation

> **Priority:** P3
> **Category:** Stabilize / UI
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

Verify that all UI elements display correctly across common resolutions (1280x720, 1920x1080, 2560x1440, 3840x2160) and aspect ratios (16:9, 16:10, 21:9). Current UI uses uGUI + TextMeshPro with CanvasScaler configuration in scene/prefab (not code).

## Current State

### UI Architecture
- **Framework:** uGUI + TextMeshPro
- **Canvas configuration:** Set in Unity Editor (no CanvasScaler references in C# code)
- **UI scripts:** `Assets/Scripts/UI/` — HUD, UIManager, various panel scripts
- **Panels:** Inventory, Shop, SkillTree, Dialogue, Settings, QuestTracker, Minimap, etc.

### Potential Issues
1. **CanvasScaler mode**: If set to `ConstantPixelSize`, UI won't scale with resolution
2. **Hardcoded pixel values**: UI scripts may use hardcoded position/size values that don't adapt
3. **Anchor misconfiguration**: Panels anchored to center instead of edges may overlap or leave gaps at non-16:9 ratios
4. **TextMeshPro overflow**: Text that fits at 1080p may overflow containers at 720p
5. **Minimap/HUD positioning**: Corner-anchored elements may overlap at smaller resolutions

### Expected Configuration
- CanvasScaler: `ScaleWithScreenSize`, reference resolution 1920x1080, match 0.5 (width/height blend)
- All panels: Anchored to appropriate screen edges (HUD corners, popups center)
- Text: Auto-size or overflow handling enabled

## Required Verification

### 1. Check CanvasScaler settings
- Open main Canvas in scene hierarchy
- Verify mode is `ScaleWithScreenSize` with reference 1920x1080
- If `ConstantPixelSize`: change to ScaleWithScreenSize

### 2. Audit anchors for key panels
- HUD bars (HP/MP/XP): anchored to top-left or bottom
- Minimap: anchored to top-right corner
- Inventory/Shop/SkillTree: anchored to center with stretch or fixed size
- Dialogue box: anchored to bottom-center
- Quest tracker: anchored to right edge

### 3. Test at target resolutions
- 1280x720 (minimum target)
- 1920x1080 (reference)
- 2560x1440 (common high-res)
- Verify no overlapping, clipping, or off-screen elements

### 4. Check hardcoded values in UI scripts
- Search for `new Vector2(`, `new Vector3(`, `rectTransform.anchoredPosition =` in UI scripts
- Verify values are relative or scaled, not absolute pixel values

## Affected Files
- Canvas objects in scene (CanvasScaler settings)
- `Assets/Scripts/UI/*.cs` — any hardcoded position/size values
- UI Prefabs in `Assets/Prefabs/UI/`

## Entry Point
- Game launch at any resolution
- Settings menu resolution change (if implemented)

## Test Plan
- Manual: Set Game view to 1280x720 → verify all UI visible and readable
- Manual: Set Game view to 2560x1440 → verify no tiny/oversized elements
- Manual: Open each UI panel at 720p → verify no overflow or clipping
- Code audit: grep UI scripts for hardcoded pixel positions

## Save Integration
- None (UI layout only)
