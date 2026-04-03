using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public class SaveSystemTests
{
    string _tempPath;

    [SetUp]
    public void Setup()
    {
        _tempPath = Path.Combine(Application.persistentDataPath, "rpg_save.json");
        SaveSystem.DeleteSave();
    }

    [TearDown]
    public void Teardown()
    {
        SaveSystem.DeleteSave();
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

    // --- New tests: complex data roundtrip ---

    [Test]
    public void SaveAndLoad_PreservesLearnedSkills()
    {
        var data = new SaveData
        {
            level = 1,
            learnedSkills = new Dictionary<string, int>
            {
                ["fireball"] = 3,
                ["heal"] = 1
            }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded.learnedSkills);
        Assert.AreEqual(3, loaded.learnedSkills["fireball"]);
        Assert.AreEqual(1, loaded.learnedSkills["heal"]);
    }

    [Test]
    public void SaveAndLoad_PreservesEquippedSkills()
    {
        var data = new SaveData
        {
            level = 1,
            equippedSkills = new[] { "fireball", "heal", null, "slash" }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.AreEqual(4, loaded.equippedSkills.Length);
        Assert.AreEqual("fireball", loaded.equippedSkills[0]);
        Assert.IsNull(loaded.equippedSkills[2]);
    }

    [Test]
    public void SaveAndLoad_PreservesQuestState()
    {
        var data = new SaveData
        {
            level = 1,
            questState = new QuestSaveData
            {
                active = new[] { "quest_01", "quest_03" },
                completed = new[] { "quest_00" }
            }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded.questState);
        Assert.AreEqual(2, loaded.questState.active.Length);
        Assert.AreEqual("quest_01", loaded.questState.active[0]);
        Assert.AreEqual(1, loaded.questState.completed.Length);
    }

    [Test]
    public void SaveAndLoad_PreservesKillCountsAndBonusStats()
    {
        var data = new SaveData
        {
            level = 1,
            killCounts = new Dictionary<string, int> { ["slime"] = 50, ["wolf"] = 12 },
            bonusStats = new Dictionary<string, int> { ["atk"] = 5, ["def"] = 3 },
            firedTriggers = new[] { "boss_defeated", "first_quest" }
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.AreEqual(50, loaded.killCounts["slime"]);
        Assert.AreEqual(5, loaded.bonusStats["atk"]);
        Assert.AreEqual(2, loaded.firedTriggers.Length);
    }

    // --- New tests: backup recovery ---

    [Test]
    public void Load_RecoversFromBackup_WhenPrimaryCorrupted()
    {
        SaveSystem.Save(new SaveData { level = 7, gold = 999 });

        File.WriteAllText(_tempPath, "CORRUPTED DATA{{{{");

        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded);
        Assert.AreEqual(7, loaded.level);
        Assert.AreEqual(999, loaded.gold);
    }

    [Test]
    public void Load_ReturnsNull_WhenAllFilesCorrupted()
    {
        SaveSystem.Save(new SaveData { level = 1 });

        File.WriteAllText(_tempPath, "CORRUPTED");
        for (int i = 1; i <= 3; i++)
        {
            string bakPath = Path.Combine(Application.persistentDataPath, $"rpg_save.bak{i}.json");
            if (File.Exists(bakPath))
                File.WriteAllText(bakPath, "CORRUPTED");
        }

        Assert.IsNull(SaveSystem.Load());
    }

    // --- New tests: checksum validation ---

    [Test]
    public void Load_RejectsFile_WhenChecksumTampered()
    {
        SaveSystem.Save(new SaveData { level = 5 });

        string json = File.ReadAllText(_tempPath);
        var root = JObject.Parse(json);
        root["checksum"] = "0000000000000000000000000000000000000000000000000000000000000000";
        File.WriteAllText(_tempPath, root.ToString());

        for (int i = 1; i <= 3; i++)
        {
            string bakPath = Path.Combine(Application.persistentDataPath, $"rpg_save.bak{i}.json");
            if (File.Exists(bakPath)) File.Delete(bakPath);
        }

        Assert.IsNull(SaveSystem.Load());
    }

    // --- New tests: SaveMigrations ---

    [Test]
    public void Migrations_Apply_V0ToV1_ReturnsData()
    {
        var data = new JObject { ["level"] = 3, ["gold"] = 100 };
        var migrated = SaveMigrations.Apply(0, 1, data);
        Assert.IsNotNull(migrated);
        Assert.AreEqual(3, migrated["level"].Value<int>());
    }

    [Test]
    public void Migrations_Apply_SameVersion_NoChange()
    {
        var data = new JObject { ["level"] = 5 };
        var migrated = SaveMigrations.Apply(1, 1, data);
        Assert.AreEqual(5, migrated["level"].Value<int>());
    }

    [Test]
    public void Migrations_Apply_MissingMigration_StillReturnsData()
    {
        var data = new JObject { ["level"] = 5 };
        var migrated = SaveMigrations.Apply(99, 100, data);
        Assert.IsNotNull(migrated);
        Assert.AreEqual(5, migrated["level"].Value<int>());
    }

    // --- New tests: multiple saves create backups ---

    [Test]
    public void Save_CreatesBackupFiles()
    {
        SaveSystem.Save(new SaveData { level = 1 });
        SaveSystem.Save(new SaveData { level = 2 });

        string bak1 = Path.Combine(Application.persistentDataPath, "rpg_save.bak1.json");
        Assert.IsTrue(File.Exists(bak1));
    }

    [Test]
    public void SaveAndLoad_PreservesNullOptionalFields()
    {
        var data = new SaveData
        {
            level = 1,
            inventory = null,
            equipment = null,
            learnedSkills = null,
            questState = null
        };
        SaveSystem.Save(data);
        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.level);
    }
}
