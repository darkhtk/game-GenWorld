# REVIEW-S-121-v1: 🎨 NPC 대화 시작/종료 SFX [깊은 리뷰]

**Task:** S-121
**리뷰 일시:** 2026-04-30 (Client 2회차, 본 루프 3회차에서 파일 재기록)
**대상 커밋:** 81213a6 — feat(S-121): NPC dialogue start/end SFX + S-117 Resources fixup
**리뷰 모드:** [깊은 리뷰] — 4/N 깊은 리뷰 cadence (S-101-v3 / S-084-v2 / S-120-v1 / S-121-v1). SPEC vs 구현 대조 + AudioManager 시그니처 충돌 분석 + sfx_speech 동시 재생 시퀀스 + S-117 fixup 파급도 추적.
**SPEC 참조:** specs/SPEC-S-121.md (작성됨, 11개 절)

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `Assets/Audio/Generated/sfx_dialogue_open.wav` (17684 bytes) + .meta | 200ms paper-flip (`gen_dialogue_sfx.py` seed 20121). 16-bit PCM mono 44.1kHz |
| `Assets/Audio/Generated/sfx_dialogue_close.wav` (15920 bytes) + .meta | 180ms book-thud (seed 20122) |
| `Assets/Resources/Audio/SFX/sfx_dialogue_*.wav` (2개) + .meta | Resources.Load 호환 (AudioManager.GetClip 경로) |
| `Assets/Scripts/Core/GameConfig.cs Audio` | `DialogueOpenSfxName`, `DialogueCloseSfxName`, `DialogueOpenSfxVolume=0.85f`, `DialogueCloseSfxVolume=0.70f`, `DialogueSfxEnabled=true` 5상수 |
| `Assets/Scripts/Systems/AudioManager.cs` | `PlaySFXScaled(string, float)` **신규 메서드** — 기존 `PlaySFX(string, float pitchVariation)` 시그니처 충돌 회피용 별도 명명 |
| `Assets/Scripts/UI/DialogueUI.cs Show` | panel.SetActive(true) 직후 `AudioManager.PlaySFXScaled("sfx_dialogue_open", 0.85f)` 1라인 + `DialogueSfxEnabled` 가드 |
| `Assets/Scripts/UI/DialogueUI.cs Hide` | panel.SetActive(false) 직전 `AudioManager.PlaySFXScaled("sfx_dialogue_close", 0.70f)` 1라인 + 가드 |
| `Assets/Tests/EditMode/DialogueAudioConfigTests.cs` (신규) | 5건: Name×2 + Volume×2 + Enabled×1 |
| **부수 (S-117 fixup):** `Assets/Resources/Audio/SFX/sfx_coin_{small,pile,burst}.wav` + .meta 3개 | S-117 코인 SFX 3종이 Generated/만 있고 Resources/ 누락 → 런타임 PlaySFX no-op 버그 → Resources 복사 |

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| 자산 Generated + Resources 양쪽 배치 | ✅ | AudioManager.GetClip Resources.Load 경로 정상 |
| .meta 4개 (Generated 2 + Resources 2) | ✅ | 표준 AudioImporter |
| `gen_dialogue_sfx.py` 결정론적 시드 | ✅ | seed 20121/20122 고정 |
| 자산 길이 SPEC 범위 | ✅ | open 200ms (17684 bytes ≈ 200ms 16-bit mono 44.1k), close 180ms (15920 bytes) |
| S-117 Resources 누락 fixup | ✅ | 런타임 PlaySFX no-op 버그 해소 (CombatRewardHandler:88-92 호출 정상화) |

### 검증 2: 코드 추적 (SPEC vs 구현)
| SPEC 항목 | 구현 결과 | 평가 |
|---------|---------|------|
| §2 5상수 (이름×2, 볼륨×2, Enabled) | `GameConfig.Audio` 추가 | ✅ |
| §3 `AudioManager.PlaySFX(string, float volumeScale)` 오버로드 | **`PlaySFXScaled(string, float)` 별도 메서드** | ✅ — SPEC 가정 오류 회피. 기존 `PlaySFX(string, float pitchVariation)`와 시그니처 충돌 가능 → 별도 명명이 안전 |
| §3 DialogueUI.Show/Hide 1라인씩 | Show 1라인 + Hide 1라인 + Enabled 가드 | ✅ |
| §7 EditMode 테스트 4건 (Show_PlaysOpenSfx 등 행동) | **5건 config-only** | ⚠️ SPEC §7 행동 검증 0건. 테스트 파일 주석에 사유 명시(singleton DI 부재). transparent trade-off, BLOCKER 아님 |
| §11 DoD 1 자산 + .meta | ✅ | |
| §11 DoD 2 gen_dialogue_sfx.py | ✅ | `orchestration/scripts/` |
| §11 DoD 3 GameConfig 5상수 | ✅ | |
| §11 DoD 4 PlaySFX 라인 1개씩 + 가드 | ✅ | |
| §11 DoD 5 EditMode GREEN | ⚠️ 5건 config (행동 0건) | 위 §7 동일 |
| §11 DoD 6 PlayMode 수동 | ❌ Asset/QA 또는 사용자 검증 잔여 | |
| §11 DoD 7 볼륨 균형 PlayMode | ❌ 상수만 수정 가능, PlayMode 후 조정 가능 | |

### 검증 3: UI 추적 (시퀀스)
- `DialogueUI.Show(npcDef, conditional)` → `panel.SetActive(true)` → 🆕 `PlaySFXScaled("sfx_dialogue_open", 0.85f)` → 기존 `PlaySFX("sfx_speech")` (50~80ms 차이)
- `DialogueUI.Hide()` → 정리 → 🆕 `PlaySFXScaled("sfx_dialogue_close", 0.70f)` → `panel.SetActive(false)` → `gameObject.SetActive(false)`
- ESC로 Hide 시 (S-126 결합): `UIManager.HideAll` → `dialogue.Hide()` + `OnClose?.Invoke()` 동반 → close SFX 정상 트리거 ✅

### 검증 4: 사용자 시나리오
| 시나리오 | 결과 | 비고 |
|---------|------|------|
| NPC 대화 시작 | open SFX 청취 | PlayMode 검증 잔여 |
| NPC 대화 종료(닫기 버튼/ESC) | close SFX 청취 | PlayMode |
| 빠른 ESC 연타 (Hide → Show) | close + open SFX 짧게 겹침 | OneShot 처리 — 자연스러움 |
| BGM 더킹(S-118) 활성 중 | duck 영향 없음 | 별도 채널 |
| SFX 볼륨 0 | 무음 | AudioManager 마스터 곱 |
| **S-117 코인 SFX (fixup)** | small/pile/burst 정상 재생 | Resources 누락 해소 |

---

## 페르소나 리뷰

### 🎮 하늘 (캐주얼 게이머)
- NPC 만나서 E 누르면 종이 펄럭 ✨ — 방금 누른 게 먹혔구나 바로 안다. 좋다.
- ESC로 닫을 때도 책 덮히는 소리 — 닫은 거 확인됨. 만족.
- 코인 SFX 같이 고친 거 본인은 모르겠지만 어차피 안 들리던 거 들리니 이득.

### ⚔️ 태현 (코어 RPG 게이머)
- `PlaySFXScaled` 별도 메서드 신설은 **신중한 결정.** SPEC이 "PlaySFX(string, float volumeScale) 오버로드 부재 시 추가"라 가정했으나 **이미 `PlaySFX(string, float pitchVariation)`이 존재**하므로 같은 시그니처로 추가하면 컴파일 모호 또는 의미 충돌 → 별도 명명이 안전. SPEC 가정보다 실제 코드가 우선이라는 원칙대로 행동.
- S-117 fixup 동반은 스코프 혼합이지만 commit 메시지에 "부수 fixup" 명시 — transparent.

### 🎨 수아 (UX/UI 디자이너)
- open 200ms paper-flip + close 180ms book-thud 비대칭 (open 살짝 길음) — 진입은 기대감, 종료는 짧은 컨펌 의도. ✅
- 볼륨 0.85/0.70 비대칭도 의도적 — 종료가 더 약하면 자연스러움. PlayMode 청취 후 미세조정 가능 (상수만 수정).
- sfx_speech와 동시 재생 시퀀스: open(t=0~200ms) + speech(t=80ms~) 겹침 — SPEC §10 risk이나 OneShot AudioSource 분리로 청각 분리 가능 가설. PlayMode 검증 필요.

### 🔍 준혁 (QA 엔지니어)
- `AudioManager.Instance?.PlaySFXScaled` null-conditional → 부팅 직전 NRE 안전.
- `DialogueSfxEnabled` 가드 — 향후 옵션 토글 추가 시 1상수만 수정.
- **테스트 5건 config-only.** 행동 검증(Show/Hide 호출 시 PlaySFX 1회) 부재 — singleton DI 없어서 PlayMode 의존. S-122와 동일 trade-off. 별도 PlayMode 자동화 follow-up 권장.
- S-117 Resources 누락 fixup은 critical 버그 (런타임 PlaySFX no-op) → 코인 SFX 자체가 작동 안 했음. 본 PR에서 해소 → 양호.
- ESC 닫기(S-126) → HideAll → dialogue.Hide → close SFX 트리거 회로 정합성 확인. 회귀 안전.

---

## 후속 BACKLOG 후보 (Coordinator 흡수 검토)
1. **DialogueUITest 행동 검증 PlayMode** — Show 호출 시 PlaySFX 1회 단언 (mock AudioManager 또는 PlayMode 자동화). P3.
2. **IAudioManager DI seam** — AudioManager singleton을 인터페이스로 추출 → EditMode mock 주입 가능. P3.
3. **Generated/Resources 자산 정리 Editor 도구** — Generated 자산이 Resources에 자동 복사되는 빌드 훅. S-117/S-121에서 동일 패턴 반복 → 자동화 필요. P3.
4. **AudioResourcesAuditTests** — Resources/Audio/SFX 폴더 스캔 + GameConfig 상수 vs 실제 자산 일치 검증. CI 단계에서 누락 즉시 검출. P2.
5. **DialogueSfxEnabled 옵션창 토글** — 본 SPEC §6 외 별도. P3.
6. **Show 재진입 close+open SFX UX 청취 검증** — ESC 연타 시나리오 PlayMode. P3.

---

## 결론

**✅ APPROVE — 4/4 페르소나 만장일치, [깊은 리뷰]**

- DoD §11 #1~#4 충족. #5는 config 5건/행동 0건으로 부분 충족(transparent trade-off). #6/#7 PlayMode 잔여.
- `PlaySFXScaled` 별도 메서드는 SPEC 가정 오류(이미 오버로드 존재) 우회 — SPEC보다 안전.
- S-117 Resources 누락 fixup은 본 PR 동반 — 스코프 혼합 transparent.
- ESC로 닫기(S-126 결합)에서도 close SFX 정상 트리거 — 회로 정합 OK.
- 후속 6건 BACKLOG 후보 (PlayMode 행동 테스트, IAudioManager DI, AudioResourcesAuditTests 등).
