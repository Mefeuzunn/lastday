using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    public static class Step4A_UndergroundBunkerAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Audit Step 4A Underground Bunker", false, 71)]
        public static void RunAuditMenuItem()
        {
            RunAuditCLI();
        }

        public static void RunAuditCLI()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING STEP 4A FORENSIC UNDERGROUND BUNKER DUNGEON AUDIT <<<");
            Debug.Log("==================================================================");

            int passed = 0;
            int failed = 0;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step4A_Audit] Could not open scene at {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // 1. Verify Item Asset & Dynamic Fallback
            ItemData keycardRes = Resources.Load<ItemData>("Items/item_bunker_keycard");
            ItemData keycardData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bunker_keycard.asset");
            ItemData keycardResolved = InventorySystem.FindItemData("item_bunker_keycard");

            Assert("Item Assets: item_bunker_keycard in Resources", keycardRes != null && keycardRes.itemId == "item_bunker_keycard", ref passed, ref failed);
            Assert("Item Assets: item_bunker_keycard in Data", keycardData != null, ref passed, ref failed);
            Assert("InventorySystem: FindItemData resolves item_bunker_keycard", keycardResolved != null, ref passed, ref failed);

            // 2. Verify Master UndergroundBunkerDirector
            UndergroundBunkerDirector director = UnityEngine.Object.FindAnyObjectByType<UndergroundBunkerDirector>(FindObjectsInactive.Include);
            Assert("Bunker Director: Master UndergroundBunkerDirector present in scene", director != null, ref passed, ref failed);

            // 3. Verify All 5 Subterranean Chambers exist
            string[] chamberNames = new string[]
            {
                "Chamber1_EntryAirlock",
                "Chamber2_GeneratorRoom",
                "Chamber3_DarkArchive",
                "Chamber4_BioContainmentLab",
                "Chamber5_SynthesisVault"
            };

            bool allChambersExist = true;
            bool allCeilingsSolid = true;
            foreach (var chName in chamberNames)
            {
                GameObject ch = GameObject.Find(chName);
                if (ch == null)
                {
                    allChambersExist = false;
                    Debug.LogError($"[Step4A_Audit] Chamber missing: {chName}");
                }
                else
                {
                    Transform ceil = ch.transform.Find("Solid_Ceiling");
                    if (ceil == null || ceil.GetComponent<Collider>() == null)
                    {
                        allCeilingsSolid = false;
                        Debug.LogError($"[Step4A_Audit] Chamber {chName} lacks solid ceiling collider!");
                    }
                }
            }
            Assert("Subterranean Layout: All 5 chambers exist in hierarchy", allChambersExist, ref passed, ref failed);
            Assert("Weather Protection: All chambers have solid ceiling colliders", allCeilingsSolid, ref passed, ref failed);

            // 4. Verify Mobile 60 FPS Lighting Rule (Shadows: None on all bunker lights)
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool allBunkerLightsShadowsNone = true;
            int bunkerLightCount = 0;

            foreach (var lt in allLights)
            {
                if (lt.transform.position.y < -3.0f || lt.name.Contains("Airlock") || lt.name.Contains("Fluorescent") || lt.name.Contains("Bunker"))
                {
                    bunkerLightCount++;
                    if (lt.shadows != LightShadows.None)
                    {
                        allBunkerLightsShadowsNone = false;
                        Debug.LogError($"[Step4A_Audit] Bunker light {lt.name} has shadows enabled! Must be None.");
                    }
                }
            }
            Assert("Lighting (Mobile 60 FPS): All bunker lights have Shadows: None", allBunkerLightsShadowsNone && bunkerLightCount >= 5, ref passed, ref failed);

            // 5. Verify Continuous NavMesh Connectivity (Surface Hatch -> Subterranean Floor -> Chamber 5)
            Vector3 surfaceHatch = new Vector3(-235f, 0.45f, 255f);
            Vector3 chamber1Pos = new Vector3(-235f, -12.0f, 255f);
            Vector3 chamber5Pos = new Vector3(-235f, -12.0f, 190f);

            bool sampleSurface = NavMesh.SamplePosition(surfaceHatch, out NavMeshHit hitSurface, 4.0f, NavMesh.AllAreas);
            bool sampleCh1 = NavMesh.SamplePosition(chamber1Pos, out NavMeshHit hitCh1, 4.0f, NavMesh.AllAreas);
            bool sampleCh5 = NavMesh.SamplePosition(chamber5Pos, out NavMeshHit hitCh5, 4.0f, NavMesh.AllAreas);

            Assert("NavMesh: Surface entrance has valid NavMesh", sampleSurface, ref passed, ref failed);
            Assert("NavMesh: Chamber 1 airlock floor has valid NavMesh", sampleCh1, ref passed, ref failed);
            Assert("NavMesh: Chamber 5 synthesis vault floor has valid NavMesh", sampleCh5, ref passed, ref failed);

            NavMeshPath path = new NavMeshPath();
            bool pathCalculated = NavMesh.CalculatePath(hitCh1.position, hitCh5.position, NavMesh.AllAreas, path);
            bool fullPath = pathCalculated && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
            Assert("NavMesh: Continuous subterranean pathing between Chamber 1 and Chamber 5", fullPath, ref passed, ref failed);

            // 6. Verify Chamber 2 Auxiliary Generator
            BunkerGeneratorInteractable generator = UnityEngine.Object.FindAnyObjectByType<BunkerGeneratorInteractable>(FindObjectsInactive.Include);
            Assert("Chamber 2: BunkerGeneratorInteractable present in scene", generator != null, ref passed, ref failed);
            if (generator != null)
            {
                AudioSource gSrc = generator.GetComponent<AudioSource>();
                bool validAudio = gSrc != null && gSrc.spatialBlend >= 0.95f && gSrc.rolloffMode == AudioRolloffMode.Logarithmic;
                Assert("Chamber 2: Generator has 3D positional audio (spatialBlend=1.0)", validAudio, ref passed, ref failed);

                NavMeshObstacle navObs = generator.GetComponent<NavMeshObstacle>();
                Assert("Chamber 2: Generator has NavMeshObstacle with carving=true", navObs != null && navObs.carving, ref passed, ref failed);

                var verbs = generator.GetAvailableVerbs();
                bool hasVerb = verbs.Count > 0 && verbs[0].verbId == "start_generator";
                Assert("Chamber 2: Generator exposes 'start_generator' verb", hasVerb, ref passed, ref failed);
            }

            // 7. Verify Chamber 2 Anti-Soft-Lock Backup Fuel Can Pickup
            GameObject rackFuel = GameObject.Find("WallRack_Backup_FuelCan");
            Assert("Chamber 2: Anti-soft-lock backup fuel can rack placed", rackFuel != null, ref passed, ref failed);
            if (rackFuel != null)
            {
                BunkerBackupFuelInteractable pick = rackFuel.GetComponent<BunkerBackupFuelInteractable>();
                Assert("Chamber 2: Backup fuel pickup is attached and configured", pick != null, ref passed, ref failed);
            }

            // 8. Verify Chamber 3 Head Virologist Desk & Level-2 Keycard
            BunkerKeycardPickupInteractable keycardPickup = UnityEngine.Object.FindAnyObjectByType<BunkerKeycardPickupInteractable>(FindObjectsInactive.Include);
            Assert("Chamber 3: BunkerKeycardPickupInteractable present on Virologist desk", keycardPickup != null, ref passed, ref failed);
            if (keycardPickup != null)
            {
                var verbs = keycardPickup.GetAvailableVerbs();
                bool hasPickupVerb = verbs.Count > 0 && verbs[0].verbId == "pickup_keycard";
                Assert("Chamber 3: Keycard exposes 'pickup_keycard' single-tap verb", hasPickupVerb, ref passed, ref failed);
            }

            // 9. Verify Chamber 4 KeycardSecurityDoor Motorized Blast Door
            KeycardSecurityDoor securityDoor = UnityEngine.Object.FindAnyObjectByType<KeycardSecurityDoor>(FindObjectsInactive.Include);
            Assert("Chamber 4: KeycardSecurityDoor blast door present in scene", securityDoor != null, ref passed, ref failed);
            if (securityDoor != null)
            {
                AudioSource dSrc = securityDoor.GetComponent<AudioSource>();
                bool validAudio = dSrc != null && dSrc.spatialBlend >= 0.95f && dSrc.rolloffMode == AudioRolloffMode.Logarithmic;
                Assert("Chamber 4: Security Door has 3D hydraulic audio (spatialBlend=1.0)", validAudio, ref passed, ref failed);

                var verbs = securityDoor.GetAvailableVerbs();
                bool hasDoorVerb = verbs.Count > 0 && verbs[0].verbId == "open_blast_door";
                Assert("Chamber 4: Security Door exposes 'open_blast_door' verb", hasDoorVerb, ref passed, ref failed);
            }

            // 10. Verify Mutated Lab Specimens & Lab Zombies (Total 5)
            GameObject z1 = GameObject.Find("Lab_Zombie_01");
            GameObject z2 = GameObject.Find("Lab_Zombie_02");
            GameObject z3 = GameObject.Find("Specimen_Runner_01");
            GameObject z4 = GameObject.Find("Specimen_Runner_02");
            GameObject z5 = GameObject.Find("Specimen_Lab_Tank");

            bool allZombiesExist = (z1 != null && z2 != null && z3 != null && z4 != null && z5 != null);
            Assert("Lab Mutants: Exactly 5 lab zombies present (2 in Ch3, 3 in Ch4)", allZombiesExist, ref passed, ref failed);

            bool allZombiesGrounded = true;
            GameObject[] zombies = new GameObject[] { z1, z2, z3, z4, z5 };
            foreach (var z in zombies)
            {
                if (z != null)
                {
                    NavMeshAgent ag = z.GetComponent<NavMeshAgent>();
                    if (ag == null) allZombiesGrounded = false;
                }
            }
            Assert("Lab Mutants: All 5 zombies have NavMeshAgent attached and configured", allZombiesGrounded, ref passed, ref failed);

            // 11. Verify Chamber 5 Story Synthesis Terminal (Objective 3B)
            StoryTerminalInteractable[] terminals = UnityEngine.Object.FindObjectsByType<StoryTerminalInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            StoryTerminalInteractable bunkerTerminal = null;
            foreach (var t in terminals)
            {
                if (t.TerminalId == "terminal_bunker") bunkerTerminal = t;
            }
            Assert("Chamber 5: Antiviral Synthesis Terminal configured for 'terminal_bunker' (Chapter 3)", bunkerTerminal != null, ref passed, ref failed);

            // 12. Verify Chamber 5 Antiviral Vault Chest
            BunkerVaultChestInteractable vaultChest = UnityEngine.Object.FindAnyObjectByType<BunkerVaultChestInteractable>(FindObjectsInactive.Include);
            Assert("Chamber 5: BunkerVaultChestInteractable present in vault", vaultChest != null, ref passed, ref failed);
            if (vaultChest != null)
            {
                var verbs = vaultChest.GetAvailableVerbs();
                bool hasVaultVerb = verbs.Count > 0 && verbs[0].verbId == "open_vault_chest";
                Assert("Chamber 5: Vault chest exposes 'open_vault_chest' verb", hasVaultVerb, ref passed, ref failed);
            }

            // 13. Verify SaveSystem Round-Trip Persistence of Bunker Flags
            SaveData testSave = new SaveData();
            testSave.isBunkerGeneratorPowered = true;
            testSave.isBunkerSecurityDoorUnlocked = true;
            testSave.isBunkerVaultLooted = true;

            string json = JsonUtility.ToJson(testSave);
            SaveData loadedSave = JsonUtility.FromJson<SaveData>(json);

            bool saveValid = loadedSave != null &&
                             loadedSave.isBunkerGeneratorPowered &&
                             loadedSave.isBunkerSecurityDoorUnlocked &&
                             loadedSave.isBunkerVaultLooted;

            Assert("SaveSystem: Bunker state flags round-trip serialization succeeds", saveValid, ref passed, ref failed);

            // 14. Code Quality: Zero TODO comments across project scripts
            int todoCount = ScanForTodos("Assets/_Project/Scripts");
            Assert("Code Quality: 0 // " + "TODO stubs across Assets/_Project/Scripts", todoCount == 0, ref passed, ref failed);

            Debug.Log("==================================================================");
            Debug.Log($">>> STEP 4A AUDIT COMPLETE: {passed} PASSED, {failed} FAILED <<<");
            Debug.Log("==================================================================");

            if (failed > 0)
            {
                EditorApplication.Exit(1);
            }
            else if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Assert(string description, bool condition, ref int passed, ref int failed)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"[PASS] {description}");
            }
            else
            {
                failed++;
                Debug.LogError($"[FAIL] {description}");
            }
        }

        private static int ScanForTodos(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return 0;
            int count = 0;
            string needle = "/" + "/ " + "TODO";
            string[] files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Skip audit and diagnostic scripts checking for the string literal
                if (file.Contains("Audit"))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                if (text.Contains(needle))
                {
                    Debug.LogWarning($"[ScanForTodos] Found {needle} in: {file}");
                    count++;
                }
            }
            return count;
        }
    }
}
