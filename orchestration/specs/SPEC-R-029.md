# SPEC-R-029: PlayerAnimator 검증 및 상태 정리

**관련 태스크:** R-029

---

## 개요
PlayerAnimator의 기존 애니메이션 상태 전이를 점검하고, AnimationDef와 연결하여 누락·불일치를 해소한다.

## 상세 설명
PlayerAnimator.cs와 PlayerController.cs에서 사용 중인 Animator 파라미터·트리거·상태명을 전수 조사한다. Animator Controller 에셋과 코드 간 불일치(오타, 미사용 상태, 누락 트리거)를 수정하고, AnimationDef SO와 매칭시켜 검증 가능한 구조로 전환한다. 콤보(ComboSystem), 회피(DodgeVFX), 피격(CombatSystem) 연동 시 애니메이션 전이가 정상 동작하는지 확인한다.

## 데이터 구조
N/A (기존 PlayerAnimator 수정)

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| PlayerController | PlayerAnimator | SetTrigger / SetBool |
| ComboSystem | PlayerAnimator | 콤보 공격 트리거 |
| DodgeVFX | PlayerAnimator | 회피 트리거 |
| CombatSystem | PlayerAnimator | 피격·사망 트리거 |
| AnimationDef (R-027) | PlayerAnimator | 상태명 검증 참조 |

## UI 와이어프레임
N/A

## 호출 진입점
- **어디서:** PlayerAnimator.cs 내부
- **어떻게:** `ValidateAnimationStates()` 에디터 전용 메서드 추가, AnimationDef 대비 Animator 파라미터 일치 검사

## 수용 기준
- [ ] PlayerAnimator에서 사용하는 모든 상태명·트리거명 목록 문서화 (코드 주석)
- [ ] Animator Controller와 코드 간 불일치 항목 0건
- [ ] Player용 AnimationDef SO와 연결 완료
- [ ] idle → run → idle 전이 정상
- [ ] attack (콤보 1~3타) 전이 정상
- [ ] dodge 전이 및 DodgeVFX 타이밍 정상
- [ ] hit → idle 복귀 정상
- [ ] die 상태 진입 후 조작 불가 확인
