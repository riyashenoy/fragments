using System;
using UnityEngine;

namespace Found.Scrap
{
    /// <summary>
    /// The story of where a fragment was found. This is the point of FOUND — the journal
    /// keeps not just the asset but its origin. Attached to every ScrapFragment and shown
    /// by the little "i" info control, exactly like the web prototype's meta popover.
    /// </summary>
    [Serializable]
    public class FragmentProvenance
    {
        public string sourceName;     // "Table flower"
        public string description;    // "Flower from the table vase"
        public string category;       // color / tape / sticker / photo / label
        public string placeLabel;     // "the wooden table"
        public string locationName = "Café Marlow";
        public string capturedAt;     // human-readable local time

        public FragmentProvenance() { capturedAt = DateTime.Now.ToString("h:mm tt"); }

        public FragmentProvenance(string name, string desc, string category, string place)
        {
            sourceName = name;
            description = desc;
            this.category = category;
            placeLabel = place;
            capturedAt = DateTime.Now.ToString("h:mm tt");
        }

        public string OneLine =>
            $"{description}. {locationName} · {capturedAt}";
    }
}
