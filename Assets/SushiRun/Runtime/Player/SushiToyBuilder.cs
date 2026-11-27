// 임시 캐릭터 — 기본 도형으로 만든 "장난감 같은" 계란초밥 (회색 박스 단계 전용, 최종 아트는 3단계에서 모델로).
// 구조: Visual(발바닥 피벗) → 다리·밥 몸통·밥알·얼굴·팔 + Topping(토핑 피벗) → 토핑 판·줄무늬·김 띠.
// 크기: 몸 폭 약 1.0 m, 높이 약 0.95 m (SushiCharacter 콜라이더와 맞춤). 앞 = +Z.
using UnityEngine;

namespace SushiRun.Runtime
{
    [System.Serializable]
    public struct SushiPalette
    {
        public string Name;
        public Color Rice;
        public Color Topping;
        public float ToppingSmoothness;
        public bool AccentStripes; // 생선 지방 줄무늬
        public Color Accent;
        public bool NoriBand; // 김 띠 (계란초밥)
        public Color Nori;
        public Color Blush;
        public string ToppingTexture; // Resources/Characters/ 안 이름 (Codex 텍스처). 없으면 Topping 색만


        public static SushiPalette Tamago => new SushiPalette
        {
            Name = "계란",
            ToppingTexture = "tamago",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(1.00f, 0.80f, 0.22f),
            ToppingSmoothness = 0.75f,
            NoriBand = true,
            Nori = new Color(0.10f, 0.17f, 0.12f),
            Blush = new Color(1.00f, 0.56f, 0.62f),
        };

        public static SushiPalette Salmon => new SushiPalette
        {
            Name = "연어",
            ToppingTexture = "salmon",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(1.00f, 0.50f, 0.30f),
            ToppingSmoothness = 0.78f,
            AccentStripes = true,
            Accent = new Color(1.00f, 0.88f, 0.80f),
            Blush = new Color(1.00f, 0.56f, 0.62f),
        };

        public static SushiPalette Tuna => new SushiPalette
        {
            Name = "참치",
            ToppingTexture = "tuna",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(0.80f, 0.12f, 0.18f),
            ToppingSmoothness = 0.72f,
            AccentStripes = true,
            Accent = new Color(0.92f, 0.36f, 0.40f),
            Blush = new Color(1.00f, 0.50f, 0.58f),
        };

        public static SushiPalette Mackerel => new SushiPalette
        {
            Name = "고등어",
            ToppingTexture = "mackerel_skin",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(0.66f, 0.74f, 0.82f),
            ToppingSmoothness = 0.85f,
            AccentStripes = true,
            Accent = new Color(0.16f, 0.24f, 0.40f),
            Blush = new Color(1.00f, 0.58f, 0.66f),
        };

        public static SushiPalette Ebi => new SushiPalette
        {
            Name = "새우",
            ToppingTexture = "ebi",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(1.00f, 0.56f, 0.36f),
            ToppingSmoothness = 0.7f,
            AccentStripes = true,
            Accent = new Color(1.00f, 0.95f, 0.90f),
            Blush = new Color(1.00f, 0.56f, 0.62f),
        };

        public static SushiPalette Hamachi => new SushiPalette
        {
            Name = "방어",
            ToppingTexture = "hamachi",
            Rice = new Color(0.97f, 0.96f, 0.92f),
            Topping = new Color(0.98f, 0.84f, 0.74f),
            ToppingSmoothness = 0.8f,
            AccentStripes = true,
            Accent = new Color(0.94f, 0.58f, 0.56f),
            Blush = new Color(1.00f, 0.56f, 0.62f),
        };

        /// <summary>봇용 순서 (플레이어는 계란).</summary>
        public static SushiPalette[] BotPalettes => new[] { Salmon, Tuna, Mackerel, Ebi, Hamachi, Salmon };
    }

    public static class SushiToyBuilder
    {
        // 밥 몸통 (눕힌 캡슐): 가운데 높이·반높이·반깊이·반폭, 캡슐 끝 반지름
        private const float RiceCenterY = 0.46f;
        private const float RiceHalfY = 0.32f;
        private const float RiceHalfZ = 0.40f;
        private const float RiceHalfX = 0.50f;
        private const float RiceCapX = 0.25f;
        private const float ToppingPivotY = 0.74f;

        /// <summary>root 아래에 "Visual"을 만들어요. topping = 출렁일 토핑 피벗.</summary>
        public static Transform Build(Transform root, SushiPalette p, out Transform topping)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);

            Material rice = ProtoMaterials.Opaque(p.Rice, 0.35f); // 밥알 텍스처는 몸통이 지저분해 보여서 안 씀 (콘셉트: 매끈한 흰 밥)
            Material riceDark = ProtoMaterials.Opaque(p.Rice * 0.9f + new Color(0f, 0f, 0f, 0.1f), 0.3f);
            Material grain = ProtoMaterials.Opaque(new Color(1f, 0.995f, 0.96f), 0.5f);
            Material eye = ProtoMaterials.Opaque(new Color(0.04f, 0.04f, 0.06f), 0.92f);
            Material shine = ProtoMaterials.Opaque(Color.white, 0.3f);
            Material blush = ProtoMaterials.Opaque(p.Blush, 0.25f);
            Material mouth = ProtoMaterials.Opaque(new Color(0.28f, 0.10f, 0.10f), 0.4f);

            // 다리 (짧고 통통)
            for (int side = -1; side <= 1; side += 2)
            {
                ProtoMaterials.VisualPart(visual, side < 0 ? "LegL" : "LegR", PrimitiveType.Capsule,
                    new Vector3(0.2f * side, 0.09f, 0f), new Vector3(0.17f, 0.09f, 0.17f), Quaternion.identity, riceDark);
            }

            // 밥 몸통: 캡슐을 옆으로 눕혀 둥근 사각 덩어리처럼 (폭 1.0, 높이 0.64, 깊이 0.8)
            ProtoMaterials.VisualPart(visual, "Rice", PrimitiveType.Capsule,
                new Vector3(0f, RiceCenterY, 0f), new Vector3(RiceHalfY * 2f, RiceHalfX, RiceHalfZ * 2f), Quaternion.Euler(0f, 0f, 90f), rice);

            // 밥알 (얼굴은 비워 둠): (x, 각도 — 위 0°, 앞 90°, 아래 180°, 뒤 270°)
            Vector2[] grains =
            {
                new Vector2(-0.30f, 125f), new Vector2(0.05f, 137f), new Vector2(0.28f, 128f), new Vector2(-0.10f, 150f),
                new Vector2(-0.44f, 62f), new Vector2(-0.43f, 112f), new Vector2(0.44f, 76f), new Vector2(0.43f, 116f),
                new Vector2(-0.25f, 232f), new Vector2(0.10f, 246f), new Vector2(0.30f, 220f), new Vector2(-0.05f, 290f),
                new Vector2(0.20f, 300f), new Vector2(-0.30f, 266f), new Vector2(0.36f, 268f), new Vector2(-0.38f, 205f),
            };
            for (int i = 0; i < grains.Length; i++)
            {
                Vector3 pos = RiceSurface(grains[i].x, grains[i].y, 0.985f);
                Quaternion rot = Quaternion.Euler(grains[i].y, i * 47f % 180f, i * 31f % 90f);
                ProtoMaterials.VisualPart(visual, "Grain" + i, PrimitiveType.Sphere, pos, new Vector3(0.11f, 0.075f, 0.075f), rot, grain, false);
            }

            // 얼굴: 크고 반짝이는 검은 눈 + 하이라이트 2개, 볼터치, 작은 웃는 입
            var face = new GameObject("Face").transform;
            face.SetParent(visual, false);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.16f * side;
                ProtoMaterials.VisualPart(face, side < 0 ? "EyeL" : "EyeR", PrimitiveType.Sphere,
                    new Vector3(x, 0.50f, 0.375f), new Vector3(0.13f, 0.17f, 0.07f), Quaternion.identity, eye, false);
                ProtoMaterials.VisualPart(face, "Shine", PrimitiveType.Sphere,
                    new Vector3(x + 0.028f, 0.545f, 0.40f), new Vector3(0.05f, 0.05f, 0.03f), Quaternion.identity, shine, false);
                ProtoMaterials.VisualPart(face, "ShineSmall", PrimitiveType.Sphere,
                    new Vector3(x - 0.025f, 0.468f, 0.405f), new Vector3(0.024f, 0.024f, 0.02f), Quaternion.identity, shine, false);
                Vector3 cheek = RiceSurface(0.29f * side, 104f, 0.975f);
                ProtoMaterials.VisualPart(face, "Blush", PrimitiveType.Sphere,
                    cheek, new Vector3(0.12f, 0.055f, 0.03f), Quaternion.Euler(0f, 18f * side, 0f), blush, false);
            }
            for (int i = 0; i < 5; i++)
            {
                float t = (i - 2) / 2f; // -1..1
                Vector3 pos = new Vector3(t * 0.05f, 0.425f - 0.016f * (1f - t * t), 0.393f);
                ProtoMaterials.VisualPart(face, "Smile" + i, PrimitiveType.Sphere, pos, new Vector3(0.026f, 0.022f, 0.02f), Quaternion.identity, mouth, false);
            }

            // 팔 (짧은 캡슐, 바깥 아래로)
            for (int side = -1; side <= 1; side += 2)
            {
                ProtoMaterials.VisualPart(visual, side < 0 ? "ArmL" : "ArmR", PrimitiveType.Capsule,
                    new Vector3(0.54f * side, 0.42f, 0.02f), new Vector3(0.11f, 0.1f, 0.11f), Quaternion.Euler(0f, 0f, 70f * side), rice);
            }

            // 토핑 (피벗 = 밥 위, JellyBody가 출렁)
            topping = new GameObject("Topping").transform;
            topping.SetParent(visual, false);
            topping.localPosition = new Vector3(0f, ToppingPivotY, 0f);
            string toppingTex = string.IsNullOrEmpty(p.ToppingTexture) ? null : "Characters/" + p.ToppingTexture;
            bool textured = toppingTex != null && Resources.Load<Texture2D>(toppingTex) != null;
            Material top = TexOr(toppingTex, 1.5f, p.Topping, p.ToppingSmoothness);
            ProtoMaterials.VisualPart(topping, "Slab", PrimitiveType.Capsule,
                new Vector3(0f, 0.10f, 0f), new Vector3(0.24f, 0.56f, 0.9f), Quaternion.Euler(0f, 0f, 90f), top);
            if (p.AccentStripes && !textured) // 텍스처에 이미 지방 줄무늬가 있어요
            {
                Material accent = ProtoMaterials.Opaque(p.Accent, Mathf.Max(0.3f, p.ToppingSmoothness - 0.1f));
                for (int i = -1; i <= 1; i++)
                {
                    ProtoMaterials.VisualPart(topping, "Stripe", PrimitiveType.Sphere,
                        new Vector3(i * 0.28f, 0.205f, 0f), new Vector3(0.065f, 0.03f, 0.55f), Quaternion.Euler(0f, 24f, 0f), accent, false);
                }
            }
            if (p.NoriBand)
            {
                // 김 띠: 납작한 원기둥을 X축으로 눕혀 토핑을 감싸게 (안쪽은 토핑·밥에 가려 띠만 보여요)
                Material nori = TexOr("Characters/nori", 2f, p.Nori, 0.45f);
                ProtoMaterials.VisualPart(topping, "NoriBand", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.08f, 0f), new Vector3(0.34f, 0.11f, 0.96f), Quaternion.Euler(0f, 0f, 90f), nori);
            }
            return visual;
        }

        /// <summary>Codex 텍스처 재질 (Resources에 있으면), 없으면 단색. tiling = UV 1당 반복 수.</summary>
        private static Material TexOr(string tex, float tiling, Color fallback, float smoothness)
        {
            if (tex != null && Resources.Load<Texture2D>(tex) != null)
            {
                return SinkKit.TexMat(tex, tiling, Color.white, smoothness);
            }
            return ProtoMaterials.Opaque(fallback, smoothness);
        }

        /// <summary>밥 몸통 겉면 위 점. angleDeg: 위 0°, 앞 90°, 아래 180°, 뒤 270° (X축 둘레).</summary>
        private static Vector3 RiceSurface(float x, float angleDeg, float inset)
        {
            float ax = Mathf.Abs(x);
            float f = 1f;
            float inner = RiceHalfX - RiceCapX;
            if (ax > inner)
            {
                float k = Mathf.Clamp01((ax - inner) / RiceCapX);
                f = Mathf.Sqrt(Mathf.Max(0f, 1f - k * k));
            }
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(x, RiceCenterY + RiceHalfY * f * Mathf.Cos(rad) * inset, RiceHalfZ * f * Mathf.Sin(rad) * inset);
        }
    }
}
