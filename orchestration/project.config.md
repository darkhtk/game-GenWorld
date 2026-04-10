# Project Configuration

> 자동 생성됨 (auto-setup.sh, 2026-04-03 15:20)
> Claude 프로젝트 메모리 + 파일 구조에서 감지한 설정입니다.
> 필요 시 수동으로 수정하세요.

## 기본 정보
- **프로젝트명:** GENWorld
- **엔진/프레임워크:** Unity 6000.3.9f1
- **언어:** C#
- **플랫폼:** Windows

## Git
- **Remote:** https://github.com/darkhtk/game-GenWorld.git
- **Branch:** master

## Runtime
- Loop interval: 2m

## Orchestration
- Agent mode: full
- Review level: strict
- Dev direction: stabilize

## 디렉토리 매핑

| 용도 | 경로 |
|------|------|
| 소스코드 | Assets/Scripts |
| 에셋 (이미지) | Assets/Art/Sprites |
| 에셋 (오디오) | Assets/Audio |
| 에셋 (리소스) | Assets/Resources |
| 테스트 | Assets/Tests/EditMode |
| 씬/레벨 | Assets/Scenes |
| 도구 | (미감지) |

## 에이전트 권한

### Supervisor (감독관) 수정 가능
- Assets/Scripts (버그 수정/품질 개선)
- Assets/Art/Sprites
- Assets/Audio
- Assets/Resources
- orchestration/logs/SUPERVISOR.md
- orchestration/BACKLOG_RESERVE.md
- orchestration/BOARD.md

### Developer (개발자) 수정 가능
- Assets/Scripts
- Assets/Tests/EditMode
- orchestration/BOARD.md (자기 태스크만)
- orchestration/logs/DEVELOPER.md

### Client (고객사) 수정 가능
- orchestration/reviews/ (생성만)
- orchestration/BOARD.md (In Review 결과 컬럼만)
- orchestration/logs/CLIENT.md

### Coordinator (소통 관리자) 수정 가능
- orchestration/BOARD.md
- orchestration/BACKLOG_RESERVE.md
- orchestration/specs/
- orchestration/logs/COORDINATOR.md
- orchestration/discussions/
- orchestration/prompts/COORDINATOR.txt

## 빌드/컴파일 에러 체크
- **에러 로그 경로:** %LOCALAPPDATA%\Unity\Editor\Editor.log
- **에러 패턴:** "error CS"
- **경고 패턴:** "warning CS"

## 에셋 규격

### 이미지
- 캐릭터 스프라이트: (프로젝트에 맞게 설정)
- 오브젝트 스프라이트: (프로젝트에 맞게 설정)
- UI 아이콘: (프로젝트에 맞게 설정)
- 아트 스타일: (프로젝트에 맞게 설정)

### 오디오
- BGM: (프로젝트에 맞게 설정)
- SFX: (프로젝트에 맞게 설정)

## 커밋/푸시 정책
- **컨벤션:** conventional commits (feat:/fix:/refactor:/test:/asset:/docs:)
- **커밋 단위:** 한 태스크 = 한 커밋 원칙
- **Push 정책:** batch
  - task: 태스크 완료 시마다 commit+push
  - review: In Review 제출 시에만 push (중간 작업은 commit만)
  - batch: 30분마다 변경사항 일괄 push
  - manual: 자동 push 안 함 (에이전트는 commit만, push는 사용자가 수동)

## 코드 아키텍처 규칙
- PascalCase: 클래스, 메서드, 프로퍼티 / camelCase: 파라미터, 로컬 변수 / _camelCase: private 필드
- 파일당 최대 300줄, 1 클래스 = 1 파일 원칙
- EventBus 통해 시스템 간 통신 (직접 참조 금지)
- MonoBehaviour 싱글톤은 GameManager만 허용, 나머지는 일반 클래스
- LINQ 사용 금지 (GC 할당) — 수동 루프 사용
- Phaser Y↓ → Unity Y↑ 변환은 시스템 경계에서만 처리
- 시간 단위: Phaser ms → Unity seconds 변환은 시스템 경계에서만 처리
- 코드/주석은 영어, 한글은 게임 데이터 JSON과 UI 텍스트에만 허용

## 루프 간격
- Supervisor: 2m
- Developer: 2m
- Client: 2m
- Coordinator: 2m

## 알림
- **이메일 subject:** (프로젝트에 맞게 설정)
- **메일 체크 주기:** 5분

## Claude 메모리에서 가져온 피드백 규칙
- (감지된 피드백 없음)

## 리뷰 페르소나

### 페르소나 1
- **이름:** 하늘 (캐주얼 게이머)
- **아이콘:** 🎮
- **역할:** 캐주얼 게이머
- **배경:** RPG 경험 적음, 직관적 UI 선호
- **관점:** 직관성, 온보딩, 첫인상
- **말투:** 솔직하고 짧음
- **주로 잡는 문제:** 불친절한 UI, 설명 부족, 진입 장벽

### 페르소나 2
- **이름:** 태현 (코어 RPG 게이머)
- **아이콘:** ⚔️
- **역할:** 코어 게이머
- **배경:** 2D RPG/액션 RPG 다수 플레이, 시스템 깊이 중시
- **관점:** 전투 밸런스, 스킬 빌드 다양성, 엔드게임 콘텐츠
- **말투:** 분석적
- **주로 잡는 문제:** 시스템 깊이 부족, 밸런스 불균형, 반복 콘텐츠

### 페르소나 3
- **이름:** 수아 (UX/UI 디자이너)
- **아이콘:** 🎨
- **역할:** UX/UI 디자이너
- **배경:** 게임 UI 전문가, 픽셀아트 게임 UI 경험
- **관점:** 시각적 일관성, 접근성, 정보 계층
- **말투:** 전문적
- **주로 잡는 문제:** UI 일관성, 피드백 부재, 정보 과부하

### 페르소나 4
- **이름:** 준혁 (QA 엔지니어)
- **아이콘:** 🔍
- **역할:** QA 엔지니어
- **배경:** Unity 게임 QA 경험, 크래시/메모리 이슈 전문
- **관점:** 안정성, 예외 처리, 경계값, 메모리
- **말투:** 체계적
- **주로 잡는 문제:** 크래시, NullRef, 경계값 버그, 메모리 누수

## 검증 체계

### 검증 1: 엔진 검증
- **도구:** (MCP 플러그인 있으면 기재)
- **확인 항목:**
  - 씬/레벨 구조
  - 컴포넌트/노드 참조
  - 에셋 존재 여부
  - 빌드 세팅

### 검증 2: 코드 추적
- **확인 항목:**
  - 로직이 TASK 명세에 부합하는가
  - 기존 코드와 호환되는가
  - 아키텍처 패턴 준수
  - 테스트 커버리지

### 검증 3: UI 추적
- **확인 항목:**
  - 입력 → 이벤트 → UI 반응 체인
  - 패널/화면 열기/닫기
  - 데이터 바인딩 정확성

### 검증 4: 플레이 시나리오
- **시나리오 목록:**
  - (프로젝트에 맞게 설정)

## 기존 문서 (에이전트 필독)

> 오케스트레이션 시작 전 기존 작업물. 모든 에이전트는 첫 루프에서 아래 문서를 읽고 프로젝트 맥락을 파악해야 한다.

- `docs/PRE-FLIGHT-CHECKLIST.md` — Pre-Flight Checklist
- `docs/architecture.md` — Architecture
- `docs/current-state.md` — Current State
- `docs/dev-priorities.md` — Development Priorities
- `docs/orchestration/assignments/asset-qa.md` — Current Assignment: Asset/QA
- `docs/orchestration/assignments/dev-backend.md` — Current Assignment: Dev-Backend
- `docs/orchestration/assignments/dev-frontend.md` — Current Assignment: Dev-Frontend
- `docs/orchestration/questions/asset-qa-asmdef-errors.md` — Question from Asset/QA
- `docs/orchestration/questions/backend-gamemanager-bugs.md` — Report from Dev-Backend
- `docs/orchestration/questions/frontend-damage-text-wiring.md` — Question from Dev-Frontend
- `docs/orchestration/questions/frontend-death-marker-wiring.md` — Question from Dev-Frontend
- `docs/orchestration/reference/architecture.md` — System Architecture
- `docs/orchestration/reference/coding-standards.md` — Coding Standards
- `docs/orchestration/reference/data-schema.md` — Data Schema Reference
- `docs/orchestration/reference/interface-contracts.md` — Interface Contracts
- `docs/orchestration/reference/phaser-unity-mapping.md` — Phaser to Unity Mapping Reference
- `docs/orchestration/roles/asset-qa.md` — Role: Asset/QA
- `docs/orchestration/roles/dev-backend.md` — Role: Dev-Backend
- `docs/orchestration/roles/dev-frontend.md` — Role: Dev-Frontend
- `docs/orchestration/roles/director.md` — Role: Director
- `docs/orchestration/status/asset-qa.md` — Status: Asset/QA
- `docs/orchestration/status/compile-status.md` — Compile Status
- `docs/orchestration/status/dev-backend.md` — Status: Dev-Backend
- `docs/orchestration/status/dev-frontend.md` — Status: Dev-Frontend
- `docs/orchestration/status/director.md` — Status: Director
- `docs/testing.md` — Testing Guide
- `README.md` — Project Overview
- `CLAUDE.md` — Claude 프로젝트 설정

## 개발 방향/우선순위
- **방향:** stabilize
- 안정성 > 기존 기능 개선 > 신규 기능. 버그 수정과 방어 코드 최우선. 신규 기능 추가 금지.

## 에이전트 구성
- **모드:** full
  - full: 4개 전부 (Supervisor + Developer + Client + Coordinator)
  - lean: 2개 (Developer + Supervisor만)
  - solo: 1개 (Developer만, 모든 역할 통합)

## 리뷰 강도
- **레벨:** strict
  - strict: 모든 태스크 리뷰 필수
  - standard: 새 시스템만 리뷰, QA/수치조정은 자가진행
  - minimal: 리뷰 없음, 전부 자가진행
