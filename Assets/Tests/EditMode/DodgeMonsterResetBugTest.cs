using NUnit.Framework;
using UnityEngine;

public class DodgeMonsterResetBugTest
{
    GameObject _playerGO;
    GameObject _monsterGO;
    PlayerController _player;
    MonsterController _monster;
    CombatManager _combatManager;
    GameObject _combatGO;

    [SetUp]
    public void SetUp()
    {
        // Component order matters: Rigidbody2D + SpriteRenderer must exist before
        // PlayerController/MonsterController so their Awake/Init see required deps.
        _playerGO = new GameObject("Player");
        _playerGO.AddComponent<Rigidbody2D>();
        _playerGO.AddComponent<SpriteRenderer>();
        _player = _playerGO.AddComponent<PlayerController>();
        _playerGO.transform.position = new Vector3(10f, 10f, 0f);

        _monsterGO = new GameObject("Monster");
        _monsterGO.AddComponent<Rigidbody2D>();
        _monsterGO.AddComponent<SpriteRenderer>();
        _monster = _monsterGO.AddComponent<MonsterController>();

        var monsterDef = ScriptableObject.CreateInstance<MonsterDef>();
        monsterDef.name = "test_monster";
        monsterDef.hp = 100;
        monsterDef.atk = 10;
        monsterDef.def = 5;
        monsterDef.spd = 2f;
        monsterDef.detectRange = 5f;
        monsterDef.attackRange = 2f;
        monsterDef.attackCooldown = 1f;

        _monster.Init(monsterDef, Vector2.zero);

        _combatGO = new GameObject("CombatManager");
        _combatManager = _combatGO.AddComponent<CombatManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null) Object.DestroyImmediate(_playerGO);
        if (_monsterGO != null) Object.DestroyImmediate(_monsterGO);
        if (_combatGO != null) Object.DestroyImmediate(_combatGO);
    }

    [Test]
    public void MonsterAtFullHpStaysAtFullHpWhenReturning()
    {
        // SPEC-S-101 §3-1: Hp guard — only damaged monsters (Hp < Def.hp) get reset.
        // Monster placed at spawn while at full HP must remain at full HP.
        _monster.Hp = _monster.Def.hp;
        _monsterGO.transform.position = Vector3.zero;
        _monster.LastHitByPlayerTime = Time.time - 30f; // Far past RecentHitWindow
        _monster.SetAIStateForTest(MonsterAIState.Return);

        _monster.UpdateAI(_player.Position, Time.time);

        Assert.AreEqual(_monster.Def.hp, _monster.Hp,
            "Monster Hp should remain at full when already undamaged on Return");
    }

    [Test]
    public void DamagedMonsterFullHealsOnReturnToSpawn()
    {
        // SPEC-S-101 §3-1: Damaged monster reaching spawn fully heals (full reset is intended).
        _monster.Hp = _monster.Def.hp - 30; // Damaged monster (Hp = 70)
        _monsterGO.transform.position = Vector3.zero;
        _monster.LastHitByPlayerTime = Time.time - 30f;
        _monster.SetAIStateForTest(MonsterAIState.Return);

        _monster.UpdateAI(_player.Position, Time.time);

        Assert.AreEqual(_monster.Def.hp, _monster.Hp,
            "Damaged monster Hp should fully reset on Return to spawn");
    }

    [Test]
    public void DodgeKeepsLastHitByPlayerTimeFreshForNearbyMonsters()
    {
        // SPEC-S-101 §3-2: While dodging, monsters within attackRange × DodgeAggroSyncRangeMult
        // must have their LastHitByPlayerTime refreshed so they do not slip into Return.
        var playerEffects = new EffectHolder();
        _combatManager.Init(
            _player,
            () => new Stats { def = 5, maxHp = 100, hp = 100 },
            () => 1,
            _ => { },
            () => { },
            playerEffects);
        _combatManager.PlayerState = new PlayerStats();

        _player.SetDodgeStateForTest(true, true);

        _playerGO.transform.position = Vector3.zero;
        _monsterGO.transform.position = new Vector3(1.5f, 0f, 0f); // Within atkRange 2 × 1.3 = 2.6

        _monster.LastHitByPlayerTime = Time.time - 30f;
        float beforeUpdate = _monster.LastHitByPlayerTime;

        var monsters = new System.Collections.Generic.List<MonsterController> { _monster };
        _combatManager.HandleMonsterAttacks(monsters, Time.time * 1000f);

        Assert.Greater(_monster.LastHitByPlayerTime, beforeUpdate,
            "Dodging in attack range should refresh LastHitByPlayerTime so the monster does not slip into Return");
    }

    [Test]
    public void DodgeDoesNotApplyMeleeDamageToPlayer()
    {
        // SPEC-S-101 §3-2: ApplyDamageToPlayer is the single chokepoint that must
        // respect invincibility. Pattern progression continues during dodge but Hp
        // must be unchanged at the end of the frame.
        var playerEffects = new EffectHolder();
        var stats = new Stats { def = 5, maxHp = 100, hp = 100 };
        _combatManager.Init(_player, () => stats, () => 1, _ => { }, () => { }, playerEffects);
        _combatManager.PlayerState = new PlayerStats { Hp = 100, Mp = 0 };

        _player.SetDodgeStateForTest(true, true);

        _playerGO.transform.position = Vector3.zero;
        _monsterGO.transform.position = new Vector3(1.5f, 0f, 0f);

        // Force the monster's plain-melee path: AIState=Attack and a 'now' far past
        // any attackCooldown so CanAttack returns true and the damage path executes.
        _monster.SetAIStateForTest(MonsterAIState.Attack);
        _monster.MarkAttacked(0f);

        int hpBefore = _combatManager.PlayerState.Hp;
        var monsters = new System.Collections.Generic.List<MonsterController> { _monster };
        const float farFutureNow = 100000f;
        _combatManager.HandleMonsterAttacks(monsters, farFutureNow);

        Assert.AreEqual(hpBefore, _combatManager.PlayerState.Hp,
            "Player Hp must be unchanged while dodging — ApplyDamageToPlayer must respect invincibility");
    }
}
