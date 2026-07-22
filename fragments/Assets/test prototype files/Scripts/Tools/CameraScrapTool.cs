using UnityEngine;
using Found.Core;
using Found.Capture;
using Found.Scrap;

namespace Found.Tools
{
    /// <summary>
    /// Captures a rectangular photo of the place. After framing, your UI presents the
    /// style choice (Polaroid / Borderless / Torn) and an optional caption, then calls
    /// Commit(). We stash the pending capture between the selection and the choice so the
    /// modal flow mirrors the web prototype.
    /// </summary>
    public class CameraScrapTool : MonoBehaviour, ITool
    {
        public FragmentFactory factory;
        public PhotoStyleChooser chooser;   // world-space panel that shows the 3 options

        public ToolId Id => ToolId.CameraScrap;
        public bool UsesEnvironmentSelection => true;

        EnvironmentSelection _pending;
        bool _hasPending;

        public void OnActivate() => FoundEvents.Toast("Frame a moment to keep as a photograph.");
        public void OnDeactivate() { }

        public void OnSelectionComplete(in EnvironmentSelection sel)
        {
            _pending = sel;
            _hasPending = true;
            if (chooser != null) chooser.Show(sel.CroppedTexture, OnChoice);
            else OnChoice(FragmentFactory.PhotoStyle.Polaroid, ""); // fallback: default style
        }

        void OnChoice(FragmentFactory.PhotoStyle style, string caption)
        {
            if (!_hasPending) return;
            var label = style switch
            {
                FragmentFactory.PhotoStyle.Polaroid   => "Polaroid",
                FragmentFactory.PhotoStyle.Torn       => "Torn photo scrap",
                _                                     => "Photograph"
            };
            var prov = new FragmentProvenance(
                label,
                $"A {label.ToLower()} of {_pending.PlaceLabel}" + (string.IsNullOrEmpty(caption) ? "" : $" — \"{caption}\""),
                "photo", _pending.PlaceLabel);

            factory.CreatePhoto(_pending.CroppedTexture, style, caption, prov, _pending.CenterPose);
            FoundEvents.Toast($"{label} kept. Grab it onto a page.");
            FoundEvents.RaiseRecipeItem("photo");
            _hasPending = false;
        }
    }
}
