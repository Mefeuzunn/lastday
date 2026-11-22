using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectSurvival.EditorTools
{
    public static class CinematographyRig
    {
        public const string RendererDataPath = "Assets/_Project/Settings/UniversalRendererData.asset";
        public const string ContactShadowMatPath = "Assets/_Project/Materials/AAA_PBR/MAT_ContactShadow.mat";

        [MenuItem("ProjectSurvival/Setup Cinematography Rig (SSAO & Grounding)", false, 23)]
        public static void SetupRigMenuItem()
        {
            SetupRig(true);
        }

        public static void SetupRig(bool saveScene = true)
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING CINEMATOGRAPHY RIG (SSAO, CONTACT SHADOWS & LIGHTING) <<<");
            Debug.Log("==================================================================");

            // 1. Enable URP Screen Space Ambient Occlusion (SSAO)
            EnableURPSSAO();

            // 2. Attach Soft Contact Shadows under Characters & Vehicles
            AttachContactShadows();

            // 3. Configure Unified Atmospheric Lighting & Skybox
            ConfigureAtmosphericLighting();

            // 4. Apply Unified PBR Materials (Wet Puddle Asphalt & Concrete Spalling) to Roads & Sidewalks
            ApplyPBRMaterialsToWorld();

            if (saveScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[CinematographyRig] Scene saved successfully: {scene.path}");
            }

            Debug.Log("[CinematographyRig] Cinematography Rig Setup Completed!");
        }

        #region 1. URP SSAO Configuration

        public static void EnableURPSSAO()
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                Debug.LogWarning($"[CinematographyRig] UniversalRendererData not found at: {RendererDataPath}");
                return;
            }

            // Check if SSAO is already attached
            bool alreadyHasSSAO = false;
            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                if (rendererData.rendererFeatures[i] is ScreenSpaceAmbientOcclusion)
                {
                    alreadyHasSSAO = true;
                    ConfigureSSAOFeature(rendererData.rendererFeatures[i]);
                    break;
                }
            }

            if (!alreadyHasSSAO)
            {
                var ssaoFeature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssaoFeature.name = "ScreenSpaceAmbientOcclusion";
                ConfigureSSAOFeature(ssaoFeature);

                AssetDatabase.AddObjectToAsset(ssaoFeature, rendererData);

                SerializedObject soData = new SerializedObject(rendererData);
                SerializedProperty features = soData.FindProperty("m_RendererFeatures");
                int idx = features.arraySize;
                features.InsertArrayElementAtIndex(idx);
                features.GetArrayElementAtIndex(idx).objectReferenceValue = ssaoFeature;
                soData.ApplyModifiedProperties();

                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
                Debug.Log("[CinematographyRig] Created & Activated ScreenSpaceAmbientOcclusion on UniversalRendererData.");
            }
            else
            {
                Debug.Log("[CinematographyRig] Configured existing ScreenSpaceAmbientOcclusion on UniversalRendererData.");
            }
        }

        private static void ConfigureSSAOFeature(ScriptableRendererFeature feature)
        {
            if (feature == null) return;

            SerializedObject so = new SerializedObject(feature);
            SerializedProperty settings = so.FindProperty("m_Settings");
            if (settings != null)
            {
                var intensity = settings.FindPropertyRelative("Intensity") ?? settings.FindPropertyRelative("intensity");
                if (intensity != null) intensity.floatValue = 1.35f;

                var radius = settings.FindPropertyRelative("Radius") ?? settings.FindPropertyRelative("radius");
                if (radius != null) radius.floatValue = 0.45f;

                var directStrength = settings.FindPropertyRelative("DirectLightingStrength") ?? settings.FindPropertyRelative("directLightingStrength");
                if (directStrength != null) directStrength.floatValue = 0.25f;

                var downsample = settings.FindPropertyRelative("Downsample") ?? settings.FindPropertyRelative("downsample");
                if (downsample != null) downsample.boolValue = true; // Mobile 60 FPS performance
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(feature);
        }

        #endregion

        #region 2. Soft Contact Shadows

        public static void AttachContactShadows()
        {
            Material shadowMat = AssetDatabase.LoadAssetAtPath<Material>(ContactShadowMatPath);
            if (shadowMat == null)
            {
                Debug.LogWarning($"[CinematographyRig] Contact shadow material not found at: {ContactShadowMatPath}");
                return;
            }

            int count = 0;

            // A. Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                EnsureContactShadowQuad(player.transform, "ContactShadow_Player", new Vector2(1.25f, 1.25f), 0.015f, shadowMat);
                count++;
            }

            // B. NPCs
            var npcs = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++)
            {
                string n = npcs[i].name;
                if (n.StartsWith("NPC_") && n.EndsWith("_Root"))
                {
                    EnsureContactShadowQuad(npcs[i], "ContactShadow_NPC", new Vector2(1.15f, 1.15f), 0.015f, shadowMat);
                    count++;
                }
            }

            // C. Zombies
            for (int i = 0; i < npcs.Length; i++)
            {
                string n = npcs[i].name;
                if ((n.StartsWith("Zombie_") || n.StartsWith("Pack_")) && (n.Contains("Root") || npcs[i].GetComponent<ProjectSurvival.AI.ZombieAI>() != null))
                {
                    bool isTank = n.Contains("Tank");
                    Vector2 size = isTank ? new Vector2(1.95f, 1.95f) : new Vector2(1.25f, 1.25f);
                    EnsureContactShadowQuad(npcs[i], "ContactShadow_Zombie", size, 0.015f, shadowMat);
                    count++;
                }
            }

            // D. Vehicles
            Transform fleetParent = GameObject.Find("--- VEHICLE FLEET ---")?.transform;
            if (fleetParent != null)
            {
                for (int i = 0; i < fleetParent.childCount; i++)
                {
                    Transform car = fleetParent.GetChild(i);
                    string cn = car.name.ToLower();
                    float w = cn.Contains("truck") ? 2.6f : 2.1f;
                    float l = cn.Contains("truck") ? 8.6f : (cn.Contains("van") || cn.Contains("ambulance") ? 5.4f : 4.8f);

                    EnsureContactShadowQuad(car, "ContactShadow_Vehicle", new Vector2(w, l), 0.02f, shadowMat);
                    count++;
                }
            }

            Debug.Log($"[CinematographyRig] Attached {count} Soft Contact Shadows under Characters & Vehicles.");
        }

        private static void EnsureContactShadowQuad(Transform parent, string name, Vector2 size, float yOffset, Material mat)
        {
            Transform existing = parent.Find(name);
            GameObject quadObj;

            if (existing == null)
            {
                quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadObj.name = name;
                quadObj.transform.SetParent(parent, false);

                // Remove mesh collider from contact shadow quad
                Collider col = quadObj.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            else
            {
                quadObj = existing.gameObject;
            }

            quadObj.transform.localPosition = new Vector3(0f, yOffset, 0f);
            quadObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quadObj.transform.localScale = new Vector3(size.x, size.y, 1f);

            Renderer r = quadObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        #endregion

        #region 3. Atmospheric Lighting & Skybox

        public static void ConfigureAtmosphericLighting()
        {
            // 1. Directional Light (Moonlight)
            Light dirLight = null;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    dirLight = lights[i];
                    break;
                }
            }

            if (dirLight != null)
            {
                dirLight.color = new Color(0.72f, 0.78f, 0.85f); // Cool silvery moonlight #B8C8D8
                dirLight.intensity = 0.85f;
                dirLight.shadows = LightShadows.Soft;
                dirLight.shadowBias = 0.05f;
                dirLight.shadowNormalBias = 0.4f;
                dirLight.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
                Debug.Log("[CinematographyRig] Configured Balanced Directional Moonlight with Soft Shadows.");
            }

            // 2. Ambient Trilight Gradient
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.17f, 0.21f, 0.27f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.14f, 0.17f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.09f, 0.10f);

            // 3. Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.14f, 0.17f, 0.21f);
            RenderSettings.fogDensity = 0.0055f;

            Debug.Log("[CinematographyRig] Unified Atmospheric Lighting, Trilight Gradient & Distance Fog Configured.");
        }

        public static void ApplyPBRMaterialsToWorld()
        {
            Material asphaltMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Asphalt_WetPuddles.mat");
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Concrete_Spalling.mat");
            Material brickMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Brick_Weathered.mat");

            int roadCount = 0;
            int walkCount = 0;

            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r == null || r.gameObject == null) continue;
                string n = r.gameObject.name.ToLower();

                if (n.StartsWith("road_") && asphaltMat != null)
                {
                    r.sharedMaterial = asphaltMat;
                    roadCount++;
                }
                else if (n.StartsWith("sidewalk_") && concreteMat != null)
                {
                    r.sharedMaterial = concreteMat;
                    walkCount++;
                }
            }

            Debug.Log($"[CinematographyRig] Applied MAT_Asphalt_WetPuddles to {roadCount} road sections and MAT_Concrete_Spalling to {walkCount} sidewalks.");
        }

        #endregion
    }
}
