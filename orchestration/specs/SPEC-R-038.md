# SPEC-R-038: 게임 설정 시스템 (SettingsManager)

**관련 태스크:** R-038

---

## 개요
그래픽·오디오·조작 설정을 관리하고 영속적으로 저장하는 `SettingsManager` 데이터 계층 구현.

## 상세 설명
PC/Steam 배포를 위해 사용자가 해상도, 전체화면 모드, 그래픽 품질, 오디오 볼륨, 키 바인딩을 조정할 수 있어야 한다. `SettingsManager`는 이 설정값들을 메모리에 보유하고, JSON 파일로 영속화하며, 변경 시 즉시 적용한다. Unity의 `QualitySettings`, `Screen`, `AudioMixer`에 대한 래퍼 역할을 한다.

## 데이터 구조
```csharp
[System.Serializable]
public class GameSettings
{
    // 그래픽
    public int resolutionWidth;
    public int resolutionHeight;
    public int refreshRate;
    public FullScreenMode fullScreenMode;
    public int qualityLevel;         // 0=Low, 1=Medium, 2=High
    public bool vSyncEnabled;
    public int targetFrameRate;      // 30, 60, 120, Unlimited

    // 오디오
    public float masterVolume;       // 0~1
    public float bgmVolume;
    public float sfxVolume;

    // 조작
    public Dictionary<string, KeyCode> keyBindings;
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public GameSettings Current { get; private set; }

    public void ApplyGraphics();
    public void ApplyAudio();
    public void ApplyKeyBindings();
    public void Save();             // JSON → Application.persistentDataPath
    public void Load();
    public void ResetToDefaults();
}
```

## 연동 경로
| From | To | 방식 |
|------|----|------|
| SettingsManager | QualitySettings | `QualitySettings.SetQualityLevel()` |
| SettingsManager | Screen | `Screen.SetResolution()`, `Screen.fullScreenMode` |
| SettingsManager | AudioMixer | `SetFloat("MasterVol", ...)` |
| SettingsUI (R-039) | SettingsManager | UI 변경 → `Apply*()` → `Save()` |
| BootSceneController | SettingsManager | 부팅 시 `Load()` 호출 |

## UI 와이어프레임
N/A (데이터 계층만, UI는 R-039)

## 호출 진입점
- **어디서:** `BootSceneController.Start()` → `SettingsManager.Load()`
- **어떻게:** 저장된 JSON 로드 → `ApplyGraphics()` + `ApplyAudio()` + `ApplyKeyBindings()` 순차 호출

## 수용 기준
- [ ] 모든 설정값이 JSON 파일로 `Application.persistentDataPath`에 저장/로드됨
- [ ] `ApplyGraphics()` 호출 시 해상도, 전체화면, 품질, VSync 즉시 반영
- [ ] `ApplyAudio()` 호출 시 AudioMixer 파라미터 즉시 반영
- [ ] `ResetToDefaults()` 호출 시 시스템 감지 기본값으로 복원
- [ ] 설정 파일 없거나 손상 시 기본값으로 초기화 (크래시 방지)
- [ ] 키 바인딩 변경 시 중복 키 감지 및 경고
