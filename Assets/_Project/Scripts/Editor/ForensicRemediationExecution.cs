using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Building;
using ProjectSurvival.Environment;
using ProjectSurvival.Vehicles;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class ForensicRemediationExecution
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Project Survival/Remediation/Execute Full Forensic Remediation", false, 2)]
        public static void RunRemediationFromMenu()
        {
            ExecuteRemediation();
        }

        public static void RunRemediationCLI()
        {
            ExecuteRemediation();
            EditorApplication.Exit(0);
        }

        public static void ExecuteRemediation()
        {
            Debug.Log("================================================================================");
            Debug.Log(">>> STARTING SURGICAL FORENSIC REMEDIATION EXECUTION <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // STAGE 1: CRITICAL AI & NPC GROUNDING FIXES
            ExecuteStage1_CriticalAIAndGrounding();

            // STAGE 2: 3D POSITIONAL VEHICLE AUDIO FIX (spatialBlend = 1.0)
            ExecuteStage2_VehicleAudio3D();

            // STAGE 3: PERSISTENT SCENE MANAGER REGISTRATION (--- MANAGERS ---)
            ExecuteStage3_SceneManagers();

            // STAGE 4: MOBILE 60 FPS GPU INSTANCING & UI TOUCH ERGONOMICS
            ExecuteStage4_GPUInstancingAndUIErgonomics();

            // Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            bool sceneSaved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ForensicRemediationExecution] Scene saved: {sceneSaved}. Remediation complete!");
            Debug.Log("================================================================================");
        }

        // =====================================================================
        // STAGE 1: CRITICAL AI & NPC GROUNDING FIXES
        // =====================================================================
        private static void ExecuteStage1_CriticalAIAndGrounding()
        {
            Debug.Log("--- Executing Stage 1: Critical AI & NPC Grounding Fixes ---");

            // 1.1 Relocate Scavenger_Point_Murat
            GameObject murat = GameObject.Find("Scavenger_Point_Murat");
            if (murat != null)
            {
                Vector3 targetPos = new Vector3(23.67f, 0.04f, 40.83f);
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    murat.transform.position = hit.position;
                    Debug.Log($"[Stage 1] Relocated Scavenger_Point_Murat to NavMesh position: {hit.position} (Hit distance: {hit.distance:F3}m)");
                }
                else
                {
                    murat.transform.position = targetPos;
                    Debug.Log($"[Stage 1] Relocated Scavenger_Point_Murat to {targetPos}");
                }
                EditorUtility.SetDirty(murat);
            }
            else
            {
                Debug.LogWarning("[Stage 1] Scavenger_Point_Murat GameObject not found!");
            }

            // 1.2 Fix Floating Visual Bounds on NPCs
            var npcs = UnityEngine.Object.FindObjectsByType<SurvivorNPC_AI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int fixedNpcCount = 0;

            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                var rends = npc.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) continue;

                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

                float offset = b.min.y - npc.transform.position.y;
                if (offset > 0.08f)
                {
                    // Find child Visual_Model or primary child transform
                    Transform visualModel = npc.transform.Find("Visual_Model");
                    if (visualModel != null)
                    {
                        Vector3 localPos = visualModel.localPosition;
                        localPos.y -= offset;
                        visualModel.localPosition = localPos;
                        EditorUtility.SetDirty(visualModel);
                    }
                    else
                    {
                        bool adjustedChild = false;
                        for (int c = 0; c < npc.transform.childCount; c++)
                        {
                            var child = npc.transform.GetChild(c);
                            if (child.GetComponentInChildren<Renderer>() != null)
                            {
                                Vector3 lp = child.localPosition;
                                lp.y -= offset;
                                child.localPosition = lp;
                                EditorUtility.SetDirty(child);
                                adjustedChild = true;
                            }
                        }
                        if (!adjustedChild)
                        {
                            npc.transform.position -= new Vector3(0f, offset, 0f);
                        }
                    }
                    EditorUtility.SetDirty(npc);
                    fixedNpcCount++;
                    Debug.Log($"[Stage 1] Adjusted NPC `{npc.gameObject.name}` downward by {offset:F3}m to ground level.");
                }
            }

            Debug.Log($"[Stage 1] Grounded {fixedNpcCount} floating NPCs.");
        }

        // =====================================================================
        // STAGE 2: 3D POSITIONAL VEHICLE AUDIO FIX (spatialBlend = 1.0)
        // =====================================================================
        private static void ExecuteStage2_VehicleAudio3D()
        {
            Debug.Log("--- Executing Stage 2: 3D Positional Vehicle Audio Fix ---");

            string[] heroVehicleNames = new string[]
            {
                "Vehicle_NorthHighway_ArmoredSUV",
                "Vehicle_SafeHouse_Pickup",
                "Vehicle_Police_Interceptor",
                "Vehicle_Forest_RangerJeep",
                "Vehicle_RailYard_CargoVan"
            };

            foreach (var vName in heroVehicleNames)
            {
                GameObject vObj = GameObject.Find(vName);
                if (vObj == null)
                {
                    Debug.LogWarning($"[Stage 2] Vehicle `{vName}` not found in scene!");
                    continue;
                }

                var audioSources = vObj.GetComponentsInChildren<AudioSource>(true);
                if (audioSources.Length == 0)
                {
                    // Add AudioSource
                    var newSrc = vObj.AddComponent<AudioSource>();
                    audioSources = new AudioSource[] { newSrc };
                }

                foreach (var src in audioSources)
                {
                    src.spatialBlend = 1.0f;
                    src.minDistance = 2.0f;
                    src.maxDistance = 35.0f;
                    src.rolloffMode = AudioRolloffMode.Logarithmic;
                    src.playOnAwake = false;
                    EditorUtility.SetDirty(src);
                    Debug.Log($"[Stage 2] Configured AudioSource on `{vName}`: spatialBlend=1.0, min=2m, max=35m, rolloff=Logarithmic.");
                }
                EditorUtility.SetDirty(vObj);
            }
        }

        // =====================================================================
        // STAGE 3: PERSISTENT SCENE MANAGER REGISTRATION (--- MANAGERS ---)
        // =====================================================================
        private static void ExecuteStage3_SceneManagers()
        {
            Debug.Log("--- Executing Stage 3: Persistent Scene Manager Registration ---");

            GameObject managersRoot = GameObject.Find("--- MANAGERS ---");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("--- MANAGERS ---");
                Debug.Log("[Stage 3] Created `--- MANAGERS ---` root hierarchy.");
            }

            // 1. AdaptiveAudioDirector
            var audioDir = UnityEngine.Object.FindAnyObjectByType<AdaptiveAudioDirector>(FindObjectsInactive.Include);
            if (audioDir == null)
            {
                GameObject go = new GameObject("AdaptiveAudioDirector");
                go.transform.SetParent(managersRoot.transform, false);
                audioDir = go.AddComponent<AdaptiveAudioDirector>();
                EditorUtility.SetDirty(go);
                Debug.Log("[Stage 3] Instantiated `AdaptiveAudioDirector` under `--- MANAGERS ---`.");
            }
            else if (audioDir.transform.parent == null)
            {
                audioDir.transform.SetParent(managersRoot.transform, false);
                EditorUtility.SetDirty(audioDir.gameObject);
                Debug.Log("[Stage 3] Reparented existing `AdaptiveAudioDirector` under `--- MANAGERS ---`.");
            }

            // 2. BaseBuildingManager
            var buildingMgr = UnityEngine.Object.FindAnyObjectByType<BaseBuildingManager>(FindObjectsInactive.Include);
            if (buildingMgr == null)
            {
                GameObject go = new GameObject("BaseBuildingManager");
                go.transform.SetParent(managersRoot.transform, false);
                buildingMgr = go.AddComponent<BaseBuildingManager>();
                EditorUtility.SetDirty(go);
                Debug.Log("[Stage 3] Instantiated `BaseBuildingManager` under `--- MANAGERS ---`.");
            }
            else if (buildingMgr.transform.parent == null)
            {
                buildingMgr.transform.SetParent(managersRoot.transform, false);
                EditorUtility.SetDirty(buildingMgr.gameObject);
                Debug.Log("[Stage 3] Reparented existing `BaseBuildingManager` under `--- MANAGERS ---`.");
            }

            // 3. DynamicWeatherManager
            var weatherMgr = UnityEngine.Object.FindAnyObjectByType<DynamicWeatherManager>(FindObjectsInactive.Include);
            if (weatherMgr == null)
            {
                GameObject go = new GameObject("DynamicWeatherManager");
                go.transform.SetParent(managersRoot.transform, false);
                weatherMgr = go.AddComponent<DynamicWeatherManager>();
                EditorUtility.SetDirty(go);
                Debug.Log("[Stage 3] Instantiated `DynamicWeatherManager` under `--- MANAGERS ---`.");
            }
            else if (weatherMgr.transform.parent == null)
            {
                weatherMgr.transform.SetParent(managersRoot.transform, false);
                EditorUtility.SetDirty(weatherMgr.gameObject);
                Debug.Log("[Stage 3] Reparented existing `DynamicWeatherManager` under `--- MANAGERS ---`.");
            }

            // 4. RegionalBossDirector
            var bossDir = UnityEngine.Object.FindAnyObjectByType<RegionalBossDirector>(FindObjectsInactive.Include);
            if (bossDir == null)
            {
                GameObject go = new GameObject("RegionalBossDirector");
                go.transform.SetParent(managersRoot.transform, false);
                bossDir = go.AddComponent<RegionalBossDirector>();
                EditorUtility.SetDirty(go);
                Debug.Log("[Stage 3] Instantiated `RegionalBossDirector` under `--- MANAGERS ---`.");
            }
            else if (bossDir.transform.parent == null)
            {
                bossDir.transform.SetParent(managersRoot.transform, false);
                EditorUtility.SetDirty(bossDir.gameObject);
                Debug.Log("[Stage 3] Reparented existing `RegionalBossDirector` under `--- MANAGERS ---`.");
            }

            EditorUtility.SetDirty(managersRoot);
        }

        // =====================================================================
        // STAGE 4: MOBILE 60 FPS GPU INSTANCING & UI TOUCH ERGONOMICS
        // =====================================================================
        private static void ExecuteStage4_GPUInstancingAndUIErgonomics()
        {
            Debug.Log("--- Executing Stage 4: GPU Instancing & UI Touch Ergonomics ---");

            // 4.1 Enable GPU Instancing across ALL project materials
            string[] matGuids = AssetDatabase.FindAssets("t:Material");
            int instancingUpdatedCount = 0;
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    instancingUpdatedCount++;
                }
            }

            // Also check all scene materials
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && !m.enableInstancing)
                    {
                        m.enableInstancing = true;
                        EditorUtility.SetDirty(m);
                        instancingUpdatedCount++;
                    }
                }
            }
            Debug.Log($"[Stage 4] Enabled GPU Instancing on {instancingUpdatedCount} materials (100% compliant).");

            // 4.2 Enforce >= 48x48px on UI Buttons
            Canvas.ForceUpdateCanvases();
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int adjustedButtonCount = 0;

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var rt = btn.GetComponent<RectTransform>();
                if (rt == null) continue;

                bool modified = false;

                // Enforce minimum dimensions on sizeDelta where applicable
                if (rt.sizeDelta.x > 0f && rt.sizeDelta.x < 48f) { Vector2 sd = rt.sizeDelta; sd.x = 48f; rt.sizeDelta = sd; modified = true; }
                if (rt.sizeDelta.y > 0f && rt.sizeDelta.y < 48f) { Vector2 sd = rt.sizeDelta; sd.y = 48f; rt.sizeDelta = sd; modified = true; }

                // Also check LayoutElement if present
                var le = btn.GetComponent<LayoutElement>();
                if (le != null)
                {
                    if (le.minHeight > 0f && le.minHeight < 48f) { le.minHeight = 48f; modified = true; }
                    if (le.preferredHeight > 0f && le.preferredHeight < 48f) { le.preferredHeight = 48f; modified = true; }
                    if (le.minWidth > 0f && le.minWidth < 48f) { le.minWidth = 48f; modified = true; }
                }

                // If height or width < 44px, ensure targetGraphic raycastPadding expands effective touch target to >= 48x48px
                float curW = rt.rect.width > 0f ? rt.rect.width : (rt.sizeDelta.x > 0f ? rt.sizeDelta.x : 48f);
                float curH = rt.rect.height > 0f ? rt.rect.height : (rt.sizeDelta.y > 0f ? rt.sizeDelta.y : 48f);
                if (curW < 44f || curH < 44f || rt.sizeDelta.y < 48f)
                {
                    if (btn.targetGraphic != null)
                    {
                        float padX = Mathf.Max(0f, (48f - curW) * 0.5f);
                        float padY = Mathf.Max(0f, (48f - curH) * 0.5f);

                        if (padX > 0f || padY > 0f)
                        {
                            btn.targetGraphic.raycastPadding = new Vector4(-padX, -padY, -padX, -padY);
                            EditorUtility.SetDirty(btn.targetGraphic);
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(btn);
                    adjustedButtonCount++;
                    Debug.Log($"[Stage 4] Enlarged/Padded UI Button `{btn.gameObject.name}` (SizeDelta: {rt.sizeDelta}).");
                }
            }
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[Stage 4] Enlarged/Padded {adjustedButtonCount} UI buttons to meet >=48x48px touch budget.");

            // 4.3 Disable raycastTarget on all static text components
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int disabledRaycastCount = 0;
            foreach (var txt in texts)
            {
                if (txt == null) continue;
                if (txt.raycastTarget)
                {
                    txt.raycastTarget = false;
                    EditorUtility.SetDirty(txt);
                    disabledRaycastCount++;
                }
            }

            var tmpTexts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tmp in tmpTexts)
            {
                if (tmp == null) continue;
                if (tmp.raycastTarget)
                {
                    tmp.raycastTarget = false;
                    EditorUtility.SetDirty(tmp);
                    disabledRaycastCount++;
                }
            }
            Debug.Log($"[Stage 4] Disabled raycastTarget on {disabledRaycastCount} static text elements.");
        }
    }
}
