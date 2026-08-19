using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Fragments.Book
{
    public enum SheetKind { Page, Board, SoftCover }
    public enum SheetMode { Idle, Drag, Auto, Converge }

    /// <summary>
    /// One sheet of the book.
    ///
    /// Pages and soft covers DEFORM: they carry a grabbed material point A, a
    /// target G, and a velocity, all driven by a critically-damped spring. Only
    /// the spring TARGET ever changes, so velocity is continuous and releasing a
    /// page never snaps.
    ///
    /// Boards DO NOT deform. They are rigid and rotate about a real pivot placed
    /// left of the block, so the opened board sweeps outside the page stack and
    /// cannot intersect it.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BookSheet : MonoBehaviour
    {
        [HideInInspector] public SheetKind kind = SheetKind.Page;
        [HideInInspector] public int index;
        [HideInInspector] public float width, height;
        [HideInInspector] public bool turned;
        [HideInInspector] public int frontPageIndex, backPageIndex;
        [HideInInspector] public int topTriangleCount;

        public SheetMode Mode { get; private set; } = SheetMode.Idle;
        public bool IsBoard => kind == SheetKind.Board;
        public bool IsCover => kind != SheetKind.Page;
        public bool Asleep { get; private set; } = true;
        public JournalPage FrontPage { get; set; }
        public JournalPage BackPage { get; set; }

        Mesh _mesh;
        Vector3[] _rest, _work, _conv0;
        Transform _pivot, _visual;

        // paper state
        Vector2 _A, _G, _Gv, _Gt;
        float _R, _Rt;
        // board state
        float _ang, _angv, _angT;
        // stack height
        float _y, _yv, _yT;

        float _convT;
        int _dir = 1;
        BookSettings _s;
        bool _collisionEnabled = true;
        bool _leftBoardOpen;
        float _leftFloorY = -9f;

        /// <summary>
        /// Book tells each sheet whether the front cover is lying open, and at
        /// what world-Y pages may not sink below on the left of the spine.
        /// Hardcover/rings: the rigid board at y≈0. Staples: the soft cover's stack height.
        /// </summary>
        public void SetBoardOpen(bool open, float leftFloorY = -9f)
        {
            _leftBoardOpen = open;
            _leftFloorY = open ? leftFloorY : -9f;
        }
        public void SetCollision(bool on) => _collisionEnabled = on;

        public float Angle => _ang;
        public Vector2 GrabTarget => _G;
        public float StackY { get => _y; set { _y = value; _yT = value; _yv = 0f; } }
        public float StackTarget { get => _yT; set => _yT = value; }

        // ------------------------------------------------------------------
        public void Initialise(BookSettings s, SheetMeshGenerator.Result gen,
                               SheetKind k, int idx, float w, float h,
                               Material front, Material back, Material edge,
                               Transform pivot, Transform visual)
        {
            _s = s; kind = k; index = idx; width = w; height = h;
            _pivot = pivot; _visual = visual;

            _mesh = gen.mesh;
            _rest = gen.restVertices;
            _work = (Vector3[])_rest.Clone();
            _conv0 = (Vector3[])_rest.Clone();
            topTriangleCount = gen.topTriangleCount;

            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterials = new[] { front, back, edge };

            frontPageIndex = idx * 2;
            backPageIndex = idx * 2 + 1;

            SetRest(false);
        }

        public void SetMaterials(Material front, Material back, Material edge)
            => GetComponent<MeshRenderer>().sharedMaterials = new[] { front, back, edge };

        // ------------------------------------------------------------------
        public void SetRest(bool isTurned)
        {
            turned = isTurned;
            Mode = SheetMode.Idle;
            Asleep = true;

            if (IsBoard)
            {
                _ang = isTurned ? Mathf.PI : 0f;
                _angT = _ang; _angv = 0f;
            }
            else
            {
                _A = new Vector2(width, 0f);
                Vector2 far = PaperDeformer.FullyTurnedTarget(_A, _s.BindX, height);
                _G = isTurned ? far : _A;
                _Gt = _G; _Gv = Vector2.zero;
                _R = _s.restCurlRadius; _Rt = _R;
            }
            _yv = 0f;
            Apply();
        }

        public void BeginDrag(Vector2 materialPoint, Vector3 worldHit)
        {
            Mode = SheetMode.Drag; Asleep = false;
            if (IsBoard) return;
            _A = materialPoint;
            _G = new Vector2(worldHit.x, worldHit.z);   // no jump on grab
            _Gt = _G; _Gv = Vector2.zero;
        }

        public void DragTo(Vector3 worldPoint)
        {
            if (IsBoard)
            {
                float span = Mathf.Max(0.02f, width);
                float frac = Mathf.Clamp01((width - worldPoint.x) / (2f * span));
                _angT = Mathf.PI * frac;
                return;
            }
            _Gt = PaperDeformer.Clamp(_A, new Vector2(worldPoint.x, worldPoint.z), _s.BindX, height);
        }

        public bool PastMidpoint =>
            IsBoard ? (_ang > Mathf.PI * 0.5f) : (_G.x < _s.BindX);

        public void StartAuto(bool forward, float stackTarget)
        {
            Mode = SheetMode.Auto; Asleep = false;
            _dir = forward ? 1 : -1;
            if (IsBoard) _angT = forward ? Mathf.PI : 0f;
            else
            {
                _A = new Vector2(width, 0f);
                _Gt = forward
                    ? PaperDeformer.FullyTurnedTarget(_A, _s.BindX, height)
                    : _A;
            }
            _yT = stackTarget;
        }

        public void SettleTo(bool isTurned, float stackTarget)
        {
            Mode = SheetMode.Idle; Asleep = false;
            turned = isTurned;
            if (IsBoard) _angT = isTurned ? Mathf.PI : 0f;
            else
            {
                _A = new Vector2(width, 0f);
                _Gt = isTurned
                    ? PaperDeformer.FullyTurnedTarget(_A, _s.BindX, height)
                    : _A;
            }
            _yT = stackTarget;
        }

        /// <summary>Blend into the canonical rest state with no visible pop.</summary>
        public void BeginConverge()
        {
            if (!IsBoard) System.Array.Copy(_work, _conv0, _work.Length);
            bool fwd = _dir > 0;

            if (IsBoard) { _ang = fwd ? Mathf.PI : 0f; _angT = _ang; _angv = 0f; }
            else
            {
                _A = new Vector2(width, 0f);
                Vector2 far = PaperDeformer.FullyTurnedTarget(_A, _s.BindX, height);
                _G = fwd ? far : _A;
                _Gt = _G; _Gv = Vector2.zero;
                _R = _s.restCurlRadius; _Rt = _R;
            }
            turned = fwd;
            Mode = SheetMode.Converge;
            _convT = 0f;
        }

        public bool ForwardDirection => _dir > 0;

        // ------------------------------------------------------------------
        public void Tick(float dt)
        {
            if (Asleep && Mode == SheetMode.Idle) return;

            if (Mode == SheetMode.Converge)
            {
                _convT = Mathf.Min(1f, _convT + dt / Mathf.Max(0.04f, _s.convergeTime));
                SpringY(dt);
                Apply();
                if (_convT >= 1f && Mathf.Abs(_yT - _y) < 3e-5f)
                {
                    Mode = SheetMode.Idle; Asleep = true; Apply();
                }
                return;
            }

            float spd = IsCover ? _s.coverSpeed : _s.paperSpeed;
            float k = (Mode == SheetMode.Drag ? spd * 2.6f : spd);
            float c = 2f * Mathf.Sqrt(k) * _s.damping;

            if (IsBoard)
            {
                _angv += ((_angT - _ang) * k - _angv * c) * dt;
                _ang += _angv * dt;
                if (_ang < 0f) { _ang = 0f; _angv *= 0.3f; }
                if (_ang > Mathf.PI) { _ang = Mathf.PI; _angv *= 0.3f; }
            }
            else
            {
                _Gv += ((_Gt - _G) * k - _Gv * c) * dt;
                _G += _Gv * dt;

                Vector2 cl = PaperDeformer.Clamp(_A, _G, _s.BindX, height);
                if ((cl - _G).sqrMagnitude > 1e-12f) { _G = cl; _Gv *= 0.35f; }

                float dist = (_Gt - _G).magnitude;
                float near = 1f - Mathf.Min(1f, dist / (width * 1.2f));
                _Rt = Mathf.Lerp(_s.turnCurlRadius, _s.restCurlRadius, near * near);
                _R += (_Rt - _R) * Mathf.Min(1f, dt * 10f);
            }

            SpringY(dt);
            Apply();
        }

        void SpringY(float dt)
        {
            float ky = IsBoard ? 70f : 110f;
            float cy = 2f * Mathf.Sqrt(ky) * 1.05f;
            _yv += ((_yT - _y) * ky - _yv * cy) * dt;
            _y += _yv * dt;
        }

        public bool ReadyToConverge()
        {
            if (Mode != SheetMode.Auto) return false;
            float pe, ve;
            if (IsBoard) { pe = Mathf.Abs(_angT - _ang); ve = Mathf.Abs(_angv); }
            else { pe = (_Gt - _G).magnitude / Mathf.Max(0.01f, width); ve = _Gv.magnitude; }
            return pe < 0.03f && ve < 0.05f;
        }

        public void TrySleep()
        {
            if (Mode != SheetMode.Idle) return;
            float pe = IsBoard ? Mathf.Abs(_angT - _ang) : (_Gt - _G).magnitude;
            float ve = IsBoard ? Mathf.Abs(_angv) : _Gv.magnitude;
            if (pe < 0.0012f && ve < 0.012f) Asleep = true;
        }

        // ------------------------------------------------------------------
        void Apply()
        {
            if (IsBoard)
            {
                // rigid: pure rotation about the hinge, never deformed
                if (_pivot) _pivot.localRotation = Quaternion.Euler(0f, 0f, _ang * Mathf.Rad2Deg);
                return;
            }

            float radius = IsCover ? _R * (1f + _s.coverStiffness * 2.6f) : _R;

            // Sag exists only mid-flight. At either resting end it fades to
            // zero so a settled page lies dead flat with a STRAIGHT edge --
            // a scalloped edge is what lets the cover show through.
            Vector2 far = PaperDeformer.FullyTurnedTarget(new Vector2(width, 0f), _s.BindX, height);
            float dRest = Mathf.Min(Mathf.Abs(_G.x - width), Mathf.Abs(_G.x - far.x));
            float sagK = Mathf.Min(1f, dRest / Mathf.Max(1e-4f, width * 0.30f));
            float sag = (IsCover ? _s.sag * 0.35f : _s.sag) * sagK * sagK;

            FoldState f = PaperDeformer.PaperFold(_A, _G, _s.BindX, radius, sag, height);
            if (!f.valid) System.Array.Copy(_rest, _work, _rest.Length);
            else PaperDeformer.Apply(_rest, _work, f);

            if (Mode == SheetMode.Converge)
            {
                float e = _convT * _convT * (3f - 2f * _convT);
                for (int i = 0; i < _work.Length; i++)
                    _work[i] = Vector3.Lerp(_conv0[i], _work[i], e);
            }

            // ---- HARD COLLISION FLOOR -------------------------------------
            // Nothing may sink below the boards. Left of the spine the floor is
            // the opened front board; right of it, the back board. A real
            // constraint, not a spacing coincidence.
            if (!IsCover && _collisionEnabled)
            {
                float mrg = _s.coverPageClearance * 0.30f;
                float rightTop = _s.coverThickness * 0.5f + mrg;
                float leftTop = _leftBoardOpen ? _leftFloorY : -9f;
                for (int i = 0; i < _work.Length; i++)
                {
                    float worldY = _work[i].y + _y;
                    float lim = (_work[i].x < 0f) ? leftTop : rightTop;
                    if (worldY < lim) _work[i].y = lim - _y;
                }
            }

            _mesh.SetVertices(_work);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (_pivot) _pivot.localPosition = new Vector3(_pivot.localPosition.x, _y, _pivot.localPosition.z);
        }
    }
}