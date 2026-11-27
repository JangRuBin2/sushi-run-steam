// Final 맵 — "찻잎 정원 결승" (콘셉트 맵 시트 5번). 연못 위 둥근 차밭, 가운데 찻잔이 돌며 대나무 국자로 쓸어 내요.
//   찻잔 회전: 6초에 한 바퀴, 무릎 높이 국자 = 점프로 넘기.
//   차밭이 무너짐: 15초마다 바깥 고리부터 한 줄씩 (2초 흔들림 경고 → 떨어짐). 가운데 받침 접시는 안 무너져요.
// 떨어지면 "탈락" (시제품: 3초 뒤 다시). 60초가 지나면 차밭이 돌아오고 판 다시 시작.
// 텍스처는 Codex로 뽑은 Resources/TeaGarden/.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class TeaGardenLevel : PrototypeLevel
    {
        public const float SaucerRadius = 4f;
        public const float RingWidth = 3f;
        public const int RingCount = 3;
        public const float CollapseEvery = 15f;
        public const float RoundSeconds = 60f;
        public const float WarnSeconds = 2f;
        public const float CupSpeed = 60f;
        private const int SpawnSlots = 8;

        private sealed class Tile
        {
            public GameObject Go;
            public Collider Col;
            public Vector3 Home;
            public float FallSpeed;
        }

        private readonly List<Material> _owned = new List<Material>();
        private readonly List<Tile>[] _rings = new List<Tile>[RingCount];
        private int _collapsed; // 무너진 고리 수 (바깥부터)
        private Rotator _cup;
        private float _roundStart;
        private float _bestSurvive = -1f;
        private float _lastSurvive = -1f;

        public override string Title => "찻잎 정원 결승 (Final)";

        public override bool BotAttacksPaused => _cup != null && _cup.IsHolding;

        public override void Build()
        {
            KillY = -3f;
            BotEdgeJumps = false;
            WanderCenter = transform.position;
            WanderRadius = SaucerRadius + RingWidth * RingCount - 2f;
            Transform root = transform;
            SetupGardenLighting();
            SinkKit.BackdropBox(root, SinkKit.Backdrop("TeaGarden/garden_backdrop", _owned), new Vector3(0f, 14f, 0f), 130f);

            // 연못 (반투명 물) + 바닥 돌
            Material water = ProtoMaterials.Transparent(new Color(0.45f, 0.75f, 0.72f, 0.6f));
            _owned.Add(water);
            SinkKit.Part(root, "Pond", MeshKit.Disc(60f), new Vector3(0f, -4f, 0f), Quaternion.identity, water, false);
            SinkKit.Part(root, "PondBed", MeshKit.Disc(60f), new Vector3(0f, -9f, 0f), Quaternion.identity, SinkKit.TexMat("TeaGarden/stone_path", 0.08f, new Color(0.6f, 0.7f, 0.65f), 0.2f));

            // 가운데 받침 접시 (안 무너짐)
            Material celadon = SinkKit.TexMat("TeaGarden/celadon_glaze", 0.3f, Color.white, 0.85f);
            SinkKit.Part(root, "Saucer", MeshKit.Disc(SaucerRadius), Vector3.zero, Quaternion.identity, celadon);
            Vector2[] saucerSide = { new Vector2(SaucerRadius, 0f), new Vector2(SaucerRadius + 0.15f, -0.1f), new Vector2(SaucerRadius - 0.3f, -0.9f), new Vector2(0f, -0.9f) };
            SinkKit.Part(root, "SaucerSide", MeshKit.Lathe(saucerSide, 64), Vector3.zero, Quaternion.identity, celadon);
            SinkKit.CylinderCollider(root, 0f, SaucerRadius, 0.9f);
            SinkKit.Part(root, "Pillar", MeshKit.Lathe(new[] { new Vector2(0f, -0.9f), new Vector2(1.6f, -0.9f), new Vector2(1.2f, -5f), new Vector2(0f, -5f) }, 32),
                Vector3.zero, Quaternion.identity, SinkKit.TexMat("TeaGarden/stone_path", 0.3f, Color.white, 0.2f));

            // 찻잔 (돌아요) + 대나무 국자 두 개 (무릎 높이 막대)
            _cup = GreyboxKit.MakeRotator(root, "Teacup", Vector3.zero, CupSpeed);
            Transform cup = _cup.transform;
            Vector2[] cupProfile =
            {
                new Vector2(0f, 2.05f), new Vector2(1.45f, 2.05f), new Vector2(1.6f, 2.2f), new Vector2(1.7f, 2.15f), new Vector2(1.55f, 1.2f),
                new Vector2(1.2f, 0.25f), new Vector2(0.9f, 0f), new Vector2(0f, 0f),
            };
            SinkKit.Part(cup, "Cup", MeshKit.Lathe(cupProfile, 48), Vector3.zero, Quaternion.identity, celadon);
            SinkKit.Part(cup, "Tea", MeshKit.Disc(1.45f), new Vector3(0f, 2.06f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.55f, 0.68f, 0.22f), 0.9f), false);
            SinkKit.CylinderCollider(cup, 2.2f, 1.5f, 2.2f);
            Ladle(cup, RingWidth * 2f + SaucerRadius);

            // 차밭 고리 3개 (바깥부터 무너짐)
            Material bush = SinkKit.TexMat("TeaGarden/tea_bush", 0.5f, Color.white, 0.4f);
            Material soil = SinkKit.Mat(new Color(0.42f, 0.30f, 0.20f), 0.2f);
            for (int ring = 0; ring < RingCount; ring++)
            {
                _rings[ring] = new List<Tile>();
                float mid = SaucerRadius + RingWidth * (ring + 0.5f);
                int n = Mathf.RoundToInt(2f * Mathf.PI * mid / 2.9f);
                float w = 2f * Mathf.PI * mid / n - 0.12f;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + (ring % 2) * 0.5f) * 360f / n;
                    Quaternion q = Quaternion.Euler(0f, a, 0f);
                    var go = new GameObject("TeaTile_r" + ring + "_" + i);
                    go.transform.SetParent(root, false);
                    go.transform.localPosition = q * new Vector3(0f, 0f, mid);
                    go.transform.localRotation = q;
                    var size = new Vector3(w, 0.9f, RingWidth - 0.15f);
                    SinkKit.Part(go.transform, "Bush", MeshKit.RoundedBox(new Vector3(size.x, 0.7f, size.z), 0.3f), new Vector3(0f, -0.35f, 0f), Quaternion.identity, bush);
                    SinkKit.Part(go.transform, "Soil", MeshKit.RoundedBox(new Vector3(size.x - 0.1f, 0.5f, size.z - 0.1f), 0.15f), new Vector3(0f, -0.85f, 0f), Quaternion.identity, soil, false);
                    var col = go.AddComponent<BoxCollider>();
                    col.size = size;
                    col.center = new Vector3(0f, -size.y * 0.5f, 0f);
                    _rings[ring].Add(new Tile { Go = go, Col = col, Home = go.transform.localPosition });
                }
            }

            Decor(root);
            BotPath.Clear(); // 결승: 길 없이 돌아다님
            NewRound();
        }

        /// <summary>찻잔에 붙은 대나무 국자 두 개 (양쪽으로 뻗은 막대 판정 하나).</summary>
        private void Ladle(Transform cup, float reach)
        {
            const float barHeight = 0.45f;
            float length = reach * 2f;
            var bar = new GameObject("LadleBar");
            bar.transform.SetParent(cup, false); // KnockHazard.Awake가 부모 Rotator를 찾게 먼저 붙여요
            bar.transform.localPosition = new Vector3(0f, barHeight, 0f);
            var box = bar.AddComponent<BoxCollider>();
            box.size = new Vector3(0.34f, 0.3f, length);
            var hazard = bar.AddComponent<KnockHazard>();
            hazard.Box = box;
            hazard.Low = true;
            hazard.KnockHorizontal = 9f;
            hazard.KnockUp = 4.5f;
            Material bamboo = SinkKit.Mat(new Color(0.62f, 0.72f, 0.38f), 0.55f);
            Material node = SinkKit.Mat(new Color(0.48f, 0.58f, 0.28f), 0.5f);
            SinkKit.Part(bar.transform, "Bamboo", MeshKit.RoundedBox(new Vector3(0.28f, 0.24f, length - 0.6f), 0.11f), Vector3.zero, Quaternion.identity, bamboo);
            for (float d = 1.8f; d < reach; d += 1.4f)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    SinkKit.Part(bar.transform, "Node", MeshKit.RoundedBox(new Vector3(0.34f, 0.3f, 0.12f), 0.05f), new Vector3(0f, 0f, s * d), Quaternion.identity, node, false);
                }
            }
            for (int s = -1; s <= 1; s += 2)
            {
                // 국자 끝 오목한 머리
                SinkKit.Part(bar.transform, "LadleHead", MeshKit.Lathe(new[] { new Vector2(0f, 0.25f), new Vector2(0.45f, 0.25f), new Vector2(0.5f, 0.2f), new Vector2(0.35f, -0.2f), new Vector2(0f, -0.25f) }, 24),
                    new Vector3(0f, 0.05f, s * (reach - 0.2f)), Quaternion.identity, bamboo);
            }
        }

        /// <summary>벚나무·석등·큰 찻주전자 (장식, 연못 둘레).</summary>
        private void Decor(Transform root)
        {
            Material trunk = SinkKit.Mat(new Color(0.40f, 0.26f, 0.20f), 0.3f);
            Material blossom = SinkKit.Mat(new Color(1f, 0.74f, 0.82f), 0.3f);
            Material blossom2 = SinkKit.Mat(new Color(1f, 0.86f, 0.90f), 0.3f);
            Material stone = SinkKit.TexMat("TeaGarden/stone_path", 0.4f, Color.white, 0.2f);
            var rnd = new System.Random(11);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + 12f;
                float r = 24f + (float)rnd.NextDouble() * 10f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -4f, r);
                SinkKit.Part(root, "Island", MeshKit.Disc(4.5f, 0.6f), p, Quaternion.identity, SinkKit.Mat(new Color(0.55f, 0.72f, 0.40f), 0.3f));
                if (i % 3 == 2)
                {
                    // 석등
                    SinkKit.Part(root, "Lantern", MeshKit.Lathe(new[] { new Vector2(0f, 4.2f), new Vector2(0.3f, 4.2f), new Vector2(1.6f, 3.2f), new Vector2(1.6f, 3f), new Vector2(0.7f, 3f),
                        new Vector2(0.7f, 2f), new Vector2(0.35f, 1.8f), new Vector2(0.35f, 0.4f), new Vector2(1f, 0f), new Vector2(0f, 0f) }, 6), p, Quaternion.identity, stone);
                    continue;
                }
                SinkKit.Part(root, "Trunk", MeshKit.Lathe(new[] { new Vector2(0f, 6f), new Vector2(0.4f, 6f), new Vector2(0.7f, 0f), new Vector2(0f, 0f) }, 12), p, Quaternion.identity, trunk);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 off = new Vector3(((float)rnd.NextDouble() - 0.5f) * 4f, 6f + (float)rnd.NextDouble() * 2.5f, ((float)rnd.NextDouble() - 0.5f) * 4f);
                    float s = 3f + (float)rnd.NextDouble() * 2f;
                    ProtoMaterials.VisualPart(root, "Blossom", PrimitiveType.Sphere, p + off, new Vector3(s, s * 0.8f, s), Quaternion.identity, k % 2 == 0 ? blossom : blossom2);
                }
            }
            // 큰 찻주전자 (멀리)
            Vector3 pot = new Vector3(-30f, -4f, 36f);
            SinkKit.Part(root, "Teapot", MeshKit.Lathe(new[] { new Vector2(0f, 13f), new Vector2(1.5f, 13f), new Vector2(2f, 12f), new Vector2(7f, 10f), new Vector2(9f, 6f),
                new Vector2(8f, 1.5f), new Vector2(6f, 0f), new Vector2(0f, 0f) }, 48), pot, Quaternion.identity, SinkKit.Mat(new Color(0.35f, 0.25f, 0.22f), 0.8f));
            SinkKit.Part(root, "Spout", MeshKit.RoundedBox(new Vector3(1.6f, 1.6f, 9f), 0.75f), pot + new Vector3(8f, 7.5f, -2f), Quaternion.Euler(-35f, 60f, 0f), SinkKit.Mat(new Color(0.35f, 0.25f, 0.22f), 0.8f));
        }

        private void SetupGardenLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1f, 0.95f, 0.88f);
                sun.intensity = 1.3f;
                sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.82f, 0.85f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.70f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.45f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.93f, 0.88f, 0.90f);
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 380f;
        }

        // ---------- 판 진행 ----------

        private void NewRound()
        {
            _roundStart = Time.time;
            _collapsed = 0;
            for (int ring = 0; ring < RingCount; ring++)
            {
                if (_rings[ring] == null)
                {
                    continue;
                }
                foreach (Tile t in _rings[ring])
                {
                    t.Go.SetActive(true);
                    t.Go.transform.localPosition = t.Home;
                    t.Col.enabled = true;
                    t.FallSpeed = 0f;
                }
            }
            if (_cup != null)
            {
                _cup.SpeedDeg = CupSpeed; // (Rotator.ResetSpeed는 Start 전이면 0이 돼요)
                _cup.Hold(2.5f); // 판 시작 직후엔 멈춰 있어요
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
            for (int k = 0; k < RingCount; k++)
            {
                int ring = RingCount - 1 - k; // 바깥부터
                float collapseAt = CollapseEvery * (k + 1);
                List<Tile> tiles = _rings[ring];
                if (tiles == null)
                {
                    continue;
                }
                if (k >= _collapsed && t >= collapseAt)
                {
                    _collapsed = k + 1;
                    foreach (Tile tile in tiles)
                    {
                        tile.Col.enabled = false;
                        tile.FallSpeed = 0.5f;
                    }
                }
                bool warning = k == _collapsed && t >= collapseAt - WarnSeconds;
                foreach (Tile tile in tiles)
                {
                    if (!tile.Go.activeSelf)
                    {
                        continue;
                    }
                    if (tile.FallSpeed > 0f)
                    {
                        tile.FallSpeed += 14f * Time.deltaTime;
                        tile.Go.transform.localPosition += Vector3.down * (tile.FallSpeed * Time.deltaTime);
                        if (tile.Go.transform.localPosition.y < -8f)
                        {
                            tile.Go.SetActive(false);
                        }
                    }
                    else if (warning)
                    {
                        // 흔들림 경고 (보이기만 — 콜라이더도 같이 살짝 움직여요)
                        float j = 0.06f * Mathf.Sin(Time.time * 40f + tile.Home.x);
                        tile.Go.transform.localPosition = tile.Home + new Vector3(j, 0f, -j);
                    }
                }
            }
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            float ang = (index % SpawnSlots) * 360f / SpawnSlots + 22.5f + (index / SpawnSlots) * 11f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            yaw = ang + 180f;
            return transform.TransformPoint(dir * (SaucerRadius - 1.1f) + Vector3.up * 0.05f);
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
            string next = _collapsed < RingCount
                ? string.Format("다음 무너짐까지 {0:0}초", Mathf.Max(0f, CollapseEvery * (_collapsed + 1) - t))
                : string.Format("차밭이 돌아오기까지 {0:0}초", Mathf.Max(0f, RoundSeconds - t));
            string best = _bestSurvive >= 0f ? string.Format(" · 최고 {0:0.0}초", _bestSurvive) : "";
            return string.Format("{0}\n버틴 시간 {1:0.0}초{2} · 정원 위 {3}/{4}\n{5}", Title, survive, best, AliveCount(), Runners.Count, next);
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
