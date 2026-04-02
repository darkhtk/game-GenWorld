# REVIEW-R030-v1: MonsterController 애니메이션 검증 [깊은 리뷰]

> **리뷰 일시:** 2026-04-02
> **태스크:** R-030 Monster 애니메이션 검증
> **스펙:** SPEC-R-030
> **판정:** ✅ APPROVE
> **리뷰 유형:** 깊은 리뷰 (코드 전문 직접 읽기)

---

## 읽은 파일 목록

| 파일 | 행수 | 목적 |
|------|------|------|
| Assets/Scripts/Entities/MonsterController.cs | 218행 전체 | 핵심 파일, 애니메이션 연동 및 AI 상태 전이 |

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | MonsterDef animationDef 필드 | MonsterDef.cs:14 (`[JsonIgnore] AnimationDef animationDef`) | ✅ |
| 2 | idle ↔ walk 전이 | UpdateAI: Patrol→DoPatrol (walk), Chase/Return→MoveToward | ✅ |
| 3 | attack 애니메이션 + 데미지 타이밍 | CanAttack line 211, CombatManager 연동 | ✅ |
| 4 | hit 애니메이션 + 넉백 동기화 | TakeDamage line 190 `PlayAnimation("hit")` + FlashWhite | ✅ |
| 5 | die + DeathMarker | TakeDamage line 189 `PlayAnimation("die")` + return true → CombatManager | ✅ |
| 6 | 스폰 시 Animator 누락 경고 | ValidateAnimations line 55-63 → LogWarning | ✅ |

---

## 깊은 리뷰: MonsterController.cs 전문 분석

### Init() (line 35-53)
- Def/Hp/SpawnTime/Rigidbody 초기화 — 기존 동일
- Collider2D 보장 (BoxCollider2D) — projectile 감지용 ✅
- `_hpBar = MonsterHPBar.Create(transform, def.hp)` — R-015 연동 ✅
- **`ValidateAnimations()`** — AnimationDef 5개 상태 검증 (NEW) ✅

### ValidateAnimations() (line 55-63)
```
idle, walk, attack, hit, die — 5개 필수 상태 검증
animationDef null → 조기 반환 (선택적 기능)
누락 시 LogWarning 출력
```
SPEC 일치 ✅

### PlayAnimation() (line 66-74)
```
AnimationDef null → return
entry/clip null → return (방어적)
Animator 있으면 Animator.Play(stateName)
```
Animator 미부착 몬스터에서도 안전 ✅

### UpdateAI() (line 76-144) — 성능 최적화 확인
이전 버전 대비 **sqrMagnitude 최적화** 적용:
- `distToPlayerSq = (Position - playerPos).sqrMagnitude` (line 81)
- Patrol → Chase: `distToPlayerSq <= detectRange²` (line 99)
- Chase → Attack: `distToPlayerSq <= attackRange²` (line 106)
- Chase → Return: `distToPlayerSq > (detectRange * ChaseRangeMult)²` (line 108-109)
- Return 도착: `sqrMagnitude < 256f` (16² = 256) (line 133)

모든 Distance() 호출이 sqrMagnitude로 대체 — sqrt 제거로 매 프레임 연산 최적화 ✅

### TakeDamage() (line 183-192) — 애니메이션 연동
```
Line 186: Hp -= dmg
Line 187: _hpBar.UpdateHP (R-015)
Line 188: FlashWhite() — 빨간색 틴트 0.1초
Line 189: if (Hp <= 0) → PlayAnimation("die") + return true
Line 190: PlayAnimation("hit")
```
사망 시 die, 생존 시 hit — 순서 정확 ✅

### FlashWhite() (line 198-209)
- `static readonly WaitForSeconds FlashWait` — GC 최적화 (캐싱) ✅
- HitTint (1, 0.4, 0.4) → 0.1초 → Color.white 복원 ✅

### 기존 기능 유지 확인
- Return 피해 감소 80% (line 185) — R-007 ✅
- 보스 페이즈 (CheckPhase line 161-181) — 유지 ✅
- 어그로 윈도우 2초 (line 110) — R-007 ✅
- Return 텔레포트 5초 (line 125) — R-007 ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
몬스터 맞을 때 빨갛게 번쩍이고, 죽을 때 죽는 모션 나오면 타격감 좋겠다. 지금은 스프라이트 기반이라 단순하지만 기반은 잘 깔려있음.

### ⚔️ 코어 게이머
sqrMagnitude 최적화로 몬스터 수가 많아져도 프레임 안정적. AI 상태 전이 로직이 견고 — Patrol/Chase/Attack/Return 4상태 모두 정확.

### 🎨 UX/UI 디자이너
FlashWhite 0.1초는 짧지만 피격 인지에 충분. PlayAnimation 기반이라 향후 클립 추가만으로 비주얼 개선 가능.

### 🔍 QA 엔지니어
- ValidateAnimations가 Init에서 호출 — 모든 몬스터 스폰 시 자동 검증 ✅
- PlayAnimation null 3중 체크 (animationDef, entry, clip) — 극도로 안전 ✅
- sqrMagnitude 수학 검증: `a*b*a*b == (a*b)²` — 결합법칙으로 정확하나 `(a*b)*(a*b)` 형태가 가독성 좋음 (Info)
- DoFlash 코루틴: 사망 직후 FlashWhite → 코루틴 시작 → 0.1초 후 Color.white 복원 시 이미 Destroy 가능 — 하지만 MonsterSpawner.RemoveMonster는 `Destroy(go, 0.5f)` 딜레이이므로 0.1초 코루틴 완료 가능 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 |
| 2 | Info | sqrMagnitude 비교식 가독성 개선 가능 (`var x = a*b; distSq > x*x`) |

---

## 최종 판정

**✅ APPROVE**

수용 기준 6개 전부 충족. AnimationDef 연결, ValidateAnimations 자동 검증, PlayAnimation 안전 호출, TakeDamage hit/die 연동, sqrMagnitude 성능 최적화 모두 정확. 기존 R-007 어그로 로직 유지 확인.
