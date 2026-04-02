# Current Assignment: Asset/QA

## Status: ACTIVE

## Task
1. 데이터 파일 복사 (testgame2 → GenWorld)
2. 아트 에셋 복사
3. 컴파일 모니터링

## Data Files
```bash
cp -r "C:/sourcetree/testgame2/data/items.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/skills.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/monsters.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/npcs.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/quests.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/regions.json" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/npc-profiles" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/lore" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
cp -r "C:/sourcetree/testgame2/data/ai-rules" "C:/sourcetree/GenWorld/Assets/StreamingAssets/Data/"
```

## Art Assets
```bash
cp "C:/sourcetree/testgame2/assets/sprites/"*.png "C:/sourcetree/GenWorld/Assets/Art/Sprites/"
cp "C:/sourcetree/testgame2/assets/tiles/"* "C:/sourcetree/GenWorld/Assets/Art/Tiles/"
cp "C:/sourcetree/testgame2/assets/items/"* "C:/sourcetree/GenWorld/Assets/Art/Items/"
cp "C:/sourcetree/testgame2/assets/skills/"* "C:/sourcetree/GenWorld/Assets/Art/Skills/"
cp "C:/sourcetree/testgame2/assets/effects/"* "C:/sourcetree/GenWorld/Assets/Art/Effects/"
```

## Compile Monitoring
```bash
LOG="$LOCALAPPDATA/Unity/Editor/Editor.log"
grep "error CS" "$LOG" | tail -20
```
결과를 status/compile-status.md 에 기록.

## When Done
1. git add Assets/StreamingAssets/ Assets/Art/ && git commit
2. status/asset-qa.md 갱신 (Current: DONE)
3. 컴파일 모니터링 계속 (QA 모드)
