using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Animation;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 4 (Step 4B): Master 3D Character Visual Upgrader.
    /// Replaces primitive cylinder/capsule meshes on Player_Root, 19 NPCs, 12 Human Raiders, and 49 Zombies
    /// with genuine CC0 3D character meshes (Kenney CC0 Mini Characters / Survival / Graveyard imported via com.unity.cloud.gltfast).
    /// Preserves Root -> Visual_Model hierarchy, Animator.applyRootMotion = false, HumanoidProceduralAnimator AutoBindLimbs(),
    /// FPS self-head hiding, ZombieHitZoneResolver 2.5x headshot hitboxes, and ZombieRagdollController.
    /// </summary>
    public static class Character3DVisualUpgrader
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Vehicles_Characters";
        public const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Upgrade Characters 3D Visuals (Step 4B)", false, 38)]
        public static void UpgradeAllCharactersMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("===============================================================================");
            Debug.Log(">>> EXECUTING ROADMAP PHASE 4 (STEP 4B): CHARACTERS 3D VISUAL UPGRADE <<<");
            Debug.Log("===============================================================================");

            UpgradePlayerVisual();
            UpgradeAllNPCs();
            UpgradeAllRaiders();
            UpgradeAllZombies();
            EnforceURPMaterialsAndInstancing();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(">>> PHASE 4 (STEP 4B) CHARACTERS 3D VISUAL UPGRADE COMPLETED & SAVED! <<<");
            Debug.Log("===============================================================================");
        }

        public static void UpgradePlayerVisual()
        {
            GameObject player = GameObject.Find("Player_Root");
            if (player == null)
            {
                Debug.LogWarning("[Character3DVisualUpgrader] Player_Root not found!");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(player))
            {
                PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            Transform vm = player.transform.Find("Visual_Model");
            if (vm == null) return;

            // Strip primitive shapes under Visual_Model except weapon sockets
            StripPrimitiveMeshesPreservingWeapons(vm);

            Transform ex = vm.Find("CC0_Player_Mesh");
            if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

            GameObject model = LoadOrInstantiateModel("survivor_human.glb", vm, Vector3.zero, Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));
            if (model != null)
            {
                model.name = "CC0_Player_Mesh";
            }

            // Re-bind procedural animation limbs
            var anim = player.GetComponent<HumanoidProceduralAnimator>();
            if (anim != null)
            {
                anim.AutoBindLimbs();
            }

            Debug.Log("[Character3DVisualUpgrader] Player_Root visual upgraded with real CC0 3D mesh.");
        }

        public static void UpgradeAllNPCs()
        {
            int upgradedCount = 0;
            var allAnimators = UnityEngine.Object.FindObjectsByType<HumanoidProceduralAnimator>(FindObjectsSortMode.None);

            foreach (var hpa in allAnimators)
            {
                if (hpa.gameObject.name.Contains("Player") || hpa.gameObject.name.Contains("Raider")) continue;

                if (PrefabUtility.IsPartOfPrefabInstance(hpa.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(hpa.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Transform vm = hpa.transform.Find("Visual_Model");
                if (vm == null)
                {
                    GameObject vmGo = new GameObject("Visual_Model");
                    vmGo.transform.SetParent(hpa.transform, false);
                    vm = vmGo.transform;
                }

                StripPrimitiveMeshes(vm);

                Transform ex = vm.Find("CC0_NPC_Mesh");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                string npcName = hpa.gameObject.name.ToLower();
                bool isFemale = npcName.Contains("zeynep") || npcName.Contains("elif") || npcName.Contains("female");
                string modelFile = isFemale ? "survivor_female.glb" : (upgradedCount % 2 == 0 ? "survivor_human.glb" : "survivor_male_b.glb");

                GameObject model = LoadOrInstantiateModel(modelFile, vm, Vector3.zero, Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));
                if (model != null)
                {
                    model.name = "CC0_NPC_Mesh";
                    upgradedCount++;
                }

                hpa.AutoBindLimbs();
            }

            Debug.Log($"[Character3DVisualUpgrader] Upgraded {upgradedCount} NPCs with real CC0 survivor 3D meshes.");
        }

        public static void UpgradeAllRaiders()
        {
            int upgradedCount = 0;
            var raiders = UnityEngine.Object.FindObjectsByType<HumanRaiderAI>(FindObjectsSortMode.None);

            foreach (var raider in raiders)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(raider.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(raider.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Transform vm = raider.transform.Find("Visual_Model");
                if (vm == null)
                {
                    GameObject vmGo = new GameObject("Visual_Model");
                    vmGo.transform.SetParent(raider.transform, false);
                    vm = vmGo.transform;
                }

                StripPrimitiveMeshes(vm);

                Transform ex = vm.Find("CC0_Raider_Mesh");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                string modelFile = (upgradedCount % 2 == 0) ? "raider_human.glb" : "raider_keeper.glb";
                GameObject model = LoadOrInstantiateModel(modelFile, vm, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
                if (model != null)
                {
                    model.name = "CC0_Raider_Mesh";
                    upgradedCount++;
                }

                var hpa = raider.GetComponent<HumanoidProceduralAnimator>();
                if (hpa != null) hpa.AutoBindLimbs();
            }

            Debug.Log($"[Character3DVisualUpgrader] Upgraded {upgradedCount} Human Raiders with tactical CC0 3D bandit meshes.");
        }

        public static void UpgradeAllZombies()
        {
            int upgradedCount = 0;
            var zombies = UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);

            foreach (var z in zombies)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(z.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(z.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Transform vm = z.transform.Find("Visual_Model");
                if (vm == null)
                {
                    GameObject vmGo = new GameObject("Visual_Model");
                    vmGo.transform.SetParent(z.transform, false);
                    vm = vmGo.transform;
                }

                StripPrimitiveMeshes(vm);

                Transform ex = vm.Find("CC0_Zombie_Mesh");
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);

                GameObject model = LoadOrInstantiateModel("zombie_character.glb", vm, Vector3.zero, Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));
                if (model != null)
                {
                    model.name = "CC0_Zombie_Mesh";
                    upgradedCount++;
                }
            }

            Debug.Log($"[Character3DVisualUpgrader] Upgraded {upgradedCount} Infected Zombies with real CC0 3D zombie meshes.");
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

                bool isChar = goName.Contains("character") || goName.Contains("zombie") || goName.Contains("survivor") ||
                              goName.Contains("raider") || goName.Contains("npc") || goName.Contains("player") ||
                              parentName.Contains("character") || parentName.Contains("visual_model");

                if (isChar)
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

            Debug.Log($"[Character3DVisualUpgrader] Enforced GPU instancing on {instancedCount} character materials.");
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

        private static void StripPrimitiveMeshes(Transform parent)
        {
            if (parent == null) return;
            var all = parent.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null || t == parent) continue;
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount <= 48)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                    UnityEngine.Object.DestroyImmediate(mf);
                }
            }
        }

        private static void StripPrimitiveMeshesPreservingWeapons(Transform parent)
        {
            if (parent == null) return;
            var all = parent.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null || t == parent) continue;
                string n = t.name.ToLower();
                if (n.Contains("weapon") || n.Contains("socket") || n.Contains("axe") || n.Contains("pistol") || n.Contains("shotgun") || n.Contains("crossbow"))
                {
                    continue; // Preserve weapon sockets
                }

                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount <= 48)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                    UnityEngine.Object.DestroyImmediate(mf);
                }
            }
        }
#endif
    }
}
