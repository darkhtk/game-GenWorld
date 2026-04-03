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
        if (panel != null && panel.activeSelf) Hide();

        _currentNpc = npcDef;
        gameObject.SetActive(true);
        if (panel != null) panel.SetActive(true);

        if (npcNameText != null)
        {
            string nameHex = !string.IsNullOrEmpty(npcDef.color) ? npcDef.color : "#ffe8aa";
            npcNameText.color = Color.white;
            npcNameText.text = $"<b><color={nameHex}>{npcDef.name}</color></b>";
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
        if (_loadingCoroutine != null) { StopCoroutine(_loadingCoroutine); _loadingCoroutine = null; }
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (questProposalPanel != null) questProposalPanel.SetActive(false);
        if (freeInputField != null) freeInputField.gameObject.SetActive(false);
        if (panel != null) panel.SetActive(false);
        gameObject.SetActive(false);
        _currentNpc = null;
    }

    public void AppendLog(string sender, string text, string color = null)
    {
        if (logContent == null || logEntryPrefab == null) return;

        var entry = Instantiate(logEntryPrefab, logContent);
        string senderColor = !string.IsNullOrEmpty(color) ? color
            : sender == "You" ? "#ccddff" : "#ffe8aa";
        entry.text = $"<b><color={senderColor}>{sender}:</color></b> {text}";
        entry.color = Color.white;

        entry.overflowMode = TMPro.TextOverflowModes.Overflow;
        entry.enableWordWrapping = true;
        var fitter = entry.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = entry.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
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
            if (label != null) { label.text = $"<color=#aaaaaa>\u25b8</color> {option}"; label.color = Color.white; }

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

    public void SetActionButtons(string[] actions, System.Collections.Generic.Dictionary<string, ActionDef> actionDefs = null)
    {
        ClearActionButtons();
        if (actionButtonsContainer == null || actionButtonPrefab == null) return;

        foreach (string action in actions)
        {
            var btn = Instantiate(actionButtonPrefab, actionButtonsContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string display = (actionDefs != null && actionDefs.TryGetValue(action, out var def))
                    ? def.label : action;
                label.text = $"<color=#ffcc66>\u25cf</color> {display}";
                label.color = Color.white;
            }

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
            if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
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
        float totalElapsed = 0f;
        var cg = loadingPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = loadingPanel.AddComponent<CanvasGroup>();

        while (true)
        {
            string phrase = ThinkingPhrases[index % ThinkingPhrases.Length];
            if (loadingText != null)
            {
                loadingText.color = Color.white;
                loadingText.text = totalElapsed > 10f
                    ? $"{phrase} <color=#ff9944><b>({(int)totalElapsed}s)</b></color>"
                    : phrase;
            }
            index++;

            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                totalElapsed += Time.unscaledDeltaTime;
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

        if (questProposalTitle != null) { questProposalTitle.color = Color.white; questProposalTitle.text = $"<b><color=#ffe888>\u25b8 {quest.title}</color></b>"; }
        if (questProposalDesc != null)
        {
            questProposalDesc.text = quest.description;
            questProposalDesc.color = new Color(0.78f, 0.78f, 0.78f);
            questProposalDesc.fontStyle = TMPro.FontStyles.Italic;
        }

        if (questProposalRequirements != null)
        {
            questProposalRequirements.color = Color.white;
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
            questProposalRequirements.text = lines.Count > 0
                ? string.Join("\n", lines) : "<color=#666666>None</color>";
        }

        if (questProposalRewards != null)
        {
            questProposalRewards.color = Color.white;
            var lines = new List<string>();
            if (scaledRewards.gold > 0) lines.Add($"  <color=#ffd900>\u25c6 <b>{scaledRewards.gold:N0}</b>G</color>");
            if (scaledRewards.xp > 0) lines.Add($"  <color=#aaffaa>\u25b8 <b>{scaledRewards.xp}</b> XP</color>");
            if (scaledRewards.items != null)
            {
                foreach (var item in scaledRewards.items)
                    lines.Add($"  <color=#ccddff>\u25b9 {item.itemId} \u00d7{item.count}</color>");
            }
            questProposalRewards.text = lines.Count > 0 ? string.Join("\n", lines) : "<color=#666666>None</color>";
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
        if (questTitleText == null) return;
        questTitleText.color = Color.white;
        questTitleText.text = !string.IsNullOrEmpty(title)
            ? $"<b><color=#ffe888>\u25b8 {title}</color></b>" : "";
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
