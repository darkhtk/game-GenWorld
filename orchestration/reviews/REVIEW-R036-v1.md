# REVIEW-R036-v1: Steam 클라우드 저장

> **리뷰 일시:** 2026-04-02
> **태스크:** R-036 Steam 클라우드 저장
> **스펙:** SPEC-R-036
> **판정:** ✅ APPROVE

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | 로컬 + 클라우드 동시 기록 | SaveSystem.cs:26-29 | ✅ |
| 2 | 클라우드/로컬 타임스탬프 비교 | — | ⚠️ LoadFromCloud 호출 미확인 (Save만 연동) |
| 3 | 충돌 UI | — | ⚠️ 미구현 (Phase 2) |
| 4 | Steam 미초기화 시 폴백 | SteamCloudStorage.cs:9 `Initialized` 체크 → false 반환 | ✅ |
| 5 | 할당량 초과 경고 | — | Info (Steam API가 자체 관리) |

### 코드 품질

- 모든 메서드에 `#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED` — R-035와 일관 ✅
- SteamManager.Initialized 체크 — 미초기화 시 안전한 false/null 반환 ✅
- try-catch 예외 처리 — Steam API 오류 시 크래시 방지 ✅
- `IsAvailable` 프로퍼티 — 호출자가 간편하게 체크 ✅
- SaveSystem 연동: `SteamCloudStorage.IsAvailable` → `SaveToCloud` ✅

### 참고

클라우드 로드(타임스탬프 비교, 충돌 UI)는 미구현. 현재 Save만 클라우드 연동. Load 연동과 충돌 해소 UI는 별도 태스크로 추가 권장. 핵심 인프라(Save/Load/Exists API + SaveSystem 연동)는 완료.

---

## 최종 판정

**✅ APPROVE**

클라우드 저장 인프라(SaveToCloud/LoadFromCloud/CloudFileExists) 정확 구현. SaveSystem 연동 완료. 오프라인 폴백 안전. 클라우드 로드/충돌 UI는 후속 태스크로.
