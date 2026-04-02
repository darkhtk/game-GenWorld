# Current Assignment: Dev-Frontend

## Status: ACTIVE

## Task
Phase 6 — UI 패널 구현 (12개 파일)

## Priority Order
1. UIManager.cs
2. HUD.cs
3. InventoryUI.cs
4. DialogueUI.cs
5. SkillTreeUI.cs
6. QuestUI.cs
7. ShopUI.cs
8. CraftingUI.cs
9. EnhanceUI.cs
10. NpcProfilePanel.cs
11. NpcQuestPanel.cs
12. PauseMenuUI.cs

## How
스텁 파일이 Assets/Scripts/UI/ 에 존재. uGUI + TextMeshPro로 구현.
Systems 스텁이 컴파일 가능하므로 시스템 API 호출 코드를 바로 작성 가능.
상세 UI 스펙은 C:\sourcetree\testgame2\docs\superpowers\plans\2026-04-02-unity-port.md Phase 6 참조.

## Reference
- docs/orchestration/reference/interface-contracts.md (UI 섹션)
- docs/orchestration/reference/phaser-unity-mapping.md
- docs/orchestration/roles/dev-frontend.md (UI 구현 패턴, 핫키 목록)

## Acceptance Criteria
- 모든 public 메서드가 interface-contracts.md와 일치
- Show/Hide/Toggle/Refresh 패턴 준수
- compile-status.md 에러 0

## When Done
1. git add Assets/Scripts/UI/ Assets/Scripts/Effects/ && git commit
2. status/dev-frontend.md 갱신 (Current: DONE)
3. 이 파일 다시 확인
