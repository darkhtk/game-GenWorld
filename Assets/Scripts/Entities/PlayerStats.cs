using System.Collections.Generic;
using UnityEngine;

public class PlayerStats
{
    public int Level = 1, Xp, Gold, SkillPoints, StatPoints;
    public Dictionary<string, int> BonusStats = new() { ["str"] = 0, ["dex"] = 0, ["wis"] = 0, ["luc"] = 0 };
    public Stats CurrentStats;
    public int Hp, Mp;
    public Dictionary<string, ItemInstance> Equipment = new() { ["weapon"] = null, ["helmet"] = null, ["armor"] = null, ["boots"] = null, ["accessory"] = null };

    public void RecalcStats(Dictionary<string, ItemDef> itemDefs, Dictionary<string, SetBonusDef> setBonuses)
    {
        var baseStats = StatsSystem.ComputeBaseStats(Level, BonusStats);
        CurrentStats = StatsSystem.ComputeStats(baseStats, Equipment, itemDefs, setBonuses);
        Hp = Mathf.Min(Hp, CurrentStats.maxHp);
        Mp = Mathf.Min(Mp, CurrentStats.maxMp);
    }

    public void FullHeal() { Hp = CurrentStats.maxHp; Mp = CurrentStats.maxMp; }
}
