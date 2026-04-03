using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;
    [SerializeField] TextMeshProUGUI titleText;

    [Header("Gold")]
    [SerializeField] TextMeshProUGUI goldText;

    [Header("Status")]
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Item List")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform itemListContent;
    [SerializeField] GameObject shopItemPrefab;

    static readonly Color AffordableColor = new(0.4f, 1f, 0.4f);
    static readonly Color UnaffordableColor = new(0.4f, 0.4f, 0.4f);

    readonly List<GameObject> _itemEntries = new();
    readonly List<GameObject> _pool = new();

    InventorySystem _inventory;
    Dictionary<string, ItemDef> _itemDefs;
    Func<int> _getGold;
    Action<int> _spendGold;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open(InventorySystem inventory, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold)
    {
        if (IsOpen) return;
        _inventory = inventory;
        _itemDefs = itemDefs;
        _getGold = getGold;
        _spendGold = spendGold;
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "<b><color=#ffd900>Shop</color></b>";
        AudioManager.Instance?.PlaySFX("sfx_menu_open");
        if (_inventory == null || _itemDefs == null) return;
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_menu_close");
    }

    public void Toggle(InventorySystem inventory, Dictionary<string, ItemDef> itemDefs,
        Func<int> getGold, Action<int> spendGold)
    {
        if (IsOpen) Close();
        else Open(inventory, itemDefs, getGold, spendGold);
    }

    public void Refresh()
    {
        ClearEntries();
        if (_itemDefs == null || _getGold == null) return;

        int gold = _getGold();
        if (goldText != null) goldText.text = $"<color=#ffd900>\u25c6 {gold:N0}G</color>";

        foreach (var kv in _itemDefs)
        {
            var def = kv.Value;
            if (def.shopPrice <= 0) continue;
            AddShopEntry(def, gold);
        }
    }

    void AddShopEntry(ItemDef def, int gold)
    {
        if (itemListContent == null || shopItemPrefab == null) return;

        var go = GetOrCreateEntry();
        go.SetActive(true);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        bool canAfford = gold >= def.shopPrice;

        if (texts.Length > 0)
        {
            string prefix = canAfford ? "\u25b8" : "\u25b9";
            string prefixColor = canAfford ? "#88ff88" : "#555555";
            string nameHex = canAfford
                ? "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(def.GradeEnum))
                : "#555555";
            texts[0].text = $"<color={prefixColor}>{prefix}</color> <color={nameHex}>{def.name}</color>";
            texts[0].color = Color.white;
        }

        if (texts.Length > 1)
        {
            string priceColor = canAfford ? "#ffd900" : "#885500";
            texts[1].text = $"<color={priceColor}>\u25c6 {def.shopPrice:N0}G</color>";
            texts[1].color = Color.white;
        }

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.interactable = canAfford;
        string itemId = def.id;
        int price = def.shopPrice;
        btn.onClick.AddListener(() => BuyItem(itemId, price, def.stackable, def.maxStack));

        _itemEntries.Add(go);
    }

    void BuyItem(string itemId, int price, bool stackable, int maxStack)
    {
        if (_getGold == null || _spendGold == null || _inventory == null) return;

        if (_getGold() < price)
        {
            ShowStatus("<color=#ff4444>Not enough gold!</color>");
            AudioManager.Instance?.PlaySFX("sfx_error");
            return;
        }

        int overflow = _inventory.AddItem(itemId, 1, stackable, maxStack);
        if (overflow == 0)
        {
            _spendGold(price);
            AudioManager.Instance?.PlaySFX("sfx_coin");
            ShowStatus("<color=#66ff66>Purchased!</color>");
            Refresh();
        }
        else
        {
            ShowStatus("<color=#ffaa44>Inventory full!</color>");
            AudioManager.Instance?.PlaySFX("sfx_error");
        }
    }

    void ShowStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    GameObject GetOrCreateEntry()
    {
        if (_pool.Count > 0)
        {
            var go = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            go.SetActive(true);
            return go;
        }
        return Instantiate(shopItemPrefab, itemListContent);
    }

    void ClearEntries()
    {
        foreach (var go in _itemEntries)
        {
            go.SetActive(false);
            _pool.Add(go);
        }
        _itemEntries.Clear();
    }

    void OnDestroy()
    {
        foreach (var go in _pool) Destroy(go);
        _pool.Clear();
        foreach (var go in _itemEntries) Destroy(go);
        _itemEntries.Clear();
    }
}
