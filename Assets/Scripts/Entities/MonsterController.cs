using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour
{
    public MonsterDef Def { get; private set; }
    public int Hp { get; set; }
    public bool IsDead => Hp <= 0;
    public Vector2 Position => (Vector2)transform.position;
    public EffectHolder Effects { get; } = new();
    public MonsterAIState AIState { get; private set; } = MonsterAIState.Patrol;
    public float SpawnTime { get; set; }
    public float LastHitByPlayerTime { get; set; }
    public bool IsReturning => AIState == MonsterAIState.Return;

    Rigidbody2D _rb;
    Vector2 _spawnPos;
    Vector2? _patrolTarget;
    float _lastAttackTime;
    float _returnStartTime;
    int _currentPhase;

    const float ReturnForceTeleport = 5f;
    const float ReturnDamageReduction = 5f;
    const float ReturnSpeedMult = 1.5f;
    const float ReturnReaggroMult = 0.5f;
    const float RecentHitWindow = 2f;

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
        // Future: trigger actual Animator state when Animator is attached
        var animator = GetComponent<Animator>();
        if (animator != null) animator.Play(stateName, 0, 0f);
    }

    public void UpdateAI(Vector2 playerPos, float now)
    {
        if (IsDead) return;
        if (Effects.Has("stun")) { _rb.linearVelocity = Vector2.zero; return; }

        float distToPlayerSq = (Position - playerPos).sqrMagnitude;
        float slowFactor = Effects.Has("slow") ? Effects.GetValue("slow") : 1f;
        float speed = Def.spd * _spdMult * slowFactor;

        // DoT tick
        float dotDmg = Effects.TickDot(now);
        if (dotDmg > 0) Hp -= Mathf.RoundToInt(dotDmg);

        // Expire effects
        Effects.Tick(now);

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
                    Hp = Def.hp;
                    AIState = MonsterAIState.Patrol;
                    break;
                }
                MoveToward(_spawnPos, speed * ReturnSpeedMult);
                if ((Position - _spawnPos).sqrMagnitude < 256f)
                {
                    Hp = Def.hp;
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
                break;
            }
        }
    }

    public bool TakeDamage(int dmg)
    {
        if (IsReturning) dmg = Mathf.Max(1, dmg / (int)ReturnDamageReduction);
        Hp -= dmg;
        if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);
        FlashWhite();
        if (Hp <= 0) { PlayAnimation("die"); return true; }
        PlayAnimation("hit");
        return false;
    }

    SpriteRenderer _sr;
    static readonly Color HitTint = new(1f, 0.4f, 0.4f, 1f);
    static readonly WaitForSeconds FlashWait = new(0.1f);

    void FlashWhite()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) StartCoroutine(DoFlash());
    }

    System.Collections.IEnumerator DoFlash()
    {
        _sr.color = HitTint;
        yield return FlashWait;
        _sr.color = Color.white;
    }

    public bool CanAttack(float now)
    {
        return AIState == MonsterAIState.Attack &&
               now - _lastAttackTime >= Def.attackCooldown * _cooldownMult;
    }

    public void MarkAttacked(float now) => _lastAttackTime = now;
}
