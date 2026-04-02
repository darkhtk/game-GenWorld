# Status: Asset/QA

## Current: LOOP (monitoring)

## Last Update: 2026-04-02T14:30

## Current Task
Step 1-5 완료. Loop 모드 진입.

## Progress
- [x] Data files copied
- [x] Art assets copied
- [x] Step 1 — 컴파일 확인: PASS (0 errors, 1 warning stub)
- [x] Step 2 — EditMode 테스트: 8 suites / 61 tests, EditModeTests.dll compiled OK
- [x] Step 3 — WorldMapGenerator.cs: Generate/IsWalkable/IsVillageTile 구현 (Y축 반전 적용)
- [x] Step 4 — Scene 셋업: Editor tool 생성 (GenWorld > Setup Scenes menu)
  - BootScene: Camera + splash text + loading text
  - MainMenuScene: Camera + title + New Game/Continue buttons
  - GameScene: Camera + Grid/Tilemaps + Player placeholder + HUD Canvas
- [x] Step 5 — Prefab 생성: Editor tool 생성 (GenWorld > Setup Prefabs menu)
  - Monster (MonsterController + RB2D + SpriteRenderer + BoxCollider2D)
  - NPC (VillageNPC + RB2D + SpriteRenderer + BoxCollider2D)
  - Projectile (Projectile + SpriteRenderer + CircleCollider2D trigger)
  - DamageNumber (WorldSpace Canvas + TextMeshProUGUI)

## Notes
- Scene/Prefab은 Unity Editor에서 메뉴 실행 필요: GenWorld > Setup Scenes/Prefabs
- Grid cellSize = (32, 32, 1) — pixel-scale 좌표계
- Tile→Cell 변환: cell = (tileX, -tileY - 1, 0)

## Issues
- None
