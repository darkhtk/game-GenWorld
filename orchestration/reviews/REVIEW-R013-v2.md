# REVIEW-R013-v2: 인벤토리 필터/정렬 — 재리뷰

> **리뷰 일시:** 2026-04-02
> **태스크:** R-013 인벤토리 필터/정렬
> **스펙:** SPEC-R013
> **이전 리뷰:** REVIEW-R013-v1 (❌ NEEDS_WORK)
> **판정:** ✅ APPROVE

---

## v1 지적사항 해결 확인

| # | 심각도 | 지적 내용 | 해결 |
|---|--------|-----------|------|
| 1 | Critical | RefreshGrid()가 GetFiltered() 미호출 | ✅ line 157 `_inventory.GetFiltered(_itemDefs, _currentFilter, _currentSortMode)` |
| 2 | Medium | "Recent" 정렬 모드 누락 | ✅ SortModeNames에 "Recent" 추가 (line 79), GetFiltered sortMode==3 → Reverse (line 129-132) |

---

## 검증 2: 코드 추적 (변경분)

**RefreshGrid() (line 154-174) — 수정 완료:**
- `GetFiltered()` 호출로 필터+정렬된 배열 획득
- filtered 배열 기반으로 슬롯 표시
- 필터 결과보다 슬롯이 많으면 Clear() — 빈칸 처리 ✅

**SortModeNames = { "Name", "Grade", "Type", "Recent" }** — 4종 ✅

**InventorySystem.GetFiltered sortMode 3:**
- `items.Reverse()` — 인벤토리 추가 순서의 역순 (최신 먼저) ✅

---

## 최종 판정

**✅ APPROVE**

v1 Critical + Medium 모두 해결. 필터/정렬이 RefreshGrid에서 정상 적용됨. "Recent" 정렬 추가.
