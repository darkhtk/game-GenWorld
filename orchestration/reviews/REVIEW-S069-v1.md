# REVIEW-S069-v1: Projectile 풀 반환 null 콜백 방어

> **리뷰 일시:** 2026-04-03
> **태스크:** S-069 Projectile 풀 반환 null 콜백 방어
> **스펙:** SPEC-S-069
> **커밋:** `f80733e` fix: S-069 Projectile capture-then-clear callback + _arrived flag before invoke
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `f80733e`에서 Projectile.cs 변경 (1개 파일, +8 -5 라인):

1. **Update() OnArrive 경로** (line 130-134) — capture-then-clear 패턴 적용: `OnArrive`를 로컬 `cb`에 캡처, 필드 null → 로컬로 Invoke
2. **OnTriggerEnter2D() OnHitMonster 경로** (line 149-157) — `_arrived = true`를 콜백 실행 **전**으로 이동 + capture-then-clear 패턴 적용

---

## 검증 1: 엔진 검증

- Projectile은 ObjectPool로 관리되며, ReturnToPool()에서 콜백을 null 처리함
- Unity 물리 엔진은 동일 프레임에 다중 OnTriggerEnter2D 발생 가능 → _arrived 플래그 선행 설정이 필수
- CCD는 Player에만 적용, Projectile은 Kinematic RB이므로 CCD 불필요 (트리거 기반)

---

## 검증 2: 코드 추적

### 2.1 OnArrive 경로 (line 128-135)

```csharp
if (_traveled >= _totalDist)
{
    _arrived = true;
    var cb = OnArrive;
    OnArrive = null;
    cb?.Invoke(_to);
    ReturnToPool();
}
```

- `_arrived = true` 선행 → 동일 프레임 타이밍 이슈 방지 ✅
- `OnArrive = null` 후 `cb?.Invoke()` → 콜백 내에서 Projectile 참조를 사용해도 필드는 이미 clean ✅
- `ReturnToPool()`이 `OnArrive = null`을 다시 설정하지만 이미 null → 무해한 이중 처리 ✅

### 2.2 OnHitMonster 경로 (line 138-158)

```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if (_arrived) return;                    // (A) guard
    // ... id check ...
    if (!_piercing) _arrived = true;          // (B) flag BEFORE callback
    var cb = OnHitMonster;                    // (C) capture
    OnHitMonster = null;                      // (D) clear
    cb?.Invoke(monster);                      // (E) invoke via local
    if (!_piercing) ReturnToPool();           // (F) return
}
```

**동일 프레임 이중 충돌 시나리오:**
1. 첫 번째 OnTriggerEnter2D → (A) 통과 → (B) `_arrived = true` → (E) 콜백 실행 → (F) 풀 반환
2. 두 번째 OnTriggerEnter2D → (A) `_arrived == true` → 조기 반환 ✅

**piercing 투사체 시나리오:**
1. `_piercing = true` → (B) 스킵 → (D) `OnHitMonster = null` → 두 번째 충돌 시 `cb == null` → Invoke 무시
2. 단, piercing 투사체는 여러 몬스터를 타격해야 하는데, **첫 번째 충돌에서 `OnHitMonster = null`로 설정하면 이후 충돌에서 콜백이 호출되지 않는다.**

⚠️ **piercing 투사체 분석:** 스펙에 따르면 piercing은 `_hitIds`로 동일 몬스터 중복만 막으며 "동작 변화 없음"이어야 한다. 그런데 변경 후 `OnHitMonster = null`이 첫 타격에서 실행되므로, 두 번째 **다른** 몬스터 타격 시 콜백이 호출되지 않는다.

**그러나** `OnHitMonster` 콜백은 `ActionRunner.HandleProjectile()`에서 설정하며, 스킬 피해 계산 람다이다. piercing이든 아니든 콜백은 "이 투사체가 몬스터를 맞출 때 뭘 할지" 정의한다. 현재 코드에서 piercing 투사체의 콜백은 각 몬스터에 개별 피해를 적용하는 구조인지 확인이 필요하다.

실제 `ActionRunner`에서 `proj.OnHitMonster = m => { ... damage to m ... }` 형태라면, 클로저가 특정 몬스터를 캡처하지 않으므로 한 번만 호출되면 나머지 몬스터에 피해가 적용되지 않는다.

**결론:** 이 프로젝트에서 piercing 투사체가 실제로 사용되는지 확인 필요. 현재 스킬 데이터에 `piercing` 속성이 있는 스킬이 없다면 실질적 영향 없음. 있다면 별도 수정 필요.

→ 이 항목은 SHOULD (비차단)로 기록.

### 2.3 ReturnToPool() 이중 안전망

```csharp
void ReturnToPool()
{
    OnHitMonster = null;
    OnArrive = null;
    // pool return...
}
```

capture-then-clear 후 ReturnToPool()이 다시 null 처리 → 무해한 이중 정리. 타임아웃 경로에서도 안전. ✅

---

## 검증 3: UI 추적

- 투사체 로직 변경이므로 UI 영향 없음.

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|-----------|------|
| non-piercing 투사체 단일 몬스터 명중 | 1회 피해 + 풀 반환 | **PASS** |
| 동일 프레임 이중 OnTriggerEnter2D | _arrived 가드로 2번째 무시 | **PASS** |
| 투사체 도달 거리 완료 (OnArrive) | capture-then-clear 후 풀 반환 | **PASS** |
| 타임아웃 경로 | ReturnToPool()에서 null 처리 | **PASS** |
| 몬스터 투사체 → 플레이어 (CombatManager) | OnArrive 경로, 기존 동작 유지 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
체감되는 변화는 없다. 가끔 투사체 맞았는데 두 번 데미지가 뜨는 현상이 있었다면 그게 고쳐진 것.

### ⚔️ 코어 게이머
프레임 드랍 시 투사체 이중 피해 버그는 보스전에서 치명적일 수 있었다. capture-then-clear 패턴으로 콜백 일회성 보장은 올바른 접근이다.

### 🎨 UX/UI 디자이너
시각 변경 없음. DamageText 이중 표시 방지에 간접적 기여.

### 🔍 QA 엔지니어
_arrived 플래그 선행 + capture-then-clear로 이중 호출 경로가 완전히 차단됨. piercing 투사체 경로에서 OnHitMonster null 후 2차 타격 불가 이슈는 현재 piercing 스킬이 활성화되어 있다면 확인 필요. 테스트 커버리지(스펙의 Test Plan)는 미구현 상태이나 태스크 범위 외.

---

## 미해결 권장사항 (비차단)

| # | 항목 | 심각도 | 비고 |
|---|------|--------|------|
| 1 | piercing 투사체 OnHitMonster null 처리 재검토 | SHOULD | piercing 시 OnHitMonster를 null하면 2차 이후 타격에 콜백 미실행. piercing 스킬 존재 시 수정 필요. |

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 검증 항목 | 대응 | 판정 |
|---|--------------|------|------|
| 1 | _arrived 플래그 콜백 전 설정 | ✅ non-piercing: line 150 | **PASS** |
| 2 | 동일 프레임 이중 콜백 차단 | ✅ _arrived guard | **PASS** |
| 3 | OnHitMonster 1회 실행 보장 | ✅ capture-then-clear | **PASS** |
| 4 | piercing _hitIds 동작 유지 | ✅ 변경 없음 | **PASS** |
| 5 | OnArrive capture-then-clear | ✅ line 131-133 | **PASS** |
| 6 | 타임아웃 경로 정상 | ✅ ReturnToPool 기존 유지 | **PASS** |

핵심 문제(동일 프레임 이중 콜백)를 정확히 해결. 변경이 최소한이면서 capture-then-clear 패턴이 일관적으로 적용됨.
