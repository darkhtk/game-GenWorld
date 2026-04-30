using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// S-123: empty inventory slot bg dims to GameConfig.UI.InventorySlotEmptyAlpha (0.3)
// for visible "available drop target" affordance while keeping the grid structure
// readable. Filled slots restore to InventorySlotFilledAlpha (1.0).
public class InventorySlotEmptyAlphaTests
{
    [Test]
    public void EmptyAlpha_LessThan_FilledAlpha()
    {
        Assert.That(GameConfig.UI.InventorySlotEmptyAlpha,
            Is.LessThan(GameConfig.UI.InventorySlotFilledAlpha),
            "Empty slot must be visually distinct from filled — alpha must be lower.");
    }

    [Test]
    public void EmptyAlpha_InRange()
    {
        Assert.That(GameConfig.UI.InventorySlotEmptyAlpha,
            Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
    }

    [Test]
    public void FilledAlpha_IsOpaque()
    {
        Assert.AreEqual(1f, GameConfig.UI.InventorySlotFilledAlpha);
    }

    [Test]
    public void EmptyAlpha_StaysLegible()
    {
        // 0 would hide the slot entirely; 0.5 would barely differ from filled.
        // Aim for the ghost-tier band 0.2..0.4.
        Assert.That(GameConfig.UI.InventorySlotEmptyAlpha, Is.GreaterThanOrEqualTo(0.2f));
        Assert.That(GameConfig.UI.InventorySlotEmptyAlpha, Is.LessThanOrEqualTo(0.4f));
    }

    [Test]
    public void Clear_DimsBgImageAlpha()
    {
        var go = new GameObject("slot_test_clear");
        try
        {
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var slot = go.AddComponent<InventorySlotUI>();
            slot.Clear();
            Assert.AreEqual(GameConfig.UI.InventorySlotEmptyAlpha, img.color.a, 0.0001f,
                "Clear() must dim the slot background to InventorySlotEmptyAlpha.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SetItem_RestoresBgImageAlpha()
    {
        var go = new GameObject("slot_test_setitem");
        try
        {
            var img = go.AddComponent<Image>();
            var slot = go.AddComponent<InventorySlotUI>();
            slot.Clear();
            Assert.AreEqual(GameConfig.UI.InventorySlotEmptyAlpha, img.color.a, 0.0001f);

            slot.SetItem("dummy", 1, 0, Color.white, ItemType.Material, null, null);
            Assert.AreEqual(GameConfig.UI.InventorySlotFilledAlpha, img.color.a, 0.0001f,
                "SetItem() must restore bg alpha to InventorySlotFilledAlpha.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
