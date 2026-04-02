# Current Assignment: Asset/QA

## Status: ACTIVE (Phase 7)

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Step 끝나면 → Loop로.

## Completed
- [x] Data files + Art assets copied
- [x] Step 1-5 — 컴파일 확인, 테스트 실행, WorldMapGenerator, Scene/Prefab 도구

## Task: Phase 7 — Runtime Verification + Polish

### Step 1: 컴파일 재확인
Director가 GameManager에 UI 와이어링 추가함 (f10b355). 새 코드로 컴파일 확인.
```bash
LOG="$LOCALAPPDATA/Unity/Editor/Editor.log"
grep "error CS" "$LOG" | tail -20
```
결과를 `status/compile-status.md`에 기록.

### Step 2: Scene 와이어링 검증
Editor tool로 Scene/Prefab을 생성했지만, GameManager의 SerializeField 연결이 필요:
- GameScene의 GameManager 오브젝트에 player, worldMap, monsterSpawner, combatManager, uiManager 연결 확인
- 연결 안 되어 있으면 `questions/`에 보고 (Scene 와이어링은 수동 작업)

### Step 3: EditMode 테스트 전체 실행
Dev-Backend가 AI 테스트 3개 추가함 (f1005ac). 기존 8 + 신규 3 = 11 suites 확인.
```bash
LOG="$LOCALAPPDATA/Unity/Editor/Editor.log"
grep -E "Test Run|Passed|Failed" "$LOG" | tail -10
```

### Step 4: 데이터 무결성 재확인
- regions.json의 bounds가 MapWidthTiles(200) × MapHeightTiles(200) 범위 안인지 확인
- 모든 monster.drops의 itemId가 items.json에 존재하는지 크로스체크
- 모든 quest의 requirement.monsterId/itemId가 유효한지 확인

### Step 5: Cinemachine 설정 준비
GameScene에 Cinemachine 2D 카메라가 필요:
- Package Manager에서 Cinemachine 설치 여부 확인
- 설치 안 되어 있으면 `questions/`에 보고 (패키지 설치는 Unity Editor에서)
- CinemachineVirtualCamera 추가 + Follow = Player 설정 문서화

## Reference
- docs/orchestration/reference/interface-contracts.md
- docs/orchestration/reference/data-schema.md
- Assets/Scripts/Core/GameManager.cs (최신 — UI 와이어링 포함)

## Git
```bash
git add Assets/Scripts/Map/ Assets/Scenes/ Assets/Prefabs/ Assets/StreamingAssets/
git commit -m "fix: <설명>"
```

## Loop
1. 컴파일 모니터링
2. `questions/` 확인
3. 새 커밋 있으면 컴파일/테스트 재확인
4. 데이터 파일 무결성
5. `status/asset-qa.md` 갱신
6. 1번으로
