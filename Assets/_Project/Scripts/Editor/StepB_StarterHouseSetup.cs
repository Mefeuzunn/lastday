using System;
using System.Collections.Generic;
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
using ProjectSurvival.Player;
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Step B: Scene Setup and World Injection for Furnished Starter House (4 Connected Rooms),
    /// Bedroom Wake-Up Sequence, Solid Weather Roof Slab, Nightstand Smartphone, and NavMesh.
    /// </summary>
    public static class StepB_StarterHouseSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Step B Furnished Starter House & Wake-Up", false, 44)]
        public static void SetupStepB()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[StepB_Setup] Starting Step B Furnished Starter House & Wake-Up Setup...");

            // 1. Setup Starter House Root & Footprint
            GameObject houseRoot = SetupStarterHouseRoot();

            // 2. Setup 4 Distinct Rooms & Solid Weather Roof Slab
            SetupArchitecturalRoomsAndRoof(houseRoot);

            // 3. Furnish Bedroom with Bed, Nightstand & Interactive Smartphone
            SetupBedroomFurnishing(houseRoot);

            // 4. Furnish Living Room & Entryway
            SetupLivingRoomFurnishing(houseRoot);

            // 5. Furnish Kitchen with Countertop & Sink
            SetupKitchenFurnishing(houseRoot);

            // 6. Furnish Bathroom with Washbasin & Mirror
            SetupBathroomFurnishing(houseRoot);

            // 7. Ensure ApartmentWakeUpSequence & Smartphone UI
            SetupWakeUpSequenceAndUI(houseRoot);

            // 8. Rebuild NavMesh for Seamless Indoor-Outdoor Flow
            RebuildHouseNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StepB_Setup] Step B Furnished Starter House & Wake-Up Setup completed successfully!");
        }

        private static GameObject SetupStarterHouseRoot()
        {
            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot == null)
            {
                houseRoot = new GameObject("StarterHouse_Safehome");
                houseRoot.transform.position = Vector3.zero;

                GameObject sectorCenter = GameObject.Find("Sector_0_0_Center");
                if (sectorCenter != null)
                {
                    houseRoot.transform.SetParent(sectorCenter.transform, true);
                }
            }

            // Remove legacy single floor if present to allow dedicated room flooring
            Transform legacyFloor = houseRoot.transform.Find("House_Floor");
            if (legacyFloor != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyFloor.gameObject);
            }

            // Remove any legacy / stray root collider blocking spawn at (0, 0, 0)
            GameObject fireBarrels = GameObject.Find("--- ATMOSPHERIC FIRE BARRELS ---");
            if (fireBarrels != null)
            {
                var interactables = fireBarrels.GetComponents<MonoBehaviour>();
                foreach (var mb in interactables)
                {
                    if (mb != null)
                    {
                        UnityEngine.Object.DestroyImmediate(mb);
                    }
                }
                var cols = fireBarrels.GetComponents<Collider>();
                foreach (var c in cols)
                {
                    if (c != null) UnityEngine.Object.DestroyImmediate(c);
                }
            }

            return houseRoot;
        }

        private static void SetupArchitecturalRoomsAndRoof(GameObject houseRoot)
        {
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Building_Wall.mat") ?? woodMat;
            Material roofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Viaduct_Concrete.mat") ?? wallMat;
            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Window_Glass.mat");

            // 1. Architectural Structure Container
            Transform structTf = houseRoot.transform.Find("Architecture");
            GameObject structObj;
            if (structTf != null)
            {
                structObj = structTf.gameObject;
            }
            else
            {
                structObj = new GameObject("Architecture");
                structObj.transform.SetParent(houseRoot.transform, false);
            }

            // Solid Roof Slab (Footprint: 11.2 x 9.2m at y = 3.15m)
            // Non-trigger BoxCollider guarantees DynamicWeatherManager upward raycast hits ceiling
            Transform roofTf = structObj.transform.Find("House_Roof_Slab");
            GameObject roof;
            if (roofTf == null)
            {
                roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "House_Roof_Slab";
                roof.transform.SetParent(structObj.transform, false);
            }
            else
            {
                roof = roofTf.gameObject;
            }
            roof.transform.localPosition = new Vector3(0f, 3.15f, 0f);
            roof.transform.localScale = new Vector3(11.2f, 0.2f, 9.2f);
            if (roofMat != null) roof.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
            var col = roof.GetComponent<BoxCollider>();
            if (col == null) col = roof.AddComponent<BoxCollider>();
            col.isTrigger = false;
            // Roof must NOT be NavigationStatic to avoid carving holes or baking roof walkmesh
            GameObjectUtility.SetStaticEditorFlags(roof, 0);

            // Continuous Main House Base Floor (Footprint: 11.2 x 9.2m at y = -0.05m)
            // Guarantees completely seamless NavMesh across all 4 rooms
            CreateRoomFloor(structObj.transform, "House_Floor_Main", new Vector3(0f, -0.05f, 0f), new Vector3(11.2f, 0.1f, 9.2f), woodMat);

            // Front Porch & Entrance Threshold (Extends from Z = 4.4 to Z = 6.2 to connect smoothly with street asphalt)
            CreateRoomFloor(structObj.transform, "Porch_Floor_Outdoor", new Vector3(0f, -0.05f, 5.3f), new Vector3(3.4f, 0.1f, 2.0f), woodMat);

            // Exterior Perimeter Walls (Height = 3.0m, Thickness = 0.2m)
            // South Wall (Z = -4.5)
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_South", new Vector3(0f, 1.5f, -4.5f), new Vector3(11.2f, 3.0f, 0.2f), wallMat);

            // West Wall (X = -5.5)
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_West", new Vector3(-5.5f, 1.5f, 0f), new Vector3(0.2f, 3.0f, 9.2f), wallMat);

            // East Wall (X = 5.5)
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_East", new Vector3(5.5f, 1.5f, 0f), new Vector3(0.2f, 3.0f, 9.2f), wallMat);

            // North Wall with Front Doorway (Z = 4.5) - 2.2m wide opening at X: [-1.1, 1.1]
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_North_Left", new Vector3(-3.3f, 1.5f, 4.5f), new Vector3(4.4f, 3.0f, 0.2f), wallMat);
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_North_Right", new Vector3(3.3f, 1.5f, 4.5f), new Vector3(4.4f, 3.0f, 0.2f), wallMat);
            CreateOrUpdateWall(structObj.transform, "Wall_Exterior_North_Lintel", new Vector3(0f, 2.6f, 4.5f), new Vector3(2.2f, 0.8f, 0.2f), wallMat, isNavStatic: false);

            // Interior Partition Walls
            // Central East-West Partition Wall (Z = 0.2) separating Bedroom/Bathroom (South) from Kitchen/Living (North)
            // Leaves a 2.2m wide doorway at X: [-1.1, 1.1]
            CreateOrUpdateWall(structObj.transform, "Wall_Interior_EW_Left", new Vector3(-3.3f, 1.5f, 0.2f), new Vector3(4.4f, 3.0f, 0.15f), wallMat);
            CreateOrUpdateWall(structObj.transform, "Wall_Interior_EW_Right", new Vector3(3.3f, 1.5f, 0.2f), new Vector3(4.4f, 3.0f, 0.15f), wallMat);
            CreateOrUpdateWall(structObj.transform, "Wall_Interior_EW_Lintel", new Vector3(0f, 2.6f, 0.2f), new Vector3(2.2f, 0.8f, 0.15f), wallMat, isNavStatic: false);

            // Central North-South Partition Wall separating Kitchen (West) from Living Room (East) (X = -0.8, Z: 0.2 to 4.5)
            CreateOrUpdateWall(structObj.transform, "Wall_Interior_NS_Kitchen_Living", new Vector3(-0.8f, 1.5f, 2.3f), new Vector3(0.15f, 3.0f, 2.4f), wallMat);

            // Interior North-South Partition Wall separating Bedroom (West) from Bathroom (East) (X = 0.8, Z: -4.5 to 0.2)
            CreateOrUpdateWall(structObj.transform, "Wall_Interior_NS_Bed_Bath", new Vector3(0.8f, 1.5f, -2.4f), new Vector3(0.15f, 3.0f, 2.6f), wallMat);

            // Ambient Ceiling Lights in Each Room
            CreateOrUpdatePointLight(structObj.transform, "Light_Bedroom", new Vector3(-1.8f, 2.6f, -1.8f), new Color(1.0f, 0.88f, 0.65f), 1.6f, 6.5f);
            CreateOrUpdatePointLight(structObj.transform, "Light_LivingRoom", new Vector3(2.5f, 2.6f, 2.2f), new Color(1.0f, 0.90f, 0.72f), 1.8f, 7.5f);
            CreateOrUpdatePointLight(structObj.transform, "Light_Kitchen", new Vector3(-3.0f, 2.6f, 2.5f), new Color(0.95f, 0.95f, 1.0f), 1.6f, 6.5f);
            CreateOrUpdatePointLight(structObj.transform, "Light_Bathroom", new Vector3(3.2f, 2.6f, -2.0f), new Color(0.90f, 0.95f, 1.0f), 1.4f, 5.5f);
        }

        private static void SetupBedroomFurnishing(GameObject houseRoot)
        {
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material sheetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Bedsheet_Pattern.mat") ?? woodMat;
            Material blanketMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Blanket_Wool.mat") ?? woodMat;
            Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Ambulance_White.mat") ?? woodMat;

            // 1. Room Root
            Transform roomTf = houseRoot.transform.Find("Room_Bedroom");
            GameObject roomObj;
            if (roomTf != null) roomObj = roomTf.gameObject;
            else
            {
                roomObj = new GameObject("Room_Bedroom");
                roomObj.transform.SetParent(houseRoot.transform, false);
            }

            // Hardwood Floor: X: [-5.4, 0.7], Z: [-4.4, 0.2]
            CreateRoomFloor(roomObj.transform, "Floor_Bedroom", new Vector3(-2.35f, -0.05f, -2.1f), new Vector3(6.1f, 0.1f, 4.4f), woodMat);

            // 2. Bed (Frame + Mattress + Blanket + Pillow)
            Transform bedTf = roomObj.transform.Find("Furniture_Bed_Master");
            GameObject bedObj;
            if (bedTf != null) bedObj = bedTf.gameObject;
            else
            {
                bedObj = new GameObject("Furniture_Bed_Master");
                bedObj.transform.SetParent(roomObj.transform, false);
            }
            bedObj.transform.localPosition = new Vector3(-2.8f, 0.25f, -1.8f);

            // Bed Mattress Base
            CreateOrUpdatePrimitiveChild(bedObj.transform, "Bed_Mattress", PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 0.45f, 2.1f), sheetMat);

            // Bed Blanket
            CreateOrUpdatePrimitiveChild(bedObj.transform, "Bed_Blanket", PrimitiveType.Cube, new Vector3(0f, 0.08f, -0.25f), new Vector3(1.62f, 0.35f, 1.5f), blanketMat);

            // Pillow
            CreateOrUpdatePrimitiveChild(bedObj.transform, "Bed_Pillow", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.7f), new Vector3(1.1f, 0.18f, 0.45f), whiteMat);

            // Bed BoxCollider must be added before UtilityFixtureInteractable (due to [RequireComponent(typeof(Collider))])
            var bedCol = bedObj.GetComponent<BoxCollider>();
            if (bedCol == null) bedCol = bedObj.AddComponent<BoxCollider>();
            bedCol.size = new Vector3(1.6f, 0.6f, 2.1f);
            bedCol.center = Vector3.zero;

            // Bed UtilityFixtureInteractable (Resting / Sleep point)
            var fixture = bedObj.GetComponent<UtilityFixtureInteractable>();
            if (fixture == null) fixture = bedObj.AddComponent<UtilityFixtureInteractable>();
            fixture.SetObjectDetails("Dinlenme Yatağı", "DİNLENME NOKTASI");
            fixture.SetPersistentId("starter_bed_master");

            // 3. Wooden Nightstand (Beside bed on player's side)
            Transform nsTf = roomObj.transform.Find("Nightstand_Wood");
            GameObject nsObj;
            if (nsTf != null) nsObj = nsTf.gameObject;
            else
            {
                nsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nsObj.name = "Nightstand_Wood";
                nsObj.transform.SetParent(roomObj.transform, false);
            }
            nsObj.transform.localPosition = new Vector3(-1.6f, 0.35f, -1.8f);
            nsObj.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
            if (woodMat != null) nsObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // 4. Interactive Nightstand Smartphone (Resting on nightstand top)
            Transform phoneTf = roomObj.transform.Find("Smartphone_Nightstand");
            GameObject phoneObj;
            if (phoneTf != null) phoneObj = phoneTf.gameObject;
            else
            {
                phoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                phoneObj.name = "Smartphone_Nightstand";
                phoneObj.transform.SetParent(roomObj.transform, false);
            }
            phoneObj.transform.localPosition = new Vector3(-1.6f, 0.72f, -1.8f);
            phoneObj.transform.localEulerAngles = new Vector3(0f, 25f, 0f);
            phoneObj.transform.localScale = new Vector3(0.09f, 0.015f, 0.18f);

            Material phoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Window_LitCyan.mat") ??
                                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Beacon_Blue.mat");
            if (phoneMat != null) phoneObj.GetComponent<MeshRenderer>().sharedMaterial = phoneMat;

            // Trigger SphereCollider for proximity interaction (radius = 2.6f)
            SphereCollider phoneCol = phoneObj.GetComponent<SphereCollider>();
            if (phoneCol == null)
            {
                var bc = phoneObj.GetComponent<BoxCollider>();
                if (bc != null && bc.isTrigger) UnityEngine.Object.DestroyImmediate(bc);
                phoneCol = phoneObj.AddComponent<SphereCollider>();
            }
            phoneCol.isTrigger = true;
            phoneCol.radius = 2.6f;

            var phoneInteractable = phoneObj.GetComponent<SmartphoneNewsInteractable>();
            if (phoneInteractable == null) phoneInteractable = phoneObj.AddComponent<SmartphoneNewsInteractable>();
            phoneInteractable.SetObjectDetails("AKILLI TELEFON", "Haberlere Bak");
            phoneInteractable.SetPersistentId("starter_intro_smartphone");

            // Point Light for pulsing emissive screen glow (Shadows: None)
            Transform glowTf = phoneObj.transform.Find("Screen_Glow_Light");
            if (glowTf == null)
            {
                GameObject glowObj = new GameObject("Screen_Glow_Light");
                glowObj.transform.SetParent(phoneObj.transform, false);
                glowObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                Light l = glowObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.color = new Color(0.2f, 0.75f, 1.0f);
                l.intensity = 0.8f;
                l.range = 1.4f;
            }

            // 5. Wardrobe / Closet (from Step C, re-parented or updated)
            Transform wardrobeTf = houseRoot.transform.Find("Furniture_Starter_Wardrobe");
            if (wardrobeTf != null)
            {
                wardrobeTf.SetParent(roomObj.transform, true);
                wardrobeTf.localPosition = new Vector3(-4.5f, 1.05f, -2.5f);
            }

            // 6. Bedroom Interior Door
            SetupInteriorDoor(roomObj.transform, "Door_Starter_Bedroom", "Yatak Odası Kapısı", new Vector3(0f, 0f, 0.2f), 90f);
        }

        private static void SetupLivingRoomFurnishing(GameObject houseRoot)
        {
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material couchMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Tactical_Fabric.mat") ?? woodMat;

            // 1. Room Root
            Transform roomTf = houseRoot.transform.Find("Room_LivingRoom");
            GameObject roomObj;
            if (roomTf != null) roomObj = roomTf.gameObject;
            else
            {
                roomObj = new GameObject("Room_LivingRoom");
                roomObj.transform.SetParent(houseRoot.transform, false);
            }

            // Hardwood Floor: X: [-0.8, 5.4], Z: [0.2, 4.4]
            CreateRoomFloor(roomObj.transform, "Floor_LivingRoom", new Vector3(2.3f, -0.05f, 2.3f), new Vector3(6.2f, 0.1f, 4.2f), woodMat);

            // 2. Sofa / Couch
            Transform sofaTf = roomObj.transform.Find("Furniture_Sofa_Living");
            GameObject sofaObj;
            if (sofaTf != null) sofaObj = sofaTf.gameObject;
            else
            {
                sofaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sofaObj.name = "Furniture_Sofa_Living";
                sofaObj.transform.SetParent(roomObj.transform, false);
            }
            sofaObj.transform.localPosition = new Vector3(3.2f, 0.45f, 2.5f);
            sofaObj.transform.localScale = new Vector3(2.0f, 0.8f, 0.9f);
            if (couchMat != null) sofaObj.GetComponent<MeshRenderer>().sharedMaterial = couchMat;

            var sofaFixture = sofaObj.GetComponent<UtilityFixtureInteractable>();
            if (sofaFixture == null) sofaFixture = sofaObj.AddComponent<UtilityFixtureInteractable>();
            sofaFixture.SetObjectDetails("Oturma Koltuğu", "DİNLENME NOKTASI");
            sofaFixture.SetPersistentId("starter_living_sofa");

            // 3. Coffee Table
            Transform tableTf = roomObj.transform.Find("Furniture_CoffeeTable");
            GameObject tableObj;
            if (tableTf != null) tableObj = tableTf.gameObject;
            else
            {
                tableObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableObj.name = "Furniture_CoffeeTable";
                tableObj.transform.SetParent(roomObj.transform, false);
            }
            tableObj.transform.localPosition = new Vector3(3.2f, 0.25f, 1.3f);
            tableObj.transform.localScale = new Vector3(1.2f, 0.45f, 0.7f);
            if (woodMat != null) tableObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // 4. Bookshelf
            Transform shelfTf = roomObj.transform.Find("Furniture_Bookshelf");
            GameObject shelfObj;
            if (shelfTf != null) shelfObj = shelfTf.gameObject;
            else
            {
                shelfObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelfObj.name = "Furniture_Bookshelf";
                shelfObj.transform.SetParent(roomObj.transform, false);
            }
            shelfObj.transform.localPosition = new Vector3(5.1f, 1.1f, 2.5f);
            shelfObj.transform.localScale = new Vector3(0.45f, 2.1f, 1.4f);
            if (woodMat != null) shelfObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // 5. Coat Rack
            Transform coatTf = roomObj.transform.Find("Furniture_CoatRack");
            GameObject coatObj;
            if (coatTf != null) coatObj = coatTf.gameObject;
            else
            {
                coatObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coatObj.name = "Furniture_CoatRack";
                coatObj.transform.SetParent(roomObj.transform, false);
            }
            coatObj.transform.localPosition = new Vector3(1.0f, 0.9f, 3.8f);
            coatObj.transform.localScale = new Vector3(0.25f, 0.9f, 0.25f);
            if (woodMat != null) coatObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // 6. Ensure Front Door (Door_Starter_Front at 0, 0, 4.0)
            Transform frontDoor = houseRoot.transform.Find("Door_Starter_Front") ?? houseRoot.transform.Find("Room_LivingRoom/Door_Starter_Front");
            if (frontDoor != null)
            {
                frontDoor.SetParent(houseRoot.transform, true);
                frontDoor.localPosition = new Vector3(0f, 0f, 4.0f);
                GameObjectUtility.SetStaticEditorFlags(frontDoor.gameObject, 0);

                var doorCol = frontDoor.GetComponent<Collider>();
                if (doorCol != null) doorCol.isTrigger = true;

                Transform frontLeaf = frontDoor.Find("Door_Leaf");
                if (frontLeaf != null)
                {
                    GameObjectUtility.SetStaticEditorFlags(frontLeaf.gameObject, 0);
                    var col = frontLeaf.GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;
                }
            }
        }

        private static void SetupKitchenFurnishing(GameObject houseRoot)
        {
            Material tileMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Subway_Tiled.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_AxeSteel.mat") ?? woodMat;

            // 1. Room Root
            Transform roomTf = houseRoot.transform.Find("Room_Kitchen");
            GameObject roomObj;
            if (roomTf != null) roomObj = roomTf.gameObject;
            else
            {
                roomObj = new GameObject("Room_Kitchen");
                roomObj.transform.SetParent(houseRoot.transform, false);
            }

            // Tiled Floor: X: [-5.4, -0.8], Z: [0.2, 4.4]
            CreateRoomFloor(roomObj.transform, "Floor_Kitchen", new Vector3(-3.1f, -0.05f, 2.3f), new Vector3(4.6f, 0.1f, 4.2f), tileMat ?? woodMat);

            // 2. Re-parent Pantry Cabinet & Refrigerator into Room_Kitchen
            Transform cabTf = houseRoot.transform.Find("Furniture_Starter_KitchenCabinet");
            if (cabTf != null) cabTf.SetParent(roomObj.transform, true);

            Transform fridgeTf = houseRoot.transform.Find("Furniture_Starter_Refrigerator");
            if (fridgeTf != null) fridgeTf.SetParent(roomObj.transform, true);

            // 3. Kitchen Countertop with Sink
            Transform counterTf = roomObj.transform.Find("Kitchen_Countertop_Sink");
            GameObject counterObj;
            if (counterTf != null) counterObj = counterTf.gameObject;
            else
            {
                counterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                counterObj.name = "Kitchen_Countertop_Sink";
                counterObj.transform.SetParent(roomObj.transform, false);
            }
            counterObj.transform.localPosition = new Vector3(-4.8f, 0.5f, 2.5f);
            counterObj.transform.localScale = new Vector3(0.8f, 0.95f, 2.4f);
            if (woodMat != null) counterObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Sink Fixture on countertop
            var sinkFixture = counterObj.GetComponent<UtilityFixtureInteractable>();
            if (sinkFixture == null) sinkFixture = counterObj.AddComponent<UtilityFixtureInteractable>();
            sinkFixture.SetObjectDetails("Mutfak Eviyesi", "SU KAYNAĞI");
            sinkFixture.SetPersistentId("starter_kitchen_sink");

            // 4. Dining Table with Chairs
            Transform dtTf = roomObj.transform.Find("Furniture_DiningTable");
            GameObject dtObj;
            if (dtTf != null) dtObj = dtTf.gameObject;
            else
            {
                dtObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dtObj.name = "Furniture_DiningTable";
                dtObj.transform.SetParent(roomObj.transform, false);
            }
            dtObj.transform.localPosition = new Vector3(-3.0f, 0.45f, 0.9f);
            dtObj.transform.localScale = new Vector3(1.2f, 0.85f, 0.8f);
            if (woodMat != null) dtObj.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        }

        private static void SetupBathroomFurnishing(GameObject houseRoot)
        {
            Material tileMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Subway_Tiled.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MAT_Glass_DustyReflective.mat") ??
                                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Car_Glass.mat");

            // 1. Room Root
            Transform roomTf = houseRoot.transform.Find("Room_Bathroom");
            GameObject roomObj;
            if (roomTf != null) roomObj = roomTf.gameObject;
            else
            {
                roomObj = new GameObject("Room_Bathroom");
                roomObj.transform.SetParent(houseRoot.transform, false);
            }

            // Tiled Floor: X: [0.8, 5.4], Z: [-4.4, 0.2]
            CreateRoomFloor(roomObj.transform, "Floor_Bathroom", new Vector3(3.1f, -0.05f, -2.1f), new Vector3(4.6f, 0.1f, 4.4f), tileMat ?? woodMat);

            // 2. Re-parent Medicine Cabinet into Room_Bathroom
            Transform medTf = houseRoot.transform.Find("Furniture_Starter_MedicineCabinet");
            if (medTf != null)
            {
                medTf.SetParent(roomObj.transform, true);
                medTf.localPosition = new Vector3(5.1f, 1.4f, -1.8f);
            }

            // 3. Washbasin Sink
            Transform sinkTf = roomObj.transform.Find("Bathroom_Sink");
            GameObject sinkObj;
            if (sinkTf != null) sinkObj = sinkTf.gameObject;
            else
            {
                sinkObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sinkObj.name = "Bathroom_Sink";
                sinkObj.transform.SetParent(roomObj.transform, false);
            }
            sinkObj.transform.localPosition = new Vector3(4.8f, 0.5f, -3.2f);
            sinkObj.transform.localScale = new Vector3(0.7f, 0.95f, 0.9f);
            if (tileMat != null) sinkObj.GetComponent<MeshRenderer>().sharedMaterial = tileMat;

            var sinkFixture = sinkObj.GetComponent<UtilityFixtureInteractable>();
            if (sinkFixture == null) sinkFixture = sinkObj.AddComponent<UtilityFixtureInteractable>();
            sinkFixture.SetObjectDetails("Banyo Lavabosu", "SU KAYNAĞI");
            sinkFixture.SetPersistentId("starter_bathroom_sink");

            // 4. Bathroom Mirror
            Transform mirrorTf = roomObj.transform.Find("Bathroom_Mirror");
            GameObject mirrorObj;
            if (mirrorTf != null) mirrorObj = mirrorTf.gameObject;
            else
            {
                mirrorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mirrorObj.name = "Bathroom_Mirror";
                mirrorObj.transform.SetParent(roomObj.transform, false);
            }
            mirrorObj.transform.localPosition = new Vector3(5.3f, 1.5f, -3.2f);
            mirrorObj.transform.localScale = new Vector3(0.05f, 0.8f, 0.65f);
            if (glassMat != null) mirrorObj.GetComponent<MeshRenderer>().sharedMaterial = glassMat;

            // 5. Bathroom Interior Door
            SetupInteriorDoor(roomObj.transform, "Door_Starter_Bathroom", "Banyo Kapısı", new Vector3(0.8f, 0f, -0.8f), 90f);
        }

        private static void SetupWakeUpSequenceAndUI(GameObject houseRoot)
        {
            // Clear any stale cached modal in the scene so the new texts and layout are fresh
            var staleModal = GameObject.Find("Smartphone_Screen_Modal");
            if (staleModal != null) UnityEngine.Object.DestroyImmediate(staleModal);

            // 1. Ensure ApartmentWakeUpSequence on StarterHouse_Safehome
            var wakeUp = houseRoot.GetComponent<ApartmentWakeUpSequence>();
            if (wakeUp == null) wakeUp = houseRoot.AddComponent<ApartmentWakeUpSequence>();

            // 2. Ensure SmartphoneScreenUI exists in scene
            var phoneUI = UnityEngine.Object.FindAnyObjectByType<SmartphoneScreenUI>(FindObjectsInactive.Include);
            if (phoneUI == null)
            {
                Canvas mainCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                GameObject uiObj = new GameObject("SmartphoneScreenUI");
                if (mainCanvas != null) uiObj.transform.SetParent(mainCanvas.transform, false);
                uiObj.AddComponent<SmartphoneScreenUI>();
            }

            // 3. Ensure SpawnProtectionController Safe Radius is 45m
            var spc = UnityEngine.Object.FindAnyObjectByType<SpawnProtectionController>(FindObjectsInactive.Include);
            if (spc != null)
            {
                var so = new SerializedObject(spc);
                var radiusProp = so.FindProperty("spawnSafeRadius");
                if (radiusProp != null)
                {
                    radiusProp.floatValue = 45.0f;
                    so.ApplyModifiedProperties();
                }
            }

            Debug.Log("[StepB_Setup] ApartmentWakeUpSequence, SmartphoneScreenUI, and SpawnProtectionController (45m) configured.");
        }

        private static void RebuildHouseNavMesh()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("[StepB_Setup] NavMesh successfully rebuilt across all 4 rooms and front door exit trajectory.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StepB_Setup] NavMesh rebuild encountered notice: {ex.Message}");
            }
        }

        private static void CreateOrUpdateWall(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, bool isNavStatic = true)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            if (mat != null)
            {
                var ren = go.GetComponent<MeshRenderer>();
                if (ren != null) ren.sharedMaterial = mat;
            }

            // Mark NavigationStatic only if specified (overhead lintels must NOT be navigation static to prevent carving holes in doorway floor)
            GameObjectUtility.SetStaticEditorFlags(go, isNavStatic ? StaticEditorFlags.NavigationStatic : 0);
        }

        private static void CreateRoomFloor(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            if (mat != null)
            {
                var ren = go.GetComponent<MeshRenderer>();
                if (ren != null) ren.sharedMaterial = mat;
            }

            // Mark NavigationStatic
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateOrUpdatePrimitiveChild(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            if (mat != null)
            {
                var ren = go.GetComponent<MeshRenderer>();
                if (ren != null) ren.sharedMaterial = mat;
            }
        }

        private static void CreateOrUpdatePointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPos;
            Light l = go.GetComponent<Light>();
            if (l == null) l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        private static void SetupInteriorDoor(Transform parent, string goName, string doorTitle, Vector3 localPos, float angle)
        {
            Transform existing = parent.Find(goName);
            GameObject doorObj;
            if (existing != null) doorObj = existing.gameObject;
            else
            {
                doorObj = new GameObject(goName);
                doorObj.transform.SetParent(parent, false);
            }
            doorObj.transform.localPosition = localPos;

            Transform leafTf = doorObj.transform.Find("Door_Leaf");
            GameObject leafObj;
            if (leafTf != null) leafObj = leafTf.gameObject;
            else
            {
                leafObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leafObj.name = "Door_Leaf";
                leafObj.transform.SetParent(doorObj.transform, false);
                leafObj.transform.localPosition = new Vector3(0.45f, 1.05f, 0f);
                leafObj.transform.localScale = new Vector3(0.9f, 2.1f, 0.08f);

                Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Door_Wood.mat") ??
                                   AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
                if (doorMat != null) leafObj.GetComponent<MeshRenderer>().sharedMaterial = doorMat;
            }

            var idoor = doorObj.GetComponent<InteractiveDoor>();
            if (idoor == null) idoor = doorObj.AddComponent<InteractiveDoor>();

            var so = new SerializedObject(idoor);
            so.FindProperty("doorHinge").objectReferenceValue = leafObj.transform;
            so.FindProperty("doorName").stringValue = doorTitle;
            so.FindProperty("openAngle").floatValue = angle;
            so.ApplyModifiedProperties();

            // Clear static navigation flags and set leaf collider to trigger to guarantee open NavMesh passage
            GameObjectUtility.SetStaticEditorFlags(doorObj, 0);
            GameObjectUtility.SetStaticEditorFlags(leafObj, 0);
            var leafCol = leafObj.GetComponent<Collider>();
            if (leafCol != null) leafCol.isTrigger = true;

            BoxCollider bc = doorObj.GetComponent<BoxCollider>();
            if (bc == null) bc = doorObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = new Vector3(0.45f, 1.0f, 0f);
            bc.size = new Vector3(2.0f, 2.2f, 2.0f);
        }
#endif
    }
}
