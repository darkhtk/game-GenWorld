using NUnit.Framework;

public class LootSystemTests
{
    [Test]
    public void RollDrops_GuaranteedDrop_AlwaysDrops()
    {
        var drops = new[] { new DropEntry { itemId = "wood", chance = 1f, minCount = 1, maxCount = 3 } };
        var results = LootSystem.RollDrops(drops, () => 0f);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("wood", results[0].itemId);
        Assert.GreaterOrEqual(results[0].count, 1);
    }

    [Test]
    public void RollDrops_ZeroChance_NeverDrops()
    {
        var drops = new[] { new DropEntry { itemId = "rare", chance = 0f, minCount = 1, maxCount = 1 } };
        var results = LootSystem.RollDrops(drops, () => 0.5f);
        Assert.AreEqual(0, results.Count);
    }

    [Test]
    public void RollDrops_MultipleEntries_RollsEach()
    {
        var drops = new[] {
            new DropEntry { itemId = "a", chance = 1f, minCount = 1, maxCount = 1 },
            new DropEntry { itemId = "b", chance = 1f, minCount = 2, maxCount = 2 }
        };
        var results = LootSystem.RollDrops(drops, () => 0f);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("a", results[0].itemId);
        Assert.AreEqual("b", results[1].itemId);
        Assert.AreEqual(2, results[1].count);
    }

    [Test]
    public void RollDrops_NullDrops_ReturnsEmpty()
    {
        var results = LootSystem.RollDrops(null);
        Assert.AreEqual(0, results.Count);
    }

    [Test]
    public void RollDrops_PartialChance_FiltersCorrectly()
    {
        int callCount = 0;
        var drops = new[] {
            new DropEntry { itemId = "a", chance = 0.5f, minCount = 1, maxCount = 1 },
            new DropEntry { itemId = "b", chance = 0.5f, minCount = 1, maxCount = 1 }
        };
        // First roll = 0.3 (< 0.5, drops), second roll = 0.7 (>= 0.5, no drop)
        var results = LootSystem.RollDrops(drops, () => callCount++ == 0 ? 0.3f : 0.7f);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("a", results[0].itemId);
    }
}
