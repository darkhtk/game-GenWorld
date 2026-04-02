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
            Destroy(gameObject);
            return;
        }

        if (_follow != null)
            transform.position = _follow.position;

        if (_onTick != null && _onTick.Length > 0 && nowMs - _lastTick >= _tickInterval)
        {
            _lastTick = nowMs;
            _runner?.Run(_onTick, _ctx, transform.position.x, transform.position.y);
        }
    }
}
