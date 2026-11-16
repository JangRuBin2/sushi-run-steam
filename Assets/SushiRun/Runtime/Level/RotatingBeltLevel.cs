// Race 회색 박스 — "회전 벨트" (로블록스판 rotating-belt를 미터로 옮겨 다시 짠 시제품 코스).
// 출발 → 도는 접시 2개(두 번째는 위에서 젓가락이 휘두름) → 체크포인트 1 → 거꾸로 도는 벨트 + 젓가락 2개
// → 체크포인트 2 → 작은 접시 3개 → 체크포인트 3 → 젓가락 하나 → 결승. 약 125 m, +Z 방향.
// 떨어지면 3초 뒤 마지막 체크포인트에서 부활 (로블록스 Race 부활과 같은 흐름).
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class RotatingBeltLevel : PrototypeLevel
    {
        private const float StartZ = 1f;
        private const float FinishZ = 121f;

        private float _bestTime = -1f;

        public override string Title => "회전 벨트 (Race)";

        public override void Build()
        {
            KillY = -10f;
            BotEdgeJumps = true;
            Transform root = transform;

            GreyboxKit.KitchenBackdrop(root, new Vector3(0f, 0f, 60f), -40f);

            // 1. 출발대 + 첫 길
            Pad(root, "StartPad", new Vector3(0f, 0f, 3f), new Vector2(12f, 10f));
            Pad(root, "WalkA", new Vector3(0f, 0f, 12f), new Vector2(6f, 8f));

            // 2. 도는 접시 2개 (두 번째는 반대로 돌고, 위에서 젓가락이 휘둘러요)
            GreyboxKit.RotatingPlate(root, "Plate1", new Vector3(0f, 0f, 22.8f), 5f, 22f, new Color(1f, 0.62f, 0.36f));
            GreyboxKit.RotatingPlate(root, "Plate2", new Vector3(0f, 0f, 35.3f), 5.5f, -28f, new Color(0.45f, 0.75f, 0.55f));
            GreyboxKit.Sweeper(root, "Plate2Chopsticks", new Vector3(0f, 0f, 35.3f), 10f, 0.45f, 60f, true);

            // 3. 체크포인트 1
            CheckpointPad(root, 0, new Vector3(0f, 0f, 45.6f), new Vector2(9f, 6f));

            // 4. 거꾸로 도는 벨트 (뒤로 2.8 m/s) + 휘두르는 젓가락 2개
            GameObject belt = GreyboxKit.Box(root, "Belt", new Vector3(0f, -0.5f, 56.6f), new Vector3(6f, 1f, 16f), GreyboxKit.Belt, 0.35f);
            var conveyor = belt.AddComponent<ConveyorSurface>();
            conveyor.LocalDirection = Vector3.back;
            conveyor.Speed = 2.8f;
            // 벨트 상자는 스케일이 (6,1,16)이라, 줄무늬는 스케일 1인 형제 오브젝트에 두고 벨트와 같은 속도로 흘려요
            var stripeRoot = new GameObject("BeltStripes");
            stripeRoot.transform.SetParent(root, false);
            stripeRoot.transform.localPosition = new Vector3(0f, 0.004f, 56.6f);
            var stripes = stripeRoot.AddComponent<ConveyorSurface>();
            stripes.LocalDirection = Vector3.back;
            stripes.Speed = 2.8f;
            stripes.StripeSpan = 16f;
            Material stripeMat = ProtoMaterials.Opaque(GreyboxKit.BeltStripe, 0.4f);
            for (int i = 0; i < 8; i++)
            {
                GameObject s = ProtoMaterials.VisualPart(stripeRoot.transform, "Stripe", PrimitiveType.Cube,
                    new Vector3(0f, 0f, -8f + i * 2f + 1f), new Vector3(5.6f, 0.008f, 0.35f), Quaternion.identity, stripeMat, false);
                stripes.Stripes.Add(s.transform);
            }
            GreyboxKit.Sweeper(root, "BeltChopsticksA", new Vector3(0f, 0f, 52.5f), 6.6f, 0.45f, 75f, true);
            GreyboxKit.Sweeper(root, "BeltChopsticksB", new Vector3(0f, 0f, 60f), 6.6f, 0.45f, -85f, true);

            // 5. 체크포인트 2
            CheckpointPad(root, 1, new Vector3(0f, 0f, 67.6f), new Vector2(9f, 6f));

            // 6. 작은 접시 3개 (좌우로 엇갈림, 점점 빨리)
            GreyboxKit.RotatingPlate(root, "SmallPlate1", new Vector3(-1.4f, 0f, 75.5f), 3.3f, 45f, new Color(0.95f, 0.45f, 0.45f));
            GreyboxKit.RotatingPlate(root, "SmallPlate2", new Vector3(1.4f, 0f, 83.8f), 3.3f, -55f, new Color(0.45f, 0.6f, 0.95f));
            GreyboxKit.RotatingPlate(root, "SmallPlate3", new Vector3(-1.0f, 0f, 92.1f), 3.3f, 65f, new Color(0.95f, 0.8f, 0.35f));

            // 7. 체크포인트 3 + 마지막 젓가락
            CheckpointPad(root, 2, new Vector3(0f, 0f, 100.1f), new Vector2(8f, 6f));
            Pad(root, "WalkC", new Vector3(0f, 0f, 108.1f), new Vector2(7f, 10f));
            GreyboxKit.Sweeper(root, "LastChopsticks", new Vector3(0f, 0f, 108.1f), 7.2f, 0.45f, 95f, true);

            // 8. 결승대 + 문
            Pad(root, "FinishPad", new Vector3(0f, 0f, 119.9f), new Vector2(12f, 10f));
            FinishGate(root, FinishZ);

            BuildBotPath();
        }

        private void BuildBotPath()
        {
            BotPath.Clear();
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 6f), 1.5f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 15.3f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 22.8f), 1.8f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 27.2f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(1.8f, 0f, 35.3f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 40.3f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 45.6f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(1.6f, 0f, 52.5f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(-1.6f, 0f, 60f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 67.6f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(-0.6f, 0f, 70.0f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(-1.4f, 0f, 75.5f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(-0.45f, 0f, 78.3f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(1.4f, 0f, 83.8f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(0.55f, 0f, 86.7f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(-1.0f, 0f, 92.1f), 1.4f));
            BotPath.Add(new BotWaypoint(new Vector3(-0.6f, 0f, 95.0f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 100.1f), 1.6f));
            BotPath.Add(new BotWaypoint(new Vector3(1.8f, 0f, 108.1f), 1.5f));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, 112.6f), 1.0f, true));
            BotPath.Add(new BotWaypoint(new Vector3(0f, 0f, FinishZ + 1.5f), 1.5f));
        }

        public override Vector3 SpawnPoint(int index, out float yaw)
        {
            yaw = 0f;
            int col = index % 3;
            int row = index / 3;
            return transform.TransformPoint(new Vector3(-2.6f + 2.6f * col, 0.05f, StartZ + 2.2f * (row % 3)));
        }

        /// <summary>윗면 = topCenter.y인 바닥 + 아래 테두리 (장식).</summary>
        private static void Pad(Transform root, string name, Vector3 topCenter, Vector2 sizeXZ)
        {
            GreyboxKit.Box(root, name, topCenter + Vector3.down * 0.5f, new Vector3(sizeXZ.x, 1f, sizeXZ.y), GreyboxKit.Floor, 0.3f);
            GreyboxKit.Box(root, name + "Trim", topCenter + Vector3.down * 1.1f, new Vector3(sizeXZ.x + 0.3f, 0.4f, sizeXZ.y + 0.3f), GreyboxKit.FloorEdge, 0.3f, false);
        }

        private void CheckpointPad(Transform root, int index, Vector3 topCenter, Vector2 sizeXZ)
        {
            Pad(root, "Checkpoint" + index + "Pad", topCenter, sizeXZ);
            // 들어오는 쪽 초록 띠 + 양옆 깃대
            float entryZ = topCenter.z - sizeXZ.y * 0.5f + 0.5f;
            GreyboxKit.Box(root, "CheckpointStripe", new Vector3(topCenter.x, topCenter.y + 0.005f, entryZ), new Vector3(sizeXZ.x, 0.01f, 0.6f), GreyboxKit.CheckpointColor, 0.4f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pole = new Vector3(topCenter.x + s * (sizeXZ.x * 0.5f - 0.3f), topCenter.y, entryZ);
                GreyboxKit.Box(root, "Pole", pole + Vector3.up * 1.1f, new Vector3(0.12f, 2.2f, 0.12f), GreyboxKit.WoodDark, 0.4f, false);
                GreyboxKit.Box(root, "Flag", pole + new Vector3(-s * 0.35f, 1.9f, 0f), new Vector3(0.6f, 0.4f, 0.04f), GreyboxKit.CheckpointColor, 0.4f, false);
            }
            Vector3 respawn = new Vector3(topCenter.x, topCenter.y + 0.05f, topCenter.z);
            GreyboxKit.MakeCheckpoint(this, root, index, topCenter + Vector3.up * 1.5f, new Vector3(sizeXZ.x, 3f, sizeXZ.y), respawn, 0f);
        }

        private void FinishGate(Transform root, float z)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                GreyboxKit.Box(root, "GatePillar", new Vector3(s * 5.2f, 2.25f, z), new Vector3(0.6f, 4.5f, 0.6f), GreyboxKit.Lacquer, 0.55f);
            }
            GreyboxKit.Box(root, "GateBeam", new Vector3(0f, 4.7f, z), new Vector3(12f, 0.5f, 0.8f), GreyboxKit.Lacquer, 0.55f);
            GreyboxKit.Box(root, "GateBeamTop", new Vector3(0f, 5.15f, z), new Vector3(12.8f, 0.3f, 1.0f), GreyboxKit.WoodDark, 0.4f);
            // 노렌(가게 천) 조각 — 장식만
            Material cloth = ProtoMaterials.Opaque(new Color(0.16f, 0.24f, 0.45f), 0.2f);
            for (int i = 0; i < 6; i++)
            {
                ProtoMaterials.VisualPart(root, "Noren", PrimitiveType.Cube, new Vector3(-4.2f + i * 1.68f, 3.9f, z),
                    new Vector3(1.5f, 1.1f, 0.04f), Quaternion.identity, cloth, false);
            }
            GreyboxKit.Box(root, "FinishLine", new Vector3(0f, 0.005f, z), new Vector3(10f, 0.01f, 0.5f), GreyboxKit.FinishColor, 0.4f, false);
            GameObject trigger = GreyboxKit.Trigger(root, "FinishTrigger", new Vector3(0f, 1.5f, z), new Vector3(10f, 3f, 1f));
            trigger.AddComponent<FinishLine>().Level = this;
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
