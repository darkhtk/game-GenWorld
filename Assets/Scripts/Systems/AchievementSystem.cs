using System.Collections.Generic;
using UnityEngine;

public struct AchievementUnlockedEvent { public string id, name; public string rewardDesc; }

public class AchievementSystem
{
    public struct AchievementDef
    {
        public string id, name, description;
        public string eventType;
        public int requiredCount;
        public string rewardType, rewardId;
        public int rewardAmount;
    }

    readonly Dictionary<string, AchievementDef> _defs = new();
    readonly Dictionary<string, int> _progress = new();
    readonly HashSet<string> _completed = new();

    public void Init()
    {
        Register("first_blood", "First Blood", "Defeat your first monster", "monster_kill", 1, "gold", null, 50);
        Register("monster_hunter", "Monster Hunter", "Defeat 100 monsters", "monster_kill", 100, "gold", null, 500);
        Register("level_10", "Rising Star", "Reach level 10", "level_up", 10, "gold", null, 100);
        Register("level_25", "Veteran", "Reach level 25", "level_up", 25, "gold", null, 500);
        Register("quest_5", "Errand Runner", "Complete 5 quests", "quest_complete", 5, "gold", null, 200);
        Register("rich", "Wealthy", "Have 10,000 gold", "gold", 10000, "gold", null, 0);

        EventBus.On<MonsterKillEvent>(_ => OnEvent("monster_kill", 1));
        EventBus.On<LevelUpEvent>(e => OnEvent("level_up", e.level));
        EventBus.On<QuestCompleteEvent>(_ => OnEvent("quest_complete", 1));
        EventBus.On<GoldChangeEvent>(e => OnEvent("gold", e.gold));
    }

    void Register(string id, string name, string desc, string eventType, int required,
        string rewardType, string rewardId, int rewardAmount)
    {
        _defs[id] = new AchievementDef
        {
            id = id, name = name, description = desc, eventType = eventType,
            requiredCount = required, rewardType = rewardType, rewardId = rewardId,
            rewardAmount = rewardAmount
        };
    }

    public void OnEvent(string eventType, int value)
    {
        foreach (var kv in _defs)
        {
            var def = kv.Value;
            if (def.eventType != eventType) continue;
            if (_completed.Contains(def.id)) continue;

            if (eventType == "gold" || eventType == "level_up")
                _progress[def.id] = value;
            else
            {
                _progress.TryGetValue(def.id, out int cur);
                _progress[def.id] = cur + 1;
            }

            if (_progress[def.id] >= def.requiredCount)
            {
                _completed.Add(def.id);
                GiveReward(def);
                string rewardDesc = def.rewardAmount > 0 ? $"{def.rewardAmount} {def.rewardType}" : "";
                EventBus.Emit(new AchievementUnlockedEvent { id = def.id, name = def.name, rewardDesc = rewardDesc });
                Debug.Log($"[Achievement] Unlocked: {def.name}");
            }
        }
    }

    void GiveReward(AchievementDef def)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (def.rewardType == "gold" && def.rewardAmount > 0)
        {
            gm.PlayerState.Gold += def.rewardAmount;
            EventBus.Emit(new GoldChangeEvent { gold = gm.PlayerState.Gold });
        }
        else if (def.rewardType == "item" && !string.IsNullOrEmpty(def.rewardId))
        {
            bool stackable = gm.Data.Items.TryGetValue(def.rewardId, out var itemDef) && itemDef.stackable;
            gm.Inventory.AddItem(def.rewardId, def.rewardAmount > 0 ? def.rewardAmount : 1,
                stackable, itemDef?.maxStack ?? 1);
        }
    }

    public bool IsCompleted(string id) => _completed.Contains(id);

    public (int current, int required) GetProgress(string id)
    {
        _progress.TryGetValue(id, out int cur);
        return _defs.TryGetValue(id, out var def) ? (cur, def.requiredCount) : (0, 0);
    }

    public AchievementDef[] GetAll()
    {
        var result = new AchievementDef[_defs.Count];
        _defs.Values.CopyTo(result, 0);
        return result;
    }

    public Dictionary<string, int> SerializeProgress() => new(_progress);
    public string[] SerializeCompleted() => new List<string>(_completed).ToArray();

    public void Restore(Dictionary<string, int> progress, string[] completed)
    {
        _progress.Clear();
        _completed.Clear();
        if (progress != null)
            foreach (var kv in progress) _progress[kv.Key] = kv.Value;
        if (completed != null)
            foreach (var c in completed) _completed.Add(c);
    }
}
