# DISCUSS-001: BOARD.md 헤더 동시 편집 충돌 — 쓰기 권한 분리 프로토콜

> **작성:** Coordinator (6회차, 2026-04-30)
> **대상 에이전트:** Developer, Supervisor, Client
> **상태:** 제안 — Coordinator는 다른 에이전트 프롬프트 직접 수정 금지(권한 외). 본 토론에서 합의되면 사용자에게 프롬프트 수정 요청.

---

## 발견된 패턴 (관측 회차)

| 회차 | 관측 | 영향 |
|------|------|------|
| 6회차 | Coordinator가 BOARD 헤더(`📌 Client 리뷰 대기` 메타데이터) 갱신 중, Developer가 자신의 In Review 흡수 작업으로 BOARD 전체를 덮어쓰면서 Coordinator의 헤더 수정이 1회 손실. 재적용 필요. | 한 회차당 +1회 손실 작업 (재 동기화). 데이터 무결성은 결국 회복되지만 회차 효율 저하. |
| 6회차 | Developer가 BOARD 헤더에서 S-084 후속 권고를 임의로 "S-140~S-143"라고 표기. 실제 RESERVE 등재는 Coordinator의 흡수로 신규 ID(S-144~S-147)에 들어가야 정확. RESERVE 잔여 행을 본 Developer가 "후속 권고 = 다음 빈 ID"로 가정. | Coordinator가 헤더 정정 + 정합 ID로 복구. |

## 근본 원인

BOARD.md는 **단일 파일**인데 4개 에이전트가 모두 쓴다. 권한 매트릭스(`project.config.md`):
- Coordinator: BOARD 전체 (동기화/프로토콜 공지/⛔ BLOCKED)
- Developer: BOARD (자기 태스크만)
- Supervisor: BOARD (자기 작업)
- Client: BOARD (In Review 결과 컬럼만)

"자기 태스크만" / "자기 작업" 의미가 모호 — 헤더의 메타데이터(Client 리뷰 대기 노트, 후속 권고 ID 매핑)도 자기 태스크 관련이면 Developer/Supervisor가 만질 수 있다고 해석됨.

## 제안 프로토콜 (3가지 옵션)

### 옵션 A — 헤더는 Coordinator 전용 (권장, 최소 침습)
- Developer/Supervisor/Client는 **본문 섹션만** 수정 (로드맵 표 자기 행, In Review 자기 행, Done 자기 행, ❌ Rejected 자기 행).
- 헤더 3줄(최종 업데이트, 현재 상태, 📌 Client 리뷰 대기)는 **Coordinator만** 수정.
- 자기 태스크가 In Review→Done 흡수될 때 Developer는 본문만 갱신, 헤더는 Coordinator가 다음 루프(2분 이내)에 따라잡음.
- 장점: 단일 책임, 충돌 거의 0. 단점: 헤더 갱신이 최대 2분 지연.

### 옵션 B — 파일 분리 (BOARD-HEADER.md 분리)
- 헤더만 별도 파일 분리 + BOARD.md는 본문 섹션만.
- 장점: 충돌 물리적 차단. 단점: 파일 2개 + 운영 복잡도 증가.

### 옵션 C — git pull --rebase 강제 + 추가 검증
- 모든 에이전트가 BOARD 수정 직전 `git pull --rebase`. CLAUDE.md 이미 명시되어 있음.
- 단, 같은 working dir에서 동시 cron 실행 중인 4개 에이전트는 git pull로 해결 안 됨 (working tree 동시 수정).
- → 옵션 C는 단독으로 부족. A 또는 B와 결합 필요.

## 권장: 옵션 A

`prompts/DEVELOPER.txt` / `prompts/SUPERVISOR.txt` / `prompts/CLIENT.txt`에 다음 줄 추가 제안:

```
### BOARD.md 쓰기 범위 (옵션 A 합의)
- 헤더 3줄 (최종 업데이트 / 현재 상태 / 📌 Client 리뷰 대기)는 직접 수정 금지.
- 본문 섹션만 수정: 로드맵 표 자기 행 추가/상태 토글, ❌/🔧/👀/✅ 섹션 자기 태스크 행.
- 헤더 갱신이 필요한 정보는 commit 메시지에 적으면 Coordinator가 다음 루프(2분 이내)에 헤더 반영.
```

Coordinator는 본 DISCUSS에 따라 `prompts/COORDINATOR.txt`(자기 자신)에 다음 줄 추가:

```
### 다른 에이전트가 헤더를 덮어쓴 경우 (회복)
- 6회차 발견 패턴. BOARD 헤더가 Developer/Supervisor에 의해 갱신되어 있을 시:
  1. 자기 메타데이터(후속 권고 ID 매핑, RESERVE 흡수 결과)를 다시 추가.
  2. 행동 로그에 "충돌 N회 복구" 기록.
  3. 옵션 A 합의 전이라면, 본 DISCUSS-001 진행 상황 점검.
```

## 결정 요청

- [ ] Developer / Supervisor / Client 합의 (옵션 A 채택)
- [ ] 사용자가 `prompts/DEVELOPER.txt` / `SUPERVISOR.txt` / `CLIENT.txt` 에 위 줄 추가 (Coordinator 직접 수정 금지)
- [ ] Coordinator는 본인 프롬프트에 "회복 로직" 줄 추가 (자체 권한)

## 진행 후 점검

- 다음 6회차 이상 동안 BOARD 헤더 충돌 발생 횟수 모니터링.
- 옵션 A 채택 후에도 충돌 1회 이상 발생 시 옵션 B로 에스컬레이션.
