# SPEC-R-027: AnimationDef — 엔티티별 애니메이션 정의 데이터

**관련 태스크:** R-027

---

## 개요
플레이어·몬스터·NPC·스킬에 필요한 애니메이션 상태를 ScriptableObject로 정의하여 일원화된 참조 체계를 만든다.

## 상세 설명
현재 애니메이션 트리거/상태명이 코드 곳곳에 문자열로 흩어져 있어 어떤 클립이 필요한지 한눈에 파악하기 어렵다. AnimationDef SO를 도입하여 각 엔티티가 요구하는 애니메이션 목록을 데이터로 관리하고, 미리보기·검증 도구의 기반 데이터로 활용한다.

## 데이터 구조
```csharp
[CreateAssetMenu(menuName = "Game/AnimationDef")]
public class AnimationDef : ScriptableObject
{
    [System.Serializable]
    public class AnimEntry
    {
        public string stateName;        // Animator 상태명
        public AnimationClip clip;      // 실제 클립 (nullable = 누락)
        public float expectedDuration;  // 예상 길이(초)
        public bool isLooping;
    }

    public enum EntityType { Player, Monster, NPC, Skill }

    public EntityType entityType;
    public AnimEntry[] entries;
}
```

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| AnimationDef | PlayerAnimator | SO 참조, 상태명 검증 |
| AnimationDef | MonsterDef | SO 참조 필드 추가 |
| AnimationDef | NpcDef | SO 참조 필드 추가 |
| AnimationDef | AnimationPreviewUI (R-028) | 목록 표시·재생 데이터 |

## UI 와이어프레임
N/A (Inspector에서 편집)

## 호출 진입점
- **어디서:** Unity Inspector, 각 Def SO 에셋
- **어떻게:** MonsterDef.animationDef / NpcDef.animationDef 필드로 참조

## 수용 기준
- [ ] AnimationDef ScriptableObject 생성 가능
- [ ] Player용 기본 AnimationDef 에셋 생성 (idle, run, attack, dodge, hit, die)
- [ ] Monster용 기본 AnimationDef 에셋 템플릿 생성 (idle, walk, attack, hit, die)
- [ ] NPC용 기본 AnimationDef 에셋 템플릿 생성 (idle, talk, react)
- [ ] Skill용 기본 AnimationDef 에셋 템플릿 생성 (cast, projectile, impact)
- [ ] clip 필드가 null인 엔트리를 Inspector에서 경고 색상으로 표시하는 PropertyDrawer 작성
