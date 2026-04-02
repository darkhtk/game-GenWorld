# Question from Dev-Frontend

## Date: 2026-04-02
## To: Director
## Status: OPEN

## Request: DeathMarker wiring in GameManager

`Assets/Scripts/Effects/DeathMarker.cs` created. Needs wiring in `GameManager.OnPlayerDeath()`:

### 1. Drop items on death
```csharp
void OnPlayerDeath()
{
    // existing gold penalty...
    
    // Drop all inventory items at death location
    var items = new List<ItemInstance>();
    for (int i = 0; i < Inventory.MaxSlots; i++)
    {
        var item = Inventory.RemoveAtSlot(i);
        if (item != null) items.Add(item);
    }
    
    if (items.Count > 0)
        DeathMarker.Create(player.Position, items);
    
    // existing FullHeal, events...
}
```

### 2. E-key recovery (in Update or interaction handler)
```csharp
// When E pressed near a DeathMarker
var marker = FindObjectOfType<DeathMarker>();
if (marker != null && marker.IsInRange(player.Position) && marker.HasItems)
{
    var items = marker.Recover();
    foreach (var item in items)
        Inventory.AddItem(item.itemId, item.count, ...);
}
```

### Features
- † symbol with red color, bobbing animation
- Blinks when < 10 seconds remaining  
- Auto-destroys after 120 seconds (items lost)
- 48px interaction range
