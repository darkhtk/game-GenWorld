using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestSystem
{
    static readonly float[] TierMult = { 1f, 1.5f, 2f, 2.5f };

    readonly QuestDef[] _defs;
    readonly Dictionary<string, QuestDef> _active = new();
    readonly HashSet<string> _completed = new();

    public QuestSystem(QuestDef[] defs) => _defs = defs;

    public QuestDef[] GetQuestDefs() => _defs;

    public bool AcceptQuest(string questId)
    {
        var def = _defs.FirstOrDefault(q => q.id == questId);
        if (def == null || _active.ContainsKey(questId) || _completed.Contains(questId))
            return false;
        _active[questId] = def;
        EventBus.Emit(new QuestAcceptEvent { questId = questId, npcId = def.npcId });
        return true;
    }

    public QuestReward CompleteQuest(string questId, InventorySystem inv)
    {
        if (!_active.TryGetValue(questId, out var def)) return null;
        if (!def.requirements.All(r => inv.HasItems(r.itemId, r.count))) return null;

        foreach (var r in def.requirements)
            inv.RemoveItem(r.itemId, r.count);

        _active.Remove(questId);
        _completed.Add(questId);
        EventBus.Emit(new QuestCompleteEvent
        {
            questTitle = def.title,
            completedCount = _completed.Count
        });
        return def.rewards;
    }

    public QuestDef[] GetActiveQuests() => _active.Values.ToArray();
    public bool IsActive(string questId) => _active.ContainsKey(questId);
    public bool IsCompleted(string questId) => _completed.Contains(questId);

    public string GetStatus(string questId, InventorySystem inv)
    {
        if (_completed.Contains(questId)) return "completed";
        if (_active.ContainsKey(questId))
        {
            var def = _active[questId];
            bool met = def.requirements.All(r => inv.HasItems(r.itemId, r.count));
            return met ? "completable" : "active";
        }
        return "available";
    }

    public (QuestDef quest, string status)? GetQuestStatusForNpc(string npcId, InventorySystem inv)
    {
        foreach (var kvp in _active)
        {
            if (kvp.Value.npcId != npcId) continue;
            bool met = kvp.Value.requirements.All(r => inv.HasItems(r.itemId, r.count));
            if (met) return (kvp.Value, "completable");
        }
        foreach (var kvp in _active)
        {
            if (kvp.Value.npcId == npcId) return (kvp.Value, "active");
        }
        var available = _defs.FirstOrDefault(q =>
            q.npcId == npcId && !_active.ContainsKey(q.id) && !_completed.Contains(q.id));
        if (available != null) return (available, "available");
        return null;
    }

    public QuestDef PickQuestForNpc(string npcId, int rejectionCount, int relationship = 0)
    {
        return _defs.FirstOrDefault(q =>
            q.npcId == npcId && !_active.ContainsKey(q.id) && !_completed.Contains(q.id));
    }

    public QuestReward GetScaledRewards(QuestDef quest, int rejectionCount, int relationship,
        float generosityMult = 1f)
    {
        float tier = rejectionCount < TierMult.Length ? TierMult[rejectionCount] : 2.5f;
        float relBonus = relationship >= 20 ? 1.5f : relationship >= 10 ? 1.2f : 1f;
        float mult = tier * relBonus * generosityMult;

        return new QuestReward
        {
            gold = Mathf.RoundToInt(quest.rewards.gold * mult),
            xp = Mathf.RoundToInt(quest.rewards.xp * mult),
            items = quest.rewards.items
        };
    }

    public (string[] active, string[] completed) Serialize() =>
        (_active.Keys.ToArray(), _completed.ToArray());

    public void Restore((string[] active, string[] completed) data)
    {
        _active.Clear();
        _completed.Clear();
        if (data.completed != null)
            foreach (var id in data.completed) _completed.Add(id);
        if (data.active != null)
            foreach (var id in data.active)
            {
                var def = _defs.FirstOrDefault(q => q.id == id);
                if (def != null) _active[id] = def;
            }
    }
}
