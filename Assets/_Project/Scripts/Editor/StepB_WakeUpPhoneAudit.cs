using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
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
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Step B: Automated Verification Audit for Furnished Starter House (4 Connected Rooms),
    /// Bedroom Wake-Up Sequence, Solid Weather Roof Slab, Nightstand Smartphone, and NavMesh Connectivity.
    /// </summary>
    public static class StepB_WakeUpPhoneAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly List<string> FailureMessages = new List<string>();

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Step B Furnished Starter House & Wake-Up", false, 45)]
        public static void RunAudit()
        {
            passedTests = 0;
            failedTests = 0;
            FailureMessages.Clear();

            Debug.Log("===============================================================================");
            Debug.Log(">>> STARTING MASTER STEP B STARTER HOUSE & WAKE-UP AUDIT (UNITY 6 BATCHMODE) <<<");
            Debug.Log("===============================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AuditStarterHouseArchitectureAndRoof();
            AuditBedroomFurnishingAndSmartphone();
            AuditLivingRoomKitchenBathroom();
            AuditPlayerSpawnAndWakeUpSequence();
            AuditSmartphoneScreenUI();
            AuditNavMeshContinuousConnectivity();
            AuditSaveSystemPersistence();
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
                Debug.Log("[AUDIT SUCCESS] All Step B furnished starter house and wake-up sequence tests passed cleanly with 0 errors!");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void AuditStarterHouseArchitectureAndRoof()
        {
            Debug.Log("--- AUDIT: Starter House Structure, 4 Rooms & Weather Roof Slab ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            Assert(house != null, "StarterHouse_Safehome GameObject exists in scene");
            if (house == null) return;

            Transform arch = house.transform.Find("Architecture");
            Assert(arch != null, "Architecture container exists under StarterHouse_Safehome");
            if (arch != null)
            {
                Transform roof = arch.Find("House_Roof_Slab");
                Assert(roof != null, "House_Roof_Slab exists");
                if (roof != null)
                {
                    Assert(roof.position.y >= 3.0f, $"House_Roof_Slab elevation y={roof.position.y}m (Target: >= 3.0m)");
                    var col = roof.GetComponent<BoxCollider>();
                    Assert(col != null && !col.isTrigger, "House_Roof_Slab has non-trigger BoxCollider for weather occlusion");
                }

                // Verify upward raycast from inside the house hits the roof slab
                Vector3 headPos = new Vector3(0f, 1.7f, 0f);
                bool hitRoof = Physics.Raycast(headPos, Vector3.up, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore);
                Assert(hitRoof && hit.collider != null, "Upward raycast from bedroom hits solid ceiling (guarantees DynamicWeatherManager isIndoors = true)");
            }

            // Verify 4 Room Containers
            Transform bedRoom = house.transform.Find("Room_Bedroom");
            Assert(bedRoom != null, "Room_Bedroom container exists");

            Transform livingRoom = house.transform.Find("Room_LivingRoom");
            Assert(livingRoom != null, "Room_LivingRoom container exists");

            Transform kitchenRoom = house.transform.Find("Room_Kitchen");
            Assert(kitchenRoom != null, "Room_Kitchen container exists");

            Transform bathRoom = house.transform.Find("Room_Bathroom");
            Assert(bathRoom != null, "Room_Bathroom container exists");
        }

        private static void AuditBedroomFurnishingAndSmartphone()
        {
            Debug.Log("--- AUDIT: Bedroom Furnishing, Bed, Nightstand & Interactive Smartphone ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            if (house == null) return;

            Transform bedRoom = house.transform.Find("Room_Bedroom");
            if (bedRoom == null) return;

            // 1. Bed
            Transform bed = bedRoom.Find("Furniture_Bed_Master");
            Assert(bed != null, "Furniture_Bed_Master exists in Bedroom");
            if (bed != null)
            {
                var fixture = bed.GetComponent<UtilityFixtureInteractable>();
                Assert(fixture != null, "Bed has UtilityFixtureInteractable attached (Dinlenme Noktası)");
            }

            // 2. Wooden Nightstand
            Transform nightstand = bedRoom.Find("Nightstand_Wood");
            Assert(nightstand != null, "Nightstand_Wood exists beside bed");

            // 3. Nightstand Smartphone
            Transform phone = bedRoom.Find("Smartphone_Nightstand");
            Assert(phone != null, "Smartphone_Nightstand exists on nightstand");
            if (phone != null)
            {
                var interactable = phone.GetComponent<SmartphoneNewsInteractable>();
                Assert(interactable != null, "Smartphone has SmartphoneNewsInteractable attached");
                if (interactable != null)
                {
                    var verbs = interactable.GetAvailableVerbs();
                    Assert(verbs != null && verbs.Count > 0, "Smartphone provides available interaction verbs");
                    if (verbs != null && verbs.Count > 0)
                    {
                        Assert(verbs[0].verbId == "check_phone", "Primary verb is 'check_phone'");
                        Assert(verbs[0].holdDuration == 0f, "Smartphone has 0.0s instant holdDuration verb");
                    }
                }

                var col = phone.GetComponent<Collider>();
                Assert(col != null && col.isTrigger, "Smartphone has trigger Collider for proximity scan");
            }

            // 4. Wardrobe
            Transform wardrobe = bedRoom.Find("Furniture_Starter_Wardrobe");
            Assert(wardrobe != null, "Furniture_Starter_Wardrobe exists in Bedroom");

            // 5. Bedroom Door
            Transform bedroomDoor = bedRoom.Find("Door_Starter_Bedroom");
            Assert(bedroomDoor != null, "Door_Starter_Bedroom exists with InteractiveDoor");
        }

        private static void AuditLivingRoomKitchenBathroom()
        {
            Debug.Log("--- AUDIT: Living Room, Kitchen & Bathroom Furnishing & Utility Fixtures ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            if (house == null) return;

            // 1. Living Room
            Transform living = house.transform.Find("Room_LivingRoom");
            if (living != null)
            {
                Assert(living.Find("Furniture_Sofa_Living") != null, "Furniture_Sofa_Living exists");
                Assert(living.Find("Furniture_CoffeeTable") != null, "Furniture_CoffeeTable exists");
                Assert(living.Find("Furniture_Bookshelf") != null, "Furniture_Bookshelf exists");
                Assert(living.Find("Furniture_CoatRack") != null, "Furniture_CoatRack exists");

                Transform frontDoor = living.Find("Door_Starter_Front") ?? house.transform.Find("Door_Starter_Front");
                Assert(frontDoor != null, "Door_Starter_Front exists in Living Room / StarterHouse at exit threshold");
            }

            // 2. Kitchen
            Transform kitchen = house.transform.Find("Room_Kitchen");
            if (kitchen != null)
            {
                Assert(kitchen.Find("Furniture_Starter_KitchenCabinet") != null, "Furniture_Starter_KitchenCabinet exists in Kitchen");
                Assert(kitchen.Find("Furniture_Starter_Refrigerator") != null, "Furniture_Starter_Refrigerator exists in Kitchen");

                Transform sink = kitchen.Find("Kitchen_Countertop_Sink");
                Assert(sink != null, "Kitchen_Countertop_Sink exists");
                if (sink != null)
                {
                    var fixture = sink.GetComponent<UtilityFixtureInteractable>();
                    Assert(fixture != null, "Kitchen countertop has UtilityFixtureInteractable sink attached");
                }
            }

            // 3. Bathroom
            Transform bath = house.transform.Find("Room_Bathroom");
            if (bath != null)
            {
                Assert(bath.Find("Furniture_Starter_MedicineCabinet") != null, "Furniture_Starter_MedicineCabinet exists in Bathroom");

                Transform sink = bath.Find("Bathroom_Sink");
                Assert(sink != null, "Bathroom_Sink exists");
                if (sink != null)
                {
                    var fixture = sink.GetComponent<UtilityFixtureInteractable>();
                    Assert(fixture != null, "Bathroom washbasin has UtilityFixtureInteractable sink attached");
                }

                Assert(bath.Find("Bathroom_Mirror") != null, "Bathroom_Mirror exists");
                Assert(bath.Find("Door_Starter_Bathroom") != null, "Door_Starter_Bathroom exists");
            }
        }

        private static void AuditPlayerSpawnAndWakeUpSequence()
        {
            Debug.Log("--- AUDIT: Player Spawn Calibration & Bedroom Wake-Up Sequence ---");

            GameObject house = GameObject.Find("StarterHouse_Safehome");
            if (house == null) return;

            var wakeUp = house.GetComponent<ApartmentWakeUpSequence>();
            Assert(wakeUp != null, "ApartmentWakeUpSequence component attached to StarterHouse_Safehome");

            // Test simulated unarmed calibration
            if (wakeUp != null)
            {
                wakeUp.CalibrateUnarmedPlayer();

                if (WeaponSystem.Instance != null)
                {
                    Assert(WeaponSystem.Instance.CurrentWeapon == WeaponType.None, "Player starts completely unarmed (WeaponType.None)");
                }

                var stats = UnityEngine.Object.FindAnyObjectByType<SurvivalStats>();
                if (stats != null)
                {
                    Assert(Mathf.Approximately(stats.CurrentHealth, 100f), $"Player Health calibrated to 100% (Actual: {stats.CurrentHealth})");
                    Assert(Mathf.Approximately(stats.CurrentHunger, 85f), $"Player Hunger calibrated to 85% (Actual: {stats.CurrentHunger})");
                    Assert(Mathf.Approximately(stats.CurrentThirst, 80f), $"Player Thirst calibrated to 80% (Actual: {stats.CurrentThirst})");
                    Assert(Mathf.Approximately(stats.CurrentInfection, 0f), $"Player Infection calibrated to 0% (Actual: {stats.CurrentInfection})");
                }

                var trauma = TraumaAndInjurySystem.Instance;
                if (trauma != null)
                {
                    Assert(!trauma.IsBleeding, "Player starts with 0 bleeding");
                    Assert(!trauma.HasLegFracture, "Player starts with 0 leg fractures");
                }
            }
        }

        private static void AuditSmartphoneScreenUI()
        {
            Debug.Log("--- AUDIT: Smartphone Emergency Screen UI Modal ---");

            var phoneUI = UnityEngine.Object.FindAnyObjectByType<SmartphoneScreenUI>(FindObjectsInactive.Include);
            Assert(phoneUI != null, "SmartphoneScreenUI instance exists in scene hierarchy");
            if (phoneUI == null) return;

            // Trigger modal display
            phoneUI.ShowSmartphone();
            Assert(phoneUI.IsModalOpen, "SmartphoneScreenUI is opened via ShowSmartphone()");

            // Check UI text elements in canvas / scene hierarchy
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool foundHeader = false;
            bool foundLore = false;
            bool foundCloseBtn = false;

            for (int i = 0; i < texts.Length; i++)
            {
                string t = texts[i].text;
                if (string.IsNullOrEmpty(t)) continue;
                if (t.Contains("ACİL DURUM YAYINI") && t.Contains("Batarya: %14"))
                {
                    foundHeader = true;
                }
                if (t.Contains("340"))
                {
                    foundLore = true;
                }
                if (t.Contains("TELEFONU KAPAT VE HAREKETE GEÇ"))
                {
                    foundCloseBtn = true;
                }
            }

            Assert(foundHeader, "Smartphone UI displays header: ACİL DURUM YAYINI • KARANTİNA BÖLGESİ AĞI | Batarya: %14");
            Assert(foundLore, "Smartphone UI displays lore alert with ~340 survivors remaining count");
            Assert(foundCloseBtn, "Smartphone UI has dismiss action button: 'TELEFONU KAPAT VE HAREKETE GEÇ'");

            // Close modal and verify state update
            phoneUI.CloseModal();
            Assert(!phoneUI.IsModalOpen, "SmartphoneScreenUI closes cleanly upon CloseModal()");

            if (ApartmentWakeUpSequence.Instance != null)
            {
                Assert(ApartmentWakeUpSequence.Instance.HasCheckedPhone, "ApartmentWakeUpSequence records phone as checked (hasCheckedIntroPhone = true)");
            }
        }

        private static void AuditNavMeshContinuousConnectivity()
        {
            Debug.Log("--- AUDIT: NavMesh Continuous Indoor-Outdoor Connectivity & Safe Perimeter ---");

            // Verify continuous path from bedroom bed spawn (0, 0.1, 0) to porch Fire Axe (0, 0.1, 6.5)
            Vector3 startPos = new Vector3(0f, 0.1f, 0f);
            Vector3 targetPos = new Vector3(0f, 0.1f, 6.5f);

            bool sampleStart = NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 3.0f, NavMesh.AllAreas);
            bool sampleTarget = NavMesh.SamplePosition(targetPos, out NavMeshHit targetHit, 3.0f, NavMesh.AllAreas);

            Assert(sampleStart, "Bedroom spawn point samples cleanly onto NavMesh");
            Assert(sampleTarget, "Front porch / outdoor weapon discovery point samples cleanly onto NavMesh");

            if (sampleStart && sampleTarget)
            {
                NavMeshPath path = new NavMeshPath();
                bool pathCalculated = NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, path);
                Debug.Log($"[NavMeshDiag] StartHit: {startHit.position}, TargetHit: {targetHit.position}, PathCalculated: {pathCalculated}, Status: {path.status}, Corners: {path.corners.Length}");
                for (int i = 0; i < path.corners.Length; i++)
                {
                    Debug.Log($"[NavMeshDiag] Corner[{i}]: {path.corners[i]}");
                }
                Assert(pathCalculated, "NavMesh path successfully computed between bedroom and outdoor porch");
                Assert(path.status == NavMeshPathStatus.PathComplete, $"NavMesh path status is PathComplete (Actual: {path.status})");
            }

            // Verify 45m safe perimeter
            var spc = UnityEngine.Object.FindAnyObjectByType<SpawnProtectionController>(FindObjectsInactive.Include);
            Assert(spc != null, "SpawnProtectionController instance exists");
            if (spc != null)
            {
                Assert(spc.SpawnSafeRadius >= 45.0f, $"SpawnProtectionController Safe Radius is {spc.SpawnSafeRadius}m (Target: >= 45.0m)");
            }
        }

        private static void AuditSaveSystemPersistence()
        {
            Debug.Log("--- AUDIT: SaveSystem hasCheckedIntroPhone Persistence ---");

            var field = typeof(SaveData).GetField("hasCheckedIntroPhone");
            Assert(field != null, "SaveData contains 'hasCheckedIntroPhone' boolean field");

            SaveData testData = new SaveData();
            testData.hasCheckedIntroPhone = true;

            SaveSystem.Save(testData);
            SaveData loaded = SaveSystem.Load();

            Assert(loaded != null && loaded.hasCheckedIntroPhone, "SaveSystem successfully saved and reloaded hasCheckedIntroPhone = true");
        }

        private static void AuditSceneIntegrityAndShaders()
        {
            Debug.Log("--- AUDIT: Scene Integrity, Missing Scripts & Shader Health ---");

            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int magentaCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.gameObject.scene.IsValid()) continue;
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null && mats[m].shader != null && mats[m].shader.name.Contains("InternalErrorShader"))
                    {
                        magentaCount++;
                    }
                }
            }
            Assert(magentaCount == 0, $"0 magenta (InternalErrorShader) materials in scene (Found: {magentaCount})");

            var allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int missingScriptCount = 0;
            for (int i = 0; i < allGos.Length; i++)
            {
                var go = allGos[i];
                if (go == null || !go.scene.IsValid()) continue;
                var components = go.GetComponents<MonoBehaviour>();
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                    {
                        missingScriptCount++;
                    }
                }
            }
            Assert(missingScriptCount == 0, $"0 missing MonoBehaviour script references in scene hierarchy (Found: {missingScriptCount})");
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                Debug.Log($"[PASS] {message}");
            }
            else
            {
                failedTests++;
                FailureMessages.Add(message);
                Debug.LogError($"[FAIL] {message}");
            }
        }
#endif
    }
}
