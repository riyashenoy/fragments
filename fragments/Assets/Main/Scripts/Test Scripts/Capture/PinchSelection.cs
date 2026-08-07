using UnityEngine;
using Found.Core;

namespace Found.Capture
{
    /// <summary>
    /// Watches the pinch provider whenever the active tool wants an environment
    /// selection, draws a live frame quad between the two corners (the XR equivalent of
    /// the web prototype's translucent selection rectangle + pinch handles), and on
    /// release asks PassthroughCapture to lift the region, then hands the result to the
    /// active tool via ToolManager.DispatchSelection.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PinchSelection : MonoBehaviour
    {
        public ToolManager toolManager;
        public PassthroughCapture capture;

        [Tooltip("Component implementing IPinchProvider (ControllerPinchProvider / HandPinchProvider).")]
        public MonoBehaviour pinchProviderBehaviour;

        [Header("Frame visuals")]
        public Transform handleA;      // small sphere shown at corner A while framing
        public Transform handleB;      // small sphere shown at corner B
        public Color frameColor = new(0.97f, 0.93f, 0.84f, 0.9f);

        IPinchProvider _pinch;
        LineRenderer _line;
        bool _armedLastFrame;

        void Awake()
        {
            _pinch = pinchProviderBehaviour as IPinchProvider;
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 5;
            _line.loop = false;
            _line.widthMultiplier = 0.004f;
            _line.startColor = _line.endColor = frameColor;
            SetVisible(false);
        }

        void Update()
        {
            bool armed = toolManager != null && toolManager.WantsSelection;

            if (!armed)
            {
                if (_armedLastFrame) SetVisible(false);
                _armedLastFrame = false;
                return;
            }
            _armedLastFrame = true;

            if (_pinch == null) return;

            if (_pinch.IsFraming)
            {
                DrawFrame(_pinch.CornerA, _pinch.CornerB);
                SetVisible(true);
            }

            if (_pinch.ReleasedThisFrame)
            {
                SetVisible(false);
                if (capture != null &&
                    capture.TryCapture(_pinch.CornerA, _pinch.CornerB, out var sel))
                {
                    toolManager.DispatchSelection(sel);
                }
                else
                {
                    FoundEvents.Toast("Frame a little wider — like pinching two fingers apart.");
                }
            }
        }

        void DrawFrame(Vector3 a, Vector3 b)
        {
            // Build a rectangle in the plane facing the camera between the two corners.
            Vector3 center = (a + b) * 0.5f;
            Vector3 camPos = Camera.main ? Camera.main.transform.position : center + Vector3.back;
            Vector3 normal = (camPos - center).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, normal).normalized;
            up = Vector3.Cross(normal, right).normalized;

            Vector3 d = b - a;
            float halfW = Mathf.Abs(Vector3.Dot(d, right)) * 0.5f;
            float halfH = Mathf.Abs(Vector3.Dot(d, up)) * 0.5f;

            Vector3 c0 = center - right * halfW - up * halfH;
            Vector3 c1 = center + right * halfW - up * halfH;
            Vector3 c2 = center + right * halfW + up * halfH;
            Vector3 c3 = center - right * halfW + up * halfH;

            _line.SetPositions(new[] { c0, c1, c2, c3, c0 });

            if (handleA) handleA.position = a;
            if (handleB) handleB.position = b;
        }

        void SetVisible(bool v)
        {
            if (_line) _line.enabled = v;
            if (handleA) handleA.gameObject.SetActive(v);
            if (handleB) handleB.gameObject.SetActive(v);
        }
    }
}
