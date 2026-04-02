# SPEC-R015: 몬스터 HP 바

## 목적
몬스터 머리 위에 체력 바를 표시하여 전투 중 적의 남은 체력을 시각적으로 확인한다.

## 현재 상태
- `MonsterController.cs` — Hp, Def.hp (최대 HP) 프로퍼티 존재.
- HUD에 bossBarRoot (보스 전용 HP 바) 존재하나, 일반 몬스터용 월드 스페이스 HP 바 없음.
- `Assets/Art/Sprites/UI/hp_bar_fill.png`, `bar_bg.png` — R-025에서 생성된 바 스프라이트 존재.

## 구현 명세

### 새 파일
- `Assets/Scripts/UI/MonsterHPBar.cs` — 월드 스페이스 HP 바 컴포넌트

### 새 프리팹
- `Assets/Prefabs/UI/MonsterHPBar.prefab` — Canvas(WorldSpace) + HP 바

### 수정 파일
- `Assets/Scripts/Entities/MonsterController.cs` — HP 바 인스턴스 연결

### 프리팹 구조
```
MonsterHPBar (Canvas - WorldSpace)
├── Background (Image: bar_bg.png)
│   └── Fill (Image: hp_bar_fill.png, Image.Type=Filled)
└── CanvasGroup (alpha 제어)
```

### 데이터 구조

```csharp
// MonsterHPBar.cs
public class MonsterHPBar : MonoBehaviour
{
    [SerializeField] Image fillImage;
    [SerializeField] CanvasGroup canvasGroup;
    
    Transform _target;       // 몬스터 Transform
    float _offsetY = 0.8f;   // 머리 위 오프셋
    float _hideDelay = 3f;   // 비전투 시 페이드 시간
    float _lastDamageTime;
    
    public void Init(Transform target, float maxHp);
    public void UpdateHP(int currentHp, int maxHp);
}
```

### 로직

1. **HP 바 생성:** MonsterController.Init()에서 프리팹 인스턴스.
2. **위치:** 매 프레임 몬스터 위치 + offsetY 추적. 카메라를 향해 빌보드 회전.
3. **HP 갱신:** MonsterController.TakeDamage() 호출 시 UpdateHP().
4. **표시 조건:**
   - HP < maxHP일 때만 표시 (만피 시 숨김).
   - 마지막 피격 후 3초 경과 시 페이드 아웃.
5. **색상:** fillAmount > 0.5: 녹색, 0.25~0.5: 노란색, < 0.25: 빨간색.
6. **정리:** 몬스터 사망/디스폰 시 HP 바도 Destroy.

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 바 크기 | 1.0 x 0.15 월드 유닛 | |
| Y 오프셋 | 0.8 | 몬스터 위 |
| 페이드 딜레이 | 3초 | 비전투 후 숨김 |
| 페이드 시간 | 0.5초 | alpha 1→0 |
| 녹색 임계 | > 50% HP | |
| 노란색 임계 | 25~50% HP | |
| 빨간색 임계 | < 25% HP | |

### 세이브 연동
없음. 월드 스페이스 UI 전용.

## 호출 진입점
- MonsterController.Init() — 몬스터 스폰 시 자동 생성.
- UI 진입점 없음 (자동 표시/숨김).

## 테스트 항목
- [ ] 몬스터 피격 시 HP 바가 나타나는지
- [ ] fillAmount가 HP 비율에 비례하는지
- [ ] HP 50% 이하에서 색상이 변경되는지
- [ ] 비전투 3초 후 페이드 아웃되는지
- [ ] 만피 몬스터는 HP 바가 숨겨져 있는지
- [ ] 몬스터 사망 시 HP 바가 파괴되는지
- [ ] 카메라 회전 시 빌보드가 유지되는지
