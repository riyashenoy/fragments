using System.Collections.Generic;
using UnityEngine;

namespace Fragments.Book
{
    [System.Serializable]
    public class PageElement
    {
        public string type = "sticker";  // "sticker", "tape", "photo", "stroke"
        public float u = 0.5f;
        public float v = 0.5f;
        public float rotation = 0f;
        public float scale = 1f;
        public string colorHex = "#D9584A";
        public int layer = 0;

        // For draw strokes only
        public List<StrokePoint> points = new();
        public float thickness = 2f;

        // For text
        public string text = "";
        public float fontSize = 20f;
    }
}
