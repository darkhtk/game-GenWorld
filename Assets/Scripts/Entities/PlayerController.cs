using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    Rigidbody2D _rb;
    float _moveSpeed;

    public Vector2 AimDirection { get; private set; }
    public Vector2 Position => (Vector2)transform.position;
    public string Facing { get; private set; } = "down";
    public bool Frozen { get; set; }
    public bool IsDodging { get; private set; }
    public bool Invincible { get; private set; }

    void Awake() { _rb = GetComponent<Rigidbody2D>(); _rb.gravityScale = 0; _rb.freezeRotation = true; }
    void Update() { }
    public void SetSpeed(float speed) => _moveSpeed = speed;
    public float GetDodgeCooldownFraction() => 1f;
}
