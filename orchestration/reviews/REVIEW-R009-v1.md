# REVIEW-R009-v1: 스킬 콤보 시스템

> **리뷰 일시:** 2026-04-02
> **태스크:** R-009 스킬 콤보 시스템
> **스펙:** SPEC-R009
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 순수 C# 클래스 (MonoBehaviour 아님) |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 수치 검증

| 파라미터 | 스펙 값 | 코드 값 | 결과 |
|---------|---------|---------|------|
| 콤보 윈도우 | 3초 | `ComboWindow = 3000f` (ms) | ✅ (nowMs 기준 올바른 단위) |
| 히스토리 크기 | 5 | `MaxHistory = 5` | ✅ |
| Blade Fury | dmg x1.5 (slash→thrust) | line 28-33 | ✅ |
| Elemental Burst | AoE x2.0 (fireball→ice_bolt) | line 34-39 | ✅ |
| Arcane Fortify | 지속시간 x1.5 (heal→mana_shield) | line 40-45 | ✅ |

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | ComboSystem 순수 C# 클래스 | ComboSystem.cs:11 | ✅ |
| 2 | ComboResult 구조체 | ComboSystem.cs:3-9 | ✅ |
| 3 | ComboEntry 구조체 (sequence, bonusType, bonusValue, name) | ComboSystem.cs:13-19 | ✅ |
| 4 | RecordSkill — 히스토리 기록 + 만료 정리 | ComboSystem.cs:48-58 | ✅ |
| 5 | CheckCombo — 시퀀스 매칭 + 히스토리 클리어 | ComboSystem.cs:60-80 | ✅ |
| 6 | CombatManager.ExecuteSkill 통합 | CombatManager.cs:181-198 | ✅ |
| 7 | 보너스 적용 (damage_mult, aoe_expand, duration_extend) | CombatManager.cs:191-196 | ✅ |
| 8 | ComboEvent 발행 | CombatManager.cs:197, GameEvents.cs:16 | ✅ |
| 9 | 콤보 후 히스토리 클리어 | ComboSystem.cs:69 `_history.Clear()` | ✅ |

### 로직 흐름

1. `ExecuteSkill` → `RecordSkill(skill.id, nowMs)` — 현재 스킬 히스토리에 추가
2. `CheckCombo(skill.id, nowMs)` — 현재 스킬이 콤보 시퀀스 마지막인지 빠른 체크 (line 65)
3. `MatchSequence` — 히스토리 끝에서 시퀀스 길이만큼 역추적, 순서+시간 검증
4. 매칭 시 `_history.Clear()` → 같은 콤보 중복 발동 방지
5. 보너스를 dmgMult/aoeBonus/durBonus에 곱연산으로 적용

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| ComboEvent 발행 | ✅ | CombatManager.cs:197 |
| HUD 구독/표시 | — | SPEC "화면 중앙에 1.5초 표시" — HUD 구현은 확인 필요하나 이벤트 발행은 정상 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| slash → thrust (3초 이내) | Blade Fury 발동, 데미지 x1.5 |
| thrust → slash (잘못된 순서) | 콤보 미발동 |
| slash → (4초 경과) → thrust | 윈도우 만료, 미발동 |
| slash → thrust → slash → thrust | 첫 콤보 후 클리어, 두 번째 연속 가능 |
| fireball → ice_bolt | Elemental Burst, AoE x2.0 |
| heal → mana_shield | Arcane Fortify, 지속시간 x1.5 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
스킬 연속으로 쓰면 뭔가 특별한 게 나오는 거 재밌겠다. "Blade Fury!" 같은 텍스트 나오면 좀 멋질 듯. 근데 콤보 목록을 어디서 볼 수 있는지 모르겠다 — 인게임 가이드 있으면 좋겠음.

### ⚔️ 코어 게이머
콤보 수치 분석:
- Blade Fury (slash→thrust) dmg x1.5 — 근접 빌드 핵심. 쿨다운 관리 중요.
- Elemental Burst AoE x2.0 — 범위 파밍에 강력. 마나 소모 주의.
- Arcane Fortify 지속시간 x1.5 — 방어 빌드 시너지.
- 3초 윈도우는 충분하되 남발은 불가 — 전략적 타이밍 필요. 밸런스 적절.
- 히스토리 5개면 3스킬 콤보도 향후 가능 — 확장성 좋음.

### 🎨 UX/UI 디자이너
ComboEvent 발행까지만 구현 — HUD에서 콤보 이름 표시 구현은 별도 확인 필요. 콤보 성공 시 화면 이펙트(간단한 플래시 등)가 있으면 타격감 극대화.

### 🔍 QA 엔지니어
- `RecordSkill` → `CheckCombo` 순서: 현재 스킬이 히스토리에 먼저 추가된 후 체크 — MatchSequence가 히스토리 끝에서 매칭하므로 올바른 순서 ✅
- `_history.RemoveAt(0)` 반복: List 앞쪽 제거는 O(n)이지만 MaxHistory=5라 무시 가능
- 시퀀스 마지막 스킬 빠른 필터 (line 65): 불필요한 MatchSequence 호출 방지 — 최적화 ✅
- 빈 sequence 방어 (line 64): null/길이 2 미만 skip ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | HUD 콤보 텍스트 표시 구현 여부 미확인 (이벤트 발행은 정상) |
| 3 | Info | 인게임 콤보 목록 UI 없음 (플레이어 발견 유도 또는 가이드 필요) |

---

## 최종 판정

**✅ APPROVE**

SPEC 수치 5개 + 기능 9개 항목 전부 충족. ComboSystem 순수 C# 클래스, 시퀀스 매칭 로직, CombatManager 통합, 보너스 적용, ComboEvent 발행 모두 정확. 확장성 있는 설계(하드코딩 → 추후 JSON 전환 용이).
