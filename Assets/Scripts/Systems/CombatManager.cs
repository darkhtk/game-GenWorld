using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    PlayerController _player;
    System.Func<Stats> _getStats;
    System.Func<int> _getLevel;
    System.Action<MonsterController> _onMonsterDeath;
    System.Action _onPlayerDeath;
    EffectHolder _playerEffects;

    SkillExecutor _skillExecutor;
    ActionRunner _actionRunner;
    ComboSystem _comboSystem;

    float _lastAutoAttackTime;
    List<MonsterController> _cachedMonsters;

    // Set by Director after Init
    public SkillSystem Skills { get; set; }
    public PlayerStats PlayerState { get; set; }

    public void Init(PlayerController player, System.Func<Stats> getStats, System.Func<int> getLevel,
        System.Action<MonsterController> onMonsterDeath, System.Action onPlayerDeath, EffectHolder playerEffects)
    {
        _player = player;
        _getStats = getStats;
        _getLevel = getLevel;
        _onMonsterDeath = onMonsterDeath;
        _onPlayerDeath = onPlayerDeath;
        _playerEffects = playerEffects;
        _skillExecutor = new SkillExecutor();
        _actionRunner = new ActionRunner();
        _comboSystem = new ComboSystem();
    }

    public void PerformAutoAttack(List<MonsterController> monsters)
    {
        if (monsters == null) return;
        _cachedMonsters = monsters;

        float nowMs = Time.time * 1000f;
        if (!Input.GetMouseButton(0)) return;
        if (nowMs - _lastAutoAttackTime < GameConfig.AutoAttackCooldown) return;

        _lastAutoAttackTime = nowMs;

        var stats = _getStats();
        int level = _getLevel();
        float swingRange = GameConfig.AutoAttackBaseRange + level * GameConfig.AutoAttackRangePerLevel;
        float levelDmgMult = 1f + level * GameConfig.AutoAttackDmgPerLevel;

        Vector2 playerPos = _player.Position;
        Vector2 aimDir = _player.AimDirection;
        float swingAngle = Mathf.Atan2(aimDir.y, aimDir.x);

        bool stealthActive = _playerEffects.Has("stealth");
        List<MonsterController> killed = null;

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (m == null || m.IsDead) continue;
            float distSq = (playerPos - m.Position).sqrMagnitude;
            if (distSq > swingRange * swingRange) continue;

            Vector2 toMonster = m.Position - playerPos;
            float angleToMonster = Mathf.Atan2(toMonster.y, toMonster.x);
            float angleDiff = Mathf.Abs(swingAngle - angleToMonster);
            if (angleDiff > Mathf.PI) angleDiff = 2f * Mathf.PI - angleDiff;
            if (angleDiff > GameConfig.AutoAttackArcHalf) continue;

            int baseDmg = Mathf.FloorToInt(stats.atk * levelDmgMult);
            bool isCrit = stealthActive || CombatSystem.CalcCrit(stats.crit);
            int dmg = CombatSystem.CalcDamage(baseDmg, m.EffectiveDef, isCrit);

            m.LastHitByPlayerTime = Time.time;
            bool dead = m.TakeDamage(dmg);
            ShowDamageNumber(m.Position + Vector2.up * 0.5f, dmg, isCrit);
            SkillVFX.ShowAtPosition(this, "vfx_melee_hit", m.Position.x, m.Position.y);
            AudioManager.Instance?.PlaySFX("sfx_attack", 0.1f);
            if (dead) { killed ??= new(); killed.Add(m); }
        }

        if (killed != null)
            foreach (var m in killed) _onMonsterDeath?.Invoke(m);

        if (stealthActive)
            _playerEffects.Remove("stealth");
    }

    public void HandleMonsterAttacks(List<MonsterController> monsters, float now)
    {
        if (monsters == null || _player == null) return;
        _cachedMonsters = monsters;
        if (_player.Invincible || _player.IsDodging) return;

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (m == null || m.IsDead) continue;
            if (!m.CanAttack(now)) continue;

            float atkRange = m.Def.attackRange * 1.3f;
            float distSq = (m.Position - _player.Position).sqrMagnitude;
            if (distSq > atkRange * atkRange) continue;

            m.MarkAttacked(now);

            if (m.Def.ranged != null)
            {
                FireMonsterProjectile(m);
                continue;
            }

            var stats = _getStats();
            int dmg = CombatSystem.CalcDamage(m.EffectiveAtk, stats.def, false);
            ApplyDamageToPlayer(dmg);
            SkillVFX.ShowAtPosition(this, "vfx_melee_hit", _player.Position.x, _player.Position.y);
            AudioManager.Instance?.PlaySFX("sfx_attack", 0.15f);
        }
    }

    void FireMonsterProjectile(MonsterController m)
    {
        var r = m.Def.ranged;
        Vector2 from = m.Position;
        Vector2 to = _player.Position;

        var proj = Projectile.Get();
        if (proj == null) return;
        proj.Init(from, to, r.projectileSpeed, r.projectileColor, r.projectileSize, false);
        proj.OnArrive = arrivePos =>
        {
            if (_player == null || PlayerState == null) return;
            if (_player.Invincible || _player.IsDodging) return;
            if (m == null || m.IsDead) return;
            float hitDist = Vector2.Distance(arrivePos, _player.Position);
            if (hitDist > 40f) return;
            var stats = _getStats();
            int dmg = CombatSystem.CalcDamage(m.EffectiveAtk, stats.def, false);
            ApplyDamageToPlayer(dmg);
            SkillVFX.ShowAtPosition(this, "vfx_ranged_hit", _player.Position.x, _player.Position.y);
        };
    }

    void ApplyDamageToPlayer(int dmg)
    {
        if (PlayerState == null) return;

        if (_playerEffects.Has("mana_shield") && PlayerState.Mp > 0)
        {
            int absorbed = Mathf.Min(dmg, PlayerState.Mp);
            PlayerState.Mp -= absorbed;
            int remaining = dmg - absorbed;
            if (remaining > 0) PlayerState.Hp -= remaining;
            ShowDamageNumber(_player.Position + Vector2.up * 0.5f, dmg, false,
                new Color(0.27f, 0.53f, 1f));
        }
        else
        {
            PlayerState.Hp -= dmg;
            ShowDamageNumber(_player.Position + Vector2.up * 0.5f, dmg, false,
                new Color(1f, 0.27f, 0.27f));
        }

        if (PlayerState.Hp <= 0)
            _onPlayerDeath?.Invoke();
    }

    public void ExecuteSkill(int slot)
    {
        if (Skills == null || PlayerState == null || _player == null) return;

        float nowMs = Time.time * 1000f;
        var result = Skills.UseSkill(slot, PlayerState.Mp, nowMs);
        if (!result.success || result.skill == null) return;

        PlayerState.Mp -= result.mpCost;
        var skill = result.skill;

        // Skill use SFX
        string sfxName = skill.tree switch
        {
            "magic" => "sfx_combat_ultimate",
            "ranged" => "sfx_escape_whoosh",
            _ => "sfx_attack"
        };
        AudioManager.Instance?.PlaySFX(sfxName, 0.1f);

        _comboSystem.RecordSkill(skill.id, nowMs);
        var combo = _comboSystem.CheckCombo(skill.id, nowMs);

        float aoeBonus = Skills.GetAoeBonus(skill.id);
        float durBonus = Skills.GetDurationBonus(skill.id);
        float dmgMult = Skills.GetDamageMultiplier(skill.id);
        float buffBonus = Skills.GetBuffBonus(skill.id);

        if (combo.triggered)
        {
            switch (combo.bonusType)
            {
                case "damage_mult": dmgMult *= combo.bonusValue; break;
                case "aoe_expand": aoeBonus *= combo.bonusValue; break;
                case "duration_extend": durBonus *= combo.bonusValue; break;
            }
            EventBus.Emit(new ComboEvent { name = combo.comboName });
        }
        float range = skill.range * (1f + (aoeBonus - 1f) * 0.5f);

        Vector2 playerPos = _player.Position;
        Vector2 aimDir = _player.AimDirection;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x);
        float clampedDist = range;
        float targetX = playerPos.x + Mathf.Cos(angle) * clampedDist;
        float targetY = playerPos.y + Mathf.Sin(angle) * clampedDist;

        var monsters = _cachedMonsters ?? new List<MonsterController>();
        var stats = _getStats();

        if (skill.actions != null && skill.actions.Length > 0 && string.IsNullOrEmpty(skill.behavior))
        {
            var ctx = BuildActionContext(skill, result.skillLevel, stats, monsters, nowMs,
                dmgMult, aoeBonus, durBonus, buffBonus, range, angle, targetX, targetY);
            _actionRunner.ExecuteSkill(skill.actions, ctx);
        }
        else
        {
            var ctx = BuildSkillContext(skill, result.skillLevel, stats, monsters, nowMs,
                dmgMult, aoeBonus, durBonus, buffBonus, range, angle, targetX, targetY);
            _skillExecutor.Execute(ctx);
        }
    }

    SkillContext BuildSkillContext(SkillDef skill, int skillLevel, Stats stats,
        List<MonsterController> monsters, float now, float dmgMult, float aoeBonus,
        float durBonus, float buffBonus, float range, float angle, float tx, float ty)
    {
        return new SkillContext
        {
            player = _player.transform,
            stats = stats,
            monsters = monsters,
            now = now,
            skill = skill,
            skillLevel = skillLevel,
            dmgMult = dmgMult,
            aoe = skill.aoe * aoeBonus,
            duration = durBonus,
            range = range,
            buffBonus = buffBonus,
            angle = angle,
            targetX = tx,
            targetY = ty,
            dealDamage = DealDamageToMonster,
            showDmg = (x, y, amt, crit, c) => ShowDamageNumber(new Vector2(x, y), amt, crit, c),
            showEffect = (x, y) => SkillVFX.ShowAtPosition(this, x, y),
            onKill = m => _onMonsterDeath?.Invoke(m),
            recalcStats = s => { },
            shakeCamera = (d, i) => CameraShake.Shake(this, d, i),
            applyPlayerEffect = (type, expires, val) => _playerEffects.Apply(type, expires, val),
            healPlayer = amount =>
            {
                if (PlayerState != null)
                    PlayerState.Hp = Mathf.Min(PlayerState.Hp + amount, stats.maxHp);
            }
        };
    }

    ActionContext BuildActionContext(SkillDef skill, int skillLevel, Stats stats,
        List<MonsterController> monsters, float now, float dmgMult, float aoeBonus,
        float durBonus, float buffBonus, float range, float angle, float tx, float ty)
    {
        var ctx = new ActionContext
        {
            player = _player.transform,
            stats = stats,
            monsters = monsters,
            now = now,
            skill = skill,
            skillLevel = skillLevel,
            dmgMult = dmgMult,
            aoe = skill.aoe * aoeBonus,
            duration = durBonus,
            range = range,
            buffBonus = buffBonus,
            angle = angle,
            targetX = tx,
            targetY = ty,
            dealDamage = DealDamageToMonster,
            showDmg = (x, y, amt, crit, c) => ShowDamageNumber(new Vector2(x, y), amt, crit, c),
            showEffect = (x, y) => SkillVFX.ShowAtPosition(this, x, y),
            onKill = m => _onMonsterDeath?.Invoke(m),
            recalcStats = s => { },
            shakeCamera = (d, i) => CameraShake.Shake(this, d, i),
            applyPlayerEffect = (type, expires, val) => _playerEffects.Apply(type, expires, val),
            healPlayer = amount =>
            {
                if (PlayerState != null)
                    PlayerState.Hp = Mathf.Min(PlayerState.Hp + amount, stats.maxHp);
            },
            runner = _actionRunner
        };
        return ctx;
    }

    bool DealDamageToMonster(MonsterController m, float baseDmg, bool isCrit, int tintColor)
    {
        if (m == null || m.IsDead) return false;
        m.LastHitByPlayerTime = Time.time;
        int dmg = CombatSystem.CalcDamage(Mathf.RoundToInt(baseDmg), m.EffectiveDef, isCrit);
        bool dead = m.TakeDamage(dmg);
        ShowDamageNumber(m.Position + Vector2.up * 0.5f, dmg, isCrit);
        if (dead) _onMonsterDeath?.Invoke(m);
        return dead;
    }

    public void ShowDamageNumber(Vector2 pos, int amount, bool isCrit, Color? color = null)
    {
        DamageText.Spawn(this, pos, amount, isCrit, color);
    }

    public void ShowFloatingText(Vector2 pos, string msg, Color? color = null)
    {
        DamageText.SpawnText(this, pos, msg, color);
    }
}
