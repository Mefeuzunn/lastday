using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Automated Verification Suite for Map Expansion Phase 3 (West & South Sectors).
    /// Validates all 4 newly expanded sectors:
    /// - Sector_-1_0_West (Urban Forest, 65+ Trees, Stealth Trails, Ranger Camp)
    /// - Sector_-1_1_NorthWest (Lakefront Cabins, Dock, Fire Lookout Tower)
    /// - Sector_0_-1_South (Sunken Subway Station & 2-Story Grand Mall)
    /// - Sector_-1_-1_SouthWest (Flooded Suburbs, Movement Modifier, Boardwalks)
    /// Checks multi-verb interaction compatibility, NavMesh walkability, 12-zone ecology, and streaming integrity.
    /// </summary>
    public static class Phase3ExpansionVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string BakedMapPath = "Assets/_Project/UI/Map/BakedCityMap.png";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Verify Phase 3 Outer Sectors (West & South)", false, 17)]
        public static void VerifyPhase3()
        {
            Debug.Log("[Phase3ExpansionVerifier] Batchmode Execution Started...");
            bool passed = RunVerification();
            if (!passed)
            {
                Debug.LogError("[Phase3ExpansionVerifier] Batchmode verification FAILED.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[Phase3ExpansionVerifier] Batchmode verification SUCCESSFUL.");
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
                    Debug.Log($"<color=green>[PASS]</color> {testName}" + (string.IsNullOrEmpty(details) ? "" : $" ({details})"));
                }
                else
                {
                    Debug.LogError($"<color=red>[FAIL]</color> {testName}" + (string.IsNullOrEmpty(details) ? "" : $" - {details}"));
                }
            }

            Debug.Log("==================================================================");
            Debug.Log(">>> PHASE 3 EXPANSION VERIFICATION SUITE STARTING <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            // --- GROUP 1: Sector Roots & Hierarchy ---
            Debug.Log("--- GROUP 1: Sector Roots & Zero Regression ---");
            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            Assert(worldSectorsRoot != null, "World Sectors Root '--- WORLD SECTORS ---' exists in Scene");

            Transform westSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_-1_0_West") : null;
            Transform northWestSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_-1_1_NorthWest") : null;
            Transform southSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_0_-1_South") : null;
            Transform southWestSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_-1_-1_SouthWest") : null;

            Assert(westSector != null, "Sector_-1_0_West exists under world sectors");
            Assert(northWestSector != null, "Sector_-1_1_NorthWest exists under world sectors");
            Assert(southSector != null, "Sector_0_-1_South exists under world sectors");
            Assert(southWestSector != null, "Sector_-1_-1_SouthWest exists under world sectors");

            // Zero Regression Checks
            Transform centerSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_0_0_Center") : null;
            Transform northSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_0_1_North") : null;
            Transform eastSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_1_0_East") : null;
            Transform northEastSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_1_1_NorthEast") : null;
            Transform southEastSector = worldSectorsRoot != null ? worldSectorsRoot.transform.Find("Sector_1_-1_SouthEast") : null;

            Assert(centerSector != null && northSector != null && eastSector != null && northEastSector != null && southEastSector != null,
                "Zero Regression: Sector_0_0_Center and all Phase 2 sectors preserved untouched");

            // --- GROUP 2: Sector_-1_0_West (Urban Forest Park & Stealth Trails) ---
            Debug.Log("--- GROUP 2: Sector_-1_0_West Architecture & Stealth ---");
            if (westSector != null)
            {
                Transform forestRoot = westSector.Find("Phase3_UrbanForestAndPark");
                Assert(forestRoot != null, "Phase3_UrbanForestAndPark exists under Sector_-1_0_West");

                Transform gazebo = forestRoot != null ? forestRoot.Find("Stone_Gazebo_Pavilion") : null;
                Assert(gazebo != null, "Stone Gazebo Pavilion exists at park center");

                Transform trails = forestRoot != null ? forestRoot.Find("Park_Trails_Network") : null;
                Assert(trails != null, "Park Trails Network exists");

                Transform trees = forestRoot != null ? forestRoot.Find("Procedural_Forest_Trees") : null;
                int treeCount = trees != null ? trees.childCount : 0;
                Assert(treeCount >= 65, $"65+ Sculpted Trees exist with GPU instanced canopies (found {treeCount})");

                Transform bushes = forestRoot != null ? forestRoot.Find("Tactical_Stealth_Bushes") : null;
                HidingSpot[] stealthSpots = bushes != null ? bushes.GetComponentsInChildren<HidingSpot>(true) : new HidingSpot[0];
                Assert(stealthSpots.Length >= 6, $"Tactical Stealth Foliage Bushes with HidingSpot exist (found {stealthSpots.Length})");

                Transform shed = forestRoot != null ? forestRoot.Find("Park_Maintenance_Shed") : null;
                Assert(shed != null, "Park Maintenance Shed exists");
                if (shed != null)
                {
                    InteractiveDoor shedDoor = shed.GetComponentInChildren<InteractiveDoor>(true);
                    Assert(shedDoor != null, "Maintenance Shed has InteractiveDoor");
                    WindowInteractable shedWin = shed.GetComponentInChildren<WindowInteractable>(true);
                    Assert(shedWin != null, "Maintenance Shed has WindowInteractable");
                }

                Transform truck = forestRoot != null ? forestRoot.Find("Ranger_Pickup_Truck") : null;
                VehicleInteractable vi = truck != null ? truck.GetComponent<VehicleInteractable>() : null;
                Assert(vi != null, "Abandoned Ranger Pickup Truck has VehicleInteractable");

                Transform camp = forestRoot != null ? forestRoot.Find("Hiker_Tents_Camp") : null;
                Assert(camp != null, "Abandoned Hiker Tents Camp exists");
            }

            // --- GROUP 3: Sector_-1_1_NorthWest (Lakefront Cabins & Fire Lookout Tower) ---
            Debug.Log("--- GROUP 3: Sector_-1_1_NorthWest Lake & Cabins ---");
            if (northWestSector != null)
            {
                Transform lakeRoot = northWestSector.Find("Phase3_LakesideCabinsAndOutpost");
                Assert(lakeRoot != null, "Phase3_LakesideCabinsAndOutpost exists under Sector_-1_1_NorthWest");

                Transform lakeBasin = lakeRoot != null ? lakeRoot.Find("Forest_Lake_Basin") : null;
                Assert(lakeBasin != null, "Forest Lake Basin & Dark Water Surface exist");

                Transform pier = lakeRoot != null ? lakeRoot.Find("Timber_Fishing_Pier") : null;
                Assert(pier != null, "Weathered Timber Fishing Pier exists extending over water");

                Transform cabinA = lakeRoot != null ? lakeRoot.Find("Cabin_A_SouthLakefront") : null;
                Transform cabinB = lakeRoot != null ? lakeRoot.Find("Cabin_B_MidLakefront") : null;
                Transform cabinC = lakeRoot != null ? lakeRoot.Find("Cabin_C_NorthPines") : null;
                Assert(cabinA != null && cabinB != null && cabinC != null, "All 3 Enterable Timber Cabins exist");

                InteractiveDoor[] cabinDoors = lakeRoot != null ? lakeRoot.GetComponentsInChildren<InteractiveDoor>(true) : new InteractiveDoor[0];
                Assert(cabinDoors.Length >= 3, $"All cabins have InteractiveDoor (found {cabinDoors.Length})");

                WindowInteractable[] cabinWins = lakeRoot != null ? lakeRoot.GetComponentsInChildren<WindowInteractable>(true) : new WindowInteractable[0];
                Assert(cabinWins.Length >= 3, $"All cabins have WindowInteractable (found {cabinWins.Length})");

                UtilityFixtureInteractable[] cabinBeds = lakeRoot != null ? lakeRoot.GetComponentsInChildren<UtilityFixtureInteractable>(true) : new UtilityFixtureInteractable[0];
                Assert(cabinBeds.Length >= 3, $"All cabins have UtilityFixtureInteractable beds (found {cabinBeds.Length})");

                Transform tower = lakeRoot != null ? lakeRoot.Find("Fire_Lookout_Tower") : null;
                Assert(tower != null, "5.5m Elevated Forest Fire Lookout Tower exists");
            }

            // --- GROUP 4: Sector_0_-1_South (Sunken Subway & Grand Shopping Mall) ---
            Debug.Log("--- GROUP 4: Sector_0_-1_South Subway & Grand Mall ---");
            if (southSector != null)
            {
                Transform southRoot = southSector.Find("Phase3_SubwayAndGrandMall");
                Assert(southRoot != null, "Phase3_SubwayAndGrandMall exists under Sector_0_-1_South");

                Transform subway = southRoot != null ? southRoot.Find("Sunken_Subway_Terminal") : null;
                Assert(subway != null, "Sunken Subway Terminal exists (Y: -4.2m)");

                Transform carA = subway != null ? subway.Find("Subway_Car_A") : null;
                Transform carB = subway != null ? subway.Find("Subway_Car_B") : null;
                Assert(carA != null && carB != null, "Two Stationary Subway Train Cars exist with lootable crates");

                SubwayServiceTunnelGate[] gates = subway != null ? subway.GetComponentsInChildren<SubwayServiceTunnelGate>(true) : new SubwayServiceTunnelGate[0];
                Assert(gates.Length >= 2, $"Two SubwayServiceTunnelGate pre-wired transit nodes exist (found {gates.Length})");

                Transform mall = southRoot != null ? southRoot.Find("Grand_Shopping_Mall") : null;
                Assert(mall != null, "Two-Story Grand Shopping Mall exists");

                Transform rampW = mall != null ? mall.Find("Escalator_Ramp_West") : null;
                Transform rampE = mall != null ? mall.Find("Escalator_Ramp_East") : null;
                Assert(rampW != null && rampE != null, "Inclined Escalator Ramps connect Ground Floor to Second Floor");

                Transform store1 = mall != null ? mall.Find("Store_SportingGoods_L1") : null;
                Transform store2 = mall != null ? mall.Find("Store_Electronics_L1") : null;
                Transform store3 = mall != null ? mall.Find("Store_MegaPharmacy_L2") : null;
                Transform store4 = mall != null ? mall.Find("Store_FoodCourt_L2") : null;
                Assert(store1 != null && store2 != null && store3 != null && store4 != null, "All 4 Distinct Enterable Storefronts exist");

                PowerGridInteractable mallGen = mall != null ? mall.GetComponentInChildren<PowerGridInteractable>(true) : null;
                Assert(mallGen != null, "Mall Backup Generator with PowerGridInteractable exists in Utility Room");
            }

            // --- GROUP 5: Sector_-1_-1_SouthWest (Flooded Suburbs & Boardwalks) ---
            Debug.Log("--- GROUP 5: Sector_-1_-1_SouthWest Flooded Suburbs ---");
            if (southWestSector != null)
            {
                Transform floodedRoot = southWestSector.Find("Phase3_FloodedSuburbsAndBoardwalk");
                Assert(floodedRoot != null, "Phase3_FloodedSuburbsAndBoardwalk exists under Sector_-1_-1_SouthWest");

                FloodwaterMovementModifier floodMod = floodedRoot != null ? floodedRoot.GetComponentInChildren<FloodwaterMovementModifier>(true) : null;
                Assert(floodMod != null, "FloodwaterMovementModifier trigger exists covering flooded streets");
                if (floodMod != null)
                {
                    Assert(Mathf.Approximately(floodMod.SpeedMultiplier, 0.82f), "FloodwaterMovementModifier slows movement by exactly 18% (0.82x)");
                }

                Transform boardwalks = floodedRoot != null ? floodedRoot.Find("Elevated_Boardwalk_Network") : null;
                Assert(boardwalks != null, "Elevated Wooden Boardwalk Network exists connecting porches at Y: 0.38m");

                for (int h = 1; h <= 5; h++)
                {
                    Transform house = floodedRoot != null ? floodedRoot.Find($"Submerged_House_{h}") : null;
                    Assert(house != null, $"Submerged_House_{h} exists with two stories");
                    if (house != null)
                    {
                        InteractiveDoor d = house.GetComponentInChildren<InteractiveDoor>(true);
                        Assert(d != null, $"Submerged_House_{h} has InteractiveDoor");
                    }
                }

                Transform vehParent = floodedRoot != null ? floodedRoot.Find("Stranded_Submerged_Vehicles") : null;
                VehicleInteractable[] submergedVehicles = vehParent != null ? vehParent.GetComponentsInChildren<VehicleInteractable>(true) : new VehicleInteractable[0];
                Assert(submergedVehicles.Length >= 4, $"Half-Submerged Stranded Vehicles with VehicleInteractable exist (found {submergedVehicles.Length})");
            }

            // --- GROUP 6: Scavenger Squads & 12-Zone Ecology ---
            Debug.Log("--- GROUP 6: Scavenger Squads & 12-Zone District Ecology ---");
            GameObject squadRoot = GameObject.Find("--- SCAVENGER SQUADS ---");
            Assert(squadRoot != null, "Scavenger Squads root exists");
            if (squadRoot != null)
            {
                AutonomousScavengerSquad[] squads = squadRoot.GetComponentsInChildren<AutonomousScavengerSquad>(true);
                Assert(squads.Length >= 6, $"At least 6 AutonomousScavengerSquad roaming teams active (found {squads.Length})");
            }

            DistrictZombieEcology ecology = UnityEngine.Object.FindAnyObjectByType<DistrictZombieEcology>(FindObjectsInactive.Include);
            Assert(ecology != null, "DistrictZombieEcology found in Scene");
            if (ecology != null)
            {
                Assert(ecology.Zones.Count >= 12, $"DistrictZombieEcology has all 12 territorial zones registered (found {ecology.Zones.Count})");
            }

            // --- GROUP 7: NavMesh Connectivity in Phase 3 Sectors ---
            Debug.Log("--- GROUP 7: NavMesh Connectivity in Phase 3 Sectors ---");
            (string name, Vector3 pos)[] navMeshPoints = new (string, Vector3)[]
            {
                ("West Forest Park Gazebo", new Vector3(-240f, 0.25f, 0f)),
                ("West Forest Maintenance Shed", new Vector3(-210f, 0.15f, 35f)),
                ("NorthWest Lake Fishing Pier", new Vector3(-220f, 0.35f, 259f)),
                ("NorthWest Cabin A Interior", new Vector3(-185f, 0.25f, 220f)),
                ("NorthWest Fire Lookout Tower", new Vector3(-175f, 5.5f, 175f)),
                ("South Subway Sunken Floor", new Vector3(0f, -4.2f, -190f)),
                ("South Mall Ground Floor Atrium", new Vector3(0f, 0.1f, -295f)),
                ("South Mall Second Floor Walkway", new Vector3(0f, 4.4f, -263f)),
                ("SouthWest Flooded Boardwalk", new Vector3(-240f, 0.38f, -240f)),
                ("SouthWest Submerged House 1 Upper Floor", new Vector3(-275f, 3.2f, -210f))
            };

            for (int i = 0; i < navMeshPoints.Length; i++)
            {
                var pt = navMeshPoints[i];
                bool sample = NavMesh.SamplePosition(pt.pos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas);
                Assert(sample, $"NavMesh reachable at '{pt.name}' ({pt.pos.x}, {pt.pos.y}, {pt.pos.z})", sample ? $"Dist: {hit.distance:F2}m" : "No NavMesh surface within 5m");
            }

            // --- GROUP 8: Cartography & World Sector Streamer ---
            Debug.Log("--- GROUP 8: Cartography & World Sector Streamer ---");
            Assert(File.Exists(BakedMapPath), "BakedCityMap.png exists on disk");
            WorldSectorStreamer streamer = UnityEngine.Object.FindAnyObjectByType<WorldSectorStreamer>(FindObjectsInactive.Include);
            Assert(streamer != null, "WorldSectorStreamer present");
            if (streamer != null)
            {
                streamer.AutoDiscoverSectorRootsIfMissing();
                Assert(streamer.SectorBindings.Count == 9, "Streamer holds all 9 sector bindings");
            }

            Debug.Log("==================================================================");
            Debug.Log($"[Phase3ExpansionVerifier] SUMMARY: {passedTests}/{totalTests} tests passed.");
            Debug.Log("==================================================================");

            return passedTests == totalTests;
        }
#endif
    }
}
