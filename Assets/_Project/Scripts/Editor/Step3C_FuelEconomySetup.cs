using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.Vehicles;

namespace ProjectSurvival.EditorTools
{
    public static class Step3C_FuelEconomySetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Setup Step 3C Fuel Economy", false, 65)]
        public static void SetupMenuItem()
        {
            SetupStep3C();
        }

        public static void SetupStep3C()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> EXECUTING STEP 3C SETUP: GAS STATIONS, WRECKS & JERRYCANS <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step3C_Setup] Could not open scene at {ScenePath}");
                return;
            }

            // Load PBR Materials
            Material matRust = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_RustMetal.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Vehicle_Rust.mat");
            Material matConcrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Concrete_Spalling.mat");
            Material matDrumRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Fuel_Drum_Red.mat");
            Material matShadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_ContactShadow.mat");
            Material matTire = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Vehicle_Tire.mat");
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Glass_DustyReflective.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Vehicle_Glass.mat");

            GameObject carWreckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/TripoGenerated/Car_Wreck.glb");

            // Guarantee Root Group: --- PHASE 3C FUEL ECONOMY ---
            GameObject rootGroup = GameObject.Find("--- PHASE 3C FUEL ECONOMY ---");
            if (rootGroup != null)
            {
                UnityEngine.Object.DestroyImmediate(rootGroup);
            }
            rootGroup = new GameObject("--- PHASE 3C FUEL ECONOMY ---");

            GameObject stationsGroup = new GameObject("Gas_Stations_Pumps");
            stationsGroup.transform.SetParent(rootGroup.transform, false);

            GameObject wrecksGroup = new GameObject("Wrecked_Cars_Siphons");
            wrecksGroup.transform.SetParent(rootGroup.transform, false);

            GameObject pickupsGroup = new GameObject("Starter_Jerrycan_Pickups");
            pickupsGroup.transform.SetParent(rootGroup.transform, false);

            // =========================================================================
            // 1. CONSTRUCT 4 ROADSIDE GAS STATION / FUEL DEPOT LANDMARKS
            // =========================================================================
            var stations = new[]
            {
                new { id = "station_south_boulevard", name = "Güney Bulvarı Benzin İstasyonu", pos = new Vector3(15f, 0f, -210f), liters = 65f, max = 100f, sector = "Sector_0_-1_South" },
                new { id = "station_north_highway", name = "Kuzey Otoyolu Dinlenme İstasyonu", pos = new Vector3(20f, 0f, 215f), liters = 75f, max = 100f, sector = "Sector_0_1_North" },
                new { id = "station_east_railyard", name = "Doğu Tren Garı Dizel Deposu", pos = new Vector3(215f, 0f, 20f), liters = 80f, max = 120f, sector = "Sector_1_0_East" },
                new { id = "station_southwest_suburb", name = "Güneybatı Sular Altında Köşe Pompası", pos = new Vector3(-215f, 0f, -215f), liters = 45f, max = 80f, sector = "Sector_-1_-1_SouthWest" }
            };

            for (int i = 0; i < stations.Length; i++)
            {
                var s = stations[i];
                GameObject stObj = new GameObject($"GasStation_{s.id}");
                stObj.transform.SetParent(stationsGroup.transform, false);
                stObj.transform.position = s.pos;

                // Concrete Island Platform
                GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cube);
                island.name = "Station_Concrete_Island";
                island.transform.SetParent(stObj.transform, false);
                island.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                island.transform.localScale = new Vector3(4.2f, 0.24f, 8.5f);
                if (matConcrete != null) island.GetComponent<Renderer>().sharedMaterial = matConcrete;
                UnityEngine.Object.DestroyImmediate(island.GetComponent<Collider>());

                // Canopy Roof
                GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canopy.name = "Station_Canopy_Roof";
                canopy.transform.SetParent(stObj.transform, false);
                canopy.transform.localPosition = new Vector3(0f, 4.4f, 0f);
                canopy.transform.localScale = new Vector3(8.0f, 0.45f, 12.0f);
                if (matConcrete != null) canopy.GetComponent<Renderer>().sharedMaterial = matConcrete;
                UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());

                // 4 Canopy Pillars
                Vector3[] pillarOffsets = new Vector3[]
                {
                    new Vector3(-1.8f, 2.2f, -3.2f),
                    new Vector3(1.8f, 2.2f, -3.2f),
                    new Vector3(-1.8f, 2.2f, 3.2f),
                    new Vector3(1.8f, 2.2f, 3.2f)
                };
                for (int p = 0; p < pillarOffsets.Length; p++)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.name = $"Canopy_Pillar_{p + 1}";
                    pillar.transform.SetParent(stObj.transform, false);
                    pillar.transform.localPosition = pillarOffsets[p];
                    pillar.transform.localScale = new Vector3(0.45f, 2.2f, 0.45f);
                    if (matRust != null) pillar.GetComponent<Renderer>().sharedMaterial = matRust;
                    UnityEngine.Object.DestroyImmediate(pillar.GetComponent<Collider>());
                }

                // Central Fuel Dispenser Pump
                GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pump.name = "Fuel_Dispenser_Housing";
                pump.transform.SetParent(stObj.transform, false);
                pump.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                pump.transform.localScale = new Vector3(0.95f, 2.2f, 1.5f);
                if (matDrumRed != null) pump.GetComponent<Renderer>().sharedMaterial = matDrumRed;
                UnityEngine.Object.DestroyImmediate(pump.GetComponent<Collider>());

                // Digital Display Panel
                GameObject display = GameObject.CreatePrimitive(PrimitiveType.Cube);
                display.name = "Liter_Readout_Display";
                display.transform.SetParent(pump.transform, false);
                display.transform.localPosition = new Vector3(0.52f, 0.15f, 0f);
                display.transform.localScale = new Vector3(0.08f, 0.35f, 0.6f);
                if (matRust != null) display.GetComponent<Renderer>().sharedMaterial = matRust;
                UnityEngine.Object.DestroyImmediate(display.GetComponent<Collider>());

                // Contact Shadow
                if (matShadow != null)
                {
                    GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    shadow.name = "Pump_ContactShadow";
                    shadow.transform.SetParent(stObj.transform, false);
                    shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    shadow.transform.localScale = new Vector3(5.5f, 10f, 1f);
                    shadow.GetComponent<Renderer>().sharedMaterial = matShadow;
                    UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());
                }

                // Indicator Light (Shadows: None!)
                GameObject lightObj = new GameObject("Pump_Indicator_Light");
                lightObj.transform.SetParent(pump.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                Light pLight = lightObj.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.range = 7.5f;
                pLight.intensity = 1.8f;
                pLight.color = new Color(0.2f, 0.95f, 0.35f);
                pLight.shadows = LightShadows.None; // 60 FPS mobile limit compliance

                // Collider on Root
                BoxCollider col = stObj.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 1.5f, 0f);
                col.size = new Vector3(2.5f, 3.0f, 3.5f);

                // AudioSource on Root (3D positional)
                AudioSource aSrc = stObj.AddComponent<AudioSource>();
                aSrc.spatialBlend = 1.0f;
                aSrc.minDistance = 1.5f;
                aSrc.maxDistance = 22.0f;
                aSrc.rolloffMode = AudioRolloffMode.Logarithmic;
                aSrc.playOnAwake = false;

                // NavMeshObstacle with carving
                NavMeshObstacle navObs = stObj.AddComponent<NavMeshObstacle>();
                navObs.shape = NavMeshObstacleShape.Box;
                navObs.center = new Vector3(0f, 1.2f, 0f);
                navObs.size = new Vector3(3.2f, 2.5f, 4.0f);
                navObs.carving = true;
                navObs.carveOnlyStationary = false;

                // GasStationPumpInteractable component
                GasStationPumpInteractable pumpComp = stObj.AddComponent<GasStationPumpInteractable>();
                pumpComp.ConfigureStation(s.id, s.name, s.liters, s.max);

                // Wire serialized fields via SerializedObject
                SerializedObject so = new SerializedObject(pumpComp);
                so.FindProperty("pumpAudioSource").objectReferenceValue = aSrc;
                so.FindProperty("pumpIndicatorLight").objectReferenceValue = pLight;
                so.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[Step3C_Setup] Created Gas Station: {s.name} at {s.pos} ({s.liters}L)");
            }

            // =========================================================================
            // 2. CONSTRUCT 10 ABANDONED WRECKED CAR FUEL SIPHON POINTS
            // =========================================================================
            var wrecks = new[]
            {
                new { id = "wreck_north_01", pos = new Vector3(25f, 0f, 180f), rot = 18f, liters = 14f, sector = "Sector_0_1_North" },
                new { id = "wreck_north_02", pos = new Vector3(-30f, 0f, 260f), rot = -35f, liters = 16f, sector = "Sector_0_1_North" },
                new { id = "wreck_east_01", pos = new Vector3(185f, 0f, 45f), rot = 75f, liters = 12f, sector = "Sector_1_0_East" },
                new { id = "wreck_east_02", pos = new Vector3(260f, 0f, -30f), rot = 110f, liters = 18f, sector = "Sector_1_0_East" },
                new { id = "wreck_south_01", pos = new Vector3(-25f, 0f, -175f), rot = -15f, liters = 10f, sector = "Sector_0_-1_South" },
                new { id = "wreck_south_02", pos = new Vector3(35f, 0f, -260f), rot = 42f, liters = 15f, sector = "Sector_0_-1_South" },
                new { id = "wreck_southwest_01", pos = new Vector3(-185f, 0f, -190f), rot = -65f, liters = 11f, sector = "Sector_-1_-1_SouthWest" },
                new { id = "wreck_southwest_02", pos = new Vector3(-260f, 0f, -250f), rot = 125f, liters = 9f, sector = "Sector_-1_-1_SouthWest" },
                new { id = "wreck_west_01", pos = new Vector3(-170f, 0f, 25f), rot = 55f, liters = 13f, sector = "Sector_-1_0_West" },
                new { id = "wreck_northwest_01", pos = new Vector3(-195f, 0f, 210f), rot = -80f, liters = 17f, sector = "Sector_-1_1_NorthWest" }
            };

            for (int i = 0; i < wrecks.Length; i++)
            {
                var w = wrecks[i];
                GameObject wreckObj = new GameObject($"WreckedCar_{w.id}");
                wreckObj.transform.SetParent(wrecksGroup.transform, false);
                wreckObj.transform.position = w.pos;
                wreckObj.transform.rotation = Quaternion.Euler(0f, w.rot, 0f);

                // Visual Representation
                if (carWreckPrefab != null)
                {
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(carWreckPrefab, wreckObj.transform);
                    model.name = "Visual_Model";
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one * 0.95f;
                }
                else
                {
                    // Procedural sculpted wreck body fallback
                    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "Visual_Body";
                    body.transform.SetParent(wreckObj.transform, false);
                    body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                    body.transform.localScale = new Vector3(1.85f, 1.15f, 4.2f);
                    if (matRust != null) body.GetComponent<Renderer>().sharedMaterial = matRust;
                    UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

                    GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cabin.name = "Visual_Cabin";
                    cabin.transform.SetParent(wreckObj.transform, false);
                    cabin.transform.localPosition = new Vector3(0f, 1.35f, -0.3f);
                    cabin.transform.localScale = new Vector3(1.6f, 0.85f, 2.2f);
                    if (matGlass != null) cabin.GetComponent<Renderer>().sharedMaterial = matGlass;
                    UnityEngine.Object.DestroyImmediate(cabin.GetComponent<Collider>());
                }

                // Contact Shadow
                if (matShadow != null)
                {
                    GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    shadow.name = "Car_ContactShadow";
                    shadow.transform.SetParent(wreckObj.transform, false);
                    shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    shadow.transform.localScale = new Vector3(2.4f, 4.8f, 1f);
                    shadow.GetComponent<Renderer>().sharedMaterial = matShadow;
                    UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());
                }

                // Collider
                BoxCollider col = wreckObj.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.85f, 0f);
                col.size = new Vector3(2.2f, 1.7f, 4.5f);

                // AudioSource (3D positional)
                AudioSource aSrc = wreckObj.AddComponent<AudioSource>();
                aSrc.spatialBlend = 1.0f;
                aSrc.minDistance = 1.5f;
                aSrc.maxDistance = 22.0f;
                aSrc.rolloffMode = AudioRolloffMode.Logarithmic;
                aSrc.playOnAwake = false;

                // NavMeshObstacle with carving
                NavMeshObstacle navObs = wreckObj.AddComponent<NavMeshObstacle>();
                navObs.shape = NavMeshObstacleShape.Box;
                navObs.center = new Vector3(0f, 0.75f, 0f);
                navObs.size = new Vector3(2.4f, 1.6f, 4.6f);
                navObs.carving = true;
                navObs.carveOnlyStationary = false;

                // WreckedCarSiphonInteractable component
                WreckedCarSiphonInteractable siphonComp = wreckObj.AddComponent<WreckedCarSiphonInteractable>();
                siphonComp.ConfigureWreck(w.id, w.liters);

                SerializedObject so = new SerializedObject(siphonComp);
                so.FindProperty("audioSource").objectReferenceValue = aSrc;
                so.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[Step3C_Setup] Created Wrecked Car: {w.id} at {w.pos} ({w.liters}L)");
            }

            // =========================================================================
            // 3. SCATTER 6 STARTER BOŞ YAKIT BİDONU (item_empty_jerrycan) PICKUPS
            // =========================================================================
            var pickups = new[]
            {
                new { id = "jerrycan_safehouse_garage", name = "Boş Yakıt Bidonu (Sığınak Garajı)", pos = new Vector3(14f, 0.25f, 8f) },
                new { id = "jerrycan_south_station_office", name = "Boş Yakıt Bidonu (Güney İstasyon Ofisi)", pos = new Vector3(18f, 0.25f, -214f) },
                new { id = "jerrycan_north_highway_rest", name = "Boş Yakıt Bidonu (Kuzey Dinlenme Tesisi)", pos = new Vector3(23f, 0.25f, 218f) },
                new { id = "jerrycan_east_warehouse_a", name = "Boş Yakıt Bidonu (Doğu Depo A)", pos = new Vector3(210f, 0.25f, 15f) },
                new { id = "jerrycan_east_warehouse_b", name = "Boş Yakıt Bidonu (Doğu Depo B)", pos = new Vector3(230f, 0.25f, -20f) },
                new { id = "jerrycan_se_substation_shed", name = "Boş Yakıt Bidonu (Güneydoğu Trafo Binası)", pos = new Vector3(235f, 0.25f, -235f) }
            };

            for (int i = 0; i < pickups.Length; i++)
            {
                var p = pickups[i];
                GameObject pickObj = new GameObject($"Pickup_{p.id}");
                pickObj.transform.SetParent(pickupsGroup.transform, false);
                pickObj.transform.position = p.pos;

                // Jerrycan 3D model (Canister Body + Handle + Spout)
                GameObject canBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canBody.name = "Jerrycan_Canister_Body";
                canBody.transform.SetParent(pickObj.transform, false);
                canBody.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                canBody.transform.localScale = new Vector3(0.25f, 0.44f, 0.38f);
                if (matDrumRed != null) canBody.GetComponent<Renderer>().sharedMaterial = matDrumRed;
                UnityEngine.Object.DestroyImmediate(canBody.GetComponent<Collider>());

                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = "Jerrycan_Handle";
                handle.transform.SetParent(canBody.transform, false);
                handle.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                handle.transform.localScale = new Vector3(0.2f, 0.16f, 0.6f);
                if (matRust != null) handle.GetComponent<Renderer>().sharedMaterial = matRust;
                UnityEngine.Object.DestroyImmediate(handle.GetComponent<Collider>());

                // Contact Shadow
                if (matShadow != null)
                {
                    GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    shadow.name = "Jerrycan_Shadow";
                    shadow.transform.SetParent(pickObj.transform, false);
                    shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    shadow.transform.localScale = new Vector3(0.55f, 0.65f, 1f);
                    shadow.GetComponent<Renderer>().sharedMaterial = matShadow;
                    UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());
                }

                // Trigger Sphere Collider for pickup
                SphereCollider sc = pickObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.35f;

                // JerrycanPickupInteractable component
                JerrycanPickupInteractable pickComp = pickObj.AddComponent<JerrycanPickupInteractable>();
                pickComp.ConfigurePickup(p.id, p.name);

                Debug.Log($"[Step3C_Setup] Placed Jerrycan Pickup: {p.id} at {p.pos}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Step3C_Setup] Successfully created all 4 Gas Stations, 10 Wrecked Cars, and 6 Jerrycan Pickups!");
        }
    }
}
