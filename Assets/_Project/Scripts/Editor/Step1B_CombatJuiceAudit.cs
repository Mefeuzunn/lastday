using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Combat;
using ProjectSurvival.UI;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class Step1B_CombatJuiceAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Project Survival/Audit/Run Step 1B Combat Juice Audit", false, 21)]
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
            Debug.Log(">>> RUNNING STEP 1B: COMBAT JUICE, RECOIL, DIRECTIONAL HUD & DECALS AUDIT <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int passedChecks = 0;
            int failedChecks = 0;
            StringBuilder log = new StringBuilder();

            // 1. Audit CombatJuiceController
            var juice = UnityEngine.Object.FindFirstObjectByType<CombatJuiceController>(FindObjectsInactive.Include);
            if (juice != null)
            {
                passedChecks++;
                log.AppendLine($"[PASS] CombatJuiceController is active on `{juice.gameObject.name}`.");
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] CombatJuiceController is missing from scene!");
            }

            // 2. Audit DirectionalDamageHUD
            var dirDmg = UnityEngine.Object.FindFirstObjectByType<DirectionalDamageHUD>(FindObjectsInactive.Include);
            if (dirDmg != null)
            {
                passedChecks++;
                log.AppendLine($"[PASS] DirectionalDamageHUD is active on `{dirDmg.gameObject.name}`.");

                // Verify raycastTarget = false on all indicator graphics
                var images = dirDmg.GetComponentsInChildren<Image>(true);
                bool allRaycastFalse = true;
                foreach (var img in images)
                {
                    if (img.raycastTarget) allRaycastFalse = false;
                }
                if (allRaycastFalse)
                {
                    passedChecks++;
                    log.AppendLine("[PASS] DirectionalDamageHUD indicators have raycastTarget = false.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine("[FAIL] DirectionalDamageHUD has images with raycastTarget = true!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] DirectionalDamageHUD is missing from scene!");
            }

            // 3. Audit DynamicCrosshairUI
            var dynCross = UnityEngine.Object.FindFirstObjectByType<DynamicCrosshairUI>(FindObjectsInactive.Include);
            if (dynCross != null)
            {
                passedChecks++;
                log.AppendLine($"[PASS] DynamicCrosshairUI is active on `{dynCross.gameObject.name}`.");

                var images = dynCross.GetComponentsInChildren<Image>(true);
                bool allRaycastFalse = true;
                foreach (var img in images)
                {
                    if (img.raycastTarget) allRaycastFalse = false;
                }
                if (allRaycastFalse)
                {
                    passedChecks++;
                    log.AppendLine("[PASS] DynamicCrosshairUI ticks and dot have raycastTarget = false.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine("[FAIL] DynamicCrosshairUI has images with raycastTarget = true!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] DynamicCrosshairUI is missing from scene!");
            }

            // 4. Audit WeaponSwayAndRecoil on Player
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var tps = UnityEngine.Object.FindFirstObjectByType<TPSPlayerController>();
                if (tps != null) player = tps.gameObject;
            }

            if (player != null)
            {
                var sway = player.GetComponent<WeaponSwayAndRecoil>();
                if (sway != null)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] WeaponSwayAndRecoil is active on Player `{player.name}`.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] WeaponSwayAndRecoil is missing from Player `{player.name}`!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] Player not found in scene!");
            }

            // 5. Audit ImpactDecalPoolManager
            var decalManager = UnityEngine.Object.FindFirstObjectByType<ImpactDecalPoolManager>(FindObjectsInactive.Include);
            if (decalManager != null)
            {
                passedChecks++;
                log.AppendLine($"[PASS] ImpactDecalPoolManager is active on `{decalManager.gameObject.name}` (MaxPoolSize: {decalManager.MaxPoolSize}).");

                if (decalManager.MaxPoolSize == 40)
                {
                    passedChecks++;
                    log.AppendLine("[PASS] ImpactDecalPoolManager pool size is verified at exactly 40 ring-buffer slots.");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] ImpactDecalPoolManager pool size is {decalManager.MaxPoolSize} (expected 40)!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] ImpactDecalPoolManager is missing from scene!");
            }

            // 6. Codebase Stubs Check (TODO, NotImplementedException)
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

            // 7. Shaders Check
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
            Debug.Log($"[Step1B_CombatJuiceAudit] Summary: Passed: {passedChecks}, Failed: {failedChecks}");
            Debug.Log("================================================================================");

            return failedChecks == 0;
        }
    }
}
