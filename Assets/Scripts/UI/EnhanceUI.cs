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

    [Header("Result")]
    [SerializeField] TextMeshProUGUI resultText;

    static readonly Color AffordableColor = new(0.4f, 1f, 0.4f);
    static readonly Color UnaffordableColor = new(0.4f, 0.4f, 0.4f);
    static readonly string[] SlotLabels = { "Weapon", "Helmet", "Armor", "Boots", "Accessory" };

    static readonly (float success, float destroy, int gold)[] EnhanceTable =
    {
        (1.0f,  0.00f, 100),   // +0→+1
        (0.9f,  0.00f, 200),   // +1→+2
        (0.8f,  0.00f, 400),   // +2→+3
        (0.7f,  0.00f, 700),   // +3→+4
        (0.6f,  0.05f, 1200),  // +4→+5
        (0.5f,  0.10f, 2000),  // +5→+6
        (0.4f,  0.15f, 3500),  // +6→+7
        (0.3f,  0.20f, 5000),  // +7→+8
        (0.2f,  0.25f, 8000),  // +8→+9
        (0.1f,  0.30f, 12000), // +9→+10
    };

    public static (float success, float destroy, int gold) GetEnhanceInfo(int level)
    {
        if (level < 0 || level >= EnhanceTable.Length) return (0, 0, 0);
        return EnhanceTable[level];
    }

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

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open(Dictionary<string, ItemInstance> equipment, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold, Action<string> onEnhance)
    {
        _equipment = equipment;
        _itemDefs = itemDefs;
        _getGold = getGold;
        _spendGold = spendGold;
        _onEnhance = onEnhance;
        if (panel != null) panel.SetActive(true);
        AudioManager.Instance?.PlaySFX("sfx_menu_open");
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_menu_close");
    }

    public void Toggle(Dictionary<string, ItemInstance> equipment, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold, Action<string> onEnhance)
    {
        if (IsOpen) Close();
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

        var info = GetEnhanceInfo(enhLevel);
        int cost = info.gold;
        bool maxed = enhLevel >= EnhanceTable.Length;
        bool canAfford = hasEquip && !maxed && gold >= cost;

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
        {
            if (!hasEquip) texts[2].text = "";
            else if (maxed) texts[2].text = "MAX";
            else texts[2].text = $"Cost: {cost}G | {info.success * 100:F0}% success" +
                (info.destroy > 0 ? $" | {info.destroy * 100:F0}% destroy" : "");
        }

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.interactable = canAfford;
        string captured = slotName;
        btn.onClick.AddListener(() => TryEnhance(captured));

        _entries.Add(go);
    }

    void TryEnhance(string slotName)
    {
        if (_equipment == null || !_equipment.TryGetValue(slotName, out var inst) || inst == null) return;
        var info = GetEnhanceInfo(inst.enhanceLevel);
        if (info.gold <= 0) return;

        _spendGold?.Invoke(info.gold);

        float roll = UnityEngine.Random.value;
        if (roll < info.success)
        {
            inst.enhanceLevel++;
            _onEnhance?.Invoke(slotName);
            AudioManager.Instance?.PlaySFX("sfx_enchant_success");
            ShowResult($"<color=#66ff66>Success! +{inst.enhanceLevel}</color>");
        }
        else if (roll < info.success + info.destroy)
        {
            _equipment.Remove(slotName);
            AudioManager.Instance?.PlaySFX("sfx_enchant_critfail");
            ShowResult("<color=#ff4444>Equipment Destroyed!</color>");
        }
        else
        {
            AudioManager.Instance?.PlaySFX("sfx_enchant_fail");
            ShowResult("<color=#ffaa44>Enhancement Failed...</color>");
        }
        Refresh();
    }

    void ShowResult(string text)
    {
        if (resultText != null) resultText.text = text;
    }

    void ClearEntries()
    {
        foreach (var go in _entries)
            Destroy(go);
        _entries.Clear();
    }
}
