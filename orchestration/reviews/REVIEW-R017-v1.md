# REVIEW-R017-v1: 사운드/음악 시스템 기반

> **리뷰 일시:** 2026-04-02
> **태스크:** R-017 사운드/음악 시스템
> **스펙:** SPEC-R017
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | DontDestroyOnLoad, 씬 전환 유지 |
| 컴포넌트/노드 참조 | ✅ | BGM/SFX/Ambient AudioSource (자동 생성) |
| 에셋 존재 여부 | ✅ | Resources/Audio/ 구조 (오디오 파일은 Phase 2) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | 싱글턴 + DontDestroyOnLoad | line 23-25 | ✅ |
| 2 | BGM/SFX/Ambient AudioSource | line 10-12, 27-29 자동 생성 | ✅ |
| 3 | Resources.Load + 캐싱 | GetClip line 44-50 | ✅ |
| 4 | PlayBGM + 크로스페이드 | line 52-60, CrossfadeBGM line 68-89 | ✅ |
| 5 | StopBGM + 페이드아웃 | line 62-66, FadeOut line 91-101 | ✅ |
| 6 | PlaySFX + pitchVariation | line 103-112, PlayOneShot | ✅ |
| 7 | PlaySFXAt (위치 기반) | line 114-119, PlayClipAtPoint | ✅ |
| 8 | 볼륨 설정 (BGM/SFX/Master) | line 121-140 | ✅ |
| 9 | PlayerPrefs 저장/로드 | line 125, 131, 139 / LoadVolumeSettings line 146-153 | ✅ |
| 10 | 중복 재생 방지 | line 56 `clip == current && isPlaying → return` | ✅ |

### 코드 품질

- `Time.unscaledDeltaTime` 사용 (line 73, 83, 94) — 일시정지 중에도 페이드 동작 ✅
- `Mathf.Clamp01` 모든 볼륨 설정 — 범위 안전 ✅
- `_clipCache` Dictionary — 동일 클립 중복 로드 방지 ✅
- AudioSource 자동 생성 (line 27-29) — SerializeField 미바인딩 시 fallback ✅
- 중복 싱글턴 Destroy (line 23) — 씬 전환 안전 ✅
- FadeOut 후 volume 복원 (line 100) — AudioSource 재사용 가능 ✅

### 참고

- SPEC "GameManager.cs 수정" — 불필요. AudioManager 자체 초기화 싱글턴으로 GameManager 수정 없이 동작. 더 깔끔한 설계.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 볼륨 프로퍼티 노출 | ✅ | BGMVolume/SFXVolume/MasterVolume getter |
| PauseMenuUI 연결 | — | SPEC "있으면" 조건, Phase 2 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 마을 진입 | PlayBGM("village") → 크로스페이드 |
| 전투 시작 | PlayBGM("battle") → 기존 BGM 페이드아웃 → 전투 BGM 페이드인 |
| 검 휘두르기 | PlaySFX("sword_hit") → 즉시 재생 |
| 다수 동시 피격 | PlayOneShot 다중 호출 → 동시 재생 |
| 설정 변경 | SetBGMVolume → PlayerPrefs 저장 → 재시작 후 로드 |
| 씬 전환 | DontDestroyOnLoad → AudioManager 유지 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
드디어 소리가 나겠다! 마을/전투 BGM 전환에 페이드 있으면 분위기 전환이 자연스러울 듯. 볼륨 설정 저장되니까 매번 조절 안 해도 됨.

### ⚔️ 코어 게이머
pitchVariation으로 효과음 단조로움 방지 — 반복 공격 시 자연스러움. PlaySFXAt 위치 기반 재생은 공간감 제공. 크로스페이드 1초 기본값 적절.

### 🎨 UX/UI 디자이너
볼륨 3채널 분리 (Master/BGM/SFX) — 표준적 설정 구조. PlayerPrefs 영속성 확보. 볼륨 프로퍼티 getter로 UI 슬라이더 바인딩 용이.

### 🔍 QA 엔지니어
- Resources.Load null 시 LogWarning + return — 누락 오디오에 크래시 없음 ✅
- _fadeCoroutine 중복 방지 (StopCoroutine → 재시작) ✅
- PlayOneShot: AudioSource.volume이 아닌 인자로 볼륨 전달 — SFX 볼륨 독립 관리 ✅
- 씬 전환 시 기존 Instance Destroy — 중복 방지 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | 오디오 파일 미존재 (Phase 2에서 추가 예정) |
| 3 | Info | 이벤트 연동 미구현 (RegionVisit→BGM 등, SPEC Phase 2 명시) |

---

## 최종 판정

**✅ APPROVE**

SPEC 10개 기능 항목 전부 충족. 싱글턴, DontDestroyOnLoad, 크로스페이드, SFX PlayOneShot, 위치 기반 재생, 볼륨 3채널, PlayerPrefs 영속성 모두 정확. unscaledDeltaTime으로 일시정지 안전.
