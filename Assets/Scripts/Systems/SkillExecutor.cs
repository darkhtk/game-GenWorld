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

    void HandleProjectileMulti(SkillContext ctx)
    {
        int shotCount = 3 + Mathf.Min(ctx.skillLevel - 1, 2);
        float spread = Mathf.PI / 6f + (shotCount - 3) * 0.05f;
        float pathWidth = 16f;
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);
        Vector2 playerPos = (Vector2)ctx.player.position;

        for (int i = 0; i < shotCount; i++)
        {
            float sa = ctx.angle - spread + (2f * spread * i) / Mathf.Max(1, shotCount - 1);
            MonsterController best = null;
            float bestD = ctx.range + 1f;

            foreach (var m in ctx.monsters)
            {
                if (m == null || m.IsDead) continue;
                float d = Vector2.Distance(playerPos, m.Position);
                if (d > ctx.range) continue;

                Vector2 delta = m.Position - playerPos;
                float proj = delta.x * Mathf.Cos(sa) + delta.y * Mathf.Sin(sa);
                if (proj < 0) continue;
                float perp = Mathf.Abs(-delta.x * Mathf.Sin(sa) + delta.y * Mathf.Cos(sa));
                if (perp <= pathWidth && proj < bestD)
                {
                    bestD = proj;
                    best = m;
                }
            }

            if (best != null)
            {
                bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
                ctx.dealDamage?.Invoke(best, baseDmg, crit, 0x6b9bff);
            }
        }
        ctx.showEffect?.Invoke(ctx.targetX, ctx.targetY);
    }

    void HandleChain(SkillContext ctx)
    {
        float coneHalf = Mathf.PI / 4f;
        MonsterController primary = null;
        float bestD = ctx.range + 1f;
        Vector2 playerPos = (Vector2)ctx.player.position;

        foreach (var m in ctx.monsters)
        {
            if (m == null || m.IsDead) continue;
            float d = Vector2.Distance(playerPos, m.Position);
            if (d > ctx.range) continue;

            Vector2 delta = m.Position - playerPos;
            float a = Mathf.Atan2(delta.y, delta.x);
            float diff = Mathf.Abs(ctx.angle - a);
            if (diff > Mathf.PI) diff = 2f * Mathf.PI - diff;
            if (diff <= coneHalf && d < bestD) { bestD = d; primary = m; }
        }

        if (primary == null) return;

        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);
        bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
        ctx.dealDamage?.Invoke(primary, baseDmg, crit, 0xffff44);

        var hit = new List<MonsterController> { primary };
        int maxChains = 2 + Mathf.Min(ctx.skillLevel - 1, 2);
        MonsterController last = primary;
        float bounceRange = ctx.aoe > 0 ? ctx.aoe : 80f;

        for (int c = 0; c < maxChains; c++)
        {
            MonsterController next = null;
            float nextD = bounceRange + 1f;
            foreach (var m in ctx.monsters)
            {
                if (m == null || m.IsDead || hit.Contains(m)) continue;
                float d = Vector2.Distance(last.Position, m.Position);
                if (d < nextD) { nextD = d; next = m; }
            }
            if (next == null) break;
            crit = CombatSystem.CalcCrit(ctx.stats.crit);
            ctx.dealDamage?.Invoke(next, Mathf.RoundToInt(baseDmg * 0.7f), crit, 0xffff44);
            hit.Add(next);
            last = next;
        }
        ctx.showEffect?.Invoke(ctx.targetX, ctx.targetY);
    }

    void HandleAoeDebuff(SkillContext ctx)
    {
        Vector2 playerPos = (Vector2)ctx.player.position;
        float dur = (ctx.skill.effectDuration > 0 ? ctx.skill.effectDuration : 8000f) * ctx.duration;

        foreach (var m in ctx.monsters)
        {
            if (m == null || m.IsDead) continue;
            if (Vector2.Distance(playerPos, m.Position) <= ctx.aoe)
                m.Effects.Apply("slow", ctx.now + dur, 0.5f);
        }
        ctx.showEffect?.Invoke(playerPos.x, playerPos.y);
    }

    void HandlePlaceTrap(SkillContext ctx)
    {
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);
        float dur = (ctx.skill.effectDuration > 0 ? ctx.skill.effectDuration : 4000f) * ctx.duration;
        float tx = ctx.targetX, ty = ctx.targetY;

        // Immediate AoE damage at trap location
        foreach (var m in ctx.monsters)
        {
            if (m == null || m.IsDead) continue;
            if (Vector2.Distance(new Vector2(tx, ty), m.Position) <= ctx.aoe)
            {
                bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
                ctx.dealDamage?.Invoke(m, baseDmg, crit, 0);
            }
        }
        ctx.showEffect?.Invoke(tx, ty);
    }

    void HandlePlaceBlizzard(SkillContext ctx)
    {
        int baseDmg = Mathf.RoundToInt(ctx.stats.atk * ctx.dmgMult);
        float dur = (ctx.skill.effectDuration > 0 ? ctx.skill.effectDuration : 5000f) * ctx.duration;
        float tx = ctx.targetX, ty = ctx.targetY;

        // Immediate AoE damage + slow at blizzard location
        foreach (var m in ctx.monsters)
        {
            if (m == null || m.IsDead) continue;
            if (Vector2.Distance(new Vector2(tx, ty), m.Position) <= ctx.aoe)
            {
                bool crit = CombatSystem.CalcCrit(ctx.stats.crit);
                ctx.dealDamage?.Invoke(m, baseDmg, crit, 0x88ccff);
                m.Effects.Apply("slow", ctx.now + dur, 0.5f);
            }
        }
        ctx.showEffect?.Invoke(tx, ty);
    }
}
