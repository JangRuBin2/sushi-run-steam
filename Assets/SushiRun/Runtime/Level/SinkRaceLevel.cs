// Race 맵 — "깨끗하게! 설거지통 레이스" (콘셉트 맵 시트 1번). 거대한 스테인리스 싱크대 안, 비눗물 위를 달려요.
// 출발 도마 → 도는 접시 3개(마지막 접시는 위에서 솔이 휘두름) → 체크포인트 1 → 가운데 칸막이 위, 좌우로 미는 스펀지 3개
// → 체크포인트 2 → 물에 떠서 출렁이는 컵 3개 → 체크포인트 3 → 솔 하나 → 결승 식기 건조대. 약 120 m, +Z 방향.
// 물에 빠지면 3초 뒤 마지막 체크포인트에서 부활. 메시는 MeshKit/SinkKit (최종 아트 전 1차 아트).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class SinkRaceLevel : PrototypeLevel
    {
        private const float StartZ = 1f;
        private const float FinishZ = 119f;
        private const float WaterY = -1.2f;

        private readonly List<Material> _owned = new List<Material>();
        private float _bestTime = -1f;

        public override string Title => "설거지통 레이스 (Race)";

        public override void Build()
        {
            KillY = -3f;
            BotEdgeJumps = true;
            Transform root = transform;
            SinkKit.SetupLighting();
            SinkKit.SinkBasin(root, WaterY, 125f, _owned);

            // 1. 출발 도마
            Material board = SinkKit.TexMat("wood_board", 0.15f, Color.white, 0.35f);
            SinkKit.Slab(root, "StartBoard", new Vector3(0f, 0f, 3f), new Vector3(12f, 0.8f, 10f), 0.3f, board);
            Material groove = SinkKit.Mat(new Color(0.66f, 0.48f, 0.30f), 0.3f);
            ProtoMaterials.VisualPart(root, "BoardGroove", PrimitiveType.Cube, new Vector3(0f, 0.003f, -1.4f), new Vector3(10.6f, 0.006f, 0.25f), Quaternion.identity, groove, false);
            Legs(root, new Vector3(0f, -0.8f, 3f), new Vector2(10f, 8f));

            // 2. 도는 접시 3개 (세 번째 위에서 솔이 휘둘러요)
            SinkKit.SpinningPlate(root, "Plate1", new Vector3(0f, 0f, 13.5f), 4f, 24f);
            SinkKit.SpinningPlate(root, "Plate2", new Vector3(0f, 0f, 23.5f), 4f, -30f);
            SinkKit.SpinningPlate(root, "Plate3", new Vector3(0f, 0f, 33.5f), 4f, 20f);
            SinkKit.Brush(root, "Plate3Brush", new Vector3(0f, 0f, 33.5f), 7.6f, 60f);

            // 3. 체크포인트 1 (비누 받침)
            CheckpointPad(root, 0, new Vector3(0f, 0f, 42.5f), new Vector2(8f, 5f));
            SoapBar(root, new Vector3(-3f, 0f, 43.2f), new Color(1f, 0.70f, 0.80f));

            // 4. 가운데 칸막이 위 + 좌우로 미는 스펀지 3개
            Material steel = SinkKit.TexMat("steel_brushed", 0.25f, Color.white, 0.82f, 0.35f);
            SinkKit.Slab(root, "Divider", new Vector3(0f, 0f, 57f), new Vector3(4.5f, 1f, 24f), 0.25f, steel);
            SinkKit.Part(root, "DividerStem", MeshKit.RoundedBox(new Vector3(3.2f, 8f, 24f), 0.4f), new Vector3(0f, -5f, 57f), Quaternion.identity, SinkKit.TexMat("steel_brushed", 0.25f, SinkKit.SteelDark, 0.7f, 0.35f));
            SinkKit.Sponge(root, "Sponge1", new Vector3(0f, 0f, 50.5f), 4f, 7f, 0f);
            SinkKit.Sponge(root, "Sponge2", new Vector3(0f, 0f, 57f), 4f, 8f, 0.5f);
            SinkKit.Sponge(root, "Sponge3", new Vector3(0f, 0f, 63.5f), 4f, 6.5f, 0.25f);

            // 5. 체크포인트 2
            CheckpointPad(root, 1, new Vector3(0f, 0f, 72.5f), new Vector2(8f, 5f));

            // 6. 물에 떠서 출렁이는 컵 3개 (좌우로 엇갈림)
            SinkKit.BobbingCup(root, "Cup1", new Vector3(-1.2f, 0f, 78.5f), 1.6f, new Color(0.62f, 0.86f, 0.78f), 0f);
            SinkKit.BobbingCup(root, "Cup2", new Vector3(1.2f, 0f, 84.5f), 1.6f, new Color(0.98f, 0.72f, 0.74f), 0.33f);
            SinkKit.BobbingCup(root, "Cup3", new Vector3(-1.2f, 0f, 90.5f), 1.6f, new Color(0.98f, 0.88f, 0.62f), 0.66f);

            // 7. 체크포인트 3 + 마지막 솔
            CheckpointPad(root, 2, new Vector3(0f, 0f, 96.8f), new Vector2(8f, 5f));
            SinkKit.Slab(root, "DrainBoard", new Vector3(0f, 0f, 106.2f), new Vector3(7f, 0.8f, 13.8f), 0.25f, steel);
            Material ridge = SinkKit.TexMat("steel_brushed", 0.25f, SinkKit.SteelDark, 0.75f, 0.35f);
            for (int i = 0; i < 8; i++)
            {
                ProtoMaterials.VisualPart(root, "Ridge", PrimitiveType.Cube, new Vector3(-2.8f + i * 0.8f, 0.01f, 106.2f), new Vector3(0.08f, 0.02f, 13f), Quaternion.identity, ridge, false);
            }
            SinkKit.Brush(root, "LastBrush", new Vector3(0f, 0f, 106.5f), 7.2f, 80f);

            // 8. 결승 — 깨끗한 접시가 꽂힌 식기 건조대
            FinishRack(root, new Vector3(0f, 0f, 118f));

            BuildBotPath();
        }

        private void BuildBotPath()
        {
            BotPath.Clear();
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 6f), 1.5f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 7.3f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 13.5f), 1.8f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 16.8f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 23.5f), 1.8f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 26.8f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(1.8f, 0f, 33.5f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 36.8f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 42.5f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 52f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 60f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 67f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 72.5f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(-0.5f, 0f, 74.4f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(-1.2f, 0f, 78.5f), 1.0f));
            BotPath.Add(new BotWaypoint(new Vector3(-0.8f, 0f, 79.6f), 0.8f, true));
            BotPath.Add(new BotWaypoint(new Vector3(1.2f, 0f, 84.5f), 1.0f));
            BotPath.Add(new BotWaypoint(new Vector3(0.8f, 0f, 85.6f), 0.8f, true));
            BotPath.Add(new BotWaypoint(new Vector3(-1.2f, 0f, 90.5f), 1.0f));
            BotPath.Add(new BotWaypoint(new Vector3(-1.0f, 0f, 91.7f), 0.8f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 96.8f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(1.8f, 0f, 106.5f), 1.5f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, FinishZ + 1.5f), 1.5f));
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            yaw = 0f;
            int col = index % 3;
            int row = index / 3;
            return transform.TransformPoint(new Vector3(-2.6f + 2.6f * col, 0.05f, StartZ + 2.2f * (row % 3)));
        }

        /// <summary>도마·발판 아래 받침 다리 (물속으로, 장식).</summary>
        private static void Legs(Transform root, Vector3 top, Vector2 span)
        {
            Material m = SinkKit.TexMat("steel_brushed", 0.25f, SinkKit.SteelDark, 0.7f, 0.35f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    SinkKit.Part(root, "Leg", MeshKit.RoundedBox(new Vector3(0.6f, 8f, 0.6f), 0.25f),
                        top + new Vector3(sx * span.x * 0.5f, -4f, sz * span.y * 0.5f), Quaternion.identity, m);
                }
            }
        }

        private static void SoapBar(Transform root, Vector3 floor, Color color)
        {
            SinkKit.Part(root, "SoapBar", MeshKit.RoundedBox(new Vector3(1.4f, 0.55f, 0.9f), 0.25f), floor + Vector3.up * 0.27f,
                Quaternion.Euler(0f, 20f, 0f), SinkKit.Mat(color, 0.7f));
            SinkKit.FoamCluster(root, floor + new Vector3(0.4f, 0.55f, 0.1f), 0.45f, 7);
        }

        /// <summary>비누 받침 모양 체크포인트 발판 (윗면 = topCenter) + 초록 띠·깃대 + 부활 지점.</summary>
        private void CheckpointPad(Transform root, int index, Vector3 topCenter, Vector2 sizeXZ)
        {
            SinkKit.Slab(root, "Checkpoint" + index + "Pad", topCenter, new Vector3(sizeXZ.x, 0.8f, sizeXZ.y), 0.35f, SinkKit.Mat(SinkKit.CeramicBlue, 0.75f));
            SinkKit.Part(root, "Checkpoint" + index + "Lip", MeshKit.RoundedBox(new Vector3(sizeXZ.x + 0.3f, 0.3f, sizeXZ.y + 0.3f), 0.14f),
                topCenter + Vector3.down * 0.75f, Quaternion.identity, SinkKit.Mat(SinkKit.Ceramic, 0.8f), false);
            Legs(root, topCenter + Vector3.down * 0.8f, sizeXZ - new Vector2(1.5f, 1.5f));
            float entryZ = topCenter.z - sizeXZ.y * 0.5f + 0.5f;
            GreyboxKit.Box(root, "CheckpointStripe", new Vector3(topCenter.x, topCenter.y + 0.005f, entryZ), new Vector3(sizeXZ.x - 0.4f, 0.01f, 0.6f), GreyboxKit.CheckpointColor, 0.4f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pole = new Vector3(topCenter.x + s * (sizeXZ.x * 0.5f - 0.4f), topCenter.y, entryZ);
                SinkKit.Part(root, "Pole", MeshKit.RoundedBox(new Vector3(0.14f, 2.2f, 0.14f), 0.06f), pole + Vector3.up * 1.1f, Quaternion.identity, SinkKit.TexMat("steel_brushed", 0.25f, Color.white, 0.8f, 0.35f));
                SinkKit.Part(root, "Flag", MeshKit.RoundedBox(new Vector3(0.6f, 0.4f, 0.05f), 0.02f), pole + new Vector3(-s * 0.35f, 1.9f, 0f), Quaternion.identity, SinkKit.Mat(GreyboxKit.CheckpointColor, 0.4f));
            }
            Vector3 respawn = new Vector3(topCenter.x, topCenter.y + 0.05f, topCenter.z);
            GreyboxKit.MakeCheckpoint(this, root, index, topCenter + Vector3.up * 1.5f, new Vector3(sizeXZ.x, 3f, sizeXZ.y), respawn, 0f);
        }

        /// <summary>결승 발판(흰 쟁반) + 식기 건조대 문(스테인리스 철사) + 꽂힌 접시들.</summary>
        private void FinishRack(Transform root, Vector3 topCenter)
        {
            SinkKit.Slab(root, "FinishTray", topCenter, new Vector3(12f, 0.8f, 10f), 0.35f, SinkKit.Mat(SinkKit.Ceramic, 0.8f));
            Legs(root, topCenter + Vector3.down * 0.8f, new Vector2(10f, 8f));
            Material wire = SinkKit.TexMat("steel_brushed", 0.25f, Color.white, 0.85f, 0.35f);
            float z = FinishZ;
            for (int s = -1; s <= 1; s += 2)
            {
                SinkKit.Part(root, "GatePillar", MeshKit.RoundedBox(new Vector3(0.35f, 4.6f, 0.35f), 0.15f), new Vector3(s * 5.2f, 2.3f, z), Quaternion.identity, wire);
            }
            SinkKit.Part(root, "GateBeam", MeshKit.RoundedBox(new Vector3(10.8f, 0.35f, 0.35f), 0.15f), new Vector3(0f, 4.6f, z), Quaternion.identity, wire);
            for (int i = 0; i < 9; i++)
            {
                SinkKit.Part(root, "GateWire", MeshKit.RoundedBox(new Vector3(0.1f, 1.2f, 0.1f), 0.04f), new Vector3(-4f + i, 4.0f, z), Quaternion.identity, wire, false);
            }
            // 건조대에 꽂힌 깨끗한 접시 (양옆)
            Vector2[] dish =
            {
                new Vector2(0f, 0f), new Vector2(1.1f, 0f), new Vector2(1.35f, 0.12f), new Vector2(1.38f, 0.06f), new Vector2(1.2f, -0.08f), new Vector2(0f, -0.12f),
            };
            Mesh dishMesh = MeshKit.Lathe(dish, 40);
            Material dishMat = SinkKit.Mat(SinkKit.Ceramic, 0.9f);
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    SinkKit.Part(root, "RackDish", dishMesh, new Vector3(s * 4.8f, 1.45f, topCenter.z - 3.5f + i * 1.0f), Quaternion.Euler(90f, 0f, 0f), dishMat);
                }
            }
            GreyboxKit.Box(root, "FinishLine", new Vector3(0f, 0.005f, z), new Vector3(10f, 0.01f, 0.5f), GreyboxKit.FinishColor, 0.4f, false);
            GameObject trigger = GreyboxKit.Trigger(root, "FinishTrigger", new Vector3(0f, 1.5f, z), new Vector3(10f, 3f, 1f));
            trigger.AddComponent<FinishLine>().Level = this;
        }

        private void OnDestroy()
        {
            GreyboxKit.SetupLighting(RenderSettings.sun); // 다른 맵 조명으로 되돌려요
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Destroy(_owned[i]);
                }
            }
            _owned.Clear();
        }

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
            if (MatchFinish(r))
            {
                return; // 한 판 흐름: 완주하면 빠져요
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
            // 잠깐 기뻐한 뒤 출발점으로 (다시 달리기)
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
