# SPEC-R010: 회피 시 시각적 피드백

## 목적
닷지(Ctrl/Space) 사용 시 플레이어에게 시각적 피드백(플래시 + 잔상)을 제공하여 무적 프레임을 명확히 인지시킨다.

## 현재 상태
- `PlayerController.cs:46-64` — 닷지 로직 존재 (0.2초 지속, 0.8초 쿨다운, Invincible 플래그).
- SpriteRenderer 참조(`_sr`) 있으나 닷지 중 시각 효과 없음.
- `HUD.cs:20` — dodgeFill 이미지로 쿨다운 표시는 있음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Entities/PlayerController.cs` — 닷지 시각 효과 트리거

### 새 파일
- `Assets/Scripts/Effects/DodgeVFX.cs` — 잔상 스폰 + 플래시 처리

### 변경사항

1. **플래시 효과 (SpriteRenderer 색상):**
   ```csharp
   // 닷지 시작 시:
   _sr.color = new Color(1f, 1f, 1f, 0.5f); // 반투명
   
   // 닷지 종료 시:
   _sr.color = Color.white; // 복원
   ```

2. **잔상 효과 (DodgeVFX.cs):**
   ```csharp
   public class DodgeVFX : MonoBehaviour
   {
       // 닷지 중 0.05초 간격으로 잔상 스프라이트 생성 (최대 4개)
       // 잔상: 현재 스프라이트 복사, alpha 0.3 → 0으로 0.15초간 페이드
       // 완료 후 자동 파괴 (또는 풀링 — R-002 이후)
       
       public static void SpawnTrail(SpriteRenderer source, Vector2 position)
       {
           var go = new GameObject("DodgeTrail");
           var sr = go.AddComponent<SpriteRenderer>();
           sr.sprite = source.sprite;
           sr.color = new Color(0.5f, 0.8f, 1f, 0.3f); // 청백색 잔상
           sr.sortingOrder = source.sortingOrder - 1;
           go.transform.position = position;
           Destroy(go, 0.15f);
       }
   }
   ```

3. **PlayerController 연동:**
   ```csharp
   // StartDodge()에서:
   StartCoroutine(DodgeTrailCoroutine());
   
   IEnumerator DodgeTrailCoroutine()
   {
       while (IsDodging)
       {
           DodgeVFX.SpawnTrail(_sr, Position);
           yield return new WaitForSeconds(0.05f);
       }
   }
   ```

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 잔상 생성 간격 | 0.05초 | 닷지 0.2초 동안 ~4개 |
| 잔상 페이드 시간 | 0.15초 | |
| 잔상 알파 | 0.3 → 0 | |
| 잔상 색상 | (0.5, 0.8, 1.0) | 청백색 |
| 플레이어 닷지 알파 | 0.5 | 반투명 |

### 세이브 연동
없음.

## 호출 진입점
- PlayerController.StartDodge() — Ctrl/Space 입력 시 자동.

## 테스트 항목
- [ ] 닷지 중 플레이어가 반투명해지는지
- [ ] 잔상이 4개 정도 생성되는지
- [ ] 잔상이 0.15초 후 사라지는지
- [ ] 닷지 종료 후 플레이어 색상이 복원되는지
- [ ] 연속 닷지 시 이전 잔상과 겹치지 않는지
