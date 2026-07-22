using System.Collections;
using UnityEngine;

namespace Found.Journal3D
{
    /// <summary>
    /// One leaf of the journal (front cover or a page). It pivots about the spine (local
    /// Z axis at X=0), animates its turn with easing, and bows the paper mid-turn for a
    /// believable page curl. Front and back each get their own material slot, and each
    /// face exposes a decoration surface transform that scraps/drawings parent to — so
    /// decorations turn with the page and stay attached, exactly like the web prototype.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class JournalPage : MonoBehaviour
    {
        [Header("State")]
        public bool turned;              // false = resting on the right, true = flipped to the left
        [Tooltip("Covers stay rigid (no paper bow). Pages curl during the turn.")]
        public bool rigid;

        [Header("Curl")]
        public float curlAmplitude = 0.012f;   // metres of bow at mid-turn
        public float curlShapePower = 1.6f;    // higher = curl concentrated near the free edge

        public Transform FrontSurface { get; private set; }
        public Transform BackSurface { get; private set; }
        public bool IsAnimating { get; private set; }

        MeshFilter _mf;
        Mesh _mesh;
        Vector3[] _base;
        Vector3[] _work;
        float _width;
        float _angle;                    // current turn angle in degrees (0..180)

        static readonly Vector3 Hinge = Vector3.forward;   // spine runs along local Z

        public void Initialize(PageMeshBuilder.Result r, Material front, Material back)
        {
            _mf = GetComponent<MeshFilter>();
            _mesh = r.mesh;
            _base = r.baseVertices;
            _work = (Vector3[])_base.Clone();
            _width = r.width;
            _mf.sharedMesh = _mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { front, back };

            // Decoration anchors: front sits just above the sheet, back just below,
            // both centred on the page, so parented items rest on the paper.
            FrontSurface = MakeSurface("FrontSurface", +0.0015f);
            BackSurface  = MakeSurface("BackSurface", -0.0015f);
            BackSurface.localRotation = Quaternion.Euler(0f, 180f, 0f); // face outward when flipped

            _angle = turned ? 180f : 0f;
            transform.localRotation = Quaternion.AngleAxis(_angle, Hinge);
            ApplyCurl(0f);
        }

        Transform MakeSurface(string n, float yOffset)
        {
            var go = new GameObject(n);
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = new Vector3(_width * 0.5f, yOffset, 0f);
            return t;
        }

        public void SetMaterials(Material front, Material back)
        {
            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { front, back };
        }

        /// <summary>Animate a turn. forward=true flips right→left, false flips back.</summary>
        public IEnumerator Turn(bool forward, float duration, AnimationCurve ease)
        {
            IsAnimating = true;
            float from = _angle;
            float to = forward ? 180f : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = ease != null ? ease.Evaluate(Mathf.Clamp01(t / duration)) : Mathf.Clamp01(t / duration);
                _angle = Mathf.LerpAngle(from, to, k);
                transform.localRotation = Quaternion.AngleAxis(_angle, Hinge);
                ApplyCurl(CurlFactor(_angle));
                yield return null;
            }
            _angle = to;
            transform.localRotation = Quaternion.AngleAxis(_angle, Hinge);
            ApplyCurl(0f);
            turned = forward;
            IsAnimating = false;
        }

        /// <summary>Drive the turn directly from a 0..1 drag (for grab-to-flip).</summary>
        public void SetProgress(float p01)
        {
            _angle = Mathf.Clamp01(p01) * 180f;
            transform.localRotation = Quaternion.AngleAxis(_angle, Hinge);
            ApplyCurl(CurlFactor(_angle));
        }

        public void SnapTo(bool flipped)
        {
            turned = flipped;
            _angle = flipped ? 180f : 0f;
            transform.localRotation = Quaternion.AngleAxis(_angle, Hinge);
            ApplyCurl(0f);
        }

        public float Progress => _angle / 180f;

        float CurlFactor(float angleDeg)
        {
            if (rigid) return 0f;
            return Mathf.Sin(angleDeg / 180f * Mathf.PI); // 0 at flat, peak at 90°
        }

        void ApplyCurl(float factor)
        {
            if (rigid || factor <= 0.0001f)
            {
                _mesh.vertices = _base;
                _mesh.RecalculateNormals();
                _mesh.RecalculateBounds();
                return;
            }
            for (int i = 0; i < _base.Length; i++)
            {
                var p = _base[i];
                float u = Mathf.Clamp01(p.x / _width);
                float bow = Mathf.Pow(u, curlShapePower);         // ramps toward free edge
                float tip = Mathf.Pow(u, 4f) * 0.4f;              // slight extra flick at the very edge
                _work[i] = new Vector3(p.x, p.y + curlAmplitude * factor * (bow + tip), p.z);
            }
            _mesh.vertices = _work;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
