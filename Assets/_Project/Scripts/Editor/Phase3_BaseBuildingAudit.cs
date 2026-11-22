using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Building;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.OpenWorld;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorScripts
{
    public static class Phase3_BaseBuildingAudit
    {
        [MenuItem("Tools/Project Survival/Audit Phase 3 Base Building")]
        public static void RunAudit()
        {
            Debug.Log("==========================================================================");
            Debug.Log("[Phase3_BaseBuildingAudit] STARTING MODULAR BASE BUILDING & FORTIFICATION AUDIT");
            Debug.Log("==========================================================================");

            int passed = 0;
            int failed = 0;

            // 1. Structure Definitions & Procedural Meshes
            passed += AssertCondition("StructureType Enum Count == 5", Enum.GetValues(typeof(StructureType)).Length == 5, ref failed);

            GameObject tempContainer = new GameObject("Temp_Audit_Container");
            try
            {
                for (int t = 0; t < 5; t++)
                {
                    StructureType type = (StructureType)t;
                    GameObject sGo = BuildableStructure.CreateStructureGameObject(type, new Vector3(t * 5f, 0f, 0f), Quaternion.identity, false);
                    sGo.transform.SetParent(tempContainer.transform);

                    BuildableStructure bs = sGo.GetComponent<BuildableStructure>();
                    passed += AssertCondition($"Structure {type} has BuildableStructure component", bs != null, ref failed);

                    float expectedHP = BuildableStructure.GetDefaultMaxHP(type);
                    passed += AssertCondition($"Structure {type} default HP ({bs.MaxHP}) == {expectedHP}", bs.MaxHP == expectedHP, ref failed);

                    // Check Mesh Renderers & GPU Instancing
                    Renderer[] rends = sGo.GetComponentsInChildren<Renderer>();
                    passed += AssertCondition($"Structure {type} has visual renderers (count: {rends.Length})", rends.Length > 0, ref failed);

                    bool allInstanced = true;
                    for (int r = 0; r < rends.Length; r++)
                    {
                        if (rends[r].sharedMaterial != null && !rends[r].sharedMaterial.enableInstancing)
                        {
                            allInstanced = false;
                        }
                    }
                    passed += AssertCondition($"Structure {type} all materials have enableInstancing=true", allInstanced, ref failed);

                    // Component Specifics
                    if (type == StructureType.DefenseWall)
                    {
                        NavMeshObstacle obs = sGo.GetComponent<NavMeshObstacle>();
                        passed += AssertCondition("DefenseWall has NavMeshObstacle with carving=true", obs != null && obs.carving, ref failed);
                        BoxCollider col = sGo.GetComponent<BoxCollider>();
                        passed += AssertCondition("DefenseWall has physical BoxCollider", col != null && !col.isTrigger, ref failed);
                    }
                    else if (type == StructureType.RazorWire)
                    {
                        BoxCollider col = sGo.GetComponent<BoxCollider>();
                        passed += AssertCondition("RazorWire has Trigger BoxCollider", col != null && col.isTrigger, ref failed);
                    }
                    else if (type == StructureType.Floodlight)
                    {
                        Light spot = sGo.GetComponentInChildren<Light>();
                        passed += AssertCondition("Floodlight has SpotLight component", spot != null && spot.type == LightType.Spot, ref failed);
                        if (spot != null)
                        {
                            passed += AssertCondition("Floodlight spot range >= 25m", spot.range >= 25f, ref failed);
                            passed += AssertCondition("Floodlight shadows == LightShadows.None (mobile 60 FPS compliance)", spot.shadows == LightShadows.None, ref failed);
                        }
                    }
                    else if (type == StructureType.RainCollector)
                    {
                        NavMeshObstacle obs = sGo.GetComponent<NavMeshObstacle>();
                        passed += AssertCondition("RainCollector has NavMeshObstacle with carving=true", obs != null && obs.carving, ref failed);
                    }
                    else if (type == StructureType.HerbPlanter)
                    {
                        NavMeshObstacle obs = sGo.GetComponent<NavMeshObstacle>();
                        passed += AssertCondition("HerbPlanter has NavMeshObstacle with carving=true", obs != null && obs.carving, ref failed);
                    }
                }

                // 2. Hologram Ghost Mesh & Shader
                GameObject ghostGo = BuildableStructure.CreateStructureGameObject(StructureType.DefenseWall, Vector3.zero, Quaternion.identity, true);
                ghostGo.transform.SetParent(tempContainer.transform);
                Renderer ghostRend = ghostGo.GetComponentInChildren<Renderer>();
                passed += AssertCondition("Ghost hologram has renderer", ghostRend != null, ref failed);
                if (ghostRend != null && ghostRend.sharedMaterial != null)
                {
                    passed += AssertCondition("Ghost hologram material has enableInstancing=true", ghostRend.sharedMaterial.enableInstancing, ref failed);
                }

                // 3. Resource Cost Specifications
                ResourceCost[] wallCosts = BuildableStructure.GetCosts(StructureType.DefenseWall);
                passed += AssertCondition("DefenseWall costs 4 scrap + 1 cloth", wallCosts.Length == 2 && wallCosts[0].itemId == "item_scrap" && wallCosts[0].amount == 4 && wallCosts[1].itemId == "item_cloth" && wallCosts[1].amount == 1, ref failed);

                ResourceCost[] wireCosts = BuildableStructure.GetCosts(StructureType.RazorWire);
                passed += AssertCondition("RazorWire costs 3 scrap", wireCosts.Length == 1 && wireCosts[0].itemId == "item_scrap" && wireCosts[0].amount == 3, ref failed);

                ResourceCost[] lightCosts = BuildableStructure.GetCosts(StructureType.Floodlight);
                passed += AssertCondition("Floodlight costs 3 scrap + 1 battery", lightCosts.Length == 2 && lightCosts[0].itemId == "item_scrap" && lightCosts[0].amount == 3 && lightCosts[1].itemId == "item_battery" && lightCosts[1].amount == 1, ref failed);

                ResourceCost[] rainCosts = BuildableStructure.GetCosts(StructureType.RainCollector);
                passed += AssertCondition("RainCollector costs 2 scrap + 2 cloth", rainCosts.Length == 2 && rainCosts[0].itemId == "item_scrap" && rainCosts[0].amount == 2 && rainCosts[1].itemId == "item_cloth" && rainCosts[1].amount == 2, ref failed);

                ResourceCost[] herbCosts = BuildableStructure.GetCosts(StructureType.HerbPlanter);
                passed += AssertCondition("HerbPlanter costs 2 scrap + 1 water bottle", herbCosts.Length == 2 && herbCosts[0].itemId == "item_scrap" && herbCosts[0].amount == 2 && herbCosts[1].itemId == "item_water_bottle" && herbCosts[1].amount == 1, ref failed);

                // 4. Durability & Repair
                GameObject testWall = BuildableStructure.CreateStructureGameObject(StructureType.DefenseWall, new Vector3(100f, 0f, 100f), Quaternion.identity, false);
                testWall.transform.SetParent(tempContainer.transform);
                BuildableStructure wallBs = testWall.GetComponent<BuildableStructure>();

                wallBs.TakeStructuralDamage(150f);
                passed += AssertCondition("Wall took 150 damage, currentHP == 200", Mathf.Approximately(wallBs.CurrentHP, 200f), ref failed);
                passed += AssertCondition("Wall IsDamaged == true", wallBs.IsDamaged, ref failed);

                // 5. Authorized Zone Checking
                GameObject bmMgrGo = new GameObject("BaseBuildingManager_Audit");
                bmMgrGo.transform.SetParent(tempContainer.transform);
                BaseBuildingManager bmMgr = bmMgrGo.AddComponent<BaseBuildingManager>();

                passed += AssertCondition("SafeHouse courtyard (0, 0, 0) is authorized build zone", bmMgr.IsInAuthorizedBuildZone(Vector3.zero), ref failed);
                passed += AssertCondition("SafeHouse courtyard edge (25, 0, 25) is authorized build zone", bmMgr.IsInAuthorizedBuildZone(new Vector3(25f, 0f, 25f)), ref failed);
                passed += AssertCondition("Distant unliberated wilderness (450, 0, 450) is NOT authorized build zone", !bmMgr.IsInAuthorizedBuildZone(new Vector3(450f, 0f, 450f)), ref failed);

                // 6. SaveSystem Round-Trip Serialization
                SaveData testData = new SaveData();
                SavedBaseStructure rec = wallBs.ToSaveRecord();
                testData.builtStructures.Add(rec);

                string json = JsonUtility.ToJson(testData);
                SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

                passed += AssertCondition("SaveData contains 1 builtStructure", loadedData.builtStructures != null && loadedData.builtStructures.Count == 1, ref failed);
                if (loadedData.builtStructures != null && loadedData.builtStructures.Count > 0)
                {
                    var loadedRec = loadedData.builtStructures[0];
                    passed += AssertCondition("Restored structureId matches", loadedRec.structureId == rec.structureId, ref failed);
                    passed += AssertCondition("Restored structureType matches", loadedRec.structureType == (int)StructureType.DefenseWall, ref failed);
                    passed += AssertCondition("Restored currentHP matches", Mathf.Approximately(loadedRec.currentHP, 200f), ref failed);
                }

                // 7. BaseBuildingUI & ModalWindowManager Integration
                GameObject uiGo = new GameObject("BaseBuildingUI_Audit");
                uiGo.transform.SetParent(tempContainer.transform);
                BaseBuildingUI bbUI = uiGo.AddComponent<BaseBuildingUI>();
                passed += AssertCondition("BaseBuildingUI implements IModalWindow", bbUI is IModalWindow, ref failed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempContainer);
            }

            Debug.Log("==========================================================================");
            Debug.Log($"[Phase3_BaseBuildingAudit] AUDIT COMPLETE: {passed} PASSED, {failed} FAILED.");
            Debug.Log("==========================================================================");
        }

        private static int AssertCondition(string message, bool condition, ref int failCount)
        {
            if (condition)
            {
                Debug.Log($"  [PASS] {message}");
                return 1;
            }
            else
            {
                Debug.LogError($"  [FAIL] {message}");
                failCount++;
                return 0;
            }
        }
    }
}
