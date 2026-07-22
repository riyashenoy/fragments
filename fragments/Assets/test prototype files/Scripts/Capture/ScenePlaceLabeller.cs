using UnityEngine;

namespace Found.Capture
{
    /// <summary>
    /// Best-effort "where did this come from" labels. In the café web demo these were
    /// hard-coded object regions; in real MR you can derive them from MRUK scene anchors
    /// (TABLE, WALL_ART, PLANT, WINDOW_FRAME, etc.). This base version returns a generic
    /// label; override LabelAt to query MRUK's nearest anchor once the scene is loaded.
    /// </summary>
    public class ScenePlaceLabeller : MonoBehaviour
    {
        [Tooltip("Fallback label when no scene anchor is nearby.")]
        public string defaultLabel = "the café";

        /// <summary>
        /// Return a short human phrase for the world point a fragment was lifted from.
        /// Hook this to MRUK: find the nearest MRUKAnchor, map its label enum to phrasing
        /// like "the table" / "the wall" / "the window light".
        /// </summary>
        public virtual string LabelAt(Vector3 worldPoint)
        {
            // Example MRUK integration (uncomment once MRUK is installed):
            //
            // var room = MRUK.Instance ? MRUK.Instance.GetCurrentRoom() : null;
            // if (room != null)
            // {
            //     var anchor = room.TryGetClosestSurfacePosition(worldPoint, out _, out var a) ? a : null;
            //     if (anchor != null) return PhraseFor(anchor.Label);
            // }
            return defaultLabel;
        }
    }
}
