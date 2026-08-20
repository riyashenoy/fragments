using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace Fragments.Book
{
    /// <summary>
    /// Builds and drives the whole book.
    ///
    /// Layering rule that stops cover/page intersection:
    ///   opened board  -> y around 0
    ///   turned pages  -> y >= settings.StackBase  (board half-thickness + clearance)
    /// The board is rigid and pivots LEFT of the block, so its arc is outside
    /// the page stack entirely.
    /// </summary>
    public class Book : MonoBehaviour
    {
        public BookSettings settings;

        [Header("Materials")]
        public Material coverMaterial;
        public Material pageEdgeMaterial;
        public Material metalMaterial;
        public Material spineMaterial;
        [Tooltip("Fallback when a JournalPage texture isn't supplied.")]
        public Material defaultPageMaterial;

        [Header("Events")]
        public UnityEvent<int> onSpreadChanged;

        [SerializeField] bool buildOnStart = true;

        public List<BookSheet> Sheets { get; private set; } = new();
        public List<JournalPage> Pages { get; private set; } = new();
        public int TurnedCount { get; private set; }
        public float CoverTop { get; private set; }

        Transform _bindingRoot, _spineFlexRoot;
        float _spineOpen, _spineOpenTarget;

        void Start()
        {
            if (buildOnStart && settings != null) Build();
        }

        // ==============================================================
        [ContextMenu("Rebuild Book")]
        public void Build()
        {
            for (int p = 0; p < Pages.Count; p++)
            {
                if (Pages[p]?.texture == null) continue;
                if (Application.isPlaying) Destroy(Pages[p].texture);
                else DestroyImmediate(Pages[p].texture);
            }
            Pages.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }
            Sheets.Clear();
            TurnedCount = 0;

            var s = settings;
            _bindingRoot = new GameObject("Binding").transform;
            _bindingRoot.SetParent(transform, false);

            float[] holes = s.binding == BindingType.Rings ? s.HoleZ() : null;

            // --- static binding geometry ---
            switch (s.binding)
            {
                case BindingType.Hardcover: BuildSpine(); BuildBackBoard(false); break;
                case BindingType.Rings: BuildRings(); BuildBackBoard(true); break;
                case BindingType.Staples: BuildSpine(); BuildBackBoard(false); BuildStaples(); break;
            }

            // --- sheets: index 0 is the cover, then pages ---
            SheetKind coverKind = s.binding == BindingType.Staples ? SheetKind.SoftCover : SheetKind.Board;
            Sheets.Add(CreateSheet(0, coverKind, holes));
            for (int i = 1; i <= s.sheetCount; i++)
                Sheets.Add(CreateSheet(i, SheetKind.Page, holes));

            CoverTop = StackY(0, 0);

            // Board cover pivots LEFT of the block so it can never land on the
            // right-hand page when opened.
            var cover = Sheets[0];
            if (cover.IsBoard)
            {
                var pivot = cover.transform.parent;
                pivot.localPosition = new Vector3(s.HingeX, CoverTop * 0.5f, 0f);
                cover.transform.localPosition = new Vector3(-s.HingeX, CoverTop * 0.5f, 0f);
            }

            for (int i = 0; i < Sheets.Count; i++)
            {
                Sheets[i].StackY = StackY(i, 0);
                Sheets[i].SetRest(false);
            }
        }

        BookSheet CreateSheet(int i, SheetKind kind, float[] holes)
        {
            var s = settings;
            bool isCover = kind != SheetKind.Page;
            float w = isCover ? s.CoverWidth : s.pageWidth;
            float h = isCover ? s.CoverHeight : s.pageHeight;
            float t = kind == SheetKind.Board ? s.coverThickness
                    : kind == SheetKind.SoftCover ? s.paperThickness * 1.8f
                    : s.paperThickness;

            var gen = SheetMeshGenerator.Generate(new SheetMeshGenerator.Params
            {
                width = w,
                height = h,
                thickness = t,
                spansX = isCover ? 44 : s.spansX,
                spansZ = isCover ? 20 : s.spansZ,
                cornerRadius = isCover ? s.cornerRadius : 0f,
                noise = isCover ? 0f : s.paperNoise,
                seed = i,
                holeZ = holes,
                holeX = s.holeInset,
                holeRadius = s.holeRadius
            });

            var pivot = new GameObject(kind == SheetKind.Page ? $"Sheet_{i}" : "Cover").transform;
            pivot.SetParent(transform, false);

            var go = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer), typeof(BookSheet));
            go.transform.SetParent(pivot, false);

            Material front = defaultPageMaterial, back = defaultPageMaterial;
            JournalPage frontPage = null, backPage = null;
            if (isCover)
            {
                front = coverMaterial;
                back = coverMaterial;
            }
            else
            {
                // Sheet 0 is the cover — paper sheets start at i=1, so page 1 is index 0.
                frontPage = new JournalPage((i - 1) * 2);
                backPage = new JournalPage((i - 1) * 2 + 1);
                Pages.Add(frontPage);
                Pages.Add(backPage);
                PageRenderer.Render(frontPage);
                PageRenderer.Render(backPage);

                front = new Material(defaultPageMaterial);
                front.mainTexture = frontPage.texture;
                back = new Material(defaultPageMaterial);
                back.mainTexture = backPage.texture;
            }

            var sheet = go.GetComponent<BookSheet>();
            sheet.Initialise(s, gen, kind, i, w, h,
                front, back, isCover ? coverMaterial : pageEdgeMaterial,
                pivot, go.transform);

            if (kind == SheetKind.Page)
            {
                sheet.FrontPage = frontPage;
                sheet.BackPage = backPage;
            }

            var mc = go.AddComponent<MeshCollider>();
            mc.convex = false;
            mc.cookingOptions = MeshColliderCookingOptions.None;
            // Dedicated copy — Unity collider cooking must never touch the deforming visual mesh
            // or pages can weld/sink through the cover mid-turn.
            mc.sharedMesh = Object.Instantiate(gen.mesh);

            // subtle per-sheet irregularity so the stack isn't cloned rectangles
            if (kind == SheetKind.Page && s.irregularity > 0f)
            {
                var r = new System.Random(i * 4271 + 5);
                float a1 = (float)r.NextDouble(), a2 = (float)r.NextDouble(), a3 = (float)r.NextDouble();
                go.transform.localPosition = new Vector3(
                    (a3 - 0.5f) * 0.0008f * s.irregularity, 0f,
                    (a2 - 0.5f) * 0.0012f * s.irregularity);
                go.transform.localRotation = Quaternion.Euler(0f, (a1 - 0.5f) * 0.4f * s.irregularity, 0f);
            }
            return sheet;
        }

        // ==============================================================
        public float StackY(int i, int turned)
        {
            float b = settings.StackBase;
            float gap = settings.Gap;
            if (i < turned) return b + (i + 1) * gap;          // left pile, earliest lowest
            return b + (Sheets.Count - i) * gap;                // right pile, next-to-turn highest
        }

        /// <summary>
        /// World-Y pages must stay above on the left of the spine.
        /// A hinged board lies near y=0; a staple soft cover sits in the page stack.
        /// </summary>
        float LeftFloorY()
        {
            float mrg = settings.coverPageClearance * 0.30f;
            if (TurnedCount <= 0 || Sheets.Count == 0) return -9f;
            var cover = Sheets[0];
            if (cover.IsBoard)
                return settings.coverThickness * 0.5f + mrg;
            float halfT = settings.paperThickness * 1.8f * 0.5f;
            return cover.StackY + halfT + mrg;
        }

        // ==============================================================
        void BuildBackBoard(bool punched)
        {
            var s = settings;
            var gen = SheetMeshGenerator.Generate(new SheetMeshGenerator.Params
            {
                width = s.CoverWidth,
                height = s.CoverHeight,
                thickness = s.coverThickness,
                spansX = 44,
                spansZ = 20,
                cornerRadius = s.cornerRadius,
                seed = 99,
                holeZ = punched ? s.HoleZ() : null,
                holeX = s.holeInset,
                holeRadius = s.holeRadius
            });
            var go = new GameObject("BackBoard", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_bindingRoot, false);
            go.transform.localPosition = new Vector3(0f, s.coverThickness * 0.5f, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = gen.mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials =
                new[] { coverMaterial, coverMaterial, coverMaterial };
        }

        /// <summary>
        /// Spine built from y=0 upward and clamped to x &lt;= 0 so it can never
        /// cover a page. Held in a flex root that squashes when the book opens,
        /// which is what stops turned pages passing through it.
        /// </summary>
        void BuildSpine()
        {
            var s = settings;
            float d = s.StackHeight * s.spineBulge;
            float top = s.StackBase + (s.sheetCount + 2) * s.Gap;
            float coverH = s.CoverHeight;

            var prof = new List<Vector2>();
            const int seg = 20;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * 0.5f + (i / (float)seg) * Mathf.PI;
                prof.Add(new Vector2(Mathf.Min(0f, Mathf.Cos(a) * d),
                                     Mathf.Sin(a) * top * 0.5f + top * 0.5f));
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int z = 0; z < 2; z++)
            {
                float zp = z == 0 ? -coverH * 0.5f : coverH * 0.5f;
                foreach (var p in prof) verts.Add(new Vector3(p.x, p.y, zp));
            }
            for (int e = 0; e < prof.Count - 1; e++)
            {
                int A = e, B = e + 1, C = prof.Count + e, D = prof.Count + e + 1;
                tris.AddRange(new[] { A, C, B, B, C, D });
            }
            for (int zz = 0; zz < 2; zz++)
            {
                float zc = zz == 0 ? -coverH * 0.5f : coverH * 0.5f;
                int ci = verts.Count;
                verts.Add(new Vector3(-d * 0.35f, top * 0.5f, zc));
                int rs = verts.Count;
                foreach (var p in prof) verts.Add(new Vector3(p.x, p.y, zc));
                for (int q = 0; q < prof.Count - 1; q++)
                {
                    if (zz == 0) tris.AddRange(new[] { ci, rs + q, rs + q + 1 });
                    else tris.AddRange(new[] { ci, rs + q + 1, rs + q });
                }
            }

            var mesh = new Mesh { name = "Spine" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _spineFlexRoot = new GameObject("SpineFlex").transform;
            _spineFlexRoot.SetParent(_bindingRoot, false);

            var go = new GameObject("SpineMesh", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_spineFlexRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                spineMaterial != null ? spineMaterial : coverMaterial;
        }

        void BuildRings()
        {
            var s = settings;
            float major = s.RingMajor();
            float cx = s.holeInset + s.holeRadius * 0.55f - major + s.ringShift;
            float[] zs = s.HoleZ();

            for (int i = 0; i < zs.Length; i++)
            {
                var go = new GameObject($"Ring_{i}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(_bindingRoot, false);
                // Torus default orientation (ring in XY, axis along Z) is correct.
                // DO NOT rotate it.
                go.GetComponent<MeshFilter>().sharedMesh = TorusMesh(major, s.wireRadius, 7, 40);
                go.GetComponent<MeshRenderer>().sharedMaterial = metalMaterial;
                go.transform.localPosition = new Vector3(cx, s.StackHeight * 0.5f, zs[i]);
            }
        }

        void BuildStaples()
        {
            var s = settings;
            int n = s.stapleCount;
            float wr = Mathf.Max(0.00028f, s.wireRadius * 0.8f * s.stapleSize);
            float cl = s.CoverHeight * 0.075f * s.stapleSize;
            float d = s.StackHeight * s.spineBulge;
            float sx = -d - wr * 0.9f;

            for (int i = 0; i < n; i++)
            {
                float z = n == 1 ? 0f : (-0.5f + i / (float)(n - 1)) * s.CoverHeight * s.stapleSpacing;
                var grp = new GameObject($"Staple_{i}").transform;
                grp.SetParent(_bindingRoot, false);
                grp.localPosition = new Vector3(0f, 0f, z);

                AddCyl(grp, wr, cl, new Vector3(sx, s.StackHeight * 0.5f, 0f), Quaternion.Euler(90, 0, 0));
                for (int sgn = -1; sgn <= 1; sgn += 2)
                    AddCyl(grp, wr, d * 0.6f,
                        new Vector3(sx + d * 0.3f, s.StackHeight * 0.5f, sgn * cl * 0.5f),
                        Quaternion.Euler(0, 0, 90));
            }
        }

        void AddCyl(Transform parent, float r, float len, Vector3 pos, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = new Vector3(r * 2f, len * 0.5f, r * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = metalMaterial;
        }

        static Mesh TorusMesh(float major, float minor, int minorSeg, int majorSeg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= majorSeg; i++)
            {
                float u = i / (float)majorSeg * Mathf.PI * 2f;
                Vector3 c = new Vector3(Mathf.Cos(u) * major, Mathf.Sin(u) * major, 0f);
                Vector3 nrm = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f);
                for (int j = 0; j <= minorSeg; j++)
                {
                    float w = j / (float)minorSeg * Mathf.PI * 2f;
                    v.Add(c + nrm * (Mathf.Cos(w) * minor) + Vector3.forward * (Mathf.Sin(w) * minor));
                }
            }
            int ring = minorSeg + 1;
            for (int i = 0; i < majorSeg; i++)
                for (int j = 0; j < minorSeg; j++)
                {
                    int a = i * ring + j, b = a + ring, c2 = a + 1, d = b + 1;
                    t.AddRange(new[] { a, b, c2, c2, b, d });
                }
            var m = new Mesh { name = "Ring" };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // ==============================================================
        public BookSheet NextSheet => TurnedCount < Sheets.Count ? Sheets[TurnedCount] : null;
        public BookSheet PrevSheet => TurnedCount > 0 ? Sheets[TurnedCount - 1] : null;
        public bool Busy
        {
            get
            {
                foreach (var s in Sheets)
                    if (s.Mode == SheetMode.Drag || s.Mode == SheetMode.Auto) return true;
                return false;
            }
        }

        public void TurnForward()
        {
            if (Busy || TurnedCount >= Sheets.Count) return;
            var s = Sheets[TurnedCount];
            s.StartAuto(true, StackY(TurnedCount, TurnedCount + 1));
        }

        public void TurnBackward()
        {
            if (Busy || TurnedCount <= 0) return;
            var s = Sheets[TurnedCount - 1];
            s.StartAuto(false, StackY(TurnedCount - 1, TurnedCount - 1));
        }

        void Update()
        {
            float dt = Mathf.Min(0.033f, Time.deltaTime);

            for (int i = 0; i < Sheets.Count; i++)
            {
                var s = Sheets[i];
                s.Tick(dt);

                if (s.ReadyToConverge())
                {
                    bool fwd = s.ForwardDirection;
                    s.BeginConverge();
                    TurnedCount = Mathf.Clamp(TurnedCount + (fwd ? 1 : -1), 0, Sheets.Count);
                    for (int j = 0; j < Sheets.Count; j++)
                        Sheets[j].StackTarget = StackY(j, TurnedCount);
                    onSpreadChanged?.Invoke(TurnedCount);
                }
                s.TrySleep();
            }

            bool boardOpen = TurnedCount > 0;
            float leftFloor = LeftFloorY();
            for (int i = 0; i < Sheets.Count; i++) Sheets[i].SetBoardOpen(boardOpen, leftFloor);

            // Spine relaxes flat while open so pages never pass through it.
            _spineOpenTarget = (TurnedCount > 0 && TurnedCount < Sheets.Count) ? 1f : 0f;
            _spineOpen += (_spineOpenTarget - _spineOpen) * Mathf.Min(1f, dt * 6f);
            if (_spineFlexRoot != null)
            {
                float sy = 1f - (1f - settings.spineFlatWhenOpen) * _spineOpen;
                _spineFlexRoot.localScale = new Vector3(1f, sy, 1f);
            }
        }

        public void SetOpenAmount(float t01)
        {
            if (Busy) return;
            RestoreSpread(Mathf.RoundToInt(Mathf.Clamp01(t01) * Sheets.Count));
        }

        /// <summary>
        /// Snap every sheet to a finished spread without playing the turn animation.
        /// Used after a rebuild so adding a page doesn't close the book.
        /// </summary>
        public void RestoreSpread(int turned)
        {
            if (Sheets.Count == 0) return;
            int target = Mathf.Clamp(turned, 0, Sheets.Count);
            for (int i = 0; i < Sheets.Count; i++) Sheets[i].SetRest(i < target);
            TurnedCount = target;
            bool boardOpen = target > 0;
            for (int i = 0; i < Sheets.Count; i++)
                Sheets[i].StackY = StackY(i, TurnedCount);
            float leftFloor = LeftFloorY();
            for (int i = 0; i < Sheets.Count; i++)
                Sheets[i].SetBoardOpen(boardOpen, leftFloor);

            _spineOpenTarget = (target > 0 && target < Sheets.Count) ? 1f : 0f;
            _spineOpen = _spineOpenTarget;
            if (_spineFlexRoot != null)
            {
                float sy = 1f - (1f - settings.spineFlatWhenOpen) * _spineOpen;
                _spineFlexRoot.localScale = new Vector3(1f, sy, 1f);
            }

            onSpreadChanged?.Invoke(TurnedCount);
        }
    }
}