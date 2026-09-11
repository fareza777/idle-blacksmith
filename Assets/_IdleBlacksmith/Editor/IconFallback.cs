using System.IO;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Draws simple flat icons for any Recraft PNG that failed to download,
    /// so the UI never ships with missing sprites. Only fills gaps.
    /// </summary>
    public static class IconFallback
    {
        const int S = 256;

        static readonly Color32 White = new Color32(255, 255, 255, 255);
        static readonly Color32 Brown = new Color32(107, 65, 39, 255);
        static readonly Color32 Gold = new Color32(242, 193, 78, 255);
        static readonly Color32 GoldDark = new Color32(210, 152, 48, 255);
        static readonly Color32 Steel = new Color32(60, 65, 76, 255);
        static readonly Color32 SteelLight = new Color32(168, 177, 190, 255);
        static readonly Color32 Wood = new Color32(122, 78, 46, 255);
        static readonly Color32 Skin = new Color32(243, 198, 158, 255);
        static readonly Color32 Hair = new Color32(74, 50, 38, 255);
        static readonly Color32 Red = new Color32(217, 95, 78, 255);
        static readonly Color32 Leather = new Color32(122, 78, 46, 255);

        public static void Ensure()
        {
            Try("coin", DrawCoin);
            Try("craft", DrawCraft);
            Try("carry", DrawCarry);
            Try("rack", DrawRack);
            Try("helper", DrawHelper);
            Try("sound_on", p => DrawSpeaker(p, false));
            Try("sound_off", p => DrawSpeaker(p, true));
            Try("logo", DrawLogo);
            Try("dungeon", DrawDungeon);
            Try("ore", DrawOre);
        }

        static void Try(string name, System.Action<Canvas2D> draw)
        {
            string path = $"{Paths.IconRaw}/{name}.png";
            if (File.Exists(path) && new FileInfo(path).Length > 10000) return;
            var c = new Canvas2D();
            draw(c);
            File.WriteAllBytes(path, c.Encode());
            Debug.Log($"[IconFallback] drew fallback icon '{name}'");
        }

        // ------------------------------------------------------------ icons

        static void DrawCoin(Canvas2D c)
        {
            c.Circle(128, 128, 100, GoldDark);
            c.Circle(128, 122, 88, Gold);
            c.Star(128, 122, 52, 22, GoldDark);
        }

        static void DrawCraft(Canvas2D c)
        {
            // anvil
            c.Rect(58, 176, 140, 26, Steel);
            c.Rect(102, 140, 52, 36, Steel);
            c.Rect(56, 108, 150, 30, SteelLight);
            c.Tri(206, 108, 236, 122, 206, 138, SteelLight);
            // hammer
            c.RotRect(150, 62, 16, 86, 35, Wood);
            c.RotRect(150, 92, 62, 30, 35, Steel);
        }

        static void DrawCarry(Canvas2D c)
        {
            // boot
            c.RRect(58, 156, 140, 30, 12, Leather);
            c.RRect(64, 84, 62, 82, 10, Wood);
            c.RRect(120, 130, 66, 40, 10, Wood);
            // wing
            c.Ellipse(196, 96, 44, 20, White);
            c.Ellipse(188, 118, 36, 15, White);
            c.Ellipse(182, 136, 26, 11, White);
        }

        static void DrawRack(Canvas2D c)
        {
            c.Rect(52, 60, 18, 140, Wood);
            c.Rect(186, 60, 18, 140, Wood);
            c.Rect(44, 92, 168, 14, Wood);
            c.Rect(44, 150, 168, 14, Wood);
            // crossed swords
            c.RotRect(128, 128, 12, 130, 35, SteelLight);
            c.RotRect(128, 128, 12, 130, -35, SteelLight);
            c.RotRect(97, 158, 30, 10, 35, Gold);
            c.RotRect(159, 158, 30, 10, -35, Gold);
        }

        static void DrawHelper(Canvas2D c)
        {
            c.Circle(128, 138, 78, Skin);
            c.HalfDisc(128, 138, 82, Hair);           // hair top
            c.Rect(50, 108, 156, 20, Red);           // headband
            c.Circle(100, 142, 9, Steel);            // eyes
            c.Circle(156, 142, 9, Steel);
            c.Ellipse(128, 178, 22, 12, new Color32(226, 171, 127, 255)); // smile shadow
        }

        static void DrawSpeaker(Canvas2D c, bool muted)
        {
            c.Rect(52, 104, 40, 52, Steel);
            c.Tri(92, 104, 150, 62, 150, 198, Steel);
            if (muted)
            {
                c.RotRect(196, 130, 14, 64, 45, Red);
                c.RotRect(196, 130, 14, 64, -45, Red);
            }
            else
            {
                c.Arc(96, 130, 62, -55, 55, 10, Steel);
                c.Arc(96, 130, 92, -55, 55, 10, Steel);
            }
        }

        static void DrawLogo(Canvas2D c)
        {
            c.Circle(128, 128, 108, Brown);
            c.Circle(128, 128, 96, new Color32(251, 243, 228, 255));
            c.RotRect(128, 128, 14, 150, 45, SteelLight);   // sword
            c.RotRect(128, 128, 16, 130, -45, Wood);       // hammer handle
            c.RotRect(104, 80, 56, 30, -45, Steel);        // hammer head
            c.RotRect(150, 176, 34, 10, 45, Gold);         // sword guard
        }

        static void DrawDungeon(Canvas2D c)
        {
            var stone = new Color32(110, 112, 124, 255);
            var dark = new Color32(30, 34, 44, 255);
            var cyan = new Color32(127, 212, 232, 255);
            c.Circle(128, 148, 100, stone);                 // stone ring
            c.Circle(128, 148, 76, dark);                   // cave mouth
            c.Tri(92, 206, 106, 138, 78, 138, cyan);        // crystal spikes
            c.Tri(152, 196, 164, 142, 140, 142, cyan);
            c.Rect(168, 150, 10, 44, Wood);                 // torch stick
            c.Circle(173, 122, 16, new Color32(242, 153, 74, 255)); // flame
        }

        static void DrawOre(Canvas2D c)
        {
            var rock = new Color32(86, 91, 102, 255);
            var cyan = new Color32(127, 212, 232, 255);
            c.Circle(118, 158, 74, rock);                   // rock base
            c.Circle(158, 176, 42, rock);
            c.Tri(88, 218, 108, 120, 68, 120, cyan);        // crystal spikes
            c.Tri(128, 232, 146, 130, 110, 130, cyan);
            c.Tri(162, 206, 176, 140, 148, 140, cyan);
            c.Tri(126, 210, 136, 150, 116, 150, White);     // highlight
        }

        // ------------------------------------------------------------ tiny raster canvas

        public class Canvas2D
        {
            readonly Color32[] px = new Color32[S * S];

            public Canvas2D()
            {
                for (int i = 0; i < px.Length; i++) px[i] = White;
            }

            void Set(int x, int y, Color32 col, float coverage = 1f)
            {
                if (x < 0 || y < 0 || x >= S || y >= S || coverage <= 0f) return;
                Color32 dst = px[y * S + x];
                float a = col.a / 255f * Mathf.Clamp01(coverage);
                px[y * S + x] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Lerp(dst.r, col.r, a)),
                    (byte)Mathf.RoundToInt(Mathf.Lerp(dst.g, col.g, a)),
                    (byte)Mathf.RoundToInt(Mathf.Lerp(dst.b, col.b, a)),
                    255);
            }

            public void Circle(float cx, float cy, float r, Color32 col)
            {
                for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                    for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        Set(x, y, col, r - d + 0.5f);
                    }
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color32 col)
            {
                for (int y = (int)(cy - ry - 1); y <= cy + ry + 1; y++)
                    for (int x = (int)(cx - rx - 1); x <= cx + rx + 1; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        float d = dx * dx + dy * dy;
                        if (d < 1.3f) Set(x, y, col, (1f - d) * 2f + 0.4f);
                    }
            }

            public void HalfDisc(float cx, float cy, float r, Color32 col)
            {
                for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                    for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                    {
                        if (y < cy) continue;
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        Set(x, y, col, r - d + 0.5f);
                    }
            }

            public void Rect(float x, float y, float w, float h, Color32 col)
            {
                for (int yy = (int)y; yy < y + h; yy++)
                    for (int xx = (int)x; xx < x + w; xx++)
                        Set(xx, yy, col);
            }

            public void RRect(float x, float y, float w, float h, float r, Color32 col)
            {
                for (int yy = (int)y; yy < y + h; yy++)
                    for (int xx = (int)x; xx < x + w; xx++)
                    {
                        float qx = Mathf.Max(x + r - xx, 0, xx - (x + w - r));
                        float qy = Mathf.Max(y + r - yy, 0, yy - (y + h - r));
                        float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                        Set(xx, yy, col, 0.5f - d);
                    }
            }

            public void RotRect(float cx, float cy, float w, float h, float deg, Color32 col)
            {
                float rad = -deg * Mathf.Deg2Rad;
                float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                float ext = (w + h) / 2f + 2;
                for (int y = (int)(cy - ext); y <= cy + ext; y++)
                    for (int x = (int)(cx - ext); x <= cx + ext; x++)
                    {
                        float dx = x - cx, dy = y - cy;
                        float lx = dx * cs - dy * sn;
                        float ly = dx * sn + dy * cs;
                        float ex = Mathf.Abs(lx) - w / 2f;
                        float ey = Mathf.Abs(ly) - h / 2f;
                        float d = Mathf.Max(ex, ey);
                        Set(x, y, col, 0.5f - d);
                    }
            }

            public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Color32 col)
            {
                float minX = Mathf.Min(ax, bx, cx), maxX = Mathf.Max(ax, bx, cx);
                float minY = Mathf.Min(ay, by, cy), maxY = Mathf.Max(ay, by, cy);
                float denom = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
                if (Mathf.Abs(denom) < 0.001f) return;
                for (int y = (int)minY; y <= maxY; y++)
                    for (int x = (int)minX; x <= maxX; x++)
                    {
                        float w1 = ((by - cy) * (x - cx) + (cx - bx) * (y - cy)) / denom;
                        float w2 = ((cy - ay) * (x - cx) + (ax - cx) * (y - cy)) / denom;
                        float w3 = 1f - w1 - w2;
                        if (w1 >= -0.02f && w2 >= -0.02f && w3 >= -0.02f) Set(x, y, col);
                    }
            }

            public void Star(float cx, float cy, float ro, float ri, Color32 col)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a0 = -Mathf.PI / 2f + i * Mathf.PI * 2f / 5f;
                    float a1 = a0 + Mathf.PI / 5f;
                    float a2 = a0 + Mathf.PI * 2f / 5f;
                    Tri(cx, cy,
                        cx + Mathf.Cos(a0) * ro, cy + Mathf.Sin(a0) * ro,
                        cx + Mathf.Cos(a1) * ri, cy + Mathf.Sin(a1) * ri, col);
                    Tri(cx, cy,
                        cx + Mathf.Cos(a1) * ri, cy + Mathf.Sin(a1) * ri,
                        cx + Mathf.Cos(a2) * ro, cy + Mathf.Sin(a2) * ro, col);
                }
            }

            public void Arc(float cx, float cy, float r, float fromDeg, float toDeg, float thick, Color32 col)
            {
                for (int y = (int)(cy - r - thick); y <= cy + r + thick; y++)
                    for (int x = (int)(cx - r - thick); x <= cx + r + thick; x++)
                    {
                        float dx = x - cx, dy = y - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg; // y down in image space
                        if (ang < fromDeg || ang > toDeg) continue;
                        float cov = thick / 2f - Mathf.Abs(d - r) + 0.5f;
                        Set(x, y, col, cov);
                    }
            }

            public byte[] Encode()
            {
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                // flip vertically: our y grows down, textures grow up
                var flipped = new Color32[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                        flipped[(S - 1 - y) * S + x] = px[y * S + x];
                tex.SetPixels32(flipped);
                tex.Apply(false);
                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                return bytes;
            }
        }
    }
}
