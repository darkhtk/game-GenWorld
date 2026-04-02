using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnhanceUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Gold")]
    [SerializeField] TextMeshProUGUI goldText;

    [Header("Slot List")]
    [SerializeField] Transform slotListContent;
    [SerializeField] GameObject enhanceSlotPrefab;

    static readonly Color AffordableColor = new(0.4f, 1f, 0.4f);
    static readonly Color UnaffordableColor = new(0.4f, 0.4f, 0.4f);
    static readonly string[] SlotLabels = { "Weapon", "Helmet", "Armor", "Boots", "Accessory" };

    readonly List<GameObject> _entries = new();

    Dictionary<string, ItemInstance> _equipment;
    Dictionary<string, ItemDef> _itemDefs;
    Func<int> _getGold;
    Action<int> _spendGold;
    Action<string> _onEnhance;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(Dictionary<string, ItemInstance> equipment, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold, Action<string> onEnhance)
    {
        _equipment = equipment;
        _itemDefs = itemDefs;
        _getGold = getGold;
        _spendGold = spendGold;
        _onEnhance = onEnhance;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void Toggle(Dictionary<string, ItemInstance> equipment, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold, Action<string> onEnhance)
    {
        if (panel.activeSelf) Close();
        else Open(equipment, itemDefs, getGold, spendGold, onEnhance);
    }

    void Refresh()
    {
        ClearEntries();
        if (_equipment == null || _getGold == null) return;

        int gold = _getGold();
        if (goldText != null) goldText.text = $"Gold: {gold:N0}";

        for (int i = 0; i < GameConfig.EquipSlots.Length; i++)
        {
            string slotName = GameConfig.EquipSlots[i];
            string label = i < SlotLabels.Length ? SlotLabels[i] : slotName;
            AddSlotEntry(slotName, label, gold);
        }
    }

    void AddSlotEntry(string slotName, string label, int gold)
    {
        if (slotListContent == null || enhanceSlotPrefab == null) return;

        var go = Instantiate(enhanceSlotPrefab, slotListContent);
        go.SetActive(true);

        bool hasEquip = _equipment.TryGetValue(slotName, out var inst) && inst != null;
        string itemName = "";
        int enhLevel = 0;

        if (hasEquip)
        {
            enhLevel = inst.enhanceLevel;
            itemName = _itemDefs != null && _itemDefs.TryGetValue(inst.itemId, out var def)
                ? def.name : inst.itemId;
        }

        int cost = GameConfig.EnhanceCost(enhLevel);
        bool canAfford = hasEquip && gold >= cost;

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts.Length > 0)
            texts[0].text = label;

        if (texts.Length > 1)
        {
            if (hasEquip)
            {
                string prefix = canAfford ? ">>" : "  ";
                texts[1].text = $"{prefix} {itemName} +{enhLevel}";
                texts[1].color = canAfford ? AffordableColor : Color.white;
            }
            else
            {
                texts[1].text = "(empty)";
                texts[1].color = UnaffordableColor;
            }
        }

        if (texts.Length > 2)
            texts[2].text = hasEquip ? $"Cost: {cost}G" : "";

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.interactable = canAfford;
        string captured = slotName;
        btn.onClick.AddListener(() =>
        {
            _onEnhance?.Invoke(captured);
            Refresh();
        });

        _entries.Add(go);
    }

    void ClearEntries()
    {
        foreach (var go in _entries)
            Destroy(go);
        _entries.Clear();
    }
}
