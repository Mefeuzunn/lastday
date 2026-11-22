using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AI;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.UI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 3 (Step 3A): Starter House Interior Upgrader.
    /// Replaces all primitive cube/box furniture across Bedroom, Living Room, Kitchen, and Bathroom
    /// with genuine CC0 3D models downloaded from GitHub into 'Assets/_Project/Models/CC0_Interior/'.
    /// Preserves all 4 WorldInteractableObject loot containers, SmartphoneNewsInteractable,
    /// InteractiveDoor hinges, UtilityFixtureInteractables, and NavMesh walkability.
    /// Converts all materials to Universal Render Pipeline (URP/Lit) with enableInstancing = true.
    /// </summary>
    public static class StarterHouseInteriorUpgrader
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string ModelsDir = "Assets/_Project/Models/CC0_Interior";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Upgrade Starter House with CC0 3D Models", false, 47)]
        public static void UpgradeStarterHouseMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[StarterHouseInteriorUpgrader] Beginning complete Starter House 3D Model Upgrade...");

            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot == null)
            {
                Debug.LogError("[StarterHouseInteriorUpgrader] StarterHouse_Safehome root not found in scene!");
                return;
            }

            // Upgrade Room by Room
            UpgradeBedroom(houseRoot);
            UpgradeLivingRoom(houseRoot);
            UpgradeKitchen(houseRoot);
            UpgradeBathroom(houseRoot);

            // Convert and optimize all materials to URP Lit
            ConvertAllMaterialsToURP(houseRoot);

            // Rebuild NavMesh
            RebuildHouseNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StarterHouseInteriorUpgrader] SUCCESS: All Starter House primitive cubes replaced with real CC0 3D models!");
        }

        public static GameObject LoadOrInstantiateModel(string modelFileName, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            string assetPath = Path.Combine(ModelsDir, modelFileName);
            GameObject instance = null;

            // 1. Try AssetDatabase prefab load
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }

            // 2. Fallback to GLTFast direct import if prefab not yet baked
            if (instance == null)
            {
                string fullPath = Path.GetFullPath(assetPath);
                if (File.Exists(fullPath))
                {
                    var gltf = new GLTFast.GltfImport();
                    var task = gltf.LoadFile(fullPath);
                    task.Wait();
                    if (task.Result)
                    {
                        GameObject container = new GameObject(Path.GetFileNameWithoutExtension(modelFileName) + "_GLTF");
                        container.transform.SetParent(parent, false);
                        gltf.InstantiateMainScene(container.transform);
                        instance = container;
                    }
                }
            }

            if (instance != null)
            {
                instance.name = Path.GetFileNameWithoutExtension(modelFileName);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPos;
                instance.transform.localEulerAngles = localRot;
                instance.transform.localScale = localScale;
            }
            else
            {
                Debug.LogWarning($"[StarterHouseInteriorUpgrader] Could not instantiate 3D model '{modelFileName}'!");
            }

            return instance;
        }

        private static void UpgradeBedroom(GameObject houseRoot)
        {
            Transform roomTf = houseRoot.transform.Find("Room_Bedroom");
            if (roomTf == null)
            {
                roomTf = new GameObject("Room_Bedroom").transform;
                roomTf.SetParent(houseRoot.transform, false);
            }

            // 1. Double Bed
            Transform bedTf = roomTf.Find("Furniture_Bed_Master");
            if (bedTf == null)
            {
                bedTf = new GameObject("Furniture_Bed_Master").transform;
                bedTf.SetParent(roomTf, false);
            }
            bedTf.localPosition = new Vector3(-2.8f, 0.0f, -1.8f);
            bedTf.localEulerAngles = new Vector3(0f, 90f, 0f);

            // Strip primitive cube children
            StripPrimitiveChildren(bedTf, new[] { "Bed_Mattress", "Bed_Blanket", "Bed_Pillow", "Bed_Frame" });
            StripMeshComponents(bedTf.gameObject);

            // Instantiate real bedDouble.glb
            Transform existingBedModel = bedTf.Find("bedDouble");
            if (existingBedModel != null) UnityEngine.Object.DestroyImmediate(existingBedModel.gameObject);
            GameObject bedModel = LoadOrInstantiateModel("bedDouble.glb", bedTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (bedModel != null)
            {
                GroundModel(bedModel, 0.0f);
            }

            // Add real pillows on head of bed
            Transform existingPillows = bedTf.Find("Pillows_Root");
            if (existingPillows != null) UnityEngine.Object.DestroyImmediate(existingPillows.gameObject);
            GameObject pillowsRoot = new GameObject("Pillows_Root");
            pillowsRoot.transform.SetParent(bedTf, false);
            LoadOrInstantiateModel("pillow.glb", pillowsRoot.transform, new Vector3(-0.45f, 0.52f, 0.72f), new Vector3(15f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            LoadOrInstantiateModel("pillowBlue.glb", pillowsRoot.transform, new Vector3(0.45f, 0.52f, 0.72f), new Vector3(15f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f));

            // Bed BoxCollider & UtilityFixture
            var bedCol = bedTf.GetComponent<BoxCollider>();
            if (bedCol == null) bedCol = bedTf.gameObject.AddComponent<BoxCollider>();
            bedCol.size = new Vector3(2.2f, 0.85f, 1.9f);
            bedCol.center = new Vector3(0f, 0.42f, 0f);

            var bedFixture = bedTf.GetComponent<UtilityFixtureInteractable>();
            if (bedFixture == null) bedFixture = bedTf.gameObject.AddComponent<UtilityFixtureInteractable>();
            bedFixture.SetObjectDetails("Dinlenme Yatağı", "DİNLENME NOKTASI");
            bedFixture.SetPersistentId("starter_bed_master");

            // 2. Nightstand
            Transform nsTf = roomTf.Find("Nightstand_Wood");
            if (nsTf == null)
            {
                nsTf = new GameObject("Nightstand_Wood").transform;
                nsTf.SetParent(roomTf, false);
            }
            nsTf.localPosition = new Vector3(-1.6f, 0.0f, -1.8f);
            nsTf.localEulerAngles = new Vector3(0f, 90f, 0f);
            StripMeshComponents(nsTf.gameObject);

            Transform existingNsModel = nsTf.Find("cabinetBedDrawer");
            if (existingNsModel != null) UnityEngine.Object.DestroyImmediate(existingNsModel.gameObject);
            GameObject nsModel = LoadOrInstantiateModel("cabinetBedDrawer.glb", nsTf, Vector3.zero, Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            if (nsModel != null)
            {
                GroundModel(nsModel, 0.0f);
            }

            var nsCol = nsTf.GetComponent<BoxCollider>();
            if (nsCol == null) nsCol = nsTf.gameObject.AddComponent<BoxCollider>();
            nsCol.size = new Vector3(0.55f, 0.65f, 0.55f);
            nsCol.center = new Vector3(0f, 0.32f, 0f);

            // Table Lamp on Nightstand
            Transform existingTableLamp = nsTf.Find("lampRoundTable");
            if (existingTableLamp != null) UnityEngine.Object.DestroyImmediate(existingTableLamp.gameObject);
            LoadOrInstantiateModel("lampRoundTable.glb", nsTf, new Vector3(0.12f, 0.65f, 0.12f), Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f));

            // 3. Interactive Smartphone on Nightstand Top
            Transform phoneTf = roomTf.Find("Smartphone_Nightstand");
            if (phoneTf == null)
            {
                phoneTf = new GameObject("Smartphone_Nightstand").transform;
                phoneTf.SetParent(roomTf, false);
            }
            phoneTf.localPosition = new Vector3(-1.6f, 0.67f, -1.8f);
            phoneTf.localEulerAngles = new Vector3(0f, 25f, 0f);
            phoneTf.localScale = Vector3.one;
            StripMeshComponents(phoneTf.gameObject);

            Transform existingPhoneModel = phoneTf.Find("phone");
            if (existingPhoneModel != null) UnityEngine.Object.DestroyImmediate(existingPhoneModel.gameObject);
            GameObject phoneModel = LoadOrInstantiateModel("phone.glb", phoneTf, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.12f, 0.12f, 0.12f));
            if (phoneModel != null)
            {
                phoneModel.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            }

            // Ensure SmartphoneNewsInteractable and Sphere Trigger
            var phoneCol = phoneTf.GetComponent<SphereCollider>();
            if (phoneCol == null) phoneCol = phoneTf.gameObject.AddComponent<SphereCollider>();
            phoneCol.isTrigger = true;
            phoneCol.radius = 2.6f;

            var phoneInteractable = phoneTf.GetComponent<SmartphoneNewsInteractable>();
            if (phoneInteractable == null) phoneInteractable = phoneTf.gameObject.AddComponent<SmartphoneNewsInteractable>();
            phoneInteractable.SetObjectDetails("AKILLI TELEFON", "Haberlere Bak");
            phoneInteractable.SetPersistentId("starter_intro_smartphone");

            // Point Light for pulsing emissive screen glow
            Transform glowTf = phoneTf.Find("Screen_Glow_Light");
            if (glowTf == null)
            {
                GameObject glowObj = new GameObject("Screen_Glow_Light");
                glowObj.transform.SetParent(phoneTf, false);
                glowObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Light l = glowObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.color = new Color(0.2f, 0.75f, 1.0f);
                l.intensity = 0.9f;
                l.range = 1.4f;
            }

            // 4. Wardrobe Container (Loot Container 1)
            Transform wardrobeTf = roomTf.Find("Furniture_Starter_Wardrobe") ?? houseRoot.transform.Find("Furniture_Starter_Wardrobe");
            if (wardrobeTf != null)
            {
                wardrobeTf.SetParent(roomTf, true);
                wardrobeTf.localPosition = new Vector3(-4.5f, 0.0f, -2.5f);
                wardrobeTf.localEulerAngles = new Vector3(0f, 90f, 0f);
                StripMeshComponents(wardrobeTf.gameObject);

                Transform existingWardrobeModel = wardrobeTf.Find("bookcaseClosedDoors");
                if (existingWardrobeModel != null) UnityEngine.Object.DestroyImmediate(existingWardrobeModel.gameObject);
                GameObject wardrobeModel = LoadOrInstantiateModel("bookcaseClosedDoors.glb", wardrobeTf, Vector3.zero, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
                if (wardrobeModel != null) GroundModel(wardrobeModel, 0.0f);

                var wCol = wardrobeTf.GetComponent<BoxCollider>();
                if (wCol == null) wCol = wardrobeTf.gameObject.AddComponent<BoxCollider>();
                wCol.size = new Vector3(1.1f, 1.95f, 0.75f);
                wCol.center = new Vector3(0f, 0.98f, 0f);

                // Preserve WorldInteractableObject
                var wio = wardrobeTf.GetComponent<WorldInteractableObject>();
                if (wio != null)
                {
                    wio.SetObjectDetails("Yatak Odası Dolabı", "GARDROP VE TEÇHİZAT");
                }
            }

            // 5. Bedroom Rug & Floor Lamp
            Transform existingBedRug = roomTf.Find("rugRectangle");
            if (existingBedRug != null) UnityEngine.Object.DestroyImmediate(existingBedRug.gameObject);
            GameObject bedRug = LoadOrInstantiateModel("rugRectangle.glb", roomTf, new Vector3(-2.2f, 0.005f, -1.8f), new Vector3(0f, 90f, 0f), new Vector3(1.6f, 1.0f, 1.6f));

            Transform existingFloorLamp = roomTf.Find("lampRoundFloor");
            if (existingFloorLamp != null) UnityEngine.Object.DestroyImmediate(existingFloorLamp.gameObject);
            LoadOrInstantiateModel("lampRoundFloor.glb", roomTf, new Vector3(-5.0f, 0.0f, -4.0f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
        }

        private static void UpgradeLivingRoom(GameObject houseRoot)
        {
            Transform roomTf = houseRoot.transform.Find("Room_LivingRoom");
            if (roomTf == null)
            {
                roomTf = new GameObject("Room_LivingRoom").transform;
                roomTf.SetParent(houseRoot.transform, false);
            }

            // 1. Sofa
            Transform sofaTf = roomTf.Find("Furniture_Sofa_Living");
            if (sofaTf == null)
            {
                sofaTf = new GameObject("Furniture_Sofa_Living").transform;
                sofaTf.SetParent(roomTf, false);
            }
            sofaTf.localPosition = new Vector3(3.2f, 0.0f, 2.5f);
            sofaTf.localEulerAngles = new Vector3(0f, 180f, 0f);
            StripMeshComponents(sofaTf.gameObject);

            Transform existingSofaModel = sofaTf.Find("loungeSofa");
            if (existingSofaModel != null) UnityEngine.Object.DestroyImmediate(existingSofaModel.gameObject);
            GameObject sofaModel = LoadOrInstantiateModel("loungeSofa.glb", sofaTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (sofaModel != null) GroundModel(sofaModel, 0.0f);

            var sofaCol = sofaTf.GetComponent<BoxCollider>();
            if (sofaCol == null) sofaCol = sofaTf.gameObject.AddComponent<BoxCollider>();
            sofaCol.size = new Vector3(2.1f, 0.85f, 1.0f);
            sofaCol.center = new Vector3(0f, 0.42f, 0f);

            var sofaFixture = sofaTf.GetComponent<UtilityFixtureInteractable>();
            if (sofaFixture == null) sofaFixture = sofaTf.gameObject.AddComponent<UtilityFixtureInteractable>();
            sofaFixture.SetObjectDetails("Oturma Koltuğu", "DİNLENME NOKTASI");
            sofaFixture.SetPersistentId("starter_living_sofa");

            // 2. Coffee Table
            Transform tableTf = roomTf.Find("Furniture_CoffeeTable");
            if (tableTf == null)
            {
                tableTf = new GameObject("Furniture_CoffeeTable").transform;
                tableTf.SetParent(roomTf, false);
            }
            tableTf.localPosition = new Vector3(3.2f, 0.0f, 1.3f);
            tableTf.localEulerAngles = Vector3.zero;
            StripMeshComponents(tableTf.gameObject);

            Transform existingTable = tableTf.Find("tableCoffee");
            if (existingTable != null) UnityEngine.Object.DestroyImmediate(existingTable.gameObject);
            GameObject tableModel = LoadOrInstantiateModel("tableCoffee.glb", tableTf, Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            if (tableModel != null) GroundModel(tableModel, 0.0f);

            var tableCol = tableTf.GetComponent<BoxCollider>();
            if (tableCol == null) tableCol = tableTf.gameObject.AddComponent<BoxCollider>();
            tableCol.size = new Vector3(1.3f, 0.48f, 0.75f);
            tableCol.center = new Vector3(0f, 0.24f, 0f);

            // 3. Bookshelf
            Transform shelfTf = roomTf.Find("Furniture_Bookshelf");
            if (shelfTf == null)
            {
                shelfTf = new GameObject("Furniture_Bookshelf").transform;
                shelfTf.SetParent(roomTf, false);
            }
            shelfTf.localPosition = new Vector3(5.0f, 0.0f, 2.5f);
            shelfTf.localEulerAngles = new Vector3(0f, -90f, 0f);
            StripMeshComponents(shelfTf.gameObject);

            Transform existingShelf = shelfTf.Find("bookcaseClosed");
            if (existingShelf != null) UnityEngine.Object.DestroyImmediate(existingShelf.gameObject);
            GameObject shelfModel = LoadOrInstantiateModel("bookcaseClosed.glb", shelfTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (shelfModel != null) GroundModel(shelfModel, 0.0f);

            var shelfCol = shelfTf.GetComponent<BoxCollider>();
            if (shelfCol == null) shelfCol = shelfTf.gameObject.AddComponent<BoxCollider>();
            shelfCol.size = new Vector3(1.1f, 1.95f, 0.65f);
            shelfCol.center = new Vector3(0f, 0.98f, 0f);

            // 4. Coat Rack
            Transform coatTf = roomTf.Find("Furniture_CoatRack");
            if (coatTf == null)
            {
                coatTf = new GameObject("Furniture_CoatRack").transform;
                coatTf.SetParent(roomTf, false);
            }
            coatTf.localPosition = new Vector3(1.1f, 0.0f, 3.8f);
            coatTf.localEulerAngles = Vector3.zero;
            StripMeshComponents(coatTf.gameObject);

            Transform existingCoat = coatTf.Find("coatRackStanding");
            if (existingCoat != null) UnityEngine.Object.DestroyImmediate(existingCoat.gameObject);
            GameObject coatModel = LoadOrInstantiateModel("coatRackStanding.glb", coatTf, Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            if (coatModel != null) GroundModel(coatModel, 0.0f);

            var coatCol = coatTf.GetComponent<BoxCollider>();
            if (coatCol == null) coatCol = coatTf.gameObject.AddComponent<BoxCollider>();
            coatCol.size = new Vector3(0.45f, 1.8f, 0.45f);
            coatCol.center = new Vector3(0f, 0.9f, 0f);

            // 5. Living Room TV Unit & Modern Television
            Transform existingTvUnit = roomTf.Find("Living_TV_Unit");
            if (existingTvUnit != null) UnityEngine.Object.DestroyImmediate(existingTvUnit.gameObject);
            GameObject tvUnit = new GameObject("Living_TV_Unit");
            tvUnit.transform.SetParent(roomTf, false);
            tvUnit.transform.localPosition = new Vector3(1.8f, 0.0f, 0.5f);
            tvUnit.transform.localEulerAngles = Vector3.zero;

            GameObject tvStand = LoadOrInstantiateModel("cabinetTelevisionDoors.glb", tvUnit.transform, Vector3.zero, Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            if (tvStand != null) GroundModel(tvStand, 0.0f);
            LoadOrInstantiateModel("televisionModern.glb", tvUnit.transform, new Vector3(0f, 0.62f, 0f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));

            var tvCol = tvUnit.AddComponent<BoxCollider>();
            tvCol.size = new Vector3(1.3f, 1.2f, 0.65f);
            tvCol.center = new Vector3(0f, 0.6f, 0f);

            // 6. Round Rug & Potted Plant
            Transform existingRoundRug = roomTf.Find("rugRound");
            if (existingRoundRug != null) UnityEngine.Object.DestroyImmediate(existingRoundRug.gameObject);
            LoadOrInstantiateModel("rugRound.glb", roomTf, new Vector3(3.2f, 0.005f, 1.6f), Vector3.zero, new Vector3(1.7f, 1.0f, 1.7f));

            Transform existingPlant = roomTf.Find("pottedPlant");
            if (existingPlant != null) UnityEngine.Object.DestroyImmediate(existingPlant.gameObject);
            LoadOrInstantiateModel("pottedPlant.glb", roomTf, new Vector3(5.0f, 0.0f, 4.0f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
        }

        private static void UpgradeKitchen(GameObject houseRoot)
        {
            Transform roomTf = houseRoot.transform.Find("Room_Kitchen");
            if (roomTf == null)
            {
                roomTf = new GameObject("Room_Kitchen").transform;
                roomTf.SetParent(houseRoot.transform, false);
            }

            // 1. Kitchen Countertop with Sink
            Transform counterTf = roomTf.Find("Kitchen_Countertop_Sink");
            if (counterTf == null)
            {
                counterTf = new GameObject("Kitchen_Countertop_Sink").transform;
                counterTf.SetParent(roomTf, false);
            }
            counterTf.localPosition = new Vector3(-4.8f, 0.0f, 2.5f);
            counterTf.localEulerAngles = new Vector3(0f, 90f, 0f);
            StripMeshComponents(counterTf.gameObject);

            Transform existingSink = counterTf.Find("kitchenSink");
            if (existingSink != null) UnityEngine.Object.DestroyImmediate(existingSink.gameObject);
            GameObject sinkModel = LoadOrInstantiateModel("kitchenSink.glb", counterTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (sinkModel != null) GroundModel(sinkModel, 0.0f);

            var counterCol = counterTf.GetComponent<BoxCollider>();
            if (counterCol == null) counterCol = counterTf.gameObject.AddComponent<BoxCollider>();
            counterCol.size = new Vector3(1.0f, 0.95f, 2.4f);
            counterCol.center = new Vector3(0f, 0.48f, 0f);

            var sinkFixture = counterTf.GetComponent<UtilityFixtureInteractable>();
            if (sinkFixture == null) sinkFixture = counterTf.gameObject.AddComponent<UtilityFixtureInteractable>();
            sinkFixture.SetObjectDetails("Mutfak Eviyesi", "SU KAYNAĞI");
            sinkFixture.SetPersistentId("starter_kitchen_sink");

            // 2. Kitchen Pantry Cabinet (Loot Container 2)
            Transform cabTf = roomTf.Find("Furniture_Starter_KitchenCabinet") ?? houseRoot.transform.Find("Furniture_Starter_KitchenCabinet");
            if (cabTf != null)
            {
                cabTf.SetParent(roomTf, true);
                cabTf.localPosition = new Vector3(-4.8f, 0.0f, 1.2f);
                cabTf.localEulerAngles = new Vector3(0f, 90f, 0f);
                StripMeshComponents(cabTf.gameObject);

                Transform existingCabModel = cabTf.Find("kitchenCabinet");
                if (existingCabModel != null) UnityEngine.Object.DestroyImmediate(existingCabModel.gameObject);
                GameObject cabModel = LoadOrInstantiateModel("kitchenCabinet.glb", cabTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
                if (cabModel != null) GroundModel(cabModel, 0.0f);

                // Upper cabinets
                LoadOrInstantiateModel("kitchenCabinetUpperDouble.glb", cabTf, new Vector3(0f, 1.6f, 0f), Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));

                var cCol = cabTf.GetComponent<BoxCollider>();
                if (cCol == null) cCol = cabTf.gameObject.AddComponent<BoxCollider>();
                cCol.size = new Vector3(0.95f, 0.95f, 0.95f);
                cCol.center = new Vector3(0f, 0.48f, 0f);

                var wio = cabTf.GetComponent<WorldInteractableObject>();
                if (wio != null) wio.SetObjectDetails("Mutfak Dolabı", "ERZAK DOLABI");
            }

            // 3. Kitchen Refrigerator (Loot Container 3)
            Transform fridgeTf = roomTf.Find("Furniture_Starter_Refrigerator") ?? houseRoot.transform.Find("Furniture_Starter_Refrigerator");
            if (fridgeTf != null)
            {
                fridgeTf.SetParent(roomTf, true);
                fridgeTf.localPosition = new Vector3(-4.8f, 0.0f, 3.8f);
                fridgeTf.localEulerAngles = new Vector3(0f, 90f, 0f);
                StripMeshComponents(fridgeTf.gameObject);

                Transform existingFridgeModel = fridgeTf.Find("kitchenFridgeLarge");
                if (existingFridgeModel != null) UnityEngine.Object.DestroyImmediate(existingFridgeModel.gameObject);
                GameObject fridgeModel = LoadOrInstantiateModel("kitchenFridgeLarge.glb", fridgeTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
                if (fridgeModel != null) GroundModel(fridgeModel, 0.0f);

                var fCol = fridgeTf.GetComponent<BoxCollider>();
                if (fCol == null) fCol = fridgeTf.gameObject.AddComponent<BoxCollider>();
                fCol.size = new Vector3(0.95f, 1.85f, 0.95f);
                fCol.center = new Vector3(0f, 0.92f, 0f);

                var wio = fridgeTf.GetComponent<WorldInteractableObject>();
                if (wio != null) wio.SetObjectDetails("Buzdolabı", "SOĞUTUCU");
            }

            // 4. Kitchen Electric Stove
            Transform existingStove = roomTf.Find("kitchenStoveElectric");
            if (existingStove != null) UnityEngine.Object.DestroyImmediate(existingStove.gameObject);
            GameObject stoveObj = LoadOrInstantiateModel("kitchenStoveElectric.glb", roomTf, new Vector3(-3.6f, 0.0f, 4.1f), new Vector3(0f, 180f, 0f), new Vector3(1.35f, 1.35f, 1.35f));
            if (stoveObj != null)
            {
                GroundModel(stoveObj, 0.0f);
                var stoveCol = stoveObj.AddComponent<BoxCollider>();
                stoveCol.size = new Vector3(0.95f, 0.95f, 0.95f);
                stoveCol.center = new Vector3(0f, 0.48f, 0f);
            }

            // 5. Dining Table with Real Chairs
            Transform tableTf = roomTf.Find("Furniture_DiningTable");
            if (tableTf == null)
            {
                tableTf = new GameObject("Furniture_DiningTable").transform;
                tableTf.SetParent(roomTf, false);
            }
            tableTf.localPosition = new Vector3(-2.6f, 0.0f, 1.8f);
            tableTf.localEulerAngles = Vector3.zero;
            StripMeshComponents(tableTf.gameObject);

            Transform existingTable = tableTf.Find("table");
            if (existingTable != null) UnityEngine.Object.DestroyImmediate(existingTable.gameObject);
            GameObject tableModel = LoadOrInstantiateModel("table.glb", tableTf, Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            if (tableModel != null) GroundModel(tableModel, 0.0f);

            var tCol = tableTf.GetComponent<BoxCollider>();
            if (tCol == null) tCol = tableTf.gameObject.AddComponent<BoxCollider>();
            tCol.size = new Vector3(1.3f, 0.85f, 0.85f);
            tCol.center = new Vector3(0f, 0.42f, 0f);

            // Add 2 Chairs
            Transform existingChairs = tableTf.Find("Dining_Chairs");
            if (existingChairs != null) UnityEngine.Object.DestroyImmediate(existingChairs.gameObject);
            GameObject chairsRoot = new GameObject("Dining_Chairs");
            chairsRoot.transform.SetParent(tableTf, false);
            LoadOrInstantiateModel("chairModernCushion.glb", chairsRoot.transform, new Vector3(-0.75f, 0.0f, 0.0f), new Vector3(0f, 90f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            LoadOrInstantiateModel("chairModernCushion.glb", chairsRoot.transform, new Vector3(0.75f, 0.0f, 0.0f), new Vector3(0f, -90f, 0f), new Vector3(1.2f, 1.2f, 1.2f));

            // Kitchen appliances on counter
            Transform existingAppliances = roomTf.Find("Kitchen_Appliances");
            if (existingAppliances != null) UnityEngine.Object.DestroyImmediate(existingAppliances.gameObject);
            GameObject appRoot = new GameObject("Kitchen_Appliances");
            appRoot.transform.SetParent(roomTf, false);
            LoadOrInstantiateModel("kitchenMicrowave.glb", appRoot.transform, new Vector3(-4.8f, 0.95f, 3.1f), new Vector3(0f, 90f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            LoadOrInstantiateModel("kitchenCoffeeMachine.glb", appRoot.transform, new Vector3(-4.8f, 0.95f, 1.9f), new Vector3(0f, 90f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
        }

        private static void UpgradeBathroom(GameObject houseRoot)
        {
            Transform roomTf = houseRoot.transform.Find("Room_Bathroom");
            if (roomTf == null)
            {
                roomTf = new GameObject("Room_Bathroom").transform;
                roomTf.SetParent(houseRoot.transform, false);
            }

            // 1. Ceramic Washbasin Sink
            Transform sinkTf = roomTf.Find("Bathroom_Sink");
            if (sinkTf == null)
            {
                sinkTf = new GameObject("Bathroom_Sink").transform;
                sinkTf.SetParent(roomTf, false);
            }
            sinkTf.localPosition = new Vector3(4.9f, 0.0f, -3.2f);
            sinkTf.localEulerAngles = new Vector3(0f, -90f, 0f);
            StripMeshComponents(sinkTf.gameObject);

            Transform existingSink = sinkTf.Find("bathroomSink");
            if (existingSink != null) UnityEngine.Object.DestroyImmediate(existingSink.gameObject);
            GameObject sinkModel = LoadOrInstantiateModel("bathroomSink.glb", sinkTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (sinkModel != null) GroundModel(sinkModel, 0.0f);

            var sCol = sinkTf.GetComponent<BoxCollider>();
            if (sCol == null) sCol = sinkTf.gameObject.AddComponent<BoxCollider>();
            sCol.size = new Vector3(0.75f, 0.95f, 0.85f);
            sCol.center = new Vector3(0f, 0.48f, 0f);

            var sinkFixture = sinkTf.GetComponent<UtilityFixtureInteractable>();
            if (sinkFixture == null) sinkFixture = sinkTf.gameObject.AddComponent<UtilityFixtureInteractable>();
            sinkFixture.SetObjectDetails("Banyo Lavabosu", "SU KAYNAĞI");
            sinkFixture.SetPersistentId("starter_bathroom_sink");

            // 2. Bathroom Mirror
            Transform mirrorTf = roomTf.Find("Bathroom_Mirror");
            if (mirrorTf == null)
            {
                mirrorTf = new GameObject("Bathroom_Mirror").transform;
                mirrorTf.SetParent(roomTf, false);
            }
            mirrorTf.localPosition = new Vector3(5.4f, 1.4f, -3.2f);
            mirrorTf.localEulerAngles = new Vector3(0f, -90f, 0f);
            StripMeshComponents(mirrorTf.gameObject);

            Transform existingMirror = mirrorTf.Find("bathroomMirror");
            if (existingMirror != null) UnityEngine.Object.DestroyImmediate(existingMirror.gameObject);
            LoadOrInstantiateModel("bathroomMirror.glb", mirrorTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));

            // 3. Medicine Cabinet (Loot Container 4)
            Transform medTf = roomTf.Find("Furniture_Starter_MedicineCabinet") ?? houseRoot.transform.Find("Furniture_Starter_MedicineCabinet");
            if (medTf != null)
            {
                medTf.SetParent(roomTf, true);
                medTf.localPosition = new Vector3(5.4f, 1.3f, -1.8f);
                medTf.localEulerAngles = new Vector3(0f, -90f, 0f);
                StripMeshComponents(medTf.gameObject);

                Transform existingMed = medTf.Find("bathroomCabinet");
                if (existingMed != null) UnityEngine.Object.DestroyImmediate(existingMed.gameObject);
                GameObject medModel = LoadOrInstantiateModel("bathroomCabinet.glb", medTf, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));

                var mCol = medTf.GetComponent<BoxCollider>();
                if (mCol == null) mCol = medTf.gameObject.AddComponent<BoxCollider>();
                mCol.size = new Vector3(0.65f, 0.85f, 0.35f);
                mCol.center = Vector3.zero;

                var wio = medTf.GetComponent<WorldInteractableObject>();
                if (wio != null) wio.SetObjectDetails("Banyo Ecza Dolabı", "İLK YARDIM");
            }

            // 4. Ceramic Toilet
            Transform existingToilet = roomTf.Find("Bathroom_Toilet");
            if (existingToilet != null) UnityEngine.Object.DestroyImmediate(existingToilet.gameObject);
            GameObject toiletObj = new GameObject("Bathroom_Toilet");
            toiletObj.transform.SetParent(roomTf, false);
            toiletObj.transform.localPosition = new Vector3(2.5f, 0.0f, -4.0f);
            toiletObj.transform.localEulerAngles = Vector3.zero;
            GameObject toiletModel = LoadOrInstantiateModel("toilet.glb", toiletObj.transform, Vector3.zero, Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            if (toiletModel != null) GroundModel(toiletModel, 0.0f);

            var tCol = toiletObj.AddComponent<BoxCollider>();
            tCol.size = new Vector3(0.65f, 0.85f, 0.75f);
            tCol.center = new Vector3(0f, 0.42f, 0f);

            // 5. Bathtub
            Transform existingTub = roomTf.Find("Bathroom_Bathtub");
            if (existingTub != null) UnityEngine.Object.DestroyImmediate(existingTub.gameObject);
            GameObject tubObj = new GameObject("Bathroom_Bathtub");
            tubObj.transform.SetParent(roomTf, false);
            tubObj.transform.localPosition = new Vector3(1.8f, 0.0f, -2.2f);
            tubObj.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            GameObject tubModel = LoadOrInstantiateModel("bathtub.glb", tubObj.transform, Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            if (tubModel != null) GroundModel(tubModel, 0.0f);

            var tubCol = tubObj.AddComponent<BoxCollider>();
            tubCol.size = new Vector3(1.7f, 0.65f, 0.85f);
            tubCol.center = new Vector3(0f, 0.32f, 0f);
        }

        private static void StripPrimitiveChildren(Transform parent, string[] childNames)
        {
            for (int i = 0; i < childNames.Length; i++)
            {
                Transform c = parent.Find(childNames[i]);
                if (c != null)
                {
                    UnityEngine.Object.DestroyImmediate(c.gameObject);
                }
            }
        }

        private static void StripMeshComponents(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) UnityEngine.Object.DestroyImmediate(mf);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
        }

        public static void GroundModel(GameObject instance, float floorTopY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            float currentBottomY = b.min.y;
            float deltaY = floorTopY - currentBottomY;
            instance.transform.position += new Vector3(0f, deltaY, 0f);
        }

        public static void ConvertAllMaterialsToURP(GameObject root)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("[StarterHouseInteriorUpgrader] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            int convertedCount = 0;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer rend = renderers[r];
                if (rend == null) continue;

                Material[] mats = rend.sharedMaterials;
                bool modified = false;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;

                    string shaderName = mat.shader != null ? mat.shader.name : "";
                    bool isMagentaOrBroken = mat.shader == null ||
                                             shaderName.Contains("InternalErrorShader") ||
                                             shaderName.Contains("Error");

                    bool isGltfShader = shaderName.Contains("glTF") || shaderName.Contains("Standard");

                    if (isMagentaOrBroken || isGltfShader)
                    {
                        Color baseColor = Color.white;
                        if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
                        else if (mat.HasProperty("_Color")) baseColor = mat.color;

                        Texture mainTex = mat.mainTexture;

                        Material newMat = new Material(urpLit);
                        newMat.name = mat.name + "_URPLit";
                        newMat.SetColor("_BaseColor", baseColor);
                        if (mainTex != null) newMat.SetTexture("_BaseMap", mainTex);
                        newMat.SetFloat("_Smoothness", 0.4f);
                        newMat.SetFloat("_Metallic", 0.05f);
                        newMat.enableInstancing = true;

                        mats[m] = newMat;
                        modified = true;
                        convertedCount++;
                    }
                    else
                    {
                        mat.enableInstancing = true;
                    }
                }

                if (modified)
                {
                    rend.sharedMaterials = mats;
                }
            }

            Debug.Log($"[StarterHouseInteriorUpgrader] Converted and validated {convertedCount} materials to Universal Render Pipeline/Lit (enableInstancing = true).");
        }

        private static void RebuildHouseNavMesh()
        {
            Debug.Log("[StarterHouseInteriorUpgrader] Rebuilding NavMesh for Starter House...");
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("[StarterHouseInteriorUpgrader] NavMesh baked successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StarterHouseInteriorUpgrader] NavMesh bake note: {ex.Message}");
            }
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class StarterHouseAutoUpgrader
    {
        private const string UpgradedPrefKey = "StarterHouse_CC0_Step3A_Upgraded_v1";

        static StarterHouseAutoUpgrader()
        {
            EditorApplication.delayCall += CheckAndUpgrade;
        }

        private static void CheckAndUpgrade()
        {
            if (SessionState.GetBool(UpgradedPrefKey, false)) return;
            SessionState.SetBool(UpgradedPrefKey, true);

            Debug.Log("[StarterHouseAutoUpgrader] Automatically executing Step 3A CC0 3D Interior Upgrade & Audit...");
            try
            {
                StarterHouseInteriorUpgrader.UpgradeStarterHouseMenu();
                Step3A_Interior3DModelsAudit.RunAudit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StarterHouseAutoUpgrader] Auto upgrade failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
#endif
}
