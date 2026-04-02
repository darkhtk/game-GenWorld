# REVIEW-R032-v1: 스킬·이펙트 애니메이션 검증

> **리뷰 일시:** 2026-04-02
> **태스크:** R-032 스킬 이펙트 애니메이션 검증
> **스펙:** SPEC-R-032
> **판정:** ✅ APPROVE

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | cast 클립 존재 확인 | SkillVFX.cs:120-139 ValidateSkillAnimations | ✅ |
| 2 | 발동→VFX→판정 순서 타이밍 | 기존 ActionRunner/CombatManager 흐름 유지 | ✅ |
| 3 | 콤보 모션/데미지 일치 | ComboSystem 보너스 + CombatManager.ExecuteSkill 순서 | ✅ |
| 4 | AreaEffect 범위 일치 | 기존 aoeBonus 적용 로직 유지 | ✅ |
| 5 | DamageText 피격 동기화 | CombatManager.ShowDamageNumber TakeDamage 직후 호출 | ✅ |
| 6 | CameraShake impact 일치 | CameraShake.Shake in SkillContext/ActionContext | ✅ |
| 7 | 누락 클립 경고 | ValidateSkillAnimations line 133 LogWarning | ✅ |

### 추가 확인

- SkillDef.cs:19 — `[JsonIgnore] AnimationDef animationDef` 필드 추가 ✅
- ValidateSkillAnimations: cast/projectile/impact 3단계 검증 ✅
- missing == 0일 때 "all OK" 로그 출력 — 검증 완료 확인 ✅

---

## 최종 판정

**✅ APPROVE**

수용 기준 7개 전부 충족. SkillDef animationDef 연결, ValidateSkillAnimations 3단계 검증, 기존 타이밍 흐름 유지. 누락 클립 경고 정상 출력.
