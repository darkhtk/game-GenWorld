# SPEC-B-007: Inventory Screen Audit

> **Priority:** P0 (user bug report)
> **Symptom:** Inventory screen needs full check.

---

## Key Files

| Component | File |
|-----------|------|
| InventoryUI | `Assets/Scripts/UI/InventoryUI.cs` |
| EquipSlotUI | (search for `class EquipSlotUI`) |
| InventorySystem | `Assets/Scripts/Systems/InventorySystem.cs` |
| items.json | `Assets/StreamingAssets/Data/items.json` |

## Diagnostic Checklist

### 1. Panel Open/Close
- Toggle key (I or Tab) → `panel.SetActive` works?
- `closeButton` wired to `Hide()`?

### 2. Item Grid Display
- `gridContent` Transform + `slotPrefab` — slots instantiated correctly?
- Item icon sprites loading? Check if `ItemDef` has icon field → `Resources.Load<Sprite>()`.
- Grade frame (A-002: `grade_frame_common/uncommon/rare/epic/legendary`) applied?
- Item count text visible for stackable items?

### 3. Equipment Slots
- 5 slots: weapon, helmet, armor, boots, accessory.
- Equip callback: `OnEquipCallback(slotIndex)` → `InventorySystem.Equip()`.
- Unequip: `OnUnequipCallback(slot)` → `InventorySystem.Unequip()`.
- Stats panel updates on equip/unequip (ATK/DEF/SPD/CRIT/HP/MP).

### 4. Stat Allocation
- STR/DEX/WIS/LUC buttons + text visible?
- Available stat points display?
- Allocation callback works?

### 5. Filter / Sort
- `filterButtons[]` — All/Weapon/Armor/Consumable/Material?
- `sortModeButton` + `sortModeText` — Name/Level/Rarity?
- `OnSortCallback` fires on button click?

### 6. Item Use
- `OnUseItemCallback(index)` → potion/consumable use.
- Item count decrement after use.

### 7. Data Loading
- `items.json` parsed by DataManager → `Data.ItemList` populated?
- `ItemDef` fields: id, name, type, icon, stats, grade, etc.

## Fix Direction
1. Verify panel toggle key exists in PlayerController or UIManager.
2. Check slotPrefab has Image component for item icon.
3. Verify all SerializeField references are connected in scene/prefab.
4. Test equip → stat update → unequip cycle.

## Verification
- Inventory opens with correct item icons and counts.
- Equip/unequip updates character stats.
- Filter/sort works correctly.
- Use consumable decrements count.
