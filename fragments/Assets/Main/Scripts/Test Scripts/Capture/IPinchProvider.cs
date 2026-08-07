using UnityEngine;

namespace Found.Capture
{
    /// <summary>
    /// Abstracts "the two points the user is framing between" so the selection logic
    /// doesn't care whether it's driven by two hand pinches, one hand + gaze, or a
    /// controller trigger drag. Provide one concrete implementation and PinchSelection
    /// just works. Keeps everything testable in the editor without a headset.
    /// </summary>
    public interface IPinchProvider
    {
        /// <summary>True while the user is actively framing (e.g. both fingers pinched).</summary>
        bool IsFraming { get; }

        /// <summary>World-space position of the first corner (e.g. left index pinch).</summary>
        Vector3 CornerA { get; }

        /// <summary>World-space position of the second corner (e.g. right index pinch).</summary>
        Vector3 CornerB { get; }

        /// <summary>Fired on the frame the user releases the pinch.</summary>
        bool ReleasedThisFrame { get; }
    }
}
