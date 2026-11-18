# CLAUDE.md — 스시런 (Sushi Run) 스팀판

Unity 6.3 LTS(URP)로 만드는 물리 파티 레이스 게임. 플레이어는 초밥 캐릭터가 되어 라운드마다 장애물 코스를 달리고, 3~4라운드 뒤 1명이 우승한다. 로블록스판(`JangRuBin2/sushi-survival-race`)의 기획을 이어받은 **고품질 스팀 버전**.

- 사용자와는 **한국어**로 대화한다. 코드 식별자·커밋 메시지는 영어, 코드 주석은 짧은 한국어.
- 진행 계획·결정: **`docs/PLAN.md`** (단계와 관문). 지금 단계의 스펙: `docs/specs/`.
- 아트·모션 품질 기준: **`docs/ART-DIRECTION.md`** — "GPT 콘셉트 시트(`docs/reference/`)와 나란히 놓아도 같은 게임처럼 보이는가", 캐릭터는 **쫀득쫀득**. 로블록스식 기본 도형 조립 아트는 회색 박스 단계에서만.
- 사용자 PC 준비·조작: **`docs/SETUP.md`**.
- AI 플레이테스트(Autopilot)·게임 화면 녹화: **`docs/AUTOPILOT.md`**.

## 원칙 (로블록스판에서 이어받음)
- **스킨은 겉모습만**: 크기·히트박스·속도·공격 효과는 모두 같다. 판정 코드에 스킨 id 금지. 수익은 스킨 판매, **유료 뽑기·확률 상품 없음**.
- **판정은 권위 쪽(지금은 로컬, 2단계부터 호스트/서버)**에서만. 클라이언트 연출(쫀득 모션·래그돌·이펙트)은 판정에 영향 없음.
- 특정 작품 캐릭터를 닮은 요청은 장르만 쓰고 오리지널로.
- 로블록스판과 같은 규칙(라운드 수·통과 인원·공격 등)은 **같은 답을 내도록 테스트로 고정**한다(`RoundRulesTests.LuauGolden`). 규칙을 바꾸면 두 판을 같이 본다.

## 구조
```
Assets/SushiRun/
  Core/        # 순수 C# (asmdef noEngineReferences) — Rules, Movement, Combat, Motion(Spring, JellyMotion). UnityEngine 참조 금지
  Runtime/     # MonoBehaviour — Player(SushiCharacter, JellyBody, SushiToyBuilder, AttackAbility), Camera, Level, Bots, Bootstrap
  Editor/      # 에디터 메뉴 (SushiRun → Create Feel Prototype Scene 등)
tests/SushiRun.Core.Tests/   # xUnit, Core 소스를 링크해 net8.0 + LangVersion 9로 컴파일
docs/          # PLAN, SETUP, ART-DIRECTION, specs/, reference/(콘셉트 시트), playtest/
tools/check.sh # 클라우드 검증
```
- 씬·프리팹은 가능한 한 **코드로 만든다**(에디터 메뉴가 씬 저장). YAML을 손으로 쓰지 않는다.
- 단위는 미터. 앞 = +Z, 캐릭터 발바닥 = 원점.
- Unity 6 API: `Rigidbody.linearVelocity`·`linearDamping`, `PhysicsMaterial`, `FindObjectsByType`, Input System(`Keyboard.current` 등).
- C# 9까지만 (file-scoped namespace·record·init 금지 — Unity 호환).

## 검증 (작업 끝내기 전에)
```bash
bash tools/check.sh     # = dotnet test tests/SushiRun.Core.Tests (마지막 줄 "Failed: 0" 확인)
```
- **Unity 컴파일·실행은 클라우드에서 못 한다.** Runtime/Editor 코드는 꼼꼼히 리뷰하고, 보고에 "Unity 컴파일은 사용자 에디터 확인 필요"라고 분명히 적는다. 사용자가 콘솔 오류를 보내 주면 그걸로 고친다.
- 에디터에서 확인할 것은 스펙의 "에디터에서 볼 것"에 구체적으로 적는다.
- `dotnet`이 없는 PC: `docs/SETUP.md` 4절.
- AI 플레이테스트(Autopilot): `docs/AUTOPILOT.md`. 테스트 케이스·이력: `docs/playtest/TEST-CASES.md` (실행할 때마다 갱신).

## 작업 흐름
- 스펙(`docs/specs/`) → 구현 + 테스트 → 사용자 에디터 확인 → 관문 판단 → 다음 단계.
- 브랜치: `main`(안정) ← 기능 브랜치 PR. 커밋할 때마다 push하고, 큰 작업은 `docs/dev-notes/`에 인계 메모.
