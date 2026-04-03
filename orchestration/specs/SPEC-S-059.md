# SPEC-S-059: AudioManager 클립 캐시 메모리 누수

> **태스크:** S-059
> **우선순위:** P2
> **태그:** 🔧 안정성 / 메모리

---

## 문제

`AudioManager._clipCache`는 `Dictionary<string, AudioClip>`로 구현되어 있으며, `GetClip()` 호출 시 `Resources.Load<AudioClip>(path)` 결과를 캐시에 추가하지만 제거하는 로직이 전혀 없다. AudioManager는 `DontDestroyOnLoad`이므로 씬 전환 시에도 파괴되지 않아 캐시가 앱 수명 동안 무한 성장한다.

현재 오디오 리소스 규모:
- SFX: 97개 파일
- BGM: 10개 파일
- Ambient: 9개 파일
- **합계: 116개 WAV 파일**

현 규모에서는 전체 캐시해도 문제가 크지 않으나, 콘텐츠 추가 시 WAV 클립이 수백 개로 늘어나면 메모리 압박이 발생한다. 특히 특정 씬에서만 사용하는 SFX(예: `sfx_birthday_fanfare`, `sfx_boss_defeat`)가 다른 씬에서도 메모리를 점유한 채 남는다.

## 목표

1. `_clipCache`가 무제한 성장하지 않도록 상한을 설정한다.
2. 씬 전환 시 현재 사용하지 않는 클립을 정리한다.
3. 자주 사용하는 클립(confirm, click, attack 등)은 캐시에 유지하여 재로드를 방지한다.

## 수치/제한

| 항목 | 값 | 비고 |
|------|-----|------|
| 캐시 최대 크기 | 32개 | 현재 SFX 호출 패턴 기준, BGM/Ambient 포함 |
| 최소 유지 항목 | 현재 재생 중인 BGM + Ambient 클립 | eviction 대상에서 제외 |
| 씬 전환 시 정리 | 사용 빈도 0인 클립 제거 | LRU 타임스탬프 기준 |

## 연동 경로

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Systems/AudioManager.cs` | 캐시 구현, 주 수정 대상 |
| `Assets/Scripts/Core/GameManager.cs` | 씬 전환 시 정리 트리거 호출 (line ~280) |
| `Assets/Scripts/Core/GameUIWiring.cs` | SFX 호출 최다 (15+ 개소) |
| `Assets/Scripts/Systems/CombatManager.cs` | 전투 SFX 호출 |

## 데이터 구조

현재:
```csharp
readonly Dictionary<string, AudioClip> _clipCache = new();
```

변경 후:
```csharp
struct CacheEntry
{
    public AudioClip clip;
    public float lastAccessTime;  // Time.unscaledTime
}

readonly Dictionary<string, CacheEntry> _clipCache = new();
const int MaxCacheSize = 32;
```

## 구현 방향

### 1. LRU 캐시 eviction

`GetClip()` 수정:
- 캐시 히트 시 `lastAccessTime` 갱신
- 캐시 미스 + `_clipCache.Count >= MaxCacheSize` 일 때 가장 오래된 엔트리 제거
- 제거 전 해당 클립이 현재 재생 중(`bgmSource.clip`, `ambientSource.clip`)이면 건너뛰기

```csharp
AudioClip GetClip(string path)
{
    if (_clipCache.TryGetValue(path, out var entry))
    {
        entry.lastAccessTime = Time.unscaledTime;
        _clipCache[path] = entry;
        return entry.clip;
    }

    if (_clipCache.Count >= MaxCacheSize)
        EvictLRU();

    var clip = Resources.Load<AudioClip>(path);
    if (clip != null)
        _clipCache[path] = new CacheEntry { clip = clip, lastAccessTime = Time.unscaledTime };
    return clip;
}
```

### 2. 씬 전환 시 일괄 정리

`ClearUnusedCache()` public 메서드 추가:
- 현재 재생 중인 클립(BGM, Ambient)을 제외하고 전부 제거
- `Resources.UnloadUnusedAssets()` 호출은 GameManager 쪽에서 수행 (S-051과 연계)

```csharp
public void ClearUnusedCache()
{
    var activeClips = new HashSet<AudioClip>();
    if (bgmSource.clip != null) activeClips.Add(bgmSource.clip);
    if (ambientSource.clip != null) activeClips.Add(ambientSource.clip);

    var keysToRemove = new List<string>();
    foreach (var kvp in _clipCache)
    {
        if (!activeClips.Contains(kvp.Value.clip))
            keysToRemove.Add(kvp.Key);
    }
    foreach (var key in keysToRemove)
        _clipCache.Remove(key);
}
```

### 3. SceneManager.sceneLoaded 구독 (선택)

AudioManager.Awake()에서 `SceneManager.sceneLoaded += OnSceneLoaded` 등록하여 자동 정리할 수도 있으나, GameManager에서 명시적으로 호출하는 방식이 순서 제어에 더 안전하다.

## 호출 진입점

1. **LRU eviction:** `GetClip()` 내부 자동 발동 (캐시 크기 >= 32일 때)
2. **씬 전환 정리:** `GameManager`에서 씬 로드 전 `AudioManager.Instance.ClearUnusedCache()` 호출
3. **수동 정리 (디버그):** 필요 시 `ClearUnusedCache()` 직접 호출

## 세이브 연동

없음. `_clipCache`는 런타임 전용 캐시이며 SaveData에 포함되지 않는다.

## 검증 기준

1. **단위 테스트:** `GetClip()`을 33회 서로 다른 경로로 호출 후 `_clipCache.Count <= 32` 확인
2. **단위 테스트:** `ClearUnusedCache()` 호출 후 재생 중 BGM 클립만 캐시에 남는지 확인
3. **단위 테스트:** 캐시 히트 시 `lastAccessTime`이 갱신되어 LRU 대상에서 제외되는지 확인
4. **수동 테스트:** 5회 씬 전환 후 Profiler에서 AudioClip 메모리 증가 없음 확인
5. **회귀 테스트:** 기존 SFX/BGM 재생이 정상 동작하는지 확인 (캐시 미스 시 재로드)
