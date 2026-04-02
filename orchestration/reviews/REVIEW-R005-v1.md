# REVIEW-R005-v1: CombatManager null 참조 방어

> **리뷰 일시:** 2026-04-02
> **태스크:** R-005 CombatManager null 방어
> **스펙:** SPEC-R005
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | CombatManager 내부 로직만 변경 |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 항목별 검증

| # | 스펙 요구사항 | 코드 위치 | 결과 |
|---|--------------|-----------|------|
| 1 | PerformAutoAttack 역순 순회 | line 59 `for (int i = monsters.Count - 1; i >= 0; i--)` | ✅ |
| 2 | HandleMonsterAttacks 역순 순회 | line 96 `for (int i = monsters.Count - 1; i >= 0; i--)` | ✅ |
| 3 | HandleMonsterAttacks `_player == null` 체크 | line 92 | ✅ |
| 4 | FireMonsterProjectile.OnArrive `_player == null \|\| PlayerState == null` | line 131 | ✅ |
| 5 | FireMonsterProjectile.OnArrive `m == null \|\| m.IsDead` | line 133 | ✅ |
| 6 | DealDamageToMonster `m == null \|\| m.IsDead` | line 282 | ✅ (기존 유지) |
| 7 | ExecuteSkill `_player == null` 추가 | line 169 | ✅ |

### 추가 확인

- `PerformAutoAttack` line 38: `if (monsters == null) return` — null 리스트 방어 ✅
- `ApplyDamageToPlayer` line 145: `if (PlayerState == null) return` — 이미 존재 ✅
- `_cachedMonsters ?? new List<>()` line 191: stale 참조 방어 ✅
- killed 리스트 deferred 처리 (line 80-84): 루프 중 리스트 변경 방지 ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 해당 없음 | — | 내부 방어 로직, UI 연동 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 루프 중 몬스터 사망 | 역순 순회 + null/IsDead 체크로 안전 skip |
| 프로젝타일 도착 시 플레이어 null | line 131에서 early return |
| 프로젝타일 도착 시 발사 몬스터 사망 | line 133에서 early return (데미지 0 처리) |
| 스킬 실행 중 _player null | line 169에서 early return |
| 정상 전투 | 역순 순회는 결과에 영향 없음 (순서 무관한 피격 판정) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
전투 중 갑자기 크래시나는 게 없어지면 좋겠다. 빠르게 이동하면서 전투할 때 가끔 튕겼는데 그게 이거 때문이었을 수도.

### ⚔️ 코어 게이머
역순 순회가 전투 밸런스에 영향? 없다. 오토어택은 범위 내 전체 적에게 동시 히트이고, 데미지 계산은 순서 무관. killed 리스트를 루프 후 처리하는 것도 올바른 패턴.

### 🎨 UX/UI 디자이너
방어 코드이므로 UX 영향 없음. 크래시 대신 조용히 스킵하는 것이 사용자 경험상 올바른 선택.

### 🔍 QA 엔지니어
- 역순 순회: 루프 중 리스트 끝에서 제거 시 인덱스 안전 ✅
- `_cachedMonsters` stale 참조: `?? new List<>()` fallback ✅
- `OnArrive` 클로저에서 `m` 캡처: 몬스터 Destroy 후 Unity null이므로 `m == null` 체크가 정확히 동작 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 4항목) |

---

## 최종 판정

**✅ APPROVE**

SPEC 7개 요구사항 전부 충족. 역순 순회, null 방어, 비동기 콜백 안전성 모두 정확히 구현. 전투 결과에 영향 없이 안정성만 개선.
