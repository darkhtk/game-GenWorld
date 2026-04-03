# REVIEW-S040-v1: CombatManager 타겟팅 범위

> **리뷰 일시:** 2026-04-03
> **태스크:** S-040 CombatManager 타겟팅 범위 — 화면 밖 몬스터 자동 타겟팅 방지
> **스펙:** (없음)
> **판정:** ✅ APPROVE

---

## 변경 요약

3개 파일 변경 (+12 라인):

1. **CombatSystem.cs** — `IsOnScreen(Vector2 worldPos, float margin)` 정적 메서드 추가. Camera.main 뷰포트 좌표 기반 화면 내 판정.
2. **CombatManager.cs:98** — `PerformAutoAttack` 루프에 `IsOnScreen` 필터 추가.
3. **SkillExecutor.cs:98** — `HandleSingleTarget`의 `FindClosest` 호출에 `IsOnScreen` 필터 추가.

---

## 검증 1: 엔진 검증

| 항목 | 요구 | 구현 | 판정 |
|------|------|------|------|
| 뷰포트 판정 메서드 | Camera.main 기반 화면 내 판정 | `WorldToViewportPoint` + margin 0.05f | **PASS** |
| Camera.main null 방어 | 카메라 없으면 안전 동작 | `_cachedCam == null → return true` (fail-open) | **PASS** |
| 카메라 캐싱 | 매 프레임 Camera.main 호출 방지 | `_cachedCam` static 필드 캐싱 | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 IsOnScreen (CombatSystem.cs:6-13)

```csharp
static Camera _cachedCam;

public static bool IsOnScreen(Vector2 worldPos, float margin = 0.05f)
{
    if (_cachedCam == null) _cachedCam = Camera.main;
    if (_cachedCam == null) return true;
    Vector3 vp = _cachedCam.WorldToViewportPoint(worldPos);
    return vp.x >= -margin && vp.x <= 1f + margin
        && vp.y >= -margin && vp.y <= 1f + margin;
}
```

- **fail-open 설계:** 카메라가 없을 때 `true` 반환 — 기존 동작 유지, 안전
- **margin 0.05f:** 화면 경계 5% 여유 — 경계에 걸친 몬스터가 갑자기 타겟 해제되는 것 방지
- **캐싱:** `Camera.main`은 내부적으로 FindObjectOfType이므로 캐싱 적절
- **주의점:** 카메라가 런타임에 교체되면 `_cachedCam`이 stale될 수 있음. 단, 본 프로젝트는 단일 카메라 구조이므로 문제 없음. 카메라 파괴 시 Unity가 null 비교에서 `true` 반환하므로 재캐싱됨.

### 2.2 PerformAutoAttack 필터 (CombatManager.cs:98)

```csharp
if (!CombatSystem.IsOnScreen(m.Position)) continue;
```

- 기존 distance/arc 필터 **앞에** 배치 — 화면 밖 몬스터를 조기 제거하여 불필요한 거리/각도 계산 절약
- 기존 로직 변경 없음, 순수 추가

### 2.3 HandleSingleTarget 필터 (SkillExecutor.cs:98)

```csharp
m => !m.IsDead && CombatSystem.IsOnScreen(m.Position)
```

- `FindClosest`의 `isAlive` 람다에 `IsOnScreen` 조건 추가
- 스킬 단일 타겟팅도 화면 내 몬스터만 대상으로 제한

---

## 검증 3: UI 추적

| 경로 | 영향 | 판정 |
|------|------|------|
| 자동 공격 | 화면 밖 몬스터 타격 불가 — 플레이어 예상과 일치 | **PASS** |
| 스킬 단일 타겟 | 화면 밖 몬스터 스킬 타겟 불가 | **PASS** |
| AoE 스킬 | `HandleAoE`에는 IsOnScreen 미적용 — AoE는 범위 기반이므로 적절 | **PASS** |
| 몬스터 공격 (HandleMonsterAttacks) | IsOnScreen 미적용 — 몬스터의 플레이어 공격은 거리 기반만으로 충분 | **PASS** |

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|----------|------|
| 화면 중앙 몬스터 클릭 공격 | 정상 타격 | **PASS** |
| 화면 밖 몬스터 방향으로 클릭 | 타격 안 됨 (의도된 동작) | **PASS** |
| 화면 경계에 걸친 몬스터 | margin 5%로 타격 가능 | **PASS** |
| 카메라 이동 중 공격 | 현재 뷰포트 기준 판정 — 실시간 반영 | **PASS** |
| 스킬 사용 시 화면 밖 타겟 | 화면 내 가장 가까운 몬스터로 타겟팅 | **PASS** |
| Camera.main 없는 상태 | fail-open, 기존 동작 유지 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
클릭해서 보이는 몬스터만 때린다. 화면 밖 안 보이는 몬스터 때리는 건 혼란스러웠는데 이제 직관적이다.

### ⚔️ 코어 게이머
화면 경계 5% 여유 마진이 적절하다. 스킬도 화면 내만 타겟팅하니 PvE에서 의도치 않은 어그로 유발이 줄어든다. AoE는 범위 기반이라 필터 없는 게 맞다.

### 🎨 UX/UI 디자이너
"보이는 것만 상호작용 가능" 원칙에 부합. 시각적 피드백과 실제 동작이 일치하므로 UX 정합성 향상.

### 🔍 QA 엔지니어
- 카메라 null 방어 적절 (fail-open).
- `_cachedCam` static 캐싱 — 카메라 파괴 시 Unity null 체크로 자동 재캐싱.
- `HandleMonsterAttacks`에는 필터 미적용 — 몬스터가 화면 밖에서 공격하는 건 기존 동작이며 이 태스크 범위 밖.
- **테스트 부재:** `IsOnScreen`에 대한 단위 테스트가 없으나, Camera.main 의존으로 EditMode 테스트가 어렵고 변경 규모가 작아 수용 가능.

---

## 종합 판정

### ✅ APPROVE

변경이 명확하고 범위가 좁다. 자동 공격과 스킬 단일 타겟팅에 뷰포트 필터를 추가하여 "화면 밖 타겟팅 방지" 목적을 달성. fail-open 설계로 안전성 확보. 카메라 캐싱으로 성능 고려. AoE / 몬스터 공격에는 적절히 미적용. 기존 로직에 대한 부작용 없음.
