using NUnit.Framework;
using System.Collections.Generic;

public class SkillSystemTests
{
    SkillSystem sys;
    Dictionary<string, SkillDef> defs;

    [SetUp]
    public void Setup()
    {
        defs = new Dictionary<string, SkillDef>
        {
            ["heavy_strike"] = new SkillDef
            {
                id = "heavy_strike", name = "Heavy Strike", tree = "melee",
                requiredPoints = 1, requiredLevel = 1,
                mpCost = 8, cooldown = 3000, damage = 2.5f, range = 72, aoe = 0
            },
            ["fireball"] = new SkillDef
            {
                id = "fireball", name = "Fireball", tree = "magic",
                requiredPoints = 3, requiredLevel = 5,
                mpCost = 15, cooldown = 5000, damage = 3.0f, range = 200, aoe = 64
            }
        };
        sys = new SkillSystem(defs);
    }

    [Test]
    public void LearnSkill_Succeeds_WithEnoughPoints()
    {
        var result = sys.LearnSkill("heavy_strike", 5, 1);
        Assert.IsTrue(result.learned);
        Assert.AreEqual(4, result.remainingPoints);
        Assert.AreEqual(1, sys.GetSkillLevel("heavy_strike"));
    }

    [Test]
    public void LearnSkill_Fails_TooFewPoints()
    {
        var result = sys.LearnSkill("fireball", 2, 5);
        Assert.IsFalse(result.learned);
    }

    [Test]
    public void LearnSkill_Fails_LevelTooLow()
    {
        var result = sys.LearnSkill("fireball", 10, 3);
        Assert.IsFalse(result.learned);
    }

    [Test]
    public void LearnSkill_LevelsUp_WhenAlreadyLearned()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        var result = sys.LearnSkill("heavy_strike", 4, 1);
        Assert.IsTrue(result.learned);
        Assert.AreEqual(2, sys.GetSkillLevel("heavy_strike"));
    }

    [Test]
    public void LearnSkill_MaxLevel5()
    {
        int pts = 20;
        for (int i = 0; i < 5; i++)
        {
            var r = sys.LearnSkill("heavy_strike", pts, 1);
            pts = r.remainingPoints;
        }
        Assert.AreEqual(5, sys.GetSkillLevel("heavy_strike"));
        var fail = sys.LearnSkill("heavy_strike", pts, 1);
        Assert.IsFalse(fail.learned);
    }

    [Test]
    public void EquipSkill_Succeeds_ForLearnedSkill()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        Assert.IsTrue(sys.EquipSkill("heavy_strike", 0));
    }

    [Test]
    public void EquipSkill_Fails_ForUnlearnedSkill()
    {
        Assert.IsFalse(sys.EquipSkill("heavy_strike", 0));
    }

    [Test]
    public void UseSkill_ReturnsSuccess_WhenReady()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        sys.EquipSkill("heavy_strike", 0);
        var result = sys.UseSkill(0, 100, 0f);
        Assert.IsTrue(result.success);
        Assert.AreEqual(8, result.mpCost);
    }

    [Test]
    public void UseSkill_FailsCooldown()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        sys.EquipSkill("heavy_strike", 0);
        sys.UseSkill(0, 100, 0f);
        var result = sys.UseSkill(0, 100, 1000f);
        Assert.IsFalse(result.success);
        Assert.AreEqual("cooldown", result.reason);
    }

    [Test]
    public void UseSkill_FailsNoMp()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        sys.EquipSkill("heavy_strike", 0);
        var result = sys.UseSkill(0, 5, 0f);
        Assert.IsFalse(result.success);
        Assert.AreEqual("no_mp", result.reason);
    }

    [Test]
    public void ResetAll_RefundsPoints()
    {
        sys.LearnSkill("heavy_strike", 5, 1);
        sys.LearnSkill("heavy_strike", 4, 1);
        int refund = sys.ResetAllSkills();
        Assert.AreEqual(2, refund);
        Assert.AreEqual(0, sys.GetSkillLevel("heavy_strike"));
    }
}
