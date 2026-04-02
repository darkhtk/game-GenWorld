# Status: Dev-Backend

## Current: DONE (Phase 3)

## Last Update: 2026-04-02

## Current Task
Phase 3 — Entities implemented (5 files)

## Progress

### Phase 2 (DONE)
- [x] All 11 Systems + 8 test suites (61 tests)

### Phase 3 (DONE)
- [x] PlayerController.cs — WASD movement, mouse aim, dodge with invincibility
- [x] PlayerStats.cs — level/xp/gold, RecalcStats via StatsSystem, FullHeal
- [x] MonsterController.cs — 4-state AI (Patrol/Chase/Attack/Return), DoT tick, phase transitions
- [x] MonsterSpawner.cs — region-based spawning, walkable tile check, density calc
- [x] VillageNPC.cs — patrol within radius, stop/resume, interaction range
- [x] Projectile.cs — directional movement, piercing support, OnHitMonster/OnArrive callbacks

## Issues
None

## Next
Phase 4: CombatManager (waiting for assignment update)
