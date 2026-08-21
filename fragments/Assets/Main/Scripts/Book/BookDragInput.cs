using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Fragments.Book
{
    /// <summary>
    /// Mouse / XR input that mirrors the v10 prototype's beginGrab / moveGrab / endGrab.
    /// Pointer math is done in book-local space (the HTML book's world space).
    /// Also handles stamp (click), freehand draw, and text placement.
    /// </summary>
    public class BookDragInput : MonoBehaviour
    {
        [SerializeField] Book book;
        [SerializeField] Camera mainCamera;

        [Header("Stamp Tool")]
        public string activeStampType = "sticker";
        public string activeStampColorHex = "#D9584A";

        [Header("Draw Tool")]
        public bool drawModeActive = false;
        public float baseThickness = 2f;

        [Header("Text Tool")]
        public bool textModeActive = false;
        public TMP_InputField textInputField;
        public float textFontSize = 20f;

        BookSheet _held;
        int _heldDir = 1;
        Vector2 pressScreenPos;
        bool didMove;
        const float MOVE_THRESHOLD = 4f;  // pixels
        const float STROKE_MIN_PIXELS = 2f;

        PageElement activeStroke;
        JournalPage activeStrokePage;
        Vector2 lastStrokeScreenPos;

        JournalPage _pendingTextPage;
        float _pendingTextU, _pendingTextV;
        bool _awaitingText;

        void OnEnable()
        {
            if (book == null) book = GetComponent<Book>();
            if (mainCamera == null) mainCamera = Camera.main;

            if (textInputField != null)
            {
                textInputField.gameObject.SetActive(false);
                textInputField.onEndEdit.RemoveListener(OnTextSubmitted);
                textInputField.onEndEdit.AddListener(OnTextSubmitted);
            }
        }

        void OnDisable()
        {
            if (textInputField != null)
                textInputField.onEndEdit.RemoveListener(OnTextSubmitted);
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (drawModeActive)
                {
                    HandleDrawInput();
                    return;  // draw mode takes over — no page dragging
                }

                if (textModeActive)
                {
                    HandleTextInput();
                    return;
                }

                Vector2 pos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    pressScreenPos = pos;
                    didMove = false;
                    BeginGrab(pos);
                }
                if (mouse.leftButton.isPressed)
                {
                    if (!didMove && Vector2.Distance(pos, pressScreenPos) > MOVE_THRESHOLD)
                        didMove = true;
                    if (_held != null) MoveGrab(pos);
                }
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    if (_held != null) EndGrab();
                    else if (!didMove) TryStamp(pressScreenPos);
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rightArrowKey.wasPressedThisFrame) book.TurnForward();
                if (kb.leftArrowKey.wasPressedThisFrame) book.TurnBackward();
            }
        }

        // ------------------------------------------------------------------
        // text
        void HandleTextInput()
        {
            if (_awaitingText) return; // wait for InputField submit

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector2 pos = mouse.position.ReadValue();
            if (!TryGetPageHit(pos, out _, out JournalPage page, out float u, out float v, out _))
                return;

            _pendingTextPage = page;
            _pendingTextU = u;
            _pendingTextV = v;
            _awaitingText = true;

            if (textInputField == null)
            {
                // No UI field assigned — place a placeholder so the tool still works.
                CommitText("text");
                return;
            }

            textInputField.gameObject.SetActive(true);
            textInputField.text = "";
            textInputField.Select();
            textInputField.ActivateInputField();
        }

        void OnTextSubmitted(string value)
        {
            if (!_awaitingText) return;
            CommitText(value);
        }

        void CommitText(string value)
        {
            _awaitingText = false;
            if (textInputField != null)
                textInputField.gameObject.SetActive(false);

            if (_pendingTextPage == null) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                _pendingTextPage = null;
                return;
            }

            var el = new PageElement
            {
                type = "text",
                text = value.Trim(),
                fontSize = textFontSize,
                u = _pendingTextU,
                v = _pendingTextV,
                scale = 1f,
                colorHex = activeStampColorHex,
                layer = _pendingTextPage.elements.Count
            };
            _pendingTextPage.Add(el);
            PageRenderer.Render(_pendingTextPage);
            _pendingTextPage = null;
        }

        // ------------------------------------------------------------------
        // draw
        void HandleDrawInput()
        {
            var mouse = Mouse.current;
            if (mouse == null || book == null || mainCamera == null) return;

            Vector2 pos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!TryGetPageHit(pos, out _, out JournalPage page, out float u, out float v, out _))
                    return;

                activeStroke = new PageElement
                {
                    type = "stroke",
                    colorHex = activeStampColorHex,
                    thickness = baseThickness,
                    scale = 1f,
                    layer = page.elements.Count,
                    u = u,
                    v = v
                };
                activeStroke.points.Add(new StrokePoint { u = u, v = v, pressure = 1f });
                page.Add(activeStroke);
                activeStrokePage = page;
                lastStrokeScreenPos = pos;
                PageRenderer.Render(page);
            }

            if (mouse.leftButton.isPressed && activeStroke != null && activeStrokePage != null)
            {
                if (!TryGetPageHit(pos, out _, out JournalPage page, out float u, out float v, out _))
                    return;
                if (page != activeStrokePage) return;

                float moved = Vector2.Distance(pos, lastStrokeScreenPos);
                if (moved < STROKE_MIN_PIXELS) return;

                float pressure = Mathf.Clamp01(1f - (moved / 40f));
                activeStroke.points.Add(new StrokePoint { u = u, v = v, pressure = pressure });
                lastStrokeScreenPos = pos;
                PageRenderer.Render(activeStrokePage);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                activeStroke = null;
                activeStrokePage = null;
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
            if (book == null || drawModeActive) return;
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
        // stamp: click without drag — only the currently visible spread faces
        void TryStamp(Vector2 screenPos)
        {
            if (book == null || book.Busy) return;
            if (!TryGetPageHit(screenPos, out _, out JournalPage page, out float u, out float v, out _))
                return;

            var el = new PageElement
            {
                type = activeStampType,
                u = u, v = v,
                rotation = Random.Range(-0.25f, 0.25f),
                scale = Random.Range(0.85f, 1.20f),
                colorHex = activeStampColorHex,
                layer = page.elements.Count
            };
            page.Add(el);
            PageRenderer.Render(page);
        }

        bool TryGetPageHit(Vector2 screenPos, out BookSheet sheet, out JournalPage page,
                           out float u, out float v, out bool isTop)
        {
            sheet = null;
            page = null;
            u = v = 0f;
            isTop = false;

            if (book == null || mainCamera == null) return false;

            var next = book.NextSheet;
            var prev = book.PrevSheet;
            Cook(next);
            Cook(prev);

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            RaycastHit hit = default;
            float best = float.MaxValue;
            TryCandidate(next, ray, ref sheet, ref hit, ref best);
            TryCandidate(prev, ray, ref sheet, ref hit, ref best);
            if (sheet == null || sheet.IsBoard || sheet.IsCover) return false;

            // Right pile shows the front; left (turned) pile shows the back.
            bool onRight = sheet == next;
            isTop = (hit.triangleIndex * 3) < sheet.topTriangleCount;
            // Reject hits on the underside of a thin sheet.
            if (onRight != isTop) return false;

            page = onRight ? sheet.FrontPage : sheet.BackPage;
            if (page == null) return false;

            Vector2 uv = hit.textureCoord;
            u = isTop ? uv.x : 1f - uv.x;
            v = uv.y;
            if (u < 0.03f) return false;
            return true;
        }

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
            var src = s.GetComponent<MeshFilter>()?.sharedMesh;
            if (mc == null || src == null) return;

            var copy = new Mesh { name = src.name + "_col", indexFormat = src.indexFormat };
            copy.vertices = src.vertices;
            copy.triangles = src.triangles;
            copy.uv = src.uv;
            copy.RecalculateBounds();

            mc.cookingOptions = MeshColliderCookingOptions.None;
            mc.sharedMesh = null;
            mc.sharedMesh = copy;
        }
    }
}
