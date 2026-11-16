// 회색 박스 도구: 상자·원기둥·회전 접시·휘두르는 젓가락·트리거, "거대한 주방" 배경, 조명.
// 판정 파츠는 콜라이더 있음, 장식은 콜라이더 없음 (ProtoMaterials.VisualPart).
using UnityEngine;
using UnityEngine.Rendering;

namespace SushiRun.Runtime
{
    public static class GreyboxKit
    {
        // 회색 박스 색 — 따뜻한 주방 톤 (판정 바닥은 밝고 깨끗하게, 배경은 탁하게)
        public static readonly Color Floor = new Color(0.86f, 0.84f, 0.80f);
        public static readonly Color FloorEdge = new Color(0.62f, 0.58f, 0.52f);
        public static readonly Color PlateWhite = new Color(0.96f, 0.95f, 0.92f);
        public static readonly Color PlateRim = new Color(0.20f, 0.33f, 0.55f);
        public static readonly Color Lacquer = new Color(0.62f, 0.12f, 0.10f);
        public static readonly Color Wood = new Color(0.64f, 0.44f, 0.27f);
        public static readonly Color WoodDark = new Color(0.42f, 0.26f, 0.15f);
        public static readonly Color Chopstick = new Color(0.78f, 0.58f, 0.36f);
        public static readonly Color ChopstickTip = new Color(0.30f, 0.17f, 0.10f);
        public static readonly Color CheckpointColor = new Color(0.36f, 0.78f, 0.52f);
        public static readonly Color FinishColor = new Color(0.95f, 0.30f, 0.26f);
        public static readonly Color Belt = new Color(0.30f, 0.30f, 0.33f);
        public static readonly Color BeltStripe = new Color(0.95f, 0.78f, 0.25f);
        public static readonly Color Sky = new Color(0.93f, 0.86f, 0.76f);

        /// <summary>콜라이더 있는 상자. size = 월드 크기 (m).</summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color, float smoothness = 0.25f, bool collider = true)
        {
            return Box(parent, name, center, size, Quaternion.identity, color, smoothness, collider);
        }

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation, Color color, float smoothness = 0.25f, bool collider = true)
        {
            GameObject go = ProtoMaterials.VisualPart(parent, name, PrimitiveType.Cube, center, size, rotation, ProtoMaterials.Opaque(color, smoothness));
            if (collider)
            {
                go.AddComponent<BoxCollider>(); // 크기 1 상자 × 스케일 = size
            }
            return go;
        }

        /// <summary>원기둥 (Y축). 콜라이더는 볼록 MeshCollider — 기본 원기둥의 캡슐 콜라이더는 납작한 원판에 안 맞아요.</summary>
        public static GameObject Cylinder(Transform parent, string name, Vector3 center, float diameter, float height, Color color, float smoothness = 0.3f, bool collider = true)
        {
            GameObject go = ProtoMaterials.VisualPart(parent, name, PrimitiveType.Cylinder, center,
                new Vector3(diameter, height * 0.5f, diameter), Quaternion.identity, ProtoMaterials.Opaque(color, smoothness));
            if (collider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = ProtoMaterials.PrimitiveMesh(PrimitiveType.Cylinder);
                mc.convex = true;
            }
            return go;
        }

        public static Rotator MakeRotator(Transform parent, string name, Vector3 localPivot, float speedDeg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPivot;
            var rot = go.AddComponent<Rotator>();
            rot.SpeedDeg = speedDeg;
            return rot;
        }

        /// <summary>도는 접시 (윗면 = topCenter). 방사형 줄무늬로 도는 게 보여요.</summary>
        public static Rotator RotatingPlate(Transform parent, string name, Vector3 topCenter, float radius, float speedDeg, Color stripe)
        {
            Rotator pivot = MakeRotator(parent, name, topCenter, speedDeg);
            Transform p = pivot.transform;
            Cylinder(p, "Plate", new Vector3(0f, -0.25f, 0f), radius * 2f, 0.5f, PlateWhite, 0.6f);
            Cylinder(p, "Rim", new Vector3(0f, -0.33f, 0f), radius * 2f + 0.3f, 0.5f, PlateRim, 0.5f, false);
            Cylinder(p, "Foot", new Vector3(0f, -0.75f, 0f), radius * 1.2f, 0.5f, PlateWhite, 0.5f, false);
            Material m = ProtoMaterials.Opaque(stripe, 0.5f);
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                float ang = i * 360f / count;
                Quaternion q = Quaternion.Euler(0f, ang, 0f);
                ProtoMaterials.VisualPart(p, "Stripe", PrimitiveType.Cube, q * new Vector3(0f, 0.006f, radius * 0.55f),
                    new Vector3(0.35f, 0.01f, radius * 0.8f), q, m, false);
            }
            ProtoMaterials.VisualPart(p, "Center", PrimitiveType.Cylinder, new Vector3(0f, 0.006f, 0f),
                new Vector3(radius * 0.35f, 0.006f, radius * 0.35f), Quaternion.identity, m, false);
            return pivot;
        }

        /// <summary>휘두르는 젓가락 한 쌍 (가운데 기둥 + 막대). low = 점프로 넘을 높이.</summary>
        public static Rotator Sweeper(Transform parent, string name, Vector3 floorPivot, float length, float barHeight, float speedDeg, bool low)
        {
            Rotator pivot = MakeRotator(parent, name, floorPivot, speedDeg);
            Transform p = pivot.transform;
            Cylinder(p, "Post", new Vector3(0f, (barHeight + 0.35f) * 0.5f, 0f), 0.55f, barHeight + 0.35f, Lacquer, 0.55f);

            var bar = new GameObject("Bar");
            bar.transform.SetParent(p, false);
            bar.transform.localPosition = new Vector3(0f, barHeight, 0f);
            var box = bar.AddComponent<BoxCollider>();
            box.size = new Vector3(0.32f, 0.3f, length);
            var hazard = bar.AddComponent<KnockHazard>();
            hazard.Box = box;
            hazard.Low = low;

            // 보이는 젓가락 두 짝 (끝은 진하게, 살짝 가늘게)
            Material wood = ProtoMaterials.Opaque(Chopstick, 0.45f);
            Material tip = ProtoMaterials.Opaque(ChopstickTip, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                ProtoMaterials.VisualPart(bar.transform, "Stick", PrimitiveType.Cube, new Vector3(0.08f * s, 0f, 0f),
                    new Vector3(0.12f, 0.12f, length * 0.86f), Quaternion.identity, wood);
                for (int e = -1; e <= 1; e += 2)
                {
                    ProtoMaterials.VisualPart(bar.transform, "Tip", PrimitiveType.Cube, new Vector3(0.08f * s, 0f, e * length * 0.465f),
                        new Vector3(0.09f, 0.09f, length * 0.07f), Quaternion.identity, tip);
                }
            }
            return pivot;
        }

        /// <summary>보이지 않는 트리거 상자.</summary>
        public static GameObject Trigger(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            return go;
        }

        public static Checkpoint MakeCheckpoint(PrototypeLevel level, Transform parent, int index, Vector3 center, Vector3 size, Vector3 respawn, float yaw)
        {
            GameObject go = Trigger(parent, "Checkpoint" + index, center, size);
            var cp = go.AddComponent<Checkpoint>();
            cp.Index = index;
            cp.Level = level;
            cp.RespawnPoint = parent.TransformPoint(respawn);
            cp.RespawnYaw = yaw;
            return cp;
        }

        /// <summary>"거대한 주방" 배경 — 아래 멀리 나무 조리대, 거대한 찻잔·젓가락·접시 더미·간장병, 타일 벽 (장식만).</summary>
        public static void KitchenBackdrop(Transform parent, Vector3 center, float floorY)
        {
            var root = new GameObject("KitchenBackdrop").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, 0f, center.z);

            Box(root, "Counter", new Vector3(0f, floorY - 1f, 0f), new Vector3(520f, 2f, 520f), Wood, 0.35f, false);
            // 조리대 나뭇결 띠
            for (int i = -6; i <= 6; i++)
            {
                Box(root, "Grain", new Vector3(i * 38f + 7f, floorY + 0.01f, 0f), new Vector3(2.5f, 0.02f, 520f), WoodDark, 0.3f, false);
            }

            // 거대한 찻잔
            Vector3 cup = new Vector3(-75f, floorY, 70f);
            Cylinder(root, "Teacup", cup + Vector3.up * 16f, 36f, 32f, new Color(0.95f, 0.93f, 0.87f), 0.7f, false);
            Cylinder(root, "TeacupBand", cup + Vector3.up * 24f, 36.6f, 4f, new Color(0.30f, 0.50f, 0.38f), 0.6f, false);
            Cylinder(root, "Tea", cup + Vector3.up * 31.2f, 33.5f, 1.8f, new Color(0.46f, 0.55f, 0.24f), 0.9f, false);

            // 거대한 젓가락 한 쌍 (조리대에 놓임)
            Box(root, "ChopstickA", new Vector3(62f, floorY + 1.4f, 30f), new Vector3(2.6f, 2.6f, 150f), Quaternion.Euler(0f, 18f, 0f), Chopstick, 0.45f, false);
            Box(root, "ChopstickB", new Vector3(69f, floorY + 1.4f, 34f), new Vector3(2.6f, 2.6f, 150f), Quaternion.Euler(0f, 23f, 0f), Chopstick, 0.45f, false);
            Box(root, "ChopRest", new Vector3(48f, floorY + 2f, -30f), new Vector3(14f, 4f, 5f), Quaternion.Euler(0f, 20f, 0f), PlateRim, 0.6f, false);

            // 접시 더미
            for (int i = 0; i < 6; i++)
            {
                Vector3 c = new Vector3(85f + (i % 2) * 1.2f, floorY + 0.9f + i * 1.8f, -55f + (i % 3) * 0.8f);
                Cylinder(root, "PlateStack", c, 46f, 1.6f, i % 2 == 0 ? PlateWhite : new Color(0.88f, 0.90f, 0.95f), 0.7f, false);
            }
            Cylinder(root, "PlateStackRim", new Vector3(85f, floorY + 11.6f, -55f), 47f, 0.6f, PlateRim, 0.6f, false);

            // 간장병
            Vector3 soy = new Vector3(-60f, floorY, -80f);
            Cylinder(root, "SoyBottle", soy + Vector3.up * 18f, 14f, 36f, new Color(0.22f, 0.08f, 0.05f), 0.85f, false);
            Cylinder(root, "SoyCap", soy + Vector3.up * 39f, 7f, 6f, new Color(0.78f, 0.12f, 0.10f), 0.6f, false);

            // 타일 벽 (안개에 묻혀 멀리)
            Box(root, "WallBack", new Vector3(0f, floorY + 90f, 230f), new Vector3(520f, 200f, 4f), new Color(0.90f, 0.87f, 0.80f), 0.5f, false);
            Box(root, "WallSide", new Vector3(-230f, floorY + 90f, 0f), new Vector3(4f, 200f, 520f), new Color(0.88f, 0.84f, 0.77f), 0.5f, false);
        }

        /// <summary>해(방향광) + 따뜻한 환경광 + 안개. 이미 있으면 다시 설정만.</summary>
        public static Light SetupLighting(Light existing)
        {
            Light sun = existing;
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
            }
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.75f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.58f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.30f, 0.24f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 300f;
            return sun;
        }
    }
}
