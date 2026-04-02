using System.Collections.Generic;
using UnityEngine;

public static class StatsSystem
{
    public static Stats ComputeBaseStats(int level, Dictionary<string, int> bonusStats)
    {
        int str = bonusStats.GetValueOrDefault("str");
        int dex = bonusStats.GetValueOrDefault("dex");
        int wis = bonusStats.GetValueOrDefault("wis");
        int luc = bonusStats.GetValueOrDefault("luc");

        return new Stats
        {
            maxHp = GameConfig.BaseHp + level * GameConfig.HpPerLevel
                  + Mathf.RoundToInt(str * GameConfig.StrHpBonus),
            maxMp = GameConfig.BaseMp + level * GameConfig.MpPerLevel
                  + Mathf.RoundToInt(wis * GameConfig.WisMpBonus),
            atk = GameConfig.BaseAtk + Mathf.RoundToInt(str * GameConfig.StrAtkBonus),
            def = GameConfig.BaseDef + Mathf.RoundToInt(dex * GameConfig.DexDefBonus),
            spd = GameConfig.BaseSpd + Mathf.RoundToInt(dex * GameConfig.DexSpdBonus),
            crit = GameConfig.BaseCrit + Mathf.RoundToInt(luc * GameConfig.LucCritBonus)
        };
    }

    public static Stats ComputeStats(
        Stats baseStats,
        Dictionary<string, ItemInstance> equipment,
        Dictionary<string, ItemDef> itemDefs,
        Dictionary<string, SetBonusDef> setBonuses)
    {
        var result = baseStats;
        var setCounts = new Dictionary<string, int>();

        if (equipment != null)
        {
            foreach (var kvp in equipment)
            {
                if (kvp.Value == null) continue;
                if (!itemDefs.TryGetValue(kvp.Value.itemId, out var def)) continue;

                result = result + def.stats.ToStats();

                if (kvp.Value.enhanceLevel > 0)
                {
                    int bonus = kvp.Value.enhanceLevel * GameConfig.EnhanceBonusPerLevel;
                    result.atk += bonus;
                }

                if (!string.IsNullOrEmpty(def.setId))
                {
                    setCounts.TryGetValue(def.setId, out int c);
                    setCounts[def.setId] = c + 1;
                }
            }
        }

        if (setBonuses != null)
        {
            foreach (var kvp in setCounts)
            {
                if (!setBonuses.TryGetValue(kvp.Key, out var bonus)) continue;
                if (kvp.Value >= 2 && bonus.pc2 != null) result = result + bonus.pc2.ToStats();
                if (kvp.Value >= 3 && bonus.pc3 != null) result = result + bonus.pc3.ToStats();
                if (kvp.Value >= 4 && bonus.pc4 != null) result = result + bonus.pc4.ToStats();
                if (kvp.Value >= 5 && bonus.pc5 != null) result = result + bonus.pc5.ToStats();
            }
        }

        return result;
    }

    public static void AddXp(ref PlayerLevelState state, int amount)
    {
        state.xp += amount;
        while (state.xp >= GameConfig.XpForLevel(state.level))
        {
            state.xp -= GameConfig.XpForLevel(state.level);
            state.level++;
            state.skillPoints += GameConfig.SkillPointsPerLevel;
            state.statPoints += GameConfig.StatPointsPerLevel;
            EventBus.Emit(new LevelUpEvent { level = state.level, prevLevel = state.level - 1 });
        }
    }
}
