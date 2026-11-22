using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

namespace ProjectSurvival.EditorTools.TripoAI
{
    public static class TripoBatchIntegrator
    {
        private const string ModelsFolder = "Assets/_Project/Models/TripoGenerated";
        private const string MainScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Tools/Tripo AI/Toplu Sahne ve Prefab Entegrasyonu (Batch Integrate)", false, 30)]
        [MenuItem("ProjectSurvival/Batch Integrate Tripo Models", false, 31)]
        public static void IntegrateAll()
        {
            Debug.Log("[TripoBatchIntegrator] Toplu model entegrasyonu başlatılıyor...");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            int prefabsDressed = 0;
            int propsPlaced = 0;

            // 1. Dress Character & Prop Prefabs
            prefabsDressed += TryDressPrefab("Survivor.glb", "Assets/_Project/Prefabs/Player/Player_Root.prefab", 1.8f);
            prefabsDressed += TryDressPrefab("Zombie_Standard.glb", "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab", 1.8f);
            prefabsDressed += TryDressPrefab("Zombie_Runner.glb", "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab", 1.75f);
            prefabsDressed += TryDressPrefab("Zombie_Screamer.glb", "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab", 1.8f);
            prefabsDressed += TryDressPrefab("Zombie_Tank.glb", "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab", 2.3f);
            prefabsDressed += TryDressPrefab("Loot_Crate.glb", "Assets/_Project/Prefabs/Environment/LootContainer_Root.prefab", 1.0f);

            // 2. Scene Environment Placements
            if (File.Exists(MainScenePath))
            {
                var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

                // Dr. Aras
                GameObject drArasGlb = LoadModelAsset("Dr_Aras.glb");
                if (drArasGlb != null)
                {
                    GameObject existingNpc = GameObject.Find("NPC_DrAras");
                    if (existingNpc != null)
                    {
                        TripoModelDresser.DressSceneObject(existingNpc, drArasGlb, 1.8f);
                        propsPlaced++;
                    }
                    else
                    {
                        GameObject spawnedNpc = SpawnSceneProp(drArasGlb, "NPC_DrAras", new Vector3(15f, 0f, 9.5f), Quaternion.Euler(0, -90, 0), 1.8f, isStatic: false);
                        propsPlaced++;
                    }
                }

                // Ambulance (in front of Clinic)
                GameObject ambulanceGlb = LoadModelAsset("Ambulance.glb");
                if (ambulanceGlb != null)
                {
                    SpawnSceneProp(ambulanceGlb, "Prop_Ambulance_Clinic", new Vector3(12.5f, 0f, 13.5f), Quaternion.Euler(0, -35, 0), 2.2f, isStatic: true);
                    propsPlaced++;
                }

                // Car Wrecks (street cover)
                GameObject carWreckGlb = LoadModelAsset("Car_Wreck.glb");
                if (carWreckGlb != null)
                {
                    SpawnSceneProp(carWreckGlb, "Prop_CarWreck_West", new Vector3(-5f, 0f, 6f), Quaternion.Euler(0, 45, 0), 1.5f, isStatic: true);
                    SpawnSceneProp(carWreckGlb, "Prop_CarWreck_East", new Vector3(7f, 0f, -7f), Quaternion.Euler(0, -20, 0), 1.5f, isStatic: true);
                    propsPlaced += 2;
                }

                // Barricades (street choke points)
                GameObject barricadeGlb = LoadModelAsset("Barricade.glb");
                if (barricadeGlb != null)
                {
                    SpawnSceneProp(barricadeGlb, "Prop_Barricade_North", new Vector3(0f, 0f, 10f), Quaternion.identity, 1.2f, isStatic: true);
                    SpawnSceneProp(barricadeGlb, "Prop_Barricade_South", new Vector3(0f, 0f, -10f), Quaternion.identity, 1.2f, isStatic: true);
                    propsPlaced += 2;
                }

                // 3. Re-Bake NavMesh
                RebakeSceneNavMesh();

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[TripoBatchIntegrator] Sahne güncellendi ve kaydedildi ({MainScenePath}).");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TripoBatchIntegrator] TAMAMLANDI! {prefabsDressed} prefab güncellendi, {propsPlaced} sahne varlığı yerleştirildi.");
        }

        private static int TryDressPrefab(string modelFileName, string prefabPath, float targetHeight)
        {
            string modelPath = Path.Combine(ModelsFolder, modelFileName);
            if (!File.Exists(modelPath))
            {
                return 0;
            }

            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (glbAsset == null)
            {
                Debug.LogWarning($"[TripoBatchIntegrator] '{modelPath}' GameObject olarak açılamadı.");
                return 0;
            }

            bool success = TripoModelDresser.DressPrefab(prefabPath, glbAsset, targetHeight);
            return success ? 1 : 0;
        }

        private static GameObject LoadModelAsset(string modelFileName)
        {
            string modelPath = Path.Combine(ModelsFolder, modelFileName);
            if (!File.Exists(modelPath)) return null;
            return AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        }

        private static GameObject SpawnSceneProp(GameObject glbAsset, string objectName, Vector3 position, Quaternion rotation, float targetHeight, bool isStatic)
        {
            // Remove existing instance if already present
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbAsset);
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(glbAsset);
            }

            instance.name = objectName;
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            // Ensure colliders
            if (instance.GetComponentInChildren<Collider>() == null)
            {
                var mf = instance.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = instance.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                else
                {
                    var bc = instance.AddComponent<BoxCollider>();
                    bc.size = CalculateBounds(instance).size;
                    bc.center = new Vector3(0, bc.size.y * 0.5f, 0);
                }
            }

            // Scale to target height
            Bounds b = CalculateBounds(instance);
            if (b.size.y > 0.05f && targetHeight > 0f)
            {
                float scale = targetHeight / b.size.y;
                instance.transform.localScale = Vector3.one * scale;

                // Ground align
                Bounds sb = CalculateBounds(instance);
                float yOffset = -sb.min.y;
                instance.transform.position = new Vector3(position.x, position.y + yOffset, position.z);
            }

            if (isStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.NavigationStatic | StaticEditorFlags.OccludeeStatic);
            }

            return instance;
        }

        private static Bounds CalculateBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        private static void RebakeSceneNavMesh()
        {
            var navMeshSurfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            if (navMeshSurfaces != null && navMeshSurfaces.Length > 0)
            {
                foreach (var surface in navMeshSurfaces)
                {
                    surface.BuildNavMesh();
                    Debug.Log($"[TripoBatchIntegrator] NavMeshSurface '{surface.name}' yeniden pişirildi.");
                }
            }
            else
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("[TripoBatchIntegrator] UnityEditor.AI.NavMeshBuilder ile NavMesh yeniden pişirildi.");
            }
        }
    }
}
