using NUnit.Framework;

// S-140: GameConfig.Audio boss-death feedback constants — sfx name, volume,
// shake intensity/duration, BGM duck. Behavioural verification (CombatRewardHandler
// branch firing on def.rank == "boss") is deferred to PlayMode since CameraShake +
// AudioManager.Instance singleton + Cinemachine impulse source need scene context.
public class BossDeathConfigTests
{
    [Test]
    public void BossDeathSfxName_IsCorrect()
    {
        Assert.AreEqual("sfx_boss_defeat", GameConfig.Audio.BossDeathSfxName,
            "S-140 reuses existing sfx_boss_defeat.wav (orphan asset, ~1.5s) — no new generation.");
    }

    [Test]
    public void BossDeathSfxVolume_InRangeAndProminent()
    {
        Assert.That(GameConfig.Audio.BossDeathSfxVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        Assert.That(GameConfig.Audio.BossDeathSfxVolume, Is.GreaterThan(0.85f),
            "Boss death is a climactic moment — should be louder than dialogue open (0.85).");
    }

    [Test]
    public void BossDeathCameraShakeMs_InReasonableRange()
    {
        Assert.That(GameConfig.Audio.BossDeathCameraShakeMs, Is.GreaterThan(200f).And.LessThan(1500f),
            "Phase-transition shake is 500ms (MonsterController:223). Boss death should be in similar order, not exceed 1.5s.");
    }

    [Test]
    public void BossDeathCameraShakeIntensity_StrongerThanPhaseTransition()
    {
        // MonsterController phase-transition uses 0.012f. Boss death should hit harder.
        Assert.That(GameConfig.Audio.BossDeathCameraShakeIntensity, Is.GreaterThan(0.012f),
            "Boss death shake should outweigh phase-transition (0.012f) for finality.");
        Assert.That(GameConfig.Audio.BossDeathCameraShakeIntensity, Is.LessThan(0.1f),
            "Cap to avoid nausea / off-screen camera flight.");
    }

    [Test]
    public void BossDeathDuckBgmDropDb_DeeperThanItemPickup()
    {
        // S-118 item pickup defaults: -6dB / 0.4s. Boss death is rarer + more emphatic
        // → duck deeper so the chord rings clean above the BGM.
        Assert.That(GameConfig.Audio.BossDeathDuckBgmDropDb, Is.LessThan(-6f),
            "Boss-death duck should drop deeper than S-118 item-pickup default (-6dB).");
        Assert.That(GameConfig.Audio.BossDeathDuckBgmDropDb, Is.GreaterThan(-24f),
            "Hard cap — full-mute would be jarring.");
    }

    [Test]
    public void BossDeathDuckBgmDuration_CoversChordTail()
    {
        // sfx_boss_defeat.wav is ~1.5s mono 44.1kHz. Duck must hold long enough that
        // the chord's ring-out tail is not buried by the BGM coming back too early.
        Assert.That(GameConfig.Audio.BossDeathDuckBgmDuration, Is.GreaterThanOrEqualTo(1.0f),
            "Duck duration should at least cover the bulk of the 1.2-1.5s chord.");
        Assert.That(GameConfig.Audio.BossDeathDuckBgmDuration, Is.GreaterThan(GameConfig.Audio.BossDeathCameraShakeMs / 1000f),
            "Audio impact outlasts the visual shake by design (visual finishes, audio rings).");
    }

    [Test]
    public void BossDeathEnabled_DefaultsTrue()
    {
        Assert.IsTrue(GameConfig.Audio.BossDeathEnabled);
    }
}
