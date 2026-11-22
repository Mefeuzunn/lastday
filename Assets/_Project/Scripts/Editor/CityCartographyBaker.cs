using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class CityCartographyBaker
    {
        private const string OutputPath = "Assets/_Project/UI/Map/BakedCityMap.png";
        private const int MapResolution = 2048;
        private const float CityExtent = 360f; // 720x720m total (-360 to +360)

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Bake City Cartography Map", false, 13)]
        public static void BakeCityMap()
        {
            Debug.Log("[CityCartographyBaker] Initiating Tactical Cartography Map Generation (720x720m, 2048x2048)...");

            EnsureDirectory();

            Texture2D finalMapTex = new Texture2D(MapResolution, MapResolution, TextureFormat.RGBA32, false);

            // 1. Snapshot scene geometry if in active scene
            RenderTexture rt = new RenderTexture(MapResolution, MapResolution, 24, RenderTextureFormat.ARGB32);
            GameObject camObj = new GameObject("__Temp_Cartography_Cam__");
            Camera cam = camObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 260f, 0f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = CityExtent;
            cam.aspect = 1.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.055f, 0.078f, 1.0f); // #0B0E14
            cam.cullingMask = ~LayerMask.GetMask("UI");
            cam.targetTexture = rt;

            bool prevFog = RenderSettings.fog;
            RenderSettings.fog = false;

            cam.Render();

            RenderTexture.active = rt;
            finalMapTex.ReadPixels(new Rect(0, 0, MapResolution, MapResolution), 0, 0);
            finalMapTex.Apply();

            // Restore settings & cleanup camera immediately
            RenderSettings.fog = prevFog;
            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camObj);

            // 2. Composite Crisp Tactical GPS Cartography & Vector Overlays
            ApplyTacticalCartographyStyling(finalMapTex);

            // 3. Save to disk & configure TextureImporter
            byte[] pngData = finalMapTex.EncodeToPNG();
            File.WriteAllBytes(OutputPath, pngData);
            UnityEngine.Object.DestroyImmediate(finalMapTex);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;
                importer.alphaIsTransparency = false;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CityCartographyBaker] Successfully baked and saved Tactical Map to {OutputPath}");
        }

        private static void EnsureDirectory()
        {
            string dir = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        private static void ApplyTacticalCartographyStyling(Texture2D tex)
        {
            int res = tex.width;
            float worldToTex = res / (CityExtent * 2.0f); // ~2.844 pixels per meter

            Color32[] pixels = tex.GetPixels32();

            // Tactical Blueprint Palette
            Color32 slateBg = new Color32(11, 14, 20, 255);       // #0B0E14
            Color32 gridLineColor = new Color32(22, 28, 38, 255);  // Subtle topographic grid
            Color32 sectorBorderColor = new Color32(48, 65, 88, 255); // Distinct 3x3 sector boundary
            Color32 avenueAsphalt = new Color32(28, 36, 48, 255);  // #1C2430 Major roads
            Color32 streetAsphalt = new Color32(22, 28, 38, 255);  // Minor streets
            Color32 curbColor = new Color32(45, 58, 76, 255);      // Sidewalks
            Color32 markingWhite = new Color32(140, 165, 195, 255);// Road markings
            Color32 markingYellow = new Color32(220, 175, 45, 255);// Center double lines
            Color32 buildingRoof = new Color32(16, 21, 29, 255);   // Building rooftop
            Color32 buildingBorder = new Color32(52, 68, 90, 255); // Building wall perimeter
            Color32 waterColor = new Color32(18, 55, 75, 255);     // Water bodies
            Color32 railColor = new Color32(95, 90, 80, 255);      // Railroad tracks
            Color32 parkFoliageColor = new Color32(18, 34, 24, 255);// Park greenery
            Color32 checkpointGate = new Color32(220, 120, 30, 255);// Perimeter checkpoints

            // Helper conversion functions
            int WorldXToTex(float wx) => Mathf.Clamp(Mathf.RoundToInt((wx + CityExtent) * worldToTex), 0, res - 1);
            int WorldZToTex(float wz) => Mathf.Clamp(Mathf.RoundToInt((wz + CityExtent) * worldToTex), 0, res - 1);

            // 1. Process base pixel contrast & tone
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                float lum = (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 255f;

                byte r = (byte)Mathf.Clamp((lum * 65f) + 8f, 0f, 255f);
                byte g = (byte)Mathf.Clamp((lum * 85f) + 12f, 0f, 255f);
                byte b = (byte)Mathf.Clamp((lum * 115f) + 18f, 0f, 255f);

                pixels[i] = new Color32(r, g, b, 255);
            }

            // 2. Draw Topographic Coordinate Grid (every 30m)
            for (float w = -330f; w <= 330f; w += 30f)
            {
                int tx = WorldXToTex(w);
                int tz = WorldZToTex(w);

                for (int y = 0; y < res; y++)
                {
                    int idx = y * res + tx;
                    pixels[idx] = Blend(pixels[idx], gridLineColor, 0.40f);
                }
                for (int x = 0; x < res; x++)
                {
                    int idx = tz * res + x;
                    pixels[idx] = Blend(pixels[idx], gridLineColor, 0.40f);
                }
            }

            // 3. Draw 3x3 Sector Grid Boundaries (at x = ±120m, z = ±120m)
            float[] sectorLines = new float[] { -120f, 120f };
            foreach (float bCoord in sectorLines)
            {
                int bx = WorldXToTex(bCoord);
                int bz = WorldZToTex(bCoord);

                for (int y = 0; y < res; y++)
                {
                    // Dashed line pattern
                    if ((y / 8) % 2 == 0)
                    {
                        for (int offset = -1; offset <= 1; offset++)
                        {
                            int px = Mathf.Clamp(bx + offset, 0, res - 1);
                            pixels[y * res + px] = sectorBorderColor;
                        }
                    }
                }

                for (int x = 0; x < res; x++)
                {
                    if ((x / 8) % 2 == 0)
                    {
                        for (int offset = -1; offset <= 1; offset++)
                        {
                            int py = Mathf.Clamp(bz + offset, 0, res - 1);
                            pixels[py * res + x] = sectorBorderColor;
                        }
                    }
                }
            }

            // 4. Draw Road Network with Vector Clarity
            void DrawRoadRect(float cx, float cz, float width, float length, bool isHorizontal, Color32 color, Color32 curb)
            {
                float halfW = width * 0.5f;
                float halfL = length * 0.5f;

                float minX = isHorizontal ? (cx - halfL) : (cx - halfW);
                float maxX = isHorizontal ? (cx + halfL) : (cx + halfW);
                float minZ = isHorizontal ? (cz - halfW) : (cz - halfL);
                float maxZ = isHorizontal ? (cz + halfW) : (cz + halfL);

                int x0 = WorldXToTex(minX - 1.5f);
                int x1 = WorldXToTex(maxX + 1.5f);
                int z0 = WorldZToTex(minZ - 1.5f);
                int z1 = WorldZToTex(maxZ + 1.5f);

                int ix0 = WorldXToTex(minX);
                int ix1 = WorldXToTex(maxX);
                int iz0 = WorldZToTex(minZ);
                int iz1 = WorldZToTex(maxZ);

                for (int z = z0; z <= z1; z++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int idx = z * res + x;
                        if (x >= ix0 && x <= ix1 && z >= iz0 && z <= iz1)
                        {
                            pixels[idx] = color;
                        }
                        else
                        {
                            pixels[idx] = curb;
                        }
                    }
                }
            }

            // Extended Grand Avenues spanning the full 720m world (from -350 to +350)
            DrawRoadRect(0f, 0f, 14f, 700f, false, avenueAsphalt, curbColor); // Grand Boulevard North-South
            DrawRoadRect(0f, 0f, 14f, 700f, true, avenueAsphalt, curbColor);  // Commercial Avenue East-West

            // Central Core Cross Streets
            DrawRoadRect(0f, 50f, 10f, 160f, true, streetAsphalt, curbColor);
            DrawRoadRect(0f, -50f, 10f, 160f, true, streetAsphalt, curbColor);
            DrawRoadRect(-45f, 0f, 9f, 140f, false, streetAsphalt, curbColor);
            DrawRoadRect(45f, 0f, 9f, 140f, false, streetAsphalt, curbColor);

            // Outer Sector Roads
            // North Highway secondary ramps
            DrawRoadRect(0f, 240f, 16f, 180f, false, avenueAsphalt, curbColor);
            DrawRoadRect(40f, 210f, 10f, 100f, true, streetAsphalt, curbColor);
            // East Rail industrial access
            DrawRoadRect(240f, 0f, 12f, 180f, true, avenueAsphalt, curbColor);
            DrawRoadRect(200f, -40f, 9f, 90f, false, streetAsphalt, curbColor);
            // West Park perimeter road
            DrawRoadRect(-240f, 0f, 12f, 180f, true, avenueAsphalt, curbColor);
            // South Plaza boulevard & bypass
            DrawRoadRect(0f, -240f, 16f, 180f, false, avenueAsphalt, curbColor);
            DrawRoadRect(-40f, -210f, 10f, 100f, true, streetAsphalt, curbColor);

            // Central Roundabout (Center 0,0, radius 18m)
            int rcx = WorldXToTex(0f);
            int rcz = WorldZToTex(0f);
            float rOuter = 18f * worldToTex;
            float rInner = 12f * worldToTex;
            float rFountain = 6f * worldToTex;

            for (int y = (int)(rcz - rOuter - 2); y <= (int)(rcz + rOuter + 2); y++)
            {
                for (int x = (int)(rcx - rOuter - 2); x <= (int)(rcx + rOuter + 2); x++)
                {
                    if (x < 0 || x >= res || y < 0 || y >= res) continue;
                    float dist = Mathf.Sqrt((x - rcx) * (x - rcx) + (y - rcz) * (y - rcz));
                    int idx = y * res + x;

                    if (dist <= rFountain)
                    {
                        pixels[idx] = waterColor;
                    }
                    else if (dist <= rInner)
                    {
                        pixels[idx] = curbColor;
                    }
                    else if (dist <= rOuter)
                    {
                        pixels[idx] = avenueAsphalt;
                    }
                    else if (dist <= rOuter + 2f)
                    {
                        pixels[idx] = curbColor;
                    }
                }
            }

            // Road centerlines (Double yellow on Grand Boulevard)
            int mainAveX0 = WorldXToTex(-0.35f);
            int mainAveX1 = WorldXToTex(0.35f);
            for (int y = 0; y < res; y++)
            {
                float wz = (y / worldToTex) - CityExtent;
                if (Mathf.Abs(wz) < 18f) continue; // Skip roundabout
                if (Mathf.Abs(wz) > 348f) continue;
                pixels[y * res + mainAveX0] = markingYellow;
                pixels[y * res + mainAveX1] = markingYellow;
            }

            // Centerlines (Double yellow on Commercial Avenue)
            int comAveZ0 = WorldZToTex(-0.35f);
            int comAveZ1 = WorldZToTex(0.35f);
            for (int x = 0; x < res; x++)
            {
                float wx = (x / worldToTex) - CityExtent;
                if (Mathf.Abs(wx) < 18f) continue;
                if (Mathf.Abs(wx) > 348f) continue;
                pixels[comAveZ0 * res + x] = markingYellow;
                pixels[comAveZ1 * res + x] = markingYellow;
            }

            // 5. Draw Building Footprints & Silhouettes
            void DrawBuilding(float cx, float cz, float sx, float sz)
            {
                int x0 = WorldXToTex(cx - sx * 0.5f);
                int x1 = WorldXToTex(cx + sx * 0.5f);
                int z0 = WorldZToTex(cz - sz * 0.5f);
                int z1 = WorldZToTex(cz + sz * 0.5f);

                for (int z = z0; z <= z1; z++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (x < 0 || x >= res || z < 0 || z >= res) continue;
                        int idx = z * res + x;
                        bool isBorder = (x == x0 || x == x1 || z == z0 || z == z1);
                        pixels[idx] = isBorder ? buildingBorder : buildingRoof;
                    }
                }
            }

            // Central Sector POIs
            DrawBuilding(-36f, 32f, 24f, 22f); // Clinic
            DrawBuilding(-35f, -42f, 22f, 22f); // Shelter
            DrawBuilding(32f, 30f, 26f, 24f);   // Supermarket
            DrawBuilding(30f, -48f, 20f, 20f);  // Pharmacy
            DrawBuilding(65f, -65f, 22f, 26f);  // Gas station canopy & shop
            DrawBuilding(0f, 18f, 16f, 14f);    // Police Checkpoint

            // Surrounding urban city blocks
            DrawBuilding(-75f, 50f, 30f, 35f);
            DrawBuilding(-75f, -50f, 30f, 35f);
            DrawBuilding(75f, 50f, 30f, 35f);
            DrawBuilding(75f, -45f, 30f, 35f);

            DrawBuilding(-45f, 80f, 35f, 20f);
            DrawBuilding(45f, 80f, 35f, 20f);
            DrawBuilding(-45f, -80f, 35f, 20f);
            DrawBuilding(45f, -80f, 35f, 20f);

            // Outer Sector Landmarks
            // North: Highway Overpass & FOB Outpost
            DrawRoadRect(0f, 245f, 16f, 195f, false, avenueAsphalt, curbColor); // Elevated Viaduct Deck
            DrawBuilding(40f, 220f, 35f, 25f); // FOB Camp Tents & Bunkers

            // East: Rail Yard Depots & Warehouses
            for (int t = 0; t < 4; t++)
            {
                float rx = 210f + (t * 12f);
                DrawRoadRect(rx, 0f, 2.4f, 190f, false, streetAsphalt, curbColor); // Rail Tracks
            }
            DrawBuilding(305f, 65f, 45f, 28f);  // Warehouse 1 (North Logistics)
            DrawBuilding(305f, 0f, 45f, 28f);   // Warehouse 2 (Machine Shop)
            DrawBuilding(305f, -65f, 45f, 28f); // Warehouse 3 (South Steel Depot)

            // NorthEast: Harbor Seawall, Container Maze & Fuel Depot
            DrawRoadRect(335f, 240f, 6f, 200f, false, curbColor, avenueAsphalt); // Seawall East
            DrawRoadRect(240f, 335f, 200f, 6f, true, curbColor, avenueAsphalt);  // Seawall North
            DrawBuilding(240f, 240f, 75f, 75f);  // Container Maze Yard
            DrawBuilding(175f, 265f, 52f, 64f);  // Coastal Fuel Depot

            // SouthEast: Power Substation Yard & Control Building
            DrawBuilding(240f, -240f, 85f, 105f); // Transformer Yard
            DrawBuilding(265f, -260f, 18f, 14f);  // Control Room Building

            // =========================================================================
            // PHASE 3 OUTER SECTORS CARTOGRAPHY
            // =========================================================================
            // 1. West: Urban Forest Park, Maintenance Shed & Trails
            DrawBuilding(-240f, 0f, 16f, 16f);   // Stone Gazebo Pavilion
            DrawBuilding(-210f, 35f, 18f, 14f);  // Park Maintenance Shed
            DrawBuilding(-265f, -20f, 22f, 18f); // Abandoned Ranger Hiker Camp

            // 2. NorthWest: Lakefront Pier, 3 Cabins & Lookout Tower
            DrawRoadRect(-265f, 265f, 130f, 110f, false, new Color32(16, 36, 40, 240), new Color32(28, 55, 60, 255)); // Lake Basin
            DrawRoadRect(-220f, 240f, 38f, 5f, true, streetAsphalt, curbColor); // Timber Pier
            DrawBuilding(-185f, 220f, 16f, 14f); // Cabin A (South Lakefront)
            DrawBuilding(-195f, 275f, 18f, 14f); // Cabin B (Mid Lakefront)
            DrawBuilding(-235f, 320f, 15f, 13f); // Cabin C (North Pines)
            DrawBuilding(-175f, 175f, 10f, 10f); // Forest Fire Lookout Tower

            // 3. South: Sunken Subway Plaza & Two-Story Grand Shopping Mall
            DrawRoadRect(0f, -185f, 48f, 65f, false, streetAsphalt, curbColor); // Sunken Subway Plaza
            DrawRoadRect(-7f, -205f, 4f, 45f, false, markingYellow, streetAsphalt); // Subway Track A
            DrawRoadRect(7f, -205f, 4f, 45f, false, markingYellow, streetAsphalt);  // Subway Track B
            DrawBuilding(0f, -295f, 85f, 95f);  // Grand Shopping Mall Main Body
            DrawBuilding(0f, -250f, 32f, 20f);  // Mall Glass Entrance Atrium

            // 4. SouthWest: Flooded Suburbs & Elevated Boardwalks
            DrawRoadRect(-240f, -240f, 150f, 150f, false, new Color32(22, 38, 36, 230), new Color32(35, 58, 54, 255)); // Flooded Street Basin
            DrawRoadRect(-240f, -240f, 120f, 4f, true, streetAsphalt, curbColor);  // Main Boardwalk East-West
            DrawRoadRect(-240f, -240f, 4f, 120f, false, streetAsphalt, curbColor); // Main Boardwalk North-South
            DrawBuilding(-275f, -210f, 22f, 20f); // Submerged House 1
            DrawBuilding(-205f, -210f, 24f, 20f); // Submerged House 2
            DrawBuilding(-280f, -270f, 22f, 22f); // Submerged House 3
            DrawBuilding(-200f, -270f, 24f, 22f); // Submerged House 4
            DrawBuilding(-240f, -315f, 26f, 24f); // Submerged House 5

            // 6. Draw 4 Perimeter Checkpoint Indicators at ±120m
            void DrawCheckpointIcon(float cx, float cz, bool horizontal)
            {
                int x0 = WorldXToTex(cx - (horizontal ? 3f : 9f));
                int x1 = WorldXToTex(cx + (horizontal ? 3f : 9f));
                int z0 = WorldZToTex(cz - (horizontal ? 9f : 3f));
                int z1 = WorldZToTex(cz + (horizontal ? 9f : 3f));

                for (int z = z0; z <= z1; z++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (x < 0 || x >= res || z < 0 || z >= res) continue;
                        pixels[z * res + x] = checkpointGate;
                    }
                }
            }

            DrawCheckpointIcon(0f, 120f, false);  // Kuzey Kapısı
            DrawCheckpointIcon(0f, -120f, false); // Güney Kapısı
            DrawCheckpointIcon(120f, 0f, true);   // Doğu Kapısı
            DrawCheckpointIcon(-120f, 0f, true);  // Batı Kapısı

            tex.SetPixels32(pixels);
            tex.Apply();
        }

        private static Color32 Blend(Color32 baseCol, Color32 overCol, float alpha)
        {
            byte r = (byte)Mathf.Clamp(Mathf.Lerp(baseCol.r, overCol.r, alpha), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.Lerp(baseCol.g, overCol.g, alpha), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.Lerp(baseCol.b, overCol.b, alpha), 0, 255);
            return new Color32(r, g, b, 255);
        }
#endif
    }
}
