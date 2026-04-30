# SPEC-S-117: 🎨 몬스터 처치 시 골드 드롭 사운드 (등급별 3종)

> **작성:** 2026-04-30 (Coordinator)
> **출처:** BACKLOG_RESERVE.md S-117 (🎨 SFX, P3)
> **우선순위:** P3
> **상태:** Spec Drafted → 구현 대기 (Supervisor 픽업 대상, 🎨 태그)
> **호출 진입점:** `MonsterController.Die()` → `LootSystem.DropGold(monsterDef)` 직후 `AudioManager.PlaySfx(coinClipId)` 1회

---

## 1. 문제 정의

현재 모든 몬스터 처치 시 골드 드롭이 시각만 표시되고 사운드가 없거나(또는 존재해도) 일반/엘리트/보스 차별이 없음. 하이엔드 처치(엘리트/보스)에서 보상 임팩트 부족 → 게임 진행 만족도 저하(P3).

목표:
1. 일반 몬스터 처치 → `coin_small` 단조롭고 짧은 동전 1개 톤
2. 엘리트(`Def.tier == Elite`) → `coin_pile` 동전 다발 톤
3. 보스(`Def.tier == Boss`) → `coin_burst` 풍부한 골드 폭발 톤
4. 동시 처치 다수 시 사운드 클램프(0.15s 윈도우 내 동일 클립 1회)

---

## 2. 수치/상수 (단일 소스)

| 상수                          | 값       | 위치                    | 비고                                  |
| --------------------------- | ------- | --------------------- | ----------------------------------- |
| `GoldSfxClampWindow`        | **0.15s** | `GameConfig`         | 동일 클립 다중 재생 클램프                      |
| `GoldSfxVolume.Small`       | **0.65** | `AudioManager`       | 작은 동전 (FMOD/AudioMixer SFX 채널 기준)     |
| `GoldSfxVolume.Pile`        | **0.80** | `AudioManager`       | 다발 — 살짝 도드라지게                        |
| `GoldSfxVolume.Burst`       | **0.95** | `AudioManager`       | 보스 — 거의 풀 볼륨                         |
| Pitch 변동 범위 (Small/Pile)    | **±0.05** | `AudioManager.PlaySfx`| 단조 방지. Burst는 변동 없음(고유 톤 보존)         |

---

## 3. 연동 경로 (변경 범위)

| 파일/위치                                      | 변경 내용                                                                  |
| ----------------------------------------- | ---------------------------------------------------------------------- |
| `Assets/StreamingAssets/Audio/sfx/`       | 신규 wav 3종: `coin_small.wav`, `coin_pile.wav`, `coin_burst.wav` (감독관 생성) |
| `Assets/Scripts/Data/MonsterDef.cs`       | 기존 `tier` enum (Normal/Elite/Boss) 활용 — 신규 필드 없음                       |
| `Assets/Scripts/Systems/LootSystem.cs`    | `DropGold()` 후 `AudioManager.PlayGoldDropSfx(monsterDef.tier, amount)` 호출 |
| `Assets/Scripts/Systems/AudioManager.cs`  | 신규 `PlayGoldDropSfx(MonsterTier tier, int amount)` 메서드 + 클램프 윈도우 멤버    |
| `Assets/StreamingAssets/Data/audio.json`  | (이미 존재 시) `coin_small/coin_pile/coin_burst` 엔트리 추가                     |

비변경 (영향 검토만):
- `MonsterController.Die()` — 기존 LootSystem 호출 흐름 유지
- 기존 sfx_* 클립과 채널 충돌 여부: 동일 SFX 채널 사용, 동시 재생 한도(8)에 합류

---

## 4. 데이터 구조

### audio.json (예시)

```json
{
  "id": "coin_small",
  "path": "Audio/sfx/coin_small",
  "volume": 0.65,
  "pitchVariation": 0.05,
  "channel": "SFX"
}
```

3건 동일 패턴, `id` / `volume` / `pitchVariation` 만 표 §2 값으로 대치.

### MonsterTier enum (기존)

```csharp
public enum MonsterTier { Normal, Elite, Boss }
```

`MonsterTier → coinClipId` 매핑은 `AudioManager` 내부 switch (테이블 외부화 불필요, 3 케이스).

---

## 5. UI / 와이어프레임

**UI 변경 없음.** 청각 피드백만 추가. 골드 드롭 비주얼(낙하 애니메이션 / DamageText 골드 텍스트)은 기존 `LootSystem` 흐름 유지.

음향 트리거 시점:
```
몬스터 HP <= 0
  → MonsterController.Die()
    → LootSystem.DropGold(def, amount) [기존]
      → AudioManager.PlayGoldDropSfx(def.tier, amount) [신규, 1라인]
```

---

## 6. 세이브 연동

**없음.** 사운드는 런타임 1회성. 세이브 데이터 변경 없음.

---

## 7. 테스트 계획

### EditMode (NUnit)
1. `AudioManagerTest.PlayGoldDropSfx_NormalTier_PlaysCoinSmall()` — Mock AudioSource로 clip id 검증
2. `AudioManagerTest.PlayGoldDropSfx_ClampWindow_BlocksDoubleWithin150ms()`
3. `AudioManagerTest.PlayGoldDropSfx_BossTier_NoPitchVariation()` — 보스 톤 보존 검증

### PlayMode 수동
- 일반/엘리트/보스 몬스터 처치 → 각기 다른 톤 청취
- 같은 프레임 동시 5마리 처치 → 클램프로 1회만 (또는 중첩 1회 + skip 4회)

---

## 8. 호출 진입점 (재명시)

| 진입점                       | 트리거 조건                            |
| ------------------------- | --------------------------------- |
| `MonsterController.Die()` | HP ≤ 0 + Death 처리 1회               |
| `LootSystem.DropGold()`   | Die 내 호출 (이미 존재). 골드 amount > 0 분기 |
| `AudioManager.PlayGoldDropSfx(tier, amount)` | LootSystem.DropGold 직후 1회 |

신규 UI 진입점 없음 — 기존 처치 플로우에 사운드 라인 1개 추가.

---

## 9. 작업 분담

| 영역 | 담당 | 산출물 |
| --- | --- | --- |
| wav 3종 생성 | Asset/QA (Supervisor 🎨 픽업) | `coin_small.wav`, `coin_pile.wav`, `coin_burst.wav` (8/16-bit mono, ≤500ms) |
| audio.json 엔트리 | Asset/QA | 3 엔트리 추가 |
| AudioManager 메서드 | Dev-Backend | `PlayGoldDropSfx`, 클램프 멤버 |
| LootSystem 호출 라인 | Dev-Backend | 1 라인 |
| EditMode 테스트 | Dev-Backend | 3 테스트 케이스 |

---

## 10. 리스크

- **클램프 윈도우 너무 좁/넓:** 동시 처치 다발 시 부자연. 0.15s 시작 → 실측 후 조정.
- **Pitch 변동 ±0.05 단조 잔존:** 보강 시 ±0.08까지 허용. 보스는 변동 금지(고유성).
- **wav 파일 경로 누락:** `audio.json` 등록과 wav 생성 양쪽 동기화 필수. Supervisor 한 번에 처리.

---

## 11. 완료 조건 (DoD)

1. wav 3종 + audio.json 엔트리 추가
2. `AudioManager.PlayGoldDropSfx` 구현 + 단위 테스트 3건 GREEN
3. `LootSystem.DropGold` 후 호출 라인 추가
4. PlayMode 수동 검증 — 일반/엘리트/보스 사운드 차이 청취 확인
5. 동시 다수 처치 시 클램프 동작 확인
