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
        if (sortModeText != null)
        {
            sortModeText.color = Color.white;
            sortModeText.text = $"<color=#666666>\u21c5</color> <color=#88ddff><b>{SortModeNames[_currentSortMode]}</b></color>";
        }

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
        UpdateFilterButtonHighlight(System.Array.IndexOf(FilterNames, _currentFilter));
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
        {
            levelGoldText.color = Color.white;
            levelGoldText.text = $"<color=#55ee88>Lv.<b>{gm.PlayerState.Level}</b></color>  <color=#ffd900>\u25c6 <b>{gm.PlayerState.Gold:N0}</b>G</color>";
        }
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
            string gradeHex = "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(def.GradeEnum));
            tooltipName.text = $"<b><color={gradeHex}>{def.name}</color></b>";
            tooltipName.color = Color.white;
            tooltipName.fontStyle = TMPro.FontStyles.Normal;
        }
        if (tooltipDesc != null)
        {
            tooltipDesc.text = $"<color=#bbbbbb>{def.description ?? ""}</color>";
            tooltipDesc.color = Color.white;
            tooltipDesc.fontStyle = TMPro.FontStyles.Italic;
        }

        if (tooltipStats != null)
        {
            tooltipStats.color = Color.white;
            var lines = new List<string>();
            var s = def.stats;
            int enh = inst.enhanceLevel * GameConfig.EnhanceBonusPerLevel;
            if (s.atk > 0) lines.Add($"<color=#885522>ATK</color> <color=#ff9944><b>+{s.atk + enh}</b></color>");
            if (s.def > 0) lines.Add($"<color=#224488>DEF</color> <color=#55aaff><b>+{s.def + enh}</b></color>");
            if (s.maxHp > 0) lines.Add($"<color=#882222>HP</color> <color=#ff6655><b>+{s.maxHp + enh}</b></color>");
            if (s.maxMp > 0) lines.Add($"<color=#333388>MP</color> <color=#6688ff><b>+{s.maxMp + enh}</b></color>");
            if (s.spd > 0) lines.Add($"<color=#226644>SPD</color> <color=#55ee88><b>+{s.spd}</b></color>");
            if (s.crit > 0) lines.Add($"<color=#886600>CRIT</color> <color=#ffdd44><b>+{s.crit}%</b></color>");
            if (inst.enhanceLevel > 0)
            {
                string ec = inst.enhanceLevel >= 10 ? "#ff9944" : inst.enhanceLevel >= 7 ? "#6688ff" : inst.enhanceLevel >= 4 ? "#66ff88" : "#aaaaaa";
                lines.Add($"<color=#888888>Enh</color> <color={ec}><b>+{inst.enhanceLevel}</b></color>");
            }
            if (def.healHp > 0) lines.Add($"<color=#884444>HealHP</color> <color=#66ff88><b>{def.healHp}</b></color>");
            if (def.healMp > 0) lines.Add($"<color=#444488>HealMP</color> <color=#88aaff><b>{def.healMp}</b></color>");
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
            string hex = "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(newDef.GradeEnum));
            compareNewName.text = $"<b><color={hex}>{newDef.name}</color></b>";
            compareNewName.color = Color.white;
        }
        if (compareCurrentName != null)
        {
            if (currentDef != null)
            {
                string hex = "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(currentDef.GradeEnum));
                compareCurrentName.text = $"<b><color={hex}>{currentDef.name}</color></b>";
                compareCurrentName.color = Color.white;
            }
            else
            {
                compareCurrentName.text = "<color=#444444>(empty)</color>";
                compareCurrentName.color = Color.white;
            }
        }

        var newS = newDef.stats ?? new ItemStats();
        var curS = currentDef?.stats ?? new ItemStats();

        if (compareNewStats != null) { compareNewStats.color = Color.white; compareNewStats.text = FormatStats(newS); }
        if (compareCurrentStats != null) { compareCurrentStats.color = Color.white; compareCurrentStats.text = FormatStats(curS); }
        if (compareDiffStats != null) { compareDiffStats.color = Color.white; compareDiffStats.text = FormatDiff(newS, curS); }
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
        if (s.atk > 0) lines.AppendLine($"<color=#885522>ATK</color> <color=#ff9944><b>{s.atk}</b></color>");
        if (s.def > 0) lines.AppendLine($"<color=#224488>DEF</color> <color=#55aaff><b>{s.def}</b></color>");
        if (s.maxHp > 0) lines.AppendLine($"<color=#882222>HP</color> <color=#ff6655><b>{s.maxHp}</b></color>");
        if (s.maxMp > 0) lines.AppendLine($"<color=#333388>MP</color> <color=#6688ff><b>{s.maxMp}</b></color>");
        if (s.spd > 0) lines.AppendLine($"<color=#226644>SPD</color> <color=#55ee88><b>{s.spd}</b></color>");
        if (s.crit > 0) lines.AppendLine($"<color=#886600>CRIT</color> <color=#ffdd44><b>{s.crit}%</b></color>");
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
        if (diff > 0) sb.AppendLine($"<color=#888888>{label}</color> <color=#66ff88><b>+{diff} \u25b2</b></color>");
        else if (diff < 0) sb.AppendLine($"<color=#888888>{label}</color> <color=#ff6655><b>{diff} \u25bc</b></color>");
    }

    void SetFilter(int idx)
    {
        _currentFilter = idx < FilterNames.Length ? FilterNames[idx] : "all";
        AudioManager.Instance?.PlaySFX("sfx_tab_switch");
        UpdateFilterButtonHighlight(idx);
        Refresh();
    }

    void UpdateFilterButtonHighlight(int activeIdx)
    {
        if (filterButtons == null) return;
        for (int i = 0; i < filterButtons.Length; i++)
        {
            if (filterButtons[i] == null) continue;
            bool active = i == activeIdx;
            var img = filterButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = active ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.15f, 0.15f, 0.15f);
            var label = filterButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontStyle = active ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                label.color = active ? new Color(1f, 1f, 0.8f) : new Color(0.533f, 0.533f, 0.533f);
            }
        }
    }

    void CycleSortMode()
    {
        _currentSortMode = (_currentSortMode + 1) % SortModeNames.Length;
        if (sortModeText != null) { sortModeText.color = Color.white; sortModeText.text = $"<color=#666666>\u21c5</color> <color=#88ddff><b>{SortModeNames[_currentSortMode]}</b></color>"; }
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
        if (atkText != null) { atkText.color = Color.white; atkText.text = $"<color=#885522>ATK</color> <color=#ff9944><b>{stats.atk}</b></color>"; }
        if (defText != null) { defText.color = Color.white; defText.text = $"<color=#224488>DEF</color> <color=#55aaff><b>{stats.def}</b></color>"; }
        if (spdText != null) { spdText.color = Color.white; spdText.text = $"<color=#226644>SPD</color> <color=#55ee88><b>{stats.spd}</b></color>"; }
        if (critText != null) { critText.color = Color.white; critText.text = $"<color=#886600>CRIT</color> <color=#ffdd44><b>{stats.crit}%</b></color>"; }
        if (hpStatText != null) { hpStatText.color = Color.white; hpStatText.text = $"<color=#882222>HP</color> <color=#ff6655><b>{stats.hp}</b></color><color=#888888>/{stats.maxHp}</color>"; }
        if (mpStatText != null) { mpStatText.color = Color.white; mpStatText.text = $"<color=#333388>MP</color> <color=#6688ff><b>{stats.mp}</b></color><color=#888888>/{stats.maxMp}</color>"; }

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
        {
            statPointsText.color = Color.white;
            statPointsText.text = hasPoints ? $"<color=#ffdd44>SP: <b>{points}</b></color>" : "";
        }

        int strVal = bonus != null && bonus.TryGetValue("str", out int s) ? s : 0;
        int dexVal = bonus != null && bonus.TryGetValue("dex", out int d) ? d : 0;
        int wisVal = bonus != null && bonus.TryGetValue("wis", out int w) ? w : 0;
        int lucVal = bonus != null && bonus.TryGetValue("luc", out int l) ? l : 0;

        if (strText != null) { strText.color = Color.white; strText.text = $"<color=#ff9944>STR <b>{strVal}</b></color>  <color=#888888>ATK+{strVal * GameConfig.StrAtkBonus:F0} HP+{strVal * GameConfig.StrHpBonus:F0}</color>"; }
        if (dexText != null) { dexText.color = Color.white; dexText.text = $"<color=#55ddff>DEX <b>{dexVal}</b></color>  <color=#888888>SPD+{dexVal * GameConfig.DexSpdBonus:F0} DEF+{dexVal * GameConfig.DexDefBonus:F0}</color>"; }
        if (wisText != null) { wisText.color = Color.white; wisText.text = $"<color=#aa77ff>WIS <b>{wisVal}</b></color>  <color=#888888>MP+{wisVal * GameConfig.WisMpBonus:F0}</color>"; }
        if (lucText != null) { lucText.color = Color.white; lucText.text = $"<color=#ffdd44>LUC <b>{lucVal}</b></color>  <color=#888888>CRIT+{lucVal * GameConfig.LucCritBonus:F0}</color>"; }

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
            string ec = enhanceLevel >= 10 ? "#ff9944" : enhanceLevel >= 7 ? "#6688ff" : enhanceLevel >= 4 ? "#66ff88" : "#aaaaaa";
            string enh = enhanceLevel > 0 ? $" <color={ec}><b>+{enhanceLevel}</b></color>" : "";
            string gradeHex = ColorUtility.ToHtmlStringRGB(gradeColor);
            nameText.text = $"<b><color=#{gradeHex}>{itemName}</color></b>{enh}";
            nameText.color = Color.white;
        }
        if (borderImage != null) borderImage.color = gradeColor;
        if (unequipButton != null) unequipButton.gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (nameText != null) { nameText.text = "<color=#444444>(empty)</color>"; nameText.color = Color.white; }
        if (borderImage != null) borderImage.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
    }
}
