using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Vehicles;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 4 (Step 4B): Master 3D Vehicle Visual Upgrader.
    /// Replaces primitive boxy visuals on all 5 hero drivable vehicles and 10 siphonable wrecked cars
    /// with genuine CC0 3D vehicle models (Kenney CC0 Car Kit imported via com.unity.cloud.gltfast).
    /// Preserves WheelCollider physics, 4 rotating wheel bindings, trunk inventories,
    /// NavMeshObstacle carving, headlights (Shadows: None), and 3D spatialBlend = 1.0 engine audio.
    /// </summary>
    public static class Vehicle3DVisualUpgrader
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Vehicles_Characters";
        public const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Upgrade Vehicles 3D Visuals (Step 4B)", false, 37)]
        public static void UpgradeAllVehiclesMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("===============================================================================");
            Debug.Log(">>> EXECUTING ROADMAP PHASE 4 (STEP 4B): VEHICLES 3D VISUAL UPGRADE <<<");
            Debug.Log("===============================================================================");

            UpgradeDrivableHeroVehicles();
            UpgradeWreckedCars();
            EnforceURPMaterialsAndInstancing();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(">>> PHASE 4 (STEP 4B) VEHICLES 3D VISUAL UPGRADE COMPLETED & SAVED! <<<");
            Debug.Log("===============================================================================");
        }

        public static void UpgradeDrivableHeroVehicles()
        {
            var vehicleConfigs = new (string goName, string bodyModel, string wheelModel, Vector3 bodyScale)[]
            {
                ("Vehicle_NorthHighway_ArmoredSUV", "suv.glb", "wheel_dark.glb", new Vector3(1.8f, 1.8f, 1.8f)),
                ("Vehicle_SafeHouse_Pickup", "truck.glb", "wheel_truck.glb", new Vector3(1.85f, 1.85f, 1.85f)),
                ("Vehicle_Police_Interceptor", "police.glb", "wheel.glb", new Vector3(1.75f, 1.75f, 1.75f)),
                ("Vehicle_Forest_RangerJeep", "jeep.glb", "wheel_dark.glb", new Vector3(1.8f, 1.8f, 1.8f)),
                ("Vehicle_RailYard_CargoVan", "van.glb", "wheel.glb", new Vector3(1.85f, 1.85f, 1.85f))
            };

            int upgradedCount = 0;

            foreach (var cfg in vehicleConfigs)
            {
                GameObject vehGo = GameObject.Find(cfg.goName);
                if (vehGo == null)
                {
                    Debug.LogWarning($"[Vehicle3DVisualUpgrader] Vehicle '{cfg.goName}' not found in scene!");
                    continue;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(vehGo))
                {
                    PrefabUtility.UnpackPrefabInstance(vehGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                DrivableVehicleController dvc = vehGo.GetComponent<DrivableVehicleController>();
                Transform vm = vehGo.transform.Find("Visual_Model");
                if (vm == null)
                {
                    GameObject vmGo = new GameObject("Visual_Model");
                    vmGo.transform.SetParent(vehGo.transform, false);
                    vm = vmGo.transform;
                }

                // 1. Strip primitive body meshes inside Visual_Model
                StripPrimitiveChildren(vm, new[] { "Chassis", "Cabin", "Bed", "Bullbar", "Roof_Rack", "Bumper", "Body", "Hood", "Trunk", "Door" });

                // Remove existing CC0 body container if present
                Transform exBody = vm.Find("CC0_Vehicle_Body");
                if (exBody != null) UnityEngine.Object.DestroyImmediate(exBody.gameObject);

                // 2. Instantiate real CC0 3D Vehicle Body
                GameObject bodyObj = LoadOrInstantiateModel(cfg.bodyModel, vm, Vector3.zero, Vector3.zero, cfg.bodyScale);
                if (bodyObj != null)
                {
                    bodyObj.name = "CC0_Vehicle_Body";
                }

                // 3. Setup and Bind 4 Rotating 3D Wheels
                Transform wheelsParent = vm.Find("Wheel_Visuals") ?? vm.Find("Wheels_Visual");
                if (wheelsParent == null)
                {
                    GameObject wp = new GameObject("Wheel_Visuals");
                    wp.transform.SetParent(vm, false);
                    wheelsParent = wp.transform;
                }

                Transform wFL = EnsureWheelVisual(wheelsParent, "Wheel_FL_3D", cfg.wheelModel, new Vector3(-0.85f, 0.35f, 1.25f), Vector3.one * 1.6f);
                Transform wFR = EnsureWheelVisual(wheelsParent, "Wheel_FR_3D", cfg.wheelModel, new Vector3(0.85f, 0.35f, 1.25f), new Vector3(-1.6f, 1.6f, 1.6f));
                Transform wRL = EnsureWheelVisual(wheelsParent, "Wheel_RL_3D", cfg.wheelModel, new Vector3(-0.85f, 0.35f, -1.25f), Vector3.one * 1.6f);
                Transform wRR = EnsureWheelVisual(wheelsParent, "Wheel_RR_3D", cfg.wheelModel, new Vector3(0.85f, 0.35f, -1.25f), new Vector3(-1.6f, 1.6f, 1.6f));

                // Re-bind to DrivableVehicleController SerializedObject
                if (dvc != null)
                {
                    SerializedObject so = new SerializedObject(dvc);
                    so.FindProperty("wheelMeshFL").objectReferenceValue = wFL;
                    so.FindProperty("wheelMeshFR").objectReferenceValue = wFR;
                    so.FindProperty("wheelMeshRL").objectReferenceValue = wRL;
                    so.FindProperty("wheelMeshRR").objectReferenceValue = wRR;
                    so.FindProperty("visualModel").objectReferenceValue = vm;
                    so.ApplyModifiedProperties();
                }

                // 4. Ensure AudioSource is 3D Spatial
                AudioSource audio = vehGo.GetComponent<AudioSource>();
                if (audio != null)
                {
                    audio.spatialBlend = 1.0f; // Strict 3D positional audio
                }

                // 5. Ensure Headlights have Shadows: None
                var lights = vehGo.GetComponentsInChildren<Light>(true);
                foreach (var lt in lights)
                {
                    lt.shadows = LightShadows.None;
                }

                upgradedCount++;
            }

            Debug.Log($"[Vehicle3DVisualUpgrader] Upgraded all {upgradedCount} drivable hero vehicles with CC0 3D chassis and wheel models.");
        }

        public static void UpgradeWreckedCars()
        {
            var wrecks = UnityEngine.Object.FindObjectsByType<WreckedCarSiphonInteractable>(FindObjectsSortMode.None);
            int upgradedWrecks = 0;

            foreach (var wreck in wrecks)
            {
                if (wreck == null) continue;

                if (PrefabUtility.IsPartOfPrefabInstance(wreck.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(wreck.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Transform wreckTf = wreck.transform;

                // Strip primitive cubes
                StripPrimitiveChildren(wreckTf, new[] { "Wreck_Chassis", "Wreck_Cabin", "Chassis", "Body", "Hood" });

                Transform exModel = wreckTf.Find("CC0_Wreck_Model");
                if (exModel != null) UnityEngine.Object.DestroyImmediate(exModel.gameObject);

                // Instantiate real 3D wrecked sedan mesh
                GameObject model = LoadOrInstantiateModel("sedan_wreck.glb", wreckTf, Vector3.zero, Vector3.zero, new Vector3(1.7f, 1.7f, 1.7f));
                if (model != null)
                {
                    model.name = "CC0_Wreck_Model";
                    upgradedWrecks++;
                }

                // Ensure NavMeshObstacle is present with carving = true
                NavMeshObstacle nmo = wreck.GetComponent<NavMeshObstacle>();
                if (nmo == null) nmo = wreck.gameObject.AddComponent<NavMeshObstacle>();
                nmo.carving = true;
                nmo.shape = NavMeshObstacleShape.Box;
                nmo.size = new Vector3(2.2f, 1.4f, 4.2f);
                nmo.center = new Vector3(0f, 0.7f, 0f);

                // Ensure 3D Positional Audio
                AudioSource audio = wreck.GetComponent<AudioSource>();
                if (audio != null)
                {
                    audio.spatialBlend = 1.0f;
                }
            }

            Debug.Log($"[Vehicle3DVisualUpgrader] Upgraded {upgradedWrecks} wrecked siphonable cars with real 3D meshes.");
        }

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

                bool isVehicle = goName.Contains("vehicle") || goName.Contains("car") || goName.Contains("truck") ||
                                 goName.Contains("suv") || goName.Contains("police") || goName.Contains("van") ||
                                 goName.Contains("jeep") || goName.Contains("wreck") || goName.Contains("wheel") ||
                                 parentName.Contains("vehicle") || parentName.Contains("wreck");

                if (isVehicle)
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

            Debug.Log($"[Vehicle3DVisualUpgrader] Enforced GPU instancing on {instancedCount} vehicle materials.");
        }

        private static Transform EnsureWheelVisual(Transform parent, string name, string modelFile, Vector3 localPos, Vector3 localScale)
        {
            Transform existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject wObj = new GameObject(name);
            wObj.transform.SetParent(parent, false);
            wObj.transform.localPosition = localPos;
            wObj.transform.localRotation = Quaternion.identity;

            LoadOrInstantiateModel(modelFile, wObj.transform, Vector3.zero, Vector3.zero, localScale);
            return wObj.transform;
        }

        public static GameObject LoadOrInstantiateModel(string modelFileName, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            string assetPath = $"{ModelDirectory}/{modelFileName}";
            GameObject instance = null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }

            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance != null && PrefabUtility.IsPartOfPrefabInstance(instance))
                {
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }

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
            return instance;
        }

        private static void StripPrimitiveChildren(Transform parent, string[] keywords)
        {
            if (parent == null || keywords == null) return;

            var all = parent.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null || t == parent) continue;
                string n = t.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (n.Contains(kw.ToLower()))
                    {
                        var mf = t.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount <= 48)
                        {
                            var mr = t.GetComponent<MeshRenderer>();
                            if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                            UnityEngine.Object.DestroyImmediate(mf);
                        }
                    }
                }
            }
        }
#endif
    }
}
