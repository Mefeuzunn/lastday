using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    public static class UniversalInteractionVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Verify Universal World Interactions", false, 16)]
        public static void VerifyInteractions()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING UNIVERSAL INTERACTION VERIFICATION (BATCHMODE) <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Verifier FAIL] Could not open scene: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int errors = 0;

            // 1. Verify Player Scanner
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[Verifier FAIL] Player object not found!");
                errors++;
            }
            else
            {
                var scanner = player.GetComponent<PlayerInteractionScanner>();
                if (scanner == null)
                {
                    Debug.LogError("[Verifier FAIL] PlayerInteractionScanner missing on Player!");
                    errors++;
                }
                else
                {
                    Debug.Log("[Verifier PASS] PlayerInteractionScanner attached to Player.");
                }
            }

            // 2. Verify Contextual HUD
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Verifier FAIL] UI Canvas not found!");
                errors++;
            }
            else
            {
                var hud = canvas.GetComponentInChildren<ContextualInteractionHUD>(true);
                if (hud == null)
                {
                    Debug.LogError("[Verifier FAIL] ContextualInteractionHUD missing from Canvas!");
                    errors++;
                }
                else
                {
                    Debug.Log("[Verifier PASS] ContextualInteractionHUD located on Canvas.");
                }
            }

            // 3. Count WorldInteractableObject instances
            var allInteractables = UnityEngine.Object.FindObjectsByType<WorldInteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Verifier] Total WorldInteractableObject count in scene: {allInteractables.Length}");
            if (allInteractables.Length < 150)
            {
                Debug.LogError($"[Verifier FAIL] Expected at least 150 interactables, but found only {allInteractables.Length}!");
                errors++;
            }
            else
            {
                Debug.Log($"[Verifier PASS] Abundant world interactables active ({allInteractables.Length} objects).");
            }

            // 4. Verify Archetype Counts
            var vehicles = UnityEngine.Object.FindObjectsByType<VehicleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var windows = UnityEngine.Object.FindObjectsByType<WindowInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var furniture = UnityEngine.Object.FindObjectsByType<FurnitureInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fixtures = UnityEngine.Object.FindObjectsByType<UtilityFixtureInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var generators = UnityEngine.Object.FindObjectsByType<PowerGridInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var tacticalProps = UnityEngine.Object.FindObjectsByType<TacticalPropInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"[Verifier Archetypes] Vehicles: {vehicles.Length} (Expected >= 25)");
            Debug.Log($"[Verifier Archetypes] Windows: {windows.Length} (Expected >= 10)");
            Debug.Log($"[Verifier Archetypes] Furniture: {furniture.Length} (Expected >= 15)");
            Debug.Log($"[Verifier Archetypes] Utility Fixtures: {fixtures.Length} (Expected >= 10)");
            Debug.Log($"[Verifier Archetypes] Power Generators: {generators.Length} (Expected >= 5)");
            Debug.Log($"[Verifier Archetypes] Tactical Props: {tacticalProps.Length} (Expected >= 10)");

            if (vehicles.Length < 25) { Debug.LogError($"[Verifier FAIL] Vehicle count ({vehicles.Length}) < 25"); errors++; }
            if (windows.Length < 10) { Debug.LogError($"[Verifier FAIL] Window count ({windows.Length}) < 10"); errors++; }
            if (furniture.Length < 15) { Debug.LogError($"[Verifier FAIL] Furniture count ({furniture.Length}) < 15"); errors++; }
            if (fixtures.Length < 10) { Debug.LogError($"[Verifier FAIL] Fixture count ({fixtures.Length}) < 10"); errors++; }
            if (generators.Length < 5) { Debug.LogError($"[Verifier FAIL] Generator count ({generators.Length}) < 5"); errors++; }
            if (tacticalProps.Length < 10) { Debug.LogError($"[Verifier FAIL] Tactical Props count ({tacticalProps.Length}) < 10"); errors++; }

            // 5. Test Sample Interactivity on Archetypes
            if (vehicles.Length > 0)
            {
                var v = vehicles[0];
                var verbs = v.GetAvailableVerbs();
                Debug.Log($"[Verifier Check] Vehicle '{v.name}' provides {verbs.Count} verbs: {string.Join(", ", GetVerbNames(verbs))}");
                if (verbs.Count < 3) { Debug.LogError("[Verifier FAIL] Vehicle has fewer than 3 verbs!"); errors++; }
            }

            if (windows.Length > 0)
            {
                var w = windows[0];
                var verbs = w.GetAvailableVerbs();
                Debug.Log($"[Verifier Check] Window '{w.name}' provides {verbs.Count} verbs: {string.Join(", ", GetVerbNames(verbs))}");
                if (verbs.Count < 3) { Debug.LogError("[Verifier FAIL] Window has fewer than 3 verbs!"); errors++; }
            }

            if (generators.Length > 0)
            {
                var g = generators[0];
                var verbs = g.GetAvailableVerbs();
                Debug.Log($"[Verifier Check] Generator '{g.name}' provides {verbs.Count} verbs: {string.Join(", ", GetVerbNames(verbs))}");
                if (verbs.Count < 1) { Debug.LogError("[Verifier FAIL] Generator has no verbs!"); errors++; }
            }

            if (tacticalProps.Length > 0)
            {
                var t = tacticalProps[0];
                var verbs = t.GetAvailableVerbs();
                Debug.Log($"[Verifier Check] Tactical Prop '{t.name}' provides {verbs.Count} verbs: {string.Join(", ", GetVerbNames(verbs))}");
                if (verbs.Count < 1) { Debug.LogError("[Verifier FAIL] Tactical Prop has no verbs!"); errors++; }
            }

            // 6. Test Serialization Round-Trip
            if (allInteractables.Length > 0)
            {
                var sample = allInteractables[0];
                string state = sample.GetSerializedState();
                sample.LoadSerializedState(state);
                Debug.Log($"[Verifier PASS] Serialization round-trip for {sample.name}: {state}");
            }

            if (errors > 0)
            {
                Debug.LogError($"==================================================================");
                Debug.LogError($">>> UNIVERSAL INTERACTION VERIFICATION FAILED WITH {errors} ERRORS <<<");
                Debug.LogError($"==================================================================");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("==================================================================");
                Debug.Log(">>> UNIVERSAL INTERACTION VERIFICATION PASSED (ALL TESTS GREEN) <<<");
                Debug.Log("==================================================================");
                EditorApplication.Exit(0);
            }
        }

        private static List<string> GetVerbNames(IReadOnlyList<InteractionVerb> verbs)
        {
            var list = new List<string>();
            if (verbs != null)
            {
                for (int i = 0; i < verbs.Count; i++)
                {
                    list.Add($"{verbs[i].verbId} ('{verbs[i].displayLabel}')");
                }
            }
            return list;
        }
    }
}
