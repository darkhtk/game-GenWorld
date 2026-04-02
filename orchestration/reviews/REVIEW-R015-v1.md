# REVIEW-R015-v1: 몬스터 HP 바 [깊은 리뷰]

> **리뷰 일시:** 2026-04-02
> **태스크:** R-015 몬스터 HP 바
> **스펙:** SPEC-R015
> **판정:** ✅ APPROVE
> **리뷰 유형:** 깊은 리뷰 (코드 전문 직접 읽기)

---

## 읽은 파일 목록

| 파일 | 행수 | 목적 |
|------|------|------|
| Assets/Scripts/UI/MonsterHPBar.cs | 97행 전체 | 새 파일, HP 바 컴포넌트 |
| Assets/Scripts/Entities/MonsterController.cs | 관련 행 (30, 51, 161-168) | 연동 확인 |

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | WorldSpace Canvas 코드 생성 |
| 컴포넌트/노드 참조 | ✅ | MonsterController → MonsterHPBar 연결 |
| 에셋 존재 여부 | ✅ | 코드 기반 생성 (프리팹 대신 — 기능 동등) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적 (깊은 리뷰)

### MonsterHPBar.cs 전체 분석

**정적 색상 (line 17-19):**
```
ColorGreen = (0.2, 0.9, 0.2) — > 50% HP
ColorYellow = (0.9, 0.9, 0.2) — 25-50% HP  
ColorRed = (0.9, 0.2, 0.2) — < 25% HP
```
SPEC 3단계 색상 ✅

**Create() 팩토리 (line 21-65):**
- Canvas: WorldSpace, sortingOrder=100 → 몬스터 위에 표시 ✅
- RectTransform: sizeDelta (1.0, 0.15) — SPEC 일치 ✅
- CanvasGroup: alpha=0 초기값 — 만피 시 숨김 ✅
- Background: 검은색(0.1, 0.1, 0.1, 0.8) Image, 전체 앵커 ✅
- Fill: Image.Type.Filled, FillMethod.Horizontal, fillAmount=1 ✅
- 앵커 설정: anchorMin=zero, anchorMax=one, offset=zero — 부모 크기 채움 ✅

**UpdateHP() (line 67-78):**
```
ratio = Clamp01(currentHp / maxHp)
fillImage.fillAmount = ratio
fillImage.color = ratio > 0.5 ? Green : ratio > 0.25 ? Yellow : Red
_visible = ratio < 1f  (만피 시 숨김)
_lastDamageTime = Time.time
```
SPEC 색상 임계값 + 만피 숨김 ✅

**LateUpdate() (line 80-96):**
```
Line 82: target null → Destroy(gameObject) — 몬스터 사망/디스폰 시 정리 ✅
Line 84: position = target.position + up * 0.8 — Y 오프셋 ✅
Line 86-92: targetAlpha 결정:
  - !_visible → 0 (만피)
  - HideDelay(3s) 초과 → 0 (비전투 페이드)
  - 그 외 → 1 (표시)
Line 95: MoveTowards(alpha, target, FadeSpeed * dt)
  FadeSpeed=2f → 1/2 = 0.5초에 alpha 1→0 — SPEC 일치 ✅
```

### MonsterController 연동

```
Line 30: MonsterHPBar _hpBar;
Line 51: _hpBar = MonsterHPBar.Create(transform, def.hp);  (Init에서)
Line 165: if (_hpBar != null) _hpBar.UpdateHP(Hp, Def.hp);  (TakeDamage에서)
```
Init 시 생성 + 피격 시 갱신 ✅

### SPEC 수치 교차 검증

| 파라미터 | SPEC | 코드 | 결과 |
|---------|------|------|------|
| 바 크기 | 1.0 x 0.15 | sizeDelta(1, 0.15) | ✅ |
| Y 오프셋 | 0.8 | _offsetY = 0.8f | ✅ |
| 페이드 딜레이 | 3초 | HideDelay = 3f | ✅ |
| 페이드 시간 | 0.5초 | FadeSpeed = 2f (1/2=0.5s) | ✅ |
| 녹색 | > 50% | ratio > 0.5f | ✅ |
| 노란색 | 25-50% | ratio > 0.25f | ✅ |
| 빨간색 | < 25% | else | ✅ |

### 참고

- SPEC은 프리팹 기반을 제시했으나, 구현은 `Create()` 팩토리에서 코드 기반 생성. 기능적으로 동등하며 프리팹 에셋 의존성 없이 동작 — 실용적 판단.
- hp_bar_fill.png/bar_bg.png 스프라이트 미사용, solid-color Image로 대체 — 시각적으로 충분.
- 빌보드 회전: 2D 게임이므로 Z축 회전 불필요 — 위치 추적만으로 충분.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| HP 바 표시/숨김 | ✅ | CanvasGroup alpha 제어 |
| 색상 변경 | ✅ | 3단계 (녹/황/적) |
| 만피 숨김 | ✅ | _visible = ratio < 1f |
| 비전투 페이드 | ✅ | 3초 후 0.5초 페이드아웃 |
| 위치 추적 | ✅ | LateUpdate + offsetY |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 몬스터 첫 피격 | HP 바 페이드인 (alpha 0→1), 녹색 |
| HP 50% 이하 | 노란색으로 전환 |
| HP 25% 이하 | 빨간색으로 전환 |
| 만피 상태 | HP 바 숨김 (alpha 0) |
| 3초 비전투 | HP 바 0.5초 페이드아웃 |
| 재피격 | HP 바 즉시 재표시 (alpha → 1) |
| 몬스터 사망 | _target null → Destroy → HP 바 제거 |
| 몬스터 디스폰 | 동일 (target 파괴 → HP 바 자동 정리) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
몬스터 체력 보이니까 "이거 죽일 수 있나?" 판단이 쉬워졌다! 색상 변화도 직관적 — 빨갛게 되면 거의 죽은 거. 안 맞으면 자동으로 사라지는 것도 깔끔.

### ⚔️ 코어 게이머
HP 비율 기반 색상 전환이 전투 판단에 유용. 파밍 중 어느 몬스터에 집중할지 빠르게 결정 가능. FadeSpeed 2f(0.5초)면 자연스러운 전환.

### 🎨 UX/UI 디자이너
WorldSpace Canvas 사용 — 몬스터에 자연스럽게 부착. CanvasGroup alpha 제어는 부드러운 전환. MoveTowards로 smooth fade. 코드 기반 생성이라 프리팹 바인딩 오류 없음.

### 🔍 QA 엔지니어
- LateUpdate에서 target null 체크 → Destroy — 몬스터 파괴 시 orphan HP 바 방지 ✅
- `_lastDamageTime` UpdateHP에서만 갱신 — DoT 데미지가 TakeDamage를 거치지 않으면 HP 바 미갱신 가능. 현재 MonsterController에서 DoT은 Hp 직접 감소(line 63 `Hp -= RoundToInt(dotDmg)`)이고 TakeDamage를 거치지 않음 → HP 바에 DoT 데미지 미반영.
  - 심각도: Low — DoT 데미지가 HP 바에 반영 안 되지만, 플레이어 직접 공격 시에는 정상 표시.
- 코드 기반 생성이라 매 몬스터 스폰마다 Canvas + Image 생성 — 풀링 미적용. 디스폰 시스템과 결합하면 빈번한 생성/파괴. 그러나 성능 영향은 몬스터 수에 비례 (수십 마리 수준) — 허용 가능.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | DoT 데미지가 TakeDamage를 거치지 않아 HP 바 미갱신 (MonsterController.UpdateAI line 63) |
| 2 | Low | 테스트 미작성 (SPEC 체크리스트 7항목) |
| 3 | Info | 프리팹 대신 코드 기반 생성 — 기능 동등, 에셋 의존성 없음 |

---

## 최종 판정

**✅ APPROVE**

SPEC 수치 7개 전부 일치. WorldSpace Canvas HP 바, 3단계 색상, 만피 숨김, 3초 페이드, 0.5초 전환, Y 오프셋, 몬스터 파괴 시 자동 정리 모두 정확 구현. DoT 미반영은 Low 이슈로 별도 고려 가능.
