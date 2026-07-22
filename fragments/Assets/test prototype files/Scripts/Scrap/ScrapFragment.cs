using UnityEngine;

namespace Found.Scrap
{
    public enum FragmentKind { Tape, Polaroid, PhotoScrap, Sticker, ColorLabel, Ink }

    /// <summary>
    /// A single placed scrapbook element living on (or near) the journal. Holds its
    /// provenance and exposes the edit affordances the Move tool drives: reposition,
    /// resize, rotate, duplicate, reorder, delete. Keep the mesh/quad as a child so
    /// scaling the visual doesn't fight collider math.
    ///
    /// Pair this with the Meta Interaction SDK's Grabbable + HandGrabInteractable for
    /// natural hand grabbing; the methods below are what your tool / UI buttons call.
    /// </summary>
    public class ScrapFragment : MonoBehaviour
    {
        public FragmentKind kind;
        public FragmentProvenance provenance = new();

        [Tooltip("The renderer whose material shows the captured texture.")]
        public Renderer visual;

        [Tooltip("Root that gets scaled/rotated by edit controls.")]
        public Transform editRoot;

        public int LayerOrder { get; private set; }

        void Reset()
        {
            editRoot = transform;
            visual = GetComponentInChildren<Renderer>();
        }

        public void SetTexture(Texture2D tex)
        {
            if (visual == null) visual = GetComponentInChildren<Renderer>();
            if (visual != null)
            {
                // Unlit so real-world colour reads true against passthrough light.
                var mat = visual.material;
                mat.mainTexture = tex;
            }
            AspectFitToTexture(tex);
        }

        /// <summary>Match the quad's local scale to the texture's aspect so it isn't stretched.</summary>
        public void AspectFitToTexture(Texture2D tex, float longestSide = 0.16f)
        {
            if (tex == null || editRoot == null) return;
            float aspect = tex.width / (float)tex.height;
            float w = aspect >= 1f ? longestSide : longestSide * aspect;
            float h = aspect >= 1f ? longestSide / aspect : longestSide;
            editRoot.localScale = new Vector3(w, h, editRoot.localScale.z);
        }

        public void Nudge(Vector3 worldDelta) => transform.position += worldDelta;

        public void RotateBy(float degrees) =>
            editRoot.Rotate(editRoot.forward, degrees, Space.World);

        public void Scale(float factor) =>
            editRoot.localScale = Vector3.Max(editRoot.localScale * factor, Vector3.one * 0.02f);

        public void SetOpacity(float a)
        {
            if (visual == null) return;
            var c = visual.material.color; c.a = a; visual.material.color = c;
        }

        public void BringForward() => Reorder(+1);
        public void SendBackward() => Reorder(-1);

        void Reorder(int dir)
        {
            LayerOrder += dir;
            // Push slightly along local normal so coplanar scraps don't z-fight.
            var p = editRoot.localPosition; p.z = -LayerOrder * 0.001f; editRoot.localPosition = p;
        }

        public ScrapFragment Duplicate()
        {
            var copy = Instantiate(gameObject, transform.position, transform.rotation, transform.parent)
                       .GetComponent<ScrapFragment>();
            copy.transform.position += transform.right * 0.02f + transform.up * 0.02f;
            copy.provenance = new FragmentProvenance(provenance.sourceName, provenance.description,
                                                     provenance.category, provenance.placeLabel);
            return copy;
        }

        public void Remove() => Destroy(gameObject);
    }
}
