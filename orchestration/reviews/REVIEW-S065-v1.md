# REVIEW-S065-v1: EffectHolder DoT 중복 적용

> **리뷰 일시:** 2026-04-03
> **태스크:** S-065 EffectHolder DoT 중복 적용
> **스펙:** SPEC-S-065
> **커밋:** `deb6cbe`
> **판정:** ✅ APPROVE

---

## 변경 요약

2개 파일 변경:

1. **EffectSystem.cs** — `ApplyDot()`에 재적용 로직 추가: duration 연장 + 강한 damage 유지 + lastTick 보존.
2. **EffectSystemTests.cs** — DoT 재적용 테스트 4건 추가.

---

## 검증 1: 엔진 검증

| 항목 | 스펙 요구 | 구현 | 판정 |
|------|----------|------|------|
| 동일 damage 재적용 → duration 연장 | expiresAt = max(현재, 새것) | `Mathf.Max(existing.expiresAt, expiresAt)` | **PASS** |
| 강한 damage 재적용 → damage 갱신 + 연장 | damage > existing.value → overwrite | `if (damage > existing.value) existing.value = damage` | **PASS** |
| 약한 damage 재적용 → 현재 damage 유지 + 연장 | 기존 damage 보존 | 조건 불충족 시 값 미변경 | **PASS** |
| lastTick 보존 | 재적용 시 틱 리듬 유지 | 기존 객체 in-place 수정 (새 객체 생성 안 함) | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 ApplyDot 재적용 로직 (EffectSystem.cs:58-65)

```csharp
if (_effects.TryGetValue("dot", out var existing))
{
    existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
    existing.totalDuration = existing.expiresAt - now;
    if (damage > existing.value)
        existing.value = damage;
    return;
}
```

**분석:**
- `TryGetValue`로 기존 DoT 존재 확인 — 딕셔너리 이중 조회 방지
- `Mathf.Max`로 더 긴 duration 채택 — 짧은 재적용이 기존 duration 단축하지 않음
- `totalDuration` 재계산으로 UI 표시(잔여 시간 바 등)와 정합성 유지
- `damage > existing.value` 조건으로 강한 DoT만 갱신 — 약한 재적용은 무시
- `existing` 객체를 직접 수정하므로 `lastTick`, `interval` 보존 — **핵심 요구사항 충족**
- `return`으로 새 ActiveEffect 생성 차단 — 기존 객체 재사용

### 2.2 기존 stun/slow 재적용과 비교

| 이펙트 | 재적용 정책 | 패턴 |
|--------|-----------|------|
| Stun | duration 연장, 10초 상한 | `Mathf.Max + Mathf.Min` |
| Slow | 강한 슬로우 유지, duration 연장 | `Mathf.Min(value) + Mathf.Max(expires)` |
| DoT (v1) | **덮어쓰기** | 새 객체 생성 |
| DoT (수정) | **duration 연장 + 강한 damage 유지** | 기존 객체 수정 |

수정 후 DoT가 stun/slow와 동일한 "기존 객체 수정" 패턴을 따름 — **일관성 양호**.

### 2.3 ActionRunner 연동 (ActionRunner.cs:191-193)

```csharp
case "dot":
    m.Effects.ApplyDot(ctx.now + dur,
        Mathf.RoundToInt(ctx.stats.atk * (a.value > 0 ? a.value : 0.5f)));
    break;
```

- 멀티히트 스킬이 같은 몬스터에 DoT를 연속 적용할 때, 이제 duration이 연장되고 강한 damage가 유지됨
- `interval` 파라미터 미전달 → 기본값 1000f 사용 — 재적용 시 interval 변경 없음 (기존 객체의 interval 보존으로 동작 일관)

---

## 검증 3: UI 추적

| 경로 | 영향 | 판정 |
|------|------|------|
| 몬스터 HP 바 DoT 표시 | totalDuration 재계산으로 잔여 시간 정확 | **PASS** |
| 데미지 텍스트 | DoT 틱 damage 표시 — lastTick 보존으로 이중 틱 없음 | **PASS** |
| 버프/디버프 아이콘 | expiresAt 갱신으로 아이콘 지속 시간 정확 | **PASS** |

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|----------|------|
| 독 스킬 → 같은 몬스터에 2연속 적용 | duration 연장, damage 유지, 틱 리듬 유지 | **PASS** |
| 약한 독 → 강한 독 재적용 | damage 상향, duration 연장 | **PASS** |
| 강한 독 → 약한 독 재적용 | 강한 damage 유지, duration만 연장 | **PASS** |
| 독 진행 중 보스전 장기 전투 | lastTick 보존으로 안정적 틱 간격 유지 | **PASS** |
| 독 만료 후 재적용 | 새 ActiveEffect 생성 (기존 경로) | **PASS** |

---

## 테스트 검증

| 테스트 | 검증 대상 | 판정 |
|--------|----------|------|
| Dot_SameDamage_ExtendsDuration | 동일 damage 재적용 시 duration 연장 | **PASS** |
| Dot_StrongerDamage_Overwrites | 강한 damage 재적용 시 damage 갱신 + 연장 | **PASS** |
| Dot_WeakerDamage_KeepsCurrent | 약한 damage 재적용 시 기존 damage 유지 + 연장 | **PASS** |
| Dot_Reapply_PreservesLastTick | 재적용 후 lastTick 미리셋 확인 (틱 간격 보존) | **PASS** |

4건 모두 스펙의 재적용 정책을 정확히 검증. 특히 `Dot_Reapply_PreservesLastTick`이 핵심 불변성(틱 리듬 유지)을 검증하여 커버리지 양호. 기존 13건 테스트 회귀 없음.

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
독 스킬 연속 사용 시 이전에는 이상하게 데미지가 리셋되거나 틱이 꼬이는 느낌이 있었을 수 있으나, 이제 자연스럽게 지속시간만 연장됨. 체감상 독 스킬이 더 직관적.

### ⚔️ 코어 게이머
DoT 재적용 정책이 명확해짐: 강한 독이 유지되고 약한 독은 duration만 연장. 밸런스 관점에서 "가장 강한 DoT 유지" 정책은 합리적. 틱 리듬 보존으로 DPS 계산이 예측 가능.

### 🎨 UX/UI 디자이너
totalDuration 재계산으로 디버프 아이콘의 남은 시간 표시가 정확. 시각적 피드백 일관성 유지.

### 🔍 QA 엔지니어
4건 테스트가 모든 재적용 시나리오를 커버. lastTick 보존 테스트가 특히 중요 — 이중 틱 버그를 방지. stun/slow와 동일한 패턴으로 코드 일관성 확보. 기존 테스트 13건 회귀 없음 확인.

---

## 종합 판정

### ✅ APPROVE

스펙의 4가지 재적용 정책(동일/강한/약한 damage + lastTick 보존) 전부 정확히 구현. 기존 stun/slow 패턴과 일관된 in-place 수정 방식. 테스트 4건으로 모든 경계 조건 커버. 최소한의 변경(8줄)으로 명확한 문제 해결.
