using System.Collections.Generic;
using UnityEngine;

namespace FluidLove.AlbumCases
{
    // Jewel case box centred on its origin, front facing +Z.
    // Atlas (from album-blades): front u0-0.5 v0.5-1 | back u0.5-1 v0.5-1 | spine v0.4375-0.5 | dark edges v0-0.25
    internal static class CaseMesh
    {
        public static Mesh Make(float w, float h, float t)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 n)
            {
                int i = v.Count; v.AddRange(new[] { a, b, c, d }); uv.AddRange(new[] { ua, ub, uc, ud });
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) > 0) tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else tri.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            float x0 = -w / 2, x1 = w / 2, y0 = -h / 2, y1 = h / 2, z0 = -t / 2, z1 = t / 2;
            Vector2 D0 = new Vector2(0.05f, 0.05f), D1 = new Vector2(0.95f, 0.05f), D2 = new Vector2(0.95f, 0.2f), D3 = new Vector2(0.05f, 0.2f);
            Quad(new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), Vector3.forward);
            Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
                new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector3.back);
            Quad(new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.4375f), new Vector2(1f, 0.4375f), new Vector2(1f, 0.5f), Vector3.right);
            Quad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), D0, D1, D2, D3, Vector3.left);
            Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), D0, D1, D2, D3, Vector3.up);
            Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), D0, D1, D2, D3, Vector3.down);
            var m = new Mesh { name = "AlbumCase" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }
    }
}
