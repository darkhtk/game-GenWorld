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
}
