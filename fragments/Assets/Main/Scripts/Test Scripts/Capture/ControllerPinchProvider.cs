using UnityEngine;

namespace Found.Capture
{
    /// <summary>
    /// A pragmatic pinch provider for bring-up and editor testing. Corner A is anchored
    /// where the user first presses; corner B follows the pointer/controller. Swap this
    /// out for a HandPinchProvider (two OVRHand index-thumb pinches) once hand tracking
    /// is wired — the rest of the pipeline is identical.
    ///
    /// Wire `pointer` to the right controller anchor (OVRCameraRig > RightControllerAnchor)
    /// or leave null in the editor to use a ray from the main camera through the mouse.
    /// </summary>
    public class ControllerPinchProvider : MonoBehaviour, IPinchProvider
    {
        [Tooltip("Transform whose forward ray defines the framing pointer. Null = editor mouse ray.")]
        public Transform pointer;

        [Tooltip("Distance in front of the pointer where the frame plane sits, in metres.")]
        public float reachDistance = 0.6f;

        [Tooltip("OVRInput button that arms framing. In editor, left mouse is used instead.")]
        public bool useTriggerButton = true;

        Vector3 _a, _b;
        bool _framing, _released;

        public bool IsFraming => _framing;
        public Vector3 CornerA => _a;
        public Vector3 CornerB => _b;
        public bool ReleasedThisFrame => _released;

        void Update()
        {
            _released = false;
            bool down = ReadPressed();
            Vector3 tip = CurrentPoint();

            if (down && !_framing) { _framing = true; _a = tip; _b = tip; }
            else if (down && _framing) { _b = tip; }
            else if (!down && _framing) { _framing = false; _released = true; _b = tip; }
        }

        bool ReadPressed()
        {
#if UNITY_EDITOR
            if (pointer == null) return Input.GetMouseButton(0);
#endif
            // Replace with OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) once the SDK is in.
            // Kept as a serialized bool trigger so this file has zero Meta dependencies.
            return useTriggerButton && Input.GetMouseButton(0);
        }

        Vector3 CurrentPoint()
        {
            if (pointer != null)
                return pointer.position + pointer.forward * reachDistance;

            // Editor mouse ray fallback.
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            return r.origin + r.direction * reachDistance;
        }
    }
}
