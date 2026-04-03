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

    [Header("Confirm Popup")]
    [SerializeField] GameObject confirmPopup;
    [SerializeField] TextMeshProUGUI confirmText;
    [SerializeField] Button confirmOkButton;
    [SerializeField] Button confirmCancelButton;

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
    string _pendingSlot;

    Dictionary<string, ItemInstance> _equipment;
    Dictionary<string, ItemDef> _itemDefs;
    Func<int> _getGold;
    Action<int> _spendGold;
    Action<string> _onEnhance;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (confirmOkButton != null) confirmOkButton.onClick.AddListener(OnConfirmOk);
        if (confirmCancelButton != null) confirmCancelButton.onClick.AddListener(OnConfirmCancel);
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open(Dictionary<string, ItemInstance> equipment, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold, Action<string> onEnhance)
    {
        if (IsOpen) return;
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
        if (confirmPopup != null) confirmPopup.SetActive(false);
        _pendingSlot = null;
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
        if (goldText != null) goldText.text = $"<color=#ffd900>\u25c6 {gold:N0}G</color>";

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
        {
            string slotColor = slotName switch
            {
                "weapon"    => "#ff7744",
                "helmet"    => "#aabbdd",
                "armor"     => "#6699ff",
                "boots"     => "#66dd88",
                "accessory" => "#cc88ff",
                _           => "#aaaaaa"
            };
            texts[0].text = $"<color={slotColor}>{label}</color>";
            texts[0].color = Color.white;
        }

        if (texts.Length > 1)
        {
            if (hasEquip)
            {
                string prefix = canAfford ? "\u25b8" : "\u25b9";
                string eColor = enhLevel >= 10 ? "#ff9900" : enhLevel >= 7 ? "#66aaff" : enhLevel >= 4 ? "#66ff66" : "#aaaaaa";
                string enhTag = enhLevel > 0 ? $" <color={eColor}>+{enhLevel}</color>" : "";
                string nameColor = canAfford ? "#66ff66" : "#888888";
                texts[1].text = $"{prefix} <color={nameColor}>{itemName}</color>{enhTag}";
                texts[1].color = Color.white;
            }
            else
            {
                texts[1].text = "<color=#444444>(empty)</color>";
                texts[1].color = Color.white;
            }
        }

        if (texts.Length > 2)
        {
            if (!hasEquip) texts[2].text = "";
            else if (maxed) { texts[2].text = "<color=#ffd900>MAX</color>"; }
            else
            {
                float succ = info.success;
                string succColor = succ >= 0.7f ? "#66ff88" : succ >= 0.4f ? "#ffdd44" : "#ff6666";
                string costPart = $"<color=#ffd900>{cost}G</color>";
                string succPart = $"<color={succColor}>{succ * 100:F0}%</color>";
                string destPart = info.destroy > 0
                    ? $" | <color=#ff5555>{info.destroy * 100:F0}% lose</color>" : "";
                texts[2].text = $"{costPart} | {succPart}{destPart}";
            }
        }

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.interactable = canAfford;
        string captured = slotName;
        btn.onClick.AddListener(() => RequestEnhance(captured));

        _entries.Add(go);
    }

    void RequestEnhance(string slotName)
    {
        if (_equipment == null || !_equipment.TryGetValue(slotName, out var inst) || inst == null) return;
        var info = GetEnhanceInfo(inst.enhanceLevel);
        if (info.gold <= 0) return;

        if (info.destroy > 0)
        {
            _pendingSlot = slotName;
            string itemName = _itemDefs != null && _itemDefs.TryGetValue(inst.itemId, out var def)
                ? def.name : inst.itemId;
            ShowConfirmPopup(itemName, inst.enhanceLevel, info);
        }
        else
        {
            TryEnhance(slotName);
        }
    }

    void ShowConfirmPopup(string itemName, int level,
        (float success, float destroy, int gold) info)
    {
        if (confirmPopup != null) confirmPopup.SetActive(true);
        if (confirmText != null)
        {
            confirmText.text = $"Enhance <b>{itemName}</b> <color=#aaffaa>+{level}</color> \u2192 <color=#ffd900>+{level + 1}</color>?\n" +
                $"<color=#66ff88>Success: {info.success * 100:F0}%</color>\n" +
                $"<color=#ff4444>Destroy: {info.destroy * 100:F0}%</color>\n" +
                $"Cost: <color=#ffd900>{info.gold:N0}G</color>";
        }
    }

    void OnConfirmOk()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (_pendingSlot != null)
        {
            string slot = _pendingSlot;
            _pendingSlot = null;
            TryEnhance(slot);
        }
    }

    void OnConfirmCancel()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
        _pendingSlot = null;
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
