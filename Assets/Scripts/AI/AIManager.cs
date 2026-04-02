using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AIManager
{
    readonly OllamaClient _client;
    readonly Dictionary<string, NPCBrain> _brains = new();
    public bool AiEnabled { get; private set; }

    public AIManager(OllamaClient client = null) { _client = client ?? new OllamaClient(); }
    public async Task Init() { AiEnabled = await _client.CheckAvailability(); }
    public void RegisterNpc(string npcId, string name, string personality, string profile = null) { var b = new NPCBrain(npcId, name, personality); if (profile != null) b.Profile = profile; _brains[npcId] = b; }
    public NPCBrain GetBrain(string npcId) => _brains.TryGetValue(npcId, out var b) ? b : null;
    public void UpdateBehavior(string playerRegion, float playerX, float playerY, Dictionary<string, Vector2> npcPositions) { }
    public async Task<DialogueResponse> GenerateDialogue(string npcId, string playerInput, List<DialogueEntry> history, int playerLevel, int playerGold, InventorySystem inventory, Dictionary<string, ItemDef> itemDefs, QuestSystem questSystem, string loreContext = null, string[] npcActions = null, DialogueTraits traits = null) { return null; }
    public Dictionary<string, NPCBrainData> SerializeAllBrains() { var d = new Dictionary<string, NPCBrainData>(); foreach (var k in _brains) d[k.Key] = k.Value.Serialize(); return d; }
    public void RestoreAllBrains(Dictionary<string, NPCBrainData> data) { if (data == null) return; foreach (var k in data) { if (_brains.TryGetValue(k.Key, out var b)) b.Restore(k.Value); } }
}
