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
        static readonly Color32 Copper = new Color32(184, 92, 56, 255);
        static readonly Color32 Stone = new Color32(142, 142, 150, 255);
        static readonly Color32 StoneDark = new Color32(90, 90, 98, 255);
        static readonly Color32 CreamBg = new Color32(251, 243, 228, 255);
        static readonly Color32 Purple = new Color32(154, 111, 184, 255);
        static readonly Color32 Cyan = new Color32(127, 212, 232, 255);
        static readonly Color32 Ember = new Color32(255, 138, 61, 255);
        static readonly Color32 EmberDeep = new Color32(217, 95, 78, 255);
        static readonly Color32 Green = new Color32(111, 168, 92, 255);

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

            // Ore/recipe tiers — one rock silhouette, six metal colours.
            Try("copper", p => DrawMetal(p, new Color32(201, 116, 74, 255), Copper));
            Try("iron", p => DrawMetal(p, new Color32(168, 177, 190, 255), new Color32(110, 118, 130, 255)));
            Try("steel", p => DrawMetal(p, new Color32(214, 226, 240, 255), new Color32(150, 166, 186, 255)));
            Try("silver", p => DrawMetal(p, new Color32(232, 240, 250, 255), new Color32(170, 184, 200, 255)));
            Try("mithril", p => DrawMetal(p, new Color32(126, 226, 216, 255), new Color32(78, 172, 168, 255)));
            Try("dragonsteel", p => DrawMetal(p, new Color32(236, 132, 116, 255), new Color32(178, 74, 74, 255)));

            // Buildings
            Try("smithy", DrawSmithy);
            Try("mine", DrawMine);
            Try("market", DrawMarket);
            Try("gate", DrawGate);
            Try("sanctum", DrawSanctum);

            // Meta / UI
            Try("rune", DrawRune);
            Try("ember", DrawEmber);
            Try("scroll", DrawScroll);
            Try("trophy", DrawTrophy);
            Try("recipe", DrawRecipe);
            Try("star", DrawStar);
            Try("offline", DrawOffline);
            Try("chest", DrawChest);
            Try("gem", DrawGem);
            Try("settings", DrawSettings);
            Try("complex", DrawComplex);
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

        // ------------------------------------------------------------ content icons

        /// <summary>Ore rock with crystal spikes tinted to the metal tier.</summary>
        static void DrawMetal(Canvas2D c, Color32 gem, Color32 gemDark)
        {
            var rock = new Color32(86, 91, 102, 255);
            c.Circle(112, 150, 76, rock);
            c.Circle(158, 172, 42, rock);
            c.Circle(80, 176, 40, new Color32(72, 76, 86, 255));
            c.Tri(84, 216, 106, 112, 62, 112, gem);
            c.Tri(130, 238, 152, 122, 110, 122, gem);
            c.Tri(168, 200, 186, 132, 154, 132, gemDark);
            c.Tri(126, 212, 138, 142, 116, 142, White);
        }

        static void DrawSmithy(Canvas2D c)
        {
            c.Rect(40, 92, 176, 108, new Color32(244, 231, 206, 255));  // cream wall
            c.Tri(28, 92, 228, 92, 128, 178, EmberDeep);                 // roof
            c.Rect(110, 118, 36, 82, Wood);                              // door
            c.Rect(56, 128, 34, 30, new Color32(255, 138, 61, 255));     // glowing window
            c.Rect(168, 128, 34, 30, new Color32(255, 138, 61, 255));
            c.Rect(96, 226, 64, 26, Steel);                              // anvil base
            c.Rect(74, 200, 108, 26, SteelLight);                        // anvil top
            c.Rect(176, 34, 34, 62, StoneDark);                          // chimney
        }

        static void DrawMine(Canvas2D c)
        {
            c.Circle(128, 130, 116, Stone);
            c.Circle(128, 138, 88, StoneDark);
            c.Circle(128, 150, 60, new Color32(28, 32, 40, 255));        // tunnel mouth
            c.Rect(52, 62, 16, 130, Wood);                               // headframe
            c.Rect(188, 62, 16, 130, Wood);
            c.Rect(44, 74, 168, 14, Wood);
            c.Rect(44, 44, 168, 14, Wood);
            c.Tri(112, 200, 128, 120, 96, 120, Cyan);                    // crystal
            c.Tri(150, 190, 162, 132, 138, 132, Cyan);
        }

        static void DrawMarket(Canvas2D c)
        {
            c.Rect(44, 96, 40, 120, Wood);                               // posts
            c.Rect(172, 96, 40, 120, Wood);
            c.Rect(36, 210, 184, 26, Wood);                              // counter
            for (int i = 0; i < 4; i++)                                  // striped awning
                c.Rect(32 + i * 48, 150, 48, 34, i % 2 == 0 ? EmberDeep : CreamBg);
            c.Tri(24, 150, 232, 150, 128, 186, EmberDeep);
            c.Circle(96, 236, 12, Gold);                                 // coins
            c.Circle(128, 240, 12, Gold);
            c.Circle(158, 236, 12, GoldDark);
        }

        static void DrawGate(Canvas2D c)
        {
            c.Rect(48, 60, 34, 176, Stone);                              // pillars
            c.Rect(174, 60, 34, 176, Stone);
            c.Rect(34, 214, 188, 28, StoneDark);                         // lintel
            c.Arc(128, 148, 62, 0, 180, 22, StoneDark);                  // arch
            c.Circle(128, 152, 46, new Color32(30, 44, 66, 255));        // portal
            c.Ellipse(128, 152, 30, 40, new Color32(96, 168, 220, 255));
            c.Ellipse(128, 152, 16, 26, Cyan);
            c.Rect(24, 150, 12, 40, Wood);                               // torches
            c.Rect(220, 150, 12, 40, Wood);
            c.Circle(30, 138, 14, Ember);
            c.Circle(226, 138, 14, Ember);
        }

        static void DrawSanctum(Canvas2D c)
        {
            c.Rect(74, 84, 108, 160, new Color32(110, 74, 94, 255));     // tower
            c.Tri(58, 90, 198, 90, 128, 196, Purple);                    // roof
            c.Circle(128, 168, 22, Ember);                               // glowing window
            c.Circle(128, 60, 26, Cyan);                                 // floating crystal
            c.Tri(104, 96, 128, 34, 152, 96, Cyan);
            c.Circle(58, 244, 14, Cyan);
            c.Circle(200, 236, 12, Cyan);
        }

        static void DrawRune(Canvas2D c)
        {
            c.Rect(104, 40, 48, 180, StoneDark);                         // stone tablet
            c.RRect(112, 52, 32, 156, 8, new Color32(52, 58, 70, 255));
            c.Circle(128, 96, 16, Cyan);                                 // carved glyph
            c.RotRect(128, 158, 10, 66, 0, Cyan);
            c.RotRect(128, 128, 10, 52, 60, Cyan);
            c.RotRect(128, 128, 10, 52, -60, Cyan);
        }

        static void DrawEmber(Canvas2D c)
        {
            c.Tri(128, 30, 200, 150, 56, 150, EmberDeep);
            c.Tri(128, 62, 182, 158, 74, 158, Ember);
            c.Tri(128, 108, 162, 172, 94, 172, Gold);
            c.Circle(128, 176, 44, Ember);                               // fire bowl glow
            c.Ellipse(128, 190, 56, 22, new Color32(255, 200, 120, 255));
        }

        static void DrawScroll(Canvas2D c)
        {
            c.RRect(56, 44, 144, 168, 14, CreamBg);                      // parchment
            c.Circle(56, 128, 22, Wood);                                 // rolled ends
            c.Circle(200, 128, 22, Wood);
            c.Rect(84, 78, 96, 10, Stone);
            c.Rect(84, 108, 118, 10, Stone);
            c.Rect(84, 138, 84, 10, Stone);
            c.Rect(84, 168, 106, 10, Stone);
        }

        static void DrawTrophy(Canvas2D c)
        {
            c.RRect(84, 46, 88, 106, 16, Gold);                          // cup
            c.Rect(74, 46, 34, 54, GoldDark);                            // handles
            c.Rect(148, 46, 34, 54, GoldDark);
            c.Rect(116, 152, 24, 40, GoldDark);                          // stem
            c.RRect(80, 192, 96, 22, 6, Gold);                           // base
            c.Circle(128, 92, 26, GoldDark);                             // star
            c.Star(128, 92, 24, 10, Gold);
        }

        static void DrawRecipe(Canvas2D c)
        {
            c.RRect(58, 36, 140, 184, 12, new Color32(233, 196, 108, 255)); // book cover
            c.RRect(70, 48, 116, 160, 8, CreamBg);
            c.Rect(86, 76, 84, 10, Stone);                                  // text lines
            c.Rect(86, 104, 84, 10, Stone);
            c.Rect(86, 132, 60, 10, Stone);
            c.RotRect(128, 178, 36, 10, 30, SteelLight);                    // hammer mark
            c.RotRect(146, 166, 12, 34, 30, Wood);
        }

        static void DrawStar(Canvas2D c)
        {
            c.Star(128, 132, 106, 44, Gold);
            c.Star(128, 132, 74, 30, new Color32(255, 236, 160, 255));
        }

        static void DrawOffline(Canvas2D c)
        {
            c.Circle(120, 132, 100, new Color32(74, 96, 118, 255));       // moon
            c.Circle(156, 108, 88, White);                                // crescent cut
            c.Rect(186, 60, 34, 34, Gold);                                // clock hand / zzz
            c.Rect(170, 46, 30, 10, Gold);
            c.Circle(206, 158, 12, Gold);
            c.Circle(180, 196, 8, Gold);
        }

        static void DrawChest(Canvas2D c)
        {
            c.RRect(38, 116, 180, 96, 10, Wood);                          // body
            c.HalfDisc(128, 120, 92, new Color32(140, 92, 54, 255));      // lid
            c.Rect(38, 108, 180, 18, GoldDark);                           // strap
            c.Rect(112, 132, 32, 46, Gold);                               // lock
            c.Circle(128, 150, 10, GoldDark);
            c.Rect(120, 60, 16, 10, Gold);
        }

        static void DrawGem(Canvas2D c)
        {
            c.Tri(128, 34, 60, 116, 196, 116, Cyan);                      // crown
            c.Tri(60, 116, 196, 116, 128, 226, new Color32(88, 176, 208, 255));
            c.Tri(112, 46, 128, 116, 96, 116, White);                     // facet
        }

        static void DrawSettings(Canvas2D c)
        {
            var body = new Color32(120, 128, 142, 255);
            for (int i = 0; i < 8; i++)                                   // gear teeth
                c.RotRect(128, 128, 30, 196, i * 22.5f, body);
            c.Circle(128, 128, 82, body);
            c.Circle(128, 128, 40, CreamBg);
            c.Circle(128, 128, 26, new Color32(72, 78, 92, 255));
        }

        static void DrawComplex(Canvas2D c)
        {
            c.Rect(28, 176, 200, 34, new Color32(126, 176, 106, 255));     // ground
            c.Rect(52, 104, 74, 72, CreamBg);                             // small building
            c.Tri(40, 104, 138, 104, 89, 62, EmberDeep);
            c.Rect(76, 134, 26, 42, Wood);
            c.Rect(150, 84, 62, 92, Stone);                               // tall building
            c.Tri(140, 84, 222, 84, 181, 34, Purple);
            c.Rect(168, 112, 26, 26, Ember);
            c.Rect(112, 176, 22, 30, Wood);                               // signpost
            c.RotRect(128, 40, 56, 12, 0, SteelLight);                    // hammer
        }

        // ------------------------------------------------------------ tiny raster canvas

        public class Canvas2D
        {
            readonly Color32[] px = new Color32[S * S];

            public Canvas2D()
            {
                // Transparent, not white: these icons are drawn straight into the UI without the
                // background-removal pass the downloaded art goes through.
                var clear = new Color32(0, 0, 0, 0);
                for (int i = 0; i < px.Length; i++) px[i] = clear;
            }

            void Set(int x, int y, Color32 col, float coverage = 1f)
            {
                if (x < 0 || y < 0 || x >= S || y >= S || coverage <= 0f) return;
                Color32 dst = px[y * S + x];
                float srcA = col.a / 255f * Mathf.Clamp01(coverage);
                if (srcA <= 0f) return;

                // Standard source-over compositing onto a transparent canvas.
                float dstA = dst.a / 255f;
                float outA = srcA + dstA * (1f - srcA);
                if (outA <= 0.0001f) return;

                float srcR = col.r / 255f, srcG = col.g / 255f, srcB = col.b / 255f;
                float dstR = dst.r / 255f, dstG = dst.g / 255f, dstB = dst.b / 255f;

                px[y * S + x] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01((srcR * srcA + dstR * dstA * (1f - srcA)) / outA) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01((srcG * srcA + dstG * dstA * (1f - srcA)) / outA) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01((srcB * srcA + dstB * dstA * (1f - srcA)) / outA) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(outA) * 255f));
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
