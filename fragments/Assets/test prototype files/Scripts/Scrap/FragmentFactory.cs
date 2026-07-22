using UnityEngine;
using Found.Core;

namespace Found.Scrap
{
    /// <summary>
    /// Turns a raw captured region into a finished scrapbook material and spawns a
    /// ScrapFragment for it — the XR counterpart of the web prototype's bakeTape /
    /// bakePhoto / bakeSticker functions. Textures are generated at runtime so nothing
    /// depends on external art. Assign lightweight prefabs (a quad + ScrapFragment +
    /// Grabbable) for each kind.
    /// </summary>
    public class FragmentFactory : MonoBehaviour
    {
        [Header("Prefabs (quad + ScrapFragment + Grabbable)")]
        public ScrapFragment tapePrefab;
        public ScrapFragment photoPrefab;
        public ScrapFragment stickerPrefab;
        public ScrapFragment labelPrefab;

        [Tooltip("Where new fragments are parented — usually the journal's active page.")]
        public Transform placementParent;

        // ---- WASHI TAPE ---------------------------------------------------------

        public ScrapFragment CreateTape(Texture2D captured, FragmentProvenance prov, Pose pose)
        {
            // Tile the captured strip horizontally so it reads as a repeating pattern,
            // give it soft torn ends via an alpha ramp. Here we simply mark the texture
            // to repeat; the tape shader/material handles wrapping + torn-edge alpha.
            captured.wrapMode = TextureWrapMode.Repeat;

            var frag = Spawn(tapePrefab, pose);
            frag.kind = FragmentKind.Tape;
            frag.provenance = prov;
            frag.SetTexture(captured);
            frag.AspectFitToTexture(captured, 0.22f);
            // Make it strip-shaped rather than square.
            var s = frag.editRoot.localScale; frag.editRoot.localScale = new Vector3(0.22f, 0.045f, s.z);
            frag.SetOpacity(0.92f);
            FoundEvents.RaiseFragmentCreated(frag);
            return frag;
        }

        // ---- PHOTO / POLAROID ---------------------------------------------------

        public enum PhotoStyle { Polaroid, Borderless, Torn }

        public ScrapFragment CreatePhoto(Texture2D captured, PhotoStyle style,
                                         string caption, FragmentProvenance prov, Pose pose)
        {
            Texture2D finished = style switch
            {
                PhotoStyle.Polaroid   => TextureBaker.Polaroid(captured, caption),
                PhotoStyle.Torn       => TextureBaker.TornEdges(captured),
                _                     => captured
            };
            var frag = Spawn(photoPrefab, pose);
            frag.kind = style == PhotoStyle.Polaroid ? FragmentKind.Polaroid : FragmentKind.PhotoScrap;
            frag.provenance = prov;
            frag.SetTexture(finished);
            frag.AspectFitToTexture(finished, 0.18f);
            FoundEvents.RaiseFragmentCreated(frag);
            return frag;
        }

        // ---- STICKER ------------------------------------------------------------

        public enum StickerEdge { DieCut, None, Holographic, Paper }

        public ScrapFragment CreateSticker(Texture2D cutout, StickerEdge edge,
                                           FragmentProvenance prov, Pose pose)
        {
            Texture2D finished = edge == StickerEdge.None
                ? cutout
                : TextureBaker.StickerBorder(cutout, edge);
            var frag = Spawn(stickerPrefab, pose);
            frag.kind = FragmentKind.Sticker;
            frag.provenance = prov;
            frag.SetTexture(finished);
            frag.AspectFitToTexture(finished, 0.14f);
            FoundEvents.RaiseFragmentCreated(frag);
            return frag;
        }

        // ---- COLOUR LABEL -------------------------------------------------------

        public ScrapFragment CreateColorLabel(Color color, string text,
                                              FragmentProvenance prov, Pose pose)
        {
            var tex = TextureBaker.HandwrittenLabel(text, color);
            var frag = Spawn(labelPrefab, pose);
            frag.kind = FragmentKind.ColorLabel;
            frag.provenance = prov;
            frag.SetTexture(tex);
            frag.AspectFitToTexture(tex, 0.12f);
            FoundEvents.RaiseFragmentCreated(frag);
            return frag;
        }

        // ---- helper -------------------------------------------------------------

        ScrapFragment Spawn(ScrapFragment prefab, Pose pose)
        {
            if (prefab == null)
            {
                Debug.LogError("[FOUND] FragmentFactory is missing a prefab reference.");
                return null;
            }
            var frag = Instantiate(prefab, pose.position, pose.rotation, placementParent);
            return frag;
        }
    }
}
