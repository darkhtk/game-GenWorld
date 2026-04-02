using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    public Action<string> OnLearnSkill;
    public Action<string, int> OnEquipSkill;

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Skill Point Display")]
    [SerializeField] TextMeshProUGUI skillPointsText;
    [SerializeField] TextMeshProUGUI playerLevelText;
    [SerializeField] Button resetButton;
    public Action OnResetSkills;

    [Header("Tree Columns")]
    [SerializeField] Transform meleeColumn;
    [SerializeField] Transform rangedColumn;
    [SerializeField] Transform magicColumn;

    [Header("Skill Row Prefab")]
    [SerializeField] GameObject skillRowPrefab;

    [Header("Equip Bar")]
    [SerializeField] Button[] equipSlotButtons;
    [SerializeField] TextMeshProUGUI[] equipSlotLabels;

    static readonly Color MeleeColor = new(0.9f, 0.3f, 0.3f);
    static readonly Color RangedColor = new(0.3f, 0.9f, 0.3f);
    static readonly Color MagicColor = new(0.3f, 0.5f, 1f);

    static readonly Color MaxedBg = new(0.1f, 0.3f, 0.1f);
    static readonly Color LearnedBg = new(0.1f, 0.15f, 0.3f);
    static readonly Color LearnableBg = new(0.25f, 0.2f, 0.15f);
    static readonly Color LockedBg = new(0.15f, 0.15f, 0.15f);

    readonly Dictionary<string, SkillRowUI> _rows = new();
    string _selectedSkillId;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (resetButton != null) resetButton.onClick.AddListener(() => OnResetSkills?.Invoke());

        for (int i = 0; i < equipSlotButtons.Length; i++)
        {
            int slot = i;
            if (equipSlotButtons[i] != null)
                equipSlotButtons[i].onClick.AddListener(() => TryEquipToSlot(slot));
        }
    }

    public void Init(SkillDef[] skillDefs)
    {
        ClearRows();

        foreach (var def in skillDefs)
        {
            Transform parent = def.TreeEnum switch
            {
                SkillTree.Ranged => rangedColumn,
                SkillTree.Magic => magicColumn,
                _ => meleeColumn
            };

            if (parent == null || skillRowPrefab == null) continue;

            var go = Instantiate(skillRowPrefab, parent);
            var row = go.GetComponent<SkillRowUI>();
            if (row == null) row = go.AddComponent<SkillRowUI>();

            Color treeColor = def.TreeEnum switch
            {
                SkillTree.Ranged => RangedColor,
                SkillTree.Magic => MagicColor,
                _ => MeleeColor
            };

            row.Setup(def, treeColor);
            row.OnLearnClicked = id => OnLearnSkill?.Invoke(id);
            row.OnSelected = id => _selectedSkillId = id;
            _rows[def.id] = row;
        }
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }
    public void Toggle() { if (IsOpen) Hide(); else Show(); }

    public void Refresh(SkillSystem skillSystem, SkillDef[] skillDefs, int skillPoints, int playerLevel)
    {
        if (skillSystem == null || skillDefs == null) return;
        if (skillPointsText != null) skillPointsText.text = $"Skill Points: {skillPoints}";
        if (playerLevelText != null) playerLevelText.text = $"Lv.{playerLevel}";

        foreach (var def in skillDefs)
        {
            if (!_rows.TryGetValue(def.id, out var row)) continue;

            int level = skillSystem.GetSkillLevel(def.id);
            bool isMaxed = level >= GameConfig.SkillMaxLevel;
            bool isLearned = level > 0;
            bool canLearn = !isMaxed && skillPoints >= def.requiredPoints && playerLevel >= def.requiredLevel;

            row.UpdateState(level, isMaxed, isLearned, canLearn, def.requiredPoints, def.requiredLevel);
        }

        RefreshEquipBar(skillSystem, skillDefs);
    }

    void RefreshEquipBar(SkillSystem skillSystem, SkillDef[] skillDefs)
    {
        string[] equipped = skillSystem.GetEquippedSkills();
        var defMap = new Dictionary<string, SkillDef>();
        foreach (var d in skillDefs) defMap[d.id] = d;

        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            if (i >= equipSlotLabels.Length || equipSlotLabels[i] == null) continue;

            string skillId = i < equipped.Length ? equipped[i] : null;
            if (!string.IsNullOrEmpty(skillId) && defMap.TryGetValue(skillId, out var def))
            {
                equipSlotLabels[i].text = def.name;
                equipSlotLabels[i].color = def.TreeEnum switch
                {
                    SkillTree.Ranged => RangedColor,
                    SkillTree.Magic => MagicColor,
                    _ => MeleeColor
                };
            }
            else
            {
                equipSlotLabels[i].text = $"{i + 1}";
                equipSlotLabels[i].color = Color.gray;
            }
        }
    }

    void TryEquipToSlot(int slot)
    {
        if (!string.IsNullOrEmpty(_selectedSkillId))
            OnEquipSkill?.Invoke(_selectedSkillId, slot);
    }

    void ClearRows()
    {
        foreach (var kv in _rows)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _rows.Clear();
    }
}

public class SkillRowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] Button learnButton;
    [SerializeField] Button selectButton;
    [SerializeField] Image backgroundImage;

    static readonly Color MaxedBg = new(0.1f, 0.3f, 0.1f);
    static readonly Color LearnedBg = new(0.1f, 0.15f, 0.3f);
    static readonly Color LearnableBg = new(0.25f, 0.2f, 0.15f);
    static readonly Color LockedBg = new(0.15f, 0.15f, 0.15f);

    public Action<string> OnLearnClicked;
    public Action<string> OnSelected;

    string _skillId;

    public void Setup(SkillDef def, Color treeColor)
    {
        _skillId = def.id;
        if (nameText != null) { nameText.text = def.name; nameText.color = treeColor; }
        if (descText != null) descText.text = def.description ?? "";

        if (learnButton != null)
            learnButton.onClick.AddListener(() => OnLearnClicked?.Invoke(_skillId));

        if (selectButton != null)
            selectButton.onClick.AddListener(() => OnSelected?.Invoke(_skillId));
    }

    public void UpdateState(int level, bool isMaxed, bool isLearned, bool canLearn,
        int requiredPoints, int requiredLevel)
    {
        if (levelText != null)
        {
            levelText.text = isMaxed ? "MAX" : $"Lv.{level}/{GameConfig.SkillMaxLevel}";
            levelText.color = isMaxed ? new Color(1f, 0.84f, 0f) : Color.white;
        }

        if (costText != null)
        {
            if (isMaxed) costText.text = "";
            else if (canLearn) { costText.text = $"{requiredPoints}pt"; costText.color = Color.yellow; }
            else { costText.text = $"Lv.{requiredLevel}"; costText.color = Color.gray; }
        }

        if (learnButton != null)
        {
            learnButton.gameObject.SetActive(canLearn);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isMaxed ? MaxedBg
                : isLearned ? LearnedBg
                : canLearn ? LearnableBg
                : LockedBg;
        }
    }
}
