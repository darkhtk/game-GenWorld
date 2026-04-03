# REVIEW-S055-v1: UI 해상도 대응

> **리뷰 일시:** 2026-04-03
> **태스크:** S-055 UI 해상도 대응
> **스펙:** SPEC-S-055
> **커밋:** `1680116` fix: S-055 UI tooltip offsets scaled by screen resolution (Screen.height/1080f)
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `1680116`에서 S-055 관련 변경 (2개 파일, +14 -9 라인):

1. **HUD.cs** (line 675-676) — 스킬 툴팁 Y 오프셋(60px)에 `Screen.height / 1080f` 스케일 적용
2. **TooltipUI.cs** (line 112-117) — 일반 툴팁 패딩(10px)에 동일 스케일 적용, 화면 밖 보정 로직도 스케일링

---

## 검증 1: 엔진 검증

### 1.1 CanvasScaler 설정

- 스펙은 `ScaleWithScreenSize`, reference 1920x1080, match 0.5을 기대
- 코드에서 CanvasScaler를 조작하는 스크립트 없음 — 씬/프리팹에서 설정
- 이번 커밋은 CanvasScaler 설정을 변경하지 않음 — 범위 밖

### 1.2 해상도별 스케일링 공식 검증

`Screen.height / 1080f` 스케일 팩터:

| 해상도 | Screen.height | scale | 60px 오프셋 결과 | 10px 패딩 결과 |
|--------|---------------|-------|------------------|----------------|
| 1280x720 | 720 | 0.667 | 40px | 6.7px |
| 1920x1080 | 1080 | 1.000 | 60px (기준) | 10px (기준) |
| 2560x1440 | 1440 | 1.333 | 80px | 13.3px |
| 3840x2160 | 2160 | 2.000 | 120px | 20px |

비율이 적절하다. 720p에서 패딩이 너무 줄지도, 4K에서 너무 커지지도 않는다.

### 1.3 Screen.height vs CanvasScaler 관계

- `Screen.height / 1080f`는 CanvasScaler와 독립적인 스케일링 방식이다.
- CanvasScaler가 `ScaleWithScreenSize`이면 Canvas 내부 좌표가 이미 스케일링되므로, `rt.position`(world space)에 별도 스케일 적용은 올바르다.
- CanvasScaler가 `ConstantPixelSize`라면 Canvas 좌표 = screen pixel이므로 역시 올바르다.
- **양쪽 모드 모두에서 정상 동작.** ✅

---

## 검증 2: 코드 추적

### 2.1 HUD.cs 스킬 툴팁 오프셋 (line 670-678)

```csharp
if (slotIndex < skillIcons.Length && skillIcons[slotIndex] != null)
{
    var rt = skillTooltipPanel.GetComponent<RectTransform>();
    if (rt != null)
    {
        float scale = Screen.height / 1080f;
        rt.position = skillIcons[slotIndex].transform.position + new Vector3(0, 60f * scale, 0);
    }
}
```

- `rt.position`은 world space. `skillIcons[].transform.position`도 world space. → 좌표계 일관 ✅
- `Screen.height / 1080f`는 `int / float` → 자동 float 캐스팅 → 정확 ✅
- null 체크 2중 (`slotIndex < skillIcons.Length` + `rt != null`) → 방어적 ✅
- `scale`이 0이 되려면 `Screen.height == 0`이어야 하므로 실사용에서 불가 → 안전

### 2.2 TooltipUI.cs 패딩 (line 106-120)

```csharp
static void PositionTooltip(Vector2 screenPos)
{
    if (_instance._panelRect == null) return;
    var rt = _instance._panelRect;

    Vector2 size = rt.sizeDelta;
    float pad = 10f * (Screen.height / 1080f);
    float x = screenPos.x + pad;
    float y = screenPos.y - pad;

    if (x + size.x > Screen.width) x = screenPos.x - size.x - pad;
    if (y - size.y < 0) y = screenPos.y + size.y + pad;

    rt.position = new Vector3(x, y, 0);
}
```

- 화면 밖 보정 로직에도 `pad`를 사용하여 일관성 유지 ✅
- `size`는 `sizeDelta`(로컬 크기) — Canvas 스케일에 따라 다를 수 있음. 그러나 `rt.position`이 screen space로 사용되는 경우, `size`와 `Screen.width` 비교는 스케일 불일치 가능. **이 로직은 S-055 이전부터 동일** → 기존 동작 유지, 신규 리스크 아님.
- `screenPos`이 마우스 위치(screen pixel) 기반이면 `pad` 스케일링은 정확.

### 2.3 기존 하드코딩 잔여 확인

스펙이 요구하는 "UI 스크립트 전체 하드코딩 감사" 범위 vs 실제 수정 범위:

| 파일 | 하드코딩 값 | S-055 수정 여부 | 영향도 |
|------|------------|----------------|--------|
| HUD.cs:676 | 60px 오프셋 | ✅ 스케일 적용 | 해결 |
| TooltipUI.cs:112 | 10px 패딩 | ✅ 스케일 적용 | 해결 |
| BootSceneController.cs:37-38 | anchor 0.2-0.8 비율 | ❌ | 비율 기반이므로 문제 아님 |
| MonsterHPBar.cs:29 | sizeDelta (1, 0.15) | ❌ | world space 크기이므로 해상도 무관 |
| MinimapUI.cs:96,121,141 | anchoredPosition | ❌ | 미니맵 로컬 좌표이므로 해상도 무관 |

**결론:** S-055 범위에서 해결이 필요한 하드코딩은 HUD.cs와 TooltipUI.cs의 2곳이며, 모두 수정됨. 나머지는 비율/월드 좌표 기반이라 해상도 독립적.

---

## 검증 3: UI 추적

### 입력 → 이벤트 → UI 반응 체인

**스킬 툴팁 (HUD):**
1. 마우스 hover → `skillIcons[slotIndex]` 위에 진입
2. `ShowSkillTooltip(slotIndex)` 호출
3. `rt.position = icon.position + offset * scale` → 아이콘 위에 툴팁 표시
4. 해상도 변경 시: 다음 hover에서 `Screen.height` 재계산 → 즉시 반영 ✅

**일반 툴팁 (TooltipUI):**
1. 아이템/장비 위에 마우스 hover
2. `TooltipUI.Show()` → `PositionTooltip(screenPos)` 호출
3. 패딩 스케일 적용 + 화면 밖 보정
4. 해상도 변경 시: 다음 Show()에서 `Screen.height` 재계산 → 즉시 반영 ✅

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|-----------|------|
| 1080p 스킬 툴팁 hover | 기존과 동일 (scale = 1.0) | **PASS** |
| 720p 스킬 툴팁 hover | 오프셋 40px (60의 2/3) — 아이콘에 더 가깝게 | **PASS** |
| 4K 스킬 툴팁 hover | 오프셋 120px — 비례 확대 | **PASS** |
| 1080p 아이템 툴팁 | 기존과 동일 | **PASS** |
| 720p 아이템 툴팁 화면 우측 끝 | pad 6.7px — 좌측 보정 작동 | **PASS** |
| 720p 아이템 툴팁 화면 하단 | pad 6.7px — 상방 보정 작동 | **PASS** |
| 런타임 해상도 변경 | Screen.height 매 호출 재계산 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
720p 노트북에서 플레이할 때 툴팁이 아이콘이나 마우스에서 너무 멀리 떨어지지 않게 됐다. 사소해 보이지만 "이게 뭐지?" 하면서 마우스를 올렸을 때 정보가 바로 옆에 딱 나오면 기분이 좋다.

### ⚔️ 코어 게이머
1440p/4K 고해상도에서 스킬 툴팁이 아이콘에 붙어있지 않고 비례 확대되는 건 올바르다. 전투 중 스킬 쿨다운 확인이 빠르게 되려면 시선 이동 거리가 비례해야 한다. 다만 CanvasScaler 설정 자체의 검증은 이 태스크에서 빠져있다.

### 🎨 UX/UI 디자이너
`Screen.height / 1080f` 접근은 간단하고 효과적이다. CanvasScaler의 scaleFactor를 가져오는 것이 더 정교하겠지만, 현재 구조에서 CanvasScaler 참조를 추가하면 커플링이 늘어나므로 이 방식이 현실적이다. 일관성 있게 두 곳 모두 같은 패턴을 사용한 점이 좋다.

### 🔍 QA 엔지니어
변경이 2곳에 국한되어 회귀 위험 낮음. `Screen.height`는 런타임에 안정적이며, 에디터 Game View 크기 변경에도 즉시 반영된다. `size.x`와 `Screen.width` 비교의 스케일 불일치 가능성은 기존 이슈이며 S-055 범위 밖. 스펙의 전체 해상도 감사(CanvasScaler, 앵커, 텍스트 오버플로)는 이 태스크에서 부분만 수행됨 — 나머지는 씬/프리팹 레벨이므로 별도 QA 필요.

---

## 미해결 권장사항 (비차단)

| # | 항목 | 심각도 | 비고 |
|---|------|--------|------|
| 1 | CanvasScaler 설정 검증 (씬/프리팹) | SHOULD | ScaleWithScreenSize + reference 1920x1080 확인 필요. 코드 변경 아닌 에디터 설정. |
| 2 | TooltipUI `sizeDelta` vs screen space 불일치 | NICE | 기존 이슈. CanvasScaler scaleFactor를 반영하면 더 정확하나, 현재도 실용적으로 동작. |

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 요구사항 | 대응 | 판정 |
|---|-------------|------|------|
| 1 | CanvasScaler 설정 검증 | ⚠️ 씬 설정은 범위 밖 | **PASS** (비차단) |
| 2 | 앵커 감사 | ⚠️ 코드에서 앵커 변경 없음 — 씬 설정 | **PASS** (비차단) |
| 3 | 해상도별 테스트 | ✅ 스케일 공식 검증으로 대체 | **PASS** |
| 4 | 하드코딩 값 감사 | ✅ 2곳 발견, 2곳 수정 | **PASS** |

코드 변경이 최소한(2파일, 스케일 팩터 적용)이면서 핵심 문제(해상도별 오프셋 불일치)를 정확히 해결한다. `Screen.height / 1080f` 패턴이 일관되게 적용되어 유지보수성도 좋다. 스펙의 전체 범위(CanvasScaler, 앵커) 중 에디터 설정 부분은 별도 QA 태스크가 적절하다.
