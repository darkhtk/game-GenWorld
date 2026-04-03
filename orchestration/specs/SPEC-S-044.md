# SPEC-S-044: 장비 교체 시 스탯 복원 검증

> **Priority:** P1
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## 목적

장비를 교체(equip)하거나 해제(unequip)할 때, 이전 장비의 스탯 보너스가 완전히 제거되고 새 장비의 보너스가 정확하게 적용되는지 검증한다. 장비 교체 과정에서 스탯이 누적되거나 유실되는 버그가 없는지 확인한다.

## 현재 상태

### 장비 교체 흐름 (GameManager.cs line 574-589)

```csharp
inv.OnEquipCallback = slotIdx =>
{
    var item = Inventory.GetSlot(slotIdx);
    if (item == null || !Data.Items.TryGetValue(item.itemId, out var def)) return;
    string slot = ItemTypeUtil.GetEquipSlot(def.TypeEnum);
    if (string.IsNullOrEmpty(slot)) return;
    // Unequip current
    if (PlayerState.Equipment.TryGetValue(slot, out var old) && old != null)
        Inventory.AddItem(old.itemId, 1, false, 1);
    PlayerState.Equipment[slot] = item;
    Inventory.RemoveAtSlot(slotIdx);
    PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
    player.SetSpeed(PlayerState.CurrentStats.spd);
    EventBus.Emit(new EquipChangeEvent());
    AudioManager.Instance?.PlaySFX("sfx_confirm");
    inv.Refresh();
};
```

### 장비 해제 흐름 (GameManager.cs line 591-601)

```csharp
inv.OnUnequipCallback = slot =>
{
    if (!PlayerState.Equipment.TryGetValue(slot, out var item) || item == null) return;
    bool stackable = Data.Items.TryGetValue(item.itemId, out var def) && def.stackable;
    Inventory.AddItem(item.itemId, 1, stackable, def?.maxStack ?? 1);
    PlayerState.Equipment[slot] = null;
    PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
    player.SetSpeed(PlayerState.CurrentStats.spd);
    EventBus.Emit(new EquipChangeEvent());
    AudioManager.Instance?.PlaySFX("sfx_confirm");
    inv.Refresh();
};
```

### 스탯 계산 방식 (StatsSystem.cs)

```csharp
public static Stats ComputeStats(
    Stats baseStats,
    Dictionary<string, ItemInstance> equipment,
    Dictionary<string, ItemDef> itemDefs,
    Dictionary<string, SetBonusDef> setBonuses)
{
    var result = baseStats;
    // Iterate ALL equipped items and add their stats
    foreach (var kvp in equipment)
    {
        if (kvp.Value == null) continue;
        if (!itemDefs.TryGetValue(kvp.Value.itemId, out var def)) continue;
        result = result + def.stats.ToStats();
        if (kvp.Value.enhanceLevel > 0)
            result.atk += kvp.Value.enhanceLevel * GameConfig.EnhanceBonusPerLevel;
        // Set bonus tracking...
    }
    // Apply set bonuses...
    return result;
}
```

**핵심 설계:** `StatsSystem.ComputeStats()`는 매번 `baseStats`에서 시작하여 현재 장착된 모든 장비의 스탯을 새로 합산한다. 이전 장비의 스탯을 "빼는" 로직은 없다. 대신 `RecalcStats()`가 전체 재계산을 수행한다.

```csharp
// PlayerStats.cs line 12-18
public void RecalcStats(Dictionary<string, ItemDef> itemDefs, Dictionary<string, SetBonusDef> setBonuses)
{
    var baseStats = StatsSystem.ComputeBaseStats(Level, BonusStats);
    CurrentStats = StatsSystem.ComputeStats(baseStats, Equipment, itemDefs, setBonuses);
    Hp = Mathf.Min(Hp, CurrentStats.maxHp);
    Mp = Mathf.Min(Mp, CurrentStats.maxMp);
}
```

이 설계는 "전체 재계산" 패턴이므로 이론적으로 장비 교체 시 스탯 누적/유실이 불가능하다. 그러나 다음 잠재 문제를 검증해야 한다.

### 잠재 문제 1: 장비 교체 시 이전 장비 인벤토리 복원 순서

```csharp
// Equip flow:
Inventory.AddItem(old.itemId, 1, false, 1);  // (1) Return old to inventory
PlayerState.Equipment[slot] = item;           // (2) Set new equipment
Inventory.RemoveAtSlot(slotIdx);              // (3) Remove new from inventory
PlayerState.RecalcStats(...);                  // (4) Recalculate
```

단계 (1)에서 이전 장비를 인벤토리에 `stackable=false`로 반환한다. 그런데 장비 아이템은 항상 non-stackable인가? `ItemDef.stackable`을 확인해야 한다. 만약 장비 아이템이 `stackable=true`로 설정되어 있다면 `AddItem(old.itemId, 1, false, 1)`의 `stackable=false` 하드코딩이 올바르지 않을 수 있다 (장비는 보통 non-stackable이므로 현실적 위험은 낮음).

### 잠재 문제 2: 인벤토리가 가득 찬 상태에서 장비 교체

이전 장비를 인벤토리에 반환할 때 (`AddItem`), 인벤토리가 가득 차면 `remaining > 0`이 반환된다. 그러나 이 반환값을 체크하지 않고 새 장비를 장착한다. 결과적으로 이전 장비가 유실된다.

```csharp
// Current: return value ignored
Inventory.AddItem(old.itemId, 1, false, 1);  // What if inventory is full?
PlayerState.Equipment[slot] = item;            // New item equipped regardless
Inventory.RemoveAtSlot(slotIdx);               // New item removed from inventory
// Old item is LOST if AddItem returned remaining > 0
```

### 잠재 문제 3: enhanceLevel 이관

이전 장비의 `enhanceLevel`이 보존되는가? `Inventory.AddItem()`은 `new ItemInstance { itemId, count }`를 생성하므로 `enhanceLevel = 0`으로 초기화된다. 이전 장비의 강화 수치가 유실된다.

```csharp
// AddItem creates new ItemInstance (InventorySystem.cs line 48):
Slots[i] = new ItemInstance { itemId = itemId, count = add };
// enhanceLevel defaults to 0!
```

그러나 equip 콜백에서는 `AddItem()`이 아닌 직접 `ItemInstance` 참조를 사용해야 `enhanceLevel`이 보존된다. 현재 코드는 `AddItem(old.itemId, 1, false, 1)`로 새 인스턴스를 생성하므로 **enhanceLevel이 유실된다**.

### 잠재 문제 4: HP/MP 클램프

`RecalcStats()` 마지막에 `Hp = Min(Hp, maxHp)`, `Mp = Min(Mp, maxMp)`로 클램프한다. 장비 해제로 `maxHp`가 감소하면 `Hp`도 감소한다. 이는 의도된 동작이지만, 장비 재장착 시 `Hp`가 자동 복구되지 않으므로 사용자가 혼란스러울 수 있다. (이는 디자인 결정이므로 버그는 아님)

### 잠재 문제 5: 세트 보너스 계산

장비 교체 시 세트 보너스 `setCounts`가 변경될 수 있다. 예: 3pc 세트에서 1개를 다른 장비로 교체하면 2pc로 감소. `ComputeStats()`가 매번 전체 재계산하므로 세트 보너스도 올바르게 갱신된다. 단, 교체 전후의 세트 보너스 변화를 UI에서 미리 보여주지 않는 문제가 있다 (compare popup에서 세트 보너스 diff 미표시).

## 검증 항목

- [ ] 장비 교체 시 이전 장비의 `enhanceLevel`이 인벤토리에 보존되는지 확인 (**BUG 의심**)
- [ ] 인벤토리가 가득 찬 상태에서 장비 교체 시 이전 장비가 유실되지 않는지 확인 (**BUG 의심**)
- [ ] 장비 장착 후 `CurrentStats`가 base + 모든 장비 스탯과 일치하는지 확인
- [ ] 장비 해제 후 `CurrentStats`가 base + 나머지 장비 스탯과 일치하는지 확인
- [ ] 장비 교체 후 `player.SetSpeed()`가 새 `spd` 값으로 호출되는지 확인
- [ ] 세트 보너스 3pc -> 2pc 감소 시 3pc 보너스가 제거되는지 확인
- [ ] HP/MP 클램프가 장비 해제 시 올바르게 동작하는지 확인
- [ ] `EquipChangeEvent`가 정확히 1회 발생하는지 확인
- [ ] InventoryUI가 교체 후 정확한 스탯을 표시하는지 확인

## 수정 방안 (필요 시)

### 1. enhanceLevel 보존 (Critical Bug Fix)

이전 장비를 인벤토리에 반환할 때 `AddItem()` 대신 `ItemInstance` 참조를 직접 삽입:

```csharp
inv.OnEquipCallback = slotIdx =>
{
    var item = Inventory.GetSlot(slotIdx);
    if (item == null || !Data.Items.TryGetValue(item.itemId, out var def)) return;
    string slot = ItemTypeUtil.GetEquipSlot(def.TypeEnum);
    if (string.IsNullOrEmpty(slot)) return;

    // Unequip current — preserve enhanceLevel
    ItemInstance old = null;
    if (PlayerState.Equipment.TryGetValue(slot, out var equipped) && equipped != null)
        old = equipped;

    PlayerState.Equipment[slot] = item;
    Inventory.RemoveAtSlot(slotIdx);

    // Return old item to the now-freed slot (or first empty)
    if (old != null)
    {
        bool placed = false;
        if (slotIdx < Inventory.MaxSlots && Inventory.GetSlot(slotIdx) == null)
        {
            Inventory.Slots[slotIdx] = old;  // Place in the slot we just freed
            placed = true;
        }
        if (!placed)
        {
            // Find first empty slot
            for (int i = 0; i < Inventory.MaxSlots; i++)
            {
                if (Inventory.GetSlot(i) == null)
                {
                    Inventory.Slots[i] = old;
                    placed = true;
                    break;
                }
            }
        }
        if (!placed)
            Debug.LogError($"[GameManager] Inventory full: cannot return unequipped {old.itemId}+{old.enhanceLevel}");
    }

    PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
    player.SetSpeed(PlayerState.CurrentStats.spd);
    EventBus.Emit(new EquipChangeEvent());
    AudioManager.Instance?.PlaySFX("sfx_confirm");
    inv.Refresh();
};
```

### 2. Unequip에서도 enhanceLevel 보존

현재 unequip 콜백도 `AddItem()`을 사용한다:

```csharp
Inventory.AddItem(item.itemId, 1, stackable, def?.maxStack ?? 1);
```

이것도 `enhanceLevel`을 유실한다. 동일하게 직접 `Slots` 삽입으로 변경:

```csharp
inv.OnUnequipCallback = slot =>
{
    if (!PlayerState.Equipment.TryGetValue(slot, out var item) || item == null) return;
    PlayerState.Equipment[slot] = null;

    // Place item directly to preserve enhanceLevel
    bool placed = false;
    for (int i = 0; i < Inventory.MaxSlots; i++)
    {
        if (Inventory.GetSlot(i) == null)
        {
            Inventory.Slots[i] = item;
            placed = true;
            break;
        }
    }
    if (!placed)
        Debug.LogError($"[GameManager] Inventory full: cannot return unequipped {item.itemId}+{item.enhanceLevel}");

    PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
    player.SetSpeed(PlayerState.CurrentStats.spd);
    EventBus.Emit(new EquipChangeEvent());
    AudioManager.Instance?.PlaySFX("sfx_confirm");
    inv.Refresh();
};
```

### 3. 인벤토리 풀 방어

장비 교체 전 인벤토리에 빈 슬롯이 있는지 확인. 교체(swap)는 항상 1:1 이므로 `RemoveAtSlot` 후 빈 슬롯이 생기지만, unequip-only의 경우 빈 슬롯이 필요하다:

```csharp
inv.OnUnequipCallback = slot =>
{
    if (!PlayerState.Equipment.TryGetValue(slot, out var item) || item == null) return;
    if (Inventory.OccupiedSlots >= Inventory.MaxSlots)
    {
        Debug.LogWarning("[GameManager] Cannot unequip: inventory full");
        // TODO: Show UI warning to player
        return;
    }
    // ... proceed ...
};
```

## 호출 진입점

| 트리거 | 경로 |
|--------|------|
| 장비 장착 | `InventoryUI.OnSlotClicked()` -> `ShowComparePopup()` -> `ConfirmEquip()` -> `OnEquipCallback` -> `GameManager` lambda |
| 장비 해제 | `EquipSlotUI.unequipButton` click -> `OnUnequip` -> `OnUnequipCallback` -> `GameManager` lambda |
| 자동 장비 (미구현) | N/A (현재 없음) |

## 연동 시스템

| 시스템 | 역할 |
|--------|------|
| `GameManager` | Equip/Unequip 콜백 정의, `RecalcStats` 호출 |
| `PlayerStats` | `Equipment` dict 보유, `RecalcStats()` 실행 |
| `StatsSystem` | `ComputeBaseStats()` + `ComputeStats()` 전체 재계산 |
| `InventorySystem` | 아이템 슬롯 관리, `AddItem()` / `RemoveAtSlot()` |
| `InventoryUI` | 장비 슬롯 UI, compare popup, 스탯 표시 |
| `EventBus` | `EquipChangeEvent` 전파 |
| `HUD` | `EquipChangeEvent` 수신 후 전투력 표시 갱신 |

## 데이터 구조

### Equipment dictionary (PlayerStats)

```csharp
public Dictionary<string, ItemInstance> Equipment = new()
{
    ["weapon"] = null, ["helmet"] = null, ["armor"] = null,
    ["boots"] = null, ["accessory"] = null
};
```

### ItemDef stats (JSON)

```json
{
    "id": "iron_sword",
    "type": "weapon",
    "stats": { "atk": 12, "def": 0, "maxHp": 0, "maxMp": 0, "spd": 0, "crit": 2 },
    "setId": "iron_set"
}
```

### ItemInstance (equipped item)

```csharp
public class ItemInstance
{
    public string itemId;
    public int count = 1;
    public int enhanceLevel;  // <-- LOST during equip/unequip with current AddItem()
}
```

### SetBonusDef (JSON)

```json
{
    "iron_set": {
        "name": "Iron Set",
        "2pc": { "def": 5 },
        "3pc": { "def": 10, "maxHp": 20 }
    }
}
```

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Core/GameManager.cs` | `OnEquipCallback` / `OnUnequipCallback` 에서 `AddItem()` 대신 직접 슬롯 삽입으로 `enhanceLevel` 보존 |
| `Assets/Scripts/Core/GameManager.cs` | Unequip 시 인벤토리 풀 체크 추가 |

## Acceptance Criteria

1. +5 강화 무기를 다른 무기로 교체 시, 이전 무기가 인벤토리에 `enhanceLevel=5`로 복원
2. 장비 해제 시 `enhanceLevel`이 보존된 상태로 인벤토리에 반환
3. 인벤토리가 가득 찬 상태에서 unequip 시도 시 거부 + 경고 표시
4. 장비 교체(swap) 시 인벤토리 풀이어도 정상 동작 (1:1 교환이므로 슬롯 확보됨)
5. 교체 후 `CurrentStats`가 정확히 `base + equipped items` 와 일치
6. 세트 보너스 증감이 교체 후 정확히 반영
7. HP/MP가 maxHp/maxMp 감소 시 올바르게 클램프

## 테스트

### Edit Mode Unit Test

- `StatsSystem.ComputeStats()`에 장비 A 장착 -> 해제 -> 재장착 시 스탯 일치 검증
- `InventorySystem.AddItem()` vs 직접 슬롯 삽입 시 `enhanceLevel` 보존 비교
- 인벤토리 풀 상태에서 unequip 시 반환값/동작 검증
- 세트 보너스 3pc -> 2pc 전환 시 스탯 diff 검증

### Play Mode / 수동 테스트

1. 강화된 무기(+3) 장착 -> 다른 무기로 교체 -> 이전 무기 인벤토리 확인 -> enhanceLevel=3 확인
2. 인벤토리 19/20 상태에서 장비 교체 -> 정상 교체 확인
3. 인벤토리 20/20 상태에서 장비 해제 시도 -> 거부 확인
4. 3pc 세트 장비 중 1개 교체 -> 스탯 패널에서 세트 보너스 감소 확인
5. HP 150/200 상태에서 maxHp+50 장비 해제 -> HP 150/150 확인
