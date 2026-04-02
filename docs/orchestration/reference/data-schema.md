# Data Schema Reference

All game data: `Assets/StreamingAssets/Data/`. Loaded by `DataManager.LoadAll()` using `Newtonsoft.Json`.

## items.json

### Root: `{ "items": [], "setBonuses": {}, "recipes": [] }`

### ItemDef
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| id | string | yes | Unique ID: "wooden_sword" |
| name | string | yes | Display name (Korean) |
| type | string | yes | "weapon" "helmet" "armor" "boots" "accessory" "material" "potion" |
| grade | string | yes | "common" "uncommon" "rare" "legendary" |
| description | string | yes | Flavor text |
| icon | string | yes | Sprite frame key |
| stackable | bool | yes | Can stack in inventory |
| maxStack | int | yes | Max stack size (1-99) |
| stats | object | no | { atk, def, maxHp, maxMp, spd, crit } all int, all optional |
| setId | string | no | Equipment set ID |
| healHp | int | no | HP restored (potions) |
| healMp | int | no | MP restored (potions) |
| shopPrice | int | no | Buy price (0 = not sold) |

### SetBonusDef
| Field | Type | Description |
|-------|------|-------------|
| name | string | Set display name |
| 2pc | ItemStats | 2-piece bonus |
| 3pc | ItemStats | 3-piece bonus |
| 4pc | ItemStats? | 4-piece bonus |
| 5pc | ItemStats? | 5-piece bonus |

### RecipeDef
```json
{ "resultId": "wooden_sword", "materials": [{ "itemId": "wood", "count": 5 }] }
```

---

## skills.json

### Root: `{ "skills": [] }`

### SkillDef
| Field | Type | Description |
|-------|------|-------------|
| id | string | "heavy_strike" |
| name | string | Korean display name |
| tree | string | "melee" "ranged" "magic" |
| requiredPoints | int | Skill points to unlock (1-8) |
| requiredLevel | int | Player level minimum (default 1) |
| mpCost | int | Mana cost per use |
| cooldown | float | Cooldown in milliseconds |
| damage | float | Base damage multiplier (0 for non-damage) |
| range | float | Range in pixels |
| aoe | float | AoE radius (0 = single target) |
| description | string | Skill description |
| actions | SkillAction[] | Action pipeline |
| behavior | string? | SkillExecutor dispatch key |
| effect | string? | "stun" "slow" "dot" "heal" "buff_atk" |
| effectDuration | float? | Effect duration in ms |
| buffType | string? | Buff type for self-buff skills |
| buffValue | float? | Buff multiplier |
| scaling | object? | { damage:0.2, aoe:0.1, duration:0.15, buff:0.1 } |

### SkillAction
| Field | Type | Description |
|-------|------|-------------|
| type | string | **Required.** "deal_damage" "apply_effect" "apply_buff" "spawn_projectile" "spawn_area" "screen_shake" "visual" "teleport" |
| aoe | float | Area of effect radius |
| ratio | float | Damage ratio (default 1) |
| crit | bool | Force critical hit |
| effect | string | "stun" "slow" "dot" "knockback" |
| duration | float | Effect duration ms |
| value | float | Effect parameter |
| speed | float | Projectile speed |
| pattern | string | "radial" "fan" "scatter" "self" "trap" |
| count | int | Projectile count |
| chain | object | { maxBounces, bounceRange, decayRatio } |
| buffType | string | "rage" "speed_up" "stealth" "mana_shield" "heal" "def_down" |
| buffValue | float | Buff value |
| onHit | SkillAction[] | Triggered on projectile hit |
| onArrive | SkillAction[] | Triggered at destination |
| onTick | SkillAction[] | Triggered per tick (spawn_area) |
| tickInterval | float | ms between ticks |

---

## monsters.json

### Root: `{ "monsters": [] }`

### MonsterDef
| Field | Type | Description |
|-------|------|-------------|
| id | string | "wolf" |
| name | string | Korean name |
| region | string | "forest" "cave" "swamp" "volcano" |
| hp, atk, def, spd | int | Base stats |
| xp, gold | int | Kill rewards |
| detectRange | float | Aggro range pixels |
| attackRange | float | Melee range |
| attackCooldown | float | ms between attacks |
| drops | DropEntry[] | [{ itemId, chance(0-1), minCount, maxCount }] |
| sprite | string | Sprite key |
| rank | string? | "normal" "elite" "boss" (default "normal") |
| ranged | object? | { projectileSpeed, projectileColor, projectileSize } |
| attackPatterns | array? | [{ name, weight, windupMs, actions[] }] |
| phases | array? | [{ hpPercent, statMult:{atk,def,spd,cooldown}, attackPatterns, onEnter }] |

---

## npcs.json

### Root: `{ "npcs": [] }`

### NpcDef
| Field | Type | Description |
|-------|------|-------------|
| id | string | "elder", "hunter", etc. |
| name | string | Korean name |
| color | string | Hex "#rrggbb" |
| sprite | string | Sprite key |
| personality | string | Personality description |
| dialogueTraits | object | { friendliness, generosity, secretive, stubbornness, curiosity } int 0-10 |
| idlePhrases | string[] | Idle speech bubbles |
| thinkingPhrases | string[] | Thinking text |
| dialogue | object | { default, hasQuest, questComplete } Korean strings |
| patrol | object | { cx, cy, radius } tile coordinates |
| actions | string[] | "open_shop" "open_crafting" "open_enhance" "heal_player" "reset_skills" "reset_stats" |
| autoActions | array | [{ type, condition, range, cooldown, message, giftItemId?, giftCount? }] |
| triggers | array | [{ event, target, threshold, relationship, memory, wantToTalk?, talkReason? }] |

### Trigger Events
"region_visit" "level_up" "quest_complete" "monster_kill" "item_collect" "equip" "combat_power" "gold_reach"

### AutoAction Types
"warn" "heal" "gift" "greet"

### AutoAction Conditions
"low_level" "hp_low" "hp_critical" "mp_low" "nearby" "high_relationship" "rel_10" "rel_15"

---

## quests.json

### Root: `{ "quests": [] }`

### QuestDef
| Field | Type | Description |
|-------|------|-------------|
| id | string | "quest_wolf_leather" |
| npcId | string | Giving NPC |
| title | string | Korean title |
| description | string | Korean description |
| requirements | array | [{ itemId, count }] |
| rewards | object | { gold, xp, items: [{ itemId, count }] } |

---

## regions.json

### Root: `{ "regions": [] }`

### RegionDef
| Field | Type | Description |
|-------|------|-------------|
| id | string | "village" "forest" "cave" "swamp" "volcano" "deep_cave" "dark_swamp" "dragon_lair" |
| name | string | Display name with difficulty stars |
| difficulty | int | 0-7 |
| bounds | object | { x, y, width, height } tile coordinates |
| tileWeights | object | { "grass": 0.5, "dirt": 0.3, ... } |
| monsterIds | string[] | Spawnable monsters |
| monsterDensity | float | Monsters per 100 tiles |

---

## C# Class Mapping

| JSON | C# Wrapper | C# Definition |
|------|-----------|---------------|
| items.json | ItemsData | { ItemDef[] items, Dictionary setBonuses, RecipeDef[] recipes } |
| skills.json | SkillsData | { SkillDef[] skills } |
| monsters.json | MonstersData | { MonsterDef[] monsters } |
| npcs.json | NpcsData | { NpcDef[] npcs } |
| quests.json | QuestsData | { QuestDef[] quests } |
| regions.json | RegionsData | { RegionDef[] regions } |
