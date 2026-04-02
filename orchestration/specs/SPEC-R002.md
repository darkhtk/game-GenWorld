# SPEC-R002: 오브젝트 풀링 (Projectile/DamageText)

## 목적
Projectile과 DamageText의 빈번한 Instantiate/Destroy를 풀링으로 교체하여 GC 스파이크를 제거한다.

## 현재 상태
- `ActionRunner.cs`에서 Projectile을 매번 Instantiate.
- `CombatManager.cs`에서 DamageText를 매번 Instantiate.
- 전투가 격해지면 프레임 드롭 가능.

## 구현 명세

### 수치
- **Projectile 풀 크기:** 초기 20, 최대 50 (auto-expand)
- **DamageText 풀 크기:** 초기 30, 최대 80
- **자동 회수:** Projectile — 충돌 or 5초 타임아웃. DamageText — 애니메이션 완료(~1초).

### 연동 경로
- **새 파일:** `Assets/Scripts/Core/ObjectPool.cs` (범용 풀 클래스)
- **수정 파일:**
  - `Assets/Scripts/Systems/ActionRunner.cs` — Instantiate → Pool.Get
  - `Assets/Scripts/Systems/CombatManager.cs` — Instantiate → Pool.Get
  - `Assets/Scripts/Effects/DamageText.cs` — Destroy → Pool.Return (OnComplete)
  - `Assets/Scripts/Entities/Projectile.cs` — Destroy → Pool.Return (OnHit/OnTimeout)

### 데이터 구조
```csharp
// Assets/Scripts/Core/ObjectPool.cs
public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly Queue<T> _available;
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly int _maxSize;

    public T Get();          // Dequeue + SetActive(true) + OnGet()
    public void Return(T obj); // SetActive(false) + Enqueue
}
```

### 로직
1. `ObjectPool<T>` — 범용. `Get()`/`Return()` 인터페이스.
2. Projectile: `OnGet()`에서 위치/속도 리셋. `OnHit()`/타임아웃에서 `Return()`.
3. DamageText: `OnGet()`에서 텍스트/색상 설정. 애니메이션 완료 콜백에서 `Return()`.
4. 풀 소진 시 auto-expand (maxSize까지).

### 세이브 연동
- 없음. 풀은 런타임 전용.

## 호출 진입점
- `GameManager.Awake()` 또는 각 시스템 초기화 시점에서 풀 생성.
- 별도 UI 없음.

## 테스트 항목
- [ ] Pool.Get()이 비활성 오브젝트를 반환하는지
- [ ] Pool.Return() 후 오브젝트가 비활성화되는지
- [ ] 풀 소진 시 auto-expand가 동작하는지
- [ ] maxSize 초과 시 새 오브젝트를 생성하지 않는지
- [ ] 전투 시나리오에서 GC.Alloc이 감소하는지 (프로파일러)
