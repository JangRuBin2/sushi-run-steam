// Survival 맵 — "수산시장 생존" (콘셉트 맵 시트 4번). 수조 물 위 얼음 블록 5×5 위에서 45초 버티기.
//   얼음 블록 이동: 2·4번째 줄이 좌우로 한 칸씩 미끄러져요 (10초 왕복, 위에 타면 실려 가고 옆에 있으면 밀려요).
//   거대 생선 낙하: 8초마다 그림자 경고 → 참치가 철퍼덕 (옆으로 튕겨 냄) → 잠깐 누워 길을 막음.
// 물에 빠지면 "탈락" (시제품: 3초 뒤 다시). 45초가 지나면 "생존!" 뒤 판 다시 시작. 텍스처는 Resources/FishMarket/.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class FishMarketLevel : PrototypeLevel
    {
        public const int Grid = 5;
        public const float Cell = 3.2f;
        public const float RoundSeconds = 45f;
        private const float WaterY = -1.5f;

        private readonly List<Material> _owned = new List<Material>();
        private FishSlam _fish;
        private float _roundStart;
        private float _clearedUntil = -1f;
        private float _bestSurvive = -1f;
        private float _lastSurvive = -1f;
        private readonly List<Vector3> _spawns = new List<Vector3>();

        public override string Title => "수산시장 생존 (Survival)";

        public override bool FallEliminates => true;

        public override float RoundTimeLeft => Mathf.Max(0f, RoundSeconds - (Time.time - _roundStart));

        public override void Build()
        {
            KillY = -3f;
            BotEdgeJumps = false;
            WanderCenter = transform.position;
            WanderRadius = Cell * 2f;
            Transform root = transform;
            SinkKit.SetupLighting();
            SinkKit.BackdropBox(root, SinkKit.Backdrop("FishMarket/market_backdrop", _owned), new Vector3(0f, 12f, 0f), 120f);

            // 수조 물 + 나무 테두리
            Material water = ProtoMaterials.Transparent(new Color(0.30f, 0.62f, 0.85f, 0.65f));
            _owned.Add(water);
            SinkKit.Part(root, "Water", MeshKit.Disc(70f), new Vector3(0f, WaterY, 0f), Quaternion.identity, water, false);
            SinkKit.Part(root, "TankFloor", MeshKit.Disc(70f), new Vector3(0f, -10f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.20f, 0.36f, 0.50f), 0.3f));
            Material crate = SinkKit.TexMat("FishMarket/wood_crate", 0.3f, Color.white, 0.3f);
            float half = Grid * Cell * 0.5f + 5f;
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 90f, 0f);
                SinkKit.Part(root, "TankRim", MeshKit.RoundedBox(new Vector3(half * 2f + 2f, 1.2f, 1.4f), 0.4f), q * new Vector3(0f, WaterY + 0.3f, half), q, crate);
            }

            // 얼음 블록 5×5 (2·4번째 줄은 좌우로 미끄러짐)
            Material ice = SinkKit.TexMat("FishMarket/ice_block", 0.3f, new Color(0.92f, 0.97f, 1f), 0.9f);
            float origin = -(Grid - 1) * Cell * 0.5f;
            for (int row = 0; row < Grid; row++)
            {
                bool slides = row == 1 || row == 3;
                Transform parent = root;
                if (slides)
                {
                    var rowGo = new GameObject("IceRow" + row);
                    rowGo.transform.SetParent(root, false);
                    rowGo.transform.localPosition = new Vector3(0f, 0f, origin + row * Cell);
                    parent = rowGo.transform;
                }
                for (int col = 0; col < Grid; col++)
                {
                    float x = origin + col * Cell;
                    Vector3 top = slides ? new Vector3(x, 0f, 0f) : new Vector3(x, 0f, origin + row * Cell);
                    SinkKit.Slab(parent, "Ice", top, new Vector3(Cell - 0.2f, 1.6f, Cell - 0.2f), 0.25f, ice);
                    if (!slides && col >= 1 && col <= 3)
                    {
                        _spawns.Add(top + Vector3.up * 0.05f);
                    }
                }
                if (slides)
                {
                    var mover = parent.gameObject.AddComponent<SineMover>();
                    mover.Axis = Vector3.right;
                    mover.Amplitude = Cell;
                    mover.Period = 10f;
                    mover.Phase = row == 1 ? 0f : 0.5f;
                }
            }

            // 거대 생선 낙하
            var fishGo = new GameObject("FishSlam");
            fishGo.transform.SetParent(root, false);
            _fish = fishGo.AddComponent<FishSlam>();
            _fish.Center = transform.position;
            _fish.AreaHalf = new Vector2(Cell * 1.5f, Cell * 1.5f);
            _fish.FishMat = SinkKit.TexMat("FishMarket/tuna_skin", 0.6f, Color.white, 0.85f);
            _fish.BellyMat = SinkKit.Mat(new Color(0.86f, 0.88f, 0.92f), 0.85f);
            Material warn = ProtoMaterials.Transparent(new Color(0.05f, 0.1f, 0.2f, 0.45f));
            _owned.Add(warn);
            _fish.WarnMat = warn;

            Decor(root, crate);
            BotPath.Clear();
            _roundStart = Time.time;
        }

        /// <summary>떠 있는 생선 상자·부표, 가판대 천막 (장식).</summary>
        private void Decor(Transform root, Material crate)
        {
            var rnd = new System.Random(5);
            Material awningA = SinkKit.Mat(new Color(0.95f, 0.35f, 0.32f), 0.4f);
            Material awningB = SinkKit.Mat(new Color(0.98f, 0.96f, 0.92f), 0.4f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f + 8f;
                float r = 16f + (float)rnd.NextDouble() * 8f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, WaterY, r);
                SinkKit.Part(root, "FloatingCrate", MeshKit.RoundedBox(new Vector3(2.4f, 1.2f, 1.8f), 0.2f), p + Vector3.up * 0.3f, Quaternion.Euler(0f, a * 2f, 3f), crate);
                if (i % 3 == 0)
                {
                    ProtoMaterials.VisualPart(root, "Buoy", PrimitiveType.Sphere, p + new Vector3(2.5f, 0.3f, 0f), Vector3.one * 1.4f, Quaternion.identity, awningA);
                }
            }
            // 가판대 (수조 뒤 두 줄)
            for (int s = -1; s <= 1; s += 2)
            {
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p = new Vector3(-15f + k * 10f, 0f, s * 26f);
                    SinkKit.Part(root, "Stall", MeshKit.RoundedBox(new Vector3(8f, 3f, 4f), 0.3f), p + Vector3.up * 0.5f, Quaternion.identity, crate);
                    for (int st = 0; st < 4; st++)
                    {
                        SinkKit.Part(root, "Awning", MeshKit.RoundedBox(new Vector3(2f, 0.25f, 5f), 0.1f), p + new Vector3(-3f + st * 2f, 5f, 0f), Quaternion.Euler(s * 12f, 0f, 0f), st % 2 == 0 ? awningA : awningB);
                    }
                }
            }
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            yaw = 0f;
            if (_spawns.Count == 0)
            {
                return transform.position + Vector3.up * 0.05f;
            }
            return transform.TransformPoint(_spawns[index % _spawns.Count]);
        }

        protected override void Update()
        {
            base.Update();
            if (MatchMode)
            {
                return; // 한 판 흐름: 시간이 다 되면 MatchDirector가 라운드를 끝내요 (여기서 판을 다시 시작하지 않음)
            }
            float now = Time.time;
            if (_clearedUntil < 0f && now - _roundStart >= RoundSeconds)
            {
                _clearedUntil = now + 3f;
            }
            else if (_clearedUntil >= 0f && now >= _clearedUntil)
            {
                _clearedUntil = -1f;
                _roundStart = now;
                for (int i = 0; i < Runners.Count; i++)
                {
                    if (Runners[i].RespawnAt < 0f)
                    {
                        Runners[i].StartTime = now;
                    }
                }
                if (_fish != null)
                {
                    _fish.ResetCycle();
                }
            }
        }

        protected override void OnRegistered(Runner r)
        {
            r.StartTime = Time.time;
        }

        protected override void OnFell(Runner r)
        {
            float survived = Time.time - r.StartTime;
            if (r.Ch == Player)
            {
                _lastSurvive = survived;
                if (_bestSurvive < 0f || survived > _bestSurvive)
                {
                    _bestSurvive = survived;
                }
            }
            float yaw;
            r.RespawnPos = SpawnPoint(r.SpawnIndex, out yaw);
            r.RespawnYaw = yaw;
        }

        protected override void OnRespawned(Runner r)
        {
            r.StartTime = Time.time;
        }

        private int AliveCount()
        {
            int n = 0;
            for (int i = 0; i < Runners.Count; i++)
            {
                if (Runners[i].Ch != null && !Runners[i].Ch.IsDespawned)
                {
                    n++;
                }
            }
            return n;
        }

        public override string StatusText(SushiCharacter player)
        {
            Runner r = Find(player);
            float survive = r != null && r.RespawnAt < 0f ? Time.time - r.StartTime : 0f;
            float left = Mathf.Max(0f, RoundSeconds - (Time.time - _roundStart));
            string best = _bestSurvive >= 0f ? string.Format(" · 최고 {0:0.0}초", _bestSurvive) : "";
            return string.Format("{0}\n남은 시간 {1:00}초 · 버틴 시간 {2:0.0}초{3}\n얼음 위 {4}/{5}", Title, left, survive, best, AliveCount(), Runners.Count);
        }

        public override string CenterText(SushiCharacter player)
        {
            float c = RespawnCountdown(player);
            if (c >= 0f)
            {
                string last = _lastSurvive >= 0f ? string.Format("\n{0:0.0}초 버팀", _lastSurvive) : "";
                return "탈락!" + last + "\n" + Mathf.CeilToInt(c);
            }
            return _clearedUntil >= 0f ? "생존!" : "";
        }

        private void OnDestroy()
        {
            GreyboxKit.SetupLighting(RenderSettings.sun);
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Destroy(_owned[i]);
                }
            }
            _owned.Clear();
        }
    }
}
