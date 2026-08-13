using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace Found.Journal3D
{
    /// <summary>
    /// The book controller. Owns the ordered leaves (front cover first, then pages),
    /// runs open/close and page-turn animations one at a time, and reports which
    /// surface is currently the active decoration target.
    /// </summary>
    public class Journal : MonoBehaviour
    {
        [Tooltip("Ordered leaves: index 0 = front cover, then pages front-to-back.")]
        public List<JournalPage> leaves = new();

        [Header("Animation")]
        public float turnDuration = 0.9f;
        public AnimationCurve turnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [System.Serializable] public class SpreadEvent : UnityEvent<int> { }
        [System.Serializable] public class SurfaceEvent : UnityEvent<Transform> { }

        [Header("Events")]
        public SpreadEvent onSpreadChanged = new();
        public SurfaceEvent onActiveSurfaceChanged = new();

        int _turned;
        bool _busy;

        public int TurnedCount => _turned;
        public bool IsClosed => _turned == 0;
        public bool IsBusy => _busy;

        void Start() => RaiseActiveSurface();

        public void Open() { if (IsClosed) Next(); }

        public void Close()
        {
            if (_busy) return;
            StartCoroutine(CloseAll());
        }

        IEnumerator CloseAll()
        {
            while (_turned > 0 && !_busy)
                yield return TurnRoutine(leaves[_turned - 1], false);
        }

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
            leaf.transform.SetAsLastSibling();

            yield return leaf.Turn(forward, turnDuration, turnEase);

            _turned += forward ? 1 : -1;
            RestackDepths();
            _busy = false;
            onSpreadChanged.Invoke(_turned);
            RaiseActiveSurface();
        }

        void RestackDepths()
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                bool isTurned = i < _turned;
                int depth = isTurned ? (_turned - i) : (i - _turned + 1);
                var p = leaves[i].transform.localPosition;
                p.y = depth * 0.0024f;
                leaves[i].transform.localPosition = p;
            }
        }

        public Transform ActiveSurface
        {
            get
            {
                if (leaves.Count == 0) return transform;
                int idx = Mathf.Clamp(_turned, 0, leaves.Count - 1);
                return leaves[idx].FrontSurface;
            }
        }

        public Transform LeftSurface =>
            _turned > 0 ? leaves[_turned - 1].BackSurface : null;

        void RaiseActiveSurface() => onActiveSurfaceChanged.Invoke(ActiveSurface);

        /// <summary>Swap cover material at runtime (cover color from JournalData).</summary>
        public void SetShellMaterial(Material shell)
        {
            if (leaves.Count > 0)
            {
                var mr = leaves[0].GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterials = new[] { shell, shell, shell };
            }

            // Also update spine and back cover
            var spine = transform.Find("Spine");
            if (spine != null)
            {
                var sr = spine.GetComponent<MeshRenderer>();
                if (sr != null) sr.sharedMaterial = shell;
            }

            var back = transform.Find("BackCover");
            if (back != null)
            {
                var br = back.GetComponent<MeshRenderer>();
                if (br != null) br.sharedMaterial = shell;
            }
        }

        /// <summary>Swap page material at runtime (page pattern from JournalData).</summary>
        public void SetPageMaterial(Material page)
        {
            for (int i = 1; i < leaves.Count; i++)
            {
                var mr = leaves[i].GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterials.Length >= 3)
                {
                    var mats = mr.sharedMaterials;
                    mats[0] = page; // top face
                    mats[1] = page; // bottom face
                    // mats[2] stays as edge
                    mr.sharedMaterials = mats;
                }
            }
        }
    }
}