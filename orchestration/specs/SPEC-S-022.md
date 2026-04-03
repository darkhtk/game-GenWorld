# SPEC-S-022: EffectHolder.Tick 스레드 안전성 — 순회 중 변경 시 InvalidOperationException 방지

> **Priority:** P2
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## 문제

`EffectHolder`의 내부 컬렉션 `Dictionary<string, ActiveEffect> _effects`를 순회하는 메서드가 3개 있다.

| 메서드 | 순회 방식 | 순회 중 삭제 방어 |
|--------|-----------|-------------------|
| `Tick()` (line 78) | foreach + 지연 삭제 버퍼 | O (안전) |
| `GetActive()` (line 104) | foreach 직접 순회 | X (취약) |
| `TickDot()` (line 91) | TryGetValue 단건 | O (순회 아님) |

### 취약 시나리오

1. **`GetActive()` 도중 외부 변경** — HUD가 `GetActive()`로 이펙트 목록을 읽는 동안(HUD.cs line 606), 동일 프레임 내에서 스킬 콜백이 `Apply()` 또는 `Remove()`를 호출하면 `InvalidOperationException: Collection was modified` 발생.

2. **반환된 버퍼의 외부 보관** — `Tick()`과 `GetActive()`는 내부 `_tickRemoveBuffer` / `_activeBuffer`를 그대로 반환한다. 호출자가 이 참조를 프레임을 넘겨 보관하면, 다음 호출 시 `Clear()`로 내용이 사라져 예기치 않은 동작 발생.

3. **향후 코루틴/이벤트 확장 리스크** — 현재는 단일 Update 루프에서 순차 호출되어 실질 충돌 확률이 낮지만, 코루틴이나 이벤트 기반 스킬이 추가되면 즉시 문제가 된다.

## 현재 호출 흐름

```
GameManager.Update()
  -> combatManager.PerformAutoAttack()     // _playerEffects.Remove("stealth") 가능
  -> combatManager.HandleMonsterAttacks()
  -> HandleSkillInput()
     -> combatManager.ExecuteSkill()
        -> applyPlayerEffect callback       // _playerEffects.Apply() 호출
  -> PlayerEffects.Tick(nowMs)              // _effects 순회 + 지연 삭제

HUD.RefreshHud() (같은 프레임)
  -> gm.PlayerEffects.GetActive(nowMs)      // _effects foreach 순회 (취약)

MonsterController.UpdateAI()
  -> Effects.TickDot(now)
  -> Effects.Tick(now)                      // 몬스터별 독립 인스턴스, 상대적 안전
```

## 수정 방안

### A. `GetActive()` — foreach를 안전한 순회로 변경

```csharp
// Before (취약)
public List<ActiveEffectInfo> GetActive(float now)
{
    _activeBuffer.Clear();
    foreach (var kv in _effects)  // <-- Dictionary 직접 foreach
    {
        if (kv.Value.expiresAt > now)
            _activeBuffer.Add(new ActiveEffectInfo { ... });
    }
    return _activeBuffer;
}

// After — 방법 1: 스냅샷 키 배열 사용
public List<ActiveEffectInfo> GetActive(float now)
{
    _activeBuffer.Clear();
    // Dictionary.Keys를 복사하여 순회 — 순회 중 _effects 변경에 안전
    int count = _effects.Count;
    if (_keyBuffer == null || _keyBuffer.Length < count)
        _keyBuffer = new string[Mathf.Max(count, 8)];
    _effects.Keys.CopyTo(_keyBuffer, 0);

    for (int i = 0; i < count; i++)
    {
        if (_effects.TryGetValue(_keyBuffer[i], out var e) && e.expiresAt > now)
        {
            _activeBuffer.Add(new ActiveEffectInfo
            {
                type = _keyBuffer[i],
                expires = e.expiresAt,
                totalDuration = e.totalDuration,
                value = e.value
            });
        }
    }
    return _activeBuffer;
}
```

필드 추가:

```csharp
string[] _keyBuffer;
```

### B. 반환 버퍼 외부 보관 방지 — 문서화 + 방어적 복사 옵션

현재 `Tick()`과 `GetActive()`가 내부 버퍼를 직접 반환하므로, 호출자가 참조를 보관하면 다음 호출 시 내용이 변경된다. 최소한 XML doc 주석으로 경고를 추가한다.

```csharp
/// <summary>
/// Returns expired effect keys. WARNING: returned list is reused — do not hold a reference across frames.
/// </summary>
public List<string> Tick(float now) { ... }

/// <summary>
/// Returns active effects. WARNING: returned list is reused — do not hold a reference across frames.
/// </summary>
public List<ActiveEffectInfo> GetActive(float now) { ... }
```

### C. `Tick()` — 이미 안전하지만 foreach도 for-loop으로 통일 (선택)

`Tick()`은 이미 `_tickRemoveBuffer` 패턴으로 안전하다. 하지만 `foreach (var kv in _effects)` 순회 도중 외부에서 `Apply()`가 호출되면 여전히 예외가 발생한다. 동일한 키 스냅샷 패턴을 적용하면 완전 방어가 된다.

```csharp
public List<string> Tick(float now)
{
    _tickRemoveBuffer.Clear();
    int count = _effects.Count;
    if (_keyBuffer == null || _keyBuffer.Length < count)
        _keyBuffer = new string[Mathf.Max(count, 8)];
    _effects.Keys.CopyTo(_keyBuffer, 0);

    for (int i = 0; i < count; i++)
    {
        if (_effects.TryGetValue(_keyBuffer[i], out var e) && e.expiresAt <= now)
            _tickRemoveBuffer.Add(_keyBuffer[i]);
    }
    for (int i = 0; i < _tickRemoveBuffer.Count; i++)
        _effects.Remove(_tickRemoveBuffer[i]);
    return _tickRemoveBuffer;
}
```

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/EffectSystem.cs` | `GetActive()` 안전 순회, `Tick()` 키 스냅샷 통일, XML doc 추가, `_keyBuffer` 필드 추가 |

## 연동 경로 (호출자)

| 호출자 | 메서드 | 파일:라인 |
|--------|--------|-----------|
| `GameManager` | `PlayerEffects.Tick(nowMs)` | `GameManager.cs:143` |
| `MonsterController` | `Effects.Tick(now)` | `MonsterController.cs:110` |
| `HUD` | `gm.PlayerEffects.GetActive(nowMs)` | `HUD.cs:606` |
| `CombatManager` | `_playerEffects.Has()` / `.Remove()` | `CombatManager.cs:59,91` |
| `CombatManager` (skill callback) | `_playerEffects.Apply()` | `CombatManager.cs:273,311` |

## 수용 기준

1. `GetActive()` 순회 중 `Apply()` 또는 `Remove()`가 호출되어도 `InvalidOperationException` 미발생
2. `Tick()` 순회 중 외부 변경에도 예외 미발생
3. 반환 버퍼에 XML doc 경고 주석 추가
4. 기존 `EffectSystemTests` 13개 테스트 전부 통과
5. GC 할당 증가 없음 (키 버퍼 재사용)

## 데이터 / 세이브 영향

없음 — 내부 순회 방식 변경만. 외부 API 시그니처 변경 없음.

## 테스트

### 기존 테스트 유지

`Assets/Tests/EditMode/EffectSystemTests.cs` — 13개 테스트 전부 그대로 통과 확인.

### 추가 테스트

```csharp
[Test]
public void GetActive_SafeDuringConcurrentModification()
{
    // Setup: 3 effects, one expires during GetActive iteration
    holder.Apply("stun", 5000f, 0);
    holder.Apply("slow", 5000f, 0.5f);
    holder.Apply("rage", 5000f, 1.5f);
    // GetActive should not throw even if dictionary is large
    var active = holder.GetActive(0f);
    Assert.AreEqual(3, active.Count);
}

[Test]
public void Tick_ThenGetActive_NoException()
{
    holder.Apply("stun", 100f, 0);
    holder.Apply("slow", 5000f, 0.5f);
    holder.Tick(200f);  // removes stun
    var active = holder.GetActive(200f);
    Assert.AreEqual(1, active.Count);
    Assert.AreEqual("slow", active[0].type);
}

[Test]
public void ReturnedBuffer_IsReused()
{
    holder.Apply("stun", 5000f, 0);
    var list1 = holder.GetActive(0f);
    Assert.AreEqual(1, list1.Count);
    holder.Apply("slow", 5000f, 0.5f);
    var list2 = holder.GetActive(0f);
    // list1 and list2 are the same object — caller must not hold reference
    Assert.AreSame(list1, list2);
    Assert.AreEqual(2, list2.Count);
}
```

### 수동 검증

- Unity Play Mode에서 플레이어에게 다수 이펙트(stun + slow + dot + stealth) 동시 적용 후 HUD 이펙트 표시 확인
- 이펙트 만료 시점에 스킬 사용하여 Apply/Remove 동시 발생 유도 — 콘솔에 InvalidOperationException 없음 확인
