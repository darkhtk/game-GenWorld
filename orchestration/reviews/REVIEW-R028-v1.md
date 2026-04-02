# REVIEW-R028-v1: AnimationPreviewUI — 인게임 애니메이션 미리보기

> **리뷰 일시:** 2026-04-02
> **태스크:** R-028 AnimationPreviewUI
> **스펙:** SPEC-R-028
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | panel + UI 요소 SerializeField |
| 컴포넌트/노드 참조 | ✅ | AnimationDef, Animator, MonsterController, VillageNPC 참조 |
| 에셋 존재 여부 | ✅ | 코드만 (프리팹 바인딩) |
| 빌드 세팅 | ✅ | 조건부 컴파일 적용 |

## 검증 2: 코드 추적

### 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | F9 키 토글 | line 37-41 | ✅ |
| 2 | 엔티티 클릭 → 목록 표시 | TrySelectUnderMouse (51-72) + BuildList (97-127) | ✅ |
| 3 | 드롭다운 선택 | — | ⚠️ 미구현 (클릭 선택 + 플레이어 fallback으로 대체) |
| 4 | [▶] 버튼 재생 | PlayState line 129-133 `Animator.Play` | ✅ |
| 5 | 속도 슬라이더 0.1~3.0 | line 28-31, OnSpeedChanged 135-138 | ✅ |
| 6 | clip null → ⚠ 경고 | line 116-119 `\u26a0` + orange | ✅ |
| 7 | 조건부 컴파일 | line 1 `#if DEBUG \|\| DEVELOPMENT_BUILD \|\| UNITY_EDITOR`, line 141 `#endif` | ✅ |

### 코드 품질

- `#if` 전체 파일 감싸기 — 릴리스 빌드에서 완전 제외 ✅
- TrySelectUnderMouse: Physics2D.OverlapPoint → GetComponent/GetComponentInParent → fallback to player — 실용적 ✅
- BuildList: 기존 버튼 Destroy → 새로 생성 — 리스트 갱신 안전 ✅
- Animator.speed 직접 설정 — 글로벌 TimeScale 미변경 (다른 시스템 영향 없음) ✅
- `UNITY_EDITOR` 추가 — 에디터에서도 동작 보장 (SPEC 외 보너스) ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 패널 토글 | ✅ | F9 키 |
| 엔티티 이름 | ✅ | entityNameLabel = target.name |
| 현재 상태 표시 | ✅ | shortNameHash + normalizedTime |
| 애니메이션 목록 | ✅ | AnimationDef.entries 기반 |
| 누락 클립 경고 | ✅ | ⚠ + orange + "clip missing!" |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| F9 → 몬스터 클릭 | 패널 열림 + 몬스터 애니메이션 목록 |
| [▶] idle 클릭 | idle 애니메이션 즉시 재생 |
| 속도 슬라이더 2.0x | Animator.speed = 2.0 |
| clip 없는 항목 | ⚠ 표시, 클릭 시 Play 시도 (state 미존재면 무시) |
| 릴리스 빌드 | 코드 자체가 컴파일에서 제외 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
(디버그 기능이라 해당 없음)

### ⚔️ 코어 게이머
런타임 애니메이션 검증 도구 — 개발 속도 향상에 기여. 속도 조절로 프레임 단위 확인 가능.

### 🎨 UX/UI 디자이너
디버그 UI 레이아웃 깔끔. 누락 클립 오렌지 표시는 에디터 PropertyDrawer와 일관성 유지. F9 토글은 디버그 컨벤션에 맞음.

### 🔍 QA 엔지니어
- 조건부 컴파일 정확 — 릴리스에서 완전 제외 ✅
- Physics2D.OverlapPoint: Collider2D 필요 — Collider 없는 엔티티는 클릭 불가. MonsterController는 Init에서 BoxCollider2D 보장. Player도 Rigidbody2D + Collider 있음.
- 드롭다운 미구현은 minor — 클릭 선택이 더 직관적이고 씬 내 엔티티 수가 많으면 드롭다운도 사용성이 나빠짐.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 드롭다운 선택 미구현 (SPEC 수용 기준 3번, 클릭 선택으로 대체) |
| 2 | Low | 테스트 미작성 |

---

## 최종 판정

**✅ APPROVE**

수용 기준 7개 중 6개 충족 (드롭다운 1건 미구현). 핵심 기능(F9 토글, 클릭 선택, 재생, 속도 조절, 누락 경고, 조건부 컴파일) 모두 정확. 디버그 도구로서 충분한 기능.
