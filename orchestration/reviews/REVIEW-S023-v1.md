# REVIEW-S023-v1: RegionTracker 경계 조건 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-023 RegionTracker 경계 조건
> **스펙:** 없음
> **커밋:** fc20bb9
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | null/empty 배열 방어 — 보드 명세와 일치 |
| 기존 코드 호환 | ✅ | 반환값 null은 기존 호출자가 이미 처리 |
| 아키텍처 패턴 | ✅ | 조기 반환 null guard — 표준 방어 패턴 |
| 테스트 커버리지 | ⚠️ | 미작성 — 순수 C# 클래스이므로 Edit Mode 테스트 추가 권장 |

### 변경 분석 (코드 직접 확인)

**RegionTracker.cs:12 (추가된 가드):**
```csharp
if (_regions == null || _regions.Length == 0) return null;
```

**호출 체인 안전성:**
- `UpdatePlayerRegion()` (line 27): `var region = GetRegionAt(...)` → `region?.id ?? ""` — null 안전
- `GameManager`에서 호출 시에도 null 반환을 이미 처리

**`_regions` 필드 분석:**
- `readonly RegionDef[] _regions` — 생성자에서 1회 설정, 이후 불변
- 생성자: `public RegionTracker(RegionDef[] regions) => _regions = regions;`
- DataManager의 `RegionList`가 `Array.Empty<RegionDef>()`로 초기화되므로 (S-011에서 수정됨) 빈 배열이 전달될 수 있음
- JSON 로드 실패 시 `null`이 전달될 수도 있음 (DataManager 생성 순서에 따라)

**양쪽 조건 모두 필요:**
- `null`: DataManager 초기화 전 또는 로드 실패 시
- `Length == 0`: S-011 폴백으로 빈 배열 전달 시

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 데이터 바인딩 | ✅ | RegionTracker는 UI에 직접 바인딩 없음 — EventBus를 통한 간접 연동 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 정상 리전 데이터 | 기존과 동일 — 가드 조건 불통과 |
| regions.json 누락 (S-011 폴백) | 빈 배열 → 가드에서 null 반환 → CurrentRegionId="" 유지, 크래시 없음 |
| DataManager 미초기화 상태 | null 배열 → 가드에서 null 반환, NRE 방지 |
| 플레이어가 리전 경계 밖 | 기존과 동일 — foreach 순회 후 null 반환 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
리전 데이터가 없어도 게임이 터지지 않는다는 건 좋다. "알 수 없는 지역"으로 표시되는 건 낫지 못해도 크래시보다 100배 낫다.

### ⚔️ 코어 게이머
`_regions`가 `readonly`이므로 생성 후 null로 변하는 경우는 없다. 가드는 순수하게 생성 시점의 방어. `foreach`가 빈 배열에 대해 안전하긴 하지만, `Length == 0` 체크로 불필요한 `FloorToInt` 연산을 건너뛰는 것도 미세 최적화로 의미 있다.

### 🎨 UX/UI 디자이너
지역 진입 알림(RegionVisitEvent)이 발행되지 않을 뿐 — UI 오류 없음. 리전 이름이 없을 때의 빈 상태 표시는 기존 처리와 동일.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| null 배열 방어 | ✅ `_regions == null` 체크 |
| 빈 배열 방어 | ✅ `_regions.Length == 0` 체크 |
| 반환값 일관성 | ✅ null 반환 — 기존 "리전 없음" 반환과 동일 |
| 성능 영향 | ✅ 없음 (조기 반환) |
| 기존 동작 변경 | ✅ 없음 — 정상 경로 불영향 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ null/empty 방어 완비 |
| 기존 호환성 | ✅ 호출자 모두 null 반환 처리 완료 |
| 코드 품질 | ✅ 최소 변경, 표준 가드 패턴 |

**결론:** ✅ **APPROVE** — `GetRegionAt()`에 null/empty 가드를 추가하여 DataManager 로드 실패 또는 빈 리전 데이터 상황에서 NRE를 방지. 호출자(`UpdatePlayerRegion`)가 이미 null을 처리하므로 기존 동작에 영향 없음.
