# DEVELOPER Loop Log

**Last run:** 2026-04-03 (loop 4)
**Status:** 빌드 에러 1건 수정 + Visual Polish 2건 자가진행

## Loop Result
- Build errors: 1건 수정 (LevelSystem→StatsSystem)
- V-007, V-009: 자가진행 완료

## 수정 내역

### BF-009 빌드 에러 수정 ✅
- `GameManager.cs:628` — `LevelSystem.AddXp` → `StatsSystem.AddXp` (클래스 미존재 오타)
- **파일:** GameManager.cs

### V-007 몬스터/NPC 이름표 ✅
- NameLabel 유틸 생성 — World Space TextMeshPro로 이름 표시
- MonsterController.Init: 빨간 계열 이름표 (yOffset=1.2)
- VillageNPC.Init: 파란 계열 이름표 (yOffset=1.0)
- **파일:** NameLabel.cs (신규), MonsterController.cs, VillageNPC.cs

### V-009 UI 패널 전환 애니메이션 ✅
- PanelAnimator 컴포넌트 생성 — CanvasGroup 기반 페이드 인/아웃
- InventoryUI에 시범 적용 (Show/Hide에서 PanelAnimator 감지)
- **파일:** PanelAnimator.cs (신규), InventoryUI.cs

## specs 참조: N
