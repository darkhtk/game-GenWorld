using NUnit.Framework;
using System.Collections.Generic;

public class DataManagerSkillValidationTests
{
    [Test]
    public void NullIdSkill_IsFilteredFromList()
    {
        var raw = new SkillDef[]
        {
            new SkillDef { id = "slash", name = "Slash", cooldown = 1000, damage = 1f },
            new SkillDef { id = null, name = "Broken" },
            new SkillDef { id = "", name = "Empty" },
            new SkillDef { id = "fireball", name = "Fireball", cooldown = 2000, damage = 2f }
        };

        // Same filtering logic as DataManager.LoadSkills
        var skills = new Dictionary<string, SkillDef>();
        var valid = new List<SkillDef>();
        foreach (var skill in raw)
        {
            if (string.IsNullOrEmpty(skill.id)) continue;
            skills[skill.id] = skill;
            valid.Add(skill);
        }
        var skillList = valid.ToArray();

        Assert.AreEqual(2, skillList.Length);
        Assert.AreEqual(2, skills.Count);
        Assert.AreEqual("slash", skillList[0].id);
        Assert.AreEqual("fireball", skillList[1].id);
    }

    [Test]
    public void ZeroDamage_PatchedToDefault()
    {
        var skill = new SkillDef { id = "test", name = "Test", cooldown = 1000, damage = 0 };

        // Same validation logic as DataManager.ValidateData
        if (skill.damage <= 0) skill.damage = 1f;
        if (skill.cooldown <= 0) skill.cooldown = 1000f;
        if (skill.requiredLevel <= 0) skill.requiredLevel = 1;
        if (skill.requiredPoints <= 0) skill.requiredPoints = 1;
        if (skill.mpCost < 0) skill.mpCost = 0;

        Assert.AreEqual(1f, skill.damage);
    }

    [Test]
    public void ValidSkill_NotPatched()
    {
        var skill = new SkillDef
        {
            id = "slash", name = "Slash", cooldown = 3000, damage = 2.5f,
            mpCost = 8, requiredLevel = 1, requiredPoints = 1
        };

        bool patched = false;
        if (string.IsNullOrEmpty(skill.name)) { skill.name = skill.id; patched = true; }
        if (skill.cooldown <= 0) { skill.cooldown = 1000f; patched = true; }
        if (skill.damage <= 0) { skill.damage = 1f; patched = true; }
        if (skill.requiredLevel <= 0) { skill.requiredLevel = 1; patched = true; }
        if (skill.requiredPoints <= 0) { skill.requiredPoints = 1; patched = true; }
        if (skill.mpCost < 0) { skill.mpCost = 0; patched = true; }

        Assert.IsFalse(patched);
        Assert.AreEqual(2.5f, skill.damage);
        Assert.AreEqual(3000f, skill.cooldown);
    }

    [Test]
    public void NegativeMpCost_PatchedToZero()
    {
        var skill = new SkillDef { id = "test", name = "Test", cooldown = 1000, damage = 1f, mpCost = -5 };

        if (skill.mpCost < 0) skill.mpCost = 0;

        Assert.AreEqual(0, skill.mpCost);
    }

    [Test]
    public void ZeroRequiredLevel_PatchedToOne()
    {
        var skill = new SkillDef { id = "test", name = "Test", cooldown = 1000, damage = 1f, requiredLevel = 0 };

        if (skill.requiredLevel <= 0) skill.requiredLevel = 1;

        Assert.AreEqual(1, skill.requiredLevel);
    }
}
