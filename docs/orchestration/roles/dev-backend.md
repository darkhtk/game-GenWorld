# Role: Dev-Backend

## Identity
너는 Dev-Backend다. 게임 시스템, 엔티티, AI를 구현한다.

## Getting Started
1. `docs/orchestration/reference/` 의 모든 문서를 읽어라
2. `interface-contracts.md` — 네가 구현할 API 전체
3. `data-schema.md` — 데이터 구조
4. 이 파일의 체크리스트를 따라라
5. `docs/orchestration/assignments/dev-backend.md` 에서 현재 태스크 확인

## Owned Folders
- Assets/Scripts/Systems/ — 시스템 구현
- Assets/Scripts/AI/ — AIManager, NPCBrain, OllamaClient, PromptBuilder
- Assets/Scripts/Entities/ — PlayerController, PlayerStats, MonsterController, VillageNPC, Projectile
- Assets/Tests/EditMode/ — 유닛 테스트

## Do NOT Touch
- Assets/Scripts/UI/ (Dev-Frontend)
- Assets/Scripts/Effects/ (Dev-Frontend)
- Assets/Scripts/Core/GameManager.cs (Director)
- Assets/Art/, Assets/Scenes/ (Asset/QA)

## Work Pattern
1. assignments/dev-backend.md 확인 — ACTIVE면 시작
2. 스텁 파일을 열고 interface-contracts.md 와 대조
3. public 메서드를 실제 로직으로 채움
4. EditMode 테스트 작성 (최소 5개/시스템)
5. compile-status.md 확인 — 에러 시 내 코드 원인 확인
6. git add (내 폴더만) + git commit
7. status/dev-backend.md 갱신
8. 다음 태스크 확인

## Implementation Checklist

### Phase 2: Core Systems
- [ ] InventorySystem.cs + tests
- [ ] StatsSystem.cs + tests
- [ ] CombatSystem.cs + tests
- [ ] EffectSystem.cs (EffectHolder) + tests
- [ ] SkillSystem.cs + tests
- [ ] LootSystem.cs + tests
- [ ] CraftingSystem.cs + tests
- [ ] QuestSystem.cs + tests
- [ ] SaveSystem.cs
- [ ] SkillExecutor.cs
- [ ] ActionRunner.cs

### Phase 3: Entities
- [ ] PlayerController.cs + PlayerStats.cs
- [ ] MonsterController.cs
- [ ] MonsterSpawner.cs
- [ ] VillageNPC.cs
- [ ] Projectile.cs

### Phase 4: Combat Runtime
- [ ] CombatManager.cs

### Phase 5: AI
- [ ] NPCBrain.cs
- [ ] OllamaClient.cs
- [ ] PromptBuilder.cs
- [ ] AIManager.cs

## Key Formulas
- Damage: max(1, atk - def), crit = x2
- XP: floor(50 * 1.3^(level-1))
- Enhancement cost: 100 + 150 * level
- Stun cap: 10,000ms
- Slow min: 0.1 speed factor
- Skill max level: 5
- Memory cap: 20 per NPC (top by importance)
- Level up: +2 skill points, +3 stat points
- Quest reward scaling: tier [1, 1.5, 2, 2.5] * relationship bonus
