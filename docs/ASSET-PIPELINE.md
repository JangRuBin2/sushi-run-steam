# 에셋·확인 작업 방식

2026-11-27 결정 (사용자): 에셋은 **Codex로 뽑는 것이 기본**이다. 코드로 도형을 조립한 아트는 회색 박스 단계에서만 쓴다.
봇을 사람처럼 플레이시키는 테스트(Autopilot)는 **폐기**했다(토큰 소모 대비 효과가 낮음). 지난 기록은 [`playtest/`](playtest/)에 남아 있다.

## 1. Codex로 그림 뽑기

Codex CLI(`codex exec`, ChatGPT 로그인)의 이미지 생성 기능을 쓴다. 비용은 사용자 ChatGPT 사용량에서 나가고 Claude 토큰은 거의 쓰지 않는다.

```bash
cd <저장할 폴더>
codex exec --skip-git-repo-check -s workspace-write "<프롬프트: 그림마다 파일 이름 지정>" -i <참고 그림> < /dev/null
```

- **`-i`(참고 그림)는 프롬프트 뒤에 둔다.** 앞에 두면 `-i`가 프롬프트까지 그림 목록으로 삼켜서 아무것도 안 만든다.
- 한 번 실행에 여러 장을 부탁하면 된다(6장에 약 3분).
- 공통 스타일 문구: `bright, clean, stylized premium 3D toy render look (like a cute mobile party game), soft even lighting, no text, no logos`.
- 텍스처는 `seamless tileable texture, square`를 붙인다.
- 원본은 저장소 밖 `C:\hobby\_assets\<주제>\`에 둔다(1장에 1~3 MB PNG).

## 2. 맵 텍스처를 게임에 넣기

- 원본 PNG를 줄여서(텍스처 1024, 배경 그림 1920 폭) **JPG 품질 90**으로 `Assets/SushiRun/Resources/<맵>/`에 넣는다.
  예: `Resources/Sink/sponge_yellow.jpg`. JPG는 이 저장소 설정상 Git LFS로 올라간다.
- 코드에서는 `SinkKit.TexMat("sponge_yellow", 반복수, 색, 광택, 금속)`처럼 쓴다.
  - 반복수는 1 m당 반복 횟수다. `MeshKit.RoundedBox`의 UV는 미터 단위다.
  - 텍스처가 없으면 단색 재질로 대신한다.
- 배경 그림은 빛을 안 받는 재질(URP Unlit)을 큰 사각형에 붙인다(`SinkKit.SinkBasin`).

## 3. 캐릭터: Codex 그림 → 3D

1. Codex에게 콘셉트 시트(`docs/reference/concept-*.png`)를 `-i`로 주고 3D 변환용 그림을 뽑게 한다.
   - 조건: 흰 배경, 바닥 그림자 없음, 효과·글자 없음.
   - 뽑을 그림: 정면·3/4·옆·뒤, **밥 몸통만**, **토핑만**.
   - 고등어 초밥 그림: `C:\hobby\_assets\mackerel\`
2. 이미지→3D 서비스(Meshy 등)는 **사용자가** 돌린다(가입·크레딧).
   - 몸통과 토핑을 **따로** 변환해야 토핑이 몸보다 늦게 따라오는 쫀득 모션을 살릴 수 있다.
   - PBR, 텍스처 4096, GLB/FBX로 받는다.
   - Hunyuan3D는 라이선스상 한국이 제외되므로 쓰지 않는다.
3. 받은 모델은 Claude가 Unity에 넣는다. 키 약 0.95 m, 발바닥이 원점, 앞이 +Z가 되게 맞추고, `JellyBody`·토핑 지연을 연결한다.

- 모델이 오기 전 임시 캐릭터(`SushiToyBuilder`)는 토핑·김에 Codex 텍스처(`Resources/Characters/`, 512 JPG)를 입힌다. 밥 몸통은 텍스처를 입히면 지저분해 보여 단색으로 둔다.
- 캐릭터 확인 사진: `-executeMethod SushiRun.Editor.CharShot.Lineup` (초밥 6종 나란히 2장).

## 4. 확인: Unity를 창 없이 실행해 스크린샷

열려 있는 에디터나 사용자 화면에 창을 띄우지 않는다. **작업용 worktree**(예: `C:\hobby\_dev-sushi-run-steam`)에서 배치 모드로 돌린다.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe" -batchmode \
  -projectPath "C:/hobby/_dev-sushi-run-steam" \
  -executeMethod SushiRun.Editor.MapShot.Sink -shotDir <폴더> -logFile <로그>
```

- 로그에 `error CS`가 있으면 컴파일 오류다. `MAPSHOT_DONE`이 찍히면 사진이 나온 것이다.
- 맵마다 메서드가 있다: `MapShot.Sink`(설거지통), `MapShot.Space`(우주), `MapShot.Cooker`(밥솥), `MapShot.Garden`(찻잎 정원), `MapShot.FishMarket`(수산시장), `MapShot.Onsen`(온천탕). 각 3~5장(`Assets/SushiRun/Editor/MapShot.cs`). 새 맵을 만들면 메서드를 하나 더 만든다.
- 다른 맵 텍스처는 `Resources/<맵>/`에 넣고 `SinkKit.TexMat("Space/kaiten_belt", ...)`처럼 폴더째 부른다.
- worktree를 처음 열 때는 에셋 가져오기에 몇 분 걸린다. 두 번째부터는 1분 안팎이다.
- 새 `.cs` 파일의 `.meta`는 GUID 32자리를 꼭 넣는다. 비어 있으면 Unity가 그 파일을 무시해서 "class could not be found"가 난다.
