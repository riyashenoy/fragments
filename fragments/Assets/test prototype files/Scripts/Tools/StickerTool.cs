using UnityEngine;
using Found.Core;
using Found.Capture;
using Found.Scrap;

namespace Found.Tools
{
    /// <summary>
    /// "Peel an object out of reality." Two honest paths in real MR:
    ///
    ///  A) Segmentation (accurate, heavier): feed the crop to an on-device model
    ///     (Unity Sentis — the MultiObjectDetection PCA sample is a ready starting point)
    ///     to get an alpha mask, then die-cut it. Assign an ISegmenter for this.
    ///
    ///  B) No segmenter (default): keep the framed rectangle and treat it as a torn photo
    ///     scrap — exactly the web prototype's "no predefined object → fall back to a
    ///     rectangular scrap" behaviour, so the interaction never dead-ends.
    ///
    /// Either way it plays the "lifting from reality" peel animation before the sticker
    /// becomes grabbable.
    /// </summary>
    public class StickerTool : MonoBehaviour, ITool
    {
        public FragmentFactory factory;

        [Tooltip("Optional Sentis-backed segmenter. If null, framed region becomes a torn scrap.")]
        public MonoBehaviour segmenterBehaviour; // implements ISegmenter

        public StickerEdgeChooser edgeChooser;   // world-space panel: die-cut / none / holo / paper
        public PeelAnimation peel;               // plays the lift/curl before it drops in

        public ToolId Id => ToolId.Sticker;
        public bool UsesEnvironmentSelection => true;

        ISegmenter _segmenter;

        void Awake() => _segmenter = segmenterBehaviour as ISegmenter;

        public void OnActivate() =>
            FoundEvents.Toast("Frame an object — the flower, the mug — and peel it out.");
        public void OnDeactivate() { }

        public async void OnSelectionComplete(EnvironmentSelection sel)
        {
            Texture2D cutout = sel.CroppedTexture;
            bool segmented = false;

            if (_segmenter != null)
            {
                var masked = await _segmenter.SegmentForegroundAsync(sel.CroppedTexture);
                if (masked != null) { cutout = masked; segmented = true; }
            }

            if (peel != null) await peel.Play(sel.CenterPose, cutout);

            if (!segmented)
            {
                // Graceful fallback: torn photo scrap, never a failure.
                var prov = new FragmentProvenance("Photo scrap",
                    $"A torn scrap of {sel.PlaceLabel}", "photo", sel.PlaceLabel);
                factory.CreatePhoto(cutout, FragmentFactory.PhotoStyle.Torn, "", prov, sel.CenterPose);
                FoundEvents.Toast("No single object there — kept it as a scrap instead.");
                FoundEvents.RaiseRecipeItem("photo");
                return;
            }

            void Commit(StickerEdge edge)
            {
                var prov = new FragmentProvenance("Found object",
                    $"Lifted from {sel.PlaceLabel}", "sticker", sel.PlaceLabel);
                factory.CreateSticker(cutout,
                    (FragmentFactory.StickerEdge)edge, prov, sel.CenterPose);
                FoundEvents.Toast("Sticker peeled. Grab it onto a page.");
                FoundEvents.RaiseRecipeItem("sticker");
            }

            if (edgeChooser != null) edgeChooser.Show(cutout, e => Commit(e));
            else Commit(StickerEdge.DieCut);
        }

        // Kept as struct-by-value for the interface; async needs a plain param.
        void ITool.OnSelectionComplete(in EnvironmentSelection sel) => OnSelectionComplete(sel);
    }

    /// <summary>Mirror of FragmentFactory.StickerEdge so UI chooser stays decoupled.</summary>
    public enum StickerEdge { DieCut, None, Holographic, Paper }

    /// <summary>Plug an on-device model here (e.g. Unity Sentis). Return null to fall back.</summary>
    public interface ISegmenter
    {
        System.Threading.Tasks.Task<Texture2D> SegmentForegroundAsync(Texture2D region);
    }
}
