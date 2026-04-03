# SPEC-S-049: ObjectPool 최대 크기 제한 — 풀 무한 성장 방지

> **우선순위:** P3
> **방향:** stabilize
> **태그:** 🔧 검증

## 현재 상태

`ObjectPool<T>` (Assets/Scripts/Core/ObjectPool.cs)에 이미 `maxSize` 파라미터가 존재.
- 생성자에서 `int maxSize` 수신
- `Get()` 에서 `_totalCreated < _maxSize` 체크
- 상한 도달 시 `null` 반환

## 검증 항목

이 태스크는 **구현이 아닌 검증** 태스크.

### 1. maxSize 적절성
- [ ] 각 풀 생성 호출에서 maxSize 값 확인 (DamageText, Projectile, Monster 등)
- [ ] 과도하게 큰 maxSize 없는지 확인 (메모리 낭비 방지)

### 2. null 반환 처리
- [ ] `Get()` → null 반환 시 호출자가 null 체크하는지 확인
- [ ] null 미처리 시 NullReferenceException 발생 가능 → 방어 코드 추가 필요

### 3. Return 남용
- [ ] `Return()` 시 큐에 중복 추가 방지 확인
- [ ] 비활성화된 오브젝트만 큐에 반환되는지 확인

## 수정 방향
- **maxSize 미설정 풀 발견 시:** 적절한 상한 추가
- **null 미처리 호출자 발견 시:** null 체크 추가
- **전부 정상 시:** ✅ 확인 완료로 마감

## 세이브 연동
- 없음 (런타임 전용)

## UI 연동
- 없음 (내부 시스템)
