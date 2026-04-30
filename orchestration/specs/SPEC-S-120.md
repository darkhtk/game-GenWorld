# SPEC-S-120: 🎨 보스룸 진입 BGM 트랜지션 (크로스페이드 1.5s)

> **작성:** 2026-04-30 (Coordinator, 4회차)
> **출처:** BACKLOG_RESERVE.md S-120 (🎨 Audio, P3)
> **우선순위:** P3
> **상태:** Spec Drafted → 구현 대기 (Asset/QA 또는 Dev-Backend 픽업)
> **호출 진입점:** `GameManager.PlayRegionBGM(string region)` (이미 `RegionVisitEvent` 구독; `GameUIWiring.cs:271` `EventBus.On<RegionVisitEvent>(_ => playRegionBGM())`) — 보스 BGM 트랜지션 시 fadeTime 1.5s 명시 호출

---

## 1. 문제 정의

`GameManager.PlayRegionBGM(string region)`이 `volcano`/`dragon_lair` → `"bgm_boss"`로 전환할 때 `AudioManager.PlayBGM(bgm)`의 **기본 fadeTime=1f**가 적용된다 (`AudioManager.cs:80`). 그러나 현재 `CrossfadeBGM` 코루틴은 **half-fade out → half-fade in 시퀀셜** (`AudioManager.cs:96~117`) 구조라 보스 진입 임팩트가 약하고, 1.0s 기본은 보스 트랜지션의 긴장감 표현에 부족.

목표:
- **보스 BGM 진입(`bgm_boss`)에 한해 fadeTime을 1.5s로 명시 호출** — 일반 지역(마을/숲/늪/동굴) 트랜지션은 기존 1.0s 유지.
- (선택) `CrossfadeBGM` 코루틴을 **dual-source 동시 재생 크로스페이드**로 업그레이드 — 현재 시퀀셜 페이드는 0.75s 무음 갭이 발생.

비목표:
- AudioMixer Snapshot 도입 (S-118과 동일하게 코드 보간 유지).
- BGM 클립 자체 교체 / 보스 곡 신규 작곡 (`bgm_boss` 클립 이미 존재 가정).

---

## 2. 수치/상수 (단일 소스)

| 상수                          | 값        | 위치                | 비고                                     |
| --------------------------- | -------- | ----------------- | -------------------------------------- |
| `BgmTransitionDefault`      | **1.0s**  | `GameConfig.Audio` | 일반 지역 전환 (기존 PlayBGM 기본값과 동일)            |
| `BgmTransitionBossEnter`    | **1.5s**  | `GameConfig.Audio` | 보스룸 진입 트랜지션 — 본 SPEC 핵심                |
| `BgmTransitionBossExit`     | **1.0s**  | `GameConfig.Audio` | 보스룸 이탈은 일반과 동일                         |
| `BgmCrossfadeDualSource`    | **true**  | `GameConfig.Audio` | true=dual-source 동시 페이드, false=시퀀셜(legacy)|
| `BossRegionIds`             | `{"volcano","dragon_lair"}` | `GameConfig.Audio` | string[] 또는 HashSet<string> — 추후 확장 |

`GameManager.cs:315~318` 의 boss BGM 매핑(`volcano`/`dragon_lair` → `bgm_boss`)과 1대1 일치. RegionDef.id 기준.

---

## 3. 연동 경로 (변경 범위)

| 파일/위치                                           | 변경 내용                                                                  |
| ---------------------------------------------- | ---------------------------------------------------------------------- |
| `Assets/Scripts/Core/GameConfig.cs`            | `Audio` 정적 클래스에 §2 5개 상수 추가 (S-118의 `Audio` 클래스에 추가)                    |
| `Assets/Scripts/Core/GameManager.cs:327`       | `AudioManager.Instance?.PlayBGM(bgm)` → `PlayBGM(bgm, fadeTime)` 로 교체. fadeTime은 `BossRegionIds.Contains(region) ? BgmTransitionBossEnter : BgmTransitionDefault` |
| `Assets/Scripts/Systems/AudioManager.cs`       | (선택) `_bgmSourceB` 보조 AudioSource + `CrossfadeBGMDual(AudioClip, float)` 코루틴 추가. `BgmCrossfadeDualSource=true`면 dual 사용, false면 기존 `CrossfadeBGM` 호출 |
| `Assets/Scripts/Systems/AudioManager.cs:11`    | `[SerializeField] AudioSource bgmSource;` 옆에 `[SerializeField] AudioSource bgmSourceB;` 추가 — Awake에서 `CreateSource("BGM_B", true)` |

비변경 (영향 검토만):
- `S-118 DuckBGM`은 `bgmSource.volume`을 `_duckMultiplier`로 조작. dual-source 도입 시 `BgmTargetVolume()`이 양 소스 동일 적용되도록 두 소스 모두 갱신 필요. AudioManager 내 `bgmSource.volume = BgmTargetVolume()` 호출 7곳(`:164/:177/:200/:204/:208/:215/:219/:232`) 모두 dual 적용.
- `StopBGM` 코루틴 — boss 진입 직전 StopBGM 활성 시 충돌. 새 PlayBGM 시 기존 fade 코루틴 stop 보장(`:86 if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine)`).

---

## 4. 데이터 구조

### AudioManager dual-source 멤버

```csharp
[SerializeField] AudioSource bgmSource;     // 기존 — primary
[SerializeField] AudioSource bgmSourceB;    // 신규 — secondary (cross-fade target)
bool _useSourceA = true;                    // 활성 primary 추적

// PlayBGM 호출 시:
//   active = _useSourceA ? bgmSource : bgmSourceB
//   incoming = _useSourceA ? bgmSourceB : bgmSource
//   fade out active (volume → 0), fade in incoming (clip 설정 + Play, volume → BgmTargetVolume())
//   완료 시 _useSourceA = !_useSourceA
```

### GameConfig.Audio (S-118 클래스에 추가)

```csharp
public static class Audio
{
    // 기존 S-118 상수 6개 ...

    // S-120 신규
    public const float BgmTransitionDefault   = 1.0f;
    public const float BgmTransitionBossEnter = 1.5f;
    public const float BgmTransitionBossExit  = 1.0f;
    public const bool  BgmCrossfadeDualSource = true;
    public static readonly string[] BossRegionIds = { "volcano", "dragon_lair" };
}
```

---

## 5. UI / 와이어프레임

**UI 변경 없음.** 청각 효과만.

```
플레이어 보스룸 타일 진입
  → RegionTracker.UpdatePlayerRegion(x, y) [기존, RegionTracker.cs:24]
    → EventBus.Emit(new RegionVisitEvent { regionId="volcano", regionName="화산" })
      → GameUIWiring EventBus.On<RegionVisitEvent>(_ => playRegionBGM()) [기존]
        → GameManager.PlayRegionBGM("volcano") [기존]
          → bgm = "bgm_boss" [기존 매핑]
          → fadeTime = BossRegionIds.Contains("volcano") ? 1.5f : 1.0f [신규 1라인]
          → AudioManager.PlayBGM("bgm_boss", 1.5f) [기존 시그니처 활용]
            └─ CrossfadeBGMDual(clip, 1.5s)
               ├─ active source volume → 0 (1.5s)
               └─ secondary source clip set + Play, volume → target (1.5s, 동시 진행)
```

**dual-source vs 시퀀셜 차이:**
- 기존 시퀀셜: 0.75s active 페이드 아웃 → clip 교체 → 0.75s 페이드 인. 중간 무음 갭 약 0~50ms (Play 호출 지연).
- 신규 dual: 1.5s 동안 양 소스 음량이 교차 — 보스 BGM이 들어오면서 동시에 일반 BGM이 빠짐. 갭 없음.

---

## 6. 세이브 연동

**없음.** 현재 region BGM 상태는 RegionTracker에서 좌표 기반으로 재계산되므로 로드 직후 자동 재생.

---

## 7. 테스트 계획

### EditMode (NUnit)

1. `GameManagerTest.PlayRegionBGM_VolcanoUsesBossFadeTime()` — `PlayRegionBGM("volcano")` 호출 시 AudioManager.PlayBGM의 fadeTime 인자가 1.5f 인지 검증 (mock AudioManager).
2. `GameManagerTest.PlayRegionBGM_VillageUsesDefaultFadeTime()` — `"village"` 호출 시 1.0f.
3. `AudioManagerTest.CrossfadeBGMDual_NoSilenceGap()` — Time.unscaledTime 시뮬레이션. 트랜지션 50% 지점에서 두 소스 합산 볼륨 ≥ 0.5×target (선형 합 가정).
4. `AudioManagerTest.PlayBGM_RestartsFadeWithoutLeakingCoroutine()` — fadeTime=1.5s 진행 중 0.5s 시점에 다른 BGM 호출 시 _fadeCoroutine null 체크 통과 + 새 코루틴 단일 실행.

### PlayMode 수동
- 필드(forest) → 보스룸(volcano) 진입: `bgm_forest`가 1.5s 동안 `bgm_boss`로 부드럽게 교차. 무음 갭 X.
- 보스룸 → 필드 이탈: 1.0s 페이드 (기본).
- 보스 진입 직후 0.5s 시점에 다시 다른 region 통과 (롤백): 코루틴 안전 종료, 새 BGM 정상.
- DuckBGM (S-118) + 트랜지션 동시 발생: BGM 트랜지션 중 아이템 픽업 → 더킹이 새 active source에 적용되는지 확인.

---

## 8. 호출 진입점 (재명시)

| 진입점                               | 트리거 조건                                                 |
| --------------------------------- | ------------------------------------------------------ |
| `RegionTracker.UpdatePlayerRegion` | 매 프레임 플레이어 좌표 → region 산정 (RegionTracker.cs:24, 기존)        |
| `RegionVisitEvent` 발행             | regionId 변경 시 1회 (RegionTracker.cs:31, 기존)                |
| `GameUIWiring.WireAudio`         | `EventBus.On<RegionVisitEvent>(_ => playRegionBGM())` (GameUIWiring.cs:271, 기존) |
| `GameManager.PlayRegionBGM`       | 본 SPEC 변경점 — fadeTime 분기 (GameManager.cs:327)                          |
| `AudioManager.PlayBGM(name, 1.5f)` | 명시 fadeTime 호출. dual-source 코루틴 진입.                         |

신규 UI 진입점 없음. 보스룸 트리거는 `RegionDef.id == "volcano" | "dragon_lair"` 기존 데이터 사용.

---

## 9. 작업 분담

| 영역                                 | 담당          | 산출물                            |
| ---------------------------------- | ----------- | ------------------------------ |
| `GameConfig.Audio` 5개 상수 추가         | Dev-Backend | const 4 + string[] 1            |
| `GameManager.PlayRegionBGM` fadeTime 분기 | Dev-Backend | 1라인                            |
| `AudioManager.bgmSourceB` + Awake 생성  | Dev-Backend | SerializeField 1 + Awake 1라인        |
| `AudioManager.CrossfadeBGMDual` 코루틴 | Dev-Backend | 약 30~50라인                       |
| `BgmTargetVolume()` 호출지 dual 적용     | Dev-Backend | 7곳 갱신                          |
| EditMode 테스트 4건                    | Dev-Backend | NUnit                          |
| boss BGM 클립 신규 작곡 (선택)             | Asset/QA    | `bgm_boss` 클립이 부재할 경우만           |

---

## 10. 리스크

- **dual-source 동시 재생 → 메모리/CPU 2배:** AudioSource 1개 추가 → 무시 가능 (수십 KB). CPU는 트랜지션 1.5s 동안만.
- **DuckBGM과 충돌:** DuckBGM은 `_duckMultiplier`를 양 소스에 동일 적용해야 함. `BgmTargetVolume()` 산식이 통일되어 있어 (S-118 정리됨) dual-source 갱신 7곳 적용으로 해결.
- **`bgm_boss` 클립 부재:** Resources/Audio/BGM/bgm_boss 미존재 시 `PlayBGM`이 LogWarning 후 NoOp (`AudioManager.cs:83`). Asset/QA에서 클립 확인 필요.
- **legacy 시퀀셜 fallback 유지:** `BgmCrossfadeDualSource=false`로 토글 시 기존 `CrossfadeBGM` 사용. 회귀 시 즉시 롤백 가능.
- **Boss exit 시 페이드 길이:** 현재 SPEC은 enter만 1.5s, exit는 1.0s. 사용자가 보스 처치 후 빠르게 빠져나가는 시나리오에서 너무 빠를 경우 추후 조정.

---

## 11. 완료 조건 (DoD)

1. `GameConfig.Audio`에 §2 5개 상수 추가.
2. `GameManager.PlayRegionBGM` fadeTime 분기 라인 추가.
3. `AudioManager.bgmSourceB` SerializeField + Awake 생성 + `CrossfadeBGMDual` 코루틴 추가.
4. `BgmTargetVolume()` 호출지 7곳 dual-source 갱신.
5. EditMode 테스트 4건 GREEN.
6. PlayMode 수동 — village → volcano → village 왕복 시 트랜지션 부드러움 확인 + 무음 갭 X.
7. S-118 DuckBGM과 동시 작동 검증 (보스 진입 직후 아이템 픽업 → 더킹이 새 active source에 정상 적용).
