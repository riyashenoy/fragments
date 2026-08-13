using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Found.Journal3D
{
    /// <summary>
    /// Assembles a journal with thick, physics-enabled pages.
    ///
    /// PREFAB WORKFLOW:
    ///   1. Put on an empty GameObject.
    ///   2. Assign materials. Tweak dimensions.
    ///   3. Right-click component header → "Build Journal In Editor".
    ///   4. Visually adjust anything you want.
    ///   5. Drag from Hierarchy into Prefabs folder to save.
    ///   6. At runtime, JournalingSceneManager instantiates the prefab.
    ///
    /// Physics:
    ///   The book root gets a kinematic Rigidbody (the anchor).
    ///   Each page gets a non-kinematic Rigidbody + HingeJoint so pages
    ///   drape naturally under gravity. During scripted flips, pages go
    ///   kinematic temporarily, then release back to physics.
    /// </summary>
    [ExecuteInEditMode]
    public class JournalBuilder : MonoBehaviour
    {
        [Header("Materials")]
        public Material shellMaterial;
        public Material pageMaterial;
        [Tooltip("Paper edge color. Leave null for auto cream.")]
        public Material edgeMaterial;

        [Header("Dimensions (metres)")]
        public float pageWidth = 0.16f;
        public float pageHeight = 0.22f;
        [Min(1)] public int pageCount = 6;
        public float coverMargin = 0.006f;
        public float coverThickness = 0.005f;
        public float pageThickness = 0.0018f;
        public float spineWidth = 0.012f;

        [Header("Mesh Detail")]
        [Range(8, 64)] public int subdivisionsX = 32;
        [Range(2, 24)] public int subdivisionsZ = 10;
        [Range(0f, 0.005f)] public float paperNoise = 0.002f;

        [Header("Physics")]
        public bool enablePageGravity = true;
        [Range(0.1f, 5f)] public float pageHingeSpring = 2f;
        [Range(0.1f, 3f)] public float pageHingeDamping = 0.8f;

        [Header("Build")]
        public bool buildOnStart = false;

        void Start()
        {
            if (UnityEngine.Application.isPlaying && buildOnStart && GetComponent<Journal>() == null)
                Build();
        }

        [ContextMenu("Build Journal In Editor")]
        public void Build()
        {
            // Clear previous build
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }

            // Remove old components if rebuilding
            var oldJournal = GetComponent<Journal>();
            if (oldJournal != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(oldJournal);
                else DestroyImmediate(oldJournal);
            }

            var journal = gameObject.AddComponent<Journal>();
            journal.leaves = new List<JournalPage>();

            Material shell = shellMaterial != null ? shellMaterial : MakeDefault(new Color(0.42f, 0.25f, 0.16f), "FOUND_Shell");
            Material page = pageMaterial != null ? pageMaterial : MakeDefault(new Color(0.957f, 0.929f, 0.863f), "FOUND_Page");
            Material edge = edgeMaterial != null ? edgeMaterial : MakeDefault(new Color(0.94f, 0.91f, 0.85f), "FOUND_Edge");

            float coverW = pageWidth + coverMargin;
            float coverH = pageHeight + coverMargin * 2f;
            float stackY = coverThickness * 2f + pageCount * (pageThickness + 0.0008f);

            // ---- Book root Rigidbody (kinematic anchor for hinge joints) ----
            Rigidbody bookBody = GetComponent<Rigidbody>();
            if (bookBody == null) bookBody = gameObject.AddComponent<Rigidbody>();
            bookBody.isKinematic = true;
            bookBody.useGravity = false;

            // ---- Spine ----
            var spine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spine.name = "Spine";
            spine.transform.SetParent(transform, false);
            spine.transform.localScale = new Vector3(spineWidth, stackY, coverH);
            spine.transform.localPosition = new Vector3(-spineWidth * 0.5f, stackY * 0.5f, 0f);
            PaintSingle(spine, shell);

            // ---- Back cover ----
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "BackCover";
            back.transform.SetParent(transform, false);
            back.transform.localScale = new Vector3(coverW, coverThickness, coverH);
            back.transform.localPosition = new Vector3(coverW * 0.5f, coverThickness * 0.5f, 0f);
            PaintSingle(back, shell);

            // ---- Pages ----
            var pages = new List<JournalPage>();
            for (int i = 0; i < pageCount; i++)
            {
                var pg = MakeLeaf(
                    "Page_" + (i + 1),
                    pageWidth, pageHeight, pageThickness,
                    page, page, edge,
                    rigid: false, seed: i + 1);
                pg.transform.localPosition = new Vector3(
                    0f,
                    coverThickness + (i + 1) * (pageThickness + 0.0008f),
                    0f);
                pg.hingeSpring = pageHingeSpring;
                pg.hingeDamping = pageHingeDamping;
                pg.useGravity = enablePageGravity;
                pages.Add(pg);
            }

            // ---- Front cover ----
            var front = MakeLeaf(
                "FrontCover",
                coverW, coverH, coverThickness,
                shell, shell, shell,
                rigid: true, seed: 0);
            front.curlAmplitude = 0f;
            front.transform.localPosition = new Vector3(
                0f,
                coverThickness + (pageCount + 1) * (pageThickness + 0.0008f),
                0f);
            front.hingeSpring = pageHingeSpring * 1.5f; // cover is heavier
            front.hingeDamping = pageHingeDamping * 1.2f;
            front.useGravity = enablePageGravity;

            journal.leaves.Add(front);
            journal.leaves.AddRange(pages);

            // Wire physics (only in play mode — editor preview stays static)
            if (UnityEngine.Application.isPlaying && enablePageGravity)
            {
                foreach (var leaf in journal.leaves)
                    leaf.SetupPhysics(bookBody);
            }

            Debug.Log("[FOUND] Built journal: 1 cover + " + pageCount + " pages" +
                      (enablePageGravity ? " with gravity" : "") +
                      ". To save as prefab: drag into your Prefabs folder.");
        }

        JournalPage MakeLeaf(string leafName, float w, float h, float thick,
                             Material front, Material back, Material edgeMat,
                             bool rigid, int seed)
        {
            var go = new GameObject(leafName);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<JournalPage>();
            go.transform.SetParent(transform, false);

            var r = PageMeshBuilder.Build(
                rigid ? 6 : subdivisionsX,
                rigid ? 3 : subdivisionsZ,
                w, h,
                rigid ? coverThickness : thick,
                rigid ? 0f : paperNoise,
                seed);

            var leaf = go.GetComponent<JournalPage>();
            leaf.rigid = rigid;
            leaf.Initialize(r, front, back, edgeMat);

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(w * 0.5f, 0f, 0f);
            box.size = new Vector3(w, thick + 0.006f, h);

            return leaf;
        }

        static void PaintSingle(GameObject go, Material m)
        {
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        static Material MakeDefault(Color c, string matName)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { name = matName };
            m.color = c;
            return m;
        }
    }
}