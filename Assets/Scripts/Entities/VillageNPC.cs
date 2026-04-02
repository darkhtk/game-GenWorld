using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VillageNPC : MonoBehaviour
{
    public NpcDef Def { get; private set; }
    public NPCBrain Brain { get; set; }
    public Vector2 Position => (Vector2)transform.position;
    public bool IsStopped { get; set; }

    Vector2 _patrolCenter;
    Vector2? _patrolTarget;
    float _speed = 30f;

    Rigidbody2D _rb;

    public void Init(NpcDef def, Vector2 position)
    {
        Def = def;
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        transform.position = position;
        _patrolCenter = position;
        ValidateAnimations();
    }

    void ValidateAnimations()
    {
        if (Def.animationDef == null)
        {
            if (!string.IsNullOrEmpty(Def.id))
                Debug.LogWarning($"[VillageNPC] {Def.name}: no AnimationDef assigned");
            return;
        }
        string[] required = { "idle", "talk", "react" };
        foreach (var state in required)
        {
            if (!Def.animationDef.HasClip(state))
                Debug.LogWarning($"[VillageNPC] {Def.name}: missing animation clip for '{state}'");
        }
    }

    public void SetAnimationState(string stateName)
    {
        if (Def.animationDef == null) return;
        var entry = Def.animationDef.GetEntry(stateName);
        if (entry == null || entry.clip == null) return;
        var animator = GetComponent<Animator>();
        if (animator != null) animator.Play(stateName, 0, 0f);
    }

    void Update()
    {
        if (IsStopped || Def == null)
        {
            if (_rb) _rb.linearVelocity = Vector2.zero;
            return;
        }
        DoPatrol();
    }

    void DoPatrol()
    {
        if (Def.patrol == null) return;

        if (_patrolTarget == null || (Position - _patrolTarget.Value).sqrMagnitude < 16f)
        {
            float radius = Def.patrol.radius * GameConfig.TileSize;
            _patrolTarget = _patrolCenter + Random.insideUnitCircle * radius;
        }

        Vector2 dir = (_patrolTarget.Value - Position).normalized;
        _rb.linearVelocity = dir * _speed;
    }

    public bool IsInInteractionRange(Vector2 playerPos, float range = 48f)
    {
        return (Position - playerPos).sqrMagnitude <= range * range;
    }

    public ConditionalDialogue EvaluateConditionalDialogue(QuestSystem quests,
        InventorySystem inventory)
    {
        if (Def == null || Def.conditionalDialogues == null) return null;
        return DialogueConditionParser.FindBestMatch(Def.conditionalDialogues, quests, inventory, Brain);
    }

    public void StopMoving()
    {
        IsStopped = true;
        if (_rb) _rb.linearVelocity = Vector2.zero;
    }

    public void ResumeMoving() => IsStopped = false;
}
