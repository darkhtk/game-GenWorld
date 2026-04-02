# REVIEW-R041-v2: QA 체크리스트 (Steam)

> **리뷰 일시:** 2026-04-03
> **태스크:** R-041 QA 체크리스트 v2
> **스펙:** SPEC-R-041
> **판정:** ✅ APPROVE

---

## v1 지적사항 해결 확인

| v1 지적사항 | 해결 | 비고 |
|------------|------|------|
| 메모리 누수 테스트 항목 | ✅ | "1-hour continuous play: memory increase < 100MB" |
| 릴리즈 빌드 디버그 로그/UI 비노출 | ✅ | "No Debug.Log output" + "No debug UI visible" |
| 클라우드 충돌 해소 UI | ✅ | "Cloud save conflict resolution UI works" |
| 업적 리셋 수량 기준 | ✅ | "At least 5 achievements: unlock → verify → reset → re-unlock" |
| 스토어 페이지 항목 | ✅ | "Store / Legal" 섹션 추가 |
| EULA / 개인정보처리방침 | ✅ | "EULA / privacy policy prepared and linked" |

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 | 비고 |
|---|---------|------|------|
| 1 | Steam 오버레이 동작 | ✅ | 체크리스트 포함 |
| 2 | 오프라인 모드 정상 | ✅ | "no crash, offline fallback" 명시 |
| 3 | 업적 5개 이상 해제/리셋 | ✅ | 구체적 절차 명시 (unlock → verify → reset → re-unlock) |
| 4 | 클라우드 저장 PC A→B | ✅ | 포함 |
| 5 | 클라우드 충돌 해소 UI | ✅ | 별도 항목으로 추가됨 |
| 6 | 해상도/전체화면 크래시 없음 | ✅ | 포함 + 15초 확인 팝업 체크 추가 |
| 7 | 최소 사양 30fps | ✅ | "30fps minimum on min-spec hardware" |
| 8 | 1시간 메모리 <100MB | ✅ | Unity Profiler 명시 |
| 9 | 릴리즈 빌드 디버그 비노출 | ✅ | Player.log + debug UI 항목 분리 |
| 10 | 빌드 용량 + 불필요 에셋 제거 | ✅ | Editor Log 확인 안내 |
| 11 | 스토어 페이지 필수 항목 | ✅ | description, screenshots, system req |
| 12 | EULA / 개인정보처리방침 | ✅ | 포함 + content rating 추가 (보너스) |

---

## 페르소나별 리뷰

### 🔍 QA 엔지니어
체크리스트가 SPEC-R-041의 12개 수용 기준을 전부 커버한다. 업적 테스트 절차가 구체적이고, 성능/메모리/빌드 무결성 항목도 충분. content rating 항목은 보너스.

---

## 최종 판정: **✅ APPROVE**

v1 지적사항 6건 전부 해결. SPEC-R-041 수용 기준 12/12 충족.
