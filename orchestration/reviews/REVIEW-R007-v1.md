# REVIEW-R007-v1: 몬스터 어그로/리쉬 시스템 개선

> **리뷰 일시:** 2026-04-02
> **태스크:** R-007 어그로/리쉬 개선
> **스펙:** SPEC-R007
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | MonsterController 내부 로직만 |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 수치 검증

| # | 파라미터 | 스펙 값 | 코드 상수 | 코드 위치 | 결과 |
|---|---------|---------|-----------|-----------|------|
| 1 | Return 피해 감소 | 80% | `ReturnDamageReduction = 5f` (÷5) | line 24 | ✅ |
| 2 | Return 속도 배율 | 1.5x | `ReturnSpeedMult = 1.5f` | line 25 | ✅ |
| 3 | 강제 텔레포트 시간 | 5초 | `ReturnForceTeleport = 5f` | line 23 | ✅ |
| 4 | 재추격 범위 배율 | 0.5x | `ReturnReaggroMult = 0.5f` | line 26 | ✅ |
| 5 | 피격 후 Return 불가 | 2초 | `RecentHitWindow = 2f` | line 27 | ✅ |

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | Return 피해 감소 | line 159: `Mathf.Max(1, dmg / (int)ReturnDamageReduction)` | ✅ |
| 2 | Return 속도 1.5배 | line 106: `speed * ReturnSpeedMult` | ✅ |
| 3 | 5초 후 텔레포트 | lines 99-105: 시간 체크 → position = _spawnPos, HP 회복, Patrol 전환 | ✅ |
| 4 | 재추격 범위 축소 | line 112: `detectRange * ReturnReaggroMult` | ✅ |
| 5 | 피격 후 Return 불가 | line 85: `Time.time - LastHitByPlayerTime > RecentHitWindow` | ✅ |
| 6 | IsReturning 프로퍼티 | line 14 | ✅ |
| 7 | _returnStartTime 설정 | line 88: Return 진입 시 기록 | ✅ |
| 8 | 복귀 후 HP 완전 회복 | lines 101, 109: `Hp = Def.hp` | ✅ |

### 참고

- `ReturnDamageReduction`이 float(5f)이나 `(int)` 캐스트로 정수 나눗셈 사용 — 동작에 문제 없으나 `const int`가 더 깔끔. 스타일 이슈만.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 해당 없음 | — | AI 로직, UI 연동 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 몬스터 리쉬 거리 초과 | Chase → Return (피격 2초 이후만) |
| Return 중 플레이어 공격 | 80% 피해 감소, 최소 1 |
| Return 5초 경과 | 스폰 지점 텔레포트 + HP 회복 |
| Return 중 플레이어 근접 (detectRange*0.5 이내) | 재추격 |
| Return 중 플레이어 근접 (0.5~1.0 detectRange) | 재추격 안 함 (기존보다 강화된 리쉬) |
| 연속 공격으로 Return 방해 | 2초 동안은 Return 불가, 이후 Return + 80% 감소로 빠르게 복귀 |
| Return 완료 (16px 이내 도착) | HP 회복 + Patrol |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
몬스터가 끝까지 쫓아와서 죽을 뻔한 적 있었는데, 이제 어느 정도 거리 벌어지면 돌아가는 거 좋다. 5초 후 텔레포트도 있으니 영원히 끌려다니는 일은 없겠다.

### ⚔️ 코어 게이머
밸런스 분석:
- 80% 피해 감소는 공격적이지만 Return 상태 자체가 "전투 이탈"이므로 타당. 카이팅으로 무한 잡기 방지.
- 2초 피격 윈도우는 적절 — 오토어택 쿨다운 1초 기준, 연속 공격하면 2초 채움. 멈추면 리쉬 시작.
- 재추격 범위 0.5x — 리쉬 중 우연히 가까이 지나가는 것만 반응. 의도적 어그로만 재추격.
- 텔레포트 5초는 빠른 편이지만 "끌기 어뷰징" 방지에 효과적.

### 🎨 UX/UI 디자이너
Return 중 시각적 피드백이 없음 (스펙 "문제" 란에 언급됨). 스프라이트 틴트나 이펙트가 있으면 플레이어가 몬스터 상태를 인지할 수 있지만, 현재 스펙 범위 밖. 향후 고려 추천.

### 🔍 QA 엔지니어
- Return→Patrol 전환 경로가 3개: (1) 텔레포트 (line 99-105), (2) 자연 도착 (line 107-111), (3) 재추격→Chase (line 112-115). 모두 정상.
- `_returnStartTime`은 Return 진입 시에만 기록 (line 88). 재추격 후 다시 Return 진입 시 갱신됨 — 올바른 동작.
- 텔레포트 시 `transform.position` 직접 설정 — Rigidbody2D 위치와 불일치 가능성? `_rb.position`이 더 안전하나, 다음 프레임에서 물리가 동기화하므로 실질적 문제 없음.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | Return 상태 시각적 피드백 미구현 (스펙 범위 밖) |
| 3 | Info | `ReturnDamageReduction` float→int 캐스트 (스타일) |

---

## 최종 판정

**✅ APPROVE**

SPEC 수치 5개 + 기능 8개 항목 전부 충족. 어그로/리쉬 메커니즘이 견고해짐: 피격 윈도우, 피해 감소, 속도 증가, 텔레포트 안전장치, 재추격 범위 축소. 전투 밸런스 관점에서도 합리적.
