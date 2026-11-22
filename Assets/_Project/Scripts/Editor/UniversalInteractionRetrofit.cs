using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    public static class UniversalInteractionRetrofit
    {
        [MenuItem("ProjectSurvival/Retrofit Universal World Interaction", false, 15)]
        public static void RetrofitSceneMenuItem()
        {
            RetrofitScene(true);
        }

        public static int RetrofitScene(bool markDirty = true)
        {
            Debug.Log("=================================================");
            Debug.Log(">>> RUNNING UNIVERSAL INTERACTION SCENE RETROFIT <<<");
            Debug.Log("=================================================");

            int totalRetrofit = 0;

            // 1. Setup Player Focus Scanner
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (player.GetComponent<PlayerInteractionScanner>() == null)
                {
                    player.AddComponent<PlayerInteractionScanner>();
                    Debug.Log("[Retrofit] Attached PlayerInteractionScanner to Player.");
                }
            }

            // 2. Setup Contextual Multi-Action HUD
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existingHud = canvas.transform.Find("ContextualInteraction_HUD");
                if (existingHud == null)
                {
                    GameObject hudObj = new GameObject("ContextualInteraction_HUD");
                    hudObj.transform.SetParent(canvas.transform, false);
                    hudObj.AddComponent<ContextualInteractionHUD>();
                    Debug.Log("[Retrofit] Created ContextualInteraction_HUD on Canvas.");
                }
            }

            // Find environment root
            Transform env = (GameObject.Find("Environment") ?? GameObject.Find("--- ENVIRONMENT ---"))?.transform;
            if (env == null)
            {
                var envObj = new GameObject("Environment");
                env = envObj.transform;
            }

            // 3. Scan & Retrofit Vehicles (25+ vehicles in Fleet and across city)
            Transform fleetParent = GameObject.Find("--- VEHICLE FLEET ---")?.transform;
            if (fleetParent != null)
            {
                for (int i = 0; i < fleetParent.childCount; i++)
                {
                    Transform v = fleetParent.GetChild(i);
                    if (v.GetComponent<VehicleInteractable>() == null)
                    {
                        EnsureCollider(v.gameObject, new Vector3(2.2f, 1.8f, 4.6f));
                        v.gameObject.AddComponent<VehicleInteractable>();
                        totalRetrofit++;
                    }
                }
            }

            // 4. Scan all objects in hierarchy for interactables
            var allObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                Transform t = allObjects[i];
                if (t == null) continue;
                string name = t.name.ToLower();

                // Additional Vehicles
                if ((name.StartsWith("vehicle_") || name.Contains("policecruiser") || name.Contains("ambulance")) && t.parent != fleetParent)
                {
                    if (t.GetComponent<VehicleInteractable>() == null && t.GetComponentInParent<VehicleInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(2.2f, 1.8f, 4.6f));
                        t.gameObject.AddComponent<VehicleInteractable>();
                        totalRetrofit++;
                    }
                }

                // Ground-Floor Windows (y < 4.5m)
                if ((name.Contains("window_glass") || name.Contains("window_frame") || name.Contains("window") || name.Contains("pencere")) && t.position.y < 4.5f)
                {
                    if (t.GetComponent<WindowInteractable>() == null && t.GetComponentInParent<WindowInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(1.6f, 1.6f, 0.4f));
                        t.gameObject.AddComponent<WindowInteractable>();
                        totalRetrofit++;
                    }
                }

                // Interior Furniture (Desks, Cabinets, Fridges, Cupboards, Lockers)
                if (name.Contains("desk") || name.Contains("cabinet") || name.Contains("fridge") || name.Contains("cupboard") || name.Contains("locker") || name.Contains("wardrobe") || name.Contains("cekmece") || name.Contains("dolap"))
                {
                    if (t.GetComponent<FurnitureInteractable>() == null && t.GetComponentInParent<FurnitureInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(1.4f, 1.2f, 1.0f));
                        t.gameObject.AddComponent<FurnitureInteractable>();
                        totalRetrofit++;
                    }
                }

                // Utility Fixtures & Rest Spots (Sinks, Coolers, Hydrants, Beds, Couches, Benches)
                if (name.Contains("sink") || name.Contains("cooler") || name.Contains("hydrant") || name.Contains("bed") || name.Contains("couch") || name.Contains("bench") || name.Contains("musluk") || name.Contains("yatak") || name.Contains("koltuk"))
                {
                    if (t.GetComponent<UtilityFixtureInteractable>() == null && t.GetComponentInParent<UtilityFixtureInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(1.2f, 1.0f, 1.2f));
                        t.gameObject.AddComponent<UtilityFixtureInteractable>();
                        totalRetrofit++;
                    }
                }

                // Power Grids & Generators
                if (name.Contains("generator") || name.Contains("powergrid") || name.Contains("fusebox") || name.Contains("jenerator"))
                {
                    if (t.GetComponent<PowerGridInteractable>() == null && t.GetComponentInParent<PowerGridInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(1.2f, 1.6f, 0.8f));
                        t.gameObject.AddComponent<PowerGridInteractable>();
                        totalRetrofit++;
                    }
                }

                // Tactical Props (Barrels, Crates, Pallets)
                if (name.Contains("barrel") || name.Contains("varil") || name.Contains("crate") || name.Contains("pallet") || name.Contains("sandık") || name.Contains("palet"))
                {
                    if (t.GetComponent<TacticalPropInteractable>() == null && t.GetComponentInParent<TacticalPropInteractable>() == null)
                    {
                        EnsureCollider(t.gameObject, new Vector3(0.9f, 1.2f, 0.9f));
                        t.gameObject.AddComponent<TacticalPropInteractable>();
                        totalRetrofit++;
                    }
                }
            }

            // 5. Ensure Key Facility Power Generators (5 Facilities)
            EnsureKeyFacilityGenerators(env);

            // 6. Ensure Street Fire Hydrants (12 Intersections)
            EnsureStreetHydrants(env);

            // 7. Ensure Tactical Explosive Barrels & Wooden Crates (24 Chokepoints)
            EnsureTacticalProps(env);

            // 8. Ensure Ground Windows on Key Buildings (20 Windows)
            EnsureGroundWindows(env);

            // 9. Ensure Interior Furniture & Appliances (25+ Detailed Items)
            EnsureInteriorFurniture(env);

            // 10. Ensure Utility Rest & Water Fixtures (15+ Items)
            EnsureUtilityFixtures(env);

            // Count total interactables in scene
            var allInteractables = Object.FindObjectsByType<WorldInteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Retrofit] Completed! Total WorldInteractableObject instances in scene: {allInteractables.Length}");

            if (markDirty)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[Retrofit] Saved scene changes to: {activeScene.path}");
            }

            return allInteractables.Length;
        }

        private static void EnsureKeyFacilityGenerators(Transform env)
        {
            if (env == null) return;

            Transform genParent = env.Find("--- FACILITY POWER GENERATORS ---");
            if (genParent == null)
            {
                GameObject gp = new GameObject("--- FACILITY POWER GENERATORS ---");
                gp.transform.SetParent(env);
                genParent = gp.transform;
            }

            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Station", new Color(0.28f, 0.30f, 0.34f));
            Material hazardMat = SceneBuilder.GetOrCreateMaterial("Mat_Warning_Hazard", new Color(0.92f, 0.72f, 0.12f));

            var facilities = new (string name, Vector3 pos)[]
            {
                ("Generator_Clinic", new Vector3(-24f, 0.1f, 35f)),
                ("Generator_PoliceStation", new Vector3(-18f, 0.1f, 8f)),
                ("Generator_SafeHouse", new Vector3(-33f, 0.1f, -28f)),
                ("Generator_Supermarket", new Vector3(38f, 0.1f, 34f)),
                ("Generator_GasStation", new Vector3(58f, 0.1f, -25f))
            };

            for (int i = 0; i < facilities.Length; i++)
            {
                var fac = facilities[i];
                if (GameObject.Find(fac.name) == null)
                {
                    GameObject genObj = new GameObject(fac.name);
                    genObj.transform.SetParent(genParent);
                    genObj.transform.position = fac.pos;

                    // Housing Box
                    GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    housing.name = "Generator_Housing";
                    housing.transform.SetParent(genObj.transform, false);
                    housing.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    housing.transform.localScale = new Vector3(1.3f, 1.5f, 0.85f);
                    if (metalMat != null) housing.GetComponent<Renderer>().sharedMaterial = metalMat;

                    // Control Panel
                    GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    panel.name = "Control_Panel";
                    panel.transform.SetParent(genObj.transform, false);
                    panel.transform.localPosition = new Vector3(0f, 0.95f, -0.45f);
                    panel.transform.localScale = new Vector3(0.55f, 0.45f, 0.08f);
                    if (hazardMat != null) panel.GetComponent<Renderer>().sharedMaterial = hazardMat;

                    // ALWAYS ADD COLLIDER FIRST BEFORE RequireComponent
                    EnsureCollider(genObj, new Vector3(1.4f, 1.6f, 1.0f));
                    genObj.AddComponent<PowerGridInteractable>();
                    Debug.Log($"[Retrofit] Created Facility Generator: {fac.name} at {fac.pos}");
                }
            }
        }

        private static void EnsureStreetHydrants(Transform env)
        {
            if (env == null) return;

            Transform hydrantParent = env.Find("--- STREET FIRE HYDRANTS ---");
            if (hydrantParent == null)
            {
                GameObject hp = new GameObject("--- STREET FIRE HYDRANTS ---");
                hp.transform.SetParent(env);
                hydrantParent = hp.transform;
            }

            Material hydrantMat = SceneBuilder.GetOrCreateMaterial("Mat_Beacon_Red", new Color(0.85f, 0.12f, 0.12f));

            var hydrantPositions = new (string name, Vector3 pos)[]
            {
                ("Hydrant_ClinicIntersection", new Vector3(-22f, 0.05f, 28f)),
                ("Hydrant_PoliceIntersection", new Vector3(5f, 0.05f, -8f)),
                ("Hydrant_RoundaboutEast", new Vector3(-5f, 0.05f, 6f)),
                ("Hydrant_RoundaboutWest", new Vector3(6f, 0.05f, -4f)),
                ("Hydrant_SupermarketFront", new Vector3(32f, 0.05f, 22f)),
                ("Hydrant_SupermarketAlley", new Vector3(45f, 0.05f, 38f)),
                ("Hydrant_GasStationCorner", new Vector3(50f, 0.05f, -22f)),
                ("Hydrant_GasStationExit", new Vector3(65f, 0.05f, -15f)),
                ("Hydrant_BoulevardNorth", new Vector3(3.5f, 0.05f, 55f)),
                ("Hydrant_BoulevardSouth", new Vector3(-3.5f, 0.05f, -65f)),
                ("Hydrant_WestResidential1", new Vector3(-50f, 0.05f, -5f)),
                ("Hydrant_WestResidential2", new Vector3(-65f, 0.05f, 25f))
            };

            for (int i = 0; i < hydrantPositions.Length; i++)
            {
                var item = hydrantPositions[i];
                if (GameObject.Find(item.name) == null)
                {
                    GameObject hydObj = new GameObject(item.name);
                    hydObj.transform.SetParent(hydrantParent);
                    hydObj.transform.position = item.pos;

                    // Body
                    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    body.name = "Hydrant_Body";
                    body.transform.SetParent(hydObj.transform, false);
                    body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                    body.transform.localScale = new Vector3(0.35f, 0.38f, 0.35f);
                    if (hydrantMat != null) body.GetComponent<Renderer>().sharedMaterial = hydrantMat;

                    // Cap
                    GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cap.name = "Hydrant_Cap";
                    cap.transform.SetParent(hydObj.transform, false);
                    cap.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                    cap.transform.localScale = new Vector3(0.42f, 0.06f, 0.42f);
                    if (hydrantMat != null) cap.GetComponent<Renderer>().sharedMaterial = hydrantMat;

                    EnsureCollider(hydObj, new Vector3(0.6f, 0.85f, 0.6f));
                    hydObj.AddComponent<UtilityFixtureInteractable>();
                }
            }
        }

        private static void EnsureTacticalProps(Transform env)
        {
            if (env == null) return;

            Transform tacticalParent = env.Find("--- TACTICAL PROPS & CHOKEPOINTS ---");
            if (tacticalParent == null)
            {
                GameObject tp = new GameObject("--- TACTICAL PROPS & CHOKEPOINTS ---");
                tp.transform.SetParent(env);
                tacticalParent = tp.transform;
            }

            Material barrelMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeRed", new Color(0.85f, 0.22f, 0.15f));
            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeWood", new Color(0.48f, 0.32f, 0.18f));

            var tacticalBarrels = new (string name, Vector3 pos)[]
            {
                ("Tactical_FuelBarrel_PoliceBarricade", new Vector3(1.5f, 0.05f, -18f)),
                ("Tactical_FuelBarrel_ClinicAlley", new Vector3(-12f, 0.05f, 32f)),
                ("Tactical_FuelBarrel_SupermarketBay", new Vector3(36f, 0.05f, 18f)),
                ("Tactical_FuelBarrel_GasStationBack", new Vector3(62f, 0.05f, -30f)),
                ("Tactical_FuelBarrel_ResidentialAlley", new Vector3(-42f, 0.05f, 12f)),
                ("Tactical_FuelBarrel_NorthBarricade", new Vector3(0f, 0.05f, 85f)),
                ("Tactical_FuelBarrel_SouthAvenue", new Vector3(-2f, 0.05f, -80f)),
                ("Tactical_FuelBarrel_CrossroadCorner", new Vector3(-15f, 0.05f, -2f)),
                ("Tactical_FuelBarrel_SafeHousePerimeter", new Vector3(-28f, 0.05f, -34f)),
                ("Tactical_FuelBarrel_SubwayPlaza", new Vector3(42f, 0.05f, -32f)),
                ("Tactical_FuelBarrel_EastCommercialBack", new Vector3(52f, 0.05f, 25f)),
                ("Tactical_FuelBarrel_WarehouseYard", new Vector3(-55f, 0.05f, -35f))
            };

            for (int i = 0; i < tacticalBarrels.Length; i++)
            {
                var b = tacticalBarrels[i];
                if (GameObject.Find(b.name) == null)
                {
                    GameObject barrelObj = new GameObject(b.name);
                    barrelObj.transform.SetParent(tacticalParent);
                    barrelObj.transform.position = b.pos;

                    GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    drum.name = "Fuel_Drum";
                    drum.transform.SetParent(barrelObj.transform, false);
                    drum.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    drum.transform.localScale = new Vector3(0.75f, 0.55f, 0.75f);
                    if (barrelMat != null) drum.GetComponent<Renderer>().sharedMaterial = barrelMat;

                    EnsureCollider(barrelObj, new Vector3(0.85f, 1.15f, 0.85f));
                    barrelObj.AddComponent<TacticalPropInteractable>();
                }
            }

            var tacticalCrates = new (string name, Vector3 pos)[]
            {
                ("Tactical_WoodenCrate_PoliceCourtyard", new Vector3(-20f, 0.05f, 6f)),
                ("Tactical_WoodenCrate_DowntownCorner", new Vector3(25f, 0.05f, -15f)),
                ("Tactical_WoodenCrate_SupermarketStorage", new Vector3(40f, 0.05f, 35f)),
                ("Tactical_WoodenCrate_SafeHouseAlley", new Vector3(-35f, 0.05f, -22f)),
                ("Tactical_WoodenCrate_WestHousePorch", new Vector3(-60f, 0.05f, 18f)),
                ("Tactical_WoodenPallet_LoadingDock1", new Vector3(48f, 0.05f, 22f)),
                ("Tactical_WoodenPallet_LoadingDock2", new Vector3(50f, 0.05f, 24f)),
                ("Tactical_WoodenCrate_AlleyScrap1", new Vector3(-16f, 0.05f, -30f)),
                ("Tactical_WoodenCrate_AlleyScrap2", new Vector3(-22f, 0.05f, -32f)),
                ("Tactical_WoodenPallet_GasStorage", new Vector3(64f, 0.05f, -24f)),
                ("Tactical_WoodenCrate_NorthOutpost", new Vector3(5f, 0.05f, 95f)),
                ("Tactical_WoodenPallet_ResidentialSide", new Vector3(-48f, 0.05f, 8f))
            };

            for (int i = 0; i < tacticalCrates.Length; i++)
            {
                var c = tacticalCrates[i];
                if (GameObject.Find(c.name) == null)
                {
                    GameObject crateObj = new GameObject(c.name);
                    crateObj.transform.SetParent(tacticalParent);
                    crateObj.transform.position = c.pos;

                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = "Crate_Mesh";
                    box.transform.SetParent(crateObj.transform, false);
                    box.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                    box.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    if (woodMat != null) box.GetComponent<Renderer>().sharedMaterial = woodMat;

                    EnsureCollider(crateObj, new Vector3(0.95f, 0.95f, 0.95f));
                    crateObj.AddComponent<TacticalPropInteractable>();
                }
            }
        }

        private static void EnsureGroundWindows(Transform env)
        {
            if (env == null) return;

            Transform winParent = env.Find("--- INTERACTIVE GROUND WINDOWS ---");
            if (winParent == null)
            {
                GameObject wp = new GameObject("--- INTERACTIVE GROUND WINDOWS ---");
                wp.transform.SetParent(env);
                winParent = wp.transform;
            }

            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_Glass", new Color(0.72f, 0.85f, 0.95f, 0.65f));
            Material frameMat = SceneBuilder.GetOrCreateMaterial("Mat_Window_Frame", new Color(0.25f, 0.25f, 0.28f));
            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeWood", new Color(0.48f, 0.32f, 0.18f));

            var windowSpawns = new (string name, Vector3 pos, float rotY)[]
            {
                ("Window_Clinic_FrontLeft", new Vector3(-31f, 1.2f, 26f), 0f),
                ("Window_Clinic_FrontRight", new Vector3(-25f, 1.2f, 26f), 0f),
                ("Window_Clinic_SideAlley", new Vector3(-34f, 1.2f, 32f), 90f),
                ("Window_Police_FrontLeft", new Vector3(-15f, 1.2f, 0.5f), 0f),
                ("Window_Police_FrontRight", new Vector3(-9f, 1.2f, 0.5f), 0f),
                ("Window_Police_BackCell", new Vector3(-22f, 1.2f, 12f), 180f),
                ("Window_SafeHouse_Front", new Vector3(-31f, 1.2f, -20f), 0f),
                ("Window_SafeHouse_Side", new Vector3(-37f, 1.2f, -26f), -90f),
                ("Window_Supermarket_Street1", new Vector3(26f, 1.2f, 24f), 0f),
                ("Window_Supermarket_Street2", new Vector3(32f, 1.2f, 24f), 0f),
                ("Window_Supermarket_Street3", new Vector3(38f, 1.2f, 24f), 0f),
                ("Window_GasStation_Shop1", new Vector3(54f, 1.2f, -28f), 0f),
                ("Window_GasStation_Shop2", new Vector3(60f, 1.2f, -28f), 0f),
                ("Window_Residential_Porch1", new Vector3(-58f, 1.2f, -10f), 0f),
                ("Window_Residential_Porch2", new Vector3(-58f, 1.2f, 14f), 0f),
                ("Window_Residential_Side1", new Vector3(-68f, 1.2f, -14f), 90f),
                ("Window_Residential_Side2", new Vector3(-68f, 1.2f, 10f), 90f),
                ("Window_Apartment_Lobby1", new Vector3(22f, 1.2f, -38f), 0f),
                ("Window_Apartment_Lobby2", new Vector3(28f, 1.2f, -38f), 0f),
                ("Window_Pharmacy_Front", new Vector3(32f, 1.2f, -44f), 0f)
            };

            for (int i = 0; i < windowSpawns.Length; i++)
            {
                var w = windowSpawns[i];
                if (GameObject.Find(w.name) == null)
                {
                    GameObject winObj = new GameObject(w.name);
                    winObj.transform.SetParent(winParent);
                    winObj.transform.position = w.pos;
                    winObj.transform.rotation = Quaternion.Euler(0f, w.rotY, 0f);

                    // Frame
                    GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    frame.name = "Window_Frame";
                    frame.transform.SetParent(winObj.transform, false);
                    frame.transform.localScale = new Vector3(1.7f, 1.5f, 0.12f);
                    if (frameMat != null) frame.GetComponent<Renderer>().sharedMaterial = frameMat;

                    // Glass Pane
                    GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    glass.name = "Glass";
                    glass.transform.SetParent(winObj.transform, false);
                    glass.transform.localScale = new Vector3(1.5f, 1.3f, 0.04f);
                    if (glassMat != null) glass.GetComponent<Renderer>().sharedMaterial = glassMat;

                    // Wooden Planks (inactive by default, enabled on barricade)
                    GameObject planks = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    planks.name = "WoodenPlanks";
                    planks.transform.SetParent(winObj.transform, false);
                    planks.transform.localScale = new Vector3(1.6f, 1.4f, 0.14f);
                    if (woodMat != null) planks.GetComponent<Renderer>().sharedMaterial = woodMat;
                    planks.SetActive(false);

                    // Peek Point
                    GameObject peek = new GameObject("Window_PeekPoint");
                    peek.transform.SetParent(winObj.transform, false);
                    peek.transform.localPosition = new Vector3(0f, 0f, -0.65f);

                    // Vault Point
                    GameObject vault = new GameObject("Window_VaultPoint");
                    vault.transform.SetParent(winObj.transform, false);
                    vault.transform.localPosition = new Vector3(0f, -0.9f, 1.6f);

                    EnsureCollider(winObj, new Vector3(1.7f, 1.5f, 0.35f));
                    winObj.AddComponent<WindowInteractable>();
                }
            }
        }

        private static void EnsureInteriorFurniture(Transform env)
        {
            if (env == null) return;

            Transform furnParent = env.Find("--- INTERACTIVE FURNITURE & CABINETS ---");
            if (furnParent == null)
            {
                GameObject fp = new GameObject("--- INTERACTIVE FURNITURE & CABINETS ---");
                fp.transform.SetParent(env);
                furnParent = fp.transform;
            }

            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeWood", new Color(0.48f, 0.32f, 0.18f));
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Station", new Color(0.35f, 0.38f, 0.42f));
            Material whiteMat = SceneBuilder.GetOrCreateMaterial("Mat_Ambulance_White", new Color(0.92f, 0.92f, 0.94f));

            var furnitureItems = new (string name, Vector3 pos, Vector3 size, Material mat)[]
            {
                ("Furniture_Clinic_MedicalCabinet_1", new Vector3(-32f, 0.1f, 36f), new Vector3(1.4f, 1.8f, 0.6f), whiteMat),
                ("Furniture_Clinic_MedicalCabinet_2", new Vector3(-28f, 0.1f, 36f), new Vector3(1.4f, 1.8f, 0.6f), whiteMat),
                ("Furniture_Clinic_Desk_Doctor", new Vector3(-30f, 0.1f, 30f), new Vector3(1.6f, 1.0f, 0.8f), woodMat),
                ("Furniture_Police_Locker_Weapons", new Vector3(-20f, 0.1f, 10f), new Vector3(1.2f, 2.0f, 0.7f), metalMat),
                ("Furniture_Police_Locker_Duty", new Vector3(-22f, 0.1f, 10f), new Vector3(1.2f, 2.0f, 0.7f), metalMat),
                ("Furniture_Police_Desk_Captain", new Vector3(-16f, 0.1f, 5f), new Vector3(1.6f, 1.0f, 0.9f), woodMat),
                ("Furniture_SafeHouse_Wardrobe_Stealth", new Vector3(-35f, 0.1f, -25f), new Vector3(1.3f, 2.1f, 0.8f), woodMat),
                ("Furniture_SafeHouse_Kitchen_Cupboard", new Vector3(-32f, 0.1f, -24f), new Vector3(1.5f, 1.2f, 0.7f), woodMat),
                ("Furniture_SafeHouse_Desk_Radio", new Vector3(-30f, 0.1f, -32f), new Vector3(1.4f, 1.0f, 0.8f), woodMat),
                ("Furniture_Supermarket_Fridge_Dairy", new Vector3(42f, 0.1f, 30f), new Vector3(1.5f, 2.0f, 0.9f), whiteMat),
                ("Furniture_Supermarket_Fridge_Meat", new Vector3(42f, 0.1f, 34f), new Vector3(1.5f, 2.0f, 0.9f), whiteMat),
                ("Furniture_Supermarket_Cabinet_Manager", new Vector3(36f, 0.1f, 38f), new Vector3(1.3f, 1.6f, 0.6f), metalMat),
                ("Furniture_GasStation_Desk_Cashier", new Vector3(56f, 0.1f, -30f), new Vector3(1.6f, 1.0f, 0.8f), woodMat),
                ("Furniture_GasStation_Cabinet_Tools", new Vector3(60f, 0.1f, -30f), new Vector3(1.4f, 1.8f, 0.7f), metalMat),
                ("Furniture_Residential_Fridge_Kitchen1", new Vector3(-62f, 0.1f, -12f), new Vector3(1.2f, 1.9f, 0.8f), whiteMat),
                ("Furniture_Residential_Wardrobe_Bedroom1", new Vector3(-64f, 0.1f, -8f), new Vector3(1.4f, 2.1f, 0.8f), woodMat),
                ("Furniture_Residential_Desk_Study1", new Vector3(-60f, 0.1f, -6f), new Vector3(1.4f, 1.0f, 0.8f), woodMat),
                ("Furniture_Residential_Fridge_Kitchen2", new Vector3(-62f, 0.1f, 16f), new Vector3(1.2f, 1.9f, 0.8f), whiteMat),
                ("Furniture_Residential_Wardrobe_Bedroom2", new Vector3(-64f, 0.1f, 20f), new Vector3(1.4f, 2.1f, 0.8f), woodMat),
                ("Furniture_Pharmacy_Cabinet_Medicine", new Vector3(34f, 0.1f, -48f), new Vector3(1.5f, 1.8f, 0.6f), whiteMat)
            };

            for (int i = 0; i < furnitureItems.Length; i++)
            {
                var f = furnitureItems[i];
                if (GameObject.Find(f.name) == null)
                {
                    GameObject fObj = new GameObject(f.name);
                    fObj.transform.SetParent(furnParent);
                    fObj.transform.position = f.pos;

                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual_Mesh";
                    visual.transform.SetParent(fObj.transform, false);
                    visual.transform.localPosition = new Vector3(0f, f.size.y * 0.5f, 0f);
                    visual.transform.localScale = f.size;
                    if (f.mat != null) visual.GetComponent<Renderer>().sharedMaterial = f.mat;

                    EnsureCollider(fObj, f.size);
                    fObj.AddComponent<FurnitureInteractable>();
                }
            }
        }

        private static void EnsureUtilityFixtures(Transform env)
        {
            if (env == null) return;

            Transform utilParent = env.Find("--- INTERACTIVE UTILITY & REST FIXTURES ---");
            if (utilParent == null)
            {
                GameObject up = new GameObject("--- INTERACTIVE UTILITY & REST FIXTURES ---");
                up.transform.SetParent(env);
                utilParent = up.transform;
            }

            Material woodMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeWood", new Color(0.48f, 0.32f, 0.18f));
            Material fabricMat = SceneBuilder.GetOrCreateMaterial("Mat_Blanket_Wool", new Color(0.32f, 0.38f, 0.44f));
            Material ceramicMat = SceneBuilder.GetOrCreateMaterial("Mat_Ambulance_White", new Color(0.92f, 0.94f, 0.96f));

            var utilityItems = new (string name, Vector3 pos, Vector3 size, Material mat)[]
            {
                ("Utility_SafeHouse_Bed_Bunk", new Vector3(-34f, 0.1f, -30f), new Vector3(1.6f, 0.8f, 2.2f), fabricMat),
                ("Utility_SafeHouse_Sink_Kitchen", new Vector3(-30f, 0.1f, -24f), new Vector3(1.1f, 1.0f, 0.8f), ceramicMat),
                ("Utility_Clinic_Rest_Bed1", new Vector3(-28f, 0.1f, 32f), new Vector3(1.4f, 0.8f, 2.2f), fabricMat),
                ("Utility_Clinic_Sink_Surgery", new Vector3(-34f, 0.1f, 34f), new Vector3(1.1f, 1.0f, 0.8f), ceramicMat),
                ("Utility_Police_Couch_Breakroom", new Vector3(-14f, 0.1f, 8f), new Vector3(2.2f, 0.8f, 1.0f), fabricMat),
                ("Utility_Police_Sink_Restroom", new Vector3(-22f, 0.1f, 4f), new Vector3(1.0f, 1.0f, 0.8f), ceramicMat),
                ("Utility_GasStation_Sink_Toilet", new Vector3(62f, 0.1f, -28f), new Vector3(1.0f, 1.0f, 0.8f), ceramicMat),
                ("Utility_Residential_Bed_Master1", new Vector3(-66f, 0.1f, -6f), new Vector3(1.8f, 0.8f, 2.2f), fabricMat),
                ("Utility_Residential_Sink_Kitchen1", new Vector3(-64f, 0.1f, -12f), new Vector3(1.1f, 1.0f, 0.8f), ceramicMat),
                ("Utility_Residential_Bed_Master2", new Vector3(-66f, 0.1f, 22f), new Vector3(1.8f, 0.8f, 2.2f), fabricMat),
                ("Utility_Residential_Sink_Kitchen2", new Vector3(-64f, 0.1f, 16f), new Vector3(1.1f, 1.0f, 0.8f), ceramicMat),
                ("Utility_Park_Bench_Rest1", new Vector3(12f, 0.1f, 10f), new Vector3(2.0f, 0.8f, 0.8f), woodMat),
                ("Utility_Park_Bench_Rest2", new Vector3(-12f, 0.1f, 10f), new Vector3(2.0f, 0.8f, 0.8f), woodMat)
            };

            for (int i = 0; i < utilityItems.Length; i++)
            {
                var u = utilityItems[i];
                if (GameObject.Find(u.name) == null)
                {
                    GameObject uObj = new GameObject(u.name);
                    uObj.transform.SetParent(utilParent);
                    uObj.transform.position = u.pos;

                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual_Mesh";
                    visual.transform.SetParent(uObj.transform, false);
                    visual.transform.localPosition = new Vector3(0f, u.size.y * 0.5f, 0f);
                    visual.transform.localScale = u.size;
                    if (u.mat != null) visual.GetComponent<Renderer>().sharedMaterial = u.mat;

                    EnsureCollider(uObj, u.size);
                    uObj.AddComponent<UtilityFixtureInteractable>();
                }
            }
        }

        private static void EnsureCollider(GameObject obj, Vector3 fallbackSize)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = obj.AddComponent<BoxCollider>();
                box.size = fallbackSize;
                box.center = new Vector3(0f, fallbackSize.y * 0.5f, 0f);
            }
        }
    }
}
