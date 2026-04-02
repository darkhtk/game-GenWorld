# Status: Dev-Backend

## Current: LOOP (all phases complete)

## Last Update: 2026-04-02

## Current Task
Loop — compile fixes, code review, test coverage

## Loop Cycle #1 (2026-04-02)
- Fixed: EffectHolder stun cap bug (absolute vs relative time)
- Fixed: OllamaClient WarmModel anonymous type serialization
- Added: RegionTracker unit tests (8 tests)
- Updated: EffectSystemTests for corrected stun behavior
- Wired: DamageText.Spawn/SpawnText into CombatManager (Dev-Frontend request)
- Wired: SkillVFX.ShowAtPosition and CameraShake.Shake into skill delegates
- **Fixed: ActionRunner deal_damage had no single-target fallback** — heavy_strike, shield_charge, assassinate were dealing zero damage
- **Fixed: ActionRunner chain logic** — chain_lightning data was defined but never implemented
- **Fixed: ActionRunner apply_effect single-target fallback** — shield_charge stun was never applied
- **Fixed: ActionRunner onHit sub-actions** — deal_damage onHit callbacks were not processed
- **Fixed: ActionRunner cone filtering** — ground_slam hit full 360° instead of forward arc
- **Fixed: Projectile missing collider** — skill projectiles had no Collider2D/Rigidbody2D, OnTriggerEnter2D never fired (fireball, poison_arrow, ice_bolt, etc.)
- **Fixed: MonsterController missing collider** — ensures BoxCollider2D exists for projectile trigger detection
- Reported: 3 GameManager bugs to Director (questions/backend-gamemanager-bugs.md)

## Progress
- [x] Phase 2 — 11 Systems + 8 test suites
- [x] Phase 3 — 6 Entities (PlayerController, PlayerStats, MonsterController, MonsterSpawner, VillageNPC, Projectile)
- [x] Phase 4 — CombatManager, SkillExecutor (5 handlers), ActionRunner (3 handlers), AreaEffect
- [x] Phase 5 — OllamaClient, PromptBuilder, AIManager (RegionTracker was already done)

## Issues
- CombatManager exposes `Skills` and `PlayerState` public properties — Director needs to wire them in GameManager after Init
