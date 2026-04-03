using System.Collections.Generic;
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

    // --- New tests: edge cases ---

    [Test]
    public void GetSlot_NegativeIndex_ReturnsNull()
    {
        Assert.IsNull(inv.GetSlot(-1));
    }

    [Test]
    public void GetSlot_OutOfBounds_ReturnsNull()
    {
        Assert.IsNull(inv.GetSlot(999));
    }

    [Test]
    public void RemoveAtSlot_InvalidIndex_ReturnsNull()
    {
        Assert.IsNull(inv.RemoveAtSlot(-1));
        Assert.IsNull(inv.RemoveAtSlot(999));
    }

    [Test]
    public void RemoveAtSlot_EmptySlot_ReturnsNull()
    {
        Assert.IsNull(inv.RemoveAtSlot(0));
    }

    [Test]
    public void SwapSlots_OutOfBounds_DoesNothing()
    {
        inv.AddItem("sword", 1, false, 1);
        inv.SwapSlots(0, -1);
        Assert.AreEqual("sword", inv.GetSlot(0).itemId);
    }

    [Test]
    public void SwapSlots_WithEmptySlot_MovesItem()
    {
        inv.AddItem("sword", 1, false, 1);
        inv.SwapSlots(0, 5);
        Assert.IsNull(inv.GetSlot(0));
        Assert.AreEqual("sword", inv.GetSlot(5).itemId);
    }

    [Test]
    public void RemoveItem_AcrossMultipleStacks()
    {
        inv.AddItem("wood", 99, true, 99);
        inv.AddItem("wood", 50, true, 99);
        Assert.IsTrue(inv.RemoveItem("wood", 120));
        Assert.AreEqual(29, inv.GetCount("wood"));
    }

    [Test]
    public void RemoveItem_ClearsSlotWhenCountReachesZero()
    {
        inv.AddItem("wood", 5, true, 99);
        inv.RemoveItem("wood", 5);
        Assert.AreEqual(0, inv.OccupiedSlots);
        Assert.IsNull(inv.GetSlot(0));
    }

    [Test]
    public void GetCount_NonexistentItem_ReturnsZero()
    {
        Assert.AreEqual(0, inv.GetCount("nonexistent"));
    }

    [Test]
    public void RemoveItem_NonexistentItem_ReturnsFalse()
    {
        Assert.IsFalse(inv.RemoveItem("nonexistent", 1));
    }

    // --- New tests: SortItems ---

    [Test]
    public void SortItems_ByGradeDescending()
    {
        inv.AddItem("common_sword", 1, false, 1);
        inv.AddItem("rare_sword", 1, false, 1);
        inv.AddItem("legendary_staff", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["common_sword"] = new ItemDef { name = "Iron Sword", type = "weapon", grade = "common" },
            ["rare_sword"] = new ItemDef { name = "Fire Sword", type = "weapon", grade = "rare" },
            ["legendary_staff"] = new ItemDef { name = "Arc Staff", type = "weapon", grade = "legendary" }
        };
        inv.SortItems(defs);
        Assert.AreEqual("legendary_staff", inv.GetSlot(0).itemId);
        Assert.AreEqual("rare_sword", inv.GetSlot(1).itemId);
        Assert.AreEqual("common_sword", inv.GetSlot(2).itemId);
    }

    [Test]
    public void SortItems_SameGrade_SortsByTypeThenName()
    {
        inv.AddItem("potion_hp", 1, false, 1);
        inv.AddItem("iron_sword", 1, false, 1);
        inv.AddItem("wood", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["potion_hp"] = new ItemDef { name = "HP Potion", type = "potion", grade = "common" },
            ["iron_sword"] = new ItemDef { name = "Iron Sword", type = "weapon", grade = "common" },
            ["wood"] = new ItemDef { name = "Wood", type = "material", grade = "common" }
        };
        inv.SortItems(defs);
        Assert.AreEqual("wood", inv.GetSlot(0).itemId);
        Assert.AreEqual("potion_hp", inv.GetSlot(1).itemId);
        Assert.AreEqual("iron_sword", inv.GetSlot(2).itemId);
    }

    [Test]
    public void SortItems_CompactsEmptySlots()
    {
        inv.AddItem("a", 1, false, 1);
        inv.AddItem("b", 1, false, 1);
        inv.RemoveAtSlot(0);

        var defs = new Dictionary<string, ItemDef>
        {
            ["b"] = new ItemDef { name = "B", type = "material", grade = "common" }
        };
        inv.SortItems(defs);
        Assert.AreEqual("b", inv.GetSlot(0).itemId);
        Assert.IsNull(inv.GetSlot(1));
    }

    // --- New tests: GetFiltered ---

    [Test]
    public void GetFiltered_All_ReturnsEverything()
    {
        inv.AddItem("sword", 1, false, 1);
        inv.AddItem("potion", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["sword"] = new ItemDef { name = "Sword", type = "weapon", grade = "common" },
            ["potion"] = new ItemDef { name = "Potion", type = "potion", grade = "common" }
        };
        var result = inv.GetFiltered(defs, "all", 0);
        Assert.AreEqual(2, result.Length);
    }

    [Test]
    public void GetFiltered_WeaponFilter_ReturnsOnlyWeapons()
    {
        inv.AddItem("sword", 1, false, 1);
        inv.AddItem("potion", 1, false, 1);
        inv.AddItem("wood", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["sword"] = new ItemDef { name = "Sword", type = "weapon", grade = "common" },
            ["potion"] = new ItemDef { name = "Potion", type = "potion", grade = "common" },
            ["wood"] = new ItemDef { name = "Wood", type = "material", grade = "common" }
        };
        var result = inv.GetFiltered(defs, "weapon", 0);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("sword", result[0].itemId);
    }

    [Test]
    public void GetFiltered_ArmorFilter_IncludesAllArmorTypes()
    {
        inv.AddItem("helm", 1, false, 1);
        inv.AddItem("plate", 1, false, 1);
        inv.AddItem("boot", 1, false, 1);
        inv.AddItem("ring", 1, false, 1);
        inv.AddItem("sword", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["helm"] = new ItemDef { name = "Helm", type = "helmet", grade = "common" },
            ["plate"] = new ItemDef { name = "Plate", type = "armor", grade = "common" },
            ["boot"] = new ItemDef { name = "Boot", type = "boots", grade = "common" },
            ["ring"] = new ItemDef { name = "Ring", type = "accessory", grade = "common" },
            ["sword"] = new ItemDef { name = "Sword", type = "weapon", grade = "common" }
        };
        var result = inv.GetFiltered(defs, "armor", 0);
        Assert.AreEqual(4, result.Length);
    }

    [Test]
    public void GetFiltered_ConsumableFilter()
    {
        inv.AddItem("hp_pot", 1, false, 1);
        inv.AddItem("apple", 1, false, 1);
        inv.AddItem("wood", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["hp_pot"] = new ItemDef { name = "HP Potion", type = "potion", grade = "common" },
            ["apple"] = new ItemDef { name = "Apple", type = "food", grade = "common" },
            ["wood"] = new ItemDef { name = "Wood", type = "material", grade = "common" }
        };
        var result = inv.GetFiltered(defs, "consumable", 0);
        Assert.AreEqual(2, result.Length);
    }

    [Test]
    public void GetFiltered_SortByGrade()
    {
        inv.AddItem("common_item", 1, false, 1);
        inv.AddItem("rare_item", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["common_item"] = new ItemDef { name = "Common", type = "material", grade = "common" },
            ["rare_item"] = new ItemDef { name = "Rare", type = "material", grade = "rare" }
        };
        var result = inv.GetFiltered(defs, "all", 1);
        Assert.AreEqual("rare_item", result[0].itemId);
        Assert.AreEqual("common_item", result[1].itemId);
    }

    [Test]
    public void GetFiltered_SortReverse()
    {
        inv.AddItem("a", 1, false, 1);
        inv.AddItem("b", 1, false, 1);

        var defs = new Dictionary<string, ItemDef>
        {
            ["a"] = new ItemDef { name = "Alpha", type = "material", grade = "common" },
            ["b"] = new ItemDef { name = "Beta", type = "material", grade = "common" }
        };
        var result = inv.GetFiltered(defs, "all", 3);
        Assert.AreEqual("b", result[0].itemId);
        Assert.AreEqual("a", result[1].itemId);
    }

    [Test]
    public void GetFiltered_EmptyInventory_ReturnsEmpty()
    {
        var defs = new Dictionary<string, ItemDef>();
        var result = inv.GetFiltered(defs, "all", 0);
        Assert.AreEqual(0, result.Length);
    }
}
