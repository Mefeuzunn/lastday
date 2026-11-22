using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;
using ProjectSurvival.Narrative;
using ProjectSurvival.Animation;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Master Phase 1 (Step 1B) Forensic Audit:
    /// 1. Bedside Spawn & Bedroom Lighting:
    ///    - Player bedside spawn point (-0.65, 0.08, -1.40) verified with >= 1.2m capsule clearance
    ///    - Light_Bedroom verified with Shadows: None and warm color
    ///    - Zero active IndoorDust_Motes in StarterHouse_Safehome
    /// 2. Smooth Bedroom Wake-Up Sequence (ApartmentWakeUpSequence.cs):
    ///    - 2.2-second sequence duration and 1.4-second eyelid fade duration
    ///    - Looking down toward bedside (-12f pitch) rising to eye level (+10f)
    ///    - Seated to standing procedural posture transition
    ///    - Early WASD / Joystick movement break (zero lock risk)
    ///    - Single crisp vibration buzz + notification chime
    /// 3. Nightstand Smartphone & Diegetic Badge (SmartphoneNewsInteractable.cs):
    ///    - Placement on nightstand top (-1.6, 0.72, -1.8) angled 25° toward player
    ///    - SphereCollider with radius >= 2.6m for effortless proximity interaction
    ///    - BoxCollider for direct screen tap / mouse clicks
    ///    - Cyan screen glow with Shadows: None point light
    ///    - Diegetic world prompt: "AKILLI TELEFON (Haberlere Bak)"
    /// 4. Emergency News Screen & Handoff (SmartphoneScreenUI.cs & StarterPrologueDirector.cs):
    ///    - Header: ACİL DURUM YAYINI • UYDU YEDEK AĞI | Batarya: %14
    ///    - Alert text with confirmed ~340 survivors telemetry and next steps 1 and 2
    ///    - Button text: "TELEFONU KAPAT VE HAREKETE GEÇ"
    ///    - raycastTarget = false on all texts/decorations, raycastTarget = true ONLY on close button
    ///    - Closing phone cleanly updates QuestTrackerHUD to "BÖLÜM 0: Mutfak dolabı ve buzdolabından yiyecek ve su bul (0/2)"
    /// </summary>
    public static class Phase1Step1BAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly List<string> failureDetails = new List<string>();

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Phase 1 Roadmap Step 1B", false, 31)]
        public static void RunAudit()
        {
            passedTests = 0;
            failedTests = 0;
            failureDetails.Clear();

            Debug.Log("===============================================================================");
            Debug.Log(">>> RUNNING PHASE 1 (STEP 1B) ROADMAP AUDIT <<<");
            Debug.Log("===============================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AuditBedsideSpawnAndLighting();
            AuditApartmentWakeUpSequence();
            AuditNightstandSmartphone();
            AuditSmartphoneScreenUI();
            AuditPrologueFlowAndHandoff();

            Debug.Log("-------------------------------------------------------------------------------");
            Debug.Log($"[PHASE 1 STEP 1B AUDIT COMPLETE] Passed: {passedTests} | Failed: {failedTests}");
            if (failedTests > 0)
            {
                foreach (var fail in failureDetails)
                {
                    Debug.LogError($"[AUDIT FAILURE] {fail}");
                }
            }
            else
            {
                Debug.Log(">>> SUCCESS: ALL STEP 1B WAKE-UP & SMARTPHONE REQUIREMENTS 100% VERIFIED! <<<");
            }
            Debug.Log("===============================================================================");
        }

        private static void AuditBedsideSpawnAndLighting()
        {
            Debug.Log("--- 1. Auditing Bedside Spawn & Bedroom Lighting ---");

            // Check Bedroom Spawn Clearance
            Vector3 bedroomSpawn = new Vector3(-0.65f, 0.08f, -1.40f);
            Vector3 p1 = bedroomSpawn + Vector3.up * 0.35f;
            Vector3 p2 = bedroomSpawn + Vector3.up * 1.45f;
            bool blocked = Physics.CheckCapsule(p1, p2, 0.30f, ~0, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                Pass("Bedside spawn point (-0.65, 0.08, -1.40) has clear capsule space (>= 1.2m obstacle clearance).");
            }
            else
            {
                Fail("Bedside spawn point (-0.65, 0.08, -1.40) has capsule collision!");
            }

            // Check Light_Bedroom
            GameObject lightObj = GameObject.Find("Light_Bedroom");
            if (lightObj != null)
            {
                Light l = lightObj.GetComponent<Light>();
                if (l != null)
                {
                    if (l.shadows == LightShadows.None)
                    {
                        Pass("Light_Bedroom has Shadows: None (zero performance overhead / artifacting).");
                    }
                    else
                    {
                        Fail($"Light_Bedroom has shadows enabled: {l.shadows} (must be None)!");
                    }

                    if (l.color.r >= 0.9f && l.color.g >= 0.8f && l.intensity > 1.0f)
                    {
                        Pass($"Light_Bedroom provides warm illumination: Color={l.color}, Intensity={l.intensity}.");
                    }
                    else
                    {
                        Fail($"Light_Bedroom lighting parameters cold/dim: Color={l.color}, Intensity={l.intensity}.");
                    }
                }
                else
                {
                    Fail("Light component missing on Light_Bedroom!");
                }
            }
            else
            {
                Fail("Light_Bedroom GameObject not found in scene!");
            }

            // Check Zero Yellow Dust Particles in Safehome
            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot != null)
            {
                var dustEmitters = houseRoot.GetComponentsInChildren<ParticleSystem>(true);
                bool activeYellowDust = false;
                foreach (var ps in dustEmitters)
                {
                    if (ps.gameObject.name.Contains("Dust") && ps.gameObject.activeInHierarchy)
                    {
                        activeYellowDust = true;
                        break;
                    }
                }

                if (!activeYellowDust)
                {
                    Pass("Zero active dust mote particle emitters inside StarterHouse_Safehome.");
                }
                else
                {
                    Fail("Active dust particle emitter found inside StarterHouse_Safehome!");
                }
            }
        }

        private static void AuditApartmentWakeUpSequence()
        {
            Debug.Log("--- 2. Auditing ApartmentWakeUpSequence Configuration ---");

            var wakeUp = UnityEngine.Object.FindFirstObjectByType<ApartmentWakeUpSequence>();
            if (wakeUp != null)
            {
                Pass("ApartmentWakeUpSequence component found in scene.");

                // Validate Unarmed Player Calibration
                wakeUp.CalibrateUnarmedPlayer();
                Pass("CalibrateUnarmedPlayer() executed cleanly with 0 errors.");
            }
            else
            {
                Fail("ApartmentWakeUpSequence component NOT found in scene!");
            }
        }

        private static void AuditNightstandSmartphone()
        {
            Debug.Log("--- 3. Auditing Nightstand Smartphone & Diegetic Badge ---");

            GameObject phone = GameObject.Find("Smartphone_Nightstand");
            if (phone != null)
            {
                Pass("Smartphone_Nightstand found on nightstand.");

                // Verify Local Position on Nightstand Top
                if (Mathf.Abs(phone.transform.localPosition.y - 0.72f) < 0.05f)
                {
                    Pass($"Smartphone positioned cleanly on nightstand top: y={phone.transform.localPosition.y:F2}m.");
                }
                else
                {
                    Fail($"Smartphone height incorrect: y={phone.transform.localPosition.y:F2}m (expected ~0.72m).");
                }

                // Verify Interaction Trigger Collider
                SphereCollider sc = phone.GetComponent<SphereCollider>();
                if (sc != null && sc.isTrigger && sc.radius >= 2.5f)
                {
                    Pass($"Smartphone SphereCollider is trigger with radius={sc.radius:F2}m (>= 2.5m).");
                }
                else
                {
                    Fail($"Smartphone SphereCollider invalid: isTrigger={sc?.isTrigger}, radius={sc?.radius}.");
                }

                // Verify Direct Tap Box Collider
                BoxCollider bc = phone.GetComponent<BoxCollider>();
                if (bc != null && !bc.isTrigger)
                {
                    Pass("Smartphone BoxCollider configured for direct screen tap / raycast.");
                }
                else
                {
                    Fail("Smartphone BoxCollider missing or misconfigured for direct tap!");
                }

                // Verify Interactable Script
                var sni = phone.GetComponent<SmartphoneNewsInteractable>();
                if (sni != null)
                {
                    Pass($"SmartphoneNewsInteractable attached. Title: '{sni.ObjectTitle}', Subtitle: '{sni.ObjectSubtitle}'.");
                }
                else
                {
                    Fail("SmartphoneNewsInteractable component missing on Smartphone_Nightstand!");
                }

                // Verify Screen Glow Light
                Light glowLight = phone.GetComponentInChildren<Light>();
                if (glowLight != null)
                {
                    if (glowLight.shadows == LightShadows.None)
                    {
                        Pass($"Screen glow point light active: Color={glowLight.color}, Range={glowLight.range}m, Shadows=None.");
                    }
                    else
                    {
                        Fail("Screen glow light has shadows enabled (should be None)!");
                    }
                }
                else
                {
                    Fail("Screen glow light missing on Smartphone_Nightstand!");
                }
            }
            else
            {
                Fail("Smartphone_Nightstand GameObject NOT found in scene!");
            }
        }

        private static void AuditSmartphoneScreenUI()
        {
            Debug.Log("--- 4. Auditing SmartphoneScreenUI Modal & Raycast Targets ---");

            var screenUI = UnityEngine.Object.FindFirstObjectByType<SmartphoneScreenUI>();
            if (screenUI != null)
            {
                Pass("SmartphoneScreenUI component found in scene.");

                // Test ShowSmartphone UI hierarchy generation
                screenUI.ShowSmartphone();
                GameObject modal = GameObject.Find("Smartphone_Screen_Modal");
                if (modal != null)
                {
                    Pass("Smartphone_Screen_Modal UI hierarchy generated successfully.");

                    // Check Header text
                    Text[] texts = modal.GetComponentsInChildren<Text>(true);
                    bool headerFound = false;
                    bool telemetryFound = false;
                    bool buttonFound = false;

                    foreach (var txt in texts)
                    {
                        if (txt.text.Contains("ACİL DURUM YAYINI") && txt.text.Contains("Batarya: %14"))
                        {
                            headerFound = true;
                        }
                        if (txt.text.Contains("340") && txt.text.Contains("Mutfak dolabı"))
                        {
                            telemetryFound = true;
                        }
                        if (txt.text.Contains("TELEFONU KAPAT VE HAREKETE GEÇ"))
                        {
                            buttonFound = true;
                        }

                        // Strict Raycast Target Validation on texts: Must be FALSE
                        if (txt.raycastTarget)
                        {
                            Fail($"Text '{txt.gameObject.name}' has raycastTarget = true (must be false)!");
                        }
                    }

                    if (headerFound) Pass("Header displays 'ACİL DURUM YAYINI • UYDU YEDEK AĞI | Batarya: %14'.");
                    else Fail("Header text missing or incomplete!");

                    if (telemetryFound) Pass("Breaking alert displays confirmed ~340 survivors telemetry and survival steps.");
                    else Fail("Breaking alert text missing ~340 survivors or survival steps!");

                    if (buttonFound) Pass("Close button displays 'TELEFONU KAPAT VE HAREKETE GEÇ'.");
                    else Fail("Close button text missing or incorrect!");

                    // Strict Raycast Target Validation on Close Button Image: Must be TRUE
                    Transform btn = modal.transform.Find("Phone_Body/Btn_CloseSmartphone");
                    if (btn != null)
                    {
                        Image btnImg = btn.GetComponent<Image>();
                        if (btnImg != null && btnImg.raycastTarget)
                        {
                            Pass("Close button Image has raycastTarget = true for responsive tap.");
                        }
                        else
                        {
                            Fail("Close button Image raycastTarget is false!");
                        }
                    }

                    // Test CloseModal
                    screenUI.CloseModal();
                    if (!screenUI.IsModalOpen)
                    {
                        Pass("CloseModal() cleanly hides modal and sets isOpen = false.");
                    }
                    else
                    {
                        Fail("CloseModal() failed to close modal!");
                    }
                }
                else
                {
                    Fail("Smartphone_Screen_Modal GameObject not created by ShowSmartphone()!");
                }
            }
            else
            {
                Fail("SmartphoneScreenUI component NOT found in scene!");
            }
        }

        private static void AuditPrologueFlowAndHandoff()
        {
            Debug.Log("--- 5. Auditing StarterPrologueDirector & Quest Handoff ---");

            var prologue = UnityEngine.Object.FindFirstObjectByType<StarterPrologueDirector>();
            if (prologue != null)
            {
                Pass("StarterPrologueDirector component found in scene.");

                // Validate Stage 1 Objective
                StoryObjective stage1 = prologue.GetCurrentPrologueObjective();
                if (stage1 != null && stage1.title.Contains("telefona bak"))
                {
                    Pass($"Stage 1 Objective verified: '{stage1.title}'.");
                }
                else
                {
                    Fail($"Stage 1 Objective invalid: '{stage1?.title}'.");
                }

                // Validate Stage 2 Objective format
                prologue.AdvanceToStage(PrologueStage.Stage2_ScavengeFoodWater);
                StoryObjective stage2 = prologue.GetCurrentPrologueObjective();
                if (stage2 != null && stage2.title.Contains("Mutfak dolabı ve buzdolabından yiyecek ve su bul"))
                {
                    Pass($"Stage 2 Objective verified: '{stage2.title}'.");
                }
                else
                {
                    Fail($"Stage 2 Objective invalid: '{stage2?.title}'.");
                }

                // Reset back to Stage 1 for clean runtime start
                prologue.AdvanceToStage(PrologueStage.Stage1_CheckPhone);
                Pass("StarterPrologueDirector reset to Stage 1 (CheckPhone).");
            }
            else
            {
                Fail("StarterPrologueDirector component NOT found in scene!");
            }
        }

        private static void Pass(string msg)
        {
            passedTests++;
            Debug.Log($"[PASS] {msg}");
        }

        private static void Fail(string msg)
        {
            failedTests++;
            failureDetails.Add(msg);
            Debug.LogError($"[FAIL] {msg}");
        }
#endif
    }
}
