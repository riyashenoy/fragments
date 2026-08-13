using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Found.Journal3D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class JournalPage : MonoBehaviour
    {
        [Header("State")]
        public bool turned;
        [Tooltip("Covers stay rigid (no paper curl during flip).")]
        public bool rigid;

        [Header("Curl (during animated flips)")]
        public float curlAmplitude = 0.018f;
        public float curlShapePower = 1.4f;
        public float edgeFlick = 0.6f;

        [Header("Physics")]
        public float hingeDamping = 0.8f;
        public float hingeSpring = 2f;
        public bool useGravity = true;

        public Transform FrontSurface { get; private set; }
        public Transform BackSurface { get; private set; }
        public bool IsAnimating { get; private set; }

        MeshFilter _mf;
        Mesh _mesh;
        Vector3[] _base;
        Vector3[] _work;
        float _width;
        float _thickness;
        float _angle;
        Rigidbody _rb;
        HingeJoint _hinge;

        static readonly Vector3 HingeAxis = Vector3.forward;

        public void Initialize(PageMeshBuilder.Result r, Material front, Material back, Material edge)
        {
            _mf = GetComponent<MeshFilter>();
            _mesh = r.mesh;
            _base = r.baseVertices;
            _work = (Vector3[])_base.Clone();
            _width = r.width;
            _thickness = r.thickness;
            _mf.sharedMesh = _mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { front, back, edge };

            FrontSurface = MakeSurface("FrontSurface", _thickness * 0.5f + 0.001f);
            BackSurface = MakeSurface("BackSurface", -_thickness * 0.5f - 0.001f);
            BackSurface.localRotation = Quaternion.Euler(0f, 180f, 0f);

            _angle = turned ? 180f : 0f;
            transform.localRotation = Quaternion.AngleAxis(_angle, HingeAxis);
            ApplyCurl(0f);
        }

        public void SetupPhysics(Rigidbody bookBody)
        {
            if (!useGravity) return;

            _rb = gameObject.GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.mass = rigid ? 0.3f : 0.05f;
            _rb.linearDamping = 0.5f;
            _rb.angularDamping = hingeDamping;
            _rb.useGravity = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _hinge = gameObject.GetComponent<HingeJoint>();
            if (_hinge == null) _hinge = gameObject.AddComponent<HingeJoint>();
            _hinge.connectedBody = bookBody;
            _hinge.axis = Vector3.forward;
            _hinge.anchor = Vector3.zero;

            _hinge.useLimits = true;
            var limits = new JointLimits();
            limits.min = 0f;
            limits.max = 180f;
            limits.bounciness = 0.05f;
            limits.contactDistance = 2f;
            _hinge.limits = limits;

            _hinge.useSpring = true;
            var spring = new JointSpring();
            spring.spring = hingeSpring;
            spring.damper = hingeDamping;
            spring.targetPosition = turned ? 180f : 0f;
            _hinge.spring = spring;

            _rb.isKinematic = false;
        }

        void UpdateSpringTarget(float targetAngle)
        {
            if (_hinge == null || !_hinge.useSpring) return;
            var spring = _hinge.spring;
            spring.targetPosition = targetAngle;
            _hinge.spring = spring;
        }

        Transform MakeSurface(string n, float yOffset)
        {
            var go = new GameObject(n);
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = new Vector3(_width * 0.5f, yOffset, 0f);
            return t;
        }

        public void SetMaterials(Material front, Material back, Material edge)
        {
            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { front, back, edge };
        }

        public IEnumerator Turn(bool forward, float duration, AnimationCurve ease)
        {
            IsAnimating = true;
            if (_rb != null) _rb.isKinematic = true;

            float from = GetCurrentAngle();
            float to = forward ? 180f : 0f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = ease != null
                    ? ease.Evaluate(Mathf.Clamp01(t / duration))
                    : Mathf.Clamp01(t / duration);
                _angle = Mathf.LerpAngle(from, to, k);
                transform.localRotation = Quaternion.AngleAxis(_angle, HingeAxis);
                ApplyCurl(CurlFactor(_angle));
                yield return null;
            }

            _angle = to;
            transform.localRotation = Quaternion.AngleAxis(_angle, HingeAxis);
            ApplyCurl(0f);
            turned = forward;

            UpdateSpringTarget(to);
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            IsAnimating = false;
        }

        public void SetProgress(float p01)
        {
            if (_rb != null && !_rb.isKinematic) _rb.isKinematic = true;
            _angle = Mathf.Clamp01(p01) * 180f;
            transform.localRotation = Quaternion.AngleAxis(_angle, HingeAxis);
            ApplyCurl(CurlFactor(_angle));
        }

        public void SnapTo(bool flipped)
        {
            turned = flipped;
            _angle = flipped ? 180f : 0f;
            transform.localRotation = Quaternion.AngleAxis(_angle, HingeAxis);
            ApplyCurl(0f);
            UpdateSpringTarget(_angle);
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        public float Progress => GetCurrentAngle() / 180f;

        float GetCurrentAngle()
        {
            float angle = transform.localRotation.eulerAngles.z;
            if (angle > 180f) angle -= 360f;
            return Mathf.Clamp(angle, 0f, 180f);
        }

        float CurlFactor(float angleDeg)
        {
            if (rigid) return 0f;
            return Mathf.Sin(angleDeg / 180f * Mathf.PI);
        }

        void ApplyCurl(float factor)
        {
            if (_base == null || _mesh == null) return;
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
                float bow = Mathf.Pow(u, curlShapePower);
                float tip = Mathf.Pow(u, 5f) * edgeFlick;
                float sCurve = Mathf.Sin(u * Mathf.PI) * 0.3f;
                _work[i] = new Vector3(p.x, p.y + curlAmplitude * factor * (bow + tip + sCurve), p.z);
            }
            _mesh.vertices = _work;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}