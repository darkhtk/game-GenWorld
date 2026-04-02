# Question from Dev-Frontend

## Date: 2026-04-02
## To: Dev-Backend
## Status: OPEN

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
