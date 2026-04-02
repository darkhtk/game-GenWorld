# SPEC-B-006: Ollama AI Integration Audit

> **Priority:** P0 (user bug report)
> **Symptom:** Ollama AI utilization needs full check.

---

## Architecture Overview

| Component | File | Role |
|-----------|------|------|
| OllamaClient | `Assets/Scripts/AI/OllamaClient.cs` | HTTP client for Ollama API (localhost:11434) |
| AIManager | `Assets/Scripts/AI/AIManager.cs` | Orchestrates NPC brains, delegates to OllamaClient |
| NPCBrain | `Assets/Scripts/AI/NPCBrain.cs` | Per-NPC AI state, personality, memory |

## Diagnostic Points

### 1. Ollama Server Connectivity
- **File:** `OllamaClient.cs:13` — connects to `http://localhost:11434`
- Models: `gemma3:4b` (fast/dialogue), `gemma3:12b` (large/complex)
- **Check:** Is Ollama actually running on the user's machine? Timeout: 3s availability check.
- **Fallback:** If Ollama unavailable, does the system fall back gracefully (template responses)?

### 2. AIManager Initialization
- **File:** `AIManager.cs:19-21` — creates OllamaClient if not injected.
- **Check:** Is `AIManager` instantiated in `GameManager`? Is it a static/singleton system?
- **GameManager.cs:194** — `AI.RegisterNpc()` is called per NPC, and `AI.GetBrain()` for VillageNPC.

### 3. NPC Brain → Dialogue Chain
- NPCBrain should generate dialogue responses when player talks to NPC.
- **Check:** `NPCBrain.GenerateResponse()` or equivalent method exists and returns valid text.
- **Check:** Timeout handling — if Ollama is slow (>30s), does it timeout cleanly?
- **Check:** Brain serialization/deserialization for save/load (`GameManager.cs:539,571`).

### 4. Profile Loading
- **GameManager.cs:193** — `Data.NpcProfiles.GetValueOrDefault(npc.id)` loads NPC profile.
- Profiles are MD files in `Assets/StreamingAssets/Data/npc-profiles/`.
- **Check:** Profile files exist and are loaded correctly.

### 5. Test Coverage
- `Assets/Tests/EditMode/NPCBrainTests.cs` — existing tests.
- `Assets/Tests/EditMode/AIManagerTests.cs` — existing tests.
- **Run tests** to verify basic AI logic works without Ollama.

## Fix Direction
1. Verify Ollama fallback path (template/scripted responses when server unavailable).
2. Add clear Debug.Log for connection status at startup.
3. Ensure dialogue UI receives AI-generated or fallback text.
4. Test NPC brain save/load round-trip.

## Verification
- With Ollama running: NPC gives contextual AI-generated responses.
- Without Ollama: NPC gives fallback scripted responses (no crash, no hang).
- Save/load preserves NPC brain state (memory, affection).
