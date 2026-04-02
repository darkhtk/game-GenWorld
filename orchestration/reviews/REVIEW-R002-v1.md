# REVIEW-R002-v1: 오브젝트 풀링 (Projectile/DamageText)

> **리뷰 일시:** 2026-04-02
> **태스크:** R-002 오브젝트 풀링
> **스펙:** SPEC-R002
> **판정:** ❌ NEEDS_WORK

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 불필요 (런타임 전용) |
| 컴포넌트/노드 참조 | ✅ | ObjectPool.cs, Projectile.cs 참조 정상 |
| 에셋 존재 여부 | ✅ | 코드만 변경, 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| SPEC 명세 부합 | ❌ | **호출자 미갱신** — 아래 상세 |
| 기존 코드 호환 | ⚠️ | 풀 인프라는 호환되나, 실제 사용되지 않음 |
| 아키텍처 패턴 | ✅ | ObjectPool<T> 범용 설계 양호 |
| 테스트 커버리지 | ❌ | 테스트 미작성 |

### 상세: 미완료 항목

**1. ActionRunner.cs (line 264-265) — 미갱신**
```csharp
// 현재 (스펙 위반):
var go = new GameObject("SkillProjectile");
var proj = go.AddComponent<Projectile>();

// 스펙 요구:
var proj = Projectile.Get();
```

**2. CombatManager.cs (line 124-125) — 미갱신**
```csharp
// 현재 (스펙 위반):
var go = new GameObject("MonsterProjectile");
var proj = go.AddComponent<Projectile>();

// 스펙 요구:
var proj = Projectile.Get();
```

**3. DamageText.cs — 풀링 미구현**
- `AnimateText()` (line 28): 여전히 `new GameObject("DamageText")`
- line 63: 여전히 `Object.Destroy(go)`
- 스펙 요구: DamageText용 ObjectPool 적용, Return으로 회수

### 완료된 항목

**1. ObjectPool.cs — ✅ 정상 구현**
- Generic `ObjectPool<T>` where T : Component
- Get(): null 체크 포함, auto-expand 지원
- Return(): SetActive(false) + Enqueue
- Prewarm으로 초기 풀 생성
- `_totalCreated` 트래킹으로 maxSize 제한

**2. Projectile.cs — ✅ 풀 인프라 구현**
- static `_pool`, `EnsurePool()` lazy init (초기 20, 최대 50)
- `Get()` static 메서드, `ReturnToPool()` 메서드
- `Init()`에서 상태 리셋 (_hitIds.Clear(), 콜백 null)
- 타임아웃/도착/히트 시 ReturnToPool() 호출
- 풀 없을 때 fallback Destroy — 안전

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 해당 없음 | — | 백그라운드 시스템, UI 연동 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 스킬 발사 (ActionRunner) | ❌ 풀 미사용, 매번 new GameObject → GC 스파이크 그대로 |
| 몬스터 원거리 공격 (CombatManager) | ❌ 풀 미사용, 매번 new GameObject |
| 데미지 텍스트 표시 | ❌ 풀 미적용, 매번 Instantiate+Destroy |
| Projectile.Get() 직접 호출 시 | ✅ 정상 동작 예상 (코드 자체는 올바름) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
솔직히 차이를 모르겠다. 아직 바뀐 게 없으니까... 전투하면 여전히 끊길 텐데?

### ⚔️ 코어 게이머
풀링 시스템 자체의 설계는 괜찮다. 초기 20/최대 50, DamageText 30/80 수치도 합리적. 문제는 실제로 아무 데서도 안 쓰고 있다는 것. ActionRunner와 CombatManager가 여전히 매 프레임 new GameObject 하고 있으니 GC 스파이크는 그대로. Projectile.cs에 Get()이 있는데 아무도 호출 안 함.

### 🎨 UX/UI 디자이너
DamageText가 풀링되지 않아 많은 데미지 텍스트가 동시에 뜨는 장면에서 끊김 발생 가능. 크리티컬 히트 연출이 GC 때문에 뚝뚝 끊기면 타격감이 죽는다.

### 🔍 QA 엔지니어
- Projectile 풀은 `DontDestroyOnLoad` 적용됨 — 씬 전환 안전
- ObjectPool.Get()에서 null 반환 가능 (maxSize 초과 시) — 호출자가 null 체크 필요하나 현재 호출자가 없어 미검증
- DamageText 코루틴 기반이라 풀링 전환 시 코루틴 → MonoBehaviour 구조 변경 필요할 수 있음

---

## 미해결 사항 (필수 수정)

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | **Critical** | ActionRunner.cs line 264-265: `new GameObject` → `Projectile.Get()` 변경 필요 |
| 2 | **Critical** | CombatManager.cs line 124-125: `new GameObject` → `Projectile.Get()` 변경 필요 |
| 3 | **Critical** | DamageText.cs: 풀링 미구현 — SPEC 요구사항 미충족 |
| 4 | Medium | Projectile.Get() null 반환 시 호출자 방어 코드 필요 |
| 5 | Low | 테스트 미작성 (SPEC 체크리스트 5항목) |

---

## 최종 판정

**❌ NEEDS_WORK**

풀 인프라(ObjectPool.cs, Projectile.cs 풀 구조)는 올바르게 구현되었으나, **SPEC에 명시된 4개 수정 파일 중 3개(ActionRunner, CombatManager, DamageText)의 호출부가 갱신되지 않았다.** 현재 상태에서는 풀이 존재하지만 사용되지 않아 GC 스파이크 제거라는 핵심 목표가 달성되지 않음.

Critical 3건 수정 후 재리뷰 요청 바람.
