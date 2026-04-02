using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

public class AIManagerTests
{
    AIManager _ai;

    [SetUp]
    public void Setup()
    {
        _ai = new AIManager(null);
        _ai.RegisterNpc("elder", "장로", "Wise village elder.", "Elder profile text");
    }

    [Test]
    public void RegisterNpc_CreatesBrain()
    {
        var brain = _ai.GetBrain("elder");
        Assert.IsNotNull(brain);
        Assert.AreEqual("장로", brain.NpcName);
    }

    [Test]
    public void GetBrain_UnknownNpc_ReturnsNull()
    {
        Assert.IsNull(_ai.GetBrain("nobody"));
    }

    [Test]
    public void RegisterNpc_ProfileAssigned()
    {
        var brain = _ai.GetBrain("elder");
        Assert.AreEqual("Elder profile text", brain.Profile);
    }

    [Test]
    public async Task GenerateDialogue_Offline_ReturnsFallback()
    {
        // AI not initialized (AiEnabled=false), should return offline response
        var response = await _ai.GenerateDialogue("elder", "안녕하세요",
            new List<DialogueEntry>(), 1, 0, null, null, null);
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.dialogue);
        Assert.IsTrue(response.dialogue.Length > 0);
        Assert.AreEqual(1, response.relationshipChange);
        Assert.IsNotNull(response.newMemory);
    }

    [Test]
    public async Task GenerateDialogue_UnknownNpc_ReturnsFallback()
    {
        var response = await _ai.GenerateDialogue("nobody", "hello",
            new List<DialogueEntry>(), 1, 0, null, null, null);
        Assert.IsNotNull(response);
        Assert.AreEqual("...", response.dialogue);
    }

    [Test]
    public void SerializeAllBrains_Roundtrip()
    {
        _ai.RegisterNpc("hunter", "사냥꾼", "Skilled tracker.");
        var brain = _ai.GetBrain("elder");
        brain.UpdateRelationship("player", 10);
        brain.AddMemory("인사 나눔", 5);

        var data = _ai.SerializeAllBrains();
        Assert.AreEqual(2, data.Count);
        Assert.IsTrue(data.ContainsKey("elder"));
        Assert.IsTrue(data.ContainsKey("hunter"));

        var ai2 = new AIManager(null);
        ai2.RegisterNpc("elder", "장로", "Wise.");
        ai2.RegisterNpc("hunter", "사냥꾼", "Tracker.");
        ai2.RestoreAllBrains(data);

        Assert.AreEqual(10, ai2.GetBrain("elder").GetRelationship("player"));
    }

    [Test]
    public async Task GenerateDialogue_AppliesRelationshipChange()
    {
        var brain = _ai.GetBrain("elder");
        int relBefore = brain.GetRelationship("player");

        await _ai.GenerateDialogue("elder", "안녕하세요",
            new List<DialogueEntry>(), 1, 0, null, null, null);

        int relAfter = brain.GetRelationship("player");
        Assert.Greater(relAfter, relBefore);
    }

    [Test]
    public async Task GenerateDialogue_AddsMemory()
    {
        await _ai.GenerateDialogue("elder", "안녕하세요",
            new List<DialogueEntry>(), 1, 0, null, null, null);

        var memories = _ai.GetBrain("elder").GetTopMemories(10);
        Assert.IsTrue(memories.Count > 0);
    }
}
