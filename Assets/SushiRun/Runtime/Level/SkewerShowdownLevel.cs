// Final 회색 박스 — "회전 꼬치 쇼다운": 지름 16 m 둥근 접시 위에서 꼬치 2개가 돌며 점점 빨라져요.
//   낮은 꼬치(무릎 높이) = 점프로 넘기 / 높은 꼬치(서 있으면 머리 위, 점프하면 걸림) = 땅에 붙어 있거나 다이브로.
// 떨어지면 "탈락" (시제품: 3초 뒤 다시 올라오고, 버틴 시간을 새로 셈).
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class SkewerShowdownLevel : PrototypeLevel
    {
        public const float PlateRadius = 8f;
        public const float LowBarY = 0.45f;
        public const float HighBarY = 1.62f;
        public const float LowStartSpeed = 40f;
        public const float HighStartSpeed = -30f;
        public const int SpawnSlots = 8;
        // 판 시작·플레이어 부활 때 꼬치가 멈춰 있는 시간 — 나오자마자 맞지 않게
        public const float StartHoldSeconds = 2f;

        private Rotator _low;
        private Rotator _high;
        private float _bestSurvive = -1f;
        private float _lastSurvive = -1f;

        public override string Title => "회전 꼬치 쇼다운 (Final)";

        public override void Build()
        {
            KillY = -6f;
            BotEdgeJumps = false;
            WanderCenter = transform.position;
            WanderRadius = PlateRadius - 2.2f;
            Transform root = transform;

            GreyboxKit.KitchenBackdrop(root, Vector3.zero, -40f);

            // 접시 + 기둥 (판정은 접시 원기둥 하나)
            GreyboxKit.Cylinder(root, "ArenaPlate", new Vector3(0f, -0.3f, 0f), PlateRadius * 2f, 0.6f, GreyboxKit.PlateWhite, 0.65f);
            GreyboxKit.Cylinder(root, "ArenaRim", new Vector3(0f, -0.42f, 0f), PlateRadius * 2f + 0.5f, 0.6f, GreyboxKit.PlateRim, 0.55f, false);
            GreyboxKit.Cylinder(root, "ArenaRing", new Vector3(0f, 0.004f, 0f), PlateRadius * 1.5f, 0.006f, new Color(0.82f, 0.86f, 0.93f), 0.5f, false);
            GreyboxKit.Cylinder(root, "ArenaRingInner", new Vector3(0f, 0.006f, 0f), PlateRadius * 1.4f, 0.006f, GreyboxKit.PlateWhite, 0.6f, false);
            GreyboxKit.Cylinder(root, "Pillar", new Vector3(0f, -20.7f, 0f), 3.2f, 40f, GreyboxKit.Lacquer, 0.5f, false);

            // 낮은 꼬치 (가운데 축 포함)
            _low = GreyboxKit.MakeRotator(root, "LowSkewer", Vector3.zero, LowStartSpeed);
            _low.AccelerationDeg = 2.2f;
            _low.MaxSpeedDeg = 140f;
            GreyboxKit.Cylinder(_low.transform, "Hub", new Vector3(0f, 1.1f, 0f), 0.9f, 2.2f, GreyboxKit.Lacquer, 0.55f);
            ProtoMaterials.VisualPart(_low.transform, "HubCap", PrimitiveType.Sphere, new Vector3(0f, 2.25f, 0f),
                new Vector3(1.0f, 0.5f, 1.0f), Quaternion.identity, ProtoMaterials.Opaque(GreyboxKit.WoodDark, 0.5f));
            Skewer(_low.transform, "LowBar", LowBarY, true, new[]
            {
                new Color(1f, 0.52f, 0.32f), new Color(1f, 0.82f, 0.25f), new Color(0.40f, 0.70f, 0.32f), new Color(0.48f, 0.32f, 0.22f),
            });

            // 높은 꼬치 (반대로, 조금 느리게 시작)
            _high = GreyboxKit.MakeRotator(root, "HighSkewer", Vector3.zero, HighStartSpeed);
            _high.AccelerationDeg = 1.6f;
            _high.MaxSpeedDeg = 110f;
            Skewer(_high.transform, "HighBar", HighBarY, false, new[]
            {
                new Color(0.92f, 0.25f, 0.18f), new Color(0.98f, 0.95f, 0.88f), new Color(0.85f, 0.15f, 0.22f), new Color(0.98f, 0.70f, 0.30f),
            });

            BotPath.Clear(); // 결승은 길 없이 접시 위를 돌아다님
            ResetSkewers();
        }

        /// <summary>판정 상자(0.42 각) + 보이는 대꼬치와 음식 조각.</summary>
        private static void Skewer(Transform pivot, string name, float height, bool low, Color[] foods)
        {
            float length = PlateRadius * 2f + 0.4f;
            var bar = new GameObject(name);
            bar.transform.SetParent(pivot, false); // KnockHazard.Awake가 부모 Rotator를 찾게 먼저 붙여요
            bar.transform.localPosition = new Vector3(0f, height, 0f);
            var box = bar.AddComponent<BoxCollider>();
            box.size = new Vector3(0.42f, 0.42f, length);
            var hazard = bar.AddComponent<KnockHazard>();
            hazard.Box = box;
            hazard.Low = low;
            hazard.KnockHorizontal = 10f;
            hazard.KnockUp = 4.8f;

            Material stick = ProtoMaterials.Opaque(new Color(0.86f, 0.74f, 0.52f), 0.35f);
            ProtoMaterials.VisualPart(bar.transform, "Stick", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.09f, length * 0.5f, 0.09f), Quaternion.Euler(90f, 0f, 0f), stick);
            int i = 0;
            for (float d = 1.2f; d < PlateRadius; d += 1.1f)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Color c = foods[i % foods.Length];
                    Material m = ProtoMaterials.Opaque(c, 0.6f);
                    bool round = i % 3 == 2;
                    ProtoMaterials.VisualPart(bar.transform, "Food", round ? PrimitiveType.Sphere : PrimitiveType.Cube,
                        new Vector3(0f, 0f, s * d), round ? Vector3.one * 0.44f : new Vector3(0.38f, 0.38f, 0.34f),
                        Quaternion.Euler(0f, 0f, round ? 0f : 45f * (i % 2)), m);
                    i++;
                }
            }
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            const int Slots = SpawnSlots;
            float ang = (index % Slots) * 360f / Slots + (index / Slots) * 22.5f;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            yaw = ang + 180f; // 가운데를 보게
            return transform.TransformPoint(dir * (PlateRadius - 2.6f) + Vector3.up * 0.05f);
        }

        protected override void OnRegistered(Runner r)
        {
            r.StartTime = Time.time;
        }

        protected override void OnFell(Runner r)
        {
            float survived = Time.time - r.StartTime;
            if (r.Best < 0f || survived > r.Best)
            {
                r.Best = survived;
            }
            if (r.Ch == Player)
            {
                _lastSurvive = survived;
                if (_bestSurvive < 0f || survived > _bestSurvive)
                {
                    _bestSurvive = survived;
                }
            }
            // 다시 올라올 자리: 처음 자리
            float yaw;
            r.RespawnPos = SpawnPoint(r.SpawnIndex, out yaw);
            r.RespawnYaw = yaw;
        }

        protected override void OnRespawned(Runner r)
        {
            r.StartTime = Time.time;
            if (r.Ch == Player)
            {
                // 플레이어가 다시 올라오면 판 다시 시작 — 버틴 시간을 같은 조건에서 비교
                ResetSkewers();
            }
        }

        /// <summary>꼬치를 처음 속도로, 출발 자리 사이 각도에 놓고 잠깐 멈춰요 (판 다시 시작).</summary>
        public void ResetSkewers()
        {
            // 출발 자리(45° 간격) 사이 = 22.5° 에 두면 멈춰 있는 동안 아무도 안 걸려요
            float between = 180f / SpawnSlots;
            ResetSkewer(_low, LowStartSpeed, between);
            ResetSkewer(_high, HighStartSpeed, between + 90f);
        }

        private static void ResetSkewer(Rotator skewer, float speed, float angle)
        {
            if (skewer == null)
            {
                return;
            }
            skewer.SpeedDeg = speed;
            skewer.SetAngle(angle);
            skewer.Hold(StartHoldSeconds);
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
            string best = _bestSurvive >= 0f ? string.Format(" · 최고 {0:0.0}초", _bestSurvive) : "";
            float low = _low != null ? Mathf.Abs(_low.SpeedDeg) : 0f;
            float high = _high != null ? Mathf.Abs(_high.SpeedDeg) : 0f;
            return string.Format("{0}\n버틴 시간 {1:0.0}초{2} · 접시 위 {3}/{4}\n꼬치 속도 낮은 {5:0}°/s · 높은 {6:0}°/s",
                Title, survive, best, AliveCount(), Runners.Count, low, high);
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
    }
}
