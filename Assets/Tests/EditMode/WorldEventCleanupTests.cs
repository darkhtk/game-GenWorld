using System.Collections.Generic;
using NUnit.Framework;

public class WorldEventCleanupTests
{
    [SetUp]
    public void Setup() => EventBus.Clear();

    [TearDown]
    public void Teardown() => EventBus.Clear();

    [Test]
    public void ForceEndActiveEvent_NoActiveEvent_DoesNothing()
    {
        var sys = new WorldEventSystem();
        sys.Init();

        bool emitted = false;
        EventBus.On<WorldEventEndEvent>(_ => emitted = true);

        sys.ForceEndActiveEvent();

        Assert.IsFalse(emitted, "no active event → should not emit WorldEventEndEvent");
        Assert.IsFalse(sys.IsEventActive);
    }

    [Test]
    public void ForceEndActiveEvent_WithActive_EmitsEndEventWithMatchingId()
    {
        var sys = new WorldEventSystem();
        sys.Init();
        // Drive an active event via Restore — duration > elapsed so StartEvent is called
        var lastOcc = new Dictionary<string, float>();
        sys.Restore("blood_moon", startHour: 0f, lastOcc, currentHour: 0.5f);
        Assert.IsTrue(sys.IsEventActive, "Restore should start blood_moon");

        string capturedId = null;
        EventBus.On<WorldEventEndEvent>(e => capturedId = e.id);

        sys.ForceEndActiveEvent();

        Assert.IsFalse(sys.IsEventActive);
        Assert.AreEqual("blood_moon", capturedId);
    }

    [Test]
    public void EndEvent_ResetsGlobalMultipliers()
    {
        var sys = new WorldEventSystem();
        sys.Init();
        var lastOcc = new Dictionary<string, float>();
        sys.Restore("golden_hour", startHour: 0f, lastOcc, currentHour: 0.5f);
        Assert.AreEqual(2f, sys.GlobalDropMultiplier, 0.001f);

        sys.ForceEndActiveEvent();

        Assert.AreEqual(1f, sys.GlobalDropMultiplier, 0.001f);
        Assert.AreEqual(1f, sys.GlobalGoldMultiplier, 0.001f);
    }
}
