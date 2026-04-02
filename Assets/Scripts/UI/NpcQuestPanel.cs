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

        if (npcNameText != null) npcNameText.text = npcName;
        if (questTitleText != null) questTitleText.text = quest.title;
        if (questDescText != null) questDescText.text = quest.description ?? "";
        if (statusText != null) statusText.text = status ?? "";

        if (requirementsText != null && quest.requirements != null)
        {
            var lines = new List<string>();
            foreach (var req in quest.requirements)
                lines.Add($"  {req.itemId} x{req.count}");
            requirementsText.text = lines.Count > 0 ? string.Join("\n", lines) : "None";
        }

        if (rewardsText != null && quest.rewards != null)
        {
            var lines = new List<string>();
            if (quest.rewards.gold > 0) lines.Add($"Gold: {quest.rewards.gold}");
            if (quest.rewards.xp > 0) lines.Add($"XP: {quest.rewards.xp}");
            if (quest.rewards.items != null)
            {
                foreach (var item in quest.rewards.items)
                    lines.Add($"{item.itemId} x{item.count}");
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
