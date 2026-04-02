using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    static readonly Color HpColor = new(1f, 0.267f, 0.267f);
    static readonly Color MpColor = new(0.267f, 0.533f, 1f);

    const int MaxHistoryEntries = 8;
    readonly List<TextMeshProUGUI> _historyEntries = new();
    bool _historyVisible = true;
    PlayerController _cachedPlayer;
    int _hoveredSkillSlot = -1;

    void Awake()
    {
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (saveIndicator != null) saveIndicator.alpha = 0f;
        if (historyToggleButton != null)
            historyToggleButton.onClick.AddListener(ToggleHistory);

        if (hpFill != null) hpFill.color = HpColor;
        if (mpFill != null) mpFill.color = MpColor;

        for (int i = 0; i < skillCooldownOverlays.Length; i++)
        {
            if (skillCooldownOverlays[i] != null)
                skillCooldownOverlays[i].fillAmount = 0f;
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
        UpdateDodgeFromPlayer();
        UpdatePotionsFromInventory();
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

    void UpdateDodgeFromPlayer()
    {
        if (_cachedPlayer == null)
            _cachedPlayer = FindObjectOfType<PlayerController>();
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
        if (hpFill != null)
            hpFill.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;
        if (mpFill != null)
            mpFill.fillAmount = maxMp > 0 ? (float)mp / maxMp : 0f;
        if (hpText != null)
            hpText.text = $"{hp}/{maxHp}";
        if (mpText != null)
            mpText.text = $"{mp}/{maxMp}";
    }

    public void UpdateXpBar(int currentXp, int totalXp)
    {
        if (xpFill != null)
            xpFill.fillAmount = totalXp > 0 ? (float)currentXp / totalXp : 0f;
    }

    public void UpdateSkillBar(string[] equipped, float[] cooldowns)
    {
        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            bool hasSkill = i < equipped.Length && !string.IsNullOrEmpty(equipped[i]);
            if (i < skillIcons.Length && skillIcons[i] != null)
                skillIcons[i].enabled = hasSkill;
            if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
                skillCooldownOverlays[i].fillAmount = 0f;
        }
    }

    public void UpdateGold(int amount)
    {
        if (goldText != null)
            goldText.text = amount.ToString("N0");
    }

    public void UpdateRegion(string regionName)
    {
        if (regionText != null)
            regionText.text = regionName;
    }

    public void UpdateLevel(int level, int skillPoints, int statPoints)
    {
        if (levelText != null)
            levelText.text = $"Lv.{level}";
        if (statPointsText != null)
        {
            string sp = skillPoints > 0 ? $"SK:{skillPoints}" : "";
            string st = statPoints > 0 ? $"SP:{statPoints}" : "";
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
            hpPotionCount.text = hpCount > 0 ? hpCount.ToString() : "";
        if (mpPotionCount != null)
            mpPotionCount.text = mpCount > 0 ? mpCount.ToString() : "";
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
            skillBuffTexts[slotIndex].text = $"{seconds:F0}s";
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
        if (bossNameText != null) bossNameText.text = bossName;
        if (bossHpText != null) bossHpText.text = $"{health}/{maxHealth}";
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
        entry.text = text;
        entry.color = color;
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

        if (skillTooltipName != null) skillTooltipName.text = def.name;
        if (skillTooltipDesc != null) skillTooltipDesc.text = def.description ?? "";

        if (skillTooltipStats != null)
        {
            var lines = new List<string>();
            int level = gm.Skills.GetSkillLevel(skillId);
            float dmgMult = gm.Skills.GetDamageMultiplier(skillId);
            lines.Add($"Level: {level}/{GameConfig.SkillMaxLevel}");
            if (def.damage > 0) lines.Add($"Damage: {def.damage * dmgMult:F0}");
            if (def.range > 0) lines.Add($"Range: {def.range:F0}");
            if (def.mpCost > 0) lines.Add($"MP Cost: {def.mpCost}");
            if (def.cooldown > 0) lines.Add($"Cooldown: {def.cooldown / 1000f:F1}s");
            if (def.aoe > 0) lines.Add($"AoE: {def.aoe:F0}");
            skillTooltipStats.text = string.Join("\n", lines);
        }

        if (slotIndex < skillIcons.Length && skillIcons[slotIndex] != null)
        {
            var rt = skillTooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
                rt.position = skillIcons[slotIndex].transform.position + new Vector3(0, 60, 0);
        }
    }

    void HideSkillTooltip()
    {
        _hoveredSkillSlot = -1;
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }
}
