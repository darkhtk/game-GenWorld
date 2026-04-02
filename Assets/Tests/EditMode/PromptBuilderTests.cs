using System.Collections.Generic;
using NUnit.Framework;

public class PromptBuilderTests
{
    NPCBrain _brain;
    DialogueContext _ctx;

    [SetUp]
    public void Setup()
    {
        _brain = new NPCBrain("elder", "장로", "Wise and cautious village elder.");
        _ctx = new DialogueContext
        {
            playerInput = "안녕하세요",
            history = new List<DialogueEntry>(),
            playerLevel = 5,
            playerGold = 100,
            rejectionCount = 0
        };
    }

    [Test]
    public void SystemRules_ContainsJsonFormat()
    {
        Assert.IsTrue(PromptBuilder.SYSTEM_RULES.Contains("JSON"));
    }

    [Test]
    public void SystemRules_ContainsKoreanRequirement()
    {
        Assert.IsTrue(PromptBuilder.SYSTEM_RULES.Contains("한국어"));
    }

    [Test]
    public void BuildDialoguePrompt_ContainsNpcIdentity()
    {
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("장로"));
        Assert.IsTrue(prompt.Contains("Wise and cautious"));
    }

    [Test]
    public void BuildDialoguePrompt_ContainsPlayerStats()
    {
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("레벨: 5"));
        Assert.IsTrue(prompt.Contains("골드: 100"));
    }

    [Test]
    public void BuildDialoguePrompt_ContainsPlayerInput()
    {
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("안녕하세요"));
    }

    [Test]
    public void BuildDialoguePrompt_IncludesMemories()
    {
        _brain.AddMemory("숲 정보 제공", 5);
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("숲 정보 제공"));
    }

    [Test]
    public void BuildDialoguePrompt_FirstTurnGuidance()
    {
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("첫 턴"));
    }

    [Test]
    public void BuildDialoguePrompt_LaterTurnGuidance()
    {
        _ctx.history = new List<DialogueEntry>
        {
            new() { role = "player", text = "안녕" },
            new() { role = "npc", text = "어서 오게" },
            new() { role = "player", text = "숲에 대해" },
            new() { role = "npc", text = "숲은 위험하오" },
            new() { role = "player", text = "더 알려주세요" },
            new() { role = "npc", text = "조심하시오" }
        };
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("3턴 이상"));
    }

    [Test]
    public void BuildDialoguePrompt_HighRelationship_FriendlyAttitude()
    {
        _brain.UpdateRelationship("player", 15);
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("친근"));
    }

    [Test]
    public void BuildDialoguePrompt_NegativeRelationship_HostileAttitude()
    {
        _brain.UpdateRelationship("player", -15);
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("적대"));
    }

    [Test]
    public void BuildDialoguePrompt_IncludesActions()
    {
        _ctx.npcActions = new[] { "open_shop", "heal_player" };
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("open_shop"));
        Assert.IsTrue(prompt.Contains("heal_player"));
    }

    [Test]
    public void BuildDialoguePrompt_IncludesDialogueTraits()
    {
        _ctx.traits = new DialogueTraits
            { friendliness = 8, generosity = 5, secretive = 2, stubbornness = 3, curiosity = 7 };
        string prompt = PromptBuilder.BuildDialoguePrompt(_brain, _ctx);
        Assert.IsTrue(prompt.Contains("친절함: 8"));
        Assert.IsTrue(prompt.Contains("정보 공개 수준: 3"));
    }
}
