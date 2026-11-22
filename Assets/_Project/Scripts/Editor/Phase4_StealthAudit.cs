using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorScripts
{
    public static class Phase4_StealthAudit
    {
        [MenuItem("Tools/Project Survival/Audit Phase 4 Stealth and Physics")]
        public static void RunAudit()
        {
            Debug.Log("==========================================================================");
            Debug.Log("[Phase4_StealthAudit] STARTING PHASE 4 DEEP STEALTH & THROWING PHYSICS AUDIT");
            Debug.Log("==========================================================================");

            int passed = 0;
            int failed = 0;

            GameObject tempContainer = new GameObject("Temp_Phase4_Audit_Container");
            try
            {
                // =====================================================================
                // 1. STEALTH VISIBILITY CONTROLLER & EXPOSURE SCALING
                // =====================================================================
                GameObject svcGo = new GameObject("StealthVisibilityController_Audit");
                svcGo.transform.SetParent(tempContainer.transform);
                StealthVisibilityController svc = svcGo.AddComponent<StealthVisibilityController>();

                passed += AssertCondition("StealthVisibilityController instantiates successfully", svc != null, ref failed);
                passed += AssertCondition("Default visibility score is clamped between 0.05 and 1.0", svc.CurrentVisibilityScore >= 0.05f && svc.CurrentVisibilityScore <= 1.0f, ref failed);

                float initialMult = svc.GetVisionDistanceMultiplier();
                passed += AssertCondition("Vision distance multiplier operates in realistic bounds (0.28x - 1.45x)", initialMult >= 0.28f && initialMult <= 1.45f, ref failed);

                // Test Foliage Cover Reduction (-45%)
                svc.SetManualFoliageCover(true);
                float foliageScore = svc.CurrentVisibilityScore;
                passed += AssertCondition("Foliage cover reduces visibility score", foliageScore <= 0.60f, ref failed);
                svc.SetManualFoliageCover(false);

                // =====================================================================
                // 2. DIEGETIC STEALTH EYE HUD INDICATOR
                // =====================================================================
                GameObject hudGo = new GameObject("StealthIndicatorHUD_Audit");
                hudGo.transform.SetParent(tempContainer.transform);
                StealthIndicatorHUD eyeHUD = hudGo.AddComponent<StealthIndicatorHUD>();

                passed += AssertCondition("StealthIndicatorHUD instantiates successfully", eyeHUD != null, ref failed);
                passed += AssertCondition("StealthEyeState has 3 distinct states (Hidden, Suspicious, Exposed)", Enum.GetValues(typeof(StealthEyeState)).Length == 3, ref failed);

                // =====================================================================
                // 3. CORPSE DRAGGING & CONCEALMENT SYSTEM
                // =====================================================================
                GameObject bcsGo = new GameObject("BodyConcealmentSystem_Audit");
                bcsGo.transform.SetParent(tempContainer.transform);
                BodyConcealmentSystem bcs = bcsGo.AddComponent<BodyConcealmentSystem>();

                passed += AssertCondition("BodyConcealmentSystem instantiates successfully", bcs != null, ref failed);
                passed += AssertCondition("Initial dragging state is false", !bcs.IsDraggingCorpse, ref failed);

                // Test Zombie Corpse Registration
                GameObject fakeCorpse = new GameObject("Zombie_Corpse_Test");
                fakeCorpse.transform.SetParent(tempContainer.transform);
                fakeCorpse.transform.position = new Vector3(10f, 0f, 10f);

                bcs.RegisterCorpse(fakeCorpse);
                passed += AssertCondition("Corpse registered in activeCorpses list", bcs.ActiveCorpses.Count >= 1, ref failed);

                if (bcs.ActiveCorpses.Count > 0)
                {
                    var corpseRec = bcs.ActiveCorpses[0];
                    passed += AssertCondition("Corpse has 45s decay timer", Mathf.Approximately(corpseRec.decayTimer, 45.0f), ref failed);
                    passed += AssertCondition("Corpse is initially unconcealed", !corpseRec.isConcealed, ref failed);

                    // Test Start Dragging
                    bcs.StartDraggingCorpse(corpseRec);
                    passed += AssertCondition("IsDraggingCorpse == true after start drag", bcs.IsDraggingCorpse, ref failed);
                    passed += AssertCondition("corpseRec.isBeingDragged == true", corpseRec.isBeingDragged, ref failed);

                    // Test Drop Corpse
                    bcs.DropCorpse();
                    passed += AssertCondition("IsDraggingCorpse == false after drop", !bcs.IsDraggingCorpse, ref failed);
                }

                // =====================================================================
                // 4. KINEMATIC PARABOLIC THROW CONTROLLER & PROJECTILES
                // =====================================================================
                GameObject throwGo = new GameObject("ParabolicThrowController_Audit");
                throwGo.transform.SetParent(tempContainer.transform);
                ParabolicThrowController ptc = throwGo.AddComponent<ParabolicThrowController>();

                passed += AssertCondition("ParabolicThrowController instantiates successfully", ptc != null, ref failed);
                passed += AssertCondition("ThrowableItemType supports 3 types (Bottle, Brick, SoundBomb)", Enum.GetValues(typeof(ThrowableItemType)).Length == 3, ref failed);

                LineRenderer line = throwGo.GetComponent<LineRenderer>();
                passed += AssertCondition("Parabolic trajectory LineRenderer exists", line != null, ref failed);
                if (line != null)
                {
                    passed += AssertCondition("Trajectory arc has 28 simulation segments", line.positionCount == 28, ref failed);
                    if (line.sharedMaterial != null)
                    {
                        passed += AssertCondition("Trajectory arc material has enableInstancing=true", line.sharedMaterial.enableInstancing, ref failed);
                    }
                }

                // Test Item IDs
                passed += AssertCondition("Brick item ID == item_brick", ParabolicThrowController.GetItemId(ThrowableItemType.Brick) == "item_brick", ref failed);
                passed += AssertCondition("Bottle item ID == item_distraction_bottle", ParabolicThrowController.GetItemId(ThrowableItemType.DistractionBottle) == "item_distraction_bottle", ref failed);
                passed += AssertCondition("SoundBomb item ID == trap_sound_bomb", ParabolicThrowController.GetItemId(ThrowableItemType.SoundBomb) == "trap_sound_bomb", ref failed);

                // =====================================================================
                // 5. SCAVENGE PLACER PROPS ACROSS THE CITY
                // =====================================================================
                ThrowableScavengePlacer.EnsureThrowablePropsPlaced();
                GameObject scavengeRoot = GameObject.Find("--- THROWABLE_SCAVENGE_PROPS ---");
                passed += AssertCondition("Throwable scavenge props container exists in scene", scavengeRoot != null, ref failed);
                if (scavengeRoot != null)
                {
                    int propCount = scavengeRoot.transform.childCount;
                    passed += AssertCondition($"18 pickable bricks & bottles placed across city (Count: {propCount})", propCount >= 18, ref failed);
                }

                // =====================================================================
                // 6. THROWABLE PROJECTILE COLLISION & STUN BEHAVIOR
                // =====================================================================
                GameObject projGo = new GameObject("Projectile_Audit");
                projGo.transform.SetParent(tempContainer.transform);
                ThrowableDistractionProjectile proj = projGo.AddComponent<ThrowableDistractionProjectile>();
                proj.Initialize(ThrowableItemType.Brick);
                passed += AssertCondition("ThrowableDistractionProjectile component initializes cleanly", proj != null, ref failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempContainer);
            }

            Debug.Log("==========================================================================");
            Debug.Log($"[Phase4_StealthAudit] AUDIT COMPLETE: {passed} PASSED, {failed} FAILED.");
            Debug.Log("==========================================================================");
        }

        private static int AssertCondition(string message, bool condition, ref int failCount)
        {
            if (condition)
            {
                Debug.Log($"  [PASS] {message}");
                return 1;
            }
            else
            {
                Debug.LogError($"  [FAIL] {message}");
                failCount++;
                return 0;
            }
        }
    }
}
