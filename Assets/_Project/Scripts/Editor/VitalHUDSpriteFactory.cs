using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class VitalHUDSpriteFactory
    {
        private const string OutputDir = "Assets/_Project/UI/Sprites/VitalHUD";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Generate Vital HUD Sprites", false, 11)]
        public static void GenerateAllVitalSprites()
        {
            EnsureDirectory();

            Debug.Log("[VitalHUDSpriteFactory] Generating High-Definition Procedural Vital HUD Sprites...");

            CreateTacticalGlassPanel();
            CreateBarFrameAndFill();
            CreateStatusIcons();
            CreateRadialProgressRing();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VitalHUDSpriteFactory] Successfully generated all Vital HUD Sprites in " + OutputDir);
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
                AssetDatabase.Refresh();
            }
        }

        // =========================================================================
        // 1. BEVELED TACTICAL GLASS PANEL (256x256, 9-SLICE BORDER 24, 24, 24, 24)
        // =========================================================================
        private static void CreateTacticalGlassPanel()
        {
            int size = 256;
            int cornerBevel = 18;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color obsidianBg = new Color(0.043f, 0.055f, 0.078f, 0.84f); // #0B0E14, Alpha 0.84
            Color rimColor = new Color(0.227f, 0.267f, 0.329f, 0.88f);   // #3A4454 brushed gunmetal
            Color innerGlow = new Color(0.38f, 0.44f, 0.54f, 0.22f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Chamfered / Angled corners distance
                    int dx = Mathf.Max(0, Mathf.Max(cornerBevel - x, x - (size - 1 - cornerBevel)));
                    int dy = Mathf.Max(0, Mathf.Max(cornerBevel - y, y - (size - 1 - cornerBevel)));
                    int chamferDist = dx + dy;

                    if (chamferDist > cornerBevel)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Outer rim (1.5px to 2.5px)
                    bool isRim = (chamferDist >= cornerBevel - 2) || (x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2);

                    // Inner border glow
                    bool isInnerRim = (chamferDist >= cornerBevel - 4) || (x <= 3 || x >= size - 4 || y <= 3 || y >= size - 4);

                    // Faint diagonal micro-grid texture (Alpha: 0.05)
                    bool isGrid = ((x + y) % 7 == 0);
                    float gridAlpha = isGrid ? 0.05f : 0f;

                    // Subtle top-to-bottom subtle vertical gradient for glass realism
                    float vGrad = Mathf.Lerp(0.04f, -0.04f, (float)y / size);

                    if (isRim)
                    {
                        tex.SetPixel(x, y, rimColor);
                    }
                    else if (isInnerRim)
                    {
                        tex.SetPixel(x, y, innerGlow);
                    }
                    else
                    {
                        Color c = new Color(
                            Mathf.Clamp01(obsidianBg.r + vGrad + gridAlpha),
                            Mathf.Clamp01(obsidianBg.g + vGrad + gridAlpha),
                            Mathf.Clamp01(obsidianBg.b + vGrad + gridAlpha),
                            obsidianBg.a
                        );
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/vital_panel_bg.png", tex, new Vector4(24, 24, 24, 24));
        }

        // =========================================================================
        // 2. SEGMENTED BAR FRAME & FILL (vital_bar_frame.png & vital_bar_fill.png)
        // =========================================================================
        private static void CreateBarFrameAndFill()
        {
            // A. Bar Frame (256x64, 9-slice: 16, 6, 16, 6)
            int w = 256;
            int h = 64;
            Texture2D frameTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color frameRim = new Color(0.24f, 0.28f, 0.35f, 0.95f);      // #3D4759
            Color recessedTrack = new Color(0.06f, 0.075f, 0.095f, 0.92f); // #0F1318
            Color tickColor = new Color(0.35f, 0.42f, 0.52f, 0.55f);     // Subtle tick markers

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);

                    // Slanted edge corners (4px slant at ends)
                    if ((x < 4 && y < (4 - x)) || (x < 4 && y >= (h - 4 + x)) ||
                        (x >= w - 4 && y < (x - (w - 4))) || (x >= w - 4 && y >= (h - (x - (w - 4)))))
                    {
                        frameTex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (isBorder)
                    {
                        frameTex.SetPixel(x, y, frameRim);
                        continue;
                    }

                    // Ticks at 25%, 50%, 75%
                    bool isTick = (x == w / 4 || x == w / 2 || x == (w * 3) / 4) && (y >= 4 && y <= h - 5);

                    // Top specular highlight gradient (top 35% height)
                    float spec = v > 0.65f ? (v - 0.65f) * 0.4f : 0f;

                    if (isTick)
                    {
                        frameTex.SetPixel(x, y, tickColor);
                    }
                    else
                    {
                        Color c = new Color(
                            Mathf.Clamp01(recessedTrack.r + spec),
                            Mathf.Clamp01(recessedTrack.g + spec),
                            Mathf.Clamp01(recessedTrack.b + spec),
                            recessedTrack.a
                        );
                        frameTex.SetPixel(x, y, c);
                    }
                }
            }

            frameTex.Apply();
            SaveSpriteAsset($"{OutputDir}/vital_bar_frame.png", frameTex, new Vector4(16, 6, 16, 6));

            // B. Bar Fill (128x32)
            int fw = 128;
            int fh = 32;
            Texture2D fillTex = new Texture2D(fw, fh, TextureFormat.RGBA32, false);

            for (int y = 0; y < fh; y++)
            {
                float v = (float)y / fh;
                // Subtle top specular highlight so tinted bars look physical and volumetric
                float spec = Mathf.Lerp(0.88f, 1.0f, v);
                for (int x = 0; x < fw; x++)
                {
                    fillTex.SetPixel(x, y, new Color(spec, spec, spec, 1.0f));
                }
            }

            fillTex.Apply();
            SaveSpriteAsset($"{OutputDir}/vital_bar_fill.png", fillTex, new Vector4(4, 4, 4, 4));
        }

        // =========================================================================
        // 3. CRISP VECTOR-STYLE STATUS ICONS (64x64, PURE WHITE #FFFFFF)
        // =========================================================================
        private static void CreateStatusIcons()
        {
            CreateHeartIcon();
            CreateBoltIcon();
            CreateHungerIcon();
            CreateThirstIcon();
            CreateInfectionIcon();
        }

        // A. Tactical Heart Icon (64x64)
        private static void CreateHeartIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 4x Supersampling for anti-aliasing
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = ((x + (sx + 0.5f) * 0.5f) - 32f) / 19f;
                            float py = ((y + (sy + 0.5f) * 0.5f) - 34f) / 19f;

                            // Standard mathematical heart curve: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                            float a = px * px + py * py - 1f;
                            float heartVal = a * a * a - px * px * py * py * py;
                            if (heartVal <= 0f) hits += 1f;
                        }
                    }

                    if (hits > 0f)
                    {
                        float alpha = hits / 4f;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_heart.png", tex, Vector4.zero);
        }

        // B. Sharp Tactical Lightning Bolt (64x64)
        private static void CreateBoltIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);

            // 7-vertex sharp tactical bolt polygon
            Vector2[] poly = new Vector2[]
            {
                new Vector2(35f, 58f),
                new Vector2(17f, 33f),
                new Vector2(31f, 33f),
                new Vector2(25f, 6f),
                new Vector2(47f, 35f),
                new Vector2(33f, 35f),
                new Vector2(37f, 58f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2 pt = new Vector2(x + (sx + 0.5f) * 0.5f, y + (sy + 0.5f) * 0.5f);
                            if (IsPointInPolygon(pt, poly)) hits += 1f;
                        }
                    }

                    if (hits > 0f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 4f));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_bolt.png", tex, Vector4.zero);
        }

        // C. Fork & Knife Ration Icon (64x64)
        private static void CreateHungerIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = x + (sx + 0.5f) * 0.5f;
                            float py = y + (sy + 0.5f) * 0.5f;

                            bool filled = false;

                            // Left: Fork
                            if (px >= 18f && px <= 26f && py >= 10f && py <= 28f) // handle
                            {
                                if (Mathf.Abs(px - 22f) <= 1.8f) filled = true;
                            }
                            else if (px >= 17f && px <= 27f && py >= 28f && py <= 54f) // tines
                            {
                                if (py <= 36f && Mathf.Abs(px - 22f) <= 4.5f) filled = true;
                                else if (py > 36f && (Mathf.Abs(px - 18.5f) <= 1.0f || Mathf.Abs(px - 22f) <= 1.0f || Mathf.Abs(px - 25.5f) <= 1.0f)) filled = true;
                            }

                            // Right: Knife
                            if (px >= 38f && px <= 46f && py >= 10f && py <= 28f) // handle
                            {
                                if (Mathf.Abs(px - 42f) <= 1.8f) filled = true;
                            }
                            else if (px >= 38f && px <= 46f && py >= 28f && py <= 54f) // blade
                            {
                                float curW = Mathf.Lerp(4.5f, 0.5f, (py - 28f) / 26f);
                                if ((px - 40f) >= 0 && (px - 40f) <= curW) filled = true;
                            }

                            if (filled) hits += 1f;
                        }
                    }

                    if (hits > 0f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 4f));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_hunger.png", tex, Vector4.zero);
        }

        // D. Stylized Water Droplet Icon (64x64)
        private static void CreateThirstIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Vector2 circleCenter = new Vector2(32f, 26f);
            float circleRadius = 16.5f;
            Vector2 tip = new Vector2(32f, 56f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2 p = new Vector2(x + (sx + 0.5f) * 0.5f, y + (sy + 0.5f) * 0.5f);
                            bool inside = false;

                            if (Vector2.Distance(p, circleCenter) <= circleRadius)
                            {
                                inside = true;
                            }
                            else if (p.y >= circleCenter.y && p.y <= tip.y)
                            {
                                float t = (p.y - circleCenter.y) / (tip.y - circleCenter.y);
                                float maxHalfW = circleRadius * Mathf.Sqrt(1f - t * t * 0.4f) * (1f - t);
                                if (Mathf.Abs(p.x - 32f) <= maxHalfW) inside = true;
                            }

                            if (inside) hits += 1f;
                        }
                    }

                    if (hits > 0f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 4f));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_thirst.png", tex, Vector4.zero);
        }

        // E. Biohazard / Infection Pathogen Emblem (64x64)
        private static void CreateInfectionIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Vector2 c = new Vector2(32f, 32f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2 p = new Vector2(x + (sx + 0.5f) * 0.5f, y + (sy + 0.5f) * 0.5f);
                            float dCenter = Vector2.Distance(p, c);

                            bool inHazard = false;

                            // 3 lobe centers at 90, 210, 330 degrees
                            float[] angles = new float[] { 90f, 210f, 330f };
                            for (int i = 0; i < 3; i++)
                            {
                                float rad = angles[i] * Mathf.Deg2Rad;
                                Vector2 lobeCenter = c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 12.5f;
                                float dLobe = Vector2.Distance(p, lobeCenter);

                                // Crescent ring (outer 15.5, inner 9.5)
                                if (dLobe <= 15.5f && dLobe >= 9.5f)
                                {
                                    inHazard = true;
                                }
                            }

                            // Center cutout circle
                            if (dCenter <= 5.5f) inHazard = false;
                            // Outer boundary
                            if (dCenter > 26.5f) inHazard = false;

                            // Central dot
                            if (dCenter <= 2.2f) inHazard = true;

                            if (inHazard) hits += 1f;
                        }
                    }

                    if (hits > 0f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 4f));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_infection.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 4. SMOOTH 360 ANTI-ALIASED CIRCULAR PROGRESS RING (128x128)
        // =========================================================================
        private static void CreateRadialProgressRing()
        {
            int size = 128;
            Texture2D tex = CreateEmptyTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = 58f;
            float innerR = 46f; // Thickness ~14%

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < innerR - 1.5f || d > outerR + 1.5f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float innerAlpha = Mathf.SmoothStep(0f, 1f, d - (innerR - 1.0f));
                        float outerAlpha = Mathf.SmoothStep(1f, 0f, d - (outerR - 1.0f));
                        float alpha = Mathf.Clamp01(innerAlpha * outerAlpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/icon_radial_ring.png", tex, Vector4.zero);
        }

        // =========================================================================
        // HELPERS
        // =========================================================================
        private static Texture2D CreateEmptyTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clears = new Color[size * size];
            tex.SetPixels(clears);
            return tex;
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
        {
            int j = poly.Length - 1;
            bool inside = false;
            for (int i = 0; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static void SaveSpriteAsset(string path, Texture2D tex, Vector4 border)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                if (border != Vector4.zero) importer.spriteBorder = border;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
#endif
    }
}
