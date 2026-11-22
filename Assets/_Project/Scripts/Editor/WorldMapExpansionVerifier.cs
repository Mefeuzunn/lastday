using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class WorldMapExpansionVerifier
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string BakedMapPath = "Assets/_Project/UI/Map/BakedCityMap.png";

        [MenuItem("ProjectSurvival/Verify Map Expansion (720x720m)", false, 15)]
        public static void VerifyExpansionMenuItem()
        {
            bool passed = RunVerification();
            if (passed)
            {
                Debug.Log("<color=green><b>[WorldMapExpansionVerifier] ALL TESTS PASSED SUCCESSFULLY!</b></color>");
            }
            else
            {
                Debug.LogError("[WorldMapExpansionVerifier] VERIFICATION FAILED. Review errors above.");
            }
        }

        public static void VerifyExpansion()
        {
            Debug.Log("[WorldMapExpansionVerifier] Batchmode Execution Started...");
            bool passed = RunVerification();
            if (!passed)
            {
                Debug.LogError("[WorldMapExpansionVerifier] Batchmode verification FAILED.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[WorldMapExpansionVerifier] Batchmode verification SUCCESSFUL.");
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
            Debug.Log(">>> RUNNING WORLD MAP EXPANSION (720x720m) VERIFICATION SUITE <<<");
            Debug.Log("==================================================================");

            // Test Group 1: WorldMapConfig Registry & Coordinate Math
            Debug.Log("--- GROUP 1: WorldMapConfig Registry & UV Math ---");
            Assert(Mathf.Approximately(WorldMapConfig.TotalWorldSize, 720f), "TotalWorldSize is 720m", $"Got {WorldMapConfig.TotalWorldSize}");
            Assert(Mathf.Approximately(WorldMapConfig.WorldMin, -360f), "WorldMin is -360m", $"Got {WorldMapConfig.WorldMin}");
            Assert(Mathf.Approximately(WorldMapConfig.WorldMax, 360f), "WorldMax is 360m", $"Got {WorldMapConfig.WorldMax}");
            Assert(Mathf.Approximately(WorldMapConfig.SectorSize, 240f), "SectorSize is 240m", $"Got {WorldMapConfig.SectorSize}");

            var sectors = WorldMapConfig.GetAllSectors();
            Assert(sectors.Count == 9, "9 Sectors Registered", $"Got {sectors.Count}");

            Vector2 uvCenter = WorldMapConfig.WorldToNormalizedUV(Vector3.zero);
            Assert(Mathf.Approximately(uvCenter.x, 0.5f) && Mathf.Approximately(uvCenter.y, 0.5f),
                "Center (0,0) maps to UV (0.5, 0.5)", $"Got {uvCenter}");

            Vector2 uvMin = WorldMapConfig.WorldToNormalizedUV(new Vector3(-360f, 0f, -360f));
            Assert(Mathf.Approximately(uvMin.x, 0f) && Mathf.Approximately(uvMin.y, 0f),
                "Bottom-Left (-360, -360) maps to UV (0, 0)", $"Got {uvMin}");

            Vector2 uvMax = WorldMapConfig.WorldToNormalizedUV(new Vector3(360f, 0f, 360f));
            Assert(Mathf.Approximately(uvMax.x, 1f) && Mathf.Approximately(uvMax.y, 1f),
                "Top-Right (360, 360) maps to UV (1, 1)", $"Got {uvMax}");

            Vector3 worldFromUV = WorldMapConfig.NormalizedUVToWorld(new Vector2(0.5f, 0.5f));
            Assert(Mathf.Approximately(worldFromUV.x, 0f) && Mathf.Approximately(worldFromUV.z, 0f),
                "UV (0.5, 0.5) converts back to World (0, 0)", $"Got {worldFromUV}");

            // Test Group 2: Scene Hierarchy & Sector Structure
            Debug.Log("--- GROUP 2: Scene Hierarchy & Sector Structure ---");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject worldSectorsRoot = GameObject.Find("--- WORLD SECTORS ---");
            Assert(worldSectorsRoot != null, "Master '--- WORLD SECTORS ---' exists in Scene");

            if (worldSectorsRoot != null)
            {
                for (int i = 0; i < sectors.Count; i++)
                {
                    var s = sectors[i];
                    Transform sTrans = worldSectorsRoot.transform.Find(s.sectorId);
                    Assert(sTrans != null, $"Sector Object '{s.sectorId}' exists", $"Missing {s.sectorId}");
                }

                GameObject centerRoot = worldSectorsRoot.transform.Find("Sector_0_0_Center")?.gameObject;
                Assert(centerRoot != null, "Sector_0_0_Center exists");
                if (centerRoot != null)
                {
                    Transform env = centerRoot.transform.Find("Environment");
                    Assert(env != null, "Existing Environment safely housed in Sector_0_0_Center");
                }
            }

            // Test Group 3: 4 Perimeter Checkpoints & Road Passages
            Debug.Log("--- GROUP 3: 4 Perimeter Checkpoints & Arteries ---");
            GameObject northGate = GameObject.Find("Checkpoint_NorthHighwayGate");
            Assert(northGate != null, "North Highway Checkpoint exists");

            GameObject eastCrossing = GameObject.Find("Checkpoint_EastRailCrossing");
            Assert(eastCrossing != null, "East Rail Crossing Checkpoint exists");

            GameObject westParkGate = GameObject.Find("Checkpoint_WestParkGate");
            Assert(westParkGate != null, "West Park Archway Checkpoint exists");

            GameObject southPlazaGate = GameObject.Find("Checkpoint_SouthPlazaBoulevard");
            Assert(southPlazaGate != null, "South Plaza Boulevard Checkpoint exists");

            // Test Group 4: WorldSectorStreamer Configuration
            Debug.Log("--- GROUP 4: WorldSectorStreamer & AI Hibernation ---");
            WorldSectorStreamer streamer = UnityEngine.Object.FindAnyObjectByType<WorldSectorStreamer>(FindObjectsInactive.Include);
            Assert(streamer != null, "WorldSectorStreamer is present in Scene");
            if (streamer != null)
            {
                streamer.AutoDiscoverSectorRootsIfMissing();
                Assert(streamer.SectorBindings.Count == 9, "Streamer bound to all 9 sectors", $"Got {streamer.SectorBindings.Count}");
            }

            // Test Group 5: Cartography Texture & Importer
            Debug.Log("--- GROUP 5: Cartography Texture Resolution ---");
            Assert(File.Exists(BakedMapPath), "BakedCityMap.png file exists on disk");
            TextureImporter importer = AssetImporter.GetAtPath(BakedMapPath) as TextureImporter;
            Assert(importer != null, "TextureImporter found for BakedCityMap");
            if (importer != null)
            {
                Assert(importer.maxTextureSize >= 2048, "Map maxTextureSize is 2048+", $"Got {importer.maxTextureSize}");
            }

            // Test Group 6: FogOfWarManager & Tactical Map UI
            Debug.Log("--- GROUP 6: FogOfWar & UI World Scale ---");
            FogOfWarManager fogMgr = UnityEngine.Object.FindAnyObjectByType<FogOfWarManager>(FindObjectsInactive.Include);
            Assert(fogMgr != null, "FogOfWarManager found in Scene");
            if (fogMgr != null)
            {
                Assert(FogOfWarManager.WorldSize == 720f, "FogOfWarManager WorldSize is 720m", $"Got {FogOfWarManager.WorldSize}");
                Assert(FogOfWarManager.FogResolution == 512, "FogOfWarManager FogResolution is 512x512", $"Got {FogOfWarManager.FogResolution}");
            }

            FullTacticalMapUI fullMap = UnityEngine.Object.FindAnyObjectByType<FullTacticalMapUI>(FindObjectsInactive.Include);
            Assert(fullMap != null, "FullTacticalMapUI found in Scene Canvas");

            MinimapHUDWidget minimap = UnityEngine.Object.FindAnyObjectByType<MinimapHUDWidget>(FindObjectsInactive.Include);
            Assert(minimap != null, "MinimapHUDWidget found in Scene Canvas");

            CompassBarUI compass = UnityEngine.Object.FindAnyObjectByType<CompassBarUI>(FindObjectsInactive.Include);
            Assert(compass != null, "CompassBarUI found in Scene Canvas");

            // Test Group 7: NavMesh Connectivity in Outer Sectors
            Debug.Log("--- GROUP 7: NavMesh Sampling in Expanded Outer Sectors ---");
            Vector3[] samplePoints = new Vector3[]
            {
                new Vector3(0f, 0.1f, 0f),     // Center Core
                new Vector3(0f, 0.1f, 180f),   // North Grand Boulevard
                new Vector3(180f, 0.1f, 0f),   // East Commercial Avenue
                new Vector3(-180f, 0.1f, 0f),  // West Commercial Avenue
                new Vector3(0f, 0.1f, -180f)   // South Grand Boulevard
            };

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 pt = samplePoints[i];
                bool hasNavMesh = NavMesh.SamplePosition(pt, out NavMeshHit hit, 5.0f, NavMesh.AllAreas);
                Assert(hasNavMesh, $"NavMesh surface reachable at ({pt.x}, {pt.z})", hasNavMesh ? $"Distance {hit.distance:F2}m" : "No NavMesh found within 5m");
            }

            Debug.Log("==================================================================");
            Debug.Log($"[WorldMapExpansionVerifier] SUMMARY: {passedTests}/{totalTests} tests passed.");
            Debug.Log("==================================================================");

            return passedTests == totalTests;
        }
    }
}
