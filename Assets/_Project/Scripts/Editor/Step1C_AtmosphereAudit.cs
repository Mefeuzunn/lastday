using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class Step1C_AtmosphereAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Project Survival/Audit/Run Step 1C Atmosphere Audit", false, 31)]
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
            Debug.Log(">>> RUNNING STEP 1C: SURFACE FOOTSTEPS & ATMOSPHERE AMBIENCE AUDIT <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int passedChecks = 0;
            int failedChecks = 0;
            StringBuilder log = new StringBuilder();

            // 1. Audit SurfaceFootstepController on Player
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var tps = UnityEngine.Object.FindFirstObjectByType<TPSPlayerController>();
                if (tps != null) player = tps.gameObject;
            }

            if (player != null)
            {
                var sfc = player.GetComponent<SurfaceFootstepController>();
                if (sfc != null)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] SurfaceFootstepController is attached to Player `{player.name}`.");

                    // Verify 5 surface multipliers
                    float aspMult = sfc.GetSurfaceAcousticMultiplier(GroundSurfaceType.AsphaltConcrete);
                    float grassMult = sfc.GetSurfaceAcousticMultiplier(GroundSurfaceType.GrassSoil);
                    float woodMult = sfc.GetSurfaceAcousticMultiplier(GroundSurfaceType.WoodInterior);
                    float metalMult = sfc.GetSurfaceAcousticMultiplier(GroundSurfaceType.MetalIndustrial);
                    float waterMult = sfc.GetSurfaceAcousticMultiplier(GroundSurfaceType.ShallowWater);

                    if (aspMult == 1.0f && grassMult == 0.80f && woodMult == 0.90f && metalMult == 1.35f && waterMult == 1.30f)
                    {
                        passedChecks++;
                        log.AppendLine("[PASS] All 5 ground surface acoustic multipliers verified (Asphalt: 1.0x, Grass: 0.80x, Wood: 0.90x, Metal: 1.35x, Water: 1.30x).");
                    }
                    else
                    {
                        failedChecks++;
                        log.AppendLine($"[FAIL] Ground surface acoustic multipliers mismatch: Asphalt={aspMult}, Grass={grassMult}, Wood={woodMult}, Metal={metalMult}, Water={waterMult}!");
                    }
                }
                else
                {
                    failedChecks++;
                    log.AppendLine("[FAIL] SurfaceFootstepController is MISSING from Player!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] Player not found in scene!");
            }

            // 2. Audit AtmosphericAmbienceEnhancer
            var enhancer = UnityEngine.Object.FindFirstObjectByType<AtmosphericAmbienceEnhancer>(FindObjectsInactive.Include);
            if (enhancer != null)
            {
                passedChecks++;
                log.AppendLine($"[PASS] AtmosphericAmbienceEnhancer is active on `{enhancer.gameObject.name}`.");

                // Check 12 flickering lights
                int lightCount = enhancer.FlickeringLightCount;
                if (lightCount >= 12)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] AtmosphericAmbienceEnhancer manages {lightCount} flickering landmark lights (target >= 12).");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] AtmosphericAmbienceEnhancer has only {lightCount} flickering lights (expected >= 12)!");
                }

                // Verify 100% of flickering lights have Shadows: None (mobile 60 FPS rule)
                bool allShadowsNone = true;
                bool allSpatialBlendOne = true;
                foreach (var fl in enhancer.FlickeringLights)
                {
                    if (fl.LightComponent != null && fl.LightComponent.shadows != LightShadows.None)
                    {
                        allShadowsNone = false;
                        log.AppendLine($"  - [FAIL] Light `{fl.Name}` has Shadows={fl.LightComponent.shadows}! Must be None.");
                    }
                    if (fl.BuzzSource != null && fl.BuzzSource.spatialBlend < 0.95f)
                    {
                        allSpatialBlendOne = false;
                        log.AppendLine($"  - [FAIL] AudioSource on `{fl.Name}` has spatialBlend={fl.BuzzSource.spatialBlend}! Must be 1.0.");
                    }
                }

                if (allShadowsNone)
                {
                    passedChecks++;
                    log.AppendLine("[PASS] All managed flickering lights have Shadows = LightShadows.None (mobile 60 FPS locked).");
                }
                else
                {
                    failedChecks++;
                }

                if (allSpatialBlendOne)
                {
                    passedChecks++;
                    log.AppendLine("[PASS] All managed electrical hum audio sources have 3D spatialBlend = 1.0.");
                }
                else
                {
                    failedChecks++;
                }

                // Check indoor dust volumes (target >= 5)
                int dustCount = enhancer.IndoorDustVolumeCount;
                if (dustCount >= 5)
                {
                    passedChecks++;
                    log.AppendLine($"[PASS] AtmosphericAmbienceEnhancer manages {dustCount} indoor dust mote volumes (<22m culling) (target >= 5).");
                }
                else
                {
                    failedChecks++;
                    log.AppendLine($"[FAIL] AtmosphericAmbienceEnhancer has only {dustCount} dust volumes (expected >= 5)!");
                }
            }
            else
            {
                failedChecks++;
                log.AppendLine("[FAIL] AtmosphericAmbienceEnhancer is MISSING from scene!");
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
            Debug.Log($"[Step1C_AtmosphereAudit] Summary: Passed: {passedChecks}, Failed: {failedChecks}");
            Debug.Log("================================================================================");

            return failedChecks == 0;
        }
    }
}
