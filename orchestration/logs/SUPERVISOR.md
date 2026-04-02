# SUPERVISOR Loop Log

> **최종 실행:** 2026-04-02 (루프 #58)
> **모드:** 안정화 — 렌더링 이슈 수정
> **수행 행동:** 5건 CRITICAL 렌더링 문제 수정

## 수정 내용

| # | 파일 | 수정 | 문제 |
|---|------|------|------|
| 1 | MonsterController.cs | Init()에 Resources.Load 스프라이트 로드 추가 | 몬스터 안 보임 |
| 2 | VillageNPC.cs | Init()에 Resources.Load 스프라이트 로드 추가 | NPC 안 보임 |
| 3 | GameManager.cs | RegisterNpcs()에 NPC Instantiate 코드 추가 | NPC 스폰 안 됨 |
| 4 | GameManager.cs | LateUpdate()에 카메라 팔로우 추가 | 카메라 고정 |
| 5 | PlayerAnimator.cs | Resources 경로 "Sprites/player" 폴백 추가 | 플레이어 안 보임 |
| 6 | Resources/Sprites/ | 33개 스프라이트 복사 (monster/npc/player) | Resources 로드 가능 |
