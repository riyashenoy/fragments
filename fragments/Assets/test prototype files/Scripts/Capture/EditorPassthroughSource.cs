using UnityEngine;

namespace Found.Capture
{
    /// <summary>
    /// A stand-in passthrough source for building and testing the whole capture → tool →
    /// fragment pipeline in the editor with NO headset and NO Meta SDK. Point `sourceCamera`
    /// at a Camera that renders a mock café (or assign a static `fallbackTexture`), and the
    /// framework behaves exactly as it will on device — you just capture from a virtual
    /// café instead of a real one. This is how you iterate on tools quickly.
    /// </summary>
    public class EditorPassthroughSource : MonoBehaviour, IPassthroughSource
    {
        [Tooltip("A camera that renders your mock café scene. Its target texture is 'the world'.")]
        public Camera sourceCamera;

        [Tooltip("Or a static image to sample from if you have no mock camera.")]
        public Texture2D fallbackTexture;

        public Vector2Int resolution = new(1280, 960);

        RenderTexture _rt;

        void Awake()
        {
            if (sourceCamera != null)
            {
                _rt = new RenderTexture(resolution.x, resolution.y, 16);
                sourceCamera.targetTexture = _rt;
            }
        }

        public bool IsReady => (sourceCamera != null) || fallbackTexture != null;

        public Texture CurrentTexture => sourceCamera != null ? (Texture)_rt : fallbackTexture;

        public Vector2Int Resolution =>
            fallbackTexture != null && sourceCamera == null
                ? new Vector2Int(fallbackTexture.width, fallbackTexture.height)
                : resolution;

        public Pose CameraPose =>
            sourceCamera != null
                ? new Pose(sourceCamera.transform.position, sourceCamera.transform.rotation)
                : new Pose(Vector3.zero, Quaternion.identity);

        public bool WorldToCameraUV(Vector3 worldPoint, out Vector2 uv)
        {
            uv = default;
            var cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(worldPoint);
            if (vp.z <= 0f) return false;
            uv = new Vector2(vp.x, vp.y);
            return uv.x is >= 0f and <= 1f && uv.y is >= 0f and <= 1f;
        }
    }
}
