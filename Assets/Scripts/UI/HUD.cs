using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct StatusIconEntry
{
    public string type;
    public Sprite icon;
}

public class HUD : MonoBehaviour
{
    [Header("Health/Mana")]
    [SerializeField] Image hpFill;
    [SerializeField] Image mpFill;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI mpText;

    [Header("XP Bar")]
    [SerializeField] Image xpFill;
    [SerializeField] TextMeshProUGUI levelText;

    [Header("Dodge")]
    [SerializeField] Image dodgeFill;

    [Header("Skill Bar")]
    [SerializeField] Image[] skillIcons;
    [SerializeField] Image[] skillCooldownOverlays;
    [SerializeField] TextMeshProUGUI[] skillKeyLabels;
    [SerializeField] TextMeshProUGUI[] skillBuffTexts;

    [Header("Potion Slots")]
    [SerializeField] TextMeshProUGUI hpPotionCount;
    [SerializeField] TextMeshProUGUI mpPotionCount;

    [Header("Info")]
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI regionText;
    [SerializeField] TextMeshProUGUI statPointsText;

    [Header("Boss Bar")]
    [SerializeField] GameObject bossBarRoot;
    [SerializeField] Image bossFill;
    [SerializeField] TextMeshProUGUI bossNameText;
    [SerializeField] TextMeshProUGUI bossHpText;

    [Header("Minimap")]
    [SerializeField] RawImage minimapImage;

    [Header("History Log")]
    [SerializeField] GameObject historyRoot;
    [SerializeField] Transform historyContent;
    [SerializeField] TextMeshProUGUI historyEntryPrefab;
    [SerializeField] Button historyToggleButton;

    [Header("Save Indicator")]
    [SerializeField] CanvasGroup saveIndicator;

    [Header("Skill Tooltip")]
    [SerializeField] GameObject skillTooltipPanel;
    [SerializeField] TextMeshProUGUI skillTooltipName;
    [SerializeField] TextMeshProUGUI skillTooltipDesc;
    [SerializeField] TextMeshProUGUI skillTooltipStats;

    [Header("Status Effects")]
    [SerializeField] Transform effectIconContainer;
    [SerializeField] GameObject effectIconPrefab;
    [SerializeField] StatusIconEntry[] statusIcons;

    [Header("Quest Tracker")]
    [SerializeField] GameObject questTrackerRoot;
    [SerializeField] Transform questTrackerContent;
    [SerializeField] TextMeshProUGUI questTrackerEntryPrefab;

    [Header("Region Announce")]
    [SerializeField] TextMeshProUGUI regionAnnounceText;
    [SerializeField] CanvasGroup regionAnnounceGroup;

    static readonly Color HpColor = new(1f, 0.267f, 0.267f);
    static readonly Color MpColor = new(0.267f, 0.533f, 1f);

    const int MaxHistoryEntries = 8;
    const int MaxEffectIcons = 8;
    const int MaxTrackedQuests = 3;
    readonly List<TextMeshProUGUI> _questTrackerEntries = new();
    bool _questTrackerVisible = true;
    static readonly WaitForSeconds RegionAnnounceHold = new(2f);
    readonly System.Text.StringBuilder _questSb = new();
    const float BarLerpSpeed = 8f;
    float _targetHpFill, _targetMpFill, _targetXpFill;
    readonly List<TextMeshProUGUI> _historyEntries = new();
    readonly List<GameObject> _effectIcons = new();
    readonly List<TextMeshProUGUI> _effectTimerTexts = new();
    readonly List<Image> _effectFillImages = new();
    readonly List<Image> _effectIconImages = new();
    Dictionary<string, Sprite> _statusIconMap;
    bool _historyVisible = true;
    PlayerController _cachedPlayer;
    int _hoveredSkillSlot = -1;
    int _potionThrottle;
    int _questThrottle;

    void Awake()
    {
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (saveIndicator != null) saveIndicator.alpha = 0f;
        if (historyToggleButton != null)
            historyToggleButton.onClick.AddListener(ToggleHistory);

        if (hpFill != null) hpFill.color = HpColor;
        if (mpFill != null) mpFill.color = MpColor;

        StyleBars();

        if (skillCooldownOverlays != null)
        {
            for (int i = 0; i < skillCooldownOverlays.Length; i++)
            {
                if (skillCooldownOverlays[i] != null)
                    skillCooldownOverlays[i].fillAmount = 0f;
            }
        }

        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);

        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] == null) continue;
            int slot = i;
            var trigger = skillIcons[i].gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = skillIcons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowSkillTooltip(slot));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HideSkillTooltip());
            trigger.triggers.Add(exitEntry);
        }
    }

    void Update()
    {
        UpdateCooldowns();
        UpdateBuffDurations();
        UpdateEffectIcons();
        UpdateDodgeFromPlayer();
        LerpBars();

        // Throttle infrequent updates (every 15 frames ≈ 4x/sec)
        if (++_potionThrottle >= 15) { _potionThrottle = 0; UpdatePotionsFromInventory(); }
        if (++_questThrottle >= 30) { _questThrottle = 0; UpdateQuestTracker(); }

        if (Input.GetKeyDown(KeyCode.V))
        {
            _questTrackerVisible = !_questTrackerVisible;
            if (questTrackerRoot != null) questTrackerRoot.SetActive(_questTrackerVisible);
        }
    }

    void StyleBars()
    {
        ApplyBarSprite(hpFill, "Sprites/UI/hp_bar_fill");
        ApplyBarSprite(mpFill, "Sprites/UI/mp_bar_fill");
        ApplyBarSprite(xpFill, "Sprites/UI/xp_bar_fill");
        ApplyBarSprite(dodgeFill, "Sprites/UI/dodge_bar_fill");

        var frameSpr = Resources.Load<Sprite>("Sprites/UI/bar_frame");
        var bgSpr = Resources.Load<Sprite>("Sprites/UI/bar_bg");

        ApplyBarFrame(hpFill, frameSpr, bgSpr);
        ApplyBarFrame(mpFill, frameSpr, bgSpr);
        ApplyBarFrame(xpFill, frameSpr, bgSpr);
        ApplyBarFrame(dodgeFill, frameSpr, bgSpr);
    }

    static void ApplyBarSprite(Image fill, string path)
    {
        if (fill == null) return;
        var spr = Resources.Load<Sprite>(path);
        if (spr == null) return;
        fill.sprite = spr;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
    }

    static void ApplyBarFrame(Image fill, Sprite frameSpr, Sprite bgSpr)
    {
        if (fill == null || fill.transform.parent == null) return;
        var parent = fill.transform.parent;

        // Apply bg sprite to parent's Image if exists
        if (bgSpr != null)
        {
            var bg = parent.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = bgSpr;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.12f, 0.12f, 0.15f);
            }
        }

        // Add frame overlay if not already present
        if (frameSpr != null && parent.Find("BarFrame") == null)
        {
            var frameGo = new GameObject("BarFrame");
            frameGo.transform.SetParent(parent, false);
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.sprite = frameSpr;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = new Color(0.7f, 0.65f, 0.5f);
            frameImg.raycastTarget = false;
            var rt = frameGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-2, -2);
            rt.offsetMax = new Vector2(2, 2);
        }
    }

    void LerpBars()
    {
        float dt = Time.deltaTime * BarLerpSpeed;
        if (hpFill != null)
            hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, _targetHpFill, dt);
        if (mpFill != null)
            mpFill.fillAmount = Mathf.Lerp(mpFill.fillAmount, _targetMpFill, dt);
        if (xpFill != null)
            xpFill.fillAmount = Mathf.Lerp(xpFill.fillAmount, _targetXpFill, dt);
    }

    void UpdateCooldowns()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Skills == null) return;

        float nowMs = Time.time * 1000f;
        float[] fractions = gm.Skills.GetCooldowns(nowMs);

        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            float f = i < fractions.Length ? fractions[i] : 0f;
            if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
                skillCooldownOverlays[i].fillAmount = f;
        }
    }

    void UpdateBuffDurations()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Skills == null || gm.PlayerEffects == null || gm.Data == null) return;

        float nowMs = Time.time * 1000f;

        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            string skillId = gm.Skills.GetEquippedSkill(i);
            if (string.IsNullOrEmpty(skillId) || !gm.Data.Skills.TryGetValue(skillId, out var def)
                || string.IsNullOrEmpty(def.buffType))
            {
                SetBuffDuration(i, 0, null);
                continue;
            }

            if (gm.PlayerEffects.Has(def.buffType))
            {
                float expiresAt = gm.PlayerEffects.GetExpiresAt(def.buffType);
                float remaining = expiresAt - nowMs;
                SetBuffDuration(i, remaining > 0 ? remaining : 0, def.buffType);
            }
            else
            {
                SetBuffDuration(i, 0, null);
            }
        }
    }

    void Start()
    {
        _cachedPlayer = FindFirstObjectByType<PlayerController>();
    }

    void UpdateDodgeFromPlayer()
    {
        if (_cachedPlayer != null)
            UpdateDodgeCooldown(_cachedPlayer.GetDodgeCooldownFraction());
    }

    void UpdatePotionsFromInventory()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Inventory == null) return;

        int hpCount = gm.Inventory.GetCount("hp_potion");
        int mpCount = gm.Inventory.GetCount("mp_potion");
        UpdatePotionCounts(hpCount, mpCount);
    }

    public void UpdateBars(int hp, int maxHp, int mp, int maxMp)
    {
        _targetHpFill = Mathf.Clamp01(maxHp > 0 ? (float)hp / maxHp : 0f);
        _targetMpFill = Mathf.Clamp01(maxMp > 0 ? (float)mp / maxMp : 0f);

        // Low HP pulsing warning (below 25%)
        if (hpFill != null)
        {
            if (_targetHpFill > 0f && _targetHpFill < 0.25f)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 6f);
                hpFill.color = new Color(1f, 0.15f, 0.15f, pulse);
            }
            else
            {
                hpFill.color = HpColor;
            }
        }

        if (hpText != null) { hpText.color = Color.white; hpText.text = $"<color=#ff6666>{hp}</color><color=#aaaaaa>/{maxHp}</color>"; }
        if (mpText != null) { mpText.color = Color.white; mpText.text = $"<color=#6699ff>{mp}</color><color=#aaaaaa>/{maxMp}</color>"; }
    }

    public void UpdateXpBar(int currentXp, int totalXp)
    {
        _targetXpFill = Mathf.Clamp01(totalXp > 0 ? (float)currentXp / totalXp : 0f);
    }

    public void UpdateSkillBar(string[] equipped, float[] cooldowns)
    {
        if (_skillIconCache == null) LoadSkillIconCache();

        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            bool hasSkill = i < equipped.Length && !string.IsNullOrEmpty(equipped[i]);
            if (i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].enabled = hasSkill;
                if (hasSkill && _skillIconCache != null)
                {
                    string skillId = equipped[i];
                    if (!_skillIconCache.TryGetValue($"skill_icons_{skillId}", out var spr))
                        _skillIconCache.TryGetValue(skillId, out spr);
                    if (spr != null)
                    {
                        skillIcons[i].sprite = spr;
                        skillIcons[i].color = Color.white;
                    }
                }
            }
            if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
                skillCooldownOverlays[i].fillAmount = 0f;
        }
    }

    Dictionary<string, Sprite> _skillIconCache;

    void LoadSkillIconCache()
    {
        _skillIconCache = new Dictionary<string, Sprite>();
        var sprites = Resources.LoadAll<Sprite>("Skills/skill_icons");
        if (sprites == null) return;
        foreach (var s in sprites)
            _skillIconCache[s.name] = s;
    }

    public void UpdateGold(int amount)
    {
        if (goldText != null)
        {
            goldText.color = Color.white;
            goldText.text = $"<color=#ffd900>\u25c6 {amount:N0}G</color>";
        }
    }

    public void UpdateRegion(string regionName)
    {
        if (regionText != null)
        {
            regionText.color = Color.white;
            regionText.text = $"<color=#aaddff>{regionName}</color>";
        }
        ShowRegionAnnounce(regionName);
    }

    void ShowRegionAnnounce(string regionName)
    {
        if (regionAnnounceText == null || regionAnnounceGroup == null) return;
        regionAnnounceText.color = Color.white;
        regionAnnounceText.text = $"<color=#ffd900>\u2014</color> <b>{regionName}</b> <color=#ffd900>\u2014</color>";
        StopCoroutine(nameof(RegionAnnounceCoroutine));
        StartCoroutine(RegionAnnounceCoroutine());
    }

    System.Collections.IEnumerator RegionAnnounceCoroutine()
    {
        regionAnnounceGroup.alpha = 0f;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            regionAnnounceGroup.alpha = t / 0.5f;
            yield return null;
        }
        regionAnnounceGroup.alpha = 1f;
        yield return RegionAnnounceHold;
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            regionAnnounceGroup.alpha = 1f - t;
            yield return null;
        }
        regionAnnounceGroup.alpha = 0f;
    }

    public void UpdateLevel(int level, int skillPoints, int statPoints)
    {
        if (levelText != null) { levelText.color = Color.white; levelText.text = $"<color=#99ff99>Lv.{level}</color>"; }
        if (statPointsText != null)
        {
            string sp = skillPoints > 0 ? $"<color=#88aaff>SK:{skillPoints}</color>" : "";
            string st = statPoints > 0 ? $"<color=#ffdd44>SP:{statPoints}</color>" : "";
            statPointsText.color = Color.white;
            statPointsText.text = $"{sp} {st}".Trim();
        }
    }

    public void UpdateDodgeCooldown(float fraction)
    {
        if (dodgeFill != null)
            dodgeFill.fillAmount = fraction;
    }

    public void UpdatePotionCounts(int hpCount, int mpCount)
    {
        if (hpPotionCount != null)
        {
            hpPotionCount.color = Color.white;
            hpPotionCount.text = hpCount <= 0 ? "" : hpCount <= 2
                ? $"<color=#ff4444>{hpCount}</color>"
                : $"<color=#ff8888>{hpCount}</color>";
        }
        if (mpPotionCount != null)
        {
            mpPotionCount.color = Color.white;
            mpPotionCount.text = mpCount <= 0 ? "" : mpCount <= 2
                ? $"<color=#6666ff>{mpCount}</color>"
                : $"<color=#8899ff>{mpCount}</color>";
        }
    }

    public void SetSkillCooldown(int slotIndex, float remainingMs, float cooldownMs)
    {
        if (slotIndex < 0 || slotIndex >= skillCooldownOverlays.Length) return;
        if (skillCooldownOverlays[slotIndex] == null) return;

        float fraction = cooldownMs > 0 ? Mathf.Clamp01(remainingMs / cooldownMs) : 0f;
        skillCooldownOverlays[slotIndex].fillAmount = fraction;
    }

    public void SetBuffDuration(int slotIndex, float durationMs, string buffName)
    {
        if (slotIndex < 0 || slotIndex >= skillBuffTexts.Length) return;
        if (skillBuffTexts[slotIndex] == null) return;

        if (durationMs > 0)
        {
            float seconds = durationMs / 1000f;
            skillBuffTexts[slotIndex].color = Color.white;
            skillBuffTexts[slotIndex].text = $"<color=#ffdd44>{seconds:F0}s</color>";
            skillBuffTexts[slotIndex].gameObject.SetActive(true);
        }
        else
        {
            skillBuffTexts[slotIndex].gameObject.SetActive(false);
        }
    }

    public void ShowBossBar(string bossName, int health, int maxHealth)
    {
        if (bossBarRoot == null) return;
        bossBarRoot.SetActive(true);
        if (bossNameText != null) { bossNameText.color = Color.white; bossNameText.text = $"<b><color=#ff4444>{bossName}</color></b>"; }
        if (bossHpText != null) { bossHpText.color = Color.white; bossHpText.text = $"<color=#ff6666>{health}</color><color=#888888>/{maxHealth}</color>"; }
        if (bossFill != null)
            bossFill.fillAmount = maxHealth > 0 ? (float)health / maxHealth : 0f;
    }

    public void HideBossBar()
    {
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
    }

    public void AddHistoryEntry(string text, Color color)
    {
        if (historyContent == null || historyEntryPrefab == null) return;

        var entry = Instantiate(historyEntryPrefab, historyContent);
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        entry.text = $"<color=#{colorHex}>{text}</color>";
        entry.color = Color.white;
        var fitter = entry.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = entry.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        entry.gameObject.SetActive(true);
        _historyEntries.Add(entry);

        StartCoroutine(FadeInEntry(entry));

        while (_historyEntries.Count > MaxHistoryEntries)
        {
            var old = _historyEntries[0];
            _historyEntries.RemoveAt(0);
            Destroy(old.gameObject);
        }
    }

    IEnumerator FadeInEntry(TextMeshProUGUI entry)
    {
        var cg = entry.GetComponent<CanvasGroup>();
        if (cg == null) cg = entry.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / 0.3f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void ToggleHistory()
    {
        _historyVisible = !_historyVisible;
        if (historyRoot != null) historyRoot.SetActive(_historyVisible);
    }

    public void ToggleMinimap()
    {
        if (minimapImage != null)
            minimapImage.gameObject.SetActive(!minimapImage.gameObject.activeSelf);
    }

    public void ShowSaveIndicator()
    {
        if (saveIndicator == null) return;
        StopCoroutine(nameof(SaveIndicatorRoutine));
        StartCoroutine(SaveIndicatorRoutine());
    }

    IEnumerator SaveIndicatorRoutine()
    {
        saveIndicator.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.5f);
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            saveIndicator.alpha = 1f - Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }
        saveIndicator.alpha = 0f;
    }

    void UpdateQuestTracker()
    {
        if (questTrackerContent == null || questTrackerEntryPrefab == null) return;
        if (!_questTrackerVisible) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.Quests == null || gm.Inventory == null) return;

        var quests = gm.Quests.GetActiveQuests();
        int count = Mathf.Min(quests.Length, MaxTrackedQuests);

        while (_questTrackerEntries.Count < count)
        {
            var entry = Instantiate(questTrackerEntryPrefab, questTrackerContent);
            var fitter = entry.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = entry.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            entry.color = Color.white;
            entry.overflowMode = TMPro.TextOverflowModes.Overflow;
            entry.enableWordWrapping = true;
            entry.gameObject.SetActive(true);
            _questTrackerEntries.Add(entry);
        }

        for (int i = 0; i < _questTrackerEntries.Count; i++)
        {
            if (i < count)
            {
                _questTrackerEntries[i].gameObject.SetActive(true);
                var q = quests[i];
                _questSb.Clear();
                _questSb.AppendLine($"<b><color=#ffee88>{q.title}</color></b>");

                if (q.requirements != null)
                {
                    foreach (var r in q.requirements)
                    {
                        int have = gm.Inventory.GetCount(r.itemId);
                        bool done = have >= r.count;
                        string itemName = gm.Data?.Items != null && gm.Data.Items.TryGetValue(r.itemId, out var iDef)
                            ? iDef.name : r.itemId;
                        string color = done ? "#88ff88" : have > 0 ? "#ffcc44" : "#aaaaaa";
                        string check = done ? " <color=#66ff66>\u2713</color>" : "";
                        _questSb.AppendLine($"  <color={color}>{itemName}: {Mathf.Min(have, r.count)}/{r.count}{check}</color>");
                    }
                }
                if (q.killRequirements != null)
                {
                    foreach (var kr in q.killRequirements)
                    {
                        int kills = gm.Quests.GetKillProgress(q.id, kr.monsterId);
                        bool done = kills >= kr.count;
                        string color = done ? "#88ff88" : kills > 0 ? "#ffcc44" : "#aaaaaa";
                        string check = done ? " <color=#66ff66>\u2713</color>" : "";
                        _questSb.AppendLine($"  <color={color}><color=#ffbb77>{kr.monsterId}</color>: {Mathf.Min(kills, kr.count)}/{kr.count}{check}</color>");
                    }
                }
                _questTrackerEntries[i].text = _questSb.ToString().TrimEnd();
            }
            else
            {
                _questTrackerEntries[i].gameObject.SetActive(false);
            }
        }

        if (questTrackerRoot != null)
            questTrackerRoot.SetActive(count > 0 && _questTrackerVisible);
    }

    void BuildStatusIconMap()
    {
        _statusIconMap = new Dictionary<string, Sprite>();
        if (statusIcons == null) return;
        foreach (var entry in statusIcons)
            if (entry.icon != null) _statusIconMap[entry.type] = entry.icon;
    }

    void UpdateEffectIcons()
    {
        if (effectIconContainer == null || effectIconPrefab == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.PlayerEffects == null) return;

        if (_statusIconMap == null) BuildStatusIconMap();

        float nowMs = Time.time * 1000f;
        var active = gm.PlayerEffects.GetActive(nowMs);

        int count = Mathf.Min(active.Count, MaxEffectIcons);
        while (_effectIcons.Count < count)
        {
            var icon = Instantiate(effectIconPrefab, effectIconContainer);
            _effectIcons.Add(icon);
            _effectIconImages.Add(icon.GetComponent<Image>());
            _effectTimerTexts.Add(icon.GetComponentInChildren<TextMeshProUGUI>());
            _effectFillImages.Add(icon.transform.childCount > 1
                ? icon.transform.GetChild(1).GetComponent<Image>() : null);
        }

        for (int i = 0; i < _effectIcons.Count; i++)
        {
            if (i < count)
            {
                _effectIcons[i].SetActive(true);
                var info = active[i];
                if (_effectIconImages.Count > i && _effectIconImages[i] != null
                    && _statusIconMap.TryGetValue(info.type, out var spr))
                {
                    _effectIconImages[i].sprite = spr;
                    _effectIconImages[i].color = Color.white;
                }
                float remaining = (info.expires - nowMs) / 1000f;
                if (_effectTimerTexts[i] != null)
                {
                    string timerColor = remaining <= 2f ? "#ff6666" : remaining <= 5f ? "#ffdd44" : "#aaddff";
                    _effectTimerTexts[i].color = Color.white;
                    _effectTimerTexts[i].text = $"<color={timerColor}>{remaining:F0}s</color>";
                }
                if (_effectFillImages[i] != null && info.totalDuration > 0)
                    _effectFillImages[i].fillAmount = Mathf.Clamp01((info.expires - nowMs) / info.totalDuration);
            }
            else
            {
                _effectIcons[i].SetActive(false);
            }
        }
    }

    void ShowSkillTooltip(int slotIndex)
    {
        _hoveredSkillSlot = slotIndex;
        if (skillTooltipPanel == null) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.Skills == null || gm.Data == null) return;

        string[] equipped = gm.Skills.GetEquippedSkills();
        string skillId = slotIndex < equipped.Length ? equipped[slotIndex] : null;
        if (string.IsNullOrEmpty(skillId) || !gm.Data.Skills.TryGetValue(skillId, out var def))
        {
            skillTooltipPanel.SetActive(false);
            return;
        }

        skillTooltipPanel.SetActive(true);

        if (skillTooltipName != null)
        {
            Color treeColor = def.tree switch
            {
                "ranged" => new Color(0.3f, 0.9f, 0.3f),
                "magic"  => new Color(0.4f, 0.6f, 1f),
                _        => new Color(0.95f, 0.45f, 0.45f)
            };
            string treeHex = "#" + ColorUtility.ToHtmlStringRGB(treeColor);
            skillTooltipName.color = Color.white;
            skillTooltipName.fontStyle = TMPro.FontStyles.Normal;
            skillTooltipName.text = $"<b><color={treeHex}>{def.name}</color></b>";
        }
        if (skillTooltipDesc != null)
        {
            skillTooltipDesc.color = Color.white;
            skillTooltipDesc.text = $"<color=#bbbbbb>{def.description ?? ""}</color>";
        }

        if (skillTooltipStats != null)
        {
            skillTooltipStats.color = Color.white;
            var lines = new List<string>();
            int level = gm.Skills.GetSkillLevel(skillId);
            float dmgMult = gm.Skills.GetDamageMultiplier(skillId);
            lines.Add($"<color=#aaffaa>Level: {level}/{GameConfig.SkillMaxLevel}</color>");
            if (def.damage > 0) lines.Add($"<color=#ffaa55>Damage: {def.damage * dmgMult:F0}</color>");
            if (def.range > 0) lines.Add($"<color=#aaddff>Range: {def.range:F0}</color>");
            if (def.mpCost > 0) lines.Add($"<color=#6688ff>MP Cost: {def.mpCost}</color>");
            if (def.cooldown > 0) lines.Add($"<color=#cccccc>Cooldown: {def.cooldown / 1000f:F1}s</color>");
            if (def.aoe > 0) lines.Add($"<color=#ffcc44>AoE: {def.aoe:F0}</color>");
            skillTooltipStats.text = string.Join("  ", lines);
        }

        if (slotIndex < skillIcons.Length && skillIcons[slotIndex] != null)
        {
            var rt = skillTooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                float scale = Screen.height / 1080f;
                rt.position = skillIcons[slotIndex].transform.position + new Vector3(0, 60f * scale, 0);
            }
        }
    }

    void HideSkillTooltip()
    {
        _hoveredSkillSlot = -1;
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }
}
