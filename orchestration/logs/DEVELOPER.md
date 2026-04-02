# DEVELOPER Loop Log

**Last run:** 2026-04-02 (loop 29)
**Status:** WORKING — R-020

## Loop Result
- FREEZE: N
- Build errors: 0 (VillageNPC.TimeSystem stale)
- R-001~R-019, R-027~R-032: ✅ Done (27 tasks!)
- R-020: Completed → In Review (새 시스템)

## Completed This Loop
**R-020 NPC 호감도 이벤트** — SPEC-R020 기반 구현 완료.

### Changes
- `NPCBrain.cs`: _triggeredEvents HashSet + HasTriggered/MarkTriggered + Serialize/Restore
- `StatTypes.cs`: NPCBrainData.triggeredEvents field
- `AIManager.cs`: EvaluateTriggers() — NpcTrigger evaluation after relationship change
  - Threshold check, WantToTalk, memory, NpcTriggerEvent emission
  - Called from ApplyResponse()

Specs referenced: Y (SPEC-R020.md)
