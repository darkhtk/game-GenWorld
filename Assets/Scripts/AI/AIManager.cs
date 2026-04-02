using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class AIManager
{
    readonly OllamaClient _client;
    readonly Dictionary<string, NPCBrain> _brains = new();
    readonly Dictionary<string, int> _rejectionCounts = new();

    public bool AiEnabled { get; private set; }

    float _lastBehaviorUpdate;
    int _behaviorIndex;
    string[] _npcIds;
    const float BehaviorCycleInterval = 10000f; // 10s per NPC in ms

    public AIManager(OllamaClient client = null)
    {
        _client = client ?? new OllamaClient();
    }

    public async Task Init()
    {
        AiEnabled = await _client.CheckAvailability();
        Debug.Log($"[AIManager] AI enabled: {AiEnabled}");
    }

    public void RegisterNpc(string npcId, string name, string personality, string profile = null)
    {
        var brain = new NPCBrain(npcId, name, personality);
        if (profile != null) brain.Profile = profile;
        _brains[npcId] = brain;
    }

    public NPCBrain GetBrain(string npcId) =>
        _brains.TryGetValue(npcId, out var b) ? b : null;

    public void UpdateBehavior(string playerRegion, float playerX, float playerY,
        Dictionary<string, Vector2> npcPositions)
    {
        float nowMs = Time.time * 1000f;
        if (nowMs - _lastBehaviorUpdate < BehaviorCycleInterval) return;
        _lastBehaviorUpdate = nowMs;

        if (_npcIds == null || _npcIds.Length != _brains.Count)
        {
            _npcIds = new string[_brains.Count];
            _brains.Keys.CopyTo(_npcIds, 0);
        }

        if (_npcIds.Length == 0) return;
        _behaviorIndex = (_behaviorIndex + 1) % _npcIds.Length;
        string npcId = _npcIds[_behaviorIndex];

        if (!_brains.TryGetValue(npcId, out var brain)) return;

        int rel = brain.GetRelationship("player");
        bool isNearPlayer = npcPositions != null
            && npcPositions.TryGetValue(npcId, out var npcPos)
            && Vector2.Distance(npcPos, new Vector2(playerX, playerY)) < 200f;

        // Rule-based mood update
        if (rel >= 10)
            brain.CurrentMood = Mood.Happy;
        else if (rel >= 0)
            brain.CurrentMood = Mood.Neutral;
        else if (rel >= -10)
            brain.CurrentMood = Mood.Angry;
        else
            brain.CurrentMood = Mood.Scared;

        if (isNearPlayer && rel >= 5 && !brain.WantToTalk)
        {
            brain.WantToTalk = true;
            brain.TalkReason = "nearby_friend";
            brain.WantToTalkTime = nowMs;
        }
    }

    public async Task<DialogueResponse> GenerateDialogue(string npcId, string playerInput,
        List<DialogueEntry> history, int playerLevel, int playerGold,
        InventorySystem inventory, Dictionary<string, ItemDef> itemDefs,
        QuestSystem questSystem, string loreContext = null,
        string[] npcActions = null, DialogueTraits traits = null)
    {
        var brain = GetBrain(npcId);
        if (brain == null) return FallbackResponse(npcId);

        _rejectionCounts.TryGetValue(npcId, out int rejections);

        var ctx = new DialogueContext
        {
            playerInput = playerInput,
            history = history,
            playerLevel = playerLevel,
            playerGold = playerGold,
            inventory = inventory,
            itemDefs = itemDefs,
            questSystem = questSystem,
            loreContext = loreContext,
            npcActions = npcActions,
            traits = traits,
            rejectionCount = rejections
        };

        // Use AI if available
        if (AiEnabled)
        {
            var response = await TryGenerateWithAI(brain, ctx);
            if (response != null)
            {
                ApplyResponse(brain, response);
                return response;
            }
        }

        // Fallback: offline response
        return BuildOfflineResponse(brain, ctx);
    }

    async Task<DialogueResponse> TryGenerateWithAI(NPCBrain brain, DialogueContext ctx)
    {
        string prompt = PromptBuilder.BuildDialoguePrompt(brain, ctx);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            string rawResponse = await _client.GenerateDialogue(prompt);
            if (string.IsNullOrEmpty(rawResponse)) continue;

            try
            {
                var response = JsonConvert.DeserializeObject<DialogueResponse>(rawResponse);
                if (response == null || string.IsNullOrEmpty(response.dialogue)) continue;

                // Validate action against NPC's available actions
                if (!string.IsNullOrEmpty(response.action) && ctx.npcActions != null)
                {
                    bool valid = false;
                    foreach (var action in ctx.npcActions)
                    {
                        if (action == response.action) { valid = true; break; }
                    }
                    if (!valid) response.action = null;
                }

                // Ensure memory is never null
                if (string.IsNullOrEmpty(response.newMemory))
                    response.newMemory = "대화 나눔";

                // Ensure minimum relationship change
                if (response.relationshipChange == 0)
                    response.relationshipChange = 1;

                return response;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AIManager] JSON parse failed (attempt {attempt + 1}): {e.Message}");
            }
        }

        return null;
    }

    DialogueResponse BuildOfflineResponse(NPCBrain brain, DialogueContext ctx)
    {
        int rel = brain.GetRelationship("player");
        string dialogue;
        string[] options;

        int turnCount = ctx.history?.Count / 2 ?? 0;

        if (turnCount == 0)
        {
            dialogue = rel >= 5
                ? $"어서 오게, 모험가여! 무슨 일이오?"
                : "무슨 볼일이 있소?";
            options = new[] { "이 근처에 대해 알려주세요.", "별일은 아닙니다." };
        }
        else if (turnCount < 3)
        {
            dialogue = "흠, 그렇소... 이 주변은 조심해야 하오.";
            options = new[] { "더 알려주세요.", "감사합니다, 가보겠습니다." };
        }
        else
        {
            dialogue = "도움이 됐다면 다행이오. 행운을 빌겠소.";
            options = System.Array.Empty<string>();
        }

        var response = new DialogueResponse
        {
            dialogue = dialogue,
            options = options,
            action = null,
            relationshipChange = 1,
            newMemory = "대화 나눔",
            offerQuest = turnCount >= 2 && rel >= 0
        };

        ApplyResponse(brain, response);
        return response;
    }

    DialogueResponse FallbackResponse(string npcId)
    {
        return new DialogueResponse
        {
            dialogue = "...",
            options = new[] { "안녕히 계세요." },
            relationshipChange = 0,
            newMemory = "짧은 인사"
        };
    }

    void ApplyResponse(NPCBrain brain, DialogueResponse response)
    {
        if (response.relationshipChange != 0)
            brain.UpdateRelationship("player", response.relationshipChange);

        if (!string.IsNullOrEmpty(response.newMemory))
            brain.AddMemory(response.newMemory, 5);

        EvaluateTriggers(brain);

        brain.WantToTalk = false;
        brain.TalkReason = "";
    }

    public void RecordQuestRejection(string npcId)
    {
        _rejectionCounts.TryGetValue(npcId, out int count);
        _rejectionCounts[npcId] = count + 1;
    }

    public Dictionary<string, NPCBrainData> SerializeAllBrains()
    {
        var data = new Dictionary<string, NPCBrainData>();
        foreach (var kvp in _brains)
            data[kvp.Key] = kvp.Value.Serialize();
        return data;
    }

    public void RestoreAllBrains(Dictionary<string, NPCBrainData> data)
    {
        if (data == null) return;
        foreach (var kvp in data)
        {
            if (_brains.TryGetValue(kvp.Key, out var brain))
                brain.Restore(kvp.Value);
        }
    }
}
