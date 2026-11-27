// Survival 맵 — "초대형 밥솥 생존" (콘셉트 맵 시트 3번). 거대한 밥솥 안 밥 위에서 45초 버티기.
//   밥물 끓어오름(7초마다 세 곳, 거품 경고 → 펑 하고 튀겨 올림) + 양옆 증기구 분사(12초마다 엇갈려, 한 줄을 옆으로 밀어냄).
// 밥솥 가장자리 턱이 낮아서 세게 맞으면 밖으로 날아가 떨어져요 → "탈락" (시제품: 3초 뒤 다시, 버틴 시간 새로).
// 45초가 지나면 "생존!" 뒤 판 다시 시작. 텍스처는 Codex로 뽑은 Resources/Cooker/ (주방 배경은 Sink/ 것을 같이 씀).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class RiceCookerLevel : PrototypeLevel
    {
        public const float RiceRadius = 9f;
        public const float RoundSeconds = 45f;
        private const int SpawnSlots = 8;

        private readonly List<Material> _owned = new List<Material>();
        private EruptionField _boil;
        private SteamVent[] _vents;
        private float _roundStart;
        private float _clearedUntil = -1f;
        private float _bestSurvive = -1f;
        private float _lastSurvive = -1f;

        public override string Title => "초대형 밥솥 생존 (Survival)";

        public override void Build()
        {
            KillY = -6f;
            BotEdgeJumps = false;
            WanderCenter = transform.position;
            WanderRadius = RiceRadius - 2.5f;
            Transform root = transform;
            SinkKit.SetupLighting();
            SinkKit.BackdropBox(root, SinkKit.Backdrop("Sink/kitchen_backdrop", _owned), new Vector3(0f, 10f, 0f), 120f);

            // 밥 (판정: 볼록 원기둥, 보이는 것: 살짝 볼록한 밥 판)
            Material rice = SinkKit.TexMat("Cooker/rice_surface", 0.35f, Color.white, 0.45f);
            SinkKit.Part(root, "Rice", MeshKit.Disc(RiceRadius + 0.1f, 0.08f), Vector3.zero, Quaternion.identity, rice);
            SinkKit.CylinderCollider(root, 0.02f, RiceRadius, 1f);

            // 솥 (안쪽 벽 + 낮은 턱 + 바깥 몸통) — 턱은 밥보다 0.35 m만 높아요
            Material pot = SinkKit.TexMat("Cooker/pot_metal", 0.12f, Color.white, 0.6f, 0.3f);
            Vector2[] potProfile =
            {
                new Vector2(RiceRadius, 0.35f), new Vector2(RiceRadius + 0.5f, 0.45f), new Vector2(RiceRadius + 0.9f, 0.3f),
                new Vector2(RiceRadius + 1.0f, -0.5f), new Vector2(RiceRadius + 1.4f, -1.2f), new Vector2(RiceRadius + 1.4f, -9f),
                new Vector2(RiceRadius + 1.4f, -9f), new Vector2(0f, -9f),
            };
            SinkKit.Part(root, "Pot", MeshKit.Lathe(potProfile, 72), Vector3.zero, Quaternion.identity, pot);
            Vector2[] innerWall = { new Vector2(RiceRadius - 0.01f, -0.2f), new Vector2(RiceRadius - 0.01f, 0.35f), new Vector2(RiceRadius + 0.02f, 0.35f) };
            SinkKit.Part(root, "PotInner", MeshKit.Lathe(innerWall, 72), Vector3.zero, Quaternion.identity, pot, false);
            Rim(root, 64);
            // 손잡이 두 개 + 바닥 받침 (장식)
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, "Handle", MeshKit.RoundedBox(new Vector3(2.4f, 0.8f, 3.2f), 0.38f), new Vector3(s * (RiceRadius + 2.2f), -1.4f, 0f), Quaternion.identity,
                    SinkKit.Mat(new Color(0.18f, 0.18f, 0.2f), 0.5f));
            }
            Vector2[] base_ = { new Vector2(0f, -9f), new Vector2(RiceRadius + 2.6f, -9f), new Vector2(RiceRadius + 3f, -10f), new Vector2(RiceRadius + 3f, -40f), new Vector2(0f, -40f) };
            SinkKit.Part(root, "CookerBody", MeshKit.Lathe(base_, 72), Vector3.zero, Quaternion.identity, SinkKit.Mat(new Color(0.95f, 0.94f, 0.92f), 0.7f));
            SinkKit.Part(root, "Display", MeshKit.RoundedBox(new Vector3(5f, 2.4f, 0.4f), 0.2f), new Vector3(0f, -13f, -(RiceRadius + 3f)), Quaternion.identity,
                SinkKit.Glow(SinkKit.Mat(new Color(0.15f, 0.05f, 0.05f), 0.6f), new Color(1f, 0.25f, 0.1f), 0.6f, _owned));
            // 거대한 주걱 (장식, 솥에 기대 있음)
            SinkKit.Part(root, "Paddle", MeshKit.RoundedBox(new Vector3(2.2f, 0.35f, 16f), 0.15f), new Vector3(7f, 6f, 8f), Quaternion.Euler(-40f, -35f, 0f),
                SinkKit.TexMat("wood_board", 0.2f, Color.white, 0.35f));

            // 밥물 끓어오름
            var boilGo = new GameObject("Boil");
            boilGo.transform.SetParent(root, false);
            _boil = boilGo.AddComponent<EruptionField>();
            _boil.Center = transform.position;
            _boil.AreaRadius = RiceRadius - 2f;
            _boil.WarnMat = Transparent(new Color(1f, 1f, 1f, 0.55f));
            _boil.BlastMat = Transparent(new Color(0.97f, 0.97f, 0.95f, 0.8f));

            // 증기구 2개 (양옆에서 엇갈려 뿜음, 줄은 z ±3)
            _vents = new SteamVent[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? 1f : -1f;
                var ventGo = new GameObject("SteamVent" + i);
                ventGo.transform.SetParent(root, false);
                ventGo.transform.localPosition = new Vector3(s * (RiceRadius + 0.4f), 0f, s * 3f);
                GameObject nozzle = SinkKit.Part(ventGo.transform, "Nozzle", MeshKit.RoundedBox(new Vector3(1.2f, 1.4f, 2.4f), 0.4f), new Vector3(0f, 0.7f, 0f), Quaternion.identity,
                    SinkKit.Mat(new Color(0.55f, 0.56f, 0.6f), 0.7f, 0.5f));
                SteamVent vent = ventGo.AddComponent<SteamVent>();
                vent.Direction = new Vector3(-s, 0f, 0f);
                vent.Length = RiceRadius * 2f + 1f;
                vent.Width = 3f;
                vent.Interval = 12f;
                vent.FirstDelay = 6f + i * 6f;
                vent.Nozzle = nozzle.GetComponent<Renderer>();
                vent.SteamMat = Transparent(new Color(1f, 1f, 1f, 0.45f));
                _vents[i] = vent;
            }

            // 김 (장식: 솥 위로 피어오르는 큰 김)
            Material puff = Transparent(new Color(1f, 1f, 1f, 0.18f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                ProtoMaterials.VisualPart(root, "SteamPuff", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 6f, 9f + (i % 3) * 3f, Mathf.Sin(a) * 6f),
                    new Vector3(7f, 4f, 7f), Quaternion.identity, puff, false);
            }

            BotPath.Clear(); // 생존: 밥 위를 돌아다님
            _roundStart = Time.time;
        }

        /// <summary>솥 턱 (보이는 것 + 둘레를 따라 놓은 낮은 상자 콜라이더 — 밥 밖으로 걸어 나가진 않게, 세게 맞으면 넘어가요).</summary>
        private void Rim(Transform root, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * 360f / count;
                Quaternion q = Quaternion.Euler(0f, a, 0f);
                var go = new GameObject("RimCollider");
                go.transform.SetParent(root, false);
                go.transform.localPosition = q * new Vector3(0f, 0.18f, RiceRadius + 0.25f);
                go.transform.localRotation = q;
                go.AddComponent<BoxCollider>().size = new Vector3(2f * Mathf.PI * (RiceRadius + 0.25f) / count + 0.05f, 0.36f, 0.5f);
            }
        }

        private Material Transparent(Color c)
        {
            Material m = ProtoMaterials.Transparent(c);
            _owned.Add(m);
            return m;
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            float ang = (index % SpawnSlots) * 360f / SpawnSlots + (index / SpawnSlots) * 22.5f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            yaw = ang + 180f;
            return transform.TransformPoint(dir * 5f + Vector3.up * 0.05f);
        }

        protected override void Update()
        {
            base.Update();
            float now = Time.time;
            if (_clearedUntil < 0f && now - _roundStart >= RoundSeconds)
            {
                _clearedUntil = now + 3f; // "생존!" 잠깐 보여 주고
            }
            else if (_clearedUntil >= 0f && now >= _clearedUntil)
            {
                NewRound();
            }
        }

        private void NewRound()
        {
            _clearedUntil = -1f;
            _roundStart = Time.time;
            for (int i = 0; i < Runners.Count; i++)
            {
                if (Runners[i].RespawnAt < 0f)
                {
                    Runners[i].StartTime = Time.time;
                }
            }
            if (_boil != null)
            {
                _boil.ResetCycle();
            }
            if (_vents != null)
            {
                for (int i = 0; i < _vents.Length; i++)
                {
                    _vents[i].ResetCycle();
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
            string boil = _boil != null ? string.Format(" · 끓어오름까지 {0:0}초", _boil.NextIn) : "";
            return string.Format("{0}\n남은 시간 {1:00}초 · 버틴 시간 {2:0.0}초{3}\n밥솥 위 {4}/{5}{6}", Title, left, survive, best, AliveCount(), Runners.Count, boil);
        }

        public override string CenterText(SushiCharacter player)
        {
            float c = RespawnCountdown(player);
            if (c >= 0f)
            {
                string last = _lastSurvive >= 0f ? string.Format("\n{0:0.0}초 버팀", _lastSurvive) : "";
                return "탈락!" + last + "\n" + Mathf.CeilToInt(c);
            }
            if (_clearedUntil >= 0f)
            {
                return "생존!";
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
