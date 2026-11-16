// 시제품용 재질·메시 (코드로 만듦). 같은 색·광택은 재질 하나를 같이 써요 (SRP 배처가 묶기 좋게).
// 최종 아트가 들어오면 이 파일은 회색 박스 전용으로만 남아요.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SushiRun.Runtime
{
    public static class ProtoMaterials
    {
        private static readonly Dictionary<(int, int), Material> s_opaque = new Dictionary<(int, int), Material>();
        private static readonly Dictionary<PrimitiveType, Mesh> s_meshes = new Dictionary<PrimitiveType, Mesh>();
        private static Shader s_lit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_opaque.Clear();
            s_meshes.Clear();
            s_lit = null;
        }

        private static Shader LitShader()
        {
            if (s_lit == null)
            {
                s_lit = Shader.Find("Universal Render Pipeline/Lit");
                if (s_lit == null)
                {
                    s_lit = Shader.Find("Standard"); // URP가 아닐 때 (분홍색이면 URP 설정 확인)
                }
                if (s_lit == null)
                {
                    s_lit = Shader.Find("Sprites/Default");
                }
            }
            return s_lit;
        }

        /// <summary>불투명 재질 (공유). smoothness 0~1 — 토핑 0.75처럼 반짝, 밥 0.35.</summary>
        public static Material Opaque(Color color, float smoothness)
        {
            Color32 c32 = color;
            int rgba = (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a;
            var key = (rgba, Mathf.RoundToInt(Mathf.Clamp01(smoothness) * 100f));
            Material mat;
            if (s_opaque.TryGetValue(key, out mat) && mat != null)
            {
                return mat;
            }
            mat = new Material(LitShader());
            mat.name = "Proto_" + ColorUtility.ToHtmlStringRGB(color);
            SetColor(mat, color);
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }
            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", smoothness);
            }
            s_opaque[key] = mat;
            return mat;
        }

        /// <summary>반투명 재질 (새로 만듦 — 쓴 쪽이 다 쓰면 Object.Destroy). 색의 a가 투명도.</summary>
        public static Material Transparent(Color color)
        {
            var mat = new Material(LitShader());
            mat.name = "Proto_Transparent";
            if (mat.HasProperty("_Surface"))
            {
                // URP Lit을 코드로 반투명으로 바꾸는 표준 설정
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_SrcBlendAlpha"))
                {
                    mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                }
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.6f);
            }
            SetColor(mat, color);
            return mat;
        }

        public static void SetColor(Material mat, Color color)
        {
            if (mat == null)
            {
                return;
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        /// <summary>기본 도형 메시 (콜라이더 없이 쓰려고 한 번만 꺼내 둠).</summary>
        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            Mesh mesh;
            if (s_meshes.TryGetValue(type, out mesh) && mesh != null)
            {
                return mesh;
            }
            GameObject tmp = GameObject.CreatePrimitive(type);
            tmp.SetActive(false); // 지우기 전까지 물리에 끼지 않게
            mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying)
            {
                Object.Destroy(tmp);
            }
            else
            {
                Object.DestroyImmediate(tmp);
            }
            s_meshes[type] = mesh;
            return mesh;
        }

        /// <summary>콜라이더 없는 보이기 전용 도형.</summary>
        public static GameObject VisualPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(type);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
    }
}
