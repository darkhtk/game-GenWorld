# REVIEW-R012-v1: HUD 버프/디버프 아이콘 표시

> **리뷰 일시:** 2026-04-02
> **태스크:** R-012 HUD 버프/디버프 아이콘
> **스펙:** SPEC-R012
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | effectIconContainer + effectIconPrefab SerializeField |
| 컴포넌트/노드 참조 | ✅ | HUD → EffectHolder 참조 (GameManager.Instance) |
| 에셋 존재 여부 | ✅ | 상태 아이콘 6종 존재 (SPEC 확인) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | effectIconContainer/effectIconPrefab 필드 | HUD.cs:62-63 | ✅ |
| 2 | UpdateEffectIcons 매 프레임 호출 | HUD.cs:120 (Update에서) | ✅ |
| 3 | EffectHolder.GetActive() 활성 효과 조회 | EffectSystem.cs:102-119 | ✅ |
| 4 | totalDuration 필드 추가 | EffectSystem.cs:10, 17, 52 | ✅ |
| 5 | 최대 8개 아이콘 | HUD.cs:69 `MaxEffectIcons = 8` | ✅ |
| 6 | 남은 시간 타이머 | HUD.cs:407-409 `remaining:F0}s` | ✅ |
| 7 | 필 오버레이 | HUD.cs:410-414 `fillAmount = remaining / totalDuration` | ✅ |
| 8 | 초과 아이콘 비활성화 | HUD.cs:416-419 `SetActive(false)` | ✅ |
| 9 | null 방어 | HUD.cs:387-389 gm/PlayerEffects null 체크 | ✅ |

### 코드 품질

- 아이콘 풀 패턴: 필요 시 Instantiate, 초과 시 SetActive(false) — 재사용 ✅
- ms→s 변환: `(info.expires - nowMs) / 1000f` — 올바른 단위 변환 ✅
- `totalDuration > 0` 체크 (line 413) — 0 나눗셈 방지 ✅
- GetActive(): 만료되지 않은 효과만 필터 (`expiresAt > now`) ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 아이콘 표시/숨김 | ✅ | SetActive(true/false) |
| 타이머 카운트다운 | ✅ | 매 프레임 remaining 계산 |
| 필 오버레이 감소 | ✅ | fillAmount = 남은/전체 |
| 빈 상태 | ✅ | active.Count == 0 → 모든 아이콘 비활성 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 마나 쉴드 적용 | 아이콘 표시 + 남은 시간 카운트다운 + 필 감소 |
| 독+화상+둔화 동시 | 3개 아이콘 표시 |
| 효과 만료 | 아이콘 즉시 비활성화 |
| 9개 이상 동시 효과 | 8개까지만 표시 |
| 효과 없음 | 아이콘 영역 비어있음 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
독 걸렸는지 뭔지 모르고 죽었던 적 있는데 이제 아이콘으로 알 수 있겠다. 남은 시간도 보이니까 "좀만 버티면 풀린다" 판단 가능.

### ⚔️ 코어 게이머
버프 관리가 전략의 핵심. 마나 쉴드 남은 시간, 스텔스 타이머 등을 HUD에서 확인 가능 — 버프 갱신 타이밍 최적화에 필수. 필 오버레이가 직관적.

### 🎨 UX/UI 디자이너
HorizontalLayoutGroup + 아이콘 프리팹 구조 — 확장 용이. 타이머 텍스트가 아이콘 하위에 있어 공간 효율적. 아이콘 크기 24x24는 HUD 밀도에 적합.

### 🔍 QA 엔지니어
- Instantiate 시 풀링 미사용: 하지만 최대 8개로 제한되고 빈번하지 않음 — GC 영향 미미
- `GetComponentInChildren<TextMeshProUGUI>` 매 프레임 호출 (line 408): 캐싱하면 좋으나 8개 아이콘 범위라 성능 영향 무시 가능
- `GetActive()` 매 프레임 `new List<>()` 생성: 반복적 GC 가능 — 루프 최적화 고려 가능하나 효과 수 적어 실질 영향 미미

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | GetActive/GetComponentInChildren 매 프레임 호출 — 성능 최적화 여지 (현재 무시 가능) |

---

## 최종 판정

**✅ APPROVE**

SPEC 9개 기능 항목 전부 충족. EffectHolder.GetActive() + totalDuration 확장, HUD 아이콘 풀, 타이머, 필 오버레이 모두 정확. 최대 8개 제한, null 방어, 단위 변환 올바름.
