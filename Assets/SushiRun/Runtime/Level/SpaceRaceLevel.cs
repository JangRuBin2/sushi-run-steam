// Race 맵 — "우주 회전초밥 레이스" (콘셉트 맵 시트 2번). 우주에 떠 있는 회전초밥 레일 위를 달려요.
// 출발 정거장 → 앞으로 흐르는 회전초밥 벨트 → 빙글빙글 도는 레일 3개(줄이 맞을 때 건너기, 사이에 체크포인트 1)
// → 체크포인트 2 → 운석이 떨어지는 길(경고 원 → 충돌 → 잠깐 바위로 남음) → 체크포인트 3 → 거꾸로 흐르는 벨트 → 결승.
// 약 130 m, +Z 방향. 떨어지면 3초 뒤 마지막 체크포인트에서 부활. 텍스처는 Codex로 뽑은 Resources/Space/.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class SpaceRaceLevel : PrototypeLevel
    {
        private const float StartZ = 1f;
        private const float FinishZ = 126f;
        private const float RailLength = 11.6f;

        private static readonly Color NeonCyan = new Color(0.30f, 0.95f, 1f);
        private static readonly Color NeonPink = new Color(1f, 0.40f, 0.85f);
        private static readonly Color SpaceDark = new Color(0.07f, 0.05f, 0.16f);

        private readonly List<Material> _owned = new List<Material>();
        private float _bestTime = -1f;
        private Color _oldCamBg;
        private Camera _cam;

        public override string Title => "우주 회전초밥 레이스 (Race)";

        public override void Build()
        {
            KillY = -6f;
            BotEdgeJumps = true;
            Transform root = transform;
            SetupSpaceLighting();
            Backdrop(root);
            Decor(root);

            Material panel = SinkKit.TexMat("Space/station_panel", 0.25f, Color.white, 0.6f, 0.2f);

            // 1. 출발 정거장 + 앞으로 흐르는 벨트
            Pad(root, "StartPad", new Vector3(0f, 0f, 3f), new Vector2(12f, 10f), panel);
            Belt(root, "BeltForward", new Vector3(0f, 0f, 18f), new Vector2(4.5f, 20f), 2f);

            // 2. 도는 레일 3개 (가운데 축으로 돌아요 — 줄이 맞을 때 건너요). 사이 발판 + 체크포인트 1
            Pad(root, "PadA", new Vector3(0f, 0f, 30f), new Vector2(6f, 4f), panel);
            SpinRail(root, "Rail1", 38f, 60f);
            CheckpointPad(root, 0, new Vector3(0f, 0f, 46.5f), new Vector2(8f, 5f), panel);
            SpinRail(root, "Rail2", 55f, -60f);
            Pad(root, "PadC", new Vector3(0f, 0f, 62.5f), new Vector2(6f, 3f), panel);
            SpinRail(root, "Rail3", 70f, 75f);

            // 3. 체크포인트 2 → 운석이 떨어지는 길
            CheckpointPad(root, 1, new Vector3(0f, 0f, 78.5f), new Vector2(8f, 5f), panel);
            Pad(root, "MeteorRoad", new Vector3(0f, 0f, 94.5f), new Vector2(8f, 27f), panel);
            NeonEdges(root, new Vector3(0f, 0f, 94.5f), new Vector2(8f, 27f));
            var showerGo = new GameObject("MeteorShower");
            showerGo.transform.SetParent(root, false);
            var shower = showerGo.AddComponent<MeteorShower>();
            shower.AreaCenter = transform.TransformPoint(new Vector3(0f, 0f, 94.5f));
            shower.AreaSize = new Vector2(7f, 26f);
            shower.MeteorMat = Glow(SinkKit.TexMat("Space/meteor_lava", 0.5f, Color.white, 0.3f), new Color(1f, 0.35f, 0.1f), 0.8f);
            shower.RockMat = SinkKit.TexMat("Space/asteroid_rock", 0.6f, Color.white, 0.2f);
            Material warn = ProtoMaterials.Transparent(new Color(1f, 0.15f, 0.1f, 0.45f));
            _owned.Add(warn);
            shower.WarnMat = warn;

            // 4. 체크포인트 3 → 거꾸로 흐르는 벨트 → 결승
            CheckpointPad(root, 2, new Vector3(0f, 0f, 110.5f), new Vector2(8f, 5f), panel);
            Belt(root, "BeltBackward", new Vector3(0f, 0f, 117f), new Vector2(5f, 8f), -2.2f);
            Pad(root, "FinishPad", new Vector3(0f, 0f, 126f), new Vector2(12f, 10f), panel);
            FinishGate(root, FinishZ);

            BuildBotPath();
        }

        private void BuildBotPath()
        {
            BotPath.Clear();
            float[] zs = { 6f, 26f, 30f, 38f, 46.5f, 55f, 62.5f, 70f, 78.5f, 94f, 110.5f, 117f, FinishZ + 1.5f };
            for (int i = 0; i < zs.Length; i++)
            {
                BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, zs[i]), 1.4f));
            }
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            yaw = 0f;
            int col = index % 3;
            int row = index / 3;
            return transform.TransformPoint(new Vector3(-2.6f + 2.6f * col, 0.05f, StartZ + 2.2f * (row % 3)));
        }

        // ---------- 조각 ----------

        private void Pad(Transform root, string name, Vector3 topCenter, Vector2 sizeXZ, Material mat)
        {
            SinkKit.Slab(root, name, topCenter, new Vector3(sizeXZ.x, 0.8f, sizeXZ.y), 0.3f, mat);
            SinkKit.Part(root, name + "Under", MeshKit.RoundedBox(new Vector3(sizeXZ.x * 0.7f, 1.2f, sizeXZ.y * 0.7f), 0.4f),
                topCenter + Vector3.down * 1.3f, Quaternion.identity, SinkKit.Mat(new Color(0.35f, 0.30f, 0.55f), 0.6f, 0.4f));
            // 아래 추진 불빛
            SinkKit.Part(root, name + "Thruster", MeshKit.RoundedBox(new Vector3(sizeXZ.x * 0.4f, 0.2f, sizeXZ.y * 0.4f), 0.09f),
                topCenter + Vector3.down * 1.95f, Quaternion.identity, Glow(SinkKit.Mat(NeonCyan, 0.5f), NeonCyan, 1.5f), false);
        }

        /// <summary>회전초밥 벨트 (윗면 = topCenter). speed > 0 이면 +Z로 실어 가요.</summary>
        private void Belt(Transform root, string name, Vector3 topCenter, Vector2 sizeXZ, float speed)
        {
            const float tiling = 0.5f;
            GameObject belt = SinkKit.Slab(root, name, topCenter, new Vector3(sizeXZ.x, 0.6f, sizeXZ.y), 0.2f, SinkKit.TexMat("Space/kaiten_belt", tiling, Color.white, 0.55f));
            var conveyor = belt.AddComponent<ConveyorSurface>();
            conveyor.LocalDirection = speed >= 0f ? Vector3.forward : Vector3.back;
            conveyor.Speed = Mathf.Abs(speed);
            belt.AddComponent<TextureScroller>().Speed = new Vector2(-speed * tiling, 0f); // UV u = z (RoundedBox 윗면)
            // 회전초밥 레일 옆 턱 (빛나는 띠, 장식)
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, name + "Edge", MeshKit.RoundedBox(new Vector3(0.35f, 0.5f, sizeXZ.y), 0.15f),
                    topCenter + new Vector3(s * (sizeXZ.x * 0.5f + 0.18f), -0.1f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.85f, 0.82f, 0.95f), 0.7f, 0.3f));
                SinkKit.Part(root, name + "Neon", MeshKit.RoundedBox(new Vector3(0.1f, 0.1f, sizeXZ.y), 0.04f),
                    topCenter + new Vector3(s * (sizeXZ.x * 0.5f + 0.18f), 0.17f, 0f), Quaternion.identity, Glow(SinkKit.Mat(NeonPink, 0.5f), NeonPink, 2f), false);
            }
        }

        /// <summary>가운데 축으로 도는 레일 (줄이 맞으면 앞뒤 발판과 이어져요). 위에 탄 캐릭터는 같이 돌아요.</summary>
        private void SpinRail(Transform root, string name, float centerZ, float speedDeg)
        {
            Rotator pivot = GreyboxKit.MakeRotator(root, name, new Vector3(0f, 0f, centerZ), speedDeg);
            Transform p = pivot.transform;
            SinkKit.Slab(p, "Rail", Vector3.zero, new Vector3(2.4f, 0.6f, RailLength), 0.2f, SinkKit.TexMat("Space/kaiten_belt", 0.5f, Color.white, 0.55f));
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(p, "Neon", MeshKit.RoundedBox(new Vector3(0.1f, 0.1f, RailLength - 0.4f), 0.04f), new Vector3(s * 1.25f, 0.02f, 0f), Quaternion.identity,
                    Glow(SinkKit.Mat(NeonCyan, 0.5f), NeonCyan, 2f), false);
            }
            // 가운데 축 (도는 게 보이게)
            SinkKit.Part(p, "Hub", MeshKit.Lathe(new[] { new Vector2(0f, 0.08f), new Vector2(0.7f, 0.08f), new Vector2(0.8f, 0f), new Vector2(0f, 0f) }, 32),
                Vector3.zero, Quaternion.identity, Glow(SinkKit.Mat(NeonPink, 0.6f), NeonPink, 1.2f), false);
            SinkKit.Part(p, "Spindle", MeshKit.RoundedBox(new Vector3(0.8f, 6f, 0.8f), 0.35f), new Vector3(0f, -3.6f, 0f), Quaternion.identity, SinkKit.Mat(new Color(0.35f, 0.30f, 0.55f), 0.6f, 0.4f));
        }

        private void NeonEdges(Transform root, Vector3 topCenter, Vector2 sizeXZ)
        {
            Material neon = Glow(SinkKit.Mat(NeonCyan, 0.5f), NeonCyan, 2f);
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, "RoadNeon", MeshKit.RoundedBox(new Vector3(0.12f, 0.12f, sizeXZ.y - 0.6f), 0.05f),
                    topCenter + new Vector3(s * (sizeXZ.x * 0.5f - 0.15f), 0.03f, 0f), Quaternion.identity, neon, false);
            }
        }

        private void CheckpointPad(Transform root, int index, Vector3 topCenter, Vector2 sizeXZ, Material panel)
        {
            Pad(root, "Checkpoint" + index + "Pad", topCenter, sizeXZ, panel);
            float entryZ = topCenter.z - sizeXZ.y * 0.5f + 0.5f;
            SinkKit.Part(root, "CheckpointStripe", MeshKit.RoundedBox(new Vector3(sizeXZ.x - 0.4f, 0.04f, 0.6f), 0.02f), new Vector3(topCenter.x, topCenter.y + 0.01f, entryZ),
                Quaternion.identity, Glow(SinkKit.Mat(GreyboxKit.CheckpointColor, 0.5f), GreyboxKit.CheckpointColor, 1.2f), false);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pole = new Vector3(topCenter.x + s * (sizeXZ.x * 0.5f - 0.4f), topCenter.y, entryZ);
                SinkKit.Part(root, "Pole", MeshKit.RoundedBox(new Vector3(0.16f, 2.4f, 0.16f), 0.07f), pole + Vector3.up * 1.2f, Quaternion.identity, SinkKit.Mat(new Color(0.85f, 0.82f, 0.95f), 0.7f, 0.3f));
                SinkKit.Part(root, "Beacon", MeshKit.RoundedBox(new Vector3(0.4f, 0.4f, 0.4f), 0.19f), pole + Vector3.up * 2.55f, Quaternion.identity,
                    Glow(SinkKit.Mat(GreyboxKit.CheckpointColor, 0.5f), GreyboxKit.CheckpointColor, 2f), false);
            }
            Vector3 respawn = new Vector3(topCenter.x, topCenter.y + 0.05f, topCenter.z);
            GreyboxKit.MakeCheckpoint(this, root, index, topCenter + Vector3.up * 1.5f, new Vector3(sizeXZ.x, 3f, sizeXZ.y), respawn, 0f);
        }

        private void FinishGate(Transform root, float z)
        {
            Material frame = SinkKit.Mat(new Color(0.85f, 0.82f, 0.95f), 0.75f, 0.3f);
            Material neon = Glow(SinkKit.Mat(NeonPink, 0.5f), NeonPink, 2.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, "GatePillar", MeshKit.RoundedBox(new Vector3(0.7f, 5f, 0.7f), 0.3f), new Vector3(s * 5.2f, 2.5f, z), Quaternion.identity, frame);
                SinkKit.Part(root, "GatePillarNeon", MeshKit.RoundedBox(new Vector3(0.15f, 4.4f, 0.15f), 0.06f), new Vector3(s * 4.82f, 2.5f, z), Quaternion.identity, neon, false);
            }
            SinkKit.Part(root, "GateBeam", MeshKit.RoundedBox(new Vector3(11.2f, 0.8f, 0.8f), 0.35f), new Vector3(0f, 5.2f, z), Quaternion.identity, frame);
            SinkKit.Part(root, "GateBeamNeon", MeshKit.RoundedBox(new Vector3(9.6f, 0.15f, 0.15f), 0.06f), new Vector3(0f, 4.75f, z - 0.3f), Quaternion.identity, neon, false);
            SinkKit.Part(root, "FinishLine", MeshKit.RoundedBox(new Vector3(10f, 0.03f, 0.5f), 0.01f), new Vector3(0f, 0.01f, z), Quaternion.identity,
                Glow(SinkKit.Mat(GreyboxKit.FinishColor, 0.5f), GreyboxKit.FinishColor, 1.2f), false);
            GameObject trigger = GreyboxKit.Trigger(root, "FinishTrigger", new Vector3(0f, 1.5f, z), new Vector3(10f, 3f, 1f));
            trigger.AddComponent<FinishLine>().Level = this;
        }

        /// <summary>둘레 우주 그림 (빛 안 받는 재질, 앞·뒤·양옆).</summary>
        private void Backdrop(Transform root)
        {
            var tex = Resources.Load<Texture2D>("Space/space_backdrop");
            if (tex == null)
            {
                return;
            }
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(unlit != null ? unlit : Shader.Find("Unlit/Texture"));
            m.name = "Space_Backdrop";
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
            }
            m.mainTexture = tex;
            _owned.Add(m);
            const float d = 210f;
            Vector3 c = new Vector3(0f, 20f, 65f);
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 90f, 0f);
                ProtoMaterials.VisualPart(root, "SpaceBackdrop", PrimitiveType.Quad, c + q * new Vector3(0f, 0f, d), new Vector3(2f * d * 1.15f, d * 1.3f, 1f), q, m, false);
            }
            // 아래도 우주 (떨어질 때 바닥이 검게 비지 않게) — 위를 보게 눕힌 같은 그림
            ProtoMaterials.VisualPart(root, "SpaceBackdropBelow", PrimitiveType.Quad, c + Vector3.down * 110f, new Vector3(2f * d * 1.2f, 2f * d * 1.2f, 1f), Quaternion.Euler(90f, 0f, 0f), m, false);
        }

        /// <summary>떠 있는 소행성·행성 (장식).</summary>
        private void Decor(Transform root)
        {
            Material rock = SinkKit.TexMat("Space/asteroid_rock", 0.35f, Color.white, 0.2f);
            var rnd = new System.Random(7);
            for (int i = 0; i < 22; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (12f + (float)rnd.NextDouble() * 40f);
                float y = -14f + (float)rnd.NextDouble() * 26f;
                float z = -10f + (float)rnd.NextDouble() * 150f;
                float s = 1.5f + (float)rnd.NextDouble() * 5f;
                ProtoMaterials.VisualPart(root, "Asteroid", PrimitiveType.Sphere, new Vector3(x, y, z), new Vector3(s, s * 0.8f, s * 0.9f),
                    Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, 0f), rock, false);
            }
            // 고리 행성 (오른쪽 위)
            Vector3 planet = new Vector3(70f, 35f, 120f);
            ProtoMaterials.VisualPart(root, "Planet", PrimitiveType.Sphere, planet, Vector3.one * 34f, Quaternion.identity, SinkKit.Mat(new Color(0.98f, 0.62f, 0.45f), 0.4f), false);
            SinkKit.Ring(root, "PlanetRing", 22f, 30f, 0f, SinkKit.Mat(new Color(1f, 0.86f, 0.72f), 0.4f));
            Transform ring = root.Find("PlanetRing");
            if (ring != null)
            {
                ring.localPosition = planet;
                ring.localRotation = Quaternion.Euler(18f, 0f, 12f);
            }
        }

        private Material Glow(Material baseMat, Color emission, float intensity)
        {
            var m = new Material(baseMat);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", emission * intensity);
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            _owned.Add(m);
            return m;
        }

        private void SetupSpaceLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1f, 0.96f, 0.98f);
                sun.intensity = 1.3f;
                sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.55f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.40f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.16f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SpaceDark;
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 520f;
            _cam = Camera.main;
            if (_cam == null)
            {
                _cam = Object.FindAnyObjectByType<Camera>();
            }
            if (_cam != null)
            {
                _oldCamBg = _cam.backgroundColor;
                _cam.backgroundColor = SpaceDark;
            }
        }

        private void OnDestroy()
        {
            GreyboxKit.SetupLighting(RenderSettings.sun); // 다른 맵 조명으로 되돌려요
            if (_cam != null)
            {
                _cam.backgroundColor = _oldCamBg;
            }
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Destroy(_owned[i]);
                }
            }
            _owned.Clear();
        }

        // ---------- 레이스 규칙 (회전 벨트·설거지통과 같음) ----------

        protected override void OnRegistered(Runner r)
        {
            r.StartTime = Time.time;
        }

        public override void ReachFinish(SushiCharacter ch)
        {
            Runner r = Find(ch);
            if (r == null || r.Finished || ch.IsDespawned || r.RespawnAt >= 0f)
            {
                return;
            }
            r.Finished = true;
            r.FinishTime = Time.time - r.StartTime;
            if (r.Best < 0f || r.FinishTime < r.Best)
            {
                r.Best = r.FinishTime;
            }
            if (_bestTime < 0f || r.FinishTime < _bestTime)
            {
                _bestTime = r.FinishTime;
            }
            float yaw;
            r.RespawnPos = SpawnPoint(r.SpawnIndex, out yaw);
            r.RespawnYaw = yaw;
            r.Checkpoint = -1;
            ScheduleRespawn(r, ch == Player ? 3.5f : 2f, false);
        }

        protected override void OnRespawned(Runner r)
        {
            if (r.Finished)
            {
                r.Finished = false;
                r.StartTime = Time.time;
            }
        }

        public override string StatusText(SushiCharacter player)
        {
            Runner r = Find(player);
            if (r == null)
            {
                return Title;
            }
            float t = r.Finished ? r.FinishTime : Time.time - r.StartTime;
            string best = r.Best >= 0f ? string.Format(" · 내 최고 {0:0.0}초", r.Best) : "";
            string all = _bestTime >= 0f ? string.Format(" · 전체 최고 {0:0.0}초", _bestTime) : "";
            return string.Format("{0}\n기록 {1:0.0}초 · 체크포인트 {2}/3{3}{4}", Title, t, r.Checkpoint + 1, best, all);
        }

        public override string CenterText(SushiCharacter player)
        {
            Runner r = Find(player);
            if (r != null && r.Finished)
            {
                return string.Format("완주! {0:0.00}초", r.FinishTime);
            }
            return base.CenterText(player);
        }
    }
}
