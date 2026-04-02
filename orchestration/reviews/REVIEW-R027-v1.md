# REVIEW-R027-v1: AnimationDef — 엔티티별 애니메이션 정의 데이터

> **리뷰 일시:** 2026-04-02
> **태스크:** R-027 AnimationDef
> **스펙:** SPEC-R-027
> **판정:** ❌ NEEDS_WORK

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | ScriptableObject, 씬 불필요 |
| 컴포넌트/노드 참조 | ✅ | CreateAssetMenu 등록 |
| 에셋 존재 여부 | ❌ | .asset 파일 4종 미생성 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### 수용 기준별 검증

| # | 수용 기준 | 결과 | 상세 |
|---|---------|------|------|
| 1 | AnimationDef SO 생성 가능 | ✅ | `[CreateAssetMenu(menuName = "Game/AnimationDef")]` |
| 2 | Player용 에셋 (idle, run, attack, dodge, hit, die) | ❌ | .asset 파일 없음 |
| 3 | Monster용 에셋 (idle, walk, attack, hit, die) | ❌ | .asset 파일 없음 |
| 4 | NPC용 에셋 (idle, talk, react) | ❌ | .asset 파일 없음 |
| 5 | Skill용 에셋 (cast, projectile, impact) | ❌ | .asset 파일 없음 |
| 6 | PropertyDrawer (null clip 경고 색상) | ✅ | AnimEntryDrawer.cs — 오렌지 배경 하이라이트 |

### 완료된 항목 상세

**AnimationDef.cs (45행):**
- ScriptableObject + CreateAssetMenu ✅
- AnimEntry: stateName, clip, expectedDuration, isLooping ✅
- EntityType enum: Player, Monster, NPC, Skill ✅
- GetEntry(), HasClip(), LogMissingClips() 유틸리티 (SPEC 외 보너스) ✅

**AnimEntryDrawer.cs (33행):**
- CustomPropertyDrawer(typeof(AnimEntry)) ✅
- clip null → 오렌지 배경 (1f, 0.7f, 0.3f) 하이라이트 ✅
- GetPropertyHeight override — Inspector 레이아웃 정상 ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
(해당 없음 — 에디터 전용 시스템)

### ⚔️ 코어 게이머
애니메이션 정의가 데이터화되면 향후 콘텐츠 확장에 유리. 하지만 기본 에셋 없으면 다른 시스템(R-028 프리뷰 등)이 참조할 대상 없음.

### 🎨 UX/UI 디자이너
PropertyDrawer로 누락 클립이 오렌지 하이라이트 — Inspector에서 즉시 인지 가능. 좋은 UX. 하지만 실제 에셋 없이는 테스트 불가.

### 🔍 QA 엔지니어
- 코드 자체는 올바르게 구현됨
- .asset 파일 없으면 시스템 통합 테스트 불가
- R-028(AnimationPreviewUI)이 AnimationDef를 참조 → 에셋 없으면 R-028도 테스트 불가

---

## 미해결 사항 (필수 수정)

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | **Critical** | Player용 기본 AnimationDef .asset 미생성 (idle, run, attack, dodge, hit, die) |
| 2 | **Critical** | Monster용 기본 AnimationDef .asset 미생성 (idle, walk, attack, hit, die) |
| 3 | **Critical** | NPC용 기본 AnimationDef .asset 미생성 (idle, talk, react) |
| 4 | **Critical** | Skill용 기본 AnimationDef .asset 미생성 (cast, projectile, impact) |

---

## 최종 판정

**❌ NEEDS_WORK**

코드(AnimationDef.cs) + PropertyDrawer(AnimEntryDrawer.cs)는 정확히 구현됨. 그러나 수용 기준 6개 중 4개(.asset 에셋 생성)가 미충족. Unity Editor에서 4종 기본 에셋 생성 후 재리뷰 요청 바람. clip 필드는 null(placeholder)이어도 상관없으나 entries에 필요한 stateName 목록이 채워져야 함.
