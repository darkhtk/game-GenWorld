# SPEC-R-028: AnimationPreviewUI — 인게임 애니메이션 미리보기 디버그 패널

**관련 태스크:** R-028

---

## 개요
런타임 중 엔티티를 선택하여 보유 애니메이션을 목록으로 확인하고 개별 재생·속도 조절할 수 있는 디버그 전용 UI.

## 상세 설명
에디터 밖(빌드 포함)에서도 애니메이션 상태를 빠르게 검증할 수 있어야 한다. 화면 내 엔티티를 클릭하거나 드롭다운으로 선택하면, 해당 엔티티의 AnimationDef에 등록된 애니메이션 목록이 표시되고 버튼으로 재생할 수 있다. `DEBUG` 또는 `DEVELOPMENT_BUILD` 심볼 조건부 컴파일로 릴리스 빌드에서는 제외한다.

## 데이터 구조
```csharp
public class AnimationPreviewUI : MonoBehaviour
{
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private ScrollRect animListScroll;
    [SerializeField] private Slider speedSlider;      // 0.1x ~ 3.0x
    [SerializeField] private Text entityNameLabel;
    [SerializeField] private Text currentStateLabel;
    [SerializeField] private Button closeButton;

    private Animator targetAnimator;
    private AnimationDef targetDef;
}
```

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| AnimationDef (R-027) | AnimationPreviewUI | 목록 데이터 제공 |
| PlayerController | AnimationPreviewUI | 클릭 선택 대상 |
| MonsterController | AnimationPreviewUI | 클릭 선택 대상 |
| NPCBrain | AnimationPreviewUI | 클릭 선택 대상 |
| HUD | AnimationPreviewUI | 디버그 토글 버튼 |

## UI 와이어프레임
```
┌─── Animation Preview (Debug) ─────────────┐
│ Entity: [Goblin_01       ▼]               │
│ Current State: idle                        │
│                                            │
│ ┌────────────────────────────────────────┐ │
│ │ [▶] idle          loop   0.8s         │ │
│ │ [▶] walk          loop   0.6s         │ │
│ │ [▶] attack        once   0.5s         │ │
│ │ [▶] hit           once   0.3s         │ │
│ │ [▶] die           once   1.2s         │ │
│ │ [⚠] rage_roar     clip missing!       │ │
│ └────────────────────────────────────────┘ │
│                                            │
│ Speed: [====●=====] 1.0x                   │
│                                  [ Close ] │
└────────────────────────────────────────────┘
```

## 호출 진입점
- **어디서:** HUD 디버그 메뉴 또는 단축키 (F9)
- **어떻게:** `AnimationPreviewUI.Show(targetGameObject)` 호출

## 수용 기준
- [ ] F9 키로 패널 토글 가능
- [ ] 씬 내 엔티티 클릭 시 해당 엔티티의 애니메이션 목록 표시
- [ ] 드롭다운으로 씬 내 모든 Animator 보유 오브젝트 선택 가능
- [ ] [▶] 버튼 클릭 시 해당 애니메이션 즉시 재생
- [ ] 속도 슬라이더(0.1x ~ 3.0x) 작동
- [ ] clip이 null인 항목에 ⚠ 경고 아이콘 표시
- [ ] `#if DEBUG || DEVELOPMENT_BUILD` 조건부 컴파일 적용
