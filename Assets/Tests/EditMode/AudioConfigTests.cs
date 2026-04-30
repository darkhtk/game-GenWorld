using NUnit.Framework;

// S-120: GameConfig.Audio routing helpers — boss vs default BGM transition fadeTime.
public class AudioConfigTests
{
    [Test]
    public void IsBossRegion_VolcanoTrue()
    {
        Assert.IsTrue(GameConfig.Audio.IsBossRegion("volcano"));
    }

    [Test]
    public void IsBossRegion_DragonLairTrue()
    {
        Assert.IsTrue(GameConfig.Audio.IsBossRegion("dragon_lair"));
    }

    [Test]
    public void IsBossRegion_VillageFalse()
    {
        Assert.IsFalse(GameConfig.Audio.IsBossRegion("village"));
    }

    [Test]
    public void IsBossRegion_EmptyOrNullFalse()
    {
        Assert.IsFalse(GameConfig.Audio.IsBossRegion(""));
        Assert.IsFalse(GameConfig.Audio.IsBossRegion(null));
    }

    [Test]
    public void BgmFadeTimeFor_BossRegionUses1_5s()
    {
        Assert.AreEqual(GameConfig.Audio.BgmTransitionBossEnter,
            GameConfig.Audio.BgmFadeTimeFor("volcano"), 0.0001f);
        Assert.AreEqual(1.5f, GameConfig.Audio.BgmFadeTimeFor("dragon_lair"), 0.0001f);
    }

    [Test]
    public void BgmFadeTimeFor_NormalRegionUses1s()
    {
        Assert.AreEqual(GameConfig.Audio.BgmTransitionDefault,
            GameConfig.Audio.BgmFadeTimeFor("village"), 0.0001f);
        Assert.AreEqual(1.0f, GameConfig.Audio.BgmFadeTimeFor("forest"), 0.0001f);
        Assert.AreEqual(1.0f, GameConfig.Audio.BgmFadeTimeFor("cave"), 0.0001f);
    }

    [Test]
    public void BossRegionIds_ContainsExactlyVolcanoAndDragonLair()
    {
        Assert.AreEqual(2, GameConfig.Audio.BossRegionIds.Length);
        CollectionAssert.Contains(GameConfig.Audio.BossRegionIds, "volcano");
        CollectionAssert.Contains(GameConfig.Audio.BossRegionIds, "dragon_lair");
    }
}
