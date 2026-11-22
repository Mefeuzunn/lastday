using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Environment;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class NPCCommunityEcosystemVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string IntelMapAssetPath = "Assets/_Project/Data/Items/item_map_intel.asset";

        [MenuItem("ProjectSurvival/Verify Stage 3 NPC Community & Barter Ecosystem", false, 40)]
        public static void VerifyEcosystem()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING STAGE 3 LIVING NPC ECOSYSTEM AUDIT & VERIFICATION <<<");
            Debug.Log("==================================================================");

            // First ensure setup is applied
            NPCCommunityEcosystemSetup.SetupEcosystem(true);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Verifier FAIL] Could not open scene: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int errors = 0;

            // 1. Audit NPC_Ecosystem_Manager
            GameObject mgr = GameObject.Find("NPC_Ecosystem_Manager");
            if (mgr == null)
            {
                Debug.LogError("[Verifier FAIL] NPC_Ecosystem_Manager GameObject missing from scene!");
                errors++;
            }
            else
            {
                var scheduleMgr = mgr.GetComponent<NPCDailyScheduleManager>();
                var socialEco = mgr.GetComponent<NPCSocialEcosystem>();
                var outpostMgr = mgr.GetComponent<CommunityOutpostManager>();
                var barterSys = mgr.GetComponent<BarterSystem>();

                if (scheduleMgr == null) { Debug.LogError("[Verifier FAIL] NPCDailyScheduleManager missing from NPC_Ecosystem_Manager!"); errors++; }
                if (socialEco == null) { Debug.LogError("[Verifier FAIL] NPCSocialEcosystem missing from NPC_Ecosystem_Manager!"); errors++; }
                if (outpostMgr == null) { Debug.LogError("[Verifier FAIL] CommunityOutpostManager missing from NPC_Ecosystem_Manager!"); errors++; }
                if (barterSys == null) { Debug.LogError("[Verifier FAIL] BarterSystem missing from NPC_Ecosystem_Manager!"); errors++; }

                // Test Schedule Rules
                if (scheduleMgr != null)
                {
                    if (scheduleMgr.GetCurrentTaskForNpc("aras", 10f) != NPCTaskType.MedicalDuty) { Debug.LogError("[Verifier FAIL] Dr. Aras schedule at 10:00 should be MedicalDuty!"); errors++; }
                    if (scheduleMgr.GetCurrentTaskForNpc("aras", 23f) != NPCTaskType.RestAndSocial) { Debug.LogError("[Verifier FAIL] Dr. Aras schedule at 23:00 should be RestAndSocial!"); errors++; }
                    if (scheduleMgr.GetCurrentTaskForNpc("kerem", 10f) != NPCTaskType.GuardPost) { Debug.LogError("[Verifier FAIL] Kerem schedule at 10:00 should be GuardPost!"); errors++; }
                    if (scheduleMgr.GetCurrentTaskForNpc("kerem", 16f) != NPCTaskType.WorkshopRepair) { Debug.LogError("[Verifier FAIL] Kerem schedule at 16:00 should be WorkshopRepair!"); errors++; }
                    if (scheduleMgr.GetCurrentTaskForNpc("zeynep", 10f) != NPCTaskType.WorkshopRepair) { Debug.LogError("[Verifier FAIL] Zeynep schedule at 10:00 should be WorkshopRepair!"); errors++; }
                    if (scheduleMgr.GetCurrentTaskForNpc("elif", 12f) != NPCTaskType.ScavengeRoute) { Debug.LogError("[Verifier FAIL] Elif schedule at 12:00 should be ScavengeRoute!"); errors++; }
                    Debug.Log("[Verifier PASS] NPCDailyScheduleManager daily schedule profiles validated.");
                }

                // Test Outpost Manager & Morale
                if (outpostMgr != null)
                {
                    if (outpostMgr.CommunityMorale < 70f || outpostMgr.CommunityMorale > 80f)
                    {
                        Debug.LogError($"[Verifier FAIL] Initial Community Morale expected ~75%, found {outpostMgr.CommunityMorale}%");
                        errors++;
                    }

                    if (outpostMgr.SafeHouseResidents.Count < 4)
                    {
                        Debug.LogError($"[Verifier FAIL] Safehouse residents roster too small ({outpostMgr.SafeHouseResidents.Count})");
                        errors++;
                    }

                    // Test SaveData serialization
                    SaveData testSave = new SaveData();
                    outpostMgr.PopulateSaveData(testSave);
                    if (testSave.communityMorale != outpostMgr.CommunityMorale || testSave.safeHouseResidents.Count != outpostMgr.SafeHouseResidents.Count)
                    {
                        Debug.LogError("[Verifier FAIL] OutpostManager SaveData serialization mismatch!");
                        errors++;
                    }
                    Debug.Log("[Verifier PASS] CommunityOutpostManager morale and persistence validated.");
                }

                // Test Barter System calculations & discounts
                if (barterSys != null)
                {
                    var keremStock = barterSys.GetMerchantStock("kerem");
                    var arasStock = barterSys.GetMerchantStock("aras");
                    var elifStock = barterSys.GetMerchantStock("elif");
                    var zeynepStock = barterSys.GetMerchantStock("zeynep");

                    if (keremStock.Count == 0) { Debug.LogError("[Verifier FAIL] Kerem merchant stock empty!"); errors++; }
                    if (arasStock.Count == 0) { Debug.LogError("[Verifier FAIL] Dr. Aras merchant stock empty!"); errors++; }
                    if (elifStock.Count == 0) { Debug.LogError("[Verifier FAIL] Elif merchant stock empty!"); errors++; }
                    if (zeynepStock.Count == 0) { Debug.LogError("[Verifier FAIL] Zeynep merchant stock empty!"); errors++; }

                    int axeVal = barterSys.GetItemValue("weapon_axe");
                    int medkitVal = barterSys.GetItemValue("item_medkit");
                    int intelVal = barterSys.GetItemValue("item_map_intel");

                    if (axeVal < 50 || medkitVal < 25 || intelVal < 40)
                    {
                        Debug.LogError($"[Verifier FAIL] Item valuations incorrect! Axe:{axeVal}, Medkit:{medkitVal}, Intel:{intelVal}");
                        errors++;
                    }
                    Debug.Log("[Verifier PASS] BarterSystem merchant stocks and item pricing validated.");
                }
            }

            // 2. Audit Task Anchors
            NPCTaskAnchor[] anchors = UnityEngine.Object.FindObjectsByType<NPCTaskAnchor>(FindObjectsSortMode.None);
            if (anchors.Length < 10)
            {
                Debug.LogError($"[Verifier FAIL] Expected >= 10 task anchors across city, found: {anchors.Length}");
                errors++;
            }
            else
            {
                bool hasGuard = false, hasScavenge = false, hasMedical = false, hasWorkshop = false, hasRest = false;
                for (int i = 0; i < anchors.Length; i++)
                {
                    switch (anchors[i].TaskType)
                    {
                        case NPCTaskType.GuardPost: hasGuard = true; break;
                        case NPCTaskType.ScavengeRoute: hasScavenge = true; break;
                        case NPCTaskType.MedicalDuty: hasMedical = true; break;
                        case NPCTaskType.WorkshopRepair: hasWorkshop = true; break;
                        case NPCTaskType.RestAndSocial: hasRest = true; break;
                    }
                }

                if (!hasGuard || !hasScavenge || !hasMedical || !hasWorkshop || !hasRest)
                {
                    Debug.LogError($"[Verifier FAIL] Missing anchor archetype! Guard:{hasGuard}, Scavenge:{hasScavenge}, Medical:{hasMedical}, Workshop:{hasWorkshop}, Rest:{hasRest}");
                    errors++;
                }
                else
                {
                    Debug.Log($"[Verifier PASS] Verified {anchors.Length} task anchors across all 5 operational archetypes.");
                }
            }

            // 3. Audit item_map_intel.asset
            if (!File.Exists(IntelMapAssetPath))
            {
                Debug.LogError($"[Verifier FAIL] Missing item_map_intel asset at {IntelMapAssetPath}");
                errors++;
            }
            else
            {
                ItemData intelItem = AssetDatabase.LoadAssetAtPath<ItemData>(IntelMapAssetPath);
                if (intelItem == null || intelItem.itemId != "item_map_intel")
                {
                    Debug.LogError("[Verifier FAIL] item_map_intel asset corrupt or invalid itemId!");
                    errors++;
                }
                else
                {
                    Debug.Log($"[Verifier PASS] item_map_intel.asset verified: {intelItem.itemName}");
                }
            }

            // 4. Audit SurvivorBarterUI on Canvas
            SurvivorBarterUI barterUI = UnityEngine.Object.FindAnyObjectByType<SurvivorBarterUI>(FindObjectsInactive.Include);
            if (barterUI == null)
            {
                Debug.LogError("[Verifier FAIL] SurvivorBarterUI missing from Canvas!");
                errors++;
            }
            else
            {
                SerializedObject soBarter = new SerializedObject(barterUI);
                if (soBarter.FindProperty("barterRootPanel").objectReferenceValue == null)
                {
                    Debug.LogError("[Verifier FAIL] SurvivorBarterUI.barterRootPanel unassigned!");
                    errors++;
                }
                if (soBarter.FindProperty("confirmTradeButton").objectReferenceValue == null)
                {
                    Debug.LogError("[Verifier FAIL] SurvivorBarterUI.confirmTradeButton unassigned!");
                    errors++;
                }
                Debug.Log("[Verifier PASS] SurvivorBarterUI correctly configured on Canvas.");
            }

            // 5. Audit Scavenger Squads
            AutonomousScavengerSquad[] squads = UnityEngine.Object.FindObjectsByType<AutonomousScavengerSquad>(FindObjectsSortMode.None);
            if (squads.Length < 2)
            {
                Debug.LogError($"[Verifier FAIL] Expected 2 autonomous scavenger squads, found: {squads.Length}");
                errors++;
            }
            else
            {
                for (int i = 0; i < squads.Length; i++)
                {
                    var sq = squads[i];
                    if (sq.CurrentState != SquadState.Patrolling && sq.CurrentState != SquadState.Scavenging && sq.CurrentState != SquadState.DistressPinned)
                    {
                        Debug.LogError($"[Verifier FAIL] Squad {sq.gameObject.name} in unexpected initial state: {sq.CurrentState}");
                        errors++;
                    }
                }
                Debug.Log($"[Verifier PASS] Verified {squads.Length} AutonomousScavengerSquad roaming teams.");
            }

            // Final Tally
            Debug.Log("==================================================================");
            if (errors == 0)
            {
                Debug.Log(">>> [STAGE 3 VERIFICATION PASSED] LIVING NPC ECOSYSTEM READY! <<<");
                Debug.Log("==================================================================");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($">>> [STAGE 3 VERIFICATION FAILED] {errors} ERRORS ENCOUNTERED! <<<");
                Debug.Log("==================================================================");
                EditorApplication.Exit(1);
            }
        }
    }
}
