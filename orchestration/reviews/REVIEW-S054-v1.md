# REVIEW-S054-v1: AutoSave 전투 중 저장 방지

> **리뷰 일시:** 2026-04-03
> **태스크:** S-054 AutoSave 전투 중 저장 방지
> **스펙:** SPEC-S-054
> **판정:** ✅ APPROVE

---

## 변경 요약

**변경 파일 (2개 코드):**

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/CombatManager.cs` | `IsInCombat` 프로퍼티 추가 (lines 21-35) — `_cachedMonsters` 순회, 3초 내 피격 몬스터 존재 시 true |
| `Assets/Scripts/Core/GameUIWiring.cs` | `_pendingSave` 필드 (line 18) + RegionVisitEvent 전투 가드 (lines 248-252) + MonsterKillEvent 지연 저장 (lines 263-269) |

**범위:** 자동 저장 트리거 시점 제어. SaveSystem/SaveData 변경 없음. 게임플레이 로직 변경 없음.

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | CombatManager → _cachedMonsters 기존 참조 활용 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### IsInCombat 프로퍼티 (CombatManager.cs:21-35)

```csharp
public bool IsInCombat
{
    get
    {
        if (_cachedMonsters == null) return false;
        float now = Time.time;
        for (int i = 0; i < _cachedMonsters.Count; i++)
        {
            var m = _cachedMonsters[i];
            if (m != null && !m.IsDead && now - m.LastHitByPlayerTime < 3f)
                return true;
        }
        return false;
    }
}
```

- `_cachedMonsters` null 체크 → null-safe. **PASS**
- for 루프 (LINQ 미사용) — 프로젝트 코딩 표준 준수. **PASS**
- `!m.IsDead` + 3초 윈도우 — 사망 몬스터 제외, 최근 전투만 감지. **PASS**
- `LastHitByPlayerTime` — `PerformAutoAttack()` (line 111), `DealDamageToMonster()` (line 360)에서 갱신됨. **PASS**

### _pendingSave 플래그 설정 (GameUIWiring.cs:248-252)

```csharp
if (_combatManager != null && _combatManager.IsInCombat)
{
    _pendingSave = true;
    Debug.Log("[AutoSave] Deferred — in combat");
    return;
}
```

- RegionVisitEvent 핸들러 내부, 30초 쿨다운 체크 이후 위치. **PASS**
- `_combatManager` null 체크 — 방어적. **PASS**
- return으로 `setLastAutoSaveTime()` 호출 스킵 — 쿨다운 타이머 미갱신 (의도적). **PASS**

### _pendingSave 소비 (GameUIWiring.cs:263-269)

```csharp
if (_pendingSave && (_combatManager == null || !_combatManager.IsInCombat))
{
    _pendingSave = false;
    setLastAutoSaveTime(Time.time);
    EventBus.Emit(new SaveEvent());
    Debug.Log("[AutoSave] Deferred save executed — combat ended");
}
```

- MonsterKillEvent 핸들러에서 실행 — 전투 종료 시점 감지. **PASS**
- `_pendingSave = false` → `setLastAutoSaveTime` → `SaveEvent` 순서 — 정확. **PASS**
- 쿨다운 타이머 재설정됨 — 다음 자동 저장까지 30초 보장. **PASS**
- boolean 플래그 → 중복 저장 방지 (멱등성). **PASS**

### 30초 쿨다운 유지 (비전투 시)

- 쿨다운 체크 (line 247) → 전투 체크 (line 248) → 저장 (line 254) 순서
- 비전투 리전 전환: 기존 로직 그대로 동작. **PASS**

### 수동 저장 미차단

- PauseMenuUI `OnSaveRequested` (line 197) → 직접 `SaveEvent` 발행, 전투 가드 미적용
- SPEC 검증항목 4 충족. **PASS**

### 테스트 커버리지

- S-054 전용 단위 테스트: 없음
- `IsInCombat` 프로퍼티는 단위 테스트 가능하나, GameUIWiring 이벤트 플로우는 MonoBehaviour 의존으로 EditMode 테스트 어려움
- **권장:** `IsInCombat` 프로퍼티 단위 테스트 추가 (후속 태스크)

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 → UI 반응 | ✅ | RegionVisitEvent → 전투 체크 → 저장 또는 지연 |
| 패널 열기/닫기 | N/A | UI 변경 없음 |
| 데이터 바인딩 | ✅ | SaveEvent 기존 경로 유지 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 전투 중 리전 전환 | 자동 저장 스킵, _pendingSave 설정 |
| 전투 종료 후 (마지막 몬스터 처치) | 지연 저장 실행, 쿨다운 리셋 |
| 비전투 리전 전환 (30초 경과) | 즉시 자동 저장 (기존 동작) |
| 비전투 리전 전환 (30초 미경과) | 저장 스킵 (기존 쿨다운 동작) |
| 전투 중 수동 저장 (일시정지 메뉴) | 저장 실행 (가드 미적용) |
| 전투 중 연속 리전 전환 | _pendingSave 멱등 설정, 전투 종료 시 1회 저장 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"전투 중에 갑자기 저장되면 나중에 불러올 때 이상한 상태가 될까봐 걱정됐는데, 이제 전투 끝나고 저장된다니 안심이다. 평소 돌아다닐 때는 똑같이 자동 저장되고."

### ⚔️ 코어 게이머
"IsInCombat의 3초 윈도우가 적절하다. 전투 직후 리전 전환 시에도 마지막 히트로부터 3초 이내면 전투로 판정해서 불완전 저장을 방지한다. MonsterKillEvent에서 지연 저장을 소비하는 것도 자연스러운 트리거 포인트. 다만 몬스터가 도망가서 전투가 자연 종료되는 케이스(킬 없이 3초 경과)에서는 _pendingSave가 다음 MonsterKillEvent까지 남아있게 된다 — 실질적으로 다음 전투 종료 시 저장되므로 데이터 손실은 아니나 참고할 점."

### 🎨 UX/UI 디자이너
"사용자 관점에서 눈에 보이는 변화 없음. Debug.Log만 추가되어 디버그 시 자동 저장 지연 여부를 확인 가능. 향후 자동 저장 알림 UI 추가 시 이 이벤트 흐름에 연결하면 됨."

### 🔍 QA 엔지니어

| # | 체크 항목 | 결과 | 비고 |
|---|----------|------|------|
| 1 | IsInCombat null 안전 | ✅ | _cachedMonsters null 체크 |
| 2 | IsInCombat 판정 정확성 | ✅ | !IsDead + 3초 윈도우 |
| 3 | _pendingSave 설정 | ✅ | 전투 중 RegionVisitEvent |
| 4 | _pendingSave 소비 | ✅ | MonsterKillEvent + !IsInCombat |
| 5 | 쿨다운 유지 | ✅ | 비전투 시 기존 동작 |
| 6 | 수동 저장 미차단 | ✅ | PauseMenuUI 직접 SaveEvent |
| 7 | 멱등성 | ✅ | boolean 플래그 |
| 8 | 메모리/리소스 | ✅ | 추가 할당 없음 |
| 9 | 단위 테스트 | ⚠️ | 없음 — 권장 후속 태스크 |

**경계값 참고:** 몬스터가 리쉬/도주로 전투 자연 종료 시, _pendingSave가 다음 MonsterKillEvent까지 잔존. 데이터 손실은 아니나 저장 시점이 지연될 수 있음. 현재 아키텍처에서는 허용 범위.

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 스펙 검증항목 4개 전부 충족 |
| 기존 호환성 | ✅ 비전투 동작 변경 없음 |
| 코드 품질 | ✅ 최소 변경, 방어적 프로그래밍, LINQ 미사용 |
| 아키텍처 | ✅ 기존 EventBus 패턴 준수 |
| 테스트 커버리지 | ⚠️ IsInCombat 단위 테스트 권장 (후속) |

**결론:** ✅ **APPROVE** — 전투 중 자동 저장 방지 로직이 정확하게 구현됨. IsInCombat은 3초 윈도우로 적극 전투 상태를 감지하고, _pendingSave 플래그로 지연 저장을 관리. 비전투 시 기존 30초 쿨다운 동작 유지. 수동 저장 미차단. 코드 변경 범위가 작고 방어적. IsInCombat 단위 테스트를 후속 태스크로 권장하나 승인 차단 요인은 아님.
