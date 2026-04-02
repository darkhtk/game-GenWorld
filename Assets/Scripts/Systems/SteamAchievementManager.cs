using System.Collections.Generic;
using UnityEngine;

public class SteamAchievementManager : MonoBehaviour
{
    public static SteamAchievementManager Instance { get; private set; }

    readonly Dictionary<string, int> _stats = new();
    readonly HashSet<string> _unlocked = new();

    struct AchievementMapping
    {
        public string steamApiName;
        public string eventType;
        public string statName;
        public int statThreshold;
    }

    readonly List<AchievementMapping> _mappings = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        RegisterMappings();
        SubscribeEvents();
    }

    void RegisterMappings()
    {
        Add("ACH_FIRST_BLOOD", "monster_kill", null, 0);
        Add("ACH_MONSTER_HUNTER", null, "monsters_killed", 100);
        Add("ACH_LEVEL_10", "level_10", null, 0);
        Add("ACH_LEVEL_25", "level_25", null, 0);
        Add("ACH_QUEST_MASTER", null, "quests_completed", 10);
        Add("ACH_RICH", "gold_10k", null, 0);
        Add("ACH_BOSS_SLAYER", "boss_kill", null, 0);
        Add("ACH_CRAFTER", null, "items_crafted", 20);
    }

    void Add(string apiName, string eventType, string statName, int threshold)
    {
        _mappings.Add(new AchievementMapping
        {
            steamApiName = apiName, eventType = eventType,
            statName = statName, statThreshold = threshold
        });
    }

    void SubscribeEvents()
    {
        EventBus.On<MonsterKillEvent>(e =>
        {
            TriggerEvent("monster_kill");
            IncrementStat("monsters_killed", 1);
            if (e.monsterName != null && e.monsterName.Contains("boss"))
                TriggerEvent("boss_kill");
        });
        EventBus.On<LevelUpEvent>(e =>
        {
            if (e.level >= 10) TriggerEvent("level_10");
            if (e.level >= 25) TriggerEvent("level_25");
        });
        EventBus.On<QuestCompleteEvent>(_ => IncrementStat("quests_completed", 1));
        EventBus.On<GoldChangeEvent>(e => { if (e.gold >= 10000) TriggerEvent("gold_10k"); });
    }

    void TriggerEvent(string eventType)
    {
        foreach (var m in _mappings)
        {
            if (m.eventType != eventType) continue;
            UnlockAchievement(m.steamApiName);
        }
    }

    public void IncrementStat(string statName, int amount)
    {
        _stats.TryGetValue(statName, out int current);
        _stats[statName] = current + amount;

        foreach (var m in _mappings)
        {
            if (m.statName != statName || m.statThreshold <= 0) continue;
            if (_stats[statName] >= m.statThreshold)
                UnlockAchievement(m.steamApiName);
        }

#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance != null && SteamManager.Instance.Initialized)
        {
            Steamworks.SteamUserStats.SetStat(statName, _stats[statName]);
            Steamworks.SteamUserStats.StoreStats();
        }
#endif
    }

    public void UnlockAchievement(string apiName)
    {
        if (_unlocked.Contains(apiName)) return;
        _unlocked.Add(apiName);
        Debug.Log($"[SteamAchievement] Unlocked: {apiName}");

#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance != null && SteamManager.Instance.Initialized)
        {
            Steamworks.SteamUserStats.SetAchievement(apiName);
            Steamworks.SteamUserStats.StoreStats();
        }
#endif
    }

    public bool IsUnlocked(string apiName) => _unlocked.Contains(apiName);

    public void ResetAll()
    {
        _unlocked.Clear();
        _stats.Clear();
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance != null && SteamManager.Instance.Initialized)
        {
            Steamworks.SteamUserStats.ResetAllStats(true);
            Steamworks.SteamUserStats.StoreStats();
        }
#endif
        Debug.Log("[SteamAchievement] All achievements reset");
    }
}
