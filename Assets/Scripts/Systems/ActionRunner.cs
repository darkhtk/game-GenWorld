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
        float speed = a.speed > 0 ? a.speed : 200f;
        float range = a.range > 0 ? a.range : ctx.range;
        bool piercing = a.piercing;
        float ratio = a.ratio > 0 ? a.ratio : 1f;
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult * ratio);
        int count = a.count > 0 ? a.count : 1;
        Vector2 origin = (Vector2)ctx.player.position;

        var projTargets = new List<(Vector2 start, Vector2 end)>();
        string pattern = a.pattern;

        if (pattern == "scatter")
        {
            float sr = a.scatterRadius > 0 ? a.scatterRadius : 60f;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float d = Random.value * sr;
                var target = new Vector2(ctx.targetX + Mathf.Cos(angle) * d,
                    ctx.targetY + Mathf.Sin(angle) * d);
                projTargets.Add((origin, target));
            }
        }
        else if (pattern == "fan")
        {
            int fanCount = count > 0 ? count : 3 + Mathf.Min(ctx.skillLevel - 1, 2);
            float spread = a.spread > 0 ? a.spread : Mathf.PI / 6f + (fanCount - 3) * 0.05f;
            for (int i = 0; i < fanCount; i++)
            {
                float sa = ctx.angle - spread + (2f * spread * i) / Mathf.Max(1, fanCount - 1);
                var target = origin + new Vector2(Mathf.Cos(sa), Mathf.Sin(sa)) * range;
                projTargets.Add((origin, target));
            }
        }
        else if (pattern == "radial")
        {
            int radialCount = count > 0 ? count : 8;
            for (int i = 0; i < radialCount; i++)
            {
                float sa = (float)i / radialCount * Mathf.PI * 2f;
                var target = origin + new Vector2(Mathf.Cos(sa), Mathf.Sin(sa)) * range;
                projTargets.Add((origin, target));
            }
        }
        else
        {
            var target = origin + new Vector2(Mathf.Cos(ctx.angle), Mathf.Sin(ctx.angle)) * range;
            projTargets.Add((origin, target));
        }

        var runner = ctx.runner;

        foreach (var (start, end) in projTargets)
        {
            var go = new GameObject("SkillProjectile");
            var proj = go.AddComponent<Projectile>();
            float size = a.value > 0 ? a.value : 5f;
            proj.Init(start, end, speed, a.color, size, piercing);

            proj.OnHitMonster = m =>
            {
                bool crit = a.crit || CombatSystem.CalcCrit(ctx.stats.crit);
                ctx.dealDamage?.Invoke(m, baseDmg, crit, a.color);
                if (a.onHit != null && a.onHit.Length > 0)
                    runner?.Run(a.onHit, ctx, m.Position.x, m.Position.y,
                        new List<MonsterController> { m });
            };

            proj.OnArrive = arrivePos =>
            {
                if (a.onArrive != null && a.onArrive.Length > 0)
                    runner?.Run(a.onArrive, ctx, arrivePos.x, arrivePos.y);
            };
        }
    }

    void HandleSpawnArea(SkillAction a, ActionContext ctx, float hx, float hy,
        List<MonsterController> targets)
    {
        float aoe = a.aoe > 0 ? a.aoe * ctx.aoe : ctx.aoe;
        float dur = a.duration > 0 ? a.duration * ctx.duration : 5000f * ctx.duration;
        float tick = a.tickInterval > 0 ? a.tickInterval : 1000f;

        if (a.pattern == "trap")
        {
            foreach (var m in ctx.monsters)
            {
                if (m == null || m.IsDead) continue;
                if (Vector2.Distance(new Vector2(hx, hy), m.Position) <= aoe)
                {
                    float ratio = a.ratio > 0 ? a.ratio : 1f;
                    int dmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult * ratio);
                    bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
                    ctx.dealDamage?.Invoke(m, dmg, crit, a.color);
                }
            }
            ctx.showEffect?.Invoke(hx, hy);
            return;
        }

        bool followPlayer = a.pattern == "self";
        var go = new GameObject(followPlayer ? "FollowingArea" : "GroundArea");
        var area = go.AddComponent<AreaEffect>();
        area.Init(followPlayer ? ctx.player : null, hx, hy, aoe, dur, tick,
            a.onTick, ctx, ctx.runner);
        ctx.showEffect?.Invoke(hx, hy);
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
        Vector2 origin = (Vector2)ctx.player.position;

        if (a.behindTarget)
        {
            var target = CombatSystem.FindClosest(origin, ctx.monsters.ToArray(),
                ctx.range, m => m.Position, m => !m.IsDead);
            if (target != null)
            {
                Vector2 delta = target.Position - origin;
                float angle = Mathf.Atan2(delta.y, delta.x);
                float dist = a.distance > 0 ? a.distance : 30f;
                ctx.player.position = new Vector3(
                    target.Position.x + Mathf.Cos(angle) * dist,
                    target.Position.y + Mathf.Sin(angle) * dist, 0);
            }
        }
        else if (a.toTarget)
        {
            float maxDist = Mathf.Min(
                Vector2.Distance(origin, new Vector2(ctx.targetX, ctx.targetY)), ctx.range);
            Vector2 dir = (new Vector2(ctx.targetX, ctx.targetY) - origin).normalized;
            Vector2 dest = origin + dir * maxDist;
            ctx.player.position = new Vector3(dest.x, dest.y, 0);
        }
    }
}
