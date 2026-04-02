# SPEC-B-004: 미니맵 회색 빈 화면 수정

> **우선순위:** P0 (사용자 버그 리포트)
> **증상:** 미니맵에 아무것도 표시되지 않음. 회색 화면.

---

## 진단 포인트

### 1. 미니맵 카메라 설정
- **파일:** `Assets/Scripts/UI/MinimapUI.cs`
- 미니맵 전용 Camera가 있는지 확인. RenderTexture 연결 상태.
- 카메라가 없거나 RenderTexture 미할당 → 회색 화면.
- 카메라 Culling Mask 확인 — 타일맵/NPC/플레이어 레이어가 포함되어야 함.
- **카메라 Z 위치:** B-008과 연관. 카메라 Z가 0이면 2D 스프라이트가 안 보임. Z < 0 필요.

### 2. RawImage / RenderTexture 연결
- MinimapUI에 `RawImage` 컴포넌트가 RenderTexture를 표시하는 구조인지 확인.
- RenderTexture 해상도 (너무 작으면 빈 화면처럼 보임).
- RawImage가 Canvas에 올바르게 배치되어 있는지.

### 3. 미니맵 아이콘 스폰
- R-032에서 미니맵 아이콘(player/NPC/monster/portal/quest) 에셋 생성 완료.
- MinimapUI에서 아이콘을 동적으로 생성하는 로직 확인.
- 아이콘이 미니맵 카메라에 보이는 레이어에 있는지.

### 4. 기존 스펙 참조
- `orchestration/specs/SPEC-R018.md` — 미니맵 구현 스펙 참조.

### 5. 씬 설정 확인
- Minimap Camera가 GameScene/WorldScene에 배치되어 있는지.
- 메인 카메라와 미니맵 카메라의 레이어 분리 확인.

## 수정 방향
1. 미니맵 Camera 존재 여부 확인. 없으면 생성 (Orthographic, 적절한 Size).
2. Camera Z 위치를 -10 이상으로 설정 (B-008 연관).
3. RenderTexture 생성 + Camera.targetTexture 연결 + RawImage.texture 연결.
4. Culling Mask에 Minimap 레이어 포함.
5. 플레이어 위치 추적 (Camera.transform = Player.transform + offset).

## 검증
- 미니맵에 지형/NPC/몬스터/플레이어 아이콘이 표시되는지.
- 플레이어 이동 시 미니맵이 따라가는지.
- 리전 전환 시 미니맵이 새 지역을 표시하는지.
