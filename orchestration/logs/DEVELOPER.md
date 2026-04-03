# DEVELOPER Loop Log

**Last run:** 2026-04-03
**Status:** S-040~S-052 완료, 다음 태스크 착수 예정

## 이번 루프 완료 태스크

| ID | 태스크 | 내용 |
|----|--------|------|
| S-040 | CombatManager 타겟팅 범위 | CombatSystem.IsOnScreen viewport 필터 → APPROVE |
| S-041 | NPC 호감도 데이터 저장 | 검증: 7필드 라운드트립 정상, Restore clamp 방어 추가 (자가진행) |
| S-049 | ObjectPool 최대 크기 제한 | 검증: maxSize 적정 (DT=80, Proj=50), null 처리 완비 (자가진행) |
| S-052 | EventBus 이벤트 순서 안정성 | 검증: LIFO 순서, 전 핸들러 독립 확인, using Linq 제거 (자가진행) |

**specs 참조:** Y (SPEC-S-040, S-041, S-049, S-052)
**빌드 에러:** 0건
