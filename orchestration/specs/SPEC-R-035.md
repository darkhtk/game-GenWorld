# SPEC-R-035: Steamworks SDK 통합 및 초기화

**관련 태스크:** R-035

---

## 개요
Steamworks.NET을 프로젝트에 통합하고 게임 시작 시 Steam API를 초기화하는 기반 시스템 구축.

## 상세 설명
Steam 배포의 전제 조건으로, Steamworks.NET NuGet/unitypackage를 설치하고 `SteamManager` 싱글턴을 구현한다. 게임 부팅 시(`BootSceneController`) Steam 클라이언트 연결을 확인하고, Steam이 실행 중이 아닐 경우 경고 후 오프라인 모드로 폴백한다. `steam_appid.txt`를 프로젝트 루트에 배치하며, 에디터/개발 빌드에서만 사용되도록 빌드 시 제외 처리한다.

## 데이터 구조
```csharp
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public bool Initialized { get; private set; }
    public CSteamID SteamUserId { get; private set; }

    // Steam App ID (Steamworks 파트너 사이트에서 발급)
    private const uint APP_ID = 0; // TODO: 실제 앱 ID로 교체

    void Awake();           // DontDestroyOnLoad, 초기화
    void Update();          // SteamAPI.RunCallbacks()
    void OnDestroy();       // SteamAPI.Shutdown()
    public bool IsSteamRunning();
}
```

## 연동 경로
| From | To | 방식 |
|------|----|------|
| BootSceneController | SteamManager | Awake에서 SteamManager 초기화 확인 후 게임 진입 |
| SteamManager | GameManager | Steam 초기화 상태를 GameManager에 전달 |
| SteamManager | EventBus | `SteamInitialized` 이벤트 발행 |

## UI 와이어프레임
N/A (UI 없음, 초기화 실패 시 팝업은 기존 DialogueUI 또는 간단한 MessageBox 활용)

## 호출 진입점
- **어디서:** `BootSceneController.Awake()` → `SteamManager` 프리팹 로드
- **어떻게:** `SteamManager.Instance.Initialized` 체크 후 씬 전환 진행

## 수용 기준
- [ ] Steamworks.NET 패키지가 프로젝트에 설치되어 에러 없이 컴파일됨
- [ ] `SteamManager` 싱글턴이 `DontDestroyOnLoad`로 씬 전환 간 유지됨
- [ ] Steam 클라이언트 실행 중일 때 `SteamAPI.Init()` 성공, `Initialized == true`
- [ ] Steam 클라이언트 미실행 시 경고 로그 출력 후 `Initialized == false`, 게임은 정상 진행
- [ ] `Update()`에서 `SteamAPI.RunCallbacks()` 호출됨
- [ ] `steam_appid.txt`가 프로젝트 루트에 존재하며 빌드 시 포함되지 않음
- [ ] `#if !DISABLESTEAMWORKS` 전처리기로 Steam 비활성 빌드 지원
