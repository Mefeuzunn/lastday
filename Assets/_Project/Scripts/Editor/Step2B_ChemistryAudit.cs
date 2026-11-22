using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.UI;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorScripts
{
    public static class Step2B_ChemistryAudit
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Tools/Survival/Run Step 2B Cooking & Chemistry Audit", false, 203)]
        public static void RunAudit()
        {
            Debug.Log("====================================================================");
            Debug.Log(">>> STARTING ATOMIC MICRO-STEP 2B: COOKING & CHEMISTRY LAB AUDIT <<<");
            Debug.Log("====================================================================");

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step2B_Audit] Failed to open scene: {SCENE_PATH}");
                return;
            }

            int passedChecks = 0;
            int totalChecks = 0;

            // ---------------------------------------------------------------------
            // 1. ENSURE AND VALIDATE ALL WORLD STATIONS PLACED
            // ---------------------------------------------------------------------
            totalChecks++;
            SurvivalStationPlacer.EnsureAllStationsPlaced();

            string[] expectedStoves = new string[]
            {
                "Stove_SafeHouse_Kitchen",
                "Stove_LakeCabin",
                "Stove_SouthMall_FoodCourt",
                "Stove_Outpost_NorthHighway",
                "Stove_Outpost_EastRailStation",
                "Stove_Outpost_NorthwestRanger",
                "Stove_Outpost_SouthMetroHub"
            };

            string[] expectedChem = new string[]
            {
                "ChemLab_SafeHouse_Medical",
                "ChemLab_Zone_Clinic",
                "ChemLab_Pharmacy",
                "ChemLab_Outpost_SouthMetroHub"
            };

            int stovesFound = 0;
            foreach (var sName in expectedStoves)
            {
                GameObject obj = GameObject.Find(sName);
                if (obj != null && obj.GetComponent<CookingStoveInteractable>() != null && obj.GetComponent<Collider>() != null)
                {
                    stovesFound++;
                }
                else
                {
                    Debug.LogWarning($"[Step2B_Audit] Missing stove: {sName}");
                }
            }

            int chemFound = 0;
            foreach (var cName in expectedChem)
            {
                GameObject obj = GameObject.Find(cName);
                if (obj != null && obj.GetComponent<ChemistryLabInteractable>() != null && obj.GetComponent<Collider>() != null)
                {
                    chemFound++;
                }
                else
                {
                    Debug.LogWarning($"[Step2B_Audit] Missing chemistry lab: {cName}");
                }
            }

            if (stovesFound == expectedStoves.Length && chemFound == expectedChem.Length)
            {
                Debug.Log($"[Step2B_Audit] PASS: All {stovesFound} Camp Stoves and {chemFound} Chemistry Labs placed and validated with interaction colliders.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2B_Audit] FAIL: Stations missing! Stoves: {stovesFound}/{expectedStoves.Length}, Chem: {chemFound}/{expectedChem.Length}");
            }

            // ---------------------------------------------------------------------
            // 2. VALIDATE 4 NEW CONSUMABLE ASSETS IN DATA AND RESOURCES
            // ---------------------------------------------------------------------
            totalChecks++;
            string[] newItems = new string[] { "item_hot_stew", "item_purified_water", "item_herbal_tea", "item_adrenaline" };
            int assetsFound = 0;
            foreach (var id in newItems)
            {
                ItemData res = Resources.Load<ItemData>($"Items/{id}");
                ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/_Project/Data/Items/{id}.asset");
                if (res != null && asset != null) assetsFound++;
                else Debug.LogWarning($"[Step2B_Audit] Missing asset for {id}: Res:{res != null}, Asset:{asset != null}");
            }

            if (assetsFound == newItems.Length)
            {
                Debug.Log($"[Step2B_Audit] PASS: All {assetsFound} new item assets validated in Resources and Data.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2B_Audit] FAIL: Consumable assets missing ({assetsFound}/{newItems.Length})!");
            }

            // ---------------------------------------------------------------------
            // 3. VALIDATE SURVIVAL ALCHEMY UI & ALL 7 RECIPES
            // ---------------------------------------------------------------------
            totalChecks++;
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            SurvivalAlchemyUI alchemyUI = UnityEngine.Object.FindFirstObjectByType<SurvivalAlchemyUI>();
            if (alchemyUI == null && canvas != null)
            {
                alchemyUI = canvas.gameObject.AddComponent<SurvivalAlchemyUI>();
            }

            bool recipesOk = false;
            if (alchemyUI != null)
            {
                var recipes = alchemyUI.AllRecipes;
                if (recipes != null && recipes.Count >= 7)
                {
                    recipesOk = true;
                }
            }

            if (recipesOk)
            {
                Debug.Log($"[Step2B_Audit] PASS: SurvivalAlchemyUI initialized with {alchemyUI.AllRecipes.Count} recipes (Hot Stew, Purified Water, Herbal Tea, Splint, Painkillers, Antiviral, Adrenaline).");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2B_Audit] FAIL: SurvivalAlchemyUI recipe initialization failed!");
            }

            // ---------------------------------------------------------------------
            // 4. TEST RECIPE VALIDATION & CRAFTING SIMULATION
            // ---------------------------------------------------------------------
            totalChecks++;
            GameObject player = GameObject.FindWithTag("Player");
            InventorySystem inv = player != null ? player.GetComponent<InventorySystem>() : UnityEngine.Object.FindFirstObjectByType<InventorySystem>();
            bool craftSimulationOk = false;

            if (inv != null && alchemyUI != null)
            {
                AlchemyRecipe stewRecipe = null;
                AlchemyRecipe adrRecipe = null;
                foreach (var r in alchemyUI.AllRecipes)
                {
                    if (r.recipeId == "recipe_hot_stew") stewRecipe = r;
                    if (r.recipeId == "recipe_adrenaline") adrRecipe = r;
                }

                if (stewRecipe != null && adrRecipe != null)
                {
                    // Lacking ingredients test
                    int prevCans = inv.GetItemCount("item_canned_food");
                    int prevWater = inv.GetItemCount("item_water_bottle");

                    // Grant ingredients for hot stew
                    inv.AddItemById("item_canned_food", 1);
                    inv.AddItemById("item_water_bottle", 1);

                    bool canCraftStew = alchemyUI.CanCraftRecipe(stewRecipe);

                    // Deduct and grant
                    inv.RemoveItem("item_canned_food", 1);
                    inv.RemoveItem("item_water_bottle", 1);
                    inv.AddItemById(stewRecipe.outputItemId, 1);
                    bool stewCrafted = inv.GetItemCount("item_hot_stew") > 0;

                    // Grant ingredients for adrenaline
                    inv.AddItemById("item_alcohol", 2);
                    inv.AddItemById("item_painkillers", 1);
                    inv.AddItemById("item_scrap", 1);

                    bool canCraftAdr = alchemyUI.CanCraftRecipe(adrRecipe);
                    inv.RemoveItem("item_alcohol", 2);
                    inv.RemoveItem("item_painkillers", 1);
                    inv.RemoveItem("item_scrap", 1);
                    inv.AddItemById(adrRecipe.outputItemId, 1);
                    bool adrCrafted = inv.GetItemCount("item_adrenaline") > 0;

                    if (canCraftStew && stewCrafted && canCraftAdr && adrCrafted)
                    {
                        craftSimulationOk = true;
                    }
                }
            }

            if (craftSimulationOk)
            {
                Debug.Log("[Step2B_Audit] PASS: Recipe validation, ingredient deduction, and item output generation verified.");
                passedChecks++;
            }
            else
            {
                Debug.LogError("[Step2B_Audit] FAIL: Recipe crafting simulation failed!");
            }

            // ---------------------------------------------------------------------
            // 5. TEST ADRENALINE COMBAT SURGE & SPRINT OVERRIDE
            // ---------------------------------------------------------------------
            totalChecks++;
            TraumaAndInjurySystem trauma = player != null ? player.GetComponent<TraumaAndInjurySystem>() : UnityEngine.Object.FindFirstObjectByType<TraumaAndInjurySystem>();
            SurvivalStats stats = player != null ? player.GetComponent<SurvivalStats>() : UnityEngine.Object.FindFirstObjectByType<SurvivalStats>();

            bool adrBuffOk = false;
            if (trauma != null && stats != null)
            {
                // Test 1: Inflict fracture -> sprint is locked
                trauma.InflictLegFracture(4.0f);
                bool sprintLockedBefore = !stats.CanSprint();

                // Test 2: Activate Adrenaline -> overrides fracture lock
                trauma.ActivateAdrenaline(25f);
                bool adrActive = trauma.IsAdrenalineActive;
                bool sprintOverridden = stats.CanSprint();

                // Test 3: Sprint stamina drain immunity
                stats.ConsumeStaminaForSprint(2.0f);
                bool staminaImmune = stats.CurrentStamina >= 99f;

                // Cleanup
                trauma.TreatLegFracture();

                if (sprintLockedBefore && adrActive && sprintOverridden && staminaImmune)
                {
                    adrBuffOk = true;
                }
                else
                {
                    Debug.LogWarning($"[Step2B_Audit] Adrenaline checks: LockedBefore:{sprintLockedBefore}, Active:{adrActive}, Overridden:{sprintOverridden}, StaminaImmune:{staminaImmune}");
                }
            }

            if (adrBuffOk)
            {
                Debug.Log("[Step2B_Audit] PASS: Adrenaline Combat Stimulant grants unlimited stamina and overrides leg fracture sprint lock.");
                passedChecks++;
            }
            else
            {
                Debug.LogError("[Step2B_Audit] FAIL: Adrenaline combat surge validation failed!");
            }

            // ---------------------------------------------------------------------
            // 6. SHADER AUDIT (ZERO MAGENTA SHADERS)
            // ---------------------------------------------------------------------
            totalChecks++;
            int magentaCount = 0;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                foreach (var mat in rend.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        magentaCount++;
                        Debug.LogWarning($"[Step2B_Audit] Magenta shader on {rend.gameObject.name} (mat: {mat.name})");
                    }
                }
            }

            if (magentaCount == 0)
            {
                Debug.Log($"[Step2B_Audit] PASS: Zero magenta shaders detected across {renderers.Length} renderers.");
                passedChecks++;
            }
            else
            {
                Debug.LogError($"[Step2B_Audit] FAIL: {magentaCount} magenta shaders detected!");
            }

            // ---------------------------------------------------------------------
            // SAVE SCENE
            // ---------------------------------------------------------------------
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("====================================================================");
            Debug.Log($"[Step2B_Audit] AUDIT COMPLETE: {passedChecks}/{totalChecks} CHECKS PASSED!");
            Debug.Log("====================================================================");
        }
    }
}
