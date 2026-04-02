# System Architecture

## Scene Flow

```
BootScene -> MainMenuScene -> GameScene
                               |- WorldMapGenerator (Tilemap)
                               |- PlayerController
                               |- MonsterSpawner -> MonsterController[]
                               |- VillageNPC[]
                               |- Canvas (UI)
                               |   |- HUD
                               |   |- InventoryUI
                               |   |- ShopUI
                               |   |- CraftingUI
                               |   |- EnhanceUI
                               |   |- SkillTreeUI
                               |   |- QuestUI
                               |   |- DialogueUI
                               |   |- NpcProfilePanel
                               |   |- NpcQuestPanel
                               |   |- PauseMenuUI
                               +-- GameManager (orchestrates everything)
```

## System Dependency Graph

```
GameManager (central orchestrator)
|- DataManager          <- loads all JSON at boot
|- PlayerStats          <- level, xp, gold, equipment, bonusStats
|- InventorySystem      <- slot-based, 20 slots
|- SkillSystem          <- learn/equip/cooldown, 6 slots
|- CraftingSystem       <- depends on: InventorySystem, DataManager.Items
|- QuestSystem          <- depends on: InventorySystem (item checks)
|- StatsSystem (static) <- depends on: equipment, DataManager.SetBonuses
|- CombatSystem (static)<- pure damage calc
|- CombatManager        <- depends on: PlayerController, MonsterController[], EffectHolder
|- SkillExecutor        <- depends on: CombatSystem, MonsterController[]
|- ActionRunner         <- depends on: CombatSystem, MonsterController[], SkillExecutor
|- LootSystem (static)  <- pure drop roll
|- EffectSystem         <- EffectHolder per entity (player + each monster)
|- SaveSystem (static)  <- serializes all state to JSON file
|- RegionTracker        <- depends on: DataManager.Regions
|- AIManager            <- depends on: OllamaClient, PromptBuilder, NPCBrain[]
|   |- NPCBrain (per NPC) <- mood, relationship, memory
|   |- OllamaClient    <- HTTP to localhost:11434
|   +-- PromptBuilder (static) <- prompt templates
+-- EventBus (static)    <- typed events, global pub/sub
```

## Data Flow: Combat

```
Player Input (mouse aim)
    |
PlayerController.Update() -> AimDirection
    |
CombatManager.PerformAutoAttack() -- every 1000ms
    |
CombatSystem.CalcDamage(atk, def, isCrit)
    |
MonsterController.TakeDamage(dmg)
    | (if dead)
LootSystem.RollDrops(monster.drops)
    |
InventorySystem.AddItem(...)
    |
EventBus.Emit(MonsterKillEvent)
    |
QuestSystem (checks kill requirements)
AIManager (checks NPC triggers)
```

## Data Flow: Skill Use

```
Player presses 1-6
    |
SkillSystem.UseSkill(slot, mp, now)
    | (success)
PlayerStats.Mp -= mpCost
    |
skill.actions != null?
    |- Yes -> ActionRunner.ExecuteSkill(actions, ctx)
    +- No  -> SkillExecutor.Execute(ctx)
                  |
              Behavior dispatch (aoe_damage, single_target, self_buff, etc.)
                  |
              CombatSystem.CalcDamage + Monster.TakeDamage
              EffectHolder.Apply (stun/slow/dot)
              SkillVFX.Show
```

## Data Flow: NPC Dialogue

```
Player presses E near NPC
    |
PlayerController.Frozen = true
    |
DialogueUI.Show(npcDef)
    |
AIManager.GenerateDialogue(npcId, playerInput, history, ...)
    |
PromptBuilder.BuildDialoguePrompt(brain, ctx)
    |
OllamaClient.GenerateDialogue(prompt)
    |
Parse JSON response -> DialogueResponse
    |
DialogueUI renders: dialogue text + options + action buttons
    | (player closes)
PlayerController.Frozen = false
SaveSystem.Save(...)
```

## 14 Systems Summary

| System | Type | Responsibility |
|--------|------|---------------|
| EventBus | static class | Typed event pub/sub |
| InventorySystem | class | 20-slot inventory, stack/unstack |
| StatsSystem | static | Compute stats: base + equipment + set + enhance |
| CombatSystem | static | Damage formula: max(1, atk-def), crit x2 |
| CombatManager | MonoBehaviour | Auto-attack loop, monster attacks, damage numbers |
| SkillSystem | class | Learn/equip/cooldown/scaling |
| SkillExecutor | class | Behavior-based skill dispatch |
| ActionRunner | class | Action pipeline: deal_damage, apply_effect, spawn_projectile, etc. |
| EffectSystem | class | EffectHolder: stun, slow, dot, buffs with expiry |
| LootSystem | static | Probability-based item drops |
| CraftingSystem | class | Recipe validation and crafting |
| QuestSystem | class | Quest state: available->active->completable->completed |
| SaveSystem | static | JSON file persistence |
| AIManager | class | NPC brain registry, Ollama dialogue generation |
