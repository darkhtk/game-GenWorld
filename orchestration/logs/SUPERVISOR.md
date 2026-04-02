# SUPERVISOR Loop Log

> **최종 실행:** 2026-04-02 (루프 #51)
> **모드:** 안정화 (RESERVE 추가 중단, 이슈만 수정)
> **수행 행동:** CRITICAL — UI 콜백 미연결 5건 수정

## 수정 내용

### GameManager.WireUICallbacks() 신규 메서드 (약 80줄)

| UI | 콜백 | 연결된 시스템 |
|----|------|-------------|
| InventoryUI.OnEquipCallback | 장비 착용 → Equipment + RecalcStats | ✅ 수정 |
| InventoryUI.OnUnequipCallback | 장비 해제 → Inventory.AddItem | ✅ 수정 |
| InventoryUI.OnUseItemCallback | 아이템 사용 → UsePotion | ✅ 수정 |
| InventoryUI.OnSortCallback | 정렬 → InventorySystem.SortItems | ✅ 수정 |
| SkillTreeUI.OnLearnSkill | 스킬 학습 → Skills.LearnSkill(id, points, level) | ✅ 수정 |
| SkillTreeUI.OnEquipSkill | 스킬 장착 → Skills.EquipSkill | ✅ 수정 |
| DialogueUI.OnClose | 대화 종료 → player.Frozen=false | ✅ 수정 |
| DialogueUI.OnAcceptQuest | 퀘스트 수락 → Quests.AcceptQuest | ✅ 수정 |
| DialogueUI.OnCompleteQuest | 퀘스트 완료 → Quests.CompleteQuest(id, inv) | ✅ 수정 |
| PauseMenuUI.OnSaveRequested | 저장 → SaveEvent | ✅ 수정 |
| PauseMenuUI.OnMainMenuRequested | 메인 메뉴 → SceneManager.LoadScene | ✅ 수정 |

### API 시그니처 수정
- `Skills.LearnSkill(skillId)` → `Skills.LearnSkill(skillId, SkillPoints, Level)`
- `Quests.StartQuest` → `Quests.AcceptQuest`
- `Quests.CompleteQuest(id)` → `Quests.CompleteQuest(id, Inventory)`
- `Inventory.RemoveAt` → `Inventory.RemoveAtSlot`
