using UnityEngine;

namespace Found.Scrap
{
    /// <summary>
    /// Pure runtime texture generation — the C# port of the web prototype's canvas bakers.
    /// No external assets. Everything returns a fresh Texture2D you can drop on an unlit
    /// material. These run on capture (not per-frame) so simple CPU pixel work is fine.
    /// </summary>
    public static class TextureBaker
    {
        static readonly Color Paper = new(0.968f, 0.945f, 0.890f);

        public static Texture2D Polaroid(Texture2D src, string caption)
        {
            int m = Mathf.RoundToInt(src.width * 0.08f);
            int bottom = Mathf.RoundToInt(src.width * 0.28f);
            var tex = new Texture2D(src.width + m * 2, src.height + m + bottom, TextureFormat.RGBA32, false);
            Fill(tex, Paper);
            Blit(tex, src, m, bottom); // photo sits above the fat bottom border
            // caption rendering is left to a TextMeshPro child on the prefab for crisp text;
            // the fat bottom border here is the writing space.
            tex.Apply();
            return tex;
        }

        public static Texture2D TornEdges(Texture2D src)
        {
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(src, tex);
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            var rng = new System.Random(src.GetInstanceID());
            int jag = Mathf.Max(3, w / 40);
            for (int y = 0; y < h; y++)
            {
                int left = rng.Next(0, jag), right = rng.Next(0, jag);
                for (int x = 0; x < left; x++) px[y * w + x].a = 0;
                for (int x = 0; x < right; x++) px[y * w + (w - 1 - x)].a = 0;
            }
            for (int x = 0; x < w; x++)
            {
                int top = rng.Next(0, jag), bot = rng.Next(0, jag);
                for (int y = 0; y < top; y++) px[y * w + x].a = 0;
                for (int y = 0; y < bot; y++) px[(h - 1 - y) * w + x].a = 0;
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        public static Texture2D StickerBorder(Texture2D cutout, FragmentFactory.StickerEdge edge)
        {
            int pad = Mathf.Max(6, cutout.width / 16);
            int w = cutout.width + pad * 2, h = cutout.height + pad * 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Fill(tex, new Color(0, 0, 0, 0));

            var src = cutout.GetPixels32();
            var dst = tex.GetPixels32();

            // Dilate the cutout's alpha to form the die-cut silhouette, then colour it.
            Color border = edge switch
            {
                FragmentFactory.StickerEdge.Paper => new Color(0.94f, 0.90f, 0.80f),
                _ => Color.white
            };
            for (int y = 0; y < cutout.height; y++)
                for (int x = 0; x < cutout.width; x++)
                {
                    if (src[y * cutout.width + x].a < 40) continue;
                    for (int dy = -pad; dy <= pad; dy++)
                        for (int dx = -pad; dx <= pad; dx++)
                        {
                            if (dx * dx + dy * dy > pad * pad) continue;
                            int nx = x + pad + dx, ny = y + pad + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            ref Color32 d = ref dst[ny * w + nx];
                            if (d.a == 0)
                            {
                                Color b = border;
                                if (edge == FragmentFactory.StickerEdge.Holographic)
                                    b = Color.HSVToRGB((nx / (float)w + ny / (float)h) % 1f, 0.35f, 1f);
                                d = b;
                            }
                        }
                }
            tex.SetPixels32(dst);
            // Composite original cutout on top of the border.
            Blit(tex, cutout, pad, pad, keepAlpha: true);
            tex.Apply();
            return tex;
        }

        public static Texture2D HandwrittenLabel(string text, Color ink)
        {
            // A tinted paper chip; render the actual glyphs with a TMP child on the prefab.
            var tex = new Texture2D(256, 96, TextureFormat.RGBA32, false);
            Fill(tex, new Color(Paper.r, Paper.g, Paper.b, 0.92f));
            tex.Apply();
            return tex;
        }

        // ---- Colour naming (port of the web colorName) --------------------------

        public static string NameColor(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float H = h * 360f;
            if (s < 0.13f) return v < 0.18f ? "espresso" : v < 0.42f ? "charcoal" : v < 0.72f ? "stone gray" : "milk white";
            if (H < 16f || H >= 342f) return v < 0.42f ? "oxblood" : "terracotta rose";
            if (H < 42f) return v < 0.3f ? "espresso brown" : v < 0.55f ? "toasted caramel" : (s < 0.5f ? "warm cream" : "apricot");
            if (H < 70f) return v < 0.5f ? "mustard" : "butter yellow";
            if (H < 162f) return v < 0.45f ? "olive" : "sage";
            if (H < 255f) return v < 0.5f ? "deep teal" : "morning sky";
            return "plum";
        }

        // ---- pixel helpers ------------------------------------------------------

        static void Fill(Texture2D t, Color c)
        {
            var px = new Color32[t.width * t.height];
            var c32 = (Color32)c;
            for (int i = 0; i < px.Length; i++) px[i] = c32;
            t.SetPixels32(px);
        }

        static void Blit(Texture2D dst, Texture2D src, int offX, int offY, bool keepAlpha = false)
        {
            var s = src.GetPixels32();
            for (int y = 0; y < src.height; y++)
                for (int x = 0; x < src.width; x++)
                {
                    int dx = x + offX, dy = y + offY;
                    if (dx < 0 || dy < 0 || dx >= dst.width || dy >= dst.height) continue;
                    var sc = s[y * src.width + x];
                    if (keepAlpha && sc.a < 40) continue;
                    dst.SetPixel(dx, dy, sc);
                }
        }
    }
}
