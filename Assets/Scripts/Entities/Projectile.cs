using System;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Action<MonsterController> OnHitMonster;
    public Action<Vector2> OnArrive;

    Vector2 _from, _to;
    float _speed;
    bool _piercing;
    float _totalDist;
    float _traveled;
    Vector2 _dir;
    bool _arrived;
    readonly HashSet<int> _hitIds = new();

    public void Init(Vector2 from, Vector2 to, float speed, int color, float size, bool piercing)
    {
        _from = from;
        _to = to;
        _speed = speed;
        _piercing = piercing;
        transform.position = from;

        _dir = (to - from).normalized;
        _totalDist = Vector2.Distance(from, to);
        _traveled = 0f;

        // Scale based on size parameter
        if (size > 0)
            transform.localScale = Vector3.one * (size / 32f);

        // Rotate to face direction
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Add collider and rigidbody for trigger detection
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = size > 0 ? size / 32f : 0.25f;
    }

    void Update()
    {
        if (_arrived) return;

        float step = _speed * Time.deltaTime;
        _traveled += step;
        transform.position = (Vector2)transform.position + _dir * step;

        if (_traveled >= _totalDist)
        {
            _arrived = true;
            OnArrive?.Invoke(_to);
            Destroy(gameObject, 0.1f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_arrived) return;

        var monster = other.GetComponent<MonsterController>();
        if (monster == null || monster.IsDead) return;

        int id = monster.GetInstanceID();
        if (_hitIds.Contains(id)) return;
        _hitIds.Add(id);

        OnHitMonster?.Invoke(monster);

        if (!_piercing)
        {
            _arrived = true;
            Destroy(gameObject, 0.1f);
        }
    }
}
