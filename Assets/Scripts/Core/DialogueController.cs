using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DialogueController
{
    readonly MonoBehaviour _host;
    readonly PlayerController _player;
    readonly UIManager _uiManager;
    readonly AIManager _ai;
    readonly QuestSystem _quests;
    readonly InventorySystem _inventory;
    readonly DataManager _data;
    readonly SkillSystem _skills;
    readonly PlayerStats _playerState;
    readonly List<VillageNPC> _npcs = new();

    readonly List<DialogueEntry> _dialogueHistory = new();
    VillageNPC _dialogueNpc;
    bool _dialogueGenerating;
    bool _inDialogue;
    Dictionary<string, string> _loreCache;

    public bool InDialogue => _inDialogue;

    public DialogueController(MonoBehaviour host, PlayerController player, UIManager uiManager,
        AIManager ai, QuestSystem quests, InventorySystem inventory, DataManager data,
        SkillSystem skills, PlayerStats playerState)
    {
        _host = host;
        _player = player;
        _uiManager = uiManager;
        _ai = ai;
        _quests = quests;
        _inventory = inventory;
        _data = data;
        _skills = skills;
        _playerState = playerState;
    }

    public void RegisterNpcs()
    {
        var npcPrefab = Resources.Load<GameObject>("NPC");
        foreach (var npc in _data.NpcList)
        {
            string profile = _data.NpcProfiles.GetValueOrDefault(npc.id);
            _ai.RegisterNpc(npc.id, npc.name, npc.personality, profile);

            if (npc.patrol != null)
            {
                float x = (npc.patrol.cx + 0.5f) * GameConfig.TileSize;
                float y = -(npc.patrol.cy + 0.5f) * GameConfig.TileSize;
                Vector2 pos = new(x, y);

                GameObject go;
                if (npcPrefab != null)
                    go = Object.Instantiate(npcPrefab, pos, Quaternion.identity);
                else
                {
                    go = new GameObject($"NPC_{npc.id}");
                    go.AddComponent<SpriteRenderer>().sortingOrder = 5;
                    var rb = go.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0;
                    rb.freezeRotation = true;
                }

                var villageNpc = go.GetComponent<VillageNPC>();
                if (villageNpc == null) villageNpc = go.AddComponent<VillageNPC>();
                villageNpc.Init(npc, pos);
                villageNpc.Brain = _ai.GetBrain(npc.id);
                _npcs.Add(villageNpc);
            }
        }
    }

    public void TryInteract()
    {
        if (_inDialogue) return;
        Vector2 pp = _player.Position;
        VillageNPC nearest = null;
        float bestDist = float.MaxValue;
        foreach (var npc in _npcs)
        {
            if (npc == null) continue;
            float d = (npc.Position - pp).sqrMagnitude;
            if (d < bestDist && npc.IsInInteractionRange(pp))
            {
                bestDist = d;
                nearest = npc;
            }
        }
        if (nearest == null) return;

        _inDialogue = true;
        nearest.StopMoving();
        _player.Frozen = true;
        _uiManager.SetDialogueOpen(true);
        _dialogueNpc = nearest;
        _dialogueHistory.Clear();

        var conditional = nearest.EvaluateConditionalDialogue(_quests, _inventory);
        var dlg = _uiManager.Dialogue;
        if (dlg == null)
        {
            nearest.ResumeMoving();
            _player.Frozen = false;
            _uiManager.SetDialogueOpen(false);
            _dialogueNpc = null;
            _inDialogue = false;
            return;
        }

        dlg.ClearLog();
        dlg.Show(nearest.Def, conditional);
        EventBus.Emit(new DialogueStartEvent { npcId = nearest.Def.id });

        if (nearest.Def.actions != null && nearest.Def.actions.Length > 0)
            dlg.SetActionButtons(nearest.Def.actions);

        var capturedNpc = nearest;
        dlg.OnPlayerResponse = playerText =>
        {
            if (_dialogueGenerating) return;
            dlg.AppendLog("You", playerText);
            _dialogueHistory.Add(new DialogueEntry { role = "player", text = playerText });
            _ = HandleDialogueResponse(capturedNpc, playerText);
        };

        dlg.OnAction = action => HandleNpcAction(capturedNpc, action);

        dlg.OnClose = () =>
        {
            _uiManager.SetDialogueOpen(false);
            _player.Frozen = false;
            capturedNpc.ResumeMoving();
            int turns = 0;
            foreach (var e in _dialogueHistory)
                if (e.role == "npc") turns++;
            EventBus.Emit(new DialogueEndEvent { npcId = capturedNpc.Def.id, turns = turns });
            _dialogueNpc = null;
            _dialogueGenerating = false;
            _inDialogue = false;
        };

        if (conditional == null)
            _ = HandleDialogueResponse(capturedNpc, "\uc548\ub155\ud558\uc138\uc694");
    }

    async System.Threading.Tasks.Task HandleDialogueResponse(VillageNPC npc, string playerInput)
    {
        _dialogueGenerating = true;
        var dlg = _uiManager.Dialogue;
        dlg?.ShowLoading(true);

        try
        {
            string loreContext = BuildLoreContext(npc.Def);

            var response = await _ai.GenerateDialogue(
                npc.Def.id, playerInput, _dialogueHistory,
                _playerState.Level, _playerState.Gold,
                _inventory, _data.Items, _quests,
                loreContext, npc.Def.actions, npc.Def.dialogueTraits);

            dlg?.ShowLoading(false);
            _dialogueGenerating = false;

            if (response == null || dlg == null) return;

            _dialogueHistory.Add(new DialogueEntry { role = "npc", text = response.dialogue });
            dlg.AppendLog(npc.Def.name, response.dialogue, npc.Def.color);

            if (response.options != null && response.options.Length > 0)
                dlg.ShowOptions(response.options);
            else
                _host.StartCoroutine(AutoCloseDialogue(1.5f));

            if (response.offerQuest)
            {
                var questInfo = _quests.GetQuestStatusForNpc(npc.Def.id, _inventory);
                if (questInfo.HasValue)
                {
                    if (questInfo.Value.status == "available")
                    {
                        var quest = questInfo.Value.quest;
                        var brain = _ai.GetBrain(npc.Def.id);
                        int rel = brain?.GetRelationship("player") ?? 0;
                        float genMult = npc.Def.dialogueTraits?.generosity / 10f ?? 0.5f;
                        var rewards = _quests.GetScaledRewards(quest, 0, rel, genMult);
                        dlg.ShowQuestProposal(quest, rewards);
                    }
                    else if (questInfo.Value.status == "completable")
                    {
                        dlg.OnCompleteQuest?.Invoke(questInfo.Value.quest.id);
                        dlg.AppendLog("System", "Quest completed!", "#00ff00");
                    }
                }
            }

            if (!string.IsNullOrEmpty(response.action))
                HandleNpcAction(npc, response.action);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueController] Dialogue error: {ex.Message}");
            dlg?.ShowLoading(false);
            _dialogueGenerating = false;
            dlg?.AppendLog("System", "...", "#999999");
        }
    }

    IEnumerator AutoCloseDialogue(float delay)
    {
        yield return new WaitForSeconds(delay);
        var dlg = _uiManager?.Dialogue;
        if (dlg != null)
        {
            dlg.Hide();
            dlg.OnClose?.Invoke();
        }
    }

    void HandleNpcAction(VillageNPC npc, string action)
    {
        switch (action)
        {
            case "heal_player":
                _playerState.Hp = _playerState.CurrentStats.maxHp;
                _playerState.Mp = _playerState.CurrentStats.maxMp;
                AudioManager.Instance?.PlaySFX("sfx_potion");
                _uiManager.Hud?.AddHistoryEntry("HP/MP fully restored!", Color.green);
                break;
            case "reset_skills":
                int refunded = _skills.ResetAllSkills();
                _playerState.SkillPoints += refunded;
                _uiManager.Hud?.AddHistoryEntry($"Skills reset! (+{refunded} SP)", Color.cyan);
                break;
            case "reset_stats":
                int totalBonus = 0;
                foreach (var kv in _playerState.BonusStats) totalBonus += kv.Value;
                foreach (var key in new List<string>(_playerState.BonusStats.Keys))
                    _playerState.BonusStats[key] = 0;
                _playerState.StatPoints += totalBonus;
                _playerState.RecalcStats(_data.Items, _data.SetBonuses);
                _uiManager.Hud?.AddHistoryEntry($"Stats reset! (+{totalBonus} points)", Color.cyan);
                break;
            case "open_shop":
                break;
            case "open_crafting":
                break;
        }
    }

    string BuildLoreContext(NpcDef npcDef)
    {
        if (npcDef.loreDocs == null || npcDef.loreDocs.Length == 0) return null;

        if (_loreCache == null)
        {
            _loreCache = new Dictionary<string, string>();
            string loreDir = Path.Combine(Application.streamingAssetsPath, "Data", "lore");
            if (Directory.Exists(loreDir))
            {
                foreach (string file in Directory.GetFiles(loreDir, "*.md"))
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    _loreCache[key] = File.ReadAllText(file);
                }
            }
        }

        var sb = new StringBuilder();
        foreach (string doc in npcDef.loreDocs)
        {
            if (_loreCache.TryGetValue(doc, out string content))
            {
                sb.AppendLine(content);
                sb.AppendLine();
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
