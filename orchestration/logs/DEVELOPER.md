# DEVELOPER Loop Log

**Last run:** 2026-04-02 (loop 6)
**Status:** WORKING — R-002

## Loop Result
- FREEZE: N
- Build errors: 0 (stale log only)
- Discussions pending: 0
- NEEDS_WORK reviews: 0
- R-001: ✅ Done (approved by client)
- R-002: In Review (just completed)

## Completed This Loop
**R-002 오브젝트 풀링 (Projectile/DamageText)** — SPEC-R002 기반 구현 완료.

### Changes
- `ObjectPool.cs` (NEW): Generic pool with Get/Return, auto-expand up to maxSize
- `Projectile.cs`: Static pool (20 initial, 50 max), lazy-init, ReturnToPool on hit/arrive/timeout(5s)
- `DamageText.cs`: Static pool (30 initial, 80 max), instance-based coroutine animation, pool return on complete
- `ActionRunner.cs`: Projectile.Get() instead of new GameObject + AddComponent
- `CombatManager.cs`: Projectile.Get() instead of new GameObject + AddComponent

### UI 자가 검증
- Infrastructure system — no UI (SPEC confirms: "별도 UI 없음")

Specs referenced: Y (SPEC-R002.md)
