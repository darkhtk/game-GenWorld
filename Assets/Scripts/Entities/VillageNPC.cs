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
    int _currentPatrolRadius;
    string _currentActivity;
    string _lastPeriod;

    Rigidbody2D _rb;

    public void Init(NpcDef def, Vector2 position)
    {
        Def = def;
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        transform.position = position;

        // Load sprite
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var sprite = Resources.Load<Sprite>($"Sprites/{def.sprite}");
            if (sprite == null) sprite = Resources.Load<Sprite>(def.sprite);
            if (sprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = sprite;
            }
        }
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
        UpdateSchedule();
        if (_currentActivity == "sleeping")
        {
            if (_rb) _rb.linearVelocity = Vector2.zero;
            return;
        }
        DoPatrol();
    }

    void UpdateSchedule()
    {
        if (Def.schedule == null || Def.schedule.Length == 0) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.TimeSystem == null) return;

        string period = gm.TimeSystem.Period;
        if (period == _lastPeriod) return;
        _lastPeriod = period;

        foreach (var s in Def.schedule)
        {
            if (s.period != period) continue;
            float x = (s.cx + 0.5f) * GameConfig.TileSize;
            float y = -(s.cy + 0.5f) * GameConfig.TileSize;
            _patrolCenter = new Vector2(x, y);
            _currentPatrolRadius = s.radius;
            _currentActivity = s.activity;
            _patrolTarget = null;
            break;
        }
    }

    void DoPatrol()
    {
        if (Def.patrol == null && _currentPatrolRadius == 0) return;

        if (_patrolTarget == null || (Position - _patrolTarget.Value).sqrMagnitude < 16f)
        {
            int r = _currentPatrolRadius > 0 ? _currentPatrolRadius : (Def.patrol?.radius ?? 2);
            float radius = r * GameConfig.TileSize;
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
        SetAnimationState("talk");
    }

    public void ResumeMoving()
    {
        IsStopped = false;
        SetAnimationState("idle");
    }
}
