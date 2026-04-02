# SPEC-R018: 미니맵 UI

## 목적
플레이어 위치와 주변 NPC/몬스터/POI를 실시간으로 표시하는 미니맵을 HUD에 추가한다.

## 현재 상태
- `HUD.cs:44` — `minimapImage` (RawImage) SerializeField 이미 존재하나 로직 미구현.
- `RegionTracker.cs` — 플레이어 현재 리전 추적 중.
- `WorldMapGenerator.cs` — 타일맵 기반 월드 생성. walkability 데이터 존재.
- R-032 미니맵 아이콘(🎨) — 아직 미완성.

## 구현 명세

### 새 파일
- `Assets/Scripts/UI/MinimapUI.cs` — 미니맵 렌더링 + 아이콘 관리

### 수정 파일
- `Assets/Scripts/UI/HUD.cs` — MinimapUI 초기화 연결

### UI 와이어프레임
```
┌──────────────────────────────┐
│                    ┌────┐    │
│                    │MMap│    │  ← 우상단 128x128px
│                    │ ▲P │    │     P=플레이어, ●=몬스터
│                    │●  ◆│    │     ◆=NPC
│                    └────┘    │
│ HP ████████                  │
│ MP ████████                  │
└──────────────────────────────┘
```

### 데이터 구조

```csharp
public class MinimapUI : MonoBehaviour
{
    [SerializeField] RawImage mapImage;         // HUD.minimapImage 연결
    [SerializeField] Transform iconContainer;   // 미니맵 위 아이콘 부모
    [SerializeField] GameObject playerIcon;     // 고정 중앙
    [SerializeField] GameObject monsterIconPrefab;
    [SerializeField] GameObject npcIconPrefab;
    
    RenderTexture _minimapRT;   // 128x128 렌더 텍스처
    Camera _minimapCam;         // 직교 카메라, 위에서 아래
    
    const float VIEW_RADIUS = 30f;  // 미니맵에 표시되는 월드 반경 (타일)
    const int MAP_SIZE = 128;       // 픽셀
}
```

### 로직

**방법 A — RenderTexture 카메라 (권장):**
1. 미니맵 전용 직교 Camera 생성 (Culling Mask: Minimap 레이어).
2. 플레이어 위치 추적, orthographicSize = VIEW_RADIUS.
3. RenderTexture(128x128) 출력 → HUD.minimapImage에 바인딩.
4. 몬스터/NPC를 Minimap 레이어에 작은 아이콘 오브젝트로 표시.

**방법 B — 수동 렌더링 (카메라 없이):**
1. walkability 데이터로 정적 맵 텍스처 생성 (한 번).
2. 플레이어 중심으로 128x128 영역 잘라서 RawImage에 표시.
3. 엔티티 위치를 UI 좌표로 변환하여 아이콘 배치.

→ **방법 B 권장** (추가 카메라 오버헤드 없음, 2D RPG에 적합).

### 수동 렌더링 상세

1. **정적 맵 생성 (1회):**
   ```csharp
   // WorldMapGenerator.walkability 데이터 → Texture2D
   // walkable: dark green, unwalkable: dark brown, water: dark blue
   Texture2D GenerateMapTexture(bool[,] walkability);
   ```

2. **프레임 갱신:**
   - 플레이어 타일 좌표 계산.
   - 맵 텍스처에서 플레이어 중심 128x128 영역 crop → RawImage.uvRect 조정.
   - 범위 내 몬스터/NPC 위치를 UI 좌표로 변환 → 아이콘 RectTransform 이동.

3. **아이콘 색상:**
   - 플레이어: 흰색 삼각형 (방향 표시)
   - 몬스터: 빨간 점
   - NPC: 녹색 마름모
   - 퀘스트 NPC: 노란 느낌표

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 맵 크기 | 128x128 px | UI 표시 |
| 표시 반경 | 30 타일 | |
| 갱신 주기 | 0.2초 | 성능 고려 (5fps) |
| 아이콘 크기 | 4x4 px | 미니맵 내 |
| 위치 | 우상단 | HUD 앵커 |

### 세이브 연동
없음. 런타임 전용.

## 호출 진입점
- HUD 자동 표시. `M` 키로 확대/축소 토글 (VIEW_RADIUS 30 ↔ 60).
- 미니맵 클릭 시 전체 지도 열기 (Phase 2, 선택사항).

## 테스트 항목
- [ ] 플레이어 이동 시 미니맵이 따라가는지
- [ ] 몬스터 아이콘이 실제 위치에 표시되는지
- [ ] NPC 아이콘이 올바른 색상으로 표시되는지
- [ ] 맵 경계에서 크래시 없이 표시되는지
- [ ] M 키로 줌 토글이 동작하는지
- [ ] 갱신 주기가 0.2초 간격인지 (프로파일러)
