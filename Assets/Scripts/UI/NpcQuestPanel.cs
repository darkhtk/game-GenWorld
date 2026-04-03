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

        if (npcNameText != null) { npcNameText.color = Color.white; npcNameText.text = $"<b><color=#ffdd88>{npcName}</color></b>"; }
        if (questTitleText != null) { questTitleText.color = Color.white; questTitleText.text = $"<b><color=#ffe888>\u25b8 {quest.title}</color></b>"; }
        if (questDescText != null) { questDescText.text = quest.description ?? ""; questDescText.color = new Color(0.78f, 0.78f, 0.78f); }
        if (statusText != null)
        {
            statusText.color = Color.white;
            statusText.text = status switch
            {
                "completable" => "<color=#66ff66><b>\u2713 Ready to Complete</b></color>",
                "active"      => "<color=#66aaff>\u25cf In Progress</color>",
                "available"   => "<color=#ffee88>\u25b8 Available</color>",
                _             => ""
            };
        }

        if (requirementsText != null)
        {
            requirementsText.color = Color.white;
            var lines = new List<string>();
            if (quest.requirements != null)
            {
                foreach (var req in quest.requirements)
                    lines.Add($"  <color=#aaaaaa>\u25b9</color> {req.itemId} <color=#888888>\u00d7{req.count}</color>");
            }
            if (quest.killRequirements != null)
            {
                foreach (var kr in quest.killRequirements)
                    lines.Add($"  <color=#aaaaaa>\u25b9</color> <color=#ffbb77>{kr.monsterId}</color> <color=#888888>\u00d7{kr.count}</color>");
            }
            requirementsText.text = lines.Count > 0 ? string.Join("\n", lines) : "<color=#666666>None</color>";
        }

        if (rewardsText != null)
        {
            rewardsText.color = Color.white;
            var lines = new List<string>();
            if (quest.rewards != null)
            {
                if (quest.rewards.gold > 0) lines.Add($"<color=#ffd900>\u25c6 {quest.rewards.gold:N0}G</color>");
                if (quest.rewards.xp > 0) lines.Add($"<color=#aaffaa>\u25b8 {quest.rewards.xp} XP</color>");
                if (quest.rewards.items != null)
                {
                    foreach (var item in quest.rewards.items)
                        lines.Add($"<color=#ccddff>\u25b9 {item.itemId} \u00d7{item.count}</color>");
                }
            }
            rewardsText.text = lines.Count > 0 ? string.Join("\n", lines) : "<color=#666666>None</color>";
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
