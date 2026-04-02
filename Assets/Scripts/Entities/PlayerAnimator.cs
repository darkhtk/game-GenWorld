using UnityEngine;

// Animation States:
// idle_down/left/right/up — standing still (loop)
// walk_down/left/right/up — moving (loop, 4 frames per direction)
// attack — melee swing (one-shot, returns to idle)
// dodge — roll/dash (one-shot, driven by PlayerController)
// hit — damage taken flash (one-shot, returns to idle)
// die — death (one-shot, stays on last frame)

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] float frameRate = 8f;
    [SerializeField] Sprite[] walkDown;
    [SerializeField] Sprite[] walkLeft;
    [SerializeField] Sprite[] walkRight;
    [SerializeField] Sprite[] walkUp;
    [SerializeField] Sprite idleDown;
    [SerializeField] Sprite idleLeft;
    [SerializeField] Sprite idleRight;
    [SerializeField] Sprite idleUp;

    [Header("AnimationDef")]
    [SerializeField] AnimationDef animationDef;

    SpriteRenderer _sr;
    Sprite[][] _walkSets;
    Sprite[] _idleSprites;
    int _direction; // 0=down, 1=left, 2=right, 3=up
    float _frameTimer;
    int _frameIndex;
    bool _isMoving;

    enum State { Idle, Walk, Attack, Dodge, Hit, Die }
    State _state = State.Idle;
    float _stateEndTime;

    static bool _spritesLoaded;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _walkSets = new[] { walkDown, walkLeft, walkRight, walkUp };
        _idleSprites = new[] { idleDown, idleLeft, idleRight, idleUp };
    }

    void Start()
    {
        if (!_spritesLoaded && (walkDown == null || walkDown.Length == 0))
        {
            LoadSpritesFromSheet();
            _spritesLoaded = true;
        }
        if (animationDef != null) animationDef.LogMissingClips();
    }

    public void SetMovement(Vector2 velocity)
    {
        if (_state == State.Die) return;

        _isMoving = velocity.sqrMagnitude > 0.01f;

        if (_isMoving)
        {
            if (_state == State.Idle || _state == State.Walk)
                _state = State.Walk;
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                _direction = velocity.x > 0 ? 2 : 1;
            else
                _direction = velocity.y > 0 ? 3 : 0;
        }
        else if (_state == State.Walk)
        {
            _state = State.Idle;
        }
    }

    public void PlayAttack(float duration = 0.4f)
    {
        if (_state == State.Die) return;
        _state = State.Attack;
        _stateEndTime = Time.time + duration;
    }

    public void PlayDodge(float duration = 0.2f)
    {
        if (_state == State.Die) return;
        _state = State.Dodge;
        _stateEndTime = Time.time + duration;
    }

    public void PlayHit(float duration = 0.3f)
    {
        if (_state == State.Die) return;
        _state = State.Hit;
        _stateEndTime = Time.time + duration;
    }

    public void PlayDie()
    {
        _state = State.Die;
    }

    void Update()
    {
        if (_state is State.Attack or State.Dodge or State.Hit)
        {
            if (Time.time >= _stateEndTime)
                _state = _isMoving ? State.Walk : State.Idle;
        }

        switch (_state)
        {
            case State.Walk:
                AnimateWalk();
                break;
            case State.Idle:
                ShowIdle();
                break;
            case State.Attack:
            case State.Dodge:
            case State.Hit:
            case State.Die:
                // One-shot states — hold current sprite (or flash)
                break;
        }
    }

    void AnimateWalk()
    {
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / frameRate)
        {
            _frameTimer = 0f;
            var frames = _walkSets[_direction];
            if (frames != null && frames.Length > 0)
            {
                _frameIndex = (_frameIndex + 1) % frames.Length;
                _sr.sprite = frames[_frameIndex];
            }
        }
    }

    void ShowIdle()
    {
        _frameIndex = 0;
        _frameTimer = 0f;
        if (_idleSprites[_direction] != null)
            _sr.sprite = _idleSprites[_direction];
    }

    void LoadSpritesFromSheet()
    {
        var allSprites = Resources.LoadAll<Sprite>("Sprites/player");
        if (allSprites == null || allSprites.Length == 0)
            allSprites = Resources.LoadAll<Sprite>("player");
        if (allSprites == null || allSprites.Length < 16) return;

        var sorted = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (var s in allSprites) sorted[s.name] = s;

        string[] dirs = { "walk_down", "walk_left", "walk_right", "walk_up" };

        for (int d = 0; d < 4; d++)
        {
            var frames = new Sprite[4];
            for (int f = 0; f < 4; f++)
            {
                string key = $"player_{dirs[d]}_{f}";
                if (sorted.TryGetValue(key, out var sprite))
                    frames[f] = sprite;
            }
            _walkSets[d] = frames;
            _idleSprites[d] = frames[0];
        }
    }

#if UNITY_EDITOR
    public void ValidateAnimationStates()
    {
        if (animationDef == null)
        {
            Debug.LogWarning("[PlayerAnimator] No AnimationDef assigned");
            return;
        }
        string[] required = { "idle", "run", "attack", "dodge", "hit", "die" };
        foreach (var state in required)
        {
            if (!animationDef.HasClip(state))
                Debug.LogWarning($"[PlayerAnimator] Missing clip for state: {state}");
        }
    }
#endif
}
