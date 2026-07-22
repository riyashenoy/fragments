using UnityEngine;
using Found.Core;
using Found.Capture;
using Found.Scrap;

namespace Found.Tools
{
    /// <summary>
    /// Environmental eyedropper. On selection it averages the captured region into one
    /// colour, names it ("warm cream"), records where it came from, and raises an event
    /// so your UI can offer: apply to page background / cover / pen ink / tape tint /
    /// sticker border / handwritten label — matching the web sampler panel.
    ///
    /// Uses the pinch frame small; even a tiny frame yields a usable average.
    /// </summary>
    public class ColorSamplerTool : MonoBehaviour, ITool
    {
        public FragmentFactory factory;
        public JournalPageColor pageColor;   // component that tints the active page/cover

        public ToolId Id => ToolId.ColorSampler;
        public bool UsesEnvironmentSelection => true;

        public void OnActivate() => FoundEvents.Toast("Frame anything in the café to lift its colour.");
        public void OnDeactivate() { }

        public void OnSelectionComplete(in EnvironmentSelection sel)
        {
            Color avg = AverageColor(sel.CroppedTexture);
            string name = TextureBaker.NameColor(avg);
            var prov = new FragmentProvenance(
                $"{Capitalize(name)} colour",
                $"{Capitalize(name)} sampled from {sel.PlaceLabel}",
                "color", sel.PlaceLabel);

            // Simplest default: wash the current page in the colour, like the web demo's
            // "page background" apply. Your UI can offer the other targets.
            if (pageColor != null) pageColor.ApplyToActive(avg);

            FoundEvents.Toast($"{prov.description}.");
            FoundEvents.RaiseRecipeItem("color");
        }

        static Color AverageColor(Texture2D t)
        {
            var px = t.GetPixels32();
            long r = 0, g = 0, b = 0;
            // Sample sparsely for speed on large crops.
            int step = Mathf.Max(1, px.Length / 4096);
            int n = 0;
            for (int i = 0; i < px.Length; i += step) { r += px[i].r; g += px[i].g; b += px[i].b; n++; }
            if (n == 0) return Color.gray;
            return new Color(r / (255f * n), g / (255f * n), b / (255f * n));
        }

        static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
