using UnityEngine;
using UnityEngine.Events;

namespace Found.Journal3D
{
    /// <summary>
    /// Drag a page's free corner to drive its turn, then release to snap open or closed —
    /// the tactile "drag a page edge to flip" from the brief. Attach to a small collider
    /// at the free edge of the topmost page, or drive it from your hand-grab interactor by
    /// calling BeginDrag / UpdateDrag(worldPoint) / EndDrag.
    ///
    /// Works with mouse in the editor out of the box (no XR needed) so you can feel the
    /// flip immediately.
    /// </summary>
    public class PageCornerHandle : MonoBehaviour
    {
        public Journal journal;
        [Tooltip("How far (metres) the corner travels for a full 0→1 turn.")]
        public float dragSpan = 0.16f;

        JournalPage _leaf;
        Vector3 _start;
        bool _dragging;
        bool _forward;

        // ---- programmatic API (call these from an XR grab interactor) --------------

        public void BeginDrag(Vector3 worldPoint)
        {
            if (journal == null || journal.IsBusy) return;
            // Turning forward if the book isn't fully open at this leaf; else turning back.
            _forward = journal.TurnedCount < journal.leaves.Count;
            int idx = _forward ? journal.TurnedCount : journal.TurnedCount - 1;
            if (idx < 0 || idx >= journal.leaves.Count) return;
            _leaf = journal.leaves[idx];
            _leaf.transform.SetAsLastSibling();
            _start = worldPoint;
            _dragging = true;
        }

        public void UpdateDrag(Vector3 worldPoint)
        {
            if (!_dragging || _leaf == null) return;
            float travelled = Vector3.Distance(worldPoint, _start);
            float p = Mathf.Clamp01(travelled / dragSpan);
            _leaf.SetProgress(_forward ? p : 1f - p);
        }

        public void EndDrag()
        {
            if (!_dragging || _leaf == null) { _dragging = false; return; }
            _dragging = false;
            bool past = _leaf.Progress > 0.5f;
            _leaf.SnapTo(past);
            // Keep the Journal's counter in sync with the user's manual flip.
            if (past && _forward) journal.SendMessage("Next", SendMessageOptions.DontRequireReceiver);
            else if (!past && !_forward) journal.SendMessage("Prev", SendMessageOptions.DontRequireReceiver);
            _leaf = null;
        }

        // ---- editor mouse convenience ---------------------------------------------
#if UNITY_EDITOR
        void OnMouseDown()  => BeginDrag(MouseWorld());
        void OnMouseDrag()  => UpdateDrag(MouseWorld());
        void OnMouseUp()    => EndDrag();
        Vector3 MouseWorld()
        {
            var cam = Camera.main; if (!cam) return transform.position;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, transform.position);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : transform.position;
        }
#endif
    }

    /// <summary>
    /// Click / poke the closed cover to open the book (web parity). Put on the front
    /// cover's collider, or call Poke() from an XR poke interactor.
    /// </summary>
    public class JournalPokeOpen : MonoBehaviour
    {
        public Journal journal;
        public void Poke() { if (journal && journal.IsClosed) journal.Open(); }
#if UNITY_EDITOR
        void OnMouseDown() { if (journal && journal.IsClosed) journal.Open(); }
#endif
    }
}
