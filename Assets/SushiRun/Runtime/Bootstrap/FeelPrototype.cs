// 조작감 시제품 시작점 — 씬에 이 컴포넌트 하나만 두면 Start에서 전부 코드로 만들어요
// (맵·플레이어·봇·카메라·조명). 1/2 키로 맵을 다시 만들고, Tab으로 튜닝 패널(실행 중 슬라이더).
// 화면 글씨는 시제품용 OnGUI예요 (진짜 UI는 나중 단계).
using System.Collections.Generic;
using SushiRun.Core.Combat;
using SushiRun.Core.Motion;
using SushiRun.Core.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiRun.Runtime
{
    public sealed class FeelPrototype : MonoBehaviour
    {
        public enum LevelKind
        {
            RotatingBelt,
            SkewerShowdown,
            SinkRace,
            SpaceRace,
            RiceCooker,
            TeaGarden,
            FishMarket,
            Onsen,
        }

        public LevelKind StartLevel = LevelKind.RotatingBelt;

        [Tooltip("있으면 이 에셋의 값을 쓰고, 슬라이더로 바꾼 값도 여기에 들어가요 (프로필 저장 버튼으로 디스크에)")]
        public TuningProfile Profile;

        [Range(0, 6)]
        public int BotCount = 5;
        [Range(0f, 1f)] public float BotSkillBest = 0.9f;
        [Range(0f, 1f)] public float BotSkillWorst = 0.55f;
        [Tooltip("방해꾼 봇 수 (마지막 봇부터). 실력 단계는 그대로")]
        public int TroublemakerBots = 1;
        public float TroublemakerAttackChance = 1.2f;

        public bool ShowHelp = true;

        // 프로필이 없을 때 쓰는 값
        private MoveTuning _localMove;
        private JellyTuning _localJelly;
        private AttackConfig _localAttack;
        private float _localGravity = 2.2f;

        private GameObject _runRoot;
        private PrototypeLevel _level;
        private SushiCharacter _player;
        private AttackAbility _playerAttack;
        private PlayerInputReader _input;
        private FollowCamera _cam;
        private Light _sun;
        private readonly List<SushiCharacter> _bots = new List<SushiCharacter>();
        private readonly List<int> _botIndex = new List<int>(); // _bots[k]가 몇 번 봇인지 (한 판 흐름에서 같은 봇을 이어 가요)

        public PrototypeLevel CurrentLevel => _level;
        public SushiCharacter PlayerCharacter => _player;
        public int BotSlots => Mathf.Clamp(BotCount, 0, 6);

        /// <summary>한 판 흐름(MatchDirector)이 라운드 맵을 만들 때: 남은 봇만, 떨어지면 탈락·완주하면 빠지는 모드로.</summary>
        public void BuildRound(LevelKind kind, ICollection<int> bots)
        {
            Build(kind, bots, true);
        }

        /// <summary>자유 연습 (숫자 키와 같음).</summary>
        public void BuildFree(LevelKind kind)
        {
            Build(kind);
        }

        /// <summary>이 캐릭터가 몇 번 봇인지 (플레이어·모르는 캐릭터는 -1).</summary>
        public int BotIndexOf(SushiCharacter ch)
        {
            int k = _bots.IndexOf(ch);
            return k >= 0 ? _botIndex[k] : -1;
        }

        private bool _showTuning;
        private float _fps = 60f;
        private Vector2 _scroll;
        private GUIStyle _label;
        private GUIStyle _big;
        private GUIStyle _bigShadow;
        private GUIStyle _small;

        private MoveTuning Move => Profile != null ? Profile.Move : _localMove;
        private JellyTuning Jelly => Profile != null ? Profile.Jelly : _localJelly;
        private AttackConfig Attack => Profile != null ? Profile.Attack : _localAttack;

        private float Gravity
        {
            get => Profile != null ? Profile.GravityScale : _localGravity;
            set
            {
                if (Profile != null)
                {
                    Profile.GravityScale = value;
                }
                else
                {
                    _localGravity = value;
                }
            }
        }

        private void Awake()
        {
            _localMove = new MoveTuning();
            _localJelly = new JellyTuning();
            _localAttack = new AttackConfig();
            if (Profile != null)
            {
                // 예전 에셋에 빈 칸이 있으면 기본값으로
                if (Profile.Move == null)
                {
                    Profile.Move = new MoveTuning();
                }
                if (Profile.Jelly == null)
                {
                    Profile.Jelly = new JellyTuning();
                }
                if (Profile.Attack == null)
                {
                    Profile.Attack = new AttackConfig();
                }
            }
        }

        private void Start()
        {
            Time.fixedDeltaTime = 1f / 60f; // 물리 60Hz (보간으로 화면은 부드럽게)
            _sun = GreyboxKit.SetupLighting(_sun);
            _sun.transform.SetParent(transform, true);
            CreateCamera();
            Build(StartLevel);
        }

        private void CreateCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GreyboxKit.Sky;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 800f;
            cam.fieldOfView = 60f;
            _cam = cam.GetComponent<FollowCamera>();
            if (_cam == null)
            {
                _cam = cam.gameObject.AddComponent<FollowCamera>();
            }
        }

        private void Clear()
        {
            if (_runRoot != null)
            {
                _runRoot.SetActive(false); // OnDisable이 바로 불려 목록(SushiCharacter.All 등)에서 빠져요
                Destroy(_runRoot);
            }
            _runRoot = null;
            _level = null;
            _player = null;
            _playerAttack = null;
            _input = null;
            _bots.Clear();
            _botIndex.Clear();
            AttackAbility.Book = new AttackBook();
            if (_cam != null)
            {
                _cam.LookInput = null;
                _cam.SetTarget(null);
            }
        }

        private void Build(LevelKind kind, ICollection<int> onlyBots = null, bool matchMode = false)
        {
            Clear();
            _runRoot = new GameObject("Run_" + kind);

            var levelGo = new GameObject(kind.ToString());
            levelGo.transform.SetParent(_runRoot.transform, false);
            if (kind == LevelKind.RotatingBelt)
            {
                _level = levelGo.AddComponent<RotatingBeltLevel>();
            }
            else if (kind == LevelKind.SinkRace)
            {
                _level = levelGo.AddComponent<SinkRaceLevel>();
            }
            else if (kind == LevelKind.SpaceRace)
            {
                _level = levelGo.AddComponent<SpaceRaceLevel>();
            }
            else if (kind == LevelKind.RiceCooker)
            {
                _level = levelGo.AddComponent<RiceCookerLevel>();
            }
            else if (kind == LevelKind.TeaGarden)
            {
                _level = levelGo.AddComponent<TeaGardenLevel>();
            }
            else if (kind == LevelKind.FishMarket)
            {
                _level = levelGo.AddComponent<FishMarketLevel>();
            }
            else if (kind == LevelKind.Onsen)
            {
                _level = levelGo.AddComponent<OnsenLevel>();
            }
            else
            {
                _level = levelGo.AddComponent<SkewerShowdownLevel>();
            }
            _level.MatchMode = matchMode;
            _level.Build();

            // 플레이어 (계란초밥) — 튜닝 객체를 그대로 써서 슬라이더가 바로 반영
            _player = CreateCharacter("Player", SushiPalette.Tamago, Move, Jelly);
            _playerAttack = _player.GetComponent<AttackAbility>();
            _input = _player.gameObject.AddComponent<PlayerInputReader>();
            _input.View = _cam;
            _player.Input = _input;
            _level.Player = _player;
            _level.Register(_player, 1);

            // 봇 — 튜닝은 복사본 (슬라이더는 플레이어만)
            SushiPalette[] palettes = SushiPalette.BotPalettes;
            int botCount = Mathf.Clamp(BotCount, 0, 6);
            for (int i = 0; i < botCount; i++)
            {
                if (onlyBots != null && !onlyBots.Contains(i))
                {
                    continue; // 한 판 흐름: 앞 라운드에서 떨어진 봇은 안 나와요
                }
                SushiPalette p = palettes[i % palettes.Length];
                SushiCharacter bot = CreateCharacter("Bot" + (i + 1) + "_" + p.Name, p, Copy(Move), Copy(Jelly));
                var brain = bot.gameObject.AddComponent<WanderBot>();
                brain.Level = _level;
                // 실력은 고르게 나눠요 (Bot1 가장 잘함 → 마지막 봇 가장 못함): 측정할 때 봇마다 실력이 같게
                brain.Skill = botCount > 1 ? Mathf.Lerp(BotSkillBest, BotSkillWorst, i / (float)(botCount - 1)) : BotSkillBest;
                if (i >= botCount - Mathf.Clamp(TroublemakerBots, 0, botCount))
                {
                    brain.Troublemaker = true;
                    brain.AttackChancePerSecond = TroublemakerAttackChance;
                    bot.name += "_방해꾼";
                }
                bot.Input = brain;
                _level.Register(bot, i == 0 ? 0 : i + 1);
                _bots.Add(bot);
                _botIndex.Add(i);
            }

            if (_cam != null)
            {
                _cam.LookInput = _input;
                _cam.SetTarget(_player);
            }
        }

        private SushiCharacter CreateCharacter(string name, SushiPalette palette, MoveTuning move, JellyTuning jelly)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_runRoot.transform, false);
            var ch = go.AddComponent<SushiCharacter>();
            ch.Tuning = move;
            ch.GravityScale = Gravity;
            ch.DisplayName = palette.Name;

            Transform topping;
            Transform visual = SushiToyBuilder.Build(go.transform, palette, out topping);
            ch.Visual = visual;

            var attack = go.AddComponent<AttackAbility>();
            attack.Config = Attack;

            var body = go.AddComponent<JellyBody>(); // AttackAbility 다음에 (Bind가 공격 이벤트도 구독)
            body.Tuning = jelly;
            body.Bind(ch, visual, topping);
            return ch;
        }

        private static T Copy<T>(T source) where T : class, new()
        {
            if (source == null)
            {
                return new T();
            }
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
        }

        private void Update()
        {
            if (Time.unscaledDeltaTime > 0f)
            {
                _fps = Mathf.Lerp(_fps, 1f / Time.unscaledDeltaTime, 0.05f);
            }

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                {
                    Build(LevelKind.RotatingBelt);
                }
                else if (kb.digit2Key.wasPressedThisFrame)
                {
                    Build(LevelKind.SkewerShowdown);
                }
                else if (kb.digit3Key.wasPressedThisFrame)
                {
                    Build(LevelKind.SinkRace);
                }
                else if (kb.digit4Key.wasPressedThisFrame)
                {
                    Build(LevelKind.SpaceRace);
                }
                else if (kb.digit5Key.wasPressedThisFrame)
                {
                    Build(LevelKind.RiceCooker);
                }
                else if (kb.digit6Key.wasPressedThisFrame)
                {
                    Build(LevelKind.TeaGarden);
                }
                else if (kb.digit7Key.wasPressedThisFrame)
                {
                    Build(LevelKind.FishMarket);
                }
                else if (kb.digit8Key.wasPressedThisFrame)
                {
                    Build(LevelKind.Onsen);
                }
                else if (kb.mKey.wasPressedThisFrame)
                {
                    // 한 판 흐름: 레이스 → 생존 → 결승 (봇과 함께, 통과 인원은 RoundRules)
                    MatchDirector director = GetComponent<MatchDirector>();
                    if (director == null)
                    {
                        director = gameObject.AddComponent<MatchDirector>();
                    }
                    director.Game = this;
                    director.StartMatch();
                }
                if (kb.rKey.wasPressedThisFrame && _level != null && _player != null)
                {
                    _level.ForceRespawn(_player);
                }
                if (kb.tabKey.wasPressedThisFrame)
                {
                    ToggleTuning();
                }
                if (kb.hKey.wasPressedThisFrame)
                {
                    ShowHelp = !ShowHelp;
                }
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    UnlockCursor();
                }
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.selectButton.wasPressedThisFrame)
                {
                    ToggleTuning();
                }
                if (pad.startButton.wasPressedThisFrame && _level != null && _player != null)
                {
                    _level.ForceRespawn(_player);
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !_showTuning && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (_player != null)
            {
                _player.GravityScale = Gravity;
            }
            if (_input != null)
            {
                // 튜닝 패널이 열려 있으면 캐릭터 조작을 막아요 (맵을 다시 만들어도 매 프레임 맞춤)
                _input.InputEnabled = !_showTuning;
            }
        }

        private void ToggleTuning()
        {
            _showTuning = !_showTuning;
            if (_showTuning)
            {
                UnlockCursor();
                if (_input != null)
                {
                    _input.ClearPresses();
                }
            }
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        // ───────────── 화면 글씨 (시제품 OnGUI) ─────────────

        private void EnsureStyles()
        {
            if (_label != null)
            {
                return;
            }
            _label = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            _label.normal.textColor = Color.white;
            _small = new GUIStyle(_label) { fontSize = 13 };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 46, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _big.normal.textColor = new Color(1f, 0.97f, 0.88f);
            _bigShadow = new GUIStyle(_big);
            _bigShadow.normal.textColor = new Color(0.25f, 0.12f, 0.08f, 0.85f);
        }

        private static string ModeName(MotorMode mode)
        {
            switch (mode)
            {
                case MotorMode.Diving:
                    return "다이브";
                case MotorMode.Prone:
                    return "엎드림";
                case MotorMode.Down:
                    return "넘어짐";
                case MotorMode.GettingUp:
                    return "일어나는 중";
                default:
                    return "달리기";
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            // 왼쪽 위: 맵 상태 + 캐릭터 상태 + 조작법
            GUILayout.BeginArea(new Rect(12f, 12f, Mathf.Min(470f, w - 24f), h - 24f));
            GUILayout.BeginVertical(GUI.skin.box);
            if (_level != null)
            {
                GUILayout.Label(_level.StatusText(_player), _label);
            }
            if (_player != null)
            {
                Vector3 rv = _player.RelativeVelocity;
                float speed = new Vector2(rv.x, rv.z).magnitude;
                GUILayout.Label(string.Format("FPS {0:0} · {1} · {2} · {3:0.0} m/s",
                    _fps, ModeName(_player.Mode), _player.IsGrounded ? "땅" : "공중", speed), _small);
                if (_playerAttack != null)
                {
                    float cd = _playerAttack.CooldownRemaining;
                    GUILayout.Label(cd > 0f ? string.Format("공격 대기 {0:0.0}초", cd) : "공격 준비! (F)", _small);
                }
            }
            if (ShowHelp)
            {
                GUILayout.Label(
                    "이동 WASD/방향키 · 시점 마우스 (화면 클릭 = 잠금, Esc = 풀기)\n" +
                    "점프 Space · 다이브 Shift/E · 공격 F\n" +
                    "R 체크포인트로 · 1 회전 벨트 · 2 꼬치 쇼다운 · 3 설거지통 · 4 우주 · 5 밥솥 · 6 찻잎 정원 · 7 수산시장 · 8 온천탕 · M 한 판(라운드 3개) · Tab 튜닝 · H 도움말 끄기 · F12 스크린샷\n" +
                    "패드: 왼스틱 이동 · A 점프 · X 다이브 · LT/Y 공격 · 오른스틱 시점 · Select 튜닝 · Start 체크포인트",
                    _small);
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 가운데 큰 글씨 (떨어짐·탈락·완주)
            if (_level != null)
            {
                string center = _level.CenterText(_player);
                if (!string.IsNullOrEmpty(center))
                {
                    var rect = new Rect(0f, h * 0.28f, w, 200f);
                    GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), center, _bigShadow);
                    GUI.Label(rect, center, _big);
                }
            }

            if (_showTuning)
            {
                DrawTuning(w, h);
            }
            else
            {
                GUI.Label(new Rect(w - 160f, h - 34f, 150f, 24f), "Tab: 튜닝 패널", _small);
            }
        }

        private void DrawTuning(float w, float h)
        {
            float pw = Mathf.Min(370f, w - 24f);
            GUILayout.BeginArea(new Rect(w - pw - 12f, 12f, pw, h - 24f), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            JellyTuning j = Jelly;
            MoveTuning m = Move;
            GUILayout.Label("<b>튜닝</b> — 플레이어에 바로 반영 (봇은 시작할 때 값)", _label);
            GUILayout.Label(Profile != null ? "프로필: " + Profile.name : "프로필 없음 (값이 저장되지 않아요)", _small);

            GUILayout.Space(6f);
            GUILayout.Label("<b>쫀득 (JellyTuning)</b>", _label);
            j.SquashFrequency = Slider("SquashFrequency", j.SquashFrequency, 1f, 8f);
            j.SquashDamping = Slider("SquashDamping (낮을수록 출렁)", j.SquashDamping, 0.05f, 1f);
            j.LandKickPerSpeed = Slider("LandKickPerSpeed (착지 눌림)", j.LandKickPerSpeed, 0f, 2f);
            j.LeanPerAccel = Slider("LeanPerAccel (기울기)", j.LeanPerAccel, 0f, 4f);
            j.ToppingDamping = Slider("ToppingDamping (토핑 출렁)", j.ToppingDamping, 0.05f, 1f);

            GUILayout.Space(6f);
            GUILayout.Label("<b>이동 (MoveTuning)</b>", _label);
            m.RunSpeed = Slider("RunSpeed", m.RunSpeed, 3f, 12f);
            m.JumpSpeed = Slider("JumpSpeed", m.JumpSpeed, 4f, 12f);
            m.DiveForwardSpeed = Slider("DiveForwardSpeed", m.DiveForwardSpeed, 4f, 16f);
            Gravity = Slider("GravityScale (중력 배율)", Gravity, 1f, 4f);

            GUILayout.Space(8f);
            if (GUILayout.Button("기본값으로"))
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new JellyTuning()), j);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new MoveTuning()), m);
                Gravity = 2.2f;
            }
            if (GUILayout.Button("지금 값을 콘솔에 출력"))
            {
                Debug.Log("[SushiRun] JellyTuning\n" + JsonUtility.ToJson(j, true));
                Debug.Log("[SushiRun] MoveTuning\n" + JsonUtility.ToJson(m, true) + "\nGravityScale " + Gravity);
            }
#if UNITY_EDITOR
            if (Profile != null)
            {
                if (GUILayout.Button("프로필 저장 (에셋 파일에 쓰기)"))
                {
                    UnityEditor.EditorUtility.SetDirty(Profile);
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(Profile);
                    Debug.Log("[SushiRun] 튜닝 프로필 저장: " + Profile.name);
                }
            }
            else
            {
                GUILayout.Label("메뉴 SushiRun → Create Tuning Profile로 에셋을 만들면 이 씬의 FeelPrototype에 연결돼요. 다시 Play하면 저장 버튼이 생겨요.", _small);
            }
#endif
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _small, GUILayout.Width(190f));
            float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(110f));
            GUILayout.Label(v.ToString("0.00"), _small, GUILayout.Width(44f));
            GUILayout.EndHorizontal();
            return v;
        }
    }
}
