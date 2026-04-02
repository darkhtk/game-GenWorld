using System.Collections.Generic;
using NUnit.Framework;

public class NPCBrainTests
{
    NPCBrain _brain;

    [SetUp]
    public void Setup()
    {
        _brain = new NPCBrain("elder", "장로", "Wise and cautious village elder.");
    }

    [Test]
    public void Constructor_SetsIdentity()
    {
        Assert.AreEqual("elder", _brain.NpcId);
        Assert.AreEqual("장로", _brain.NpcName);
        Assert.AreEqual(Mood.Neutral, _brain.CurrentMood);
    }

    [Test]
    public void GetRelationship_DefaultZero()
    {
        Assert.AreEqual(0, _brain.GetRelationship("player"));
    }

    [Test]
    public void UpdateRelationship_Accumulates()
    {
        _brain.UpdateRelationship("player", 5);
        _brain.UpdateRelationship("player", 3);
        Assert.AreEqual(8, _brain.GetRelationship("player"));
    }

    [Test]
    public void UpdateRelationship_ClampedToRange()
    {
        _brain.UpdateRelationship("player", 200);
        Assert.AreEqual(100, _brain.GetRelationship("player"));

        _brain.UpdateRelationship("player", -300);
        Assert.AreEqual(-100, _brain.GetRelationship("player"));
    }

    [Test]
    public void AddMemory_StoredAndRetrievable()
    {
        _brain.AddMemory("인사를 나눔", 5);
        var memories = _brain.GetTopMemories(10);
        Assert.AreEqual(1, memories.Count);
        Assert.AreEqual("인사를 나눔", memories[0].eventText);
        Assert.AreEqual(5, memories[0].importance);
    }

    [Test]
    public void GetTopMemories_SortedByImportance()
    {
        _brain.AddMemory("low", 1);
        _brain.AddMemory("high", 10);
        _brain.AddMemory("mid", 5);
        var top = _brain.GetTopMemories(2);
        Assert.AreEqual(2, top.Count);
        Assert.AreEqual("high", top[0].eventText);
        Assert.AreEqual("mid", top[1].eventText);
    }

    [Test]
    public void AddMemory_CapsAt20()
    {
        for (int i = 0; i < 25; i++)
            _brain.AddMemory($"event_{i}", i);

        var all = _brain.GetTopMemories(100);
        Assert.AreEqual(20, all.Count);
        // Should keep highest importance (5-24)
        Assert.AreEqual("event_24", all[0].eventText);
    }

    [Test]
    public void Serialize_Roundtrip()
    {
        _brain.CurrentMood = Mood.Happy;
        _brain.UpdateRelationship("player", 15);
        _brain.AddMemory("test event", 7);
        _brain.WantToTalk = true;
        _brain.TalkReason = "nearby";

        var data = _brain.Serialize();
        var restored = new NPCBrain("elder", "장로", "Wise");
        restored.Restore(data);

        Assert.AreEqual(Mood.Happy, restored.CurrentMood);
        Assert.AreEqual(15, restored.GetRelationship("player"));
        Assert.AreEqual(1, restored.GetTopMemories(10).Count);
        Assert.IsTrue(restored.WantToTalk);
        Assert.AreEqual("nearby", restored.TalkReason);
    }

    [Test]
    public void MultipleRelationships_Independent()
    {
        _brain.UpdateRelationship("player", 10);
        _brain.UpdateRelationship("hunter", -5);
        Assert.AreEqual(10, _brain.GetRelationship("player"));
        Assert.AreEqual(-5, _brain.GetRelationship("hunter"));
    }
}
