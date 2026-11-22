using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectSurvival.EditorTools
{
    public static class AssetGenerator
    {
        private const string SpritesDir = "Assets/_Project/UI/Sprites";
        private const string TexturesDir = "Assets/_Project/Textures";

        [MenuItem("ProjectSurvival/Generate UI & PBR Assets", false, 10)]
        public static void GenerateAllAssets()
        {
            EnsureDirectories();

            Debug.Log("[AssetGenerator] Generating procedural UI Sprites and PBR Textures...");

            // 1. UI Sprites
            CreateGlassPanelSprite();
            CreateCircleButtonSprites();
            CreateRadialFillSprite();
            CreateAllUISymbolIcons();
            CreateHitMarkerSprite();

            // 2. PBR Textures
            CreatePBRAsphaltTextures();
            CreatePBRConcreteTextures();
            CreatePBRBrickTextures();
            CreatePBRWoodTextures();
            CreateParticleTextures();

            // 3. Phase 5 Atmospheric Textures
            CreateWeatheredGlassTextures();
            CreateIvyLeafTexture();
            CreateCobwebTexture();
            CreateStorefrontWoodTextures();
            CreateDecalTextures();

            // 4. Step 2 Tactical Vital HUD Sprites
            VitalHUDSpriteFactory.GenerateAllVitalSprites();

            // 5. Step 3 Tactical Minimap & Cartography
            MapSpriteFactory.GenerateAllMapSprites();
            CityCartographyBaker.BakeCityMap();

            // 6. Step 1 Weapon Holster, Switch & Fist Sprites
            WeaponSpriteFactory.GenerateAllWeaponSprites();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AssetGenerator] Successfully generated all UI & PBR Assets!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SpritesDir)) Directory.CreateDirectory(SpritesDir);
            if (!Directory.Exists(TexturesDir)) Directory.CreateDirectory(TexturesDir);
        }

        // ==========================================
        // 1. UI GLASSMORPHISM & BUTTON SPRITES
        // ==========================================

        private static void CreateGlassPanelSprite()
        {
            int size = 128;
            int radius = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color innerColor = new Color(0.08f, 0.10f, 0.14f, 0.88f);
            Color borderColor = new Color(0.32f, 0.40f, 0.52f, 0.70f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - 2.5f)
                    {
                        float t = (dist - (radius - 2.5f)) / 2.5f;
                        tex.SetPixel(x, y, Color.Lerp(borderColor, Color.clear, t * 0.4f));
                    }
                    else if (dist > radius - 4.5f)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        // Slight vertical gradient for glass realism
                        float grad = (float)y / size * 0.05f;
                        Color c = new Color(innerColor.r + grad, innerColor.g + grad, innerColor.b + grad, innerColor.a);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();

            string path = $"{SpritesDir}/panel_glass_9slice.png";
            SaveAndConfigureSprite(tex, path, new Vector4(26, 26, 26, 26));
        }

        private static void CreateCircleButtonSprites()
        {
            int size = 128;
            Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float outerR = 62f;
            float borderR = 57f;

            // 1. Default Circle Ring
            SaveCircleSprite($"{SpritesDir}/button_circle_ring.png", size, center, outerR, borderR,
                new Color(0.10f, 0.13f, 0.18f, 0.90f),
                new Color(0.38f, 0.48f, 0.62f, 0.85f));

            // 2. Cyan Glow (Active Crouch / Highlights)
            SaveCircleSprite($"{SpritesDir}/button_circle_glow.png", size, center, outerR, borderR,
                new Color(0.08f, 0.22f, 0.35f, 0.95f),
                new Color(0.20f, 0.80f, 1.00f, 1.00f));

            // 3. Red Crimson (Takedown / Critical)
            SaveCircleSprite($"{SpritesDir}/button_circle_red.png", size, center, outerR, borderR,
                new Color(0.30f, 0.06f, 0.06f, 0.95f),
                new Color(0.95f, 0.20f, 0.20f, 1.00f));

            // 4. Amber Gold (Sprint / Push)
            SaveCircleSprite($"{SpritesDir}/button_circle_amber.png", size, center, outerR, borderR,
                new Color(0.28f, 0.18f, 0.05f, 0.95f),
                new Color(1.00f, 0.70f, 0.15f, 1.00f));
        }

        private static void SaveCircleSprite(string path, int size, Vector2 center, float outerR, float borderR, Color innerColor, Color borderColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > outerR)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > borderR)
                    {
                        float alpha = Mathf.Clamp01((outerR - dist) / 2.0f);
                        Color c = borderColor;
                        c.a *= alpha;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        // Subtle center bevel
                        float rim = Mathf.Clamp01((borderR - dist) / 6f);
                        Color c = Color.Lerp(borderColor * 0.5f, innerColor, rim);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            SaveAndConfigureSprite(tex, path, Vector4.zero);
        }

        private static void CreateRadialFillSprite()
        {
            int size = 128;
            Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float outerR = 62f;
            float innerR = 52f;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= outerR && dist >= innerR)
                    {
                        float edgeAlpha = 1f;
                        if (dist > outerR - 1.5f) edgeAlpha = (outerR - dist) / 1.5f;
                        else if (dist < innerR + 1.5f) edgeAlpha = (dist - innerR) / 1.5f;

                        tex.SetPixel(x, y, new Color(1f, 0.85f, 0.25f, edgeAlpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/ring_radial_fill.png", Vector4.zero);
        }

        // ==========================================
        // 2. VECTOR STYLE UI ICONS
        // ==========================================

        private static void CreateAllUISymbolIcons()
        {
            CreateIconSprint();
            CreateIconCrouch();
            CreateIconInteract();
            CreateIconFlashlight();
            CreateIconBackpack();
            CreateIconCraft();
            CreateIconPush();
            CreateIconPause();

            // Status Bar Icons
            CreateIconHeart();
            CreateIconStamina();
            CreateIconHunger();
            CreateIconThirst();
            CreateIconInfection();
            CreateThreatArcSprite();
            CreateIconDodge();
            CreateCompassSprites();

            // Item Icons (Crisp White #FFFFFF)
            CreateIconCannedFood();
            CreateIconWaterBottle();
            CreateIconMedkit();
            CreateIconDistractionBottle();
        }

        private static void CreateIconSprint()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Head
            DrawFilledCircle(tex, new Vector2(74, 94), 10, Color.white);
            // Torso angled
            DrawCapsuleSegment(tex, new Vector2(70, 84), new Vector2(56, 52), 8, Color.white);
            // Front leg
            DrawCapsuleSegment(tex, new Vector2(56, 52), new Vector2(78, 42), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(78, 42), new Vector2(90, 20), 5, Color.white);
            // Back leg
            DrawCapsuleSegment(tex, new Vector2(56, 52), new Vector2(40, 36), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(40, 36), new Vector2(28, 46), 5, Color.white);
            // Arms
            DrawCapsuleSegment(tex, new Vector2(68, 76), new Vector2(90, 68), 5, Color.white);
            DrawCapsuleSegment(tex, new Vector2(68, 76), new Vector2(48, 62), 5, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_sprint.png", Vector4.zero);
        }

        private static void CreateIconCrouch()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Head lower
            DrawFilledCircle(tex, new Vector2(64, 76), 10, Color.white);
            // Torso tucked
            DrawCapsuleSegment(tex, new Vector2(64, 68), new Vector2(54, 46), 8, Color.white);
            // Thighs
            DrawCapsuleSegment(tex, new Vector2(54, 46), new Vector2(76, 42), 7, Color.white);
            // Shins
            DrawCapsuleSegment(tex, new Vector2(76, 42), new Vector2(70, 22), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(54, 46), new Vector2(42, 22), 6, Color.white);
            // Arm on knee
            DrawCapsuleSegment(tex, new Vector2(62, 60), new Vector2(72, 44), 5, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_crouch.png", Vector4.zero);
        }

        private static void CreateIconInteract()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Palm Base
            DrawCapsuleSegment(tex, new Vector2(64, 30), new Vector2(64, 55), 18, Color.white);
            // 4 Fingers
            DrawCapsuleSegment(tex, new Vector2(48, 55), new Vector2(46, 85), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(58, 55), new Vector2(58, 98), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(68, 55), new Vector2(70, 94), 6, Color.white);
            DrawCapsuleSegment(tex, new Vector2(78, 55), new Vector2(80, 82), 6, Color.white);
            // Thumb
            DrawCapsuleSegment(tex, new Vector2(46, 40), new Vector2(30, 56), 6, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_interact.png", Vector4.zero);
        }

        private static void CreateIconFlashlight()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Flashlight body
            DrawCapsuleSegment(tex, new Vector2(36, 46), new Vector2(66, 68), 9, Color.white);
            // Flashlight head
            DrawCapsuleSegment(tex, new Vector2(66, 68), new Vector2(76, 75), 15, Color.white);
            // Light rays
            DrawCapsuleSegment(tex, new Vector2(84, 82), new Vector2(104, 98), 3, new Color(1f, 1f, 0.4f, 0.9f));
            DrawCapsuleSegment(tex, new Vector2(86, 75), new Vector2(110, 75), 4, new Color(1f, 1f, 0.4f, 0.95f));
            DrawCapsuleSegment(tex, new Vector2(84, 68), new Vector2(104, 52), 3, new Color(1f, 1f, 0.4f, 0.9f));

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_flashlight.png", Vector4.zero);
        }

        private static void CreateIconBackpack()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Main Bag body
            DrawCapsuleSegment(tex, new Vector2(40, 32), new Vector2(88, 32), 16, Color.white);
            DrawCapsuleSegment(tex, new Vector2(40, 60), new Vector2(88, 60), 18, Color.white);
            for (int y = 30; y <= 75; y++)
            {
                for (int x = 36; x <= 92; x++) tex.SetPixel(x, y, Color.white);
            }
            // Flap
            for (int y = 68; y <= 86; y++)
            {
                for (int x = 40; x <= 88; x++) tex.SetPixel(x, y, new Color(0.9f, 0.9f, 0.95f));
            }
            // Straps
            for (int y = 30; y <= 86; y++)
            {
                tex.SetPixel(50, y, new Color(0.2f, 0.25f, 0.35f));
                tex.SetPixel(51, y, new Color(0.2f, 0.25f, 0.35f));
                tex.SetPixel(77, y, new Color(0.2f, 0.25f, 0.35f));
                tex.SetPixel(78, y, new Color(0.2f, 0.25f, 0.35f));
            }
            // Handle loop
            DrawCapsuleSegment(tex, new Vector2(54, 88), new Vector2(74, 88), 4, Color.white);
            DrawCapsuleSegment(tex, new Vector2(54, 88), new Vector2(64, 98), 4, Color.white);
            DrawCapsuleSegment(tex, new Vector2(74, 88), new Vector2(64, 98), 4, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_backpack.png", Vector4.zero);
        }

        private static void CreateIconCraft()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Crossed Wrench & Hammer
            DrawCapsuleSegment(tex, new Vector2(32, 32), new Vector2(96, 96), 6, Color.white);
            DrawFilledCircle(tex, new Vector2(96, 96), 14, Color.white);
            DrawFilledCircle(tex, new Vector2(96, 96), 6, Color.clear);

            DrawCapsuleSegment(tex, new Vector2(96, 32), new Vector2(32, 96), 6, Color.white);
            // Hammer head
            for (int y = 84; y <= 104; y++)
            {
                for (int x = 20; x <= 46; x++) tex.SetPixel(x, y, Color.white);
            }

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_craft.png", Vector4.zero);
        }

        private static void CreateIconPush()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            // Punch Fist / Impact Hand
            DrawFilledCircle(tex, new Vector2(56, 64), 22, Color.white);
            for (int y = 50; y <= 78; y++)
            {
                for (int x = 40; x <= 80; x++) tex.SetPixel(x, y, Color.white);
            }
            DrawCapsuleSegment(tex, new Vector2(30, 64), new Vector2(50, 64), 14, Color.white);

            // Impact shock lines
            DrawCapsuleSegment(tex, new Vector2(88, 64), new Vector2(108, 64), 5, new Color(1f, 0.8f, 0.2f));
            DrawCapsuleSegment(tex, new Vector2(84, 82), new Vector2(102, 94), 4, new Color(1f, 0.8f, 0.2f));
            DrawCapsuleSegment(tex, new Vector2(84, 46), new Vector2(102, 34), 4, new Color(1f, 0.8f, 0.2f));

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_push.png", Vector4.zero);
        }

        private static void CreateIconPause()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            for (int y = 34; y <= 94; y++)
            {
                for (int x = 42; x <= 56; x++) tex.SetPixel(x, y, Color.white);
                for (int x = 72; x <= 86; x++) tex.SetPixel(x, y, Color.white);
            }

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_pause.png", Vector4.zero);
        }

        private static void CreateIconHeart()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Vector2 c1 = new Vector2(23, 38);
            Vector2 c2 = new Vector2(41, 38);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d1 = Vector2.Distance(new Vector2(x, y), c1);
                    float d2 = Vector2.Distance(new Vector2(x, y), c2);
                    bool inTop = (d1 <= 12f) || (d2 <= 12f);
                    bool inBottom = (y <= 38 && y >= 14 && Mathf.Abs(x - 32) <= (y - 14) * 0.9f);

                    if (inTop || inBottom) tex.SetPixel(x, y, new Color(0.95f, 0.2f, 0.25f));
                }
            }
            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_hp.png", Vector4.zero);
        }

        private static void CreateIconStamina()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Color col = new Color(0.25f, 0.9f, 0.35f);

            DrawCapsuleSegment(tex, new Vector2(36, 54), new Vector2(24, 32), 4, col);
            DrawCapsuleSegment(tex, new Vector2(24, 32), new Vector2(36, 32), 4, col);
            DrawCapsuleSegment(tex, new Vector2(36, 32), new Vector2(26, 10), 4, col);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_stamina.png", Vector4.zero);
        }

        private static void CreateIconHunger()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Color col = new Color(1.0f, 0.65f, 0.15f);

            DrawFilledCircle(tex, new Vector2(38, 40), 12, col);
            DrawCapsuleSegment(tex, new Vector2(34, 36), new Vector2(20, 18), 5, col);
            DrawFilledCircle(tex, new Vector2(18, 16), 4, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_hunger.png", Vector4.zero);
        }

        private static void CreateIconThirst()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Color col = new Color(0.2f, 0.75f, 1.0f);

            for (int y = 12; y <= 52; y++)
            {
                for (int x = 12; x <= 52; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 26));
                    bool inCircle = dist <= 14f;
                    bool inCone = (y >= 26 && y <= 50 && Mathf.Abs(x - 32) <= (50 - y) * 0.58f);

                    if (inCircle || inCone) tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_thirst.png", Vector4.zero);
        }

        private static void CreateIconInfection()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Color col = new Color(0.75f, 0.25f, 0.95f);

            DrawFilledCircle(tex, new Vector2(32, 38), 7, col);
            DrawFilledCircle(tex, new Vector2(22, 24), 7, col);
            DrawFilledCircle(tex, new Vector2(42, 24), 7, col);
            DrawFilledCircle(tex, new Vector2(32, 28), 4, Color.clear);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_infection.png", Vector4.zero);
        }

        private static void CreateThreatArcSprite()
        {
            int size = 256;
            Texture2D tex = CreateEmptyIconTex(size);
            Vector2 center = new Vector2(128, 128);
            float innerR = 84f;
            float outerR = 106f;
            float maxAngleDeg = 42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float d = Vector2.Distance(pos, center);
                    if (d >= innerR && d <= outerR)
                    {
                        float angle = Vector2.SignedAngle(Vector2.up, pos - center);
                        float absAngle = Mathf.Abs(angle);
                        if (absAngle <= maxAngleDeg)
                        {
                            float angularFade = 1f - Mathf.Pow(absAngle / maxAngleDeg, 1.6f);
                            float midR = (innerR + outerR) * 0.5f;
                            float halfThick = (outerR - innerR) * 0.5f;
                            float radialDist = Mathf.Abs(d - midR) / halfThick;
                            float radialFade = 1f - Mathf.Pow(radialDist, 2f);

                            float alpha = Mathf.Clamp01(angularFade * radialFade);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }
                }
            }
            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/threat_arc.png", Vector4.zero);
        }

        private static void CreateIconDodge()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);

            DrawCapsuleSegment(tex, new Vector2(34, 82), new Vector2(56, 64), 5, new Color(1f, 1f, 1f, 0.45f));
            DrawCapsuleSegment(tex, new Vector2(34, 46), new Vector2(56, 64), 5, new Color(1f, 1f, 1f, 0.45f));

            DrawCapsuleSegment(tex, new Vector2(62, 88), new Vector2(88, 64), 7, Color.white);
            DrawCapsuleSegment(tex, new Vector2(62, 40), new Vector2(88, 64), 7, Color.white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_dodge.png", Vector4.zero);
        }

        private static void CreateCompassSprites()
        {
            int size = 64;
            Texture2D wpTex = CreateEmptyIconTex(size);
            Vector2 c = new Vector2(32, 32);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float manhattan = Mathf.Abs(x - c.x) + Mathf.Abs(y - c.y);
                    if (manhattan <= 14f)
                    {
                        float alpha = 1f - (manhattan / 14f) * 0.35f;
                        wpTex.SetPixel(x, y, new Color(1f, 0.85f, 0.25f, alpha));
                    }
                }
            }
            wpTex.Apply();
            SaveAndConfigureSprite(wpTex, $"{SpritesDir}/compass_waypoint.png", Vector4.zero);
        }

        private static void CreateIconCannedFood()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);
            Color white = Color.white;

            for (int y = 30; y <= 86; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            for (int y = 76; y <= 98; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    float dx = (x - 64f) / 28f;
                    float dy = (y - 86f) / 10f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            for (int y = 20; y <= 40; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    float dx = (x - 64f) / 28f;
                    float dy = (y - 30f) / 10f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            for (int x = 40; x <= 88; x++)
            {
                tex.SetPixel(x, 48, new Color(0.15f, 0.18f, 0.22f));
                tex.SetPixel(x, 68, new Color(0.15f, 0.18f, 0.22f));
            }

            DrawCapsuleSegment(tex, new Vector2(58, 96), new Vector2(70, 104), 4, white);
            DrawFilledCircle(tex, new Vector2(72, 105), 5, white);

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/item_canned_food.png", Vector4.zero);
        }

        private static void CreateIconWaterBottle()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);
            Color white = Color.white;

            for (int y = 22; y <= 74; y++)
            {
                for (int x = 44; x <= 84; x++)
                {
                    if (y < 28 && (x < 48 || x > 80))
                    {
                        float dx = (x < 48) ? (x - 48) : (x - 80);
                        float dy = y - 28;
                        if (dx * dx + dy * dy > 36f) continue;
                    }
                    tex.SetPixel(x, y, white);
                }
            }

            for (int y = 74; y <= 92; y++)
            {
                float t = (y - 74f) / 18f;
                int halfW = Mathf.RoundToInt(Mathf.Lerp(20f, 10f, t));
                for (int x = 64 - halfW; x <= 64 + halfW; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            for (int y = 92; y <= 106; y++)
            {
                for (int x = 52; x <= 76; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            for (int x = 48; x <= 80; x++)
            {
                int waveY = 50 + Mathf.RoundToInt(Mathf.Sin((x - 48) * 0.35f) * 2f);
                tex.SetPixel(x, waveY, new Color(0.12f, 0.16f, 0.22f));
                tex.SetPixel(x, waveY + 1, new Color(0.12f, 0.16f, 0.22f));
            }

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/item_water_bottle.png", Vector4.zero);
        }

        private static void CreateIconMedkit()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);
            Color white = Color.white;

            for (int y = 24; y <= 86; y++)
            {
                for (int x = 26; x <= 102; x++)
                {
                    bool cBL = (x < 36 && y < 34 && (x - 36) * (x - 36) + (y - 34) * (y - 34) > 100);
                    bool cBR = (x > 92 && y < 34 && (x - 92) * (x - 92) + (y - 34) * (y - 34) > 100);
                    bool cTL = (x < 36 && y > 76 && (x - 36) * (x - 36) + (y - 76) * (y - 76) > 100);
                    bool cTR = (x > 92 && y > 76 && (x - 92) * (x - 92) + (y - 76) * (y - 76) > 100);
                    if (!cBL && !cBR && !cTL && !cTR)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            DrawCapsuleSegment(tex, new Vector2(48, 86), new Vector2(48, 98), 5, white);
            DrawCapsuleSegment(tex, new Vector2(80, 86), new Vector2(80, 98), 5, white);
            DrawCapsuleSegment(tex, new Vector2(48, 98), new Vector2(80, 98), 5, white);

            Color darkCut = new Color(0.12f, 0.15f, 0.20f);
            for (int y = 38; y <= 72; y++)
            {
                for (int x = 58; x <= 70; x++)
                {
                    tex.SetPixel(x, y, darkCut);
                }
            }
            for (int y = 49; y <= 61; y++)
            {
                for (int x = 46; x <= 82; x++)
                {
                    tex.SetPixel(x, y, darkCut);
                }
            }

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/item_medkit.png", Vector4.zero);
        }

        private static void CreateIconDistractionBottle()
        {
            int size = 128;
            Texture2D tex = CreateEmptyIconTex(size);
            Color white = Color.white;

            for (int y = 24; y <= 74; y++)
            {
                for (int x = 32; x <= 68; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            for (int y = 74; y <= 96; y++)
            {
                for (int x = 44; x <= 56; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }
            DrawCapsuleSegment(tex, new Vector2(40, 96), new Vector2(60, 96), 4, white);

            for (int a = -45; a <= 45; a += 3)
            {
                float rad = a * Mathf.Deg2Rad;
                int wx = Mathf.RoundToInt(60 + Mathf.Cos(rad) * 22f);
                int wy = Mathf.RoundToInt(55 + Mathf.Sin(rad) * 22f);
                DrawFilledCircle(tex, new Vector2(wx, wy), 2, white);
            }
            for (int a = -45; a <= 45; a += 3)
            {
                float rad = a * Mathf.Deg2Rad;
                int wx = Mathf.RoundToInt(60 + Mathf.Cos(rad) * 36f);
                int wy = Mathf.RoundToInt(55 + Mathf.Sin(rad) * 36f);
                DrawFilledCircle(tex, new Vector2(wx, wy), 3, white);
            }

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/item_distraction_bottle.png", Vector4.zero);
        }

        // ==========================================
        // 3. PBR PROCEDURAL TEXTURES (Albedo + Normal)
        // ==========================================

        private static void CreatePBRAsphaltTextures()
        {
            int size = 256;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color darkAsphaltBase = new Color(0.137f, 0.153f, 0.180f); // #23272E

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Fine granular gritty asphalt noise
                    float noise = (Mathf.PerlinNoise(x * 0.25f, y * 0.25f) - 0.5f) * 0.035f;
                    float speckle = ((float)(new System.Random(x * 397 + y).NextDouble()) - 0.5f) * 0.02f;
                    float tone = noise + speckle;

                    Color c = new Color(
                        Mathf.Clamp01(darkAsphaltBase.r + tone),
                        Mathf.Clamp01(darkAsphaltBase.g + tone),
                        Mathf.Clamp01(darkAsphaltBase.b + tone * 1.1f)
                    );

                    albedo.SetPixel(x, y, c);

                    // Normal bump
                    Vector3 n = new Vector3(
                        (Mathf.PerlinNoise((x + 1) * 0.35f, y * 0.35f) - Mathf.PerlinNoise(x * 0.35f, y * 0.35f)) * 4f,
                        (Mathf.PerlinNoise(x * 0.35f, (y + 1) * 0.35f) - Mathf.PerlinNoise(x * 0.35f, y * 0.35f)) * 4f,
                        1f
                    ).normalized;
                    normal.SetPixel(x, y, EncodeNormal(n));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveTexture($"{TexturesDir}/Tex_Asphalt_Albedo.png", albedo, false);
            SaveTexture($"{TexturesDir}/Tex_Asphalt_Normal.png", normal, true);
        }

        private static void CreateParticleTextures()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.48f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist >= radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float norm = dist / radius;
                        float alpha = Mathf.SmoothStep(1f, 0f, norm);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            tex.Apply();
            SaveTexture($"{TexturesDir}/tex_particle_soft_circle.png", tex, false);
        }

        private static void CreatePBRConcreteTextures()
        {
            int size = 256;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f;
                    float speck = (Mathf.Sin(x * 7f) * Mathf.Cos(y * 7f)) * 0.03f;
                    float baseTone = 0.32f + noise + speck;

                    // Tile Seam groove every 64 pixels
                    bool isSeam = (x % 64 <= 1 || y % 64 <= 1);
                    float groove = isSeam ? 0.6f : 1.0f;

                    Color c = new Color(baseTone * groove, baseTone * groove, (baseTone + 0.01f) * groove);
                    albedo.SetPixel(x, y, c);

                    float bumpX = isSeam ? (x % 64 == 0 ? -0.4f : 0.4f) : 0f;
                    float bumpY = isSeam ? (y % 64 == 0 ? -0.4f : 0.4f) : 0f;
                    Vector3 n = new Vector3(bumpX, bumpY, 1f).normalized;
                    normal.SetPixel(x, y, EncodeNormal(n));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveTexture($"{TexturesDir}/Tex_Concrete_Albedo.png", albedo, false);
            SaveTexture($"{TexturesDir}/Tex_Concrete_Normal.png", normal, true);
        }

        private static void CreatePBRBrickTextures()
        {
            int size = 256;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int brickH = 32;
            int brickW = 64;

            for (int y = 0; y < size; y++)
            {
                int row = y / brickH;
                int offsetX = (row % 2 == 1) ? brickW / 2 : 0;

                for (int x = 0; x < size; x++)
                {
                    int xMod = (x + offsetX) % brickW;
                    int yMod = y % brickH;

                    bool isMortar = (xMod <= 2 || yMod <= 2);

                    if (isMortar)
                    {
                        albedo.SetPixel(x, y, new Color(0.22f, 0.23f, 0.25f));
                        normal.SetPixel(x, y, EncodeNormal(new Vector3(0, 0, 0.6f)));
                    }
                    else
                    {
                        float var = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.1f;
                        Color brickCol = new Color(0.38f + var, 0.32f + var, 0.30f + var);
                        albedo.SetPixel(x, y, brickCol);

                        float nx = (xMod <= 4 ? 0.3f : (xMod >= brickW - 4 ? -0.3f : 0f));
                        float ny = (yMod <= 4 ? 0.3f : (yMod >= brickH - 4 ? -0.3f : 0f));
                        normal.SetPixel(x, y, EncodeNormal(new Vector3(nx, ny, 1f).normalized));
                    }
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveTexture($"{TexturesDir}/Tex_Brick_Albedo.png", albedo, false);
            SaveTexture($"{TexturesDir}/Tex_Brick_Normal.png", normal, true);
        }

        private static void CreatePBRWoodTextures()
        {
            int size = 256;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int plankH = 42;

            for (int y = 0; y < size; y++)
            {
                int yMod = y % plankH;
                bool isPlankSeam = (yMod <= 2);

                for (int x = 0; x < size; x++)
                {
                    if (isPlankSeam)
                    {
                        albedo.SetPixel(x, y, new Color(0.12f, 0.08f, 0.05f));
                        normal.SetPixel(x, y, EncodeNormal(new Vector3(0, -0.5f, 0.8f)));
                    }
                    else
                    {
                        float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.04f) * 0.18f;
                        Color c = new Color(0.42f + grain, 0.28f + grain * 0.7f, 0.15f + grain * 0.4f);
                        albedo.SetPixel(x, y, c);

                        float ny = (yMod <= 5 ? 0.3f : (yMod >= plankH - 5 ? -0.3f : 0f));
                        normal.SetPixel(x, y, EncodeNormal(new Vector3(0, ny, 1f).normalized));
                    }
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveTexture($"{TexturesDir}/Tex_Wood_Albedo.png", albedo, false);
            SaveTexture($"{TexturesDir}/Tex_Wood_Normal.png", normal, true);
        }

        // ==========================================
        // PHASE 5 ATMOSPHERIC PBR & DECAL GENERATORS
        // ==========================================

        private static void CreateWeatheredGlassTextures()
        {
            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D maskMap = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGBA32, true);

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 fractureEpicenter = new Vector2(size * 0.38f, size * 0.62f);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;

                    // 1. Edge-accumulated dust (distance from borders)
                    float edgeDistX = Mathf.Min(u, 1f - u) * 2f; // 0 at edges, 1 at center
                    float edgeDistY = Mathf.Min(v, 1f - v) * 2f;
                    float edgeFactor = 1f - Mathf.Clamp01(Mathf.Min(edgeDistX, edgeDistY) * 2.2f);
                    float edgeNoise = Mathf.PerlinNoise(u * 12f, v * 12f) * 0.4f;
                    float dustStrength = Mathf.Clamp01(edgeFactor + edgeNoise * edgeFactor);

                    // 2. Vertical rain streaks
                    float streakU = Mathf.Sin(u * 48f + Mathf.PerlinNoise(u * 4f, v * 0.8f) * 6f);
                    float streakNoise = Mathf.PerlinNoise(u * 8f, v * 32f);
                    float rainStreak = (streakU > 0.75f && streakNoise > 0.45f) ? (streakNoise * 0.65f) : 0f;

                    // 3. Oxidized grime spots
                    float grimePerlin = Mathf.PerlinNoise(u * 7f + 2.3f, v * 7f + 1.8f);
                    float grime = grimePerlin > 0.62f ? (grimePerlin - 0.62f) * 1.8f : 0f;

                    float totalDirt = Mathf.Clamp01(dustStrength * 0.8f + rainStreak * 0.5f + grime * 0.7f);

                    // Albedo: Tinted dark glass (#141B22) blending into dusty grime (#3D372E)
                    Color glassTint = new Color(0.08f, 0.11f, 0.14f, 0.55f);
                    Color dirtTint = new Color(0.24f, 0.22f, 0.18f, 0.95f);
                    Color alb = Color.Lerp(glassTint, dirtTint, totalDirt);
                    albedo.SetPixel(x, y, alb);

                    // MaskMap (URP standard: R=Metallic, G=Occlusion, B=Detail, A=Smoothness)
                    float metallic = 0.04f;
                    float occlusion = Mathf.Lerp(1.0f, 0.7f, dustStrength);
                    // Central clear glass is glossy (0.65), dusty grime edges are matte (0.15)
                    float smoothness = Mathf.Lerp(0.65f, 0.15f, totalDirt);
                    maskMap.SetPixel(x, y, new Color(metallic, occlusion, 0f, smoothness));

                    // NormalMap: Surface pitting + spiderweb fracture
                    float pittingX = (Mathf.PerlinNoise(u * 64f, v * 64f) - 0.5f) * 0.12f;
                    float pittingY = (Mathf.PerlinNoise(u * 64f + 3.1f, v * 64f + 5.7f) - 0.5f) * 0.12f;

                    // Spiderweb fracture lines around epicenter
                    Vector2 toEpi = new Vector2(x, y) - fractureEpicenter;
                    float distEpi = toEpi.magnitude;
                    float angleEpi = Mathf.Atan2(toEpi.y, toEpi.x) * Mathf.Rad2Deg;
                    if (angleEpi < 0) angleEpi += 360f;

                    float crackVal = 0f;
                    if (distEpi < 160f)
                    {
                        // Radial spoke cracks
                        float spokeMod = angleEpi % 22.5f;
                        if (spokeMod < 1.2f || spokeMod > 21.3f)
                        {
                            crackVal = Mathf.Clamp01(1f - (distEpi / 160f));
                        }
                        // Concentric ring cracks
                        float ringMod = distEpi % 24f;
                        if (ringMod < 1.4f)
                        {
                            crackVal = Mathf.Max(crackVal, Mathf.Clamp01(1f - (distEpi / 160f)));
                        }
                    }

                    Vector3 normalVec = new Vector3(pittingX + crackVal * 0.45f, pittingY + crackVal * 0.45f, 1f).normalized;
                    normalMap.SetPixel(x, y, EncodeNormal(normalVec));
                }
            }

            albedo.Apply(true);
            maskMap.Apply(true);
            normalMap.Apply(true);

            SaveTexture($"{TexturesDir}/Tex_WeatheredGlass_Albedo.png", albedo, false, FilterMode.Trilinear, true);
            SaveTexture($"{TexturesDir}/Tex_WeatheredGlass_MaskMap.png", maskMap, false, FilterMode.Trilinear, true);
            SaveTexture($"{TexturesDir}/Tex_WeatheredGlass_Normal.png", normalMap, true, FilterMode.Trilinear, true);
        }

        private static void CreateIvyLeafTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Vector2 c = new Vector2(size * 0.5f, size * 0.45f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - c;
                    float dist = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);

                    // 3-lobed ivy leaf mathematical shape
                    float leafShape = (size * 0.38f) * (1f + 0.32f * Mathf.Cos(3f * angle) - 0.25f * Mathf.Sin(angle));
                    
                    if (dist > leafShape || y < 10)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float normDist = dist / leafShape;
                        // Color variation: forest green #2D4A22 center to autumn olive #5C5327 tips
                        Color deepGreen = new Color(0.176f, 0.290f, 0.133f, 1f);
                        Color oliveGreen = new Color(0.360f, 0.325f, 0.153f, 1f);
                        Color veinColor = new Color(0.43f, 0.54f, 0.24f, 1f);

                        // Central vein
                        bool isVein = Mathf.Abs(p.x) < (1.5f + (y * 0.02f));
                        Color col = isVein ? veinColor : Color.Lerp(deepGreen, oliveGreen, normDist * 0.8f);
                        tex.SetPixel(x, y, col);
                    }
                }
            }

            tex.Apply(true);
            SaveTexture($"{TexturesDir}/Tex_Ivy_Leaf_Albedo.png", tex, false, FilterMode.Bilinear, true);
        }

        private static void CreateCobwebTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color webSilk = new Color(0.85f, 0.87f, 0.90f, 0.92f); // #D8DCE3

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Corner anchored web radiating from top-left (0, size)
                    Vector2 origin = new Vector2(0, size);
                    Vector2 p = new Vector2(x, y) - origin;
                    float dist = p.magnitude;
                    float angle = Mathf.Atan2(-p.y, p.x) * Mathf.Rad2Deg; // 0 to 90 degrees

                    if (dist > size * 0.95f || angle < 0 || angle > 90f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Radial strands every 12 degrees
                    bool isRadial = (angle % 12f < 1.2f);

                    // Concentric spiral sagged strands every 20 pixels
                    float sag = Mathf.Sin(angle * Mathf.Deg2Rad * 2f) * 6f;
                    bool isSpiral = Mathf.Abs((dist + sag) % 20f) < 1.3f;

                    if (isRadial || isSpiral)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist / (size * 0.95f))) * webSilk.a;
                        tex.SetPixel(x, y, new Color(webSilk.r, webSilk.g, webSilk.b, alpha));
                    }
                    else
                    {
                        // Micro fine haze
                        float haze = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f;
                        tex.SetPixel(x, y, new Color(webSilk.r, webSilk.g, webSilk.b, haze));
                    }
                }
            }

            tex.Apply(true);
            SaveTexture($"{TexturesDir}/Tex_Cobweb_Albedo.png", tex, false, FilterMode.Bilinear, true);
        }

        private static void CreateStorefrontWoodTextures()
        {
            int size = 256;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int plankH = 64;

            for (int y = 0; y < size; y++)
            {
                int yMod = y % plankH;
                bool isPlankEdge = (yMod <= 2 || yMod >= plankH - 2);

                for (int x = 0; x < size; x++)
                {
                    // Weathered grayed-out wood tone #5C4A38
                    float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.03f) * 0.16f;
                    Color wood = new Color(0.36f + grain, 0.29f + grain * 0.7f, 0.22f + grain * 0.5f);

                    // Rusted iron nail heads at x=28 and x=228
                    bool isNail = (yMod >= 28 && yMod <= 36) && ((x >= 24 && x <= 32) || (x >= 224 && x <= 232));

                    if (isNail)
                    {
                        wood = new Color(0.18f, 0.12f, 0.10f); // Dark rusted iron
                    }
                    else if (isPlankEdge)
                    {
                        wood *= 0.55f; // Shadow seam
                    }

                    albedo.SetPixel(x, y, wood);

                    Vector3 n = Vector3.forward;
                    if (isNail)
                    {
                        n = new Vector3((x % 28 - 2) * 0.2f, (yMod - 32) * 0.2f, 1f).normalized;
                    }
                    else if (isPlankEdge)
                    {
                        n = new Vector3(0, yMod <= 2 ? -0.5f : 0.5f, 0.8f).normalized;
                    }
                    normal.SetPixel(x, y, EncodeNormal(n));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveTexture($"{TexturesDir}/Tex_Storefront_Wood_Albedo.png", albedo, false);
            SaveTexture($"{TexturesDir}/Tex_Storefront_Wood_Normal.png", normal, true);
        }

        private static void CreateDecalTextures()
        {
            // 1. Asphalt Cracks (256x256 Alpha Decal)
            int size = 256;
            Texture2D crackTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clears = new Color[size * size];
            crackTex.SetPixels(clears);

            // Draw branching crack lines
            void DrawCrackLine(Vector2 start, Vector2 end, float width)
            {
                float steps = Vector2.Distance(start, end) * 2f;
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 pt = Vector2.Lerp(start, end, i / steps);
                    int px = Mathf.Clamp((int)pt.x, 0, size - 1);
                    int py = Mathf.Clamp((int)pt.y, 0, size - 1);
                    crackTex.SetPixel(px, py, new Color(0.04f, 0.05f, 0.06f, 0.85f));
                    if (width > 1f)
                    {
                        crackTex.SetPixel(Mathf.Clamp(px + 1, 0, size - 1), py, new Color(0.04f, 0.05f, 0.06f, 0.5f));
                        crackTex.SetPixel(px, Mathf.Clamp(py + 1, 0, size - 1), new Color(0.04f, 0.05f, 0.06f, 0.5f));
                    }
                }
            }

            DrawCrackLine(new Vector2(20, 128), new Vector2(120, 140), 2f);
            DrawCrackLine(new Vector2(120, 140), new Vector2(230, 132), 2f);
            DrawCrackLine(new Vector2(80, 135), new Vector2(110, 200), 1.2f);
            DrawCrackLine(new Vector2(140, 138), new Vector2(170, 70), 1.2f);
            DrawCrackLine(new Vector2(170, 70), new Vector2(210, 45), 1.0f);

            crackTex.Apply();
            SaveTexture($"{TexturesDir}/Tex_Asphalt_Crack.png", crackTex, false);

            // 2. Curb Moss Strip (128x128 Alpha Decal)
            Texture2D mossTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
            {
                float v = (float)y / 128f;
                for (int x = 0; x < 128; x++)
                {
                    float u = (float)x / 128f;
                    float noise = Mathf.PerlinNoise(u * 14f, v * 14f);
                    float edgeCut = Mathf.SmoothStep(0.1f, 0.5f, v) * Mathf.SmoothStep(0.9f, 0.5f, v);
                    float alpha = (noise > 0.45f) ? (noise * edgeCut) : 0f;
                    Color mossCol = new Color(0.22f, 0.42f, 0.16f, alpha * 0.9f);
                    mossTex.SetPixel(x, y, mossCol);
                }
            }

            mossTex.Apply();
            SaveTexture($"{TexturesDir}/Tex_Moss_Strip.png", mossTex, false);
        }

        private static void CreateHitMarkerSprite()
        {
            int size = 64;
            Texture2D tex = CreateEmptyIconTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

            // 4 diagonal ticks radiating from center
            float gap = 6f;
            float length = 16f;

            void DrawDiagonalTick(float dx, float dy)
            {
                for (float d = gap; d <= gap + length; d += 0.5f)
                {
                    int x = (int)(c.x + dx * d);
                    int y = (int)(c.y + dy * d);
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        tex.SetPixel(x, y, Color.white);
                        // 2px width
                        tex.SetPixel(Mathf.Clamp(x + (int)Mathf.Sign(dx), 0, size - 1), y, new Color(1f, 1f, 1f, 0.8f));
                    }
                }
            }

            DrawDiagonalTick(1f, 1f);   // Top-Right
            DrawDiagonalTick(-1f, 1f);  // Top-Left
            DrawDiagonalTick(1f, -1f);  // Bottom-Right
            DrawDiagonalTick(-1f, -1f); // Bottom-Left

            tex.Apply();
            SaveAndConfigureSprite(tex, $"{SpritesDir}/icon_hitmarker_x.png", Vector4.zero);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private static Texture2D CreateEmptyIconTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clears = new Color[size * size];
            tex.SetPixels(clears);
            return tex;
        }

        private static void DrawFilledCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, (int)(center.x - radius));
            int maxX = Mathf.Min(tex.width - 1, (int)(center.x + radius));
            int minY = Mathf.Max(0, (int)(center.y - radius));
            int maxY = Mathf.Min(tex.height - 1, (int)(center.y + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= radius)
                    {
                        float a = Mathf.Clamp01(radius - d + 0.5f);
                        Color c = color;
                        c.a *= a;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        private static void DrawCapsuleSegment(Texture2D tex, Vector2 p1, Vector2 p2, float radius, Color color)
        {
            float len = Vector2.Distance(p1, p2);
            int steps = Mathf.Max(1, (int)(len * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pt = Vector2.Lerp(p1, p2, t);
                DrawFilledCircle(tex, pt, radius, color);
            }
        }

        private static Color EncodeNormal(Vector3 n)
        {
            return new Color((n.x * 0.5f) + 0.5f, (n.y * 0.5f) + 0.5f, (n.z * 0.5f) + 0.5f, 1f);
        }

        private static void SaveAndConfigureSprite(Texture2D tex, string path, Vector4 border)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                if (border != Vector4.zero) importer.spriteBorder = border;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        private static void SaveTexture(string path, Texture2D tex, bool isNormal, FilterMode filterMode = FilterMode.Bilinear, bool enableMipmap = false)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (isNormal)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                }
                importer.mipmapEnabled = enableMipmap;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = filterMode;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }
}
