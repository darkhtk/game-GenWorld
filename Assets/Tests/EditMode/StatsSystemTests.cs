using NUnit.Framework;
using System.Collections.Generic;

public class StatsSystemTests
{
    [SetUp]
    public void Setup() => EventBus.Clear();

    [Test]
    public void ComputeBaseStats_Level1_NoBonusStats()
    {
        var stats = StatsSystem.ComputeBaseStats(1, new Dictionary<string, int>());
        Assert.AreEqual(GameConfig.BaseHp + GameConfig.HpPerLevel, stats.maxHp);
        Assert.AreEqual(GameConfig.BaseAtk, stats.atk);
        Assert.AreEqual(GameConfig.BaseDef, stats.def);
    }

    [Test]
    public void ComputeBaseStats_Level5_Str10()
    {
        var bonus = new Dictionary<string, int> { ["str"] = 10 };
        var stats = StatsSystem.ComputeBaseStats(5, bonus);
        // HP = 100 + 5*12 + 10*3 = 190
        Assert.AreEqual(190, stats.maxHp);
        // ATK = 10 + 10*1.5 = 25
        Assert.AreEqual(25, stats.atk);
    }

    [Test]
    public void ComputeStats_WithEquipment_AddsStats()
    {
        var baseStats = new Stats { maxHp = 100, atk = 10, def = 5 };
        var equip = new Dictionary<string, ItemInstance>
        {
            ["weapon"] = new ItemInstance { itemId = "wooden_sword" }
        };
        var itemDefs = new Dictionary<string, ItemDef>
        {
            ["wooden_sword"] = new ItemDef { id = "wooden_sword", stats = new ItemStats { atk = 5 } }
        };
        var result = StatsSystem.ComputeStats(baseStats, equip, itemDefs, null);
        Assert.AreEqual(15, result.atk);
    }

    [Test]
    public void ComputeStats_WithEnhancement_AddsBonusAtk()
    {
        var baseStats = new Stats { atk = 10 };
        var equip = new Dictionary<string, ItemInstance>
        {
            ["weapon"] = new ItemInstance { itemId = "sword", enhanceLevel = 3 }
        };
        var itemDefs = new Dictionary<string, ItemDef>
        {
            ["sword"] = new ItemDef { id = "sword", stats = new ItemStats { atk = 5 } }
        };
        var result = StatsSystem.ComputeStats(baseStats, equip, itemDefs, null);
        // 10 + 5 (equip) + 3*2 (enhance) = 21
        Assert.AreEqual(21, result.atk);
    }

    [Test]
    public void AddXp_LevelsUp_WhenEnough()
    {
        var state = new PlayerLevelState { level = 1, xp = 0, skillPoints = 0, statPoints = 0 };
        int xpNeeded = GameConfig.XpForLevel(1);
        StatsSystem.AddXp(ref state, xpNeeded);
        Assert.AreEqual(2, state.level);
        Assert.AreEqual(2, state.skillPoints);
        Assert.AreEqual(3, state.statPoints);
    }

    [Test]
    public void AddXp_MultipleLevelUps()
    {
        var state = new PlayerLevelState { level = 1, xp = 0, skillPoints = 0, statPoints = 0 };
        StatsSystem.AddXp(ref state, 9999);
        Assert.IsTrue(state.level > 2);
    }

    [Test]
    public void ComputeBaseStats_DexBonuses()
    {
        var bonus = new Dictionary<string, int> { ["dex"] = 10 };
        var stats = StatsSystem.ComputeBaseStats(1, bonus);
        // DEF = 5 + 10*1.5 = 20
        Assert.AreEqual(20, stats.def);
        // SPD = 80 + 10*1 = 90
        Assert.AreEqual(90, stats.spd);
    }
}
