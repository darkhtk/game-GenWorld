using NUnit.Framework;
using System.Collections.Generic;

public class CraftingSystemTests
{
    CraftingSystem crafting;
    InventorySystem inv;

    [SetUp]
    public void Setup()
    {
        var recipes = new[] {
            new RecipeDef {
                resultId = "wooden_sword",
                materials = new[] {
                    new RecipeMaterial { itemId = "wood", count = 5 }
                }
            },
            new RecipeDef {
                resultId = "iron_sword",
                materials = new[] {
                    new RecipeMaterial { itemId = "iron", count = 3 },
                    new RecipeMaterial { itemId = "wood", count = 2 }
                }
            }
        };
        var itemDefs = new Dictionary<string, ItemDef>
        {
            ["wooden_sword"] = new ItemDef { id = "wooden_sword", stackable = false, maxStack = 1 },
            ["iron_sword"] = new ItemDef { id = "iron_sword", stackable = false, maxStack = 1 },
            ["wood"] = new ItemDef { id = "wood", stackable = true, maxStack = 99 },
            ["iron"] = new ItemDef { id = "iron", stackable = true, maxStack = 99 }
        };
        crafting = new CraftingSystem(recipes, itemDefs);
        inv = new InventorySystem(20);
    }

    [Test]
    public void CanCraft_True_WhenMaterialsPresent()
    {
        inv.AddItem("wood", 5, true, 99);
        Assert.IsTrue(crafting.CanCraft("wooden_sword", inv));
    }

    [Test]
    public void CanCraft_False_WhenMaterialsMissing()
    {
        inv.AddItem("wood", 3, true, 99);
        Assert.IsFalse(crafting.CanCraft("wooden_sword", inv));
    }

    [Test]
    public void Craft_ConsumesMaterials_AddsResult()
    {
        inv.AddItem("wood", 10, true, 99);
        Assert.IsTrue(crafting.Craft("wooden_sword", inv));
        Assert.AreEqual(5, inv.GetCount("wood"));
        Assert.AreEqual(1, inv.GetCount("wooden_sword"));
    }

    [Test]
    public void Craft_Fails_WhenNotEnoughMaterials()
    {
        inv.AddItem("wood", 2, true, 99);
        Assert.IsFalse(crafting.Craft("wooden_sword", inv));
        Assert.AreEqual(2, inv.GetCount("wood"));
    }

    [Test]
    public void GetAvailableRecipes_FiltersCorrectly()
    {
        inv.AddItem("wood", 5, true, 99);
        var available = crafting.GetAvailableRecipes(inv);
        Assert.AreEqual(1, available.Length);
        Assert.AreEqual("wooden_sword", available[0].resultId);
    }

    [Test]
    public void Craft_MultiMaterial_ConsumesAll()
    {
        inv.AddItem("iron", 5, true, 99);
        inv.AddItem("wood", 5, true, 99);
        Assert.IsTrue(crafting.Craft("iron_sword", inv));
        Assert.AreEqual(2, inv.GetCount("iron"));
        Assert.AreEqual(3, inv.GetCount("wood"));
    }
}
