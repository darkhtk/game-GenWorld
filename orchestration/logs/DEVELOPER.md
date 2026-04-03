# DEVELOPER Loop Log

**Last run:** 2026-04-03
**Status:** 👀 S-002 In Review 대기 + S-003 착수 예정

## Loop Result
- Build errors: 0
- FREEZE: N
- Discussions: N/A (no files)
- NEEDS_WORK (active): 0 (old v1 reviews only — all superseded)
- Action: S-001 APPROVE → Done 처리, S-002 구현 → In Review 제출

## S-002 EventBus 구독 누수 방지 — 구현 내역

### 변경 파일
- `Assets/Scripts/Core/GameManager.cs`

### 구현 내용
1. **OnDestroy() 추가**: GameManager에 OnDestroy 라이프사이클 메서드 신규 추가
2. **EventBus.Clear() 호출**: GameManager 파괴 시 static EventBus의 모든 구독 일괄 해제
3. **영향 범위**: GameManager, QuestSystem, AchievementSystem 등 anonymous lambda 구독 전부 정리됨

### 참고: 잔여 누수 후보 (S-002 범위 밖)
- DeathScreenUI, AchievementUI, DialogueCameraZoom, SteamAchievementManager — 개별 OnDestroy 미구현
- EventVFX는 OnEnable/OnDisable 패턴으로 이미 정상 처리

### specs 참조: N (SPEC-S002 없음)

## 대기 중
- S-002 Client 리뷰 대기 (strict mode)
- 리뷰 대기 중 S-003 착수 가능 (Step 2.6 허용)
