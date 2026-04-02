# Coding Standards

## Naming
| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `InventorySystem` |
| Public method | PascalCase | `AddItem()` |
| Public property | PascalCase | `MaxSlots` |
| Private field | _camelCase | `_learned` |
| Parameter | camelCase | `itemId` |
| Local variable | camelCase | `totalCount` |
| Constant | PascalCase | `SkillMaxLevel` |
| Enum | PascalCase | `ItemGrade.Legendary` |
| Interface | IPascalCase | `ITargetable` |

## File Organization
- One class per file (except small related types like struct + enum)
- File name = class name
- Max 300 lines recommended; split at 400
- Group: fields, constructor, public methods, private methods

## System Classes (non-MonoBehaviour)
```csharp
// Pure C# — no Unity dependency except Mathf/Vector2
public class InventorySystem
{
    readonly int _maxSlots;
    public InventorySystem(int maxSlots) { ... }
    public int AddItem(string itemId, int count, bool stackable, int maxStack) { ... }
}
```

## Entity Classes (MonoBehaviour)
```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour
{
    public MonsterDef Def { get; private set; }
    public void Init(MonsterDef def, Vector2 spawnPos) { ... }
    void Update() { ... }
}
```

## UI Classes (MonoBehaviour on Canvas child)
```csharp
public class InventoryUI : MonoBehaviour
{
    [SerializeField] Transform slotContainer;
    public void Show() { ... }
    public void Hide() { ... }
    public void Refresh(InventorySystem inventory, ...) { ... }
}
```

## Data Classes (Serializable POCO)
```csharp
[Serializable]
public class ItemDef
{
    public string id;
    public string name;
    // Newtonsoft.Json handles serialization
}
```

## Tests (EditMode, NUnit)
```csharp
public class InventorySystemTests
{
    InventorySystem inv;
    [SetUp] public void Setup() => inv = new InventorySystem(20);

    [Test]
    public void AddItem_Stackable_FillsExistingStack()
    {
        inv.AddItem("wood", 5, true, 99);
        inv.AddItem("wood", 3, true, 99);
        Assert.AreEqual(8, inv.GetCount("wood"));
    }
}
```

## Commit Messages
```
feat: implement InventorySystem with tests
fix: correct damage calculation for critical hits
refactor: extract EffectHolder from Monster
```

## Things to Avoid
- public fields on MonoBehaviour (use [SerializeField] private + property)
- GameObject.Find() at runtime
- Resources.Load() for game data (use StreamingAssets + File.ReadAllText)
- Korean in code/comments
- Static mutable state (except EventBus, GameConfig)
- async void (use async Task or Coroutine)
