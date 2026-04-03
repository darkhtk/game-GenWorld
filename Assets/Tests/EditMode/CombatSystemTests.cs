using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CombatSystemTests
{
    [Test]
    public void CalcDamage_NoCrit_ReturnsAtkMinusDef()
    {
        Assert.AreEqual(8, CombatSystem.CalcDamage(10, 2, false));
    }

    [Test]
    public void CalcDamage_MinimumOne()
    {
        Assert.AreEqual(1, CombatSystem.CalcDamage(1, 100, false));
    }

    [Test]
    public void CalcDamage_Crit_Doubles()
    {
        Assert.AreEqual(16, CombatSystem.CalcDamage(10, 2, true));
    }

    [Test]
    public void CalcDamage_EqualAtkDef_ReturnsOne()
    {
        Assert.AreEqual(1, CombatSystem.CalcDamage(5, 5, false));
    }

    [Test]
    public void CalcDamage_ZeroAtk_ReturnsOne()
    {
        Assert.AreEqual(1, CombatSystem.CalcDamage(0, 10, false));
    }

    [Test]
    public void CalcDamage_CritMinimum_ReturnsTwo()
    {
        Assert.AreEqual(2, CombatSystem.CalcDamage(1, 100, true));
    }

    [Test]
    public void CalcCrit_HighChance_ReturnsTrue()
    {
        Assert.IsTrue(CombatSystem.CalcCrit(100, () => 0.5f));
    }

    [Test]
    public void CalcCrit_ZeroChance_ReturnsFalse()
    {
        Assert.IsFalse(CombatSystem.CalcCrit(0, () => 0.5f));
    }

    [Test]
    public void CalcCrit_BoundaryExact_ReturnsFalse()
    {
        Assert.IsFalse(CombatSystem.CalcCrit(50, () => 0.5f));
    }

    [Test]
    public void CalcCrit_BoundaryJustBelow_ReturnsTrue()
    {
        Assert.IsTrue(CombatSystem.CalcCrit(50, () => 0.499f));
    }

    [Test]
    public void FindClosest_FindsNearestAlive()
    {
        var targets = new[] {
            new TestTarget { pos = new Vector2(100, 0), alive = true },
            new TestTarget { pos = new Vector2(50, 0), alive = true },
            new TestTarget { pos = new Vector2(30, 0), alive = false }
        };
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 200f,
            t => t.pos, t => t.alive);
        Assert.AreEqual(targets[1], result);
    }

    [Test]
    public void FindClosest_ReturnsNull_WhenOutOfRange()
    {
        var targets = new[] {
            new TestTarget { pos = new Vector2(500, 0), alive = true }
        };
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 100f,
            t => t.pos, t => t.alive);
        Assert.IsNull(result);
    }

    [Test]
    public void FindClosest_ReturnsNull_WhenAllDead()
    {
        var targets = new[] {
            new TestTarget { pos = new Vector2(10, 0), alive = false },
            new TestTarget { pos = new Vector2(20, 0), alive = false }
        };
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 200f,
            t => t.pos, t => t.alive);
        Assert.IsNull(result);
    }

    [Test]
    public void FindClosest_ReturnsNull_WhenEmpty()
    {
        var targets = new TestTarget[0];
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 200f,
            t => t.pos, t => t.alive);
        Assert.IsNull(result);
    }

    [Test]
    public void FindClosest_ListOverload_FindsNearest()
    {
        var targets = new List<TestTarget>
        {
            new TestTarget { pos = new Vector2(100, 0), alive = true },
            new TestTarget { pos = new Vector2(30, 0), alive = true }
        };
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 200f,
            t => t.pos, t => t.alive);
        Assert.AreEqual(targets[1], result);
    }

    [Test]
    public void FindClosest_ListOverload_ReturnsNull_WhenEmpty()
    {
        var targets = new List<TestTarget>();
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 200f,
            t => t.pos, t => t.alive);
        Assert.IsNull(result);
    }

    [Test]
    public void FindClosest_ListOverload_ReturnsNull_WhenOutOfRange()
    {
        var targets = new List<TestTarget>
        {
            new TestTarget { pos = new Vector2(500, 0), alive = true }
        };
        var result = CombatSystem.FindClosest(
            Vector2.zero, targets, 100f,
            t => t.pos, t => t.alive);
        Assert.IsNull(result);
    }

    class TestTarget
    {
        public Vector2 pos;
        public bool alive;
    }
}
