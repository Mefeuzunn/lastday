using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Environment;
using ProjectSurvival.Core;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 4 (Step 4A): Master Exterior 3D World Upgrader.
    /// Replaces primitive cube buildings, blocky trees, and placeholder street props across Main_Prototype.unity
    /// with genuine CC0 3D meshes (Kenney/Quaternius CC0 models imported via com.unity.cloud.gltfast).
    /// Enforces Universal Render Pipeline (URP/Lit) materials with GPU instancing (enableInstancing = true),
    /// strict mobile 60 FPS single shadow-caster rule, solid weather roof occlusion, and NavMesh obstacle carving.
    /// </summary>
    public static class Exterior3DWorldUpgrader
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Exterior";
        public const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Upgrade Exterior 3D World (Step 4A)", false, 35)]
        public static void UpgradeAllExteriorWorldMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("===============================================================================");
            Debug.Log(">>> EXECUTING ROADMAP PHASE 4 (STEP 4A): EXTERIOR 3D WORLD UPGRADE <<<");
            Debug.Log("===============================================================================");

            UpgradeStarterHouseExterior();
            UpgradeTreesAndFoliage();
            UpgradeStreetPropsAndBarriers();
            UpgradeSurroundingBuildings();
            EnforceURPMaterialsAndInstancing();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(">>> PHASE 4 (STEP 4A) EXTERIOR 3D WORLD UPGRADE COMPLETED & SAVED! <<<");
            Debug.Log("===============================================================================");
        }

        // =========================================================================
        // 1. STARTER HOUSE SAFEHOME EXTERIOR FACADE, PORCH & ROOF
        // =========================================================================
        public static void UpgradeStarterHouseExterior()
        {
            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot == null)
            {
                Debug.LogWarning("[Exterior3DWorldUpgrader] StarterHouse_Safehome not found!");
                return;
            }

            Transform arch = houseRoot.transform.Find("Architecture");
            if (arch == null)
            {
                GameObject archObj = new GameObject("Architecture");
                archObj.transform.SetParent(houseRoot.transform, false);
                arch = archObj.transform;
            }

            // 1. Solid Weather Roof Occlusion (Keep BoxCollider intact for DynamicWeatherManager)
            Transform roofSlab = arch.Find("House_Roof_Slab");
            if (roofSlab != null)
            {
                // Disable primitive cube MeshRenderer so only CC0 detailed roof renders
                MeshRenderer mr = roofSlab.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;

                // Ensure non-trigger BoxCollider remains for weather rain-check raycasts
                BoxCollider bc = roofSlab.GetComponent<BoxCollider>();
                if (bc == null) bc = roofSlab.gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = false;
                bc.size = new Vector3(12f, 0.4f, 10f);

                // Attach architectural CC0 roof modules
                Transform existingRoofVisual = arch.Find("CC0_Roof_Assembly");
                if (existingRoofVisual != null) UnityEngine.Object.DestroyImmediate(existingRoofVisual.gameObject);

                GameObject roofAssembly = new GameObject("CC0_Roof_Assembly");
                roofAssembly.transform.SetParent(arch, false);
                roofAssembly.transform.localPosition = new Vector3(0f, 3.2f, 0f);

                LoadOrInstantiateModel("roof-flat.glb", roofAssembly.transform, new Vector3(-2.8f, 0f, 0f), Vector3.zero, new Vector3(2.5f, 1.2f, 2.5f));
                LoadOrInstantiateModel("roof-center.glb", roofAssembly.transform, new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(2.5f, 1.4f, 2.5f));
                LoadOrInstantiateModel("roof-flat.glb", roofAssembly.transform, new Vector3(2.8f, 0f, 0f), Vector3.zero, new Vector3(2.5f, 1.2f, 2.5f));
            }

            // 2. Porch Columns and Awning
            Transform porchRoot = arch.Find("Porch_Structure");
            if (porchRoot == null)
            {
                GameObject pr = new GameObject("Porch_Structure");
                pr.transform.SetParent(arch, false);
                porchRoot = pr.transform;
            }

            // Remove primitive porch posts if present
            Transform post1 = arch.Find("Porch_Post_1");
            if (post1 != null) UnityEngine.Object.DestroyImmediate(post1.gameObject);
            Transform post2 = arch.Find("Porch_Post_2");
            if (post2 != null) UnityEngine.Object.DestroyImmediate(post2.gameObject);

            Transform existingPorchVisual = porchRoot.Find("CC0_Porch_Assembly");
            if (existingPorchVisual != null) UnityEngine.Object.DestroyImmediate(existingPorchVisual.gameObject);

            GameObject porchAssembly = new GameObject("CC0_Porch_Assembly");
            porchAssembly.transform.SetParent(porchRoot, false);
            porchAssembly.transform.localPosition = new Vector3(0f, 0f, 4.2f);

            // Left and Right real 3D architectural columns
            LoadOrInstantiateModel("wall-column.glb", porchAssembly.transform, new Vector3(-2.4f, 0f, 0.4f), Vector3.zero, new Vector3(1.5f, 2.8f, 1.5f));
            LoadOrInstantiateModel("wall-column.glb", porchAssembly.transform, new Vector3(2.4f, 0f, 0.4f), Vector3.zero, new Vector3(1.5f, 2.8f, 1.5f));

            // Porch pitched awning (protecting entrance from rain)
            LoadOrInstantiateModel("porch-awning.glb", porchAssembly.transform, new Vector3(0f, 2.7f, 0.2f), Vector3.zero, new Vector3(2.2f, 1.5f, 1.8f));

            // Front Yard Boundary Fences (leaving clear 2.8m center gate for player access)
            Transform yardForts = houseRoot.transform.Find("Starter_Yard_Fortifications");
            if (yardForts == null)
            {
                GameObject yf = new GameObject("Starter_Yard_Fortifications");
                yf.transform.SetParent(houseRoot.transform, false);
                yardForts = yf.transform;
            }

            Transform existingFences = yardForts.Find("CC0_FrontYard_Fences");
            if (existingFences != null) UnityEngine.Object.DestroyImmediate(existingFences.gameObject);

            GameObject fencesGroup = new GameObject("CC0_FrontYard_Fences");
            fencesGroup.transform.SetParent(yardForts, false);

            // Left fence line
            GameObject fl = LoadOrInstantiateModel("fence.glb", fencesGroup.transform, new Vector3(-5.5f, 0f, 6.2f), Vector3.zero, Vector3.one);
            if (fl != null) EnsureNavObstacle(fl, new Vector3(3.8f, 1.4f, 0.4f));

            // Right fence line
            GameObject fr = LoadOrInstantiateModel("fence.glb", fencesGroup.transform, new Vector3(5.5f, 0f, 6.2f), Vector3.zero, Vector3.one);
            if (fr != null) EnsureNavObstacle(fr, new Vector3(3.8f, 1.4f, 0.4f));

            Debug.Log("[Exterior3DWorldUpgrader] StarterHouse_Safehome exterior facade, porch columns, roof assembly and yard fences upgraded.");
        }

        // =========================================================================
        // 2. TREES & VEGETATION OVERHAUL (SECTOR_-1_0_WEST, PARKS & SIDEWALKS)
        // =========================================================================
        public static void UpgradeTreesAndFoliage()
        {
            int upgradedTrees = 0;
            int upgradedFoliage = 0;

            // 1. Sector_-1_0_West Forest Trees
            GameObject forestTrees = GameObject.Find("Procedural_Forest_Trees");
            if (forestTrees != null)
            {
                Transform parent = forestTrees.transform;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform treeTf = parent.GetChild(i);
                    bool isPine = treeTf.name.ToLower().Contains("pine");

                    // Strip primitive cylinders/spheres
                    StripPrimitiveMeshes(treeTf, new[] { "Trunk", "Canopy", "Canopy_Tier_1", "Canopy_Tier_2", "Canopy_Tier_3", "Branch" });

                    Transform existingModel = treeTf.Find("CC0_Tree_Model");
                    if (existingModel != null) UnityEngine.Object.DestroyImmediate(existingModel.gameObject);

                    string modelName = isPine ? ((i % 2 == 0) ? "tree_pine.glb" : "tree_pine_round.glb") : "tree_detailed.glb";
                    float scaleFactor = UnityEngine.Random.Range(2.0f, 2.7f);
                    Vector3 treeScale = new Vector3(scaleFactor, scaleFactor * (isPine ? 1.25f : 1.05f), scaleFactor);
                    Vector3 treeRot = new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    GameObject treeModel = LoadOrInstantiateModel(modelName, treeTf, Vector3.zero, treeRot, treeScale);
                    if (treeModel != null)
                    {
                        treeModel.name = "CC0_Tree_Model";
                        EnsureCapsuleCollider(treeTf.gameObject, 0.5f * scaleFactor, 3.5f * scaleFactor);
                        upgradedTrees++;
                    }

                    // Add scattered bushes and rocks in 25% of forest trees
                    if (i % 4 == 0)
                    {
                        string groundProp = (i % 8 == 0) ? "rock.glb" : "bush.glb";
                        Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.6f, 1.6f), 0f, UnityEngine.Random.Range(-1.6f, 1.6f));
                        GameObject undergrowth = LoadOrInstantiateModel(groundProp, treeTf, offset, treeRot, Vector3.one * UnityEngine.Random.Range(1.2f, 1.8f));
                        if (undergrowth != null) upgradedFoliage++;
                    }
                }
            }

            // 2. Street & Boulevard Sidewalk Trees
            string[] streetTreeNames = new[]
            {
                "Tree_Boulevard_W1", "Tree_Boulevard_E1", "Tree_Boulevard_W2", "Tree_Boulevard_E2",
                "Tree_Boulevard_W3", "Tree_Boulevard_E3", "Tree_Residential_W1", "Tree_Residential_W2"
            };

            foreach (var tName in streetTreeNames)
            {
                GameObject treeGo = GameObject.Find(tName);
                if (treeGo != null)
                {
                    StripPrimitiveMeshes(treeGo.transform, new[] { "Trunk", "Canopy", "Stem", "Branch" });
                    Transform ex = treeGo.transform.Find("CC0_Street_Tree");
                    if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                    GameObject model = LoadOrInstantiateModel("tree_suburban.glb", treeGo.transform, Vector3.zero, new Vector3(0f, 45f, 0f), new Vector3(2.2f, 2.4f, 2.2f));
                    if (model != null)
                    {
                        model.name = "CC0_Street_Tree";
                        EnsureCapsuleCollider(treeGo, 0.45f, 3.8f);
                        upgradedTrees++;
                    }
                }
            }

            // 3. Park Trees across outer sectors
            GameObject[] allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                if (go != null && go.name.StartsWith("ParkTree_") && go.transform.Find("CC0_Park_Tree") == null)
                {
                    StripPrimitiveMeshes(go.transform, new[] { "Trunk", "Canopy", "Leaves" });
                    GameObject model = LoadOrInstantiateModel("tree_detailed.glb", go.transform, Vector3.zero, Vector3.zero, new Vector3(2.0f, 2.2f, 2.0f));
                    if (model != null)
                    {
                        model.name = "CC0_Park_Tree";
                        EnsureCapsuleCollider(go, 0.4f, 3.2f);
                        upgradedTrees++;
                    }
                }
            }

            Debug.Log($"[Exterior3DWorldUpgrader] Replaced {upgradedTrees} primitive trees with real CC0 3D trees and {upgradedFoliage} forest undergrowth props.");
        }

        // =========================================================================
        // 3. STREET PROPS, LIGHTS, BARRIERS, DUMPSTERS & BENCHES
        // =========================================================================
        public static void UpgradeStreetPropsAndBarriers()
        {
            int upgradedLamps = 0;
            int upgradedBarriers = 0;
            int upgradedDumpsters = 0;
            int upgradedBenches = 0;

            // 1. Street Lamps (Curved Streetlight Pole)
            GameObject[] allLamps = FindGameObjectsByName("StreetLamp_Pole");
            foreach (var lamp in allLamps)
            {
                StripPrimitiveMeshes(lamp.transform, new[] { "Pole", "Arm", "LampHead", "Bulb", "Housing" });
                Transform ex = lamp.transform.Find("CC0_Streetlight");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                GameObject model = LoadOrInstantiateModel("streetlight.glb", lamp.transform, Vector3.zero, Vector3.zero, new Vector3(1.8f, 2.2f, 1.8f));
                if (model != null)
                {
                    model.name = "CC0_Streetlight";
                    EnsureCapsuleCollider(lamp, 0.25f, 4.5f);
                    EnsureNavObstacle(lamp, new Vector3(0.6f, 4.5f, 0.6f));
                    upgradedLamps++;
                }

                // Strict Mobile 60 FPS Single Shadow-Caster Rule: Ensure secondary streetlight has Shadows: None
                Light lightComp = lamp.GetComponentInChildren<Light>(true);
                if (lightComp != null)
                {
                    lightComp.shadows = LightShadows.None;
                }
            }

            // 2. Concrete Jersey Barriers
            GameObject[] allBarriers = FindGameObjectsByName("Jersey_Barrier");
            foreach (var barrier in allBarriers)
            {
                StripPrimitiveMeshes(barrier.transform, new[] { "Barrier_Base", "Barrier_Wall", "Barrier_Top", "Stripe" });
                Transform ex = barrier.transform.Find("CC0_Barrier");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                GameObject model = LoadOrInstantiateModel("barrier_concrete.glb", barrier.transform, Vector3.zero, Vector3.zero, new Vector3(1.6f, 1.5f, 1.6f));
                if (model != null)
                {
                    model.name = "CC0_Barrier";
                    EnsureBoxCollider(barrier, new Vector3(2.4f, 1.1f, 0.8f));
                    EnsureNavObstacle(barrier, new Vector3(2.6f, 1.2f, 1.0f));
                    upgradedBarriers++;
                }
            }

            // 3. Metal Dumpsters (Hiding Spots)
            GameObject[] allDumpsters = FindGameObjectsByName("MetalDumpster");
            foreach (var dumpster in allDumpsters)
            {
                StripPrimitiveMeshes(dumpster.transform, new[] { "Dumpster_Body", "Dumpster_Lid" });
                Transform ex = dumpster.transform.Find("CC0_Dumpster");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                GameObject model = LoadOrInstantiateModel("dumpster.glb", dumpster.transform, Vector3.zero, Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f));
                if (model != null)
                {
                    model.name = "CC0_Dumpster";
                    EnsureBoxCollider(dumpster, new Vector3(2.2f, 1.4f, 1.3f));
                    EnsureNavObstacle(dumpster, new Vector3(2.4f, 1.5f, 1.5f));
                    upgradedDumpsters++;
                }
            }

            // 4. Street Benches
            GameObject[] allBenches = FindGameObjectsByName("Street_Bench");
            foreach (var bench in allBenches)
            {
                StripPrimitiveMeshes(bench.transform, new[] { "Bench_Seat", "Bench_Leg_Left", "Bench_Leg_Right", "Bench_Back" });
                Transform ex = bench.transform.Find("CC0_Bench");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                GameObject model = LoadOrInstantiateModel("bench.glb", bench.transform, Vector3.zero, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
                if (model != null)
                {
                    model.name = "CC0_Bench";
                    EnsureBoxCollider(bench, new Vector3(1.8f, 0.9f, 0.7f));
                    EnsureNavObstacle(bench, new Vector3(2.0f, 1.0f, 0.9f));
                    upgradedBenches++;
                }
            }

            Debug.Log($"[Exterior3DWorldUpgrader] Upgraded street props: {upgradedLamps} streetlights, {upgradedBarriers} concrete barriers, {upgradedDumpsters} dumpsters, {upgradedBenches} benches.");
        }

        // =========================================================================
        // 4. SURROUNDING DISTRICT BUILDINGS
        // =========================================================================
        public static void UpgradeSurroundingBuildings()
        {
            int upgradedBuildings = 0;

            // Target key commercial and suburban building anchors
            var buildingTargets = new (string objName, string modelFile, Vector3 localScale)[]
            {
                ("Building_GunShop", "building-commercial-a.glb", new Vector3(3.5f, 3.2f, 3.5f)),
                ("Building_Pharmacy", "building-commercial-b.glb", new Vector3(3.2f, 3.2f, 3.2f)),
                ("Building_Supermarket", "building-commercial-c.glb", new Vector3(4.2f, 3.5f, 4.2f)),
                ("Building_PoliceStation", "building-commercial-d.glb", new Vector3(4.5f, 3.8f, 4.5f)),
                ("SafeHouse_Building", "building-suburban-b.glb", new Vector3(3.0f, 3.0f, 3.0f))
            };

            foreach (var target in buildingTargets)
            {
                GameObject bldg = GameObject.Find(target.objName);
                if (bldg != null)
                {
                    Transform ex = bldg.transform.Find("CC0_Building_Facade");
                    if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                    // Map fallback model file if specific commercial tag
                    string actualModel = target.modelFile;
                    if (!File.Exists(Path.Combine(ModelDirectory, actualModel)))
                    {
                        actualModel = "building-a.glb";
                        if (target.objName.Contains("Pharmacy")) actualModel = "building-b.glb";
                        else if (target.objName.Contains("Supermarket")) actualModel = "building-c.glb";
                        else if (target.objName.Contains("PoliceStation")) actualModel = "building-d.glb";
                        else if (target.objName.Contains("SafeHouse")) actualModel = "building-suburban-a.glb";
                    }

                    GameObject model = LoadOrInstantiateModel(actualModel, bldg.transform, Vector3.zero, Vector3.zero, target.localScale);
                    if (model != null)
                    {
                        model.name = "CC0_Building_Facade";
                        upgradedBuildings++;
                    }
                }
            }

            Debug.Log($"[Exterior3DWorldUpgrader] Upgraded {upgradedBuildings} landmark city buildings with CC0 architectural modules.");
        }

        // =========================================================================
        // 5. URP MATERIALS & GPU INSTANCING PASS
        // =========================================================================
        public static void EnforceURPMaterialsAndInstancing()
        {
            int instancedCount = 0;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            Renderer[] rends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in rends)
            {
                if (r == null) continue;
                string goName = r.gameObject.name.ToLower();
                Transform p = r.transform.parent;
                string parentName = p != null ? p.gameObject.name.ToLower() : "";

                bool isExterior = goName.Contains("tree") || goName.Contains("building") || goName.Contains("roof") ||
                                  goName.Contains("porch") || goName.Contains("fence") || goName.Contains("barrier") ||
                                  goName.Contains("streetlight") || goName.Contains("dumpster") || goName.Contains("bench") ||
                                  goName.Contains("bush") || goName.Contains("rock") || goName.Contains("facade") ||
                                  parentName.Contains("sector") || parentName.Contains("procedural_forest");

                if (isExterior)
                {
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;

                        if (urpLit != null && (mat.shader == null || mat.shader.name.Contains("InternalErrorShader") || mat.shader.name.Contains("Standard")))
                        {
                            mat.shader = urpLit;
                        }

                        if (!mat.enableInstancing)
                        {
                            mat.enableInstancing = true;
                            instancedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[Exterior3DWorldUpgrader] Enforced GPU instancing on {instancedCount} exterior materials.");
        }

        // =========================================================================
        // UTILITY & MODEL LOADING HELPERS
        // =========================================================================
        public static GameObject LoadOrInstantiateModel(string modelFileName, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            string assetPath = $"{ModelDirectory}/{modelFileName}";
            GameObject instance = null;

            // 1. Try AssetDatabase prefab load
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }

            // 2. Fallback to direct glTFast load if prefab not yet generated
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

                // Ensure URP Lit and instancing on all child renderers
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                var rends = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (r != null && r.sharedMaterials != null)
                    {
                        foreach (var m in r.sharedMaterials)
                        {
                            if (m != null)
                            {
                                if (urpLit != null && (m.shader == null || m.shader.name.Contains("InternalErrorShader")))
                                {
                                    m.shader = urpLit;
                                }
                                m.enableInstancing = true;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Exterior3DWorldUpgrader] Could not instantiate model '{modelFileName}'!");
            }

            return instance;
        }

        private static void StripPrimitiveMeshes(Transform parent, string[] primitiveKeywords)
        {
            if (parent == null || primitiveKeywords == null) return;

            var allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == null || child == parent) continue;
                string cName = child.name.ToLower();
                foreach (var kw in primitiveKeywords)
                {
                    if (cName.Contains(kw.ToLower()))
                    {
                        var mr = child.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = false;
                        var mf = child.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount <= 24)
                        {
                            mr = child.GetComponent<MeshRenderer>();
                            if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                            UnityEngine.Object.DestroyImmediate(mf);
                        }
                    }
                }
            }
        }

        private static void EnsureCapsuleCollider(GameObject go, float radius, float height)
        {
            if (go == null) return;
            var cc = go.GetComponent<CapsuleCollider>();
            if (cc == null) cc = go.AddComponent<CapsuleCollider>();
            cc.radius = radius;
            cc.height = height;
            cc.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private static void EnsureBoxCollider(GameObject go, Vector3 size)
        {
            if (go == null) return;
            var bc = go.GetComponent<BoxCollider>();
            if (bc == null) bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = new Vector3(0f, size.y * 0.5f, 0f);
        }

        private static void EnsureNavObstacle(GameObject go, Vector3 size)
        {
            if (go == null) return;
            var nmo = go.GetComponent<NavMeshObstacle>();
            if (nmo == null) nmo = go.AddComponent<NavMeshObstacle>();
            nmo.carving = true;
            nmo.shape = NavMeshObstacleShape.Box;
            nmo.size = size;
            nmo.center = new Vector3(0f, size.y * 0.5f, 0f);
        }

        private static GameObject[] FindGameObjectsByName(string targetName)
        {
            List<GameObject> matches = new List<GameObject>();
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in all)
            {
                if (go != null && go.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(go);
                }
            }
            return matches.ToArray();
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class Exterior3DWorldAutoUpgrader
    {
        private const string UpgradedPrefKey = "Phase4_Step4A_Exterior3D_Upgraded_v1";

        static Exterior3DWorldAutoUpgrader()
        {
            EditorApplication.delayCall += CheckAndUpgrade;
        }

        private static void CheckAndUpgrade()
        {
            if (SessionState.GetBool(UpgradedPrefKey, false)) return;
            SessionState.SetBool(UpgradedPrefKey, true);

            Debug.Log("[Exterior3DWorldAutoUpgrader] Automatically executing Step 4A CC0 Exterior 3D World Upgrade...");
            try
            {
                Exterior3DWorldUpgrader.UpgradeAllExteriorWorldMenu();
                Step4A_Exterior3DModelsAudit.RunAudit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Exterior3DWorldAutoUpgrader] Auto upgrade failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
#endif
}
