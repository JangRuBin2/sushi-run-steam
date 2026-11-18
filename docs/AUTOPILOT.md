# Autopilot 플레이테스트 자동화

[Autopilot](https://github.com/ramenvr/autopilot)(Apache-2.0)은 AI가 게임을 직접 플레이하며 확인하는 도구예요.
에디터 안에서 플레이 모드를 스스로 켜고, 키를 누르고, 화면을 보고, 끝나면 보고서와 **화면 녹화**를 남겨요.

- 프로젝트에는 Unity 패키지 `com.ramenvr.autopilot` **1.0.1**이 이미 들어 있어요
  (`Packages/com.ramenvr.autopilot-1.0.1.tgz`, `manifest.json`에서 `file:` 경로로 참조).
  릴리스 `v1.0.0-1ebf2a5`의 `Autopilot-Unity.zip`에서 꺼냈고, SHA256은 릴리스의 `SHA256SUMS.txt`와 같아요.
- 이 패키지는 **에디터와 Development 빌드에만** 들어가요. 릴리스(스팀) 빌드에는 포함되지 않아요.
- 우리 코드(`SushiRun.*` asmdef)는 이 패키지를 참조하지 않아요. 패키지를 빼도 컴파일돼요.

## 1. PC마다 한 번 (Mac 기준)

Unity 패키지는 저장소에 들어 있지만, AI가 돌아가는 **에이전트 서비스**와 **로그인**은 컴퓨터마다 해야 해요.

1. `git pull` → Unity Hub에서 `sushi-run-steam` 열기 (Unity 6000.3.26f1).
2. 메뉴 **Window > Autopilot** → 처음 설정 화면:
   1. **계정**: Aura 계정으로 **Sign in**(없으면 Create Account), 또는 **BYOK**(직접 API 키).
   2. **Agent service**: 설정 화면이 패키지 안의 `install.sh`로 자동 설치해요
      (uv와 Python 3.12, Bun을 `~/.local/bin` 등에 설치). *Agent service ready* 가 뜨면 **Continue**.
   3. **Privacy**: 실행 기록(녹화·스크린샷)을 Autopilot 개발사에 보낼지 고르고 **Finish**.
      보내고 싶지 않으면 끄세요.
3. **Claude Code에서 쓰려면** 터미널에서 한 번 실행:
   ```bash
   autopilot connect claude
   ```
   (`autopilot` 명령이 없다고 나오면 새 터미널을 열거나 `~/.local/bin`을 PATH에 넣어요.)
   그다음 Claude Code를 다시 열면 `autopilot` MCP 도구가 생겨요.

> 설정 파일·키 위치: `~/Library/Application Support/Autopilot/` (Windows는 `%APPDATA%\Autopilot\`).
> 저장소에 올라가지 않아요.

## 2. 실행하는 법

Unity 에디터에 이 프로젝트가 열려 있고 **Window > Autopilot** 창이 연결된 상태(*Connected*)여야 해요.
테스트할 씬(`Assets/SushiRun/Scenes/FeelPrototype.unity`)을 열어 두세요.

**Autopilot 창에서**: 입력란에 할 일을 쓰고 **Run**. 예:

| 종류 | 예시 |
|---|---|
| 확인(Verify) | "Space를 누르면 캐릭터가 점프하는지 확인해" |
| 목표(Goal) | "회전 벨트 맵에서 결승선까지 가 봐" |
| 탐색(Explore) | "현재 레벨을 돌아다니면서 이상해 보이는 걸 전부 알려줘" |

**Claude Code에서**: "Autopilot으로 ~ 확인해" 라고 하면 Claude가 `start_run`을 불러 실행해요.
Claude Code에서 시작한 실행도 에디터의 Autopilot 창에 실시간으로 보여요.

- `autopilot connect claude`를 해 둔 PC에서는 Claude Code 세션에 `autopilot` MCP 도구가 이미 있어요.
  Autopilot 창이 *Connected* 면 바로 실행돼요 (Git Bash PATH에 `autopilot` 명령이 없어도 상관없어요).
- 실행 중 `402 billing_error` (spending limit)가 나면 AI가 아무것도 고르지 못하고 에피소드가 멈춰요.
  Autopilot 창에서 계정 크레딧이나 BYOK 키를 확인해요.
- Autopilot이 쓰는 게임 메모(`notes.md`)와 실행 기록(`runs/`)은 Autopilot 쪽 게임 폴더에 있어요(저장소 밖).
- 지금까지 실행 결과는 [`playtest/`](playtest/)에 날짜별로, 테스트 케이스·통과 기준·이력은 [`playtest/TEST-CASES.md`](playtest/TEST-CASES.md)에 적어요.
- 측정 스크립트 원본은 `tools/autopilot/lib.py` (Autopilot 게임 폴더 `%LOCALAPPDATA%\Autopilot\memory\SushiRun\lib.py`에 복사해서 써요).

조작 키는 [SETUP.md 3절](SETUP.md#3-조작-시제품)과 같아요 (WASD 이동, Space 점프, Shift/E 다이브, F 공격, 1/2 맵 바꾸기).
Autopilot 창 **Settings > Design brief**에 게임 규칙을 적어 두면, 탐색 실행이 규칙과 다른 동작을 버그로 보고해요.

## 3. 게임 화면 캡처 (녹화·스크린샷)

실행마다 화면이 저장돼요. **에디터를 그래픽 있는 상태로** 실행해야 해요(`-nographics` 실행에서는 녹화가 안 돼요).

- 위치: `Logs/Playtest/<시각>/`
  - `result.json` — 판정(CONFIRMED / DENIED / INCONCLUSIVE 등)과 근거
  - `recording.mp4` — 실행 전체 녹화
- 녹화의 JPEG 프레임은 에이전트 서비스 쪽에 MP4와 함께 남아요(`result.json`의 `recording.frames`가 경로).
  Claude Code에서 실행하면 Claude가 이 프레임을 이미지로 읽어 화면을 확인할 수 있어요.
- Autopilot 창 **Result** 탭에서 바로 재생하고, 폴더 아이콘으로 실행 폴더를 열 수 있어요.
- `Logs/`는 `.gitignore`에 있어서 저장소에는 올라가지 않아요.

## 4. 창 없이 실행 (CI·스크립트)

에이전트 서비스를 먼저 띄우고(`autopilot serve`), 에디터를 배치 모드로 실행해요. **`-quit`는 붙이지 않아요.**

```bash
"/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity" -batchmode \
  -projectPath "$PWD" -executeMethod Autopilot.Playtest.Editor.Headless.Run \
  -Playtest=verify -PlaytestClaim="Pressing Space makes the sushi jump" \
  -PlaytestMap=Assets/SushiRun/Scenes/FeelPrototype.unity -logFile Logs/autopilot-headless.log
```

`-Playtest=verify|goal|explore`, `-PlaytestGoal=...`, `-PlaytestMinutes=N`, `-PlaytestReportDir=경로` 등은
[패키지 README](https://github.com/ramenvr/autopilot/blob/main/plugin/unity/com.ramenvr.autopilot/README.md#headless-and-ci)를 봐요.
녹화가 필요하면 `-nographics` 를 빼요.

## 5. 업데이트·제거

- 업데이트: 새 릴리스의 `Autopilot-Unity.zip`에서 tgz를 꺼내 `Packages/`의 옛 tgz와 바꾸고,
  `manifest.json`의 파일 이름을 고쳐요. 에이전트 서비스는 Autopilot 창 Settings에서 업데이트돼요.
- 제거: `manifest.json`에서 `com.ramenvr.autopilot` 줄과 `Packages/`의 tgz를 지워요.
  컴퓨터에서 지우려면 Autopilot 창 **Settings > Uninstall** (또는 `autopilot uninstall`).
