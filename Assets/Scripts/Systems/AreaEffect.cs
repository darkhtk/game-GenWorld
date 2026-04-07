using System.Collections.Generic;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    Transform _follow;
    float _endTime;
    float _tickInterval;
    float _lastTick;
    SkillAction[] _onTick;
    ActionContext _ctx;
    ActionRunner _runner;

    static readonly Queue<AreaEffect> _pool = new();
    static Transform _poolParent;

    public static void ClearPool()
    {
        while (_pool.Count > 0)
        {
            var ae = _pool.Dequeue();
            if (ae != null) Destroy(ae.gameObject);
        }
        if (_poolParent != null) Destroy(_poolParent.gameObject);
        _poolParent = null;
    }

    static void EnsureParent()
    {
        if (_poolParent != null) return;
        var go = new GameObject("AreaEffectPool");
        DontDestroyOnLoad(go);
        _poolParent = go.transform;
    }

    public static AreaEffect Get()
    {
        EnsureParent();
        if (_pool.Count > 0)
        {
            var ae = _pool.Dequeue();
            ae.transform.SetParent(_poolParent);
            ae.gameObject.SetActive(true);
            return ae;
        }
        var newGo = new GameObject("AreaEffect");
        newGo.transform.SetParent(_poolParent);
        return newGo.AddComponent<AreaEffect>();
    }

    public void Init(Transform follow, float x, float y, float aoe, float duration,
        float tickInterval, SkillAction[] onTick, ActionContext ctx, ActionRunner runner)
    {
        _follow = follow;
        transform.position = follow != null ? follow.position : new Vector3(x, y, 0);
        _endTime = ctx.now + duration;
        _tickInterval = tickInterval;
        _lastTick = ctx.now;
        _onTick = onTick;
        _ctx = ctx;
        _runner = runner;
    }

    void Update()
    {
        float nowMs = Time.time * 1000f;

        if (nowMs >= _endTime)
        {
            ReturnToPool();
            return;
        }

        if (_follow != null)
            transform.position = _follow.position;

        if (_onTick != null && _onTick.Length > 0 && _ctx != null && _runner != null
            && nowMs - _lastTick >= _tickInterval)
        {
            _lastTick = nowMs;
            _runner.Run(_onTick, _ctx, transform.position.x, transform.position.y);
            if (_ctx.skill != null)
                SkillVFX.ShowAtPosition(this, SkillVFX.ResolveVFXName(_ctx.skill.id),
                    transform.position.x, transform.position.y);
        }
    }

    void ReturnToPool()
    {
        _onTick = null;
        _ctx = null;
        _runner = null;
        _follow = null;
        gameObject.SetActive(false);
        transform.SetParent(_poolParent);
        _pool.Enqueue(this);
    }
}
