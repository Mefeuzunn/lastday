using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 3 Open-World Level Builder for Unity 6 (URP).
    /// Constructs the remaining 4 western and southern sectors of the 720x720m 3x3 open-world grid:
    /// 1. Sector_-1_0_West: Overgrown Urban Forest Park, 65+ Sculpted Trees, Stealth Trails & Ranger Camp.
    /// 2. Sector_-1_1_NorthWest: Lakefront Cabins, Reflective Lake Basin, Pier & Fire Lookout Tower.
    /// 3. Sector_0_-1_South: Sunken Subway Station (Y: -4.2m) & Two-Story Grand Shopping Mall.
    /// 4. Sector_-1_-1_SouthWest: Flooded Residential Suburbs, 0.22m Murky Water Layer, Movement Modifier & Elevated Boardwalks.
    /// Re-bakes unified 720x720m NavMesh, refreshes 2048x2048 BakedCityMap.png, and registers all 12 districts.
    /// </summary>
    public static class Phase3DistrictBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Build Phase 3 Outer Sectors (West & South)", false, 16)]
        public static void BuildPhase3Districts()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> PHASE 3 MAP EXPANSION (WEST & SOUTH SECTORS) STARTED <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            if (worldSectorsRoot == null)
            {
                Debug.LogError("[Phase3DistrictBuilder] '--- WORLD SECTORS ---' root not found! Run Phase 1 first.");
                return;
            }

            // Ensure 4 target sectors exist
            Transform westSector = worldSectorsRoot.transform.Find("Sector_-1_0_West");
            Transform northWestSector = worldSectorsRoot.transform.Find("Sector_-1_1_NorthWest");
            Transform southSector = worldSectorsRoot.transform.Find("Sector_0_-1_South");
            Transform southWestSector = worldSectorsRoot.transform.Find("Sector_-1_-1_SouthWest");

            if (westSector == null || northWestSector == null || southSector == null || southWestSector == null)
            {
                Debug.LogError("[Phase3DistrictBuilder] Target sector roots missing under '--- WORLD SECTORS ---'!");
                return;
            }

            // 1. Prepare PBR Materials
            Material concreteMat = SceneBuilder.GetOrCreateMaterial("Mat_Viaduct_Concrete", new Color(0.38f, 0.40f, 0.42f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(8f, 8f));
            Material timberWoodMat = SceneBuilder.GetOrCreateMaterial("Mat_Wood_Timber", new Color(0.35f, 0.25f, 0.16f),
                "Assets/_Project/Textures/Tex_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Wood_Normal.png", new Vector2(4f, 4f));
            Material brickMat = SceneBuilder.GetOrCreateMaterial("Mat_Brick_Red", new Color(0.48f, 0.26f, 0.20f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(6f, 4f));
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Transformer_Metal", new Color(0.24f, 0.26f, 0.28f));
            Material hazardMat = SceneBuilder.GetOrCreateMaterial("Mat_Warning_Hazard", new Color(0.95f, 0.75f, 0.10f));
            Material militaryMat = SceneBuilder.GetOrCreateMaterial("Mat_Military_Olive", new Color(0.22f, 0.28f, 0.20f));

            // Specialized Phase 3 Materials
            Material treeTrunkMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Trunk", new Color(0.28f, 0.20f, 0.14f),
                "Assets/_Project/Textures/Tex_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Wood_Normal.png", new Vector2(2f, 8f));

            // Foliage Material: Alpha-Clipped + GPU Instanced
            Material foliageClippedMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Foliage_Clipped", new Color(0.18f, 0.42f, 0.22f));
            foliageClippedMat.SetFloat("_AlphaClip", 1f);
            foliageClippedMat.SetFloat("_Cutoff", 0.45f);
            foliageClippedMat.EnableKeyword("_ALPHATEST_ON");
            foliageClippedMat.enableInstancing = true;

            // Bush / Hiding Foliage
            Material bushFoliageMat = SceneBuilder.GetOrCreateMaterial("Mat_Bush_StealthFoliage", new Color(0.14f, 0.35f, 0.18f));
            bushFoliageMat.SetFloat("_AlphaClip", 1f);
            bushFoliageMat.SetFloat("_Cutoff", 0.45f);
            bushFoliageMat.EnableKeyword("_ALPHATEST_ON");
            bushFoliageMat.enableInstancing = true;

            // Reflective Forest Lake Water (Deep Pine-Slate #102426)
            Material lakeWaterMat = SceneBuilder.GetOrCreateMaterial("Mat_Lake_DarkWater", new Color(0.063f, 0.141f, 0.149f, 0.95f),
                null, null, null, true, 0.92f);
            lakeWaterMat.SetFloat("_Metallic", 0.30f);

            // Shallow Murky Floodwater (#1B2A28)
            Material floodWaterMat = SceneBuilder.GetOrCreateMaterial("Mat_Flood_MurkyWater", new Color(0.106f, 0.165f, 0.157f, 0.88f),
                null, null, null, true, 0.88f);

            // Mall & Subway Materials
            Material subwayTileMat = SceneBuilder.GetOrCreateMaterial("Mat_Subway_Tiled", new Color(0.68f, 0.70f, 0.66f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", null, new Vector2(10f, 10f));
            Material mallMarbleMat = SceneBuilder.GetOrCreateMaterial("Mat_Mall_Marble", new Color(0.78f, 0.80f, 0.82f),
                null, null, null, false, 0.65f);
            Material mallGlassMat = SceneBuilder.GetOrCreateMaterial("Mat_Mall_Glass", new Color(0.35f, 0.55f, 0.65f, 0.45f),
                null, null, null, true, 0.95f);
            Material storePartitionMat = SceneBuilder.GetOrCreateMaterial("Mat_Store_Partition", new Color(0.25f, 0.28f, 0.32f));

            // Load loot item data
            ItemData ammo9mm = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_9mm.asset");
            ItemData ammoShells = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_shells.asset");
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            ItemData bandage = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bandage.asset");
            ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
            ItemData alcohol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_alcohol.asset");
            ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");
            ItemData crowbar = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_crowbar.asset");
            ItemData axe = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/weapon_axe.asset");
            ItemData shiv = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_shiv.asset");
            ItemData cannedFood = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData waterBottle = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");

            // 2. Build Sector 1: Sector_-1_0_West (Urban Forest Park & Stealth Trails)
            BuildSectorMinus10West(westSector, treeTrunkMat, foliageClippedMat, bushFoliageMat, concreteMat, brickMat, timberWoodMat,
                militaryMat, cannedFood, waterBottle, bandage, ammo9mm, scrap, crowbar, battery);

            // 3. Build Sector 2: Sector_-1_1_NorthWest (Lakefront Cabins & Fire Lookout Tower)
            BuildSectorMinus11NorthWest(northWestSector, lakeWaterMat, timberWoodMat, brickMat, concreteMat,
                cannedFood, waterBottle, bandage, alcohol, ammo9mm, ammoShells, medkit, axe);

            // 4. Build Sector 3: Sector_0_-1_South (Sunken Subway Station & Grand Shopping Mall)
            BuildSector0Minus1South(southSector, subwayTileMat, mallMarbleMat, mallGlassMat, storePartitionMat, concreteMat, metalMat,
                ammo9mm, ammoShells, axe, crowbar, scrap, battery, alcohol, medkit, bandage, cannedFood, waterBottle);

            // 5. Build Sector 4: Sector_-1_-1_SouthWest (Flooded Suburbs & Boardwalks)
            BuildSectorMinus1Minus1SouthWest(southWestSector, floodWaterMat, timberWoodMat, brickMat, concreteMat,
                cannedFood, waterBottle, bandage, alcohol, ammo9mm, scrap, battery);

            // 6. Build Autonomous Scavenger Squads (Mall & Forest Patrols)
            BuildScavengerSquads();

            // 7. Atmosphere Pass: Generate Ivy & Cobwebs
            GenerateAtmosphereDetails(westSector, northWestSector, southSector, southWestSector);

            // 8. Update DistrictZombieEcology territorial zones to include all 12 zones
            DistrictZombieEcology ecology = UnityEngine.Object.FindAnyObjectByType<DistrictZombieEcology>(FindObjectsInactive.Include);
            if (ecology != null)
            {
                ecology.InitializeZones();
                EditorUtility.SetDirty(ecology);
                Debug.Log($"[Phase3DistrictBuilder] Initialized DistrictZombieEcology with {ecology.Zones.Count} zones.");
            }

            // 9. Re-bake NavMesh across the unified 720x720m world
            Debug.Log("[Phase3DistrictBuilder] Re-baking unified NavMesh across forest trails, lake dock, sunken subway, and 2-story mall...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[Phase3DistrictBuilder] NavMesh re-bake completed.");

            // 10. Re-bake Cartography Map
            Debug.Log("[Phase3DistrictBuilder] Re-baking 2048x2048 Tactical City Map with Phase 3 districts...");
            CityCartographyBaker.BakeCityMap();

            // 11. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("==================================================================");
            Debug.Log(">>> PHASE 3 DISTRICT EXPANSION COMPLETE & SCENE SAVED <<<");
            Debug.Log("==================================================================");
        }

        // =========================================================================
        // SECTOR 1: SECTOR_-1_0_WEST (OVERGROWN URBAN FOREST PARK & STEALTH TRAILS)
        // World bounds: X: -360..-120, Z: -120..+120. Local center is (-240, 0, 0).
        // =========================================================================
        private static void BuildSectorMinus10West(Transform sectorRoot, Material trunkMat, Material foliageMat, Material bushMat,
            Material concreteMat, Material brickMat, Material woodMat, Material militaryMat, ItemData cannedFood, ItemData waterBottle,
            ItemData bandage, ItemData ammo9mm, ItemData scrap, ItemData crowbar, ItemData battery)
        {
            Transform oldLandmark = sectorRoot.Find("Landmark_WestCityPark");
            if (oldLandmark != null) UnityEngine.Object.DestroyImmediate(oldLandmark.gameObject);

            Transform existing = sectorRoot.Find("Phase3_UrbanForestAndPark");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase3_UrbanForestAndPark");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Stone Gazebo Pavilion at park center (0, 0, 0)
            GameObject gazebo = new GameObject("Stone_Gazebo_Pavilion");
            gazebo.transform.SetParent(root.transform, false);
            gazebo.transform.localPosition = Vector3.zero;

            CreateBox(gazebo.transform, "Plaza_Base", new Vector3(0f, 0.25f, 0f), new Vector3(18f, 0.5f, 18f), concreteMat, true);
            // 4 Corner Pillars
            CreateBox(gazebo.transform, "Pillar_NE", new Vector3(7f, 2.5f, 7f), new Vector3(0.9f, 4.5f, 0.9f), concreteMat, true);
            CreateBox(gazebo.transform, "Pillar_NW", new Vector3(-7f, 2.5f, 7f), new Vector3(0.9f, 4.5f, 0.9f), concreteMat, true);
            CreateBox(gazebo.transform, "Pillar_SE", new Vector3(7f, 2.5f, -7f), new Vector3(0.9f, 4.5f, 0.9f), concreteMat, true);
            CreateBox(gazebo.transform, "Pillar_SW", new Vector3(-7f, 2.5f, -7f), new Vector3(0.9f, 4.5f, 0.9f), concreteMat, true);
            // Roof
            CreateBox(gazebo.transform, "Roof_Main", new Vector3(0f, 5.0f, 0f), new Vector3(20f, 0.8f, 20f), woodMat, true);
            // Stone Benches inside
            GameObject bench1 = CreateBox(gazebo.transform, "Bench_North", new Vector3(0f, 0.65f, 6.2f), new Vector3(6f, 0.5f, 0.8f), concreteMat, true);
            bench1.AddComponent<FurnitureInteractable>();
            GameObject bench2 = CreateBox(gazebo.transform, "Bench_South", new Vector3(0f, 0.65f, -6.2f), new Vector3(6f, 0.5f, 0.8f), concreteMat, true);
            bench2.AddComponent<FurnitureInteractable>();

            // 2. Asphalt & Dirt Park Trails (2.4m+ walkable corridors)
            GameObject trails = new GameObject("Park_Trails_Network");
            trails.transform.SetParent(root.transform, false);

            // Main East-West Connector (From Gateway at X: 120 to Gazebo at 0)
            CreateBox(trails.transform, "Trail_East_Main", new Vector3(60f, 0.05f, 0f), new Vector3(120f, 0.1f, 4.2f), concreteMat, true);
            // West loop trail
            CreateBox(trails.transform, "Trail_West_Loop", new Vector3(-55f, 0.05f, 0f), new Vector3(90f, 0.1f, 3.8f), concreteMat, true);
            // North trail to Shed & Lake gateway
            CreateBox(trails.transform, "Trail_North_Branch", new Vector3(20f, 0.05f, 45f), new Vector3(3.8f, 0.1f, 90f), concreteMat, true);
            // South trail to Hiker Camp
            CreateBox(trails.transform, "Trail_South_Branch", new Vector3(-20f, 0.05f, -45f), new Vector3(3.8f, 0.1f, 90f), concreteMat, true);

            // Wooden footbridges over terrain dips
            CreateBox(trails.transform, "Footbridge_North", new Vector3(20f, 0.35f, 65f), new Vector3(5f, 0.3f, 14f), woodMat, true);
            CreateBox(trails.transform, "Footbridge_South", new Vector3(-20f, 0.35f, -65f), new Vector3(5f, 0.3f, 14f), woodMat, true);

            // 3. 68 Procedural Multi-Branch Sculpted Trees with GPU Instanced Canopies
            GameObject treesParent = new GameObject("Procedural_Forest_Trees");
            treesParent.transform.SetParent(root.transform, false);

            // Seeded deterministic layout avoiding the 3.8m trail corridors
            int treeCount = 70;
            float goldenRatio = (1f + Mathf.Sqrt(5f)) * 0.5f;

            for (int i = 0; i < treeCount; i++)
            {
                float radius = Mathf.Lerp(14f, 105f, Mathf.Sqrt((float)(i + 1) / treeCount));
                float theta = i * 2f * Mathf.PI * goldenRatio;

                float tx = Mathf.Cos(theta) * radius;
                float tz = Mathf.Sin(theta) * radius;

                // Ensure clearance from trails and structures (minimum 3.2m distance)
                if (Mathf.Abs(tz) < 3.2f && Mathf.Abs(tx) < 115f) tx += (tx >= 0 ? 4.5f : -4.5f);
                if (Mathf.Abs(tx - 20f) < 3.2f && tz > 0f) tx += 4.5f;
                if (Mathf.Abs(tx + 20f) < 3.2f && tz < 0f) tx -= 4.5f;
                if (Vector2.Distance(new Vector2(tx, tz), Vector2.zero) < 12f) continue; // Keep gazebo clear

                bool isPine = (i % 3 == 0);
                CreateProceduralTree(treesParent.transform, $"Tree_{i + 1}_{(isPine ? "Pine" : "Oak")}",
                    new Vector3(tx, 0f, tz), isPine, trunkMat, foliageMat);
            }

            // Fallen Mossy Trunks along trail sides
            CreateFallenTrunk(treesParent.transform, "FallenTrunk_1", new Vector3(45f, 0.4f, 4f), 15f, trunkMat);
            CreateFallenTrunk(treesParent.transform, "FallenTrunk_2", new Vector3(-35f, 0.4f, -4.5f), -25f, trunkMat);
            CreateFallenTrunk(treesParent.transform, "FallenTrunk_3", new Vector3(25f, 0.4f, 28f), 65f, trunkMat);

            // 4. Tactical Stealth Foliage Bushes with HidingSpot triggers
            GameObject bushesParent = new GameObject("Tactical_Stealth_Bushes");
            bushesParent.transform.SetParent(root.transform, false);

            Vector3[] bushPositions = new Vector3[]
            {
                new Vector3(38f, 0f, 3.8f),
                new Vector3(75f, 0f, -3.8f),
                new Vector3(-42f, 0f, 3.8f),
                new Vector3(-72f, 0f, -3.8f),
                new Vector3(16f, 0f, 32f),
                new Vector3(24f, 0f, 78f),
                new Vector3(-24f, 0f, -32f),
                new Vector3(-16f, 0f, -78f)
            };

            for (int b = 0; b < bushPositions.Length; b++)
            {
                CreateStealthBushHidingSpot(bushesParent.transform, $"Stealth_Bush_Cluster_{b + 1}", bushPositions[b], bushMat);
            }

            // 5. Park Maintenance Shed (World: -210, 0, 35 -> Local: 30, 0, 35)
            CreateParkMaintenanceShed(root.transform, "Park_Maintenance_Shed", new Vector3(30f, 0f, 35f), brickMat, concreteMat,
                scrap, crowbar, battery);

            // Abandoned Ranger Pickup Truck near Shed
            CreateRangerPickupTruck(root.transform, "Ranger_Pickup_Truck", new Vector3(40f, 0.1f, 32f), -20f, militaryMat, scrap);

            // 6. Abandoned Hiker Tents Camp (World: -265, 0, -20 -> Local: -25, 0, -20)
            CreateHikerCamp(root.transform, "Hiker_Tents_Camp", new Vector3(-25f, 0f, -20f), militaryMat, woodMat,
                cannedFood, waterBottle, bandage, ammo9mm);
        }

        // =========================================================================
        // SECTOR 2: SECTOR_-1_1_NORTHWEST (LAKEFRONT CABINS & RANGER OUTPOST)
        // World bounds: X: -360..-120, Z: +120..+360. Local center is (-240, 0, 240).
        // =========================================================================
        private static void BuildSectorMinus11NorthWest(Transform sectorRoot, Material waterMat, Material woodMat,
            Material brickMat, Material concreteMat, ItemData cannedFood, ItemData waterBottle, ItemData bandage,
            ItemData alcohol, ItemData ammo9mm, ItemData ammoShells, ItemData medkit, ItemData axe)
        {
            Transform oldLandmark = sectorRoot.Find("Landmark_LakesideWoodlands");
            if (oldLandmark != null) UnityEngine.Object.DestroyImmediate(oldLandmark.gameObject);

            Transform existing = sectorRoot.Find("Phase3_LakesideCabinsAndOutpost");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase3_LakesideCabinsAndOutpost");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Reflective Forest Lake Basin & Water Sheet (Local: -25, -0.3f, 25; World: -265, -0.3f, 265)
            GameObject lake = new GameObject("Forest_Lake_Basin");
            lake.transform.SetParent(root.transform, false);
            lake.transform.localPosition = new Vector3(-25f, 0f, 25f);

            // Water Plane
            GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "Lake_Water_Surface";
            waterPlane.transform.SetParent(lake.transform, false);
            waterPlane.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            waterPlane.transform.localScale = new Vector3(13f, 1f, 11f); // 130m x 110m
            waterPlane.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Deep-water safety colliders around edge to prevent falling out-of-bounds
            CreateBox(lake.transform, "DeepWater_Bound_W", new Vector3(-62f, 1f, 0f), new Vector3(4f, 4f, 110f), null, true);
            CreateBox(lake.transform, "DeepWater_Bound_N", new Vector3(0f, 1f, 52f), new Vector3(130f, 4f, 4f), null, true);

            // 2. Weathered Timber Fishing Pier (Walkable Dock)
            // Extends from south embankment (Z = -25) out to (Z = +13)
            GameObject pier = new GameObject("Timber_Fishing_Pier");
            pier.transform.SetParent(root.transform, false);
            pier.transform.localPosition = new Vector3(20f, 0.35f, 0f);

            CreateBox(pier.transform, "Pier_Planks_Deck", new Vector3(0f, 0f, 19f), new Vector3(4.8f, 0.4f, 38f), woodMat, true);
            // Timber pilings into the lake
            for (float pz = 4f; pz <= 36f; pz += 8f)
            {
                CreateBox(pier.transform, $"Piling_L_{pz}", new Vector3(-2.2f, -1.0f, pz), new Vector3(0.4f, 2.8f, 0.4f), woodMat, true);
                CreateBox(pier.transform, $"Piling_R_{pz}", new Vector3(2.2f, -1.0f, pz), new Vector3(0.4f, 2.8f, 0.4f), woodMat, true);
            }
            // Safety railings along left and right
            CreateBox(pier.transform, "Railing_L", new Vector3(-2.3f, 0.6f, 19f), new Vector3(0.2f, 0.9f, 38f), woodMat, true);
            CreateBox(pier.transform, "Railing_R", new Vector3(2.3f, 0.6f, 19f), new Vector3(0.2f, 0.9f, 38f), woodMat, true);

            // Pier end fishing crate with loot
            GameObject pierLoot = CreateBox(pier.transform, "Pier_Tackle_Box", new Vector3(0f, 0.5f, 36f), new Vector3(1.2f, 0.6f, 1.0f), woodMat, true);
            pierLoot.AddComponent<FurnitureInteractable>();
            LootContainer pierLc = pierLoot.AddComponent<LootContainer>();
            if (cannedFood != null) pierLc.AddItem(cannedFood, 2, 1f);
            if (waterBottle != null) pierLc.AddItem(waterBottle, 2, 1f);

            // 3. Three Enterable Timber Cabins
            // Cabin A (South Lakefront): Local (55, 0, -20) -> World (-185, 0, 220)
            CreateEnterableCabin(root.transform, "Cabin_A_SouthLakefront", new Vector3(55f, 0f, -20f), 0f, woodMat, brickMat,
                cannedFood, waterBottle, bandage, alcohol);

            // Cabin B (Mid Lakefront): Local (45, 0, 35) -> World (-195, 0, 275)
            CreateEnterableCabin(root.transform, "Cabin_B_MidLakefront", new Vector3(45f, 0f, 35f), -35f, woodMat, brickMat,
                cannedFood, waterBottle, bandage, ammo9mm);

            // Cabin C (North Pines): Local (5, 0, 80) -> World (-235, 0, 320)
            CreateEnterableCabin(root.transform, "Cabin_C_NorthPines", new Vector3(5f, 0f, 80f), 45f, woodMat, brickMat,
                cannedFood, waterBottle, alcohol, axe);

            // 4. 5.5m Elevated Forest Fire Lookout Tower (Local: 65, 0, -65; World: -175, 0, 175)
            CreateFireLookoutTower(root.transform, "Fire_Lookout_Tower", new Vector3(65f, 0f, -65f), woodMat, concreteMat,
                ammo9mm, ammoShells, medkit, axe);
        }

        // =========================================================================
        // SECTOR 3: SECTOR_0_-1_SOUTH (SUNKEN SUBWAY STATION & GRAND SHOPPING MALL)
        // World bounds: X: -120..+120, Z: -360..-120. Local center is (0, 0, -240).
        // =========================================================================
        private static void BuildSector0Minus1South(Transform sectorRoot, Material subwayTileMat, Material marbleMat,
            Material glassMat, Material storePartitionMat, Material concreteMat, Material metalMat,
            ItemData ammo9mm, ItemData ammoShells, ItemData axe, ItemData crowbar, ItemData scrap, ItemData battery,
            ItemData alcohol, ItemData medkit, ItemData bandage, ItemData cannedFood, ItemData waterBottle)
        {
            Transform oldLandmark = sectorRoot.Find("Landmark_SouthMetroPlaza");
            if (oldLandmark != null) UnityEngine.Object.DestroyImmediate(oldLandmark.gameObject);

            Transform existing = sectorRoot.Find("Phase3_SubwayAndGrandMall");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase3_SubwayAndGrandMall");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Sunken Plaza & Underground Subway Concourse (Local Z: +85 to +15; World Z: -155 to -225)
            GameObject subway = new GameObject("Sunken_Subway_Terminal");
            subway.transform.SetParent(root.transform, false);
            subway.transform.localPosition = new Vector3(0f, 0f, 50f);

            // Plaza Entrance Ramps / Wide Stairs (Descending from Y: 0 to Y: -4.2m, Slope ~22° <= 28°)
            GameObject stairRampNorth = CreateBox(subway.transform, "Stair_Ramp_North", new Vector3(0f, -2.1f, 32f), new Vector3(14f, 0.3f, 11f), concreteMat, true);
            stairRampNorth.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            // Subterranean Concourse Floor (Y = -4.2m, 48m x 65m)
            CreateBox(subway.transform, "Subway_Floor", new Vector3(0f, -4.2f, 0f), new Vector3(48f, 0.4f, 65f), subwayTileMat, true);

            // Retaining walls & ceiling
            CreateBox(subway.transform, "Wall_West", new Vector3(-24f, -1.8f, 0f), new Vector3(0.6f, 4.8f, 65f), concreteMat, true);
            CreateBox(subway.transform, "Wall_East", new Vector3(24f, -1.8f, 0f), new Vector3(0.6f, 4.8f, 65f), concreteMat, true);
            CreateBox(subway.transform, "Wall_South", new Vector3(0f, -1.8f, -32.5f), new Vector3(48f, 4.8f, 0.6f), concreteMat, true);

            // Tiled Station Pillars
            for (float pz = -22f; pz <= 22f; pz += 14f)
            {
                CreateBox(subway.transform, $"Pillar_L_{pz}", new Vector3(-9f, -1.8f, pz), new Vector3(1.4f, 4.8f, 1.4f), subwayTileMat, true);
                CreateBox(subway.transform, $"Pillar_R_{pz}", new Vector3(9f, -1.8f, pz), new Vector3(1.4f, 4.8f, 1.4f), subwayTileMat, true);
            }

            // Ticket Booths & Turnstiles
            GameObject ticketBooth = CreateBox(subway.transform, "Ticket_Booth", new Vector3(-16f, -3.2f, 18f), new Vector3(4f, 2.2f, 3.2f), subwayTileMat, true);
            ticketBooth.AddComponent<FurnitureInteractable>();
            LootContainer tbLoot = ticketBooth.AddComponent<LootContainer>();
            if (scrap != null) tbLoot.AddItem(scrap, 3, 1f);
            if (battery != null) tbLoot.AddItem(battery, 1, 0.8f);

            // Turnstile barrier
            CreateBox(subway.transform, "Turnstiles_Bar", new Vector3(0f, -3.6f, 18f), new Vector3(16f, 1.0f, 0.8f), metalMat, true);

            // Stationary Subway Train Cars on Recessed Tracks
            CreateSubwayCar(subway.transform, "Subway_Car_A", new Vector3(-15f, -4.2f, -8f), 0f, metalMat, scrap, battery, ammo9mm);
            CreateSubwayCar(subway.transform, "Subway_Car_B", new Vector3(15f, -4.2f, -8f), 0f, metalMat, scrap, alcohol, ammo9mm);

            // Pre-wired Fast-Travel Anchor Gates on East & West tunnel walls
            CreateSubwayServiceGate(subway.transform, "SubwayServiceGate_West", new Vector3(-23.6f, -3.0f, -18f), Quaternion.Euler(0f, 9f, 0f));
            CreateSubwayServiceGate(subway.transform, "SubwayServiceGate_East", new Vector3(23.6f, -3.0f, -18f), Quaternion.Euler(0f, -90f, 0f));

            // Fluorescent Emergency Light Strips
            CreateFluorescentLight(subway.transform, "Fluorescent_Red_1", new Vector3(0f, -0.2f, 10f), new Color(1.0f, 0.2f, 0.2f));
            CreateFluorescentLight(subway.transform, "Fluorescent_White_2", new Vector3(0f, -0.2f, -12f), new Color(0.85f, 0.95f, 1.0f));

            // 2. Two-Story Grand Shopping Mall (Local Z: -5 to -105; World Z: -245 to -345)
            GameObject mall = new GameObject("Grand_Shopping_Mall");
            mall.transform.SetParent(root.transform, false);
            mall.transform.localPosition = new Vector3(0f, 0f, -55f);

            float mw = 85f;
            float ml = 95f;

            // Ground Floor (Y = 0m)
            CreateBox(mall.transform, "Mall_Floor_L1", new Vector3(0f, 0.1f, 0f), new Vector3(mw, 0.2f, ml), marbleMat, true);

            // Second Floor Gallery Walkway (Y = 4.4m) with open center atrium
            // North Walkway
            CreateBox(mall.transform, "Walkway_L2_North", new Vector3(0f, 4.4f, 32f), new Vector3(mw - 4f, 0.3f, 18f), marbleMat, true);
            // South Walkway
            CreateBox(mall.transform, "Walkway_L2_South", new Vector3(0f, 4.4f, -32f), new Vector3(mw - 4f, 0.3f, 18f), marbleMat, true);
            // West Walkway
            CreateBox(mall.transform, "Walkway_L2_West", new Vector3(-28f, 4.4f, 0f), new Vector3(16f, 0.3f, 48f), marbleMat, true);
            // East Walkway
            CreateBox(mall.transform, "Walkway_L2_East", new Vector3(28f, 4.4f, 0f), new Vector3(16f, 0.3f, 48f), marbleMat, true);

            // Safety Railings along second floor mezzanine
            CreateBox(mall.transform, "Railing_L2_Inner_W", new Vector3(-20f, 4.9f, 0f), new Vector3(0.2f, 0.9f, 48f), metalMat, true);
            CreateBox(mall.transform, "Railing_L2_Inner_E", new Vector3(20f, 4.9f, 0f), new Vector3(0.2f, 0.9f, 48f), metalMat, true);

            // Escalator / Inclined Concrete Ramps connecting L1 to L2 (Slope ~22° <= 26°)
            GameObject rampWest = CreateBox(mall.transform, "Escalator_Ramp_West", new Vector3(-8f, 2.2f, 0f), new Vector3(4.2f, 0.3f, 11.5f), metalMat, true);
            rampWest.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);

            GameObject rampEast = CreateBox(mall.transform, "Escalator_Ramp_East", new Vector3(8f, 2.2f, 0f), new Vector3(4.2f, 0.3f, 11.5f), metalMat, true);
            rampEast.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            // Exterior Mall Walls & Roof
            CreateBox(mall.transform, "Mall_Wall_West", new Vector3(-mw * 0.5f, 4.5f, 0f), new Vector3(0.8f, 9.0f, ml), concreteMat, true);
            CreateBox(mall.transform, "Mall_Wall_East", new Vector3(mw * 0.5f, 4.5f, 0f), new Vector3(0.8f, 9.0f, ml), concreteMat, true);
            CreateBox(mall.transform, "Mall_Wall_South", new Vector3(0f, 4.5f, -ml * 0.5f), new Vector3(mw, 9.0f, 0.8f), concreteMat, true);
            CreateBox(mall.transform, "Mall_Roof", new Vector3(0f, 9.0f, 0f), new Vector3(mw, 0.6f, ml), concreteMat, true);

            // Glass Atrium Entrance at North Facade (Z = +ml * 0.5f)
            CreateBox(mall.transform, "Mall_Atrium_Glass", new Vector3(0f, 4.5f, ml * 0.5f), new Vector3(32f, 9.0f, 0.4f), glassMat, false);

            // 4 Distinct Storefronts
            // Store 1: Sporting Goods / Hardware (Ground Floor West)
            CreateMallStore(mall.transform, "Store_SportingGoods_L1", new Vector3(-28f, 0.1f, -24f), new Vector3(24f, 4.2f, 26f),
                storePartitionMat, "Av ve Hırdavat Malzemeleri", axe, crowbar, scrap, ammoShells);

            // Store 2: Electronics & Tech (Ground Floor East)
            CreateMallStore(mall.transform, "Store_Electronics_L1", new Vector3(28f, 0.1f, -24f), new Vector3(24f, 4.2f, 26f),
                storePartitionMat, "Teknoloji & Elektronik", battery, scrap, alcohol, null);

            // Store 3: Mega Pharmacy (Second Floor West)
            CreateMallStore(mall.transform, "Store_MegaPharmacy_L2", new Vector3(-28f, 4.4f, -24f), new Vector3(24f, 4.2f, 26f),
                storePartitionMat, "Mega Eczane & İlaç Deposu", medkit, bandage, alcohol, null);

            // Store 4: Food Court (Second Floor East)
            CreateMallStore(mall.transform, "Store_FoodCourt_L2", new Vector3(28f, 4.4f, -24f), new Vector3(24f, 4.2f, 26f),
                storePartitionMat, "Lezzet Meydanı & Restoranlar", cannedFood, waterBottle, cannedFood, waterBottle);

            // Mall Basement / Utility Room with Backup Generator
            CreateMallUtilityGenerator(mall.transform, "Mall_Backup_Generator", new Vector3(0f, 0.1f, -38f), metalMat, scrap, battery);
        }

        // =========================================================================
        // SECTOR 4: SECTOR_-1_-1_SOUTHWEST (FLOODED RESIDENTIAL SUBURB & BOARDWALKS)
        // World bounds: X: -360..-120, Z: -360..-120. Local center is (-240, 0, -240).
        // =========================================================================
        private static void BuildSectorMinus1Minus1SouthWest(Transform sectorRoot, Material waterMat, Material woodMat,
            Material brickMat, Material concreteMat, ItemData cannedFood, ItemData waterBottle,
            ItemData bandage, ItemData alcohol, ItemData ammo9mm, ItemData scrap, ItemData battery)
        {
            Transform oldLandmark = sectorRoot.Find("Landmark_FloodedSuburbs");
            if (oldLandmark != null) UnityEngine.Object.DestroyImmediate(oldLandmark.gameObject);

            Transform existing = sectorRoot.Find("Phase3_FloodedSuburbsAndBoardwalk");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase3_FloodedSuburbsAndBoardwalk");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Shallow Floodwater Sheet & Movement Modifier (Y = 0.22m)
            GameObject flood = new GameObject("Flooded_Streets_Basin");
            flood.transform.SetParent(root.transform, false);
            flood.transform.localPosition = Vector3.zero;

            GameObject waterSheet = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterSheet.name = "Flood_Water_Plane";
            waterSheet.transform.SetParent(flood.transform, false);
            waterSheet.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            waterSheet.transform.localScale = new Vector3(15f, 1f, 15f); // 150m x 150m
            waterSheet.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Trigger Box for FloodwaterMovementModifier (-18% speed cut + water-splash steps)
            GameObject triggerObj = new GameObject("Floodwater_Movement_Trigger");
            triggerObj.transform.SetParent(flood.transform, false);
            triggerObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            BoxCollider bc = triggerObj.AddComponent<BoxCollider>();
            bc.size = new Vector3(150f, 0.6f, 150f);
            bc.isTrigger = true;
            triggerObj.AddComponent<FloodwaterMovementModifier>();

            // 2. Elevated Wooden Boardwalk Network (Y = 0.38m, above water layer)
            GameObject boardwalks = new GameObject("Elevated_Boardwalk_Network");
            boardwalks.transform.SetParent(root.transform, false);

            // Main East-West Boardwalk Spine
            CreateBox(boardwalks.transform, "Boardwalk_Spine_EW", new Vector3(0f, 0.38f, 0f), new Vector3(120f, 0.25f, 3.8f), woodMat, true);
            // Main North-South Boardwalk Spine
            CreateBox(boardwalks.transform, "Boardwalk_Spine_NS", new Vector3(0f, 0.38f, 0f), new Vector3(3.8f, 0.25f, 120f), woodMat, true);

            // Feeder Boardwalks to 5 house porches
            CreateBox(boardwalks.transform, "Feeder_House_1", new Vector3(-20f, 0.38f, 30f), new Vector3(3.4f, 0.25f, 40f), woodMat, true);
            CreateBox(boardwalks.transform, "Feeder_House_2", new Vector3(20f, 0.38f, 30f), new Vector3(3.4f, 0.25f, 40f), woodMat, true);
            CreateBox(boardwalks.transform, "Feeder_House_3", new Vector3(-25f, 0.38f, -30f), new Vector3(3.4f, 0.25f, 40f), woodMat, true);
            CreateBox(boardwalks.transform, "Feeder_House_4", new Vector3(25f, 0.38f, -30f), new Vector3(3.4f, 0.25f, 40f), woodMat, true);
            CreateBox(boardwalks.transform, "Feeder_House_5", new Vector3(0f, 0.38f, -60f), new Vector3(3.4f, 0.25f, 30f), woodMat, true);

            // Boardwalk support pilings
            for (float bx = -50f; bx <= 50f; bx += 20f)
            {
                CreateBox(boardwalks.transform, $"Piling_EW_{bx}", new Vector3(bx, 0.15f, 0f), new Vector3(0.35f, 0.5f, 3.6f), woodMat, true);
                CreateBox(boardwalks.transform, $"Piling_NS_{bx}", new Vector3(0f, 0.15f, bx), new Vector3(3.6f, 0.5f, 0.35f), woodMat, true);
            }

            // 3. Five Two-Story Half-Submerged Suburban Houses
            // House 1 (Northwest): Local (-35, 0, 30) -> World (-275, 0, -210)
            CreateSubmergedHouse(root.transform, "Submerged_House_1", new Vector3(-35f, 0f, 30f), 0f, brickMat, woodMat, concreteMat,
                cannedFood, waterBottle, bandage, ammo9mm);

            // House 2 (Northeast): Local (35, 0, 30) -> World (-205, 0, -210)
            CreateSubmergedHouse(root.transform, "Submerged_House_2", new Vector3(35f, 0f, 30f), 180f, brickMat, woodMat, concreteMat,
                cannedFood, waterBottle, alcohol, scrap);

            // House 3 (Mid-West): Local (-40, 0, -30) -> World (-280, 0, -270)
            CreateSubmergedHouse(root.transform, "Submerged_House_3", new Vector3(-40f, 0f, -30f), 0f, brickMat, woodMat, concreteMat,
                cannedFood, waterBottle, bandage, battery);

            // House 4 (Mid-East): Local (40, 0, -30) -> World (-200, 0, -270)
            CreateSubmergedHouse(root.transform, "Submerged_House_4", new Vector3(40f, 0f, -30f), 180f, brickMat, woodMat, concreteMat,
                cannedFood, waterBottle, ammo9mm, scrap);

            // House 5 (South Cul-de-sac): Local (0, 0, -75) -> World (-240, 0, -315)
            CreateSubmergedHouse(root.transform, "Submerged_House_5", new Vector3(0f, 0f, -75f), 90f, brickMat, woodMat, concreteMat,
                cannedFood, waterBottle, alcohol, ammo9mm);

            // 4. Half-Submerged Stranded Vehicles
            GameObject vehParent = new GameObject("Stranded_Submerged_Vehicles");
            vehParent.transform.SetParent(root.transform, false);

            CreateSubmergedSedan(vehParent.transform, "Submerged_Sedan_1", new Vector3(-12f, 0.1f, 15f), 25f, 12f, concreteMat);
            CreateSubmergedSedan(vehParent.transform, "Submerged_Sedan_2", new Vector3(14f, 0.1f, -12f), -35f, -8f, concreteMat);
            CreateSubmergedDeliveryVan(vehParent.transform, "Submerged_Van_1", new Vector3(-16f, 0.1f, -45f), 15f, 10f, concreteMat);
            CreateSubmergedDeliveryVan(vehParent.transform, "Submerged_Van_2", new Vector3(18f, 0.1f, 48f), -65f, -12f, concreteMat);
        }

        // =========================================================================
        // AUTONOMOUS SCAVENGER SQUADS (MALL & FOREST PATROLS)
        // =========================================================================
        private static void BuildScavengerSquads()
        {
            GameObject squadRoot = GameObject.Find("--- SCAVENGER SQUADS ---");
            if (squadRoot == null)
            {
                squadRoot = new GameObject("--- SCAVENGER SQUADS ---");
            }

            // Squad 5: Mall Plaza Patrol (Arda & Deniz)
            Transform squadMall = squadRoot.transform.Find("Scavenger_Squad_Mall");
            if (squadMall == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_Mall");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(0f, 0.1f, -150f);

                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();
                SurvivorNPC_AI point = CreateSurvivor("Scavenger_Point_Arda", sqObj.transform.position, NPCArchetype.Armed, "arda", "Arda");
                SurvivorNPC_AI carrier = CreateSurvivor("Scavenger_Carrier_Deniz", sqObj.transform.position + Vector3.right * 1.5f, NPCArchetype.Civilian, "deniz_mall", "Deniz");
                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Phase3DistrictBuilder] Created Scavenger_Squad_Mall with Arda & Deniz.");
            }

            // Squad 6: Forest Trail Patrol (Tolga & Naz)
            Transform squadForest = squadRoot.transform.Find("Scavenger_Squad_Forest");
            if (squadForest == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_Forest");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(-220f, 0.1f, 0f);

                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();
                SurvivorNPC_AI point = CreateSurvivor("Scavenger_Point_Tolga", sqObj.transform.position, NPCArchetype.Armed, "tolga", "Tolga");
                SurvivorNPC_AI carrier = CreateSurvivor("Scavenger_Carrier_Naz", sqObj.transform.position + Vector3.forward * 1.5f, NPCArchetype.Civilian, "naz", "Naz");
                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Phase3DistrictBuilder] Created Scavenger_Squad_Forest with Tolga & Naz.");
            }
        }

        private static SurvivorNPC_AI CreateSurvivor(string rootName, Vector3 pos, NPCArchetype archetype, string npcId, string displayName)
        {
            GameObject root = new GameObject(rootName);
            root.transform.position = pos;
            root.tag = "NPC";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.0f;
            agent.stoppingDistance = 1.5f;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.9f, 0f);

            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual_Model";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());

            SurvivorNPC_AI ai = root.AddComponent<SurvivorNPC_AI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("npcId").stringValue = npcId;
            soAi.FindProperty("npcDisplayName").stringValue = displayName;
            soAi.FindProperty("archetype").enumValueIndex = (int)archetype;
            soAi.ApplyModifiedProperties();

            return ai;
        }

        // =========================================================================
        // ATMOSPHERE PASS (IVY & COBWEBS)
        // =========================================================================
        private static void GenerateAtmosphereDetails(Transform west, Transform northWest, Transform south, Transform southWest)
        {
            // Ivy patches on cabin walls, mall atrium pillars, shed walls
            var westIvy = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(-210f, 0.2f, 35f), Vector3.forward, 3.5f, 2.8f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(west, "Atmosphere_Ivy_West", westIvy);

            var nwIvy = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(-185f, 0.2f, 220f), Vector3.back, 4.0f, 3.2f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(northWest, "Atmosphere_Ivy_NorthWest", nwIvy);

            var southIvy = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(0f, 0.2f, -250f), Vector3.forward, 7.0f, 4.0f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(south, "Atmosphere_Ivy_South", southIvy);

            var swIvy = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(-275f, 0.2f, -210f), Vector3.back, 4.5f, 3.5f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(southWest, "Atmosphere_Ivy_SouthWest", swIvy);

            // Cobweb patches inside subway tunnel, shed, and cabins
            var cobwebs = new (Vector3 pos, Vector2 size, Vector3 rot)[]
            {
                (new Vector3(-210f, 2.8f, 35f), new Vector2(2f, 2f), new Vector3(0f, 45f, 0f)),
                (new Vector3(-185f, 2.6f, 220f), new Vector2(2.5f, 2f), new Vector3(0f, -45f, 0f)),
                (new Vector3(0f, -3.8f, -210f), new Vector2(3f, 2f), new Vector3(0f, 90f, 0f)),
                (new Vector3(0f, 4.2f, -280f), new Vector2(2.5f, 2.5f), new Vector3(0f, 0f, 0f))
            };
            SpiderwebGenerator.GenerateCobwebPatches(south, "Atmosphere_Cobwebs_South", cobwebs);
        }

        // =========================================================================
        // ARCHITECTURAL HELPER METHODS
        // =========================================================================
        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool navStatic)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (navStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
            return go;
        }

        private static void CreateProceduralTree(Transform parent, string name, Vector3 localPos, bool isPine,
            Material trunkMat, Material foliageMat)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;

            if (isPine)
            {
                // Pine Tree: tall straight trunk with 3 conical tiered canopies
                CreateBox(tree.transform, "Trunk", new Vector3(0f, 3.5f, 0f), new Vector3(0.7f, 7.0f, 0.7f), trunkMat, true);
                CreateBox(tree.transform, "Tier_Bottom", new Vector3(0f, 4.5f, 0f), new Vector3(4.8f, 2.2f, 4.8f), foliageMat, false);
                CreateBox(tree.transform, "Tier_Mid", new Vector3(0f, 6.2f, 0f), new Vector3(3.6f, 2.0f, 3.6f), foliageMat, false);
                CreateBox(tree.transform, "Tier_Top", new Vector3(0f, 7.8f, 0f), new Vector3(2.2f, 1.8f, 2.2f), foliageMat, false);
            }
            else
            {
                // Oak Tree: thick trunk with 2 angled branch stems and wide canopy
                CreateBox(tree.transform, "Trunk", new Vector3(0f, 2.8f, 0f), new Vector3(0.9f, 5.6f, 0.9f), trunkMat, true);
                CreateBox(tree.transform, "Canopy_Main", new Vector3(0f, 5.8f, 0f), new Vector3(5.8f, 3.4f, 5.8f), foliageMat, false);
                CreateBox(tree.transform, "Canopy_Top", new Vector3(0f, 7.2f, 0f), new Vector3(3.8f, 2.2f, 3.8f), foliageMat, false);
            }
        }

        private static void CreateFallenTrunk(Transform parent, string name, Vector3 localPos, float yaw, Material trunkMat)
        {
            GameObject trunk = CreateBox(parent, name, localPos, new Vector3(0.85f, 0.75f, 8.5f), trunkMat, true);
            trunk.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void CreateStealthBushHidingSpot(Transform parent, string name, Vector3 localPos, Material foliageMat)
        {
            GameObject bush = new GameObject(name);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = localPos;

            // Visual leaf clusters
            CreateBox(bush.transform, "Foliage_Cluster_A", new Vector3(0f, 0.75f, 0f), new Vector3(2.6f, 1.5f, 2.4f), foliageMat, false);
            CreateBox(bush.transform, "Foliage_Cluster_B", new Vector3(0.4f, 0.65f, 0.3f), new Vector3(2.2f, 1.3f, 2.2f), foliageMat, false);

            // Trigger BoxCollider for HidingSpot
            BoxCollider col = bush.AddComponent<BoxCollider>();
            col.size = new Vector3(2.8f, 1.8f, 2.6f);
            col.center = new Vector3(0f, 0.9f, 0f);
            col.isTrigger = true;

            // HidingSpot component configured as FoliageBush
            HidingSpot hs = bush.AddComponent<HidingSpot>();
            hs.ConfigureSpotType(HidingSpotType.FoliageBush);
        }

        private static void CreateParkMaintenanceShed(Transform parent, string name, Vector3 localPos, Material brickMat,
            Material concreteMat, ItemData scrap, ItemData crowbar, ItemData battery)
        {
            GameObject shed = new GameObject(name);
            shed.transform.SetParent(parent, false);
            shed.transform.localPosition = localPos;

            float w = 12f;
            float h = 4.2f;
            float l = 9f;

            // Floor & Roof
            CreateBox(shed.transform, "Floor", new Vector3(0f, 0.15f, 0f), new Vector3(w, 0.3f, l), concreteMat, true);
            CreateBox(shed.transform, "Roof", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.8f, 0.35f, l + 0.8f), concreteMat, true);

            // Walls (North, East, South)
            CreateBox(shed.transform, "Wall_North", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, 0.35f), brickMat, true);
            CreateBox(shed.transform, "Wall_East", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, l), brickMat, true);
            CreateBox(shed.transform, "Wall_South", new Vector3(0f, h * 0.5f, -l * 0.5f), new Vector3(w, h, 0.35f), brickMat, true);

            // West wall with door opening
            CreateBox(shed.transform, "Wall_West_L", new Vector3(-w * 0.5f, h * 0.5f, -2.5f), new Vector3(0.35f, h, 4f), brickMat, true);
            CreateBox(shed.transform, "Wall_West_R", new Vector3(-w * 0.5f, h * 0.5f, 2.5f), new Vector3(0.35f, h, 4f), brickMat, true);
            CreateBox(shed.transform, "Wall_West_Top", new Vector3(-w * 0.5f, 3.4f, 0f), new Vector3(0.35f, 1.6f, 2.0f), brickMat, true);

            // Entrance Door
            GameObject door = CreateBox(shed.transform, "Shed_Door", new Vector3(-w * 0.5f, 1.2f, 0f), new Vector3(0.15f, 2.4f, 1.8f), null, false);
            door.AddComponent<InteractiveDoor>();

            // Window on North wall
            GameObject win = CreateBox(shed.transform, "Shed_Window", new Vector3(3f, 1.8f, l * 0.5f), new Vector3(2.0f, 1.6f, 0.15f), null, false);
            win.AddComponent<WindowInteractable>();

            // Inside Workbench with loot
            GameObject bench = CreateBox(shed.transform, "Maintenance_Workbench", new Vector3(4f, 0.8f, 0f), new Vector3(1.2f, 1.0f, 3.5f), null, true);
            bench.AddComponent<FurnitureInteractable>();
            LootContainer lc = bench.AddComponent<LootContainer>();
            if (scrap != null) lc.AddItem(scrap, 3, 1f);
            if (crowbar != null) lc.AddItem(crowbar, 1, 0.85f);
            if (battery != null) lc.AddItem(battery, 2, 0.75f);
        }

        private static void CreateRangerPickupTruck(Transform parent, string name, Vector3 localPos, float yaw, Material mat, ItemData scrap)
        {
            GameObject truck = new GameObject(name);
            truck.transform.SetParent(parent, false);
            truck.transform.localPosition = localPos;
            truck.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(truck.transform, "Chassis", new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 0.8f, 5.8f), mat, true);
            CreateBox(truck.transform, "Cab", new Vector3(0f, 1.6f, 0.8f), new Vector3(2.3f, 1.4f, 2.2f), mat, true);
            CreateBox(truck.transform, "CargoBed", new Vector3(0f, 1.2f, -1.6f), new Vector3(2.3f, 1.0f, 2.6f), mat, true);

            BoxCollider bc = truck.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.6f, 2.2f, 6.2f);
            bc.center = new Vector3(0f, 1.1f, 0f);
            truck.AddComponent<VehicleInteractable>();
        }

        private static void CreateHikerCamp(Transform parent, string name, Vector3 localPos, Material tentMat, Material woodMat,
            ItemData food, ItemData water, ItemData bandage, ItemData ammo)
        {
            GameObject camp = new GameObject(name);
            camp.transform.SetParent(parent, false);
            camp.transform.localPosition = localPos;

            // 3 Hiker Tents
            CreateHikerTent(camp.transform, "Hiker_Tent_1", new Vector3(-4f, 0f, 3f), 25f, tentMat);
            CreateHikerTent(camp.transform, "Hiker_Tent_2", new Vector3(4f, 0f, 2f), -35f, tentMat);
            CreateHikerTent(camp.transform, "Hiker_Tent_3", new Vector3(0f, 0f, -5f), 180f, tentMat);

            // Campfire Pit
            CreateBox(camp.transform, "Campfire_Logs", new Vector3(0f, 0.2f, 0f), new Vector3(1.4f, 0.4f, 1.4f), woodMat, true);

            // Supply Crate with food & medical loot
            GameObject crate = CreateBox(camp.transform, "Hiker_Supply_Crate", new Vector3(1.5f, 0.4f, -3f), new Vector3(1.2f, 0.8f, 1.0f), woodMat, true);
            crate.AddComponent<FurnitureInteractable>();
            LootContainer lc = crate.AddComponent<LootContainer>();
            if (food != null) lc.AddItem(food, 3, 1f);
            if (water != null) lc.AddItem(water, 3, 1f);
            if (bandage != null) lc.AddItem(bandage, 2, 0.85f);
            if (ammo != null) lc.AddItem(ammo, 12, 0.75f);
        }

        private static void CreateHikerTent(Transform parent, string name, Vector3 localPos, float yaw, Material mat)
        {
            GameObject tent = new GameObject(name);
            tent.transform.SetParent(parent, false);
            tent.transform.localPosition = localPos;
            tent.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(tent.transform, "Tent_Body", new Vector3(0f, 0.9f, 0f), new Vector3(2.6f, 1.8f, 3.2f), mat, true);
        }

        private static void CreateEnterableCabin(Transform parent, string name, Vector3 localPos, float yaw, Material woodMat,
            Material brickMat, ItemData food, ItemData water, ItemData bandage, ItemData extraLoot)
        {
            GameObject cabin = new GameObject(name);
            cabin.transform.SetParent(parent, false);
            cabin.transform.localPosition = localPos;
            cabin.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 11f;
            float h = 4.4f;
            float l = 9f;

            // Porch & Floor
            CreateBox(cabin.transform, "Front_Porch", new Vector3(0f, 0.25f, -l * 0.5f - 1.5f), new Vector3(w, 0.5f, 3.0f), woodMat, true);
            CreateBox(cabin.transform, "Interior_Floor", new Vector3(0f, 0.25f, 0f), new Vector3(w, 0.5f, l), woodMat, true);

            // Walls (North, East, West)
            CreateBox(cabin.transform, "Wall_Back", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, 0.35f), woodMat, true);
            CreateBox(cabin.transform, "Wall_East", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, l), woodMat, true);
            CreateBox(cabin.transform, "Wall_West", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, l), woodMat, true);

            // South Wall with Door & Window
            CreateBox(cabin.transform, "Wall_Front_L", new Vector3(-3.5f, h * 0.5f, -l * 0.5f), new Vector3(4f, h, 0.35f), woodMat, true);
            CreateBox(cabin.transform, "Wall_Front_R", new Vector3(3.5f, h * 0.5f, -l * 0.5f), new Vector3(4f, h, 0.35f), woodMat, true);
            CreateBox(cabin.transform, "Wall_Front_Top", new Vector3(0f, 3.4f, -l * 0.5f), new Vector3(3.0f, 1.8f, 0.35f), woodMat, true);

            // Entrance Door
            GameObject door = CreateBox(cabin.transform, "Cabin_Door", new Vector3(0f, 1.3f, -l * 0.5f), new Vector3(1.6f, 2.5f, 0.15f), null, false);
            door.AddComponent<InteractiveDoor>();

            // Window
            GameObject win = CreateBox(cabin.transform, "Cabin_Window", new Vector3(-3.5f, 1.8f, -l * 0.5f), new Vector3(1.8f, 1.5f, 0.15f), null, false);
            win.AddComponent<WindowInteractable>();

            // Pitched Roof
            CreateBox(cabin.transform, "Roof_Ridge", new Vector3(0f, h + 0.3f, 0f), new Vector3(w + 1f, 0.5f, l + 1f), woodMat, true);

            // Stone Fireplace
            CreateBox(cabin.transform, "Stone_Fireplace", new Vector3(w * 0.5f - 1.0f, 1.6f, 1.5f), new Vector3(1.6f, 3.2f, 2.2f), brickMat, true);

            // Bed rest point
            GameObject bed = CreateBox(cabin.transform, "Cabin_Bed", new Vector3(-w * 0.5f + 1.8f, 0.7f, 2.2f), new Vector3(2.4f, 0.8f, 3.2f), woodMat, true);
            bed.AddComponent<UtilityFixtureInteractable>();

            // Kitchen Cabinet with loot
            GameObject cabinet = CreateBox(cabin.transform, "Kitchen_Cabinet", new Vector3(-w * 0.5f + 1.5f, 1.0f, -2.0f), new Vector3(1.4f, 1.8f, 2.0f), woodMat, true);
            cabinet.AddComponent<FurnitureInteractable>();
            LootContainer lc = cabinet.AddComponent<LootContainer>();
            if (food != null) lc.AddItem(food, 2, 1f);
            if (water != null) lc.AddItem(water, 2, 1f);
            if (bandage != null) lc.AddItem(bandage, 2, 0.85f);
            if (extraLoot != null) lc.AddItem(extraLoot, 1, 0.75f);

            // Amber Lantern Light
            GameObject lantern = new GameObject("Cabin_Lantern_Light");
            lantern.transform.SetParent(cabin.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            Light lgt = lantern.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1.0f, 0.75f, 0.45f);
            lgt.range = 14f;
            lgt.intensity = 2.0f;
        }

        private static void CreateFireLookoutTower(Transform parent, string name, Vector3 localPos, Material woodMat,
            Material concreteMat, ItemData ammo1, ItemData ammo2, ItemData medkit, ItemData axe)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = localPos;

            // 4 Heavy Timber Legs (5.5m high)
            CreateBox(tower.transform, "Leg_A", new Vector3(-2.5f, 2.75f, -2.5f), new Vector3(0.5f, 5.5f, 0.5f), woodMat, true);
            CreateBox(tower.transform, "Leg_B", new Vector3(2.5f, 2.75f, -2.5f), new Vector3(0.5f, 5.5f, 0.5f), woodMat, true);
            CreateBox(tower.transform, "Leg_C", new Vector3(-2.5f, 2.75f, 2.5f), new Vector3(0.5f, 5.5f, 0.5f), woodMat, true);
            CreateBox(tower.transform, "Leg_D", new Vector3(2.5f, 2.75f, 2.5f), new Vector3(0.5f, 5.5f, 0.5f), woodMat, true);

            // Elevated Observation Deck (Y = 5.5m)
            CreateBox(tower.transform, "Observation_Floor", new Vector3(0f, 5.5f, 0f), new Vector3(6.5f, 0.35f, 6.5f), woodMat, true);
            CreateBox(tower.transform, "Observation_Roof", new Vector3(0f, 8.2f, 0f), new Vector3(7.2f, 0.4f, 7.2f), woodMat, true);
            CreateBox(tower.transform, "Perimeter_Parapet", new Vector3(0f, 6.0f, 0f), new Vector3(6.5f, 0.9f, 6.5f), woodMat, true);

            // Access Staircase / Ramp (Slope ~22° <= 28°)
            GameObject stairs = CreateBox(tower.transform, "Tower_Stairs", new Vector3(0f, 2.75f, -5.8f), new Vector3(1.8f, 0.3f, 7.2f), woodMat, true);
            stairs.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);

            // High-Tier Sniper / Medical Crate on observation deck
            GameObject crate = CreateBox(tower.transform, "Ranger_Watch_Crate", new Vector3(1.5f, 6.0f, 1.5f), new Vector3(1.4f, 0.8f, 1.2f), woodMat, true);
            crate.AddComponent<FurnitureInteractable>();
            LootContainer lc = crate.AddComponent<LootContainer>();
            if (ammo1 != null) lc.AddItem(ammo1, 20, 1f);
            if (ammo2 != null) lc.AddItem(ammo2, 10, 1f);
            if (medkit != null) lc.AddItem(medkit, 2, 0.9f);
            if (axe != null) lc.AddItem(axe, 1, 0.75f);
        }

        private static void CreateSubwayCar(Transform parent, string name, Vector3 localPos, float yaw, Material mat,
            ItemData scrap, ItemData loot2, ItemData ammo)
        {
            GameObject car = new GameObject(name);
            car.transform.SetParent(parent, false);
            car.transform.localPosition = localPos;
            car.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 3.6f;
            float h = 3.4f;
            float l = 28f;

            // Floor & Roof
            CreateBox(car.transform, "Floor", new Vector3(0f, 0.2f, 0f), new Vector3(w, 0.3f, l), mat, true);
            CreateBox(car.transform, "Roof", new Vector3(0f, h + 0.2f, 0f), new Vector3(w, 0.3f, l), mat, true);

            // Ends
            CreateBox(car.transform, "End_North", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, 0.3f), mat, true);
            CreateBox(car.transform, "End_South", new Vector3(0f, h * 0.5f, -l * 0.5f), new Vector3(w, h, 0.3f), mat, true);

            // Walls with open sliding doorways for run-through passage
            CreateBox(car.transform, "Wall_W_South", new Vector3(-w * 0.5f, h * 0.5f, -8f), new Vector3(0.3f, h, 10f), mat, true);
            CreateBox(car.transform, "Wall_W_North", new Vector3(-w * 0.5f, h * 0.5f, 8f), new Vector3(0.3f, h, 10f), mat, true);
            CreateBox(car.transform, "Wall_E_South", new Vector3(w * 0.5f, h * 0.5f, -8f), new Vector3(0.3f, h, 10f), mat, true);
            CreateBox(car.transform, "Wall_E_North", new Vector3(w * 0.5f, h * 0.5f, 8f), new Vector3(0.3f, h, 10f), mat, true);

            // Interior Luggage / Crates
            GameObject luggage = CreateBox(car.transform, "Train_Luggage_Crate", new Vector3(0f, 0.7f, 0f), new Vector3(1.2f, 0.8f, 1.2f), null, true);
            luggage.AddComponent<FurnitureInteractable>();
            LootContainer lc = luggage.AddComponent<LootContainer>();
            if (scrap != null) lc.AddItem(scrap, 3, 1f);
            if (loot2 != null) lc.AddItem(loot2, 1, 0.8f);
            if (ammo != null) lc.AddItem(ammo, 12, 0.8f);
        }

        private static void CreateSubwayServiceGate(Transform parent, string name, Vector3 localPos, Quaternion rot)
        {
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = name;
            gate.transform.SetParent(parent, false);
            gate.transform.localPosition = localPos;
            gate.transform.localRotation = rot;
            gate.transform.localScale = new Vector3(0.25f, 3.2f, 2.6f);

            gate.AddComponent<SubwayServiceTunnelGate>();
        }

        private static void CreateFluorescentLight(Transform parent, string name, Vector3 localPos, Color col)
        {
            GameObject fl = new GameObject(name);
            fl.transform.SetParent(parent, false);
            fl.transform.localPosition = localPos;

            Light l = fl.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 22f;
            l.intensity = 2.5f;
        }

        private static void CreateMallStore(Transform parent, string name, Vector3 localPos, Vector3 size, Material partMat,
            string storeTitle, ItemData loot1, ItemData loot2, ItemData loot3, ItemData loot4)
        {
            GameObject store = new GameObject(name);
            store.transform.SetParent(parent, false);
            store.transform.localPosition = localPos;

            float w = size.x;
            float h = size.y;
            float l = size.z;

            // Partition Walls (Back, Left, Right)
            CreateBox(store.transform, "Wall_Back", new Vector3(0f, h * 0.5f, -l * 0.5f), new Vector3(w, h, 0.4f), partMat, true);
            CreateBox(store.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, l), partMat, true);
            CreateBox(store.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, l), partMat, true);

            // Front Entrance Shutter
            GameObject shutter = CreateBox(store.transform, "Security_Shutter", new Vector3(0f, 1.3f, l * 0.5f), new Vector3(4.2f, 2.6f, 0.2f), null, false);
            shutter.AddComponent<InteractiveDoor>();

            // Store Shelving with loot
            GameObject shelf = CreateBox(store.transform, "Store_Shelving", new Vector3(-w * 0.3f, 1.2f, 0f), new Vector3(1.2f, 2.4f, 8f), null, true);
            shelf.AddComponent<FurnitureInteractable>();
            LootContainer lc = shelf.AddComponent<LootContainer>();
            if (loot1 != null) lc.AddItem(loot1, 2, 1f);
            if (loot2 != null) lc.AddItem(loot2, 2, 0.85f);
            if (loot3 != null) lc.AddItem(loot3, 2, 0.85f);
            if (loot4 != null) lc.AddItem(loot4, 2, 0.75f);
        }

        private static void CreateMallUtilityGenerator(Transform parent, string name, Vector3 localPos, Material mat,
            ItemData scrap, ItemData battery)
        {
            GameObject gen = new GameObject(name);
            gen.transform.SetParent(parent, false);
            gen.transform.localPosition = localPos;

            CreateBox(gen.transform, "Gen_Base", new Vector3(0f, 0.9f, 0f), new Vector3(3.2f, 1.8f, 2.4f), mat, true);

            BoxCollider bc = gen.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.4f, 2.0f, 2.6f);
            bc.center = new Vector3(0f, 0.9f, 0f);

            gen.AddComponent<PowerGridInteractable>();

            // Utility Locker
            GameObject locker = CreateBox(gen.transform, "Utility_Locker", new Vector3(3.5f, 1.1f, 0f), new Vector3(1.2f, 2.2f, 0.8f), null, true);
            locker.AddComponent<FurnitureInteractable>();
            LootContainer lc = locker.AddComponent<LootContainer>();
            if (scrap != null) lc.AddItem(scrap, 3, 1f);
            if (battery != null) lc.AddItem(battery, 2, 1f);
        }

        private static void CreateSubmergedHouse(Transform parent, string name, Vector3 localPos, float yaw, Material brickMat,
            Material woodMat, Material concreteMat, ItemData food, ItemData water, ItemData med, ItemData ammo)
        {
            GameObject house = new GameObject(name);
            house.transform.SetParent(parent, false);
            house.transform.localPosition = localPos;
            house.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 12f;
            float h = 6.2f;
            float l = 10f;

            // Ground Floor (Y = 0m, submerged in 0.22m water) & Second Floor (Y = 3.2m, dry)
            CreateBox(house.transform, "Floor_L1", new Vector3(0f, 0.1f, 0f), new Vector3(w, 0.2f, l), concreteMat, true);
            CreateBox(house.transform, "Floor_L2", new Vector3(0f, 3.2f, 0f), new Vector3(w, 0.3f, l), woodMat, true);
            CreateBox(house.transform, "Roof", new Vector3(0f, h + 0.2f, 0f), new Vector3(w + 0.8f, 0.4f, l + 0.8f), woodMat, true);

            // Water-stained lower brick walls & Upper walls
            CreateBox(house.transform, "Wall_Back", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, 0.4f), brickMat, true);
            CreateBox(house.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, l), brickMat, true);
            CreateBox(house.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, l), brickMat, true);

            // Front Entrance Porch connecting to elevated boardwalk
            CreateBox(house.transform, "Front_Porch", new Vector3(0f, 0.38f, -l * 0.5f - 1.8f), new Vector3(5f, 0.25f, 3.6f), woodMat, true);

            // Front Wall with door opening
            CreateBox(house.transform, "Wall_Front_L", new Vector3(-3.8f, h * 0.5f, -l * 0.5f), new Vector3(4.4f, h, 0.4f), brickMat, true);
            CreateBox(house.transform, "Wall_Front_R", new Vector3(3.8f, h * 0.5f, -l * 0.5f), new Vector3(4.4f, h, 0.4f), brickMat, true);
            CreateBox(house.transform, "Wall_Front_Top", new Vector3(0f, 2.8f, -l * 0.5f), new Vector3(3.2f, 1.6f, 0.4f), brickMat, true);

            // Entrance Door
            GameObject door = CreateBox(house.transform, "Front_Door", new Vector3(0f, 1.2f, -l * 0.5f), new Vector3(1.6f, 2.4f, 0.15f), null, false);
            door.AddComponent<InteractiveDoor>();

            // Interior Staircase to L2 (Slope ~25°)
            GameObject stairs = CreateBox(house.transform, "Interior_Stairs", new Vector3(-3.5f, 1.6f, 1.5f), new Vector3(1.6f, 0.25f, 5.2f), woodMat, true);
            stairs.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);

            // Upper floor dry bedroom furniture & loot
            GameObject bed = CreateBox(house.transform, "Upper_Bed", new Vector3(3.5f, 3.65f, 2.5f), new Vector3(2.2f, 0.7f, 3.2f), woodMat, true);
            bed.AddComponent<UtilityFixtureInteractable>();

            GameObject dresser = CreateBox(house.transform, "Upper_Dresser", new Vector3(3.5f, 4.0f, -2.5f), new Vector3(1.4f, 1.6f, 2.2f), woodMat, true);
            dresser.AddComponent<FurnitureInteractable>();
            LootContainer lc = dresser.AddComponent<LootContainer>();
            if (food != null) lc.AddItem(food, 2, 1f);
            if (water != null) lc.AddItem(water, 2, 1f);
            if (med != null) lc.AddItem(med, 2, 0.85f);
            if (ammo != null) lc.AddItem(ammo, 12, 0.8f);

            // Closet Hiding Spot
            GameObject closet = CreateBox(house.transform, "Upper_Closet_Hiding", new Vector3(-3.5f, 4.2f, -3.2f), new Vector3(1.4f, 2.0f, 1.2f), woodMat, false);
            BoxCollider col = closet.GetComponent<BoxCollider>();
            col.isTrigger = true;
            HidingSpot hs = closet.AddComponent<HidingSpot>();
            hs.ConfigureSpotType(HidingSpotType.Locker);
        }

        private static void CreateSubmergedSedan(Transform parent, string name, Vector3 localPos, float yaw, float roll, Material mat)
        {
            GameObject car = new GameObject(name);
            car.transform.SetParent(parent, false);
            car.transform.localPosition = localPos;
            car.transform.localRotation = Quaternion.Euler(roll, yaw, 0f);

            CreateBox(car.transform, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 0.8f, 4.8f), mat, true);
            CreateBox(car.transform, "Roof", new Vector3(0f, 1.4f, -0.2f), new Vector3(2.0f, 0.9f, 2.6f), mat, true);

            BoxCollider bc = car.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.4f, 1.8f, 5.0f);
            bc.center = new Vector3(0f, 0.9f, 0f);
            car.AddComponent<VehicleInteractable>();
        }

        private static void CreateSubmergedDeliveryVan(Transform parent, string name, Vector3 localPos, float yaw, float pitch, Material mat)
        {
            GameObject van = new GameObject(name);
            van.transform.SetParent(parent, false);
            van.transform.localPosition = localPos;
            van.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            CreateBox(van.transform, "Chassis", new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 0.8f, 6.0f), mat, true);
            CreateBox(van.transform, "Box_Body", new Vector3(0f, 1.8f, -0.6f), new Vector3(2.3f, 1.8f, 4.2f), mat, true);
            CreateBox(van.transform, "Cab", new Vector3(0f, 1.4f, 2.0f), new Vector3(2.2f, 1.4f, 1.8f), mat, true);

            BoxCollider bc = van.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.6f, 2.6f, 6.4f);
            bc.center = new Vector3(0f, 1.3f, 0f);
            van.AddComponent<VehicleInteractable>();
        }
#endif
    }
}
