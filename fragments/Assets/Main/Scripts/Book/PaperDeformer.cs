using UnityEngine;

namespace Fragments.Book
{
    /// <summary>
    /// The isometric page-fold. One rule: a vertex at material distance t past
    /// the fold line is placed at ARC LENGTH t along a cylinder, then continues
    /// along the tangent. Distance along the sheet is preserved exactly, so the
    /// paper bends but can never stretch.
    ///
    /// All maths is in the sheet's local space:
    ///   x = 0 at the binding, x = width at the free edge
    ///   z spans -height/2 .. +height/2
    ///   y is sheet thickness
    /// </summary>
    public struct FoldState
    {
        public float ox, oz;    // a point the crease passes through
        public float nx, nz;    // unit normal, pointing at the material that lifts
        public float radius;    // cylinder radius
        public float maxAngle;  // radians wrapped before continuing tangentially
        public float sag;       // extra wrap on outer rows (still arc-length preserving)
        public float height;    // sheet height, for the sag falloff
        public bool valid;
    }

    public static class PaperDeformer
    {
        /// <summary>Deform rest vertices into dest. Same length required.</summary>
        public static void Apply(Vector3[] rest, Vector3[] dest, in FoldState f)
        {
            if (!f.valid || rest == null || dest == null || rest.Length != dest.Length)
            {
                if (rest != null && dest != null && rest.Length == dest.Length)
                    System.Array.Copy(rest, dest, rest.Length);
                return;
            }

            float nx = f.nx, nz = f.nz;
            float tx = -nz, tz = nx;
            float R = Mathf.Max(0.0005f, f.radius);
            float halfH = Mathf.Max(0.001f, f.height * 0.5f);

            for (int i = 0; i < rest.Length; i++)
            {
                Vector3 v = rest[i];
                float rx = v.x - f.ox;
                float rz = v.z - f.oz;
                float t = rx * nx + rz * nz;

                if (t <= 0f) { dest[i] = v; continue; }

                float s = rx * tx + rz * tz;

                // Outer rows wrap a little further -> gravity sag, still isometric.
                float maxA = f.maxAngle;
                if (f.sag > 0f)
                {
                    float af = Mathf.Abs(s) / halfH;
                    maxA += f.sag * af * af;
                }

                float arcLen = maxA * R;
                float alongN, h;

                if (t <= arcLen)
                {
                    float th = t / R;
                    alongN = R * Mathf.Sin(th);
                    h = R * (1f - Mathf.Cos(th));
                }
                else
                {
                    float extra = t - arcLen;
                    alongN = R * Mathf.Sin(maxA) + extra * Mathf.Cos(maxA);
                    h = R * (1f - Mathf.Cos(maxA)) + extra * Mathf.Sin(maxA);
                }

                dest[i] = new Vector3(
                    f.ox + nx * alongN + tx * s,
                    v.y + h,
                    f.oz + nz * alongN + tz * s);
            }
        }

        /// <summary>
        /// How far the grabbed point A may travel toward G before the crease
        /// would cross the bound edge. Checked against the WHOLE bind line in
        /// every direction, which is what makes a page impossible to tear off.
        /// </summary>
        public static float MaxTravel(Vector2 A, Vector2 G, float bindX, float height)
        {
            Vector2 d = G - A;
            float dist = d.magnitude;
            if (dist < 1e-6f) return 0f;

            Vector2 u = d / dist;
            float zEdge = (u.y > 0f) ? (-height * 0.5f) : (height * 0.5f);
            float minDot = (bindX - A.x) * u.x + (zEdge - A.y) * u.y;
            return Mathf.Max(0f, 2f * minDot);
        }

        /// <summary>Project a drag target into the region the sheet can physically reach.</summary>
        public static Vector2 Clamp(Vector2 A, Vector2 G, float bindX, float height)
        {
            Vector2 d = G - A;
            float dist = d.magnitude;
            if (dist < 1e-6f) return G;

            float max = MaxTravel(A, G, bindX, height);
            if (dist > max) dist = max;
            return A + d.normalized * dist;
        }

        /// <summary>
        /// Fold material point A onto target G. The crease is the perpendicular
        /// bisector of A->G, which is literally how paper folds, so the grabbed
        /// point stays attached to the pointer.
        /// </summary>
        public static FoldState PaperFold(Vector2 A, Vector2 G, float bindX,
                                          float radius, float sag, float height)
        {
            FoldState f = default;
            Vector2 d = G - A;
            float dist = d.magnitude;
            if (dist < 1e-5f) return f;

            float max = MaxTravel(A, G, bindX, height);
            if (dist > max) dist = max;
            if (dist < 1e-5f) return f;

            Vector2 u = d.normalized;
            Vector2 origin = A + u * (dist * 0.5f);

            f.ox = origin.x; f.oz = origin.y;
            f.nx = -u.x;     f.nz = -u.y;
            f.radius = radius;
            f.maxAngle = Mathf.PI;
            f.sag = sag;
            f.height = height;
            f.valid = true;
            return f;
        }

        /// <summary>The fully-turned resting target for a sheet grabbed at its free edge.</summary>
        public static Vector2 FullyTurnedTarget(Vector2 A, float bindX, float height)
        {
            float m = MaxTravel(A, new Vector2(-1f, A.y), bindX, height);
            return new Vector2(A.x - m, A.y);
        }
    }
}
