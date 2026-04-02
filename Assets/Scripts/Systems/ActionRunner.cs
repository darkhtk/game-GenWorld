using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionContext : SkillContext
{
    public ActionRunner runner;
}

public class ActionRunner
{
    readonly Dictionary<string, Action<SkillAction, ActionContext, float, float, List<MonsterController>>> _handlers = new();

    public ActionRunner()
    {
        Register("deal_damage", HandleDealDamage);
        Register("apply_effect", HandleApplyEffect);
        Register("apply_buff", HandleApplyBuff);
        Register("spawn_projectile", HandleSpawnProjectile);
        Register("spawn_area", HandleSpawnArea);
        Register("screen_shake", HandleScreenShake);
        Register("visual", HandleVisual);
        Register("teleport", HandleTeleport);
    }

    public void Register(string type,
        Action<SkillAction, ActionContext, float, float, List<MonsterController>> handler) =>
        _handlers[type] = handler;

    public void Run(SkillAction[] actions, ActionContext ctx, float hitX, float hitY,
        List<MonsterController> hitTargets = null)
    {
        if (actions == null) return;
        foreach (var action in actions)
        {
            if (_handlers.TryGetValue(action.type, out var handler))
                handler(action, ctx, hitX, hitY, hitTargets);
        }
    }

    public void ExecuteSkill(SkillAction[] actions, ActionContext ctx)
    {
        Run(actions, ctx, ctx.targetX, ctx.targetY);
    }

    void HandleDealDamage(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        float ratio = a.ratio > 0 ? a.ratio : 1f;
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult * ratio);
        bool crit = a.crit || CombatSystem.CalcCrit(ctx.stats.crit);

        if (a.aoe > 0)
        {
            float effectiveAoe = a.aoe * ctx.aoe;
            foreach (var m in ctx.monsters)
            {
                if (m == null || m.IsDead) continue;
                float dist = Vector2.Distance(new Vector2(hx, hy), m.Position);
                if (dist <= effectiveAoe)
                    ctx.dealDamage?.Invoke(m, baseDmg, crit, a.color);
            }
        }
        else if (targets != null && targets.Count > 0)
        {
            foreach (var m in targets)
                ctx.dealDamage?.Invoke(m, baseDmg, crit, a.color);
        }
    }

    void HandleApplyEffect(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        float dur = (a.duration > 0 ? a.duration : 2000f) * ctx.duration;
        float effectiveAoe = a.aoe > 0 ? a.aoe * ctx.aoe : 0;

        var affected = new List<MonsterController>();
        if (effectiveAoe > 0)
        {
            foreach (var m in ctx.monsters)
            {
                if (m == null || m.IsDead) continue;
                if (Vector2.Distance(new Vector2(hx, hy), m.Position) <= effectiveAoe)
                    affected.Add(m);
            }
        }
        else if (targets != null) affected.AddRange(targets);

        foreach (var m in affected)
        {
            switch (a.effect)
            {
                case "stun":
                    m.Effects.Apply("stun", ctx.now + dur, 0);
                    break;
                case "slow":
                    m.Effects.Apply("slow", ctx.now + dur, a.value > 0 ? a.value : 0.5f);
                    break;
                case "dot":
                    m.Effects.ApplyDot(ctx.now + dur,
                        Mathf.RoundToInt(ctx.stats.atk * (a.value > 0 ? a.value : 0.5f)));
                    break;
                case "knockback":
                    break;
            }
        }
    }

    void HandleApplyBuff(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        float dur = (a.duration > 0 ? a.duration : 5000f) * ctx.duration;
        float val = a.buffValue > 0 ? a.buffValue * ctx.buffBonus : 1.5f;
        ctx.applyPlayerEffect?.Invoke(a.buffType ?? "rage", ctx.now + dur, val);
        if (a.buffType == "heal")
            ctx.healPlayer?.Invoke(Mathf.RoundToInt(ctx.stats.maxHp * val));
    }

    void HandleSpawnProjectile(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        // Phase 4: delegates to ProjectileSpawner
    }

    void HandleSpawnArea(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        // Phase 4: delegates to AreaEffectSpawner
    }

    void HandleScreenShake(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        ctx.shakeCamera?.Invoke(a.duration, a.intensity);
    }

    void HandleVisual(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        ctx.showEffect?.Invoke(hx, hy);
    }

    void HandleTeleport(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        // Phase 4: move player toward/behind target
    }
}
