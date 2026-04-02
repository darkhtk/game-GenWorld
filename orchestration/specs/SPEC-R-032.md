# SPEC-R-032: 스킬·이펙트 애니메이션 검증 및 연동

**관련 태스크:** R-032

---

## 개요
EffectSystem·ComboSystem·AreaEffect에서 발동하는 스킬 애니메이션과 VFX 타이밍을 검증하고 보정한다.

## 상세 설명
스킬 발동 시 캐스팅 모션 → 이펙트 생성 → 히트 판정 순서의 타이밍이 시각적으로 자연스러운지 검증한다. EffectSystem의 이펙트 재생, ComboSystem의 연속 공격 모션, AreaEffect의 범위 이펙트 각각에 대해 애니메이션 이벤트(Animation Event) 기반 타이밍을 점검하고, DamageText 출력 시점과의 동기화를 확인한다. 스킬용 AnimationDef를 활용하여 cast·projectile·impact 각 단계의 클립 존재 여부를 검증한다.

## 데이터 구조
N/A (기존 시스템 수정, AnimationDef 참조 추가)

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| ComboSystem | PlayerAnimator | 콤보 스킬 트리거 |
| EffectSystem | ObjectPool | VFX 프리팹 생성 |
| AreaEffect | CombatManager | 범위 데미지 판정 |
| DamageText | CombatManager | 데미지 표시 타이밍 |
| CameraShake | EffectSystem | 화면 흔들림 타이밍 |
| AnimationDef (R-027) | EffectSystem | 스킬 클립 검증 |

## UI 와이어프레임
N/A

## 호출 진입점
- **어디서:** EffectSystem.cs, ComboSystem.cs
- **어떻게:** 스킬 발동 메서드 내에서 AnimationDef 기반 클립 존재 확인 후 Animation Event 타이밍 검증

## 수용 기준
- [ ] 모든 스킬의 cast 애니메이션 클립 존재 확인
- [ ] 스킬 발동 → VFX 생성 → 히트 판정 순서 타이밍 검증 (프레임 단위)
- [ ] ComboSystem 1~3타 각각 모션과 데미지 판정 시점 일치
- [ ] AreaEffect 범위 이펙트와 실제 판정 범위 시각적 일치
- [ ] DamageText 출력 시점이 피격 애니메이션과 동기화
- [ ] CameraShake 발동 시점이 impact 시점과 일치
- [ ] 누락 클립에 대해 콘솔 경고 출력
