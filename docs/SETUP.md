# 개발 환경 준비 (사용자 PC, Windows)

1단계(조작감 시제품)를 에디터에서 직접 해 보려면 아래를 한 번 해요. 30~60분 정도 걸려요(다운로드 포함).

## 1. 설치
1. **Git + Git LFS**: [git-scm.com](https://git-scm.com) 설치 → 터미널에서 `git lfs install` 한 번.
2. **Unity Hub**: [unity.com/download](https://unity.com/download) → 로그인 → **Personal 라이선스** 활성화(최근 12개월 매출·투자 $200,000 미만이면 무료).
3. Unity Hub → Installs → **Unity 6.3 LTS (6000.3.x 최신)** 설치. 모듈:
   - **Windows Build Support (IL2CPP)** (스팀 빌드용)
   - Microsoft Visual Studio Community (코드 편집기 — 없으면 같이)
4. (선택) **.NET 8 SDK** — 순수 로직 테스트(`dotnet test`)를 직접 돌려 보고 싶을 때만. 평소에는 Claude가 클라우드에서 돌려요.

## 2. 프로젝트 처음 열기 (한 번만)
저장소에는 우리 코드(`Assets/SushiRun/`)만 있고, Unity가 만드는 설정 파일은 아직 없어요. 공식 템플릿에서 가져와 한 번 합쳐요.

1. 저장소 받기: `git clone https://github.com/JangRuBin2/sushi-run-steam.git`
2. Unity Hub → **New project** → 템플릿 **Universal 3D** (6000.3) → 이름 `sushi-run-template`(아무 폴더) → 만들기 → 에디터가 열리면 **바로 닫기**.
3. 만들어진 `sushi-run-template` 폴더에서 아래 3개를 저장소 폴더(`sushi-run-steam`)로 **복사**:
   - `Packages/` 폴더 통째로
   - `ProjectSettings/` 폴더 통째로
   - `Assets/Settings/` 폴더 + `Assets/Settings.meta` (URP 설정 파일들)
4. Unity Hub → **Add → Add project from disk** → `sushi-run-steam` 폴더 → 열기 (처음엔 몇 분 걸려요).
5. 열리면 상단 메뉴 **SushiRun → Create Feel Prototype Scene** → `Assets/SushiRun/Scenes/FeelPrototype.unity`가 만들어지고 열려요.
6. ▶ **Play**.
7. 복사한 설정 파일들을 커밋해 주세요(또는 Claude에게 "설정 파일 올렸어"라고 알려 주세요):
   ```bash
   git add Packages ProjectSettings Assets/Settings Assets/Settings.meta Assets/SushiRun
   git commit -m "Add Unity project settings from Universal 3D template"
   git push
   ```
   `Assets/SushiRun/` 안에 Unity가 만든 `.meta` 파일도 같이 올라가야 해요(지우면 연결이 끊겨요).

> 에디터가 "Input System 패키지를 켜고 다시 시작할까요?"라고 물으면 **Yes**.
> 콘솔(Console 창)에 빨간 오류가 있으면 **오류 줄을 그대로 복사해서** Claude에게 보내 주세요. 클라우드에서는 Unity를 돌릴 수 없어서 이 단계는 사용자 확인이 필요해요.

## 3. 조작 (시제품)
| 동작 | 키보드·마우스 | 게임패드 |
|---|---|---|
| 이동 | WASD / 방향키 | 왼쪽 스틱 |
| 카메라 | 마우스 | 오른쪽 스틱 |
| 점프 | Space | A (아래 버튼) |
| 다이브 | Left Shift 또는 E | X (왼쪽 버튼) |
| 공격 | F | LT 또는 Y |
| 맵 바꾸기 | 1 = 회전 벨트(Race), 2 = 회전 꼬치(Final) | — |
| 체크포인트로 | R | — |
| 튜닝 패널 | Tab (슬라이더로 실행 중 바로 바뀜, 콘솔 출력·프로필 저장 버튼) | — |
| 도움말 | H | — |
| 마우스 풀기 / 잠그기 | Esc / 게임 화면 클릭 | — |

## 4. 순수 로직 테스트 (선택)
```bash
dotnet test tests/SushiRun.Core.Tests
```
마지막 줄 `Passed! - Failed: 0`을 확인해요.
