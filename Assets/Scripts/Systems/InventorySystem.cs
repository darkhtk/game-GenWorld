using System.Collections.Generic;
using System.Linq;

public class InventorySystem
{
    public ItemInstance[] Slots { get; private set; }
    public int MaxSlots { get; }
    public int OccupiedSlots => Slots.Count(s => s != null);

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
        var items = Slots.Where(s => s != null).ToList();
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
}
