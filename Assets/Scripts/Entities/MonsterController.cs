using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour
{
    Rigidbody2D _rb;
    Vector2 _spawnPos;
    float _lastAttackTime;
    int _currentPhase;
    float _atkMult = 1f, _defMult = 1f, _spdMult = 1f, _cooldownMult = 1f;

    public MonsterDef Def { get; private set; }
    public int Hp { get; set; }
    public bool IsDead => Hp <= 0;
    public Vector2 Position => (Vector2)transform.position;
    public EffectHolder Effects { get; } = new();
    public MonsterAIState AIState { get; private set; } = MonsterAIState.Patrol;
    public int EffectiveAtk => Mathf.RoundToInt(Def.atk * _atkMult);
    public int EffectiveDef => Mathf.RoundToInt(Def.def * _defMult);

    public void Init(MonsterDef def, Vector2 spawnPos) { Def = def; Hp = def.hp; _spawnPos = spawnPos; _rb = GetComponent<Rigidbody2D>(); _rb.gravityScale = 0; _rb.freezeRotation = true; }
    public void UpdateAI(Vector2 playerPos, float now) { }
    public bool TakeDamage(int dmg) { Hp -= dmg; return Hp <= 0; }
    public bool CanAttack(float now) => AIState == MonsterAIState.Attack && now - _lastAttackTime >= Def.attackCooldown * _cooldownMult;
    public void MarkAttacked(float now) => _lastAttackTime = now;
}
