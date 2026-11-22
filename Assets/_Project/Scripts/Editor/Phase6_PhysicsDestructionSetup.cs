using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Active Phase 6: Scene Setup and World Injection for Directional Ragdoll Physics,
    /// Chain-Reaction Explosive Hazards, and Destructible Environmental Props.
    /// </summary>
    public static class Phase6_PhysicsDestructionSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static readonly string[] ZombiePrefabPaths = new string[]
        {
            "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab",
            "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab",
            "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab",
            "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab"
        };

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Phase 6 Physics & Destruction", false, 38)]
        public static void SetupPhase6()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Phase6_Setup] Starting Phase 6 Physics & Destruction setup...");

            // 1. Update Zombie Prefabs with ZombieRagdollController
            SetupZombiePrefabs();

            // 2. Setup Zombie instances in Scene
            SetupSceneZombies();

            // 3. Materials
            Material barrelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Barrel_ExplosiveRed.mat");
            if (barrelMat == null)
            {
                barrelMat = CreateLitMaterial("Mat_Barrel_ExplosiveRed", new Color(0.85f, 0.12f, 0.1f), new Color(1f, 0.2f, 0f) * 0.4f);
            }

            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Window_Glass.mat");
            if (glassMat == null)
            {
                glassMat = CreateLitMaterial("Mat_Window_Glass", new Color(0.7f, 0.85f, 0.95f, 0.5f), Color.black, isTransparent: true);
            }

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            if (woodMat == null)
            {
                woodMat = CreateLitMaterial("Mat_Wood_Timber", new Color(0.45f, 0.3f, 0.18f), Color.black);
            }

            Material fenceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Picket_Fence.mat");
            if (fenceMat == null)
            {
                fenceMat = CreateLitMaterial("Mat_Picket_Fence", new Color(0.4f, 0.32f, 0.22f), Color.black);
            }

            // 4. Spawn 20 Explosive Hazards across Map Chokepoints
            SetupExplosiveHazards(barrelMat);

            // 5. Spawn Destructible Props (Glass, Crates, Fences)
            SetupDestructibleProps(glassMat, woodMat, fenceMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Phase6_Setup] Phase 6 Physics & Destruction setup completed successfully!");
        }

        private static void SetupZombiePrefabs()
        {
            foreach (string path in ZombiePrefabPaths)
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                if (prefabContents == null)
                {
                    Debug.LogWarning($"[Phase6_Setup] Could not load prefab at: {path}");
                    continue;
                }

                var ragdoll = prefabContents.GetComponent<ZombieRagdollController>();
                if (ragdoll == null)
                {
                    ragdoll = prefabContents.AddComponent<ZombieRagdollController>();
                }

                ragdoll.EnsureRagdollRig();

                PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                Debug.Log($"[Phase6_Setup] Updated Zombie Prefab with Ragdoll: {path}");
            }
        }

        private static void SetupSceneZombies()
        {
            ZombieAI[] sceneZombies = UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            foreach (var z in sceneZombies)
            {
                if (z == null) continue;
                var ragdoll = z.GetComponent<ZombieRagdollController>();
                if (ragdoll == null)
                {
                    ragdoll = z.gameObject.AddComponent<ZombieRagdollController>();
                }
                ragdoll.EnsureRagdollRig();
            }
            Debug.Log($"[Phase6_Setup] Verified {sceneZombies.Length} scene zombie instances.");
        }

        private static void SetupExplosiveHazards(Material barrelMat)
        {
            GameObject hazardsRoot = GameObject.Find("--- EXPLOSIVE HAZARDS ---");
            if (hazardsRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(hazardsRoot);
            }

            hazardsRoot = new GameObject("--- EXPLOSIVE HAZARDS ---");

            // 20 Barrels across 5 Chokepoints
            Vector3[] locations = new Vector3[]
            {
                // Boulevard
                new Vector3(20f, 0f, 15f),
                new Vector3(-15f, 0f, 30f),
                new Vector3(5f, 0f, 50f),
                new Vector3(35f, 0f, -20f),

                // East Rail Yard
                new Vector3(180f, 0f, -120f),
                new Vector3(195f, 0f, -110f),
                new Vector3(170f, 0f, -135f),
                new Vector3(210f, 0f, -100f),

                // Northeast Harbor
                new Vector3(220f, 0f, 180f),
                new Vector3(235f, 0f, 195f),
                new Vector3(205f, 0f, 170f),
                new Vector3(250f, 0f, 210f),

                // North Highway Checkpoint
                new Vector3(-50f, 0f, 240f),
                new Vector3(-65f, 0f, 255f),
                new Vector3(-35f, 0f, 230f),
                new Vector3(-80f, 0f, 270f),

                // Southeast Substation
                new Vector3(160f, 0f, 220f),
                new Vector3(145f, 0f, 205f),
                new Vector3(175f, 0f, 235f),
                new Vector3(130f, 0f, 190f)
            };

            for (int i = 0; i < locations.Length; i++)
            {
                Vector3 basePos = locations[i];
                float groundY = GetGroundHeight(basePos);
                Vector3 finalPos = new Vector3(basePos.x, groundY + 0.45f, basePos.z);

                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = $"Explosive_Fuel_Barrel_{i + 1:D2}";
                barrel.transform.SetParent(hazardsRoot.transform, false);
                barrel.transform.position = finalPos;
                barrel.transform.localScale = new Vector3(0.75f, 0.45f, 0.75f); // ~0.9m height, 0.75m diameter

                var renderer = barrel.GetComponent<MeshRenderer>();
                if (renderer != null && barrelMat != null)
                {
                    renderer.sharedMaterial = barrelMat;
                }

                var col = barrel.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    col.height = 2.0f;
                    col.radius = 0.5f;
                }

                var hazard = barrel.AddComponent<ExplosiveFuelHazard>();
                // Defaults are 45 HP, 7.5m radius, 220 dmg, 480N impulse, 6.5m chain reaction
            }

            Debug.Log($"[Phase6_Setup] Spawned {locations.Length} Explosive Fuel Hazards.");
        }

        private static void SetupDestructibleProps(Material glassMat, Material woodMat, Material fenceMat)
        {
            GameObject propsRoot = GameObject.Find("--- DESTRUCTIBLE PROPS ---");
            if (propsRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(propsRoot);
            }

            propsRoot = new GameObject("--- DESTRUCTIBLE PROPS ---");

            // 1. ShatterGlass (8 instances)
            Vector3[] glassPositions = new Vector3[]
            {
                new Vector3(6.5f, 0f, 8.0f),
                new Vector3(-8.0f, 0f, 5.0f),
                new Vector3(18.0f, 0f, 25.0f),
                new Vector3(28.0f, 0f, 35.0f),
                new Vector3(-12.0f, 0f, 45.0f),
                new Vector3(175.0f, 0f, -115.0f),
                new Vector3(185.0f, 0f, -125.0f),
                new Vector3(215.0f, 0f, 185.0f)
            };

            for (int i = 0; i < glassPositions.Length; i++)
            {
                Vector3 pos = glassPositions[i];
                float groundY = GetGroundHeight(pos);
                Vector3 finalPos = new Vector3(pos.x, groundY + 1.0f, pos.z);

                GameObject glassObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glassObj.name = $"Destructible_Glass_Window_{i + 1:D2}";
                glassObj.transform.SetParent(propsRoot.transform, false);
                glassObj.transform.position = finalPos;
                glassObj.transform.localScale = new Vector3(2.2f, 2.0f, 0.12f);

                var ren = glassObj.GetComponent<MeshRenderer>();
                if (ren != null && glassMat != null) ren.sharedMaterial = glassMat;

                var obs = glassObj.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.size = new Vector3(2.2f, 2.0f, 0.2f);

                var prop = glassObj.AddComponent<DestructibleProp>();
                prop.ConfigureArchetype(DestructibleArchetype.ShatterGlass, 20f);
            }

            // 2. BreakableCrate (10 instances)
            Vector3[] cratePositions = new Vector3[]
            {
                new Vector3(12.0f, 0f, 20.0f),
                new Vector3(-10.0f, 0f, 18.0f),
                new Vector3(4.0f, 0f, 32.0f),
                new Vector3(168.0f, 0f, -108.0f),
                new Vector3(190.0f, 0f, -118.0f),
                new Vector3(202.0f, 0f, -95.0f),
                new Vector3(212.0f, 0f, 172.0f),
                new Vector3(228.0f, 0f, 188.0f),
                new Vector3(-42.0f, 0f, 235.0f),
                new Vector3(-72.0f, 0f, 260.0f)
            };

            for (int i = 0; i < cratePositions.Length; i++)
            {
                Vector3 pos = cratePositions[i];
                float groundY = GetGroundHeight(pos);
                Vector3 finalPos = new Vector3(pos.x, groundY + 0.55f, pos.z);

                GameObject crateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crateObj.name = $"Destructible_Wooden_Crate_{i + 1:D2}";
                crateObj.transform.SetParent(propsRoot.transform, false);
                crateObj.transform.position = finalPos;
                crateObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

                var ren = crateObj.GetComponent<MeshRenderer>();
                if (ren != null && woodMat != null) ren.sharedMaterial = woodMat;

                var prop = crateObj.AddComponent<DestructibleProp>();
                prop.ConfigureArchetype(DestructibleArchetype.BreakableCrate, 35f);
            }

            // 3. WeakTimberFence (8 instances)
            Vector3[] fencePositions = new Vector3[]
            {
                new Vector3(14.0f, 0f, 10.0f),
                new Vector3(-16.0f, 0f, 12.0f),
                new Vector3(22.0f, 0f, 40.0f),
                new Vector3(-25.0f, 0f, 55.0f),
                new Vector3(160.0f, 0f, -130.0f),
                new Vector3(200.0f, 0f, -105.0f),
                new Vector3(200.0f, 0f, 165.0f),
                new Vector3(240.0f, 0f, 205.0f)
            };

            for (int i = 0; i < fencePositions.Length; i++)
            {
                Vector3 pos = fencePositions[i];
                float groundY = GetGroundHeight(pos);
                Vector3 finalPos = new Vector3(pos.x, groundY + 0.9f, pos.z);

                GameObject fenceObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fenceObj.name = $"Destructible_Timber_Fence_{i + 1:D2}";
                fenceObj.transform.SetParent(propsRoot.transform, false);
                fenceObj.transform.position = finalPos;
                fenceObj.transform.localScale = new Vector3(2.8f, 1.8f, 0.22f);

                var ren = fenceObj.GetComponent<MeshRenderer>();
                if (ren != null && fenceMat != null) ren.sharedMaterial = fenceMat;

                var obs = fenceObj.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.size = new Vector3(2.8f, 1.8f, 0.3f);

                var prop = fenceObj.AddComponent<DestructibleProp>();
                prop.ConfigureArchetype(DestructibleArchetype.WeakTimberFence, 50f);
            }

            Debug.Log($"[Phase6_Setup] Spawned {glassPositions.Length} Glass Windows, {cratePositions.Length} Crates, {fencePositions.Length} Timber Fences.");
        }

        private static float GetGroundHeight(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                return hit.point.y;
            }
            return 0f;
        }

        private static Material CreateLitMaterial(string name, Color color, Color emission, bool isTransparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            if (emission != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }

            string savePath = $"Assets/_Project/Materials/{name}.mat";
            AssetDatabase.CreateAsset(mat, savePath);
            return mat;
        }
#endif
    }
}
