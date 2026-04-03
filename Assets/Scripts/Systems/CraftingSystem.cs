using System.Collections.Generic;

public class CraftingSystem
{
    readonly RecipeDef[] _recipes;
    readonly Dictionary<string, ItemDef> _itemDefs;

    public CraftingSystem(RecipeDef[] recipes, Dictionary<string, ItemDef> itemDefs)
    {
        _recipes = recipes;
        _itemDefs = itemDefs;
    }

    public RecipeDef[] AllRecipes => _recipes;

    public RecipeDef[] GetAvailableRecipes(InventorySystem inv)
    {
        var result = new List<RecipeDef>();
        for (int i = 0; i < _recipes.Length; i++)
        {
            if (CanCraft(_recipes[i], inv))
                result.Add(_recipes[i]);
        }
        return result.ToArray();
    }

    public bool CanCraft(string resultId, InventorySystem inv)
    {
        var recipe = FindRecipe(resultId);
        return recipe != null && CanCraft(recipe, inv);
    }

    bool CanCraft(RecipeDef recipe, InventorySystem inv)
    {
        for (int i = 0; i < recipe.materials.Length; i++)
        {
            if (!inv.HasItems(recipe.materials[i].itemId, recipe.materials[i].count))
                return false;
        }
        return true;
    }

    public bool Craft(string resultId, InventorySystem inv)
    {
        var recipe = FindRecipe(resultId);
        if (recipe == null || !CanCraft(recipe, inv)) return false;

        foreach (var m in recipe.materials)
            inv.RemoveItem(m.itemId, m.count);

        _itemDefs.TryGetValue(resultId, out var def);
        inv.AddItem(resultId, 1, def?.stackable ?? false, def?.maxStack ?? 1);
        return true;
    }

    RecipeDef FindRecipe(string resultId)
    {
        for (int i = 0; i < _recipes.Length; i++)
        {
            if (_recipes[i].resultId == resultId)
                return _recipes[i];
        }
        return null;
    }
}
