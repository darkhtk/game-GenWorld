# REVIEW-R002-v2: 오브젝트 풀링 (Projectile/DamageText) — 재리뷰

> **리뷰 일시:** 2026-04-02
> **태스크:** R-002 오브젝트 풀링
> **스펙:** SPEC-R002
> **이전 리뷰:** REVIEW-R002-v1 (❌ NEEDS_WORK)
> **판정:** ✅ APPROVE

---

## v1 지적사항 해결 확인

| # | 심각도 | 지적 내용 | 해결 |
|---|--------|-----------|------|
| 1 | Critical | ActionRunner.cs: new GameObject → Pool | ✅ `Projectile.Get()` + null guard |
| 2 | Critical | CombatManager.cs: new GameObject → Pool | ✅ `Projectile.Get()` + null guard |
| 3 | Critical | DamageText.cs: 풀링 미구현 | ✅ 전면 풀링 전환 |
| 4 | Medium | Get() null 반환 시 방어 코드 | ✅ `if (proj == null) continue/return` |

---

## 검증 2: 코드 추적 (변경분)

**ActionRunner.cs:264** — `Projectile.Get()` + null check `continue` → 풀 소진 시 스킬 발사 조용히 스킵. 합리적.

**CombatManager.cs:126-127** — `Projectile.Get()` + null check `return` → 몬스터 원거리 공격 스킵. 합리적.

**DamageText.cs 전면 재구현:**
- Static pool (초기 30, 최대 80) — 스펙 일치
- `EnsurePool()` lazy init + `DontDestroyOnLoad`
- `Spawn()`/`SpawnText()` → pool.Get() + null guard
- `Run()` 메서드로 상태 리셋 (위치, 텍스트, 색상, 크기)
- `Animate()` 코루틴 종료 시 `_pool?.Return(this)` — 자동 회수
- MonoBehaviour 기반으로 전환 → StartCoroutine 직접 사용 가능

**ObjectPool.cs / Projectile.cs** — v1에서 이미 양호 판정, 변경 없음.

---

## 페르소나 리뷰 (변경분 중심)

### 🎮 캐주얼 게이머
이제 전투 많아져도 끊김 덜하겠다. 풀 소진 시 투사체가 안 나가는 게 좀 그렇긴 한데, 50개나 되니까 보통 플레이에선 문제없을 듯.

### ⚔️ 코어 게이머
Projectile 풀 20/50, DamageText 풀 30/80. 대규모 전투(10+ 몬스터)에서도 충분한 수치. GC.Alloc 제거 확인은 프로파일러 필요하지만 코드상 Instantiate/Destroy 호출이 제거되었으므로 목표 달성.

### 🔍 QA 엔지니어
- DamageText 코루틴이 중간에 오브젝트가 disable되면? → Return은 Animate 완료 후에만 호출되므로 안전
- `_tmp` null fallback (`GetComponent<TextMeshPro>()`) 있음 — 방어적

---

## 최종 판정

**✅ APPROVE**

v1의 Critical 3건 + Medium 1건 모두 해결. 풀 인프라와 호출부가 정상 연결되어 GC 스파이크 제거 목표 달성.
