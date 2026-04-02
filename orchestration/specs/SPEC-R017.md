# SPEC-R017: 사운드/음악 시스템 기반

## 목적
BGM 재생, SFX 재생, 볼륨 설정을 통합 관리하는 AudioManager를 구축한다.

## 현재 상태
- 오디오 시스템 전무. AudioSource, AudioClip 사용 코드 없음.
- PauseMenuUI에 설정 UI 구조 있으나 볼륨 슬라이더 미구현.
- 에셋 디렉토리에 오디오 파일 없음.

## 구현 명세

### 새 파일
- `Assets/Scripts/Systems/AudioManager.cs` — 싱글턴 오디오 관리자

### 수정 파일
- `Assets/Scripts/Core/GameManager.cs` — AudioManager 초기화 연결
- `Assets/Scripts/UI/PauseMenuUI.cs` — 볼륨 슬라이더 연결 (있으면)

### 데이터 구조

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Sources")]
    [SerializeField] AudioSource bgmSource;      // 루프 재생
    [SerializeField] AudioSource sfxSource;       // 단발 재생
    [SerializeField] AudioSource ambientSource;   // 환경음 (선택)
    
    [Header("Settings")]
    float _bgmVolume = 0.5f;
    float _sfxVolume = 0.7f;
    float _masterVolume = 1.0f;
    
    // BGM
    public void PlayBGM(string clipName, float fadeTime = 1f);
    public void StopBGM(float fadeTime = 0.5f);
    
    // SFX
    public void PlaySFX(string clipName, float pitchVariation = 0f);
    public void PlaySFXAt(string clipName, Vector2 worldPos); // 3D 위치 기반
    
    // Volume
    public void SetBGMVolume(float vol);
    public void SetSFXVolume(float vol);
    public void SetMasterVolume(float vol);
}
```

### 로직

1. **AudioManager 싱글턴:** DontDestroyOnLoad, 씬 전환 시 유지.
2. **클립 로딩:** `Resources.Load<AudioClip>("Audio/SFX/" + clipName)` — 런타임 로드 + 캐싱.
3. **BGM 크로스페이드:** 새 BGM 재생 시 기존 BGM 페이드 아웃 → 새 BGM 페이드 인.
4. **SFX 풀링:** sfxSource.PlayOneShot() 사용 (동시 다중 재생).
5. **볼륨 저장:** PlayerPrefs에 bgm_volume, sfx_volume, master_volume 저장.
6. **이벤트 연동 (Phase 2):**
   - RegionVisitEvent → 지역별 BGM 전환
   - 전투 시작/종료 → 전투 BGM 전환
   - UI 버튼 클릭 → 클릭 SFX

### 디렉토리 구조
```
Assets/Resources/Audio/
├── BGM/       (배경 음악)
├── SFX/       (효과음)
└── Ambient/   (환경음)
```

### 세이브 연동
- PlayerPrefs: `bgm_volume`, `sfx_volume`, `master_volume` (float, 0~1).

## 호출 진입점
- `AudioManager.Instance.PlayBGM("village")` — 코드에서 직접 호출.
- `AudioManager.Instance.PlaySFX("sword_hit")` — 전투 시스템 등에서 호출.
- PauseMenuUI: 볼륨 슬라이더 → SetBGMVolume/SetSFXVolume.

## 테스트 항목
- [ ] PlayBGM 호출 시 음악 재생 시작
- [ ] PlaySFX 호출 시 효과음 재생
- [ ] BGM 전환 시 크로스페이드 동작
- [ ] 볼륨 설정이 PlayerPrefs에 저장/로드되는지
- [ ] SFX 동시 다수 재생 시 문제 없는지
- [ ] 씬 전환 후 AudioManager 유지되는지
