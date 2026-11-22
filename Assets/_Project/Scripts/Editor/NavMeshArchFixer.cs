using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class NavMeshArchFixer
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Inspect and Fix Arch NavMesh", false, 35)]
        public static void InspectAndFixArch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[NavMeshArchFixer] Inspecting geometry near (0, 3, 0)...");

            Vector3 target = new Vector3(0f, 3f, 0f);
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            List<GameObject> foundNear = new List<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject go = allObjects[i];
                Renderer r = go.GetComponent<Renderer>();
                if (r != null && r.bounds.Contains(target))
                {
                    foundNear.Add(go);
                    Debug.Log($"[NavMeshArchFixer] Direct bounds contain (0,3,0): {go.name} at {go.transform.position}, bounds={r.bounds}");
                }
                else if (Vector3.Distance(go.transform.position, target) <= 5.0f)
                {
                    foundNear.Add(go);
                    Debug.Log($"[NavMeshArchFixer] Position near (0,3,0): {go.name} at {go.transform.position}");
                }
                else if (r != null && Vector3.Distance(r.bounds.center, target) <= 4.0f)
                {
                    foundNear.Add(go);
                    Debug.Log($"[NavMeshArchFixer] Bounds center near (0,3,0): {go.name} at {r.bounds.center}");
                }
            }

            Debug.Log($"[NavMeshArchFixer] Total candidates found: {foundNear.Count}");

            // Mark any elevated structure near Y=3m as Not Walkable
            int modifiedCount = 0;
            for (int i = 0; i < foundNear.Count; i++)
            {
                GameObject go = foundNear[i];
                Renderer r = go.GetComponent<Renderer>();
                if (r != null && r.bounds.max.y >= 2.0f && r.bounds.min.y >= 0.5f)
                {
                    // This is an elevated geometry (not pavement ground)
                    GameObjectUtility.SetNavMeshArea(go, 1); // 1 = Not Walkable
                    // Also strip NavigationStatic if desired
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                    flags &= ~StaticEditorFlags.NavigationStatic;
                    GameObjectUtility.SetStaticEditorFlags(go, flags);

                    EditorUtility.SetDirty(go);
                    modifiedCount++;
                    Debug.Log($"[NavMeshArchFixer] Marked as NOT WALKABLE & stripped NavigationStatic: {go.name} (bounds={r.bounds})");
                }
            }

            Debug.Log($"[NavMeshArchFixer] Modified {modifiedCount} elevated GameObjects.");

            // Rebake NavMesh
            Debug.Log("[NavMeshArchFixer] Rebuilding NavMesh...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[NavMeshArchFixer] NavMesh rebuilt successfully.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NavMeshArchFixer] Scene saved.");

            // Test NavMesh sampling at (0, 0.1, 0) and (0, 0.1, 6)
            NavMesh.SamplePosition(new Vector3(0f, 0.1f, 0f), out NavMeshHit hitZero, 2.0f, NavMesh.AllAreas);
            Debug.Log($"[NavMeshArchFixer] Sample at (0, 0.1, 0) hit: {hitZero.position}");

            NavMesh.SamplePosition(new Vector3(0f, 0.1f, 6f), out NavMeshHit hitSix, 1.5f, NavMesh.AllAreas);
            Debug.Log($"[NavMeshArchFixer] Sample at (0, 0.1, 6) hit: {hitSix.position}");

            EditorApplication.Exit(0);
        }
#endif
    }
}
