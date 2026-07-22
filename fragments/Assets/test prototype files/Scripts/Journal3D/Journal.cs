using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Found.Journal3D
{
    /// <summary>
    /// The book controller. Owns the ordered leaves (front cover first, then pages),
    /// runs the open/close and page-turn animations one at a time, and reports which
    /// surface is currently "the page you can decorate" so the FOUND tools know where to
    /// drop scraps. Mirrors the web prototype's spread state machine.
    ///
    /// Build it with JournalBuilder (recommended) or assign leaves manually in order.
    /// </summary>
    public class Journal : MonoBehaviour
    {
        [Tooltip("Ordered leaves: index 0 = front cover, then pages front-to-back.")]
        public List<JournalPage> leaves = new();

        [Header("Animation")]
        public float turnDuration = 0.9f;
        public AnimationCurve turnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Concrete subclasses so these show up and serialize in the Inspector.
        [System.Serializable] public class SpreadEvent : UnityEvent<int> { }
        [System.Serializable] public class SurfaceEvent : UnityEvent<Transform> { }

        [Header("Events")]
        public SpreadEvent onSpreadChanged = new();              // number of turned leaves
        public SurfaceEvent onActiveSurfaceChanged = new();      // where new decorations should go

        int _turned;              // how many leaves are flipped to the left
        bool _busy;

        public int TurnedCount => _turned;
        public bool IsClosed => _turned == 0;
        public bool IsBusy => _busy;

        void Start() => RaiseActiveSurface();

        // ---- navigation --------------------------------------------------------

        public void Open()  { if (IsClosed) Next(); }
        public void Close() { while (_turned > 0 && !_busy) Prev(); }

        public void Next()
        {
            if (_busy || _turned >= leaves.Count) return;
            StartCoroutine(TurnRoutine(leaves[_turned], true));
        }

        public void Prev()
        {
            if (_busy || _turned <= 0) return;
            StartCoroutine(TurnRoutine(leaves[_turned - 1], false));
        }

        IEnumerator TurnRoutine(JournalPage leaf, bool forward)
        {
            _busy = true;
            // Lift the turning leaf above the stack so it sweeps cleanly over the others.
            int savedOrder = leaf.transform.GetSiblingIndex();
            leaf.transform.SetAsLastSibling();

            yield return leaf.Turn(forward, turnDuration, turnEase);

            _turned += forward ? 1 : -1;
            RestackDepths();
            _busy = false;
            onSpreadChanged?.Invoke(_turned);
            RaiseActiveSurface();
        }

        /// <summary>
        /// Nudge each leaf's depth so turned leaves settle on the left pile and unturned
        /// on the right pile, giving the book visible thickness without z-fighting.
        /// </summary>
        void RestackDepths()
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                bool isTurned = i < _turned;
                // stack height grows away from the centre spread on each side
                int depth = isTurned ? (_turned - i) : (i - _turned + 1);
                var p = leaves[i].transform.localPosition;
                p.y = depth * 0.0016f;
                leaves[i].transform.localPosition = p;
            }
        }

        // ---- decoration target -------------------------------------------------

        /// <summary>
        /// The surface the tools should parent new scraps to: the front of the topmost
        /// un-turned leaf (the right-hand page). When closed, that's the cover front, so
        /// you can decorate the cover too.
        /// </summary>
        public Transform ActiveSurface
        {
            get
            {
                if (leaves.Count == 0) return transform;
                int idx = Mathf.Clamp(_turned, 0, leaves.Count - 1);
                return leaves[idx].FrontSurface;
            }
        }

        /// <summary>The left-hand visible surface (inside of the last turned leaf), or null.</summary>
        public Transform LeftSurface =>
            _turned > 0 ? leaves[_turned - 1].BackSurface : null;

        void RaiseActiveSurface() => onActiveSurfaceChanged?.Invoke(ActiveSurface);

        // ---- materials (bulk helpers) -----------------------------------------

        public void SetShellMaterial(Material shell)
        {
            if (leaves.Count > 0) leaves[0].SetMaterials(shell, shell);
        }

        public void SetPageMaterial(Material page)
        {
            for (int i = 1; i < leaves.Count; i++) leaves[i].SetMaterials(page, page);
        }
    }
}
