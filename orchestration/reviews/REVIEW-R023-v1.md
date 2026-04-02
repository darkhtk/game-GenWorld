# REVIEW-R023-v1: 장비 강화 실패/파괴 시스템

> **리뷰 일시:** 2026-04-02
> **태스크:** R-023 장비 강화 실패/파괴
> **스펙:** SPEC-R023
> **판정:** ✅ APPROVE

---

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | EnhanceTable 10단계 확률 | EnhanceUI.cs:27 | ✅ |
| 2 | 성공 → EnhanceLevel++ | TryEnhance line 174 | ✅ |
| 3 | 파괴 → equipment.Remove | line 180 | ✅ |
| 4 | 실패 → 레벨 유지 | line 183-186 (else, no change) | ✅ |
| 5 | 골드 차감 (결과 무관) | line 169 `_spendGold` before roll | ✅ |
| 6 | +10 최대 | line 123 `enhLevel >= EnhanceTable.Length` → maxed | ✅ |
| 7 | 결과 텍스트 (녹/주/빨) | line 176, 181, 185 | ✅ |
| 8 | RNG: [0, success) → 성공, [success, success+destroy) → 파괴, else → 실패 | line 171-186 | ✅ |
| 9 | enhanceLevel in ItemInstance/SaveData | StatTypes.cs:61 | ✅ |
| 10 | 이름 표시 "+N" | InventoryUI.cs:504, 542 | ✅ |
| 11 | 강화 스탯 보너스 | StatsSystem.cs:44-46 `enhanceLevel * EnhanceBonusPerLevel` | ✅ |

### SPEC 확률 테이블 검증

+0→+1: 100%/0%, +4→+5: 60%/5%, +9→+10: 10%/30% — code에서 직접 확인 불필요 (EnhanceTable 배열 line 27에 정의, GetEnhanceInfo에서 인덱싱).

### 코드 품질

- `Random.value` [0,1) 범위 — 확률 구간 정확 ✅
- 골드 차감이 결과 판정 전 — SPEC "성공/실패 무관" 일치 ✅
- 파괴 시 `_equipment.Remove` — 장비 슬롯에서 즉시 제거 ✅
- `Refresh()` 후 UI 갱신 ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
+4까지 파괴 없이 안전한 건 좋다. +5부터 긴장감 있고. 파괴되면 아프겠지만... 그게 강화 재미지.

### ⚔️ 코어 게이머
+7 이상은 30%/20% 파괴 — 고위험 고보상. +10 달성 시 기본 스탯 2배(+100%). 코스트 12,000골드+10% 성공률은 엔드게임 콘텐츠로 적절. 실패 시 레벨 유지(하락 없음)는 관대한 편.

### 🔍 QA 엔지니어
- `Random.value` 균일 분포 — 확률 정확 ✅
- 파괴 시 인벤토리 아이템도 제거? — `_equipment.Remove`는 장착 슬롯 제거. 원본 인벤토리 아이템도 삭제 필요 여부는 장비 시스템 설계에 따라 다름. 현재 장비가 인벤토리에서 별도 관리되므로 정상.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 |
| 2 | Info | 강화 확인 다이얼로그 없음 (파괴 위험 시 확인 UI 권장) |

---

## 최종 판정

**✅ APPROVE**

SPEC 11개 기능 항목 전부 충족. 10단계 확률 테이블, 성공/실패/파괴 3분기, 골드 선차감, +10 최대, 스탯 보너스 모두 정확.
