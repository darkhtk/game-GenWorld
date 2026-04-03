# SPEC-S-080: PlayerController CCD (Continuous Collision Detection) Verification

## 목표
`PlayerController`의 `Rigidbody2D`가 빠른 이동(dodge, 이동속도 버프 등) 시 
콜라이더를 관통하지 않도록 CCD 설정이 올바르게 구성되어 있는지 검증하고, 
미비한 부분을 보완한다.

## 현재 상태
`PlayerController.cs` (line 29-37) Awake에서:

```csharp
void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
    // ...
    _rb.gravityScale = 0;
    _rb.freezeRotation = true;
    _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
}
```

**현재 올바르게 설정된 항목:**
- `CollisionDetectionMode2D.Continuous` — CCD 활성화됨
- `gravityScale = 0` — 2D 탑뷰 게임에 적합
- `freezeRotation = true` — 물리 회전 방지

**잠재적 문제점:**

### P1: Dodge 속도와 CCD 유효성
Dodge 속도: `DodgeSpeed = 12f` (units/sec), 지속시간: `DodgeDuration = 0.2f`.
Dodge 중 한 프레임 이동 거리 (60fps 기준): `12 * (1/60) = 0.2 units = 6.4px`.
32px(1 unit) 타일 기준, 프레임당 약 20% 타일 이동. **현재 dodge 속도에서는 관통 위험 낮음.**

하지만 이동속도가 외부 버프(`rage`, `SetSpeed()`)에 의해 증가하면:
- `SetSpeed(speed)` 호출로 `_moveSpeed` 설정 — 상한 없음
- `Stats.spd` 기반 계산이 외부(`PlayerStats`)에서 이루어지며, 버프 적용 시 이론적으로 매우 높은 값 가능
- `EffectHolder`의 rage/speed 버프가 스택되면(S-079 이전 기준) 속도 무한 증가 가능

### P2: Rigidbody2D Body Type
코드에서 `bodyType`을 명시적으로 설정하지 않음. Unity 기본값은 `Dynamic`.
`CollisionDetectionMode2D.Continuous`는 `Dynamic` body에서만 유효하므로, 
Inspector에서 실수로 `Kinematic`으로 변경되면 CCD가 무효화된다.

### P3: Collider 설정
`[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]`에 Collider2D가 포함되어 있지 않음.
CCD가 작동하려면 Collider2D가 반드시 있어야 하는데, 컴포넌트 보장이 코드 레벨에서 없다.

### P4: velocity 직접 설정과 물리 갱신
`Update()`에서 `_rb.linearVelocity`를 직접 설정한다 (`FixedUpdate`가 아님).
Unity 물리 엔진은 `FixedUpdate` 주기로 동작하므로, `Update`에서 velocity를 매 프레임 덮어쓰면
물리 시뮬레이션과의 타이밍 불일치가 발생할 수 있다. CCD 정확도에도 영향을 줄 수 있음.

## 변경 사항

### C1: RequireComponent에 Collider2D 추가

```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
```

**참고:** `Collider2D`는 추상 타입이므로 `RequireComponent`가 자동 추가하지 않는다. 
`BoxCollider2D`로 구체화하거나, 런타임 검증으로 대체:

```csharp
void Awake()
{
    // ... existing ...
    if (GetComponent<Collider2D>() == null)
        Debug.LogError($"[PlayerController] Missing Collider2D on {gameObject.name}. CCD requires a collider.");
}
```

### C2: bodyType 명시적 보장

```csharp
void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
    _rb.bodyType = RigidbodyType2D.Dynamic;  // NEW — ensure CCD works
    _rb.gravityScale = 0;
    _rb.freezeRotation = true;
    _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
}
```

### C3: 이동속도 상한 추가

`SetSpeed()` 또는 velocity 설정 시 상한 clamp:

```csharp
const float MaxMoveSpeed = 20f;  // DodgeSpeed(12) 보다 높되, 물리적 합리적 범위

public void SetSpeed(float speed) => _moveSpeed = Mathf.Min(speed, MaxMoveSpeed);
```

Dodge 속도는 `DodgeSpeed = 12f`로 const이므로 이미 제한됨.

### C4: velocity 설정을 FixedUpdate로 이동 검토 (선택적)

이동 로직을 `FixedUpdate`로 옮기면 물리 엔진과의 동기화가 개선된다. 
다만 입력 감지(`Input.GetKey`)는 `Update`에서 해야 하므로, 하이브리드 방식 필요:

```csharp
Vector2 _pendingVelocity;

void Update()
{
    // ... input reading, dodge checks ...
    // Instead of: _rb.linearVelocity = moveDir * _moveSpeed;
    _pendingVelocity = moveDir * _moveSpeed;
}

void FixedUpdate()
{
    _rb.linearVelocity = _pendingVelocity;
}
```

**주의:** 이 변경은 기존 게임 필(feel)에 영향을 줄 수 있으므로, 
P3 안정성 태스크 범위에서는 **검토만 하고 적용은 보류**를 권장한다.
현재 `Update`에서의 velocity 설정으로도 CCD는 작동하며, 
Unity 2D 물리에서 이 패턴은 일반적으로 사용된다.

## 수치/상수

| 상수 | 값 | 비고 |
|------|-----|------|
| `DodgeSpeed` | 12f (기존) | units/sec, 변경 없음 |
| `DodgeDuration` | 0.2f (기존) | seconds, 변경 없음 |
| `MaxMoveSpeed` | 20f (신규) | 이동속도 상한. `DodgeSpeed * 1.67` 수준 |
| 프레임당 최대 이동 | ~0.33 units (60fps) | `20/60 = 0.33`. 32px 타일의 33%. CCD로 충분히 감지 가능 |

## 연동 경로

| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Entities/PlayerController.cs` | C1, C2, C3 적용 |
| `Assets/Scripts/Core/GameManager.cs` | 변경 없음 — `SetSpeed()` 호출 측 |
| `Assets/Scripts/Systems/EffectSystem.cs` | 변경 없음 — 속도 버프 자체는 EffectHolder에서 값만 저장 |
| Prefab: Player | Inspector 확인 — Collider2D 컴포넌트 존재 여부, bodyType 확인 |

## 호출 진입점

- `PlayerController.Awake()` — CCD 설정 시점
- `PlayerController.Update()` — 매 프레임 velocity 설정
- `PlayerController.SetSpeed(float)` — `GameManager` 또는 `PlayerStats`에서 스탯 변경 시 호출
- `PlayerController.StartDodge()` — dodge 시 `DodgeSpeed`로 velocity 설정

## 데이터 구조

변경 없음. `MaxMoveSpeed` const 추가만.

## 세이브 연동

영향 없음. `PlayerController`의 물리 설정은 런타임 전용.
이동속도는 `PlayerStats.CurrentStats.spd`에서 계산되어 `SetSpeed()`로 전달되며,
저장/복원 시 `Stats` 기반으로 재계산된다.

## 검증 기준

- [ ] `Awake()` 후 `_rb.collisionDetectionMode == CollisionDetectionMode2D.Continuous` 확인
- [ ] `Awake()` 후 `_rb.bodyType == RigidbodyType2D.Dynamic` 확인
- [ ] Player 프리팹에 `Collider2D` 컴포넌트 존재 확인 (BoxCollider2D, CapsuleCollider2D 등)
- [ ] `SetSpeed(25f)` 호출 시 실제 `_moveSpeed == 20f` (MaxMoveSpeed 클램프)
- [ ] Dodge 중 벽 타일(Tilemap Collider) 관통 불가 확인 — DodgeSpeed(12)로 벽에 돌진 시 멈춤
- [ ] 최대 이동속도(20) + 대각선 이동 시 벽 관통 불가 확인
- [ ] `Collider2D` 미부착 시 콘솔에 `LogError` 출력 확인
- [ ] 기존 이동 감각(responsiveness) 변화 없음 — 속도 상한이 정상 플레이 범위를 제한하지 않음
