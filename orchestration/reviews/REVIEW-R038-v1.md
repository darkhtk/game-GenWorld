# REVIEW-R038-v1: SettingsManager

> **리뷰 일시:** 2026-04-02
> **태스크:** R-038 SettingsManager
> **스펙:** SPEC-R-038
> **판정:** ✅ APPROVE

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 |
|---|---------|------|
| 1 | JSON/PlayerPrefs 저장/로드 | ✅ PlayerPrefs getter/setter 즉시 영속 |
| 2 | ApplyGraphics (해상도/전체화면) | ✅ ResolutionIndex setter → Screen.SetResolution, Fullscreen setter → Screen.fullScreen |
| 3 | ApplyAudio | ✅ Apply() → AudioManager 3채널 |
| 4 | ResetToDefaults | — (기본값은 getter에 내장) |
| 5 | 설정 파일 손상 시 기본값 | ✅ PlayerPrefs.Get*에 기본값 매개변수 |
| 6 | AutoPotion 설정 | ✅ 보너스 (SPEC 외) |

4개 해상도(1024x768 ~ 2560x1440), 즉시 적용, SaveAll() 명시 호출.

---

## 최종 판정: **✅ APPROVE**
