using NUnit.Framework;
using UnityEngine;

// S-129: GameManager singleton hardening — duplicate Awake guard + OnDestroy cleanup +
// IsReady gate. Exercises only the singleton invariants; full Start() init wiring needs
// a PlayMode harness with serialized references, which is out of scope here.
public class GameManagerSingletonTests
{
    [SetUp]
    public void SetUp()
    {
        // Defensive: a previous test (or hot-reloaded session) may have left an instance.
        if (GameManager.Instance != null)
            Object.DestroyImmediate(GameManager.Instance.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        if (GameManager.Instance != null)
            Object.DestroyImmediate(GameManager.Instance.gameObject);
    }

    [Test]
    public void Awake_AssignsInstance()
    {
        var go = new GameObject("gm_awake_assigns");
        var gm = go.AddComponent<GameManager>();
        Assert.AreEqual(gm, GameManager.Instance);
    }

    [Test]
    public void OnDestroy_ClearsInstance()
    {
        var go = new GameObject("gm_ondestroy_clears");
        go.AddComponent<GameManager>();
        Assert.IsNotNull(GameManager.Instance);
        Object.DestroyImmediate(go);
        Assert.IsNull(GameManager.Instance,
            "OnDestroy must clear Instance so callers do not see a stale reference.");
    }

    [Test]
    public void Awake_DuplicateDoesNotOverwriteInstance()
    {
        var go1 = new GameObject("gm_first");
        var gm1 = go1.AddComponent<GameManager>();
        var go2 = new GameObject("gm_duplicate");
        // Awake on the duplicate logs a warning and schedules self-destroy; Instance
        // must still point to the original. Warnings do not fail the EditMode runner.
        go2.AddComponent<GameManager>();
        Assert.AreEqual(gm1, GameManager.Instance,
            "First-wins policy: a duplicate Awake must leave the original Instance intact.");
    }

    [Test]
    public void IsReady_FalseWhenNoInstance()
    {
        Assert.IsNull(GameManager.Instance);
        Assert.IsFalse(GameManager.IsReady,
            "IsReady must be false when Instance is null.");
    }

    [Test]
    public void IsReady_FalseAfterAwakeBeforeStart()
    {
        var go = new GameObject("gm_isready_before_start");
        go.AddComponent<GameManager>();
        // Awake assigns Instance, but Start() is what sets _initialized=true. EditMode
        // never runs Start, so IsReady must remain false even though Instance is alive.
        Assert.IsNotNull(GameManager.Instance);
        Assert.IsFalse(GameManager.IsReady,
            "IsReady must be false until Start() finishes constructing subsystems.");
    }
}
