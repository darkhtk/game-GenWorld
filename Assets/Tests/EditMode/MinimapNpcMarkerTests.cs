using NUnit.Framework;
using UnityEngine;

// S-127: minimap NPC marker color classification — shop / quest / default.
public class MinimapNpcMarkerTests
{
    [Test]
    public void ShopAction_ReturnsShopColor()
    {
        var def = new NpcDef { id = "merchant", actions = new[] { "open_shop" } };
        Assert.AreEqual(MinimapUI.NpcMarkerShop,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: false));
    }

    [Test]
    public void ShopAction_TakesPrecedenceOverQuest()
    {
        var def = new NpcDef { id = "innkeeper", actions = new[] { "heal_player", "open_shop" } };
        Assert.AreEqual(MinimapUI.NpcMarkerShop,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: true));
    }

    [Test]
    public void QuestAvailable_ReturnsQuestColor()
    {
        var def = new NpcDef { id = "elder", actions = new[] { "reset_skills" } };
        Assert.AreEqual(MinimapUI.NpcMarkerQuest,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: true));
    }

    [Test]
    public void NoActionsNoQuest_ReturnsDefault()
    {
        var def = new NpcDef { id = "guard", actions = System.Array.Empty<string>() };
        Assert.AreEqual(MinimapUI.NpcMarkerDefault,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: false));
    }

    [Test]
    public void NullDef_ReturnsDefault()
    {
        Assert.AreEqual(MinimapUI.NpcMarkerDefault,
            MinimapUI.ClassifyNpcMarkerColor(null, hasQuestForNpc: true));
    }

    [Test]
    public void NullActions_FallsThroughToQuestCheck()
    {
        var def = new NpcDef { id = "scholar", actions = null };
        Assert.AreEqual(MinimapUI.NpcMarkerQuest,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: true));
        Assert.AreEqual(MinimapUI.NpcMarkerDefault,
            MinimapUI.ClassifyNpcMarkerColor(def, hasQuestForNpc: false));
    }

    [Test]
    public void MarkerColors_AreVisuallyDistinct()
    {
        Assert.AreNotEqual(MinimapUI.NpcMarkerDefault, MinimapUI.NpcMarkerShop);
        Assert.AreNotEqual(MinimapUI.NpcMarkerDefault, MinimapUI.NpcMarkerQuest);
        Assert.AreNotEqual(MinimapUI.NpcMarkerShop, MinimapUI.NpcMarkerQuest);
    }
}
