# DEVELOPER Loop Log

**Last run:** 2026-04-02 (loop 7)
**Status:** WORKING — R-002 fix + R-003

## Loop Result
- FREEZE: N
- Build errors: 0 (stale log only)
- Discussions pending: 0
- NEEDS_WORK reviews: R-002 (REVIEW-R002-v1)
- R-001: ✅ Done
- R-002: Fixed (NEEDS_WORK → In Review v2)
- R-003: Completed → In Review

## R-002 Fix (NEEDS_WORK response)
Review pointed out caller files weren't updated (changes lost in concurrent commits).
Re-applied:
- `ActionRunner.cs`: `new GameObject("SkillProjectile")` → `Projectile.Get()`
- `CombatManager.cs`: `new GameObject("MonsterProjectile")` → `Projectile.Get()`
- `DamageText.cs`: Full rewrite with ObjectPool (30 init/80 max), instance coroutine

## R-003 세이브 버전 마이그레이션 (NEW)
- `SaveSystem.cs`: SaveEnvelope wrapper (version + JObject data)
- SaveMigrations registry with sequential v0→v1 migration
- Legacy saves (no version field) treated as v0
- Backup creation on migration + failure handling

Specs referenced: Y (SPEC-R002.md, SPEC-R003.md)
