using UnityEngine;
using TMPro;

namespace Fragments.DesignSystem
{
    [CreateAssetMenu(fileName = "FragmentsTheme", menuName = "Fragments/Theme")]
    public class FragmentsTheme : ScriptableObject
    {
        [Header("Colors — Backgrounds")]
        public Color passthroughDim = new Color(0f, 0f, 0f, 0.72f);
        public Color panelSurface = new Color(1f, 1f, 1f, 1f);              // white pills/panels
        public Color panelDark = new Color(0.08f, 0.07f, 0.06f, 0.94f);     // near-black tinted panels

        [Header("Colors — Text")]
        public Color textOnDark = new Color(1f, 1f, 1f, 1f);
        public Color textOnDarkMuted = new Color(1f, 1f, 1f, 0.60f);
        public Color textOnLight = new Color(0.11f, 0.10f, 0.09f, 1f);
        public Color textOnLightMuted = new Color(0.11f, 0.10f, 0.09f, 0.55f);

        [Header("Colors — Accent")]
        public Color accentWarm = new Color(0.96f, 0.85f, 0.34f, 1f);       // buttery yellow
        public Color accentSoft = new Color(0.96f, 0.85f, 0.34f, 0.15f);
        public Color outlineHand = new Color(0.75f, 0.72f, 0.68f, 0.70f);   // sketchy off-white line
        public Color danger = new Color(0.85f, 0.35f, 0.29f, 1f);

        [Header("Shadows")]
        public Color shadowSoft = new Color(0f, 0f, 0f, 0.28f);
        public float shadowDistance = 6f;

        [Header("Spacing (pixels at Quest 3 UI scale)")]
        public float spacingXs = 6f;
        public float spacingSm = 12f;
        public float spacingMd = 20f;
        public float spacingLg = 32f;
        public float spacingXl = 48f;
        public float spacingXxl = 72f;

        [Header("Corner Radii (pixels)")]
        public float radiusSm = 8f;
        public float radiusMd = 16f;
        public float radiusLg = 24f;
        public float radiusPill = 999f;

        [Header("Typography — Fonts")]
        public TMP_FontAsset scriptFont;         // Seaweed Script
        public TMP_FontAsset bodyFont;           // Plus Jakarta Sans Regular
        public TMP_FontAsset bodyFontMedium;
        public TMP_FontAsset bodyFontSemibold;
        public TMP_FontAsset bodyFontBold;

        [Header("Typography — Sizes")]
        public float sizeDisplay = 96f;      // Fragments title
        public float sizeH1 = 64f;           // Your Journals, Customize Journal
        public float sizeH2 = 40f;           // section headers
        public float sizeH3 = 28f;           // subheadings
        public float sizeBodyLg = 22f;       // Create a new journal, or resume...
        public float sizeBody = 18f;         // form labels
        public float sizeSmall = 14f;
        public float sizeCaption = 12f;

        [Header("Motion")]
        public float hoverScale = 1.06f;
        public float pressScale = 0.94f;
        public float durFast = 0.15f;
        public float durMed = 0.28f;
        public float durSlow = 0.45f;
        public AnimationCurve easeOut = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
        public AnimationCurve easeInOut = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve easeSpring = new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(0.6f, 1.08f), new Keyframe(1, 1));

        [Header("Hover")]
        public float hoverGlowRadius = 8f;
        public Color hoverGlow = new Color(1f, 0.95f, 0.75f, 0.35f);
    }
}
