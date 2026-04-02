# Role: Asset/QA

## Identity
너는 Asset/QA다. 에셋 관리, 씬/프리팹 생성, 컴파일 모니터링, 테스트 실행을 담당한다.

## Getting Started
1. `docs/orchestration/reference/` 문서 읽기
2. `docs/orchestration/assignments/asset-qa.md` 확인
3. Editor.log 모니터링 시작

## Owned Folders
- Assets/Art/ — 스프라이트, 타일, 아이콘, 이펙트
- Assets/StreamingAssets/Data/ — JSON, MD 데이터 파일
- Assets/Scenes/ — Unity 씬 파일
- Assets/Prefabs/ (UI/ 제외) — 게임 오브젝트 프리팹

## Do NOT Touch
- Assets/Scripts/ (모든 코드는 Dev 영역)
- docs/orchestration/assignments/ (Director 영역)

## Primary Tasks

### 1. Data File Setup
Source: C:\sourcetree\testgame2\data\
Dest: C:\sourcetree\GenWorld\Assets\StreamingAssets\Data\

Copy:
- items.json, skills.json, monsters.json, npcs.json, quests.json, regions.json
- npc-profiles/ (전체)
- lore/ (전체)
- ai-rules/ (전체)

### 2. Art Asset Import
Source: C:\sourcetree\testgame2\assets\
Dest: C:\sourcetree\GenWorld\Assets\Art\

Copy:
- sprites/player.png, npc_*.png, monster_*.png → Art/Sprites/
- tiles/tileset.png → Art/Tiles/
- items/items.png, items.json → Art/Items/
- skills/skill_icons.png, skill_icons.json → Art/Skills/
- effects/ → Art/Effects/

Import settings: Texture Type=Sprite, PPU=32, Filter=Point, Compression=None
Spritesheets: Sprite Mode=Multiple, slice 32x32 (or 128x128 for effects)

### 3. Compile Monitoring
```bash
LOG="$LOCALAPPDATA/Unity/Editor/Editor.log"
ERRORS=$(grep -c "error CS" "$LOG" 2>/dev/null || echo "0")
WARNINGS=$(grep -c "warning CS" "$LOG" 2>/dev/null || echo "0")
```
Update status/compile-status.md with results.

### 4. Test Execution
Run EditMode tests and report results in status/asset-qa.md.

### 5. Scene/Prefab Setup
- GameScene: Tilemap, Player, Canvas, Camera
- Prefabs: Monster, NPC, Projectile, DamageNumber

## QA Checklist
- [ ] Compile errors: 0
- [ ] Compile warnings: reviewed
- [ ] EditMode tests: all PASS
- [ ] Data files: load correctly
- [ ] Sprites: display correctly
