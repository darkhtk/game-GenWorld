using NUnit.Framework;

public class InventorySystemTests
{
    InventorySystem inv;

    [SetUp]
    public void Setup() => inv = new InventorySystem(20);

    [Test]
    public void AddItem_Stackable_FillsExistingStack()
    {
        inv.AddItem("wood", 5, true, 99);
        inv.AddItem("wood", 3, true, 99);
        Assert.AreEqual(8, inv.GetCount("wood"));
        Assert.AreEqual(1, inv.OccupiedSlots);
    }

    [Test]
    public void AddItem_NonStackable_UsesOneSlotEach()
    {
        inv.AddItem("sword", 1, false, 1);
        inv.AddItem("sword", 1, false, 1);
        Assert.AreEqual(2, inv.GetCount("sword"));
        Assert.AreEqual(2, inv.OccupiedSlots);
    }

    [Test]
    public void AddItem_Full_ReturnsOverflow()
    {
        var small = new InventorySystem(2);
        small.AddItem("a", 1, false, 1);
        small.AddItem("b", 1, false, 1);
        int overflow = small.AddItem("c", 1, false, 1);
        Assert.AreEqual(1, overflow);
    }

    [Test]
    public void RemoveItem_Succeeds_WhenEnough()
    {
        inv.AddItem("wood", 10, true, 99);
        Assert.IsTrue(inv.RemoveItem("wood", 7));
        Assert.AreEqual(3, inv.GetCount("wood"));
    }

    [Test]
    public void RemoveItem_Fails_WhenNotEnough()
    {
        inv.AddItem("wood", 3, true, 99);
        Assert.IsFalse(inv.RemoveItem("wood", 5));
        Assert.AreEqual(3, inv.GetCount("wood"));
    }

    [Test]
    public void SwapSlots_SwapsItems()
    {
        inv.AddItem("a", 1, false, 1);
        inv.AddItem("b", 1, false, 1);
        inv.SwapSlots(0, 1);
        Assert.AreEqual("b", inv.GetSlot(0).itemId);
        Assert.AreEqual("a", inv.GetSlot(1).itemId);
    }

    [Test]
    public void HasItems_ReturnsCorrectly()
    {
        inv.AddItem("wood", 5, true, 99);
        Assert.IsTrue(inv.HasItems("wood", 5));
        Assert.IsFalse(inv.HasItems("wood", 6));
        Assert.IsFalse(inv.HasItems("stone", 1));
    }

    [Test]
    public void RemoveAtSlot_ReturnsItem_AndClearsSlot()
    {
        inv.AddItem("sword", 1, false, 1);
        var item = inv.RemoveAtSlot(0);
        Assert.IsNotNull(item);
        Assert.AreEqual("sword", item.itemId);
        Assert.IsNull(inv.GetSlot(0));
    }

    [Test]
    public void AddItem_Stackable_OverflowsToNewSlot()
    {
        inv.AddItem("wood", 99, true, 99);
        inv.AddItem("wood", 10, true, 99);
        Assert.AreEqual(109, inv.GetCount("wood"));
        Assert.AreEqual(2, inv.OccupiedSlots);
    }
}
