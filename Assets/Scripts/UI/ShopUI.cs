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

    [Header("Item List")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform itemListContent;
    [SerializeField] GameObject shopItemPrefab;

    static readonly Color AffordableColor = new(0.4f, 1f, 0.4f);
    static readonly Color UnaffordableColor = new(0.4f, 0.4f, 0.4f);

    readonly List<GameObject> _itemEntries = new();

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
        _inventory = inventory;
        _itemDefs = itemDefs;
        _getGold = getGold;
        _spendGold = spendGold;
        if (panel != null) panel.SetActive(true);
        if (_inventory == null || _itemDefs == null) return;
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
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
        if (goldText != null) goldText.text = $"Gold: {gold:N0}";

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

        var go = Instantiate(shopItemPrefab, itemListContent);
        go.SetActive(true);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        bool canAfford = gold >= def.shopPrice;

        if (texts.Length > 0)
        {
            string prefix = canAfford ? ">>" : "  ";
            texts[0].text = $"{prefix} {def.name}";
            texts[0].color = canAfford ? AffordableColor : UnaffordableColor;
        }

        if (texts.Length > 1)
            texts[1].text = $"{def.shopPrice}G";

        if (texts.Length > 2)
            texts[2].text = def.description ?? "";

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.interactable = canAfford;
        string itemId = def.id;
        int price = def.shopPrice;
        btn.onClick.AddListener(() => BuyItem(itemId, price, def.stackable, def.maxStack));

        _itemEntries.Add(go);
    }

    void BuyItem(string itemId, int price, bool stackable, int maxStack)
    {
        if (_getGold == null || _spendGold == null || _inventory == null) return;
        if (_getGold() < price) return;

        int overflow = _inventory.AddItem(itemId, 1, stackable, maxStack);
        if (overflow == 0)
        {
            _spendGold(price);
            Refresh();
        }
    }

    void ClearEntries()
    {
        foreach (var go in _itemEntries)
            Destroy(go);
        _itemEntries.Clear();
    }
}
