using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestSystem
{
    static readonly float[] TierMult = { 1f, 1.5f, 2f, 2.5f };

    readonly QuestDef[] _defs;
    readonly Dictionary<string, QuestDef> _active = new();
    readonly HashSet<string> _completed = new();
    readonly Dictionary<string, Dictionary<string, int>> _killProgress = new();

    public QuestSystem(QuestDef[] defs) => _defs = defs;

    public QuestDef[] GetQuestDefs() => _defs;

    public void SubscribeEvents()
    {
        EventBus.On<MonsterKillEvent>(OnMonsterKill);
    }

    void OnMonsterKill(MonsterKillEvent e)
    {
        foreach (var kvp in _active)
        {
            var def = kvp.Value;
            if (def.killRequirements == null || def.killRequirements.Length == 0) continue;

            foreach (var kr in def.killRequirements)
            {
                if (kr.monsterId != e.monsterId) continue;
                if (!_killProgress.ContainsKey(kvp.Key))
                    _killProgress[kvp.Key] = new Dictionary<string, int>();
                _killProgress[kvp.Key].TryGetValue(kr.monsterId, out int count);
                _killProgress[kvp.Key][kr.monsterId] = count + 1;
            }
        }
    }

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
        if (!AreRequirementsMet(def, inv)) return null;

        if (def.requirements != null)
            foreach (var r in def.requirements)
                inv.RemoveItem(r.itemId, r.count);

        _active.Remove(questId);
        _killProgress.Remove(questId);
        _completed.Add(questId);
        EventBus.Emit(new QuestCompleteEvent
        {
            questTitle = def.title,
            completedCount = _completed.Count
        });
        return def.rewards;
    }

    bool AreRequirementsMet(QuestDef def, InventorySystem inv)
    {
        if (def.requirements != null && !def.requirements.All(r => inv.HasItems(r.itemId, r.count)))
            return false;

        if (def.killRequirements != null && def.killRequirements.Length > 0)
        {
            _killProgress.TryGetValue(def.id, out var progress);
            if (progress == null) return false;
            foreach (var kr in def.killRequirements)
            {
                progress.TryGetValue(kr.monsterId, out int count);
                if (count < kr.count) return false;
            }
        }

        return true;
    }

    public int GetKillProgress(string questId, string monsterId)
    {
        if (!_killProgress.TryGetValue(questId, out var progress)) return 0;
        progress.TryGetValue(monsterId, out int count);
        return count;
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
            return AreRequirementsMet(def, inv) ? "completable" : "active";
        }
        return "available";
    }

    public (QuestDef quest, string status)? GetQuestStatusForNpc(string npcId, InventorySystem inv)
    {
        foreach (var kvp in _active)
        {
            if (kvp.Value.npcId != npcId) continue;
            if (AreRequirementsMet(kvp.Value, inv)) return (kvp.Value, "completable");
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
        if (quest.rewards == null)
            return new QuestReward { gold = 0, xp = 0, items = null };

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
        _killProgress.Clear();
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
