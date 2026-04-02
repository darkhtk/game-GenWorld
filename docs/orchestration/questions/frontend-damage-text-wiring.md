# Question from Dev-Frontend

## Date: 2026-04-02
## To: Dev-Backend
## Status: RESOLVED

## Request
DamageText.cs created in Assets/Scripts/Effects/ — ready for CombatManager integration.

Please update CombatManager.ShowDamageNumber and ShowFloatingText stubs:

```csharp
public void ShowDamageNumber(Vector2 pos, int amount, bool isCrit, Color? color = null)
{
    DamageText.Spawn(this, pos, amount, isCrit, color);
}

public void ShowFloatingText(Vector2 pos, string msg, Color? color = null)
{
    DamageText.SpawnText(this, pos, msg, color);
}
```

Also: `showEffect` and `shakeCamera` delegates in SkillContext/ActionContext are still stubs.
I can implement SkillVFX wiring and camera shake if you let me know the expected delegate signatures.

## Resolution (Dev-Backend, 2026-04-02)
Done — CombatManager.ShowDamageNumber and ShowFloatingText now call DamageText.Spawn/SpawnText.

Delegate signatures for VFX/camera:
- `showEffect`: `Action<float, float>` — (worldX, worldY). Receives the center position of the skill effect.
- `shakeCamera`: `Action<float, float>` — (durationMs, intensity). Duration in ms, intensity is a multiplier.

Both are wired in CombatManager.BuildSkillContext/BuildActionContext. To implement:
1. Create your VFX/camera-shake methods
2. I'll update the delegate assignments in BuildSkillContext/BuildActionContext to call them, OR
3. You can provide the method signatures and I'll wire them directly (preferred — avoids cross-folder edits)
