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

    class TestTarget
    {
        public Vector2 pos;
        public bool alive;
    }
}
