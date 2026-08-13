using UnityEngine;

namespace Found.Journal3D
{
    /// <summary>
    /// Builds one journal page as a thick, subdivided sheet — not a paper-thin quad.
    /// The page has real depth (top surface, bottom surface, edge strip) so it reads
    /// as a physical piece of paper with visible thickness when stacked.
    ///
    /// 3 submeshes:
    ///   0 = top surface  → materials[0] (front page content)
    ///   1 = bottom surface → materials[1] (back page content)
    ///   2 = edge strip    → materials[2] (paper edge — cream colored)
    ///
    /// UVs on the bottom face are mirrored so textures read correctly from both sides.
    /// Perlin noise adds organic paper waviness. Higher subdivision = smoother curls.
    /// </summary>
    public static class PageMeshBuilder
    {
        public struct Result
        {
            public Mesh mesh;
            public Vector3[] baseVertices;
            public int columns;
            public int rows;
            public float width;
            public float thickness;
            public int topVertCount;
        }

        public static Result Build(
            int nx = 32,
            int nz = 12,
            float width = 0.16f,
            float height = 0.22f,
            float thickness = 0.0018f,
            float paperNoise = 0.002f,
            int seed = 0)
        {
            nx = Mathf.Max(4, nx);
            nz = Mathf.Max(2, nz);
            int cols = nx + 1, rows = nz + 1;
            int surfVerts = cols * rows;

            // Top surface + bottom surface + edge verts
            int edgeVerts = (cols + rows) * 2 * 2; // perimeter * 2 (top+bottom ring)
            int totalVerts = surfVerts * 2 + edgeVerts;

            var verts = new Vector3[totalVerts];
            var uvs = new Vector2[totalVerts];
            float halfT = thickness * 0.5f;
            float ox = seed * 13.37f, oy = seed * 7.71f;

            // ---- TOP SURFACE (submesh 0) ----
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float u = c / (float)nx;
                    float v = r / (float)nz;
                    float x = u * width;
                    float z = (v - 0.5f) * height;

                    // Organic paper waviness — fades to zero at spine
                    float noise1 = (Mathf.PerlinNoise(ox + u * 5f, oy + v * 5f) - 0.5f) * 2f;
                    float noise2 = (Mathf.PerlinNoise(ox + u * 11f + 3.3f, oy + v * 11f + 7.7f) - 0.5f);
                    float wob = (noise1 * 0.7f + noise2 * 0.3f) * paperNoise * Mathf.Clamp01(u * 3f);

                    int i = r * cols + c;
                    verts[i] = new Vector3(x, halfT + wob, z);
                    uvs[i] = new Vector2(u, v);
                }

            // ---- BOTTOM SURFACE (submesh 1) — mirrors top, offset down ----
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int iTop = r * cols + c;
                    int iBot = surfVerts + iTop;
                    Vector3 top = verts[iTop];
                    verts[iBot] = new Vector3(top.x, top.y - thickness, top.z);
                    uvs[iBot] = new Vector2(1f - (c / (float)nx), r / (float)nz); // mirrored U
                }

            // ---- EDGE STRIP (submesh 2) — connects top and bottom around perimeter ----
            int edgeStart = surfVerts * 2;
            int ei = 0;

            // Right edge (free edge, x = width): top row c=nx, then bottom
            for (int r = 0; r < rows; r++)
            {
                int topIdx = r * cols + nx;
                int botIdx = surfVerts + topIdx;
                verts[edgeStart + ei] = verts[topIdx];
                uvs[edgeStart + ei] = new Vector2(r / (float)nz, 1f);
                ei++;
                verts[edgeStart + ei] = verts[botIdx];
                uvs[edgeStart + ei] = new Vector2(r / (float)nz, 0f);
                ei++;
            }

            // Top edge (z = +height/2): top row r=nz
            for (int c = cols - 1; c >= 0; c--)
            {
                int topIdx = nz * cols + c;
                int botIdx = surfVerts + topIdx;
                verts[edgeStart + ei] = verts[topIdx];
                uvs[edgeStart + ei] = new Vector2(c / (float)nx, 1f);
                ei++;
                verts[edgeStart + ei] = verts[botIdx];
                uvs[edgeStart + ei] = new Vector2(c / (float)nx, 0f);
                ei++;
            }

            // Bottom edge (z = -height/2): top row r=0
            for (int c = 0; c < cols; c++)
            {
                int topIdx = c;
                int botIdx = surfVerts + topIdx;
                verts[edgeStart + ei] = verts[topIdx];
                uvs[edgeStart + ei] = new Vector2(c / (float)nx, 1f);
                ei++;
                verts[edgeStart + ei] = verts[botIdx];
                uvs[edgeStart + ei] = new Vector2(c / (float)nx, 0f);
                ei++;
            }

            // ---- TRIANGLES ----

            // Top surface — CCW from above
            var topTris = new int[nx * nz * 6];
            int t = 0;
            for (int r = 0; r < nz; r++)
                for (int c = 0; c < nx; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    topTris[t++] = a; topTris[t++] = d; topTris[t++] = b;
                    topTris[t++] = b; topTris[t++] = d; topTris[t++] = e;
                }

            // Bottom surface — reversed winding
            var botTris = new int[nx * nz * 6];
            t = 0;
            for (int r = 0; r < nz; r++)
                for (int c = 0; c < nx; c++)
                {
                    int a = surfVerts + r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    botTris[t++] = a; botTris[t++] = b; botTris[t++] = d;
                    botTris[t++] = b; botTris[t++] = e; botTris[t++] = d;
                }

            // Edge strip — quad strip from paired top/bottom edge verts
            int edgePairs = ei / 2;
            var edgeTris = new int[Mathf.Max(0, (edgePairs - 1) * 6)];
            t = 0;
            for (int p = 0; p < edgePairs - 1 && t + 5 < edgeTris.Length; p++)
            {
                int t0 = edgeStart + p * 2;
                int b0 = t0 + 1;
                int t1 = t0 + 2;
                int b1 = t0 + 3;
                edgeTris[t++] = t0; edgeTris[t++] = t1; edgeTris[t++] = b0;
                edgeTris[t++] = b0; edgeTris[t++] = t1; edgeTris[t++] = b1;
            }

            // ---- BUILD MESH ----
            var mesh = new Mesh { name = "FOUND_ThickPage" };
            if (totalVerts > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(topTris, 0);
            mesh.SetTriangles(botTris, 1);
            mesh.SetTriangles(edgeTris, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return new Result
            {
                mesh = mesh,
                baseVertices = (Vector3[])verts.Clone(),
                columns = cols,
                rows = rows,
                width = width,
                thickness = thickness,
                topVertCount = surfVerts
            };
        }
    }
}