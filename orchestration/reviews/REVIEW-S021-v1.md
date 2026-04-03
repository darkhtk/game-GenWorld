# REVIEW-S021-v1: 테스트 커버리지 확장

> **리뷰 일시:** 2026-04-03
> **태스크:** S-021 테스트 커버리지 확장
> **스펙:** 없음
> **커밋:** d3c53b8 (관련 변경 포함)
> **판정:** ✅ APPROVE
> **[깊은 리뷰]** — 소스 코드 직접 대조 수행

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
| TASK 명세 부합 | ✅ | CombatSystem +10, InventorySystem +13, SaveSystem +9 신규 테스트 작성 |
| 기존 코드 호환 | ✅ | 테스트 전용 — 프로덕션 코드 변경 없음 |
| 아키텍처 패턴 | ✅ | NUnit, EditMode, 파일당 하나의 테스트 클래스 |
| 테스트 커버리지 | ✅ | 경계값·예외·복합 시나리오 커버리지 확보 |

### CombatSystem 테스트 분석 (18개 총, +10 신규)

**CalcDamage 경계값 (3건 추가):**
- `EqualAtkDef_ReturnsOne`: atk == def → Mathf.Max(1, 0) = 1 ✅ 코드 일치
- `ZeroAtk_ReturnsOne`: atk=0 → Mathf.Max(1, -10) = 1 ✅ 코드 일치
- `CritMinimum_ReturnsTwo`: 최소 데미지 1 × 2 = 2 ✅ 코드 일치

**CalcCrit 테스트 (4건 추가):**
- `HighChance_ReturnsTrue`: critChance=100, roll=0.5 → 0.5 < 1.0 = true ✅
- `ZeroChance_ReturnsFalse`: critChance=0, roll=0.5 → 0.5 < 0.0 = false ✅
- `BoundaryExact_ReturnsFalse`: critChance=50, roll=0.5 → 0.5 < 0.5 = false ✅ (strict less-than)
- `BoundaryJustBelow_ReturnsTrue`: critChance=50, roll=0.499 → 0.499 < 0.5 = true ✅

> CalcCrit 테스트가 `Func<float> roll` 의존성 주입을 활용하여 결정론적 테스트를 수행한 점이 좋음.

**FindClosest 확장 (3건 추가):**
- `AllDead`: alive=false인 타겟만 → null 반환 ✅
- `Empty`: 빈 배열 → null 반환 ✅
- `ListOverload` 3건: List<T> 오버로드 커버리지 (FindNearest, Empty, OutOfRange) ✅

### InventorySystem 테스트 분석 (30개 총, +13 신규)

**경계값 테스트 (6건):**
- `GetSlot_NegativeIndex_ReturnsNull`: index < 0 → null ✅ 코드: `index >= 0 && index < MaxSlots`
- `GetSlot_OutOfBounds_ReturnsNull`: index=999 → null ✅
- `RemoveAtSlot_InvalidIndex_ReturnsNull`: -1, 999 → null ✅
- `RemoveAtSlot_EmptySlot_ReturnsNull`: 비어있는 슬롯 → null ✅
- `SwapSlots_OutOfBounds_DoesNothing`: 범위 밖 스왑 → 원본 유지 ✅
- `SwapSlots_WithEmptySlot_MovesItem`: 빈 슬롯과 스왑 → 이동 ✅

**복합 시나리오 (4건):**
- `RemoveItem_AcrossMultipleStacks`: 2개 스택 걸쳐 제거 (99+50 중 120 제거 → 29 잔여) ✅
- `RemoveItem_ClearsSlotWhenCountReachesZero`: 정확히 0이 되면 슬롯 null 처리 ✅
- `GetCount_NonexistentItem_ReturnsZero`: 없는 아이템 조회 → 0 ✅
- `RemoveItem_NonexistentItem_ReturnsFalse`: 없는 아이템 제거 → false ✅

**SortItems / GetFiltered (3건):**
- `SortItems_ByGradeDescending`: legendary > rare > common 정렬 확인 ✅
- `SortItems_SameGrade_SortsByTypeThenName`: 동등 등급 → 타입 → 이름 순 ✅
- `SortItems_CompactsEmptySlots`: 빈 슬롯 압축 확인 ✅

> `GetFiltered` 8건 추가 — 필터("all", "weapon", "armor", "consumable") + 정렬 모드(grade, reverse) + 빈 인벤토리 시나리오 포함. 코드의 `switch` 분기를 모두 커버.

### SaveSystem 테스트 분석 (17개 총, +9 신규)

**복합 데이터 라운드트립 (4건):**
- `PreservesLearnedSkills`: Dictionary<string, int> 직렬화/역직렬화 ✅
- `PreservesEquippedSkills`: string[] with null 요소 직렬화 ✅
- `PreservesQuestState`: QuestSaveData 중첩 객체 직렬화 ✅
- `PreservesKillCountsAndBonusStats`: 복수 Dictionary + string[] 직렬화 ✅

**백업 복구 테스트 (2건):**
- `RecoversFromBackup_WhenPrimaryCorrupted`: 메인 파일 손상 → bak1에서 복구 ✅
- `ReturnsNull_WhenAllFilesCorrupted`: 전체 손상 → null 반환 ✅

> Save()가 RotateBackups() 호출하여 bak1 생성 후 메인 파일만 손상시키는 테스트 설계가 정확.

**체크섬 검증 (1건):**
- `RejectsFile_WhenChecksumTampered`: 체크섬 변조 후 백업도 삭제 → null 반환 ✅

**마이그레이션 테스트 (3건):**
- `Apply_V0ToV1_ReturnsData`: 마이그레이션 적용 후 데이터 유지 ✅
- `Apply_SameVersion_NoChange`: fromVersion == toVersion → 변경 없음 ✅
- `Apply_MissingMigration_StillReturnsData`: 없는 버전 → 경고만, 데이터 유지 ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| UI 변경 | N/A | 테스트 전용 — UI 변경 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 결과 |
|----------|------|
| 기존 테스트 영향 | ✅ 기존 테스트 변경 없음, 신규 추가만 |
| 프로덕션 코드 영향 | ✅ 프로덕션 코드 0줄 변경 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
테스트는 플레이어가 직접 보는 건 아니지만, 인벤토리가 꽉 찼을 때 아이템 사라지는 버그 같은 거 잡아주는 거니까 좋다. 세이브 파일 깨져도 백업에서 복구되는 거 테스트해둔 것도 안심된다.

### ⚔️ 코어 게이머
테스트 설계가 체계적이다. CalcCrit 경계값(50% 확률에서 0.5 vs 0.499)을 정확히 검증하고, 인벤토리 멀티 스택 제거(99+50에서 120 제거)처럼 실제 플레이에서 발생하는 복합 상황을 커버한다. SaveSystem의 체크섬 변조 감지 테스트는 세이브 데이터 무결성 보장에 필수적이다.

### 🎨 UX/UI 디자이너
테스트 전용 변경이라 UI 영향 없음. GetFiltered 테스트에서 "armor" 필터가 helmet/armor/boots/accessory 4종을 모두 포함하는지 확인한 것은 인벤토리 필터 UI의 정확성을 보장한다.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| 테스트 명명 규칙 | ✅ Method_Condition_Expected 패턴 일관 |
| 하나의 테스트 = 하나의 동작 | ✅ 각 테스트가 단일 동작 검증 |
| Setup/Teardown 정리 | ✅ SaveSystemTests: Setup에서 DeleteSave, Teardown에서 재정리 |
| 결정론적 테스트 | ✅ CalcCrit: roll 함수 주입으로 랜덤 제거 |
| 경계값 커버리지 | ✅ 음수 인덱스, 0, 빈 배열, 최대값 초과 |
| 소스 코드 대조 | ✅ 모든 Assert 값이 실제 코드 로직과 일치 확인 |
| 잠재적 누락 | ⚠️ CombatSystem: FindClosest에 null 배열 전달 시 NullRef 가능성 — 현재 미테스트 (방어 코드도 미존재, 호출처에서 보장) |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ CombatSystem +10, InventorySystem +13, SaveSystem +9 = 32건 신규 테스트 |
| 기존 호환성 | ✅ 프로덕션 코드 변경 없음, 기존 테스트 수정 없음 |
| 코드 품질 | ✅ 일관된 명명, 단일 책임 테스트, 결정론적 설계 |
| 소스 대조 | ✅ 모든 기대값이 실제 구현과 정확히 일치 |

**결론:** ✅ **APPROVE** — 3개 핵심 시스템(CombatSystem, InventorySystem, SaveSystem)에 대해 경계값, 예외 경로, 복합 시나리오를 포괄하는 32건의 테스트를 추가. 테스트 설계가 체계적이고 실제 코드 로직과의 대조가 정확하다. 프로덕션 코드 변경 없이 안정성 검증 인프라를 강화.

**참고:** FindClosest에 null 배열 전달 시 NullReferenceException 발생 가능하나, 현재 모든 호출처에서 배열이 보장되므로 방어 코드 추가는 선택적 개선 사항.
