using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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

    // ------------------------------------------------------------
    // S-084 Phase 2: EventOriginId 태깅 + Spawner 구독 정리
    // ------------------------------------------------------------

    GameObject _spawnerGO;
    GameObject _prefabGO;
    MonsterSpawner _spawner;
    MonsterDef _def;

    void BuildSpawnerHarness()
    {
        _prefabGO = new GameObject("MonsterPrefab");
        _prefabGO.AddComponent<Rigidbody2D>();
        _prefabGO.AddComponent<SpriteRenderer>();
        _prefabGO.AddComponent<MonsterController>();
        _prefabGO.SetActive(false); // act as a prefab template

        _spawnerGO = new GameObject("Spawner");
        _spawner = _spawnerGO.AddComponent<MonsterSpawner>();

        typeof(MonsterSpawner)
            .GetField("monsterPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_spawner, _prefabGO);

        _def = ScriptableObject.CreateInstance<MonsterDef>();
        _def.name = "test_monster";
        _def.hp = 50; _def.atk = 5; _def.def = 1; _def.spd = 1f;
        _def.detectRange = 5f; _def.attackRange = 2f; _def.attackCooldown = 1f;
    }

    void TeardownSpawnerHarness()
    {
        if (_spawnerGO != null) Object.DestroyImmediate(_spawnerGO);
        if (_prefabGO != null) Object.DestroyImmediate(_prefabGO);
        if (_def != null) Object.DestroyImmediate(_def);
    }

    [Test]
    public void MonsterController_EventOriginId_DefaultsToNull()
    {
        var go = new GameObject("M");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<SpriteRenderer>();
        var mc = go.AddComponent<MonsterController>();

        Assert.IsNull(mc.EventOriginId);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void DespawnEventMonsters_RemovesOnlyMatchingTags()
    {
        BuildSpawnerHarness();
        try
        {
            _spawner.SpawnEventMonster(_def, Vector2.zero, "blood_moon");
            _spawner.SpawnEventMonster(_def, Vector2.right, "blood_moon");
            _spawner.SpawnEventMonster(_def, Vector2.up, "goblin_raid");
            Assert.AreEqual(3, _spawner.ActiveMonsters.Count);

            int removed = _spawner.DespawnEventMonsters("blood_moon");

            Assert.AreEqual(2, removed);
            Assert.AreEqual(1, _spawner.ActiveMonsters.Count);
            Assert.AreEqual("goblin_raid", _spawner.ActiveMonsters[0].EventOriginId);
        }
        finally { TeardownSpawnerHarness(); }
    }

    [Test]
    public void DespawnEventMonsters_NullOrEmptyId_NoOp()
    {
        BuildSpawnerHarness();
        try
        {
            _spawner.SpawnEventMonster(_def, Vector2.zero, "blood_moon");
            Assert.AreEqual(1, _spawner.ActiveMonsters.Count);

            Assert.AreEqual(0, _spawner.DespawnEventMonsters(null));
            Assert.AreEqual(0, _spawner.DespawnEventMonsters(string.Empty));
            Assert.AreEqual(1, _spawner.ActiveMonsters.Count, "tagged monsters must survive null/empty cleanup");
        }
        finally { TeardownSpawnerHarness(); }
    }

    [Test]
    public void WorldEventEndEvent_TriggersSpawnerCleanup()
    {
        BuildSpawnerHarness();
        try
        {
            _spawner.SpawnEventMonster(_def, Vector2.zero, "blood_moon");
            _spawner.SpawnEventMonster(_def, Vector2.up, "goblin_raid");

            EventBus.Emit(new WorldEventEndEvent { id = "blood_moon" });

            Assert.AreEqual(1, _spawner.ActiveMonsters.Count);
            Assert.AreEqual("goblin_raid", _spawner.ActiveMonsters[0].EventOriginId);
        }
        finally { TeardownSpawnerHarness(); }
    }
}
