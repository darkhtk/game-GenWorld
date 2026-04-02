# REVIEW-R036+R039-v1: 사망 화면 + 자동 포션

> **리뷰 일시:** 2026-04-02
> **태스크:** R-036 사망 화면 + R-039 자동 포션
> **스펙:** 없음 (폴리시)
> **판정:** ✅ APPROVE

---

## R-036: 사망 화면 (DeathScreenUI.cs, 71행)

| 항목 | 결과 | 상세 |
|------|------|------|
| 패널 + 텍스트 필드 | ✅ | panel, titleText, goldLossText, respawnButtons, canvasGroup |
| EventBus 구독 | ✅ | `On<PlayerDeathEvent>(OnDeath)` line 18 |
| 골드 손실 표시 | ✅ | `Gold lost: {loss}` (DeathGoldPenalty) |
| 플레이어 정지 | ✅ | `SetSpeed(0)` line 36 |
| 페이드인 | ✅ | CanvasGroup alpha 0→1, unscaledDeltaTime (일시정지 안전) |
| 마을 리스폰 | ✅ | position = zero + FullHeal + SetSpeed 복원 |
| 현장 리스폰 | ✅ | FullHeal + SetSpeed 복원 (위치 유지) |
| null 방어 | ✅ | panel, gm, canvasGroup 모두 체크 |

## R-039: 자동 포션 (GameManager.cs:180-189)

| 항목 | 결과 | 상세 |
|------|------|------|
| AutoPotionEnabled 토글 | ✅ | public property, default true |
| HP 임계값 30% | ✅ | `hpPct > 0.3f → return` |
| 쿨다운 1초 | ✅ | `Time.time - _lastAutoPotionTime < 1f` |
| hp_potion 인벤토리 확인 | ✅ | `GetCount("hp_potion") <= 0 → return` |
| 기존 UsePotion 재사용 | ✅ | `UsePotion("hp_potion")` |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
죽으면 화면 나오고 리스폰 선택할 수 있는 게 좋다. 자동 포션도 편하고 — 전투 중 포션 까먹어서 죽는 일 줄어들겠다.

### ⚔️ 코어 게이머
30% HP 자동 포션은 합리적 임계값. 1초 쿨다운으로 포션 낭비 방지. 토글 가능해서 수동 관리도 가능.

### 🔍 QA 엔지니어
- unscaledDeltaTime 사용 — Time.timeScale=0이어도 페이드 동작 ✅
- Respawn 후 SetSpeed 복원 — 사망 시 0으로 설정한 것 되돌림 ✅
- AutoPotion GetCount 체크 — 포션 없으면 시도 안 함 ✅

---

## 최종 판정

**✅ APPROVE**

사망 화면: EventBus 구독, 골드 손실 표시, 2가지 리스폰, 페이드인. 자동 포션: 30% 임계, 1초 쿨다운, 토글, 인벤토리 확인. 모두 정확.
