# Current Assignment: Dev-Backend

## Status: ACTIVE

## Task
Phase 2 — Systems 구현 (11개 파일)

## Priority Order
1. InventorySystem.cs + InventorySystemTests.cs
2. StatsSystem.cs + StatsSystemTests.cs
3. CombatSystem.cs + CombatSystemTests.cs
4. EffectSystem.cs + EffectSystemTests.cs
5. SkillSystem.cs + SkillSystemTests.cs
6. LootSystem.cs + LootSystemTests.cs
7. CraftingSystem.cs + CraftingSystemTests.cs
8. QuestSystem.cs + QuestSystemTests.cs
9. SaveSystem.cs
10. SkillExecutor.cs
11. ActionRunner.cs

## How
스텁 파일이 Assets/Scripts/Systems/ 에 존재. public 시그니처를 유지하면서 실제 로직으로 채워라.
상세 구현 코드는 C:\sourcetree\testgame2\docs\superpowers\plans\2026-04-02-unity-port.md 참조.

## Reference
- docs/orchestration/reference/interface-contracts.md
- docs/orchestration/reference/data-schema.md
- docs/orchestration/reference/architecture.md

## Acceptance Criteria
- 모든 public 메서드가 interface-contracts.md와 일치
- 시스템당 최소 5개 EditMode 테스트
- compile-status.md 에러 0

## When Done
1. git add Assets/Scripts/Systems/ Assets/Tests/EditMode/ && git commit
2. status/dev-backend.md 갱신 (Current: DONE)
3. 이 파일 다시 확인 (다음 태스크: Phase 3 Entities)
