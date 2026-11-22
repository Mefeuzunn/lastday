using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class WeaponSpriteFactory
    {
        private const string OutputDir = "Assets/_Project/UI/Sprites";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Generate Weapon Sprites", false, 15)]
        public static void GenerateAllWeaponSprites()
        {
            EnsureDirectory();

            Debug.Log("[WeaponSpriteFactory] Generating High-Resolution Procedural Weapon & Holster Sprites...");

            CreateUnarmedFistIcon();
            CreateHolsterIcon();
            CreateSwitchWeaponIcon();
            CreateAxeIcon();
            CreatePistolIcon();
            CreateShotgunIcon();
            CreateCrossbowIcon();
            CreateBoltIcon();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WeaponSpriteFactory] Successfully generated all Weapon Sprites in " + OutputDir);
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
        // 1. UNARMED FIST ICON (64x64)
        // =========================================================================
        private static void CreateUnarmedFistIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color c = new Color(0.95f, 0.95f, 0.98f, 1f);

            // Palm base & curled fingers
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Wrist
                    if (y >= 10 && y <= 22 && x >= 24 && x <= 40)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Palm & main fist mass
                    if (y >= 20 && y <= 46 && x >= 18 && x <= 46)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Thumb folded across
                    if (y >= 26 && y <= 38 && x >= 14 && x <= 28)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Knuckle tops
                    if (y >= 44 && y <= 52 && x >= 20 && x <= 44)
                    {
                        // 4 distinct knuckles
                        int k = (x - 20) % 6;
                        if (k < 5) tex.SetPixel(x, y, c);
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_unarmed_fist.png");
        }

        // =========================================================================
        // 2. HOLSTER ICON (64x64)
        // =========================================================================
        private static void CreateHolsterIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color c = new Color(0.95f, 0.85f, 0.35f, 1f); // Tactical amber/gold

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Belt loop top
                    if (y >= 48 && y <= 54 && x >= 22 && x <= 42)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Retention strap
                    if (y >= 40 && y <= 48 && x >= 28 && x <= 36)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Holster body (tapered pouch)
                    if (y >= 14 && y <= 42)
                    {
                        float t = (y - 14f) / 28f;
                        int minX = (int)Mathf.Lerp(25f, 18f, t);
                        int maxX = (int)Mathf.Lerp(39f, 46f, t);
                        if (x >= minX && x <= maxX)
                        {
                            tex.SetPixel(x, y, c);
                        }
                    }
                    // Snap button
                    if ((x - 32) * (x - 32) + (y - 36) * (y - 36) <= 5)
                    {
                        tex.SetPixel(x, y, new Color(0.12f, 0.14f, 0.18f, 1f));
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_holster.png");
        }

        // =========================================================================
        // 3. SWITCH / CYCLE WEAPON ICON (64x64, DOUBLE CYCLIC ARROWS)
        // =========================================================================
        private static void CreateSwitchWeaponIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color c = new Color(0.35f, 0.85f, 1.0f, 1f); // Tactical cyan
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Ring between radius 16 and 22
                    if (r >= 16f && r <= 22f)
                    {
                        // Cut gap for two arrowheads
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if ((angle > 25f && angle < 155f) || (angle < -25f && angle > -155f))
                        {
                            tex.SetPixel(x, y, c);
                        }
                    }

                    // Arrowhead 1 (top right)
                    if (x >= 40 && x <= 50 && y >= 32 && y <= 44)
                    {
                        if ((x - 40) + (y - 32) >= 6 && (x - 40) <= 6)
                            tex.SetPixel(x, y, c);
                    }

                    // Arrowhead 2 (bottom left)
                    if (x >= 14 && x <= 24 && y >= 20 && y <= 32)
                    {
                        if ((24 - x) + (32 - y) >= 6 && (24 - x) <= 6)
                            tex.SetPixel(x, y, c);
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_switch_weapon.png");
        }

        // =========================================================================
        // 4. FIRE AXE ICON (64x64)
        // =========================================================================
        private static void CreateAxeIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color handleCol = new Color(0.72f, 0.45f, 0.22f, 1f); // Wood handle
            Color bladeCol = new Color(0.92f, 0.22f, 0.22f, 1f); // Red fire axe head

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diagonal handle from bottom-left (14, 12) to top-right (48, 50)
                    float distToDiag = Mathf.Abs((x - 14) - (y - 12));
                    if (distToDiag <= 2.5f && (x + y) >= 28 && (x + y) <= 96)
                    {
                        tex.SetPixel(x, y, handleCol);
                    }

                    // Axe head near (44, 46)
                    if (x >= 36 && x <= 56 && y >= 40 && y <= 56)
                    {
                        // Pick on back, blade on front
                        if ((x - 36) + (y - 40) >= 6)
                        {
                            tex.SetPixel(x, y, bladeCol);
                        }
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_weapon_axe.png");
        }

        // =========================================================================
        // 5. 9MM PISTOL ICON (64x64)
        // =========================================================================
        private static void CreatePistolIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color c = new Color(0.9f, 0.92f, 0.96f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Slide & Barrel
                    if (y >= 32 && y <= 42 && x >= 14 && x <= 50)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Grip
                    if (y >= 14 && y <= 34 && x >= 14 && x <= 26)
                    {
                        // Slight grip slant
                        int offset = (y - 14) / 4;
                        if (x >= 14 + offset && x <= 24 + offset)
                            tex.SetPixel(x, y, c);
                    }
                    // Trigger guard
                    if (y >= 24 && y <= 32 && x >= 26 && x <= 34)
                    {
                        if (y == 24 || x == 34) tex.SetPixel(x, y, c);
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_weapon_pistol.png");
        }

        // =========================================================================
        // 6. SHOTGUN ICON (64x64)
        // =========================================================================
        private static void CreateShotgunIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color c = new Color(0.92f, 0.88f, 0.82f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Long barrel & receiver
                    if (y >= 28 && y <= 33 && x >= 10 && x <= 56)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Pump grip
                    if (y >= 26 && y <= 31 && x >= 32 && x <= 44)
                    {
                        tex.SetPixel(x, y, c);
                    }
                    // Stock
                    if (y >= 23 && y <= 30 && x >= 8 && x <= 22)
                    {
                        int t = x - 8;
                        if (y >= 23 - (14 - t) / 3 && y <= 30)
                            tex.SetPixel(x, y, c);
                    }
                }
            }

            SaveAndImportSprite(tex, "icon_weapon_shotgun.png");
        }

        // =========================================================================
        // 7. TACTICAL CROSSBOW ICON (64x64)
        // =========================================================================
        private static void CreateCrossbowIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color polymer = new Color(0.85f, 0.88f, 0.92f, 1f);
            Color prod = new Color(0.35f, 0.75f, 0.95f, 1f);
            Color str = new Color(1.0f, 0.95f, 0.6f, 0.9f);

            // Stock / rail body
            for (int y = 28; y <= 34; y++)
            {
                for (int x = 12; x <= 52; x++)
                {
                    tex.SetPixel(x, y, polymer);
                }
            }
            // Grip & Stock butt
            for (int y = 14; y <= 30; y++)
            {
                for (int x = 14; x <= 22; x++)
                {
                    tex.SetPixel(x, y, polymer);
                }
            }
            // Curved Limbs (Prod)
            for (int y = 10; y <= 54; y++)
            {
                int xLimb = 48 - (int)(Mathf.Abs(y - 32) * 0.45f);
                for (int dx = 0; dx <= 2; dx++)
                {
                    tex.SetPixel(xLimb + dx, y, prod);
                }
            }
            // Bowstring taut to receiver (at x = 34)
            for (int y = 12; y <= 52; y++)
            {
                float t = Mathf.Abs(y - 32) / 20f;
                int sx = Mathf.RoundToInt(Mathf.Lerp(34, 42, t));
                tex.SetPixel(sx, y, str);
            }

            SaveAndImportSprite(tex, "icon_weapon_crossbow.png");
        }

        // =========================================================================
        // 8. CROSSBOW BOLT ICON (64x64)
        // =========================================================================
        private static void CreateBoltIcon()
        {
            int size = 64;
            Texture2D tex = CreateEmptyTex(size);
            Color shaft = new Color(0.80f, 0.82f, 0.85f, 1f);
            Color tip = new Color(1.0f, 0.35f, 0.25f, 1f);
            Color fletch = new Color(0.20f, 0.85f, 0.95f, 1f);

            // Shaft (diagonal from (14, 14) to (48, 48))
            for (int i = 0; i <= 36; i++)
            {
                int x = 14 + i;
                int y = 14 + i;
                tex.SetPixel(x, y, shaft);
                tex.SetPixel(x + 1, y, shaft);
            }
            // Broadhead Tip (at top right 49..55)
            for (int i = 0; i <= 6; i++)
            {
                int bx = 49 + i;
                int by = 49 + i;
                for (int w = -2; w <= 2; w++)
                {
                    tex.SetPixel(bx + w, by - w, tip);
                }
            }
            // Fletching Vanes (at bottom left 12..20)
            for (int i = 0; i <= 6; i++)
            {
                int fx = 14 + i;
                int fy = 14 + i;
                tex.SetPixel(fx - 3, fy + 3, fletch);
                tex.SetPixel(fx - 4, fy + 4, fletch);
                tex.SetPixel(fx + 3, fy - 3, fletch);
                tex.SetPixel(fx + 4, fy - 4, fletch);
            }

            SaveAndImportSprite(tex, "icon_ammo_crossbow_bolt.png");
        }

        // =========================================================================
        // HELPERS
        // =========================================================================
        private static Texture2D CreateEmptyTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);
            return tex;
        }

        private static void SaveAndImportSprite(Texture2D tex, string fileName)
        {
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(OutputDir, fileName);
            File.WriteAllBytes(fullPath, bytes);
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
#endif
    }
}
