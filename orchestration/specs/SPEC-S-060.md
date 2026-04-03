# SPEC-S-060: MinimapUI 텍스처 재생성 누수

> **태스크:** S-060
> **우선순위:** P2
> **태그:** 🔧

## 문제

`MinimapUI.Init()` 호출 시 `GenerateMapTexture()`로 새 `Texture2D`를 생성하여 `_mapTexture` 필드에 할당하지만, 이전에 할당된 `Texture2D`에 대해 `Object.Destroy()`를 호출하지 않는다. Init()이 재호출되면 이전 텍스처가 GPU/CPU 메모리에 orphan으로 남아 누수 발생.

현재 코드 (MinimapUI.cs:42):
```csharp
_mapTexture = GenerateMapTexture(walkability, width, height);
```
이전 `_mapTexture`가 null이 아니어도 그냥 덮어쓴다.

또한 `OnDestroy()`가 없으므로 GameObject 파괴 시에도 마지막 텍스처가 명시적으로 해제되지 않는다. Unity GC가 언젠가 수거하지만, `Texture2D`는 native 리소스이므로 명시 Destroy가 권장된다.

## 목표

- Init() 재호출 시 기존 `_mapTexture`를 `Object.Destroy()`로 해제한 뒤 새 텍스처 생성
- `OnDestroy()`에서 `_mapTexture` 해제하여 씬 전환 시 누수 방지

## 수치/제한

- 텍스처 크기: `w x h` 픽셀 (현재 200x200), `TextureFormat.RGB24` = 약 120KB/장
- 씬 전환마다 1회 Init() 호출 → 반복 시 120KB씩 누적
- 수정 후 Init() N회 호출해도 텍스처는 항상 1장만 존재해야 함

## 연동 경로

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/MinimapUI.cs` | 수정 대상 — Init(), OnDestroy() |
| `Assets/Scripts/Core/GameManager.cs:248` | Init() 호출부 — `minimap.Init(worldMap.Walkable, ...)` |
| `Assets/Scripts/Core/GameConfig.cs:7` | `MapWidthTiles=200, MapHeightTiles=200` |

## 데이터 구조

```csharp
// MinimapUI 필드
Texture2D _mapTexture;          // Init()에서 생성, native 리소스 보유
RawImage mapImage;              // _mapTexture를 표시하는 UI 컴포넌트
```

`Texture2D`는 managed wrapper + native GPU 리소스로 구성. `Object.Destroy()`를 호출해야 native 리소스가 즉시 해제된다.

## 구현 방향

MinimapUI.cs에서 두 곳 수정:

1. **Init() 상단에 기존 텍스처 파괴 추가:**
```csharp
public void Init(bool[,] walkability, int width, int height)
{
    _mapWidth = width;
    _mapHeight = height;

    if (mapImage == null) mapImage = GetComponentInChildren<RawImage>(true);
    if (mapImage == null)
    {
        Debug.LogWarning("[MinimapUI] No RawImage found");
        return;
    }

    if (_mapTexture != null)
        Destroy(_mapTexture);

    _mapTexture = GenerateMapTexture(walkability, width, height);
    mapImage.texture = _mapTexture;
    mapImage.enabled = true;
    Debug.Log($"[MinimapUI] Initialized {width}x{height}");
}
```

2. **OnDestroy() 추가:**
```csharp
void OnDestroy()
{
    if (_mapTexture != null)
    {
        Destroy(_mapTexture);
        _mapTexture = null;
    }
}
```

## 호출 진입점

- `GameManager.InitMinimap()` (GameManager.cs:91) → `minimap.Init(worldMap.Walkable, MapWidthTiles, MapHeightTiles)`
- GameManager.Awake() → InitializeGame() → InitMinimap() 순서로 1회 호출
- 씬 재로드(메인 메뉴 → 게임 씬) 시 GameManager가 새로 생성되므로 Init()이 다시 호출됨
- MinimapUI가 DontDestroyOnLoad라면 동일 인스턴스에서 중복 Init() 가능

## 세이브 연동

- 없음. 미니맵 텍스처는 런타임 전용 리소스이며 세이브/로드와 무관.

## 검증 기준

1. **코드 검증:** Init() 내에서 `_mapTexture != null` 체크 후 `Destroy()` 호출 확인
2. **OnDestroy 존재:** MinimapUI에 OnDestroy()가 있고 `_mapTexture`를 Destroy 확인
3. **단위 테스트:** Init()을 2회 연속 호출한 뒤 이전 텍스처 참조가 destroyed 상태인지 확인
4. **수동 검증:** Unity Profiler Memory 탭에서 씬 전환 5회 반복 → Texture2D 인스턴스 수가 증가하지 않음
