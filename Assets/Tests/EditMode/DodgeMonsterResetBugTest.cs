using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DodgeMonsterResetBugTest
{
    GameObject _playerGO;
    GameObject _monsterGO;
    PlayerController _player;
    MonsterController _monster;
    CombatManager _combatManager;

    [SetUp]
    public void SetUp()
    {
        // Create player
        _playerGO = new GameObject("Player");
        _player = _playerGO.AddComponent<PlayerController>();
        _playerGO.AddComponent<Rigidbody2D>();
        _playerGO.AddComponent<SpriteRenderer>();

        // Create monster
        _monsterGO = new GameObject("Monster");
        _monster = _monsterGO.AddComponent<MonsterController>();
        _monsterGO.AddComponent<Rigidbody2D>();
        _monsterGO.AddComponent<SpriteRenderer>();

        // Create test monster def
        var monsterDef = ScriptableObject.CreateInstance<MonsterDef>();
        monsterDef.hp = 100;
        monsterDef.atk = 10;
        monsterDef.def = 5;
        monsterDef.spd = 2f;
        monsterDef.detectRange = 5f;
        monsterDef.attackRange = 2f;
        monsterDef.attackCooldown = 1f;

        _monster.Init(monsterDef, Vector2.zero);

        // Create combat manager
        var combatGO = new GameObject("CombatManager");
        _combatManager = combatGO.AddComponent<CombatManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null) Object.DestroyImmediate(_playerGO);
        if (_monsterGO != null) Object.DestroyImmediate(_monsterGO);
        if (_combatManager != null) Object.DestroyImmediate(_combatManager.gameObject);
    }

    [Test]
    public void MonsterHPShouldNotResetWhenPlayerDodgesWithoutDamage()
    {
        // Arrange: Monster at full HP
        _monster.Hp = _monster.Def.hp; // Full HP

        // Simulate player dodging
        _player.GetComponent<Rigidbody2D>().position = Vector2.zero;
        _monsterGO.transform.position = new Vector3(1.5f, 0, 0); // Within attack range

        // Act: Put monster in Return state (simulate conditions for return)
        _monster.LastHitByPlayerTime = Time.time - 3f; // 3 seconds ago (beyond RecentHitWindow)
        _monster.AIState = MonsterAIState.Return;

        // Trigger return state HP reset logic
        _monsterGO.transform.position = Vector3.zero; // At spawn position
        _monster.UpdateAI(_player.Position, Time.time);

        // Assert: HP should remain full (not reset from lower value)
        Assert.AreEqual(_monster.Def.hp, _monster.Hp,
            "Monster HP should not be reset when already at full HP");
    }

    [Test]
    public void MonsterHPShouldResetWhenDamagedAndReturning()
    {
        // Arrange: Monster with reduced HP
        _monster.Hp = _monster.Def.hp - 20; // Damaged monster

        // Act: Put monster in Return state and trigger HP reset
        _monster.AIState = MonsterAIState.Return;
        _monsterGO.transform.position = Vector3.zero; // At spawn position
        _monster.UpdateAI(_player.Position, Time.time);

        // Assert: HP should be reset to full
        Assert.AreEqual(_monster.Def.hp, _monster.Hp,
            "Damaged monster HP should be reset to full when returning to spawn");
    }

    [Test]
    public void LastHitByPlayerTimeShouldUpdateDuringDodge()
    {
        // Arrange: Player is dodging
        var reflection = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var isDodgingField = typeof(PlayerController).GetField("IsDodging", reflection);
        isDodgingField?.SetValue(_player, true);

        var invincibleField = typeof(PlayerController).GetProperty("Invincible");
        invincibleField?.SetValue(_player, true);

        // Position monster in attack range
        _monsterGO.transform.position = new Vector3(1.5f, 0, 0);

        float initialTime = _monster.LastHitByPlayerTime;

        // Act: Handle monster attacks while player is dodging
        var monsters = new System.Collections.Generic.List<MonsterController> { _monster };
        _combatManager.HandleMonsterAttacks(monsters, Time.time);

        // Assert: LastHitByPlayerTime should be updated even during dodge
        Assert.Greater(_monster.LastHitByPlayerTime, initialTime,
            "Monster LastHitByPlayerTime should be updated when player dodges in attack range");
    }
}