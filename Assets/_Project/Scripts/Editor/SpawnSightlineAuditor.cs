using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ProjectSurvival.EditorTools
{
    public static class SpawnSightlineAuditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Spawn Sightlines", false, 40)]
        public static void AuditSightlines()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[SpawnSightlineAuditor] Scanning overhead boulevard geometry...");

            Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r == null) continue;
                Bounds b = r.bounds;

                // Check boulevard corridor: X in [-8, 8], Z in [-10, 30], Y in [2.2, 14]
                if (Mathf.Abs(b.center.x) <= 8f && b.center.z >= -10f && b.center.z <= 30f && b.center.y >= 2.2f && b.center.y <= 14f)
                {
                    count++;
                    Debug.Log($"[SpawnSightlineAuditor] Overhead object: '{r.gameObject.name}', Parent='{r.transform.parent?.name}', Center={b.center}, Size={b.size}");
                }
            }

            Debug.Log($"[SpawnSightlineAuditor] Total overhead boulevard objects found: {count}");
            EditorApplication.Exit(0);
        }
#endif
    }
}
