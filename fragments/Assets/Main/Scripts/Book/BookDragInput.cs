using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Fragments.Book
{
    /// <summary>
    /// Drag-to-peel input. Mouse on desktop; call BeginPeel/UpdatePeel/EndPeel
    /// directly from an XR interactor with a world position and nothing else
    /// needs to change.
    ///
    /// The grabbed point is derived from the raycast UV (material coordinates),
    /// NOT from world position. A turned sheet's vertices already live at
    /// negative x — there is no mirroring — so using UV is what makes backward
    /// dragging behave identically to forward.
    /// </summary>
    public class BookDragInput : MonoBehaviour
    {
        public Book book;
        public Camera inputCamera;
        [Tooltip("Fraction of the page width, measured from the outer edge, that can be grabbed.")]
        [Range(0.1f, 0.6f)] public float grabBand = 0.34f;

        BookSheet _held;
        int _heldDir = 1;

        void Awake() { if (inputCamera == null) inputCamera = Camera.main; }

        void Update()
        {
            if (book == null || inputCamera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Ray r = inputCamera.ScreenPointToRay(Input.mousePosition);
                TryBeginPeel(r);
            }
            else if (Input.GetMouseButton(0) && _held != null)
            {
                Ray r = inputCamera.ScreenPointToRay(Input.mousePosition);
                if (PlaneHit(r, _held.StackY, out Vector3 wp)) _held.DragTo(wp);
            }
            else if (Input.GetMouseButtonUp(0) && _held != null)
            {
                EndPeel();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow)) book.TurnForward();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) book.TurnBackward();
        }

        bool PlaneHit(Ray ray, float y, out Vector3 point)
        {
            var plane = new Plane(book.transform.up, book.transform.position + Vector3.up * y);
            if (plane.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
            point = default; return false;
        }

        public bool TryBeginPeel(Ray ray)
        {
            if (book.Busy) return false;

            var cands = new System.Collections.Generic.List<BookSheet>();
            if (book.NextSheet != null) cands.Add(book.NextSheet);
            if (book.PrevSheet != null) cands.Add(book.PrevSheet);
            if (cands.Count == 0) return false;

            RaycastHit best = default; BookSheet hitSheet = null; float bestD = float.MaxValue;
            foreach (var s in cands)
            {
                var mc = s.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = s.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = s.GetComponent<MeshFilter>().sharedMesh;
                }
                if (mc.Raycast(ray, out RaycastHit h, 100f) && h.distance < bestD)
                { bestD = h.distance; best = h; hitSheet = s; }
            }
            if (hitSheet == null) return false;

            // material coordinates from UV — correct regardless of current bend
            bool isTop = best.triangleIndex * 3 < hitSheet.topTriangleCount;
            float u = best.textureCoord.x;
            if (!isTop) u = 1f - u;
            var material = new Vector2(u * hitSheet.width,
                                       (1f - best.textureCoord.y - 0.5f) * hitSheet.height);

            if (!hitSheet.IsBoard && !InGrabZone(hitSheet, material)) return false;

            _held = hitSheet;
            _heldDir = (hitSheet == book.NextSheet) ? 1 : -1;
            _held.BeginDrag(material, best.point);
            return true;
        }

        bool InGrabZone(BookSheet s, Vector2 m)
        {
            float band = s.width * grabBand;
            if (m.x > s.width - band) return true;
            float cr = Mathf.Min(s.width, s.height) * 0.32f;
            return Vector2.Distance(m, new Vector2(s.width, s.height * 0.5f)) < cr
                || Vector2.Distance(m, new Vector2(s.width, -s.height * 0.5f)) < cr;
        }

        public void UpdatePeel(Vector3 worldPoint) { if (_held != null) _held.DragTo(worldPoint); }

        public void EndPeel()
        {
            if (_held == null) return;
            var s = _held; int dir = _heldDir; _held = null;

            int idx = book.Sheets.IndexOf(s);
            bool past = s.PastMidpoint;

            if (dir > 0)
            {
                if (past) s.StartAuto(true, book.StackY(idx, book.TurnedCount + 1));
                else s.SettleTo(false, book.StackY(idx, book.TurnedCount));
            }
            else
            {
                if (!past) s.StartAuto(false, book.StackY(idx, book.TurnedCount - 1));
                else s.SettleTo(true, book.StackY(idx, book.TurnedCount));
            }
        }

        public bool IsPeeling => _held != null;
    }
}
