# SPEC-S-079: EffectHolder Buff Stack Cap (maxStack)

## 목표
`EffectHolder`에 동일 타입 버프의 무한 중첩(갱신)을 방지하는 `maxStack` 메커니즘을 도입하여,
특정 버프가 지속적으로 재적용될 때 duration이 무한 연장되거나 value가 비정상적으로 누적되는 것을 차단한다.

## 현재 상태
`EffectHolder` (EffectSystem.cs) — `Dictionary<string, ActiveEffect>` 기반, key = effect type string.

현재 동작 방식:
- **stun**: 기존 stun이 있으면 `expiresAt = Max(기존, 새값)` — duration 연장만 허용, value 불변. `MaxStunMs = 10000ms` 절대 상한 존재.
- **slow**: `expiresAt = Max`, `value = Min(기존, 새값)` — 더 강한 slow로 갱신. 하한 `MinSlow = 0.1f`.
- **dot**: `ApplyDot()`에서 `expiresAt = Max`, `value = Max(기존, 새값)` — 더 강한 dot으로 갱신.
- **기타 (rage, stealth, mana_shield, heal 등)**: 조건분기 없이 `_effects[type] = new ActiveEffect{...}` — **무조건 덮어쓰기**. 동일 버프 재사용 시 이전 ActiveEffect를 새 인스턴스로 교체.

**핵심 문제:**
1. `stun`의 duration 연장이 `MaxStunMs`로 제한되지만, 빠른 주기로 반복 적용하면 실질적으로 무한 stun이 가능하다 (매 적용 시 `now + MaxStunMs`로 갱신되므로).
2. 일반 버프(rage, stealth 등)는 덮어쓰기이므로 "스택 중첩" 자체는 없지만, 지속적 재적용으로 duration이 무한 연장된다. 예: rage 5초 버프를 3초마다 재사용하면 사실상 영구 버프.
3. `EffectHolder`에는 "몇 번 적용되었는가"(stack count) 개념이 없어, 향후 "3중첩 시 추가 효과" 같은 디자인 확장이 불가능하다.

적용 경로:
- `SkillExecutor.HandleSelfBuff()` → `ctx.applyPlayerEffect(buff, now + dur, val)` → `_playerEffects.Apply()`
- `ActionRunner.HandleApplyBuff()` → 동일 경로
- `ActionRunner.HandleApplyEffect()` → 몬스터 `m.Effects.Apply("stun"|"slow", ...)` 
- `SkillExecutor.HandleAoeDebuff()` / `HandlePlaceBlizzard()` → 몬스터에 slow 적용

## 변경 사항

### C1: ActiveEffect에 stack count 필드 추가

```csharp
public class ActiveEffect
{
    public float expiresAt;
    public float value;
    public float interval;
    public float lastTick;
    public float totalDuration;
    public int stackCount;    // NEW — current stack count (1-based)
    public int maxStack;      // NEW — maximum allowed stacks (0 = unlimited)
}
```

### C2: EffectHolder.Apply()에 maxStack 파라미터 추가

시그니처 변경:
```csharp
public void Apply(string type, float expiresAt, float value, int maxStack = 0)
```

적용 로직:
```csharp
if (_effects.TryGetValue(type, out var existing))
{
    // Stack cap check
    if (existing.maxStack > 0 && existing.stackCount >= existing.maxStack)
    {
        // At cap: only refresh duration, do NOT increase stack count
        existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
        return;
    }
    existing.stackCount++;
    existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
    // value handling depends on type (existing stun/slow logic preserved)
    return;
}
// New effect
float nowMs = Time.time * 1000f;
_effects[type] = new ActiveEffect
{
    expiresAt = expiresAt, value = value,
    totalDuration = expiresAt - nowMs,
    stackCount = 1,
    maxStack = maxStack
};
```

기존 stun/slow 전용 분기는 유지하되, maxStack 체크를 공통 앞단에 추가한다.

### C3: 기본 maxStack 값 설정

| Effect Type | maxStack | 근거 |
|-------------|----------|------|
| stun | 1 | 이미 duration 연장 방식, 중첩 불필요. `MaxStunMs` 상한과 병행 |
| slow | 1 | 이미 value 최소화 방식, 중첩 불필요 |
| dot | 1 | 이미 value 최대화 방식, 중첩 불필요 |
| rage | 3 | 전투 버프, 적당한 중첩 허용 |
| stealth | 1 | 투명 상태는 on/off, 중첩 무의미 |
| mana_shield | 1 | 방어 버프, 단일 유지 |
| heal | 0 (무제한) | 즉시 효과이므로 제한 불필요 |
| 기타 미정의 | 0 (무제한) | 기본값 — 향후 데이터 주도로 설정 가능 |

이 기본값은 `EffectHolder` 내부 `static readonly Dictionary<string, int>` 또는 호출 측에서 전달하는 방식 중 택 1. **권장: 호출 측 전달** — `Apply()` 시그니처에 `maxStack` 파라미터로 전달하되, 기존 호출 코드는 기본값 0으로 동작하여 하위 호환성 유지.

### C4: GetStackCount() 메서드 추가

```csharp
public int GetStackCount(string type) =>
    _effects.TryGetValue(type, out var e) ? e.stackCount : 0;
```

UI 표시 또는 향후 "n중첩 보너스" 로직에 활용 가능.

## 수치/상수

| 상수 | 값 | 위치 |
|------|-----|------|
| `MaxStunMs` | 10000f (기존) | `EffectHolder` |
| `MinSlow` | 0.1f (기존) | `EffectHolder` |
| 기본 maxStack | 타입별 (위 표 참조) | 호출 측 또는 `EffectHolder` static map |

## 연동 경로

| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Systems/EffectSystem.cs` | C1, C2, C4 — `ActiveEffect` 필드 추가, `Apply()` 시그니처/로직 변경, `GetStackCount()` 추가 |
| `Assets/Scripts/Systems/SkillExecutor.cs` | `HandleSelfBuff()` → `applyPlayerEffect` 호출 시 maxStack 전달 (선택적) |
| `Assets/Scripts/Systems/ActionRunner.cs` | `HandleApplyBuff()`, `HandleApplyEffect()` → maxStack 전달 (선택적) |
| `Assets/Scripts/Systems/CombatManager.cs` | `_applyEffectDel` 시그니처 변경 필요 시 수정 |
| `Assets/Scripts/Core/GameManager.cs` | 변경 없음 |

**주의:** `Apply()` 시그니처에 `maxStack = 0` 기본값을 사용하면, 기존 모든 호출 코드가 수정 없이 컴파일 가능하다. 단, `CombatManager._applyEffectDel`의 `Action<string, float, float>` delegate 시그니처는 변경 불필요 (기본 파라미터는 delegate에 투명하지 않으므로, delegate 경유 호출은 maxStack=0으로 동작).

## 호출 진입점

1. **플레이어 버프 적용:** `SkillExecutor.HandleSelfBuff()` → `ctx.applyPlayerEffect(buff, now+dur, val)` → `CombatManager._applyEffectDel` → `_playerEffects.Apply(type, expires, val)` 
2. **몬스터 debuff 적용:** `ActionRunner.HandleApplyEffect()` → `m.Effects.Apply("stun"|"slow", ...)`, `SkillExecutor.HandleAoeDebuff()` → `m.Effects.Apply("slow", ...)`
3. **Tick 제거:** `EffectHolder.Tick(now)` — expired effect 제거 시 stackCount도 함께 삭제됨 (ActiveEffect 인스턴스 자체 제거)

## 데이터 구조

`ActiveEffect` 클래스에 `stackCount` (int), `maxStack` (int) 필드 추가.
`ActiveEffectInfo` 구조체에도 `stackCount` 필드 추가 (UI에서 "x3" 등 표시 가능):

```csharp
public struct ActiveEffectInfo
{
    public string type;
    public float expires;
    public float totalDuration;
    public float value;
    public int stackCount;     // NEW
}
```

## 세이브 연동

영향 없음. `EffectHolder`의 active effects는 런타임 전용이며 세이브/로드 대상이 아니다.
(버프는 세션 간 유지되지 않음)

## 검증 기준

- [ ] `stun` 1회 적용 후 재적용 시 `stackCount == 1` 유지, duration만 연장 (기존 동작 유지)
- [ ] `rage` 3회 적용 후 4번째 적용 시 `stackCount == 3` 유지, duration만 갱신
- [ ] `stealth` 재적용 시 덮어쓰기 대신 stack cap(1) 적용
- [ ] `Apply()` 기본 maxStack=0 호출 시 무제한 스택 (기존 하위 호환)
- [ ] `GetStackCount()` 반환값이 실제 stackCount와 일치
- [ ] `Tick()` 에서 expired effect 제거 시 정상 동작 (이후 동일 타입 재적용 시 stackCount=1부터 시작)
- [ ] `GetActive()` 반환 목록에 stackCount 포함
- [ ] 기존 stun/slow/dot 전용 로직(MaxStunMs, MinSlow, value 갱신)이 변경 없이 동작
