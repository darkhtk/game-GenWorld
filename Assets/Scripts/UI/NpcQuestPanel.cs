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

        if (npcNameText != null) { npcNameText.text = npcName; npcNameText.color = new Color(1f, 0.9f, 0.6f); }
        if (questTitleText != null) { questTitleText.text = $"\u25b8 {quest.title}"; questTitleText.color = new Color(1f, 0.95f, 0.5f); }
        if (questDescText != null) questDescText.text = quest.description ?? "";
        if (statusText != null)
        {
            statusText.text = status ?? "";
            statusText.color = status == "completable" ? new Color(0.4f, 1f, 0.4f)
                : status == "active" ? new Color(0.6f, 0.8f, 1f)
                : Color.white;
        }

        if (requirementsText != null)
        {
            if (quest.requirements != null && quest.requirements.Length > 0)
            {
                var lines = new List<string>();
                foreach (var req in quest.requirements)
                    lines.Add($"  \u25b9 {req.itemId} \u00d7{req.count}");
                requirementsText.text = string.Join("\n", lines);
            }
            else requirementsText.text = "None";
        }

        if (rewardsText != null && quest.rewards != null)
        {
            var lines = new List<string>();
            if (quest.rewards.gold > 0) lines.Add($"<color=#ffd900>Gold: {quest.rewards.gold}</color>");
            if (quest.rewards.xp > 0) lines.Add($"<color=#aaffaa>XP: {quest.rewards.xp}</color>");
            if (quest.rewards.items != null)
            {
                foreach (var item in quest.rewards.items)
                    lines.Add($"\u25b8 {item.itemId} \u00d7{item.count}");
            }
            rewardsText.text = string.Join("\n", lines);
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
