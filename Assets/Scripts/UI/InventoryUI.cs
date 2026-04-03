using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public Action<int> OnEquipCallback;
    public Action<string> OnUnequipCallback;
    public Action<int> OnUseItemCallback;
    public Action OnSortCallback;

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;
    [SerializeField] Button sortButton;

    [Header("Filter/Sort")]
    [SerializeField] Button[] filterButtons;
    [SerializeField] Button sortModeButton;
    [SerializeField] TextMeshProUGUI sortModeText;

    [Header("Inventory Grid")]
    [SerializeField] Transform gridContent;
    [SerializeField] GameObject slotPrefab;

    [Header("Equipment Slots")]
    [SerializeField] EquipSlotUI weaponSlot;
    [SerializeField] EquipSlotUI helmetSlot;
    [SerializeField] EquipSlotUI armorSlot;
    [SerializeField] EquipSlotUI bootsSlot;
    [SerializeField] EquipSlotUI accessorySlot;

    [Header("Stats Panel")]
    [SerializeField] TextMeshProUGUI atkText;
    [SerializeField] TextMeshProUGUI defText;
    [SerializeField] TextMeshProUGUI spdText;
    [SerializeField] TextMeshProUGUI critText;
    [SerializeField] TextMeshProUGUI hpStatText;
    [SerializeField] TextMeshProUGUI mpStatText;

    [Header("Stat Allocation")]
    [SerializeField] TextMeshProUGUI strText;
    [SerializeField] TextMeshProUGUI dexText;
    [SerializeField] TextMeshProUGUI wisText;
    [SerializeField] TextMeshProUGUI lucText;
    [SerializeField] Button strAddButton;
    [SerializeField] Button dexAddButton;
    [SerializeField] Button wisAddButton;
    [SerializeField] Button lucAddButton;
    [SerializeField] TextMeshProUGUI statPointsText;

    [Header("Info")]
    [SerializeField] TextMeshProUGUI levelGoldText;

    [Header("Tooltip")]
    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TextMeshProUGUI tooltipName;
    [SerializeField] TextMeshProUGUI tooltipDesc;
    [SerializeField] TextMeshProUGUI tooltipStats;

    [Header("Drag Visual")]
    [SerializeField] Image dragIcon;

    [Header("Equipment Compare")]
    [SerializeField] GameObject comparePanel;
    [SerializeField] TextMeshProUGUI compareCurrentName;
    [SerializeField] TextMeshProUGUI compareNewName;
    [SerializeField] TextMeshProUGUI compareCurrentStats;
    [SerializeField] TextMeshProUGUI compareNewStats;
    [SerializeField] TextMeshProUGUI compareDiffStats;
    [SerializeField] Button compareEquipButton;
    [SerializeField] Button compareCancelButton;

    const int Columns = 10;
    readonly List<InventorySlotUI> _slots = new();

    InventorySystem _inventory;
    Dictionary<string, ItemInstance> _equipment;
    Dictionary<string, ItemDef> _itemDefs;

    int _dragFromSlot = -1;
    bool _dragging;
    int _pendingEquipIndex = -1;
    string _currentFilter = "all";
    int _currentSortMode; // 0=name, 1=grade, 2=type
    static readonly string[] FilterNames = { "all", "weapon", "armor", "consumable", "material" };
    static readonly string[] SortModeNames = { "Name", "Grade", "Type", "Recent" };

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (comparePanel != null) comparePanel.SetActive(false);
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);

        if (compareEquipButton != null)
            compareEquipButton.onClick.AddListener(ConfirmEquip);
        if (compareCancelButton != null)
            compareCancelButton.onClick.AddListener(() => { if (comparePanel != null) comparePanel.SetActive(false); });
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (sortButton != null) sortButton.onClick.AddListener(() => OnSortCallback?.Invoke());

        if (filterButtons != null)
        {
            for (int i = 0; i < filterButtons.Length && i < FilterNames.Length; i++)
            {
                int idx = i;
                if (filterButtons[i] != null)
                    filterButtons[i].onClick.AddListener(() => SetFilter(idx));
            }
        }
        if (sortModeButton != null)
            sortModeButton.onClick.AddListener(CycleSortMode);

        if (strAddButton != null) strAddButton.onClick.AddListener(() => AllocateStat("str"));
        if (dexAddButton != null) dexAddButton.onClick.AddListener(() => AllocateStat("dex"));
        if (wisAddButton != null) wisAddButton.onClick.AddListener(() => AllocateStat("wis"));
        if (lucAddButton != null) lucAddButton.onClick.AddListener(() => AllocateStat("luc"));
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show()
    {
        if (IsOpen) return;
        var anim = panel != null ? panel.GetComponent<PanelAnimator>() : null;
        if (anim != null) anim.Show();
        else if (panel != null) panel.SetActive(true);
        AudioManager.Instance?.PlaySFX("sfx_menu_open");
        Refresh();
    }

    public void Hide()
    {
        var anim = panel != null ? panel.GetComponent<PanelAnimator>() : null;
        if (anim != null) anim.Hide();
        else if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_menu_close");
        HideTooltip();
        CancelDrag();
    }

    public void Toggle()
    {
        if (IsOpen) Hide(); else Show();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var stats = gm.PlayerState.CurrentStats;
        stats.hp = gm.PlayerState.Hp;
        stats.mp = gm.PlayerState.Mp;
        Refresh(gm.Inventory, gm.PlayerState.Equipment, gm.Data.Items, stats);

        if (levelGoldText != null)
            levelGoldText.text = $"<color=#99ff99>Lv.{gm.PlayerState.Level}</color>  <color=#ffd900>{gm.PlayerState.Gold:N0}G</color>";
    }

    public void Refresh(InventorySystem inventory, Dictionary<string, ItemInstance> equipment,
        Dictionary<string, ItemDef> itemDefs, Stats playerStats)
    {
        _inventory = inventory;
        _equipment = equipment;
        _itemDefs = itemDefs;

        if (_inventory == null || _itemDefs == null) return;

        RefreshGrid();
        RefreshEquipment();
        RefreshStats(playerStats);
    }

    void RefreshGrid()
    {
        EnsureSlots(_inventory.MaxSlots);
        var filtered = _inventory.GetFiltered(_itemDefs, _currentFilter, _currentSortMode);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < filtered.Length)
            {
                var item = filtered[i];
                if (_itemDefs.TryGetValue(item.itemId, out var def))
                {
                    _slots[i].SetItem(def.name, item.count, item.enhanceLevel,
                        GameConfig.GetGradeColor(def.GradeEnum), def.TypeEnum, def.icon, def.grade);
                    _slots[i].SetActive(true);
                    continue;
                }
            }
            _slots[i].Clear();
        }
    }

    void EnsureSlots(int count)
    {
        while (_slots.Count < count)
        {
            var go = Instantiate(slotPrefab, gridContent);
            var slot = go.GetComponent<InventorySlotUI>();
            if (slot == null) slot = go.AddComponent<InventorySlotUI>();
            int index = _slots.Count;
            slot.SlotIndex = index;
            slot.OnClicked = OnSlotClicked;
            slot.OnBeginDragAction = OnSlotBeginDrag;
            slot.OnEndDragAction = OnSlotEndDrag;
            slot.OnDropAction = OnSlotDrop;
            slot.OnHover = OnSlotHover;
            slot.OnHoverExit = _ => HideTooltip();
            _slots.Add(slot);
        }
    }

    void OnSlotClicked(int index)
    {
        var item = _inventory.GetSlot(index);
        if (item == null) return;

        if (_itemDefs.TryGetValue(item.itemId, out var def))
        {
            if (def.TypeEnum == ItemType.Potion)
                OnUseItemCallback?.Invoke(index);
            else if (ItemTypeUtil.IsEquipment(def.TypeEnum))
                ShowComparePopup(index, def);
        }
    }

    void OnSlotBeginDrag(int index)
    {
        var item = _inventory.GetSlot(index);
        if (item == null) return;
        _dragFromSlot = index;
        _dragging = true;
        if (dragIcon != null)
        {
            dragIcon.gameObject.SetActive(true);
            dragIcon.color = Color.white;
        }
    }

    void OnSlotEndDrag(int index)
    {
        CancelDrag();
    }

    void OnSlotDrop(int targetIndex)
    {
        if (!_dragging || _dragFromSlot < 0) return;
        if (_dragFromSlot != targetIndex)
            _inventory.SwapSlots(_dragFromSlot, targetIndex);
        CancelDrag();
        RefreshGrid();
    }

    void CancelDrag()
    {
        _dragging = false;
        _dragFromSlot = -1;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_dragging && dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    void OnSlotHover(int index)
    {
        var item = _inventory.GetSlot(index);
        if (item == null || !_itemDefs.TryGetValue(item.itemId, out var def)) return;
        ShowTooltip(def, item);
    }

    void ShowTooltip(ItemDef def, ItemInstance inst)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);

        if (tooltipName != null)
        {
            tooltipName.text = def.name;
            tooltipName.color = GameConfig.GetGradeColor(def.GradeEnum);
        }
        if (tooltipDesc != null)
            tooltipDesc.text = def.description ?? "";

        if (tooltipStats != null)
        {
            var lines = new List<string>();
            var s = def.stats;
            int enh = inst.enhanceLevel * GameConfig.EnhanceBonusPerLevel;
            if (s.atk > 0) lines.Add($"ATK +{s.atk + enh}");
            if (s.def > 0) lines.Add($"DEF +{s.def + enh}");
            if (s.maxHp > 0) lines.Add($"HP +{s.maxHp + enh}");
            if (s.maxMp > 0) lines.Add($"MP +{s.maxMp + enh}");
            if (s.spd > 0) lines.Add($"SPD +{s.spd}");
            if (s.crit > 0) lines.Add($"CRIT +{s.crit}");
            if (inst.enhanceLevel > 0) lines.Add($"Enhanced +{inst.enhanceLevel}");
            if (def.healHp > 0) lines.Add($"Heal HP {def.healHp}");
            if (def.healMp > 0) lines.Add($"Heal MP {def.healMp}");
            tooltipStats.text = string.Join("\n", lines);
        }

        var rt = tooltipPanel.GetComponent<RectTransform>();
        Vector2 pos = Input.mousePosition;
        float pad = 10f * (Screen.height / 1080f);
        float x = pos.x + pad;
        float y = pos.y - pad;
        if (rt != null)
        {
            if (x + rt.sizeDelta.x > Screen.width) x = pos.x - rt.sizeDelta.x - pad;
            if (y - rt.sizeDelta.y < 0) y = pos.y + rt.sizeDelta.y + pad;
        }
        tooltipPanel.transform.position = new Vector3(x, y, 0);
    }

    void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    void ShowComparePopup(int slotIndex, ItemDef newDef)
    {
        if (comparePanel == null) { OnEquipCallback?.Invoke(slotIndex); return; }

        _pendingEquipIndex = slotIndex;
        string equipSlot = ItemTypeUtil.GetEquipSlot(newDef.TypeEnum);
        ItemDef currentDef = null;
        if (_equipment != null && _equipment.TryGetValue(equipSlot, out var equipped)
            && equipped != null && _itemDefs.TryGetValue(equipped.itemId, out var cDef))
            currentDef = cDef;

        comparePanel.SetActive(true);

        if (compareNewName != null)
        {
            compareNewName.text = newDef.name;
            compareNewName.color = GameConfig.GetGradeColor(newDef.GradeEnum);
        }
        if (compareCurrentName != null)
            compareCurrentName.text = currentDef != null ? currentDef.name : "(empty)";

        var newS = newDef.stats ?? new ItemStats();
        var curS = currentDef?.stats ?? new ItemStats();

        if (compareNewStats != null) compareNewStats.text = FormatStats(newS);
        if (compareCurrentStats != null) compareCurrentStats.text = FormatStats(curS);
        if (compareDiffStats != null) compareDiffStats.text = FormatDiff(newS, curS);
    }

    void ConfirmEquip()
    {
        if (_pendingEquipIndex >= 0)
            OnEquipCallback?.Invoke(_pendingEquipIndex);
        _pendingEquipIndex = -1;
        if (comparePanel != null) comparePanel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_confirm");
    }

    static string FormatStats(ItemStats s)
    {
        var lines = new System.Text.StringBuilder();
        if (s.atk > 0) lines.AppendLine($"ATK: {s.atk}");
        if (s.def > 0) lines.AppendLine($"DEF: {s.def}");
        if (s.maxHp > 0) lines.AppendLine($"HP: {s.maxHp}");
        if (s.maxMp > 0) lines.AppendLine($"MP: {s.maxMp}");
        if (s.spd > 0) lines.AppendLine($"SPD: {s.spd}");
        if (s.crit > 0) lines.AppendLine($"CRIT: {s.crit}%");
        return lines.ToString().TrimEnd();
    }

    static string FormatDiff(ItemStats newS, ItemStats curS)
    {
        var lines = new System.Text.StringBuilder();
        AppendDiff(lines, "ATK", newS.atk - curS.atk);
        AppendDiff(lines, "DEF", newS.def - curS.def);
        AppendDiff(lines, "HP", newS.maxHp - curS.maxHp);
        AppendDiff(lines, "MP", newS.maxMp - curS.maxMp);
        AppendDiff(lines, "SPD", newS.spd - curS.spd);
        AppendDiff(lines, "CRIT", newS.crit - curS.crit);
        return lines.ToString().TrimEnd();
    }

    static void AppendDiff(System.Text.StringBuilder sb, string label, int diff)
    {
        if (diff > 0) sb.AppendLine($"<color=#66ff66>{label}: +{diff} \u25b2</color>");
        else if (diff < 0) sb.AppendLine($"<color=#ff6666>{label}: {diff} \u25bc</color>");
    }

    void SetFilter(int idx)
    {
        _currentFilter = idx < FilterNames.Length ? FilterNames[idx] : "all";
        AudioManager.Instance?.PlaySFX("sfx_tab_switch");
        Refresh();
    }

    void CycleSortMode()
    {
        _currentSortMode = (_currentSortMode + 1) % SortModeNames.Length;
        if (sortModeText != null) sortModeText.text = SortModeNames[_currentSortMode];
        AudioManager.Instance?.PlaySFX("sfx_click");
        Refresh();
    }

    void RefreshEquipment()
    {
        RefreshEquipSlot(weaponSlot, "weapon");
        RefreshEquipSlot(helmetSlot, "helmet");
        RefreshEquipSlot(armorSlot, "armor");
        RefreshEquipSlot(bootsSlot, "boots");
        RefreshEquipSlot(accessorySlot, "accessory");
    }

    void RefreshEquipSlot(EquipSlotUI slot, string slotName)
    {
        if (slot == null) return;
        if (_equipment != null && _equipment.TryGetValue(slotName, out var inst)
            && inst != null && _itemDefs.TryGetValue(inst.itemId, out var def))
        {
            slot.SetItem(def.name, inst.enhanceLevel, GameConfig.GetGradeColor(def.GradeEnum));
            slot.OnUnequip = () => OnUnequipCallback?.Invoke(slotName);
        }
        else
        {
            slot.Clear();
            slot.OnUnequip = null;
        }
    }

    void RefreshStats(Stats stats)
    {
        if (atkText != null) { atkText.text = $"ATK {stats.atk}"; atkText.color = new Color(1f, 0.6f, 0.2f); }
        if (defText != null) { defText.text = $"DEF {stats.def}"; defText.color = new Color(0.4f, 0.7f, 1f); }
        if (spdText != null) { spdText.text = $"SPD {stats.spd}"; spdText.color = new Color(0.4f, 1f, 0.6f); }
        if (critText != null) { critText.text = $"CRIT {stats.crit}%"; critText.color = new Color(1f, 0.85f, 0.2f); }
        if (hpStatText != null) { hpStatText.text = $"HP {stats.hp}/{stats.maxHp}"; hpStatText.color = new Color(1f, 0.4f, 0.4f); }
        if (mpStatText != null) { mpStatText.text = $"MP {stats.mp}/{stats.maxMp}"; mpStatText.color = new Color(0.4f, 0.6f, 1f); }

        RefreshStatAllocation();
    }

    void RefreshStatAllocation()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.PlayerState == null) return;

        var bonus = gm.PlayerState.BonusStats;
        int points = gm.PlayerState.StatPoints;
        bool hasPoints = points > 0;

        if (statPointsText != null)
            statPointsText.text = hasPoints ? $"SP: {points}" : "";

        int strVal = bonus != null && bonus.TryGetValue("str", out int s) ? s : 0;
        int dexVal = bonus != null && bonus.TryGetValue("dex", out int d) ? d : 0;
        int wisVal = bonus != null && bonus.TryGetValue("wis", out int w) ? w : 0;
        int lucVal = bonus != null && bonus.TryGetValue("luc", out int l) ? l : 0;

        if (strText != null) strText.text = $"<color=#ff9955>STR {strVal}</color>  ATK+{strVal * GameConfig.StrAtkBonus:F0} HP+{strVal * GameConfig.StrHpBonus:F0}";
        if (dexText != null) dexText.text = $"<color=#55ddff>DEX {dexVal}</color>  SPD+{dexVal * GameConfig.DexSpdBonus:F0} DEF+{dexVal * GameConfig.DexDefBonus:F0}";
        if (wisText != null) wisText.text = $"<color=#aa77ff>WIS {wisVal}</color>  MP+{wisVal * GameConfig.WisMpBonus:F0}";
        if (lucText != null) lucText.text = $"<color=#ffdd44>LUC {lucVal}</color>  CRIT+{lucVal * GameConfig.LucCritBonus:F0}";

        if (strAddButton != null) strAddButton.gameObject.SetActive(hasPoints);
        if (dexAddButton != null) dexAddButton.gameObject.SetActive(hasPoints);
        if (wisAddButton != null) wisAddButton.gameObject.SetActive(hasPoints);
        if (lucAddButton != null) lucAddButton.gameObject.SetActive(hasPoints);
    }

    void AllocateStat(string statName)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.PlayerState == null) return;
        if (gm.PlayerState.StatPoints <= 0) return;

        if (gm.PlayerState.BonusStats == null)
            gm.PlayerState.BonusStats = new Dictionary<string, int>();

        gm.PlayerState.BonusStats.TryGetValue(statName, out int current);
        gm.PlayerState.BonusStats[statName] = current + 1;
        gm.PlayerState.StatPoints--;
        gm.PlayerState.RecalcStats(gm.Data.Items, gm.Data.SetBonuses);

        Refresh();
    }
}

public class EquipSlotUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI enhanceText;
    [SerializeField] Image borderImage;
    [SerializeField] Button unequipButton;

    public Action OnUnequip;

    void Awake()
    {
        if (unequipButton != null)
            unequipButton.onClick.AddListener(() => OnUnequip?.Invoke());
    }

    public void SetItem(string itemName, int enhanceLevel, Color gradeColor)
    {
        if (nameText != null)
        {
            nameText.text = enhanceLevel > 0 ? $"{itemName} +{enhanceLevel}" : itemName;
            nameText.color = gradeColor;
        }
        if (borderImage != null) borderImage.color = gradeColor;
        if (unequipButton != null) unequipButton.gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (nameText != null) { nameText.text = ""; nameText.color = Color.gray; }
        if (borderImage != null) borderImage.color = Color.gray;
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
    }
}
