// "설거지통" 맵 도구: 스테인리스·도자기·물·거품 재질, 도는 접시, 출렁이는 컵, 미는 스펀지, 휘두르는 솔, 싱크대 배경.
// 메시는 MeshKit(둥근 상자·회전체)로 만들어요. 판정은 단순한 콜라이더(상자·볼록 원기둥)로 따로 둬요.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SushiRun.Runtime
{
    public static class SinkKit
    {
        public static readonly Color Steel = new Color(0.84f, 0.86f, 0.89f);
        public static readonly Color SteelDark = new Color(0.55f, 0.58f, 0.62f);
        public static readonly Color Ceramic = new Color(0.97f, 0.97f, 0.95f);
        public static readonly Color CeramicBlue = new Color(0.70f, 0.84f, 0.95f);
        public static readonly Color RimBlue = new Color(0.22f, 0.45f, 0.78f);
        public static readonly Color BoardWood = new Color(0.80f, 0.62f, 0.42f);
        public static readonly Color SpongeYellow = new Color(1.00f, 0.84f, 0.22f);
        public static readonly Color ScrubGreen = new Color(0.22f, 0.62f, 0.36f);
        public static readonly Color BrushBlue = new Color(0.25f, 0.55f, 0.92f);
        public static readonly Color Bristle = new Color(0.98f, 0.96f, 0.90f);
        public static readonly Color Foam = new Color(0.98f, 0.99f, 1.00f);
        public static readonly Color Counter = new Color(0.82f, 0.77f, 0.70f);
        public static readonly Color Tile = new Color(0.86f, 0.93f, 0.97f);
        public static readonly Color Sky = new Color(0.84f, 0.92f, 0.98f);

        private static readonly Dictionary<(int, int, int), Material> s_mats = new Dictionary<(int, int, int), Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_mats.Clear();
            s_texMats.Clear();
        }

        private static readonly Dictionary<string, Material> s_texMats = new Dictionary<string, Material>();

        /// <summary>텍스처 재질 (공유). tex = Resources/Sink/ 안 이름 (Codex로 뽑은 그림), tiling = 1 m당 반복 수.
        /// 텍스처가 없으면 tint 색만 쓴 재질로 (회색 박스처럼).</summary>
        public static Material TexMat(string tex, float tiling, Color tint, float smoothness, float metallic = 0f)
        {
            string key = tex + "|" + tiling + "|" + ColorUtility.ToHtmlStringRGBA(tint) + "|" + smoothness + "|" + metallic;
            Material m;
            if (s_texMats.TryGetValue(key, out m) && m != null)
            {
                return m;
            }
            m = new Material(Mat(tint, smoothness, metallic));
            m.name = "Sink_" + tex;
            // "Space/kaiten_belt"처럼 폴더를 주면 그 폴더, 이름만 주면 Sink/ (다른 맵도 이 재질 도구를 같이 써요)
            var t = Resources.Load<Texture2D>(tex.Contains("/") ? tex : "Sink/" + tex);
            if (t != null)
            {
                t.wrapMode = TextureWrapMode.Repeat;
                if (m.HasProperty("_BaseMap"))
                {
                    m.SetTexture("_BaseMap", t);
                    m.SetTextureScale("_BaseMap", Vector2.one * tiling);
                }
                if (m.HasProperty("_MainTex"))
                {
                    m.SetTexture("_MainTex", t);
                    m.SetTextureScale("_MainTex", Vector2.one * tiling);
                }
            }
            s_texMats[key] = m;
            return m;
        }

        /// <summary>불투명 재질 (공유). metallic > 0 이면 금속 (스테인리스).</summary>
        public static Material Mat(Color color, float smoothness, float metallic = 0f)
        {
            Color32 c = color;
            var key = ((c.r << 24) | (c.g << 16) | (c.b << 8) | c.a, Mathf.RoundToInt(smoothness * 100f), Mathf.RoundToInt(metallic * 100f));
            Material m;
            if (s_mats.TryGetValue(key, out m) && m != null)
            {
                return m;
            }
            m = new Material(ProtoMaterials.Opaque(color, smoothness));
            m.name = "Sink_" + ColorUtility.ToHtmlStringRGB(color);
            if (m.HasProperty("_Metallic"))
            {
                m.SetFloat("_Metallic", metallic);
            }
            s_mats[key] = m;
            return m;
        }

        /// <summary>메시 하나로 된 보이기 전용 파츠.</summary>
        public static GameObject Part(Transform parent, string name, Mesh mesh, Vector3 localPos, Quaternion localRot, Material mat, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        /// <summary>둥근 상자 + 같은 크기 상자 콜라이더. 윗면 = topCenter.</summary>
        public static GameObject Slab(Transform parent, string name, Vector3 topCenter, Vector3 size, float radius, Material mat, bool collider = true)
        {
            GameObject go = Part(parent, name, MeshKit.RoundedBox(size, radius), topCenter + Vector3.down * (size.y * 0.5f), Quaternion.identity, mat);
            if (collider)
            {
                go.AddComponent<BoxCollider>().size = size;
            }
            return go;
        }

        /// <summary>볼록 원기둥 콜라이더만 (보이지 않음). 윗면 = top (부모 기준).</summary>
        public static void CylinderCollider(Transform parent, float top, float radius, float height)
        {
            var go = new GameObject("Collider");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, top - height * 0.5f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f); // 기본 원기둥: 지름 1, 높이 2
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = ProtoMaterials.PrimitiveMesh(PrimitiveType.Cylinder);
            mc.convex = true;
        }

        /// <summary>도는 접시 (윗면 = topCenter). 가장자리가 살짝 올라온 흰 접시 + 파란 테 두 줄.</summary>
        public static Rotator SpinningPlate(Transform parent, string name, Vector3 topCenter, float radius, float speedDeg)
        {
            Rotator pivot = GreyboxKit.MakeRotator(parent, name, topCenter, speedDeg);
            Transform p = pivot.transform;
            float r = radius;
            Vector2[] plate =
            {
                new Vector2(0f, 0f), new Vector2(r * 0.80f, 0f), new Vector2(r * 0.93f, 0.05f), new Vector2(r, 0.09f),
                new Vector2(r * 1.01f, 0.06f), new Vector2(r * 0.97f, -0.06f), new Vector2(r * 0.60f, -0.30f),
                new Vector2(r * 0.55f, -0.42f), new Vector2(r * 0.55f, -0.42f), new Vector2(0f, -0.42f),
            };
            Part(p, "Plate", MeshKit.Lathe(plate, 64), Vector3.zero, Quaternion.identity, Mat(Ceramic, 0.85f));
            Ring(p, "RimLine", r * 0.84f, r * 0.87f, 0.004f, Mat(RimBlue, 0.7f));
            Ring(p, "RimLine2", r * 0.89f, r * 0.905f, 0.03f, Mat(RimBlue, 0.7f));
            Ring(p, "Center", r * 0.16f, r * 0.30f, 0.004f, Mat(CeramicBlue, 0.7f));
            CylinderCollider(p, 0f, r, 0.45f);
            return pivot;
        }

        /// <summary>납작한 고리 (위에서 본 선 장식).</summary>
        public static void Ring(Transform parent, string name, float r0, float r1, float y, Material mat)
        {
            Vector2[] ring = { new Vector2(r0, y + 0.003f), new Vector2(r1, y + 0.003f), new Vector2(r1, y), new Vector2(r0, y) };
            Part(parent, name, MeshKit.Lathe(ring, 64), Vector3.zero, Quaternion.identity, mat, false);
        }

        /// <summary>물에 엎어져 떠서 위아래로 출렁이는 컵 (윗면 = topCenter 근처).</summary>
        public static SineMover BobbingCup(Transform parent, string name, Vector3 topCenter, float radius, Color color, float phase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = topCenter;
            float r = radius;
            Vector2[] cup =
            {
                new Vector2(0f, 0f), new Vector2(r * 0.92f, 0f), new Vector2(r, -0.08f), new Vector2(r * 1.08f, -1.6f),
                new Vector2(r * 1.16f, -2.3f), new Vector2(r * 1.16f, -2.3f), new Vector2(r * 0.95f, -2.3f),
            };
            Part(go.transform, "Cup", MeshKit.Lathe(cup, 48), Vector3.zero, Quaternion.identity, Mat(color, 0.8f));
            Ring(go.transform, "Band", r * 0.55f, r * 0.68f, 0.004f, Mat(Ceramic, 0.8f));
            Part(go.transform, "Handle", MeshKit.RoundedBox(new Vector3(0.35f, 1.1f, 0.9f), 0.17f), new Vector3(r * 1.18f, -1.1f, 0f), Quaternion.identity, Mat(color, 0.8f));
            CylinderCollider(go.transform, 0f, r, 2.2f);
            var mover = go.AddComponent<SineMover>();
            mover.Axis = Vector3.up;
            mover.Amplitude = 0.3f;
            mover.Period = 3.2f;
            mover.Phase = phase;
            return mover;
        }

        /// <summary>좌우로 왔다 갔다 하며 옆에 닿은 캐릭터를 밀어내는 스펀지 (바닥 = floorCenter). 위에 올라타면 실려 가요.</summary>
        public static SineMover Sponge(Transform parent, string name, Vector3 floorCenter, float amplitude, float period, float phase)
        {
            var size = new Vector3(2.2f, 1.4f, 2.4f);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = floorCenter + Vector3.up * (size.y * 0.5f);
            Part(go.transform, "Body", MeshKit.RoundedBox(new Vector3(size.x, 1.0f, size.z), 0.22f), new Vector3(0f, -0.2f, 0f), Quaternion.identity, TexMat("sponge_yellow", 0.45f, Color.white, 0.12f));
            Part(go.transform, "Scrub", MeshKit.RoundedBox(new Vector3(size.x, 0.42f, size.z), 0.12f), new Vector3(0f, 0.49f, 0f), Quaternion.identity, TexMat("scrub_green", 0.6f, Color.white, 0.1f));
            go.AddComponent<BoxCollider>().size = size;
            var mover = go.AddComponent<SineMover>();
            mover.Axis = Vector3.right;
            mover.Amplitude = amplitude;
            mover.Period = period;
            mover.Phase = phase;

            // 미는 판정: 옆면만 (위에 올라탄 캐릭터는 밀지 않게 윗면 아래까지)
            var push = new GameObject("Push");
            push.transform.SetParent(go.transform, false);
            var box = push.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x + 0.2f, 1.0f, size.z + 0.2f);
            box.center = new Vector3(0f, -0.15f, 0f);
            box.enabled = false; // 크기만 씀 (KnockHazard가 OverlapBox로 직접 찾아요)
            var hazard = push.AddComponent<KnockHazard>();
            hazard.Box = box;
            hazard.Low = false;
            hazard.KnockHorizontal = 7f;
            hazard.KnockUp = 3f;
            hazard.Stun = 0.6f;
            hazard.RepeatDelay = 1f;
            return mover;
        }

        /// <summary>휘두르는 설거지 솔 (가운데 기둥 + 막대, 무릎 높이 = 점프로 넘기). 판정은 GreyboxKit.Sweeper와 같아요.</summary>
        public static Rotator Brush(Transform parent, string name, Vector3 floorPivot, float length, float speedDeg)
        {
            const float barHeight = 0.45f;
            Rotator pivot = GreyboxKit.MakeRotator(parent, name, floorPivot, speedDeg);
            Transform p = pivot.transform;
            Vector2[] post =
            {
                new Vector2(0f, barHeight + 0.42f), new Vector2(0.38f, barHeight + 0.40f), new Vector2(0.5f, barHeight + 0.25f),
                new Vector2(0.5f, 0.1f), new Vector2(0.62f, 0f), new Vector2(0f, 0f),
            };
            Part(p, "Post", MeshKit.Lathe(post, 32), Vector3.zero, Quaternion.identity, TexMat("steel_brushed", 0.25f, Color.white, 0.8f, 0.35f));

            var bar = new GameObject("Bar");
            bar.transform.SetParent(p, false);
            bar.transform.localPosition = new Vector3(0f, barHeight, 0f);
            var box = bar.AddComponent<BoxCollider>();
            box.size = new Vector3(0.32f, 0.3f, length);
            var hazard = bar.AddComponent<KnockHazard>();
            hazard.Box = box;
            hazard.Low = true;

            // 보이는 솔: 파란 손잡이 막대 + 양 끝 흰 솔털 뭉치
            Part(bar.transform, "Handle", MeshKit.RoundedBox(new Vector3(0.26f, 0.22f, length * 0.92f), 0.1f), Vector3.zero, Quaternion.identity, Mat(BrushBlue, 0.75f));
            for (int e = -1; e <= 1; e += 2)
            {
                Part(bar.transform, "Bristles", MeshKit.RoundedBox(new Vector3(0.42f, 0.34f, length * 0.2f), 0.12f),
                    new Vector3(0f, -0.02f, e * length * 0.38f), Quaternion.identity, Mat(Bristle, 0.2f));
            }
            return pivot;
        }

        /// <summary>물에 뜬 거품 덩어리 (장식).</summary>
        public static void FoamCluster(Transform parent, Vector3 center, float size, int seed)
        {
            var rnd = new System.Random(seed);
            Material m = Mat(Foam, 0.35f);
            int count = 4 + rnd.Next(4);
            for (int i = 0; i < count; i++)
            {
                float s = size * (0.45f + (float)rnd.NextDouble() * 0.6f);
                Vector3 off = new Vector3(((float)rnd.NextDouble() - 0.5f) * size * 1.6f, ((float)rnd.NextDouble() - 0.3f) * s * 0.3f, ((float)rnd.NextDouble() - 0.5f) * size * 1.6f);
                ProtoMaterials.VisualPart(parent, "Foam", PrimitiveType.Sphere, center + off, new Vector3(s, s * 0.6f, s), Quaternion.identity, m, false);
            }
        }

        /// <summary>스테인리스 싱크대 + 물 + 주변 조리대 + 타일 벽 + 수도꼭지 (장식). 물 높이 waterY, 코스는 z 0 → length.</summary>
        public static Material SinkBasin(Transform parent, float waterY, float length, List<Material> owned)
        {
            var root = new GameObject("Sink").transform;
            root.SetParent(parent, false);
            Material steel = TexMat("steel_brushed", 0.25f, Color.white, 0.82f, 0.35f);
            Material steelDark = TexMat("steel_brushed", 0.25f, SteelDark, 0.7f, 0.35f);
            float zMid = length * 0.5f;
            float zLen = length + 30f;
            const float halfW = 15f;
            const float floorY = -9f;
            const float rimY = 2.5f;

            // 바닥·벽 (물 아래로 비쳐 보이게)
            Part(root, "Floor", MeshKit.RoundedBox(new Vector3(halfW * 2f + 2f, 1f, zLen), 0.4f), new Vector3(0f, floorY - 0.5f, zMid), Quaternion.identity, steelDark);
            for (int s = -1; s <= 1; s += 2)
            {
                Part(root, "Wall", MeshKit.RoundedBox(new Vector3(1.2f, rimY - floorY, zLen), 0.5f), new Vector3(s * (halfW + 0.6f), (rimY + floorY) * 0.5f, zMid), Quaternion.identity, steel);
                Part(root, "Counter", MeshKit.RoundedBox(new Vector3(40f, 1.2f, zLen + 40f), 0.5f), new Vector3(s * (halfW + 21f), rimY - 0.6f, zMid), Quaternion.identity, TexMat("counter_top", 0.15f, new Color(0.86f, 0.82f, 0.76f), 0.35f));
            }
            Part(root, "WallFront", MeshKit.RoundedBox(new Vector3(halfW * 2f + 2.4f, rimY - floorY, 1.2f), 0.5f), new Vector3(0f, (rimY + floorY) * 0.5f, zMid - zLen * 0.5f), Quaternion.identity, steel);
            Part(root, "WallBack", MeshKit.RoundedBox(new Vector3(halfW * 2f + 2.4f, rimY - floorY, 1.2f), 0.5f), new Vector3(0f, (rimY + floorY) * 0.5f, zMid + zLen * 0.5f), Quaternion.identity, steel);
            // 배수구
            Ring(root, "Drain", 0.2f, 1.4f, floorY + 0.01f, steel);

            // 물 (반투명) + 거품
            Material water = ProtoMaterials.Transparent(new Color(0.55f, 0.80f, 0.95f, 0.55f));
            if (water.HasProperty("_Smoothness"))
            {
                water.SetFloat("_Smoothness", 0.95f);
            }
            owned.Add(water);
            Part(root, "Water", MeshKit.RoundedBox(new Vector3(halfW * 2f, 0.1f, zLen), 0.04f), new Vector3(0f, waterY - 0.05f, zMid), Quaternion.identity, water, false);
            for (int i = 0; i < 16; i++)
            {
                float z = -8f + i * (length + 16f) / 15f;
                float side = (i % 2 == 0 ? -1f : 1f) * (10.5f + (i * 37 % 5));
                FoamCluster(root, new Vector3(side, waterY, z), 2.2f + (i % 3) * 0.6f, i);
            }

            // 뒤 타일 벽(조리대 위 낮게) + 그 뒤 주방 배경 그림 (Codex로 뽑은 그림, 빛 안 받는 재질)
            float wallZ = zMid + zLen * 0.5f + 22f;
            Part(root, "TileWall", MeshKit.RoundedBox(new Vector3(140f, 16f, 2f), 0.5f), new Vector3(0f, rimY + 8f, wallZ), Quaternion.identity, TexMat("tile_wall", 0.125f, Color.white, 0.7f));
            var backdropTex = Resources.Load<Texture2D>("Sink/kitchen_backdrop");
            if (backdropTex != null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                var backdrop = new Material(unlit != null ? unlit : Shader.Find("Unlit/Texture"));
                backdrop.name = "Sink_Backdrop";
                if (backdrop.HasProperty("_BaseMap"))
                {
                    backdrop.SetTexture("_BaseMap", backdropTex);
                }
                backdrop.mainTexture = backdropTex;
                owned.Add(backdrop);
                // 정면 + 양옆 (옆은 같은 그림을 돌려 세움 — 빈 하늘이 안 보이게)
                ProtoMaterials.VisualPart(root, "Backdrop", PrimitiveType.Quad, new Vector3(0f, rimY + 50f, wallZ + 8f), new Vector3(280f, 158f, 1f), Quaternion.identity, backdrop, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    ProtoMaterials.VisualPart(root, "BackdropSide", PrimitiveType.Quad, new Vector3(s * 140f, rimY + 50f, zMid + 20f),
                        new Vector3(280f, 158f, 1f), Quaternion.Euler(0f, s * 90f, 0f), backdrop, false);
                }
            }

            // 수도꼭지 (왼쪽 조리대에서 물 위로 굽어 나옴) + 떨어지는 물줄기
            Vector3 fBase = new Vector3(-halfW - 3f, rimY, zMid);
            Vector2[] neck = { new Vector2(0f, 9f), new Vector2(0.7f, 9f), new Vector2(0.7f, 0.6f), new Vector2(1.6f, 0.3f), new Vector2(1.6f, 0f), new Vector2(0f, 0f) };
            Part(root, "FaucetNeck", MeshKit.Lathe(neck, 40), fBase, Quaternion.identity, steel);
            Part(root, "FaucetArm", MeshKit.RoundedBox(new Vector3(12f, 1.3f, 1.3f), 0.6f), fBase + new Vector3(5.6f, 9f, 0f), Quaternion.identity, steel);
            Part(root, "FaucetHead", MeshKit.RoundedBox(new Vector3(1.6f, 1.8f, 1.6f), 0.6f), fBase + new Vector3(11.4f, 8.4f, 0f), Quaternion.identity, steel);
            Part(root, "Lever", MeshKit.RoundedBox(new Vector3(0.5f, 0.5f, 3.2f), 0.24f), fBase + new Vector3(0f, 9.8f, 1.2f), Quaternion.Euler(-25f, 0f, 0f), steelDark);
            Material stream = ProtoMaterials.Transparent(new Color(0.70f, 0.88f, 1f, 0.45f));
            owned.Add(stream);
            float streamTop = rimY + 7.5f;
            ProtoMaterials.VisualPart(root, "WaterStream", PrimitiveType.Cylinder, new Vector3(fBase.x + 11.4f, (streamTop + waterY) * 0.5f, fBase.z),
                new Vector3(0.9f, (streamTop - waterY) * 0.5f, 0.9f), Quaternion.identity, stream, false);
            FoamCluster(root, new Vector3(fBase.x + 11.4f, waterY, fBase.z), 2.6f, 99);

            // 세제 통 (오른쪽 조리대)
            Vector3 soap = new Vector3(halfW + 6f, rimY, zMid + 18f);
            Vector2[] bottle = { new Vector2(0f, 14f), new Vector2(0.8f, 14f), new Vector2(0.9f, 12.5f), new Vector2(2.6f, 11f), new Vector2(3f, 9f), new Vector2(3f, 0.5f), new Vector2(2.7f, 0f), new Vector2(0f, 0f) };
            Part(root, "SoapBottle", MeshKit.Lathe(bottle, 40), soap, Quaternion.identity, Mat(new Color(0.42f, 0.82f, 0.62f), 0.85f));
            Part(root, "SoapLabel", MeshKit.RoundedBox(new Vector3(4.2f, 4f, 4.2f), 1.4f), soap + new Vector3(0f, 5f, 0f), Quaternion.identity, Mat(Ceramic, 0.5f));
            return water;
        }

        /// <summary>밝고 시원한 주방 조명 (설거지통). 원래대로는 GreyboxKit.SetupLighting.</summary>
        public static void SetupLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1f, 0.97f, 0.92f);
                sun.intensity = 1.35f;
                sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.80f, 0.86f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.72f, 0.76f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.46f, 0.50f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 420f;
        }
    }
}
