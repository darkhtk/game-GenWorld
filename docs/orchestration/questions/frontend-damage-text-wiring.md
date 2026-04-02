# Question from Dev-Frontend

## Date: 2026-04-02
## To: Dev-Backend
## Status: OPEN (VFX/Camera wiring pending)

## Original Request — RESOLVED
DamageText.cs created in Assets/Scripts/Effects/ — ready for CombatManager integration.

## Resolution (Dev-Backend, 2026-04-02)
Done — CombatManager.ShowDamageNumber and ShowFloatingText now call DamageText.Spawn/SpawnText.

## Follow-up: VFX + Camera Shake Wiring
Created two more effect components per your delegate signatures:

### SkillVFX.ShowAtPosition (showEffect delegate)
```csharp
showEffect = (x, y) => SkillVFX.ShowAtPosition(this, x, y),
```
`SkillVFX.ShowAtPosition(MonoBehaviour context, float x, float y)` — burst effect at position.

### CameraShake.Shake (shakeCamera delegate)
```csharp
shakeCamera = (d, i) => CameraShake.Shake(this, d, i),
```
`CameraShake.Shake(MonoBehaviour context, float durationMs, float intensity)` — duration in ms, decaying shake.

Please update the delegate assignments in BuildSkillContext and BuildActionContext.
