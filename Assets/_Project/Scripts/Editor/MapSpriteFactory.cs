using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class MapSpriteFactory
    {
        private const string OutputDir = "Assets/_Project/UI/Sprites/Map";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Generate Map Sprites", false, 12)]
        public static void GenerateAllMapSprites()
        {
            EnsureDirectory();

            Debug.Log("[MapSpriteFactory] Generating High-Definition Procedural Minimap & Cartography Sprites...");

            CreateMinimapMask();
            CreateMinimapFrame();
            CreatePlayerBlip();
            CreateCustomWaypointPin();
            CreateZombieRadarBlip();
            CreatePoiIcons();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MapSpriteFactory] Successfully generated all Map Sprites in " + OutputDir);
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
        // 1. MINIMAP MASK (256x256, ANTI-ALIASED CIRCLE)
        // =========================================================================
        private static void CreateMinimapMask()
        {
            int size = 256;
            Texture2D tex = CreateEmptyTex(size);
            float center = size * 0.5f;
            float radius = 112f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hits = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = (x + (sx + 0.5f) * 0.5f) - center;
                            float py = (y + (sy + 0.5f) * 0.5f) - center;
                            float dist = Mathf.Sqrt(px * px + py * py);
                            if (dist <= radius) hits += 1f;
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
            SaveSpriteAsset($"{OutputDir}/minimap_mask.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 2. MINIMAP GPS BEZEL FRAME (256x256)
        // =========================================================================
        private static void CreateMinimapFrame()
        {
            int size = 256;
            Texture2D tex = CreateEmptyTex(size);
            float center = size * 0.5f;
            float innerR = 112f;
            float outerR = 126f;

            Color rimDark = new Color(0.09f, 0.12f, 0.17f, 0.98f);     // #171F2B
            Color rimLight = new Color(0.24f, 0.31f, 0.42f, 0.95f);    // #3D4F6B
            Color innerBorder = new Color(0.38f, 0.48f, 0.62f, 1.0f);  // #617A9E
            Color tickCyan = new Color(0.30f, 0.79f, 0.94f, 0.95f);    // #4CC9F0
            Color tickRed = new Color(0.95f, 0.14f, 0.24f, 1.0f);      // #F2243D (Kuzey / North)

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - center;
                    float py = y + 0.5f - center;
                    float dist = Mathf.Sqrt(px * px + py * py);

                    // A. Outer circular bezel ring
                    if (dist >= innerR - 1.5f && dist <= outerR + 1.0f)
                    {
                        float edgeFalloff = Mathf.Clamp01((outerR + 1.0f - dist) * 1.5f);
                        float innerFalloff = Mathf.Clamp01((dist - (innerR - 1.5f)) * 1.5f);
                        float alpha = Mathf.Min(edgeFalloff, innerFalloff);

                        float t = (dist - innerR) / (outerR - innerR);
                        Color col = Color.Lerp(innerBorder, Color.Lerp(rimDark, rimLight, (py + center) / size), t);

                        // Subtle cardinal tick marks (0, 90, 180, 270 deg)
                        float angle = Mathf.Atan2(py, px) * Mathf.Rad2Deg;
                        if (angle < 0f) angle += 360f;

                        bool isNorth = Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < 3.5f && dist >= innerR - 1f && dist <= outerR - 4f;
                        bool isEast = Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) < 2.5f && dist >= innerR && dist <= outerR - 6f;
                        bool isSouth = Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) < 2.5f && dist >= innerR && dist <= outerR - 6f;
                        bool isWest = Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 2.5f && dist >= innerR && dist <= outerR - 6f;

                        if (isNorth)
                        {
                            tex.SetPixel(x, y, new Color(tickRed.r, tickRed.g, tickRed.b, alpha));
                            continue;
                        }
                        if (isEast || isSouth || isWest)
                        {
                            tex.SetPixel(x, y, new Color(tickCyan.r, tickCyan.g, tickCyan.b, alpha));
                            continue;
                        }

                        // Rivet accents at 45, 135, 225, 315 deg
                        float diagAngle = Mathf.Abs(Mathf.DeltaAngle(angle, 45f));
                        float diagAngle2 = Mathf.Abs(Mathf.DeltaAngle(angle, 135f));
                        float diagAngle3 = Mathf.Abs(Mathf.DeltaAngle(angle, 225f));
                        float diagAngle4 = Mathf.Abs(Mathf.DeltaAngle(angle, 315f));
                        if ((diagAngle < 2.0f || diagAngle2 < 2.0f || diagAngle3 < 2.0f || diagAngle4 < 2.0f) &&
                            dist >= 118f && dist <= 122f)
                        {
                            col = new Color(0.75f, 0.85f, 0.95f, alpha);
                        }

                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                    }

                    // B. Bottom Pill Badge Area for "TAM HARİTA"
                    if (y >= 6 && y <= 34 && x >= 74 && x <= 182)
                    {
                        float bx = Mathf.Clamp(x, 88, 168);
                        float bdx = x - bx;
                        float bdy = y - 20;
                        float bdist = Mathf.Sqrt(bdx * bdx + bdy * bdy);

                        if (bdist <= 14f)
                        {
                            float bAlpha = Mathf.Clamp01((14f - bdist) * 1.5f);
                            bool isBBorder = bdist >= 12f;
                            Color bCol = isBBorder ? new Color(0.35f, 0.45f, 0.60f, bAlpha) : new Color(0.06f, 0.08f, 0.12f, 0.94f * bAlpha);
                            tex.SetPixel(x, y, bCol);
                        }
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/minimap_frame.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 3. DIRECTIONAL PLAYER BLIP (64x64 CHEVRON)
        // =========================================================================
        private static void CreatePlayerBlip()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);

            Vector2[] outerChevron = new Vector2[]
            {
                new Vector2(32f, 58f),
                new Vector2(54f, 10f),
                new Vector2(32f, 22f),
                new Vector2(10f, 10f)
            };

            Vector2[] innerChevron = new Vector2[]
            {
                new Vector2(32f, 52f),
                new Vector2(48f, 14f),
                new Vector2(32f, 24f),
                new Vector2(16f, 14f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hitsOuter = 0f;
                    float hitsInner = 0f;

                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2 p = new Vector2(x + (sx + 0.5f) * 0.5f, y + (sy + 0.5f) * 0.5f);
                            if (IsPointInPolygon(p, outerChevron)) hitsOuter += 1f;
                            if (IsPointInPolygon(p, innerChevron)) hitsInner += 1f;
                        }
                    }

                    if (hitsOuter > 0f)
                    {
                        float alpha = hitsOuter / 4f;
                        Color cyanGlow = new Color(0.0f, 0.85f, 1.0f, alpha);
                        Color whiteCore = new Color(1.0f, 1.0f, 1.0f, alpha);

                        Color c = Color.Lerp(cyanGlow, whiteCore, hitsInner / 4f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/player_blip.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 4. CUSTOM WAYPOINT PIN (64x64 GLOWING AMBER DIAMOND)
        // =========================================================================
        private static void CreateCustomWaypointPin()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            float cx = 32f;
            float cy = 32f;

            Color gold = new Color(1.0f, 0.72f, 0.01f); // #FFB703
            Color amber = new Color(0.97f, 0.50f, 0.0f); // #F77F00

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx);
                    float dy = Mathf.Abs(y + 0.5f - cy);
                    float diamondDist = (dx + dy) / 26f;

                    if (diamondDist <= 1.0f)
                    {
                        float a = Mathf.Clamp01((1.0f - diamondDist) * 3f);
                        Color c;
                        if (diamondDist < 0.35f)
                        {
                            c = Color.Lerp(Color.white, gold, diamondDist / 0.35f);
                        }
                        else
                        {
                            c = Color.Lerp(gold, amber, (diamondDist - 0.35f) / 0.65f);
                        }
                        c.a = a;
                        tex.SetPixel(x, y, c);
                    }
                    else if (diamondDist <= 1.25f)
                    {
                        // Outer soft aura
                        float aura = Mathf.Clamp01((1.25f - diamondDist) / 0.25f) * 0.35f;
                        tex.SetPixel(x, y, new Color(gold.r, gold.g, gold.b, aura));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/waypoint_custom_pin.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 5. ACOUSTIC ZOMBIE RADAR BLIP (32x32 PULSING CRIMSON DOT)
        // =========================================================================
        private static void CreateZombieRadarBlip()
        {
            int size = 32;
            Texture2D tex = CreateEmptyTex(size);
            float center = 16f;

            Color redCore = new Color(1.0f, 0.20f, 0.25f, 1.0f);
            Color redGlow = new Color(0.85f, 0.02f, 0.16f, 0.45f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    if (dist <= 6f)
                    {
                        float a = Mathf.Clamp01((6f - dist) * 1.5f);
                        tex.SetPixel(x, y, new Color(redCore.r, redCore.g, redCore.b, a));
                    }
                    else if (dist <= 14f)
                    {
                        float a = Mathf.Clamp01((14f - dist) / 8f) * 0.5f;
                        tex.SetPixel(x, y, new Color(redGlow.r, redGlow.g, redGlow.b, a));
                    }
                }
            }

            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/zombie_radar_blip.png", tex, Vector4.zero);
        }

        // =========================================================================
        // 6. TACTICAL POI ICONS (64x64)
        // =========================================================================
        private static void CreatePoiIcons()
        {
            CreateIconHospital();
            CreateIconPolice();
            CreateIconShelter();
            CreateIconShop();
            CreateIconGas();
            CreateIconNpc();
        }

        private static void CreateIconHospital()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color cyan = new Color(0.2f, 0.88f, 0.95f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inH = (x >= 14 && x <= 50 && y >= 25 && y <= 39);
                    bool inV = (x >= 25 && x <= 39 && y >= 14 && y <= 50);
                    if (inH || inV) tex.SetPixel(x, y, cyan);
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_hospital.png", tex, Vector4.zero);
        }

        private static void CreateIconPolice()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color blue = new Color(0.28f, 0.62f, 1.0f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Shield shape
                    float nx = Mathf.Abs(x - 32f) / 22f;
                    float ny = (y - 12f) / 44f;
                    if (nx <= 1.0f && ny >= 0f && ny <= 1.0f)
                    {
                        float curve = ny * ny * 0.6f + (1f - ny) * 0.2f;
                        if (nx <= (1.0f - curve * 0.6f))
                        {
                            tex.SetPixel(x, y, blue);
                        }
                    }
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_police.png", tex, Vector4.zero);
        }

        private static void CreateIconShelter()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color green = new Color(0.25f, 0.88f, 0.45f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Roof triangle + box
                    bool inRoof = (y >= 32 && y <= 54 && Mathf.Abs(x - 32) <= (54 - y));
                    bool inBase = (y >= 14 && y < 32 && Mathf.Abs(x - 32) <= 18);
                    bool inDoor = (y >= 14 && y <= 24 && Mathf.Abs(x - 32) <= 6);

                    if ((inRoof || inBase) && !inDoor)
                    {
                        tex.SetPixel(x, y, green);
                    }
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_shelter.png", tex, Vector4.zero);
        }

        private static void CreateIconShop()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color yellow = new Color(0.96f, 0.78f, 0.22f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Shopping basket
                    bool basket = (y >= 18 && y <= 38 && Mathf.Abs(x - 32) <= (14 + (y - 18) * 0.4f));
                    bool handle = (y >= 38 && y <= 50 && Mathf.Abs(Mathf.Abs(x - 32) - 10) <= 2);
                    if (basket || handle) tex.SetPixel(x, y, yellow);
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_shop.png", tex, Vector4.zero);
        }

        private static void CreateIconGas()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color orange = new Color(1.0f, 0.55f, 0.15f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool pumpBody = (x >= 16 && x <= 40 && y >= 12 && y <= 52);
                    bool window = (x >= 20 && x <= 36 && y >= 36 && y <= 46);
                    bool nozzle = (x >= 44 && x <= 50 && y >= 20 && y <= 42);

                    if ((pumpBody && !window) || nozzle)
                    {
                        tex.SetPixel(x, y, orange);
                    }
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_gas.png", tex, Vector4.zero);
        }

        private static void CreateIconNpc()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color purple = new Color(0.85f, 0.40f, 0.95f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Exclamation point
                    bool dot = (Vector2.Distance(new Vector2(x, y), new Vector2(32, 18)) <= 4f);
                    bool bar = (x >= 29 && x <= 35 && y >= 26 && y <= 50);
                    if (dot || bar) tex.SetPixel(x, y, purple);
                }
            }
            tex.Apply();
            SaveSpriteAsset($"{OutputDir}/poi_npc.png", tex, Vector4.zero);
        }

        // =========================================================================
        // UTILITIES
        // =========================================================================
        private static Texture2D CreateEmptyTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clear = new Color[size * size];
            tex.SetPixels(clear);
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
