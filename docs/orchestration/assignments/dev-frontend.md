# Current Assignment: Dev-Frontend

## Status: ACTIVE (Phase 7)

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Step 끝나면 → Loop로.

## Completed
- [x] Phase 6 — 12 UI panels + SkillVFX
- [x] Step 1-4 — 커밋, 품질 점검, 연동 준비, Visual Polish

## Task: Phase 7 — Scene Flow + UI Polish

### Step 1: BootScene 스크립트
파일: `Assets/Scripts/UI/BootSceneController.cs`
- 스플래시 텍스트 표시 (1.5초)
- SceneManager.LoadScene("MainMenuScene")
- MonoBehaviour, Canvas 자식에 부착

### Step 2: MainMenu 스크립트
파일: `Assets/Scripts/UI/MainMenuController.cs`
- "New Game" 버튼 → SceneManager.LoadScene("GameScene")
- "Continue" 버튼 → SaveSystem.HasSave() 확인 후 GameScene 로드
- Continue 버튼은 세이브 없으면 비활성화

### Step 3: UI 시스템 Refresh 연동
Director가 EventBus→HUD 와이어링 완료 (f10b355).
이제 각 UI 패널의 Refresh가 시스템에서 데이터를 가져올 수 있도록:
- InventoryUI.Refresh() — GameManager.Instance 또는 FindObjectOfType로 시스템 참조
- QuestUI.Refresh() — 동일
- ShopUI.Refresh() — 동일
- SkillTreeUI.Refresh() — 동일
**주의**: static 싱글턴 패턴이 없으면 [SerializeField] GameManager 참조로 처리

### Step 4: SkillBar 쿨다운 시각화
HUD.SetSkillCooldown()이 이미 있음. Update에서 쿨다운 갱신하는 로직 추가:
- HUD에서 GameManager 참조 → Skills.GetCooldowns() 호출 (Dev-Backend가 구현 예정)
- null이면 무시

## Reference
- docs/orchestration/reference/interface-contracts.md (UI 섹션)
- Assets/Scripts/Core/GameManager.cs (EventBus→UI 와이어링 참고)
- docs/orchestration/reference/architecture.md (Scene Flow)

## Git
```bash
git add Assets/Scripts/UI/ Assets/Scripts/Effects/
git commit -m "feat: Phase 7 Step N — <설명>"
```

## Loop
1. `compile-status.md` 확인
2. `questions/` 확인
3. Backend 새 시스템 확인 → UI 연동 업데이트
4. 자기 UI 코드 버그/개선점
5. `status/dev-frontend.md` 갱신
6. 1번으로
