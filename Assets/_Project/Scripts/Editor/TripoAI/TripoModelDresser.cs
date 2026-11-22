using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Player;
using ProjectSurvival.AI;

namespace ProjectSurvival.EditorTools.TripoAI
{
    public static class TripoModelDresser
    {
        /// <summary>
        /// Replaces the placeholder geometry inside a target prefab's Visual_Model child with the imported 3D model.
        /// </summary>
        public static bool DressPrefab(string prefabPath, GameObject glbAsset, float targetHeight = 1.8f)
        {
            if (string.IsNullOrEmpty(prefabPath) || glbAsset == null)
            {
                Debug.LogError("[TripoModelDresser] Geçersiz prefab yolu veya 3D model varlığı.");
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[TripoModelDresser] Prefab yüklenemedi: {prefabPath}");
                return false;
            }

            try
            {
                ApplyModelToRoot(prefabRoot, glbAsset, targetHeight);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[TripoModelDresser] BAŞARILI: Prefab '{prefabPath}' yeni 3D model ile giydirildi!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TripoModelDresser] Prefab giydirme hatası: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Replaces the placeholder geometry inside an existing scene object's Visual_Model child.
        /// </summary>
        public static bool DressSceneObject(GameObject sceneRoot, GameObject glbAsset, float targetHeight = 1.8f)
        {
            if (sceneRoot == null || glbAsset == null) return false;

            Undo.RegisterFullObjectHierarchyUndo(sceneRoot, "Tripo Model Dress");
            ApplyModelToRoot(sceneRoot, glbAsset, targetHeight);
            EditorSceneManager.MarkSceneDirty(sceneRoot.scene);
            Debug.Log($"[TripoModelDresser] Sahne objesi '{sceneRoot.name}' yeni model ile giydirildi!");
            return true;
        }

        /// <summary>
        /// Spawns the imported model directly in front of the active SceneView camera.
        /// </summary>
        public static GameObject SpawnInSceneView(GameObject glbAsset, float targetHeight = 1.5f)
        {
            if (glbAsset == null) return null;

            Vector3 spawnPosition = Vector3.zero;
            Camera sceneCam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;

            if (sceneCam != null)
            {
                Ray ray = new Ray(sceneCam.transform.position, sceneCam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                {
                    spawnPosition = hit.point;
                }
                else
                {
                    spawnPosition = sceneCam.transform.position + sceneCam.transform.forward * 4f;
                    spawnPosition.y = 0f;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbAsset);
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(glbAsset);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Tripo Model");
            instance.transform.position = spawnPosition;
            instance.transform.rotation = Quaternion.identity;

            // Auto-scale
            Bounds b = CalculateTotalBounds(instance);
            if (b.size.y > 0.05f && targetHeight > 0f)
            {
                float scale = targetHeight / b.size.y;
                instance.transform.localScale = Vector3.one * scale;
            }

            // Ensure colliders exist for interaction/physics
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
                    bc.size = CalculateTotalBounds(instance).size;
                    bc.center = new Vector3(0, bc.size.y * 0.5f, 0);
                }
            }

            EnsureUrpMaterials(instance);
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return instance;
        }

        private static void ApplyModelToRoot(GameObject root, GameObject glbAsset, float targetHeight)
        {
            // 1. Locate or create Visual_Model
            Transform visualTransform = root.transform.Find("Visual_Model");
            if (visualTransform == null)
            {
                GameObject newVisual = new GameObject("Visual_Model");
                newVisual.transform.SetParent(root.transform, false);
                visualTransform = newVisual.transform;
            }

            // 2. Clean temporary components directly on Visual_Model (capsules, cubes, etc.)
            var directMeshFilter = visualTransform.GetComponent<MeshFilter>();
            if (directMeshFilter != null) UnityEngine.Object.DestroyImmediate(directMeshFilter);

            var directMeshRenderer = visualTransform.GetComponent<MeshRenderer>();
            if (directMeshRenderer != null) UnityEngine.Object.DestroyImmediate(directMeshRenderer);

            var directCollider = visualTransform.GetComponent<Collider>();
            if (directCollider != null) UnityEngine.Object.DestroyImmediate(directCollider);

            // 3. Remove existing children under Visual_Model
            int childCount = visualTransform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(visualTransform.GetChild(i).gameObject);
            }

            // 4. Instantiate the GLB asset as child of Visual_Model
            GameObject modelInstance = UnityEngine.Object.Instantiate(glbAsset, visualTransform);
            modelInstance.name = "Model_Mesh";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // 5. Calculate Bounds and compute scale
            Bounds rawBounds = CalculateTotalBounds(modelInstance);
            float currentHeight = rawBounds.size.y;

            if (currentHeight > 0.05f && targetHeight > 0f)
            {
                float scaleFactor = targetHeight / currentHeight;
                modelInstance.transform.localScale = Vector3.one * scaleFactor;

                // Pivot alignment: if bottom of model is offset from local 0, snap feet to Y=0
                Bounds scaledBounds = CalculateTotalBounds(modelInstance);
                float yOffset = -scaledBounds.min.y;
                modelInstance.transform.localPosition = new Vector3(0, yOffset, 0);
            }

            // 6. Fix URP materials if needed
            EnsureUrpMaterials(modelInstance);

            // 7. Reconnect Animation Bridges
            var playerBridge = root.GetComponent<PlayerAnimationBridge>();
            if (playerBridge != null)
            {
                playerBridge.RefreshAnimator();
            }

            var zombieBridge = root.GetComponent<ZombieAnimationBridge>();
            if (zombieBridge != null)
            {
                zombieBridge.RefreshAnimator();
            }
        }

        private static Bounds CalculateTotalBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }
            return totalBounds;
        }

        private static void EnsureUrpMaterials(GameObject go)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterials == null) continue;
                var mats = r.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null || mats[i].shader.name == "Hidden/InternalErrorShader" || mats[i].shader.name == "Standard")
                    {
                        var newMat = new Material(urpLit);
                        newMat.name = mats[i] != null ? mats[i].name : "Mat_Tripo_URP";
                        if (mats[i] != null && mats[i].HasProperty("_MainTex"))
                        {
                            newMat.SetTexture("_BaseMap", mats[i].GetTexture("_MainTex"));
                        }
                        mats[i] = newMat;
                        changed = true;
                    }
                }

                if (changed)
                {
                    r.sharedMaterials = mats;
                }
            }
        }
    }
}
