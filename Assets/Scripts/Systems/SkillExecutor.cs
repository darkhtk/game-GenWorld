using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillContext
{
    public Transform player;
    public Stats stats;
    public List<MonsterController> monsters;
    public float now;

    public SkillDef skill;
    public int skillLevel;
    public float dmgMult;
    public float aoe;
    public float duration;
    public float range;
    public float buffBonus;

    public float angle;
    public float targetX, targetY;

    public Func<MonsterController, float, bool, int, bool> dealDamage;
    public Action<float, float, int, bool, Color?> showDmg;
    public Action<float, float> showEffect;
    public Action<MonsterController> onKill;
    public Action<Stats> recalcStats;
    public Action<float, float> shakeCamera;
    public Action<string, float, float> applyPlayerEffect;
    public Action<int> healPlayer;
}

public class SkillExecutor
{
    readonly Dictionary<string, Action<SkillContext>> _handlers = new();

    public SkillExecutor()
    {
        Register("self_buff", HandleSelfBuff);
        Register("aoe_damage", HandleAoeDamage);
        Register("single_target", HandleSingleTarget);
        Register("projectile_multi", HandleProjectileMulti);
        Register("chain", HandleChain);
        Register("aoe_debuff", HandleAoeDebuff);
        Register("place_trap", HandlePlaceTrap);
        Register("place_blizzard", HandlePlaceBlizzard);
    }

    public void Register(string behavior, Action<SkillContext> handler) =>
        _handlers[behavior] = handler;

    public void Execute(SkillContext ctx)
    {
        string behavior = ctx.skill.behavior;
        if (!string.IsNullOrEmpty(behavior) && _handlers.TryGetValue(behavior, out var handler))
        {
            handler(ctx);
            return;
        }
        if (ctx.aoe > 0) HandleAoeDamage(ctx);
        else HandleSingleTarget(ctx);
    }

    void HandleSelfBuff(SkillContext ctx)
    {
        string buff = ctx.skill.buffType ?? ctx.skill.effect ?? "rage";
        float val = ctx.skill.buffValue * ctx.buffBonus;
        float dur = (ctx.skill.effectDuration > 0 ? ctx.skill.effectDuration : 5000f) * ctx.duration;
        ctx.applyPlayerEffect?.Invoke(buff, ctx.now + dur, val);
        if (buff == "heal") ctx.healPlayer?.Invoke(Mathf.RoundToInt(ctx.stats.maxHp * val));
        ctx.showEffect?.Invoke(ctx.player.position.x, ctx.player.position.y);
    }

    void HandleAoeDamage(SkillContext ctx)
    {
        float tx = ctx.targetX, ty = ctx.targetY;
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);

        foreach (var m in ctx.monsters)
        {
            if (m == null || m.IsDead) continue;
            float dist = Vector2.Distance(new Vector2(tx, ty), m.Position);
            if (dist <= ctx.aoe)
            {
                bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
                ctx.dealDamage?.Invoke(m, baseDmg, crit, 0);
            }
        }
        ctx.showEffect?.Invoke(tx, ty);
        if (ctx.dmgMult >= 3f) ctx.shakeCamera?.Invoke(200f, 0.005f);
    }

    void HandleSingleTarget(SkillContext ctx)
    {
        var target = CombatSystem.FindClosest(
            new Vector2(ctx.player.position.x, ctx.player.position.y),
            ctx.monsters.ToArray(), ctx.range,
            m => m.Position, m => !m.IsDead);

        if (target == null) return;
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);
        bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
        ctx.dealDamage?.Invoke(target, baseDmg, crit, 0);
        ctx.showEffect?.Invoke(target.Position.x, target.Position.y);
    }

    void HandleProjectileMulti(SkillContext ctx) { /* Phase 4 */ }
    void HandleChain(SkillContext ctx) { /* Phase 4 */ }
    void HandleAoeDebuff(SkillContext ctx) { /* Phase 4 */ }
    void HandlePlaceTrap(SkillContext ctx) { /* Phase 4 */ }
    void HandlePlaceBlizzard(SkillContext ctx) { /* Phase 4 */ }
}
