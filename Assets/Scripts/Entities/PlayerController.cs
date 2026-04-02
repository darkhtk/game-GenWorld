using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    Rigidbody2D _rb;
    SpriteRenderer _sr;

    public Vector2 AimDirection { get; private set; }
    public Vector2 Position => (Vector2)transform.position;
    public string Facing { get; private set; } = "down";
    public bool Frozen { get; set; }
    public bool IsDodging { get; private set; }
    public bool Invincible { get; private set; }

    float _lastDodgeTime = -999f;
    const float DodgeCooldown = 0.8f;
    const float DodgeDuration = 0.2f;
    const float DodgeSpeed = 400f;

    float _moveSpeed;
    Vector2 _dodgeDir;
    float _dodgeEndTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
    }

    void Update()
    {
        if (Frozen) { _rb.linearVelocity = Vector2.zero; return; }

        // Aim direction (mouse)
        if (Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            AimDirection = ((Vector2)mouseWorld - Position).normalized;
        }

        // Dodge (Ctrl / Space)
        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Space))
            && Time.time - _lastDodgeTime >= DodgeCooldown && !IsDodging)
        {
            StartDodge();
        }

        if (IsDodging)
        {
            if (Time.time >= _dodgeEndTime)
            {
                IsDodging = false;
                Invincible = false;
            }
            else
            {
                _rb.linearVelocity = _dodgeDir * DodgeSpeed;
                return;
            }
        }

        // Movement (WASD / arrows)
        float mx = 0, my = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) my = 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) my = -1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) mx = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) mx = 1;

        var moveDir = new Vector2(mx, my).normalized;
        _rb.linearVelocity = moveDir * _moveSpeed;

        // Facing direction
        if (moveDir.sqrMagnitude > 0)
        {
            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
                Facing = moveDir.x > 0 ? "right" : "left";
            else
                Facing = moveDir.y > 0 ? "up" : "down";
        }
    }

    void StartDodge()
    {
        float mx = 0, my = 0;
        if (Input.GetKey(KeyCode.W)) my = 1;
        if (Input.GetKey(KeyCode.S)) my = -1;
        if (Input.GetKey(KeyCode.A)) mx = -1;
        if (Input.GetKey(KeyCode.D)) mx = 1;
        _dodgeDir = new Vector2(mx, my).normalized;
        if (_dodgeDir.sqrMagnitude < 0.01f) _dodgeDir = AimDirection;

        IsDodging = true;
        Invincible = true;
        _lastDodgeTime = Time.time;
        _dodgeEndTime = Time.time + DodgeDuration;
    }

    public void SetSpeed(float speed) => _moveSpeed = speed;

    public float GetDodgeCooldownFraction()
    {
        float elapsed = Time.time - _lastDodgeTime;
        return Mathf.Clamp01(elapsed / DodgeCooldown);
    }
}
