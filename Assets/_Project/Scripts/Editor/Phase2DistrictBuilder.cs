using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.EditorTools
{
    public static class Phase2DistrictBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Build Phase 2 Districts (North & East Sectors)", false, 16)]
        public static void BuildPhase2Districts()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> PHASE 2 MAP EXPANSION (NORTH HIGHWAY & EAST SECTORS) STARTED <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            if (worldSectorsRoot == null)
            {
                Debug.LogError("[Phase2DistrictBuilder] '--- WORLD SECTORS ---' root not found! Run Phase 1 first.");
                return;
            }

            // Ensure 4 target sectors exist
            Transform northSector = worldSectorsRoot.transform.Find("Sector_0_1_North");
            Transform eastSector = worldSectorsRoot.transform.Find("Sector_1_0_East");
            Transform northEastSector = worldSectorsRoot.transform.Find("Sector_1_1_NorthEast");
            Transform southEastSector = worldSectorsRoot.transform.Find("Sector_1_-1_SouthEast");

            if (northSector == null || eastSector == null || northEastSector == null || southEastSector == null)
            {
                Debug.LogError("[Phase2DistrictBuilder] Target sector roots missing under '--- WORLD SECTORS ---'!");
                return;
            }

            // 1. Prepare PBR Materials
            Material viaductConcreteMat = SceneBuilder.GetOrCreateMaterial("Mat_Viaduct_Concrete", new Color(0.38f, 0.40f, 0.42f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(10f, 10f));
            Material militaryOliveMat = SceneBuilder.GetOrCreateMaterial("Mat_Military_Olive", new Color(0.22f, 0.28f, 0.20f));
            Material sandbagMat = SceneBuilder.GetOrCreateMaterial("Mat_Sandbag_Khaki", new Color(0.48f, 0.42f, 0.32f));
            Material timberWoodMat = SceneBuilder.GetOrCreateMaterial("Mat_Wood_Timber", new Color(0.35f, 0.25f, 0.16f),
                "Assets/_Project/Textures/Tex_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Wood_Normal.png", new Vector2(4f, 4f));
            Material railSteelMat = SceneBuilder.GetOrCreateMaterial("Mat_Rail_Steel", new Color(0.28f, 0.30f, 0.34f));
            Material railGravelMat = SceneBuilder.GetOrCreateMaterial("Mat_Rail_Gravel", new Color(0.16f, 0.16f, 0.16f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(15f, 15f));
            Material trainRustMat = SceneBuilder.GetOrCreateMaterial("Mat_Train_RustMetal", new Color(0.42f, 0.22f, 0.18f));
            Material warehouseSidingMat = SceneBuilder.GetOrCreateMaterial("Mat_Warehouse_Corrugated", new Color(0.32f, 0.35f, 0.38f));
            Material containerCrimsonMat = SceneBuilder.GetOrCreateMaterial("Mat_Container_Crimson", new Color(0.68f, 0.20f, 0.18f));
            Material containerNavyMat = SceneBuilder.GetOrCreateMaterial("Mat_Container_Navy", new Color(0.16f, 0.32f, 0.55f));
            Material containerOchreMat = SceneBuilder.GetOrCreateMaterial("Mat_Container_Ochre", new Color(0.78f, 0.52f, 0.18f));
            Material containerGreenMat = SceneBuilder.GetOrCreateMaterial("Mat_Container_Green", new Color(0.20f, 0.45f, 0.28f));
            Material harborSeawallMat = SceneBuilder.GetOrCreateMaterial("Mat_Harbor_Seawall", new Color(0.30f, 0.32f, 0.35f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(8f, 8f));
            Material harborWaterMat = SceneBuilder.GetOrCreateMaterial("Mat_Harbor_DarkWater", new Color(0.05f, 0.11f, 0.16f, 0.95f), null, null, null, true, 0.90f);
            Material substationBrickMat = SceneBuilder.GetOrCreateMaterial("Mat_Substation_Brick", new Color(0.52f, 0.28f, 0.22f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(6f, 4f));
            Material transformerMetalMat = SceneBuilder.GetOrCreateMaterial("Mat_Transformer_Metal", new Color(0.24f, 0.26f, 0.28f));
            Material explosiveRedMat = SceneBuilder.GetOrCreateMaterial("Mat_Barrel_ExplosiveRed", new Color(0.85f, 0.15f, 0.12f));
            Material yellowHazardMat = SceneBuilder.GetOrCreateMaterial("Mat_Warning_Hazard", new Color(0.95f, 0.75f, 0.10f));

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

            // 2. Build Sector 1: Sector_0_1_North (Collapsed Evacuation Highway & Military FOB)
            BuildSector01North(northSector, viaductConcreteMat, militaryOliveMat, sandbagMat, timberWoodMat, yellowHazardMat,
                ammo9mm, ammoShells, medkit, shiv);

            // 3. Build Sector 2: Sector_1_0_East (Industrial Freight Rail Yard & Steel Warehouses)
            BuildSector10East(eastSector, railSteelMat, railGravelMat, trainRustMat, warehouseSidingMat, timberWoodMat,
                scrap, battery, crowbar, axe, alcohol);

            // 4. Build Sector 3: Sector_1_1_NorthEast (Waterfront Container Harbor & Fuel Depot)
            BuildSector11NorthEast(northEastSector, harborSeawallMat, harborWaterMat, containerCrimsonMat, containerNavyMat,
                containerOchreMat, containerGreenMat, explosiveRedMat, timberWoodMat, scrap, alcohol, ammo9mm);

            // 5. Build Sector 4: Sector_1_-1_SouthEast (Regional Power Substation)
            BuildSector1Minus1SouthEast(southEastSector, substationBrickMat, transformerMetalMat, railGravelMat, viaductConcreteMat,
                scrap, alcohol, battery);

            // 6. Build Autonomous Scavenger Squads (North & East Patrols)
            BuildScavengerSquads();

            // 7. Atmosphere Pass: Generate Ivy & Cobwebs
            GenerateAtmosphereDetails(northSector, eastSector, northEastSector, southEastSector);

            // 8. Update DistrictZombieEcology territorial zones to include Phase 2 outer sectors
            DistrictZombieEcology ecology = UnityEngine.Object.FindAnyObjectByType<DistrictZombieEcology>(FindObjectsInactive.Include);
            if (ecology != null)
            {
                ecology.InitializeZones();
                EditorUtility.SetDirty(ecology);
                Debug.Log($"[Phase2DistrictBuilder] Initialized DistrictZombieEcology with {ecology.Zones.Count} zones.");
            }

            // 9. Re-bake NavMesh across the unified 720x720m world
            Debug.Log("[Phase2DistrictBuilder] Re-baking unified NavMesh across all new districts, ramps, and interiors...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[Phase2DistrictBuilder] NavMesh re-bake completed.");

            // 10. Re-bake Cartography Map
            Debug.Log("[Phase2DistrictBuilder] Re-baking 2048x2048 Tactical City Map with Phase 2 districts...");
            CityCartographyBaker.BakeCityMap();

            // 11. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("==================================================================");
            Debug.Log(">>> PHASE 2 DISTRICT EXPANSION COMPLETE & SCENE SAVED <<<");
            Debug.Log("==================================================================");
        }

        // =========================================================================
        // SECTOR 1: SECTOR_0_1_NORTH (COLLAPSED HIGHWAY VIADUCT & MILITARY FOB)
        // World: X: -120..+120, Z: +120..+360. Local center is (0, 0, 240).
        // =========================================================================
        private static void BuildSector01North(Transform sectorRoot, Material concreteMat, Material militaryMat,
            Material sandbagMat, Material woodMat, Material hazardMat, ItemData ammo9mm, ItemData ammoShells, ItemData medkit, ItemData shiv)
        {
            Transform existing = sectorRoot.Find("Phase2_HighwayAndFOB");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase2_HighwayAndFOB");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Elevated Highway Viaduct Structure (Elevated Y: 6.5m)
            // Center of viaduct deck is along local X = 0, running from Z = -90 (World Z=150) to Z = +105 (World Z=345)
            // Total length: 195m, Width: 16m, Deck Thickness: 1.2m
            GameObject viaductDeck = CreateBox(root.transform, "Viaduct_Deck_Main", new Vector3(0f, 6.5f, 7.5f),
                new Vector3(16f, 1.2f, 195f), concreteMat, true);

            // Safety Parapets / Barriers on East and West sides
            CreateBox(viaductDeck.transform, "Parapet_West", new Vector3(-7.6f, 0.9f, 0f), new Vector3(0.6f, 1.2f, 195f), concreteMat, true);
            CreateBox(viaductDeck.transform, "Parapet_East", new Vector3(7.6f, 0.9f, 0f), new Vector3(0.6f, 1.2f, 195f), concreteMat, true);
            // Center Jersey Divider
            CreateBox(viaductDeck.transform, "Center_Divider", new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.9f, 195f), concreteMat, true);

            // Concrete Piers under the deck (every 25m)
            for (float lz = -80f; lz <= 95f; lz += 25f)
            {
                GameObject pier = CreateBox(root.transform, $"Viaduct_Pier_{lz}", new Vector3(0f, 3.0f, lz), new Vector3(3.2f, 6.0f, 3.2f), concreteMat, true);
                // Crosshead support beam
                CreateBox(pier.transform, "Crosshead_Beam", new Vector3(0f, 2.6f, 0f), new Vector3(15.5f, 1.0f, 3.8f), concreteMat, true);
            }

            // 2. South Collapsed Ramp (Slope <= 28°): from local Z = -116 (World Z=124, Y=0.1) up to local Z = -90 (World Z=150, Y=6.5)
            // Length = 26m, Rise = 6.4m -> Angle = 13.8°!
            GameObject southRamp = CreateBox(root.transform, "Highway_Ramp_South", new Vector3(0f, 3.3f, -103f),
                new Vector3(14f, 0.8f, 26.8f), concreteMat, true);
            southRamp.transform.localRotation = Quaternion.Euler(-13.8f, 0f, 0f);
            CreateBox(southRamp.transform, "Ramp_Barrier_W", new Vector3(-6.6f, 0.7f, 0f), new Vector3(0.5f, 1.0f, 26.8f), concreteMat, true);
            CreateBox(southRamp.transform, "Ramp_Barrier_E", new Vector3(6.6f, 0.7f, 0f), new Vector3(0.5f, 1.0f, 26.8f), concreteMat, true);

            // 3. Mid-Overpass Collapsed Slab (Secondary tactical ramp): local Z = 20 (World Z=260)
            GameObject midRamp = CreateBox(root.transform, "Highway_Ramp_CollapsedMid", new Vector3(6f, 3.2f, 20f),
                new Vector3(6f, 0.8f, 24f), concreteMat, true);
            midRamp.transform.localRotation = Quaternion.Euler(15.0f, 12.0f, 0f);

            // 4. Northern Border Viaduct Landslide & Hazard Barricade (local Z = +115, World Z=355)
            GameObject northEndBlock = CreateBox(root.transform, "North_Landslide_Barrier", new Vector3(0f, 4.5f, 115f),
                new Vector3(28f, 9.0f, 8f), concreteMat, true);
            // Red warning beacons
            CreateWarningBeacon(northEndBlock.transform, "WarningBeacon_1", new Vector3(-6f, 4.8f, 0f));
            CreateWarningBeacon(northEndBlock.transform, "WarningBeacon_2", new Vector3(6f, 4.8f, 0f));

            // 5. Abandoned Military Evacuation Convoy (Vehicles on and under viaduct)
            GameObject convoyParent = new GameObject("Military_Evac_Convoy");
            convoyParent.transform.SetParent(root.transform, false);

            // Heavy Armored Military Trucks (with VehicleInteractable)
            CreateMilitaryArmoredTruck(convoyParent.transform, "Mil_Truck_Viaduct_1", new Vector3(-3.5f, 7.1f, -50f), -8f, militaryMat, ammo9mm, ammoShells);
            CreateMilitaryArmoredTruck(convoyParent.transform, "Mil_Truck_Viaduct_2", new Vector3(3.5f, 7.1f, -10f), 12f, militaryMat, ammo9mm, ammoShells);
            CreateMilitaryArmoredTruck(convoyParent.transform, "Mil_Truck_Ground_1", new Vector3(-12f, 0.1f, -40f), 45f, militaryMat, ammo9mm, ammoShells);
            CreateMilitaryArmoredTruck(convoyParent.transform, "Mil_Truck_Ground_2", new Vector3(14f, 0.1f, 10f), -35f, militaryMat, ammo9mm, ammoShells);

            // Field Ambulances
            CreateFieldAmbulance(convoyParent.transform, "Ambulance_Viaduct", new Vector3(3.2f, 7.1f, -70f), 15f, medkit);
            CreateFieldAmbulance(convoyParent.transform, "Ambulance_Ground", new Vector3(-14f, 0.1f, -10f), -65f, medkit);

            // Stalled Military Humvees / SUVs
            CreateMilitaryHumvee(convoyParent.transform, "Humvee_Ground_1", new Vector3(12f, 0.1f, -65f), 18f, militaryMat, shiv);
            CreateMilitaryHumvee(convoyParent.transform, "Humvee_Ground_2", new Vector3(-3.0f, 7.1f, 45f), -22f, militaryMat, shiv);

            // Concrete T-Wall Chicanes along the road
            for (float cz = -80f; cz <= 60f; cz += 35f)
            {
                CreateBox(convoyParent.transform, $"TWall_L_{cz}", new Vector3(-5f, 1.2f, cz), new Vector3(5f, 2.4f, 1.0f), concreteMat, true);
                CreateBox(convoyParent.transform, $"TWall_R_{cz}", new Vector3(5f, 1.2f, cz + 15f), new Vector3(5f, 2.4f, 1.0f), concreteMat, true);
            }

            // 6. Military Field Camp (FOB) (around local X: 35m, Z: -20m; World X: 35, Z: 220)
            GameObject fobParent = new GameObject("Military_FOB_Camp");
            fobParent.transform.SetParent(root.transform, false);
            fobParent.transform.localPosition = new Vector3(40f, 0f, -20f);

            // 3 Large Military Command / Triage Tents
            CreateMilitaryTent(fobParent.transform, "Tent_CommandHQ", new Vector3(0f, 0f, 0f), new Vector3(12f, 4.2f, 8f), militaryMat);
            CreateMilitaryTent(fobParent.transform, "Tent_MedicalTriage", new Vector3(0f, 0f, 16f), new Vector3(12f, 4.2f, 8f), militaryMat);
            CreateMilitaryTent(fobParent.transform, "Tent_Barracks", new Vector3(18f, 0f, 8f), new Vector3(10f, 3.8f, 14f), militaryMat);

            // Sandbag Bunkers
            CreateSandbagBunker(fobParent.transform, "Sandbag_Bunker_West", new Vector3(-14f, 0f, 0f), sandbagMat);
            CreateSandbagBunker(fobParent.transform, "Sandbag_Bunker_North", new Vector3(0f, 0f, 26f), sandbagMat);

            // 2 Wooden Watchtowers (4.5m tall with walk-up stairs)
            CreateWatchtower(fobParent.transform, "Watchtower_Alpha", new Vector3(-16f, 0f, 24f), woodMat, sandbagMat);
            CreateWatchtower(fobParent.transform, "Watchtower_Bravo", new Vector3(22f, 0f, -12f), woodMat, sandbagMat);

            // Portable Diesel Generator Floodlights (Range 35m, culled beyond 42m)
            CreateFloodlightTrailer(fobParent.transform, "Floodlight_FOB_Center", new Vector3(-6f, 0f, 8f), new Vector3(0.3f, -0.6f, 0.7f));
            CreateFloodlightTrailer(fobParent.transform, "Floodlight_FOB_Perimeter", new Vector3(15f, 0f, 22f), new Vector3(-0.5f, -0.5f, -0.7f));

            // 6 Reinforced Military Ammo Crates
            Vector3[] cratePositions = new Vector3[]
            {
                new Vector3(-4f, 0f, 3f),
                new Vector3(4f, 0f, -2f),
                new Vector3(-2f, 0f, 14f),
                new Vector3(14f, 0f, 6f),
                new Vector3(-15f, 4.5f, 24f), // Atop Watchtower Alpha
                new Vector3(-12f, 7.1f, -40f)  // Atop Overpass Sniping Nest
            };

            for (int i = 0; i < cratePositions.Length; i++)
            {
                CreateMilitaryAmmoCrate(fobParent.transform, $"AmmoCrate_{i + 1}", cratePositions[i], militaryMat, ammo9mm, ammoShells, medkit, shiv);
            }
        }

        // =========================================================================
        // SECTOR 2: SECTOR_1_0_EAST (INDUSTRIAL FREIGHT RAIL YARD & WAREHOUSES)
        // World: X: +120..+360, Z: -120..+120. Local center is (240, 0, 0).
        // =========================================================================
        private static void BuildSector10East(Transform sectorRoot, Material railMat, Material gravelMat,
            Material trainMat, Material warehouseMat, Material woodMat, ItemData scrap, ItemData battery, ItemData crowbar, ItemData axe, ItemData alcohol)
        {
            Transform existing = sectorRoot.Find("Phase2_RailYardAndWarehouses");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase2_RailYardAndWarehouses");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Crushed Gravel Ballast Ground Bed for Rail Yard: local X = -30 to +15, Z = -100 to +100
            CreateBox(root.transform, "Gravel_Ballast_Bed", new Vector3(-10f, 0.03f, 0f), new Vector3(55f, 0.06f, 210f), gravelMat, true);

            // 4 Parallel Steel Rail Tracks (North-South)
            float[] trackX = new float[] { -30f, -18f, -6f, 6f }; // Relative to center (240, 0, 0) -> World X = 210, 222, 234, 246
            for (int t = 0; t < trackX.Length; t++)
            {
                float x = trackX[t];
                // Left & Right steel rails
                CreateBox(root.transform, $"Rail_L_{t}", new Vector3(x - 0.75f, 0.12f, 0f), new Vector3(0.12f, 0.14f, 200f), railMat, true);
                CreateBox(root.transform, $"Rail_R_{t}", new Vector3(x + 0.75f, 0.12f, 0f), new Vector3(0.12f, 0.14f, 200f), railMat, true);

                // Wooden ties every 4m
                for (float z = -96f; z <= 96f; z += 4f)
                {
                    CreateBox(root.transform, $"Tie_{t}_{z}", new Vector3(x, 0.06f, z), new Vector3(2.4f, 0.10f, 0.35f), woodMat, false);
                }
            }

            // 2. Diesel Locomotives
            CreateDieselLocomotive(root.transform, "Locomotive_1", new Vector3(trackX[1], 0f, 65f), 180f, trainMat);
            CreateDieselLocomotive(root.transform, "Locomotive_2", new Vector3(trackX[2], 0f, -70f), 0f, trainMat);

            // 3. 10 Freight Boxcars (5 with Open Sliding Doors & Walk-Through Ramps)
            (float x, float z, bool isOpen)[] boxcarPositions = new (float, float, bool)[]
            {
                (trackX[0], -60f, true),
                (trackX[0], -20f, false),
                (trackX[0], 25f, true),
                (trackX[1], -30f, false),
                (trackX[1], 15f, true),
                (trackX[2], -25f, true),
                (trackX[2], 30f, false),
                (trackX[3], -75f, false),
                (trackX[3], -15f, true),
                (trackX[3], 50f, false)
            };

            for (int b = 0; b < boxcarPositions.Length; b++)
            {
                var bp = boxcarPositions[b];
                CreateFreightBoxcar(root.transform, $"Boxcar_{b + 1}", new Vector3(bp.x, 0f, bp.z), 0f, bp.isOpen, trainMat, woodMat, scrap, battery, crowbar);
            }

            // 4. Corrugated Steel Warehouses (3 Units on East side, local X = 55..75, World X = 295..315)
            CreateIndustrialWarehouse(root.transform, "Warehouse_1_NorthLogistics", new Vector3(65f, 0f, 65f), new Vector3(45f, 12f, 30f),
                warehouseMat, scrap, battery, crowbar, axe, alcohol);
            CreateIndustrialWarehouse(root.transform, "Warehouse_2_MachineShop", new Vector3(65f, 0f, 0f), new Vector3(45f, 12f, 30f),
                warehouseMat, scrap, battery, crowbar, axe, alcohol);
            CreateIndustrialWarehouse(root.transform, "Warehouse_3_SouthSteelDepot", new Vector3(65f, 0f, -65f), new Vector3(45f, 12f, 30f),
                warehouseMat, scrap, battery, crowbar, axe, alcohol);
        }

        // =========================================================================
        // SECTOR 3: SECTOR_1_1_NORTHEAST (WATERFRONT CONTAINER HARBOR & FUEL DEPOT)
        // World: X: +120..+360, Z: +120..+360. Local center is (240, 0, 240).
        // =========================================================================
        private static void BuildSector11NorthEast(Transform sectorRoot, Material seawallMat, Material waterMat,
            Material cRed, Material cNavy, Material cOchre, Material cGreen, Material expBarrelMat, Material woodMat,
            ItemData scrap, ItemData alcohol, ItemData ammo9mm)
        {
            Transform existing = sectorRoot.Find("Phase2_ContainerHarborAndFuel");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase2_ContainerHarborAndFuel");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Concrete Seawall Pier along East and North boundaries (local X = 95m, Z = 95m; World X = 335, Z = 335)
            GameObject seawallEast = CreateBox(root.transform, "Seawall_Quay_East", new Vector3(95f, 1.8f, 0f),
                new Vector3(6f, 3.6f, 210f), seawallMat, true);
            GameObject seawallNorth = CreateBox(root.transform, "Seawall_Quay_North", new Vector3(0f, 1.8f, 95f),
                new Vector3(210f, 3.6f, 6f), seawallMat, true);

            // Safety Railings on seawall top
            CreateBox(seawallEast.transform, "Railing_East", new Vector3(2.6f, 2.3f, 0f), new Vector3(0.3f, 1.1f, 210f), seawallMat, true);
            CreateBox(seawallNorth.transform, "Railing_North", new Vector3(0f, 2.3f, 2.6f), new Vector3(210f, 1.1f, 0.3f), seawallMat, true);

            // Steel Mooring Bollards
            for (float bz = -80f; bz <= 80f; bz += 25f)
            {
                GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bollard.name = $"Bollard_East_{bz}";
                bollard.transform.SetParent(seawallEast.transform, false);
                bollard.transform.localPosition = new Vector3(1.5f, 2.2f, bz);
                bollard.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f);
                bollard.GetComponent<Renderer>().sharedMaterial = seawallMat;
            }

            // Dark reflective ocean water beyond seawall
            GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "Harbor_Water_Surface";
            waterPlane.transform.SetParent(root.transform, false);
            waterPlane.transform.localPosition = new Vector3(105f, 0.05f, 105f);
            waterPlane.transform.localScale = new Vector3(12f, 1f, 12f);
            waterPlane.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Partially Sunken Cargo Tugboat moored along East Pier (local X: 104f, Z: 20f)
            CreateSunkenTugboat(root.transform, "Sunken_Tugboat_Wreck", new Vector3(104f, 0.2f, 20f), -14f, seawallMat);

            // 2. Shipping Container Tactical Maze (24 Staggered Containers)
            // Stacks in grid local X: -35 to +55, Z: -35 to +55
            Material[] containerMats = new Material[] { cRed, cNavy, cOchre, cGreen };
            GameObject containerMazeRoot = new GameObject("Container_Tactical_Maze");
            containerMazeRoot.transform.SetParent(root.transform, false);

            int containerIndex = 1;
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if ((row + col) % 3 == 0) continue; // Alleyway breaks

                    float cx = -30f + (col * 16f);
                    float cz = -30f + (row * 16f);
                    Material cMat = containerMats[(row + col) % containerMats.Length];

                    bool is2High = (row * col) % 2 == 1;
                    bool isOpen = (containerIndex % 4 == 0);

                    CreateShippingContainer(containerMazeRoot.transform, $"Container_{containerIndex++}",
                        new Vector3(cx, 1.4f, cz), 0f, isOpen, cMat, woodMat, scrap, alcohol, ammo9mm);

                    if (is2High)
                    {
                        CreateShippingContainer(containerMazeRoot.transform, $"Container_{containerIndex++}_Stacked",
                            new Vector3(cx, 4.2f, cz), 0f, false, containerMats[(row + col + 1) % containerMats.Length], woodMat, null, null, null);
                    }
                }
            }

            // Elevated Container Gangplanks (linking container roofs for sniper perches)
            CreateGangplank(containerMazeRoot.transform, "Gangplank_1", new Vector3(-30f, 1.4f, 2f), new Vector3(1.8f, 0.2f, 6.5f), 22f, woodMat);
            CreateGangplank(containerMazeRoot.transform, "Gangplank_2", new Vector3(18f, 1.4f, -14f), new Vector3(1.8f, 0.2f, 6.5f), 22f, woodMat);

            // 3. Coastal Fuel Storage Depot (local X = -65, Z = 25; World X = 175, Z = 265)
            GameObject fuelDepotRoot = new GameObject("Coastal_Fuel_Storage_Depot");
            fuelDepotRoot.transform.SetParent(root.transform, false);
            fuelDepotRoot.transform.localPosition = new Vector3(-65f, 0f, 25f);

            // Concrete Bund Containment Walls around fuel tanks
            CreateBox(fuelDepotRoot.transform, "BundWall_N", new Vector3(0f, 1.0f, 32f), new Vector3(55f, 2.0f, 1.2f), seawallMat, true);
            CreateBox(fuelDepotRoot.transform, "BundWall_S", new Vector3(0f, 1.0f, -32f), new Vector3(55f, 2.0f, 1.2f), seawallMat, true);
            CreateBox(fuelDepotRoot.transform, "BundWall_E", new Vector3(26f, 1.0f, 0f), new Vector3(1.2f, 2.0f, 65f), seawallMat, true);
            CreateBox(fuelDepotRoot.transform, "BundWall_W", new Vector3(-26f, 1.0f, 0f), new Vector3(1.2f, 2.0f, 65f), seawallMat, true);

            // 3 Large Cylindrical Fuel Storage Tanks
            CreateFuelStorageTank(fuelDepotRoot.transform, "FuelTank_1", new Vector3(-12f, 0f, -15f), 8f, 10f, seawallMat);
            CreateFuelStorageTank(fuelDepotRoot.transform, "FuelTank_2", new Vector3(12f, 0f, -15f), 8f, 10f, seawallMat);
            CreateFuelStorageTank(fuelDepotRoot.transform, "FuelTank_3", new Vector3(0f, 0f, 15f), 8f, 10f, seawallMat);

            // 10 Explosive Fuel Barrels with TacticalPropInteractable (shootable chain AoE)
            Vector3[] barrelSpots = new Vector3[]
            {
                new Vector3(-6f, 0f, 0f),
                new Vector3(6f, 0f, 0f),
                new Vector3(-18f, 0f, -4f),
                new Vector3(18f, 0f, -4f),
                new Vector3(0f, 0f, -4f),
                new Vector3(-8f, 0f, 24f),
                new Vector3(8f, 0f, 24f),
                new Vector3(-20f, 0f, 20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 28f)
            };

            for (int eb = 0; eb < barrelSpots.Length; eb++)
            {
                CreateExplosiveFuelBarrel(fuelDepotRoot.transform, $"Explosive_Barrel_{eb + 1}", barrelSpots[eb], expBarrelMat);
            }
        }

        // =========================================================================
        // SECTOR 4: SECTOR_1_-1_SOUTHEAST (REGIONAL POWER SUBSTATION)
        // World: X: +120..+360, Z: -360..-120. Local center is (240, 0, -240).
        // =========================================================================
        private static void BuildSector1Minus1SouthEast(Transform sectorRoot, Material brickMat, Material metalMat,
            Material gravelMat, Material concreteMat, ItemData scrap, ItemData alcohol, ItemData battery)
        {
            Transform existing = sectorRoot.Find("Phase2_PowerSubstation");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new GameObject("Phase2_PowerSubstation");
            root.transform.SetParent(sectorRoot, false);
            root.transform.localPosition = Vector3.zero;

            // 1. Substation Crushed Gravel Yard: local X = -40 to +40, Z = -50 to +50
            CreateBox(root.transform, "Substation_Gravel_Yard", new Vector3(0f, 0.03f, 0f), new Vector3(90f, 0.06f, 110f), gravelMat, true);

            // Security Chain-Link Perimeter Fence with Open Gate (South side)
            CreateBox(root.transform, "Fence_North", new Vector3(0f, 1.5f, 55f), new Vector3(90f, 3.0f, 0.3f), metalMat, true);
            CreateBox(root.transform, "Fence_East", new Vector3(45f, 1.5f, 0f), new Vector3(0.3f, 3.0f, 110f), metalMat, true);
            CreateBox(root.transform, "Fence_West", new Vector3(-45f, 1.5f, 0f), new Vector3(0.3f, 3.0f, 110f), metalMat, true);
            // South Fence with 10m Open Entrance Gate
            CreateBox(root.transform, "Fence_South_W", new Vector3(-25f, 1.5f, -55f), new Vector3(40f, 3.0f, 0.3f), metalMat, true);
            CreateBox(root.transform, "Fence_South_E", new Vector3(25f, 1.5f, -55f), new Vector3(40f, 3.0f, 0.3f), metalMat, true);

            // 2. Heavy Ribbed Electrical Transformers (2 banks of 3)
            for (int tr = 0; tr < 3; tr++)
            {
                float z = -25f + (tr * 22f);
                CreateRibbedTransformer(root.transform, $"Transformer_BankA_{tr + 1}", new Vector3(-20f, 0f, z), metalMat);
                CreateRibbedTransformer(root.transform, $"Transformer_BankB_{tr + 1}", new Vector3(5f, 0f, z), metalMat);
            }

            // High Voltage Lattice Transmission Pylons (Towers)
            CreateTransmissionPylon(root.transform, "Pylon_North", new Vector3(-30f, 0f, 35f), metalMat);
            CreateTransmissionPylon(root.transform, "Pylon_South", new Vector3(30f, 0f, 35f), metalMat);

            // 3. Substation Control Room (Brick Building, 16m x 12m x 5m, local X = 25f, Z = -20f)
            GameObject controlBuilding = CreateSubstationControlRoom(root.transform, "Substation_Control_Building",
                new Vector3(25f, 0f, -20f), brickMat, concreteMat, scrap, alcohol, battery);

            // 4. Master Regional Power Substation Grid Panel inside Control Room
            GameObject panelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelObj.name = "Regional_Master_Grid_Panel";
            panelObj.transform.SetParent(controlBuilding.transform, false);
            panelObj.transform.localPosition = new Vector3(0f, 1.2f, 4.8f);
            panelObj.transform.localScale = new Vector3(2.4f, 1.8f, 0.6f);
            panelObj.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Attach RegionalMasterGridPanel
            RegionalMasterGridPanel gridPanel = panelObj.AddComponent<RegionalMasterGridPanel>();
            GameObjectUtility.SetStaticEditorFlags(panelObj, StaticEditorFlags.NavigationStatic);

            // Streetlamp and yard lights for demonstration
            GameObject yardLight = new GameObject("Substation_Yard_FloodLight");
            yardLight.transform.SetParent(root.transform, false);
            yardLight.transform.localPosition = new Vector3(0f, 6.5f, 0f);
            Light lComp = yardLight.AddComponent<Light>();
            lComp.type = LightType.Point;
            lComp.range = 38f;
            lComp.intensity = 1.4f;
            lComp.color = new Color(1.0f, 0.85f, 0.55f);
            lComp.enabled = false; // Initially powered off until panel energized!
        }

        // =========================================================================
        // AUTONOMOUS SCAVENGER SQUADS (NORTH & EAST)
        // =========================================================================
        private static void BuildScavengerSquads()
        {
            GameObject squadRoot = GameObject.Find("--- SCAVENGER SQUADS ---");
            if (squadRoot == null)
            {
                squadRoot = new GameObject("--- SCAVENGER SQUADS ---");
            }

            // Squad 1: Highway Patrol (Murat & Can) - patrols North Checkpoint and viaduct base
            Transform squadHighway = squadRoot.transform.Find("Scavenger_Squad_Highway");
            if (squadHighway == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_Highway");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(15f, 0.1f, 160f);

                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();
                SurvivorNPC_AI point = CreateSurvivor("Scavenger_Point_Volkan", sqObj.transform.position, NPCArchetype.Armed, "volkan", "Volkan");
                SurvivorNPC_AI carrier = CreateSurvivor("Scavenger_Carrier_Ece", sqObj.transform.position + Vector3.right * 1.5f, NPCArchetype.Civilian, "ece", "Ece");
                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Phase2DistrictBuilder] Created Scavenger_Squad_Highway with Volkan & Ece.");
            }

            // Squad 2: Rail Yard Patrol (Burak & Deniz) - patrols East Rail Yard and Warehouses
            Transform squadRail = squadRoot.transform.Find("Scavenger_Squad_RailYard");
            if (squadRail == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_RailYard");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(200f, 0.1f, 25f);

                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();
                SurvivorNPC_AI point = CreateSurvivor("Scavenger_Point_Sinan", sqObj.transform.position, NPCArchetype.Armed, "sinan", "Sinan");
                SurvivorNPC_AI carrier = CreateSurvivor("Scavenger_Carrier_Cem", sqObj.transform.position + Vector3.right * 1.5f, NPCArchetype.Civilian, "cem", "Cem");
                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Phase2DistrictBuilder] Created Scavenger_Squad_RailYard with Sinan & Cem.");
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
        // ATMOSPHERE DETAILS (IVY & COBWEBS)
        // =========================================================================
        private static void GenerateAtmosphereDetails(Transform north, Transform east, Transform northEast, Transform southEast)
        {
            // Ivy patches on concrete piers, warehouse walls, seawalls
            var northTargets = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(0f, 0.2f, 160f), Vector3.forward, 6.0f, 3.5f),
                (new Vector3(0f, 0.2f, 220f), Vector3.back, 6.0f, 3.5f),
                (new Vector3(0f, 0.2f, 280f), Vector3.forward, 6.0f, 3.5f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(north, "Atmosphere_Ivy_North", northTargets);

            var eastTargets = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                (new Vector3(275f, 0.2f, 65f), Vector3.right, 7.5f, 5.0f),
                (new Vector3(275f, 0.2f, -65f), Vector3.right, 7.5f, 5.0f)
            };
            OvergrownIvyGenerator.GenerateIvyPatches(east, "Atmosphere_Ivy_East", eastTargets);

            // Cobweb patches in train boxcars and warehouse corners
            var eastCobwebs = new (Vector3 pos, Vector2 size, Vector3 rot)[]
            {
                (new Vector3(300f, 4.5f, 60f), new Vector2(2.5f, 2.5f), new Vector3(0f, 45f, 0f)),
                (new Vector3(300f, 4.5f, -60f), new Vector2(2.5f, 2.5f), new Vector3(0f, 45f, 0f)),
                (new Vector3(210f, 2.8f, -60f), new Vector2(1.8f, 1.8f), new Vector3(0f, 90f, 0f))
            };
            SpiderwebGenerator.GenerateCobwebPatches(east, "Atmosphere_Cobwebs_East", eastCobwebs);
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

        private static void CreateWarningBeacon(Transform parent, string name, Vector3 localPos)
        {
            GameObject tripod = CreateBox(parent, name, localPos, new Vector3(0.5f, 1.2f, 0.5f), null, false);
            GameObject lightObj = new GameObject("Beacon_Light");
            lightObj.transform.SetParent(tripod.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.15f, 0.15f);
            l.range = 14f;
            l.intensity = 2.0f;
        }

        private static void CreateMilitaryArmoredTruck(Transform parent, string name, Vector3 localPos, float yaw, Material mat, ItemData ammo1, ItemData ammo2)
        {
            GameObject truck = new GameObject(name);
            truck.transform.SetParent(parent, false);
            truck.transform.localPosition = localPos;
            truck.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Body
            CreateBox(truck.transform, "Chassis", new Vector3(0f, 0.9f, 0f), new Vector3(3.2f, 1.2f, 8.5f), mat, true);
            CreateBox(truck.transform, "Cab", new Vector3(0f, 2.2f, 1.8f), new Vector3(3.0f, 1.6f, 3.2f), mat, true);
            CreateBox(truck.transform, "CargoBed", new Vector3(0f, 2.2f, -1.8f), new Vector3(3.1f, 1.8f, 4.4f), mat, true);

            // Wheels
            float[] wz = new float[] { -2.4f, 0f, 2.6f };
            for (int w = 0; w < wz.Length; w++)
            {
                CreateBox(truck.transform, $"Wheel_L_{w}", new Vector3(-1.7f, 0.5f, wz[w]), new Vector3(0.45f, 1.0f, 1.0f), null, true);
                CreateBox(truck.transform, $"Wheel_R_{w}", new Vector3(1.7f, 0.5f, wz[w]), new Vector3(0.45f, 1.0f, 1.0f), null, true);
            }

            // Interactive vehicle component
            BoxCollider bc = truck.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.4f, 3.2f, 9.0f);
            bc.center = new Vector3(0f, 1.6f, 0f);
            VehicleInteractable vi = truck.AddComponent<VehicleInteractable>();
        }

        private static void CreateFieldAmbulance(Transform parent, string name, Vector3 localPos, float yaw, ItemData medkit)
        {
            GameObject amb = new GameObject(name);
            amb.transform.SetParent(parent, false);
            amb.transform.localPosition = localPos;
            amb.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(amb.transform, "Chassis", new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 0.8f, 6.2f), null, true);
            CreateBox(amb.transform, "Box_Body", new Vector3(0f, 1.9f, -0.6f), new Vector3(2.3f, 1.8f, 4.2f), null, true);
            CreateBox(amb.transform, "Cab", new Vector3(0f, 1.5f, 2.0f), new Vector3(2.2f, 1.4f, 2.0f), null, true);

            BoxCollider bc = amb.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.6f, 2.8f, 6.5f);
            bc.center = new Vector3(0f, 1.4f, 0f);
            VehicleInteractable vi = amb.AddComponent<VehicleInteractable>();
        }

        private static void CreateMilitaryHumvee(Transform parent, string name, Vector3 localPos, float yaw, Material mat, ItemData loot)
        {
            GameObject hum = new GameObject(name);
            hum.transform.SetParent(parent, false);
            hum.transform.localPosition = localPos;
            hum.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(hum.transform, "Body", new Vector3(0f, 0.7f, 0f), new Vector3(2.4f, 0.9f, 4.8f), mat, true);
            CreateBox(hum.transform, "Sloped_Roof", new Vector3(0f, 1.6f, -0.4f), new Vector3(2.2f, 1.0f, 2.8f), mat, true);

            BoxCollider bc = hum.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.6f, 2.2f, 5.2f);
            bc.center = new Vector3(0f, 1.1f, 0f);
            VehicleInteractable vi = hum.AddComponent<VehicleInteractable>();
        }

        private static void CreateMilitaryTent(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject tent = new GameObject(name);
            tent.transform.SetParent(parent, false);
            tent.transform.localPosition = localPos;

            // Tent Walls
            CreateBox(tent.transform, "Wall_L", new Vector3(-size.x * 0.5f, size.y * 0.4f, 0f), new Vector3(0.3f, size.y * 0.8f, size.z), mat, true);
            CreateBox(tent.transform, "Wall_R", new Vector3(size.x * 0.5f, size.y * 0.4f, 0f), new Vector3(0.3f, size.y * 0.8f, size.z), mat, true);
            CreateBox(tent.transform, "Wall_Back", new Vector3(0f, size.y * 0.4f, size.z * 0.5f), new Vector3(size.x, size.y * 0.8f, 0.3f), mat, true);
            // Sloped Ridge Roof
            CreateBox(tent.transform, "Ridge_Roof", new Vector3(0f, size.y, 0f), new Vector3(size.x + 0.5f, 0.4f, size.z + 0.5f), mat, true);
        }

        private static void CreateSandbagBunker(Transform parent, string name, Vector3 localPos, Material mat)
        {
            GameObject bunker = new GameObject(name);
            bunker.transform.SetParent(parent, false);
            bunker.transform.localPosition = localPos;

            CreateBox(bunker.transform, "BagRow_Front", new Vector3(0f, 0.6f, 2f), new Vector3(6f, 1.2f, 0.8f), mat, true);
            CreateBox(bunker.transform, "BagRow_Left", new Vector3(-2.6f, 0.6f, 0f), new Vector3(0.8f, 1.2f, 4f), mat, true);
            CreateBox(bunker.transform, "BagRow_Right", new Vector3(2.6f, 0.6f, 0f), new Vector3(0.8f, 1.2f, 4f), mat, true);
        }

        private static void CreateWatchtower(Transform parent, string name, Vector3 localPos, Material woodMat, Material sandbagMat)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = localPos;

            // 4 Corner Legs (4.5m tall)
            CreateBox(tower.transform, "Leg_1", new Vector3(-1.8f, 2.25f, -1.8f), new Vector3(0.35f, 4.5f, 0.35f), woodMat, true);
            CreateBox(tower.transform, "Leg_2", new Vector3(1.8f, 2.25f, -1.8f), new Vector3(0.35f, 4.5f, 0.35f), woodMat, true);
            CreateBox(tower.transform, "Leg_3", new Vector3(-1.8f, 2.25f, 1.8f), new Vector3(0.35f, 4.5f, 0.35f), woodMat, true);
            CreateBox(tower.transform, "Leg_4", new Vector3(1.8f, 2.25f, 1.8f), new Vector3(0.35f, 4.5f, 0.35f), woodMat, true);

            // Elevated Platform (Y: 4.5m)
            CreateBox(tower.transform, "Platform", new Vector3(0f, 4.5f, 0f), new Vector3(4.2f, 0.3f, 4.2f), woodMat, true);
            CreateBox(tower.transform, "Parapet", new Vector3(0f, 5.0f, 0f), new Vector3(4.2f, 0.8f, 4.2f), sandbagMat, true);

            // Stairs / Ramp leading up (Slope ~25°)
            GameObject stairs = CreateBox(tower.transform, "Stairs", new Vector3(0f, 2.25f, -4.5f), new Vector3(1.4f, 0.25f, 5.5f), woodMat, true);
            stairs.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
        }

        private static void CreateFloodlightTrailer(Transform parent, string name, Vector3 localPos, Vector3 forwardDir)
        {
            GameObject fl = new GameObject(name);
            fl.transform.SetParent(parent, false);
            fl.transform.localPosition = localPos;

            CreateBox(fl.transform, "Generator_Cart", new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.0f, 2.2f), null, true);
            CreateBox(fl.transform, "Mast", new Vector3(0f, 2.8f, 0f), new Vector3(0.25f, 3.8f, 0.25f), null, false);

            GameObject lightObj = new GameObject("SpotLight");
            lightObj.transform.SetParent(fl.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            lightObj.transform.forward = forwardDir.normalized;

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 35f;
            l.spotAngle = 65f;
            l.intensity = 3.5f;
            l.color = new Color(1.0f, 0.92f, 0.75f);
        }

        private static void CreateMilitaryAmmoCrate(Transform parent, string name, Vector3 localPos, Material mat,
            ItemData ammo1, ItemData ammo2, ItemData medkit, ItemData shiv)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = name;
            crate.transform.SetParent(parent, false);
            crate.transform.localPosition = localPos + Vector3.up * 0.4f;
            crate.transform.localScale = new Vector3(1.3f, 0.8f, 0.85f);
            if (mat != null) crate.GetComponent<Renderer>().sharedMaterial = mat;

            LootContainer loot = crate.AddComponent<LootContainer>();
            if (ammo1 != null) loot.AddItem(ammo1, 15, 1f);
            if (ammo2 != null) loot.AddItem(ammo2, 8, 0.85f);
            if (medkit != null) loot.AddItem(medkit, 1, 0.5f);
            if (shiv != null) loot.AddItem(shiv, 1, 0.75f);

            crate.AddComponent<TacticalPropInteractable>();
            GameObjectUtility.SetStaticEditorFlags(crate, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateDieselLocomotive(Transform parent, string name, Vector3 localPos, float yaw, Material mat)
        {
            GameObject loco = new GameObject(name);
            loco.transform.SetParent(parent, false);
            loco.transform.localPosition = localPos;
            loco.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(loco.transform, "Chassis", new Vector3(0f, 1.2f, 0f), new Vector3(3.4f, 1.2f, 18f), mat, true);
            CreateBox(loco.transform, "EngineHood", new Vector3(0f, 2.8f, -1.5f), new Vector3(3.2f, 2.0f, 12f), mat, true);
            CreateBox(loco.transform, "Cab", new Vector3(0f, 3.4f, 6.5f), new Vector3(3.3f, 2.8f, 4.2f), mat, true);
            CreateBox(loco.transform, "Cowcatcher", new Vector3(0f, 0.5f, -9.2f), new Vector3(3.4f, 0.8f, 1.2f), mat, true);
        }

        private static void CreateFreightBoxcar(Transform parent, string name, Vector3 localPos, float yaw, bool isOpenPassage,
            Material trainMat, Material woodMat, ItemData scrap, ItemData battery, ItemData crowbar)
        {
            GameObject car = new GameObject(name);
            car.transform.SetParent(parent, false);
            car.transform.localPosition = localPos;
            car.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 3.4f;
            float h = 3.6f;
            float l = 15.0f;

            // Floor & Roof
            CreateBox(car.transform, "Floor", new Vector3(0f, 1.1f, 0f), new Vector3(w, 0.3f, l), trainMat, true);
            CreateBox(car.transform, "Roof", new Vector3(0f, 1.1f + h, 0f), new Vector3(w, 0.3f, l), trainMat, true);
            CreateBox(car.transform, "End_North", new Vector3(0f, 1.1f + (h * 0.5f), l * 0.5f), new Vector3(w, h, 0.3f), trainMat, true);
            CreateBox(car.transform, "End_South", new Vector3(0f, 1.1f + (h * 0.5f), -l * 0.5f), new Vector3(w, h, 0.3f), trainMat, true);

            if (isOpenPassage)
            {
                // Walls have open center doorways for run-through line of sight breaking!
                CreateBox(car.transform, "Wall_W_South", new Vector3(-w * 0.5f, 1.1f + (h * 0.5f), -4.5f), new Vector3(0.3f, h, 5.5f), trainMat, true);
                CreateBox(car.transform, "Wall_W_North", new Vector3(-w * 0.5f, 1.1f + (h * 0.5f), 4.5f), new Vector3(0.3f, h, 5.5f), trainMat, true);

                CreateBox(car.transform, "Wall_E_South", new Vector3(w * 0.5f, 1.1f + (h * 0.5f), -4.5f), new Vector3(0.3f, h, 5.5f), trainMat, true);
                CreateBox(car.transform, "Wall_E_North", new Vector3(w * 0.5f, 1.1f + (h * 0.5f), 4.5f), new Vector3(0.3f, h, 5.5f), trainMat, true);

                // Boarding ramps on East and West sides (Slope ~14°)
                GameObject rampW = CreateBox(car.transform, "BoardingRamp_W", new Vector3(-3.2f, 0.55f, 0f), new Vector3(3.2f, 0.2f, 2.8f), woodMat, true);
                rampW.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);

                GameObject rampE = CreateBox(car.transform, "BoardingRamp_E", new Vector3(3.2f, 0.55f, 0f), new Vector3(3.2f, 0.2f, 2.8f), woodMat, true);
                rampE.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);

                // Interior Furniture crate / Hiding spot
                GameObject crate = CreateBox(car.transform, "Interior_Loot_Crate", new Vector3(0f, 1.6f, -5.5f), new Vector3(1.2f, 0.8f, 1.2f), woodMat, true);
                crate.AddComponent<FurnitureInteractable>();
                LootContainer lc = crate.AddComponent<LootContainer>();
                if (scrap != null) lc.AddItem(scrap, 3, 1f);
                if (battery != null) lc.AddItem(battery, 1, 0.75f);
                if (crowbar != null) lc.AddItem(crowbar, 1, 0.5f);
            }
            else
            {
                // Fully closed walls
                CreateBox(car.transform, "Wall_West", new Vector3(-w * 0.5f, 1.1f + (h * 0.5f), 0f), new Vector3(0.3f, h, l), trainMat, true);
                CreateBox(car.transform, "Wall_East", new Vector3(w * 0.5f, 1.1f + (h * 0.5f), 0f), new Vector3(0.3f, h, l), trainMat, true);
            }
        }

        private static void CreateIndustrialWarehouse(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat,
            ItemData scrap, ItemData battery, ItemData crowbar, ItemData axe, ItemData alcohol)
        {
            GameObject wh = new GameObject(name);
            wh.transform.SetParent(parent, false);
            wh.transform.localPosition = localPos;

            float w = size.x;
            float h = size.y;
            float l = size.z;

            // Raised concrete loading dock foundation (height 1.2m)
            CreateBox(wh.transform, "Foundation_Dock", new Vector3(0f, 0.6f, 0f), new Vector3(w, 1.2f, l), null, true);
            // Raised loading dock apron on West side
            CreateBox(wh.transform, "Loading_Apron", new Vector3(-w * 0.5f - 2.5f, 0.6f, 0f), new Vector3(5f, 1.2f, l * 0.7f), null, true);
            // Apron access ramp (Slope 14°)
            GameObject dockRamp = CreateBox(wh.transform, "Dock_Ramp", new Vector3(-w * 0.5f - 2.5f, 0.6f, -l * 0.35f - 2.5f), new Vector3(5f, 0.3f, 5f), null, true);
            dockRamp.transform.localRotation = Quaternion.Euler(-13.5f, 0f, 0f);

            // Exterior Warehouse Shell
            CreateBox(wh.transform, "Wall_East", new Vector3(w * 0.5f, 1.2f + (h * 0.5f), 0f), new Vector3(0.4f, h, l), mat, true);
            CreateBox(wh.transform, "Wall_North", new Vector3(0f, 1.2f + (h * 0.5f), l * 0.5f), new Vector3(w, h, 0.4f), mat, true);
            CreateBox(wh.transform, "Wall_South", new Vector3(0f, 1.2f + (h * 0.5f), -l * 0.5f), new Vector3(w, h, 0.4f), mat, true);
            CreateBox(wh.transform, "Roof_Peak", new Vector3(0f, 1.2f + h, 0f), new Vector3(w + 1f, 0.5f, l + 1f), mat, true);

            // West wall with large 6m loading bay door opening
            CreateBox(wh.transform, "Wall_West_N", new Vector3(-w * 0.5f, 1.2f + (h * 0.5f), l * 0.28f), new Vector3(0.4f, h, l * 0.44f), mat, true);
            CreateBox(wh.transform, "Wall_West_S", new Vector3(-w * 0.5f, 1.2f + (h * 0.5f), -l * 0.28f), new Vector3(0.4f, h, l * 0.44f), mat, true);
            CreateBox(wh.transform, "Wall_West_Header", new Vector3(-w * 0.5f, 1.2f + 5.5f, 0f), new Vector3(0.4f, h - 5.5f, 6.0f), mat, true);

            // Shutter Door
            GameObject shutterDoor = CreateBox(wh.transform, "Rolling_Shutter_Door", new Vector3(-w * 0.5f, 1.2f + 2.5f, 0f), new Vector3(0.2f, 5.0f, 5.8f), null, false);
            shutterDoor.AddComponent<InteractiveDoor>();

            // Interior Steel Shelving Aisles (2 rows of 3)
            for (int r = -1; r <= 1; r += 2)
            {
                for (int c = -1; c <= 1; c++)
                {
                    GameObject rack = CreateBox(wh.transform, $"SteelRack_{r}_{c}", new Vector3(r * 10f, 1.2f + 2.2f, c * 8f),
                        new Vector3(2.5f, 4.4f, 6.0f), null, true);
                    LootContainer lc = rack.AddComponent<LootContainer>();
                    if (scrap != null) lc.AddItem(scrap, 4, 1f);
                    if (battery != null) lc.AddItem(battery, 2, 0.8f);
                    if (crowbar != null) lc.AddItem(crowbar, 1, 0.6f);
                    if (axe != null) lc.AddItem(axe, 1, 0.45f);
                    if (alcohol != null) lc.AddItem(alcohol, 2, 0.75f);
                    rack.AddComponent<FurnitureInteractable>();
                }
            }

            // Interior Floodlight (energized when RegionalMasterGridPanel is activated)
            GameObject intLight = new GameObject("Warehouse_Interior_Light");
            intLight.transform.SetParent(wh.transform, false);
            intLight.transform.localPosition = new Vector3(0f, 1.2f + 8.5f, 0f);
            Light whLight = intLight.AddComponent<Light>();
            whLight.type = LightType.Point;
            whLight.range = 32f;
            whLight.intensity = 1.6f;
            whLight.color = new Color(1.0f, 0.90f, 0.70f);
            whLight.enabled = false;
        }

        private static void CreateSunkenTugboat(Transform parent, string name, Vector3 localPos, float tiltZ, Material mat)
        {
            GameObject boat = new GameObject(name);
            boat.transform.SetParent(parent, false);
            boat.transform.localPosition = localPos;
            boat.transform.localRotation = Quaternion.Euler(0f, 90f, tiltZ);

            CreateBox(boat.transform, "Hull", new Vector3(0f, 1.2f, 0f), new Vector3(6f, 3.2f, 22f), mat, true);
            CreateBox(boat.transform, "Cabin", new Vector3(0f, 3.4f, 2f), new Vector3(4.5f, 2.4f, 7f), mat, true);
            CreateBox(boat.transform, "Smokestack", new Vector3(0f, 5.2f, -1f), new Vector3(1.2f, 2.2f, 1.2f), mat, false);
        }

        private static void CreateShippingContainer(Transform parent, string name, Vector3 localPos, float yaw, bool isOpen,
            Material cMat, Material woodMat, ItemData scrap, ItemData alcohol, ItemData ammo)
        {
            GameObject cont = new GameObject(name);
            cont.transform.SetParent(parent, false);
            cont.transform.localPosition = localPos;
            cont.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 2.8f;
            float h = 2.8f;
            float l = 12.0f;

            if (isOpen)
            {
                // Hollow container with open doors
                CreateBox(cont.transform, "Floor", new Vector3(0f, -h * 0.5f + 0.1f, 0f), new Vector3(w, 0.2f, l), cMat, true);
                CreateBox(cont.transform, "Roof", new Vector3(0f, h * 0.5f - 0.1f, 0f), new Vector3(w, 0.2f, l), cMat, true);
                CreateBox(cont.transform, "Wall_L", new Vector3(-w * 0.5f + 0.1f, 0f, 0f), new Vector3(0.2f, h, l), cMat, true);
                CreateBox(cont.transform, "Wall_R", new Vector3(w * 0.5f - 0.1f, 0f, 0f), new Vector3(0.2f, h, l), cMat, true);
                CreateBox(cont.transform, "Wall_Back", new Vector3(0f, 0f, -l * 0.5f + 0.1f), new Vector3(w, h, 0.2f), cMat, true);

                // Open door flaps
                CreateBox(cont.transform, "Door_Flap_L", new Vector3(-w * 0.5f - 0.5f, 0f, l * 0.5f + 0.5f), new Vector3(0.15f, h, 1.4f), cMat, true);
                CreateBox(cont.transform, "Door_Flap_R", new Vector3(w * 0.5f + 0.5f, 0f, l * 0.5f + 0.5f), new Vector3(0.15f, h, 1.4f), cMat, true);

                // Interior loot shelf
                GameObject shelf = CreateBox(cont.transform, "Container_Shelf", new Vector3(0f, -0.6f, -3.5f), new Vector3(1.6f, 0.8f, 1.8f), woodMat, true);
                shelf.AddComponent<FurnitureInteractable>();
                LootContainer lc = shelf.AddComponent<LootContainer>();
                if (scrap != null) lc.AddItem(scrap, 3, 1f);
                if (alcohol != null) lc.AddItem(alcohol, 2, 0.8f);
                if (ammo != null) lc.AddItem(ammo, 12, 0.8f);
            }
            else
            {
                CreateBox(cont.transform, "Solid_Container", Vector3.zero, new Vector3(w, h, l), cMat, true);
            }
        }

        private static void CreateGangplank(Transform parent, string name, Vector3 localPos, Vector3 size, float pitch, Material mat)
        {
            GameObject gp = CreateBox(parent, name, localPos, size, mat, true);
            gp.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static void CreateFuelStorageTank(Transform parent, string name, Vector3 localPos, float radius, float height, Material mat)
        {
            GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = name;
            tank.transform.SetParent(parent, false);
            tank.transform.localPosition = localPos + Vector3.up * (height * 0.5f);
            tank.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            if (mat != null) tank.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(tank, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateExplosiveFuelBarrel(Transform parent, string name, Vector3 localPos, Material mat)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name;
            barrel.transform.SetParent(parent, false);
            barrel.transform.localPosition = localPos + Vector3.up * 0.6f;
            barrel.transform.localScale = new Vector3(0.85f, 0.6f, 0.85f);
            if (mat != null) barrel.GetComponent<Renderer>().sharedMaterial = mat;

            barrel.AddComponent<TacticalPropInteractable>();
            GameObjectUtility.SetStaticEditorFlags(barrel, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateRibbedTransformer(Transform parent, string name, Vector3 localPos, Material mat)
        {
            GameObject tr = new GameObject(name);
            tr.transform.SetParent(parent, false);
            tr.transform.localPosition = localPos;

            CreateBox(tr.transform, "Base_Plaque", new Vector3(0f, 0.3f, 0f), new Vector3(5.5f, 0.6f, 4.5f), null, true);
            CreateBox(tr.transform, "Main_Core", new Vector3(0f, 2.5f, 0f), new Vector3(4.8f, 3.8f, 3.6f), mat, true);

            // Ceramic bushings on top
            for (int b = -1; b <= 1; b++)
            {
                GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bush.name = $"Bush_{b}";
                bush.transform.SetParent(tr.transform, false);
                bush.transform.localPosition = new Vector3(b * 1.5f, 4.8f, 0f);
                bush.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            }
        }

        private static void CreateTransmissionPylon(Transform parent, string name, Vector3 localPos, Material mat)
        {
            GameObject pylon = new GameObject(name);
            pylon.transform.SetParent(parent, false);
            pylon.transform.localPosition = localPos;

            CreateBox(pylon.transform, "Leg_A", new Vector3(-2f, 12f, -2f), new Vector3(0.6f, 24f, 0.6f), mat, true);
            CreateBox(pylon.transform, "Leg_B", new Vector3(2f, 12f, -2f), new Vector3(0.6f, 24f, 0.6f), mat, true);
            CreateBox(pylon.transform, "Leg_C", new Vector3(-2f, 12f, 2f), new Vector3(0.6f, 24f, 0.6f), mat, true);
            CreateBox(pylon.transform, "Leg_D", new Vector3(2f, 12f, 2f), new Vector3(0.6f, 24f, 0.6f), mat, true);
            CreateBox(pylon.transform, "Crossarm", new Vector3(0f, 19f, 0f), new Vector3(14f, 1.2f, 1.2f), mat, false);
        }

        private static GameObject CreateSubstationControlRoom(Transform parent, string name, Vector3 localPos, Material brickMat,
            Material concreteMat, ItemData scrap, ItemData alcohol, ItemData battery)
        {
            GameObject bldg = new GameObject(name);
            bldg.transform.SetParent(parent, false);
            bldg.transform.localPosition = localPos;

            float w = 16f;
            float h = 5f;
            float l = 12f;

            // Floor & Roof
            CreateBox(bldg.transform, "Floor", new Vector3(0f, 0.15f, 0f), new Vector3(w, 0.3f, l), concreteMat, true);
            CreateBox(bldg.transform, "Roof", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 1f, 0.4f, l + 1f), concreteMat, true);

            // Walls (North, East, South)
            CreateBox(bldg.transform, "Wall_North", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, 0.4f), brickMat, true);
            CreateBox(bldg.transform, "Wall_East", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, l), brickMat, true);
            CreateBox(bldg.transform, "Wall_South", new Vector3(0f, h * 0.5f, -l * 0.5f), new Vector3(w, h, 0.4f), brickMat, true);

            // West wall with entrance door and window openings
            CreateBox(bldg.transform, "Wall_West_L", new Vector3(-w * 0.5f, h * 0.5f, -3.5f), new Vector3(0.4f, h, 5f), brickMat, true);
            CreateBox(bldg.transform, "Wall_West_R", new Vector3(-w * 0.5f, h * 0.5f, 3.5f), new Vector3(0.4f, h, 5f), brickMat, true);
            CreateBox(bldg.transform, "Wall_West_Header", new Vector3(-w * 0.5f, 3.8f, 0f), new Vector3(0.4f, 2.4f, 2.5f), brickMat, true);

            // Entrance Door
            GameObject door = CreateBox(bldg.transform, "ControlRoom_Door", new Vector3(-w * 0.5f, 1.3f, 0f), new Vector3(0.15f, 2.4f, 1.8f), null, false);
            door.AddComponent<InteractiveDoor>();

            // Window on North wall
            GameObject win = CreateBox(bldg.transform, "ControlRoom_Window", new Vector3(4f, 1.8f, l * 0.5f), new Vector3(2.4f, 1.8f, 0.2f), null, false);
            win.AddComponent<WindowInteractable>();

            // Interior Lockers / Furniture
            GameObject locker = CreateBox(bldg.transform, "Control_Locker", new Vector3(-6f, 1.2f, 4f), new Vector3(1.2f, 2.2f, 0.8f), null, true);
            locker.AddComponent<FurnitureInteractable>();
            LootContainer lc = locker.AddComponent<LootContainer>();
            if (scrap != null) lc.AddItem(scrap, 3, 1f);
            if (battery != null) lc.AddItem(battery, 2, 0.85f);
            if (alcohol != null) lc.AddItem(alcohol, 2, 0.85f);

            return bldg;
        }
    }
}
