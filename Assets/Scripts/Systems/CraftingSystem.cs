using System.Collections.Generic;
using System.Linq;

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
        return _recipes.Where(r => CanCraft(r, inv)).ToArray();
    }

    public bool CanCraft(string resultId, InventorySystem inv)
    {
        var recipe = _recipes.FirstOrDefault(r => r.resultId == resultId);
        return recipe != null && CanCraft(recipe, inv);
    }

    bool CanCraft(RecipeDef recipe, InventorySystem inv)
    {
        return recipe.materials.All(m => inv.HasItems(m.itemId, m.count));
    }

    public bool Craft(string resultId, InventorySystem inv)
    {
        var recipe = _recipes.FirstOrDefault(r => r.resultId == resultId);
        if (recipe == null || !CanCraft(recipe, inv)) return false;

        foreach (var m in recipe.materials)
            inv.RemoveItem(m.itemId, m.count);

        var def = _itemDefs.GetValueOrDefault(resultId);
        inv.AddItem(resultId, 1, def?.stackable ?? false, def?.maxStack ?? 1);
        return true;
    }
}
