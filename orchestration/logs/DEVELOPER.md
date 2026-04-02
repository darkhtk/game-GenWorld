# DEVELOPER Loop Log

**Last run:** 2026-04-03
**Status:** P0 버그 8건 수정 완료 → In Review

## Loop Result
- Build errors: 0
- B-001~B-008: 전부 수정 → In Review 제출

## 수정 내역

| Bug | 원인 | 수정 | 파일 |
|-----|------|------|------|
| B-001 몬스터 이동 불가 | UpdateAI 미호출 | GameManager.Update에 AI 루프 추가 | GameManager.cs |
| B-002 NPC 흔들림 | 패트롤 임계값 16f (너무 큼) | 0.25f로 수정 | VillageNPC.cs |
| B-003 NPC 대화 불가 | 인터랙션 핸들러 없음 | F키 + AI 대화 루프 구현 | GameManager.cs |
| B-004 미니맵 빈 화면 | MinimapUI.Init 미호출 | Walkable 접근자 + InitMinimap | WorldMapGenerator.cs, GameManager.cs |
| B-005 스킬 아이콘 없음 | 아이콘 로딩 코드 없음 | 스프라이트시트 캐시 + 로딩 | SkillTreeUI.cs |
| B-006 AI 연동 | GenerateDialogue 미호출 | B-003 수정 시 연결됨 | GameManager.cs |
| B-007 인벤토리 아이콘 | iconImage 미할당 | SetItem에 icon 로딩 추가 | InventoryUI.cs |
| B-008 카메라 Z값 | Z=0 → 스프라이트 안 보임 | Z>=0이면 Z=-10 강제 | GameManager.cs |

## 참고
- B-005: Inspector에서 SkillRow 프리팹에 iconImage 연결 필요
- B-007: 아이템 아이콘 스프라이트가 Resources/Sprites/Items/ 에 필요 (🎨 에셋)
- specs 참조: N

## TOTAL: 47 tasks + 8 bugfix
