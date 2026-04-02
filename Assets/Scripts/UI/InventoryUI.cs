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

    [Header("Info")]
    [SerializeField] TextMeshProUGUI levelGoldText;

    [Header("Tooltip")]
    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TextMeshProUGUI tooltipName;
    [SerializeField] TextMeshProUGUI tooltipDesc;
    [SerializeField] TextMeshProUGUI tooltipStats;

    [Header("Drag Visual")]
    [SerializeField] Image dragIcon;

    const int Columns = 5;
    readonly List<InventorySlotUI> _slots = new();

    InventorySystem _inventory;
    Dictionary<string, ItemInstance> _equipment;
    Dictionary<string, ItemDef> _itemDefs;

    int _dragFromSlot = -1;
    bool _dragging;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (sortButton != null) sortButton.onClick.AddListener(() => OnSortCallback?.Invoke());
    }

    public void Show() { panel.SetActive(true); }
    public void Hide()
    {
        panel.SetActive(false);
        HideTooltip();
        CancelDrag();
    }

    public void Toggle()
    {
        if (panel.activeSelf) Hide(); else Show();
    }

    public void Refresh(InventorySystem inventory, Dictionary<string, ItemInstance> equipment,
        Dictionary<string, ItemDef> itemDefs, Stats playerStats)
    {
        _inventory = inventory;
        _equipment = equipment;
        _itemDefs = itemDefs;

        RefreshGrid();
        RefreshEquipment();
        RefreshStats(playerStats);
    }

    void RefreshGrid()
    {
        EnsureSlots(_inventory.MaxSlots);

        for (int i = 0; i < _inventory.MaxSlots; i++)
        {
            var item = _inventory.GetSlot(i);
            var slot = _slots[i];

            if (item != null && _itemDefs.TryGetValue(item.itemId, out var def))
            {
                slot.SetItem(def.name, item.count, item.enhanceLevel,
                    GameConfig.GetGradeColor(def.GradeEnum), def.TypeEnum);
                slot.SetActive(true);
            }
            else
            {
                slot.Clear();
            }
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
                OnEquipCallback?.Invoke(index);
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

        tooltipPanel.transform.position = Input.mousePosition + new Vector3(10, -10, 0);
    }

    void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
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
        if (atkText != null) atkText.text = $"ATK {stats.atk}";
        if (defText != null) defText.text = $"DEF {stats.def}";
        if (spdText != null) spdText.text = $"SPD {stats.spd}";
        if (critText != null) critText.text = $"CRIT {stats.crit}%";
        if (hpStatText != null) hpStatText.text = $"HP {stats.hp}/{stats.maxHp}";
        if (mpStatText != null) mpStatText.text = $"MP {stats.mp}/{stats.maxMp}";
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

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI enhanceText;
    [SerializeField] Image borderImage;
    [SerializeField] Image iconImage;

    public int SlotIndex;
    public Action<int> OnClicked;
    public Action<int> OnBeginDragAction;
    public Action<int> OnEndDragAction;
    public Action<int> OnDropAction;
    public Action<int> OnHover;
    public Action<int> OnHoverExit;

    public void SetItem(string name, int count, int enhanceLevel, Color gradeColor, ItemType type)
    {
        if (nameText != null) { nameText.text = name; nameText.color = gradeColor; }
        if (countText != null)
            countText.text = count > 1 ? count.ToString() : "";
        if (enhanceText != null)
            enhanceText.text = enhanceLevel > 0 ? $"+{enhanceLevel}" : "";
        if (borderImage != null) borderImage.color = gradeColor;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Clear()
    {
        if (nameText != null) { nameText.text = ""; nameText.color = Color.white; }
        if (countText != null) countText.text = "";
        if (enhanceText != null) enhanceText.text = "";
        if (borderImage != null) borderImage.color = new Color(0.3f, 0.3f, 0.3f);
    }

    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(SlotIndex);
    public void OnBeginDrag(PointerEventData eventData) => OnBeginDragAction?.Invoke(SlotIndex);
    public void OnEndDrag(PointerEventData eventData) => OnEndDragAction?.Invoke(SlotIndex);
    public void OnDrop(PointerEventData eventData) => OnDropAction?.Invoke(SlotIndex);
    public void OnDrag(PointerEventData eventData) { }
    public void OnPointerEnter(PointerEventData eventData) => OnHover?.Invoke(SlotIndex);
    public void OnPointerExit(PointerEventData eventData) => OnHoverExit?.Invoke(SlotIndex);
}
