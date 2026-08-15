using System.Collections.Generic;
using UnityEngine;

namespace Fragments.Book
{
    /// <summary>
    /// Builds a solid sheet with real thickness and, optionally, real punched
    /// holes you can see through.
    ///
    /// Submeshes (order matters — BookDragInput uses topTriangleCount):
    ///   0 = top surface     -> front page material
    ///   1 = bottom surface  -> back page material
    ///   2 = edge band       -> paper edge material
    /// </summary>
    public static class SheetMeshGenerator
    {
        public class Result
        {
            public Mesh mesh;
            public Vector3[] restVertices;
            public int topTriangleCount;   // index count of submesh 0
        }

        public class Params
        {
            public float width = 0.15f;
            public float height = 0.21f;
            public float thickness = 0.0005f;
            public int spansX = 40;
            public int spansZ = 14;
            public float cornerRadius = 0f;
            public float noise = 0f;
            public int seed = 0;
            public float[] holeZ = null;    // null = no holes
            public float holeX = 0.0085f;
            public float holeRadius = 0.0038f;
        }

        static float Rand(ref uint s)
        {
            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
            return (s & 0xFFFFFF) / 16777216f;
        }

        public static Result Generate(Params p)
        {
            float W = p.width, H = p.height, T = p.thickness;
            float hT = T * 0.5f, hH = H * 0.5f;
            uint rng = (uint)(p.seed * 7919 + 13) | 1u;

            var pos = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triTop = new List<int>();
            var triBot = new List<int>();
            var triEdge = new List<int>();

            // ---- row positions: dense across each hole, sparse between ----
            var zList = new List<float>();
            for (int i = 0; i <= p.spansZ; i++) zList.Add(-hH + H * i / p.spansZ);
            if (p.holeZ != null)
            {
                foreach (float hz in p.holeZ)
                {
                    const int N = 9;
                    for (int j = 0; j <= N; j++)
                    {
                        float z = hz - p.holeRadius + 2f * p.holeRadius * j / N;
                        if (z > -hH && z < hH) zList.Add(z);
                    }
                    if (hz - p.holeRadius - 0.0006f > -hH) zList.Add(hz - p.holeRadius - 0.0006f);
                    if (hz + p.holeRadius + 0.0006f < hH) zList.Add(hz + p.holeRadius + 0.0006f);
                }
            }
            zList.Sort();
            var rows = new List<float>();
            for (int i = 0; i < zList.Count; i++)
                if (i == 0 || zList[i] - zList[i - 1] > 1e-6f) rows.Add(zList[i]);
            int R = rows.Count;

            float DxAt(float z)
            {
                if (p.holeZ == null) return 0f;
                foreach (float hz in p.holeZ)
                {
                    float d = z - hz;
                    if (Mathf.Abs(d) < p.holeRadius)
                        return Mathf.Sqrt(p.holeRadius * p.holeRadius - d * d);
                }
                return 0f;
            }

            Vector2 Corner(float x, float z)
            {
                if (p.cornerRadius <= 1e-4f) return new Vector2(x, z);
                float df = W - x, dt = hH - z, db = z + hH;
                float cr = p.cornerRadius;
                if (df < cr && dt < cr)
                {
                    Vector2 c = new Vector2(W - cr, hH - cr);
                    Vector2 v = new Vector2(x, z) - c;
                    if (v.magnitude > cr && v.magnitude > 1e-6f) return c + v.normalized * cr;
                }
                else if (df < cr && db < cr)
                {
                    Vector2 c = new Vector2(W - cr, -hH + cr);
                    Vector2 v = new Vector2(x, z) - c;
                    if (v.magnitude > cr && v.magnitude > 1e-6f) return c + v.normalized * cr;
                }
                return new Vector2(x, z);
            }

            var noiseVals = new float[Mathf.Max(64, R * (p.spansX + 8))];
            for (int i = 0; i < noiseVals.Length; i++) noiseVals[i] = (Rand(ref rng) - 0.5f) * 2f;

            float Noise(float x, int idx)
            {
                if (p.noise <= 0f) return 0f;
                float u = Mathf.Clamp01(x / Mathf.Max(1e-5f, W));
                return Mathf.Sin(u * 1.9f + p.seed * 0.7f) * p.noise * Mathf.Min(1f, u * 2.5f)
                     + noiseVals[idx % noiseVals.Length] * p.noise * 0.07f;
            }

            // Build one grid spanning [xa(row), xb(row)] with `cols` columns.
            (int top, int bot, int w1) AddGrid(float[] xa, float[] xb, int cols)
            {
                int startTop = pos.Count;
                int ci = 0;
                for (int r = 0; r < R; r++)
                    for (int c = 0; c <= cols; c++)
                    {
                        float x = Mathf.Lerp(xa[r], xb[r], c / (float)cols);
                        Vector2 pt = Corner(x, rows[r]);
                        pos.Add(new Vector3(pt.x, hT + Noise(x, ci), pt.y));
                        uvs.Add(new Vector2(Mathf.Clamp01(pt.x / W), 1f - (rows[r] + hH) / H));
                        ci++;
                    }

                int startBot = pos.Count;
                ci = 0;
                for (int r = 0; r < R; r++)
                    for (int c = 0; c <= cols; c++)
                    {
                        float x = Mathf.Lerp(xa[r], xb[r], c / (float)cols);
                        Vector2 pt = Corner(x, rows[r]);
                        pos.Add(new Vector3(pt.x, -hT + Noise(x, ci), pt.y));
                        uvs.Add(new Vector2(1f - Mathf.Clamp01(pt.x / W), 1f - (rows[r] + hH) / H));
                        ci++;
                    }

                int w1 = cols + 1;
                for (int r = 0; r < R - 1; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        int a = startTop + r * w1 + c, b = a + 1, d = a + w1, e = d + 1;
                        triTop.Add(a); triTop.Add(d); triTop.Add(b);
                        triTop.Add(b); triTop.Add(d); triTop.Add(e);

                        int a2 = startBot + r * w1 + c, b2 = a2 + 1, d2 = a2 + w1, e2 = d2 + 1;
                        triBot.Add(a2); triBot.Add(b2); triBot.Add(d2);
                        triBot.Add(b2); triBot.Add(e2); triBot.Add(d2);
                    }
                return (startTop, startBot, w1);
            }

            var grids = new List<(int top, int bot, int w1)>();
            if (p.holeZ != null)
            {
                const int LC = 5;
                var la = new float[R]; var lb = new float[R];
                var ra = new float[R]; var rb = new float[R];
                for (int r = 0; r < R; r++)
                {
                    float dx = DxAt(rows[r]);
                    la[r] = 0f;                 lb[r] = p.holeX - dx;
                    ra[r] = p.holeX + dx;       rb[r] = W;
                }
                grids.Add(AddGrid(la, lb, LC));
                grids.Add(AddGrid(ra, rb, p.spansX));
            }
            else
            {
                var ua = new float[R]; var ub = new float[R];
                for (int r = 0; r < R; r++) { ua[r] = 0f; ub[r] = W; }
                grids.Add(AddGrid(ua, ub, p.spansX));
            }

            // ---- outer edge band around the last grid ----
            var gi = grids[grids.Count - 1];
            int cols2 = gi.w1 - 1;
            var ring = new List<int>();
            for (int r = 0; r < R; r++) ring.Add(r * gi.w1 + cols2);
            for (int c = cols2 - 1; c >= 0; c--) ring.Add((R - 1) * gi.w1 + c);
            for (int r = R - 2; r >= 0; r--) ring.Add(r * gi.w1);
            for (int c = 1; c <= cols2; c++) ring.Add(c);

            int edgeStart = pos.Count;
            for (int k = 0; k < ring.Count; k++)
            {
                pos.Add(pos[gi.top + ring[k]]); uvs.Add(new Vector2(k / (float)(ring.Count - 1), 1f));
                pos.Add(pos[gi.bot + ring[k]]); uvs.Add(new Vector2(k / (float)(ring.Count - 1), 0f));
            }
            for (int k = 0; k < ring.Count - 1; k++)
            {
                int t0 = edgeStart + k * 2, b0 = t0 + 1, t1 = t0 + 2, b1 = t0 + 3;
                triEdge.Add(t0); triEdge.Add(t1); triEdge.Add(b0);
                triEdge.Add(b0); triEdge.Add(t1); triEdge.Add(b1);
            }

            // ---- hole inner walls ----
            if (p.holeZ != null && grids.Count == 2)
            {
                var L = grids[0]; var Rg = grids[1];
                for (int r = 0; r < R - 1; r++)
                {
                    if (DxAt(rows[r]) <= 1e-6f && DxAt(rows[r + 1]) <= 1e-6f) continue;

                    int lt0 = L.top + r * L.w1 + L.w1 - 1, lb0 = L.bot + r * L.w1 + L.w1 - 1;
                    int lt1 = L.top + (r + 1) * L.w1 + L.w1 - 1, lb1 = L.bot + (r + 1) * L.w1 + L.w1 - 1;
                    int rt0 = Rg.top + r * Rg.w1, rb0 = Rg.bot + r * Rg.w1;
                    int rt1 = Rg.top + (r + 1) * Rg.w1, rb1 = Rg.bot + (r + 1) * Rg.w1;

                    int s0 = pos.Count;
                    pos.Add(pos[lt0]); uvs.Add(new Vector2(0, 1));
                    pos.Add(pos[lb0]); uvs.Add(new Vector2(0, 0));
                    pos.Add(pos[lt1]); uvs.Add(new Vector2(1, 1));
                    pos.Add(pos[lb1]); uvs.Add(new Vector2(1, 0));
                    triEdge.Add(s0); triEdge.Add(s0 + 1); triEdge.Add(s0 + 2);
                    triEdge.Add(s0 + 2); triEdge.Add(s0 + 1); triEdge.Add(s0 + 3);

                    int s1 = pos.Count;
                    pos.Add(pos[rt0]); uvs.Add(new Vector2(0, 1));
                    pos.Add(pos[rb0]); uvs.Add(new Vector2(0, 0));
                    pos.Add(pos[rt1]); uvs.Add(new Vector2(1, 1));
                    pos.Add(pos[rb1]); uvs.Add(new Vector2(1, 0));
                    triEdge.Add(s1); triEdge.Add(s1 + 2); triEdge.Add(s1 + 1);
                    triEdge.Add(s1 + 1); triEdge.Add(s1 + 2); triEdge.Add(s1 + 3);
                }
            }

            var mesh = new Mesh { name = "Fragments_Sheet" };
            if (pos.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(pos);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(triTop, 0);
            mesh.SetTriangles(triBot, 1);
            mesh.SetTriangles(triEdge, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return new Result
            {
                mesh = mesh,
                restVertices = pos.ToArray(),
                topTriangleCount = triTop.Count
            };
        }
    }
}
