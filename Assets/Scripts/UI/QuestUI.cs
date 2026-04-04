using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Tabs")]
    [SerializeField] Button activeTab;
    [SerializeField] Button completedTab;
    [SerializeField] Image activeTabBg;
    [SerializeField] Image completedTabBg;

    [Header("Content")]
    [SerializeField] Transform questListContent;
    [SerializeField] GameObject questEntryPrefab;

    static readonly Color ActiveTabColor = new(0.3f, 0.4f, 0.6f);
    static readonly Color InactiveTabColor = new(0.2f, 0.2f, 0.2f);
    static readonly Color MetColor = new(0.4f, 1f, 0.4f);
    static readonly Color UnmetColor = new(1f, 0.4f, 0.4f);

    bool _showCompleted;
    readonly List<GameObject> _entries = new();

    QuestSystem _questSystem;
    InventorySystem _inventory;
    Dictionary<string, ItemDef> _itemDefs;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (activeTab != null) activeTab.onClick.AddListener(() => SetTab(false));
        if (completedTab != null) completedTab.onClick.AddListener(() => SetTab(true));
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show()
    {
        if (IsOpen) return;
        if (panel != null) panel.SetActive(true);
        AudioManager.Instance?.PlaySFX("sfx_menu_open");
        Refresh();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_menu_close");
    }
    public void Toggle() { if (IsOpen) Hide(); else Show(); }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        Refresh(gm.Quests, gm.Inventory, gm.Data.Items);
    }

    public void Refresh(QuestSystem questSystem, InventorySystem inventory, Dictionary<string, ItemDef> itemDefs)
    {
        _questSystem = questSystem;
        _inventory = inventory;
        _itemDefs = itemDefs;
        if (_questSystem == null) return;
        RebuildList();
    }

    void SetTab(bool completed)
    {
        _showCompleted = completed;
        if (activeTabBg != null) activeTabBg.color = completed ? InactiveTabColor : ActiveTabColor;
        if (completedTabBg != null) completedTabBg.color = completed ? ActiveTabColor : InactiveTabColor;
        AudioManager.Instance?.PlaySFX("sfx_tab_switch");
        RebuildList();
    }

    void RebuildList()
    {
        ClearEntries();
        if (_questSystem == null) return;

        if (_showCompleted)
        {
            var data = _questSystem.Serialize();
            if (data.completed != null)
            {
                foreach (string questId in data.completed)
                    AddCompletedEntry(questId);
            }
        }
        else
        {
            var activeQuests = _questSystem.GetActiveQuests();
            foreach (var quest in activeQuests)
                AddActiveEntry(quest);
        }

        if (_entries.Count == 0)
            AddEmptyPlaceholder(_showCompleted ? "No completed quests" : "No active quests");
    }

    void AddEmptyPlaceholder(string message)
    {
        if (questListContent == null || questEntryPrefab == null) return;

        var go = Instantiate(questEntryPrefab, questListContent);
        go.SetActive(true);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts.Length > 0)
        {
            texts[0].text = $"<color=#888888>{message}</color>";
            texts[0].color = Color.white;
            texts[0].fontStyle = FontStyles.Italic;
        }
        for (int i = 1; i < texts.Length; i++)
            texts[i].text = "";

        _entries.Add(go);
    }

    void AddActiveEntry(QuestDef quest)
    {
        if (questListContent == null || questEntryPrefab == null) return;

        var go = Instantiate(questEntryPrefab, questListContent);
        go.SetActive(true);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts.Length == 0) return;

        var titleText = texts[0];
        titleText.color = Color.white;
        titleText.text = $"<b><color=#ffe888>\u25b8 {quest.title}</color></b>";

        if (texts.Length > 1)
        {
            texts[1].text = quest.description ?? "";
            texts[1].color = new Color(0.78f, 0.78f, 0.78f);
        }

        if (texts.Length > 2)
        {
            var lines = new List<string>();
            if (quest.requirements != null)
            {
                foreach (var req in quest.requirements)
                {
                    int have = _inventory != null ? _inventory.GetCount(req.itemId) : 0;
                    bool met = have >= req.count;
                    string check = met ? "<color=#66ff66>\u2713</color>" : "<color=#ff5555>\u2717</color>";
                    string itemName = _itemDefs != null && _itemDefs.TryGetValue(req.itemId, out var def)
                        ? def.name : req.itemId;
                    string countColor = met ? "#66ff66" : "#ffaa44";
                    lines.Add($"  {check} {itemName} <color={countColor}>(<b>{have}</b>/{req.count})</color>");
                }
            }
            if (quest.killRequirements != null)
            {
                foreach (var kr in quest.killRequirements)
                {
                    int kills = _questSystem != null ? _questSystem.GetKillProgress(quest.id, kr.monsterId) : 0;
                    bool met = kills >= kr.count;
                    string check = met ? "<color=#66ff66>\u2713</color>" : "<color=#ff5555>\u2717</color>";
                    string countColor = met ? "#66ff66" : "#ffaa44";
                    lines.Add($"  {check} <color=#ffbb77>{kr.monsterId}</color> <color={countColor}>(<b>{kills}</b>/{kr.count})</color>");
                }
            }
            texts[2].color = Color.white;
            texts[2].text = string.Join("\n", lines);
        }

        if (texts.Length > 3 && quest.rewards != null)
        {
            var rewardLines = new List<string>();
            if (quest.rewards.gold > 0) rewardLines.Add($"<color=#ffd900>\u25c6 <b>{quest.rewards.gold:N0}</b>G</color>");
            if (quest.rewards.xp > 0) rewardLines.Add($"<color=#aaffaa>\u25b8 <b>{quest.rewards.xp}</b> XP</color>");
            if (quest.rewards.items != null)
            {
                foreach (var item in quest.rewards.items)
                {
                    string itemName = _itemDefs != null && _itemDefs.TryGetValue(item.itemId, out var def)
                        ? def.name : item.itemId;
                    rewardLines.Add($"<color=#ccddff>\u25b9 {itemName} \u00d7{item.count}</color>");
                }
            }
            texts[3].color = Color.white;
            texts[3].text = rewardLines.Count > 0
                ? string.Join("\n", rewardLines) : "";
        }

        _entries.Add(go);
    }

    void AddCompletedEntry(string questId)
    {
        if (questListContent == null || questEntryPrefab == null) return;

        var go = Instantiate(questEntryPrefab, questListContent);
        go.SetActive(true);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts.Length > 0)
        {
            string title = questId;
            if (_questSystem != null)
            {
                var defs = _questSystem.GetQuestDefs();
                foreach (var d in defs)
                    if (d.id == questId) { title = d.title; break; }
            }
            texts[0].text = $"<color=#66ff66><b>\u2713</b></color> <color=#99ff99><b>{title}</b></color>";
            texts[0].color = Color.white;
        }
        for (int i = 1; i < texts.Length; i++)
            texts[i].text = "";

        _entries.Add(go);
    }

    void ClearEntries()
    {
        foreach (var go in _entries)
            Destroy(go);
        _entries.Clear();
    }
}
