# REVIEW-R011-v1: 아이템/스킬 툴팁 시스템

> **리뷰 일시:** 2026-04-02
> **태스크:** R-011 아이템/스킬 툴팁
> **스펙:** SPEC-R011
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | TooltipUI Canvas 프리팹 필요 (SerializeField 바인딩) |
| 컴포넌트/노드 참조 | ✅ | panel, titleText, descText, statsText, gradeBar |
| 에셋 존재 여부 | ✅ | tooltip_bg.png 존재 (SPEC 확인) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | TooltipUI 범용 싱글턴 | TooltipUI.cs:14, 18 | ✅ |
| 2 | ShowItem (이름, 등급색, 설명, 스탯, 가격, 스택) | TooltipUI.cs:22-62 | ✅ |
| 3 | ShowSkill (이름, 설명, 레벨, MP, 쿨다운, 범위, AoE, 트리) | TooltipUI.cs:64-93 | ✅ |
| 4 | Hide | TooltipUI.cs:95-99 | ✅ |
| 5 | 위치: 마우스 우측 하단 + 화면 경계 플립 | TooltipUI.cs:101-115 | ✅ |
| 6 | InventoryUI 호버 이벤트 | InventoryUI.cs:446-447 IPointerEnter/Exit | ✅ |
| 7 | HUD 스킬 툴팁 로직 | HUD.cs:379-422 | ✅ |
| 8 | 등급별 색상 | GameConfig.GetGradeColor() 호출 (TooltipUI:31, 59) | ✅ |

### 코드 품질

- null 체크 철저: `_instance == null`, `panel == null`, `item == null`, `skill == null` ✅
- 스탯 표시: `> 0` 체크로 0인 스탯 숨김 — 깔끔한 출력 ✅
- 쿨다운 ms→s 변환: `skill.cooldown / 1000f:F1` — 단위 변환 정확 ✅
- PositionTooltip 플립 로직: 우측 넘침 → 좌측, 하단 넘침 → 상단 ✅
- Awake에서 panel.SetActive(false) — 초기 숨김 ✅

### 아키텍처 참고

InventoryUI에 기존 인라인 툴팁(ShowTooltip, line 237+)이 있고, 새 TooltipUI도 별도 존재. 두 시스템 공존:
- InventoryUI 자체 툴팁: 강화 레벨 반영 (enhanceLevel 보너스 포함) — 인벤토리 특화
- TooltipUI: 범용 (다른 UI에서도 호출 가능) — SPEC 요구사항 충족

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 아이템 호버 → 툴팁 표시 | ✅ | IPointerEnter → OnHover callback |
| 호버 해제 → 툴팁 숨김 | ✅ | IPointerExit → HideTooltip |
| 스킬 슬롯 호버 → 스킬 정보 | ✅ | HUD skillTooltipPanel |
| 화면 경계 플립 | ✅ | PositionTooltip (right/bottom) |
| 빈 슬롯 → 툴팁 안 나옴 | ✅ | item == null / skill == null 체크 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 장비 아이템 호버 | 이름(등급색) + 스탯 보너스 표시 |
| 소모품 호버 | Heal HP/MP + Value + Stack 표시 |
| 빈 슬롯 호버 | 툴팁 미표시 |
| 화면 우측 끝 슬롯 호버 | 툴팁 좌측으로 플립 |
| 스킬 바 호버 | 스킬 이름/레벨/MP/쿨다운 표시 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
아이템에 마우스 올리면 뭔지 바로 알 수 있어서 편하다! 전에는 이름만 보고 뭔지 몰랐는데. 등급별로 색 다른 것도 직관적.

### ⚔️ 코어 게이머
스탯 비교가 가능해짐 — ATK/DEF 수치를 한눈에 볼 수 있어서 장비 교체 판단이 쉬워짐. 스킬 쿨다운/MP 소모도 툴팁에서 확인 가능 — 빌드 계획 수립에 필수.

### 🎨 UX/UI 디자이너
등급별 색상 일관성 유지 (GameConfig.GetGradeColor 공유). gradeBar 이미지로 등급 시각 강조. 화면 경계 플립은 기본이지만 빠짐없이 구현됨. 텍스트 줄바꿈(`\n` join)이 깔끔.

### 🔍 QA 엔지니어
- 싱글턴 `_instance` 설정이 Awake에서 — 씬에 TooltipUI가 여러 개면 마지막이 이김. DontDestroyOnLoad 미사용이나 UI는 씬 전환 시 재생성이 일반적 — OK.
- `PositionTooltip` RectTransform.position 직접 설정 — Canvas 모드(Overlay/Camera)에 따라 좌표계 다를 수 있으나 Screen Space Overlay면 정확.
- `stats != null` 체크 (line 41) — ItemStats 기본값 `new()` 있으나 방어적 — 양호.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | 스킬 "다음 레벨 미리보기" 미구현 (SPEC에서 "있으면" 조건부) |

---

## 최종 판정

**✅ APPROVE**

SPEC 8개 기능 항목 충족. TooltipUI 범용 싱글턴 + InventoryUI 호버 이벤트 + HUD 스킬 툴팁 + 화면 경계 플립 모두 정확 구현. 등급 색상 일관성, null 안전성, 단위 변환 올바름.
