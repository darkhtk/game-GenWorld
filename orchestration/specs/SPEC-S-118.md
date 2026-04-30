# SPEC-S-118: 🎨 아이템 획득 팝업 BGM 더킹 (-6dB, 0.4초)

> **작성:** 2026-04-30 (Coordinator)
> **출처:** BACKLOG_RESERVE.md S-118 (🎨 Audio, P3)
> **우선순위:** P3
> **상태:** Spec Drafted → 구현 대기 (Dev-Backend 픽업 + Asset/QA Mixer 자산)
> **호출 진입점:** `LootSystem.OnItemPickup(ItemDef, count)` → `AudioManager.DuckBGM(ItemPickupDuckProfile)` 1회

---

## 1. 문제 정의

아이템 획득 SFX(`sfx_item_pickup`, `sfx_loot_glow`)가 BGM에 묻혀 청각적 임팩트가 부족. 특히 보스/엘리트 처치 후 다중 드롭 상황에서 SFX 식별 불가. 결과:
1. 획득 인지 약화 → 보상 만족도 저하 (P3).
2. SFX 자체 볼륨 상향은 다른 사운드와 밸런스 붕괴 → 채택 불가.

목표:
- 아이템 획득 시 **BGM 만 일시적 -6dB 감쇠** (SFX/Ambient 유지) → 0.4s 구간.
- AudioMixer 스냅샷 전환 또는 `bgmSource.volume` 직접 보간.
- 0.4s 내 다중 획득 시 더킹 윈도우 자동 연장 (마지막 트리거 +0.4s, 최대 1.2s).

---

## 2. 수치/상수 (단일 소스)

| 상수                          | 값       | 위치                | 비고                                 |
| --------------------------- | ------- | ----------------- | ---------------------------------- |
| `BgmDuckAttenuationDb`      | **-6dB** | `GameConfig.Audio` | 선형 0.501 (10^(-6/20))                |
| `BgmDuckAttackTime`         | **0.05s** | `GameConfig.Audio` | 빠른 진입                              |
| `BgmDuckHoldTime`           | **0.30s** | `GameConfig.Audio` | 픽업 임팩트 유지                          |
| `BgmDuckReleaseTime`        | **0.10s** | `GameConfig.Audio` | 부드러운 복귀                            |
| `BgmDuckMaxStackedHold`     | **1.20s** | `GameConfig.Audio` | 다중 획득 시 hold 누적 상한                  |
| `BgmDuckEnabled`            | **true**  | `GameConfig.Audio` | 옵션 토글 가능 (옵션창 향후 추가 시)              |

총 길이 기본 = 0.05 + 0.30 + 0.10 = **0.45s** (BACKLOG의 "0.4초"는 hold+release ≈ 0.4 의미).

---

## 3. 연동 경로 (변경 범위)

| 파일/위치                                           | 변경 내용                                                                  |
| ---------------------------------------------- | ---------------------------------------------------------------------- |
| `Assets/Scripts/Core/GameConfig.cs`            | `Audio` 중첩 클래스에 §2 6개 상수 추가                                              |
| `Assets/Scripts/Systems/AudioManager.cs`       | 신규 `DuckBGM(float attackS, float holdS, float releaseS, float dbAtten)` 메서드 + 활성 더킹 추적 멤버 (`_duckEndTime`, `_duckCoroutine`) |
| `Assets/Scripts/Systems/LootSystem.cs`         | `OnItemPickup(ItemDef, int)` 처리 후 `AudioManager.Instance?.DuckBGM(...)` 1라인 |
| `Assets/Resources/Audio/Mixer/MainMixer.mixer` | (선택) AudioMixer 스냅샷 `BgmDucked` 추가 — 코드 직접 보간으로 대체 가능. v1은 코드 보간 채택 |

비변경 (영향 검토만):
- 기존 `bgmSource.volume`은 fade 코루틴이 사용 — 더킹 코루틴과 충돌 시 더킹 우선. 양쪽 활성 시 더킹 코루틴이 fade 결과를 덮어쓰므로 BGM 전환 직후 더킹 트리거 상호작용 점검 필요.

---

## 4. 데이터 구조

### AudioManager 더킹 추적

```csharp
float _duckBaselineVolume;   // 더킹 시작 시점 BGM 볼륨 스냅샷
float _duckEndTime;          // hold 종료 예정 시각 (Time.unscaledTime)
Coroutine _duckCoroutine;
```

`DuckBGM` 호출이 누적되면 `_duckEndTime`을 `max(_duckEndTime, now + holdS + releaseS)` 로 연장 (단, baselineVolume - now < BgmDuckMaxStackedHold 조건 내).

### GameConfig.Audio (예시)

```csharp
public static class Audio
{
    public const float BgmDuckAttenuationDb   = -6f;
    public const float BgmDuckAttackTime      = 0.05f;
    public const float BgmDuckHoldTime        = 0.30f;
    public const float BgmDuckReleaseTime     = 0.10f;
    public const float BgmDuckMaxStackedHold  = 1.20f;
    public const bool  BgmDuckEnabled         = true;
}
```

---

## 5. UI / 와이어프레임

**UI 변경 없음.** 청각 효과만 추가.

```
플레이어 아이템 픽업 트리거
  → LootSystem.OnItemPickup(def, count) [기존]
    → DamageText 표시 / sfx_item_pickup 재생 [기존]
    → AudioManager.DuckBGM(0.05, 0.30, 0.10, -6f) [신규, 1라인]
       └─ BGM 볼륨 -6dB → 유지 → 원복
```

---

## 6. 세이브 연동

**없음.** 더킹 자체는 런타임 1회성. 옵션 토글(`BgmDuckEnabled`)이 추후 옵션창에 추가될 경우 `OptionsData.bgmDuckEnabled` 필드 1개 세이브 — 본 SPEC 범위 외.

---

## 7. 테스트 계획

### EditMode (NUnit)

1. `AudioManagerTest.DuckBGM_AttenuatesByExpectedDb()` — 더킹 호출 후 `_duckBaselineVolume * 0.501` ±5% 검증.
2. `AudioManagerTest.DuckBGM_RestoresBaselineAfterRelease()` — 0.5s 시뮬 후 볼륨이 baseline 복귀.
3. `AudioManagerTest.DuckBGM_RestackedCallsExtendHoldUpToMax()` — 0.1s 간격 5회 호출 시 `_duckEndTime - startTime ≤ MaxStackedHold`.
4. `AudioManagerTest.DuckBGM_DisabledFlag_NoOp()` — `BgmDuckEnabled=false` 시 볼륨 변화 없음.

### PlayMode 수동
- 일반 아이템 픽업 → BGM 살짝 줄어들고 SFX 도드라짐.
- 보스 처치 직후 5+ 아이템 동시 픽업 → 더킹 1.2s 이내로 연장, 폭주 없음.
- 아이템 픽업 직후 BGM 트랜지션(다른 지역 진입) 발생 → 더킹/페이드 충돌 없이 새 BGM 정상 재생.

---

## 8. 호출 진입점 (재명시)

| 진입점                              | 트리거 조건                                       |
| -------------------------------- | -------------------------------------------- |
| `LootSystem.OnItemPickup(def, count)` | 플레이어가 드롭된 아이템 ItemPickup 콜리전 진입 (기존)         |
| `AudioManager.DuckBGM(...)`     | OnItemPickup 처리 후 1회. 인자 4개 §2 상수            |

신규 UI 진입점 없음 — 기존 픽업 플로우에 사운드 라인 1개 추가.

---

## 9. 작업 분담

| 영역 | 담당 | 산출물 |
| --- | --- | --- |
| `GameConfig.Audio` 상수 | Dev-Backend | 6개 const |
| `AudioManager.DuckBGM` 메서드 + 코루틴 | Dev-Backend | 약 40~60라인 |
| `LootSystem.OnItemPickup` 호출 라인 | Dev-Backend | 1라인 + flag 가드 |
| EditMode 테스트 4건 | Dev-Backend | NUnit |
| AudioMixer 스냅샷 (선택) | Asset/QA | v1 미사용 — 코드 보간 채택 시 생략 |

---

## 10. 리스크

- **BGM Crossfade 도중 더킹 트리거:** 두 코루틴이 같은 `bgmSource.volume`을 보간 → 충돌. 해결: `DuckBGM`은 `_fadeCoroutine` 활성 시 baseline을 fade target으로 추정 (next-frame snapshot) 또는 release 시 baseline 재계산.
- **-6dB 부족/과다:** 실측 후 -4 ~ -8dB 범위 조정. 상수만 수정 → 재컴파일.
- **더킹 코루틴 누수:** hold 연장 시 `_duckCoroutine` 재시작 — `StopCoroutine` 누락 시 다중 코루틴 동작. Stop+Start 패턴 명시.
- **AudioMixer 미사용 환경:** 현재 `bgmSource.volume` 직접 보간 → 향후 AudioMixer 도입 시 메서드 시그니처 유지 + 내부 구현 교체로 흡수.

---

## 11. 완료 조건 (DoD)

1. `GameConfig.Audio` 6개 상수 추가.
2. `AudioManager.DuckBGM` 메서드 + 코루틴 + 멤버 추가.
3. `LootSystem.OnItemPickup`에 호출 라인 추가 (`BgmDuckEnabled` 가드).
4. EditMode 테스트 4건 GREEN.
5. PlayMode 수동 검증 — 단일/다중 픽업, BGM 트랜지션 도중 픽업 모두 안전 동작.
6. SFX/Ambient 채널 영향 없음 (BGM만 더킹) 확인.
