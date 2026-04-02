using NUnit.Framework;
using UnityEngine;

public class EffectSystemTests
{
    EffectHolder holder;

    [SetUp]
    public void Setup() => holder = new EffectHolder();

    [Test]
    public void Apply_Stun_IsActive()
    {
        holder.Apply("stun", 5000f, 0);
        Assert.IsTrue(holder.Has("stun"));
    }

    [Test]
    public void Stun_CappedAt10Seconds()
    {
        float now = Time.time * 1000f;
        holder.Apply("stun", now + 99999f, 0);
        float maxAllowed = now + EffectHolder.MaxStunMs;
        Assert.LessOrEqual(holder.GetExpiresAt("stun"), maxAllowed + 100f);
    }

    [Test]
    public void Slow_ClampsToMinimum()
    {
        holder.Apply("slow", 5000f, 0.01f);
        Assert.AreEqual(EffectHolder.MinSlow, holder.GetValue("slow"), 0.01f);
    }

    [Test]
    public void Tick_RemovesExpired()
    {
        holder.Apply("stun", 100f, 0);
        var expired = holder.Tick(200f);
        Assert.Contains("stun", expired);
        Assert.IsFalse(holder.Has("stun"));
    }

    [Test]
    public void Dot_DealsDamageOnInterval()
    {
        holder.ApplyDot(5000f, 10, 1000f);
        float dmg = holder.TickDot(1500f);
        Assert.AreEqual(10f, dmg);
    }

    [Test]
    public void Clear_RemovesAllEffects()
    {
        holder.Apply("stun", 5000f, 0);
        holder.Apply("slow", 5000f, 0.5f);
        holder.Clear();
        Assert.IsFalse(holder.Has("stun"));
        Assert.IsFalse(holder.Has("slow"));
    }

    [Test]
    public void Stun_ExtendsExistingDuration()
    {
        holder.Apply("stun", 3000f, 0);
        holder.Apply("stun", 5000f, 0);
        Assert.AreEqual(5000f, holder.GetExpiresAt("stun"));
    }

    [Test]
    public void Slow_KeepsStrongerSlow()
    {
        holder.Apply("slow", 5000f, 0.5f);
        holder.Apply("slow", 6000f, 0.3f);
        Assert.AreEqual(0.3f, holder.GetValue("slow"), 0.01f);
        Assert.AreEqual(6000f, holder.GetExpiresAt("slow"));
    }
}
