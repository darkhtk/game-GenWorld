# REVIEW-S022-v1: EffectHolder.Tick 안전성

> **리뷰 일시:** 2026-04-03
> **태스크:** S-022 EffectHolder.Tick 스레드 안전성
> **스펙:** 없음
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
| TASK 명세 부합 | ✅ | static→instance 버퍼 변환 완료 |
| 기존 코드 호환 | ✅ | 외부 API 변경 없음, 반환 타입 동일 |
| 아키텍처 패턴 | ✅ | instance 버퍼로 인스턴스 간 격리 보장 |
| 테스트 커버리지 | ⚠️ | 미작성 |

### 변경 분석

**instance 버퍼 (EffectSystem.cs:76, 102):**
```csharp
readonly List<string> _tickRemoveBuffer = new();       // line 76
readonly List<ActiveEffectInfo> _activeBuffer = new();  // line 102
```

이전 static 버퍼가 instance 필드로 변경됨.

**사용 패턴:**
- `Tick()` (line 78-89): `_tickRemoveBuffer.Clear()` → 만료 이펙트 수집 → Dictionary 에서 제거
- `GetActive()` (line 104-121): `_activeBuffer.Clear()` → 활성 이펙트 수집 → 반환

**왜 중요한가:**
- `EffectHolder`는 플레이어 1개 + 몬스터 N개 인스턴스가 동시 존재
- 같은 프레임 내 `GameManager.Update()`에서 모든 몬스터의 `Effects.Tick()`이 호출됨
- static 버퍼였다면: 몬스터 A의 Tick이 버퍼에 쓰고, 몬스터 B의 Tick이 Clear하면 A의 만료 이펙트가 제거 안 됨
- instance 버퍼: 각 EffectHolder가 독립 버퍼 → 간섭 없음

**주의: 반환값 참조 문제**
`Tick()`과 `GetActive()`는 내부 버퍼를 직접 반환한다. 호출자가 반환된 List를 보관하고, 다음 호출에서 Clear되면 데이터가 사라진다. 현재 사용 패턴에서는 반환 직후 소비하므로 문제 없으나, 향후 주의 필요.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 버프/디버프 아이콘 | ✅ | GetActive()로 UI 갱신 — instance 버퍼로 안전 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 다수 몬스터 동시 이펙트 만료 | 각자의 버퍼로 독립 처리 |
| 플레이어 + 몬스터 동시 Tick | 간섭 없음 |
| DoT + 스턴 동시 적용 | 기존과 동일 작동 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
보이지 않는 수정이지만, 여러 몬스터에 독을 걸어놓았을 때 이펙트가 갑자기 사라지는 버그가 있었다면 이걸로 해결됐을 것.

### ⚔️ 코어 게이머
static 공유 버퍼는 Unity 싱글 스레드에서도 위험하다 — 같은 프레임 내 순차 호출에서 Clear 충돌이 발생. instance로 격리하는 건 올바른 수정. 추가 GC 비용은 EffectHolder당 List 2개(빈 상태로 시작) — 무시할 수준.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| 인스턴스 간 격리 | ✅ readonly instance 필드 |
| GC 할당 | ✅ 생성 시 1회만 — Clear/재사용 패턴 |
| 반환값 안전성 | ⚠️ 내부 버퍼 직접 반환 — 현재 사용 패턴에서 안전, 문서화 권장 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ static→instance 변환 완료 |
| 기존 호환성 | ✅ API 변경 없음 |
| 코드 품질 | ✅ 올바른 격리 패턴 |

**결론:** ✅ **APPROVE** — static 공유 버퍼를 instance 필드로 변환하여 다수 EffectHolder 인스턴스 간 Tick/GetActive 충돌 방지. GC 비용 무시 가능, 안전성 확보.
