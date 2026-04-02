# SPEC-R012: HUD 버프/디버프 아이콘 표시

## 목적
플레이어에게 현재 활성 상태이상(버프/디버프)을 HUD 아이콘 + 남은 시간으로 표시한다.

## 현재 상태
- `EffectSystem.cs` — `EffectHolder` 클래스로 버프/디버프 관리 (type, expires, value).
- `HUD.cs` — 상태이상 표시 영역 없음.
- `Assets/Art/Sprites/Icons/` — R-026에서 생성된 상태이상 아이콘 6종 (poison, burn, slow, stun, bleed, mana_shield).

## 구현 명세

### 수정 파일
- `Assets/Scripts/UI/HUD.cs` — 버프 아이콘 영역 추가

### 데이터 구조

```csharp
// HUD.cs에 추가
[Header("Status Effects")]
[SerializeField] Transform effectIconContainer; // HorizontalLayoutGroup
[SerializeField] GameObject effectIconPrefab;    // 아이콘 + 타이머 텍스트

// effectIconPrefab 구조:
// - Image (아이콘 sprite)
// - TextMeshProUGUI (남은 시간 "3s")
// - Image (쿨다운 필 오버레이)
```

### UI 와이어프레임
```
HP [████████████] 120/150
MP [████████████]  80/100
[poison 3s] [mana_shield 8s] [slow 2s]   ← 활성 효과 아이콘 행
```

### 로직

1. **Update에서 매 프레임 갱신:**
   ```csharp
   void UpdateEffectIcons(EffectHolder effects, float now)
   {
       var activeEffects = effects.GetActive(now);
       // 활성 효과 수에 맞게 아이콘 풀 조정 (최대 8개)
       for (int i = 0; i < activeEffects.Count; i++)
       {
           var icon = GetOrCreateIcon(i);
           icon.sprite = GetEffectSprite(activeEffects[i].type);
           float remaining = activeEffects[i].expires - now;
           icon.timerText.text = $"{remaining:F0}s";
           icon.fillOverlay.fillAmount = remaining / activeEffects[i].totalDuration;
       }
       // 초과 아이콘 비활성화
   }
   ```

2. **아이콘-스프라이트 매핑:**
   ```csharp
   static readonly Dictionary<string, string> EffectSpriteMap = new()
   {
       {"poison", "status_poison"},
       {"burn", "status_burn"},
       {"slow", "status_slow"},
       {"stun", "status_stun"},
       {"bleed", "status_bleed"},
       {"mana_shield", "status_mana_shield"},
       {"stealth", "status_stealth"},  // 추후 추가
   };
   ```

3. **EffectHolder 확장 필요:**
   - `GetActive(float now)` 메서드 추가 — 만료되지 않은 활성 효과 리스트 반환.
   - `totalDuration` 필드 추가 (초기 지속 시간 기록 — 필 오버레이 계산용).

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 최대 아이콘 수 | 8 | 동시 표시 제한 |
| 아이콘 크기 | 24x24 px | UI 스케일 기준 |
| 아이콘 간격 | 4px | HorizontalLayoutGroup spacing |
| 갱신 주기 | 매 프레임 | 타이머 정확도 |

### 세이브 연동
없음. 효과 상태는 이미 EffectHolder에서 관리 (세이브 시 포함 여부는 SaveSystem 담당).

## 호출 진입점
- HUD.Update() — 자동 갱신. 별도 UI 진입점 없음.
- 효과 적용: 기존 `EffectHolder.Apply()` 호출 시 자동 반영.

## 테스트 항목
- [ ] 버프 적용 시 아이콘이 즉시 나타나는지
- [ ] 남은 시간이 실시간으로 카운트다운되는지
- [ ] 효과 만료 시 아이콘이 사라지는지
- [ ] 8개 초과 효과 시 최대 8개만 표시되는지
- [ ] 필 오버레이가 남은 시간 비율에 맞게 감소하는지
- [ ] 효과 없을 때 아이콘 영역이 비어 있는지
