using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    public static class WorldPerimeterExpander
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Expand World to 720x720m (Phase 1)", false, 14)]
        public static void ExpandWorld()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> OPEN-WORLD MAP EXPANSION (720x720m 3x3 SECTOR GRID) INITIATED <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            // 1. Establish Master Root: --- WORLD SECTORS ---
            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            if (worldSectorsRoot == null)
            {
                worldSectorsRoot = new GameObject("--- WORLD SECTORS ---");
                worldSectorsRoot.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(worldSectorsRoot, "Create World Sectors Root");
            }

            // 2. Prepare Materials
            Material highwayGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Highway", new Color(0.12f, 0.13f, 0.15f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(30f, 30f));
            Material industrialGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Industrial", new Color(0.14f, 0.14f, 0.14f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(20f, 20f));
            Material harborGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Harbor", new Color(0.11f, 0.13f, 0.16f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(25f, 25f));
            Material parkGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Park", new Color(0.10f, 0.16f, 0.11f),
                null, null, new Vector2(25f, 25f));
            Material lakesideGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Lakeside", new Color(0.14f, 0.16f, 0.13f),
                null, null, new Vector2(25f, 25f));
            Material plazaGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Plaza", new Color(0.16f, 0.17f, 0.19f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(20f, 20f));
            Material floodedGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Flooded", new Color(0.09f, 0.12f, 0.14f),
                null, null, new Vector2(20f, 20f));
            Material substationGroundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground_Substation", new Color(0.15f, 0.14f, 0.13f),
                null, null, new Vector2(20f, 20f));

            Material streetAsphaltMat = SceneBuilder.GetOrCreateMaterial("Mat_Street", new Color(0.137f, 0.153f, 0.180f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(15f, 15f));
            Material curbConcreteMat = SceneBuilder.GetOrCreateMaterial("Mat_Curb", new Color(0.40f, 0.42f, 0.45f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(6f, 6f));
            Material yellowLineMat = SceneBuilder.GetOrCreateMaterial("Mat_RoadMarking_Yellow", new Color(0.96f, 0.78f, 0.20f));
            Material barrierMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Barrier", new Color(0.48f, 0.50f, 0.53f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(2f, 2f));
            Material hazardStripesMat = SceneBuilder.GetOrCreateMaterial("Mat_Warning_Hazard", new Color(0.95f, 0.75f, 0.10f));
            Material rustyMetalMat = SceneBuilder.GetOrCreateMaterial("Mat_Rust_Metal", new Color(0.35f, 0.25f, 0.20f));
            Material militaryGreenMat = SceneBuilder.GetOrCreateMaterial("Mat_Military_Green", new Color(0.18f, 0.25f, 0.18f));
            Material waterMat = SceneBuilder.GetOrCreateMaterial("Mat_Water_Murky", new Color(0.08f, 0.22f, 0.28f, 0.85f), null, null, null, true);
            Material woodCabinMat = SceneBuilder.GetOrCreateMaterial("Mat_Wood_Cabin", new Color(0.38f, 0.28f, 0.18f));
            Material perimeterWallMat = SceneBuilder.GetOrCreateMaterial("Mat_Perimeter_Wall", new Color(0.25f, 0.27f, 0.30f));

            // 3. Ensure 9 Sector Roots
            var allSectors = WorldMapConfig.GetAllSectors();
            Dictionary<string, GameObject> sectorRoots = new Dictionary<string, GameObject>();

            foreach (var s in allSectors)
            {
                Transform existing = worldSectorsRoot.transform.Find(s.sectorId);
                GameObject sObj;
                if (existing != null)
                {
                    sObj = existing.gameObject;
                }
                else
                {
                    sObj = new GameObject(s.sectorId);
                    sObj.transform.SetParent(worldSectorsRoot.transform, false);
                    sObj.transform.position = s.worldCenter;
                }
                sectorRoots[s.sectorId] = sObj;
            }

            // 4. Organize Existing Environment into Sector_0_0_Center
            GameObject centerRoot = sectorRoots["Sector_0_0_Center"];
            GameObject existingEnv = GameObject.Find("Environment");
            if (existingEnv != null && existingEnv.transform.parent != centerRoot.transform)
            {
                existingEnv.transform.SetParent(centerRoot.transform, true);
                Debug.Log("[WorldPerimeterExpander] Reparented existing Environment into Sector_0_0_Center");
            }

            // Adjust or relocate old distant skyline towers if present so they don't block outer sectors
            Transform oldSkyline = centerRoot.transform.Find("Environment/--- DISTANT CITY SKYLINE ---");
            if (oldSkyline != null)
            {
                // Relocate old skyline to true perimeter (beyond 360m)
                oldSkyline.gameObject.SetActive(false); // Clean sightlines into outer sectors
                Debug.Log("[WorldPerimeterExpander] Disabled old 120m skyline in favor of true 360m perimeter");
            }

            // 5. Generate 8 Outer Sector Ground Plates (240x240m each)
            Dictionary<string, Material> sectorGroundMats = new Dictionary<string, Material>
            {
                { "Sector_0_1_North", highwayGroundMat },
                { "Sector_1_0_East", industrialGroundMat },
                { "Sector_1_1_NorthEast", harborGroundMat },
                { "Sector_-1_0_West", parkGroundMat },
                { "Sector_-1_1_NorthWest", lakesideGroundMat },
                { "Sector_0_-1_South", plazaGroundMat },
                { "Sector_-1_-1_SouthWest", floodedGroundMat },
                { "Sector_1_-1_SouthEast", substationGroundMat }
            };

            foreach (var kvp in sectorGroundMats)
            {
                string sectorId = kvp.Key;
                GameObject sRoot = sectorRoots[sectorId];
                Transform existingGround = sRoot.transform.Find($"Ground_{sectorId}");
                if (existingGround == null)
                {
                    GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    ground.name = $"Ground_{sectorId}";
                    ground.transform.SetParent(sRoot.transform, false);
                    ground.transform.localPosition = Vector3.zero;
                    ground.transform.localScale = new Vector3(24f, 1f, 24f); // 240x240m
                    ground.GetComponent<Renderer>().sharedMaterial = kvp.Value;
                    GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);
                }
            }

            // 6. Extend Arterial Avenues (Grand Boulevard & Commercial Avenue to ±350m)
            ExtendNorthSouthGrandBoulevard(sectorRoots, streetAsphaltMat, curbConcreteMat, yellowLineMat);
            ExtendEastWestCommercialAvenue(sectorRoots, streetAsphaltMat, curbConcreteMat, yellowLineMat);

            // 7. Construct 4 Perimeter Checkpoint Gateways at ±120m boundaries
            ConstructNorthHighwayCheckpoint(sectorRoots["Sector_0_1_North"].transform, barrierMat, militaryGreenMat, hazardStripesMat);
            ConstructEastRailCrossingCheckpoint(sectorRoots["Sector_1_0_East"].transform, barrierMat, rustyMetalMat, hazardStripesMat);
            ConstructWestParkArchwayCheckpoint(sectorRoots["Sector_-1_0_West"].transform, barrierMat, woodCabinMat);
            ConstructSouthPlazaBoulevardCheckpoint(sectorRoots["Sector_0_-1_South"].transform, barrierMat, plazaGroundMat, hazardStripesMat);

            // 8. Construct Starter Landmark Silhouettes in Outer Sectors
            ConstructNorthHighwayOverpassLandmarks(sectorRoots["Sector_0_1_North"].transform, barrierMat, militaryGreenMat);
            ConstructEastRailDepotLandmarks(sectorRoots["Sector_1_0_East"].transform, industrialGroundMat, rustyMetalMat);
            ConstructNorthEastHarborLandmarks(sectorRoots["Sector_1_1_NorthEast"].transform, harborGroundMat, rustyMetalMat);
            ConstructWestCityParkLandmarks(sectorRoots["Sector_-1_0_West"].transform, parkGroundMat, woodCabinMat);
            ConstructNorthWestLakesideLandmarks(sectorRoots["Sector_-1_1_NorthWest"].transform, waterMat, woodCabinMat);
            ConstructSouthMetroPlazaLandmarks(sectorRoots["Sector_0_-1_South"].transform, plazaGroundMat, barrierMat);
            ConstructSouthWestFloodedSuburbsLandmarks(sectorRoots["Sector_-1_-1_SouthWest"].transform, floodedGroundMat, waterMat, woodCabinMat);
            ConstructSouthEastSubstationLandmarks(sectorRoots["Sector_1_-1_SouthEast"].transform, substationGroundMat, rustyMetalMat);

            // 9. Build True Perimeter Walls/Cliffs at ±360m
            ConstructWorldPerimeterWalls(worldSectorsRoot.transform, perimeterWallMat);

            // 10. Setup & Wire WorldSectorStreamer Component
            SetupWorldSectorStreamer(worldSectorsRoot, sectorRoots);

            // 11. Re-bake NavMesh for the entire 720x720m world
            Debug.Log("[WorldPerimeterExpander] Re-baking NavMesh across unified 720x720m world...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[WorldPerimeterExpander] NavMesh baking complete.");

            // 12. Bake Upgraded 2048x2048 Cartography Map
            Debug.Log("[WorldPerimeterExpander] Baking 2048x2048 Tactical City Map...");
            CityCartographyBaker.BakeCityMap();

            // 13. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("==================================================================");
            Debug.Log(">>> OPEN-WORLD MAP EXPANSION (720x720m) COMPLETE & SAVED <<<");
            Debug.Log("==================================================================");
        }

        // =========================================================================
        // ROAD EXTENSIONS
        // =========================================================================
        private static void ExtendNorthSouthGrandBoulevard(Dictionary<string, GameObject> sectorRoots, Material roadMat, Material curbMat, Material yellowLineMat)
        {
            // North Extension in Sector_0_1_North (center is 0, 0, 240)
            // World Z range: 120 to 355 -> local Z relative to 240 is -120 to +115 (length 235, center local Z = -2.5)
            Transform northSector = sectorRoots["Sector_0_1_North"].transform;
            Transform existingNorthRoad = northSector.Find("Avenue_GrandBoulevard_North");
            if (existingNorthRoad == null)
            {
                GameObject roadGroup = new GameObject("Avenue_GrandBoulevard_North");
                roadGroup.transform.SetParent(northSector, false);

                // Road surface: 14m wide, 235m long
                CreateBox(roadGroup.transform, "Road_Asphalt", new Vector3(0f, 0.04f, -2.5f), new Vector3(14f, 0.08f, 235f), roadMat, true);
                // Sidewalks
                CreateBox(roadGroup.transform, "Sidewalk_West", new Vector3(-8.5f, 0.12f, -2.5f), new Vector3(3f, 0.16f, 235f), curbMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_East", new Vector3(8.5f, 0.12f, -2.5f), new Vector3(3f, 0.16f, 235f), curbMat, true);

                // Double Yellow Centerlines
                for (float lz = -110f; lz <= 105f; lz += 8f)
                {
                    CreateBox(roadGroup.transform, "YellowLine_L", new Vector3(-0.25f, 0.09f, lz), new Vector3(0.18f, 0.01f, 4.5f), yellowLineMat, false);
                    CreateBox(roadGroup.transform, "YellowLine_R", new Vector3(0.25f, 0.09f, lz), new Vector3(0.18f, 0.01f, 4.5f), yellowLineMat, false);
                }
            }

            // South Extension in Sector_0_-1_South (center is 0, 0, -240)
            // World Z range: -120 to -355 -> local Z relative to -240 is +120 to -115 (length 235, center local Z = 2.5)
            Transform southSector = sectorRoots["Sector_0_-1_South"].transform;
            Transform existingSouthRoad = southSector.Find("Avenue_GrandBoulevard_South");
            if (existingSouthRoad == null)
            {
                GameObject roadGroup = new GameObject("Avenue_GrandBoulevard_South");
                roadGroup.transform.SetParent(southSector, false);

                CreateBox(roadGroup.transform, "Road_Asphalt", new Vector3(0f, 0.04f, 2.5f), new Vector3(14f, 0.08f, 235f), roadMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_West", new Vector3(-8.5f, 0.12f, 2.5f), new Vector3(3f, 0.16f, 235f), curbMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_East", new Vector3(8.5f, 0.12f, 2.5f), new Vector3(3f, 0.16f, 235f), curbMat, true);

                for (float lz = -105f; lz <= 110f; lz += 8f)
                {
                    CreateBox(roadGroup.transform, "YellowLine_L", new Vector3(-0.25f, 0.09f, lz), new Vector3(0.18f, 0.01f, 4.5f), yellowLineMat, false);
                    CreateBox(roadGroup.transform, "YellowLine_R", new Vector3(0.25f, 0.09f, lz), new Vector3(0.18f, 0.01f, 4.5f), yellowLineMat, false);
                }
            }
        }

        private static void ExtendEastWestCommercialAvenue(Dictionary<string, GameObject> sectorRoots, Material roadMat, Material curbMat, Material yellowLineMat)
        {
            // East Extension in Sector_1_0_East (center is 240, 0, 0)
            // World X range: 120 to 355 -> local X relative to 240 is -120 to +115 (length 235, center local X = -2.5)
            Transform eastSector = sectorRoots["Sector_1_0_East"].transform;
            Transform existingEastRoad = eastSector.Find("Avenue_Commercial_East");
            if (existingEastRoad == null)
            {
                GameObject roadGroup = new GameObject("Avenue_Commercial_East");
                roadGroup.transform.SetParent(eastSector, false);

                CreateBox(roadGroup.transform, "Road_Asphalt", new Vector3(-2.5f, 0.04f, 0f), new Vector3(235f, 0.08f, 14f), roadMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_North", new Vector3(-2.5f, 0.12f, 8.5f), new Vector3(235f, 0.16f, 3f), curbMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_South", new Vector3(-2.5f, 0.12f, -8.5f), new Vector3(235f, 0.16f, 3f), curbMat, true);

                for (float lx = -110f; lx <= 105f; lx += 8f)
                {
                    CreateBox(roadGroup.transform, "YellowLine_N", new Vector3(lx, 0.09f, 0.25f), new Vector3(4.5f, 0.01f, 0.18f), yellowLineMat, false);
                    CreateBox(roadGroup.transform, "YellowLine_S", new Vector3(lx, 0.09f, -0.25f), new Vector3(4.5f, 0.01f, 0.18f), yellowLineMat, false);
                }
            }

            // West Extension in Sector_-1_0_West (center is -240, 0, 0)
            // World X range: -120 to -355 -> local X relative to -240 is +120 to -115 (length 235, center local X = 2.5)
            Transform westSector = sectorRoots["Sector_-1_0_West"].transform;
            Transform existingWestRoad = westSector.Find("Avenue_Commercial_West");
            if (existingWestRoad == null)
            {
                GameObject roadGroup = new GameObject("Avenue_Commercial_West");
                roadGroup.transform.SetParent(westSector, false);

                CreateBox(roadGroup.transform, "Road_Asphalt", new Vector3(2.5f, 0.04f, 0f), new Vector3(235f, 0.08f, 14f), roadMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_North", new Vector3(2.5f, 0.12f, 8.5f), new Vector3(235f, 0.16f, 3f), curbMat, true);
                CreateBox(roadGroup.transform, "Sidewalk_South", new Vector3(2.5f, 0.12f, -8.5f), new Vector3(235f, 0.16f, 3f), curbMat, true);

                for (float lx = -105f; lx <= 110f; lx += 8f)
                {
                    CreateBox(roadGroup.transform, "YellowLine_N", new Vector3(lx, 0.09f, 0.25f), new Vector3(4.5f, 0.01f, 0.18f), yellowLineMat, false);
                    CreateBox(roadGroup.transform, "YellowLine_S", new Vector3(lx, 0.09f, -0.25f), new Vector3(4.5f, 0.01f, 0.18f), yellowLineMat, false);
                }
            }
        }

        // =========================================================================
        // 4 PERIMETER CHECKPOINT GATEWAYS (at ±120m)
        // =========================================================================
        private static void ConstructNorthHighwayCheckpoint(Transform parent, Material barrierMat, Material militaryMat, Material hazardMat)
        {
            // World pos: (0, 0, 120) -> relative to Sector_0_1_North (center: 0, 0, 240) local pos is (0, 0, -120)
            Transform existing = parent.Find("Checkpoint_NorthHighwayGate");
            if (existing != null) return;

            GameObject cp = new GameObject("Checkpoint_NorthHighwayGate");
            cp.transform.SetParent(parent, false);
            cp.transform.localPosition = new Vector3(0f, 0f, -120f);

            // Left Barricade Wing (West side, x = -5 to -15)
            CreateBox(cp.transform, "Barrier_Left_1", new Vector3(-8.5f, 0.8f, 0f), new Vector3(7f, 1.6f, 1.2f), barrierMat, true);
            CreateBox(cp.transform, "GuardPost_Left", new Vector3(-13.5f, 1.5f, 2f), new Vector3(4f, 3f, 4f), militaryMat, true);
            // Right Barricade Wing (East side, x = +5 to +15)
            CreateBox(cp.transform, "Barrier_Right_1", new Vector3(8.5f, 0.8f, 0f), new Vector3(7f, 1.6f, 1.2f), barrierMat, true);
            CreateBox(cp.transform, "SandbagBunker_Right", new Vector3(13.5f, 1.0f, 1.5f), new Vector3(4f, 2f, 3f), militaryMat, true);

            // Overhead Military Truss Archway spanning across the road (Clear height 5.5m)
            CreateBox(cp.transform, "Pillar_Left", new Vector3(-5.5f, 3f, 0f), new Vector3(0.8f, 6f, 0.8f), militaryMat, false);
            CreateBox(cp.transform, "Pillar_Right", new Vector3(5.5f, 3f, 0f), new Vector3(0.8f, 6f, 0.8f), militaryMat, false);
            CreateBox(cp.transform, "Truss_Beam", new Vector3(0f, 5.8f, 0f), new Vector3(12f, 0.6f, 0.8f), militaryMat, false);

            // Signpost
            CreateSignpost(cp.transform, "Sign_NorthGate", new Vector3(0f, 5.1f, 0.5f), new Vector3(7f, 1.0f, 0.1f),
                "KUZEY: ASKERİ TAHLİYE BÖLGESİ ->", hazardMat);

            // Open Center Gateway: Width 8m (from x = -4 to +4) on the road surface is COMPLETELY UNOBSTRUCTED
        }

        private static void ConstructEastRailCrossingCheckpoint(Transform parent, Material barrierMat, Material rustyMetalMat, Material hazardMat)
        {
            // World pos: (120, 0, 0) -> relative to Sector_1_0_East (center: 240, 0, 0) local pos is (-120, 0, 0)
            Transform existing = parent.Find("Checkpoint_EastRailCrossing");
            if (existing != null) return;

            GameObject cp = new GameObject("Checkpoint_EastRailCrossing");
            cp.transform.SetParent(parent, false);
            cp.transform.localPosition = new Vector3(-120f, 0f, 0f);

            // Rail tracks embedded in asphalt crossing avenue (North-South)
            CreateBox(cp.transform, "RailTrack_L", new Vector3(-1f, 0.06f, 0f), new Vector3(0.15f, 0.05f, 22f), rustyMetalMat, false);
            CreateBox(cp.transform, "RailTrack_R", new Vector3(1f, 0.06f, 0f), new Vector3(0.15f, 0.05f, 22f), rustyMetalMat, false);

            // Railroad crossing signal towers on sidewalk (North and South)
            CreateBox(cp.transform, "SignalTower_North", new Vector3(-3f, 2.5f, 8.5f), new Vector3(0.5f, 5f, 0.5f), rustyMetalMat, true);
            CreateBox(cp.transform, "Crossbuck_North", new Vector3(-3f, 4.5f, 8.5f), new Vector3(2f, 0.4f, 0.1f), hazardMat, false);

            CreateBox(cp.transform, "SignalTower_South", new Vector3(-3f, 2.5f, -8.5f), new Vector3(0.5f, 5f, 0.5f), rustyMetalMat, true);
            CreateBox(cp.transform, "Crossbuck_South", new Vector3(-3f, 4.5f, -8.5f), new Vector3(2f, 0.4f, 0.1f), hazardMat, false);

            // Signpost
            CreateSignpost(cp.transform, "Sign_EastCrossing", new Vector3(-3f, 3.2f, 8.5f), new Vector3(5f, 0.8f, 0.1f),
                "DOĞU: SANAYİ & TREN GARI ->", hazardMat);

            // Center Roadway (z = -4 to +4) is completely open for vehicles and survivors
        }

        private static void ConstructWestParkArchwayCheckpoint(Transform parent, Material barrierMat, Material woodMat)
        {
            // World pos: (-120, 0, 0) -> relative to Sector_-1_0_West (center: -240, 0, 0) local pos is (120, 0, 0)
            Transform existing = parent.Find("Checkpoint_WestParkGate");
            if (existing != null) return;

            GameObject cp = new GameObject("Checkpoint_WestParkGate");
            cp.transform.SetParent(parent, false);
            cp.transform.localPosition = new Vector3(120f, 0f, 0f);

            // Stone Entry Pillars on both sides of Avenue
            CreateBox(cp.transform, "StonePillar_North", new Vector3(0f, 2.8f, 6.5f), new Vector3(1.4f, 5.6f, 1.4f), barrierMat, true);
            CreateBox(cp.transform, "StonePillar_South", new Vector3(0f, 2.8f, -6.5f), new Vector3(1.4f, 5.6f, 1.4f), barrierMat, true);

            // Decorative Wrought-iron Archway Overhead (Clearance 5.2m)
            CreateBox(cp.transform, "ParkArch_Beam", new Vector3(0f, 5.4f, 0f), new Vector3(0.6f, 0.5f, 14f), barrierMat, false);

            // Road Sign
            CreateSignpost(cp.transform, "Sign_WestPark", new Vector3(-0.4f, 4.7f, 0f), new Vector3(0.1f, 0.8f, 6f),
                "BATI: ŞEHİR ORMANI & GÖL ->", woodMat);

            // Park boundary iron fences extending outward
            CreateBox(cp.transform, "Fence_North", new Vector3(0f, 1.2f, 13f), new Vector3(0.3f, 2.4f, 12f), barrierMat, true);
            CreateBox(cp.transform, "Fence_South", new Vector3(0f, 1.2f, -13f), new Vector3(0.3f, 2.4f, 12f), barrierMat, true);

            // Center Roadway (z = -5 to +5) is open
        }

        private static void ConstructSouthPlazaBoulevardCheckpoint(Transform parent, Material barrierMat, Material plazaMat, Material hazardMat)
        {
            // World pos: (0, 0, -120) -> relative to Sector_0_-1_South (center: 0, 0, -240) local pos is (0, 0, 120)
            Transform existing = parent.Find("Checkpoint_SouthPlazaBoulevard");
            if (existing != null) return;

            GameObject cp = new GameObject("Checkpoint_SouthPlazaBoulevard");
            cp.transform.SetParent(parent, false);
            cp.transform.localPosition = new Vector3(0f, 0f, 120f);

            // Bollard rows on sidewalk edges
            for (float x = 5.5f; x <= 14f; x += 2.5f)
            {
                CreateBox(cp.transform, $"Bollard_East_{x}", new Vector3(x, 0.5f, 0f), new Vector3(0.4f, 1.0f, 0.4f), barrierMat, true);
                CreateBox(cp.transform, $"Bollard_West_{-x}", new Vector3(-x, 0.5f, 0f), new Vector3(0.4f, 1.0f, 0.4f), barrierMat, true);
            }

            // Metro Entrance Canopy off to the East sidewalk
            GameObject metroCanopy = new GameObject("MetroCanopy_SouthGate");
            metroCanopy.transform.SetParent(cp.transform, false);
            metroCanopy.transform.localPosition = new Vector3(12f, 0f, -5f);
            CreateBox(metroCanopy.transform, "Stairwell_Cut", new Vector3(0f, -0.2f, 0f), new Vector3(4f, 0.4f, 8f), plazaMat, true);
            CreateBox(metroCanopy.transform, "Roof_Glass", new Vector3(0f, 2.8f, 0f), new Vector3(4.5f, 0.3f, 8.5f), barrierMat, false);

            // Signpost
            CreateSignpost(cp.transform, "Sign_SouthPlaza", new Vector3(6f, 3f, 1f), new Vector3(6f, 0.8f, 0.1f),
                "GÜNEY: METRO & AVM MEYDANI ->", hazardMat);

            // Center Roadway (x = -4.5 to +4.5) is open
        }

        // =========================================================================
        // STARTER LANDMARKS IN THE 8 OUTER SECTORS
        // =========================================================================
        private static void ConstructNorthHighwayOverpassLandmarks(Transform parent, Material concreteMat, Material militaryMat)
        {
            // Center of Sector_0_1_North is (0, 0, 240)
            Transform existing = parent.Find("Landmark_HighwayOverpass");
            if (existing != null) return;

            GameObject overpass = new GameObject("Landmark_HighwayOverpass");
            overpass.transform.SetParent(parent, false);
            overpass.transform.localPosition = Vector3.zero;

            // Massive elevated highway span crossing overhead from East to West (Clearance 7.5m)
            CreateBox(overpass.transform, "Overpass_Deck", new Vector3(0f, 8f, 0f), new Vector3(160f, 1.5f, 22f), concreteMat, true);
            // Concrete support pillars
            CreateBox(overpass.transform, "Pillar_Center_W", new Vector3(-9.5f, 4f, 0f), new Vector3(2.5f, 8f, 2.5f), concreteMat, true);
            CreateBox(overpass.transform, "Pillar_Center_E", new Vector3(9.5f, 4f, 0f), new Vector3(2.5f, 8f, 2.5f), concreteMat, true);
            CreateBox(overpass.transform, "Pillar_Far_W", new Vector3(-45f, 4f, 0f), new Vector3(3f, 8f, 3f), concreteMat, true);
            CreateBox(overpass.transform, "Pillar_Far_E", new Vector3(45f, 4f, 0f), new Vector3(3f, 8f, 3f), concreteMat, true);

            // Military Outpost under the overpass
            CreateBox(overpass.transform, "Convoy_Truck_Wreck", new Vector3(-18f, 1.8f, -4f), new Vector3(3f, 2.8f, 8f), militaryMat, true);
            CreateBox(overpass.transform, "Command_Tent", new Vector3(22f, 1.6f, 6f), new Vector3(8f, 3.2f, 6f), militaryMat, true);
        }

        private static void ConstructEastRailDepotLandmarks(Transform parent, Material groundMat, Material metalMat)
        {
            // Center of Sector_1_0_East is (240, 0, 0)
            Transform existing = parent.Find("Landmark_EastRailDepot");
            if (existing != null) return;

            GameObject depot = new GameObject("Landmark_EastRailDepot");
            depot.transform.SetParent(parent, false);
            depot.transform.localPosition = Vector3.zero;

            // Industrial logistics warehouse silhouettes
            CreateBox(depot.transform, "Warehouse_North", new Vector3(15f, 7f, 45f), new Vector3(55f, 14f, 30f), groundMat, true);
            CreateBox(depot.transform, "Warehouse_South", new Vector3(15f, 6f, -45f), new Vector3(45f, 12f, 28f), groundMat, true);

            // Cargo train boxcars on siding tracks
            CreateBox(depot.transform, "Train_Boxcar_1", new Vector3(-15f, 2.4f, 20f), new Vector3(3.4f, 4.2f, 16f), metalMat, true);
            CreateBox(depot.transform, "Train_Boxcar_2", new Vector3(-15f, 2.4f, 42f), new Vector3(3.4f, 4.2f, 16f), metalMat, true);
            CreateBox(depot.transform, "Train_Tanker", new Vector3(-20f, 2.2f, -25f), new Vector3(3.2f, 3.8f, 14f), metalMat, true);
        }

        private static void ConstructNorthEastHarborLandmarks(Transform parent, Material groundMat, Material metalMat)
        {
            // Center of Sector_1_1_NorthEast is (240, 0, 240)
            Transform existing = parent.Find("Landmark_ContainerHarbor");
            if (existing != null) return;

            GameObject harbor = new GameObject("Landmark_ContainerHarbor");
            harbor.transform.SetParent(parent, false);
            harbor.transform.localPosition = Vector3.zero;

            // Multi-colored Shipping Container Stacks
            Color[] containerCols = new Color[] {
                new Color(0.85f, 0.35f, 0.15f), // Orange
                new Color(0.18f, 0.45f, 0.75f), // Blue
                new Color(0.25f, 0.55f, 0.30f), // Green
                new Color(0.65f, 0.20f, 0.20f)  // Crimson
            };

            for (int i = 0; i < 6; i++)
            {
                float x = -30f + (i * 14f);
                Material cMat = SceneBuilder.GetOrCreateMaterial($"Mat_Container_{i % 4}", containerCols[i % 4]);
                // Stack of 2
                CreateBox(harbor.transform, $"Container_{i}_A", new Vector3(x, 1.4f, 20f), new Vector3(3f, 2.8f, 12f), cMat, true);
                CreateBox(harbor.transform, $"Container_{i}_B", new Vector3(x, 4.2f, 20f), new Vector3(3f, 2.8f, 12f), cMat, true);
            }

            // Gantry Crane Frame Silhouette
            CreateBox(harbor.transform, "Gantry_Leg_L", new Vector3(-20f, 11f, -20f), new Vector3(1.5f, 22f, 1.5f), metalMat, true);
            CreateBox(harbor.transform, "Gantry_Leg_R", new Vector3(20f, 11f, -20f), new Vector3(1.5f, 22f, 1.5f), metalMat, true);
            CreateBox(harbor.transform, "Gantry_Girder", new Vector3(0f, 21.5f, -20f), new Vector3(45f, 2f, 2f), metalMat, false);
        }

        private static void ConstructWestCityParkLandmarks(Transform parent, Material parkMat, Material woodMat)
        {
            // Center of Sector_-1_0_West is (-240, 0, 0)
            Transform existing = parent.Find("Landmark_WestCityPark");
            if (existing != null) return;

            GameObject park = new GameObject("Landmark_WestCityPark");
            park.transform.SetParent(parent, false);
            park.transform.localPosition = Vector3.zero;

            // Stone Park Pavilion at park center
            CreateBox(park.transform, "Pavilion_Floor", new Vector3(0f, 0.4f, 0f), new Vector3(16f, 0.8f, 16f), parkMat, true);
            CreateBox(park.transform, "Pavilion_Pillar_1", new Vector3(-6f, 3f, -6f), new Vector3(0.8f, 5f, 0.8f), parkMat, true);
            CreateBox(park.transform, "Pavilion_Pillar_2", new Vector3(6f, 3f, -6f), new Vector3(0.8f, 5f, 0.8f), parkMat, true);
            CreateBox(park.transform, "Pavilion_Pillar_3", new Vector3(-6f, 3f, 6f), new Vector3(0.8f, 5f, 0.8f), parkMat, true);
            CreateBox(park.transform, "Pavilion_Pillar_4", new Vector3(6f, 3f, 6f), new Vector3(0.8f, 5f, 0.8f), parkMat, true);
            CreateBox(park.transform, "Pavilion_Roof", new Vector3(0f, 6.2f, 0f), new Vector3(18f, 1.5f, 18f), woodMat, false);

            // Scattered Park Trees & Shrubberies
            Material trunkMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Trunk", new Color(0.28f, 0.20f, 0.14f));
            Material foliageMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Foliage", new Color(0.12f, 0.32f, 0.16f));

            float[] treeOffsets = new float[] { -35f, -18f, 22f, 40f, -50f, 55f };
            for (int t = 0; t < treeOffsets.Length; t++)
            {
                float ox = treeOffsets[t];
                float oz = treeOffsets[(t + 2) % treeOffsets.Length] * 0.7f;
                GameObject tree = new GameObject($"ParkTree_{t}");
                tree.transform.SetParent(park.transform, false);
                tree.transform.localPosition = new Vector3(ox, 0f, oz);

                CreateBox(tree.transform, "Trunk", new Vector3(0f, 3f, 0f), new Vector3(0.9f, 6f, 0.9f), trunkMat, true);
                CreateBox(tree.transform, "Foliage", new Vector3(0f, 7.5f, 0f), new Vector3(5f, 5f, 5f), foliageMat, false);
            }
        }

        private static void ConstructNorthWestLakesideLandmarks(Transform parent, Material waterMat, Material woodMat)
        {
            // Center of Sector_-1_1_NorthWest is (-240, 0, 240)
            Transform existing = parent.Find("Landmark_LakesideWoodlands");
            if (existing != null) return;

            GameObject lakeside = new GameObject("Landmark_LakesideWoodlands");
            lakeside.transform.SetParent(parent, false);
            lakeside.transform.localPosition = Vector3.zero;

            // Murky Lake Water Plane
            GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "Lake_Water_Surface";
            waterPlane.transform.SetParent(lakeside.transform, false);
            waterPlane.transform.localPosition = new Vector3(20f, 0.05f, 20f);
            waterPlane.transform.localScale = new Vector3(9f, 1f, 9f);
            waterPlane.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Wooden Pier extending into the lake
            CreateBox(lakeside.transform, "Pier_Planks", new Vector3(-12f, 0.35f, 20f), new Vector3(24f, 0.4f, 3.5f), woodMat, true);

            // Ranger Cabins
            CreateBox(lakeside.transform, "Cabin_A", new Vector3(-35f, 2.5f, 0f), new Vector3(12f, 5f, 10f), woodMat, true);
            CreateBox(lakeside.transform, "Cabin_B", new Vector3(-45f, 2.2f, 30f), new Vector3(10f, 4.4f, 8f), woodMat, true);
        }

        private static void ConstructSouthMetroPlazaLandmarks(Transform parent, Material plazaMat, Material concreteMat)
        {
            // Center of Sector_0_-1_South is (0, 0, -240)
            Transform existing = parent.Find("Landmark_SouthMetroPlaza");
            if (existing != null) return;

            GameObject plaza = new GameObject("Landmark_SouthMetroPlaza");
            plaza.transform.SetParent(parent, false);
            plaza.transform.localPosition = Vector3.zero;

            // Shopping Mall Facade Silhouettes
            CreateBox(plaza.transform, "Mall_Facade_East", new Vector3(35f, 9f, 0f), new Vector3(32f, 18f, 75f), concreteMat, true);
            CreateBox(plaza.transform, "Mall_Facade_West", new Vector3(-35f, 8f, 0f), new Vector3(28f, 16f, 65f), concreteMat, true);

            // Grand Metro Station Glass Entrance Pyramid
            CreateBox(plaza.transform, "Metro_Pyramid_Base", new Vector3(0f, 0.5f, 30f), new Vector3(18f, 1.0f, 18f), plazaMat, true);
            CreateBox(plaza.transform, "Metro_Glass_Peak", new Vector3(0f, 3.5f, 30f), new Vector3(12f, 5.0f, 12f), concreteMat, false);
        }

        private static void ConstructSouthWestFloodedSuburbsLandmarks(Transform parent, Material groundMat, Material waterMat, Material woodMat)
        {
            // Center of Sector_-1_-1_SouthWest is (-240, 0, -240)
            Transform existing = parent.Find("Landmark_FloodedSuburbs");
            if (existing != null) return;

            GameObject flooded = new GameObject("Landmark_FloodedSuburbs");
            flooded.transform.SetParent(parent, false);
            flooded.transform.localPosition = Vector3.zero;

            // Shallow floodwater sheet
            GameObject floodPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floodPlane.name = "Flood_Water_Sheet";
            floodPlane.transform.SetParent(flooded.transform, false);
            floodPlane.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            floodPlane.transform.localScale = new Vector3(14f, 1f, 14f);
            floodPlane.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Submerged / Tilted House Roofs
            CreateBox(flooded.transform, "Submerged_House_1", new Vector3(-25f, 1.2f, 15f), new Vector3(12f, 3.5f, 12f), woodMat, true);
            CreateBox(flooded.transform, "Submerged_House_2", new Vector3(25f, 0.9f, -20f), new Vector3(14f, 3.0f, 10f), woodMat, true);
            CreateBox(flooded.transform, "Submerged_House_3", new Vector3(-15f, 1.0f, -35f), new Vector3(11f, 3.2f, 11f), woodMat, true);
        }

        private static void ConstructSouthEastSubstationLandmarks(Transform parent, Material groundMat, Material metalMat)
        {
            // Center of Sector_1_-1_SouthEast is (240, 0, -240)
            Transform existing = parent.Find("Landmark_PowerSubstation");
            if (existing != null) return;

            GameObject sub = new GameObject("Landmark_PowerSubstation");
            sub.transform.SetParent(parent, false);
            sub.transform.localPosition = Vector3.zero;

            // Electrical Transformer Banks
            for (int i = 0; i < 4; i++)
            {
                float z = -20f + (i * 14f);
                CreateBox(sub.transform, $"Transformer_{i}", new Vector3(0f, 2.2f, z), new Vector3(6f, 4.4f, 4.5f), metalMat, true);
            }

            // High Voltage Transmission Pylon Silhouette
            CreateBox(sub.transform, "Pylon_Base_L", new Vector3(-18f, 12f, 0f), new Vector3(1.2f, 24f, 1.2f), metalMat, true);
            CreateBox(sub.transform, "Pylon_Base_R", new Vector3(-12f, 12f, 0f), new Vector3(1.2f, 24f, 1.2f), metalMat, true);
            CreateBox(sub.transform, "Pylon_Crossarm_1", new Vector3(-15f, 18f, 0f), new Vector3(12f, 1.2f, 1.2f), metalMat, false);
            CreateBox(sub.transform, "Pylon_Crossarm_2", new Vector3(-15f, 22f, 0f), new Vector3(8f, 1.2f, 1.2f), metalMat, false);
        }

        // =========================================================================
        // TRUE WORLD PERIMETER BOUNDARY (AT ±360m)
        // =========================================================================
        private static void ConstructWorldPerimeterWalls(Transform parent, Material wallMat)
        {
            Transform existing = parent.Find("--- WORLD BORDER WALLS ---");
            if (existing != null) return;

            GameObject borders = new GameObject("--- WORLD BORDER WALLS ---");
            borders.transform.SetParent(parent, false);

            float halfSize = WorldMapConfig.HalfWorldSize; // 360m
            float wallHeight = 8f;
            float wallThickness = 6f;
            float totalLen = WorldMapConfig.TotalWorldSize + 20f; // 740m

            // North Border (z = +360)
            CreateBox(borders.transform, "Border_North", new Vector3(0f, wallHeight * 0.5f, halfSize + (wallThickness * 0.5f)),
                new Vector3(totalLen, wallHeight, wallThickness), wallMat, true);

            // South Border (z = -360)
            CreateBox(borders.transform, "Border_South", new Vector3(0f, wallHeight * 0.5f, -halfSize - (wallThickness * 0.5f)),
                new Vector3(totalLen, wallHeight, wallThickness), wallMat, true);

            // East Border (x = +360)
            CreateBox(borders.transform, "Border_East", new Vector3(halfSize + (wallThickness * 0.5f), wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, totalLen), wallMat, true);

            // West Border (x = -360)
            CreateBox(borders.transform, "Border_West", new Vector3(-halfSize - (wallThickness * 0.5f), wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, totalLen), wallMat, true);
        }

        // =========================================================================
        // STREAMER SETUP
        // =========================================================================
        private static void SetupWorldSectorStreamer(GameObject worldSectorsRoot, Dictionary<string, GameObject> sectorRoots)
        {
            WorldSectorStreamer streamer = worldSectorsRoot.GetComponent<WorldSectorStreamer>();
            if (streamer == null)
            {
                streamer = worldSectorsRoot.AddComponent<WorldSectorStreamer>();
            }

            streamer.AutoDiscoverSectorRootsIfMissing();
            EditorUtility.SetDirty(streamer);
            Debug.Log("[WorldPerimeterExpander] Configured WorldSectorStreamer with 9 Sector Bindings");
        }

        // =========================================================================
        // UTILITIES
        // =========================================================================
        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool navStatic)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;

            if (mat != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }

            if (navStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
            }
            return go;
        }

        private static GameObject CreateSignpost(Transform parent, string name, Vector3 localPos, Vector3 size, string signText, Material bgMat)
        {
            GameObject sign = CreateBox(parent, name, localPos, size, bgMat, false);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(sign.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = signText;
            tm.fontSize = 24;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            return sign;
        }
    }
}
