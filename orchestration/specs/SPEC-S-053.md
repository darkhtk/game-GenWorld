# SPEC-S-053: PlayerController Wall Clipping Prevention

> **Priority:** P3
> **Category:** Stabilize / Physics
> **Author:** Coordinator (auto)
> **Date:** 2026-04-03

## Summary

Verify that the player character cannot clip through walls or get stuck inside colliders at tilemap boundaries. Current movement uses Rigidbody2D.linearVelocity with no explicit position correction.

## Current State

### PlayerController Physics (`Assets/Scripts/Entities/PlayerController.cs`)
- `[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]`
- Rigidbody2D: `gravityScale = 0`, `freezeRotation = true`
- Movement: `_rb.linearVelocity = moveDir.normalized * _moveSpeed` in Update()
- Dodge: `_rb.linearVelocity = _dodgeDir * DodgeSpeed` (12f) — higher speed burst
- Collider type: Not specified in code — configured on prefab (likely BoxCollider2D or CircleCollider2D)

### Potential Issues
1. **High-speed dodge clipping**: DodgeSpeed (12f) may tunnel through thin colliders at low frame rates. Unity's default CCD (Continuous Collision Detection) is off for Rigidbody2D by default.
2. **Corner trapping**: Diagonal movement into concave corners (e.g., L-shaped wall) could wedge player between two colliders.
3. **Tilemap collider gaps**: TilemapCollider2D with CompositeCollider2D can have micro-gaps between tiles if not configured properly.
4. **Velocity set in Update vs FixedUpdate**: linearVelocity is set in `Update()`, not `FixedUpdate()` — physics integration mismatch could cause jitter at collider boundaries.

## Required Verification

### 1. Check Rigidbody2D collision detection mode
- Verify `collisionDetectionMode` is set to `Continuous` (not `Discrete`) on player prefab
- If Discrete: change to Continuous to prevent dodge tunneling

### 2. Check tilemap collider setup
- Verify TilemapCollider2D uses CompositeCollider2D with `geometryType = Polygons`
- Check for micro-gaps in composite collider outline

### 3. Test corner scenarios
- Walk diagonally into L-shaped corners
- Dodge into walls at various angles
- Walk along wall edges — verify smooth sliding (no stutter)

### 4. Consider FixedUpdate migration
- Evaluate moving velocity assignment to FixedUpdate for physics consistency
- If not migrating: document why Update is acceptable (e.g., responsiveness priority)

## Affected Files
- `Assets/Scripts/Entities/PlayerController.cs` — potential FixedUpdate migration, CCD setting
- Player prefab — Rigidbody2D collision detection mode

## Entry Point
- Player movement via WASD/arrows or dodge via Space/Ctrl near wall colliders

## Test Plan
- Manual: Walk into each wall direction, verify no clipping
- Manual: Dodge directly into wall at point-blank range, verify bounce/stop
- Manual: Walk diagonally into concave corner, verify no stuck state
- Manual: Walk along wall edge, verify smooth sliding without jitter

## Save Integration
- None (physics/movement only)
