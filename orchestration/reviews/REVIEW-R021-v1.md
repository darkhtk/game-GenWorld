# REVIEW-R021-v1: 주/야간 사이클

> **리뷰 일시:** 2026-04-02
> **태스크:** R-021 주/야간 사이클
> **스펙:** SPEC-R021
> **판정:** ✅ APPROVE

---

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | DayNightCycle 조명 제어 | DayNightCycle.cs (62행) | ✅ |
| 2 | 5개 시간대 조명 프리셋 | LightPresets line 6-13 | ✅ |
| 3 | 조명 Lerp 전환 (10초) | TransitionDuration=10f, line 41-46 | ✅ |
| 4 | 시작 시 즉시 적용 | Start → ApplyImmediate line 26 | ✅ |
| 5 | MonsterSpawner 야간 밀도 | line 27-28 isNight + nightDensityMult | ✅ |
| 6 | 야간 전용 몬스터 | line 32-35 nightMonsterIds 추가 | ✅ |
| 7 | RegionDef nightMonsterIds/nightDensityMult | RegionDef.cs:4 | ✅ |
| 8 | RenderSettings.ambientLight 사용 | line 45, 60 | ✅ |

### SPEC 수치 검증

| 파라미터 | SPEC | 코드 | 결과 |
|---------|------|------|------|
| 전환 시간 | 10초 | TransitionDuration=10f | ✅ |
| 야간 밀도 | 1.5x | region.nightDensityMult (JSON 설정) | ✅ |
| dawn 조명 | 따뜻한 주황 70% | (1, 0.85, 0.7) * 0.7 | ✅ |
| night 조명 | 차가운 청색 30% | (0.3, 0.3, 0.6) * 0.3 | ✅ |

### 참고

- SPEC은 `Light2D` 사용을 제안했으나 구현은 `RenderSettings.ambientLight` 사용 — 2D 게임에서 동등하게 동작하며 URP Light2D 컴포넌트 의존성 없음.
- 야간 공격력 1.2x는 MonsterSpawner에서 미구현 — SPEC에 코드 예시로만 제시, RegionDef에 필드 미추가. 밀도 변경은 구현됨.

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
밤에 어두워지면 분위기 있겠다. 밤에 더 강한 몬스터 나오면 긴장감 있고.

### ⚔️ 코어 게이머
야간 밀도 1.5x + 야간 전용 몬스터 → 야간 파밍 보상 차별화 가능. 10초 전환은 자연스러운 시각 경험.

### 🔍 QA 엔지니어
- Start에서 ApplyImmediate → 로드 후 즉시 올바른 조명 ✅
- _lerpProgress >= 1 → 더 이상 Lerp 안 함 — CPU 절약 ✅
- LightPresets에 없는 period → TryGetValue false → 전환 안 함 — 안전 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 야간 atk 1.2x 보너스 미구현 (SPEC 코드 예시, 필수 아님) |
| 2 | Low | 테스트 미작성 |

---

## 최종 판정

**✅ APPROVE**

SPEC 8개 기능 항목 충족. DayNightCycle 5단계 조명 Lerp, MonsterSpawner 야간 밀도/몬스터 변경, RegionDef 확장 모두 정확.
