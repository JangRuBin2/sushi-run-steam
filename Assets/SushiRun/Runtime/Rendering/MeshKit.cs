// 코드로 만드는 매끈한 메시: 모서리 둥근 상자, 회전체(접시·컵처럼 옆모습을 돌려 만든 것).
// 기본 도형 조립보다 덜 싸 보이게 — 최종 아트(모델)가 들어오기 전 맵 1차 아트용.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    public static class MeshKit
    {
        /// <summary>모서리 둥근 상자. size = 전체 크기 (m), radius = 모서리 반지름, seg = 둥근 부분 나눔 수.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 5)
        {
            float minHalf = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f;
            radius = Mathf.Clamp(radius, 0.001f, minHalf - 0.001f);
            Vector3 half = size * 0.5f;
            Vector3 inner = half - Vector3.one * radius;
            float[][] coords = { AxisCoords(half.x, inner.x, seg), AxisCoords(half.y, inner.y, seg), AxisCoords(half.z, inner.z, seg) };

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int axis = 0; axis < 3; axis++)
            {
                int a1 = (axis + 1) % 3;
                int a2 = (axis + 2) % 3;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 faceN = Vector3.zero;
                    faceN[axis] = sign;
                    float[] ca = coords[a1];
                    float[] cb = coords[a2];
                    int start = verts.Count;
                    for (int j = 0; j < cb.Length; j++)
                    {
                        for (int i = 0; i < ca.Length; i++)
                        {
                            Vector3 q = Vector3.zero;
                            q[axis] = sign * half[axis];
                            q[a1] = ca[i];
                            q[a2] = cb[j];
                            Vector3 c = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                            Vector3 d = q - c;
                            Vector3 n = d.sqrMagnitude > 1e-10f ? d.normalized : faceN;
                            verts.Add(c + n * radius);
                            norms.Add(n);
                            uvs.Add(new Vector2(ca[i], cb[j]));
                        }
                    }
                    int w = ca.Length;
                    for (int j = 0; j < cb.Length - 1; j++)
                    {
                        for (int i = 0; i < w - 1; i++)
                        {
                            int v0 = start + j * w + i;
                            AddQuad(tris, verts, faceN, v0, v0 + 1, v0 + w + 1, v0 + w);
                        }
                    }
                }
            }
            return Build("RoundedBox", verts, norms, uvs, tris);
        }

        /// <summary>둥근 부분은 seg칸, 평평한 가운데는 한 칸으로 나눈 좌표 (-half → half).</summary>
        private static float[] AxisCoords(float half, float inner, int seg)
        {
            var list = new List<float>();
            for (int k = 0; k <= seg; k++)
            {
                list.Add(-half + (half - inner) * k / seg);
            }
            for (int k = 0; k <= seg; k++)
            {
                float v = inner + (half - inner) * k / seg;
                if (v > list[list.Count - 1] + 1e-5f)
                {
                    list.Add(v);
                }
            }
            return list.ToArray();
        }

        /// <summary>회전체: profile = (반지름, 높이) 점들. 위 가운데 → 바깥 → 아래로 시계 방향으로 돌아가는 순서로 주면
        /// 바깥을 향한 면이 돼요. 같은 점을 두 번 넣으면 그 자리는 각진 모서리.</summary>
        public static Mesh Lathe(Vector2[] profile, int segments = 48)
        {
            int n = profile.Length;
            var pn = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = profile[Mathf.Max(0, i - 1)];
                Vector2 next = profile[Mathf.Min(n - 1, i + 1)];
                if (i > 0 && (profile[i] - profile[i - 1]).sqrMagnitude < 1e-10f)
                {
                    prev = profile[i]; // 두 번째 겹친 점 = 다음 변의 방향
                }
                if (i < n - 1 && (profile[i + 1] - profile[i]).sqrMagnitude < 1e-10f)
                {
                    next = profile[i]; // 첫 번째 겹친 점 = 앞 변의 방향
                }
                Vector2 t = next - prev;
                if (t.sqrMagnitude < 1e-10f)
                {
                    t = Vector2.right;
                }
                pn[i] = new Vector2(-t.y, t.x).normalized;
            }

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int s = 0; s <= segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);
                for (int i = 0; i < n; i++)
                {
                    verts.Add(new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin));
                    norms.Add(new Vector3(pn[i].x * cos, pn[i].y, pn[i].x * sin).normalized);
                    uvs.Add(new Vector2(s / (float)segments, i / (float)(n - 1)));
                }
            }
            for (int s = 0; s < segments; s++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    int v0 = s * n + i;
                    Vector3 expect = norms[v0] + norms[v0 + 1] + norms[v0 + n] + norms[v0 + n + 1];
                    AddQuad(tris, verts, expect, v0, v0 + 1, v0 + n + 1, v0 + n);
                }
            }
            return Build("Lathe", verts, norms, uvs, tris);
        }

        /// <summary>사각형 하나를 삼각형 둘로 — 바깥(expect) 쪽이 앞면이 되게 감는 방향을 맞춰요.</summary>
        private static void AddQuad(List<int> tris, List<Vector3> verts, Vector3 expect, int a, int b, int c, int d)
        {
            AddTri(tris, verts, expect, a, b, c);
            AddTri(tris, verts, expect, a, c, d);
        }

        private static void AddTri(List<int> tris, List<Vector3> verts, Vector3 expect, int a, int b, int c)
        {
            Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (n.sqrMagnitude < 1e-14f)
            {
                return; // 축 위에서 접힌 삼각형
            }
            if (Vector3.Dot(n, expect) >= 0f)
            {
                tris.Add(a);
                tris.Add(b);
                tris.Add(c);
            }
            else
            {
                tris.Add(a);
                tris.Add(c);
                tris.Add(b);
            }
        }

        private static Mesh Build(string name, List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
