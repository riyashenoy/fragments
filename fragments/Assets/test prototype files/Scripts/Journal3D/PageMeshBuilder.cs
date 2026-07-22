using UnityEngine;

namespace Found.Journal3D
{
    /// <summary>
    /// Builds one journal page as a subdivided, double-sided sheet with TWO submeshes —
    /// front and back — so the cover shell and the paper can each take their own
    /// material, and a leaf's front and back faces can even differ. UVs on the back face
    /// are mirrored so a texture reads correctly from either side. A little Perlin
    /// waviness is baked in for imperfect-paper feel.
    ///
    /// Local layout (matches Journal.cs):
    ///   • hinge (spine) at X = 0, page extends toward +X to the free edge at X = width
    ///   • page height runs along Z, from -height/2 to +height/2
    ///   • page lies in the local XZ plane, surface normal = +Y
    /// The turn hinge is therefore the local Z axis, and page curl displaces along +Y.
    /// </summary>
    public static class PageMeshBuilder
    {
        public struct Result
        {
            public Mesh mesh;
            public Vector3[] baseVertices;   // flat + paper noise, before any curl
            public int columns;              // verts across width (nx + 1)
            public int rows;                 // verts across height (nz + 1)
            public float width;
        }

        public static Result Build(int nx, int nz, float width, float height,
                                   float paperNoise = 0.0012f, int seed = 0)
        {
            nx = Mathf.Max(2, nx);
            nz = Mathf.Max(1, nz);
            int cols = nx + 1, rows = nz + 1;
            int perSide = cols * rows;

            var verts = new Vector3[perSide * 2];   // [0..perSide) front, [perSide..) back
            var uvs   = new Vector2[perSide * 2];
            float ox = seed * 13.13f, oy = seed * 7.77f;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float u = c / (float)nx;                 // 0 at hinge, 1 at free edge
                    float v = r / (float)nz;
                    float x = u * width;
                    float z = (v - 0.5f) * height;

                    // Gentle paper undulation, faded to zero at the spine so binding stays flat.
                    float wob = (Mathf.PerlinNoise(ox + u * 4f, oy + v * 4f) - 0.5f) * 2f;
                    float y = wob * paperNoise * Mathf.Clamp01(u * 3f);

                    int i = r * cols + c;
                    var p = new Vector3(x, y, z);
                    verts[i] = p;
                    verts[perSide + i] = p;                  // back shares position (double-sided)
                    uvs[i] = new Vector2(u, v);
                    uvs[perSide + i] = new Vector2(1f - u, v); // mirror back UVs
                }

            // Front triangles (normal +Y), CCW when viewed from above.
            var front = new int[nx * nz * 6];
            int t = 0;
            for (int r = 0; r < nz; r++)
                for (int c = 0; c < nx; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    front[t++] = a; front[t++] = d; front[t++] = b;
                    front[t++] = b; front[t++] = d; front[t++] = e;
                }

            // Back triangles (normal -Y), reversed winding, offset into the back vertex block.
            var back = new int[nx * nz * 6];
            t = 0;
            for (int r = 0; r < nz; r++)
                for (int c = 0; c < nx; c++)
                {
                    int a = perSide + r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    back[t++] = a; back[t++] = b; back[t++] = d;
                    back[t++] = b; back[t++] = e; back[t++] = d;
                }

            var mesh = new Mesh { name = "FOUND_Page" };
            mesh.indexFormat = verts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(front, 0);   // submesh 0 = front face  → materials[0]
            mesh.SetTriangles(back, 1);    // submesh 1 = back face   → materials[1]
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return new Result
            {
                mesh = mesh,
                baseVertices = (Vector3[])verts.Clone(),
                columns = cols,
                rows = rows,
                width = width
            };
        }
    }
}
