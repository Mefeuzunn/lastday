using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.UI;
using ProjectSurvival.AI;

namespace ProjectSurvival.EditorScripts
{
    public static class Step2A_TraumaAudit
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Tools/Survival/Run Step 2A Trauma & Injury Audit", false, 202)]
        public static void RunAudit()
        {
            Debug.Log("====================================================================");
            Debug.Log(">>> STARTING ATOMIC MICRO-STEP 2A: ANATOMICAL TRAUMA AUDIT <<<");
            Debug.Log("====================================================================");

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step2A_Audit] Failed to open scene: {SCENE_PATH}");
                return;
            }

            int passedChecks = 0;
            int totalChecks = 0;

            // ---------------------------------------------------------------------
            // 1. CHECK MEDICAL ITEM ASSETS IN DATA & RESOURCES
            // ---------------------------------------------------------------------
            totalChecks++;
            ItemData splintRes = Resources.Load<ItemData>("Items/item_splint");
            ItemData painkillersRes = Resources.Load<ItemData>("Items/item_painkillers");
            ItemData splintAsset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_splint.asset");
            ItemData painkillersAsset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_painkillers.asset");

            if (splintRes != null && painkillersRes != null && splintAsset != null && painkillersAsset != null)
            {
                Debug.Log($"[Step2A_Audit] PASS: Medical assets item_splint and item_painkillers validated in Data and Resources.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Medical assets missing! splintRes:{splintRes != null}, painkillersRes:{painkillersRes != null}");
            }

            // ---------------------------------------------------------------------
            // 2. CHECK PLAYER_ROOT & TRAUMA ENGINE ATTACHMENT
            // ---------------------------------------------------------------------
            totalChecks++;
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var allRoots = scene.GetRootGameObjects();
                foreach (var r in allRoots)
                {
                    if (r.name.Contains("Player")) { player = r; break; }
                }
            }

            if (player == null)
            {
                Debug.LogError("[Step2A_Audit] FAIL: Player object not found in scene!");
                return;
            }

            TraumaAndInjurySystem trauma = player.GetComponent<TraumaAndInjurySystem>();
            if (trauma == null)
            {
                trauma = player.AddComponent<TraumaAndInjurySystem>();
                EditorUtility.SetDirty(player);
                Debug.Log("[Step2A_Audit] Added missing TraumaAndInjurySystem to Player_Root.");
            }

            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            PlayerController pController = player.GetComponent<PlayerController>();

            if (trauma != null && stats != null && pController != null)
            {
                Debug.Log($"[Step2A_Audit] PASS: Player_Root verified with TraumaAndInjurySystem, SurvivalStats, and PlayerController.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Incomplete player components on {player.name}!");
            }

            // ---------------------------------------------------------------------
            // 3. BLEEDING STATE & DPS VERIFICATION
            // ---------------------------------------------------------------------
            totalChecks++;
            trauma.InflictBleeding(1);
            bool bleedLightOk = trauma.IsBleeding && trauma.BleedSeverity == 1;
            trauma.InflictBleeding(2);
            bool bleedHeavyOk = trauma.IsBleeding && trauma.BleedSeverity == 2;
            bool bleedTreatOk = trauma.TreatBleeding() && !trauma.IsBleeding;

            if (bleedLightOk && bleedHeavyOk && bleedTreatOk)
            {
                Debug.Log("[Step2A_Audit] PASS: Bleeding infliction (Light/Heavy) and Treatment transitions verified.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Bleeding state check failed! Light:{bleedLightOk}, Heavy:{bleedHeavyOk}, Treat:{bleedTreatOk}");
            }

            // ---------------------------------------------------------------------
            // 4. LEG FRACTURE & SPRINT LOCK VERIFICATION
            // ---------------------------------------------------------------------
            totalChecks++;
            trauma.InflictLegFracture(4.2f);
            bool fractureActive = trauma.HasLegFracture;
            bool sprintLocked = !stats.CanSprint();
            bool fractureTreated = trauma.TreatLegFracture() && !trauma.HasLegFracture;
            bool sprintUnlocked = stats.CanSprint();

            if (fractureActive && sprintLocked && fractureTreated && sprintUnlocked)
            {
                Debug.Log("[Step2A_Audit] PASS: Leg fracture locks sprint and restores mobility cleanly upon treatment.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Leg fracture check failed! Active:{fractureActive}, Locked:{sprintLocked}, Treated:{fractureTreated}, Unlocked:{sprintUnlocked}");
            }

            // ---------------------------------------------------------------------
            // 5. ACUTE FEVER & PAINKILLERS SUPPRESSION VERIFICATION
            // ---------------------------------------------------------------------
            totalChecks++;
            trauma.InflictAcuteFever();
            bool feverActive = trauma.HasAcuteFever;
            bool suppressOk = trauma.TreatFever(temporary: true) && trauma.IsFeverSuppressed;
            bool cureOk = trauma.TreatFever(temporary: false) && !trauma.HasAcuteFever && !trauma.IsFeverSuppressed;

            if (feverActive && suppressOk && cureOk)
            {
                Debug.Log("[Step2A_Audit] PASS: Acute fever triggers, painkiller temporary suppression, and antiviral permanent cure verified.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Acute fever checks failed! Fever:{feverActive}, Suppress:{suppressOk}, Cure:{cureOk}");
            }

            // ---------------------------------------------------------------------
            // 6. SMART TRIAGE & RESOURCE CONSERVATION (ANTI-WASTE LOGIC)
            // ---------------------------------------------------------------------
            totalChecks++;
            // Player is now fully cured/uninjured
            bool wasteSplintBlocked = !trauma.TryUseMedicalItem("item_splint");
            bool wasteBandageBlocked = !trauma.TryUseMedicalItem("item_bandage");

            // Inflict fracture and test smart splint usage
            trauma.InflictLegFracture(4.0f);
            bool splintUsedSuccess = trauma.TryUseMedicalItem("item_splint") && !trauma.HasLegFracture;

            // Inflict bleeding and test smart bandage usage
            trauma.InflictBleeding(1);
            bool bandageUsedSuccess = trauma.TryUseMedicalItem("item_bandage") && !trauma.IsBleeding;

            if (wasteSplintBlocked && wasteBandageBlocked && splintUsedSuccess && bandageUsedSuccess)
            {
                Debug.Log("[Step2A_Audit] PASS: Smart triage validator prevents resource wasting when uninjured and consumes correctly when injured.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: Smart triage check failed! WasteSplint:{wasteSplintBlocked}, WasteBandage:{wasteBandageBlocked}, SplintUse:{splintUsedSuccess}, BandageUse:{bandageUsedSuccess}");
            }

            // ---------------------------------------------------------------------
            // 7. VITAL STATUS HUD BADGES VERIFICATION
            // ---------------------------------------------------------------------
            totalChecks++;
            VitalStatusHUD vitalHUD = UnityEngine.Object.FindFirstObjectByType<VitalStatusHUD>();
            bool hudOk = false;
            if (vitalHUD != null)
            {
                // Trigger dynamic trauma container creation if not built yet
                var mEnsure = typeof(VitalStatusHUD).GetMethod("EnsureTraumaBadges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mEnsure?.Invoke(vitalHUD, null);

                Transform badgesContainer = vitalHUD.transform.Find("TraumaBadges_Container");
                if (badgesContainer != null)
                {
                    Transform bBleed = badgesContainer.Find("Badge_Bleeding");
                    Transform bFracture = badgesContainer.Find("Badge_Fracture");
                    Transform bFever = badgesContainer.Find("Badge_Fever");

                    if (bBleed != null && bFracture != null && bFever != null)
                    {
                        hudOk = true;
                    }
                }
            }

            if (hudOk)
            {
                Debug.Log("[Step2A_Audit] PASS: VitalStatusHUD contains interactive Trauma Badges container with KANAMA, KIRIK, and ATEŞ pills.");
                passedChecks++;
            }
            else
            {
                Debug.LogError("[Step2A_Audit] FAIL: VitalStatusHUD trauma badges container could not be verified!");
            }

            // ---------------------------------------------------------------------
            // 8. SHADER AUDIT (ZERO MAGENTA SHADERS)
            // ---------------------------------------------------------------------
            totalChecks++;
            int magentaCount = 0;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                foreach (var mat in rend.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        magentaCount++;
                        Debug.LogWarning($"[Step2A_Audit] Magenta shader detected on {rend.gameObject.name} with material {mat.name}");
                    }
                }
            }

            if (magentaCount == 0)
            {
                Debug.Log($"[Step2A_Audit] PASS: Zero magenta shaders detected across {renderers.Length} renderers.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2A_Audit] FAIL: {magentaCount} magenta shaders detected!");
            }

            // ---------------------------------------------------------------------
            // SAVE SCENE
            // ---------------------------------------------------------------------
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("====================================================================");
            Debug.Log($"[Step2A_Audit] AUDIT COMPLETE: {passedChecks}/{totalChecks} CHECKS PASSED!");
            Debug.Log("====================================================================");
        }
    }
}
