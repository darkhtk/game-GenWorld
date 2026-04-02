# Project Configuration

> 자동 생성됨 (auto-setup.sh, 2026-04-02 20:34)
> Claude 프로젝트 메모리 + 파일 구조에서 감지한 설정입니다.
> 필요 시 수동으로 수정하세요.

## 기본 정보
- **프로젝트명:** GenWorld
- **엔진/프레임워크:** Unity 6000.3.9f1
- **언어:** C#
- **플랫폼:** Windows

## 디렉토리 매핑

| 용도 | 경로 |
|------|------|
| 소스코드 | Assets/Scripts |
| 에셋 (이미지) | Assets/Art/Sprites |
| 에셋 (오디오) | (미감지) |
| 에셋 (리소스) | (미감지) |
| 테스트 | Assets/Tests/EditMode |
| 씬/레벨 | Assets/Scenes |
| 도구 | (미감지) |

## 에이전트 권한

### Supervisor (감독관) 수정 가능
- Assets/Scripts (버그 수정/품질 개선)
- Assets/Art/Sprites
- (미감지)
- (미감지)
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

## 커밋 컨벤션
- conventional commits (feat:/fix:/refactor:/test:/asset:/docs:)
- 한 태스크 = 한 커밋 원칙

## 코드 아키텍처 규칙
- (프로젝트에 맞게 설정)

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
- **이름:** (설정 필요)
- **아이콘:** 🎮
- **역할:** 캐주얼 게이머
- **배경:** (프로젝트 타겟 유저 기반으로 설정)
- **관점:** 직관성, 온보딩
- **말투:** 솔직하고 짧음
- **주로 잡는 문제:** 불친절한 UI, 설명 부족

### 페르소나 2
- **이름:** (설정 필요)
- **아이콘:** ⚔️
- **역할:** 코어 게이머
- **배경:** (장르 경험 풍부)
- **관점:** 시스템 깊이, 밸런스
- **말투:** 분석적
- **주로 잡는 문제:** 시스템 깊이 부족, 밸런스

### 페르소나 3
- **이름:** (설정 필요)
- **아이콘:** 🎨
- **역할:** UX/UI 디자이너
- **배경:** 게임 UI 전문가
- **관점:** 시각적 일관성, 접근성
- **말투:** 전문적
- **주로 잡는 문제:** UI 일관성, 피드백 부재

### 페르소나 4
- **이름:** (설정 필요)
- **아이콘:** 🔍
- **역할:** QA 엔지니어
- **배경:** 게임 QA 경험
- **관점:** 안정성, 예외 처리
- **말투:** 체계적
- **주로 잡는 문제:** 크래시, 경계값 버그

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

## 개발 방향/우선순위
- 안정성 > 게임플레이 > 콘텐츠
- 기존 기능 개선 > 신규 기능
