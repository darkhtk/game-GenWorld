using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcQuestPanel : MonoBehaviour
{
    public Action<string> OnAcceptQuest;
    public Action<string> OnCompleteQuest;

    [Header("Panel")]
    [SerializeField] GameObject panel;

    [Header("Quest Info")]
    [SerializeField] TextMeshProUGUI npcNameText;
    [SerializeField] TextMeshProUGUI questTitleText;
    [SerializeField] TextMeshProUGUI questDescText;
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Requirements")]
    [SerializeField] TextMeshProUGUI requirementsText;

    [Header("Rewards")]
    [SerializeField] TextMeshProUGUI rewardsText;

    [Header("Buttons")]
    [SerializeField] Button acceptButton;
    [SerializeField] Button completeButton;
    [SerializeField] TextMeshProUGUI acceptButtonText;
    [SerializeField] TextMeshProUGUI completeButtonText;

    string _currentQuestId;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(() =>
            {
                if (_currentQuestId != null)
                    OnAcceptQuest?.Invoke(_currentQuestId);
            });

        if (completeButton != null)
            completeButton.onClick.AddListener(() =>
            {
                if (_currentQuestId != null)
                    OnCompleteQuest?.Invoke(_currentQuestId);
            });
    }

    public void Show(string npcName, QuestDef quest, string status)
    {
        if (panel != null) panel.SetActive(true);
        _currentQuestId = quest.id;

        if (npcNameText != null) { npcNameText.color = Color.white; npcNameText.text = $"<b><color=#ffdd44>{npcName}</color></b>"; }
        if (questTitleText != null) { questTitleText.color = Color.white; questTitleText.text = $"<b><color=#ffdd44>\u25b8 {quest.title}</color></b>"; }
        if (questDescText != null) { questDescText.text = quest.description ?? ""; questDescText.color = new Color(0.733f, 0.733f, 0.733f); }
        if (statusText != null)
        {
            statusText.color = Color.white;
            statusText.text = status switch
            {
                "completable" => "<color=#66ff88><b>\u2713 Ready to Complete</b></color>",
                "active"      => "<color=#88aaff>\u25cf In Progress</color>",
                "available"   => "<color=#ffdd44>\u25b8 Available</color>",
                _             => ""
            };
        }

        if (requirementsText != null)
        {
            requirementsText.color = Color.white;
            var itemDefs = GameManager.Instance?.Data?.Items;
            var lines = new List<string>();
            if (quest.requirements != null)
            {
                foreach (var req in quest.requirements)
                {
                    string name = itemDefs != null && itemDefs.TryGetValue(req.itemId, out var d) ? d.name : req.itemId;
                    lines.Add($"  <color=#aaaaaa>\u25b9</color> {name} <color=#888888>\u00d7{req.count}</color>");
                }
            }
            if (quest.killRequirements != null)
            {
                var monsters = GameManager.Instance?.Data?.Monsters;
                foreach (var kr in quest.killRequirements)
                {
                    string mName = monsters != null && monsters.TryGetValue(kr.monsterId, out var mDef) ? mDef.name : kr.monsterId;
                    lines.Add($"  <color=#aaaaaa>\u25b9</color> <color=#ff9944>{mName}</color> <color=#888888>\u00d7{kr.count}</color>");
                }
            }
            requirementsText.text = lines.Count > 0 ? string.Join("\n", lines) : "<color=#444444>None</color>";
        }

        if (rewardsText != null)
        {
            rewardsText.color = Color.white;
            var itemDefs = GameManager.Instance?.Data?.Items;
            var lines = new List<string>();
            if (quest.rewards != null)
            {
                if (quest.rewards.gold > 0) lines.Add($"<color=#ffd900>\u25c6 <b>{quest.rewards.gold:N0}</b>G</color>");
                if (quest.rewards.xp > 0) lines.Add($"<color=#66ff88>\u25b8 <b>{quest.rewards.xp}</b> XP</color>");
                if (quest.rewards.items != null)
                {
                    foreach (var item in quest.rewards.items)
                    {
                        string name = itemDefs != null && itemDefs.TryGetValue(item.itemId, out var d) ? d.name : item.itemId;
                        lines.Add($"<color=#88ddff>\u25b9 {name} \u00d7{item.count}</color>");
                    }
                }
            }
            rewardsText.text = lines.Count > 0 ? string.Join("\n", lines) : "<color=#444444>None</color>";
        }

        bool isAcceptable = status == "available" || status == null;
        bool isCompletable = status == "completable";

        if (acceptButton != null) acceptButton.gameObject.SetActive(isAcceptable);
        if (completeButton != null) completeButton.gameObject.SetActive(isCompletable);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        _currentQuestId = null;
    }
}
