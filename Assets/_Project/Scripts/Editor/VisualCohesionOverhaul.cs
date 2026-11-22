using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    public static class VisualCohesionOverhaul
    {
        public const string TripoFolder = "Assets/_Project/Models/TripoGenerated";
        public const string ExternalFolder = "Assets/_Project/ExternalModels";

        [MenuItem("ProjectSurvival/Overhaul Visual Cohesion (Tripo & Sculpted 3D)", false, 21)]
        public static void OverhaulAllMenuItem()
        {
            OverhaulAll(true);
        }

        public static void OverhaulAll(bool saveScene = true)
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING VISUAL COHESION OVERHAUL (TRIPO & SCULPTED 3D) <<<");
            Debug.Log("==================================================================");

            // 1. Priority Auto-Binding of Tripo .glb / .fbx Models
            AutoBindTripoModels();

            // 2. Upgrade Sculpted Anatomical Humanoids (Player & Story NPCs)
            UpgradeAnatomicalCharacters();

            // 3. Upgrade Multi-Part Sculpted Vehicle Fleet (Preserving VehicleInteractable)
            UpgradeVehicleFleet();

            // 4. Upgrade Organic Street Trees & Vegetation
            UpgradeStreetTrees();

            if (saveScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[VisualCohesionOverhaul] Scene saved successfully: {scene.path}");
            }

            Debug.Log("[VisualCohesionOverhaul] Visual Cohesion Overhaul Completed!");
        }

        #region 1. Tripo GLB Auto-Binding

        public static void AutoBindTripoModels()
        {
            int boundCount = 0;

            // A. Loot Containers
            GameObject lootGlb = LoadModelAsset("Loot_Crate.glb");
            if (lootGlb != null)
            {
                var lootPrefabs = new string[]
                {
                    "Assets/_Project/Prefabs/Environment/LootContainer_Root.prefab"
                };

                for (int i = 0; i < lootPrefabs.Length; i++)
                {
                    if (File.Exists(lootPrefabs[i]))
                    {
                        if (DressRootPrefab(lootPrefabs[i], lootGlb, 1.0f)) boundCount++;
                    }
                }
            }

            // B. Road Barricades
            GameObject barricadeGlb = LoadModelAsset("Barricade.glb");
            if (barricadeGlb != null)
            {
                var barricades = GameObject.FindGameObjectsWithTag("Untagged");
                for (int i = 0; i < barricades.Length; i++)
                {
                    if (barricades[i].name.StartsWith("Concrete_Barrier") || barricades[i].name.StartsWith("Barricade_T_"))
                    {
                        Transform vm = barricades[i].transform.Find("Visual_Model");
                        if (vm == null)
                        {
                            GameObject vObj = new GameObject("Visual_Model");
                            vObj.transform.SetParent(barricades[i].transform, false);
                            vm = vObj.transform;
                        }

                        // Remove primitive children
                        ClearChildren(vm);
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(barricadeGlb);
                        instance.transform.SetParent(vm, false);
                        NormalizeModelBounds(instance, 1.2f, groundToZero: true);
                        EnsureUrpLit(instance);
                        boundCount++;
                    }
                }
            }

            // C. Car Wrecks
            GameObject wreckGlb = LoadModelAsset("Car_Wreck.glb");
            if (wreckGlb != null)
            {
                GameObject overturnedSuv = GameObject.Find("Vehicle_SUV_Overturned");
                if (overturnedSuv != null)
                {
                    Transform vm = overturnedSuv.transform.Find("Visual_Model");
                    if (vm == null)
                    {
                        GameObject vObj = new GameObject("Visual_Model");
                        vObj.transform.SetParent(overturnedSuv.transform, false);
                        vm = vObj.transform;
                    }
                    ClearChildren(vm);
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(wreckGlb);
                    instance.transform.SetParent(vm, false);
                    NormalizeModelBounds(instance, 1.7f, groundToZero: true);
                    EnsureUrpLit(instance);
                    boundCount++;
                }
            }

            Debug.Log($"[VisualCohesionOverhaul] Auto-Bound {boundCount} Tripo 3D models into scene & prefabs.");
        }

        private static bool DressRootPrefab(string prefabPath, GameObject glbAsset, float targetHeight)
        {
            try
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) return false;

                Transform vm = root.transform.Find("Visual_Model");
                if (vm == null)
                {
                    GameObject vObj = new GameObject("Visual_Model");
                    vObj.transform.SetParent(root.transform, false);
                    vm = vObj.transform;
                }

                ClearChildren(vm);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbAsset);
                instance.transform.SetParent(vm, false);
                NormalizeModelBounds(instance, targetHeight, groundToZero: true);
                EnsureUrpLit(instance);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VisualCohesionOverhaul] Could not dress prefab '{prefabPath}': {ex.Message}");
                return false;
            }
        }

        private static GameObject LoadModelAsset(string filename)
        {
            string p1 = Path.Combine(TripoFolder, filename);
            if (File.Exists(p1)) return AssetDatabase.LoadAssetAtPath<GameObject>(p1);

            string p2 = Path.Combine(ExternalFolder, filename);
            if (File.Exists(p2)) return AssetDatabase.LoadAssetAtPath<GameObject>(p2);

            return null;
        }

        private static void NormalizeModelBounds(GameObject obj, float targetHeight, bool groundToZero)
        {
            Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                b.Encapsulate(rends[i].bounds);
            }

            if (b.size.y > 0.05f && targetHeight > 0f)
            {
                float scale = targetHeight / b.size.y;
                obj.transform.localScale = Vector3.one * scale;
            }

            if (groundToZero)
            {
                // Re-calculate after scaling
                Bounds bScaled = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    bScaled.Encapsulate(rends[i].bounds);
                }
                float yOffset = -bScaled.min.y;
                obj.transform.localPosition = new Vector3(0f, yOffset, 0f);
            }
        }

        private static void EnsureUrpLit(GameObject obj)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) return;

            Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null && (mats[m].shader == null || mats[m].shader.name != "Universal Render Pipeline/Lit"))
                    {
                        mats[m].shader = litShader;
                        mats[m].enableInstancing = true;
                    }
                }
                rends[i].sharedMaterials = mats;
            }
        }

        #endregion

        #region 2. Anatomical Humanoid Survivor & NPC Models

        public static void UpgradeAnatomicalCharacters()
        {
            // Upgrade Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                UpgradeHumanoidVisual(player.transform, "Player_Visual", isPlayer: true, HumanoidArchetype.Player);
            }

            // Upgrade NPCs
            GameObject aras = GameObject.Find("NPC_DrAras_Root") ?? GameObject.Find("NPC_DrAras");
            if (aras != null) UpgradeHumanoidVisual(aras.transform, "DrAras_Visual", isPlayer: false, HumanoidArchetype.DrAras);

            GameObject zeynep = GameObject.Find("NPC_Zeynep_Root") ?? GameObject.Find("NPC_Zeynep");
            if (zeynep != null) UpgradeHumanoidVisual(zeynep.transform, "Zeynep_Visual", isPlayer: false, HumanoidArchetype.Zeynep);

            GameObject kerem = GameObject.Find("NPC_Kerem_Root") ?? GameObject.Find("NPC_Kerem");
            if (kerem != null) UpgradeHumanoidVisual(kerem.transform, "Kerem_Visual", isPlayer: false, HumanoidArchetype.Kerem);

            GameObject elif = GameObject.Find("NPC_Elif_Root") ?? GameObject.Find("NPC_Elif");
            if (elif != null) UpgradeHumanoidVisual(elif.transform, "Elif_Visual", isPlayer: false, HumanoidArchetype.Elif);

            Debug.Log("[VisualCohesionOverhaul] Upgraded Anatomical Humanoid Survivor & NPC Models.");
        }

        public enum HumanoidArchetype
        {
            Player,
            DrAras,
            Zeynep,
            Kerem,
            Elif
        }

        private static void UpgradeHumanoidVisual(Transform root, string visualName, bool isPlayer, HumanoidArchetype archetype)
        {
            Transform vm = root.Find("Visual_Model");
            if (vm == null)
            {
                GameObject vObj = new GameObject("Visual_Model");
                vObj.transform.SetParent(root, false);
                vm = vObj.transform;
            }

            // Clean old primitive capsule limbs
            ClearChildren(vm);

            // Fetch materials
            Material skinMat = SceneBuilder.GetOrCreateMaterial("Mat_Skin_Human", new Color(0.92f, 0.78f, 0.69f));
            Material fabricMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Tactical_Fabric.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Tactical_Fabric", new Color(0.24f, 0.28f, 0.32f));
            Material leatherMat = SceneBuilder.GetOrCreateMaterial("Mat_Leather_Dark", new Color(0.18f, 0.15f, 0.12f));
            Material bootMat = SceneBuilder.GetOrCreateMaterial("Mat_Boots_Rubber", new Color(0.12f, 0.12f, 0.14f));

            // Character-specific wardrobe materials
            Material jacketMat;
            Material pantsMat = SceneBuilder.GetOrCreateMaterial("Mat_Cargo_Charcoal", new Color(0.18f, 0.20f, 0.24f));

            switch (archetype)
            {
                case HumanoidArchetype.DrAras:
                    jacketMat = SceneBuilder.GetOrCreateMaterial("Mat_LabCoat_White", new Color(0.92f, 0.94f, 0.96f));
                    pantsMat = SceneBuilder.GetOrCreateMaterial("Mat_Scrubs_Teal", new Color(0.22f, 0.45f, 0.48f));
                    break;
                case HumanoidArchetype.Zeynep:
                    jacketMat = SceneBuilder.GetOrCreateMaterial("Mat_Parka_DarkSlate", new Color(0.18f, 0.22f, 0.26f));
                    pantsMat = SceneBuilder.GetOrCreateMaterial("Mat_Pants_OliveDark", new Color(0.24f, 0.26f, 0.20f));
                    break;
                case HumanoidArchetype.Kerem:
                    jacketMat = SceneBuilder.GetOrCreateMaterial("Mat_Vest_WorkUtility", new Color(0.45f, 0.38f, 0.28f));
                    break;
                case HumanoidArchetype.Elif:
                    jacketMat = SceneBuilder.GetOrCreateMaterial("Mat_Jacket_CourierCrimson", new Color(0.68f, 0.18f, 0.18f));
                    break;
                default:
                    jacketMat = SceneBuilder.GetOrCreateMaterial("Mat_Jacket_TacticalNavy", new Color(0.16f, 0.24f, 0.35f));
                    break;
            }

            // Construct 1:7.5 Anatomical Silhouette
            GameObject bodyHips = new GameObject("Body_Hips");
            bodyHips.transform.SetParent(vm, false);
            bodyHips.transform.localPosition = new Vector3(0f, 0.88f, 0f);

            // 1. Pelvis & Tactical Belt
            CreateSculptedBox(bodyHips.transform, "Pelvis", new Vector3(0f, 0.02f, 0f), new Vector3(0.32f, 0.16f, 0.22f), pantsMat);
            CreateSculptedBox(bodyHips.transform, "Duty_Belt", new Vector3(0f, 0.10f, 0f), new Vector3(0.35f, 0.06f, 0.24f), leatherMat);
            CreateSculptedBox(bodyHips.transform, "Holster_Pistol", new Vector3(0.20f, 0.02f, 0f), new Vector3(0.08f, 0.18f, 0.12f), leatherMat);

            // 2. Sculpted Torso & Collar Lapels
            GameObject spine = new GameObject("Spine");
            spine.transform.SetParent(bodyHips.transform, false);
            spine.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            CreateSculptedBox(spine.transform, "Chest_Torso", new Vector3(0f, 0.20f, 0f), new Vector3(0.38f, 0.36f, 0.25f), jacketMat);
            CreateSculptedBox(spine.transform, "Collar_L", new Vector3(-0.10f, 0.38f, 0.08f), new Vector3(0.06f, 0.08f, 0.12f), jacketMat);
            CreateSculptedBox(spine.transform, "Collar_R", new Vector3(0.10f, 0.38f, 0.08f), new Vector3(0.06f, 0.08f, 0.12f), jacketMat);

            // Tactical Chest Rig (Player & Kerem)
            if (isPlayer || archetype == HumanoidArchetype.Kerem)
            {
                CreateSculptedBox(spine.transform, "Chest_Rig", new Vector3(0f, 0.18f, 0.13f), new Vector3(0.28f, 0.20f, 0.06f), fabricMat);
                CreateSculptedBox(spine.transform, "Pouch_Mag1", new Vector3(-0.08f, 0.16f, 0.17f), new Vector3(0.06f, 0.12f, 0.04f), fabricMat);
                CreateSculptedBox(spine.transform, "Pouch_Mag2", new Vector3(0.08f, 0.16f, 0.17f), new Vector3(0.06f, 0.12f, 0.04f), fabricMat);
            }

            // Stethoscope (Dr. Aras)
            if (archetype == HumanoidArchetype.DrAras)
            {
                Material stethMat = SceneBuilder.GetOrCreateMaterial("Mat_Stethoscope", new Color(0.15f, 0.15f, 0.15f));
                CreateSculptedBox(spine.transform, "Stethoscope_Neck", new Vector3(0f, 0.35f, 0.12f), new Vector3(0.22f, 0.04f, 0.08f), stethMat);
                CreateSculptedBox(spine.transform, "Stethoscope_Chest", new Vector3(0.06f, 0.16f, 0.14f), new Vector3(0.05f, 0.05f, 0.03f), SceneBuilder.GetOrCreateMaterial("Mat_Chrome_Metal", new Color(0.8f, 0.82f, 0.85f)));
            }

            // Hooded Parka Cowl & Scarf (Zeynep)
            if (archetype == HumanoidArchetype.Zeynep)
            {
                Material scarfMat = SceneBuilder.GetOrCreateMaterial("Mat_Scarf_Burgundy", new Color(0.45f, 0.15f, 0.18f));
                CreateSculptedBox(spine.transform, "Parka_Hood_Back", new Vector3(0f, 0.38f, -0.10f), new Vector3(0.28f, 0.22f, 0.14f), jacketMat);
                CreateSculptedBox(spine.transform, "Warm_Scarf", new Vector3(0f, 0.36f, 0.06f), new Vector3(0.26f, 0.10f, 0.16f), scarfMat);
            }

            // Crossbody Courier Bag (Elif)
            if (archetype == HumanoidArchetype.Elif)
            {
                Material bagMat = SceneBuilder.GetOrCreateMaterial("Mat_Courier_Bag", new Color(0.22f, 0.20f, 0.18f));
                CreateSculptedBox(spine.transform, "Bag_Strap", new Vector3(0f, 0.22f, 0f), new Vector3(0.05f, 0.38f, 0.28f), leatherMat, Quaternion.Euler(0f, 0f, 35f));
                CreateSculptedBox(bodyHips.transform, "Courier_Bag", new Vector3(-0.22f, 0.06f, 0f), new Vector3(0.12f, 0.22f, 0.26f), bagMat);
            }

            // 3. Sculpted Head & Neck
            GameObject neck = new GameObject("Neck");
            neck.transform.SetParent(spine.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.40f, 0f);

            CreateSculptedCylinder(neck.transform, "Neck_Mesh", new Vector3(0f, 0.04f, 0f), new Vector3(0.14f, 0.08f, 0.14f), skinMat);

            GameObject head = new GameObject("Head");
            head.transform.SetParent(neck.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            CreateSculptedBox(head.transform, "Cranium", new Vector3(0f, 0.10f, 0f), new Vector3(0.19f, 0.20f, 0.21f), skinMat);
            CreateSculptedBox(head.transform, "Jaw", new Vector3(0f, 0.02f, 0.03f), new Vector3(0.16f, 0.10f, 0.16f), skinMat);
            CreateSculptedBox(head.transform, "Nose", new Vector3(0f, 0.06f, 0.12f), new Vector3(0.04f, 0.06f, 0.04f), skinMat);

            Material hairMat = SceneBuilder.GetOrCreateMaterial("Mat_Hair_Dark", new Color(0.15f, 0.12f, 0.10f));
            CreateSculptedBox(head.transform, "Hair_Silhouette", new Vector3(0f, 0.15f, -0.02f), new Vector3(0.21f, 0.12f, 0.23f), hairMat);

            // Glasses (Dr. Aras)
            if (archetype == HumanoidArchetype.DrAras)
            {
                Material wireMat = SceneBuilder.GetOrCreateMaterial("Mat_Glasses_Wire", new Color(0.35f, 0.35f, 0.35f));
                CreateSculptedBox(head.transform, "Glasses_Rim", new Vector3(0f, 0.08f, 0.12f), new Vector3(0.16f, 0.04f, 0.02f), wireMat);
            }

            // Work Cap (Kerem)
            if (archetype == HumanoidArchetype.Kerem)
            {
                Material capMat = SceneBuilder.GetOrCreateMaterial("Mat_Work_Cap", new Color(0.25f, 0.28f, 0.32f));
                CreateSculptedBox(head.transform, "Cap_Crown", new Vector3(0f, 0.20f, 0f), new Vector3(0.22f, 0.08f, 0.23f), capMat);
                CreateSculptedBox(head.transform, "Cap_Visor", new Vector3(0f, 0.16f, 0.14f), new Vector3(0.18f, 0.02f, 0.10f), capMat);
            }

            // 4. Articulated Arms & Gloved Hands
            CreateArticulatedArm(spine.transform, "Arm_L", -1f, jacketMat, skinMat, leatherMat);
            CreateArticulatedArm(spine.transform, "Arm_R", 1f, jacketMat, skinMat, leatherMat);

            // 5. Cargo Trousers & Treaded Boots
            CreateArticulatedLeg(bodyHips.transform, "Leg_L", -0.10f, pantsMat, bootMat);
            CreateArticulatedLeg(bodyHips.transform, "Leg_R", 0.10f, pantsMat, bootMat);
        }

        private static void CreateArticulatedArm(Transform spine, string name, float side, Material sleeveMat, Material skinMat, Material gloveMat)
        {
            GameObject arm = new GameObject(name);
            arm.transform.SetParent(spine, false);
            arm.transform.localPosition = new Vector3(side * 0.24f, 0.34f, 0f);

            // Shoulder & Bicep
            CreateSculptedBox(arm.transform, "UpperArm", new Vector3(0f, -0.12f, 0f), new Vector3(0.12f, 0.24f, 0.13f), sleeveMat);

            // Forearm
            GameObject forearm = new GameObject("Forearm");
            forearm.transform.SetParent(arm.transform, false);
            forearm.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            CreateSculptedBox(forearm.transform, "Forearm_Mesh", new Vector3(0f, -0.12f, 0f), new Vector3(0.11f, 0.22f, 0.11f), sleeveMat);

            // Gloved Hand
            GameObject hand = new GameObject("Hand");
            hand.transform.SetParent(forearm.transform, false);
            hand.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            CreateSculptedBox(hand.transform, "Palm", new Vector3(0f, -0.06f, 0f), new Vector3(0.08f, 0.10f, 0.05f), gloveMat);
            CreateSculptedBox(hand.transform, "Thumb", new Vector3(side * 0.04f, -0.04f, 0.02f), new Vector3(0.03f, 0.06f, 0.03f), gloveMat);
        }

        private static void CreateArticulatedLeg(Transform hips, string name, float sideX, Material pantsMat, Material bootMat)
        {
            GameObject leg = new GameObject(name);
            leg.transform.SetParent(hips, false);
            leg.transform.localPosition = new Vector3(sideX, -0.08f, 0f);

            // Thigh with cargo pocket fold
            CreateSculptedBox(leg.transform, "Thigh", new Vector3(0f, -0.20f, 0f), new Vector3(0.16f, 0.38f, 0.17f), pantsMat);
            CreateSculptedBox(leg.transform, "Cargo_Pocket", new Vector3(Mathf.Sign(sideX) * 0.09f, -0.20f, 0f), new Vector3(0.04f, 0.14f, 0.12f), pantsMat);

            // Shin / Calf
            GameObject shin = new GameObject("Shin");
            shin.transform.SetParent(leg.transform, false);
            shin.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            CreateSculptedBox(shin.transform, "Calf", new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.34f, 0.14f), pantsMat);

            // Combat Boot
            GameObject boot = new GameObject("Boot");
            boot.transform.SetParent(shin.transform, false);
            boot.transform.localPosition = new Vector3(0f, -0.36f, 0f);
            CreateSculptedBox(boot.transform, "Boot_Ankle", new Vector3(0f, 0.06f, 0f), new Vector3(0.13f, 0.12f, 0.14f), bootMat);
            CreateSculptedBox(boot.transform, "Boot_Foot", new Vector3(0f, 0.02f, 0.05f), new Vector3(0.13f, 0.08f, 0.24f), bootMat);
            CreateSculptedBox(boot.transform, "Tread_Sole", new Vector3(0f, -0.03f, 0.05f), new Vector3(0.14f, 0.03f, 0.25f), bootMat);
        }

        #endregion

        #region 3. Multi-Part Sculpted 3D Vehicle Fleet

        public static void UpgradeVehicleFleet()
        {
            Transform fleetParent = GameObject.Find("--- VEHICLE FLEET ---")?.transform;
            if (fleetParent == null) return;

            Material matPolice = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_Police.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Auto_Police", new Color(0.12f, 0.13f, 0.15f));
            Material matAmbulance = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_Ambulance.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Auto_Ambulance", new Color(0.92f, 0.92f, 0.95f));
            Material matTaxi = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_Taxi.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Auto_Taxi", new Color(0.95f, 0.72f, 0.10f));
            Material matRust = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_RustMetal.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Auto_RustMetal", new Color(0.38f, 0.22f, 0.16f));
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Glass_DustyReflective.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Glass_DustyReflective", new Color(0.72f, 0.85f, 0.95f, 0.55f));
            Material matTire = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));
            Material matRim = SceneBuilder.GetOrCreateMaterial("Mat_Chrome_Metal", new Color(0.75f, 0.78f, 0.82f));

            for (int i = 0; i < fleetParent.childCount; i++)
            {
                Transform car = fleetParent.GetChild(i);
                string n = car.name.ToLower();

                // Check if vehicle has primitive box children
                if (car.Find("Chassis") != null || car.Find("Cabin") != null || car.Find("Cab") != null || car.Find("Trailer") != null || car.Find("CargoArea") != null)
                {
                    // Select appropriate livery
                    Material liveryMat = matRust;
                    bool isPolice = n.Contains("police");
                    bool isAmbulance = n.Contains("ambulance");
                    bool isTaxi = n.Contains("taxi");
                    bool isVan = n.Contains("van");
                    bool isTruck = n.Contains("truck");

                    if (isPolice) liveryMat = matPolice;
                    else if (isAmbulance) liveryMat = matAmbulance;
                    else if (isTaxi) liveryMat = matTaxi;

                    // Clean old blocky boxes while preserving VehicleInteractable and BoxColliders
                    var toDelete = new List<GameObject>();
                    for (int c = 0; c < car.childCount; c++)
                    {
                        Transform child = car.GetChild(c);
                        string cn = child.name;
                        if (cn == "Chassis" || cn == "Cabin" || cn == "Cab" || cn == "CargoArea" || cn == "Trailer" || cn == "Grille"
                            || cn.StartsWith("Hood_") || cn.StartsWith("Trunk_")
                            || cn.StartsWith("Door_") || cn == "Windshield" || cn == "RearWindow" || cn.StartsWith("Wheel_")
                            || cn == "PushBar" || cn.StartsWith("Siren_") || cn.StartsWith("Exhaust_") || cn == "Bullbar" || cn == "RoofRack" || cn == "SpareTire")
                        {
                            toDelete.Add(child.gameObject);
                        }
                    }
                    for (int d = 0; d < toDelete.Count; d++)
                    {
                        UnityEngine.Object.DestroyImmediate(toDelete[d]);
                    }

                    // Build Sculpted Automotive Body
                    BuildSculptedCarBody(car, liveryMat, matGlass, matTire, matRim, isPolice, isAmbulance, isTaxi, isVan, isTruck);
                }
            }

            Debug.Log("[VisualCohesionOverhaul] Upgraded Multi-Part Sculpted Vehicle Fleet.");
        }

        private static void BuildSculptedCarBody(Transform car, Material bodyMat, Material glassMat, Material tireMat, Material rimMat,
            bool isPolice, bool isAmbulance, bool isTaxi, bool isVan, bool isTruck)
        {
            float length = isTruck ? 8.5f : (isVan || isAmbulance ? 5.2f : 4.6f);
            float width = isTruck ? 2.5f : 2.05f;
            float cabinHeight = isVan || isAmbulance ? 1.45f : 0.85f;

            // 1. Sleek Contoured Lower Body (Rocker panels, hood slope, wheel arches)
            CreateSculptedBox(car, "Body_Chassis", new Vector3(0f, 0.45f, 0f), new Vector3(width, 0.45f, length), bodyMat);
            CreateSculptedBox(car, "Body_Hood", new Vector3(0f, 0.72f, length * 0.28f), new Vector3(width * 0.92f, 0.22f, length * 0.32f), bodyMat, Quaternion.Euler(4f, 0f, 0f));
            CreateSculptedBox(car, "Body_TrunkHinged", new Vector3(0f, 0.74f, -length * 0.32f), new Vector3(width * 0.90f, 0.24f, length * 0.26f), bodyMat);

            // 2. Angled Cabin Greenhouse & Reflective Windows
            CreateSculptedBox(car, "Cabin_Roof", new Vector3(0f, 0.85f + cabinHeight, -length * 0.05f), new Vector3(width * 0.85f, 0.08f, length * 0.50f), bodyMat);
            CreateSculptedBox(car, "Windshield_Angled", new Vector3(0f, 0.95f + cabinHeight * 0.4f, length * 0.18f), new Vector3(width * 0.82f, cabinHeight * 0.85f, 0.05f), glassMat, Quaternion.Euler(34f, 0f, 0f));
            CreateSculptedBox(car, "RearWindow_Angled", new Vector3(0f, 0.95f + cabinHeight * 0.4f, -length * 0.28f), new Vector3(width * 0.82f, cabinHeight * 0.85f, 0.05f), glassMat, Quaternion.Euler(-34f, 0f, 0f));
            CreateSculptedBox(car, "SideGlass_L", new Vector3(-width * 0.43f, 0.95f + cabinHeight * 0.4f, -length * 0.05f), new Vector3(0.04f, cabinHeight * 0.75f, length * 0.44f), glassMat);
            CreateSculptedBox(car, "SideGlass_R", new Vector3(width * 0.43f, 0.95f + cabinHeight * 0.4f, -length * 0.05f), new Vector3(0.04f, cabinHeight * 0.75f, length * 0.44f), glassMat);

            // 3. Front Honeycomb Grille & Headlights / Taillights
            Material grilleMat = SceneBuilder.GetOrCreateMaterial("Mat_Grille_Black", new Color(0.10f, 0.10f, 0.10f));
            Material lightHeadMat = SceneBuilder.GetOrCreateMaterial("Mat_Car_Headlight", new Color(0.95f, 0.95f, 1.0f));
            Material lightTailMat = SceneBuilder.GetOrCreateMaterial("Mat_Car_Taillight", new Color(0.85f, 0.12f, 0.12f));

            CreateSculptedBox(car, "Grille_Front", new Vector3(0f, 0.50f, length * 0.505f), new Vector3(width * 0.65f, 0.24f, 0.05f), grilleMat);
            CreateSculptedBox(car, "Headlight_L", new Vector3(-width * 0.38f, 0.54f, length * 0.502f), new Vector3(0.24f, 0.14f, 0.06f), lightHeadMat);
            CreateSculptedBox(car, "Headlight_R", new Vector3(width * 0.38f, 0.54f, length * 0.502f), new Vector3(0.24f, 0.14f, 0.06f), lightHeadMat);
            CreateSculptedBox(car, "Taillight_L", new Vector3(-width * 0.38f, 0.60f, -length * 0.502f), new Vector3(0.24f, 0.12f, 0.06f), lightTailMat);
            CreateSculptedBox(car, "Taillight_R", new Vector3(width * 0.38f, 0.60f, -length * 0.502f), new Vector3(0.24f, 0.12f, 0.06f), lightTailMat);

            // Front & Rear Bumpers
            Material bumperMat = SceneBuilder.GetOrCreateMaterial("Mat_Bumper_Dark", new Color(0.18f, 0.18f, 0.20f));
            CreateSculptedBox(car, "Bumper_Front", new Vector3(0f, 0.32f, length * 0.515f), new Vector3(width * 1.02f, 0.20f, 0.16f), bumperMat);
            CreateSculptedBox(car, "Bumper_Rear", new Vector3(0f, 0.34f, -length * 0.515f), new Vector3(width * 1.02f, 0.20f, 0.16f), bumperMat);

            // Side Mirrors
            CreateSculptedBox(car, "Mirror_L", new Vector3(-width * 0.48f, 0.88f, length * 0.12f), new Vector3(0.14f, 0.10f, 0.18f), bodyMat);
            CreateSculptedBox(car, "Mirror_R", new Vector3(width * 0.48f, 0.88f, length * 0.12f), new Vector3(0.14f, 0.10f, 0.18f), bodyMat);

            // 4. 4 Separate Rubber Tires + Alloy 5-Spoke Rims
            float wheelZ = length * 0.32f;
            float wheelX = width * 0.50f;
            CreateWheelAssembly(car, "Wheel_FL", new Vector3(-wheelX, 0.35f, wheelZ), tireMat, rimMat);
            CreateWheelAssembly(car, "Wheel_FR", new Vector3(wheelX, 0.35f, wheelZ), tireMat, rimMat);
            CreateWheelAssembly(car, "Wheel_RL", new Vector3(-wheelX, 0.35f, -wheelZ), tireMat, rimMat);
            CreateWheelAssembly(car, "Wheel_RR", new Vector3(wheelX, 0.35f, -wheelZ), tireMat, rimMat);

            // 5. Special Fixtures
            if (isPolice)
            {
                Material sirenRed = SceneBuilder.GetOrCreateMaterial("Mat_Siren_Red", new Color(1.0f, 0.12f, 0.12f));
                Material sirenBlue = SceneBuilder.GetOrCreateMaterial("Mat_Siren_Blue", new Color(0.12f, 0.35f, 1.0f));
                CreateSculptedBox(car, "Lightbar_Red", new Vector3(-0.35f, 1.02f + cabinHeight, -length * 0.05f), new Vector3(0.45f, 0.12f, 0.24f), sirenRed);
                CreateSculptedBox(car, "Lightbar_Blue", new Vector3(0.35f, 1.02f + cabinHeight, -length * 0.05f), new Vector3(0.45f, 0.12f, 0.24f), sirenBlue);
            }
            else if (isTaxi)
            {
                Material taxiSignMat = SceneBuilder.GetOrCreateMaterial("Mat_Taxi_Sign", new Color(1.0f, 0.90f, 0.25f));
                CreateSculptedBox(car, "Taxi_RoofWedge", new Vector3(0f, 1.02f + cabinHeight, -length * 0.05f), new Vector3(0.55f, 0.16f, 0.22f), taxiSignMat);
            }
        }

        private static void CreateWheelAssembly(Transform parent, string name, Vector3 pos, Material tireMat, Material rimMat)
        {
            GameObject wheel = new GameObject(name);
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = pos;

            // Rubber Tire
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Rubber_Tire";
            tire.transform.SetParent(wheel.transform, false);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = new Vector3(0.72f, 0.18f, 0.72f);
            if (tireMat != null) tire.GetComponent<Renderer>().sharedMaterial = tireMat;

            // Alloy Rim Hub
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Alloy_Rim";
            rim.transform.SetParent(wheel.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(0.48f, 0.19f, 0.48f);
            if (rimMat != null) rim.GetComponent<Renderer>().sharedMaterial = rimMat;
        }

        #endregion

        #region 4. Organic Street Trees & Vegetation

        public static void UpgradeStreetTrees()
        {
            Transform env = (GameObject.Find("Environment") ?? GameObject.Find("--- ENVIRONMENT ---"))?.transform;
            if (env == null) return;

            Transform treeParent = env.Find("--- ORGANIC STREET TREES & URBAN VEGETATION ---");
            if (treeParent == null)
            {
                GameObject tp = new GameObject("--- ORGANIC STREET TREES & URBAN VEGETATION ---");
                tp.transform.SetParent(env);
                treeParent = tp.transform;
            }

            Material barkMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Trunk", new Color(0.32f, 0.24f, 0.18f));
            Material leavesMat = SceneBuilder.GetOrCreateMaterial("Mat_Tree_Foliage", new Color(0.24f, 0.38f, 0.22f));

            var treePositions = new (string name, Vector3 pos)[]
            {
                ("Tree_Boulevard_W1", new Vector3(-8.8f, 0.12f, -60f)),
                ("Tree_Boulevard_E1", new Vector3(8.8f, 0.12f, -40f)),
                ("Tree_Boulevard_W2", new Vector3(-8.8f, 0.12f, -20f)),
                ("Tree_Boulevard_E2", new Vector3(8.8f, 0.12f, 20f)),
                ("Tree_Boulevard_W3", new Vector3(-8.8f, 0.12f, 40f)),
                ("Tree_Boulevard_E3", new Vector3(8.8f, 0.12f, 60f)),
                ("Tree_Residential_W1", new Vector3(-48f, 0.12f, -20f)),
                ("Tree_Residential_W2", new Vector3(-48f, 0.12f, 20f)),
                ("Tree_Residential_W3", new Vector3(-65f, 0.12f, -10f)),
                ("Tree_Residential_W4", new Vector3(-65f, 0.12f, 15f)),
                ("Tree_Clinic_Lawn", new Vector3(-22f, 0.12f, 22f)),
                ("Tree_Police_Lawn", new Vector3(-22f, 0.12f, -10f))
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                var tp = treePositions[i];
                if (treeParent.Find(tp.name) == null)
                {
                    GameObject treeObj = new GameObject(tp.name);
                    treeObj.transform.SetParent(treeParent);
                    treeObj.transform.position = tp.pos;
                    BuildSculptedTree(treeObj.transform, barkMat, leavesMat);
                }
            }

            Debug.Log("[VisualCohesionOverhaul] Placed & Upgraded Organic Multi-Branch Street Trees across City.");
        }

        private static void BuildSculptedTree(Transform parent, Material barkMat, Material leavesMat)
        {
            // Lower Trunk (Tapered)
            CreateSculptedCylinder(parent, "Trunk_Base", new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 1.2f, 0.55f), barkMat);
            // Mid Trunk with slight twist
            CreateSculptedCylinder(parent, "Trunk_Mid", new Vector3(0.08f, 2.6f, 0.05f), new Vector3(0.42f, 1.0f, 0.42f), barkMat, Quaternion.Euler(4f, 15f, -3f));

            // Branches
            CreateSculptedCylinder(parent, "Branch_Left", new Vector3(-0.45f, 3.8f, 0.2f), new Vector3(0.24f, 0.9f, 0.24f), barkMat, Quaternion.Euler(24f, 35f, -28f));
            CreateSculptedCylinder(parent, "Branch_Right", new Vector3(0.45f, 3.9f, -0.2f), new Vector3(0.24f, 0.9f, 0.24f), barkMat, Quaternion.Euler(-20f, -40f, 32f));

            // Layered Foliage Clusters (Multi-tiered canopy)
            CreateSculptedBox(parent, "Foliage_Cluster_Main", new Vector3(0f, 4.8f, 0f), new Vector3(3.2f, 2.2f, 3.2f), leavesMat);
            CreateSculptedBox(parent, "Foliage_Cluster_Top", new Vector3(0.1f, 5.8f, -0.1f), new Vector3(2.2f, 1.6f, 2.2f), leavesMat);
            CreateSculptedBox(parent, "Foliage_Cluster_Left", new Vector3(-1.1f, 4.4f, 0.6f), new Vector3(1.8f, 1.4f, 1.8f), leavesMat);
            CreateSculptedBox(parent, "Foliage_Cluster_Right", new Vector3(1.1f, 4.5f, -0.6f), new Vector3(1.8f, 1.4f, 1.8f), leavesMat);
        }

        #endregion

        #region Helpers

        private static void ClearChildren(Transform parent)
        {
            var children = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i).gameObject);
            }
            for (int i = 0; i < children.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(children[i]);
            }
        }

        private static GameObject CreateSculptedBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = size;
            if (rot.HasValue) obj.transform.localRotation = rot.Value;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreateSculptedCylinder(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = size;
            if (rot.HasValue) obj.transform.localRotation = rot.Value;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        #endregion
    }
}
