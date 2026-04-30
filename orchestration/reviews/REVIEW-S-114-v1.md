# REVIEW-S-114-v1: 회피 모션 잔상 이펙트 스프라이트

**Task:** S-114
**리뷰 일시:** 2026-04-30
**대상 커밋:** 6ab7a5c — `asset(vfx): S-114 dodge trail sprite + DodgeVFX ghost layer`
**리뷰 모드:** 일반 리뷰
**SPEC 참조:** specs/SPEC-S-114.md 미존재 — BOARD 비고란 ("vfx_dodge_trail 4f + DodgeVFX ghost layer") 만 비공식 수용기준

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `Assets/Art/Sprites/VFX/vfx_dodge_trail.png` | 128×32 4-frame slice, alpha fade 220→55 |
| `Assets/Resources/VFX/vfx_dodge_trail.png` | Resources.LoadAll 용 복제본 |
| `Assets/Scripts/Effects/DodgeVFX.cs` | `_ghostFrames` Sprite[] 캐시 + `_ghostIndex` 카운터 / SpawnTrail에서 ghost SpriteRenderer 추가 (sortingOrder -1, lifetime 0.18s) |
| `gen_dodge_trail.py` | 결정적 스프라이트 재생성 스크립트 |

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| 스프라이트 에셋 존재 | ✅ | `Art/Sprites/VFX` + `Resources/VFX` 두 위치 |
| Sliced 4-frame 메타 | ✅ | 209라인 .meta — sprite mode multiple |
| `Resources.LoadAll<Sprite>("VFX/vfx_dodge_trail")` 경로 일치 | ✅ | Resources/VFX/vfx_dodge_trail.png 매칭 |
| DodgeVFX 정적 클래스 위치 | ✅ | Assets/Scripts/Effects/DodgeVFX.cs |

### 검증 2: 코드 추적
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| `LoadGhostFrames` lazy 캐싱 | ✅ | `_ghostLoaded` 가드로 1회 로드 (line 11-22) |
| 결정적 프레임 순서 | ✅ | `Array.Sort` 이름 비교 — 결정적 |
| `_ghostIndex` 모듈로 처리 | ✅ | `frames[_ghostIndex % frames.Length]` (line 42) — overflow는 int.MaxValue까지 안전 |
| Fallback (sprite 부재 시) | ✅ | `frames == null` 시 ghost 생성 생략, 기존 silhouette clone(line 28-35)는 유지 |
| LINQ 미사용 (CLAUDE.md 규칙) | ✅ | `Array.Sort` 람다는 LINQ 아님 |
| `_ghostIndex` 도메인 리셋 시점 | ⚠️ | 씬 전환/플레이어 사망/풀 클리어 시 리셋 훅 없음. 결정적 4프레임 순환이라 시각적 영향 미미. |
| sortingOrder 음수 가능성 | ⚠️ | 본체 sortingOrder=0이면 sr=0, ghost=-1. 카메라 culling이나 다른 -1 sortingOrder 객체와 겹침 가능 — 실측 권장 |
| Object.Destroy 라이프타임 | ✅ | trail 0.15s, ghost 0.18s — ghost가 약간 늦게 사라져 잔상 효과 |

### 검증 3: UI 추적
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| 회피 입력 → DodgeVFX.SpawnTrail 체인 | ✅ (코드 외) | 이번 커밋 범위 외 — 기존 호출 경로 유지 |
| 시각 피드백 가독성 | ✅ | 기존 silhouette + ghost frame 이중 레이어 — 잔상 강조 |

### 검증 4: 사용자 시나리오
| 시나리오 | 결과 | 비고 |
|---------|------|------|
| 회피 1회 | ✅ | trail + ghost frame 0 노출 |
| 연속 회피 4회 | ✅ | ghost frame 0→1→2→3 순환 |
| 5회+ | ✅ | frame 0으로 wrap (모듈로) |
| Resource 미배포 빌드 | ✅ | `frames == null` 가드로 silhouette만 노출 |

---

## 페르소나 리뷰

### 🎮 하늘 (캐주얼 게이머)
**인상**: "회피하니까 잔상이 살짝 남아서 멋있어졌네. 이전엔 그냥 깜박이고 끝이었는데."
**문제점**: 없음 — 시각적으로 명확한 개선
**제안**: 잔상 색상이 #88ddff(파란빛)인데 캐릭터 컬러 팔레트에 맞춰 톤 조정 옵션 검토

### ⚔️ 태현 (코어 RPG 게이머)
**인상**: "회피의 무게감이 살짝 늘었다. 4프레임 사이클이라 빠른 연속 회피에서 패턴이 약간 보이지만, 0.15초/0.18초 짧은 라이프타임이라 무시할 수준."
**문제점**: 잔상 fade alpha(220→55)가 캐릭터 SilhouetteClone(alpha 0.3)과 겹쳐 보일 때 시각 노이즈 가능
**제안**: 추후 i-frames 시각화로 ghost 색상을 무적 시간과 동기화

### 🎨 수아 (UX/UI 디자이너)
**인상**: "포지션·sortingOrder·라이프타임 모두 적절. Resources 캐시 한 번 + 풀 없이 매번 GameObject 생성 패턴은 GC 부담 우려가 있지만, 회피 빈도(쿨다운 있음)를 고려하면 허용 범위."
**문제점**:
- 데미지 텍스트 풀(DamageText) 처럼 풀링 적용하면 알로케이션 0이 가능
- ghost sortingOrder 음수 가능성 (line 44) — 환경광/배경 sortingOrder와 충돌 시 묻힘
**제안**: VFX 객체 풀 도입 (S-114는 OK, 후속 태스크 권장)

### 🔍 준혁 (QA 엔지니어)
**인상**: "정적 카운터 + Lazy 로드 패턴 안정적. NRE 가드 충분."
**문제점**:
- 신규 테스트 없음 — DodgeVFX는 정적 클래스라 PlayMode 검증 필요
- `_ghostIndex` static 누적 — 도메인 리로드 끄고 장기 실행 시 int.MaxValue 도달 (이론값 ~2.1B, 실 게임 무관)
- Resources/VFX/vfx_dodge_trail.png 와 Art/Sprites/VFX/vfx_dodge_trail.png 두 사본 — 한 쪽만 수정 시 동기화 누락 위험
**제안**:
1. `gen_dodge_trail.py` CI 검증 또는 Resources/ 만 단일 소스로 유지
2. PlayMode 테스트로 SpawnTrail 호출 후 ghost GameObject 생성·라이프타임 검증

---

## 종합 판정

| 페르소나 | 판정 |
|---------|------|
| 🎮 하늘 | ✅ APPROVE |
| ⚔️ 태현 | ✅ APPROVE |
| 🎨 수아 | ✅ APPROVE (마이너) |
| 🔍 준혁 | ✅ APPROVE (마이너) |

**최종 권고: ✅ APPROVE**

---

## 마이너 이슈 (후속 검토)

1. 스프라이트 이중 사본(Art/Sprites + Resources) 동기화 정책 명시
2. DodgeTrail/Ghost GameObject 풀링 도입 (GC 알로케이션 0 목표)
3. ghost sortingOrder 음수 케이스 — 다른 -1 sortingOrder 콘텐츠와 겹침 실측
4. PlayMode 테스트로 4-frame 순환 + sprite null 분기 검증

## 다음 단계
- BOARD: S-114 In Review → ✅ APPROVE → Done 이동 (Coordinator)
- 마이너 이슈는 별도 백로그로 회수 권장 (필수 아님)
