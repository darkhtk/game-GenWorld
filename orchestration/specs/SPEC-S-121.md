# SPEC-S-121: 🎨 NPC 대화 시작/종료 SFX

> **작성:** 2026-04-30 (Coordinator, 4회차)
> **출처:** BACKLOG_RESERVE.md S-121 (🎨 SFX, P3)
> **우선순위:** P3
> **상태:** Spec Drafted → 구현 대기 (Asset/QA SFX 자산 + Dev-Backend 호출 라인)
> **호출 진입점:**
> - 시작: `DialogueUI.Show(NpcDef, ConditionalDialogue?)` 진입 시 — `AudioManager.PlaySFX("sfx_dialogue_open")`
> - 종료: `DialogueUI.Hide()` 진입 시 — `AudioManager.PlaySFX("sfx_dialogue_close")`

---

## 1. 문제 정의

현재 `DialogueUI.Show`는 진입 시 일반 음성 SFX `sfx_speech`만 재생 (`DialogueUI.cs:122`). `Hide`는 SFX 없음. 결과:

1. 대화 패널이 열리는 순간의 청각적 컨펌 부족 — UI 패널 페이드/슬라이드 인이 무음.
2. 대화 종료(닫기) 피드백 부재 — 사용자가 ESC로 닫았는지 패널이 사라졌는지 확신하기 어려움.
3. NPC별 음성 SFX(`sfx_speech`)와 UI 효과음이 혼재 → 청각 식별 어려움.

목표:
- **시작:** `sfx_dialogue_open` (UI 페이지 펼치는 종이 소리, 200ms, 800~1600Hz 필터드 노이즈 + 가벼운 attack)
- **종료:** `sfx_dialogue_close` (반대 방향, 180ms, decrescendo)
- **NPC 음성 SFX(`sfx_speech`)와 분리** — 대화 진행 중 매 라인 음성은 그대로 유지, UI 진입/종료 효과음만 추가.
- 자산 부재 시 LogWarning + NoOp (기존 AudioManager 패턴).

---

## 2. 수치/상수 (단일 소스)

| 상수                          | 값                       | 위치                | 비고                      |
| --------------------------- | ----------------------- | ----------------- | ----------------------- |
| `DialogueOpenSfxName`       | `"sfx_dialogue_open"`   | `GameConfig.Audio` | 200ms                   |
| `DialogueCloseSfxName`      | `"sfx_dialogue_close"`  | `GameConfig.Audio` | 180ms                   |
| `DialogueOpenSfxVolume`     | **0.85f**                | `GameConfig.Audio` | 음성보다 약간 작게               |
| `DialogueCloseSfxVolume`    | **0.70f**                | `GameConfig.Audio` | 종료는 더 약하게               |
| `DialogueSfxEnabled`        | **true**                 | `GameConfig.Audio` | 옵션창 토글 향후 추가 시 reuse   |

자산 (Asset/QA 생성):
- `Assets/Audio/Generated/sfx_dialogue_open.wav` — 16-bit PCM mono 44.1kHz, 200ms, 약 17.6KB
- `Assets/Audio/Generated/sfx_dialogue_close.wav` — 동일 spec, 180ms, 약 15.8KB
- `.meta` 신규 GUID 2개 (Unity 표준 AudioImporter, S-117/S-118 패턴 동일)

---

## 3. 연동 경로 (변경 범위)

| 파일/위치                                            | 변경 내용                                                            |
| ----------------------------------------------- | ---------------------------------------------------------------- |
| `Assets/Audio/Generated/sfx_dialogue_open.wav`  | **신규** — Asset/QA 생성 (gen_dialogue_sfx.py 결정론적 스크립트 권장)            |
| `Assets/Audio/Generated/sfx_dialogue_close.wav` | **신규** — 동일                                                      |
| `*.meta` 2종                                     | **신규** — Unity AudioImporter 표준                                  |
| `Assets/Scripts/Core/GameConfig.cs`             | `Audio` 클래스에 §2 5개 상수 추가                                          |
| `Assets/Scripts/UI/DialogueUI.cs:122` 부근        | `AudioManager.PlaySFX("sfx_speech")` 직전에 `PlaySFX("sfx_dialogue_open", 0.85f)` 1라인 추가. `sfx_speech` 호출은 NPC 데이터에 따라 유지 또는 conditional |
| `Assets/Scripts/UI/DialogueUI.cs:139` 부근        | `panel.SetActive(false)` 직전에 `AudioManager.Instance?.PlaySFX("sfx_dialogue_close", 0.70f)` 1라인 추가 |
| `Assets/Scripts/Systems/AudioManager.cs`        | `PlaySFX(string, float volumeScale)` 오버로드 부재 시 추가 (이미 있으면 SKIP)                  |

비변경 (영향 검토만):
- `sfx_speech` 호출은 그대로 유지 — `sfx_dialogue_open`은 panel SetActive와 동시에, `sfx_speech`는 NPC 음성 큐 시작과 함께 (즉 둘이 50~80ms 차이로 거의 동시 재생). 청각 분리 가능 여부 PlayMode 검증 필요.

---

## 4. 데이터 구조

### GameConfig.Audio (S-118/S-120 클래스에 추가)

```csharp
public static class Audio
{
    // 기존 S-118/S-120 상수들 ...

    // S-121 신규
    public const string DialogueOpenSfxName    = "sfx_dialogue_open";
    public const string DialogueCloseSfxName   = "sfx_dialogue_close";
    public const float  DialogueOpenSfxVolume  = 0.85f;
    public const float  DialogueCloseSfxVolume = 0.70f;
    public const bool   DialogueSfxEnabled     = true;
}
```

### 자산 합성 가이드 (gen_dialogue_sfx.py)

**sfx_dialogue_open (200ms, 종이/책 펼침 느낌):**
- 0~50ms: noise burst (high-pass 800Hz, attack 5ms, decay exp)
- 50~150ms: pluck (sine 1200Hz → 600Hz exponential sweep, amp envelope decay)
- 150~200ms: tail (low-pass 1600Hz, soft fadeout)
- 결정론적: `random.Random(20121)`

**sfx_dialogue_close (180ms, 종이 접힘 / 책 덮음):**
- 0~30ms: muted attack (sine 600Hz → 300Hz, amp 0.5)
- 30~140ms: decay tail (noise low-pass 600Hz, decrescendo exp)
- 140~180ms: silence ramp
- 결정론적: `random.Random(20122)`

---

## 5. UI / 와이어프레임

**UI 변경 없음.** 청각 효과만.

```
플레이어 NPC 상호작용 (E 키 또는 클릭) [기존]
  → NpcInteraction.OnInteract → DialogueUI.Show(npcDef, conditional)
    → panel.SetActive(true) [기존, DialogueUI.cs:96]
    → 🆕 AudioManager.PlaySFX("sfx_dialogue_open", 0.85f) [신규]
    → npcPortrait/name 세팅 [기존]
    → AudioManager.PlaySFX("sfx_speech") [기존, DialogueUI.cs:122]
    → AppendLog/ShowOptions [기존]

  ... 대화 진행 ...

플레이어 ESC 또는 닫기 버튼 [기존]
  → DialogueUI.Hide()
    → coroutines/panels 정리 [기존, DialogueUI.cs:135~138]
    → 🆕 AudioManager.PlaySFX("sfx_dialogue_close", 0.70f) [신규]
    → panel.SetActive(false) [기존]
    → gameObject.SetActive(false) [기존]
```

타임라인:
- `t=0ms`: panel 활성화 + `sfx_dialogue_open` 재생 시작
- `t=50ms`: NPC 초상화/이름 세팅 완료
- `t=80ms`: `sfx_speech` 재생 시작 (음성 인사)
- `t=200ms`: `sfx_dialogue_open` 종료, `sfx_speech` 진행 중
- 종료 시: `sfx_dialogue_close` 180ms → panel 즉시 비활성화 (SFX는 AudioSource에서 계속 재생됨, OneShot)

---

## 6. 세이브 연동

**없음.** 런타임 1회성. 옵션 토글(`DialogueSfxEnabled`)이 추후 옵션창에 추가될 경우 `OptionsData.dialogueSfxEnabled` 1개 — 본 SPEC 범위 외.

---

## 7. 테스트 계획

### EditMode (NUnit)

1. `DialogueUITest.Show_PlaysOpenSfx()` — `Show(npcDef, null)` 호출 시 mock AudioManager에 `PlaySFX("sfx_dialogue_open", 0.85f)`가 1회 호출되는지 검증.
2. `DialogueUITest.Hide_PlaysCloseSfx()` — `Hide()` 호출 시 mock AudioManager에 `PlaySFX("sfx_dialogue_close", 0.70f)`가 1회 호출되는지 검증.
3. `DialogueUITest.ShowSecondTime_PlaysOpenAgain()` — Show → Hide → Show 시 open SFX가 2회 호출되는지 (`Hide` 내부 첫 라인의 `if (panel.activeSelf) Hide()` 가드와 충돌 검증).
4. `DialogueUITest.SfxDisabledFlag_NoOp()` — `DialogueSfxEnabled=false`일 때 PlaySFX 호출 없음.

### PlayMode 수동
- 마을 NPC 대화 시작/종료 — open/close SFX 명확히 들림.
- 빠르게 ESC 연타 (Hide → 즉시 다른 NPC Show) — open SFX가 close SFX와 짧게 겹치지만 청각적 거슬림 없음.
- BGM 더킹(S-118) 활성 중 대화 시작 — 더킹 영향 없이 SFX 채널 정상 재생.
- 옵션창에서 SFX 볼륨 0 → 두 SFX 모두 무음.

### Asset 검증
- `sfx_dialogue_open.wav` 16-bit PCM mono 44.1kHz, 길이 0.18~0.22s 범위.
- `sfx_dialogue_close.wav` 16-bit PCM mono 44.1kHz, 길이 0.16~0.20s 범위.
- 두 파일 모두 0dBFS clipping 없음 (peak ≤ -1dBFS 권장).

---

## 8. 호출 진입점 (재명시)

| 진입점                                       | 트리거 조건                                              |
| ----------------------------------------- | --------------------------------------------------- |
| `DialogueUI.Show(NpcDef, ConditionalDialogue?)` | NPC 상호작용 진입 (NpcInteraction에서 호출, 기존)                   |
| ↳ panel.SetActive(true) 직후                | `PlaySFX("sfx_dialogue_open", 0.85f)` 추가             |
| `DialogueUI.Hide()`                       | ESC, 닫기 버튼, 또는 다른 NPC Show 시 자동 호출 (기존)           |
| ↳ panel.SetActive(false) 직전               | `PlaySFX("sfx_dialogue_close", 0.70f)` 추가            |

신규 UI 진입점 없음 — 기존 Show/Hide 메서드에 사운드 라인 1개씩 추가.

---

## 9. 작업 분담

| 영역                          | 담당          | 산출물                                     |
| --------------------------- | ----------- | --------------------------------------- |
| `sfx_dialogue_open.wav` + .meta  | Asset/QA    | gen_dialogue_sfx.py + 자산 + meta            |
| `sfx_dialogue_close.wav` + .meta | Asset/QA    | 동일                                      |
| `GameConfig.Audio` 5개 상수 추가  | Dev-Backend | const                                    |
| `DialogueUI.Show` 호출 라인     | Dev-Backend | 1라인 + flag 가드                            |
| `DialogueUI.Hide` 호출 라인     | Dev-Backend | 1라인 + flag 가드                            |
| EditMode 테스트 4건             | Dev-Backend | NUnit                                    |

---

## 10. 리스크

- **`sfx_speech`와 동시 재생 음향 충돌:** open SFX(200ms) + speech(NPC별 가변 길이)가 50~80ms 차로 겹침. 청각 검증 필요. 부적절 시 open SFX 길이 단축 (200→150ms) 또는 sfx_speech 호출을 100ms 지연.
- **빠른 Show→Hide→Show 패턴 (ESC 연타):** Hide의 close SFX가 다음 Show의 open SFX와 거의 동시 재생 → 청각적 혼선. 해결: AudioManager가 OneShot으로 처리 (기존 PlaySFX 동작), 자연스러운 겹침 허용.
- **자산 부재:** Resources.Load 실패 시 LogWarning. PlaySFX는 NoOp (기존 AudioManager 패턴) — 게임 진행 영향 없음.
- **볼륨 균형:** 0.85/0.70은 추정값. PlayMode 후 `sfx_speech` 음량과 비교하여 조정 필요. 상수만 수정.
- **DialogueSfxEnabled 옵션:** 본 SPEC에선 const true 고정. 옵션창에 토글 추가는 별도 태스크.

---

## 11. 완료 조건 (DoD)

1. `sfx_dialogue_open.wav` + `sfx_dialogue_close.wav` 자산 생성 (Generated 폴더, .meta 포함).
2. `gen_dialogue_sfx.py` 결정론적 스크립트 (`orchestration/scripts/`).
3. `GameConfig.Audio` 5개 상수 추가.
4. `DialogueUI.Show` / `Hide`에 PlaySFX 라인 1개씩 추가 (`DialogueSfxEnabled` 가드).
5. EditMode 테스트 4건 GREEN.
6. PlayMode 수동 — 시작/종료 SFX 청취 확인, sfx_speech와 충돌 없음.
7. SFX 볼륨이 BGM 대비 적절 (PlayMode 1회 청취 후 조정 가능).
