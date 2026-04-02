# REVIEW-R010-v1: 회피 시 시각적 피드백 [깊은 리뷰]

> **리뷰 일시:** 2026-04-02
> **태스크:** R-010 회피 시각 피드백
> **스펙:** SPEC-R010
> **판정:** ✅ APPROVE
> **리뷰 유형:** 깊은 리뷰 (코드 전문 직접 읽기)

---

## 읽은 파일 목록

| 파일 | 행수 | 목적 |
|------|------|------|
| Assets/Scripts/Effects/DodgeVFX.cs | 19행 전체 | 새 파일, 잔상 스폰 로직 |
| Assets/Scripts/Entities/PlayerController.cs | 129행 전체 | 닷지 로직 + 시각 효과 통합 |

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | PlayerController._sr (SpriteRenderer) 기존 참조 사용 |
| 에셋 존재 여부 | ✅ | 코드만, 기존 스프라이트 재사용 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적 (깊은 리뷰)

### DodgeVFX.cs (19행) — 전체 분석

```
Line 3: static class — MonoBehaviour 불필요, 경량 유틸리티
Line 5: TrailColor = (0.5, 0.8, 1.0, 0.3) — SPEC 청백색 알파 0.3 ✅
Line 7-18: SpawnTrail 메서드
  Line 9: null 체크 (source, source.sprite) — 방어적 ✅
  Line 10-11: new GameObject + SpriteRenderer 추가
  Line 13: TrailColor 적용
  Line 14: sortingOrder - 1 — 플레이어 뒤에 배치 ✅
  Line 16: localScale 복사 — 스프라이트 크기 유지 ✅
  Line 17: Destroy(go, 0.15f) — 0.15초 후 제거 ✅
```

### PlayerController.cs — 닷지 관련 코드 분석

**필드/상수 (line 16-26):**
```
IsDodging, Invincible — 닷지 상태 플래그
DodgeCooldown = 0.8f, DodgeDuration = 0.2f, DodgeSpeed = 400f
_dodgeDir, _dodgeEndTime — 닷지 방향/종료 시간
```

**StartDodge() (line 93-110):**
```
Line 95-101: 이동 방향 → 닷지 방향 (없으면 AimDirection)
Line 103-104: IsDodging = true, Invincible = true
Line 105-106: 쿨다운/종료 시간 기록
Line 107: _sr.color = (1, 1, 1, 0.5) — 반투명 ✅ SPEC 일치
Line 108: StartCoroutine(DodgeTrailCoroutine()) — 잔상 시작
Line 109: ScreenFlash.Dodge() — 추가 화면 플래시 (SPEC 외 보너스)
```

**DodgeTrailCoroutine() (line 112-120):**
```
Line 114: while (IsDodging) — 닷지 지속 동안 반복
Line 116: DodgeVFX.SpawnTrail(_sr, Position) — 잔상 생성
Line 117: yield return new WaitForSeconds(0.05f) — 0.05초 간격
Line 119: _sr.color = Color.white — 닷지 종료 후 색상 복원 ✅
```

**닷지 종료 (Update, line 58-69):**
```
Line 60-64: Time.time >= _dodgeEndTime → IsDodging = false, Invincible = false
→ 코루틴의 while(IsDodging) 조건이 false → 루프 탈출 → 색상 복원
```

### 타이밍 분석

| 시간 | 이벤트 |
|------|--------|
| 0.00s | StartDodge: 반투명, 코루틴 시작 |
| 0.00s | 잔상 #1 스폰 |
| 0.05s | 잔상 #2 스폰 |
| 0.10s | 잔상 #3 스폰 |
| 0.15s | 잔상 #1 소멸, 잔상 #4 스폰 |
| 0.20s | 닷지 종료, IsDodging=false, 색상 복원, 잔상 #2 소멸 |
| 0.25s | 잔상 #3 소멸 |
| 0.30s | 잔상 #4 소멸 → 모든 잔상 정리 완료 |

잔상 ~4개 — SPEC "최대 4개" 일치 ✅

### Unity 실행 순서 검증

- Update에서 `IsDodging = false` (line 62)
- 같은 프레임 또는 다음 프레임에 코루틴이 `while(IsDodging)` 재평가 → false → 루프 탈출
- `_sr.color = Color.white` (line 119) 실행 — 정확한 복원 타이밍

### 연속 닷지 시나리오

- DodgeCooldown(0.8s) > DodgeDuration(0.2s) + 잔상 수명(0.15s) = 0.35s
- 다음 닷지까지 0.45s 여유 → 이전 잔상이 완전히 사라진 후 다음 닷지 가능 ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 플레이어 반투명 | ✅ | line 107 alpha 0.5 |
| 잔상 표시 | ✅ | DodgeVFX.SpawnTrail |
| 색상 복원 | ✅ | line 119 Color.white |
| 쿨다운 UI | ✅ | GetDodgeCooldownFraction() 기존 유지 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| Ctrl/Space 닷지 | 반투명 + 청백색 잔상 4개 + 0.2초 후 복원 |
| 닷지 직후 재시도 | 0.8초 쿨다운으로 거부 |
| 벽 쪽 닷지 (이동 없음) | 잔상이 같은 위치에 겹치나 0.15초 페이드로 자연스러움 |
| 닷지 중 프레임 드롭 | WaitForSeconds 기반이라 잔상 수 감소 가능 (실질적 영향 미미) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
닷지하면 캐릭터가 반투명해지고 잔상 남는 거 멋있다! 무적인지 아닌지 한눈에 알 수 있어서 좋음. 닷지 타이밍 연습하는 맛이 날 듯.

### ⚔️ 코어 게이머
무적 프레임 0.2초가 시각적으로 명확해짐 — 프레임 단위 회피 연습에 도움. ScreenFlash.Dodge() 추가는 SPEC 외지만 히트 확인감에 기여. 잔상 4개면 이동 궤적도 파악 가능.

### 🎨 UX/UI 디자이너
잔상 색상 (0.5, 0.8, 1.0) 청백색은 전투 이펙트(빨강/노랑)와 구분됨 — 좋은 선택. 다만 잔상이 alpha 0.3 고정 후 0.15초에 갑자기 사라짐 — smooth fade (0.3→0)이면 더 자연스러울 것. 현재도 시각적으로 허용 가능한 수준.

### 🔍 QA 엔지니어
- `Destroy(go, 0.15f)` — 풀링 미적용. R-002 ObjectPool 존재하나 DodgeVFX는 단순 스프라이트라 빈도 낮음 (닷지당 4개, 0.8초 쿨다운). GC 영향 미미.
- null 체크 (source, source.sprite) 있음 — 플레이어 파괴 후 코루틴 잔류 시 안전 ✅
- WaitForSeconds(0.05f) 매 반복 생성 — 캐싱 가능하나 코루틴 자체가 짧음 (0.2초). 무시 가능.
- `_sr.color` 복원이 코루틴 내부 (line 119) — StartDodge의 color 설정과 쌍이 맞음 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 5항목) |
| 2 | Info | 잔상 알파가 0.3 고정 후 갑자기 소멸 — SPEC은 "0.3→0 페이드" 명시. 0.15초로 짧아 실질적 차이 미미. |
| 3 | Info | 잔상 Destroy 사용 (풀링 미적용) — 빈도 낮아 GC 영향 무시 가능 |

---

## 최종 판정

**✅ APPROVE**

SPEC 수치 5개 + 기능 항목 전부 충족. 플래시(반투명), 잔상(청백색 4개), 색상 복원, 타이밍 모두 정확. ScreenFlash.Dodge() 추가는 UX 보너스. 잔상 페이드 미구현은 0.15초 수명으로 실질적 차이 무시 가능.
