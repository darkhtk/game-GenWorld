using NUnit.Framework;

// S-121: GameConfig.Audio dialogue start/end SFX constants + volume sanity.
// Behavioural Show/Hide verification is deferred to PlayMode (SPEC-S-121 §11 DoD §6),
// since DialogueUI calls AudioManager.Instance singleton directly and no DI seam exists.
public class DialogueAudioConfigTests
{
    [Test]
    public void DialogueOpenSfxName_IsCorrect()
    {
        Assert.AreEqual("sfx_dialogue_open", GameConfig.Audio.DialogueOpenSfxName);
    }

    [Test]
    public void DialogueCloseSfxName_IsCorrect()
    {
        Assert.AreEqual("sfx_dialogue_close", GameConfig.Audio.DialogueCloseSfxName);
    }

    [Test]
    public void DialogueOpenSfxVolume_InRangeAndQuieterThanFull()
    {
        Assert.That(GameConfig.Audio.DialogueOpenSfxVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.AreEqual(0.85f, GameConfig.Audio.DialogueOpenSfxVolume, 0.0001f);
    }

    [Test]
    public void DialogueCloseSfxVolume_QuieterThanOpen()
    {
        Assert.That(GameConfig.Audio.DialogueCloseSfxVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.AreEqual(0.70f, GameConfig.Audio.DialogueCloseSfxVolume, 0.0001f);
        Assert.That(GameConfig.Audio.DialogueCloseSfxVolume,
            Is.LessThan(GameConfig.Audio.DialogueOpenSfxVolume),
            "Close SFX should be quieter than open per SPEC §2");
    }

    [Test]
    public void DialogueSfxEnabled_DefaultsTrue()
    {
        Assert.IsTrue(GameConfig.Audio.DialogueSfxEnabled);
    }
}
