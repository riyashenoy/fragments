using UnityEngine;

namespace Found.Capture
{
    /// <summary>
    /// The ONE seam between FOUND and the Meta SDK. Everything the app needs from
    /// passthrough is expressed here, so the rest of the codebase compiles on a blank
    /// project with no SDK installed. Provide a real implementation
    /// (MetaPassthroughSource) once Meta XR Core SDK + MRUK v81+ are imported.
    /// </summary>
    public interface IPassthroughSource
    {
        /// <summary>True once camera permission is granted and a frame is available.</summary>
        bool IsReady { get; }

        /// <summary>The live camera texture (GPU). Do not read pixels off this directly every frame.</summary>
        Texture CurrentTexture { get; }

        /// <summary>Pixel dimensions of CurrentTexture.</summary>
        Vector2Int Resolution { get; }

        /// <summary>
        /// Project a world-space point to a normalized UV (0..1) in the camera texture,
        /// using the camera's pose + intrinsics for the current frame. Returns false if
        /// the point is behind the camera or off-frame.
        /// </summary>
        bool WorldToCameraUV(Vector3 worldPoint, out Vector2 uv);

        /// <summary>The pose of the physical RGB camera this frame, in world space.</summary>
        Pose CameraPose { get; }
    }
}
