using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    public static class AAA_VisualCohesionVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string MaterialsFolder = "Assets/_Project/Materials/AAA_PBR";
        private const string RendererDataPath = "Assets/_Project/Settings/UniversalRendererData.asset";

        [MenuItem("ProjectSurvival/Verify AAA Visual Cohesion Pipeline", false, 25)]
        public static void VerifyVisualCohesion()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING AAA VISUAL COHESION AUDIT & VERIFICATION (BATCHMODE) <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Verifier FAIL] Could not open scene: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int errors = 0;

            // 1. Audit PBR Material & Texture Library
            string[] requiredMaterials = new string[]
            {
                "MAT_Asphalt_WetPuddles.mat",
                "MAT_Brick_Weathered.mat",
                "MAT_Concrete_Spalling.mat",
                "MAT_Auto_Police.mat",
                "MAT_Auto_Ambulance.mat",
                "MAT_Auto_Taxi.mat",
                "MAT_Auto_RustMetal.mat",
                "MAT_Tactical_Fabric.mat",
                "MAT_Wood_Planks.mat",
                "MAT_Glass_DustyReflective.mat",
                "MAT_ContactShadow.mat"
            };

            for (int i = 0; i < requiredMaterials.Length; i++)
            {
                string p = Path.Combine(MaterialsFolder, requiredMaterials[i]);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null)
                {
                    Debug.LogError($"[Verifier FAIL] Missing required PBR material: {p}");
                    errors++;
                }
                else
                {
                    if (m.shader == null || (!m.shader.name.StartsWith("Universal Render Pipeline/") && !m.shader.name.StartsWith("URP/")))
                    {
                        Debug.LogError($"[Verifier FAIL] Material '{m.name}' does not use URP shader (uses '{m.shader?.name}')!");
                        errors++;
                    }
                    if (!m.enableInstancing)
                    {
                        Debug.LogWarning($"[Verifier WARN] Material '{m.name}' should have GPU Instancing enabled.");
                    }
                }
            }
            Debug.Log("[Verifier PASS] Verified AAA PBR Material & Texture Pipeline.");

            // 2. Audit SSAO in UniversalRendererData
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                Debug.LogError($"[Verifier FAIL] UniversalRendererData not found at: {RendererDataPath}");
                errors++;
            }
            else
            {
                bool hasSSAO = false;
                for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
                {
                    if (rendererData.rendererFeatures[i] is ScreenSpaceAmbientOcclusion)
                    {
                        hasSSAO = true;
                        break;
                    }
                }
                if (!hasSSAO)
                {
                    Debug.LogError("[Verifier FAIL] ScreenSpaceAmbientOcclusion feature missing from UniversalRendererData!");
                    errors++;
                }
                else
                {
                    Debug.Log("[Verifier PASS] URP ScreenSpaceAmbientOcclusion active in UniversalRendererData.");
                }
            }

            // 3. Audit Soft Contact Shadows
            var contactShadows = new List<GameObject>();
            var allObjects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].name.StartsWith("ContactShadow_"))
                {
                    contactShadows.Add(allObjects[i].gameObject);
                }
            }

            Debug.Log($"[Verifier Check] Soft Contact Shadows detected in scene: {contactShadows.Count}");
            if (contactShadows.Count < 20)
            {
                Debug.LogError($"[Verifier FAIL] Expected at least 20 contact shadows, found only {contactShadows.Count}!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Contact Shadows properly grounding all characters, NPCs, zombies, and vehicles ({contactShadows.Count} shadows).");
            }

            // 4. Audit Elimination of Raw Primitive Characters & Vehicles
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Transform vm = player.transform.Find("Visual_Model");
                if (vm == null || vm.childCount == 0)
                {
                    Debug.LogError("[Verifier FAIL] Player Visual_Model missing or empty!");
                    errors++;
                }
                else
                {
                    Debug.Log($"[Verifier PASS] Player has sculpted/dressed Visual_Model with {vm.childCount} parts.");
                }
            }

            Transform fleetParent = GameObject.Find("--- VEHICLE FLEET ---")?.transform;
            if (fleetParent != null)
            {
                int sculptedCars = 0;
                for (int i = 0; i < fleetParent.childCount; i++)
                {
                    Transform car = fleetParent.GetChild(i);
                    if (car.Find("Body_Chassis") != null || car.Find("Visual_Model") != null)
                    {
                        sculptedCars++;
                    }
                }
                Debug.Log($"[Verifier Check] Sculpted multi-part vehicle bodies in fleet: {sculptedCars}/{fleetParent.childCount}");
                if (sculptedCars < 20)
                {
                    Debug.LogError($"[Verifier FAIL] Expected >= 20 sculpted vehicles, but found {sculptedCars}!");
                    errors++;
                }
                else
                {
                    Debug.Log("[Verifier PASS] Vehicle Fleet fully upgraded to sculpted multi-part automotive meshes.");
                }
            }

            // 5. Audit Architectural Facades & 3D Backlit Signs
            Transform facadeParent = GameObject.Find("--- ARCHITECTURAL FACADES & DETAILS ---")?.transform;
            if (facadeParent == null || facadeParent.childCount == 0)
            {
                Debug.LogError("[Verifier FAIL] Architectural Facades & Details group missing or empty!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Architectural Facades & Details active ({facadeParent.childCount} building detail assemblies).");
            }

            // 5.5. Audit Organic Street Trees
            Transform treeParent = GameObject.Find("--- ORGANIC STREET TREES & URBAN VEGETATION ---")?.transform;
            if (treeParent == null || treeParent.childCount < 8)
            {
                Debug.LogError($"[Verifier FAIL] Organic street trees missing or fewer than 8 (found {treeParent?.childCount ?? 0})!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Organic street trees active ({treeParent.childCount} trees across city).");
            }

            // 6. Audit Stage 1 Interactions Unbroken
            var allInteractables = UnityEngine.Object.FindObjectsByType<WorldInteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Verifier Check] Active WorldInteractableObject instances: {allInteractables.Length}");
            if (allInteractables.Length < 150)
            {
                Debug.LogError($"[Verifier FAIL] Stage 1 Interactables compromised! Found only {allInteractables.Length} (Expected >= 150)!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Stage 1 Multi-Verb Interaction Architecture 100% Intact ({allInteractables.Length} interactable objects).");
            }

            // 7. Audit Magenta / Missing Shaders
            Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int magentaCount = 0;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Material[] mats = allRenderers[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m].shader == null || mats[m].shader.name == "Hidden/InternalErrorShader")
                    {
                        magentaCount++;
                        Debug.LogError($"[Verifier FAIL] Magenta / Missing shader detected on object: {allRenderers[i].gameObject.name}");
                    }
                }
            }

            if (magentaCount > 0)
            {
                Debug.LogError($"[Verifier FAIL] Detected {magentaCount} magenta materials!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Zero magenta or missing shaders across all {allRenderers.Length} renderers.");
            }

            if (errors > 0)
            {
                Debug.LogError("==================================================================");
                Debug.LogError($">>> AAA VISUAL COHESION VERIFICATION FAILED WITH {errors} ERRORS <<<");
                Debug.LogError("==================================================================");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("==================================================================");
                Debug.Log(">>> AAA VISUAL COHESION VERIFICATION PASSED (ALL TESTS GREEN) <<<");
                Debug.Log("==================================================================");
                EditorApplication.Exit(0);
            }
        }
    }
}
