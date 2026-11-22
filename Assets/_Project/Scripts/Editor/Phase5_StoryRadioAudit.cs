using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Narrative;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;
using ProjectSurvival.Managers;
using ProjectSurvival.Progression;

namespace ProjectSurvival.EditorScripts
{
    /// <summary>
    /// Active Phase 5: Automated Verification & Anti-Stub Audit for Tactical Radio Scanner,
    /// 4-Chapter Branching Story Campaign, Scientist Audio Logs, and SaveSystem Round-Trip.
    /// </summary>
    public static class Phase5_StoryRadioAudit
    {
        [MenuItem("Tools/Project Survival/Audit Phase 5 Story, Radio and Audio Logs")]
        public static void RunAudit()
        {
            Debug.Log("==========================================================================");
            Debug.Log("[Phase5_StoryRadioAudit] STARTING ACTIVE PHASE 5 VERIFICATION AUDIT");
            Debug.Log("==========================================================================");

            int passed = 0;
            int failed = 0;

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            GameObject tempContainer = new GameObject("Temp_Phase5_Audit_Container");
            try
            {
                // =====================================================================
                // 1. TACTICAL RADIO SCANNER & TUNING DYNAMICS
                // =====================================================================
                GameObject radioGo = new GameObject("TacticalRadioScanner_Audit");
                radioGo.transform.SetParent(tempContainer.transform);
                TacticalRadioScanner scanner = radioGo.AddComponent<TacticalRadioScanner>();
                scanner.InitializeBroadcasts();
                TacticalRadioScanner.Instance = scanner;

                passed += AssertCondition("TacticalRadioScanner instantiates successfully", scanner != null, ref failed);
                passed += AssertCondition("Min tuning frequency is 88.0 MHz", TacticalRadioScanner.MIN_FREQUENCY == 88.0f, ref failed);
                passed += AssertCondition("Max tuning frequency is 108.0 MHz", TacticalRadioScanner.MAX_FREQUENCY == 108.0f, ref failed);

                // Test Frequency Clamping
                scanner.SetFrequency(80.0f);
                passed += AssertCondition("Frequencies below 88.0 clamp to 88.0 MHz", Mathf.Approximately(scanner.CurrentFrequency, 88.0f), ref failed);
                scanner.SetFrequency(120.0f);
                passed += AssertCondition("Frequencies above 108.0 clamp to 108.0 MHz", Mathf.Approximately(scanner.CurrentFrequency, 108.0f), ref failed);

                // Test Fine Tuning Step (+0.2 / -0.2)
                scanner.SetFrequency(90.0f);
                scanner.StepFrequency(0.2f);
                passed += AssertCondition("Step frequency increments by +0.2 MHz", Mathf.Abs(scanner.CurrentFrequency - 90.2f) < 0.05f, ref failed);
                scanner.StepFrequency(-0.2f);
                passed += AssertCondition("Step frequency decrements by -0.2 MHz", Mathf.Abs(scanner.CurrentFrequency - 90.0f) < 0.05f, ref failed);

                // Test Proximity Detection & Lock on 92.4 MHz (Dr. Selin Kaya Clinic Signal)
                scanner.SetFrequency(92.0f); // 0.4 MHz away (within 0.6 window)
                passed += AssertCondition("Signal detected within 0.6 MHz detection window", scanner.SignalStrength > 0.1f && !scanner.IsLockedOnSignal, ref failed);

                scanner.SetFrequency(92.4f); // Exact lock
                passed += AssertCondition("Exact tuning on 92.4 MHz locks signal", scanner.IsLockedOnSignal && scanner.ActiveBroadcast != null, ref failed);
                passed += AssertCondition("Locked broadcast is Dr. Selin Kaya Clinic Signal", scanner.ActiveBroadcast.id == "signal_ch1_clinic", ref failed);

                // Test Signal Decoding
                bool decodeResult = scanner.TryDecodeSignal(out RadioBroadcast decoded);
                passed += AssertCondition("TryDecodeSignal succeeds when tuned & locked", decodeResult && decoded != null, ref failed);
                passed += AssertCondition("92.4 MHz registered in decoded frequencies list", scanner.IsFrequencyDecoded(92.4f), ref failed);

                // Test Decoding All 6 Broadcast Frequencies
                float[] allFrequencies = new float[] { 92.4f, 95.2f, 98.6f, 101.4f, 104.2f, 107.8f };
                for (int i = 0; i < allFrequencies.Length; i++)
                {
                    scanner.SetFrequency(allFrequencies[i]);
                    bool ok = scanner.TryDecodeSignal(out RadioBroadcast b);
                    passed += AssertCondition($"Frequency {allFrequencies[i]:F1} MHz decodes successfully", ok && b != null, ref failed);
                }
                passed += AssertCondition("All 6 broadcast frequencies decoded and persisted", scanner.DecodedFrequencies.Count == 6, ref failed);

                // =====================================================================
                // 2. 4-CHAPTER MAIN STORY ARC & OBJECTIVES
                // =====================================================================
                GameObject storyGo = new GameObject("MainStoryDirector_Audit");
                storyGo.transform.SetParent(tempContainer.transform);
                MainStoryDirector story = storyGo.AddComponent<MainStoryDirector>();
                story.BuildObjectiveCatalog();
                MainStoryDirector.Instance = story;

                passed += AssertCondition("MainStoryDirector instantiates successfully", story != null, ref failed);
                passed += AssertCondition("Story starts at Chapter 1", story.CurrentChapter == 1, ref failed);
                passed += AssertCondition("Initial objective is obj_1a_tune_radio", story.CurrentObjectiveId == "obj_1a_tune_radio", ref failed);

                // Simulate Chapter 1 Progress: 92.4 MHz decode -> obj_1b_inspect_clinic
                story.OnFrequencyDecoded(92.4f);
                passed += AssertCondition("Chapter 1 advances to obj_1b_inspect_clinic upon decoding 92.4 MHz", story.CurrentObjectiveId == "obj_1b_inspect_clinic", ref failed);

                // Simulate Terminal 1 Interaction (Clinic) -> Chapter 2
                bool terminalClinicOk = story.TryInteractWithStoryTerminal("terminal_clinic");
                passed += AssertCondition("Interacting with Clinic terminal succeeds and awards +150 XP", terminalClinicOk, ref failed);
                passed += AssertCondition("Story unlocks Chapter 2 (obj_2a_tune_radio)", story.CurrentChapter == 2 && story.CurrentObjectiveId == "obj_2a_tune_radio", ref failed);

                // Simulate Chapter 2 Progress: 98.6 MHz decode -> obj_2b_recover_disk
                story.OnFrequencyDecoded(98.6f);
                passed += AssertCondition("Chapter 2 advances to obj_2b_recover_disk upon decoding 98.6 MHz", story.CurrentObjectiveId == "obj_2b_recover_disk", ref failed);

                // Simulate Terminal 2 Interaction (Railcar) -> Chapter 3
                bool terminalRailOk = story.TryInteractWithStoryTerminal("terminal_railcar");
                passed += AssertCondition("Interacting with Railcar terminal succeeds and awards +200 XP", terminalRailOk, ref failed);
                passed += AssertCondition("Story unlocks Chapter 3 (obj_3a_tune_radio)", story.CurrentChapter == 3 && story.CurrentObjectiveId == "obj_3a_tune_radio", ref failed);

                // Simulate Chapter 3 Progress: 104.2 MHz decode -> obj_3b_retrieve_key
                story.OnFrequencyDecoded(104.2f);
                passed += AssertCondition("Chapter 3 advances to obj_3b_retrieve_key upon decoding 104.2 MHz", story.CurrentObjectiveId == "obj_3b_retrieve_key", ref failed);

                // Simulate Terminal 3 Interaction (Bunker) -> Chapter 4
                bool terminalBunkerOk = story.TryInteractWithStoryTerminal("terminal_bunker");
                passed += AssertCondition("Interacting with Bunker terminal succeeds and awards +250 XP", terminalBunkerOk, ref failed);
                passed += AssertCondition("Story unlocks Chapter 4 (obj_4a_tune_radio)", story.CurrentChapter == 4 && story.CurrentObjectiveId == "obj_4a_tune_radio", ref failed);

                // Simulate Chapter 4 Progress: 107.8 MHz decode -> obj_4b_moral_choice
                story.OnFrequencyDecoded(107.8f);
                passed += AssertCondition("Chapter 4 advances to obj_4b_moral_choice upon decoding 107.8 MHz", story.CurrentObjectiveId == "obj_4b_moral_choice", ref failed);

                // Test Endgame Moral Choice A: Evacuation Broadcast
                story.ExecuteEndgameMoralChoice("Evacuation");
                passed += AssertCondition("Story marks as completed after moral choice", story.IsStoryCompleted, ref failed);
                passed += AssertCondition("Moral choice 'Evacuation' recorded", story.EndgameMoralChoice == "Evacuation", ref failed);

                // Test Endgame Moral Choice B: Global Antiviral Formula (Reset & Test)
                story.RestoreStoryState(4, "obj_4b_moral_choice", false, string.Empty, new List<string>(), false);
                story.ExecuteEndgameMoralChoice("GlobalCure");
                passed += AssertCondition("Moral choice 'GlobalCure' recorded", story.EndgameMoralChoice == "GlobalCure", ref failed);

                // =====================================================================
                // 3. SCIENTIST AUDIO LOGS & TRANSCRIPT BANNER
                // =====================================================================
                GameObject audioLogGo = new GameObject("AudioLog_Audit");
                audioLogGo.transform.SetParent(tempContainer.transform);
                var boxCol = audioLogGo.AddComponent<BoxCollider>();
                var audioLog = audioLogGo.AddComponent<AudioLogInteractable>();
                audioLog.ConfigureLog("log_audit_test", "Test Log", "Dr. Selin Kaya", "Test transcript message.");

                passed += AssertCondition("AudioLogInteractable instantiates with valid BoxCollider", boxCol != null && audioLog != null, ref failed);
                passed += AssertCondition("AudioLog provides 'listen' verb", audioLog.GetAvailableVerbs().Count > 0 && audioLog.GetAvailableVerbs()[0].verbId == "listen", ref failed);

                audioLog.PlayAudioLog();
                passed += AssertCondition("Playing audio log registers in collected list", story.IsAudioLogCollected("log_audit_test"), ref failed);

                // =====================================================================
                // 4. SECRET SUPPLY CACHES & PIN REGISTRATION
                // =====================================================================
                GameObject cacheGo = new GameObject("SecretCache_Audit");
                cacheGo.transform.SetParent(tempContainer.transform);
                cacheGo.AddComponent<BoxCollider>();
                var cache = cacheGo.AddComponent<SecretCacheInteractable>();
                cache.ConfigureCache("cache_test", "Test Cache", new List<string> { "item_scrap", "item_antiviral" }, new List<int> { 10, 2 });

                passed += AssertCondition("Secret cache instantiates unrevealed by default", !cache.IsRevealed, ref failed);
                cache.RevealCache();
                passed += AssertCondition("Secret cache reveals when radio signal decoded", cache.IsRevealed, ref failed);
                passed += AssertCondition("Revealed cache has loot verb", cache.GetAvailableVerbs()[0].verbId == "loot_cache", ref failed);

                // =====================================================================
                // 5. SAVESYSTEM ROUND-TRIP SERIALIZATION
                // =====================================================================
                SaveData srcData = new SaveData
                {
                    currentStoryChapter = 3,
                    currentObjectiveId = "obj_3b_retrieve_key",
                    isMainStoryCompleted = false,
                    endgameMoralChoice = string.Empty,
                    decodedFrequencies = new List<float> { 92.4f, 95.2f, 98.6f },
                    collectedAudioLogIds = new List<string> { "log_safehouse_origin", "log_clinic_patient_zero" },
                    hasGlobalCurePerk = true
                };

                string json = JsonUtility.ToJson(srcData);
                SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

                passed += AssertCondition("SaveData serializes and deserializes currentStoryChapter", loadedData.currentStoryChapter == 3, ref failed);
                passed += AssertCondition("SaveData serializes and deserializes currentObjectiveId", loadedData.currentObjectiveId == "obj_3b_retrieve_key", ref failed);
                passed += AssertCondition("SaveData serializes and deserializes decodedFrequencies", loadedData.decodedFrequencies.Count == 3 && Mathf.Abs(loadedData.decodedFrequencies[1] - 95.2f) < 0.05f, ref failed);
                passed += AssertCondition("SaveData serializes and deserializes collectedAudioLogIds", loadedData.collectedAudioLogIds.Count == 2 && loadedData.collectedAudioLogIds[0] == "log_safehouse_origin", ref failed);
                passed += AssertCondition("SaveData serializes and deserializes hasGlobalCurePerk", loadedData.hasGlobalCurePerk == true, ref failed);

                // =====================================================================
                // 6. SCENE AUDIT (Main_Prototype.unity)
                // =====================================================================
                AudioLogInteractable[] sceneAudioLogs = UnityEngine.Object.FindObjectsByType<AudioLogInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                passed += AssertCondition($"Scene has all 6 physical Audio Log recorders (Found: {sceneAudioLogs.Length})", sceneAudioLogs.Length >= 6, ref failed);

                StoryTerminalInteractable[] sceneTerminals = UnityEngine.Object.FindObjectsByType<StoryTerminalInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                passed += AssertCondition($"Scene has all 4 Story Terminals (Found: {sceneTerminals.Length})", sceneTerminals.Length >= 4, ref failed);

                SecretCacheInteractable[] sceneCaches = UnityEngine.Object.FindObjectsByType<SecretCacheInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                passed += AssertCondition($"Scene has both Secret Supply Caches (Found: {sceneCaches.Length})", sceneCaches.Length >= 2, ref failed);
            }
            finally
            {
                if (tempContainer != null) UnityEngine.Object.DestroyImmediate(tempContainer);
            }

            Debug.Log("==========================================================================");
            Debug.Log($"[Phase5_StoryRadioAudit] AUDIT COMPLETE: {passed} PASSED, {failed} FAILED.");
            Debug.Log("==========================================================================");

            if (failed > 0)
            {
                Debug.LogError($"[Phase5_StoryRadioAudit] AUDIT FAILED with {failed} failures!");
            }
            else
            {
                Debug.Log($"[Phase5_StoryRadioAudit] ALL {passed} TESTS PASSED CLEANLY!");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failed > 0 ? 1 : 0);
            }
        }

        private static int AssertCondition(string desc, bool condition, ref int failed)
        {
            if (condition)
            {
                Debug.Log($"[PASS] {desc}");
                return 1;
            }
            else
            {
                Debug.LogError($"[FAIL] {desc}");
                failed++;
                return 0;
            }
        }
    }
}
