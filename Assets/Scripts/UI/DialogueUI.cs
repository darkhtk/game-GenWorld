using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public Action<string> OnPlayerResponse;
    public Action<string> OnAcceptQuest;
    public Action<string> OnCompleteQuest;
    public Action OnClose;
    public Action<string> OnAction;

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("NPC Info")]
    [SerializeField] TextMeshProUGUI npcNameText;
    [SerializeField] TextMeshProUGUI questTitleText;
    [SerializeField] Image npcPortraitImage;

    [Header("Conversation Log")]
    [SerializeField] ScrollRect logScrollRect;
    [SerializeField] Transform logContent;
    [SerializeField] TextMeshProUGUI logEntryPrefab;

    [Header("Options")]
    [SerializeField] Transform optionsContainer;
    [SerializeField] Button optionButtonPrefab;

    [Header("Free Input")]
    [SerializeField] TMP_InputField freeInputField;
    [SerializeField] Button sendButton;
    [SerializeField] Button freeInputToggle;

    [Header("Action Buttons")]
    [SerializeField] Transform actionButtonsContainer;
    [SerializeField] Button actionButtonPrefab;

    [Header("Loading")]
    [SerializeField] GameObject loadingPanel;
    [SerializeField] TextMeshProUGUI loadingText;

    [Header("Quest Proposal")]
    [SerializeField] GameObject questProposalPanel;
    [SerializeField] TextMeshProUGUI questProposalTitle;
    [SerializeField] TextMeshProUGUI questProposalDesc;
    [SerializeField] TextMeshProUGUI questProposalRequirements;
    [SerializeField] TextMeshProUGUI questProposalRewards;
    [SerializeField] Button questAcceptButton;
    [SerializeField] Button questRejectButton;

    const int MaxLogEntries = 50;
    readonly List<GameObject> _logEntries = new();
    readonly List<Button> _optionButtons = new();
    readonly List<Button> _actionButtons = new();

    NpcDef _currentNpc;
    Coroutine _loadingCoroutine;

    static readonly string[] ThinkingPhrases =
    {
        "생각 중...", "흠...", "잠시만...",
        "고민 중...", "곰곰이..."
    };

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (questProposalPanel != null) questProposalPanel.SetActive(false);
        if (freeInputField != null) freeInputField.gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => { Hide(); OnClose?.Invoke(); });

        if (sendButton != null)
            sendButton.onClick.AddListener(SendFreeInput);

        if (freeInputToggle != null)
            freeInputToggle.onClick.AddListener(ToggleFreeInput);

        if (freeInputField != null)
            freeInputField.onSubmit.AddListener(_ => SendFreeInput());
    }

    public void Show(NpcDef npcDef, ConditionalDialogue conditional = null)
    {
        _currentNpc = npcDef;
        if (panel != null) panel.SetActive(true);

        if (npcNameText != null)
        {
            npcNameText.text = npcDef.name;
            if (ColorUtility.TryParseHtmlString(npcDef.color, out var npcColor))
                npcNameText.color = npcColor;
        }
        if (questTitleText != null) questTitleText.text = "";

        // Load NPC portrait
        if (npcPortraitImage != null)
        {
            Sprite portrait = null;
            if (!string.IsNullOrEmpty(npcDef.id))
                portrait = Resources.Load<Sprite>($"Sprites/Portraits/portrait_{npcDef.id}");
            if (portrait == null && !string.IsNullOrEmpty(npcDef.sprite))
            {
                var sprites = Resources.LoadAll<Sprite>($"Sprites/{npcDef.sprite}");
                if (sprites != null && sprites.Length > 0) portrait = sprites[0];
            }
            npcPortraitImage.sprite = portrait;
            npcPortraitImage.enabled = true;
            npcPortraitImage.color = portrait != null ? Color.white : new Color(0.3f, 0.3f, 0.4f);
        }

        AudioManager.Instance?.PlaySFX("sfx_speech");

        if (conditional != null)
        {
            if (!string.IsNullOrEmpty(conditional.greeting))
                AppendLog(npcDef.name, conditional.greeting, npcDef.color);
            if (conditional.options != null && conditional.options.Length > 0)
                ShowOptions(conditional.options);
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        if (_loadingCoroutine != null) { StopCoroutine(_loadingCoroutine); _loadingCoroutine = null; }
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (questProposalPanel != null) questProposalPanel.SetActive(false);
        if (freeInputField != null) freeInputField.gameObject.SetActive(false);
        _currentNpc = null;
    }

    public void AppendLog(string sender, string text, string color = null)
    {
        if (logContent == null || logEntryPrefab == null) return;

        var entry = Instantiate(logEntryPrefab, logContent);
        entry.text = $"[{sender}] {text}";

        if (!string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString(color, out var c))
            entry.color = c;
        else
            entry.color = sender == "You" ? new Color(0.8f, 0.9f, 1f) : Color.white;

        entry.gameObject.SetActive(true);
        _logEntries.Add(entry.gameObject);

        while (_logEntries.Count > MaxLogEntries)
        {
            Destroy(_logEntries[0]);
            _logEntries.RemoveAt(0);
        }

        Canvas.ForceUpdateCanvases();
        if (logScrollRect != null)
            logScrollRect.verticalNormalizedPosition = 0f;
    }

    public void ShowOptions(string[] options)
    {
        ClearOptions();
        if (optionsContainer == null || optionButtonPrefab == null) return;

        foreach (string option in options)
        {
            var btn = Instantiate(optionButtonPrefab, optionsContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = option;

            string captured = option;
            btn.onClick.AddListener(() =>
            {
                ClearOptions();
                OnPlayerResponse?.Invoke(captured);
            });
            btn.gameObject.SetActive(true);
            _optionButtons.Add(btn);
        }
    }

    public void SetActionButtons(string[] actions)
    {
        ClearActionButtons();
        if (actionButtonsContainer == null || actionButtonPrefab == null) return;

        foreach (string action in actions)
        {
            var btn = Instantiate(actionButtonPrefab, actionButtonsContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = action;

            string captured = action;
            btn.onClick.AddListener(() => OnAction?.Invoke(captured));
            btn.gameObject.SetActive(true);
            _actionButtons.Add(btn);
        }
    }

    public void ShowLoading(bool show)
    {
        if (loadingPanel == null) return;

        if (show)
        {
            loadingPanel.SetActive(true);
            _loadingCoroutine = StartCoroutine(LoadingAnimation());
        }
        else
        {
            if (_loadingCoroutine != null) { StopCoroutine(_loadingCoroutine); _loadingCoroutine = null; }
            loadingPanel.SetActive(false);
        }
    }

    IEnumerator LoadingAnimation()
    {
        int index = 0;
        var cg = loadingPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = loadingPanel.AddComponent<CanvasGroup>();

        while (true)
        {
            if (loadingText != null)
                loadingText.text = ThinkingPhrases[index % ThinkingPhrases.Length];
            index++;

            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI));
                cg.alpha = alpha;
                yield return null;
            }
        }
    }

    public void ShowQuestProposal(QuestDef quest, QuestReward scaledRewards)
    {
        if (questProposalPanel == null) return;
        questProposalPanel.SetActive(true);

        if (questProposalTitle != null) questProposalTitle.text = quest.title;
        if (questProposalDesc != null) questProposalDesc.text = quest.description;

        if (questProposalRequirements != null)
        {
            var lines = new List<string>();
            if (quest.requirements != null)
            {
                foreach (var req in quest.requirements)
                    lines.Add($"  {req.itemId} x{req.count}");
            }
            questProposalRequirements.text = lines.Count > 0
                ? string.Join("\n", lines) : "None";
        }

        if (questProposalRewards != null)
        {
            var lines = new List<string>();
            if (scaledRewards.gold > 0) lines.Add($"  Gold: {scaledRewards.gold}");
            if (scaledRewards.xp > 0) lines.Add($"  XP: {scaledRewards.xp}");
            if (scaledRewards.items != null)
            {
                foreach (var item in scaledRewards.items)
                    lines.Add($"  {item.itemId} x{item.count}");
            }
            questProposalRewards.text = string.Join("\n", lines);
        }

        string questId = quest.id;
        if (questAcceptButton != null)
        {
            questAcceptButton.onClick.RemoveAllListeners();
            questAcceptButton.onClick.AddListener(() =>
            {
                questProposalPanel.SetActive(false);
                OnAcceptQuest?.Invoke(questId);
            });
        }
        if (questRejectButton != null)
        {
            questRejectButton.onClick.RemoveAllListeners();
            questRejectButton.onClick.AddListener(() => questProposalPanel.SetActive(false));
        }
    }

    public void SetQuestTitle(string title)
    {
        if (questTitleText != null)
            questTitleText.text = title ?? "";
    }

    public void ClearLog()
    {
        foreach (var entry in _logEntries)
            Destroy(entry);
        _logEntries.Clear();
    }

    void ClearOptions()
    {
        foreach (var btn in _optionButtons)
            Destroy(btn.gameObject);
        _optionButtons.Clear();
    }

    void ClearActionButtons()
    {
        foreach (var btn in _actionButtons)
            Destroy(btn.gameObject);
        _actionButtons.Clear();
    }

    void ToggleFreeInput()
    {
        if (freeInputField == null) return;
        bool show = !freeInputField.gameObject.activeSelf;
        freeInputField.gameObject.SetActive(show);
        if (show)
        {
            freeInputField.text = "";
            freeInputField.ActivateInputField();
        }
    }

    void SendFreeInput()
    {
        if (freeInputField == null) return;
        string text = freeInputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        freeInputField.text = "";
        freeInputField.gameObject.SetActive(false);
        OnPlayerResponse?.Invoke(text);
    }
}
