using System;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Action<MonsterController> OnHitMonster;
    public Action<Vector2> OnArrive;

    static ObjectPool<Projectile> _pool;
    static Transform _poolParent;

    Vector2 _from, _to;
    float _speed;
    bool _piercing;
    float _totalDist;
    float _traveled;
    Vector2 _dir;
    bool _arrived;
    float _spawnTime;
    const float Timeout = 5f;
    readonly HashSet<int> _hitIds = new();

    Rigidbody2D _rb;
    CircleCollider2D _col;

    static void EnsurePool()
    {
        if (_pool != null) return;
        _poolParent = new GameObject("ProjectilePool").transform;
        UnityEngine.Object.DontDestroyOnLoad(_poolParent.gameObject);
        _pool = new ObjectPool<Projectile>(() =>
        {
            var go = new GameObject("Projectile");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            var proj = go.AddComponent<Projectile>();
            proj._rb = rb;
            proj._col = col;
            return proj;
        }, _poolParent, 20, 50);
    }

    public static void ClearPool()
    {
        _pool?.Clear();
        _pool = null;
        if (_poolParent != null) UnityEngine.Object.Destroy(_poolParent.gameObject);
        _poolParent = null;
    }

    public static Projectile Get()
    {
        EnsurePool();
        return _pool.Get();
    }

    public void Init(Vector2 from, Vector2 to, float speed, int color, float size, bool piercing)
    {
        _from = from;
        _to = to;
        _speed = speed * GameConfig.ProjectileSpeedScale;
        _piercing = piercing;
        _arrived = false;
        _spawnTime = Time.time;
        _hitIds.Clear();
        OnHitMonster = null;
        OnArrive = null;

        transform.SetParent(null);
        transform.position = from;

        _dir = (to - from).normalized;
        _totalDist = Vector2.Distance(from, to);
        _traveled = 0f;

        transform.localScale = size > 0 ? Vector3.one * (size / 32f) : Vector3.one;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (_rb == null && !TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (_col == null && !TryGetComponent(out _col))
        {
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.isTrigger = true;
        }
        _col.radius = size > 0 ? size / 32f : 0.25f;
    }

    void ReturnToPool()
    {
        OnHitMonster = null;
        OnArrive = null;
        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (_arrived) return;

        if (Time.time - _spawnTime > Timeout)
        {
            _arrived = true;
            ReturnToPool();
            return;
        }

        float step = _speed * Time.deltaTime;
        _traveled += step;
        transform.position = (Vector2)transform.position + _dir * step;

        if (_traveled >= _totalDist)
        {
            _arrived = true;
            var cb = OnArrive;
            OnArrive = null;
            cb?.Invoke(_to);
            ReturnToPool();
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

        if (!_piercing)
            _arrived = true;

        var cb = OnHitMonster;
        OnHitMonster = null;
        cb?.Invoke(monster);

        if (!_piercing)
            ReturnToPool();
    }
}
