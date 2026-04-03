# REVIEW-S029-v1: 인벤토리 오버플로우 알림

> **리뷰 일시:** 2026-04-03
> **태스크:** S-029 인벤토리 오버플로우 알림
> **스펙:** 없음
> **커밋:** 9e85214
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | overflow 감지 + 경고 로그 + 색상 구분 텍스트 |
| 기존 코드 호환 | ✅ | AddItem 반환값만 캡처 — 기존 동작 변경 없음 |
| 아키텍처 패턴 | ✅ | 기존 ShowFloatingText 인프라 재활용 |
| 테스트 커버리지 | ⚠️ | 미작성 |

### 변경 분석

**GameManager.cs:836-847 (수정):**

```csharp
int overflow = Inventory.AddItem(drop.itemId, drop.count, stackable, maxStack);
// ...
if (overflow > 0)
    Debug.LogWarning($"[GameManager] Inventory full: {overflow}x {itemName} lost");
// ...
combatManager.ShowFloatingText(
    monster.Position + Vector2.up * itemOffset,
    overflow > 0 ? $"+{itemName} x{drop.count - overflow} (FULL)" : $"+{itemName} x{drop.count}",
    overflow > 0 ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 1f, 0.4f));
```

**`AddItem` 반환값 확인 (InventorySystem.cs:27-53):**
- `remaining` = 추가하지 못한 아이템 수
- 정상: `remaining = 0` → overflow = 0
- 부분 추가: `0 < remaining < count` → 일부만 들어감
- 전체 실패: `remaining = count` → 아무것도 안 들어감

**표시 로직:**
- `overflow == 0`: 기존과 동일 (초록색 `+itemName xN`)
- `overflow > 0`: 주황색 `+itemName x{실제추가량} (FULL)`

**경계값 분석:**
- `drop.count = 3, overflow = 1` → `+ItemName x2 (FULL)` ✅
- `drop.count = 3, overflow = 3` → `+ItemName x0 (FULL)` — 전부 유실. 표시는 정확하나 `x0`이 다소 어색. "Inventory Full!" 등이 더 직관적일 수 있으나 기능적으로 문제 없음.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 플로팅 텍스트 체인 | ✅ | 기존 ShowFloatingText 경로 그대로 사용 |
| 색상 구분 | ✅ | 정상=초록(0.4,1,0.4), 오버플로우=주황(1,0.6,0.2) |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 인벤토리 여유 있음 | 기존과 동일 — 초록색 텍스트 |
| 인벤토리 부분 차 있음 (일부 추가 가능) | 추가된 수량 표시 + (FULL) + 주황색 |
| 인벤토리 완전 가득 참 | x0 (FULL) + 주황색 + 로그 경고 |
| 드롭 아이템 없음 (drops 빈 배열) | foreach 미진입 — 영향 없음 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
몬스터 잡았는데 아이템이 조용히 사라지는 건 짜증난다. 주황색으로 "FULL" 표시해주면 "아, 인벤토리 정리해야겠다" 바로 알 수 있어서 좋다.

### ⚔️ 코어 게이머
`AddItem`의 기존 반환값(`remaining`)을 활용한 것이 깔끔하다. 추가 시스템 변경 없이 표시 레이어에서만 처리. `drop.count - overflow`로 실제 획득량을 정확하게 표시한다. 다만 전체 유실(x0)일 때의 표현이 약간 아쉬우나, 기능적으로는 정확하다.

### 🎨 UX/UI 디자이너
주황색(1, 0.6, 0.2)은 경고 색상으로 적절하고 기존 초록색(0.4, 1, 0.4)과 명확히 구분된다. "(FULL)" 텍스트가 간결하게 상태를 전달. 향후 개선으로 전체 유실 시 "Inventory Full!" 별도 메시지를 고려할 수 있다.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| overflow 감지 정확성 | ✅ AddItem remaining 반환값 사용 |
| 로그 기록 | ✅ Debug.LogWarning 포함 — 디버깅 용이 |
| 기존 동작 변경 | ✅ overflow=0이면 완전히 기존과 동일 |
| null 안전성 | ✅ combatManager != null 가드 기존 유지 |
| 성능 영향 | ✅ 없음 — 분기만 추가 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ overflow 감지 + 경고 로그 + 시각적 구분 |
| 기존 호환성 | ✅ overflow=0 경로는 완전히 기존 동작 유지 |
| 코드 품질 | ✅ 최소 변경, 기존 인프라 재활용 |

**결론:** ✅ **APPROVE** — `AddItem` 반환값을 활용하여 인벤토리 가득 참 시 유실 수량을 정확히 표시하고 주황색으로 시각 경고. 최소한의 코드 변경으로 플레이어에게 중요한 피드백을 제공.

**참고:** 전체 유실(overflow == drop.count) 시 "x0 (FULL)" 표시가 다소 어색할 수 있음. 향후 폴리시 태스크로 "Inventory Full!" 별도 표시를 고려 가능.
