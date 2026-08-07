using UnityEngine;
using Found.Core;
using Found.Capture;
using Found.Scrap;

namespace Found.Tools
{
    /// <summary>
    /// "Capture from environment" washi tape. The framed region becomes a repeating
    /// strip; the resulting tape fragment can then be grabbed, stretched, rotated and
    /// layered on the journal via its ScrapFragment edit controls. For built-in tape
    /// rolls (solid/stripe/dot), call CreateBuiltIn from your palette UI instead.
    /// </summary>
    public class WashiTapeTool : MonoBehaviour, ITool
    {
        public FragmentFactory factory;

        public ToolId Id => ToolId.WashiTape;
        public bool UsesEnvironmentSelection => true;

        public void OnActivate() =>
            FoundEvents.Toast("Frame a pattern — the floor tiles make lovely tape. Then stretch it across a page.");
        public void OnDeactivate() { }

        public void OnSelectionComplete(in EnvironmentSelection sel)
        {
            var prov = new FragmentProvenance(
                $"Washi from {sel.PlaceLabel}",
                $"Pattern captured from {sel.PlaceLabel}",
                "tape", sel.PlaceLabel);

            var tape = factory.CreateTape(sel.CroppedTexture, prov, sel.CenterPose);
            if (tape != null)
            {
                FoundEvents.Toast($"Found a pattern in {sel.PlaceLabel}. Grab the ends to stretch it.");
                FoundEvents.RaiseRecipeItem("tape");
            }
        }

        /// <summary>Palette shortcut for a built-in tape colour/pattern (no capture).</summary>
        public ScrapFragment CreateBuiltIn(Texture2D patternTile, string label, Pose pose)
        {
            var prov = new FragmentProvenance("Washi tape", $"A roll of {label} washi from the kit", "tape", "the kit");
            return factory.CreateTape(patternTile, prov, pose);
        }
    }
}
