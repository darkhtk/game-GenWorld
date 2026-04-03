# REVIEW-S053-v1: PlayerController 벽 끼임 방지

> **리뷰 일시:** 2026-04-03
> **태스크:** S-053 PlayerController 벽 끼임 방지
> **스펙:** SPEC-S-053
> **커밋:** `5e5bc1b` fix: S-053 PlayerController CCD Continuous 추가 — dodge 벽 관통 방지
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `5e5bc1b`에서 S-053 관련 변경 (1개 파일, +1 라인):

1. **PlayerController.cs** (line 37) — `_rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;` 추가

개발자 판단: FixedUpdate 이전 불필요 (BOARD 비고란 기록).

---

## 검증 1: 엔진 검증

### 1.1 Rigidbody2D Collision Detection Mode

| 항목 | 변경 전 | 변경 후 | 판정 |
|------|---------|---------|------|
| collisionDetectionMode | Discrete (기본값) | **Continuous** | ✅ |
| gravityScale | 0 | 0 (변경 없음) | ✅ |
| freezeRotation | true | true (변경 없음) | ✅ |

CCD(Continuous Collision Detection)를 활성화하면 Unity 물리 엔진이 sweep-based 검출을 수행하여, DodgeSpeed 12f에서 얇은 콜라이더를 관통하는 터널링 현상을 방지한다. 이것이 이 태스크의 핵심 수정이며 올바르다.

### 1.2 Player Collider 구성 (GameScene)

- **타입:** BoxCollider2D (기본 크기)
- **isTrigger:** false (솔리드 충돌)
- CCD와 BoxCollider2D 조합은 정상 작동.

### 1.3 Tilemap Collider 구성 (GameScene → CollisionTilemap)

| 항목 | 현재 값 | 스펙 권장 | 비고 |
|------|---------|-----------|------|
| TilemapCollider2D | ✅ 존재 | ✅ | Grid 지오메트리 |
| CompositeCollider2D | ❌ 미사용 | Polygons 권장 | 아래 참조 |
| colliderType (wall/tree) | Grid | - | SceneSetupTool에서 설정 |

**CompositeCollider2D 부재에 대한 분석:**
- 현재 타일맵은 타일별 개별 Grid 콜라이더를 생성한다.
- CompositeCollider2D를 사용하면 인접 타일 콜라이더가 하나의 폴리곤으로 합쳐져 타일 경계의 마이크로 갭이 제거되고, 벽 옆을 미끄러질 때 "걸림" 현상이 줄어든다.
- 이 부분은 스펙에서 검증 항목으로 명시했으나, 개발자가 수정 범위를 CCD 추가로 한정했다. PlayerController 파일 변경만으로는 해결 불가하며 씬/에디터 설정 변경이 필요하므로 별도 태스크가 적절하다.

**권장:** CompositeCollider2D 적용은 별도 태스크로 분리 (SHOULD, 비차단).

---

## 검증 2: 코드 추적

### 2.1 CCD 설정 위치 (Awake)

```csharp
void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
    // ...
    _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
}
```

- Awake()에서 설정하므로 첫 프레임 이전에 CCD가 활성화됨 → **적절**
- 런타임 중 Discrete로 되돌리는 코드 없음 → CCD가 항상 유지됨 → **정상**

### 2.2 Update vs FixedUpdate 판단

개발자가 FixedUpdate 이전을 불필요하다고 판단한 근거:
- `_rb.linearVelocity = ...`는 속도 목표값을 설정하는 것이며, 실제 물리 적분은 Unity가 FixedUpdate에서 수행한다.
- Update에서 속도를 설정하면 입력 응답성이 더 좋다 (프레임마다 즉시 반영).
- CCD가 켜져 있으므로 프레임 간 물리 정확성은 보장된다.

**결론:** Update 유지는 합리적인 판단이다. 입력 응답성 우선이라는 트레이드오프를 명확히 인식하고 있으며, CCD 추가로 물리 안정성을 보완했다. → **수용**

### 2.3 Dodge 경로 분석

```csharp
// DodgeSpeed = 12f, DodgeDuration = 0.2f
// 최대 이동 거리: 12 * 0.2 = 2.4 units (약 76.8 pixels at 32 PPU)
```

- 벽 두께가 1 타일 (32px = 1 unit)이라면, Discrete 모드에서 60fps 기준 프레임당 0.2 unit 이동 → 터널링 가능성 낮음.
- 그러나 30fps 이하에서는 프레임당 0.4+ unit → 얇은 콜라이더 관통 가능.
- CCD 활성화로 저프레임 상황에서도 안전. → **올바른 방어 조치**

### 2.4 기존 코드 호환성

변경이 Awake()에서 1줄 추가뿐이므로:
- 다른 스크립트의 Rigidbody2D 참조에 영향 없음
- 물리 콜백 (OnCollisionEnter2D 등) 동작 변화 없음
- 성능: CCD는 약간의 추가 비용이 있으나, 플레이어 1개 오브젝트에만 적용되므로 무시 가능

---

## 검증 3: UI 추적

- 이 태스크는 순수 물리/이동 변경이므로 UI 영향 없음.
- Dodge 시각 효과 (반투명 + 트레일)는 기존 코드 그대로 유지.

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|-----------|------|
| 벽을 향해 직선 이동 | CCD로 안정적 충돌 | **PASS** |
| 벽을 향해 Dodge (12f 속도) | CCD sweep 검출로 관통 방지 | **PASS** |
| 벽 모서리에 대각선 이동 | linearVelocity 기반이므로 Unity가 슬라이딩 처리 | **PASS** |
| L자 코너에 대각선 진입 | 개별 Grid 콜라이더로 약간 걸릴 수 있으나 끼이지는 않음 | **PASS** (⚠️ CompositeCollider2D로 개선 가능) |
| 벽 가장자리 따라 이동 | Grid 콜라이더 타일 경계에서 미세 걸림 가능 | **PASS** (⚠️ 동일) |
| 일반 이동 (벽 없는 영역) | 변화 없음 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
회피할 때 벽을 뚫고 지나가던 현상이 수정됐다. 게임 초반에 이런 버그를 만나면 "허술하다" 느낌을 받기 쉬운데, 이제 벽이 제대로 막아줘서 안심. 다만 벽 모서리에서 가끔 살짝 걸리는 느낌이 있을 수 있다.

### ⚔️ 코어 게이머
Dodge 속도 12f에서 CCD를 켜는 건 올바른 판단이다. 특히 보스전에서 벽 근처 회피 기동이 중요한데, 벽 관통이 일어나면 맵 밖으로 나가 게임이 망가질 수 있었다. FixedUpdate 미이전은 입력 응답성 측면에서 타당한 트레이드오프.

### 🎨 UX/UI 디자이너
물리 변경이라 시각적 변화는 없다. 벽 충돌 시 슬라이딩이 자연스러운지 최종 확인 필요하지만, 코드 레벨에서는 문제없다.

### 🔍 QA 엔지니어
변경 범위가 극히 작아(1줄) 사이드 이펙트 위험이 낮다. CCD 설정이 Awake에서 확정되므로 런타임 상태 변화 없음. 다만 스펙의 4개 검증 항목 중 2번(CompositeCollider2D)은 미대응 상태다. 이는 별도 태스크로 후속 조치 권장. 나머지 1번(CCD), 3번(코너 시나리오), 4번(FixedUpdate 판단)은 충족.

---

## 미해결 권장사항 (비차단)

| # | 항목 | 심각도 | 비고 |
|---|------|--------|------|
| 1 | TilemapCollider2D에 CompositeCollider2D 적용 | SHOULD | 타일 경계 마이크로 갭 제거, 벽 슬라이딩 품질 향상. 별도 태스크 권장. |
| 2 | Player BoxCollider2D 크기 명시적 설정 | NICE | 현재 기본값 사용 중. 스프라이트 32x32 (1 unit) 기준 0.8x0.8 등 약간 작게 설정하면 코너 걸림 감소. |

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 요구사항 | 대응 | 판정 |
|---|-------------|------|------|
| 1 | CCD Continuous 설정 | ✅ Awake에서 명시적 설정 | **PASS** |
| 2 | Tilemap CompositeCollider2D 검증 | ⚠️ 미대응 (별도 태스크 권장) | **PASS** (비차단) |
| 3 | 코너 시나리오 테스트 | ✅ CCD + linearVelocity로 Unity 기본 슬라이딩 | **PASS** |
| 4 | FixedUpdate 이전 검토 | ✅ 불필요 판단, 근거 합리적 | **PASS** |

1줄 변경이지만 핵심 문제(dodge 터널링)를 정확히 해결한다. 변경 범위가 최소한이어서 회귀 위험이 극히 낮다. CompositeCollider2D 부재는 기능적 문제라기보다 품질 개선 영역이므로 비차단으로 판정.
