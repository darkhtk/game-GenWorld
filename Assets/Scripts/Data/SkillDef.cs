using System;
using Newtonsoft.Json;

[Serializable]
public class SkillDef
{
    public string id, name, tree;
    public int requiredPoints = 1, requiredLevel = 1;
    public int mpCost;
    public float cooldown, damage, range, aoe;
    public string description, effect, behavior, buffType;
    public float effectDuration, buffValue;
    public SkillScaling scaling;
    public SkillVisualEffect visualEffect;
    public SkillAction[] actions;

    [JsonIgnore]
    public SkillTree TreeEnum => tree switch { "ranged" => SkillTree.Ranged, "magic" => SkillTree.Magic, _ => SkillTree.Melee };
}

[Serializable]
public class SkillScaling { public float damage = 0.2f, aoe = 0.1f, duration = 0.15f, buff = 0.1f; }

[Serializable]
public class SkillVisualEffect { public string type; public int color, color2; public float size, duration; public string attach; }

[Serializable]
public class SkillAction
{
    public string type;
    public float aoe, ratio = 1f, duration, value, speed, range, pathWidth, spread, scatterRadius, cone, distance, radius, tickInterval, buffValue, intensity;
    public bool crit, piercing, toTarget, behindTarget;
    public int color, count;
    public string effect, trajectory, pattern, monsterId, buffType, skillId;
    public ChainConfig chain;
    public SkillAction[] onHit, onArrive, onTick;
}

[Serializable]
public class ChainConfig { public int maxBounces; public float bounceRange, decayRatio; }

[Serializable]
public class SkillsData { public SkillDef[] skills; }
