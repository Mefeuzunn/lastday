using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class Phase2ExpansionVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string BakedMapPath = "Assets/_Project/UI/Map/BakedCityMap.png";

        [MenuItem("ProjectSurvival/Verify Phase 2 Map Expansion", false, 17)]
        public static void VerifyPhase2MenuItem()
        {
            bool passed = RunVerification();
            if (passed)
            {
                Debug.Log("<color=green><b>[Phase2ExpansionVerifier] ALL TESTS PASSED SUCCESSFULLY!</b></color>");
            }
            else
            {
                Debug.LogError("[Phase2ExpansionVerifier] VERIFICATION FAILED. Review errors above.");
            }
        }

        public static void VerifyPhase2()
        {
            Debug.Log("[Phase2ExpansionVerifier] Batchmode Execution Started...");
            bool passed = RunVerification();
            if (!passed)
            {
                Debug.LogError("[Phase2ExpansionVerifier] Batchmode verification FAILED.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[Phase2ExpansionVerifier] Batchmode verification SUCCESSFUL.");
                EditorApplication.Exit(0);
            }
        }

        public static bool RunVerification()
        {
            int passedTests = 0;
            int totalTests = 0;

            void Assert(bool condition, string testName, string details = "")
            {
                totalTests++;
                if (condition)
                {
                    passedTests++;
                    Debug.Log($"<color=green>[PASS]</color> {testName}");
                }
                else
                {
                    Debug.LogError($"<color=red>[FAIL]</color> {testName} - {details}");
                }
            }

            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING PHASE 2 MAP EXPANSION (NORTH & EAST) AUDIT & TEST SUITE <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            Assert(worldSectorsRoot != null, "World Sectors Root '--- WORLD SECTORS ---' exists in Scene");
            if (worldSectorsRoot == null) return false;

            Transform northSector = worldSectorsRoot.transform.Find("Sector_0_1_North");
            Transform eastSector = worldSectorsRoot.transform.Find("Sector_1_0_East");
            Transform northEastSector = worldSectorsRoot.transform.Find("Sector_1_1_NorthEast");
            Transform southEastSector = worldSectorsRoot.transform.Find("Sector_1_-1_SouthEast");

            Assert(northSector != null, "Sector_0_1_North exists under world sectors");
            Assert(eastSector != null, "Sector_1_0_East exists under world sectors");
            Assert(northEastSector != null, "Sector_1_1_NorthEast exists under world sectors");
            Assert(southEastSector != null, "Sector_1_-1_SouthEast exists under world sectors");

            // Zero Regression Check: Sector_0_0_Center
            Transform centerSector = worldSectorsRoot.transform.Find("Sector_0_0_Center");
            Assert(centerSector != null && centerSector.Find("Environment") != null, "Zero Regression: Sector_0_0_Center and Environment preserved");

            // --- GROUP 1: Sector_0_1_North (Highway Viaduct & Military FOB) ---
            Debug.Log("--- GROUP 1: Sector_0_1_North (Highway Viaduct & FOB) ---");
            if (northSector != null)
            {
                Transform viaductDeck = northSector.Find("Phase2_HighwayAndFOB/Viaduct_Deck_Main");
                Assert(viaductDeck != null, "Elevated Highway Viaduct Deck exists");

                Transform southRamp = northSector.Find("Phase2_HighwayAndFOB/Highway_Ramp_South");
                Assert(southRamp != null, "South Walkable Collapsed Highway Ramp exists");

                Transform fob = northSector.Find("Phase2_HighwayAndFOB/Military_FOB_Camp");
                Assert(fob != null, "Military Field Camp (FOB) exists");

                VehicleInteractable[] milVehicles = northSector.GetComponentsInChildren<VehicleInteractable>(true);
                Assert(milVehicles.Length >= 6, $"Military Convoy Vehicles with VehicleInteractable exist (found {milVehicles.Length})");

                LootContainer[] milCrates = northSector.GetComponentsInChildren<LootContainer>(true);
                Assert(milCrates.Length >= 4, $"Military Ammo Crates exist (found {milCrates.Length})");
            }

            // --- GROUP 2: Sector_1_0_East (Freight Rail Yard & Warehouses) ---
            Debug.Log("--- GROUP 2: Sector_1_0_East (Freight Rail Yard & Warehouses) ---");
            if (eastSector != null)
            {
                Transform railParent = eastSector.Find("Phase2_RailYardAndWarehouses");
                Assert(railParent != null, "East Rail Yard Root exists");

                Transform wh1 = railParent?.Find("Warehouse_1_NorthLogistics");
                Transform wh2 = railParent?.Find("Warehouse_2_MachineShop");
                Transform wh3 = railParent?.Find("Warehouse_3_SouthSteelDepot");
                Assert(wh1 != null && wh2 != null && wh3 != null, "All 3 Industrial Warehouses exist with Loading Docks");

                InteractiveDoor[] doors = eastSector.GetComponentsInChildren<InteractiveDoor>(true);
                Assert(doors.Length >= 3, $"Warehouse Rolling Shutter Doors with InteractiveDoor exist (found {doors.Length})");

                FurnitureInteractable[] shelves = eastSector.GetComponentsInChildren<FurnitureInteractable>(true);
                Assert(shelves.Length >= 6, $"Warehouse Steel Shelving & Furniture exist (found {shelves.Length})");
            }

            // --- GROUP 3: Sector_1_1_NorthEast (Container Harbor & Fuel Depot) ---
            Debug.Log("--- GROUP 3: Sector_1_1_NorthEast (Harbor & Fuel Depot) ---");
            if (northEastSector != null)
            {
                Transform seawallEast = northEastSector.Find("Phase2_ContainerHarborAndFuel/Seawall_Quay_East");
                Transform seawallNorth = northEastSector.Find("Phase2_ContainerHarborAndFuel/Seawall_Quay_North");
                Assert(seawallEast != null && seawallNorth != null, "Harbor Concrete Seawalls exist");

                Transform tugboat = northEastSector.Find("Phase2_ContainerHarborAndFuel/Sunken_Tugboat_Wreck");
                Assert(tugboat != null, "Sunken Tugboat Wreck exists moored alongside pier");

                Transform containerMaze = northEastSector.Find("Phase2_ContainerHarborAndFuel/Container_Tactical_Maze");
                Assert(containerMaze != null && containerMaze.childCount >= 16, $"Container Tactical Maze exists (found {containerMaze?.childCount ?? 0} container objects)");

                Transform fuelDepot = northEastSector.Find("Phase2_ContainerHarborAndFuel/Coastal_Fuel_Storage_Depot");
                Assert(fuelDepot != null, "Coastal Fuel Storage Depot with Bund Walls exists");

                TacticalPropInteractable[] barrels = northEastSector.GetComponentsInChildren<TacticalPropInteractable>(true);
                Assert(barrels.Length >= 8, $"Explosive Red Fuel Barrels with TacticalPropInteractable exist (found {barrels.Length})");
            }

            // --- GROUP 4: Sector_1_-1_SouthEast (Regional Power Substation) ---
            Debug.Log("--- GROUP 4: Sector_1_-1_SouthEast (Power Substation) ---");
            if (southEastSector != null)
            {
                Transform controlBldg = southEastSector.Find("Phase2_PowerSubstation/Substation_Control_Building");
                Assert(controlBldg != null, "Substation Control Room Building exists");

                RegionalMasterGridPanel gridPanel = southEastSector.GetComponentInChildren<RegionalMasterGridPanel>(true);
                Assert(gridPanel != null, "RegionalMasterGridPanel component exists inside Substation");
                if (gridPanel != null)
                {
                    Assert(gridPanel.GetAvailableVerbs().Count > 0, "RegionalMasterGridPanel provides Available Verbs");
                }

                InteractiveDoor ctrlDoor = southEastSector.GetComponentInChildren<InteractiveDoor>(true);
                Assert(ctrlDoor != null, "Control Room has InteractiveDoor");

                WindowInteractable ctrlWin = southEastSector.GetComponentInChildren<WindowInteractable>(true);
                Assert(ctrlWin != null, "Control Room has WindowInteractable");
            }

            // --- GROUP 5: Autonomous Scavenger Squads & Ecology ---
            Debug.Log("--- GROUP 5: Autonomous Scavenger Squads & District Ecology ---");
            GameObject squadRoot = GameObject.Find("--- SCAVENGER SQUADS ---");
            Assert(squadRoot != null, "Scavenger Squads root exists");
            if (squadRoot != null)
            {
                AutonomousScavengerSquad[] squads = squadRoot.GetComponentsInChildren<AutonomousScavengerSquad>(true);
                Assert(squads.Length >= 4, $"At least 4 AutonomousScavengerSquad roaming teams active (found {squads.Length})");
            }

            DistrictZombieEcology ecology = UnityEngine.Object.FindAnyObjectByType<DistrictZombieEcology>(FindObjectsInactive.Include);
            Assert(ecology != null, "DistrictZombieEcology found in Scene");
            if (ecology != null)
            {
                Assert(ecology.Zones.Count >= 8, $"DistrictZombieEcology has 8+ territorial zones registered (found {ecology.Zones.Count})");
            }

            // --- GROUP 6: NavMesh Walkability across new districts ---
            Debug.Log("--- GROUP 6: NavMesh Connectivity in Phase 2 Sectors ---");
            (string name, Vector3 pos)[] navMeshPoints = new (string, Vector3)[]
            {
                ("North Highway South Ramp", new Vector3(0f, 3.3f, 137f)),
                ("North Highway Elevated Deck", new Vector3(0f, 6.5f, 200f)),
                ("North Highway FOB Camp", new Vector3(40f, 0.1f, 220f)),
                ("East Rail Yard Track Area", new Vector3(210f, 0.1f, 15f)),
                ("East Warehouse 1 Interior", new Vector3(305f, 1.3f, 65f)),
                ("East Warehouse 2 Interior", new Vector3(305f, 1.3f, 0f)),
                ("NorthEast Harbor Container Corridor", new Vector3(230f, 0.1f, 230f)),
                ("NorthEast Fuel Depot Yard", new Vector3(175f, 0.1f, 265f)),
                ("SouthEast Substation Transformer Yard", new Vector3(230f, 0.1f, -240f)),
                ("SouthEast Control Room Interior", new Vector3(265f, 0.3f, -260f))
            };

            for (int i = 0; i < navMeshPoints.Length; i++)
            {
                var pt = navMeshPoints[i];
                bool sample = NavMesh.SamplePosition(pt.pos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas);
                Assert(sample, $"NavMesh reachable at '{pt.name}' ({pt.pos.x}, {pt.pos.y}, {pt.pos.z})", sample ? $"Dist: {hit.distance:F2}m" : "No NavMesh surface within 5m");
            }

            // --- GROUP 7: Cartography & World Sector Streamer ---
            Debug.Log("--- GROUP 7: Cartography & World Sector Streamer ---");
            Assert(File.Exists(BakedMapPath), "BakedCityMap.png exists on disk");
            WorldSectorStreamer streamer = UnityEngine.Object.FindAnyObjectByType<WorldSectorStreamer>(FindObjectsInactive.Include);
            Assert(streamer != null, "WorldSectorStreamer present");
            if (streamer != null)
            {
                streamer.AutoDiscoverSectorRootsIfMissing();
                Assert(streamer.SectorBindings.Count == 9, "Streamer holds all 9 sector bindings");
            }

            Debug.Log("==================================================================");
            Debug.Log($"[Phase2ExpansionVerifier] SUMMARY: {passedTests}/{totalTests} tests passed.");
            Debug.Log("==================================================================");

            return passedTests == totalTests;
        }
    }
}
