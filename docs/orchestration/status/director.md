# Status: Director

## Current: MONITORING (Phase 7)

## Last Update: 2026-04-02

## Announcements
- **Dev-Frontend Phase 7 DONE** — BootScene, MainMenu, UI Refresh, 쿨다운 시각화 (53a48a8)
- **3 GameManager 버그 수정** (6bcf259): AI init, 레벨업 타이밍, 인벤 복원
- **포션 핫키 와이어링** (7479934): R=HP포션, T=MP포션
- **GameManager.Instance 싱글턴 추가** (78a4288): UI 패널 시스템 접근용

### Completed This Session (Director)
- EventBus→HUD 와이어링 (Gold, Level, Region, Kill, Death, Save)
- CombatManager.Skills/PlayerState 와이어링
- interface-contracts.md CombatManager 문서화
- 3 GameManager 버그 수정 (AI init, level-up timing, inventory restore)
- 포션 핫키 콜백 와이어링
- GameManager.Instance 싱글턴

### Phase 7 Progress
| Role | Status |
|------|--------|
| Dev-Backend | LOOP — 콘 필터링, 프로젝타일 콜라이더, 몬스터 콜라이더 수정 완료. Phase 7 assignment 아직 미착수 |
| Dev-Frontend | **DONE** — 4 Steps 전부 완료 (53a48a8) |
| Asset/QA | LOOP — Phase 7 assignment 아직 미착수 |

### Remaining Gaps
- ~~Scene 전환 스크립트~~ → DONE (Dev-Frontend)
- ~~UI 패널 Refresh 연동~~ → DONE (Dev-Frontend)
- ~~포션 핫키~~ → DONE (Director)
- Cinemachine 2D 카메라 미설정 (Asset/QA)
- GameManager SerializeField 수동 연결 필요 (Unity Editor)
- Dev-Backend Phase 7 태스크 (QuestSystem EventBus, 테스트 보강)
- CameraShake→Cinemachine 마이그레이션 (Cinemachine 설치 후)
