using System.Collections.Generic;
using UnityEngine;

namespace Found.Journal3D
{
    /// <summary>
    /// Assembles a complete journal from parameters + two materials. Put this on an empty
    /// GameObject, drop in a Shell material and a Page material, and hit "Build Journal"
    /// (right-click the component ▸ Build Journal), or enable buildOnStart. It creates the
    /// spine, back cover, front cover, and N pages, wires them into a Journal component,
    /// and leaves you a clean, decorate-ready book you can drag onto your café table.
    ///
    /// Separate materials are exactly what you asked for:
    ///   • shellMaterial  → front cover, back cover, spine
    ///   • pageMaterial   → every page's front and back faces
    /// Swap either at runtime via Journal.SetShellMaterial / SetPageMaterial, or per-face
    /// via each JournalPage.SetMaterials.
    /// </summary>
    public class JournalBuilder : MonoBehaviour
    {
        [Header("Materials")]
        public Material shellMaterial;
        public Material pageMaterial;

        [Header("Dimensions (metres)")]
        public float pageWidth = 0.16f;
        public float pageHeight = 0.22f;
        [Min(1)] public int pageCount = 6;
        public float coverMargin = 0.006f;   // cover overhang beyond the pages
        public float coverThickness = 0.007f;
        public float spineWidth = 0.012f;

        [Header("Mesh detail")]
        [Min(2)] public int subdivisionsX = 16;
        [Min(1)] public int subdivisionsZ = 6;
        public float paperNoise = 0.0012f;

        [Header("Build")]
        public bool buildOnStart = false;

        void Start() { if (buildOnStart && GetComponent<Journal>() == null) Build(); }

        [ContextMenu("Build Journal")]
        public void Build()
        {
            // Clear any previous build.
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            var journal = GetComponent<Journal>();
            if (journal == null) journal = gameObject.AddComponent<Journal>();
            journal.leaves = new List<JournalPage>();

            Material shell = shellMaterial != null ? shellMaterial : Default(new Color(0.42f, 0.25f, 0.16f));
            Material page  = pageMaterial  != null ? pageMaterial  : Default(new Color(0.95f, 0.93f, 0.86f));

            float coverW = pageWidth + coverMargin;
            float coverH = pageHeight + coverMargin * 2f;

            // ---- Spine: a thin block along the binding, spanning the stack height. -----
            float stackY = coverThickness * 2f + pageCount * 0.0016f;
            var spine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spine.name = "Spine";
            spine.transform.SetParent(transform, false);
            spine.transform.localScale = new Vector3(spineWidth, stackY, coverH);
            spine.transform.localPosition = new Vector3(-spineWidth * 0.5f, stackY * 0.5f, 0f);
            Paint(spine, shell);

            // ---- Back cover: static base slab. -----------------------------------------
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "BackCover";
            back.transform.SetParent(transform, false);
            back.transform.localScale = new Vector3(coverW, coverThickness, coverH);
            back.transform.localPosition = new Vector3(coverW * 0.5f, coverThickness * 0.5f, 0f);
            Paint(back, shell);

            // ---- Pages: stacked leaves, front cover added last so it sits on top. -------
            // leaves[0] must be the front cover, so we create pages first then insert cover at 0.
            var pages = new List<JournalPage>();
            for (int i = 0; i < pageCount; i++)
            {
                var pg = MakeLeaf($"Page_{i + 1}", pageWidth, pageHeight, page, page,
                                  rigid: false, seed: i + 1);
                pg.transform.localPosition = new Vector3(0f, coverThickness + (i + 1) * 0.0016f, 0f);
                pages.Add(pg);
            }

            // ---- Front cover: a rigid shell leaf that opens like a page. ----------------
            var front = MakeLeaf("FrontCover", coverW, coverH, shell, shell, rigid: true, seed: 0);
            front.curlAmplitude = 0f;
            front.transform.localPosition = new Vector3(0f, coverThickness + (pageCount + 1) * 0.0016f, 0f);

            journal.leaves.Add(front);
            journal.leaves.AddRange(pages);

            Debug.Log($"[FOUND] Built journal: 1 cover + {pageCount} pages. " +
                      "Assign Shell/Page materials on the builder and rebuild to restyle.");
        }

        JournalPage MakeLeaf(string name, float w, float h, Material front, Material back,
                             bool rigid, int seed)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(JournalPage));
            go.transform.SetParent(transform, false);
            var r = PageMeshBuilder.Build(rigid ? 4 : subdivisionsX, rigid ? 2 : subdivisionsZ,
                                          w, h, rigid ? 0f : paperNoise, seed);
            var leaf = go.GetComponent<JournalPage>();
            leaf.rigid = rigid;
            leaf.Initialize(r, front, back);

            // Add a box collider so pages can be poked / grabbed / raycast against.
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(w * 0.5f, 0f, 0f);
            box.size = new Vector3(w, 0.004f, h);
            return leaf;
        }

        static void Paint(GameObject go, Material m) => go.GetComponent<MeshRenderer>().sharedMaterial = m;

        static Material Default(Color c)
        {
            // Try URP/Lit first (your project is URP), fall back to Standard.
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh) { name = "FOUND_Default" };
            m.color = c;
            return m;
        }
    }
}
