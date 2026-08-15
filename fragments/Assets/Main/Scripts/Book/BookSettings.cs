using UnityEngine;

namespace Fragments.Book
{
    public enum BindingType { Hardcover, Rings, Staples }

    [CreateAssetMenu(fileName = "BookSettings", menuName = "Fragments/Book Settings")]
    public class BookSettings : ScriptableObject
    {
        [Header("Dimensions (metres)")]
        public float pageWidth = 0.15f;
        public float pageHeight = 0.21f;
        public float paperThickness = 0.0005f;
        [Min(2)] public int sheetCount = 8;
        public float coverOverhang = 0.005f;
        public float coverThickness = 0.002f;
        public float cornerRadius = 0.007f;

        [Header("Binding")]
        public BindingType binding = BindingType.Hardcover;
        [Range(0.15f, 1.2f)] public float spineBulge = 0.55f;
        [Tooltip("How flat the spine relaxes when the book is open. Lower = flatter.")]
        [Range(0.1f, 1f)] public float spineFlatWhenOpen = 0.35f;
        [Tooltip("How far LEFT of the block the cover hinge sits. Must be > 0.")]
        [Range(0f, 0.012f)] public float hingeGap = 0.0015f;

        [Header("Layering (prevents cover/page intersection)")]
        [Tooltip("Clearance between the opened cover board and the lowest turned page.")]
        public float coverPageClearance = 0.0035f;

        [Header("Rings")]
        public float holeInset = 0.0085f;
        public float holeRadius = 0.0038f;
        [Min(3)] public int holeCount = 9;
        [Range(0.7f, 1.8f)] public float ringSize = 1.0f;
        public float ringShift = 0f;
        public float wireRadius = 0.0006f;

        [Header("Staples")]
        [Range(0.4f, 2f)] public float stapleSize = 1.0f;
        [Min(2)] public int stapleCount = 3;
        [Range(0.2f, 0.95f)] public float stapleSpacing = 0.55f;

        [Header("Paper")]
        public float turnCurlRadius = 0.016f;
        public float restCurlRadius = 0.0015f;
        [Range(0f, 0.6f)] public float sag = 0.16f;
        public float paperSpeed = 120f;
        [Range(0f, 1f)] public float irregularity = 0.4f;
        public float paperNoise = 0.00015f;

        [Header("Cover")]
        [Range(0f, 1f)] public float coverStiffness = 0.55f;
        public float coverSpeed = 55f;

        [Header("Motion")]
        [Range(0.8f, 1.5f)] public float damping = 1.10f;
        [Range(0.06f, 0.4f)] public float convergeTime = 0.16f;

        [Header("Mesh detail")]
        [Range(16, 72)] public int spansX = 40;
        [Range(6, 28)] public int spansZ = 14;

        // ---- derived ----
        public float Gap => paperThickness + 0.0006f;
        public float CoverWidth => pageWidth + coverOverhang;
        public float CoverHeight => pageHeight + coverOverhang * 2f;

        /// <summary>
        /// Turned pages start above the opened cover board by an explicit
        /// clearance, so they can never intersect it.
        /// </summary>
        public float StackBase => coverThickness * 0.5f + coverPageClearance;

        public float StackHeight => StackBase + (sheetCount + 2) * Gap + coverThickness;

        public float BindX
        {
            get
            {
                switch (binding)
                {
                    case BindingType.Rings: return holeInset + holeRadius * 1.1f;
                    case BindingType.Staples: return 0.003f;
                    default: return 0.006f;
                }
            }
        }

        public float RingMajor()
        {
            float sh = StackHeight;
            float need = (sh * sh) / (8f * Mathf.Max(0.0008f, holeRadius));
            float encircle = sh * 0.5f + holeRadius * 1.2f;
            return Mathf.Max(need, encircle) * ringSize;
        }

        public float HingeX =>
            binding == BindingType.Rings
                ? -(RingMajor() * 0.5f + hingeGap)
                : -hingeGap;

        public float[] HoleZ()
        {
            var arr = new float[holeCount];
            for (int i = 0; i < holeCount; i++)
                arr[i] = (-0.5f + (i + 0.5f) / holeCount) * pageHeight * 0.94f;
            return arr;
        }
    }
}
