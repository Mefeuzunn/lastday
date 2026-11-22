using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 4 (Step 4B): Auto-Runner for CC0 3D Vehicles and Character Models Upgrade & Audit.
    /// Automatically executes on Editor reload to ensure scene assets are updated and verified.
    /// Also exposed via ProjectSurvival/Execute Complete Step 4B Workflow & Audit menu item.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class Step4B_VehicleCharacterAutoRunner
    {
        private const string SessionKey = "Phase4_Step4B_AutoRunner_Executed_v1";

        static Step4B_VehicleCharacterAutoRunner()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        [MenuItem("ProjectSurvival/Execute Complete Step 4B Workflow & Audit", false, 35)]
        public static void ExecuteWorkflowManual()
        {
            RunStep4BWorkflow(force: true);
        }

        private static void OnEditorReady()
        {
            RunStep4BWorkflow(force: false);
        }

        private static void RunStep4BWorkflow(bool force)
        {
            if (!force && SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            Debug.Log("===============================================================================");
            Debug.Log(">>> EXECUTING AUTOMATED STEP 4B: VEHICLES & CHARACTERS 3D UPGRADE & AUDIT <<<");
            Debug.Log("===============================================================================");

            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Vehicle3DVisualUpgrader.UpgradeAllVehiclesMenu();
                Character3DVisualUpgrader.UpgradeAllCharactersMenu();
                Step4B_VehicleCharacter3DAudit.RunAudit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Step4B_VehicleCharacterAutoRunner] Execution failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
#endif
}
