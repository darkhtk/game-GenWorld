using System.Collections.Generic;

public class InventorySystem
{
    public ItemInstance[] Slots { get; private set; }
    public int MaxSlots { get; }
    public int OccupiedSlots
    {
        get
        {
            int count = 0;
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] != null) count++;
            return count;
        }
    }

    public InventorySystem(int maxSlots = 20)
    {
        MaxSlots = maxSlots;
        Slots = new ItemInstance[maxSlots];
    }

    public ItemInstance GetSlot(int index) =>
        index >= 0 && index < MaxSlots ? Slots[index] : null;

    public int AddItem(string itemId, int count, bool stackable, int maxStack)
    {
        int remaining = count;
        if (stackable)
        {
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (Slots[i] != null && Slots[i].itemId == itemId && Slots[i].count < maxStack)
                {
                    int space = maxStack - Slots[i].count;
                    int add = System.Math.Min(remaining, space);
                    Slots[i].count += add;
                    remaining -= add;
                }
            }
        }
        for (int i = 0; i < MaxSlots && remaining > 0; i++)
        {
            if (Slots[i] == null)
            {
                int add = stackable ? System.Math.Min(remaining, maxStack) : 1;
                Slots[i] = new ItemInstance { itemId = itemId, count = add };
                remaining -= add;
            }
        }
        return remaining;
    }

    public int GetCount(string itemId)
    {
        int total = 0;
        for (int i = 0; i < MaxSlots; i++)
            if (Slots[i] != null && Slots[i].itemId == itemId)
                total += Slots[i].count;
        return total;
    }

    public bool HasItems(string itemId, int count) => GetCount(itemId) >= count;

    public bool RemoveItem(string itemId, int count)
    {
        if (!HasItems(itemId, count)) return false;
        int remaining = count;
        for (int i = MaxSlots - 1; i >= 0 && remaining > 0; i--)
        {
            if (Slots[i] != null && Slots[i].itemId == itemId)
            {
                int remove = System.Math.Min(remaining, Slots[i].count);
                Slots[i].count -= remove;
                remaining -= remove;
                if (Slots[i].count <= 0) Slots[i] = null;
            }
        }
        return true;
    }

    public ItemInstance RemoveAtSlot(int index)
    {
        if (index < 0 || index >= MaxSlots || Slots[index] == null) return null;
        var item = Slots[index];
        Slots[index] = null;
        return item;
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= MaxSlots || b < 0 || b >= MaxSlots) return;
        (Slots[a], Slots[b]) = (Slots[b], Slots[a]);
    }

    public void SortItems(Dictionary<string, ItemDef> itemDefs)
    {
        var items = new List<ItemInstance>();
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != null) items.Add(Slots[i]);
        items.Sort((a, b) =>
        {
            itemDefs.TryGetValue(a.itemId, out var defA);
            itemDefs.TryGetValue(b.itemId, out var defB);
            if (defA == null && defB == null) return 0;
            if (defA == null) return 1;
            if (defB == null) return -1;
            int gradeA = (int)defA.GradeEnum, gradeB = (int)defB.GradeEnum;
            if (gradeA != gradeB) return gradeB - gradeA;
            int typeA = (int)defA.TypeEnum, typeB = (int)defB.TypeEnum;
            if (typeA != typeB) return typeA - typeB;
            return string.Compare(defA.name, defB.name, System.StringComparison.Ordinal);
        });
        Slots = new ItemInstance[MaxSlots];
        for (int i = 0; i < items.Count; i++) Slots[i] = items[i];
    }

    public ItemInstance[] GetFiltered(Dictionary<string, ItemDef> defs, string typeFilter, int sortMode)
    {
        var items = new List<ItemInstance>();
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != null) items.Add(Slots[i]);

        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "all")
        {
            var filtered = new List<ItemInstance>();
            for (int i = 0; i < items.Count; i++)
            {
                if (!defs.TryGetValue(items[i].itemId, out var def)) continue;
                bool match = typeFilter switch
                {
                    "weapon" => def.type == "weapon",
                    "armor" => def.type == "helmet" || def.type == "armor" || def.type == "boots" || def.type == "accessory",
                    "consumable" => def.type == "potion" || def.type == "food",
                    "material" => def.type == "material",
                    _ => true
                };
                if (match) filtered.Add(items[i]);
            }
            items = filtered;
        }

        if (sortMode == 3)
        {
            items.Reverse();
        }
        else
        {
            items.Sort((a, b) =>
            {
                defs.TryGetValue(a.itemId, out var defA);
                defs.TryGetValue(b.itemId, out var defB);
                if (defA == null && defB == null) return 0;
                if (defA == null) return 1;
                if (defB == null) return -1;
                return sortMode switch
                {
                    1 => ((int)defB.GradeEnum).CompareTo((int)defA.GradeEnum),
                    2 => ((int)defA.TypeEnum).CompareTo((int)defB.TypeEnum),
                    _ => string.Compare(defA.name, defB.name, System.StringComparison.Ordinal)
                };
            });
        }

        var result = new ItemInstance[items.Count];
        for (int i = 0; i < items.Count; i++) result[i] = items[i];
        return result;
    }
}
