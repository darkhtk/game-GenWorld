using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour
{
    public MonsterDef Def { get; private set; }
    public int Hp { get; set; }
    public bool IsDead => Hp <= 0;
    public bool DeathProcessed { get; set; }
    public Vector2 Position => (Vector2)transform.position;
    public EffectHolder Effects { get; } = new();
    public MonsterAIState AIState { get; private set; } = MonsterAIState.Patrol;

    // Test-only mutator — production code MUST NOT call this. Intended for EditMode tests
    // that need to drive Return / Chase transitions deterministically without simulating
    // distance/cooldown windows.
    public void SetAIStateForTest(MonsterAIState state) => AIState = state;
    public float SpawnTime { get; set; }
    public float LastHitByPlayerTime { get; set; }
    public bool IsReturning => AIState == MonsterAIState.Return;

    Rigidbody2D _rb;
    Vector2 _spawnPos;
    Vector2? _patrolTarget;
    float _lastAttackTime;
    float _returnStartTime;
    int _currentPhase;
    float _pendingAttackReadyAtMs = -1f;
    Vector2 _pendingAttackTarget;
    MonsterAttackPattern _pendingAttackPattern;
    SkillAction[] _pendingPhaseEnterActions;
    readonly Dictionary<string, float> _patternLastUseTimesMs = new();

    const float ReturnForceTeleport = 5f;
    const float ReturnDamageReduction = 5f;
    const float ReturnSpeedMult = 1.5f;
    const float ReturnReaggroMult = 0.5f;
    // Single source of truth lives in GameConfig.MonsterAggro — see SPEC-S-101 §3-3.
    // The local alias keeps call sites readable without re-introducing a magic number.
    static float RecentHitWindow => GameConfig.MonsterAggro.RecentHitWindow;

    float _atkMult = 1f, _defMult = 1f, _spdMult = 1f, _cooldownMult = 1f;
    MonsterHPBar _hpBar;

    public int EffectiveAtk => Mathf.RoundToInt(Def.atk * _atkMult);
    public int EffectiveDef => Mathf.RoundToInt(Def.def * _defMult);

    public void Init(MonsterDef def, Vector2 spawnPos)
    {
        Def = def;
        Hp = def.hp;
        _spawnPos = spawnPos;
        SpawnTime = Time.time;
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;

        // Ensure collider exists for projectile trigger detection
        if (!TryGetComponent<Collider2D>(out _))
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }
        // Load sprite from Resources (Multiple spritesheet → LoadAll, use first frame)
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var sprites = Resources.LoadAll<Sprite>($"Sprites/{def.sprite}");
            if (sprites == null || sprites.Length == 0)
                sprites = Resources.LoadAll<Sprite>(def.sprite);
            if (sprites != null && sprites.Length > 0)
            {
                _sr = GetComponent<SpriteRenderer>();
                if (_sr != null) _sr.sprite = sprites[0];
            }
        }

        _hpBar = MonsterHPBar.Create(transform, def.hp);
        NameLabel.Create(transform, def.name, new Color(1f, 0.4f, 0.333f), 1.2f); // #ff6655
        ValidateAnimations();
    }

    void ValidateAnimations()
    {
        if (Def.animationDef == null) return;
        string[] required = { "idle", "walk", "attack", "hit", "die" };
        foreach (var state in required)
        {
            if (!Def.animationDef.HasClip(state))
                Debug.LogWarning($"[MonsterController] {Def.name}: missing animation clip for '{state}'");
        }
    }

    public void PlayAnimation(string stateName)
    {
        if (Def.animationDef == null) return;
        var entry = Def.animationDef.GetEntry(stateName);
        if (entry == null || entry.clip == null) return;
        if (!_animatorChecked) { _cachedAnimator = GetComponent<Animator>(); _animatorChecked = true; }
        if (_cachedAnimator != null) _cachedAnimator.Play(stateName, 0, 0f);
    }

    public void UpdateAI(Vector2 playerPos, float now)
    {
        if (IsDead) return;
        if (Effects.Has("stun")) { _rb.linearVelocity = Vector2.zero; return; }

        float distToPlayerSq = (Position - playerPos).sqrMagnitude;
        bool isSlow = Effects.Has("slow");
        float slowFactor = isSlow ? Effects.GetValue("slow") : 1f;
        float speed = Def.spd * _spdMult * slowFactor;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && _flashCoroutine == null)
            _sr.color = isSlow ? new Color(0.5f, 0.7f, 1f) : Color.white;

        // DoT tick (EffectHolder uses ms internally)
        float nowMs = now * 1000f;
        float dotDmg = Effects.TickDot(nowMs);
        if (dotDmg > 0)
        {
            Hp -= Mathf.RoundToInt(dotDmg);
            if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
            if (Hp <= 0) { PlayAnimation("die"); return; }
        }

        // Expire effects
        Effects.Tick(nowMs);

        // Phase check (boss)
        CheckPhase();

        switch (AIState)
        {
            case MonsterAIState.Patrol:
                DoPatrol(speed);
                if (distToPlayerSq <= Def.detectRange * Def.detectRange)
                    AIState = MonsterAIState.Chase;
                break;

            case MonsterAIState.Chase:
                MoveToward(playerPos, speed);
                float atkRangeSq = Def.attackRange * Def.attackRange;
                if (distToPlayerSq <= atkRangeSq)
                    AIState = MonsterAIState.Attack;
                else if ((distToPlayerSq > Def.detectRange * GameConfig.ChaseRangeMult * Def.detectRange * GameConfig.ChaseRangeMult ||
                         (Position - _spawnPos).sqrMagnitude > GameConfig.MaxSpawnDistance * GameConfig.MaxSpawnDistance) &&
                         Time.time - LastHitByPlayerTime > RecentHitWindow)
                {
                    AIState = MonsterAIState.Return;
                    _returnStartTime = Time.time;
                }
                break;

            case MonsterAIState.Attack:
                _rb.linearVelocity = Vector2.zero;
                float chaseThreshSq = Def.attackRange * 1.5f * (Def.attackRange * 1.5f);
                if (distToPlayerSq > chaseThreshSq)
                    AIState = MonsterAIState.Chase;
                break;

            case MonsterAIState.Return:
                if (Time.time - _returnStartTime > ReturnForceTeleport)
                {
                    transform.position = _spawnPos;
                    // Only reset HP if monster was actually damaged (no-op at full HP).
                    if (Hp < Def.hp) Hp = Def.hp;
                    AIState = MonsterAIState.Patrol;
                    break;
                }
                MoveToward(_spawnPos, speed * ReturnSpeedMult);
                if ((Position - _spawnPos).sqrMagnitude < 256f)
                {
                    if (Hp < Def.hp) Hp = Def.hp;
                    AIState = MonsterAIState.Patrol;
                }
                else if (distToPlayerSq <= Def.detectRange * ReturnReaggroMult * (Def.detectRange * ReturnReaggroMult))
                {
                    AIState = MonsterAIState.Chase;
                }
                break;
        }
    }

    void DoPatrol(float speed)
    {
        if (_patrolTarget == null || (Position - _patrolTarget.Value).sqrMagnitude < 0.25f)
        {
            _patrolTarget = _spawnPos + Random.insideUnitCircle * 2f;
        }
        MoveToward(_patrolTarget.Value, speed * 0.5f);
    }

    void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - Position).normalized;
        _rb.linearVelocity = dir * speed;
    }

    void CheckPhase()
    {
        if (Def.phases == null || Def.phases.Length == 0) return;
        float hpPct = (float)Hp / Def.hp * 100f;
        for (int i = Def.phases.Length - 1; i >= 0; i--)
        {
            if (hpPct <= Def.phases[i].hpPercent && _currentPhase <= i)
            {
                _currentPhase = i + 1;
                var phase = Def.phases[i];
                if (phase.statMult != null)
                {
                    _atkMult = phase.statMult.atk;
                    _defMult = phase.statMult.def;
                    _spdMult = phase.statMult.spd;
                    _cooldownMult = phase.statMult.cooldown;
                }
                _pendingPhaseEnterActions = phase.onEnter;
                SkillVFX.ShowAtPosition(this, "vfx_fireball", Position.x, Position.y);
                AudioManager.Instance?.PlaySFX("sfx_phase_transition");
                CameraShake.Shake(this, 500f, 0.012f);
                break;
            }
        }
    }

    public bool TakeDamage(int dmg)
    {
        if (IsDead || DeathProcessed) return false;
        if (IsReturning) dmg = Mathf.Max(1, dmg / (int)ReturnDamageReduction);
        Hp -= dmg;
        if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
        FlashWhite();
        if (Hp <= 0) { PlayAnimation("die"); return true; }
        PlayAnimation("hit");
        return false;
    }

    SpriteRenderer _sr;
    Animator _cachedAnimator;
    bool _animatorChecked;
    Coroutine _flashCoroutine;
    static readonly Color HitTint = new(1f, 0.4f, 0.4f, 1f);
    static readonly WaitForSeconds FlashWait = new(0.1f);

    void FlashWhite()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(DoFlash());
    }

    System.Collections.IEnumerator DoFlash()
    {
        _sr.color = HitTint;
        yield return FlashWait;
        _sr.color = Color.white;
        _flashCoroutine = null;
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (IsDead || _rb == null) return;
        _rb.linearVelocity = direction.normalized * force;
    }

    public bool CanAttack(float now)
    {
        return AIState == MonsterAIState.Attack &&
               now - _lastAttackTime >= Def.attackCooldown * _cooldownMult;
    }

    public void MarkAttacked(float now) => _lastAttackTime = now;

    public SkillAction[] ConsumePhaseEnterActions()
    {
        var actions = _pendingPhaseEnterActions;
        _pendingPhaseEnterActions = null;
        return actions;
    }

    public bool HasPendingAttack => _pendingAttackPattern != null;

    public bool TryGetAttackToExecute(float nowMs, Vector2 targetPosition,
        out MonsterAttackPattern pattern, out Vector2 lockedTargetPosition)
    {
        pattern = null;
        lockedTargetPosition = targetPosition;

        if (_pendingAttackPattern != null)
        {
            if (nowMs < _pendingAttackReadyAtMs)
            {
                lockedTargetPosition = _pendingAttackTarget;
                return false;
            }

            pattern = _pendingAttackPattern;
            lockedTargetPosition = _pendingAttackTarget;
            _pendingAttackPattern = null;
            _pendingAttackReadyAtMs = -1f;
            return true;
        }

        if (!CanAttack(nowMs))
            return false;

        pattern = MonsterAttackPatternSelector.ChooseReadyPattern(
            GetActiveAttackPatterns(),
            _patternLastUseTimesMs,
            nowMs);

        if (pattern == null)
            return false;

        _lastAttackTime = nowMs;
        lockedTargetPosition = targetPosition;
        _pendingAttackTarget = targetPosition;
        if (!string.IsNullOrWhiteSpace(pattern.name))
            _patternLastUseTimesMs[pattern.name] = nowMs;

        PlayAnimation("attack");

        if (pattern.windupMs > 0f)
        {
            _pendingAttackPattern = pattern;
            _pendingAttackReadyAtMs = nowMs + pattern.windupMs;
            return false;
        }

        return true;
    }

    public MonsterAttackPattern[] GetActiveAttackPatterns()
    {
        if (Def?.phases != null && _currentPhase > 0)
        {
            int phaseIndex = Mathf.Clamp(_currentPhase - 1, 0, Def.phases.Length - 1);
            var phasePatterns = Def.phases[phaseIndex]?.attackPatterns;
            if (phasePatterns != null && phasePatterns.Length > 0)
                return phasePatterns;
        }

        return Def?.attackPatterns;
    }
}
