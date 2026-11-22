using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSurvival.Animation;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class Step1A_AnimationAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Project Survival/Audit/Run Step 1A Procedural Animation Audit", false, 11)]
        public static void RunAuditFromMenu()
        {
            ExecuteAudit();
        }

        public static void RunAuditCLI()
        {
            bool passed = ExecuteAudit();
            EditorApplication.Exit(passed ? 0 : 1);
        }

        public static bool ExecuteAudit()
        {
            Debug.Log("================================================================================");
            Debug.Log(">>> RUNNING STEP 1A: PROCEDURAL HUMANOID ANIMATION RIG AUDIT <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int passedChecks = 0;
            int failedChecks = 0;
            StringBuilder log = new StringBuilder();

            // 1. Audit Player_Root
            var players = UnityEngine.Object.FindObjectsByType<TPSPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length > 0)
            {
                var player = players[0];
                var anim = player.GetComponent<HumanoidProceduralAnimator>();
                if (anim != null)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] Player `{player.gameObject.name}` has HumanoidProceduralAnimator attached.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] Player `{player.gameObject.name}` is MISSING HumanoidProceduralAnimator!");
                }

                // Check applyRootMotion
                var animators = player.GetComponentsInChildren<Animator>(true);
                bool rootMotionSafe = animators.All(a => !a.applyRootMotion);
                if (rootMotionSafe)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] Player `{player.gameObject.name}` has applyRootMotion = false on all Animator components.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] Player `{player.gameObject.name}` has applyRootMotion = true on Animator!");
                }

                // Check Ground Alignment
                var rends = player.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    float offset = b.min.y - player.transform.position.y;
                    if (Mathf.Abs(offset) <= 0.05f)
                    {
                        passedChecks++;
                        log.AppendLine($"[PASS] Player ground clearance verified: {offset:F3}m (target <= ±0.05m).");
                    }
                    else
                    {
                        failedChecks++;
                        log.AppendLine($"[FAIL] Player ground clearance anomaly: {offset:F3}m!");
                    }
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] Player with TPSPlayerController not found in scene!");
            }

            // 2. Audit All 19 NPCs
            var npcs = UnityEngine.Object.FindObjectsByType<SurvivorNPC_AI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            log.AppendLine($"\nAuditing {npcs.Length} Survivor NPCs in scene:");

            int npcRigCount = 0;
            int npcRootMotionSafeCount = 0;
            int npcGroundSafeCount = 0;

            foreach (var npc in npcs)
            {
                if (npc == null) continue;

                var anim = npc.GetComponent<HumanoidProceduralAnimator>();
                if (anim != null)
                {
                    npcRigCount++;
                }
                else
                {
                    log.AppendLine($"  - [FAIL] NPC `{npc.gameObject.name}` is MISSING HumanoidProceduralAnimator!");
                }

                var animators = npc.GetComponentsInChildren<Animator>(true);
                bool rootMotionSafe = animators.All(a => !a.applyRootMotion);
                if (rootMotionSafe) npcRootMotionSafeCount++;
                else log.AppendLine($"  - [FAIL] NPC `{npc.gameObject.name}` has applyRootMotion = true!");

                var rends = npc.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    float offset = b.min.y - npc.transform.position.y;
                    if (Mathf.Abs(offset) <= 0.05f) npcGroundSafeCount++;
                    else log.AppendLine($"  - [FAIL] NPC `{npc.gameObject.name}` ground offset: {offset:F3}m (target <= ±0.05m)!");
                }
            }

            if (npcRigCount == npcs.Length)
            {
                passedChecks++;
                log.AppendLine($"[PASS] All {npcs.Length} NPCs have HumanoidProceduralAnimator attached.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] Only {npcRigCount}/{npcs.Length} NPCs have HumanoidProceduralAnimator!");
            }

            if (npcRootMotionSafeCount == npcs.Length)
            {
                passedChecks++;
                log.AppendLine($"[PASS] All {npcs.Length} NPCs have applyRootMotion = false.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] {npcs.Length - npcRootMotionSafeCount} NPCs have applyRootMotion conflict!");
            }

            if (npcGroundSafeCount == npcs.Length)
            {
                passedChecks++;
                log.AppendLine($"[PASS] All {npcs.Length} NPCs are cleanly grounded on pavement.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] {npcs.Length - npcGroundSafeCount} NPCs have ground clearance anomalies!");
            }

            // 3. Scan for Codebase Quality (TODO stubs, NotImplementedException, Shaders)
            string scriptsDir = Path.Combine(Application.dataPath, "_Project/Scripts");
            string[] csFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
            int todoCount = 0;
            int notImplementedCount = 0;

            foreach (var file in csFiles)
            {
                if (file.Contains("/Editor/")) continue;
                string[] lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Contains("// TODO") || trimmed.Contains("//TODO") || trimmed.Contains("// ToDo"))
                    {
                        todoCount++;
                    }
                    if (trimmed.Contains("throw new NotImplementedException"))
                    {
                        notImplementedCount++;
                    }
                }
            }

            if (todoCount == 0)
            {
                passedChecks++;
                log.AppendLine("[PASS] Zero `// TODO` stubs in non-editor scripts.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] Found {todoCount} `// TODO` stubs in codebase!");
            }

            if (notImplementedCount == 0)
            {
                passedChecks++;
                log.AppendLine("[PASS] Zero `NotImplementedException` instances in codebase.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] Found {notImplementedCount} `NotImplementedException` instances!");
            }

            // 4. Shaders check
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int magentaCount = 0;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null || m.shader.name.Contains("Error") || m.shader.name.Contains("InternalError"))
                    {
                        magentaCount++;
                    }
                }
            }

            if (magentaCount == 0)
            {
                passedChecks++;
                log.AppendLine("[PASS] Zero magenta / pink shader errors in scene.");
            }
            else
            {
                failedChecks++;
                log.AppendLine($"[FAIL] Found {magentaCount} magenta shader errors!");
            }

            Debug.Log(log.ToString());
            Debug.Log($"[Step1A_AnimationAudit] Summary: Passed: {passedChecks}, Failed: {failedChecks}");
            Debug.Log("================================================================================");

            return failedChecks == 0;
        }
    }
}
