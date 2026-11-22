using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class Step1C_AtmosphereSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player_Root.prefab";

        [MenuItem("Project Survival/Atmosphere/Setup Step 1C Surface Footsteps & Atmosphere", false, 30)]
        public static void RunSetupFromMenu()
        {
            ExecuteSetup();
        }

        public static void RunSetupCLI()
        {
            ExecuteSetup();
            EditorApplication.Exit(0);
        }

        public static void ExecuteSetup()
        {
            Debug.Log("================================================================================");
            Debug.Log(">>> EXECUTING STEP 1C: SURFACE FOOTSTEPS & ATMOSPHERE AMBIENCE SETUP <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 1. Setup SurfaceFootstepController on Player_Root
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var tps = UnityEngine.Object.FindFirstObjectByType<TPSPlayerController>();
                if (tps != null) player = tps.gameObject;
            }

            if (player != null)
            {
                var sfc = player.GetComponent<SurfaceFootstepController>();
                if (sfc == null) sfc = player.AddComponent<SurfaceFootstepController>();
                EditorUtility.SetDirty(sfc);
                EditorUtility.SetDirty(player);
                Debug.Log("[Step1C_AtmosphereSetup] Configured SurfaceFootstepController on Player_Root.");
            }

            // 2. Setup AtmosphericAmbienceEnhancer in Scene
            var enhancer = UnityEngine.Object.FindFirstObjectByType<AtmosphericAmbienceEnhancer>();
            if (enhancer == null)
            {
                GameObject enhObj = new GameObject("AtmosphericAmbienceEnhancer");
                enhancer = enhObj.AddComponent<AtmosphericAmbienceEnhancer>();

                GameObject envRoot = GameObject.Find("Environment") ?? GameObject.Find("Managers");
                if (envRoot != null) enhObj.transform.SetParent(envRoot.transform, false);
                Debug.Log("[Step1C_AtmosphereSetup] Created AtmosphericAmbienceEnhancer in scene.");
            }

            // 3. Select & Configure 12 Flickering Lights
            Configure12FlickeringLights(enhancer);

            // 4. Configure 5 Indoor Dust Volumes
            ConfigureIndoorDustVolumes(enhancer);

            // 5. Update Player_Root Prefab
            try
            {
                var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                if (contents != null)
                {
                    var sfc = contents.GetComponent<SurfaceFootstepController>();
                    if (sfc == null) sfc = contents.AddComponent<SurfaceFootstepController>();

                    PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                    PrefabUtility.UnloadPrefabContents(contents);
                    Debug.Log($"[Step1C_AtmosphereSetup] Saved updated Prefab `{PlayerPrefabPath}`.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Step1C_AtmosphereSetup] Error updating player prefab: {ex.Message}");
            }

            // 6. Save Scene and Assets
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Step1C_AtmosphereSetup] Scene saved: {saved}. Step 1C Atmosphere enhancements deployed.");
            Debug.Log("================================================================================");
        }

        private static void Configure12FlickeringLights(AtmosphericAmbienceEnhancer enhancer)
        {
            var flList = enhancer.FlickeringLights;
            flList.Clear();

            // Candidate light game object search names near landmarks
            string[] landmarkQueries = new string[]
            {
                "SafeHouse_Warm_Light",
                "Clinic_Pharmacy_Light",
                "Warehouse_Interior_Light",
                "Cabin_Lantern_Light",
                "FireLight_Warm",
                "SpotLight_WarmAmber",
                "NeonSign_CLINIC_MEDICAL",
                "NeonSign_SUPERMARKET",
                "NeonSign_PHARMACY",
                "StreetLamp_Pole",
                "StreetLamp"
            };

            HashSet<Light> addedLights = new HashSet<Light>();

            // 1. Gather landmark lights first
            foreach (var query in landmarkQueries)
            {
                var objs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var obj in objs)
                {
                    if (obj.name.Contains(query) || (obj.transform.parent != null && obj.transform.parent.name.Contains(query)))
                    {
                        var light = obj.GetComponentInChildren<Light>(true);
                        if (light != null && !addedLights.Contains(light) && light.type != LightType.Directional)
                        {
                            addedLights.Add(light);
                            AddFlickeringLight(flList, obj.name, light);
                            if (addedLights.Count >= 12) break;
                        }
                    }
                }
                if (addedLights.Count >= 12) break;
            }

            // 2. If fewer than 12, pick other point/spot lights in scene
            if (addedLights.Count < 12)
            {
                var allLights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var l in allLights)
                {
                    if (l.type != LightType.Directional && !addedLights.Contains(l))
                    {
                        addedLights.Add(l);
                        AddFlickeringLight(flList, l.gameObject.name, l);
                        if (addedLights.Count >= 12) break;
                    }
                }
            }

            enhancer.ConfigureFlickeringLights();
            EditorUtility.SetDirty(enhancer);
            Debug.Log($"[Step1C_AtmosphereSetup] Configured {flList.Count} flickering lights (all Shadows: None).");
        }

        private static void AddFlickeringLight(List<AtmosphericAmbienceEnhancer.FlickeringLight> list, string name, Light light)
        {
            light.shadows = LightShadows.None; // Strict mobile rule
            float baseInt = light.intensity > 0.1f ? light.intensity : 2.5f;

            var buzz = light.GetComponent<AudioSource>();
            if (buzz == null) buzz = light.gameObject.AddComponent<AudioSource>();
            buzz.spatialBlend = 1.0f;
            buzz.minDistance = 1.5f;
            buzz.maxDistance = 14.0f;
            buzz.rolloffMode = AudioRolloffMode.Linear;
            buzz.loop = true;
            buzz.volume = 0.22f;

            list.Add(new AtmosphericAmbienceEnhancer.FlickeringLight
            {
                Name = name,
                LightComponent = light,
                BuzzSource = buzz,
                BaseIntensity = baseInt,
                Seed = Random.Range(10f, 999f),
                NoiseFrequency = Random.Range(6.5f, 9.5f),
                EnableSparks = true
            });
            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(light.gameObject);
        }

        private static void ConfigureIndoorDustVolumes(AtmosphericAmbienceEnhancer enhancer)
        {
            var dustList = enhancer.IndoorDustVolumes;
            dustList.Clear();

            string[] buildingNames = new string[]
            {
                "StarterHouse_Safehome",
                "SafeHouse_Interiors",
                "Police_Interiors",
                "Warehouse_North",
                "Clinic_Pharmacy_Light"
            };

            foreach (var bName in buildingNames)
            {
                var target = GameObject.Find(bName);
                if (target != null)
                {
                    dustList.Add(new AtmosphericAmbienceEnhancer.IndoorDustVolume
                    {
                        LocationName = bName,
                        Anchor = target.transform,
                        MaxDistance = 22.0f,
                        IsActive = true
                    });
                }
            }

            enhancer.ConfigureIndoorDustVolumes();
            EditorUtility.SetDirty(enhancer);
            Debug.Log($"[Step1C_AtmosphereSetup] Configured {dustList.Count} indoor sunbeam dust volumes (<22m culling).");
        }
    }
}
