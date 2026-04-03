using NUnit.Framework;

public class QuestSystemTests
{
    QuestSystem qs;
    InventorySystem inv;

    [SetUp]
    public void Setup()
    {
        EventBus.Clear();
        var quests = new[] {
            new QuestDef {
                id = "q1", npcId = "hunter", title = "Wolf Leather",
                description = "Bring 5 leather",
                requirements = new[] { new QuestRequirement { itemId = "leather", count = 5 } },
                rewards = new QuestReward { gold = 50, xp = 30, items = new QuestRewardItem[0] }
            }
        };
        qs = new QuestSystem(quests);
        inv = new InventorySystem(20);
    }

    [Test]
    public void AcceptQuest_MovesToActive()
    {
        Assert.IsTrue(qs.AcceptQuest("q1"));
        Assert.AreEqual(1, qs.GetActiveQuests().Length);
    }

    [Test]
    public void AcceptQuest_FailsIfAlreadyActive()
    {
        qs.AcceptQuest("q1");
        Assert.IsFalse(qs.AcceptQuest("q1"));
    }

    [Test]
    public void CompleteQuest_FailsWithoutItems()
    {
        qs.AcceptQuest("q1");
        Assert.IsNull(qs.CompleteQuest("q1", inv));
    }

    [Test]
    public void CompleteQuest_SucceedsWithItems()
    {
        qs.AcceptQuest("q1");
        inv.AddItem("leather", 5, true, 99);
        var reward = qs.CompleteQuest("q1", inv);
        Assert.IsNotNull(reward);
        Assert.AreEqual(50, reward.gold);
        Assert.AreEqual(0, inv.GetCount("leather"));
    }

    [Test]
    public void GetQuestStatusForNpc_ReturnsAvailable()
    {
        var result = qs.GetQuestStatusForNpc("hunter", inv);
        Assert.IsNotNull(result);
        Assert.AreEqual("available", result.Value.status);
    }

    [Test]
    public void GetScaledRewards_Tier1_Multiplies()
    {
        var scaled = qs.GetScaledRewards(qs.GetQuestDefs()[0], 1, 0);
        Assert.AreEqual(75, scaled.gold); // 50 * 1.5
    }

    [Test]
    public void SerializeRestore_PreservesState()
    {
        qs.AcceptQuest("q1");
        var data = qs.Serialize();
        var qs2 = new QuestSystem(new[] { new QuestDef {
            id = "q1", npcId = "hunter",
            requirements = new QuestRequirement[0],
            rewards = new QuestReward { gold = 0, xp = 0, items = new QuestRewardItem[0] }
        } });
        qs2.Restore(data);
        Assert.AreEqual(1, qs2.GetActiveQuests().Length);
    }

    [Test]
    public void GetStatus_ReturnsCompletable_WhenItemsMet()
    {
        qs.AcceptQuest("q1");
        inv.AddItem("leather", 5, true, 99);
        Assert.AreEqual("completable", qs.GetStatus("q1", inv));
    }

    // Kill quest tests

    QuestSystem killQs;

    void SetupKillQuest()
    {
        var quests = new[] {
            new QuestDef {
                id = "kill_wolves", npcId = "hunter", title = "Wolf Hunt",
                requirements = new QuestRequirement[0],
                killRequirements = new[] { new QuestKillRequirement { monsterId = "wolf", count = 3 } },
                rewards = new QuestReward { gold = 100, xp = 50, items = new QuestRewardItem[0] }
            }
        };
        killQs = new QuestSystem(quests);
        killQs.SubscribeEvents();
    }

    [Test]
    public void KillQuest_TracksKillProgress()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");

        EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = 1, totalKills = 1 });
        Assert.AreEqual(1, killQs.GetKillProgress("kill_wolves", "wolf"));

        EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = 2, totalKills = 2 });
        Assert.AreEqual(2, killQs.GetKillProgress("kill_wolves", "wolf"));
    }

    [Test]
    public void KillQuest_NotCompletable_UntilKillsMet()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");
        var emptyInv = new InventorySystem(5);

        EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = 1, totalKills = 1 });
        Assert.AreEqual("active", killQs.GetStatus("kill_wolves", emptyInv));
    }

    [Test]
    public void KillQuest_Completable_WhenKillsMet()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");
        var emptyInv = new InventorySystem(5);

        for (int i = 0; i < 3; i++)
            EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = i + 1, totalKills = i + 1 });

        Assert.AreEqual("completable", killQs.GetStatus("kill_wolves", emptyInv));
    }

    [Test]
    public void KillQuest_Complete_ReturnsReward()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");
        var emptyInv = new InventorySystem(5);

        for (int i = 0; i < 3; i++)
            EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = i + 1, totalKills = i + 1 });

        var reward = killQs.CompleteQuest("kill_wolves", emptyInv);
        Assert.IsNotNull(reward);
        Assert.AreEqual(100, reward.gold);
        Assert.IsTrue(killQs.IsCompleted("kill_wolves"));
    }

    [Test]
    public void KillQuest_IgnoresWrongMonster()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");

        EventBus.Emit(new MonsterKillEvent { monsterId = "goblin", monsterName = "Goblin", killCount = 1, totalKills = 1 });
        Assert.AreEqual(0, killQs.GetKillProgress("kill_wolves", "wolf"));
    }

    [Test]
    public void SerializeRestore_PreservesKillProgress()
    {
        SetupKillQuest();
        killQs.AcceptQuest("kill_wolves");
        EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = 1, totalKills = 1 });
        EventBus.Emit(new MonsterKillEvent { monsterId = "wolf", monsterName = "Wolf", killCount = 2, totalKills = 2 });

        var data = killQs.Serialize();
        Assert.IsNotNull(data.killProgress);
        Assert.AreEqual(2, data.killProgress["kill_wolves"]["wolf"]);

        var restored = new QuestSystem(new[] { new QuestDef {
            id = "kill_wolves", npcId = "hunter",
            killRequirements = new[] { new QuestKillRequirement { monsterId = "wolf", count = 3 } },
            requirements = new QuestRequirement[0],
            rewards = new QuestReward { gold = 100, xp = 50, items = new QuestRewardItem[0] }
        } });
        restored.Restore(data);
        Assert.AreEqual(2, restored.GetKillProgress("kill_wolves", "wolf"));
        Assert.AreEqual(1, restored.GetActiveQuests().Length);
    }

    [Test]
    public void Restore_NullKillProgress_DoesNotCrash()
    {
        var data = (
            active: new[] { "q1" },
            completed: new string[0],
            killProgress: (System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, int>>)null
        );
        qs.Restore(data);
        Assert.AreEqual(1, qs.GetActiveQuests().Length);
        Assert.AreEqual(0, qs.GetKillProgress("q1", "anything"));
    }
}
