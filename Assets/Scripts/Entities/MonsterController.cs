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

    Rigidbody2D _rb;
    Vector2 _spawnPos;
    Vector2? _patrolTarget;
    float _lastAttackTime;
    int _currentPhase;

    float _atkMult = 1f, _defMult = 1f, _spdMult = 1f, _cooldownMult = 1f;

    public int EffectiveAtk => Mathf.RoundToInt(Def.atk * _atkMult);
    public int EffectiveDef => Mathf.RoundToInt(Def.def * _defMult);

    public void Init(MonsterDef def, Vector2 spawnPos)
    {
        Def = def;
        Hp = def.hp;
        _spawnPos = spawnPos;
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;

        // Ensure collider exists for projectile trigger detection
        if (!TryGetComponent<Collider2D>(out _))
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }
    }

    public void UpdateAI(Vector2 playerPos, float now)
    {
        if (IsDead) return;
        if (Effects.Has("stun")) { _rb.linearVelocity = Vector2.zero; return; }

        float distToPlayer = Vector2.Distance(Position, playerPos);
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
                if (distToPlayer <= Def.detectRange)
                    AIState = MonsterAIState.Chase;
                break;

            case MonsterAIState.Chase:
                MoveToward(playerPos, speed);
                if (distToPlayer <= Def.attackRange)
                    AIState = MonsterAIState.Attack;
                else if (distToPlayer > Def.detectRange * GameConfig.ChaseRangeMult ||
                         Vector2.Distance(Position, _spawnPos) > GameConfig.MaxSpawnDistance)
                    AIState = MonsterAIState.Return;
                break;

            case MonsterAIState.Attack:
                _rb.linearVelocity = Vector2.zero;
                if (distToPlayer > Def.attackRange * 1.5f)
                    AIState = MonsterAIState.Chase;
                break;

            case MonsterAIState.Return:
                MoveToward(_spawnPos, speed);
                if (Vector2.Distance(Position, _spawnPos) < 16f)
                {
                    Hp = Def.hp;
                    AIState = MonsterAIState.Patrol;
                }
                if (distToPlayer <= Def.detectRange)
                    AIState = MonsterAIState.Chase;
                break;
        }
    }

    void DoPatrol(float speed)
    {
        if (_patrolTarget == null || Vector2.Distance(Position, _patrolTarget.Value) < 8f)
        {
            _patrolTarget = _spawnPos + Random.insideUnitCircle * 64f;
        }
        MoveToward(_patrolTarget.Value, speed * 0.5f);
    }

    void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - Position).normalized;
        _rb.linearVelocity = dir * speed * Time.fixedDeltaTime * 60f;
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
        Hp -= dmg;
        return Hp <= 0;
    }

    public bool CanAttack(float now)
    {
        return AIState == MonsterAIState.Attack &&
               now - _lastAttackTime >= Def.attackCooldown * _cooldownMult;
    }

    public void MarkAttacked(float now) => _lastAttackTime = now;
}
