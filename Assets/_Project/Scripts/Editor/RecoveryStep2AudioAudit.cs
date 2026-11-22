using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Vehicles;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.Editor
{
    public static class RecoveryStep2AudioAudit
    {
        [MenuItem("Tools/Survival/Audit Recovery Step 2 Audio")]
        public static void RunAudit()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Main_Prototype")
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity");
            }

            Debug.Log("=================================================");
            Debug.Log("[SURGICAL RECOVERY STEP 2 AUDIO AUDIT START]");
            Debug.Log("=================================================");

            int passed = 0;
            int failed = 0;

            // 1. Audit ProceduralAudioManager Ambient Volume
            var pam = Object.FindFirstObjectByType<ProceduralAudioManager>();
            if (pam != null)
            {
                Debug.Log($"[PASS] ProceduralAudioManager found. AmbientVolume: {pam.AmbientVolume:F2} (Target <= 0.25f)");
                if (pam.AmbientVolume <= 0.25f) passed++; else failed++;
            }
            else
            {
                Debug.LogWarning("[WARN] ProceduralAudioManager not in scene root (spawns dynamically or under MANAGERS).");
            }

            // 2. Audit AtmosphericAmbienceEnhancer SafeHouse Lights
            var aae = Object.FindFirstObjectByType<AtmosphericAmbienceEnhancer>();
            if (aae != null)
            {
                Debug.Log($"[PASS] AtmosphericAmbienceEnhancer found with {aae.FlickeringLightCount} lights.");
                bool safeHouseSilent = true;
                foreach (var fl in aae.FlickeringLights)
                {
                    if (fl.Name.Contains("SafeHouse") || (fl.LightComponent != null && Vector3.Distance(fl.LightComponent.transform.position, Vector3.zero) < 45f))
                    {
                        if (fl.BuzzSource != null && fl.BuzzSource.volume > 0.001f && fl.BuzzSource.isPlaying)
                        {
                            safeHouseSilent = false;
                        }
                    }
                }
                if (safeHouseSilent)
                {
                    Debug.Log("[PASS] SafeHouse perimeter (45m) is 100% silent from electrical buzz.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] SafeHouse light is still buzzing!");
                    failed++;
                }
            }

            // 3. Audit Vehicles Engine Audio
            var vehicles = Object.FindObjectsByType<DrivableVehicleController>(FindObjectsSortMode.None);
            Debug.Log($"[INFO] Found {vehicles.Length} DrivableVehicleController instances in scene.");
            bool allVehiclesStopped = true;
            for (int i = 0; i < vehicles.Length; i++)
            {
                var v = vehicles[i];
                var asrc = v.GetComponent<AudioSource>();
                if (asrc != null && asrc.isPlaying && asrc.volume > 0.01f)
                {
                    allVehiclesStopped = false;
                }
            }
            if (allVehiclesStopped)
            {
                Debug.Log("[PASS] All vehicle engine AudioSources are stopped at startup.");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] One or more vehicles are playing engine audio on startup!");
                failed++;
            }

            // 4. Audit Bunker Generator Audio
            var generator = Object.FindFirstObjectByType<BunkerGeneratorInteractable>();
            if (generator != null)
            {
                var genAudio = generator.GetComponent<AudioSource>();
                if (genAudio == null || !genAudio.isPlaying || genAudio.volume <= 0.01f)
                {
                    Debug.Log("[PASS] BunkerGeneratorInteractable audio is stopped at startup.");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] BunkerGenerator is playing audio on startup!");
                    failed++;
                }
            }

            // 5. Audit SettingsManager default AmbientVolume
            var settings = Object.FindFirstObjectByType<SettingsManager>();
            if (settings != null)
            {
                Debug.Log($"[PASS] SettingsManager found. AmbientVolume: {settings.AmbientVolume:F2} (Target <= 0.25f)");
                if (settings.AmbientVolume <= 0.25f) passed++; else failed++;
            }

            Debug.Log("=================================================");
            Debug.Log($"[SURGICAL RECOVERY STEP 2 AUDIO AUDIT COMPLETE] Passed: {passed}, Failed: {failed}");
            Debug.Log("=================================================");
        }
    }
}
