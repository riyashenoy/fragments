using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragments.Book
{
    /// <summary>
    /// Mouse / XR input that mirrors the v10 prototype's beginGrab / moveGrab / endGrab.
    /// Pointer math is done in book-local space (the HTML book's world space).
    /// </summary>
    public class BookDragInput : MonoBehaviour
    {
        [SerializeField] Book book;
        [SerializeField] Camera mainCamera;

        BookSheet _held;
        int _heldDir = 1;

        void OnEnable()
        {
            if (book == null) book = GetComponent<Book>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) BeginGrab(pos);
                if (_held != null && mouse.leftButton.isPressed) MoveGrab(pos);
                if (mouse.leftButton.wasReleasedThisFrame && _held != null) EndGrab();
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rightArrowKey.wasPressedThisFrame) book.TurnForward();
                if (kb.leftArrowKey.wasPressedThisFrame) book.TurnBackward();
            }
        }

        // ------------------------------------------------------------------
        // beginGrab
        bool BeginGrab(Vector2 screenPos)
        {
            if (book == null || book.Busy || mainCamera == null) return false;
            return BeginGrabRay(mainCamera.ScreenPointToRay(screenPos));
        }

        bool BeginGrabRay(Ray ray)
        {
            if (book.Busy) return false;

            var next = book.NextSheet;
            var prev = book.PrevSheet;
            if (next == null && prev == null) return false;

            Cook(next);
            Cook(prev);

            BookSheet sheet = null;
            RaycastHit hit = default;
            float best = float.MaxValue;
            TryCandidate(next, ray, ref sheet, ref hit, ref best);
            TryCandidate(prev, ray, ref sheet, ref hit, ref best);
            if (sheet == null) return false;

            MaterialPoint m = MaterialAt(sheet, hit);
            if (!sheet.IsBoard && !IsOuterEdge(sheet, m.x, m.z)) return false;

            int dir = (sheet == next) ? 1 : -1;
            Vector3 localHit = ToBookLocal(hit.point);

            _held = sheet;
            _heldDir = dir;
            sheet.BeginDrag(new Vector2(m.x, m.z), localHit);
            return true;
        }

        // ------------------------------------------------------------------
        // moveGrab
        void MoveGrab(Vector2 screenPos)
        {
            if (_held == null || mainCamera == null) return;
            if (!HitBookPlane(mainCamera.ScreenPointToRay(screenPos), PlaneY(_held), out Vector3 local))
                return;
            _held.DragTo(local);
        }

        void MoveGrabWorld(Vector3 worldPoint)
        {
            if (_held == null) return;
            _held.DragTo(ToBookLocal(worldPoint));
        }

        // ------------------------------------------------------------------
        // endGrab
        void EndGrab()
        {
            if (_held == null) return;
            BookSheet s = _held;
            int dir = _heldDir;
            _held = null;

            bool past = s.PastMidpoint;
            if (dir > 0)
            {
                if (past) s.StartAuto(true, book.StackY(s.index, book.TurnedCount + 1));
                else s.SettleTo(false, book.StackY(s.index, book.TurnedCount));
            }
            else
            {
                if (!past) s.StartAuto(false, book.StackY(s.index, book.TurnedCount - 1));
                else s.SettleTo(true, book.StackY(s.index, book.TurnedCount));
            }
        }

        // ------------------------------------------------------------------
        // XR: same three functions, driven by a world-space pinch
        public void BeginPeel(Vector3 worldPosition)
        {
            if (book == null) return;
            var ray = new Ray(worldPosition + book.transform.up * 0.08f, -book.transform.up);
            if (!BeginGrabRay(ray))
            {
                // fallback: treat the pinch as a hit on the nearer pile
                Vector3 local = ToBookLocal(worldPosition);
                BookSheet s = local.x >= 0f ? book.NextSheet : book.PrevSheet;
                if (s == null || book.Busy) return;
                if (!s.IsBoard && !IsOuterEdge(s, Mathf.Clamp(local.x, 0f, s.width), local.z))
                    return;
                _held = s;
                _heldDir = (s == book.NextSheet) ? 1 : -1;
                s.BeginDrag(new Vector2(Mathf.Clamp(local.x, 0f, s.width),
                                        Mathf.Clamp(local.z, -s.height * 0.5f, s.height * 0.5f)),
                            local);
            }
        }

        public void UpdatePeel(Vector3 worldPosition) => MoveGrabWorld(worldPosition);
        public void EndPeel() => EndGrab();

        // ------------------------------------------------------------------
        struct MaterialPoint { public float x, z; public bool top; }

        static MaterialPoint MaterialAt(BookSheet s, RaycastHit hit)
        {
            // prototype: isTop = (faceIndex * 3) < topCount
            bool top = (hit.triangleIndex * 3) < s.topTriangleCount;
            Vector2 uv = hit.textureCoord;
            float u = uv.x;
            if (!top) u = 1f - u;
            return new MaterialPoint
            {
                x = u * s.width,
                z = (1f - uv.y - 0.5f) * s.height,
                top = top
            };
        }

        // prototype outer(): free-edge strip + two outer corners
        static bool IsOuterEdge(BookSheet s, float mx, float mz)
        {
            float w = s.width, h = s.height;
            float band = w * 0.34f;
            float cr = Mathf.Min(w, h) * 0.32f;
            if (mx > w - band) return true;
            return Vector2.Distance(new Vector2(mx, mz), new Vector2(w, h * 0.5f)) < cr
                || Vector2.Distance(new Vector2(mx, mz), new Vector2(w, -h * 0.5f)) < cr;
        }

        float PlaneY(BookSheet s) => s.IsBoard ? book.CoverTop * 0.5f : s.StackY;

        bool HitBookPlane(Ray ray, float localY, out Vector3 localPoint)
        {
            Vector3 origin = book.transform.TransformPoint(new Vector3(0f, localY, 0f));
            var plane = new Plane(book.transform.up, origin);
            if (!plane.Raycast(ray, out float dist))
            {
                localPoint = default;
                return false;
            }
            localPoint = ToBookLocal(ray.GetPoint(dist));
            return true;
        }

        Vector3 ToBookLocal(Vector3 world) => book.transform.InverseTransformPoint(world);

        static void TryCandidate(BookSheet s, Ray ray, ref BookSheet best, ref RaycastHit bestHit, ref float bestDist)
        {
            if (s == null) return;
            var mc = s.GetComponent<MeshCollider>();
            if (mc == null) return;
            if (!mc.Raycast(ray, out RaycastHit hit, 1000f)) return;
            if (hit.distance >= bestDist) return;
            bestDist = hit.distance;
            bestHit = hit;
            best = s;
        }

        static void Cook(BookSheet s)
        {
            if (s == null) return;
            var mc = s.GetComponent<MeshCollider>();
            if (mc == null) return;
            var mesh = s.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) return;
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }
}
