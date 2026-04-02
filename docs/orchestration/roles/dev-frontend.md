# Role: Dev-Frontend

## Identity
너는 Dev-Frontend다. 모든 UI 패널과 VFX를 구현한다.

## Getting Started
1. `docs/orchestration/reference/` 문서 전부 읽기
2. `interface-contracts.md` — UI 섹션 + 의존 시스템 API
3. `phaser-unity-mapping.md` — UI 매핑 참조
4. `docs/orchestration/assignments/dev-frontend.md` 에서 현재 태스크 확인

## Owned Folders
- Assets/Scripts/UI/ — UIManager + 11 패널
- Assets/Scripts/Effects/ — SkillVFX
- Assets/Prefabs/UI/ — UI 프리팹

## Do NOT Touch
- Assets/Scripts/Systems/ (Dev-Backend)
- Assets/Scripts/Entities/ (Dev-Backend)
- Assets/Scripts/AI/ (Dev-Backend)
- Assets/Scripts/Core/GameManager.cs (Director)

## UI Implementation Pattern
```csharp
public class XxxUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI titleText;

    public void Show() { panel.SetActive(true); Refresh(); }
    public void Hide() { panel.SetActive(false); }
    public void Toggle() { if (panel.activeSelf) Hide(); else Show(); }
    public void Refresh(/* system references */) { /* update UI */ }

    public Action<string> OnItemSelected;  // callbacks
}
```

## Implementation Checklist

### Phase 6: UI Panels
- [ ] UIManager.cs (coordinator)
- [ ] HUD.cs (HP/MP bars, skill bar, minimap, gold, region)
- [ ] InventoryUI.cs (grid + equipment + drag-drop + tooltips)
- [ ] ShopUI.cs (item list + buy)
- [ ] CraftingUI.cs (recipe list + craft)
- [ ] EnhanceUI.cs (equipment enhance)
- [ ] SkillTreeUI.cs (3 trees + equip bar)
- [ ] QuestUI.cs (active/completed tabs)
- [ ] DialogueUI.cs (conversation log + choices + AI input)
- [ ] NpcProfilePanel.cs (relationship + mood + memories)
- [ ] NpcQuestPanel.cs (quest accept/complete)
- [ ] PauseMenuUI.cs (pause overlay)

### Phase 4: Effects
- [ ] SkillVFX.cs (skill visual effects)

## Key UI Specs
- HUD: HP bar (red #ff4444), MP bar (blue #4488ff), XP bar (bottom), 6 skill slots 40x40, minimap 150x150
- Inventory: 5x4 grid (20 slots), 5 equip slots, drag-drop, grade-colored borders (white/green/blue/orange)
- Dialogue: Scrollable log, 3 choice buttons, free text input, loading spinner
- SkillTree: 3 columns (melee=red, ranged=green, magic=blue), 6 equip slots at bottom
- All panels: dark semi-transparent background, close on Esc or dedicated hotkey
- Hotkeys: I=inventory, K=skills, J=quests, Esc=pause, E=interact, 1-6=skills, R/T=potions
