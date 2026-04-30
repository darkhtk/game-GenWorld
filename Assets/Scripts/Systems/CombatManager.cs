using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    PlayerController _player;
    PlayerAnimator _playerAnimator;
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
    List<MonsterController> _pendingKills;

    public bool IsInCombat
    {
        get
        {
            if (_cachedMonsters == null) return false;
            float now = Time.time;
            float window = GameConfig.MonsterAggro.IsInCombatWindow;
            for (int i = 0; i < _cachedMonsters.Count; i++)
            {
                var m = _cachedMonsters[i];
                if (m != null && !m.IsDead && now - m.LastHitByPlayerTime < window)
                    return true;
            }
            return false;
        }
    }

    // Cached delegates to avoid per-skill-use allocation
    System.Func<MonsterController, float, bool, int, bool> _dealDmgDel;
    System.Action<float, float, int, bool, Color?> _showDmgDel;
    System.Action<float, float> _showEffectDel;
    System.Action<MonsterController> _onKillDel;
    static readonly System.Action<Stats> NoopRecalc = _ => { };
    System.Action<float, float> _shakeCamDel;
    System.Action<string, float, float> _applyEffectDel;

    // Set by Director after Init
    public SkillSystem Skills { get; set; }
    public PlayerStats PlayerState { get; set; }

    public void Init(PlayerController player, System.Func<Stats> getStats, System.Func<int> getLevel,
        System.Action<MonsterController> onMonsterDeath, System.Action onPlayerDeath, EffectHolder playerEffects)
    {
        _player = player;
        _playerAnimator = player != null ? player.GetComponent<PlayerAnimator>() : null;
        _getStats = getStats;
        _getLevel = getLevel;
        _onMonsterDeath = onMonsterDeath;
        _onPlayerDeath = onPlayerDeath;
        _playerEffects = playerEffects;
        _skillExecutor = new SkillExecutor();
        _actionRunner = new ActionRunner();
        _comboSystem = new ComboSystem();
        _pendingKills ??= new List<MonsterController>();
        _dealDmgDel = DealDamageToMonster;
        _showDmgDel = (x, y, amt, crit, c) => ShowDamageNumber(new Vector2(x, y), amt, crit, c);
        _showEffectDel = (x, y) => SkillVFX.ShowAtPosition(this, x, y);
        _onKillDel = m => _pendingKills?.Add(m);
        _shakeCamDel = (d, i) => CameraShake.Shake(this, d, i);
        _applyEffectDel = (type, expires, val) => _playerEffects.Apply(type, expires, val);
    }

    public void PerformAutoAttack(List<MonsterController> monsters)
    {
        if (monsters == null) return;
        _cachedMonsters = monsters;

        float nowMs = Time.time * 1000f;
        if (!Input.GetMouseButton(0)) return;
        ExecuteAutoAttack(monsters, nowMs);
    }

    bool ExecuteAutoAttack(List<MonsterController> monsters, float nowMs)
    {
        if (nowMs - _lastAutoAttackTime < GameConfig.AutoAttackCooldown) return false;

        _lastAutoAttackTime = nowMs;

        var stats = _getStats();
        int level = _getLevel();
        float swingRange = GameConfig.AutoAttackBaseRange + level * GameConfig.AutoAttackRangePerLevel;
        float levelDmgMult = 1f + level * GameConfig.AutoAttackDmgPerLevel;

        Vector2 playerPos = _player.Position;
        Vector2 aimDir = _player.AimDirection;
        if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector2.right;
        float swingAngle = Mathf.Atan2(aimDir.y, aimDir.x);

        bool stealthActive = _playerEffects.Has("stealth");
        bool landedHit = false;
        _playerAnimator?.PlayAttack(0.18f);
        SkillVFX.ShowAutoAttackCast(this, playerPos, aimDir, stealthActive);
        _pendingKills ??= new List<MonsterController>();
        _pendingKills.Clear();

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (m == null || m.IsDead) continue;
            if (!CombatSystem.IsOnScreen(m.Position)) continue;
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
            SkillVFX.ShowAutoAttackImpact(this, m.Position, aimDir, isCrit);
            AudioManager.Instance?.PlaySFX(isCrit ? "sfx_critical_hit" : "sfx_attack", 0.1f);
            if (dead) _pendingKills.Add(m);
            landedHit = true;
        }

        for (int i = 0; i < _pendingKills.Count; i++)
            _onMonsterDeath?.Invoke(_pendingKills[i]);
        _pendingKills.Clear();

        if (stealthActive)
            _playerEffects.Remove("stealth");

        return landedHit;
    }

    public bool DebugPerformAutoAttack(List<MonsterController> monsters, float nowMs)
    {
        if (monsters == null) return false;
        _cachedMonsters = monsters;
        return ExecuteAutoAttack(monsters, nowMs);
    }

    public void HandleMonsterAttacks(List<MonsterController> monsters, float now)
    {
        if (monsters == null || _player == null) return;
        _cachedMonsters = monsters;
        TickPlayerHostileEffects(now);

        bool playerDodging = _player.Invincible || _player.IsDodging;
        float dodgeAggroMult = GameConfig.MonsterAggro.DodgeAggroSyncRangeMult;

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (m == null || m.IsDead || m.Def == null) continue;

            float atkRangeSync = m.Def.attackRange * dodgeAggroMult;
            float distSq = (m.Position - _player.Position).sqrMagnitude;
            float atkRangeSyncSq = atkRangeSync * atkRangeSync;

            // Dodge-aggro sync: keep nearby monsters engaged during the i-frame so
            // they do not slip into Return on the next frame. See SPEC-S-101 §3-2.
            if (playerDodging && distSq <= atkRangeSyncSq)
                m.LastHitByPlayerTime = Time.time;

            if (PlayerState != null && PlayerState.Hp <= 0) break;
            if (distSq > atkRangeSyncSq) continue;

            // Run pattern progression even while the player dodges so boss windups,
            // phase-enter actions, and multi-step patterns are not nullified by a
            // single 0.2s i-frame. ApplyDamageToPlayer is the only thing dodge gates.
            var phaseEnterActions = m.ConsumePhaseEnterActions();
            if (phaseEnterActions != null && phaseEnterActions.Length > 0)
                ExecuteMonsterActions(m, phaseEnterActions, monsters, now, m.Position);

            if (m.TryGetAttackToExecute(now, _player.Position, out var pattern, out var lockedTarget))
            {
                ExecuteMonsterPattern(m, pattern, monsters, now, lockedTarget);
                continue;
            }

            if (m.HasPendingAttack || (m.GetActiveAttackPatterns()?.Length ?? 0) > 0)
                continue;

            if (!m.CanAttack(now)) continue;

            m.MarkAttacked(now);

            if (m.Def.ranged != null)
            {
                FireMonsterProjectile(m);
                continue;
            }

            // Plain melee tick: damage is gated by ApplyDamageToPlayer's invincibility
            // checks (mana_shield + Hp <= 0 already handled there). During dodge we
            // skip the actual damage application but still play impact VFX/SFX so the
            // attack does not silently disappear.
            if (playerDodging) continue;

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
            if (m == null || m.IsDead) return;
            float hitDist = Vector2.Distance(arrivePos, _player.Position);
            if (hitDist > 40f) return;

            // Update LastHitByPlayerTime for engagement tracking even during dodge
            m.LastHitByPlayerTime = Time.time;

            if (_player.Invincible || _player.IsDodging) return;
            var stats = _getStats();
            int dmg = CombatSystem.CalcDamage(m.EffectiveAtk, stats.def, false);
            ApplyDamageToPlayer(dmg);
            SkillVFX.ShowAtPosition(this, "vfx_ranged_hit", _player.Position.x, _player.Position.y);
        };
    }

    void ExecuteMonsterPattern(MonsterController monster, MonsterAttackPattern pattern,
        List<MonsterController> monsters, float nowMs, Vector2 targetPos)
    {
        if (monster == null || pattern?.actions == null || pattern.actions.Length == 0)
            return;

        ExecuteMonsterActions(monster, pattern.actions, monsters, nowMs, targetPos);
    }

    void ExecuteMonsterActions(MonsterController monster, SkillAction[] actions,
        List<MonsterController> monsters, float nowMs, Vector2 targetPos)
    {
        if (monster == null || actions == null || actions.Length == 0 || _player == null)
            return;

        Vector2 from = monster.Position;
        Vector2 delta = targetPos - from;
        float angle = delta.sqrMagnitude > 0f ? Mathf.Atan2(delta.y, delta.x) : 0f;
        var ctx = BuildMonsterActionContext(monster, monsters, nowMs, angle, targetPos.x, targetPos.y);
        _actionRunner.Run(actions, ctx, targetPos.x, targetPos.y);
    }

    void ApplyDamageToPlayer(int dmg)
    {
        if (PlayerState == null || PlayerState.Hp <= 0) return;
        // Single chokepoint for invincibility — pattern/projectile paths can run during
        // dodge (so windups/phases progress) but never actually subtract Hp here.
        if (_player != null && (_player.Invincible || _player.IsDodging)) return;

        if (_playerEffects.Has("mana_shield") && PlayerState.Mp > 0)
        {
            int absorbed = Mathf.Min(dmg, PlayerState.Mp);
            PlayerState.Mp -= absorbed;
            int remaining = dmg - absorbed;
            if (remaining > 0) PlayerState.Hp -= remaining;
            ShowDamageNumber(_player.Position + Vector2.up * 0.5f, dmg, false,
                new Color(0.4f, 0.533f, 1f));
        }
        else
        {
            PlayerState.Hp -= dmg;
            ScreenFlash.Damage();
            ShowDamageNumber(_player.Position + Vector2.up * 0.5f, dmg, false,
                new Color(1f, 0.4f, 0.333f));
        }

        if (PlayerState.Hp <= 0)
            _onPlayerDeath?.Invoke();
    }

    public void ExecuteSkill(int slot)
    {
        if (Skills == null || PlayerState == null || _player == null) return;

        float nowMs = Time.time * 1000f;
        var result = Skills.UseSkill(slot, PlayerState.Mp, nowMs);
        if (!result.success || result.skill == null)
        {
            if (result.reason == "no_mp")
            {
                ShowFloatingText(_player.Position + Vector2.up, "MP 부족", new Color(0.4f, 0.6f, 1f));
                AudioManager.Instance?.PlaySFX("sfx_error");
            }
            else if (result.reason == "cooldown")
            {
                AudioManager.Instance?.PlaySFX("sfx_error");
            }
            return;
        }

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
            AudioManager.Instance?.PlaySFX("sfx_combo", 0f);
            ShowFloatingText(_player.Position + Vector2.up, $"COMBO: {combo.comboName}!", new Color(1f, 0.867f, 0.267f)); // #ffdd44
        }
        float range = skill.range * (1f + (aoeBonus - 1f) * 0.5f);

        Vector2 playerPos = _player.Position;
        Vector2 cursorWorld = _player.MouseWorldPosition;
        Vector2 aimDir = _player.AimDirection;
        Vector2 cursorDelta = cursorWorld - playerPos;
        if (cursorDelta.sqrMagnitude > 0.0001f)
            aimDir = cursorDelta.normalized;
        if (aimDir.sqrMagnitude < 0.0001f)
            aimDir = Vector2.right;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x);
        Vector2 directionTarget = playerPos + aimDir * range;
        Vector2 clampedCursor = cursorDelta.sqrMagnitude > range * range && range > 0f
            ? playerPos + cursorDelta.normalized * range
            : cursorWorld;
        SkillTargetMode targetMode = SkillTargetingResolver.Resolve(skill);
        Vector2 resolvedTarget = ResolveSkillTargetPoint(targetMode, playerPos, clampedCursor, directionTarget);

        var monsters = _cachedMonsters;
        if (monsters == null) return;
        var stats = _getStats();

        _pendingKills ??= new List<MonsterController>();
        _pendingKills.Clear();

        if (skill.actions != null && skill.actions.Length > 0 && string.IsNullOrEmpty(skill.behavior))
        {
            var ctx = BuildActionContext(skill, result.skillLevel, stats, monsters, nowMs,
                dmgMult, aoeBonus, durBonus, buffBonus, range, angle, targetMode,
                cursorWorld, clampedCursor, directionTarget, resolvedTarget);
            _actionRunner.ExecuteSkill(skill.actions, ctx);
        }
        else
        {
            var ctx = BuildSkillContext(skill, result.skillLevel, stats, monsters, nowMs,
                dmgMult, aoeBonus, durBonus, buffBonus, range, angle, targetMode,
                cursorWorld, clampedCursor, directionTarget, resolvedTarget);
            _skillExecutor.Execute(ctx);
        }

        for (int i = 0; i < _pendingKills.Count; i++)
            _onMonsterDeath?.Invoke(_pendingKills[i]);
        _pendingKills.Clear();
    }

    SkillContext BuildSkillContext(SkillDef skill, int skillLevel, Stats stats,
        List<MonsterController> monsters, float now, float dmgMult, float aoeBonus,
        float durBonus, float buffBonus, float range, float angle, SkillTargetMode targetMode,
        Vector2 cursorWorld, Vector2 clampedCursor, Vector2 directionTarget, Vector2 resolvedTarget)
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
            targetMode = targetMode,
            angle = angle,
            targetX = resolvedTarget.x,
            targetY = resolvedTarget.y,
            cursorX = cursorWorld.x,
            cursorY = cursorWorld.y,
            clampedCursorX = clampedCursor.x,
            clampedCursorY = clampedCursor.y,
            directionTargetX = directionTarget.x,
            directionTargetY = directionTarget.y,
            dealDamage = _dealDmgDel,
            showDmg = _showDmgDel,
            showEffect = _showEffectDel,
            onKill = _onKillDel,
            recalcStats = NoopRecalc,
            shakeCamera = _shakeCamDel,
            applyPlayerEffect = _applyEffectDel,
            healPlayer = amount =>
            {
                if (PlayerState != null)
                {
                    PlayerState.Hp = Mathf.Min(PlayerState.Hp + amount, stats.maxHp);
                    ShowDamageNumber(_player.Position + Vector2.up * 0.5f, amount, false, new Color(0.4f, 1f, 0.533f));
                }
            }
        };
    }

    ActionContext BuildActionContext(SkillDef skill, int skillLevel, Stats stats,
        List<MonsterController> monsters, float now, float dmgMult, float aoeBonus,
        float durBonus, float buffBonus, float range, float angle, SkillTargetMode targetMode,
        Vector2 cursorWorld, Vector2 clampedCursor, Vector2 directionTarget, Vector2 resolvedTarget)
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
            targetMode = targetMode,
            angle = angle,
            targetX = resolvedTarget.x,
            targetY = resolvedTarget.y,
            cursorX = cursorWorld.x,
            cursorY = cursorWorld.y,
            clampedCursorX = clampedCursor.x,
            clampedCursorY = clampedCursor.y,
            directionTargetX = directionTarget.x,
            directionTargetY = directionTarget.y,
            dealDamage = _dealDmgDel,
            showDmg = _showDmgDel,
            showEffect = _showEffectDel,
            onKill = _onKillDel,
            recalcStats = NoopRecalc,
            shakeCamera = _shakeCamDel,
            applyPlayerEffect = _applyEffectDel,
            healPlayer = amount =>
            {
                if (PlayerState != null)
                {
                    PlayerState.Hp = Mathf.Min(PlayerState.Hp + amount, stats.maxHp);
                    ShowDamageNumber(_player.Position + Vector2.up * 0.5f, amount, false, new Color(0.4f, 1f, 0.533f));
                }
            },
            runner = _actionRunner,
            mono = this
        };
        return ctx;
    }

    ActionContext BuildMonsterActionContext(MonsterController monster, List<MonsterController> monsters,
        float nowMs, float angle, float tx, float ty)
    {
        var monsterStats = new Stats
        {
            hp = monster.Hp,
            maxHp = monster.Def.hp,
            atk = monster.EffectiveAtk,
            def = monster.EffectiveDef,
            spd = Mathf.RoundToInt(monster.Def.spd),
            crit = 0
        };

        return new ActionContext
        {
            player = monster.transform,
            primaryTarget = _player.transform,
            targetPlayer = true,
            stats = monsterStats,
            monsters = monsters,
            now = nowMs,
            skill = null,
            skillLevel = 1,
            dmgMult = 1f,
            aoe = 0f,
            duration = 1f,
            range = monster.Def.attackRange,
            buffBonus = 1f,
            targetMode = SkillTargetMode.CursorPosition,
            angle = angle,
            targetX = tx,
            targetY = ty,
            cursorX = tx,
            cursorY = ty,
            clampedCursorX = tx,
            clampedCursorY = ty,
            directionTargetX = tx,
            directionTargetY = ty,
            dealDamageToPrimaryTarget = (baseDmg, isCrit, tintColor) =>
            {
                if (PlayerState == null || _player == null) return false;
                int rawDamage = Mathf.RoundToInt(baseDmg);
                int dmg = CombatSystem.CalcDamage(rawDamage, _getStats().def, isCrit);
                ApplyDamageToPlayer(dmg);
                return dmg > 0;
            },
            showDmg = _showDmgDel,
            showEffect = (x, y) => SkillVFX.ShowAtPosition(this, x, y),
            recalcStats = NoopRecalc,
            shakeCamera = _shakeCamDel,
            applyEffectToPrimaryTarget = ApplyEffectToPlayer,
            healPlayer = _ => { },
            runner = _actionRunner,
            mono = this
        };
    }

    static Vector2 ResolveSkillTargetPoint(
        SkillTargetMode targetMode, Vector2 playerPos, Vector2 clampedCursor, Vector2 directionTarget)
    {
        return targetMode switch
        {
            SkillTargetMode.CursorPosition => clampedCursor,
            SkillTargetMode.CursorDirection => directionTarget,
            _ => playerPos
        };
    }

    bool DealDamageToMonster(MonsterController m, float baseDmg, bool isCrit, int tintColor)
    {
        if (m == null || m.IsDead) return false;
        m.LastHitByPlayerTime = Time.time;
        int dmg = CombatSystem.CalcDamage(Mathf.RoundToInt(baseDmg), m.EffectiveDef, isCrit);
        bool dead = m.TakeDamage(dmg);

        Color? dmgColor = null;
        if (tintColor != 0)
        {
            float r = ((tintColor >> 16) & 0xFF) / 255f;
            float g = ((tintColor >> 8) & 0xFF) / 255f;
            float b = (tintColor & 0xFF) / 255f;
            dmgColor = new Color(r, g, b);
        }
        ShowDamageNumber(m.Position + Vector2.up * 0.5f, dmg, isCrit, dmgColor);
        if (dead)
        {
            if (_pendingKills != null)
                _pendingKills.Add(m);
            else
                _onMonsterDeath?.Invoke(m);
        }
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

    void ApplyEffectToPlayer(string effectType, float expiresAt, float value)
    {
        if (_playerEffects == null)
            return;

        switch (effectType)
        {
            case "stun":
            case "slow":
                _playerEffects.Apply(effectType, expiresAt, value);
                break;
            case "dot":
                int dotDamage = Mathf.Max(1, Mathf.RoundToInt(_getStats().maxHp * value));
                _playerEffects.ApplyDot(expiresAt, dotDamage);
                break;
        }
    }

    void TickPlayerHostileEffects(float nowMs)
    {
        if (_playerEffects == null || PlayerState == null || _player == null)
            return;

        float dotDamage = _playerEffects.TickDot(nowMs);
        if (dotDamage <= 0f)
            return;

        ApplyDamageToPlayer(Mathf.Max(1, Mathf.RoundToInt(dotDamage)));
    }
}
