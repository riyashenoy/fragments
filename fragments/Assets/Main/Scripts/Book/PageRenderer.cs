using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragments.Book
{
    /// <summary>
    /// CPU-rasterizes a <see cref="JournalPage"/> into its Texture2D.
    /// Mirrors the v10 prototype's PR.render / PR.el.
    /// </summary>
    public static class PageRenderer
    {
        public static void Render(JournalPage page)
        {
            if (page == null || page.texture == null) return;

            int w = JournalPage.WIDTH;
            int h = JournalPage.HEIGHT;
            var px = new Color32[w * h];

            Color32 bg = Parse(page.backgroundHex, new Color32(0xF8, 0xF6, 0xF0, 255));
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            DrawRules(px, w, h);
            DrawPageNumber(px, w, h, page.index);

            var sorted = new List<PageElement>(page.elements);
            sorted.Sort((a, b) => a.layer.CompareTo(b.layer));
            for (int i = 0; i < sorted.Count; i++)
                DrawElement(px, w, h, sorted[i]);

            page.texture.SetPixels32(px);
            page.texture.Apply(false);
        }

        static void DrawRules(Color32[] px, int w, int h)
        {
            var line = new Color32(92, 112, 142, 33); // rgba(92,112,142,.13)
            const int step = 40;
            for (int canvasY = 100; canvasY < h - 50; canvasY += step)
            {
                int y = h - 1 - canvasY;
                FillHLine(px, w, h, 72, w - 44, y, line);
            }
        }

        static void DrawPageNumber(Color32[] px, int w, int h, int index)
        {
            var ink = new Color32(92, 82, 62, 92); // rgba(92,82,62,.36)
            string num = (index + 1).ToString();
            bool even = (index & 1) == 0;
            int y = 26;
            if (even)
                DrawDigits(px, w, h, num, w - 42, y, ink, true);
            else
                DrawDigits(px, w, h, num, 56, y, ink, false);
        }

        static void DrawElement(Color32[] px, int w, int h, PageElement e)
        {
            if (e == null) return;
            float cx = e.u * w;
            float cy = (1f - e.v) * h;
            Color32 col = Parse(e.colorHex, new Color32(0xD9, 0x58, 0x4A, 255));
            float s = Mathf.Max(0.05f, e.scale);

            switch (e.type)
            {
                case "tape":
                    DrawTape(px, w, h, cx, cy, 118f * s, 32f * s, e.rotation, col);
                    break;
                case "photo":
                    DrawPhoto(px, w, h, cx, cy, 92f * s, 108f * s, col);
                    break;
                default:
                    DrawSticker(px, w, h, cx, cy, 28f * s, col);
                    break;
            }
        }

        static void DrawSticker(Color32[] px, int w, int h, float cx, float cy, float r, Color32 col)
        {
            var halo = new Color32(255, 253, 247, 255);
            FillCircle(px, w, h, cx, cy, r + 7f, halo);
            FillCircle(px, w, h, cx, cy, r, col);
        }

        static void DrawTape(Color32[] px, int w, int h, float cx, float cy,
                             float tw, float th, float rot, Color32 col)
        {
            col.a = 217; // ~85%
            float hw = tw * 0.5f, hh = th * 0.5f;
            float c = Mathf.Cos(rot), s = Mathf.Sin(rot);

            var quad = new Vector2[4];
            quad[0] = Rot(cx, cy, -hw, -hh, c, s);
            quad[1] = Rot(cx, cy,  hw, -hh, c, s);
            quad[2] = Rot(cx, cy,  hw,  hh, c, s);
            quad[3] = Rot(cx, cy, -hw,  hh, c, s);

            int x0 = Mathf.Max(0, Mathf.FloorToInt(Min4(quad[0].x, quad[1].x, quad[2].x, quad[3].x)));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Max4(quad[0].x, quad[1].x, quad[2].x, quad[3].x)));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Min4(quad[0].y, quad[1].y, quad[2].y, quad[3].y)));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Max4(quad[0].y, quad[1].y, quad[2].y, quad[3].y)));

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (PointInConvexQuad(x + 0.5f, y + 0.5f, quad))
                        Blend(px, y * w + x, col);
        }

        static void DrawPhoto(Color32[] px, int w, int h, float cx, float cy,
                              float pw, float ph, Color32 col)
        {
            float hw = pw * 0.5f, hh = ph * 0.5f;
            FillRect(px, w, h, cx - hw, cy - hh, pw, ph, new Color32(253, 251, 244, 255));
            FillRect(px, w, h, cx - hw + 7f, cy - hh + 7f, pw - 14f, ph - 28f, col);
        }

        // ------------------------------------------------------------------
        static void FillCircle(Color32[] px, int w, int h, float cx, float cy, float r, Color32 col)
        {
            float r2 = r * r;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f) - cy;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    if (dx * dx + dy * dy <= r2)
                        Blend(px, y * w + x, col);
                }
            }
        }

        static void FillRect(Color32[] px, int w, int h, float x, float y, float rw, float rh, Color32 col)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(x + rw) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(y + rh) - 1);
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                    Blend(px, yy * w + xx, col);
        }

        static void FillHLine(Color32[] px, int w, int h, int x0, int x1, int y, Color32 col)
        {
            if ((uint)y >= (uint)h) return;
            if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
            x0 = Mathf.Clamp(x0, 0, w - 1);
            x1 = Mathf.Clamp(x1, 0, w - 1);
            int row = y * w;
            for (int x = x0; x <= x1; x++)
                Blend(px, row + x, col);
        }

        static Vector2 Rot(float cx, float cy, float lx, float ly, float c, float s)
            => new Vector2(cx + lx * c - ly * s, cy + lx * s + ly * c);

        static bool PointInConvexQuad(float px, float py, Vector2[] q)
        {
            bool pos = true, neg = true;
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = q[i], b = q[(i + 1) & 3];
                float cross = (b.x - a.x) * (py - a.y) - (b.y - a.y) * (px - a.x);
                if (cross > 0f) neg = false;
                else if (cross < 0f) pos = false;
            }
            return pos || neg;
        }

        static void Blend(Color32[] px, int i, Color32 src)
        {
            if ((uint)i >= (uint)px.Length) return;
            if (src.a == 255) { px[i] = src; return; }
            Color32 d = px[i];
            int a = src.a, ia = 255 - a;
            px[i] = new Color32(
                (byte)((src.r * a + d.r * ia) / 255),
                (byte)((src.g * a + d.g * ia) / 255),
                (byte)((src.b * a + d.b * ia) / 255),
                255);
        }

        static Color32 Parse(string hex, Color32 fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;
            return fallback;
        }

        static float Min4(float a, float b, float c, float d) => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        static float Max4(float a, float b, float c, float d) => Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));

        // 5×7 bitmap font, bits left-to-right in the low 5 bits of each row.
        static readonly byte[][] DigitGlyphs =
        {
            new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E }, // 0
            new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E }, // 1
            new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F }, // 2
            new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x01, 0x11, 0x0E }, // 3
            new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 }, // 4
            new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E }, // 5
            new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E }, // 6
            new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 }, // 7
            new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E }, // 8
            new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C }, // 9
        };

        const int DigitScale = 2;
        const int DigitW = 5;
        const int DigitH = 7;
        const int DigitGap = 1;

        static void DrawDigits(Color32[] px, int w, int h, string num, int anchorX, int baselineY,
                               Color32 col, bool rightAlign)
        {
            int stride = (DigitW + DigitGap) * DigitScale;
            int total = num.Length * stride - DigitGap * DigitScale;
            int x = rightAlign ? anchorX - total : anchorX;
            for (int i = 0; i < num.Length; i++)
            {
                int d = num[i] - '0';
                if ((uint)d < 10)
                    DrawGlyph(px, w, h, DigitGlyphs[d], x, baselineY, col);
                x += stride;
            }
        }

        static void DrawGlyph(Color32[] px, int w, int h, byte[] rows, int ox, int oy, Color32 col)
        {
            for (int row = 0; row < DigitH; row++)
            {
                byte bits = rows[DigitH - 1 - row]; // glyph row 0 is the top of the digit
                for (int bit = 0; bit < DigitW; bit++)
                {
                    if ((bits & (1 << (DigitW - 1 - bit))) == 0) continue;
                    int x0 = ox + bit * DigitScale;
                    int y0 = oy + row * DigitScale;
                    for (int dy = 0; dy < DigitScale; dy++)
                        for (int dx = 0; dx < DigitScale; dx++)
                        {
                            int x = x0 + dx, y = y0 + dy;
                            if ((uint)x < (uint)w && (uint)y < (uint)h)
                                Blend(px, y * w + x, col);
                        }
                }
            }
        }
    }
}
