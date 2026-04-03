using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Recipe List")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform recipeListContent;
    [SerializeField] GameObject recipePrefab;

    static readonly Color CraftableColor = new(0.4f, 1f, 0.4f);
    static readonly Color UncraftableColor = new(0.4f, 0.4f, 0.4f);

    readonly List<GameObject> _entries = new();

    CraftingSystem _craftingSystem;
    InventorySystem _inventory;
    Dictionary<string, ItemDef> _itemDefs;
    Action<string> _onCraft;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open(CraftingSystem craftingSystem, InventorySystem inventory,
        Dictionary<string, ItemDef> itemDefs, Action<string> onCraft)
    {
        if (IsOpen) return;
        _craftingSystem = craftingSystem;
        _inventory = inventory;
        _itemDefs = itemDefs;
        _onCraft = onCraft;
        if (panel != null) panel.SetActive(true);
        AudioManager.Instance?.PlaySFX("sfx_menu_open");
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX("sfx_menu_close");
    }

    public void Toggle(CraftingSystem craftingSystem, InventorySystem inventory,
        Dictionary<string, ItemDef> itemDefs, Action<string> onCraft)
    {
        if (IsOpen) Close();
        else Open(craftingSystem, inventory, itemDefs, onCraft);
    }

    void Refresh()
    {
        ClearEntries();
        if (_craftingSystem == null || _inventory == null) return;

        foreach (var recipe in _craftingSystem.AllRecipes)
            AddRecipeEntry(recipe);
    }

    void AddRecipeEntry(RecipeDef recipe)
    {
        if (recipeListContent == null || recipePrefab == null) return;

        var go = Instantiate(recipePrefab, recipeListContent);
        go.SetActive(true);

        bool canCraft = _craftingSystem.CanCraft(recipe.resultId, _inventory);
        string resultName = _itemDefs != null && _itemDefs.TryGetValue(recipe.resultId, out var resultDef)
            ? resultDef.name : recipe.resultId;

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts.Length > 0)
        {
            string prefix = canCraft ? "\u25b8" : "\u25b9";
            string prefixColor = canCraft ? "#88ff88" : "#555555";
            string gradeHex = canCraft && resultDef != null
                ? "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(resultDef.GradeEnum))
                : "#555555";
            texts[0].text = $"<color={prefixColor}>{prefix}</color> <color={gradeHex}>{resultName}</color>";
            texts[0].color = Color.white;
        }

        if (texts.Length > 1 && recipe.materials != null)
        {
            var matLines = new List<string>();
            foreach (var mat in recipe.materials)
            {
                int have = _inventory.GetCount(mat.itemId);
                string matName = _itemDefs != null && _itemDefs.TryGetValue(mat.itemId, out var matDef)
                    ? matDef.name : mat.itemId;
                bool met = have >= mat.count;
                string countColor = met ? "#66ff66" : have > 0 ? "#ffaa44" : "#ff6666";
                string check = met ? "<color=#66ff66>\u2713</color> " : "";
                matLines.Add($"{check}<color=#cccccc>{matName}</color> <color={countColor}>({have}/{mat.count})</color>");
            }
            texts[1].text = string.Join("  ", matLines);
        }

        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.interactable = canCraft;
        string resultId = recipe.resultId;
        btn.onClick.AddListener(() =>
        {
            _onCraft?.Invoke(resultId);
            AudioManager.Instance?.PlaySFX("sfx_craft");
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
