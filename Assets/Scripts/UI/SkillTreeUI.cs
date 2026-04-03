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

    readonly Dictionary<string, SkillRowUI> _rows = new();
    static Dictionary<string, Sprite> _skillIconCache;
    string _selectedSkillId;

    static void EnsureIconCache()
    {
        if (_skillIconCache != null) return;
        _skillIconCache = new Dictionary<string, Sprite>();
        var sprites = Resources.LoadAll<Sprite>("Skills/skill_icons");
        if (sprites == null || sprites.Length == 0) return;
        foreach (var s in sprites)
            _skillIconCache[s.name] = s;
    }

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
        EnsureIconCache();

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

            _skillIconCache.TryGetValue($"skill_icons_{def.id}", out var icon);
            if (icon == null) _skillIconCache.TryGetValue(def.id, out icon);
            row.Setup(def, treeColor, icon);
            row.OnLearnClicked = id => OnLearnSkill?.Invoke(id);
            row.OnSelected = id => _selectedSkillId = id;
            _rows[def.id] = row;
        }
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
        if (_rows.Count == 0) Init(gm.Data.SkillList);
        Refresh(gm.Skills, gm.Data.SkillList, gm.PlayerState.SkillPoints, gm.PlayerState.Level);
    }

    public void Refresh(SkillSystem skillSystem, SkillDef[] skillDefs, int skillPoints, int playerLevel)
    {
        if (skillSystem == null || skillDefs == null) return;
        if (skillPointsText != null)
        {
            skillPointsText.text = $"Skill Points: <color=#ffd900>{skillPoints}</color>";
            skillPointsText.color = skillPoints > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
        if (playerLevelText != null) { playerLevelText.text = $"Lv.{playerLevel}"; playerLevelText.color = new Color(0.6f, 1f, 0.6f); }

        foreach (var def in skillDefs)
        {
            if (!_rows.TryGetValue(def.id, out var row)) continue;

            int level = skillSystem.GetSkillLevel(def.id);
            bool isMaxed = level >= GameConfig.SkillMaxLevel;
            bool isLearned = level > 0;
            bool canLearn = !isMaxed && skillPoints >= def.requiredPoints && playerLevel >= def.requiredLevel;

            row.UpdateState(level, isMaxed, isLearned, canLearn, def.requiredPoints, def.requiredLevel, playerLevel, skillPoints);
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
