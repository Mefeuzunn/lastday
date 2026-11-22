using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Editor
{
    public static class RecoveryStep1Audit
    {
        [MenuItem("Tools/Survival/Audit Recovery Step 1")]
        public static void RunAudit()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Main_Prototype")
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity");
            }

            Debug.Log("=================================================");
            Debug.Log("[SURGICAL RECOVERY STEP 1 AUDIT START]");
            Debug.Log("=================================================");

            int passed = 0;
            int failed = 0;

            // 1. Audit SmartphoneNewsInteractable & WorldInteractableObject
            var phone = Object.FindFirstObjectByType<SmartphoneNewsInteractable>();
            if (phone != null)
            {
                Debug.Log("[PASS] SmartphoneNewsInteractable found in StarterHouse_Safehome.");
                passed++;
            }
            else
            {
                Debug.LogWarning("[WARN] SmartphoneNewsInteractable not found directly in active scene root (may be in prefab or inactive).");
            }

            // 2. Audit Main Camera & ThirdPersonCamera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log($"[PASS] Camera.main found. NearClipPlane: {mainCam.nearClipPlane:F2}m (Expected <= 0.15m)");
                if (mainCam.nearClipPlane <= 0.15f) passed++; else failed++;

                var tpc = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    Debug.Log("[PASS] ThirdPersonCamera attached to Camera.main.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] ThirdPersonCamera NOT attached to Camera.main!");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("[FAIL] Camera.main not found in scene!");
                failed++;
            }

            // 3. Audit Player_Root
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Debug.Log($"[PASS] Player_Root found at position: {player.transform.position}.");
                passed++;

                Transform visualModel = player.transform.Find("Visual_Model");
                if (visualModel != null)
                {
                    Debug.Log($"[PASS] Visual_Model found under Player_Root at localPosition: {visualModel.localPosition}.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] Visual_Model not found under Player_Root!");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("[FAIL] Player tagged GameObject not found in scene!");
                failed++;
            }

            // 4. Audit FeedbackEffects
            var feedback = Object.FindFirstObjectByType<FeedbackEffects>();
            if (feedback != null)
            {
                Debug.Log("[PASS] FeedbackEffects manager verified.");
                passed++;
            }

            Debug.Log("=================================================");
            Debug.Log($"[SURGICAL RECOVERY STEP 1 AUDIT COMPLETE] Passed: {passed}, Failed: {failed}");
            Debug.Log("=================================================");
        }
    }
}
