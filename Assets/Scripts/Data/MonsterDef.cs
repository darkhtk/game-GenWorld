using System;

[Serializable]
public class MonsterDef
{
    public string id, name, region, sprite;
    public int hp, atk, def, xp, gold;
    public float spd;
    public float detectRange, attackRange, attackCooldown;
    public DropEntry[] drops;
    public string rank = "normal";
    public RangedConfig ranged;
    public MonsterAttackPattern[] attackPatterns;
    public MonsterPhase[] phases;
    [Newtonsoft.Json.JsonIgnore] public AnimationDef animationDef;
}

[Serializable] public class DropEntry { public string itemId; public float chance; public int minCount, maxCount; }
[Serializable] public class RangedConfig { public float projectileSpeed, projectileSize; public int projectileColor; }
[Serializable] public class MonsterAttackPattern { public string name; public float weight = 1f, cooldown, windupMs; public SkillAction[] actions; }
[Serializable] public class MonsterPhase { public float hpPercent; public MonsterAttackPattern[] attackPatterns; public PhaseStatMult statMult; public SkillAction[] onEnter; }
[Serializable] public class PhaseStatMult { public float atk = 1f, def = 1f, spd = 1f, cooldown = 1f; }
[Serializable] public class MonstersData { public MonsterDef[] monsters; }
