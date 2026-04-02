# Current Assignment: Asset/QA

## Status: ACTIVE

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Step 끝나면 → Loop로. 블로커 만나면 → 기록하고 다음 작업.
"할 일이 없다", "대기 중", "assignment 확인 필요" 같은 상태는 금지.
항상 할 일이 있다. 아래 Loop 섹션을 봐라.

## Completed
- [x] Data files copied (JSON + npc-profiles + lore + ai-rules)
- [x] Art assets copied (sprites, tileset, items, skill_icons, effects)

## Task
순서대로 진행. asmdef 이슈 해결됨 — 바로 시작.

### Step 1: 컴파일 확인
```bash
LOG="$LOCALAPPDATA/Unity/Editor/Editor.log"
grep "error CS" "$LOG" | tail -20
```
결과를 `status/compile-status.md`에 기록.
에러가 있으면 `questions/`에 보고하되, **내 작업은 멈추지 말고** 다음 Step 진행.

### Step 2: EditMode 테스트 실행
asmdef 수정이 디스크에 적용됨 (GenWorld.asmdef + EditModeTests.asmdef).
Unity Test Runner에서 EditMode 테스트 실행 가능 여부 확인.
결과를 `status/asset-qa.md`에 기록.
**테스트 실패가 있어도 멈추지 말고** 결과만 기록하고 다음으로.

### Step 3: WorldMapGenerator.cs 구현
파일: `Assets/Scripts/Map/WorldMapGenerator.cs`
- Generate(): RegionDef[] 읽고 Unity Tilemap API로 타일 생성
- IsWalkable(tileX, tileY): collisionTilemap 체크
- IsVillageTile(tileX, tileY): 마을 영역 바운드 체크
- **Y축 반전 주의**: Phaser Y↓ → Unity Y↑ (phaser-unity-mapping.md 참조)

데이터: Assets/StreamingAssets/Data/regions.json (bounds, tileWeights)
시그니처: docs/orchestration/reference/interface-contracts.md (Map 섹션)

### Step 4: Scene 셋업
- GameScene: Tilemap, Player placeholder, Canvas, Camera
- BootScene: splash + DataManager.LoadAll 호출 지점
- MainMenuScene: New Game / Continue 버튼

### Step 5: Prefab 생성
- Monster prefab (MonsterController 부착)
- NPC prefab (VillageNPC 부착)
- Projectile prefab
- DamageNumber prefab (TextMeshPro)

## Reference
- docs/orchestration/reference/interface-contracts.md (Map 섹션)
- docs/orchestration/reference/phaser-unity-mapping.md (Y축 변환)
- docs/orchestration/reference/data-schema.md (RegionDef)

## Git (Step별)
```bash
git add Assets/Scripts/Map/ Assets/Scenes/ Assets/Prefabs/
git commit -m "feat: <설명>"
```
status/asset-qa.md 갱신 후 다음 Step으로.

## Loop (Step 1-5 모두 끝난 후)
모든 Step이 끝나도 **멈추지 마라**. 아래를 반복:
1. 컴파일 모니터링 — `Editor.log` 체크, `compile-status.md` 갱신
2. `questions/` 확인 — 나한테 온 질문 답변
3. `status/dev-backend.md`, `status/dev-frontend.md` 읽기 — 새 커밋 있으면 컴파일 재확인
4. EditMode 테스트 재실행 — 새 테스트 추가됐을 수 있음
5. 스프라이트/데이터 파일 무결성 확인 (PPU=32, Filter=Point 등)
6. `status/asset-qa.md` 갱신
7. 1번으로 돌아가기
