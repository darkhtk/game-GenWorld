# Interface Contracts

All public method signatures. Do not change without Director approval.

## Core

### EventBus (static)
```csharp
static void On<T>(Action<T> handler)
static void Off<T>(Action<T> handler)
static void Emit<T>(T evt)
static void Clear()
```

### GameConfig (static constants)
```csharp
const int TileSize = 32
const int MapWidthTiles = 200, MapHeightTiles = 200
const int SkillSlotCount = 6, SkillMaxLevel = 5
const int EnhanceBonusPerLevel = 2
const float AutoAttackBaseRange = 56f, AutoAttackCooldown = 1000f
const float AutoAttackRangePerLevel = 2f, AutoAttackDmgPerLevel = 0.05f
const float AutoAttackArcHalf = PI/3
const float HpRegenPerSecond = 2f
const float DeathGoldPenalty = 0.1f
const float ChaseRangeMult = 2.5f, MaxSpawnDistance = 800f
static int XpForLevel(int level) => floor(50 * 1.3^(level-1))
static int EnhanceCost(int level) => 100 + 150 * level
static Color GetGradeColor(ItemGrade g)
```

### GameEvents (structs)
```csharp
MonsterKillEvent { monsterId, monsterName, killCount, totalKills }
PlayerDeathEvent { deathX, deathY }
LevelUpEvent { level, prevLevel }
QuestCompleteEvent { questTitle, completedCount }
QuestAcceptEvent { questId, npcId }
RegionVisitEvent { regionId, regionName }
ItemCollectEvent { itemId, count, totalCollected }
GoldChangeEvent { gold }
EquipChangeEvent { slot, itemId }
CombatPowerChangeEvent { power }
DialogueStartEvent { npcId }
DialogueEndEvent { npcId, turns }
NpcTriggerEvent { npcId, eventType, target }
SaveEvent { }
```

---

## Data

### DataManager
```csharp
Dictionary<string, ItemDef> Items
Dictionary<string, SkillDef> Skills
Dictionary<string, MonsterDef> Monsters
Dictionary<string, NpcDef> Npcs
Dictionary<string, QuestDef> Quests
Dictionary<string, RegionDef> Regions
Dictionary<string, SetBonusDef> SetBonuses
RecipeDef[] Recipes
Dictionary<string, string> NpcProfiles
ItemDef[] ItemList; SkillDef[] SkillList; MonsterDef[] MonsterList
NpcDef[] NpcList; QuestDef[] QuestList; RegionDef[] RegionList
void LoadAll()
```

---

## Systems

### InventorySystem
```csharp
InventorySystem(int maxSlots = 20)
ItemInstance[] Slots { get; }
int MaxSlots { get; }
int OccupiedSlots { get; }
ItemInstance GetSlot(int index)
int AddItem(string itemId, int count, bool stackable, int maxStack) // returns overflow
int GetCount(string itemId)
bool HasItems(string itemId, int count)
bool RemoveItem(string itemId, int count)
ItemInstance RemoveAtSlot(int index)
void SwapSlots(int a, int b)
void SortItems(Dictionary<string, ItemDef> itemDefs)
```

### StatsSystem (static)
```csharp
static Stats ComputeBaseStats(int level, Dictionary<string, int> bonusStats)
static Stats ComputeStats(Stats baseStats, Dictionary<string, ItemInstance> equipment,
    Dictionary<string, ItemDef> itemDefs, Dictionary<string, SetBonusDef> setBonuses)
static void AddXp(ref PlayerLevelState state, int amount)
```

### CombatSystem (static)
```csharp
static int CalcDamage(int atk, int def, bool isCrit)
static bool CalcCrit(int critChance, Func<float> roll = null)
static T FindClosest<T>(Vector2 origin, T[] targets, float range,
    Func<T, Vector2> getPos, Func<T, bool> isAlive) where T : class
```

### EffectHolder
```csharp
const float MaxStunMs = 10000f
const float MinSlow = 0.1f
void Apply(string type, float expiresAt, float value)
void ApplyDot(float expiresAt, float damage, float interval = 1000f)
bool Has(string type)
float GetValue(string type)
float GetExpiresAt(string type)
void Remove(string type)
List<string> Tick(float now)  // returns expired types
float TickDot(float now)      // returns damage or 0
void Clear()
```

### SkillSystem
```csharp
SkillSystem(Dictionary<string, SkillDef> defs)
int GetSkillLevel(string id)  // 0=not learned, 1-5
bool IsLearned(string id)
LearnResult LearnSkill(string skillId, int availablePoints, int playerLevel)
int ResetAllSkills()  // returns refunded points
float GetDamageMultiplier(string id)
float GetAoeBonus(string id)
float GetDurationBonus(string id)
float GetBuffBonus(string id)
bool EquipSkill(string skillId, int slot)
string GetEquippedSkill(int slot)
string[] GetEquippedSkills()
UseSkillResult UseSkill(int slot, int currentMp, float now)
float GetCooldownRemaining(string skillId, float now)
Dictionary<string, int> GetLearnedSkills()
void RestoreLearnedSkills(Dictionary<string, int> data)
void RestoreEquipped(string[] data)
```

### LootSystem (static)
```csharp
static List<DropResult> RollDrops(DropEntry[] drops, Func<float> roll = null)
```

### CraftingSystem
```csharp
CraftingSystem(RecipeDef[] recipes, Dictionary<string, ItemDef> itemDefs)
RecipeDef[] GetAvailableRecipes(InventorySystem inv)
bool CanCraft(string resultId, InventorySystem inv)
bool Craft(string resultId, InventorySystem inv)
RecipeDef[] AllRecipes { get; }
```

### QuestSystem
```csharp
QuestSystem(QuestDef[] defs)
bool AcceptQuest(string questId)
QuestReward CompleteQuest(string questId, InventorySystem inv)
QuestDef[] GetActiveQuests()
bool IsActive(string questId)
bool IsCompleted(string questId)
string GetStatus(string questId, InventorySystem inv)
(QuestDef quest, string status)? GetQuestStatusForNpc(string npcId, InventorySystem inv)
QuestDef PickQuestForNpc(string npcId, int rejectionCount, int relationship)
QuestReward GetScaledRewards(QuestDef quest, int rejectionCount, int relationship, float generosityMult = 1f)
(string[] active, string[] completed) Serialize()
void Restore((string[] active, string[] completed) data)
```

### SaveSystem (static)
```csharp
static void Save(SaveData data)
static SaveData Load()
static bool HasSave()
static void DeleteSave()
```

### SkillExecutor
```csharp
SkillExecutor()  // registers default handlers
void Register(string behavior, Action<SkillContext> handler)
void Execute(SkillContext ctx)
```

### ActionRunner
```csharp
ActionRunner()  // registers default handlers
void Register(string type, ActionHandler handler)
void Run(SkillAction[] actions, ActionContext ctx, float hitX, float hitY, List<MonsterController> hitTargets = null)
void ExecuteSkill(SkillAction[] actions, ActionContext ctx)
```

### CombatManager (MonoBehaviour)
```csharp
void PerformAutoAttack(List<MonsterController> monsters)
void HandleMonsterAttacks(List<MonsterController> monsters, float now)
void ExecuteSkill(int slot)
void ShowDamageNumber(Vector2 pos, int amount, bool isCrit, Color? color = null)
void ShowFloatingText(Vector2 pos, string msg, Color? color = null)
```

---

## Entities

### PlayerController (MonoBehaviour)
```csharp
Vector2 AimDirection { get; }
Vector2 Position { get; }  // (Vector2)transform.position
string Facing { get; }     // "up" "down" "left" "right"
bool Frozen { get; set; }
bool IsDodging { get; }
bool Invincible { get; }
void SetSpeed(float speed)
float GetDodgeCooldownFraction()  // 0-1
```

### PlayerStats
```csharp
int Level, Xp, Gold, SkillPoints, StatPoints
Dictionary<string, int> BonusStats  // str, dex, wis, luc
Stats CurrentStats
int Hp, Mp
Dictionary<string, ItemInstance> Equipment  // weapon, helmet, armor, boots, accessory
void RecalcStats(Dictionary<string, ItemDef> itemDefs, Dictionary<string, SetBonusDef> setBonuses)
void FullHeal()
```

### MonsterController (MonoBehaviour)
```csharp
MonsterDef Def { get; }
int Hp { get; set; }
bool IsDead { get; }         // Hp <= 0
Vector2 Position { get; }
EffectHolder Effects { get; }
MonsterAIState AIState { get; }
int EffectiveAtk { get; }    // Def.atk * atkMult
int EffectiveDef { get; }    // Def.def * defMult
void Init(MonsterDef def, Vector2 spawnPos)
void UpdateAI(Vector2 playerPos, float now)
bool TakeDamage(int dmg)     // returns true if died
bool CanAttack(float now)
void MarkAttacked(float now)
```

### MonsterSpawner (MonoBehaviour)
```csharp
List<MonsterController> ActiveMonsters { get; }
void SpawnForRegion(RegionDef region, Dictionary<string, MonsterDef> monsterDefs, WorldMapGenerator worldMap)
void RemoveMonster(MonsterController m)
```

### VillageNPC (MonoBehaviour)
```csharp
NpcDef Def { get; }
NPCBrain Brain { get; set; }
Vector2 Position { get; }
bool IsStopped { get; set; }
void Init(NpcDef def, Vector2 position)
bool IsInInteractionRange(Vector2 playerPos, float range = 48f)
void StopMoving()
void ResumeMoving()
```

### Projectile (MonoBehaviour)
```csharp
void Init(Vector2 from, Vector2 to, float speed, int color, float size, bool piercing)
Action<MonsterController> OnHitMonster
Action<Vector2> OnArrive
```

---

## AI

### NPCBrain
```csharp
NPCBrain(string npcId, string npcName, string personality)
string NpcId { get; }
string NpcName { get; }
Mood CurrentMood { get; set; }
bool WantToTalk { get; set; }
string TalkReason { get; set; }
string Profile { get; set; }
int GetRelationship(string targetId)     // -100 to 100
void UpdateRelationship(string targetId, int change)
void AddMemory(string eventText, int importance)
List<MemoryEntry> GetTopMemories(int count)
NPCBrainData Serialize()
void Restore(NPCBrainData data)
```

### AIManager
```csharp
AIManager(OllamaClient client = null)
Task Init()
void RegisterNpc(string npcId, string name, string personality, string profile = null)
NPCBrain GetBrain(string npcId)
void UpdateBehavior(string playerRegion, float playerX, float playerY,
    Dictionary<string, Vector2> npcPositions)
Task<DialogueResponse> GenerateDialogue(string npcId, string playerInput,
    List<DialogueEntry> history, int playerLevel, int playerGold,
    InventorySystem inventory, Dictionary<string, ItemDef> itemDefs,
    QuestSystem questSystem, string loreContext = null,
    string[] npcActions = null, DialogueTraits traits = null)
Dictionary<string, NPCBrainData> SerializeAllBrains()
void RestoreAllBrains(Dictionary<string, NPCBrainData> data)
```

### OllamaClient
```csharp
OllamaClient(string url = "http://localhost:11434",
    string fastModel = "gemma3:4b", string largeModel = "gemma3:12b")
Task<bool> CheckAvailability()
Task<string> GenerateDialogue(string prompt)
```

### PromptBuilder (static)
```csharp
static readonly string SYSTEM_RULES
static string BuildDialoguePrompt(NPCBrain brain, DialogueContext ctx)
```

---

## UI

All UI classes: MonoBehaviour, attached to Canvas child.
```csharp
public void Show() / Open(...)
public void Hide() / Close()
public void Toggle(...)
public void Refresh(...)
```

### UIManager (MonoBehaviour)
```csharp
HUD Hud; InventoryUI Inventory; ShopUI Shop; CraftingUI Crafting
EnhanceUI Enhance; SkillTreeUI SkillTree; QuestUI Quest
DialogueUI Dialogue; NpcProfilePanel NpcProfile; NpcQuestPanel NpcQuest
PauseMenuUI PauseMenu
void HideAll()
```

---

## Map

### WorldMapGenerator (MonoBehaviour)
```csharp
[SerializeField] Tilemap groundTilemap
[SerializeField] Tilemap collisionTilemap
void Generate(RegionDef[] regions)
bool IsWalkable(int tileX, int tileY)
bool IsVillageTile(int tileX, int tileY)
```

### RegionTracker
```csharp
RegionTracker(RegionDef[] regions)
string CurrentRegionId { get; }
RegionDef GetRegionAt(float worldX, float worldY)
void UpdatePlayerRegion(float worldX, float worldY)
```
