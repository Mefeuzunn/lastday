using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Vehicles;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 1 Step 1C: Comprehensive Verification & Audit for All 5 Hero Vehicles,
    /// Headlights, Emergency Sirens, Kinetic Ramming, and SaveSystem Round-Trip Persistence.
    /// </summary>
    public static class Step1C_VehicleCompleteAudit
    {
        [MenuItem("Project Survival/Audit/Step 1C - Vehicle Complete Audit", false, 303)]
        public static void RunAudit()
        {
            Debug.Log("<color=#3B82F6>==================================================</color>");
            Debug.Log("<color=#3B82F6>[Step 1C Audit] Starting Comprehensive 5-Vehicle Verification...</color>");

            int testsPassed = 0;
            int totalTests = 0;

            // Step 1: Ensure scene is Main_Prototype
            var currentScene = EditorSceneManager.GetActiveScene();
            if (!currentScene.name.Contains("Main_Prototype"))
            {
                Debug.LogWarning("[Step 1C Audit] Active scene is not Main_Prototype. Opening Assets/_Project/Scenes/Main_Prototype.unity...");
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity");
            }

            // Step 2: Spawn/Refresh all 5 vehicles in scene
            RestorableVehiclePlacer.PlaceAllRestorableVehicles();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            string[] expectedVehicles = new string[]
            {
                "Vehicle_SafeHouse_Pickup",
                "Vehicle_Police_Interceptor",
                "Vehicle_Forest_RangerJeep",
                "Vehicle_RailYard_CargoVan",
                "Vehicle_NorthHighway_ArmoredSUV"
            };

            Vector3[] expectedPositions = new Vector3[]
            {
                new Vector3(12.5f, 0.0f, -8.0f),
                new Vector3(-42.0f, 0.0f, 36.0f),
                new Vector3(-215.0f, 0.0f, 18.0f),
                new Vector3(210.0f, 0.0f, -24.0f),
                new Vector3(14.0f, 0.0f, 195.0f)
            };

            int[] expectedTrunkSlots = new int[] { 12, 10, 10, 16, 12 };

            // Step 3: Audit Each Vehicle
            for (int i = 0; i < expectedVehicles.Length; i++)
            {
                string vName = expectedVehicles[i];
                Vector3 expPos = expectedPositions[i];
                int expSlots = expectedTrunkSlots[i];

                totalTests++;
                GameObject vObj = GameObject.Find(vName);
                if (vObj == null)
                {
                    Debug.LogError($"[Step 1C Audit] FAIL: {vName} not found in scene!");
                    continue;
                }

                // Pos check
                float dist = Vector3.Distance(vObj.transform.position, expPos);
                if (dist > 1.0f)
                {
                    Debug.LogError($"[Step 1C Audit] FAIL: {vName} position offset ({vObj.transform.position}) vs expected ({expPos})!");
                    continue;
                }
                testsPassed++;

                // Components check
                totalTests++;
                var dvc = vObj.GetComponent<DrivableVehicleController>();
                var maint = vObj.GetComponent<VehicleMaintenanceModule>();
                var trunk = vObj.GetComponent<VehicleTrunkStorage>();
                var lighting = vObj.GetComponent<VehicleLightingAndAcoustics>();
                var impact = vObj.GetComponent<VehicleImpactSensor>();
                var rb = vObj.GetComponent<Rigidbody>();
                var hull = vObj.GetComponent<BoxCollider>();

                if (dvc != null && maint != null && trunk != null && lighting != null && impact != null && rb != null && hull != null)
                {
                    testsPassed++;
                }
                else
                {
                    Debug.LogError($"[Step 1C Audit] FAIL: {vName} missing required vehicle components!");
                    continue;
                }

                // Wheel Colliders check
                totalTests++;
                var wheelCols = vObj.GetComponentsInChildren<WheelCollider>();
                if (wheelCols.Length == 4)
                {
                    testsPassed++;
                }
                else
                {
                    Debug.LogError($"[Step 1C Audit] FAIL: {vName} has {wheelCols.Length} WheelColliders (expected 4)!");
                }

                // Trunk capacity check
                totalTests++;
                if (trunk.Capacity == expSlots)
                {
                    testsPassed++;
                }
                else
                {
                    Debug.LogError($"[Step 1C Audit] FAIL: {vName} trunk capacity {trunk.Capacity} != expected {expSlots}!");
                }

                // Archetype specific checks
                if (vName == "Vehicle_Police_Interceptor")
                {
                    totalTests++;
                    Transform redSiren = vObj.transform.Find("Lighting_FX/Siren_Red");
                    Transform blueSiren = vObj.transform.Find("Lighting_FX/Siren_Blue");
                    if (redSiren != null && blueSiren != null)
                    {
                        testsPassed++;
                    }
                    else
                    {
                        Debug.LogError("[Step 1C Audit] FAIL: Police Interceptor missing red/blue emergency siren lights!");
                    }
                }
                else if (vName == "Vehicle_Forest_RangerJeep")
                {
                    totalTests++;
                    bool lifted = true;
                    foreach (var wc in wheelCols)
                    {
                        if (wc.suspensionDistance < 0.27f) lifted = false;
                    }
                    if (lifted)
                    {
                        testsPassed++;
                    }
                    else
                    {
                        Debug.LogError("[Step 1C Audit] FAIL: Forest Ranger Jeep does not have lifted 0.28m suspension!");
                    }
                }
            }

            // Step 4: Test SaveSystem Vehicle Serialization Round-Trip
            totalTests++;
            SaveData testSave = new SaveData();
            for (int i = 0; i < expectedVehicles.Length; i++)
            {
                var vObj = GameObject.Find(expectedVehicles[i]);
                var v = vObj.GetComponent<DrivableVehicleController>();

                var rec = new VehicleSaveRecord
                {
                    vehicleId = v.VehicleId,
                    posX = v.transform.position.x,
                    posY = v.transform.position.y,
                    posZ = v.transform.position.z,
                    rotY = v.transform.eulerAngles.y,
                    engineHP = 82f,
                    fuel = 64f,
                    headlightsOn = true
                };
                rec.trunkItemIds.Add("fuel_canister_full");
                rec.trunkItemCounts.Add(2);
                testSave.vehicleRecords.Add(rec);
            }

            string json = JsonUtility.ToJson(testSave);
            SaveData restored = JsonUtility.FromJson<SaveData>(json);

            if (restored != null && restored.vehicleRecords.Count == 5 &&
                restored.vehicleRecords[0].headlightsOn &&
                restored.vehicleRecords[0].trunkItemIds.Count == 1 &&
                Mathf.Approximately(restored.vehicleRecords[0].fuel, 64f))
            {
                testsPassed++;
                Debug.Log("[Step 1C Audit] SaveSystem 5-vehicle serialization round-trip test passed!");
            }
            else
            {
                Debug.LogError("[Step 1C Audit] FAIL: SaveSystem vehicle serialization round-trip failed!");
            }

            // Step 5: Verify Shaders (No magenta materials)
            totalTests++;
            bool noMagenta = true;
            var renderers = GameObject.Find("--- RESTORABLE HERO VEHICLES ---")?.GetComponentsInChildren<Renderer>(true);
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                    {
                        Debug.LogError($"[Step 1C Audit] Magenta/missing shader detected on: {r.gameObject.name}");
                        noMagenta = false;
                    }
                }
            }
            if (noMagenta) testsPassed++;

            // Save Scene
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=#10B981>[Step 1C Audit] COMPLETE: {testsPassed}/{totalTests} tests passed!</color>");
            Debug.Log("<color=#3B82F6>==================================================</color>");
        }
    }
}
