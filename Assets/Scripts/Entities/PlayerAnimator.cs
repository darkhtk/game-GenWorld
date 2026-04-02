using UnityEngine;

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

    SpriteRenderer _sr;
    Sprite[][] _walkSets;
    Sprite[] _idleSprites;
    int _direction; // 0=down, 1=left, 2=right, 3=up
    float _frameTimer;
    int _frameIndex;
    bool _isMoving;

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
    }

    public void SetMovement(Vector2 velocity)
    {
        _isMoving = velocity.sqrMagnitude > 0.01f;

        if (_isMoving)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                _direction = velocity.x > 0 ? 2 : 1; // right : left
            else
                _direction = velocity.y > 0 ? 3 : 0; // up : down
        }
    }

    void Update()
    {
        if (_isMoving)
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
        else
        {
            _frameIndex = 0;
            _frameTimer = 0f;
            if (_idleSprites[_direction] != null)
                _sr.sprite = _idleSprites[_direction];
        }
    }

    void LoadSpritesFromSheet()
    {
        var allSprites = Resources.LoadAll<Sprite>("player");
        if (allSprites == null || allSprites.Length < 16) return;

        // Sort by name to ensure correct order
        var sorted = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (var s in allSprites) sorted[s.name] = s;

        string[] dirs = { "walk_down", "walk_left", "walk_right", "walk_up" };
        Sprite[][] targets = { walkDown, walkLeft, walkRight, walkUp };

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
            _idleSprites[d] = frames[0]; // First frame as idle
        }
    }
}
