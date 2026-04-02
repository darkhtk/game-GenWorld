# SPEC-R-031: NPC 애니메이션 검증 및 연동

**관련 태스크:** R-031

---

## 개요
NpcDef에 AnimationDef 참조를 추가하고, NPCBrain의 대화·반응 상태에서 애니메이션 전이를 검증한다.

## 상세 설명
NpcDef SO에 `AnimationDef animationDef` 필드를 추가한다. NPCBrain에서 idle 대기, 플레이어 접근 시 반응(react), 대화 중(talk) 상태에 맞는 애니메이션이 재생되는지 확인한다. DialogueUI 진입/퇴장 시 애니메이션 전환 타이밍, NpcProfilePanel·NpcQuestPanel 표시 중 NPC 모션이 자연스러운지 검증한다.

## 데이터 구조
```csharp
// NpcDef.cs에 추가
public AnimationDef animationDef;  // 해당 NPC의 애니메이션 정의
```

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| NPCBrain | Animator | 상태 전이 시 트리거 |
| DialogueUI | NPCBrain | 대화 시작/종료 이벤트 |
| NpcProfilePanel | NPCBrain | 프로필 표시 중 idle 유지 |
| NpcDef | AnimationDef (R-027) | SO 참조 |
| DialogueConditionParser | NPCBrain | 조건부 반응 애니메이션 |

## UI 와이어프레임
N/A

## 호출 진입점
- **어디서:** NPCBrain.cs
- **어떻게:** 상태 변경 콜백에서 `SetAnimationState(string stateName)` 호출

## 수용 기준
- [ ] NpcDef에 animationDef 필드 추가, Inspector에서 할당 가능
- [ ] NPC idle 상태에서 idle 애니메이션 루프 정상
- [ ] 플레이어 접근 시 react 애니메이션 재생 (고개 돌리기 등)
- [ ] DialogueUI 진입 시 talk 애니메이션 전환
- [ ] DialogueUI 종료 시 idle 복귀
- [ ] animationDef 미할당 NPC에 대해 경고 로그 출력
