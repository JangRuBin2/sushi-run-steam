// Final 맵 — "온천탕 인형뽑기 결승" (콘셉트 맵 시트 6번). 김이 나는 온천 위 나무 발판 7×7, 가운데 고양이 인형뽑기 기계.
//   온천수 상승: 12초마다 바깥 줄 발판부터 천천히 물에 잠겨요 (흔들림·김 경고 → 3초에 걸쳐 내려감). 가운데 3×3은 안 잠김.
//   중앙 인형뽑기 작동: 12초마다 집게가 누군가 위로 가서 쾅 (둘레 튕김 + 바닥 전체 들썩).
// 물에 닿으면 "탈락" (시제품: 3초 뒤 다시). 40초가 지나면 발판이 올라오고 판 다시 시작. 텍스처는 Resources/Onsen/.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class OnsenLevel : PrototypeLevel
    {
        public const int Grid = 7;
        public const float Cell = 2.6f;
        public const float SinkEvery = 12f;
        public const float RoundSeconds = 40f;
        public const float SinkSeconds = 3f;
        public const float SinkDepth = 2f;
        public const float WarnSeconds = 2f;
        private const float WaterY = -0.45f;
        private const int SinkingRings = 2; // 바깥 두 줄 (가운데 3×3은 남음)

        private sealed class Tile
        {
            public Transform T;
            public Vector3 Home;
            public int Ring; // 0 = 가운데
        }

        private readonly List<Material> _owned = new List<Material>();
        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Vector3> _spawns = new List<Vector3>();
        private ClawDrop _claw;
        private float _roundStart;
        private float _bestSurvive = -1f;
        private float _lastSurvive = -1f;

        public override string Title => "온천탕 인형뽑기 결승 (Final)";

        public override bool FallEliminates => true;

        public override void Build()
        {
            KillY = WaterY - 0.25f; // 온천물에 닿으면 탈락
            BotEdgeJumps = false;
            WanderCenter = transform.position;
            WanderRadius = Cell * 2.5f;
            Transform root = transform;
            SetupOnsenLighting();
            SinkKit.BackdropBox(root, SinkKit.Backdrop("Onsen/onsen_backdrop", _owned), new Vector3(0f, 12f, 0f), 110f);

            // 온천물 + 둘레 바위 + 김
            Material water = ProtoMaterials.Transparent(new Color(0.62f, 0.86f, 0.88f, 0.7f));
            _owned.Add(water);
            SinkKit.Part(root, "HotSpring", MeshKit.Disc(26f), new Vector3(0f, WaterY, 0f), Quaternion.identity, water, false);
            SinkKit.Part(root, "SpringBed", MeshKit.Disc(26f), new Vector3(0f, -4f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.45f, 0.42f, 0.40f), 0.3f));
            Material rock = SinkKit.Mat(new Color(0.50f, 0.48f, 0.46f), 0.35f);
            for (int i = 0; i < 28; i++)
            {
                float a = i * 360f / 28f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, WaterY, 14f + (i % 3) * 0.8f);
                ProtoMaterials.VisualPart(root, "Rock", PrimitiveType.Sphere, p, new Vector3(3f + i % 2, 1.6f + (i % 3) * 0.5f, 2.6f), Quaternion.Euler(0f, a, 0f), rock);
            }
            Material steam = ProtoMaterials.Transparent(new Color(1f, 1f, 1f, 0.16f));
            _owned.Add(steam);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 20f;
                ProtoMaterials.VisualPart(root, "Steam", PrimitiveType.Sphere, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 2f + (i % 3), 11f), new Vector3(5f, 3f, 5f), Quaternion.identity, steam, false);
            }

            // 나무 발판 7×7 (가운데 칸은 인형뽑기 기계)
            Material deck = SinkKit.TexMat("Onsen/wood_deck", 0.5f, new Color(0.92f, 0.82f, 0.72f), 0.5f);
            int c = Grid / 2;
            for (int z = 0; z < Grid; z++)
            {
                for (int x = 0; x < Grid; x++)
                {
                    if (x == c && z == c)
                    {
                        continue;
                    }
                    int ring = Mathf.Max(Mathf.Abs(x - c), Mathf.Abs(z - c)) - 1; // 0 = 기계 둘레 3×3
                    var go = new GameObject("Deck_" + x + "_" + z);
                    go.transform.SetParent(root, false);
                    Vector3 home = new Vector3((x - c) * Cell, 0f, (z - c) * Cell);
                    go.transform.localPosition = home;
                    SinkKit.Slab(go.transform, "Plank", Vector3.zero, new Vector3(Cell - 0.15f, 0.5f, Cell - 0.15f), 0.12f, deck);
                    SinkKit.Part(go.transform, "Post", MeshKit.RoundedBox(new Vector3(0.4f, 3f, 0.4f), 0.15f), new Vector3(0f, -2f, 0f), Quaternion.identity, deck, false);
                    _tiles.Add(new Tile { T = go.transform, Home = home, Ring = ring });
                    if (ring == 0)
                    {
                        _spawns.Add(home + Vector3.up * 0.05f);
                    }
                }
            }

            // 가운데 인형뽑기 기계 (분홍 상자 + 유리 + 꼭대기 고양이 인형) — 단단한 장애물
            Material pink = SinkKit.TexMat("Onsen/pink_cabinet", 0.5f, Color.white, 0.75f, 0.2f);
            Material plush = SinkKit.TexMat("Onsen/cat_plush", 0.8f, Color.white, 0.15f);
            GameObject cab = SinkKit.Slab(root, "ClawCabinet", new Vector3(0f, 1.6f, 0f), new Vector3(2.3f, 2.2f, 2.3f), 0.3f, pink);
            Material glass = ProtoMaterials.Transparent(new Color(0.85f, 0.95f, 1f, 0.3f));
            _owned.Add(glass);
            SinkKit.Part(root, "Glass", MeshKit.RoundedBox(new Vector3(2.1f, 1.6f, 2.1f), 0.1f), new Vector3(0f, 2.4f, 0f), Quaternion.identity, glass, false);
            SinkKit.Part(root, "CabinetTop", MeshKit.RoundedBox(new Vector3(2.5f, 0.5f, 2.5f), 0.2f), new Vector3(0f, 3.45f, 0f), Quaternion.identity, pink);
            CatPlush(root, new Vector3(0f, 3.7f, 0f), plush);
            // 위를 가로지르는 집게 레일 (장식)
            Material frame = SinkKit.Mat(new Color(0.98f, 0.80f, 0.86f), 0.7f, 0.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, "RailPillar", MeshKit.RoundedBox(new Vector3(0.6f, 13f, 0.6f), 0.25f), new Vector3(s * 11f, 5f, 0f), Quaternion.identity, frame);
            }
            SinkKit.Part(root, "Rail", MeshKit.RoundedBox(new Vector3(22.6f, 0.6f, 0.8f), 0.25f), new Vector3(0f, 11.5f, 0f), Quaternion.identity, frame);

            // 움직이는 집게
            var clawGo = new GameObject("Claw");
            clawGo.transform.SetParent(root, false);
            Transform claw = clawGo.transform;
            SinkKit.Part(claw, "Head", MeshKit.RoundedBox(new Vector3(1.4f, 0.8f, 1.4f), 0.35f), new Vector3(0f, 0.6f, 0f), Quaternion.identity, frame);
            SinkKit.Part(claw, "Cable", MeshKit.RoundedBox(new Vector3(0.12f, 6f, 0.12f), 0.05f), new Vector3(0f, 4f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.3f, 0.3f, 0.32f), 0.5f), false);
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 120f, 0f);
                SinkKit.Part(claw, "Prong", MeshKit.RoundedBox(new Vector3(0.25f, 1.4f, 0.25f), 0.1f), q * new Vector3(0f, -0.3f, 0.6f), q * Quaternion.Euler(-25f, 0f, 0f),
                    SinkKit.Mat(new Color(0.85f, 0.86f, 0.9f), 0.8f, 0.6f));
            }
            var clawCtl = new GameObject("ClawDrop");
            clawCtl.transform.SetParent(root, false);
            _claw = clawCtl.AddComponent<ClawDrop>();
            _claw.Center = transform.position;
            _claw.AreaRadius = Cell * 3.2f;
            _claw.Height = 9f;
            _claw.Claw = claw;
            Material warn = ProtoMaterials.Transparent(new Color(1f, 0.2f, 0.35f, 0.45f));
            _owned.Add(warn);
            _claw.WarnMat = warn;
            claw.position = transform.position + Vector3.up * 9f;

            BotPath.Clear();
            NewRound();
        }

        /// <summary>하얀 고양이 인형 (몸·머리·귀·눈) — 장식.</summary>
        private static void CatPlush(Transform root, Vector3 at, Material plush)
        {
            ProtoMaterials.VisualPart(root, "CatBody", PrimitiveType.Sphere, at + new Vector3(0f, 0.6f, 0f), new Vector3(1.9f, 1.4f, 1.6f), Quaternion.identity, plush);
            ProtoMaterials.VisualPart(root, "CatHead", PrimitiveType.Sphere, at + new Vector3(0f, 1.7f, 0f), new Vector3(1.7f, 1.4f, 1.5f), Quaternion.identity, plush);
            Material pinkEar = SinkKit.Mat(new Color(1f, 0.72f, 0.80f), 0.3f);
            Material eye = SinkKit.Mat(new Color(0.1f, 0.08f, 0.1f), 0.9f);
            for (int s = -1; s <= 1; s += 2)
            {
                ProtoMaterials.VisualPart(root, "CatEar", PrimitiveType.Cube, at + new Vector3(s * 0.5f, 2.4f, 0f), new Vector3(0.45f, 0.45f, 0.25f), Quaternion.Euler(0f, 0f, 45f), plush);
                ProtoMaterials.VisualPart(root, "CatEarIn", PrimitiveType.Cube, at + new Vector3(s * 0.5f, 2.38f, -0.1f), new Vector3(0.28f, 0.28f, 0.1f), Quaternion.Euler(0f, 0f, 45f), pinkEar);
                ProtoMaterials.VisualPart(root, "CatEye", PrimitiveType.Sphere, at + new Vector3(s * 0.35f, 1.8f, -0.7f), new Vector3(0.22f, 0.28f, 0.1f), Quaternion.identity, eye);
                ProtoMaterials.VisualPart(root, "CatCheek", PrimitiveType.Sphere, at + new Vector3(s * 0.55f, 1.55f, -0.66f), new Vector3(0.25f, 0.15f, 0.06f), Quaternion.identity, pinkEar);
            }
        }

        private void SetupOnsenLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1f, 0.88f, 0.76f);
                sun.intensity = 1.2f;
                sun.transform.rotation = Quaternion.Euler(45f, -20f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.76f, 0.80f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.62f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.34f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.95f, 0.88f, 0.86f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 300f;
        }

        // ---------- 판 진행 ----------

        private void NewRound()
        {
            _roundStart = Time.time;
            foreach (Tile t in _tiles)
            {
                t.T.localPosition = t.Home;
            }
            if (_claw != null)
            {
                _claw.ResetCycle();
            }
        }

        protected override void Update()
        {
            base.Update();
            float t = Time.time - _roundStart;
            if (t >= RoundSeconds)
            {
                NewRound();
                return;
            }
            foreach (Tile tile in _tiles)
            {
                if (tile.Ring == 0)
                {
                    continue;
                }
                // 바깥 줄(ring 2)이 먼저, 그다음 ring 1
                int order = SinkingRings - tile.Ring; // ring2 → 0, ring1 → 1
                float sinkAt = SinkEvery * (order + 1);
                float k = Mathf.Clamp01((t - sinkAt) / SinkSeconds);
                Vector3 p = tile.Home + Vector3.down * (SinkDepth * k);
                if (k <= 0f && t >= sinkAt - WarnSeconds)
                {
                    float j = 0.05f * Mathf.Sin(Time.time * 35f + tile.Home.x * 3f);
                    p += new Vector3(j, 0f, -j);
                }
                tile.T.localPosition = p;
            }
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            yaw = 0f;
            if (_spawns.Count == 0)
            {
                return transform.position + Vector3.up * 0.05f;
            }
            Vector3 p = _spawns[index % _spawns.Count];
            yaw = Quaternion.LookRotation(-new Vector3(p.x, 0f, p.z)).eulerAngles.y;
            return transform.TransformPoint(p);
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
            float t = Time.time - _roundStart;
            int sunk = t >= SinkEvery * 2f ? 2 : t >= SinkEvery ? 1 : 0;
            string next = sunk < SinkingRings
                ? string.Format("물이 차오르기까지 {0:0}초", Mathf.Max(0f, SinkEvery * (sunk + 1) - t))
                : string.Format("발판이 올라오기까지 {0:0}초", Mathf.Max(0f, RoundSeconds - t));
            string best = _bestSurvive >= 0f ? string.Format(" · 최고 {0:0.0}초", _bestSurvive) : "";
            return string.Format("{0}\n버틴 시간 {1:0.0}초{2} · 발판 위 {3}/{4}\n{5}", Title, survive, best, AliveCount(), Runners.Count, next);
        }

        public override string CenterText(SushiCharacter player)
        {
            float c = RespawnCountdown(player);
            if (c >= 0f)
            {
                string last = _lastSurvive >= 0f ? string.Format("\n{0:0.0}초 버팀", _lastSurvive) : "";
                return "탈락!" + last + "\n" + Mathf.CeilToInt(c);
            }
            return "";
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
