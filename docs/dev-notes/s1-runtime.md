# 개발 메모 — s1 런타임 (조작감 시제품)

스펙: [`specs/s1-01-feel-prototype.md`](../specs/s1-01-feel-prototype.md). 2026-11-16.

## 구조 (`Assets/SushiRun/Runtime`, asmdef `SushiRun.Runtime` → `SushiRun.Core`, `Unity.InputSystem`)
| 폴더 | 파일 | 하는 일 |
|---|---|---|
| Input | `ISushiInput`, `PlayerInputReader` | 사람·봇 공용 입력. 사람은 Input System 장치를 직접 읽고(액션 에셋 없음) 눌림을 FixedUpdate가 꺼낼 때까지 쌓아 둠 |
| Player | `SushiCharacter` | Rigidbody 컨트롤러. 루트 = 발바닥, 캡슐 r 0.45 / h 0.95, 레이어 2(Ignore Raycast). `MoveLogic.Step` 결과를 `linearVelocity`·`MoveRotation`에 적용, 발판 속도(회전 접시·벨트)를 더함, 중력 배율 2.2 |
| Player | `JellyBody` | `JellyMotion` → 자식 Visual(발바닥 피벗)·Topping. 다이브·엎드림 80° 눕기, 넘어짐은 뒤로 벌렁 + 흔들 |
| Player | `SushiToyBuilder` | 기본 도형 계란초밥(임시). `SushiPalette`로 봇 토핑 색 |
| Player | `AttackAbility` (+ `Fx/AttackBurstFx`) | 모두의 공격 로컬판. `AttackBook`은 정적 하나 |
| Camera | `FollowCamera` | 궤도 + 축별 `Spring1D`, SphereCast로 벽 피함, 다이브 FOV 킥, 큰 착지·넘어짐 흔들림 |
| Level | `PrototypeLevel`, `RotatingBeltLevel`, `SkewerShowdownLevel`, `GreyboxKit`, `Rotator`, `ConveyorSurface`, `KnockHazard`, `Checkpoint`, `FinishLine`, `IPlatformMotion` | 맵 2개(코드 생성), 낙하 → 3초 뒤 부활 |
| Rendering | `ProtoMaterials` | URP Lit 재질 캐시·기본 도형 메시(콜라이더 없는 장식) |
| Bots | `WanderBot` | 길 따라가기·낭떠러지 점프·꼬치 타이밍 점프·가끔 다이브/공격 |
| Bootstrap | `FeelPrototype`, `TuningProfile` | 씬 하나에 이것만. 1/2 맵, R 체크포인트, Tab 튜닝, H 도움말 |
| Editor | `FeelPrototypeMenu` | `SushiRun/Create Feel Prototype Scene`, `SushiRun/Create Tuning Profile` |

## 정한 것 (스펙에 없던 세부)
- 캐릭터 id는 `GetInstanceID` 대신 자체 번호(`SushiCharacter.Id`) — Unity 6.x에서 InstanceID API가 바뀌는 중이라 피함.
- 회전체·벨트 위 실려 가기는 `IPlatformMotion`(Rotator·ConveyorSurface가 직접 계산). 키네마틱 Rigidbody의 `GetPointVelocity`는 그 밖의 경우 대비용.
- 장애물 넉백은 충돌 메시지 대신 `KnockHazard`가 매 물리 틱 `OverlapBox`로 찾음.
- 정적 상태(`SushiCharacter.All`, `AttackAbility.Book`, `KnockHazard.Active`, 재질 캐시)는 `SubsystemRegistration`에서 비움 — 도메인 리로드를 꺼도 안전.
- 물리 60Hz(`Time.fixedDeltaTime = 1/60`), 보간 켬.
- 회전 벨트 코스 약 125 m(출발 z 1 → 결승 z 121), 체크포인트 3개. 혼자 깔끔히 달리면 30초대로 예상 — 실측해서 40~60초(AC4)가 안 나오면 접시 속도·벨트 속도·간격으로 맞춘다.
- 결승 꼬치: 낮은 막대 중심 0.45 m(점프), 높은 막대 중심 1.62 m(서 있으면 통과, 점프하면 걸림, 다이브 꼭대기 1.28 m는 통과). 속도 40→140°/s, -30→-110°/s.

## 확인 방법
- Unity 없이: `dotnet test tests/SushiRun.Core.Tests` (Core만). 런타임은 Unity 밖에서 스텁으로 C# 문법·타입만 확인했다(실제 Unity API 일치는 에디터에서 처음 열 때 확인).
- 처음 열 때 볼 것: 콘솔 컴파일 오류, 분홍 재질(URP 아님), 한글 OnGUI 글씨가 네모로 나오는지.
