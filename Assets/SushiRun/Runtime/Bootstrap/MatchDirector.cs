// 한 판 흐름 (시제품, 혼자 + 봇): M 키 → 레이스 → 생존 → 결승. 통과 인원은 RoundRules (로블록스판과 같은 답).
//   레이스: 먼저 완주한 통과 인원만 다음 라운드 / 생존: 시간이 끝날 때까지 안 떨어진 캐릭터 (통과 인원 이하로 줄면 바로 끝)
//   결승: 마지막 한 명이 우승. 플레이어가 떨어지면 등수를 보여 주고 판 끝 (M으로 새 판).
// 라운드마다 맵은 종류 안에서 무작위 (레이스 3종 · 생존 2종 · 결승 3종). 숫자 키로 맵을 바꾸면 한 판은 멈춰요.
using System.Collections.Generic;
using SushiRun.Core.Rules;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class MatchDirector : MonoBehaviour
    {
        private enum State
        {
            Off,
            Playing,
            Between,
            Over,
        }

        private static readonly FeelPrototype.LevelKind[] Races = { FeelPrototype.LevelKind.RotatingBelt, FeelPrototype.LevelKind.SinkRace, FeelPrototype.LevelKind.SpaceRace };
        private static readonly FeelPrototype.LevelKind[] Survivals = { FeelPrototype.LevelKind.RiceCooker, FeelPrototype.LevelKind.FishMarket };
        private static readonly FeelPrototype.LevelKind[] Finals = { FeelPrototype.LevelKind.SkewerShowdown, FeelPrototype.LevelKind.TeaGarden, FeelPrototype.LevelKind.Onsen };

        public FeelPrototype Game;

        [Tooltip("라운드 사이 쉬는 시간 (초)")]
        public float BetweenSeconds = 4f;

        [Tooltip("레이스 라운드 시간 제한 (초) — 통과 인원이 다 못 들어와도 여기서 끝")]
        public float RaceTimeLimit = 150f;

        private float _roundStart;

        private State _state = State.Off;
        private int _round;
        private int _roundCount;
        private int _total;
        private int _qualify;
        private bool _isRace;
        private bool _isFinal;
        private readonly List<int> _bots = new List<int>();
        private readonly List<SushiCharacter> _passed = new List<SushiCharacter>();
        private PrototypeLevel _level;
        private SushiCharacter _lastOut;
        private float _stateUntil;
        private string _banner = "";
        private float _bannerUntil;
        private GUIStyle _big;
        private GUIStyle _box;

        public bool Active => _state == State.Playing || _state == State.Between;

        public void StartMatch()
        {
            if (Game == null)
            {
                return;
            }
            _bots.Clear();
            for (int i = 0; i < Game.BotSlots; i++)
            {
                _bots.Add(i);
            }
            _total = _bots.Count + 1;
            _roundCount = RoundRules.RoundCount(_total);
            _round = 1;
            StartRound();
        }

        private void StartRound()
        {
            int alive = _bots.Count + 1;
            _round = RoundRules.RoundToPlay(_round, _roundCount, alive);
            _isFinal = _round >= _roundCount;
            _qualify = RoundRules.QualifyCount(alive, _roundCount, _round, _total);
            FeelPrototype.LevelKind kind;
            if (_isFinal)
            {
                kind = Finals[Random.Range(0, Finals.Length)];
            }
            else if (_round % 2 == 1)
            {
                kind = Races[Random.Range(0, Races.Length)];
            }
            else
            {
                kind = Survivals[Random.Range(0, Survivals.Length)];
            }
            _isRace = System.Array.IndexOf(Races, kind) >= 0;

            Unhook();
            Game.BuildRound(kind, _bots);
            _level = Game.CurrentLevel;
            _level.MatchFinished += OnFinished;
            _level.MatchEliminated += OnEliminated;
            _passed.Clear();
            _lastOut = null;
            _roundStart = Time.time;
            _state = State.Playing;
            string what = _isFinal ? "결승 — 마지막 1명이 우승"
                : _isRace ? string.Format("레이스 — 먼저 완주한 {0}명 통과", _qualify)
                : string.Format("생존 — 끝까지 버티면 통과 (최대 {0}명)", _qualify);
            Banner(string.Format("라운드 {0}/{1}\n{2}", _round, _roundCount, what), 3.5f);
        }

        private void Unhook()
        {
            if (_level != null)
            {
                _level.MatchFinished -= OnFinished;
                _level.MatchEliminated -= OnEliminated;
            }
            _level = null;
        }

        private void OnFinished(SushiCharacter ch)
        {
            if (_state != State.Playing)
            {
                return;
            }
            _passed.Add(ch);
            if (ch == Game.PlayerCharacter)
            {
                Banner(string.Format("통과! {0}등으로 완주", _passed.Count), 2.5f);
            }
            if (_passed.Count >= _qualify)
            {
                EndRound();
            }
        }

        private void OnEliminated(SushiCharacter ch)
        {
            if (_state != State.Playing)
            {
                return;
            }
            _lastOut = ch;
            if (ch == Game.PlayerCharacter)
            {
                PlayerOut(_level.InCount + 1); // 남은 사람 수 + 1 등 (먼저 떨어질수록 큰 등수)
                return;
            }
            int left = _level.InCount;
            if ((_isFinal && left <= 1) || (!_isFinal && !_isRace && left <= _qualify))
            {
                EndRound();
            }
        }

        private void Update()
        {
            if (_state == State.Off || _state == State.Over)
            {
                return;
            }
            if (_state == State.Playing)
            {
                if (Game == null || Game.CurrentLevel != _level)
                {
                    Unhook(); // 숫자 키로 다른 맵을 골랐어요 → 한 판 멈춤
                    _state = State.Off;
                    return;
                }
                float left = _level.RoundTimeLeft;
                if (!_isRace && !_isFinal && left >= 0f && left <= 0.001f)
                {
                    EndRound();
                }
                else if (_isRace && Time.time - _roundStart > RaceTimeLimit)
                {
                    EndRound(); // 다 못 들어와도 시간이 다 되면 들어온 만큼만 통과
                }
            }
            else if (_state == State.Between && Time.time >= _stateUntil)
            {
                _round++;
                StartRound();
            }
        }

        private void EndRound()
        {
            if (_state != State.Playing)
            {
                return;
            }
            SushiCharacter player = Game.PlayerCharacter;
            List<SushiCharacter> through = _isRace ? new List<SushiCharacter>(_passed) : _level.InCharacters();
            if (_isFinal)
            {
                SushiCharacter winner = through.Count > 0 ? through[0] : _lastOut;
                if (winner == player)
                {
                    Finish(string.Format("우승!\n{0}명 중 1등 — M: 새 판", _total));
                }
                else
                {
                    Finish(string.Format("{0} 우승\n당신은 2등 — M: 새 판", winner != null ? winner.DisplayName : "?"));
                }
                return;
            }
            if (!through.Contains(player))
            {
                PlayerOut(through.Count + 1);
                return;
            }
            _bots.Clear();
            foreach (SushiCharacter ch in through)
            {
                int idx = Game.BotIndexOf(ch);
                if (idx >= 0)
                {
                    _bots.Add(idx);
                }
            }
            _state = State.Between;
            _stateUntil = Time.time + BetweenSeconds;
            Banner(string.Format("라운드 {0} 통과!\n{1}명이 다음 라운드로", _round, _bots.Count + 1), BetweenSeconds);
        }

        private void PlayerOut(int place)
        {
            Finish(string.Format("탈락 — {0}명 중 {1}등\nM: 새 판", _total, place));
        }

        private void Finish(string text)
        {
            _state = State.Over;
            Banner(text, 99f);
        }

        private void Banner(string text, float seconds)
        {
            _banner = text;
            _bannerUntil = Time.time + seconds;
        }

        private void OnGUI()
        {
            if (_state == State.Off)
            {
                return;
            }
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _big.normal.textColor = Color.white;
                _box = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            }
            string info = _state == State.Over ? "한 판 끝"
                : string.Format("한 판 · 라운드 {0}/{1} · 통과 {2}명 · 남은 {3}명", _round, _roundCount, _qualify, _level != null ? _level.InCount : 0);
            GUI.Box(new Rect(Screen.width * 0.5f - 230f, 8f, 460f, 30f), info, _box);
            if (Time.time < _bannerUntil && !string.IsNullOrEmpty(_banner))
            {
                var r = new Rect(0f, Screen.height * 0.22f, Screen.width, 120f);
                Color c = _big.normal.textColor;
                _big.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), _banner, _big);
                _big.normal.textColor = c;
                GUI.Label(r, _banner, _big);
            }
        }

        private void OnDestroy()
        {
            Unhook();
        }
    }
}
