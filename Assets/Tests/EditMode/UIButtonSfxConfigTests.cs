using NUnit.Framework;

// S-122 Phase 1: GameConfig.UI button SFX constants + volume sanity. Hover must be
// noticeably quieter than click since hover fires far more frequently (every cursor
// sweep). Behavioural OnPointerEnter/Click verification deferred to PlayMode (S-148
// Phase 2 attachment task), since UIButtonSfx calls AudioManager.Instance singleton
// directly and no DI seam exists.
public class UIButtonSfxConfigTests
{
    [Test]
    public void ButtonHoverSfxName_IsCorrect()
    {
        Assert.AreEqual("sfx_ui_hover", GameConfig.UI.ButtonHoverSfxName);
    }

    [Test]
    public void ButtonClickSfxName_IsReusedFromExisting()
    {
        // Reuse existing sfx_click.wav to avoid asset bloat.
        Assert.AreEqual("sfx_click", GameConfig.UI.ButtonClickSfxName);
    }

    [Test]
    public void ButtonHoverVolume_InRangeAndSubtle()
    {
        Assert.That(GameConfig.UI.ButtonHoverSfxVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.That(GameConfig.UI.ButtonHoverSfxVolume, Is.LessThanOrEqualTo(0.6f),
            "Hover SFX must be subtle — fires on every cursor sweep, must not fatigue.");
    }

    [Test]
    public void ButtonClickVolume_LouderThanHover()
    {
        Assert.That(GameConfig.UI.ButtonClickSfxVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.That(GameConfig.UI.ButtonClickSfxVolume,
            Is.GreaterThan(GameConfig.UI.ButtonHoverSfxVolume),
            "Click SFX should be louder than hover for affordance feedback.");
    }

    [Test]
    public void ButtonSfxEnabled_DefaultsTrue()
    {
        Assert.IsTrue(GameConfig.UI.ButtonSfxEnabled);
    }
}
