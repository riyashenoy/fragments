using UnityEngine;

namespace Found.Core
{
    /// <summary>
    /// A FOUND tool. Mirrors the web prototype's tool objects: each tool owns its
    /// activation state, an optional targeting/cursor visual, and how it responds to
    /// the shared pinch-selection gesture. Tools are plain MonoBehaviours so you can
    /// drop them on the ToolManager object and wire references in the inspector.
    /// </summary>
    public interface ITool
    {
        ToolId Id { get; }

        /// <summary>Called when this tool becomes the active tool.</summary>
        void OnActivate();

        /// <summary>Called when another tool takes over.</summary>
        void OnDeactivate();

        /// <summary>
        /// True if this tool consumes the pinch-selection gesture (sampler, tape,
        /// camera, sticker). Move / pen / eraser act directly on the journal instead.
        /// </summary>
        bool UsesEnvironmentSelection { get; }

        /// <summary>
        /// Fired by PinchSelection when the user finishes framing a region of the
        /// real world. Only called for tools where UsesEnvironmentSelection is true.
        /// </summary>
        void OnSelectionComplete(in EnvironmentSelection selection);
    }

    public enum ToolId
    {
        Move, Pen, ColorSampler, WashiTape, CameraScrap, Sticker, Eraser
    }

    /// <summary>
    /// The result of a pinch-selection: the cropped RGB texture lifted from
    /// passthrough, plus the world-space frame the user drew so we know where the
    /// fragment came from and can spawn it there.
    /// </summary>
    public struct EnvironmentSelection
    {
        public Texture2D CroppedTexture;   // pixels lifted from the passthrough camera
        public Bounds WorldBounds;         // approximate world-space frame the user drew
        public Pose CenterPose;            // pose facing the user, at the frame center
        public Vector2Int PixelSize;       // size of CroppedTexture
        public string PlaceLabel;          // e.g. "the wooden table" — best-effort scene label
    }
}
