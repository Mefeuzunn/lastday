using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    public static class CityLayoutBuilder
    {
        public static void BuildExpandedCity(Transform envParent, GameObject lootPrefab)
        {
            // ==========================================
            // 1. MATERIAL SETUP (Strictly URP Lit & PBR)
            // ==========================================
            Material groundMat = SceneBuilder.GetOrCreateMaterial("Mat_Ground", new Color(0.118f, 0.13f, 0.15f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(25f, 25f));
            Material streetMat = SceneBuilder.GetOrCreateMaterial("Mat_Street", new Color(0.137f, 0.153f, 0.180f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(15f, 15f));
            Material curbMat = SceneBuilder.GetOrCreateMaterial("Mat_Curb", new Color(0.40f, 0.42f, 0.45f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(6f, 6f));
            Material whiteMarkMat = SceneBuilder.GetOrCreateMaterial("Mat_RoadMarking", new Color(0.94f, 0.92f, 0.88f));
            Material yellowMarkMat = SceneBuilder.GetOrCreateMaterial("Mat_RoadMarking_Yellow", new Color(0.96f, 0.78f, 0.20f));
            Material lampMat = SceneBuilder.GetOrCreateMaterial("Mat_StreetLamp", new Color(0.18f, 0.20f, 0.24f));
            Material bulbMat = SceneBuilder.GetOrCreateMaterial("Mat_LampBulb", new Color(1.0f, 0.718f, 0.012f));
            Material barrierMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Barrier", new Color(0.48f, 0.50f, 0.53f),
                "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(2f, 2f));
            Material sandbagMat = SceneBuilder.GetOrCreateMaterial("Mat_Sandbag_Khaki", new Color(0.48f, 0.42f, 0.32f));
            Material wireMat = SceneBuilder.GetOrCreateMaterial("Mat_Wire_Mesh", new Color(0.35f, 0.38f, 0.40f));
            Material hazardMat = SceneBuilder.GetOrCreateMaterial("Mat_Warning_Hazard", new Color(0.95f, 0.75f, 0.10f));
            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_Cover_Wood", new Color(1f, 1f, 1f),
                "Assets/_Project/Textures/Tex_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Wood_Normal.png", new Vector2(2f, 2f));
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Chest_Metal", new Color(0.35f, 0.38f, 0.42f));
            Material clinicMat = SceneBuilder.GetOrCreateMaterial("Mat_Building_Clinic", new Color(0.95f, 0.95f, 1f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(8f, 6f));
            Material wallMat = SceneBuilder.GetOrCreateMaterial("Mat_Building_Wall", new Color(0.85f, 0.85f, 0.85f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(8f, 6f));
            Material frameMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_Frame", new Color(0.12f, 0.14f, 0.16f));
            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_Glass", new Color(0.06f, 0.08f, 0.12f));
            Material litMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_WarmLit", new Color(1.0f, 0.82f, 0.42f));
            Material doorMat = SceneBuilder.GetOrCreateMaterial("Mat_Door_Wood", new Color(0.35f, 0.22f, 0.14f));
            Material signSuperMat = SceneBuilder.GetOrCreateMaterial("Mat_Sign_Supermarket", new Color(0.12f, 0.42f, 0.32f));
            Material signPharmMat = SceneBuilder.GetOrCreateMaterial("Mat_Sign_Pharmacy", new Color(0.10f, 0.38f, 0.62f));
            Material skylineMat = SceneBuilder.GetOrCreateMaterial("Mat_Skyline_Block", new Color(0.045f, 0.055f, 0.075f));
            Material redBeaconMat = SceneBuilder.GetOrCreateMaterial("Mat_Beacon_Red", new Color(1.0f, 0.08f, 0.08f));
            Material policeBlueMat = SceneBuilder.GetOrCreateMaterial("Mat_Police_Blue_Band", new Color(0.12f, 0.24f, 0.54f));
            Material redCrossMat = SceneBuilder.GetOrCreateMaterial("Mat_RedCross_Glow", new Color(1.0f, 0.15f, 0.15f));
            redCrossMat.EnableKeyword("_EMISSION");
            redCrossMat.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.15f) * 2.0f);

            Material greenSafeMat = SceneBuilder.GetOrCreateMaterial("Mat_SafeHouse_GreenGlow", new Color(0.15f, 0.85f, 0.35f));
            greenSafeMat.EnableKeyword("_EMISSION");
            greenSafeMat.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.35f) * 2.0f);

            // ==========================================
            // 2. LARGE GROUND TERRAIN (240x240m)
            // ==========================================
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Metropolis";
            ground.transform.SetParent(envParent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(24f, 1f, 24f);
            if (groundMat != null) ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);

            // ==========================================
            // 3. ROAD NETWORK (Avenues & Cross streets)
            // ==========================================
            GameObject roadsParent = new GameObject("--- ROAD NETWORK ---");
            roadsParent.transform.SetParent(envParent);

            CreateRoadPiece(roadsParent.transform, "Road_MainAvenue_N", new Vector3(0f, 0.04f, 55f), new Vector3(14f, 0.08f, 110f), streetMat);
            CreateRoadPiece(roadsParent.transform, "Road_MainAvenue_S", new Vector3(0f, 0.04f, -55f), new Vector3(14f, 0.08f, 110f), streetMat);

            CreateRoadPiece(roadsParent.transform, "Road_CrossBlvd_Center_E", new Vector3(55f, 0.04f, 0f), new Vector3(110f, 0.08f, 12f), streetMat);
            CreateRoadPiece(roadsParent.transform, "Road_CrossBlvd_Center_W", new Vector3(-55f, 0.04f, 0f), new Vector3(110f, 0.08f, 12f), streetMat);

            CreateRoadPiece(roadsParent.transform, "Road_CrossStreet_North", new Vector3(0f, 0.04f, 50f), new Vector3(160f, 0.08f, 10f), streetMat);
            CreateRoadPiece(roadsParent.transform, "Road_CrossStreet_South", new Vector3(0f, 0.04f, -50f), new Vector3(160f, 0.08f, 10f), streetMat);

            CreateRoadPiece(roadsParent.transform, "Road_ResStreet_West", new Vector3(-45f, 0.04f, 0f), new Vector3(9f, 0.08f, 140f), streetMat);
            CreateRoadPiece(roadsParent.transform, "Road_CommAlley_East", new Vector3(45f, 0.04f, 0f), new Vector3(9f, 0.08f, 140f), streetMat);

            // Sidewalks & Curbs
            CreateSidewalk(roadsParent.transform, "Sidewalk_Avenue_W", new Vector3(-8.5f, 0.12f, 0f), new Vector3(3.0f, 0.16f, 210f), curbMat);
            CreateSidewalk(roadsParent.transform, "Sidewalk_Avenue_E", new Vector3(8.5f, 0.12f, 0f), new Vector3(3.0f, 0.16f, 210f), curbMat);
            CreateSidewalk(roadsParent.transform, "Sidewalk_North_N", new Vector3(0f, 0.12f, 56.5f), new Vector3(170f, 0.16f, 2.5f), curbMat);
            CreateSidewalk(roadsParent.transform, "Sidewalk_North_S", new Vector3(0f, 0.12f, 43.5f), new Vector3(170f, 0.16f, 2.5f), curbMat);
            CreateSidewalk(roadsParent.transform, "Sidewalk_South_N", new Vector3(0f, 0.12f, -43.5f), new Vector3(170f, 0.16f, 2.5f), curbMat);
            CreateSidewalk(roadsParent.transform, "Sidewalk_South_S", new Vector3(0f, 0.12f, -56.5f), new Vector3(170f, 0.16f, 2.5f), curbMat);

            // Double Yellow Lines
            for (float z = -95f; z <= 95f; z += 6f)
            {
                if (Mathf.Abs(z) < 10f || Mathf.Abs(z - 50f) < 8f || Mathf.Abs(z + 50f) < 8f) continue;
                CreateRoadMark(roadsParent.transform, new Vector3(-0.15f, 0.09f, z), new Vector3(0.16f, 0.01f, 3.8f), yellowMarkMat);
                CreateRoadMark(roadsParent.transform, new Vector3(0.15f, 0.09f, z), new Vector3(0.16f, 0.01f, 3.8f), yellowMarkMat);
            }

            // Zebra Crossings
            CreateZebraCrossing(roadsParent.transform, new Vector3(0f, 0.09f, -10f), true, whiteMarkMat);
            CreateZebraCrossing(roadsParent.transform, new Vector3(0f, 0.09f, 10f), true, whiteMarkMat);
            CreateZebraCrossing(roadsParent.transform, new Vector3(0f, 0.09f, 42f), true, whiteMarkMat);
            CreateZebraCrossing(roadsParent.transform, new Vector3(0f, 0.09f, 58f), true, whiteMarkMat);
            CreateZebraCrossing(roadsParent.transform, new Vector3(-10f, 0.09f, 0f), false, whiteMarkMat);
            CreateZebraCrossing(roadsParent.transform, new Vector3(10f, 0.09f, 0f), false, whiteMarkMat);

            // ==========================================
            // 4. QUARANTINE CHECKPOINT (FAZ 3 - MERKEZ BULVAR)
            // ==========================================
            CreateQuarantineCheckpoint(envParent, new Vector3(0f, 0.08f, -11f), barrierMat, sandbagMat, wireMat, hazardMat, metalMat);

            // Procedural Asphalt Decals (Skidmarks, Oil Puddles, Cracks)
            CreateStreetDecals(envParent);

            // ==========================================
            // 5. HIGH-RISE SKYSCRAPERS (15-40 Stories with Scattered Window Lights)
            // ==========================================
            GameObject towerParent = new GameObject("--- DOWNTOWN SKYSCRAPERS ---");
            towerParent.transform.SetParent(envParent);

            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_ApexTower", new Vector3(24f, 0f, 75f), new Vector3(20f, 105f, 22f), wallMat, glassMat, litMat, redBeaconMat, hasHelipad: true);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_Metropolitan", new Vector3(-25f, 0f, 80f), new Vector3(22f, 85f, 20f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_FinancialEast", new Vector3(32f, 0f, -22f), new Vector3(18f, 72f, 18f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_TelecomSouth", new Vector3(26f, 0f, -80f), new Vector3(19f, 92f, 21f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_MediaCenter", new Vector3(-28f, 0f, -82f), new Vector3(20f, 65f, 20f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_WestGate", new Vector3(-75f, 0f, 25f), new Vector3(18f, 58f, 18f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_NECorp", new Vector3(72f, 0f, 65f), new Vector3(21f, 76f, 19f), wallMat, glassMat, litMat, redBeaconMat);
            CreateEpicSkyscraper(towerParent.transform, "Skyscraper_SETech", new Vector3(75f, 0f, -70f), new Vector3(24f, 68f, 22f), wallMat, glassMat, litMat, redBeaconMat);

            // ==========================================
            // 6. FIVE DETAILED WALKABLE BUILDINGS (FAZ 3 UPGRADED)
            // ==========================================
            GameObject bldgParent = new GameObject("--- DISTRICT BUILDINGS ---");
            bldgParent.transform.SetParent(envParent);

            // 1. POLICE STATION (Divided Rooms: Lobby, Office, Armory & Holding Cells)
            CreateDetailedPoliceStation(bldgParent.transform, new Vector3(-22f, 0f, 2f), wallMat, policeBlueMat, metalMat, woodMat, doorMat);

            // 2. GUN & HUNTING SHOP (Across the street)
            CreateDetailedGunShop(bldgParent.transform, new Vector3(22f, 0f, 2f), wallMat, metalMat, woodMat, doorMat);

            // 3. WALK-IN OLD CLINIC (Zone_Clinic - Reception & Pharmacy/Exam Rooms)
            CreateDetailedClinic(bldgParent.transform, new Vector3(-35f, 0f, 32f), clinicMat, wallMat, woodMat, metalMat, doorMat, redCrossMat, sandbagMat);

            // 4. FORTIFIED SAFE HOUSE (Warm amber bunker, Crafting workbench, Secure Stash, Radio Station)
            CreateDetailedSafeHouse(bldgParent.transform, new Vector3(-38f, 0f, -32f), wallMat, woodMat, metalMat, doorMat, greenSafeMat);

            // 5. SUPERMARKET & PHARMACY (Aisle stealth navigation, loot shelves, checkout counters)
            CreateDetailedSupermarket(bldgParent.transform, new Vector3(32f, 0f, 30f), new Vector3(22f, 5.5f, 18f), wallMat, signSuperMat, frameMat, glassMat, doorMat, metalMat);
            CreateDetailedPharmacy(bldgParent.transform, new Vector3(30f, 0f, -48f), new Vector3(16f, 4.8f, 14f), wallMat, signPharmMat, frameMat, glassMat, doorMat, metalMat);

            // 2. BÖLGE: BATIKONUT MAHALLESİ (Zeynep'in Bölgesi - Walkable Houses with Porches, Fences & Loot)
            CreateWalkableResidentialHouse(bldgParent.transform, "House_Victorian_1", new Vector3(-68f, 0f, -12f), new Vector3(14f, 5.5f, 12f), wallMat, woodMat, doorMat);
            CreateWalkableResidentialHouse(bldgParent.transform, "House_Victorian_2", new Vector3(-68f, 0f, 12f), new Vector3(14f, 5.5f, 12f), wallMat, woodMat, doorMat);
            CreateWalkableResidentialHouse(bldgParent.transform, "House_Suburban_3", new Vector3(-88f, 0f, -25f), new Vector3(14f, 5.2f, 12f), wallMat, woodMat, doorMat);
            CreateWalkableResidentialHouse(bldgParent.transform, "House_Suburban_4", new Vector3(-88f, 0f, 25f), new Vector3(14f, 5.2f, 12f), wallMat, woodMat, doorMat);

            // 2. BÖLGE: DOĞU TİCARET VE BENZİNLİK (Kerem & Elif'in Bölgesi - Walkable Market, Auto Garage, Fuel Barrels)
            CreateDetailedGasStationWithGarage(bldgParent.transform, new Vector3(58f, 0f, -35f), wallMat, metalMat, yellowMarkMat, doorMat);

            // 2. BÖLGE: DOĞU TAKTİK BARİKATI (Overturned Truck, Concrete Choke Points, Sandbags)
            CreateEastBarricade(bldgParent.transform, new Vector3(22f, 0f, -36f), barrierMat, sandbagMat, metalMat);

            // Apartment Blocks
            CreateApartmentBlock(bldgParent.transform, "Apartment_Brick_A", new Vector3(-65f, 0f, -72f), new Vector3(16f, 14f, 14f), wallMat, frameMat, glassMat, litMat);
            CreateApartmentBlock(bldgParent.transform, "Apartment_Brick_B", new Vector3(-65f, 0f, 72f), new Vector3(16f, 16f, 14f), wallMat, frameMat, glassMat, litMat);

            // ==========================================
            // 7. URBAN PROPS & STREET FURNITURE
            // ==========================================
            GameObject furnParent = new GameObject("--- STREET FURNITURE ---");
            furnParent.transform.SetParent(envParent);

            // A. Crashed Police Cruiser with Red Emergency Flare (5m ahead of player start)
            CreateCrashedPoliceCarWithFlare(furnParent.transform, new Vector3(0.5f, 0.10f, -22f));

            // B. Street Lamps with Volumetric Light Cones & Soft Pools
            Vector3[] lampPositions = new Vector3[]
            {
                new Vector3(-7.5f, 0.2f, -80f), new Vector3(7.5f, 0.2f, -60f),
                new Vector3(-7.5f, 0.2f, -40f), new Vector3(7.5f, 0.2f, -20f),
                new Vector3(-7.5f, 0.2f, 0f),   new Vector3(7.5f, 0.2f, 20f),
                new Vector3(-7.5f, 0.2f, 40f),  new Vector3(7.5f, 0.2f, 60f),
                new Vector3(-7.5f, 0.2f, 80f),  new Vector3(-25f, 0.2f, 32f),
                new Vector3(-28f, 0.2f, -32f), new Vector3(20f, 0.2f, 30f)
            };

            for (int i = 0; i < lampPositions.Length; i++)
            {
                float rot = lampPositions[i].x < 0 ? 90f : -90f;
                CreateStreetLamp(furnParent.transform, lampPositions[i], rot, lampMat, bulbMat, i % 3 == 0);
            }

            // C. Six Strategic Street Dumpsters (Hiding Spots)
            CreateDumpster(furnParent.transform, new Vector3(-9.2f, 0.2f, -18f), 15f, metalMat);
            CreateDumpster(furnParent.transform, new Vector3(9.2f, 0.2f, 22f), -20f, metalMat);
            CreateDumpster(furnParent.transform, new Vector3(25f, 0.2f, 42f), 85f, metalMat);
            CreateDumpster(furnParent.transform, new Vector3(-42f, 0.2f, -22f), -75f, metalMat);
            CreateDumpster(furnParent.transform, new Vector3(-12f, 0.2f, 52f), 45f, metalMat);
            CreateDumpster(furnParent.transform, new Vector3(12f, 0.2f, -52f), -45f, metalMat);

            // D. Traffic Cones
            Vector3[] conePositions = new Vector3[]
            {
                new Vector3(2.5f, 0.12f, -20f), new Vector3(-2.2f, 0.12f, -21f),
                new Vector3(1.2f, 0.12f, -16f), new Vector3(-1.8f, 0.12f, -14f),
                new Vector3(3.2f, 0.12f, -8f),  new Vector3(-2.8f, 0.12f, 8f),
                new Vector3(0.5f, 0.12f, 38f),  new Vector3(-1.2f, 0.12f, 44f)
            };
            for (int i = 0; i < conePositions.Length; i++)
            {
                CreateTrafficCone(furnParent.transform, conePositions[i], (i * 45f) % 360f, knockedOver: i % 2 == 1);
            }

            // E. Bus Stops, Benches & Mailboxes
            CreateBusStop(furnParent.transform, new Vector3(-8.2f, 0.2f, -65f), 90f, frameMat, glassMat, woodMat);
            CreateBusStop(furnParent.transform, new Vector3(8.2f, 0.2f, 65f), -90f, frameMat, glassMat, woodMat);
            CreateBench(furnParent.transform, new Vector3(-8.2f, 0.2f, 12f), 90f, woodMat);
            CreateBench(furnParent.transform, new Vector3(8.2f, 0.2f, -12f), -90f, woodMat);

            // City Mailboxes
            CreateCityMailbox(furnParent.transform, new Vector3(-8.0f, 0.18f, -24f), 90f);
            CreateCityMailbox(furnParent.transform, new Vector3(8.0f, 0.18f, 14f), -90f);
            CreateCityMailbox(furnParent.transform, new Vector3(-20f, 0.18f, 1.5f), 0f);

            // Concrete Rubble / Debris Piles
            CreateRubblePile(furnParent.transform, new Vector3(-5.5f, 0.12f, -15f), barrierMat);
            CreateRubblePile(furnParent.transform, new Vector3(6.2f, 0.12f, -5f), barrierMat);
            CreateRubblePile(furnParent.transform, new Vector3(-26f, 0.12f, 33f), barrierMat);
            CreateRubblePile(furnParent.transform, new Vector3(25f, 0.12f, 26f), barrierMat);

            // ==========================================
            // 8. ATMOSPHERIC FIRE BARRELS
            // ==========================================
            GameObject fireParent = new GameObject("--- ATMOSPHERIC FIRE BARRELS ---");
            fireParent.transform.SetParent(envParent);

            Vector3[] fireBarrelPos = new Vector3[]
            {
                new Vector3(0f, 0.1f, -24f),
                new Vector3(-28f, 0.1f, 25f),
                new Vector3(-32f, 0.1f, -25f),
                new Vector3(25f, 0.1f, 18f),
                new Vector3(22f, 0.1f, -35f),
                new Vector3(-48f, 0.1f, 0f)
            };

            for (int i = 0; i < fireBarrelPos.Length; i++)
            {
                CreateFireBarrel(fireParent.transform, $"FireBarrel_{i + 1}", fireBarrelPos[i], metalMat);
            }

            // 9. Skyline with Scattered Windows
            CreateCitySkyline(envParent, skylineMat, litMat);
        }

        // =========================================================================
        // 4. QUARANTINE CHECKPOINT (FAZ 3 - MERKEZ BULVAR)
        // =========================================================================
        private static void CreateQuarantineCheckpoint(
            Transform parent, Vector3 centerPos, Material barrierMat, Material sandbagMat, Material wireMat, Material hazardMat, Material metalMat)
        {
            GameObject cpRoot = new GameObject("Quarantine_Checkpoint_Central");
            cpRoot.transform.SetParent(parent);
            cpRoot.transform.position = centerPos;

            // A. Concrete Jersey T-Barriers in Zigzag Echelon
            // Barrier 1: Left choke
            CreateJerseyBarrier(cpRoot.transform, new Vector3(-3.8f, 0f, 0f), 15f, barrierMat);
            // Barrier 2: Center-left with hazard stripes
            CreateJerseyBarrier(cpRoot.transform, new Vector3(-1.2f, 0f, -2.5f), -10f, barrierMat, true, hazardMat);
            // Barrier 3: Center-right
            CreateJerseyBarrier(cpRoot.transform, new Vector3(2.5f, 0f, 1.8f), 20f, barrierMat);
            // Barrier 4: Right sidewalk edge
            CreateJerseyBarrier(cpRoot.transform, new Vector3(5.2f, 0f, -0.5f), 0f, barrierMat, true, hazardMat);

            // B. Sandbag Redoubt / Bunkers (Firing Positions)
            CreateSandbagBunker(cpRoot.transform, new Vector3(-4.8f, 0f, -1.8f), 35f, sandbagMat);
            CreateSandbagBunker(cpRoot.transform, new Vector3(4.2f, 0f, 2.4f), -145f, sandbagMat);

            // C. Barbed Wire Fence Sections
            CreateBarbedWireSection(cpRoot.transform, new Vector3(-6.2f, 0f, 0.5f), 90f, wireMat, metalMat);
            CreateBarbedWireSection(cpRoot.transform, new Vector3(6.2f, 0f, -0.8f), 90f, wireMat, metalMat);

            // D. Quarantine Warning Signs ("KARANTİNA BÖLGESİ - GİRİLMEZ")
            CreateQuarantineSign(cpRoot.transform, new Vector3(-2.2f, 0f, -3.2f), 12f, hazardMat);
            CreateQuarantineSign(cpRoot.transform, new Vector3(1.8f, 0f, -3.0f), -8f, hazardMat);

            // E. Fallen Searchlight with Flickering Cool White Spot Light
            CreateFallenSearchlight(cpRoot.transform, new Vector3(0.6f, 0f, -1.2f), metalMat);
        }

        private static void CreateJerseyBarrier(Transform parent, Vector3 localPos, float yRot, Material concreteMat, bool hasHazardStripe = false, Material hazardMat = null)
        {
            GameObject barrier = new GameObject("Jersey_Barrier");
            barrier.transform.SetParent(parent);
            barrier.transform.localPosition = localPos;
            barrier.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // Base
            CreateBoxSimple(barrier.transform, "Base", new Vector3(0f, 0.22f, 0f), new Vector3(3.2f, 0.44f, 0.65f), concreteMat);
            // Upper tapered slab
            CreateBoxSimple(barrier.transform, "Upper", new Vector3(0f, 0.65f, 0f), new Vector3(3.2f, 0.45f, 0.32f), concreteMat);

            if (hasHazardStripe && hazardMat != null)
            {
                CreateBoxSimple(barrier.transform, "HazardStripe", new Vector3(0f, 0.70f, 0.17f), new Vector3(2.6f, 0.18f, 0.02f), hazardMat);
            }
        }

        private static void CreateSandbagBunker(Transform parent, Vector3 localPos, float yRot, Material sandbagMat)
        {
            GameObject bunker = new GameObject("Sandbag_Bunker");
            bunker.transform.SetParent(parent);
            bunker.transform.localPosition = localPos;
            bunker.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // Stack of 4 tiers of sandbags forming an L-shape
            float[] tierHeights = new float[] { 0.15f, 0.38f, 0.60f, 0.82f };
            for (int t = 0; t < tierHeights.Length; t++)
            {
                float y = tierHeights[t];
                float w = 2.4f - (t * 0.1f);
                CreateBoxSimple(bunker.transform, $"BagRow_Front_{t}", new Vector3(0f, y, 0f), new Vector3(w, 0.22f, 0.50f), sandbagMat);
                if (t < 3)
                {
                    CreateBoxSimple(bunker.transform, $"BagRow_Side_{t}", new Vector3(-0.95f, y, 0.75f), new Vector3(0.50f, 0.22f, 1.2f), sandbagMat);
                }
            }
        }

        private static void CreateBarbedWireSection(Transform parent, Vector3 localPos, float yRot, Material wireMat, Material metalMat)
        {
            GameObject wireObj = new GameObject("BarbedWire_Fence");
            wireObj.transform.SetParent(parent);
            wireObj.transform.localPosition = localPos;
            wireObj.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // 2 metal posts
            CreatePartSimple(PrimitiveType.Cylinder, wireObj.transform, "Post_L", new Vector3(-1.2f, 0.8f, 0f), new Vector3(0.08f, 0.8f, 0.08f), metalMat);
            CreatePartSimple(PrimitiveType.Cylinder, wireObj.transform, "Post_R", new Vector3(1.2f, 0.8f, 0f), new Vector3(0.08f, 0.8f, 0.08f), metalMat);

            // Cross wires
            for (float y = 0.3f; y <= 1.4f; y += 0.35f)
            {
                CreateBoxSimple(wireObj.transform, $"Wire_H_{y}", new Vector3(0f, y, 0f), new Vector3(2.4f, 0.03f, 0.03f), wireMat);
            }
            CreateBoxSimple(wireObj.transform, "Wire_Diag_1", new Vector3(0f, 0.85f, 0f), new Vector3(2.6f, 0.025f, 0.025f), wireMat);
        }

        private static void CreateQuarantineSign(Transform parent, Vector3 localPos, float yRot, Material hazardMat)
        {
            GameObject signObj = new GameObject("Sign_QuarantineWarning");
            signObj.transform.SetParent(parent);
            signObj.transform.localPosition = localPos;
            signObj.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            Material darkPost = SceneBuilder.GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.2f, 0.22f, 0.25f));
            CreatePartSimple(PrimitiveType.Cylinder, signObj.transform, "Pole", new Vector3(0f, 0.9f, 0f), new Vector3(0.06f, 0.9f, 0.06f), darkPost);

            // Warning Board
            CreateBoxSimple(signObj.transform, "Board", new Vector3(0f, 1.6f, 0f), new Vector3(1.2f, 0.75f, 0.06f), hazardMat);

            Material blackTextMat = SceneBuilder.GetOrCreateMaterial("Mat_Sign_BlackText", new Color(0.06f, 0.06f, 0.08f));
            CreateBoxSimple(signObj.transform, "HazardStripe_Top", new Vector3(0f, 1.88f, 0.035f), new Vector3(1.15f, 0.08f, 0.02f), blackTextMat);
            CreateBoxSimple(signObj.transform, "HazardStripe_Bot", new Vector3(0f, 1.32f, 0.035f), new Vector3(1.15f, 0.08f, 0.02f), blackTextMat);
            CreateBoxSimple(signObj.transform, "Text_Bar_Middle", new Vector3(0f, 1.60f, 0.035f), new Vector3(0.95f, 0.22f, 0.02f), blackTextMat);
        }

        private static void CreateFallenSearchlight(Transform parent, Vector3 localPos, Material metalMat)
        {
            GameObject searchlight = new GameObject("Fallen_Searchlight");
            searchlight.transform.SetParent(parent);
            searchlight.transform.localPosition = localPos;
            searchlight.transform.localRotation = Quaternion.Euler(68f, -24f, 75f); // Tilted onto asphalt

            // Tripod stand
            CreateBoxSimple(searchlight.transform, "Stand_Central", new Vector3(0f, 0.6f, 0f), new Vector3(0.08f, 1.2f, 0.08f), metalMat);
            CreateBoxSimple(searchlight.transform, "Leg_1", new Vector3(0.3f, 0.2f, 0f), new Vector3(0.6f, 0.06f, 0.06f), metalMat);
            CreateBoxSimple(searchlight.transform, "Leg_2", new Vector3(-0.15f, 0.2f, 0.28f), new Vector3(0.6f, 0.06f, 0.06f), metalMat);

            // Heavy floodlight lamp head
            Material lampHousing = SceneBuilder.GetOrCreateMaterial("Mat_Floodlight_Housing", new Color(0.25f, 0.28f, 0.30f));
            CreateBoxSimple(searchlight.transform, "Lamp_Housing", new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 0.42f, 0.35f), lampHousing);

            Material lensMat = SceneBuilder.GetOrCreateMaterial("Mat_Floodlight_Lens", new Color(0.9f, 0.95f, 1.0f));
            lensMat.EnableKeyword("_EMISSION");
            lensMat.SetColor("_EmissionColor", new Color(0.85f, 0.93f, 1.0f) * 2.8f);

            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.name = "Lamp_Lens";
            lens.transform.SetParent(searchlight.transform);
            lens.transform.localPosition = new Vector3(0f, 1.25f, 0.18f);
            lens.transform.localScale = new Vector3(0.48f, 0.36f, 0.04f);
            lens.GetComponent<Renderer>().sharedMaterial = lensMat;
            Collider c = lens.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            // Cool White Flickering Spot Light projecting dramatic shadows across the barricade
            GameObject lightObj = new GameObject("Searchlight_SpotBeam");
            lightObj.transform.SetParent(searchlight.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.25f, 0.25f);
            lightObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 68f;
            spot.innerSpotAngle = 38f;
            spot.range = 22f;
            spot.intensity = 3.6f;
            spot.color = new Color(0.86f, 0.93f, 1.0f); // Cool white #DCEDFF
            spot.shadows = LightShadows.None; // Optimal mobile performance

            FlickeringLight fl = lightObj.AddComponent<FlickeringLight>();
            SerializedObject soFl = new SerializedObject(fl);
            soFl.FindProperty("baseIntensity").floatValue = 3.6f;
            soFl.FindProperty("flickerAmount").floatValue = 0.55f;
            soFl.FindProperty("flickerSpeed").floatValue = 14f;
            soFl.ApplyModifiedProperties();
        }

        private static void CreateStreetDecals(Transform parent)
        {
            GameObject decalRoot = new GameObject("--- STREET DECALS ---");
            decalRoot.transform.SetParent(parent);

            Material skidMat = SceneBuilder.GetOrCreateMaterial("Mat_Decal_Skidmark", new Color(0.04f, 0.04f, 0.05f));
            Material oilMat = SceneBuilder.GetOrCreateMaterial("Mat_Decal_OilPuddle", new Color(0.02f, 0.03f, 0.04f));
            Material crackMat = SceneBuilder.GetOrCreateMaterial("Mat_Decal_Crack", new Color(0.07f, 0.08f, 0.09f));

            // Skidmarks near crashed police car & checkpoint
            CreateDecalQuad(decalRoot.transform, "Skidmark_1", new Vector3(0.2f, 0.082f, -19f), new Vector3(0.45f, 0.005f, 5.5f), 12f, skidMat);
            CreateDecalQuad(decalRoot.transform, "Skidmark_2", new Vector3(1.4f, 0.082f, -18.5f), new Vector3(0.45f, 0.005f, 5.0f), 14f, skidMat);
            CreateDecalQuad(decalRoot.transform, "Skidmark_3", new Vector3(-2.0f, 0.082f, -6f), new Vector3(0.55f, 0.005f, 4.2f), -25f, skidMat);

            // Oil Puddles under crashed vehicles
            CreateDecalQuad(decalRoot.transform, "Oil_Puddle_Police", new Vector3(0.6f, 0.083f, -21.8f), new Vector3(2.4f, 0.005f, 2.1f), 35f, oilMat);
            CreateDecalQuad(decalRoot.transform, "Oil_Puddle_Crossroad", new Vector3(-1.5f, 0.083f, -2f), new Vector3(3.2f, 0.005f, 2.6f), -15f, oilMat);
            CreateDecalQuad(decalRoot.transform, "Oil_Puddle_Clinic", new Vector3(-31f, 0.083f, 30f), new Vector3(2.6f, 0.005f, 2.2f), 45f, oilMat);

            // Cracks on pavement
            CreateDecalQuad(decalRoot.transform, "Crack_1", new Vector3(-3.5f, 0.084f, -12f), new Vector3(0.25f, 0.005f, 6.2f), 40f, crackMat);
            CreateDecalQuad(decalRoot.transform, "Crack_2", new Vector3(3.0f, 0.084f, 8f), new Vector3(0.28f, 0.005f, 5.4f), -30f, crackMat);
        }

        private static void CreateDecalQuad(Transform parent, string name, Vector3 pos, Vector3 scale, float yRot, Material mat)
        {
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d.name = name;
            d.transform.SetParent(parent);
            d.transform.position = pos;
            d.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            d.transform.localScale = scale;
            if (mat != null) d.GetComponent<Renderer>().sharedMaterial = mat;
            Collider c = d.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void CreateCityMailbox(Transform parent, Vector3 pos, float yRot)
        {
            GameObject mb = new GameObject("City_Mailbox");
            mb.transform.SetParent(parent);
            mb.transform.position = pos;
            mb.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material blueBox = SceneBuilder.GetOrCreateMaterial("Mat_Mailbox_Blue", new Color(0.10f, 0.28f, 0.72f));
            CreateBoxSimple(mb.transform, "Body", new Vector3(0f, 0.65f, 0f), new Vector3(0.65f, 1.1f, 0.55f), blueBox);

            Material chromeMat = SceneBuilder.GetOrCreateMaterial("Mat_Chrome_Metal", new Color(0.85f, 0.88f, 0.92f));
            CreateBoxSimple(mb.transform, "DropHandle", new Vector3(0f, 1.0f, 0.28f), new Vector3(0.35f, 0.08f, 0.06f), chromeMat);
        }

        private static void CreateRubblePile(Transform parent, Vector3 pos, Material stoneMat)
        {
            GameObject pile = new GameObject("Rubble_Pile");
            pile.transform.SetParent(parent);
            pile.transform.position = pos;

            CreateBoxSimple(pile.transform, "Debris_1", new Vector3(0f, 0.18f, 0f), new Vector3(1.2f, 0.35f, 1.1f), stoneMat);
            CreateBoxSimple(pile.transform, "Debris_2", new Vector3(0.4f, 0.15f, 0.3f), new Vector3(0.85f, 0.30f, 0.75f), stoneMat);
            CreateBoxSimple(pile.transform, "Debris_3", new Vector3(-0.35f, 0.12f, -0.25f), new Vector3(0.70f, 0.24f, 0.65f), stoneMat);
        }

        // =========================================================================
        // 6. FIVE DETAILED WALKABLE BUILDINGS (FAZ 3 UPGRADED)
        // =========================================================================

        // 1. POLICE STATION (Divided Rooms: Lobby, Office, Armory & Holding Cells)
        private static GameObject CreateDetailedPoliceStation(
            Transform parent, Vector3 pos, Material wallMat, Material policeBlueMat, Material metalMat, Material woodMat, Material doorMat)
        {
            GameObject ps = new GameObject("Building_PoliceStation");
            ps.transform.SetParent(parent);
            ps.transform.position = pos;

            float w = 24f;
            float h = 5.6f;
            float d = 20f;

            // Exterior Base & Floor
            CreateBoxSimple(ps.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);

            float doorGap = 3.6f;
            float sideW = (w - doorGap) * 0.5f;

            // Outer Walls
            CreateBoxSimple(ps.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.45f), wallMat);
            CreateBoxSimple(ps.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.45f), wallMat);
            CreateBoxSimple(ps.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.45f), wallMat);
            CreateBoxSimple(ps.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d), wallMat);
            CreateBoxSimple(ps.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d), wallMat);
            CreateBoxSimple(ps.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.5f, 0.25f, d + 0.5f), wallMat);

            // Corporate Police Blue Facade Band
            CreateBoxSimple(ps.transform, "Police_Blue_Band", new Vector3(0f, 4.2f, -d * 0.5f - 0.05f), new Vector3(w, 0.75f, 0.15f), policeBlueMat);

            // 3-Step Entrance Stairs with Steel Railings
            CreateEntranceStairsWithRailings(ps.transform, new Vector3(0f, 0f, -d * 0.5f - 0.75f), doorGap + 0.8f, 0.45f, 1.5f, wallMat, metalMat);

            // Double Interactive Doors at Entrance
            CreateInteractiveDoorObj(ps.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 1.75f, 2.6f, doorMat);
            CreateInteractiveDoorObj(ps.transform, new Vector3(doorGap * 0.5f, 0f, -d * 0.5f), 180f, 1.75f, 2.6f, doorMat);

            // 3D Illuminated "POLICE" Sign
            Create3DNeonSign(ps.transform, "POLICE_STATION", new Vector3(0f, 4.35f, -d * 0.5f - 0.25f), new Vector3(7.5f, 1.1f, 0.25f), new Color(0.15f, 0.75f, 1.0f));

            // ---------------------------------------------------------
            // INTERIOR ROOM PARTITIONS (Corridor clearances >= 1.8m)
            // ---------------------------------------------------------
            // Partition 1: Separates Front Lobby from Rear Holding Cells & Armory (Z = 0.5f)
            CreateBoxSimple(ps.transform, "Partition_Wall_Rear_L", new Vector3(-7.5f, h * 0.5f, 0.5f), new Vector3(8.5f, h, 0.35f), wallMat);
            CreateBoxSimple(ps.transform, "Partition_Wall_Rear_R", new Vector3(7.5f, h * 0.5f, 0.5f), new Vector3(8.5f, h, 0.35f), wallMat);
            // Center doorway between lobby and armory: 3.5m wide!

            // Partition 2: Separates Lobby from Detective Office (X = 1.0f, from Z = -d*0.5 to Z = 0.5f)
            CreateBoxSimple(ps.transform, "Partition_Wall_Office_Front", new Vector3(1.0f, h * 0.5f, -6.5f), new Vector3(0.35f, h, 6.0f), wallMat);
            // Office doorway: 2.2m wide!

            // ---------------------------------------------------------
            // ROOM 1: LOBBY & RECEPTION (Giriş Holü)
            // ---------------------------------------------------------
            // L-shaped Reception Desk
            CreateBoxSimple(ps.transform, "Reception_Desk_Front", new Vector3(-4.2f, 0.65f, -4.5f), new Vector3(4.5f, 1.25f, 1.2f), woodMat);
            CreateBoxSimple(ps.transform, "Reception_Desk_Side", new Vector3(-6.2f, 0.65f, -2.8f), new Vector3(1.2f, 1.25f, 2.4f), woodMat);

            // Reception Glass Partition
            Material glassFrosted = SceneBuilder.GetOrCreateMaterial("Mat_Glass_Frosted", new Color(0.2f, 0.3f, 0.4f, 0.65f), isTransparent: true);
            CreateBoxSimple(ps.transform, "Reception_Glass", new Vector3(-4.2f, 1.75f, -4.5f), new Vector3(4.5f, 0.95f, 0.08f), glassFrosted);

            // Waiting Chairs (4 connected plastic seats)
            Material chairMat = SceneBuilder.GetOrCreateMaterial("Mat_Waiting_Chair_Blue", new Color(0.14f, 0.35f, 0.68f));
            for (float x = -9.5f; x <= -6.5f; x += 0.85f)
            {
                CreateBoxSimple(ps.transform, $"Waiting_Seat_{x}", new Vector3(x, 0.45f, -8.2f), new Vector3(0.65f, 0.45f, 0.60f), chairMat);
                CreateBoxSimple(ps.transform, $"Waiting_Back_{x}", new Vector3(x, 0.85f, -8.45f), new Vector3(0.65f, 0.45f, 0.10f), chairMat);
            }

            // Cork Bulletin Boards on Walls
            Material corkMat = SceneBuilder.GetOrCreateMaterial("Mat_Cork_Board", new Color(0.62f, 0.48f, 0.32f));
            CreateBoxSimple(ps.transform, "Bulletin_Board_1", new Vector3(-11.75f, 2.2f, -4.5f), new Vector3(0.08f, 1.4f, 2.6f), corkMat);

            // Knocked-over Filing Cabinet with scattered papers
            GameObject fallenCabinet = new GameObject("Fallen_Filing_Cabinet");
            fallenCabinet.transform.SetParent(ps.transform);
            fallenCabinet.transform.localPosition = new Vector3(-2.8f, 0.35f, -1.8f);
            fallenCabinet.transform.localRotation = Quaternion.Euler(0f, 35f, 78f);
            CreateBoxSimple(fallenCabinet.transform, "Cabinet_Body", Vector3.zero, new Vector3(0.65f, 1.4f, 0.55f), metalMat);

            // ---------------------------------------------------------
            // ROOM 2: DETECTIVE & OFFICER OFFICE (Çalışma Ofisi)
            // ---------------------------------------------------------
            // Desk 1
            CreateBoxSimple(ps.transform, "Officer_Desk_1", new Vector3(5.5f, 0.45f, -5.5f), new Vector3(3.0f, 0.9f, 1.6f), woodMat);
            // Glowing CRT Monitor 1 (Blue soft light)
            CreateMonitorWithGlow(ps.transform, new Vector3(5.5f, 1.15f, -5.5f), 180f);

            // Desk 2
            CreateBoxSimple(ps.transform, "Officer_Desk_2", new Vector3(5.5f, 0.45f, -2.0f), new Vector3(3.0f, 0.9f, 1.6f), woodMat);
            CreateMonitorWithGlow(ps.transform, new Vector3(5.5f, 1.15f, -2.0f), 180f);

            // Office Swivel Chairs
            CreateBoxSimple(ps.transform, "Office_Chair_1", new Vector3(5.5f, 0.45f, -4.2f), new Vector3(0.65f, 0.85f, 0.65f), chairMat);
            CreateBoxSimple(ps.transform, "Office_Chair_2", new Vector3(5.5f, 0.45f, -0.7f), new Vector3(0.65f, 0.85f, 0.65f), chairMat);

            // Loot Drawer Desk
            GameObject lootDesk = new GameObject("Police_Loot_Desk");
            lootDesk.transform.SetParent(ps.transform);
            lootDesk.transform.localPosition = new Vector3(9.5f, 0.45f, -3.5f);
            CreateBoxSimple(lootDesk.transform, "Desk_Mesh", Vector3.zero, new Vector3(1.8f, 0.9f, 1.4f), woodMat);
            SphereCollider scDesk = lootDesk.AddComponent<SphereCollider>();
            scDesk.isTrigger = true;
            scDesk.radius = 2.0f;
            var lcDesk = lootDesk.AddComponent<LootContainer>();
            ItemData ammo9 = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_9mm.asset");
            ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");
            if (ammo9 != null) lcDesk.AddItem(ammo9, 12);
            if (battery != null) lcDesk.AddItem(battery, 1);

            // ---------------------------------------------------------
            // ROOM 3: ARMORY & HOLDING CELLS (Silahlık ve Nezarethane)
            // ---------------------------------------------------------
            // Cell 1: Left
            Material barMat = SceneBuilder.GetOrCreateMaterial("Mat_Iron_Bars", new Color(0.18f, 0.20f, 0.22f));
            for (float x = -11.5f; x <= -4.5f; x += 0.55f)
            {
                CreatePartSimple(PrimitiveType.Cylinder, ps.transform, $"Cell_Bar_Front_{x}", new Vector3(x, 1.5f, 4.5f), new Vector3(0.08f, 1.5f, 0.08f), barMat);
            }
            CreateBoxSimple(ps.transform, "Cell_Wall_Divider", new Vector3(-4.5f, h * 0.5f, 7.0f), new Vector3(0.35f, h, 5.0f), wallMat);
            CreateBoxSimple(ps.transform, "Cell_Bench_1", new Vector3(-8.5f, 0.35f, 8.5f), new Vector3(2.8f, 0.45f, 1.2f), wallMat);

            // Locked Gun Cabinet with Weapons & Ammo
            GameObject cabinet = new GameObject("Police_GunCabinet");
            cabinet.transform.SetParent(ps.transform);
            cabinet.transform.localPosition = new Vector3(8.5f, 1.2f, 6.5f);
            CreateBoxSimple(cabinet.transform, "Cabinet_Mesh", Vector3.zero, new Vector3(2.6f, 2.4f, 1.1f), metalMat);

            SphereCollider sc = cabinet.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.2f;
            var lc = cabinet.AddComponent<LootContainer>();
            ItemData pistol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/weapon_pistol.asset");
            ItemData shotgun = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/weapon_shotgun.asset");
            ItemData shells = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_shells.asset");
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            if (pistol != null) lc.AddItem(pistol, 1);
            if (ammo9 != null) lc.AddItem(ammo9, 24);
            if (shotgun != null) lc.AddItem(shotgun, 1);
            if (shells != null) lc.AddItem(shells, 8);
            if (medkit != null) lc.AddItem(medkit, 1);

            // Metal Locker (Hiding Spot 1)
            CreateMetalLocker(ps.transform, new Vector3(4.5f, 0f, 6.5f), 180f, metalMat);

            // Overhead Fluorescent Lighting (Cool Police Blue-White #CAF0F8)
            CreateInteriorLight(ps.transform, "Police_Lobby_Light", new Vector3(-5f, 4.5f, -3f), new Color(0.79f, 0.94f, 0.97f), 14f, 2.2f);
            CreateInteriorLight(ps.transform, "Police_Office_Light", new Vector3(6f, 4.5f, -3f), new Color(0.79f, 0.94f, 0.97f), 12f, 2.0f);
            CreateInteriorLight(ps.transform, "Police_Armory_Light", new Vector3(0f, 4.5f, 6f), new Color(0.79f, 0.94f, 0.97f), 14f, 2.0f);

            return ps;
        }

        private static void CreateMonitorWithGlow(Transform parent, Vector3 localPos, float yRot)
        {
            GameObject monitor = new GameObject("Computer_Monitor");
            monitor.transform.SetParent(parent);
            monitor.transform.localPosition = localPos;
            monitor.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            Material casingMat = SceneBuilder.GetOrCreateMaterial("Mat_Computer_Beige", new Color(0.25f, 0.26f, 0.28f));
            CreateBoxSimple(monitor.transform, "Stand", new Vector3(0f, -0.2f, 0f), new Vector3(0.28f, 0.05f, 0.25f), casingMat);
            CreateBoxSimple(monitor.transform, "Neck", new Vector3(0f, -0.1f, -0.05f), new Vector3(0.08f, 0.2f, 0.08f), casingMat);
            CreateBoxSimple(monitor.transform, "Frame", new Vector3(0f, 0.15f, 0f), new Vector3(0.75f, 0.50f, 0.10f), casingMat);

            Material screenMat = SceneBuilder.GetOrCreateMaterial("Mat_Screen_CyanGlow", new Color(0.22f, 0.74f, 0.97f));
            screenMat.EnableKeyword("_EMISSION");
            screenMat.SetColor("_EmissionColor", new Color(0.22f, 0.74f, 0.97f) * 1.8f);

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen_Glow";
            screen.transform.SetParent(monitor.transform);
            screen.transform.localPosition = new Vector3(0f, 0.15f, 0.055f);
            screen.transform.localScale = new Vector3(0.68f, 0.44f, 0.02f);
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;
            Collider c = screen.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            // Small soft blue Point Light casting screen glow
            GameObject lObj = new GameObject("Screen_PointLight");
            lObj.transform.SetParent(monitor.transform);
            lObj.transform.localPosition = new Vector3(0f, 0.15f, 0.35f);
            Light pLight = lObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.22f, 0.74f, 0.97f);
            pLight.range = 2.4f;
            pLight.intensity = 0.9f;
            pLight.shadows = LightShadows.None;
        }

        private static void CreateEntranceStairsWithRailings(
            Transform parent, Vector3 localPos, float width, float totalHeight, float depth, Material stoneMat, Material metalMat)
        {
            GameObject stairsObj = new GameObject("Entrance_Stairs");
            stairsObj.transform.SetParent(parent);
            stairsObj.transform.localPosition = localPos;

            int steps = 3;
            float stepHeight = totalHeight / steps;
            float stepDepth = depth / steps;

            for (int s = 0; s < steps; s++)
            {
                float y = (s + 0.5f) * stepHeight;
                float z = (s - steps * 0.5f + 0.5f) * stepDepth;
                float d = depth - (s * stepDepth);
                CreateBoxSimple(stairsObj.transform, $"Step_{s + 1}", new Vector3(0f, y, z), new Vector3(width, stepHeight, d), stoneMat);
            }

            // Side Handrails
            float halfW = width * 0.5f + 0.05f;
            CreateBoxSimple(stairsObj.transform, "Railing_L", new Vector3(-halfW, totalHeight + 0.45f, 0f), new Vector3(0.06f, 0.9f, depth), metalMat);
            CreateBoxSimple(stairsObj.transform, "Railing_R", new Vector3(halfW, totalHeight + 0.45f, 0f), new Vector3(0.06f, 0.9f, depth), metalMat);
        }

        // 2. GUN & HUNTING SHOP (Across the street)
        private static GameObject CreateDetailedGunShop(
            Transform parent, Vector3 pos, Material wallMat, Material metalMat, Material woodMat, Material doorMat)
        {
            GameObject gs = new GameObject("Building_GunShop");
            gs.transform.SetParent(parent);
            gs.transform.position = pos;

            float w = 20f;
            float h = 5.2f;
            float d = 18f;

            CreateBoxSimple(gs.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);

            float doorGap = 2.4f;
            float sideW = (w - doorGap) * 0.5f;

            CreateBoxSimple(gs.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.45f), wallMat);
            CreateBoxSimple(gs.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.45f), wallMat);
            CreateBoxSimple(gs.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.45f), wallMat);
            CreateBoxSimple(gs.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d), wallMat);
            CreateBoxSimple(gs.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d), wallMat);
            CreateBoxSimple(gs.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.4f, 0.25f, d + 0.4f), wallMat);

            CreateInteractiveDoorObj(gs.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 2.2f, 2.6f, doorMat);

            // 3D Neon Sign
            Create3DNeonSign(gs.transform, "GUNS_AND_AMMO", new Vector3(0f, 3.9f, -d * 0.5f - 0.25f), new Vector3(7.0f, 1.1f, 0.25f), new Color(1.0f, 0.34f, 0.13f));

            // Glass Display Counters
            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_Glass", new Color(0.12f, 0.18f, 0.24f, 0.6f), isTransparent: true);
            CreateBoxSimple(gs.transform, "Glass_Counter_1", new Vector3(-2.5f, 0.55f, -2.5f), new Vector3(4.5f, 1.1f, 1.2f), glassMat);
            CreateBoxSimple(gs.transform, "Glass_Counter_2", new Vector3(2.5f, 0.55f, -2.5f), new Vector3(4.5f, 1.1f, 1.2f), glassMat);

            // Safe with Shotgun & Shells
            GameObject safeObj = new GameObject("GunShop_ShotgunSafe");
            safeObj.transform.SetParent(gs.transform);
            safeObj.transform.localPosition = new Vector3(6.5f, 0.8f, 6.0f);
            CreateBoxSimple(safeObj.transform, "Safe_Mesh", Vector3.zero, new Vector3(1.6f, 1.6f, 1.4f), metalMat);

            SphereCollider sc = safeObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.0f;
            var lc = safeObj.AddComponent<LootContainer>();
            ItemData shotgun = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/weapon_shotgun.asset");
            ItemData shells = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_shells.asset");
            if (shotgun != null) lc.AddItem(shotgun, 1);
            if (shells != null) lc.AddItem(shells, 12);

            // Metal Locker (Hiding Spot 2)
            CreateMetalLocker(gs.transform, new Vector3(-6.5f, 0f, 6.0f), 0f, metalMat);

            // Interior Light
            CreateInteriorLight(gs.transform, "GunShop_Interior_Light", new Vector3(0f, 4.0f, 0f), new Color(1.0f, 0.88f, 0.51f), 12f, 2.0f);

            return gs;
        }

        // 3. WALK-IN OLD CLINIC (Zone_Clinic - Divided: Reception/Waiting & Examination/Pharmacy)
        private static GameObject CreateDetailedClinic(
            Transform parent, Vector3 pos, Material clinicMat, Material wallMat, Material woodMat, Material metalMat, Material doorMat, Material redCrossMat, Material sandbagMat)
        {
            GameObject clinic = new GameObject("Zone_Clinic_OldMedical");
            clinic.transform.SetParent(parent);
            clinic.transform.position = pos;

            float w = 22f;
            float h = 5.2f;
            float d = 20f;

            CreateBoxSimple(clinic.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);

            float doorGap = 3.6f;
            float sideW = (w - doorGap) * 0.5f;

            // Outer Walls
            CreateBoxSimple(clinic.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.4f), clinicMat);
            CreateBoxSimple(clinic.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), clinicMat);
            CreateBoxSimple(clinic.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), clinicMat);
            CreateBoxSimple(clinic.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), clinicMat);
            CreateBoxSimple(clinic.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), clinicMat);
            CreateBoxSimple(clinic.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.5f, 0.2f, d + 0.5f), clinicMat);

            // Sandbag Reinforced Lower Windows on Exterior
            CreateBoxSimple(clinic.transform, "Window_Sandbags_L", new Vector3(-6.5f, 0.5f, -d * 0.5f - 0.25f), new Vector3(4.8f, 1.0f, 0.55f), sandbagMat);
            CreateBoxSimple(clinic.transform, "Window_Sandbags_R", new Vector3(6.5f, 0.5f, -d * 0.5f - 0.25f), new Vector3(4.8f, 1.0f, 0.55f), sandbagMat);

            // Door
            CreateInteractiveDoorObj(clinic.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 3.2f, 2.6f, doorMat);

            // Red Cross Emblem on Roof & Entrance
            CreateRedCrossEmblem(clinic.transform, new Vector3(0f, h + 0.95f, -d * 0.5f), 1.5f, redCrossMat);
            CreateRedCrossEmblem(clinic.transform, new Vector3(0f, 3.85f, -d * 0.5f - 0.22f), 1.0f, redCrossMat);

            // 3D Illuminated "CLINIC" Sign
            Create3DNeonSign(clinic.transform, "CLINIC_MEDICAL", new Vector3(0f, 4.6f, -d * 0.5f - 0.2f), new Vector3(6.5f, 0.95f, 0.2f), new Color(0.0f, 0.9f, 0.46f));

            // ---------------------------------------------------------
            // INTERIOR ROOM PARTITIONS (Corridor >= 1.8m)
            // ---------------------------------------------------------
            // Partition Wall dividing Front Reception from Rear Exam/Pharmacy (Z = 0f)
            CreateBoxSimple(clinic.transform, "Partition_Wall_L", new Vector3(-6.8f, h * 0.5f, 0f), new Vector3(7.5f, h, 0.35f), wallMat);
            CreateBoxSimple(clinic.transform, "Partition_Wall_R", new Vector3(6.8f, h * 0.5f, 0f), new Vector3(7.5f, h, 0.35f), wallMat);
            // Center passage is 6m wide!

            // ---------------------------------------------------------
            // ROOM 1: RECEPTION & WAITING AREA (Front)
            // ---------------------------------------------------------
            // Curved Reception Counter
            CreateBoxSimple(clinic.transform, "Reception_Counter", new Vector3(-3.5f, 0.6f, -4.5f), new Vector3(4.8f, 1.2f, 1.2f), woodMat);
            // Waiting Chairs
            Material chairMat = SceneBuilder.GetOrCreateMaterial("Mat_Clinic_Chair", new Color(0.18f, 0.45f, 0.55f));
            CreateBoxSimple(clinic.transform, "Waiting_Row_1", new Vector3(-7.5f, 0.45f, -2.5f), new Vector3(1.1f, 0.55f, 3.5f), chairMat);

            // Wheelchair Prop near entrance
            CreateWheelchairProp(clinic.transform, new Vector3(5.5f, 0f, -6.5f), -35f, metalMat);

            // IV Drip Stands (Serum Askıları)
            CreateIVDripStand(clinic.transform, new Vector3(-1.8f, 0f, -2.5f), metalMat);

            // ---------------------------------------------------------
            // ROOM 2: EXAMINATION & PHARMACY (Rear)
            // ---------------------------------------------------------
            Material sheetMat = SceneBuilder.GetOrCreateMaterial("Mat_Medical_Sheet", new Color(0.88f, 0.92f, 0.94f));
            Material curtainMat = SceneBuilder.GetOrCreateMaterial("Mat_Medical_Curtain", new Color(0.35f, 0.65f, 0.75f, 0.85f), isTransparent: true);

            // Exam Bed 1 + Privacy Curtain Divider
            CreateBoxSimple(clinic.transform, "Exam_Bed_1", new Vector3(-6.0f, 0.55f, 5.0f), new Vector3(1.5f, 0.85f, 2.6f), sheetMat);
            CreateBoxSimple(clinic.transform, "Bed_Pillow_1", new Vector3(-6.0f, 0.98f, 6.0f), new Vector3(1.2f, 0.15f, 0.45f), sheetMat);
            CreateBoxSimple(clinic.transform, "Curtain_Divider_1", new Vector3(-4.0f, 2.2f, 5.0f), new Vector3(0.06f, 2.8f, 3.2f), curtainMat);

            // Exam Bed 2
            CreateBoxSimple(clinic.transform, "Exam_Bed_2", new Vector3(-1.5f, 0.55f, 5.0f), new Vector3(1.5f, 0.85f, 2.6f), sheetMat);
            CreateBoxSimple(clinic.transform, "Bed_Pillow_2", new Vector3(-1.5f, 0.98f, 6.0f), new Vector3(1.2f, 0.15f, 0.45f), sheetMat);

            // Glass Medicine Cabinets on wall
            CreateBoxSimple(clinic.transform, "Medicine_Cabinet_Wall", new Vector3(8.5f, 1.8f, 4.5f), new Vector3(1.1f, 2.6f, 4.5f), metalMat);

            // Medical Loot Box
            GameObject medLoot = new GameObject("Clinic_Medical_Stash");
            medLoot.transform.SetParent(clinic.transform);
            medLoot.transform.localPosition = new Vector3(8.5f, 0.45f, 1.5f);
            CreateBoxSimple(medLoot.transform, "Stash_Mesh", Vector3.zero, new Vector3(1.4f, 0.9f, 1.4f), metalMat);
            SphereCollider scMed = medLoot.AddComponent<SphereCollider>();
            scMed.isTrigger = true;
            scMed.radius = 2.2f;
            var lcMed = medLoot.AddComponent<LootContainer>();
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            ItemData bandage = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bandage.asset");
            ItemData antiviral = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_antiviral.asset");
            ItemData alcohol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_alcohol.asset");
            if (medkit != null) lcMed.AddItem(medkit, 2);
            if (bandage != null) lcMed.AddItem(bandage, 3);
            if (antiviral != null) lcMed.AddItem(antiviral, 1);
            if (alcohol != null) lcMed.AddItem(alcohol, 2);

            // Metal Locker (Hiding Spot 3)
            CreateMetalLocker(clinic.transform, new Vector3(8.5f, 0f, 8.0f), -90f, metalMat);

            // Overhead Fluorescent Lighting (Cool White/Mint #D8F3DC)
            CreateInteriorLight(clinic.transform, "Clinic_Reception_Light", new Vector3(0f, 4.2f, -4f), new Color(0.85f, 0.95f, 0.86f), 13f, 2.2f);
            CreateInteriorLight(clinic.transform, "Clinic_Pharmacy_Light", new Vector3(0f, 4.2f, 4.5f), new Color(0.85f, 0.95f, 0.86f), 14f, 2.2f);

            return clinic;
        }

        private static void CreateRedCrossEmblem(Transform parent, Vector3 localPos, float size, Material redCrossMat)
        {
            GameObject cross = new GameObject("RedCross_Emblem");
            cross.transform.SetParent(parent);
            cross.transform.localPosition = localPos;

            CreateBoxSimple(cross.transform, "Bar_Vertical", Vector3.zero, new Vector3(size * 0.30f, size, 0.08f), redCrossMat);
            CreateBoxSimple(cross.transform, "Bar_Horizontal", Vector3.zero, new Vector3(size, size * 0.30f, 0.08f), redCrossMat);
        }

        private static void CreateWheelchairProp(Transform parent, Vector3 localPos, float yRot, Material metalMat)
        {
            GameObject wc = new GameObject("Prop_Wheelchair");
            wc.transform.SetParent(parent);
            wc.transform.localPosition = localPos;
            wc.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            Material blackSeat = SceneBuilder.GetOrCreateMaterial("Mat_Seat_Leather", new Color(0.12f, 0.12f, 0.14f));
            CreateBoxSimple(wc.transform, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.06f, 0.55f), blackSeat);
            CreateBoxSimple(wc.transform, "Backrest", new Vector3(0f, 0.85f, -0.25f), new Vector3(0.55f, 0.55f, 0.06f), blackSeat);

            // 2 large wheels
            CreatePartSimple(PrimitiveType.Cylinder, wc.transform, "Wheel_L", new Vector3(-0.32f, 0.35f, 0f), new Vector3(0.65f, 0.04f, 0.65f), metalMat);
            CreatePartSimple(PrimitiveType.Cylinder, wc.transform, "Wheel_R", new Vector3(0.32f, 0.35f, 0f), new Vector3(0.65f, 0.04f, 0.65f), metalMat);
        }

        private static void CreateIVDripStand(Transform parent, Vector3 localPos, Material metalMat)
        {
            GameObject iv = new GameObject("Prop_IVDripStand");
            iv.transform.SetParent(parent);
            iv.transform.localPosition = localPos;

            CreatePartSimple(PrimitiveType.Cylinder, iv.transform, "Pole", new Vector3(0f, 0.9f, 0f), new Vector3(0.04f, 0.9f, 0.04f), metalMat);
            CreateBoxSimple(iv.transform, "Base_Cross", new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.04f, 0.45f), metalMat);

            Material fluidMat = SceneBuilder.GetOrCreateMaterial("Mat_IV_Fluid", new Color(0.85f, 0.95f, 1.0f, 0.75f), isTransparent: true);
            CreateBoxSimple(iv.transform, "Saline_Bag", new Vector3(0.12f, 1.65f, 0f), new Vector3(0.14f, 0.28f, 0.06f), fluidMat);
        }

        // 4. FORTIFIED SAFE HOUSE (Warm amber bunker, Crafting workbench, Secure Stash, Radio Station)
        private static GameObject CreateDetailedSafeHouse(
            Transform parent, Vector3 pos, Material wallMat, Material woodMat, Material metalMat, Material doorMat, Material greenSafeMat)
        {
            GameObject safeHouse = new GameObject("SafeHouse_Building");
            safeHouse.transform.SetParent(parent);
            safeHouse.transform.position = pos;

            float w = 18f;
            float h = 4.8f;
            float d = 16f;

            CreateBoxSimple(safeHouse.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);
            float doorGap = 3.2f;
            float sideW = (w - doorGap) * 0.5f;

            // Heavy Fortified Exterior Walls
            CreateBoxSimple(safeHouse.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.4f), wallMat);
            CreateBoxSimple(safeHouse.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(safeHouse.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(safeHouse.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(safeHouse.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(safeHouse.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.4f, 0.2f, d + 0.4f), wallMat);

            // Boarded Windows & Corrugated Sheet Metal Barricades on Exterior
            Material plankMat = SceneBuilder.GetOrCreateMaterial("Mat_Plank_Barricade", new Color(0.48f, 0.34f, 0.22f));
            CreateBoxSimple(safeHouse.transform, "Plank_Barricade_1", new Vector3(-5.5f, 2.0f, -d * 0.5f - 0.15f), new Vector3(2.6f, 1.4f, 0.15f), plankMat);
            CreateBoxSimple(safeHouse.transform, "Plank_Barricade_2", new Vector3(5.5f, 2.0f, -d * 0.5f - 0.15f), new Vector3(2.6f, 1.4f, 0.15f), plankMat);

            // Heavy Reinforced Steel Door
            CreateInteractiveDoorObj(safeHouse.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 2.8f, 2.5f, metalMat);

            // Glowing Green Lantern over Entrance Door (Safe Zone Beacon)
            CreateGreenSafeLantern(safeHouse.transform, new Vector3(0f, 3.2f, -d * 0.5f - 0.25f), greenSafeMat);

            // ---------------------------------------------------------
            // INTERIOR ATMOSPHERE & SURVIVAL PROPS
            // ---------------------------------------------------------
            // Crafting Workbench (Crafting table with vice, tools, lamp)
            CreateCraftingWorkbench(safeHouse.transform, new Vector3(5.5f, 0f, 4.5f), woodMat, metalMat);

            // Large Secure Stash Chest
            CreateSecureStashChest(safeHouse.transform, new Vector3(5.5f, 0f, 1.2f), metalMat);

            // Radio Communications Desk with Blinking LED Indicator
            CreateRadioStationDesk(safeHouse.transform, new Vector3(5.5f, 0f, -3.5f), woodMat, metalMat);

            // Bunk Bed with Mattresses & Pillows
            CreateBunkBed(safeHouse.transform, new Vector3(-6.2f, 0f, 4.2f), metalMat, woodMat);

            // Supply Pantry Shelves (Erzak Rafları with food cans, water)
            CreateSupplyShelves(safeHouse.transform, new Vector3(-6.2f, 0f, -1.5f), metalMat);

            // Metal Locker (Hiding Spot 4)
            CreateMetalLocker(safeHouse.transform, new Vector3(-6.5f, 0f, -5.5f), 90f, metalMat);

            // Warm Amber Shelter Lighting (#FFB703, 2200K)
            CreateInteriorLight(safeHouse.transform, "SafeHouse_Warm_Light", new Vector3(0f, 3.8f, 0f), new Color(1.0f, 0.72f, 0.05f), 12f, 2.4f);

            return safeHouse;
        }

        private static void CreateGreenSafeLantern(Transform parent, Vector3 localPos, Material greenSafeMat)
        {
            GameObject lantern = new GameObject("SafeZone_Green_Lantern");
            lantern.transform.SetParent(parent);
            lantern.transform.localPosition = localPos;

            Material ironDark = SceneBuilder.GetOrCreateMaterial("Mat_Iron_Dark", new Color(0.15f, 0.16f, 0.18f));
            CreateBoxSimple(lantern.transform, "Bracket", new Vector3(0f, 0.15f, 0f), new Vector3(0.08f, 0.35f, 0.25f), ironDark);

            GameObject lampGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lampGlass.name = "Green_Glass";
            lampGlass.transform.SetParent(lantern.transform);
            lampGlass.transform.localPosition = new Vector3(0f, -0.05f, 0.18f);
            lampGlass.transform.localScale = new Vector3(0.24f, 0.32f, 0.24f);
            lampGlass.GetComponent<Renderer>().sharedMaterial = greenSafeMat;
            Collider c = lampGlass.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            GameObject lightObj = new GameObject("Green_PointLight");
            lightObj.transform.SetParent(lantern.transform);
            lightObj.transform.localPosition = new Vector3(0f, -0.05f, 0.28f);

            Light gLight = lightObj.AddComponent<Light>();
            gLight.type = LightType.Point;
            gLight.color = new Color(0.15f, 0.85f, 0.35f);
            gLight.range = 6.5f;
            gLight.intensity = 2.4f;
            gLight.shadows = LightShadows.None;
        }

        private static void CreateCraftingWorkbench(Transform parent, Vector3 localPos, Material woodMat, Material metalMat)
        {
            GameObject bench = new GameObject("Crafting_Workbench");
            bench.transform.SetParent(parent);
            bench.transform.localPosition = localPos;

            // Thick wooden slab top
            CreateBoxSimple(bench.transform, "Top_Slab", new Vector3(0f, 0.85f, 0f), new Vector3(3.4f, 0.15f, 1.6f), woodMat);
            // 4 heavy steel legs
            CreateBoxSimple(bench.transform, "Leg_FL", new Vector3(-1.5f, 0.42f, -0.65f), new Vector3(0.15f, 0.85f, 0.15f), metalMat);
            CreateBoxSimple(bench.transform, "Leg_FR", new Vector3(1.5f, 0.42f, -0.65f), new Vector3(0.15f, 0.85f, 0.15f), metalMat);
            CreateBoxSimple(bench.transform, "Leg_BL", new Vector3(-1.5f, 0.42f, 0.65f), new Vector3(0.15f, 0.85f, 0.15f), metalMat);
            CreateBoxSimple(bench.transform, "Leg_BR", new Vector3(1.5f, 0.42f, 0.65f), new Vector3(0.15f, 0.85f, 0.15f), metalMat);

            // Bench Vice on corner
            CreateBoxSimple(bench.transform, "Bench_Vice", new Vector3(-1.35f, 1.05f, -0.65f), new Vector3(0.35f, 0.25f, 0.35f), metalMat);

            // Toolbox
            Material redTool = SceneBuilder.GetOrCreateMaterial("Mat_Toolbox_Red", new Color(0.85f, 0.15f, 0.12f));
            CreateBoxSimple(bench.transform, "Toolbox", new Vector3(0.85f, 1.02f, 0.35f), new Vector3(0.65f, 0.28f, 0.35f), redTool);
        }

        private static void CreateSecureStashChest(Transform parent, Vector3 localPos, Material metalMat)
        {
            GameObject stash = new GameObject("SafeHouse_SecureStash");
            stash.transform.SetParent(parent);
            stash.transform.localPosition = localPos;

            Material lockGold = SceneBuilder.GetOrCreateMaterial("Mat_Gold_Lock", new Color(0.92f, 0.75f, 0.20f));
            CreateBoxSimple(stash.transform, "Chest_Body", new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.85f, 1.3f), metalMat);
            CreateBoxSimple(stash.transform, "Chest_Lid", new Vector3(0f, 0.92f, 0f), new Vector3(2.25f, 0.14f, 1.35f), metalMat);
            CreateBoxSimple(stash.transform, "Padlock", new Vector3(0f, 0.75f, -0.68f), new Vector3(0.18f, 0.22f, 0.08f), lockGold);

            SphereCollider sc = stash.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.2f;

            var lc = stash.AddComponent<LootContainer>();
            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
            ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
            ItemData crowbar = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_crowbar.asset");
            if (food != null) lc.AddItem(food, 4);
            if (water != null) lc.AddItem(water, 4);
            if (scrap != null) lc.AddItem(scrap, 6);
            if (crowbar != null) lc.AddItem(crowbar, 1);
        }

        private static void CreateRadioStationDesk(Transform parent, Vector3 localPos, Material woodMat, Material metalMat)
        {
            GameObject station = new GameObject("Radio_Station");
            station.transform.SetParent(parent);
            station.transform.localPosition = localPos;

            CreateBoxSimple(station.transform, "Desk", new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.9f, 1.4f), woodMat);

            // Radio Transceiver Box
            Material radioBody = SceneBuilder.GetOrCreateMaterial("Mat_Radio_MilitaryGreen", new Color(0.18f, 0.25f, 0.18f));
            CreateBoxSimple(station.transform, "Transceiver", new Vector3(0f, 1.15f, 0f), new Vector3(1.1f, 0.50f, 0.65f), radioBody);

            // Antenna
            CreatePartSimple(PrimitiveType.Cylinder, station.transform, "Antenna", new Vector3(0.42f, 1.8f, 0.15f), new Vector3(0.02f, 0.8f, 0.02f), metalMat);

            // Pulsing / Blinking LED Indicator Light
            Material ledMat = SceneBuilder.GetOrCreateMaterial("Mat_Radio_LED", new Color(0.1f, 0.95f, 0.25f));
            ledMat.EnableKeyword("_EMISSION");
            ledMat.SetColor("_EmissionColor", new Color(0.1f, 0.95f, 0.25f) * 2.5f);

            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "Status_LED";
            led.transform.SetParent(station.transform);
            led.transform.localPosition = new Vector3(-0.35f, 1.25f, -0.34f);
            led.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            led.GetComponent<Renderer>().sharedMaterial = ledMat;
            Collider c = led.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            GameObject ledLightObj = new GameObject("LED_PointLight");
            ledLightObj.transform.SetParent(led.transform);
            ledLightObj.transform.localPosition = Vector3.zero;
            Light ledLight = ledLightObj.AddComponent<Light>();
            ledLight.type = LightType.Point;
            ledLight.color = new Color(0.1f, 0.95f, 0.25f);
            ledLight.range = 1.8f;
            ledLight.intensity = 1.2f;
            ledLight.shadows = LightShadows.None;

            FlickeringLight fl = ledLightObj.AddComponent<FlickeringLight>();
            SerializedObject so = new SerializedObject(fl);
            so.FindProperty("baseIntensity").floatValue = 1.2f;
            so.FindProperty("flickerAmount").floatValue = 0.9f;
            so.FindProperty("flickerSpeed").floatValue = 4.5f;
            so.ApplyModifiedProperties();
        }

        private static void CreateBunkBed(Transform parent, Vector3 localPos, Material metalMat, Material woodMat)
        {
            GameObject bed = new GameObject("Shelter_BunkBed");
            bed.transform.SetParent(parent);
            bed.transform.localPosition = localPos;

            Material blanketMat = SceneBuilder.GetOrCreateMaterial("Mat_Blanket_Wool", new Color(0.32f, 0.35f, 0.40f));

            // Bottom mattress
            CreateBoxSimple(bed.transform, "Lower_Frame", new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.15f, 2.6f), metalMat);
            CreateBoxSimple(bed.transform, "Lower_Mattress", new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.25f, 2.4f), blanketMat);

            // Upper mattress
            CreateBoxSimple(bed.transform, "Upper_Frame", new Vector3(0f, 1.65f, 0f), new Vector3(1.6f, 0.15f, 2.6f), metalMat);
            CreateBoxSimple(bed.transform, "Upper_Mattress", new Vector3(0f, 1.85f, 0f), new Vector3(1.4f, 0.25f, 2.4f), blanketMat);

            // 4 corner poles
            CreatePartSimple(PrimitiveType.Cylinder, bed.transform, "Pole_FL", new Vector3(-0.75f, 1.1f, -1.25f), new Vector3(0.08f, 1.1f, 0.08f), metalMat);
            CreatePartSimple(PrimitiveType.Cylinder, bed.transform, "Pole_FR", new Vector3(0.75f, 1.1f, -1.25f), new Vector3(0.08f, 1.1f, 0.08f), metalMat);
            CreatePartSimple(PrimitiveType.Cylinder, bed.transform, "Pole_BL", new Vector3(-0.75f, 1.1f, 1.25f), new Vector3(0.08f, 1.1f, 0.08f), metalMat);
            CreatePartSimple(PrimitiveType.Cylinder, bed.transform, "Pole_BR", new Vector3(0.75f, 1.1f, 1.25f), new Vector3(0.08f, 1.1f, 0.08f), metalMat);
        }

        private static void CreateSupplyShelves(Transform parent, Vector3 localPos, Material metalMat)
        {
            GameObject shelves = new GameObject("Supply_Pantry_Shelves");
            shelves.transform.SetParent(parent);
            shelves.transform.localPosition = localPos;

            CreateBoxSimple(shelves.transform, "Shelf_Frame", new Vector3(0f, 1.4f, 0f), new Vector3(2.4f, 2.8f, 0.65f), metalMat);

            // Stock with canned food loot
            GameObject lootObj = new GameObject("Pantry_Loot");
            lootObj.transform.SetParent(shelves.transform);
            lootObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            SphereCollider sc = lootObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.0f;
            var lc = lootObj.AddComponent<LootContainer>();
            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
            if (food != null) lc.AddItem(food, 3);
            if (water != null) lc.AddItem(water, 3);
        }

        // 5. SUPERMARKET & PHARMACY (Aisle stealth navigation, loot shelves, checkout counters)
        private static GameObject CreateDetailedSupermarket(
            Transform parent, Vector3 pos, Vector3 size, Material wallMat, Material signMat, Material frameMat, Material glassMat, Material doorMat, Material metalMat)
        {
            GameObject store = new GameObject("Building_Supermarket");
            store.transform.SetParent(parent);
            store.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;

            CreateBoxSimple(store.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);
            float doorGap = 3.6f;
            float sideW = (w - doorGap) * 0.5f;

            // Outer Walls
            CreateBoxSimple(store.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(store.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(store.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.4f, 0.2f, d + 0.4f), wallMat);

            // Broken Glass Storefront Windows
            Material brokenGlass = SceneBuilder.GetOrCreateMaterial("Mat_Window_BrokenGlass", new Color(0.15f, 0.22f, 0.28f, 0.45f), isTransparent: true);
            CreateBoxSimple(store.transform, "Window_Glass_L", new Vector3(-6.5f, 1.6f, -d * 0.5f - 0.05f), new Vector3(4.5f, 2.2f, 0.06f), brokenGlass);
            CreateBoxSimple(store.transform, "Window_Glass_R", new Vector3(6.5f, 1.6f, -d * 0.5f - 0.05f), new Vector3(4.5f, 2.2f, 0.06f), brokenGlass);

            // Entrance Door
            CreateInteractiveDoorObj(store.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 3.2f, 2.6f, doorMat);

            // 3D Illuminated "SUPERMARKET" Sign (Warm Yellow #FACC15)
            Create3DNeonSign(store.transform, "SUPERMARKET", new Vector3(0f, h - 0.65f, -d * 0.5f - 0.2f), new Vector3(w * 0.65f, 1.1f, 0.2f), new Color(1.0f, 0.84f, 0.0f));

            // Checkout Counter with Cash Register near door
            Material counterMat = SceneBuilder.GetOrCreateMaterial("Mat_Counter_Gray", new Color(0.30f, 0.32f, 0.36f));
            CreateBoxSimple(store.transform, "Checkout_Counter", new Vector3(4.5f, 0.55f, -5.5f), new Vector3(2.8f, 1.1f, 1.4f), counterMat);
            CreateBoxSimple(store.transform, "Cash_Register", new Vector3(4.5f, 1.25f, -5.5f), new Vector3(0.55f, 0.30f, 0.45f), metalMat);

            // 3 Parallel Product Aisles (Corridors are 2.2m wide, well over the 1.6m requirement!)
            Material shelfMat = SceneBuilder.GetOrCreateMaterial("Mat_Shelf_Metal", new Color(0.55f, 0.58f, 0.62f));
            float[] aisleX = new float[] { -6.2f, -1.8f, 2.6f };
            for (int a = 0; a < aisleX.Length; a++)
            {
                float x = aisleX[a];
                CreateBoxSimple(store.transform, $"Aisle_Shelf_{a + 1}", new Vector3(x, 1.2f, 2.2f), new Vector3(1.2f, 2.4f, 9.5f), shelfMat);

                // Add Loot on each aisle
                GameObject lootObj = new GameObject($"Supermarket_Aisle_Loot_{a + 1}");
                lootObj.transform.SetParent(store.transform);
                lootObj.transform.localPosition = new Vector3(x, 0.6f, 2.2f);
                SphereCollider sc = lootObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 2.0f;
                var lc = lootObj.AddComponent<LootContainer>();
                ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
                ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
                ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");
                if (food != null) lc.AddItem(food, 2);
                if (water != null) lc.AddItem(water, 2);
                if (battery != null) lc.AddItem(battery, 1);
            }

            // Metal Locker (Hiding Spot 5) in back storage corner
            CreateMetalLocker(store.transform, new Vector3(w * 0.5f - 2.2f, 0f, d * 0.5f - 2.5f), 180f, metalMat);

            // Supermarket Fluorescent Lighting (#FEF08A)
            CreateInteriorLight(store.transform, "Supermarket_Front_Light", new Vector3(0f, h - 1.2f, -3.5f), new Color(1.0f, 0.94f, 0.65f), 14f, 2.2f);
            CreateInteriorLight(store.transform, "Supermarket_Rear_Light", new Vector3(0f, h - 1.2f, 3.5f), new Color(1.0f, 0.94f, 0.65f), 14f, 2.2f);

            return store;
        }

        private static GameObject CreateDetailedPharmacy(
            Transform parent, Vector3 pos, Vector3 size, Material wallMat, Material signMat, Material frameMat, Material glassMat, Material doorMat, Material metalMat)
        {
            GameObject store = new GameObject("Building_Pharmacy");
            store.transform.SetParent(parent);
            store.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;

            CreateBoxSimple(store.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);
            float doorGap = 3.2f;
            float sideW = (w - doorGap) * 0.5f;

            // Outer Walls
            CreateBoxSimple(store.transform, "Wall_Back", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (sideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideW, h, 0.4f), wallMat);
            CreateBoxSimple(store.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(store.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wallMat);
            CreateBoxSimple(store.transform, "Roof", new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.4f, 0.2f, d + 0.4f), wallMat);

            CreateInteractiveDoorObj(store.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 2.8f, 2.5f, doorMat);

            // 3D Illuminated "PHARMACY" Sign (Cyan-Green #10B981)
            Create3DNeonSign(store.transform, "PHARMACY", new Vector3(0f, h - 0.65f, -d * 0.5f - 0.2f), new Vector3(w * 0.65f, 1.1f, 0.2f), new Color(0.11f, 0.91f, 0.71f));

            // Prescription Dispensing Counter
            Material whiteCounter = SceneBuilder.GetOrCreateMaterial("Mat_Counter_White", new Color(0.88f, 0.92f, 0.95f));
            CreateBoxSimple(store.transform, "Prescription_Counter", new Vector3(0f, 0.55f, -3.2f), new Vector3(5.2f, 1.1f, 1.2f), whiteCounter);

            // White Medicine Shelves forming aisles
            Material whiteShelf = SceneBuilder.GetOrCreateMaterial("Mat_Shelf_White", new Color(0.92f, 0.94f, 0.96f));
            CreateBoxSimple(store.transform, "Medicine_Shelf_1", new Vector3(-4.5f, 1.1f, 1.8f), new Vector3(1.1f, 2.2f, 6.5f), whiteShelf);
            CreateBoxSimple(store.transform, "Medicine_Shelf_2", new Vector3(4.5f, 1.1f, 1.8f), new Vector3(1.1f, 2.2f, 6.5f), whiteShelf);
            CreateBoxSimple(store.transform, "Medicine_Shelf_Rear", new Vector3(0f, 1.1f, 5.2f), new Vector3(6.5f, 2.2f, 1.1f), whiteShelf);

            // Collectible Medical Loot on Shelves
            GameObject pharmLoot = new GameObject("Pharmacy_Supplies_Loot");
            pharmLoot.transform.SetParent(store.transform);
            pharmLoot.transform.localPosition = new Vector3(0f, 0.6f, 1.8f);
            SphereCollider sc = pharmLoot.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.4f;
            var lc = pharmLoot.AddComponent<LootContainer>();
            ItemData alcohol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_alcohol.asset");
            ItemData bandage = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bandage.asset");
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            ItemData cloth = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_cloth.asset");
            if (alcohol != null) lc.AddItem(alcohol, 3);
            if (bandage != null) lc.AddItem(bandage, 3);
            if (medkit != null) lc.AddItem(medkit, 1);
            if (cloth != null) lc.AddItem(cloth, 4);

            // Metal Locker (Hiding Spot)
            CreateMetalLocker(store.transform, new Vector3(-w * 0.5f + 1.8f, 0f, d * 0.5f - 2.0f), 90f, frameMat);

            // Mint-Green Fluorescent Lighting (#A7F3D0)
            CreateInteriorLight(store.transform, "Pharmacy_Interior_Light", new Vector3(0f, h - 1.2f, 0f), new Color(0.65f, 0.95f, 0.82f), 13f, 2.1f);

            return store;
        }

        private static void CreateInteriorLight(Transform parent, string name, Vector3 localPos, Color lightColor, float range, float intensity, bool flickering = false)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = localPos;

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = lightColor;
            pLight.range = range;
            pLight.intensity = intensity;
            pLight.shadows = LightShadows.None; // Maximum frame rate

            if (flickering || name.Contains("Clinic_Reception"))
            {
                lightObj.AddComponent<FlickeringLight>();
            }
        }

        // =========================================================================
        // DUMPSTERS & METAL LOCKERS (HIDING SPOTS)
        // =========================================================================
        private static void CreateDumpster(Transform parent, Vector3 pos, float yRot, Material metalMat)
        {
            GameObject dumpster = new GameObject("MetalDumpster");
            dumpster.transform.SetParent(parent);
            dumpster.transform.position = pos;
            dumpster.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Dumpster_Body";
            body.transform.SetParent(dumpster.transform);
            body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            body.transform.localScale = new Vector3(2.2f, 1.25f, 1.3f);
            if (metalMat != null) body.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(body, StaticEditorFlags.NavigationStatic);

            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Dumpster_Lid";
            lid.transform.SetParent(dumpster.transform);
            lid.transform.localPosition = new Vector3(0f, 1.30f, 0f);
            lid.transform.localScale = new Vector3(2.25f, 0.08f, 1.35f);
            if (metalMat != null) lid.GetComponent<Renderer>().sharedMaterial = metalMat;

            GameObject peekObj = new GameObject("PeekPoint");
            peekObj.transform.SetParent(dumpster.transform);
            peekObj.transform.localPosition = new Vector3(0f, 1.25f, 0.55f);
            peekObj.transform.localRotation = Quaternion.identity;

            GameObject exitObj = new GameObject("ExitPoint");
            exitObj.transform.SetParent(dumpster.transform);
            exitObj.transform.localPosition = new Vector3(0f, 0f, 1.6f);
            exitObj.transform.localRotation = Quaternion.identity;

            SphereCollider sc = dumpster.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.4f;

            var hiding = dumpster.AddComponent<HidingSpot>();
            SerializedObject so = new SerializedObject(hiding);
            so.FindProperty("spotType").enumValueIndex = 0; // Dumpster
            so.FindProperty("doorOrLid").objectReferenceValue = lid.transform;
            so.FindProperty("peekPoint").objectReferenceValue = peekObj.transform;
            so.FindProperty("exitPoint").objectReferenceValue = exitObj.transform;
            so.ApplyModifiedProperties();
        }

        private static void CreateMetalLocker(Transform parent, Vector3 localPos, float yRot, Material metalMat)
        {
            GameObject locker = new GameObject("HidingLocker");
            locker.transform.SetParent(parent);
            locker.transform.localPosition = localPos;
            locker.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Locker_Body";
            body.transform.SetParent(locker.transform);
            body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            body.transform.localScale = new Vector3(0.85f, 2.2f, 0.70f);
            if (metalMat != null) body.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(body, StaticEditorFlags.NavigationStatic);

            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Locker_Door";
            door.transform.SetParent(locker.transform);
            door.transform.localPosition = new Vector3(0.38f, 1.1f, 0.36f);
            door.transform.localScale = new Vector3(0.78f, 2.15f, 0.05f);
            Material greenMat = SceneBuilder.GetOrCreateMaterial("Mat_Locker_Green", new Color(0.20f, 0.32f, 0.25f));
            door.GetComponent<Renderer>().sharedMaterial = greenMat;

            GameObject peekObj = new GameObject("PeekPoint");
            peekObj.transform.SetParent(locker.transform);
            peekObj.transform.localPosition = new Vector3(0f, 1.65f, 0.38f);
            peekObj.transform.localRotation = Quaternion.identity;

            GameObject exitObj = new GameObject("ExitPoint");
            exitObj.transform.SetParent(locker.transform);
            exitObj.transform.localPosition = new Vector3(0f, 0f, 1.3f);
            exitObj.transform.localRotation = Quaternion.identity;

            SphereCollider sc = locker.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.9f;

            var hiding = locker.AddComponent<HidingSpot>();
            SerializedObject so = new SerializedObject(hiding);
            so.FindProperty("spotType").enumValueIndex = 1; // Locker
            so.FindProperty("doorOrLid").objectReferenceValue = door.transform;
            so.FindProperty("peekPoint").objectReferenceValue = peekObj.transform;
            so.FindProperty("exitPoint").objectReferenceValue = exitObj.transform;
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateInteractiveDoorObj(Transform parent, Vector3 localPos, float yRot, float width, float height, Material doorMat)
        {
            GameObject doorHinge = new GameObject("InteractiveDoor_Hinge");
            doorHinge.transform.SetParent(parent);
            doorHinge.transform.localPosition = localPos;
            doorHinge.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "Door_Leaf";
            doorLeaf.transform.SetParent(doorHinge.transform);
            doorLeaf.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);
            doorLeaf.transform.localScale = new Vector3(width, height, 0.12f);
            if (doorMat != null) doorLeaf.GetComponent<Renderer>().sharedMaterial = doorMat;

            Material handleMat = SceneBuilder.GetOrCreateMaterial("Mat_Brass_Handle", new Color(0.85f, 0.72f, 0.30f));
            CreateBoxSimple(doorLeaf.transform, "Handle", new Vector3(width * 0.38f, 0f, 0.08f), new Vector3(0.06f, 0.18f, 0.08f), handleMat);

            var obs = doorLeaf.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obs.carving = true;
            obs.size = new Vector3(width, height, 0.2f);

            BoxCollider bc = doorHinge.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = new Vector3(width * 0.5f, height * 0.5f, 0f);
            bc.size = new Vector3(width + 1.8f, height, 2.6f);

            var doorComp = doorHinge.AddComponent<ProjectSurvival.Environment.InteractiveDoor>();
            SerializedObject so = new SerializedObject(doorComp);
            so.FindProperty("doorHinge").objectReferenceValue = doorHinge.transform;
            so.ApplyModifiedProperties();

            return doorHinge;
        }

        private static void Create3DNeonSign(Transform parent, string text, Vector3 localPos, Vector3 size, Color neonColor)
        {
            GameObject signObj = new GameObject("NeonSign_" + text);
            signObj.transform.SetParent(parent);
            signObj.transform.localPosition = localPos;

            Material boardMat = SceneBuilder.GetOrCreateMaterial("Mat_Sign_Board", new Color(0.08f, 0.09f, 0.11f));
            CreateBoxSimple(signObj.transform, "Sign_Backboard", Vector3.zero, size, boardMat);

            Material neonMat = SceneBuilder.GetOrCreateMaterial($"Mat_Neon_{neonColor.GetHashCode()}", neonColor);
            neonMat.EnableKeyword("_EMISSION");
            neonMat.SetColor("_EmissionColor", neonColor * 2.5f);

            GameObject neonBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonBar.name = "Neon_GlowBar";
            neonBar.transform.SetParent(signObj.transform);
            neonBar.transform.localPosition = new Vector3(0f, 0f, size.z * 0.5f + 0.06f);
            neonBar.transform.localScale = new Vector3(size.x * 0.92f, size.y * 0.55f, 0.08f);
            neonBar.GetComponent<Renderer>().sharedMaterial = neonMat;
            Collider c = neonBar.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            GameObject lObj = new GameObject("Neon_PointLight");
            lObj.transform.SetParent(signObj.transform);
            lObj.transform.localPosition = new Vector3(0f, 0f, size.z * 0.5f + 0.7f);
            Light nLight = lObj.AddComponent<Light>();
            nLight.type = LightType.Point;
            nLight.color = neonColor;
            nLight.range = 8.5f;
            nLight.intensity = 2.2f;
            nLight.shadows = LightShadows.None;
        }

        private static void CreateCrashedPoliceCarWithFlare(Transform parent, Vector3 pos)
        {
            GameObject cruiser = new GameObject("Crashed_PoliceCruiser");
            cruiser.transform.SetParent(parent);
            cruiser.transform.position = pos;
            cruiser.transform.rotation = Quaternion.Euler(-3f, 18f, -6f);

            Material carBlack = SceneBuilder.GetOrCreateMaterial("Mat_Car_Police_Black", new Color(0.08f, 0.09f, 0.11f), smoothness: 0.85f);
            Material carWhite = SceneBuilder.GetOrCreateMaterial("Mat_Car_Police_White", new Color(0.95f, 0.95f, 0.95f), smoothness: 0.85f);
            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Car_Glass", new Color(0.08f, 0.12f, 0.16f), smoothness: 0.95f);
            Material redLightMat = SceneBuilder.GetOrCreateMaterial("Mat_Beacon_Red", new Color(1.0f, 0.08f, 0.08f));
            Material blueLightMat = SceneBuilder.GetOrCreateMaterial("Mat_Beacon_Blue", new Color(0.08f, 0.25f, 1.0f));

            CreateBoxSimple(cruiser.transform, "Chassis", new Vector3(0f, 0.45f, 0f), new Vector3(2.1f, 0.65f, 4.6f), carBlack);
            CreateBoxSimple(cruiser.transform, "Cabin", new Vector3(0f, 1.05f, -0.15f), new Vector3(1.85f, 0.65f, 2.5f), carWhite);
            CreateBoxSimple(cruiser.transform, "Windshield", new Vector3(0f, 1.15f, 1.05f), new Vector3(1.80f, 0.55f, 0.12f), glassMat);

            CreateBoxSimple(cruiser.transform, "Lightbar_Red", new Vector3(-0.45f, 1.45f, -0.15f), new Vector3(0.55f, 0.14f, 0.35f), redLightMat);
            CreateBoxSimple(cruiser.transform, "Lightbar_Blue", new Vector3(0.45f, 1.45f, -0.15f), new Vector3(0.55f, 0.14f, 0.35f), blueLightMat);

            // Red Emergency Flare in Open Trunk
            GameObject flareObj = new GameObject("Emergency_Flare");
            flareObj.transform.SetParent(cruiser.transform);
            flareObj.transform.localPosition = new Vector3(0f, 0.72f, -2.15f);

            GameObject flareCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flareCylinder.name = "Flare_Stick";
            flareCylinder.transform.SetParent(flareObj.transform);
            flareCylinder.transform.localPosition = Vector3.zero;
            flareCylinder.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
            flareCylinder.GetComponent<Renderer>().sharedMaterial = redLightMat;
            Collider fc = flareCylinder.GetComponent<Collider>();
            if (fc != null) UnityEngine.Object.DestroyImmediate(fc);

            Light fLight = flareObj.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(1.0f, 0.15f, 0.15f);
            fLight.range = 6.5f;
            fLight.intensity = 2.6f;
            fLight.shadows = LightShadows.None;

            // Trunk Loot Container
            GameObject trunkLoot = new GameObject("CrashedPoliceLoot");
            trunkLoot.transform.SetParent(cruiser.transform);
            trunkLoot.transform.localPosition = new Vector3(0f, 0.65f, -1.8f);

            SphereCollider sc = trunkLoot.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.5f;

            var lc = trunkLoot.AddComponent<LootContainer>();
            ItemData pistol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/weapon_pistol.asset");
            ItemData ammo9 = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_9mm.asset");
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            if (pistol != null) lc.AddItem(pistol, 1);
            if (ammo9 != null) lc.AddItem(ammo9, 24);
            if (medkit != null) lc.AddItem(medkit, 1);

            cruiser.AddComponent<EmergencyLightPulsar>();
        }

        private static void CreateTrafficCone(Transform parent, Vector3 pos, float yRot, bool knockedOver = false)
        {
            GameObject cone = new GameObject("Prop_TrafficCone");
            cone.transform.SetParent(parent);
            cone.transform.position = pos;
            cone.transform.rotation = Quaternion.Euler(knockedOver ? 78f : 0f, yRot, knockedOver ? 25f : 0f);

            Material orangeMat = SceneBuilder.GetOrCreateMaterial("Mat_Traffic_Orange", new Color(1.0f, 0.38f, 0.05f));
            Material whiteMat = SceneBuilder.GetOrCreateMaterial("Mat_Traffic_White", new Color(0.95f, 0.95f, 0.95f));

            CreateBoxSimple(cone.transform, "Cone_Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.48f, 0.04f, 0.48f), orangeMat);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Cone_Body";
            body.transform.SetParent(cone.transform);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
            body.GetComponent<Renderer>().sharedMaterial = orangeMat;
            GameObjectUtility.SetStaticEditorFlags(body, StaticEditorFlags.NavigationStatic);

            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe.name = "Cone_Stripe";
            stripe.transform.SetParent(cone.transform);
            stripe.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            stripe.transform.localScale = new Vector3(0.33f, 0.10f, 0.33f);
            stripe.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Collider sc = stripe.GetComponent<Collider>();
            if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
        }

        // =========================================================================
        // INFRASTRUCTURE HELPERS
        // =========================================================================
        private static void CreateRoadPiece(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent);
            road.transform.position = pos;
            road.transform.localScale = size;
            if (mat != null) road.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(road, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateSidewalk(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = name;
            sw.transform.SetParent(parent);
            sw.transform.position = pos;
            sw.transform.localScale = size;
            if (mat != null) sw.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(sw, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateRoadMark(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "RoadMarking";
            mark.transform.SetParent(parent);
            mark.transform.position = pos;
            mark.transform.localScale = size;
            if (mat != null) mark.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = mark.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        private static void CreateZebraCrossing(Transform parent, Vector3 centerPos, bool crossX, Material mat)
        {
            int stripes = 8;
            float stripeLength = 4.2f;
            float stripeWidth = 0.55f;
            float spacing = 0.95f;

            for (int i = 0; i < stripes; i++)
            {
                float offset = (i - stripes * 0.5f + 0.5f) * spacing;
                Vector3 pos = crossX
                    ? new Vector3(centerPos.x + offset, centerPos.y, centerPos.z)
                    : new Vector3(centerPos.x, centerPos.y, centerPos.z + offset);

                Vector3 scale = crossX
                    ? new Vector3(stripeWidth, 0.015f, stripeLength)
                    : new Vector3(stripeLength, 0.015f, stripeWidth);

                CreateRoadMark(parent, pos, scale, mat);
            }
        }

        private static void CreateEpicSkyscraper(
            Transform parent, string name, Vector3 pos, Vector3 size,
            Material wallMat, Material glassMat, Material litMat, Material redBeaconMat, bool hasHelipad = false)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent);
            tower.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Core_Structure";
            core.transform.SetParent(tower.transform);
            core.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            core.transform.localScale = size;
            if (wallMat != null) core.GetComponent<Renderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(core, StaticEditorFlags.NavigationStatic);

            // Scattered Emissive Windows along skyscraper faces (Nocturnal City Glow)
            Material winYellow = SceneBuilder.GetOrCreateMaterial("Mat_Window_LitYellow", new Color(1.0f, 0.90f, 0.45f));
            winYellow.EnableKeyword("_EMISSION");
            winYellow.SetColor("_EmissionColor", new Color(1.0f, 0.90f, 0.45f) * 1.8f);

            Material winCyan = SceneBuilder.GetOrCreateMaterial("Mat_Window_LitCyan", new Color(0.40f, 0.85f, 1.0f));
            winCyan.EnableKeyword("_EMISSION");
            winCyan.SetColor("_EmissionColor", new Color(0.40f, 0.85f, 1.0f) * 1.6f);

            System.Random rnd = new System.Random(name.GetHashCode());
            int floors = Mathf.FloorToInt(h / 3.8f);
            for (int f = 1; f < floors; f++)
            {
                float floorY = f * 3.8f;
                // South and North faces
                for (float wx = -w * 0.4f; wx <= w * 0.4f; wx += 3.2f)
                {
                    if (rnd.NextDouble() < 0.42) // 42% chance of illuminated window
                    {
                        Material wm = rnd.NextDouble() < 0.75 ? winYellow : winCyan;
                        GameObject winS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        winS.name = $"LitWin_S_{f}";
                        winS.transform.SetParent(tower.transform);
                        winS.transform.localPosition = new Vector3(wx, floorY, -d * 0.5f - 0.05f);
                        winS.transform.localScale = new Vector3(1.4f, 1.8f, 0.05f);
                        winS.GetComponent<Renderer>().sharedMaterial = wm;
                        Collider wc = winS.GetComponent<Collider>();
                        if (wc != null) UnityEngine.Object.DestroyImmediate(wc);
                    }
                }
            }

            // Red Aviation Warning Beacon on roof
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "AviationBeacon";
            beacon.transform.SetParent(tower.transform);
            beacon.transform.localPosition = new Vector3(0f, h + 1.2f, 0f);
            beacon.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            if (redBeaconMat != null) beacon.GetComponent<Renderer>().sharedMaterial = redBeaconMat;
            Collider c = beacon.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void CreateApartmentBlock(
            Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material frameMat, Material glassMat, Material litMat)
        {
            GameObject apt = new GameObject(name);
            apt.transform.SetParent(parent);
            apt.transform.position = pos;

            float h = size.y;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Apartment_Core";
            body.transform.SetParent(apt.transform);
            body.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            body.transform.localScale = size;
            if (wallMat != null) body.GetComponent<Renderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(body, StaticEditorFlags.NavigationStatic);
        }

        private static GameObject CreateWalkableResidentialHouse(
            Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material woodMat, Material doorMat)
        {
            GameObject house = new GameObject(name);
            house.transform.SetParent(parent);
            house.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;

            // Floor
            CreateBoxSimple(house.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), wallMat);

            float doorGap = 2.4f;
            float frontSideW = (w - doorGap) * 0.5f;

            // Back wall with rear back door
            float backDoorGap = 2.2f;
            float backSideW = (w - backDoorGap) * 0.5f;
            CreateBoxSimple(house.transform, "Wall_Back_L", new Vector3(-(w * 0.5f) + (backSideW * 0.5f), h * 0.5f, d * 0.5f), new Vector3(backSideW, h, 0.35f), wallMat);
            CreateBoxSimple(house.transform, "Wall_Back_R", new Vector3((w * 0.5f) - (backSideW * 0.5f), h * 0.5f, d * 0.5f), new Vector3(backSideW, h, 0.35f), wallMat);

            // Front wall with front entrance door
            CreateBoxSimple(house.transform, "Wall_Front_L", new Vector3(-(w * 0.5f) + (frontSideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(frontSideW, h, 0.35f), wallMat);
            CreateBoxSimple(house.transform, "Wall_Front_R", new Vector3((w * 0.5f) - (frontSideW * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(frontSideW, h, 0.35f), wallMat);

            // Left & Right walls
            CreateBoxSimple(house.transform, "Wall_Left", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, d), wallMat);
            CreateBoxSimple(house.transform, "Wall_Right", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, d), wallMat);

            // Roof
            CreateBoxSimple(house.transform, "Roof", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.6f, 0.25f, d + 0.6f), wallMat);

            // Front Porch (Sundurma)
            CreateBoxSimple(house.transform, "Porch_Deck", new Vector3(0f, 0.18f, -d * 0.5f - 1.2f), new Vector3(6.5f, 0.25f, 2.4f), woodMat);
            CreateBoxSimple(house.transform, "Porch_Roof", new Vector3(0f, 3.8f, -d * 0.5f - 1.2f), new Vector3(6.5f, 0.20f, 2.4f), woodMat);
            CreateBoxSimple(house.transform, "Porch_Pillar_L", new Vector3(-2.8f, 1.9f, -d * 0.5f - 2.2f), new Vector3(0.18f, 3.6f, 0.18f), woodMat);
            CreateBoxSimple(house.transform, "Porch_Pillar_R", new Vector3(2.8f, 1.9f, -d * 0.5f - 2.2f), new Vector3(0.18f, 3.6f, 0.18f), woodMat);

            // Front Door & Back Door (Interactive Doors)
            CreateInteractiveDoorObj(house.transform, new Vector3(-doorGap * 0.5f, 0f, -d * 0.5f), 0f, 2.2f, 2.4f, doorMat);
            CreateInteractiveDoorObj(house.transform, new Vector3(-backDoorGap * 0.5f, 0f, d * 0.5f), 180f, 2.0f, 2.4f, doorMat);

            // Garden Picket Fence with Alley Gate Gaps (Providing Zombie Escape Paths)
            Material fenceMat = SceneBuilder.GetOrCreateMaterial("Mat_Picket_Fence", new Color(0.85f, 0.88f, 0.90f));
            CreateBoxSimple(house.transform, "Fence_Front_L", new Vector3(-5.5f, 0.55f, -d * 0.5f - 3.2f), new Vector3(4.5f, 1.1f, 0.08f), fenceMat);
            CreateBoxSimple(house.transform, "Fence_Front_R", new Vector3(5.5f, 0.55f, -d * 0.5f - 3.2f), new Vector3(4.5f, 1.1f, 0.08f), fenceMat);
            CreateBoxSimple(house.transform, "Fence_Side_L", new Vector3(-w * 0.5f - 1.6f, 0.55f, 0f), new Vector3(0.08f, 1.1f, d + 3.0f), fenceMat);
            CreateBoxSimple(house.transform, "Fence_Side_R", new Vector3(w * 0.5f + 1.6f, 0.55f, 0f), new Vector3(0.08f, 1.1f, d + 3.0f), fenceMat);

            // ---------------------------------------------------------
            // INTERIOR ROOMS (Clear corridors >= 1.8m)
            // ---------------------------------------------------------
            CreateBoxSimple(house.transform, "Partition_Wall_Rear", new Vector3(-2.8f, h * 0.5f, 0.5f), new Vector3(7.5f, h, 0.3f), wallMat);

            // 1. Living Room (Front Left)
            Material sofaMat = SceneBuilder.GetOrCreateMaterial("Mat_Sofa_Fabric", new Color(0.25f, 0.28f, 0.32f));
            CreateBoxSimple(house.transform, "Sofa", new Vector3(-4.5f, 0.45f, -3.2f), new Vector3(2.6f, 0.75f, 1.1f), sofaMat);
            CreateBoxSimple(house.transform, "Coffee_Table", new Vector3(-4.5f, 0.30f, -1.8f), new Vector3(1.6f, 0.45f, 0.9f), woodMat);
            CreateBoxSimple(house.transform, "Armchair", new Vector3(-2.2f, 0.45f, -1.8f), new Vector3(0.95f, 0.75f, 0.95f), sofaMat);

            // 2. Kitchen (Front Right)
            Material counterMat = SceneBuilder.GetOrCreateMaterial("Mat_Kitchen_Counter", new Color(0.38f, 0.40f, 0.44f));
            CreateBoxSimple(house.transform, "Kitchen_Counter_L", new Vector3(4.5f, 0.55f, -3.5f), new Vector3(3.5f, 1.0f, 1.1f), counterMat);
            CreateBoxSimple(house.transform, "Kitchen_Sink", new Vector3(4.5f, 1.06f, -3.5f), new Vector3(0.85f, 0.05f, 0.55f), woodMat);

            // Food & Water Loot in Kitchen
            GameObject kitchenLoot = new GameObject("Kitchen_Loot");
            kitchenLoot.transform.SetParent(house.transform);
            kitchenLoot.transform.localPosition = new Vector3(4.5f, 1.1f, -3.5f);
            SphereCollider scK = kitchenLoot.AddComponent<SphereCollider>();
            scK.isTrigger = true;
            scK.radius = 1.8f;
            var lcK = kitchenLoot.AddComponent<LootContainer>();
            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
            if (food != null) lcK.AddItem(food, 2);
            if (water != null) lcK.AddItem(water, 2);

            // 3. Bedroom (Rear)
            Material bedsheetMat = SceneBuilder.GetOrCreateMaterial("Mat_Bedsheet_Pattern", new Color(0.72f, 0.65f, 0.55f));
            CreateBoxSimple(house.transform, "Bed_Mattress", new Vector3(-3.8f, 0.45f, 3.8f), new Vector3(2.0f, 0.55f, 2.5f), bedsheetMat);
            CreateBoxSimple(house.transform, "Bed_Pillow", new Vector3(-3.8f, 0.82f, 4.8f), new Vector3(1.4f, 0.18f, 0.45f), bedsheetMat);
            CreateBoxSimple(house.transform, "Nightstand", new Vector3(-5.2f, 0.35f, 4.5f), new Vector3(0.65f, 0.65f, 0.65f), woodMat);

            // Wardrobe / Metal Locker (Hiding Spot)
            Material lockerMat = SceneBuilder.GetOrCreateMaterial("Mat_Chest_Metal", new Color(0.35f, 0.38f, 0.42f));
            CreateMetalLocker(house.transform, new Vector3(4.5f, 0f, 4.2f), -90f, lockerMat);

            // Warm Interior Lighting (#FEF3C7)
            CreateInteriorLight(house.transform, "LivingRoom_Light", new Vector3(-3.5f, 3.8f, -2.5f), new Color(1.0f, 0.94f, 0.75f), 10f, 2.0f);
            CreateInteriorLight(house.transform, "Bedroom_Light", new Vector3(-2.0f, 3.8f, 3.8f), new Color(1.0f, 0.92f, 0.70f), 10f, 1.8f);

            return house;
        }

        private static GameObject CreateDetailedGasStationWithGarage(
            Transform parent, Vector3 pos, Material wallMat, Material metalMat, Material yellowMat, Material doorMat)
        {
            GameObject station = new GameObject("Facility_GasStation");
            station.transform.SetParent(parent);
            station.transform.position = pos;

            Material canopyMat = SceneBuilder.GetOrCreateMaterial("Mat_Canopy_Red", new Color(0.85f, 0.18f, 0.15f));
            Material concreteMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Station", new Color(0.55f, 0.56f, 0.58f));

            // Forecourt Slab & Overhead Canopy
            CreateBoxSimple(station.transform, "Forecourt_Slab", new Vector3(0f, 0.08f, 0f), new Vector3(26f, 0.16f, 20f), concreteMat);
            CreateBoxSimple(station.transform, "Overhead_Canopy", new Vector3(0f, 5.2f, 0f), new Vector3(22f, 0.65f, 16f), canopyMat);

            Vector3[] pillarOffsets = new Vector3[]
            {
                new Vector3(-8f, 2.5f, -5f), new Vector3(8f, 2.5f, -5f),
                new Vector3(-8f, 2.5f, 5f),  new Vector3(8f, 2.5f, 5f)
            };
            for (int i = 0; i < pillarOffsets.Length; i++)
            {
                CreatePartSimple(PrimitiveType.Cylinder, station.transform, $"Canopy_Pillar_{i + 1}", pillarOffsets[i], new Vector3(0.55f, 2.5f, 0.55f), metalMat);
            }

            Vector3[] pumpOffsets = new Vector3[]
            {
                new Vector3(-5f, 0.9f, -3f), new Vector3(5f, 0.9f, -3f),
                new Vector3(-5f, 0.9f, 3f),  new Vector3(5f, 0.9f, 3f)
            };
            for (int i = 0; i < pumpOffsets.Length; i++)
            {
                CreateBoxSimple(station.transform, $"Fuel_Pump_{i + 1}", pumpOffsets[i], new Vector3(1.2f, 1.8f, 0.85f), yellowMat);
            }

            // ---------------------------------------------------------
            // 1. WALKABLE STATION SHOP / CONVENIENCE STORE (Right side)
            // ---------------------------------------------------------
            GameObject shop = new GameObject("Station_Shop_Walkable");
            shop.transform.SetParent(station.transform);
            shop.transform.localPosition = new Vector3(18f, 0f, -5f);

            float sw = 12f;
            float sh = 4.8f;
            float sd = 14f;

            CreateBoxSimple(shop.transform, "Shop_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(sw, 0.1f, sd), wallMat);
            float sDoorGap = 2.4f;
            float sSideW = (sw - sDoorGap) * 0.5f;

            CreateBoxSimple(shop.transform, "Shop_Wall_Back", new Vector3(0f, sh * 0.5f, sd * 0.5f), new Vector3(sw, sh, 0.35f), wallMat);
            CreateBoxSimple(shop.transform, "Shop_Wall_Front_L", new Vector3(-(sw * 0.5f) + (sSideW * 0.5f), sh * 0.5f, -sd * 0.5f), new Vector3(sSideW, sh, 0.35f), wallMat);
            CreateBoxSimple(shop.transform, "Shop_Wall_Front_R", new Vector3((sw * 0.5f) - (sSideW * 0.5f), sh * 0.5f, -sd * 0.5f), new Vector3(sSideW, sh, 0.35f), wallMat);
            CreateBoxSimple(shop.transform, "Shop_Wall_Right", new Vector3(sw * 0.5f, sh * 0.5f, 0f), new Vector3(0.35f, sh, sd), wallMat);
            CreateBoxSimple(shop.transform, "Shop_Roof", new Vector3(0f, sh + 0.1f, 0f), new Vector3(sw + 0.4f, 0.2f, sd + 0.4f), wallMat);

            CreateInteractiveDoorObj(shop.transform, new Vector3(-sDoorGap * 0.5f, 0f, -sd * 0.5f), 0f, 2.2f, 2.5f, doorMat);

            // Shop Interior: Counter & Shelves
            Material counterMat = SceneBuilder.GetOrCreateMaterial("Mat_Counter_Gray", new Color(0.30f, 0.32f, 0.36f));
            CreateBoxSimple(shop.transform, "Counter", new Vector3(3.2f, 0.55f, -3.5f), new Vector3(2.2f, 1.1f, 1.2f), counterMat);

            Material shelfMat = SceneBuilder.GetOrCreateMaterial("Mat_Shelf_Metal", new Color(0.55f, 0.58f, 0.62f));
            CreateBoxSimple(shop.transform, "Shop_Shelf_1", new Vector3(-3.2f, 1.1f, 0f), new Vector3(1.1f, 2.2f, 6.5f), shelfMat);

            GameObject shopLoot = new GameObject("Station_Loot_Shelves");
            shopLoot.transform.SetParent(shop.transform);
            shopLoot.transform.localPosition = new Vector3(-3.2f, 0.6f, 0f);
            SphereCollider scShop = shopLoot.AddComponent<SphereCollider>();
            scShop.isTrigger = true;
            scShop.radius = 2.0f;
            var lcShop = shopLoot.AddComponent<LootContainer>();
            ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");
            ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            if (battery != null) lcShop.AddItem(battery, 2);
            if (scrap != null) lcShop.AddItem(scrap, 4);
            if (food != null) lcShop.AddItem(food, 2);

            CreateInteriorLight(shop.transform, "Shop_Light", new Vector3(0f, 3.8f, 0f), new Color(1.0f, 0.94f, 0.70f), 11f, 2.0f);

            // ---------------------------------------------------------
            // 2. REAR AUTO REPAIR GARAGE / WORKSHOP (Tamir Garajı)
            // ---------------------------------------------------------
            GameObject garage = new GameObject("Garage_Workshop");
            garage.transform.SetParent(station.transform);
            garage.transform.localPosition = new Vector3(18f, 0f, 11f);

            float gw = 12f;
            float gh = 5.2f;
            float gd = 14f;

            CreateBoxSimple(garage.transform, "Garage_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(gw, 0.1f, gd), concreteMat);
            float gDoorGap = 4.2f;
            float gSideW = (gw - gDoorGap) * 0.5f;

            CreateBoxSimple(garage.transform, "Garage_Wall_Back", new Vector3(0f, gh * 0.5f, gd * 0.5f), new Vector3(gw, gh, 0.35f), wallMat);
            CreateBoxSimple(garage.transform, "Garage_Wall_Front_L", new Vector3(-(gw * 0.5f) + (gSideW * 0.5f), gh * 0.5f, -gd * 0.5f), new Vector3(gSideW, gh, 0.35f), wallMat);
            CreateBoxSimple(garage.transform, "Garage_Wall_Front_R", new Vector3((gw * 0.5f) - (gSideW * 0.5f), gh * 0.5f, -gd * 0.5f), new Vector3(gSideW, gh, 0.35f), wallMat);
            CreateBoxSimple(garage.transform, "Garage_Wall_Right", new Vector3(gw * 0.5f, gh * 0.5f, 0f), new Vector3(0.35f, gh, gd), wallMat);
            CreateBoxSimple(garage.transform, "Garage_Roof", new Vector3(0f, gh + 0.1f, 0f), new Vector3(gw + 0.4f, 0.2f, gd + 0.4f), wallMat);

            // Mechanic's Workbench
            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_Cover_Wood", new Color(1f, 1f, 1f));
            CreateBoxSimple(garage.transform, "Mechanic_Workbench", new Vector3(0f, 0.55f, 4.8f), new Vector3(3.8f, 1.0f, 1.4f), woodMat);

            // Stacks of Car Tires
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));
            for (float ty = 0.2f; ty <= 0.8f; ty += 0.28f)
            {
                CreatePartSimple(PrimitiveType.Cylinder, garage.transform, $"Tire_{ty}", new Vector3(-3.8f, ty, 3.8f), new Vector3(0.75f, 0.14f, 0.75f), tireMat);
            }

            // Red Fuel Storage Drums
            Material fuelDrumMat = SceneBuilder.GetOrCreateMaterial("Mat_Fuel_Drum_Red", new Color(0.85f, 0.15f, 0.12f));
            CreatePartSimple(PrimitiveType.Cylinder, garage.transform, "Fuel_Drum_1", new Vector3(3.8f, 0.55f, 3.8f), new Vector3(0.8f, 0.55f, 0.8f), fuelDrumMat);
            CreatePartSimple(PrimitiveType.Cylinder, garage.transform, "Fuel_Drum_2", new Vector3(3.8f, 0.55f, 2.6f), new Vector3(0.8f, 0.55f, 0.8f), fuelDrumMat);

            // Metal Locker (Hiding Spot in garage)
            CreateMetalLocker(garage.transform, new Vector3(-4.2f, 0f, -2.5f), 90f, metalMat);

            // Garage Light
            CreateInteriorLight(garage.transform, "Garage_Light", new Vector3(0f, 4.2f, 0f), new Color(0.75f, 0.90f, 1.0f), 13f, 2.2f);

            return station;
        }

        private static void CreateEastBarricade(Transform parent, Vector3 centerPos, Material barrierMat, Material sandbagMat, Material metalMat)
        {
            GameObject barricade = new GameObject("East_Barricade_Tactical");
            barricade.transform.SetParent(parent);
            barricade.transform.position = centerPos;

            // Overturned Cargo Delivery Van
            VehicleFleetBuilder.CreateCargoVan(barricade.transform, "Overturned_CargoVan", new Vector3(0f, 0.8f, 0f), 35f);

            // Concrete Jersey Barriers
            CreateJerseyBarrier(barricade.transform, new Vector3(-3.5f, 0f, 1.5f), 15f, barrierMat);
            CreateJerseyBarrier(barricade.transform, new Vector3(3.8f, 0f, -1.2f), -25f, barrierMat);

            // Sandbag Bunkers
            CreateSandbagBunker(barricade.transform, new Vector3(-1.8f, 0f, -2.5f), 45f, sandbagMat);
            CreateSandbagBunker(barricade.transform, new Vector3(2.4f, 0f, 2.8f), -135f, sandbagMat);

            // Tactical Floodlight pointing toward road
            Material floodlightHousing = SceneBuilder.GetOrCreateMaterial("Mat_Floodlight_Housing", new Color(0.25f, 0.28f, 0.30f));
            CreateBoxSimple(barricade.transform, "Barricade_Lamp", new Vector3(-3.2f, 1.8f, 0f), new Vector3(0.45f, 0.35f, 0.35f), floodlightHousing);

            GameObject blLight = new GameObject("Barricade_SpotLight");
            blLight.transform.SetParent(barricade.transform);
            blLight.transform.localPosition = new Vector3(-3.2f, 1.8f, 0f);
            blLight.transform.localRotation = Quaternion.Euler(25f, -80f, 0f);
            Light spot = blLight.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 65f;
            spot.range = 18f;
            spot.intensity = 3.0f;
            spot.color = new Color(0.95f, 0.95f, 1.0f);
            spot.shadows = LightShadows.None;
        }

        private static void CreateStreetLamp(Transform parent, Vector3 pos, float yRot, Material poleMat, Material bulbMat, bool flickering)
        {
            GameObject lamp = new GameObject("StreetLamp_Pole");
            lamp.transform.SetParent(parent);
            lamp.transform.position = pos;
            lamp.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lamp.transform);
            pole.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            pole.transform.localScale = new Vector3(0.18f, 2.6f, 0.18f);
            if (poleMat != null) pole.GetComponent<Renderer>().sharedMaterial = poleMat;
            GameObjectUtility.SetStaticEditorFlags(pole, StaticEditorFlags.NavigationStatic);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Lamp_Arm";
            arm.transform.SetParent(lamp.transform);
            arm.transform.localPosition = new Vector3(0.55f, 5.5f, 0f);
            arm.transform.localScale = new Vector3(1.3f, 0.14f, 0.14f);
            if (poleMat != null) arm.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Lamp_Head";
            head.transform.SetParent(lamp.transform);
            head.transform.localPosition = new Vector3(1.15f, 5.35f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.18f, 0.35f);
            if (poleMat != null) head.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bulb.name = "Bulb";
            bulb.transform.SetParent(head.transform);
            bulb.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            bulb.transform.localScale = new Vector3(0.75f, 0.22f, 0.75f);
            if (bulbMat != null) bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;
            Collider bc = bulb.GetComponent<Collider>();
            if (bc != null) UnityEngine.Object.DestroyImmediate(bc);

            GameObject lightObj = new GameObject("SpotLight_WarmAmber");
            lightObj.transform.SetParent(bulb.transform);
            lightObj.transform.localPosition = Vector3.zero;
            lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 65f;
            spot.innerSpotAngle = 35f;
            spot.range = 14f;
            spot.intensity = 2.8f;
            spot.color = new Color(1.0f, 0.718f, 0.012f);
            spot.shadows = LightShadows.None;

            if (flickering)
            {
                FlickeringLight fl = lightObj.AddComponent<FlickeringLight>();
                SerializedObject soFl = new SerializedObject(fl);
                soFl.FindProperty("baseIntensity").floatValue = 2.8f;
                soFl.FindProperty("flickerAmount").floatValue = 0.65f;
                soFl.FindProperty("flickerSpeed").floatValue = 8.5f;
                soFl.ApplyModifiedProperties();
            }
        }

        private static void CreateBusStop(Transform parent, Vector3 pos, float yRot, Material frameMat, Material glassMat, Material woodMat)
        {
            GameObject bs = new GameObject("BusStop_Shelter");
            bs.transform.SetParent(parent);
            bs.transform.position = pos;
            bs.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            CreateBoxSimple(bs.transform, "Shelter_Roof", new Vector3(0f, 2.6f, 0f), new Vector3(4.5f, 0.18f, 2.2f), frameMat);
            CreateBoxSimple(bs.transform, "Glass_Back", new Vector3(0f, 1.3f, 0.95f), new Vector3(4.2f, 2.4f, 0.08f), glassMat);
            CreateBoxSimple(bs.transform, "Shelter_Bench", new Vector3(0f, 0.45f, 0.45f), new Vector3(3.2f, 0.45f, 0.55f), woodMat);
        }

        private static void CreateBench(Transform parent, Vector3 pos, float yRot, Material woodMat)
        {
            GameObject bench = new GameObject("Street_Bench");
            bench.transform.SetParent(parent);
            bench.transform.position = pos;
            bench.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            CreateBoxSimple(bench.transform, "Bench_Seat", new Vector3(0f, 0.45f, 0f), new Vector3(2.1f, 0.12f, 0.55f), woodMat);
            CreateBoxSimple(bench.transform, "Bench_Back", new Vector3(0f, 0.85f, -0.24f), new Vector3(2.1f, 0.55f, 0.10f), woodMat);
        }

        private static void CreateFireBarrel(Transform parent, string name, Vector3 pos, Material metalMat)
        {
            GameObject barrel = new GameObject(name);
            barrel.transform.SetParent(parent);
            barrel.transform.position = pos;

            GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Steel_Drum";
            drum.transform.SetParent(barrel.transform);
            drum.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            drum.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
            if (metalMat != null) drum.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(drum, StaticEditorFlags.NavigationStatic);

            GameObject lightObj = new GameObject("FireLight_Warm");
            lightObj.transform.SetParent(barrel.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            Light fLight = lightObj.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(1.0f, 0.45f, 0.08f);
            fLight.range = 7.5f;
            fLight.intensity = 2.4f;
            fLight.shadows = LightShadows.None;

            FlickeringLight fl = lightObj.AddComponent<FlickeringLight>();
            SerializedObject so = new SerializedObject(fl);
            so.FindProperty("baseIntensity").floatValue = 2.4f;
            so.FindProperty("flickerAmount").floatValue = 0.8f;
            so.FindProperty("flickerSpeed").floatValue = 12f;
            so.ApplyModifiedProperties();
        }

        private static void CreateCitySkyline(Transform parent, Material skylineMat, Material litMat)
        {
            GameObject skylineParent = new GameObject("--- DISTANT CITY SKYLINE ---");
            skylineParent.transform.SetParent(parent);
            System.Random rnd = new System.Random(42);

            Material winGlow = SceneBuilder.GetOrCreateMaterial("Mat_Skyline_LitWin", new Color(1.0f, 0.85f, 0.45f));
            winGlow.EnableKeyword("_EMISSION");
            winGlow.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.45f) * 1.5f);

            for (float x = -130f; x <= 130f; x += 18f)
            {
                float z = 135f + (float)(rnd.NextDouble() * 25f);
                float h = 45f + (float)(rnd.NextDouble() * 55f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(16f, h, 16f), skylineMat, winGlow, rnd);

                float zS = -135f - (float)(rnd.NextDouble() * 25f);
                float hS = 45f + (float)(rnd.NextDouble() * 55f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, zS), new Vector3(16f, hS, 16f), skylineMat, winGlow, rnd);
            }

            for (float z = -120f; z <= 120f; z += 18f)
            {
                float x = 135f + (float)(rnd.NextDouble() * 25f);
                float h = 45f + (float)(rnd.NextDouble() * 55f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(16f, h, 16f), skylineMat, winGlow, rnd);

                float xW = -135f - (float)(rnd.NextDouble() * 25f);
                float hW = 45f + (float)(rnd.NextDouble() * 55f);
                CreateSkylineTower(skylineParent.transform, new Vector3(xW, 0f, z), new Vector3(16f, hW, 16f), skylineMat, winGlow, rnd);
            }
        }

        private static void CreateSkylineTower(Transform parent, Vector3 basePos, Vector3 size, Material mat, Material litMat, System.Random rnd)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Skyline_Tower";
            tower.transform.SetParent(parent);
            tower.transform.position = new Vector3(basePos.x, size.y * 0.5f, basePos.z);
            tower.transform.localScale = size;
            if (mat != null) tower.GetComponent<Renderer>().sharedMaterial = mat;
            Collider c = tower.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            // Add 1 or 2 small clusters of illuminated windows on each distant tower
            if (rnd.NextDouble() < 0.65)
            {
                float wy = size.y * (0.3f + (float)(rnd.NextDouble() * 0.5f));
                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "Skyline_WindowCluster";
                win.transform.SetParent(tower.transform);
                win.transform.localPosition = new Vector3(0f, (wy / size.y) - 0.5f, -0.52f);
                win.transform.localScale = new Vector3(0.65f, 0.12f, 0.05f);
                if (litMat != null) win.GetComponent<Renderer>().sharedMaterial = litMat;
                Collider wc = win.GetComponent<Collider>();
                if (wc != null) UnityEngine.Object.DestroyImmediate(wc);
            }
        }

        private static void CreateBoxSimple(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.localPosition = localPos;
            box.transform.localScale = localScale;
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.NavigationStatic);
        }

        private static void CreatePartSimple(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject p = GameObject.CreatePrimitive(type);
            p.name = name;
            p.transform.SetParent(parent);
            p.transform.localPosition = localPos;
            p.transform.localScale = localScale;
            if (mat != null) p.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(p, StaticEditorFlags.NavigationStatic);
        }
    }
}
