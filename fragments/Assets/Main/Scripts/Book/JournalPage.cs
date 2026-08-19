using System.Collections.Generic;
using UnityEngine;

namespace Fragments.Book
{
    public class JournalPage
    {
        public int index;
        public string backgroundHex = "#F8F6F0";
        public List<PageElement> elements = new();
        public Texture2D texture;
        public const int WIDTH = 512;
        public const int HEIGHT = 716;

        public JournalPage(int idx)
        {
            index = idx;
            texture = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
        }

        public void Add(PageElement e) => elements.Add(e);
        public void Clear() => elements.Clear();
    }
}
