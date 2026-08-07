using UnityEngine;
using Found.Core;

namespace Found.Capture
{
    /// <summary>
    /// The heart of the "collect a fragment from the place itself" mechanic. Given the
    /// two world-space corners the user framed, it:
    ///   1. Projects those corners into the passthrough camera texture (UV space).
    ///   2. Does a single GPU->CPU readback of just that rectangle (the one real cost).
    ///   3. Returns a tidy Texture2D + world Pose so a tool can build tape/photo/sticker.
    ///
    /// SDK-agnostic: it only talks to IPassthroughSource. Wire a MetaPassthroughSource
    /// (or, in editor, an EditorPassthroughSource that samples a RenderTexture of a fake
    /// café) in the inspector.
    /// </summary>
    public class PassthroughCapture : MonoBehaviour
    {
        [Tooltip("Component implementing IPassthroughSource. See MetaPassthroughSource / EditorPassthroughSource.")]
        public MonoBehaviour passthroughSourceBehaviour;

        [Tooltip("Optional: MRUK / scene labeller to describe where a fragment came from.")]
        public ScenePlaceLabeller placeLabeller;

        [Range(64, 1024)]
        public int maxCropSize = 512;

        IPassthroughSource _source;

        void Awake()
        {
            _source = passthroughSourceBehaviour as IPassthroughSource;
            if (_source == null)
                Debug.LogError("[FOUND] PassthroughCapture needs a component implementing IPassthroughSource.");
        }

        public bool Ready => _source != null && _source.IsReady;

        /// <summary>
        /// Build an EnvironmentSelection from two world-space corners. Returns false if
        /// the frame is too small, the camera isn't ready, or the region is off-screen.
        /// </summary>
        public bool TryCapture(Vector3 cornerA, Vector3 cornerB, out EnvironmentSelection result)
        {
            result = default;
            if (_source == null || !_source.IsReady) return false;

            // World frame -> camera UVs.
            if (!_source.WorldToCameraUV(cornerA, out var uvA)) return false;
            if (!_source.WorldToCameraUV(cornerB, out var uvB)) return false;

            Vector2 uvMin = Vector2.Min(uvA, uvB);
            Vector2 uvMax = Vector2.Max(uvA, uvB);
            Vector2 uvSize = uvMax - uvMin;
            if (uvSize.x < 0.02f || uvSize.y < 0.02f) return false; // too small to be intentional

            var res = _source.Resolution;
            var srcTex = _source.CurrentTexture;
            if (srcTex == null) return false;

            int px = Mathf.RoundToInt(uvMin.x * res.x);
            int py = Mathf.RoundToInt(uvMin.y * res.y);
            int pw = Mathf.RoundToInt(uvSize.x * res.x);
            int ph = Mathf.RoundToInt(uvSize.y * res.y);
            px = Mathf.Clamp(px, 0, res.x - 2);
            py = Mathf.Clamp(py, 0, res.y - 2);
            pw = Mathf.Clamp(pw, 2, res.x - px);
            ph = Mathf.Clamp(ph, 2, res.y - py);

            Texture2D crop = ReadbackRegion(srcTex, px, py, pw, ph);
            if (crop == null) return false;

            // Compute a world Pose facing the user at the frame centre so tools can spawn there.
            Vector3 center = (cornerA + cornerB) * 0.5f;
            Vector3 toUser = (_source.CameraPose.position - center);
            toUser.y = 0f;
            Quaternion rot = toUser.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(-toUser.normalized, Vector3.up)
                : Quaternion.identity;

            result = new EnvironmentSelection
            {
                CroppedTexture = crop,
                WorldBounds = GeometryUtility_BoundsFromCorners(cornerA, cornerB),
                CenterPose = new Pose(center, rot),
                PixelSize = new Vector2Int(crop.width, crop.height),
                PlaceLabel = placeLabeller != null ? placeLabeller.LabelAt(center) : "the café"
            };
            return true;
        }

        /// <summary>
        /// Blit the requested region into a RenderTexture, then read it back once.
        /// Downscales to maxCropSize to keep readback cheap and materials light.
        /// </summary>
        Texture2D ReadbackRegion(Texture src, int x, int y, int w, int h)
        {
            float scale = Mathf.Min(1f, maxCropSize / (float)Mathf.Max(w, h));
            int outW = Mathf.Max(2, Mathf.RoundToInt(w * scale));
            int outH = Mathf.Max(2, Mathf.RoundToInt(h * scale));

            var rt = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;

            // Source sub-rect in normalized coords for Graphics.Blit scale/offset.
            Vector2 sc = new((float)w / src.width, (float)h / src.height);
            Vector2 of = new((float)x / src.width, (float)y / src.height);
            Graphics.Blit(src, rt, sc, of);

            RenderTexture.active = rt;
            var tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        static Bounds GeometryUtility_BoundsFromCorners(Vector3 a, Vector3 b)
        {
            var bounds = new Bounds(a, Vector3.zero);
            bounds.Encapsulate(b);
            return bounds;
        }
    }
}
