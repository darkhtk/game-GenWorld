# REVIEW-R035-v1: Steamworks SDK 통합

> **리뷰 일시:** 2026-04-02
> **태스크:** R-035 Steamworks SDK
> **스펙:** SPEC-R-035
> **판정:** ✅ APPROVE

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | Steamworks.NET 설치, 컴파일 에러 없음 | `#if STEAMWORKS_ENABLED` 조건부 | ✅ (미설치 시에도 안전) |
| 2 | DontDestroyOnLoad 싱글턴 | line 12-14 | ✅ |
| 3 | SteamAPI.Init() 성공 → Initialized=true | line 23-32 | ✅ |
| 4 | Steam 미실행 → 경고 + Initialized=false + 정상 진행 | line 25-28 | ✅ |
| 5 | Update에서 RunCallbacks | line 50-51 | ✅ |
| 6 | steam_appid.txt 존재 | 프로젝트 루트에 확인 | ✅ |
| 7 | `#if !DISABLESTEAMWORKS` 전처리기 | line 20, 40, 49, 57, 65 | ✅ (+ `STEAMWORKS_ENABLED` 추가 조건) |

### 추가 확인

- `SteamInitializedEvent` EventBus 발행 — 다른 시스템 연동 가능 ✅
- try-catch로 Init 예외 처리 — 안전한 폴백 ✅
- OnDestroy에서 Shutdown — 리소스 정리 ✅
- IsSteamRunning() 공개 메서드 — 상태 조회 가능 ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
Steam 없이도 게임 실행 가능한 게 좋다. 오프라인 플레이어도 배려.

### ⚔️ 코어 게이머
Steam 업적/클라우드 기반 시스템의 토대. Initialized flag로 이후 시스템(R-036/R-037)이 안전하게 분기 가능.

### 🔍 QA 엔지니어
- `STEAMWORKS_ENABLED` 이중 조건 — DISABLESTEAMWORKS만 아니라 양쪽 확인으로 확실한 분기 ✅
- 예외 발생 시에도 EventBus 발행 — 구독자가 실패 인지 가능 ✅
- 중복 Instance Destroy — 씬 전환 안전 ✅

---

## 최종 판정

**✅ APPROVE**

수용 기준 7개 전부 충족. 싱글턴, Init/RunCallbacks/Shutdown, 오프라인 폴백, 전처리기 분기, steam_appid.txt 모두 정확.
