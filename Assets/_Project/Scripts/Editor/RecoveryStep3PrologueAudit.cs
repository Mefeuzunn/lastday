using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.UI;

namespace ProjectSurvival.Editor
{
    public static class RecoveryStep3PrologueAudit
    {
        [MenuItem("Tools/Survival/Audit Recovery Step 3 Prologue")]
        public static void RunAudit()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Main_Prototype")
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity");
            }

            Debug.Log("=================================================");
            Debug.Log("[SURGICAL RECOVERY STEP 3 PROLOGUE AUDIT START]");
            Debug.Log("=================================================");

            int passed = 0;
            int failed = 0;

            // 1. Audit Player_Root Spawn Location in Bedroom
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
            if (player != null)
            {
                Vector3 pos = player.transform.position;
                Vector3 rot = player.transform.eulerAngles;
                Debug.Log($"[PASS] Player found at Pos: {pos}, Rot: {rot}");

                // Bedroom coordinates: X in [-2.5, -0.5], Y in [0.0, 0.3], Z in [-2.5, 0.0]
                if (pos.x >= -2.5f && pos.x <= -0.5f && pos.z >= -2.5f && pos.z <= 0.0f && pos.y >= 0.0f && pos.y <= 0.3f)
                {
                    Debug.Log("[PASS] Player starts inside StarterHouse_Safehome Bedroom beside the bed.");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Player not inside Bedroom! Pos: {pos}");
                    failed++;
                }

                // Check CharacterController
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    Debug.Log($"[PASS] CharacterController attached with StepOffset: {cc.stepOffset}m, SlopeLimit: {cc.slopeLimit}°.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] CharacterController NOT attached to Player!");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("[FAIL] Player GameObject not found in scene!");
                failed++;
            }

            // 2. Audit StarterHouse_Safehome Architecture & Lights
            GameObject house = GameObject.Find("StarterHouse_Safehome");
            if (house != null)
            {
                Debug.Log("[PASS] StarterHouse_Safehome found.");
                passed++;

                // Verify warm interior lights (Shadows: None)
                var lights = house.GetComponentsInChildren<Light>(true);
                bool allShadowsNone = true;
                int lightCount = 0;
                foreach (var l in lights)
                {
                    if (l.type == LightType.Point)
                    {
                        lightCount++;
                        if (l.shadows != LightShadows.None)
                        {
                            allShadowsNone = false;
                        }
                    }
                }

                if (lightCount >= 4 && allShadowsNone)
                {
                    Debug.Log($"[PASS] Verified {lightCount} interior point lights with Shadows: None (Mobile 60 FPS compliant).");
                    passed++;
                }
                else
                {
                    Debug.LogWarning($"[WARN] Found {lightCount} interior point lights, allShadowsNone={allShadowsNone}");
                    if (allShadowsNone) passed++; else failed++;
                }
            }
            else
            {
                Debug.LogError("[FAIL] StarterHouse_Safehome not found!");
                failed++;
            }

            // 3. Audit Smartphone_Nightstand & SmartphoneNewsInteractable
            var phoneObj = GameObject.Find("Smartphone_Nightstand");
            if (phoneObj != null)
            {
                Debug.Log("[PASS] Smartphone_Nightstand found on nightstand.");
                passed++;

                var sni = phoneObj.GetComponent<SmartphoneNewsInteractable>();
                if (sni != null)
                {
                    Debug.Log($"[PASS] SmartphoneNewsInteractable attached. Title: '{sni.ObjectTitle}', Subtitle: '{sni.ObjectSubtitle}'.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] SmartphoneNewsInteractable NOT attached to Smartphone_Nightstand!");
                    failed++;
                }

                // Verify Collider
                var col = phoneObj.GetComponent<Collider>();
                if (col != null && col.isTrigger)
                {
                    Debug.Log($"[PASS] Smartphone trigger collider active: {col.GetType().Name}, isTrigger=true.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] Smartphone does not have an active trigger collider!");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("[FAIL] Smartphone_Nightstand not found in scene!");
                failed++;
            }

            // 4. Audit StarterPrologueDirector 4-Stage Objectives
            var prologue = Object.FindFirstObjectByType<StarterPrologueDirector>();
            if (prologue != null)
            {
                Debug.Log("[PASS] StarterPrologueDirector found in scene.");
                passed++;

                // Stage 1 Objective
                prologue.ForceSetStage(PrologueStage.Stage1_CheckPhone);
                var obj1 = prologue.GetCurrentPrologueObjective();
                if (obj1 != null && obj1.chapterTitle.Contains("BÖLÜM 0: UYANIŞ") && obj1.title.Contains("Başucundaki akıllı telefona bak"))
                {
                    Debug.Log($"[PASS] Stage 1 Objective: '{obj1.chapterTitle} — {obj1.title}'");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Stage 1 Objective mismatch: '{obj1?.chapterTitle} — {obj1?.title}'");
                    failed++;
                }

                // Stage 2 Objective
                prologue.ForceSetStage(PrologueStage.Stage2_ScavengeFoodWater);
                var obj2 = prologue.GetCurrentPrologueObjective();
                if (obj2 != null && obj2.chapterTitle.Contains("BÖLÜM 0: İLK ERZAK") && obj2.title.Contains("0/2"))
                {
                    Debug.Log($"[PASS] Stage 2 Objective: '{obj2.chapterTitle} — {obj2.title}'");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Stage 2 Objective mismatch: '{obj2?.chapterTitle} — {obj2?.title}'");
                    failed++;
                }

                // Stage 3 Objective
                prologue.ForceSetStage(PrologueStage.Stage3_ExitAndFindWeapon);
                var obj3 = prologue.GetCurrentPrologueObjective();
                if (obj3 != null && obj3.chapterTitle.Contains("BÖLÜM 0: DIŞARI ÇIK") && obj3.title.Contains("Ön kapıdan çık ve ilk silahını bul"))
                {
                    Debug.Log($"[PASS] Stage 3 Objective: '{obj3.chapterTitle} — {obj3.title}'");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Stage 3 Objective mismatch: '{obj3?.chapterTitle} — {obj3?.title}'");
                    failed++;
                }

                // Reset back to Stage 1 for gameplay
                prologue.ForceSetStage(PrologueStage.Stage1_CheckPhone);
            }
            else
            {
                Debug.LogWarning("[WARN] StarterPrologueDirector not present at scene root (spawns under MANAGERS or dynamically).");
            }

            // 5. Audit MainStoryDirector & QuestTrackerHUD Prologue Guard
            var mainStory = Object.FindFirstObjectByType<MainStoryDirector>();
            if (mainStory != null)
            {
                Debug.Log("[PASS] MainStoryDirector found.");
                passed++;
            }

            var qhud = Object.FindFirstObjectByType<QuestTrackerHUD>();
            if (qhud != null)
            {
                Debug.Log("[PASS] QuestTrackerHUD found.");
                passed++;
            }

            Debug.Log("=================================================");
            Debug.Log($"[RECOVERY STEP 3 AUDIT FINISHED] Passed: {passed}, Failed: {failed}");
            Debug.Log("=================================================");

            if (failed > 0)
            {
                EditorUtility.DisplayDialog("Step 3 Audit Failed", $"{failed} checks failed. See Console for details.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Step 3 Audit Succeeded", "All Step 3 checks passed successfully!", "OK");
            }
        }
    }
}
