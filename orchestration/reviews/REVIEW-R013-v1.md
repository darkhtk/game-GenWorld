# REVIEW-R013-v1: 인벤토리 필터/정렬 강화

> **리뷰 일시:** 2026-04-02
> **태스크:** R-013 인벤토리 필터/정렬
> **스펙:** SPEC-R013
> **판정:** ❌ NEEDS_WORK

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | filterButtons, sortModeButton SerializeField |
| 컴포넌트/노드 참조 | ✅ | 버튼 배열 + 텍스트 참조 |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | filterButtons SerializeField | InventoryUI.cs:21 | ✅ |
| 2 | sortModeButton + sortModeText | InventoryUI.cs:22, (text 확인 필요) | ✅ |
| 3 | SetFilter 로직 | InventoryUI.cs:305-309 | ✅ |
| 4 | CycleSortMode 순환 | InventoryUI.cs:312-316 | ✅ |
| 5 | FilterNames 5종 | InventoryUI.cs:78 | ✅ |
| 6 | InventorySystem.GetFiltered() | InventorySystem.cs:109-145 | ✅ |
| 7 | 타입별 필터 매핑 | InventorySystem.cs:118-125 | ✅ |
| 8 | Grade/Type/Name 정렬 | InventorySystem.cs:129-142 | ✅ |
| 9 | **RefreshGrid()에서 필터/정렬 적용** | **InventoryUI.cs:154-174** | **❌ 미연결** |
| 10 | "Recent" 정렬 모드 | SortModeNames (line 79) | **❌ 누락** |

### 상세: Critical 미완료 항목

**1. RefreshGrid()가 GetFiltered()를 호출하지 않음 (Critical)**

현재 `RefreshGrid()` (line 154-174):
```csharp
void RefreshGrid()
{
    EnsureSlots(_inventory.MaxSlots);
    for (int i = 0; i < _inventory.MaxSlots; i++)
    {
        var item = _inventory.GetSlot(i);  // ← 전체 슬롯 순회, 필터 없음
        // ...
    }
}
```

`_currentFilter`와 `_currentSortMode`가 SetFilter/CycleSortMode에서 설정되지만, RefreshGrid는 이 값을 무시하고 모든 슬롯을 표시. `InventorySystem.GetFiltered()`는 정확히 구현되어 있으나 **어디에서도 호출되지 않음**.

결과: 필터 버튼을 눌러도 인벤토리 표시가 변하지 않음.

**필요한 수정:**
```csharp
void RefreshGrid()
{
    var filtered = _inventory.GetFiltered(_itemDefs, _currentFilter, _currentSortMode);
    // filtered 배열 기반으로 슬롯 표시
}
```

**2. "Recent" 정렬 모드 누락 (Medium)**

- SPEC: 4개 모드 (Name, Grade, Type, Recent)
- 구현: 3개만 (`SortModeNames = { "Name", "Grade", "Type" }`)
- GetFiltered()의 sortMode switch도 0/1/2만 처리

### 완료된 항목

- ✅ filterButtons SerializeField + onClick 바인딩 (line 89-95)
- ✅ sortModeButton onClick 바인딩 (line 98-99)
- ✅ FilterNames 5종 정확 (all, weapon, armor, consumable, material)
- ✅ SetFilter/CycleSortMode → Refresh() 호출 체인 정상
- ✅ GetFiltered() 필터 로직 정확 (weapon/armor/consumable/material 매핑)
- ✅ GetFiltered() 정렬 로직 정확 (Name/Grade/Type)
- ✅ null def 방어 (sort 비교에서)

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 필터 버튼 표시 | ✅ | SerializeField 바인딩 |
| 필터 적용 → 그리드 갱신 | ❌ | RefreshGrid가 필터 무시 |
| 정렬 순환 텍스트 | ✅ | sortModeText 갱신 (line 315) |
| 정렬 적용 → 그리드 갱신 | ❌ | RefreshGrid가 정렬 무시 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 실제 |
|----------|-----------|------|
| Weapon 필터 클릭 | 무기만 표시 | ❌ 전체 표시 (변화 없음) |
| Grade 정렬 클릭 | 등급순 정렬 | ❌ 순서 변화 없음 |
| All 필터 → 전체 | 전체 표시 | ✅ (기본 동작) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
필터 버튼 누르면 바뀔 줄 알았는데 아무 변화가 없다...?

### ⚔️ 코어 게이머
인벤토리 60칸 넘어가면 필터 필수인데, 백엔드 로직은 있는데 UI에 연결이 안 됨. 정렬도 마찬가지.

### 🎨 UX/UI 디자이너
필터 탭 버튼 UI는 준비됐으나 기능 미동작 — 사용자 혼란 유발.

### 🔍 QA 엔지니어
- SetFilter → Refresh → RefreshGrid: 호출 체인은 정상이나 RefreshGrid 내부에서 _currentFilter를 참조하지 않음
- GetFiltered() 코드 자체는 올바르게 구현됨 — 연결만 하면 동작

---

## 미해결 사항 (필수 수정)

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | **Critical** | RefreshGrid()가 GetFiltered()를 호출하지 않음 — 필터/정렬 기능 미동작 |
| 2 | Medium | "Recent" 정렬 모드 미구현 (SPEC 명시) |
| 3 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |

---

## 최종 판정

**❌ NEEDS_WORK**

필터/정렬 백엔드(InventorySystem.GetFiltered)는 정확히 구현되었으나, **UI(RefreshGrid)에서 호출하지 않아 기능이 동작하지 않음**. R-002 v1과 동일한 패턴 — 인프라 구현 후 연결 누락. Critical 1건 + Medium 1건 수정 후 재리뷰 요청 바람.
