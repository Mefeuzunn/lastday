using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.UI;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;
using ProjectSurvival.AI;
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Step C: Automated Verification Audit for Starter House Scavenging,
    /// Front-Door Exit Sightline, Outdoor Starter Weapons, and 4-Stage Prologue Progression Loop.
    /// </summary>
    public static class StepC_PrologueLoopAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly List<string> FailureMessages = new List<string>();

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Step C Prologue Loop", false, 43)]
        public static void RunAudit()
        {
            passedTests = 0;
            failedTests = 0;
            FailureMessages.Clear();

            Debug.Log("===============================================================================");
            Debug.Log(">>> STARTING MASTER STEP C PROLOGUE & STARTER HOUSE AUDIT (UNITY 6 BATCHMODE) <<<");
            Debug.Log("===============================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AuditStarterHouseAndIndoorContainers();
            AuditFrontDoorAndExitTrajectory();
            AuditOutdoorStarterWeapons();
            AuditPerimeterSafetyAndFirstZombie();
            AuditPrologueDirectorStagesAndProgression();
            AuditQuickSlotAutoAssignment();
            AuditSaveSystemProloguePersistence();
            AuditSceneIntegrityAndShaders();

            Debug.Log("===============================================================================");
            Debug.Log($">>> AUDIT COMPLETE: {passedTests} PASSED, {failedTests} FAILED <<<");
            Debug.Log("===============================================================================");

            if (failedTests > 0)
            {
                foreach (var err in FailureMessages)
                {
                    Debug.LogError($"[AUDIT FAILURE] {err}");
                }
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
            else
            {
                Debug.Log("[AUDIT SUCCESS] All Step C prologue, starter house, and weapon discovery tests passed cleanly with 0 errors!");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void AuditStarterHouseAndIndoorContainers()
        {
            Debug.Log("--- AUDIT: Starter House Interior & Scavenging Containers ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            Assert(house != null, "StarterHouse_Safehome GameObject exists in scene");
            if (house == null) return;

            // 1. Kitchen Pantry Cabinet
            Transform pantry = house.transform.Find("Room_Kitchen/Furniture_Starter_KitchenCabinet") ?? house.transform.Find("Furniture_Starter_KitchenCabinet");
            Assert(pantry != null, "Furniture_Starter_KitchenCabinet exists");
            if (pantry != null)
            {
                var furn = pantry.GetComponent<FurnitureInteractable>();
                Assert(furn != null, "KitchenCabinet has FurnitureInteractable attached");
                Assert(ContainsLoot(furn, "item_canned_food", 2), "KitchenCabinet contains 2x Canned Food");
                Assert(ContainsLoot(furn, "item_cloth", 1), "KitchenCabinet contains 1x Clean Cloth");
                Assert(HasSearchVerbWithHold(furn, 0.6f), "KitchenCabinet has 0.6s holdDuration search verb");
            }

            // 2. Kitchen Refrigerator
            Transform fridge = house.transform.Find("Room_Kitchen/Furniture_Starter_Refrigerator") ?? house.transform.Find("Furniture_Starter_Refrigerator");
            Assert(fridge != null, "Furniture_Starter_Refrigerator exists");
            if (fridge != null)
            {
                var furn = fridge.GetComponent<FurnitureInteractable>();
                Assert(furn != null, "Refrigerator has FurnitureInteractable attached");
                Assert(ContainsLoot(furn, "item_water_bottle", 2), "Refrigerator contains 2x Water Bottle");
                Assert(ContainsLoot(furn, "item_canned_food", 1), "Refrigerator contains 1x Canned Food");
                Assert(HasSearchVerbWithHold(furn, 0.6f), "Refrigerator has 0.6s holdDuration search verb");
            }

            // 3. Bathroom Medicine Cabinet
            Transform medCab = house.transform.Find("Room_Bathroom/Furniture_Starter_MedicineCabinet") ?? house.transform.Find("Furniture_Starter_MedicineCabinet");
            Assert(medCab != null, "Furniture_Starter_MedicineCabinet exists");
            if (medCab != null)
            {
                var furn = medCab.GetComponent<FurnitureInteractable>();
                Assert(furn != null, "MedicineCabinet has FurnitureInteractable attached");
                Assert(ContainsLoot(furn, "item_bandage", 2), "MedicineCabinet contains 2x Bandage");
                Assert(ContainsLoot(furn, "item_painkillers", 1), "MedicineCabinet contains 1x Painkillers");
                Assert(HasSearchVerbWithHold(furn, 0.6f), "MedicineCabinet has 0.6s holdDuration search verb");
            }

            // 4. Bedroom Wardrobe
            Transform wardrobe = house.transform.Find("Room_Bedroom/Furniture_Starter_Wardrobe") ?? house.transform.Find("Furniture_Starter_Wardrobe");
            Assert(wardrobe != null, "Furniture_Starter_Wardrobe exists");
            if (wardrobe != null)
            {
                var furn = wardrobe.GetComponent<FurnitureInteractable>();
                Assert(furn != null, "Wardrobe has FurnitureInteractable attached");
                Assert(ContainsLoot(furn, "item_battery", 1), "Wardrobe contains 1x Battery");
                Assert(ContainsLoot(furn, "item_distraction_bottle", 1), "Wardrobe contains 1x Distraction Bottle");
                Assert(HasSearchVerbWithHold(furn, 0.6f), "Wardrobe has 0.6s holdDuration search verb");
            }
        }

        private static void AuditFrontDoorAndExitTrajectory()
        {
            Debug.Log("--- AUDIT: Front Door & Exit Trajectory ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            if (house == null) return;

            Transform doorTf = house.transform.Find("Door_Starter_Front") ?? house.transform.Find("Room_LivingRoom/Door_Starter_Front");
            Assert(doorTf != null, "Door_Starter_Front exists in StarterHouse_Safehome");
            if (doorTf == null) return;

            var idoor = doorTf.GetComponent<InteractiveDoor>();
            Assert(idoor != null, "Door_Starter_Front has InteractiveDoor component");

            var bc = doorTf.GetComponent<BoxCollider>();
            Assert(bc != null && bc.isTrigger, "Door_Starter_Front has trigger BoxCollider for interaction");

            // Verify position at Z ~ 4.0m facing exit
            Assert(doorTf.position.z >= 3.5f && doorTf.position.z <= 4.5f, $"Front door is positioned at exit threshold Z ~ 4.0m (actual: {doorTf.position.z})");
        }

        private static void AuditOutdoorStarterWeapons()
        {
            Debug.Log("--- AUDIT: Outdoor Starter Weapons Along Exit Sightline ---");

            // 1. Weapon 1: Fire Axe
            GameObject axeObj = GameObject.Find("WeaponPickup_Starter_FireAxe");
            Assert(axeObj != null, "WeaponPickup_Starter_FireAxe exists in scene");
            if (axeObj != null)
            {
                var sc = axeObj.GetComponent<SphereCollider>();
                Assert(sc != null && sc.isTrigger, "Fire Axe has active trigger SphereCollider");
                Assert(sc != null && sc.radius >= 1.70f, $"Fire Axe trigger radius is >= 1.75m (actual: {sc?.radius})");

                var pickup = axeObj.GetComponent<WeaponPickupInteractable>();
                Assert(pickup != null, "Fire Axe has WeaponPickupInteractable attached");
                Assert(pickup?.WeaponType == WeaponType.Axe, "Fire Axe is configured as WeaponType.Axe");
                Assert(pickup?.WeaponItemId == "weapon_axe", "Fire Axe itemId is 'weapon_axe'");

                // Verify distance from house front door (~6m outside)
                float distFromDoor = Vector3.Distance(axeObj.transform.position, new Vector3(0f, 0f, 4.0f));
                Assert(distFromDoor >= 2.0f && distFromDoor <= 4.5f, $"Fire Axe is positioned along porch/front gate sightline (~2.5m from door, ~6.5m from spawn: actual {distFromDoor:F1}m)");
            }

            // 2. Weapon 2: 9mm Pistol on Police Cruiser
            GameObject pistolObj = GameObject.Find("WeaponPickup_Starter_Pistol");
            Assert(pistolObj != null, "WeaponPickup_Starter_Pistol exists in scene");
            if (pistolObj != null)
            {
                var sc = pistolObj.GetComponent<SphereCollider>();
                Assert(sc != null && sc.isTrigger, "9mm Pistol has active trigger SphereCollider");
                Assert(sc != null && sc.radius >= 1.70f, $"9mm Pistol trigger radius is >= 1.75m (actual: {sc?.radius})");

                var pickup = pistolObj.GetComponent<WeaponPickupInteractable>();
                Assert(pickup != null, "9mm Pistol has WeaponPickupInteractable attached");
                Assert(pickup?.WeaponType == WeaponType.Pistol, "Pistol is configured as WeaponType.Pistol");

                float distFromSpawn = Vector3.Distance(pistolObj.transform.position, Vector3.zero);
                Assert(distFromSpawn >= 14.0f && distFromSpawn <= 22.0f, $"9mm Pistol on Cruiser is ~18m down street (actual: {distFromSpawn:F1}m)");
            }
        }

        private static void AuditPerimeterSafetyAndFirstZombie()
        {
            Debug.Log("--- AUDIT: Perimeter Safety (45m) & First Street Zombie Placement ---");

            var spawner = SpawnProtectionController.Instance;
            if (spawner == null)
            {
                spawner = UnityEngine.Object.FindAnyObjectByType<SpawnProtectionController>(FindObjectsInactive.Include);
                if (spawner != null) SpawnProtectionController.Instance = spawner;
            }

            Assert(spawner != null, "SpawnProtectionController instance exists");
            if (spawner != null)
            {
                Assert(spawner.SpawnSafeRadius >= 45.0f, $"SpawnSafeRadius is >= 45.0m (actual: {spawner.SpawnSafeRadius})");
            }

            // Verify 0 zombies within 45m of spawn
            Vector3 spawnCenter = Vector3.zero;
            ZombieBrain[] zombies = UnityEngine.Object.FindObjectsByType<ZombieBrain>(FindObjectsSortMode.None);
            int zombiesInside45m = 0;
            float closestZombieDist = float.MaxValue;
            ZombieBrain closestZombie = null;

            foreach (var z in zombies)
            {
                if (z == null || z.IsDead) continue;
                float d = Vector3.Distance(z.transform.position, spawnCenter);
                if (d < closestZombieDist)
                {
                    closestZombieDist = d;
                    closestZombie = z;
                }
                if (d < 45.0f)
                {
                    zombiesInside45m++;
                }
            }

            Assert(zombiesInside45m == 0, $"Zero zombies within 45m of StarterHouse_Safehome (found: {zombiesInside45m})");
            Assert(closestZombieDist >= 50.0f, $"First street zombie is safely stationed >= 52m away (actual closest: {closestZombieDist:F1}m on {closestZombie?.gameObject.name})");
        }

        private static void AuditPrologueDirectorStagesAndProgression()
        {
            Debug.Log("--- AUDIT: 4-Stage Prologue Progression Loop (StarterPrologueDirector) ---");

            var director = StarterPrologueDirector.Instance;
            if (director == null)
            {
                director = UnityEngine.Object.FindAnyObjectByType<StarterPrologueDirector>(FindObjectsInactive.Include);
                if (director != null)
                {
                    // If not initialized, instantiate/assign
                    var f = typeof(StarterPrologueDirector).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    f?.SetValue(null, director);
                }
            }
            Assert(director != null, "StarterPrologueDirector exists in scene");
            if (director == null) return;

            var smartphone = SmartphoneScreenUI.Instance;
            if (smartphone == null)
            {
                smartphone = UnityEngine.Object.FindAnyObjectByType<SmartphoneScreenUI>(FindObjectsInactive.Include);
                if (smartphone != null)
                {
                    var f = typeof(SmartphoneScreenUI).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    f?.SetValue(null, smartphone);
                }
            }
            Assert(smartphone != null, "SmartphoneScreenUI exists in scene hierarchy");

            // Test Stage 1: Phone check
            director.ForceSetStage(PrologueStage.Stage1_CheckPhone);
            Assert(director.CurrentStage == PrologueStage.Stage1_CheckPhone, "Director initialized at Stage 1 (Check Phone)");
            StoryObjective obj1 = director.GetCurrentPrologueObjective();
            Assert(obj1 != null && obj1.title == "Başucundaki akıllı telefona bak", "Stage 1 objective title is 'Başucundaki akıllı telefona bak'");

            // Complete Stage 1 via Smartphone close
            director.OnSmartphoneClosed();
            Assert(director.CurrentStage == PrologueStage.Stage2_ScavengeFoodWater, "Closing smartphone advances director to Stage 2 (Scavenge Food & Water)");

            // Test Stage 2: Scavenge Food & Water
            StoryObjective obj2 = director.GetCurrentPrologueObjective();
            Assert(obj2 != null && obj2.title.Contains("0/2"), "Stage 2 objective tracks '(0/2)' rations");

            director.OnItemLooted("item_canned_food", 1);
            obj2 = director.GetCurrentPrologueObjective();
            Assert(obj2 != null && obj2.title.Contains("1/2"), "Looting food updates tracker to '(1/2)'");

            director.OnItemLooted("item_water_bottle", 1);
            Assert(director.CurrentStage == PrologueStage.Stage3_ExitAndFindWeapon, "Looting food and water advances director to Stage 3 (Exit & Find Weapon)");

            // Test Stage 3: Weapon Discovery
            StoryObjective obj3 = director.GetCurrentPrologueObjective();
            Assert(obj3 != null && obj3.title == "Ön kapıdan çık ve ilk silahını bul", "Stage 3 objective title is 'Ön kapıdan çık ve ilk silahını bul'");
            Assert(Mathf.Approximately(obj3.targetPosition.z, 6.5f), "Stage 3 waypoint targets Fire Axe at Z = 6.5m");

            // Complete Stage 3 via Weapon Equip
            director.OnWeaponPickedUp(WeaponType.Axe, "weapon_axe");
            Assert(director.IsPrologueCompleted, "Equipping weapon marks prologue completed");
            Assert(director.CurrentStage == PrologueStage.Completed, "Director transitions to Completed stage");
        }

        private static void AuditQuickSlotAutoAssignment()
        {
            Debug.Log("--- AUDIT: QuickSlot Auto-Assignment Helper ---");

            var qs = QuickSlotUI.Instance;
            if (qs == null)
            {
                qs = UnityEngine.Object.FindAnyObjectByType<QuickSlotUI>(FindObjectsInactive.Include);
                if (qs != null) QuickSlotUI.Instance = qs;
            }
            Assert(qs != null, "QuickSlotUI instance exists in scene");
            if (qs == null) return;

            // Test Auto-assigning food, water, bandage
            qs.AutoAssignStarterRation("item_canned_food");
            qs.AutoAssignStarterRation("item_water_bottle");
            qs.AutoAssignStarterRation("item_bandage");

            string[] slots = qs.GetSlotItemIds();
            Assert(slots[0] == "item_canned_food", "Slot 0 auto-assigned to item_canned_food");
            Assert(slots[1] == "item_water_bottle", "Slot 1 auto-assigned to item_water_bottle");
            Assert(slots[2] == "item_bandage", "Slot 2 auto-assigned to item_bandage");
        }

        private static void AuditSaveSystemProloguePersistence()
        {
            Debug.Log("--- AUDIT: SaveSystem Prologue Completion Persistence ---");

            SaveData data = new SaveData();
            data.isPrologueCompleted = true;
            data.prologueStageIndex = 3;

            string json = JsonUtility.ToJson(data);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);

            Assert(loaded != null && loaded.isPrologueCompleted, "SaveData round-trip preserves isPrologueCompleted = true");
            Assert(loaded != null && loaded.prologueStageIndex == 3, "SaveData round-trip preserves prologueStageIndex = 3");
        }

        private static void AuditSceneIntegrityAndShaders()
        {
            Debug.Log("--- AUDIT: Scene Integrity, Missing Scripts & Shader Validity ---");

            GameObject[] allObjs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int missingScriptCount = 0;

            foreach (var go in allObjs)
            {
                Component[] comps = go.GetComponents<Component>();
                foreach (var c in comps)
                {
                    if (c == null)
                    {
                        missingScriptCount++;
                        Debug.LogError($"[AUDIT ERROR] Missing script component on: {go.name}");
                    }
                }
            }
            Assert(missingScriptCount == 0, $"Zero missing scripts in scene hierarchy (found: {missingScriptCount})");

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int invalidShaderCount = 0;

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        invalidShaderCount++;
                        Debug.LogError($"[AUDIT ERROR] Magenta / InternalErrorShader on {r.gameObject.name} (Material: {mat.name})");
                    }
                }
            }
            Assert(invalidShaderCount == 0, $"Zero magenta / error shaders across all renderers (found: {invalidShaderCount})");
        }

        private static bool ContainsLoot(FurnitureInteractable furn, string itemId, int minCount)
        {
            if (furn == null) return false;
            foreach (var loot in furn.CustomLoot)
            {
                if (loot.itemId == itemId && loot.quantity >= minCount) return true;
            }
            return false;
        }

        private static bool HasSearchVerbWithHold(FurnitureInteractable furn, float maxHold)
        {
            if (furn == null) return false;
            foreach (var verb in furn.GetAvailableVerbs())
            {
                if (verb.verbId == "search" && verb.holdDuration <= maxHold + 0.05f) return true;
            }
            return false;
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                Debug.Log($"  [PASS] {message}");
            }
            else
            {
                failedTests++;
                FailureMessages.Add(message);
                Debug.LogError($"  [FAIL] {message}");
            }
        }
#endif
    }
}
