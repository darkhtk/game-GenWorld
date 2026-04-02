using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class SaveSystemTests
{
    string _tempPath;

    [SetUp]
    public void Setup()
    {
        _tempPath = Path.Combine(Application.persistentDataPath, "rpg_save.json");
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [TearDown]
    public void Teardown()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Test]
    public void HasSave_False_WhenNoFile()
    {
        Assert.IsFalse(SaveSystem.HasSave());
    }

    [Test]
    public void SaveAndLoad_Roundtrip()
    {
        var data = new SaveData
        {
            playerX = 100f, playerY = -200f,
            level = 5, xp = 42, gold = 300,
            skillPoints = 4, statPoints = 6,
            hp = 80, mp = 40,
            totalKills = 10
        };
        SaveSystem.Save(data);
        Assert.IsTrue(SaveSystem.HasSave());

        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded);
        Assert.AreEqual(100f, loaded.playerX);
        Assert.AreEqual(-200f, loaded.playerY);
        Assert.AreEqual(5, loaded.level);
        Assert.AreEqual(42, loaded.xp);
        Assert.AreEqual(300, loaded.gold);
        Assert.AreEqual(10, loaded.totalKills);
    }

    [Test]
    public void DeleteSave_RemovesFile()
    {
        SaveSystem.Save(new SaveData { level = 1 });
        Assert.IsTrue(SaveSystem.HasSave());
        SaveSystem.DeleteSave();
        Assert.IsFalse(SaveSystem.HasSave());
    }

    [Test]
    public void Save_SetsTimestamp()
    {
        SaveSystem.Save(new SaveData { level = 1 });
        var loaded = SaveSystem.Load();
        Assert.Greater(loaded.timestamp, 0);
    }

    [Test]
    public void SaveAndLoad_PreservesInventory()
    {
        var data = new SaveData
        {
            level = 1,
            inventory = new[]
            {
                new ItemInstance { itemId = "wood", count = 5 },
                null,
                new ItemInstance { itemId = "sword", count = 1, enhanceLevel = 3 }
            }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.AreEqual(3, loaded.inventory.Length);
        Assert.AreEqual("wood", loaded.inventory[0].itemId);
        Assert.AreEqual(5, loaded.inventory[0].count);
        Assert.IsNull(loaded.inventory[1]);
        Assert.AreEqual(3, loaded.inventory[2].enhanceLevel);
    }

    [Test]
    public void SaveAndLoad_PreservesEquipment()
    {
        var data = new SaveData
        {
            level = 1,
            equipment = new Dictionary<string, ItemInstance>
            {
                ["weapon"] = new ItemInstance { itemId = "iron_sword", count = 1 },
                ["helmet"] = null
            }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded.equipment);
        Assert.AreEqual("iron_sword", loaded.equipment["weapon"].itemId);
    }

    [Test]
    public void Load_ReturnsNull_WhenNoFile()
    {
        Assert.IsNull(SaveSystem.Load());
    }
}
