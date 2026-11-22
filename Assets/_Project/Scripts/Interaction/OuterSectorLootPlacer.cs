using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Pre-Phase 3 Optimization: Procedurally populates outer sector dead zones with thematic loot containers.
    /// - Sector_-1_0_West (West Urban Forest Park): 12 containers (Ranger supply crates, hiker backpacks, picnic coolers).
    /// - Sector_1_-1_SouthEast (Southeast Power Substation): 10 containers (Toolboxes, high-voltage lockers, maintenance van storage).
    /// </summary>
    public static class OuterSectorLootPlacer
    {
        public static void EnsureAllOuterSectorLootPlaced()
        {
            Transform parkRoot = GetOrCreateSectorLootRoot("Sector_-1_0_West", "--- WEST_PARK_LOOT ---", new Vector3(-240f, 0f, 0f));
            Transform subRoot = GetOrCreateSectorLootRoot("Sector_1_-1_SouthEast", "--- SOUTHEAST_SUBSTATION_LOOT ---", new Vector3(240f, 0f, -240f));

            // =========================================================================
            // SECTOR -1, 0 (WEST URBAN FOREST PARK) — 12 THEMATIC CONTAINERS
            // =========================================================================
            EnsureContainer(parkRoot, "WestPark_RangerCrate_Trailhead", ThematicLootContainerType.RangerSupplyCrate, new Vector3(-170f, 0.45f, -35f), 15f);
            EnsureContainer(parkRoot, "WestPark_RangerCrate_Observation", ThematicLootContainerType.RangerSupplyCrate, new Vector3(-210f, 0.45f, 45f), -30f);
            EnsureContainer(parkRoot, "WestPark_RangerCrate_Lakeside", ThematicLootContainerType.RangerSupplyCrate, new Vector3(-270f, 0.45f, 80f), 60f);
            EnsureContainer(parkRoot, "WestPark_RangerCrate_DeepForest", ThematicLootContainerType.RangerSupplyCrate, new Vector3(-310f, 0.45f, -65f), -45f);

            EnsureContainer(parkRoot, "WestPark_HikerPack_Campsite1", ThematicLootContainerType.HikerBackpack, new Vector3(-195f, 0.25f, -15f), 45f);
            EnsureContainer(parkRoot, "WestPark_HikerPack_CliffPath", ThematicLootContainerType.HikerBackpack, new Vector3(-240f, 0.25f, 60f), 0f);
            EnsureContainer(parkRoot, "WestPark_HikerPack_CaveEntrance", ThematicLootContainerType.HikerBackpack, new Vector3(-285f, 0.25f, -25f), -90f);
            EnsureContainer(parkRoot, "WestPark_HikerPack_RuinedTent", ThematicLootContainerType.HikerBackpack, new Vector3(-320f, 0.25f, 20f), 120f);

            EnsureContainer(parkRoot, "WestPark_PicnicCooler_Pavilion", ThematicLootContainerType.PicnicCooler, new Vector3(-180f, 0.35f, 25f), 0f);
            EnsureContainer(parkRoot, "WestPark_PicnicCooler_PondShore", ThematicLootContainerType.PicnicCooler, new Vector3(-225f, 0.35f, -50f), -60f);
            EnsureContainer(parkRoot, "WestPark_PicnicCooler_BenchArea", ThematicLootContainerType.PicnicCooler, new Vector3(-260f, 0.35f, 10f), 30f);
            EnsureContainer(parkRoot, "WestPark_PicnicCooler_RestStop", ThematicLootContainerType.PicnicCooler, new Vector3(-300f, 0.35f, -80f), 90f);

            // =========================================================================
            // SECTOR 1, -1 (SOUTHEAST POWER SUBSTATION) — 10 THEMATIC CONTAINERS
            // =========================================================================
            EnsureContainer(subRoot, "Substation_Toolbox_TransformerBay1", ThematicLootContainerType.ElectricianToolbox, new Vector3(165f, 0.35f, -170f), 0f);
            EnsureContainer(subRoot, "Substation_Toolbox_ControlCenter", ThematicLootContainerType.ElectricianToolbox, new Vector3(210f, 0.35f, -215f), 90f);
            EnsureContainer(subRoot, "Substation_Toolbox_GeneratorPad", ThematicLootContainerType.ElectricianToolbox, new Vector3(260f, 0.35f, -270f), -45f);
            EnsureContainer(subRoot, "Substation_Toolbox_SwitchyardNorth", ThematicLootContainerType.ElectricianToolbox, new Vector3(310f, 0.35f, -195f), 180f);

            EnsureContainer(subRoot, "Substation_HighVoltage_LockerA", ThematicLootContainerType.HighVoltageLocker, new Vector3(185f, 0.95f, -240f), 90f);
            EnsureContainer(subRoot, "Substation_HighVoltage_LockerB", ThematicLootContainerType.HighVoltageLocker, new Vector3(235f, 0.95f, -180f), 0f);
            EnsureContainer(subRoot, "Substation_HighVoltage_LockerC", ThematicLootContainerType.HighVoltageLocker, new Vector3(280f, 0.95f, -230f), -90f);

            EnsureContainer(subRoot, "Substation_MaintenanceVan_Storage1", ThematicLootContainerType.MaintenanceVanStorage, new Vector3(195f, 0.75f, -290f), 45f);
            EnsureContainer(subRoot, "Substation_MaintenanceVan_Storage2", ThematicLootContainerType.MaintenanceVanStorage, new Vector3(255f, 0.75f, -320f), -30f);
            EnsureContainer(subRoot, "Substation_MaintenanceVan_Storage3", ThematicLootContainerType.MaintenanceVanStorage, new Vector3(305f, 0.75f, -280f), 15f);

            Debug.Log("[OuterSectorLootPlacer] All 22 thematic loot containers successfully confirmed/placed across outer sectors.");
        }

        private static Transform GetOrCreateSectorLootRoot(string sectorName, string rootName, Vector3 fallbackPos)
        {
            GameObject sectorObj = GameObject.Find(sectorName);
            Transform parent = sectorObj != null ? sectorObj.transform : null;

            if (parent != null)
            {
                Transform existing = parent.Find(rootName);
                if (existing != null) return existing;

                GameObject newRoot = new GameObject(rootName);
                newRoot.transform.SetParent(parent, false);
                return newRoot.transform;
            }
            else
            {
                GameObject existing = GameObject.Find(rootName);
                if (existing != null) return existing.transform;

                GameObject newRoot = new GameObject(rootName);
                newRoot.transform.position = fallbackPos;
                return newRoot.transform;
            }
        }

        private static GameObject EnsureContainer(Transform parent, string containerName, ThematicLootContainerType type, Vector3 worldPos, float rotY)
        {
            Transform existing = parent.Find(containerName) ?? GameObject.Find(containerName)?.transform;
            if (existing != null) return existing.gameObject;

            GameObject container = new GameObject(containerName);
            container.transform.SetParent(parent, true);
            container.transform.position = worldPos;
            container.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // Visual body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(container.transform, false);

            Vector3 size = GetContainerDimensions(type);
            body.transform.localScale = size;
            body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);

            // Remove primitive collider from child body; container root holds the interaction collider
            Collider bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);

            // Material styling
            Renderer bodyRend = body.GetComponent<Renderer>();
            if (bodyRend != null)
            {
                Material mat = GetThematicMaterial(type);
                if (mat != null) bodyRend.sharedMaterial = mat;
            }

            // Visual lid / hinge
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            lid.transform.SetParent(container.transform, false);
            lid.transform.localScale = new Vector3(size.x * 1.05f, size.y * 0.18f, size.z * 1.05f);
            lid.transform.localPosition = new Vector3(0f, size.y, 0f);

            Collider lidCol = lid.GetComponent<Collider>();
            if (lidCol != null) Object.DestroyImmediate(lidCol);

            Renderer lidRend = lid.GetComponent<Renderer>();
            if (lidRend != null)
            {
                Material mat = GetThematicMaterial(type);
                if (mat != null) lidRend.sharedMaterial = mat;
            }

            // Root Interaction Box Collider
            BoxCollider rootCol = container.AddComponent<BoxCollider>();
            rootCol.size = new Vector3(size.x * 1.25f, size.y * 1.25f, size.z * 1.25f);
            rootCol.center = new Vector3(0f, size.y * 0.5f, 0f);

            // Interactable Script
            ThematicLootContainerInteractable interactable = container.AddComponent<ThematicLootContainerInteractable>();
            interactable.SetContainerType(type);

            return container;
        }

        private static Vector3 GetContainerDimensions(ThematicLootContainerType type)
        {
            switch (type)
            {
                case ThematicLootContainerType.RangerSupplyCrate:
                    return new Vector3(1.1f, 0.75f, 0.85f);
                case ThematicLootContainerType.HikerBackpack:
                    return new Vector3(0.55f, 0.65f, 0.45f);
                case ThematicLootContainerType.PicnicCooler:
                    return new Vector3(0.65f, 0.45f, 0.45f);
                case ThematicLootContainerType.ElectricianToolbox:
                    return new Vector3(0.7f, 0.4f, 0.45f);
                case ThematicLootContainerType.HighVoltageLocker:
                    return new Vector3(0.95f, 1.85f, 0.65f);
                case ThematicLootContainerType.MaintenanceVanStorage:
                    return new Vector3(1.2f, 0.85f, 0.9f);
                default:
                    return new Vector3(0.8f, 0.6f, 0.6f);
            }
        }

        private static Material GetThematicMaterial(ThematicLootContainerType type)
        {
            string matName = "Mat_Iron_Black";
            switch (type)
            {
                case ThematicLootContainerType.RangerSupplyCrate:
                    matName = "AAA_PBR/MAT_Wood_WeatheredPlanks";
                    break;
                case ThematicLootContainerType.HikerBackpack:
                    matName = "Mat_Cot_Canvas";
                    break;
                case ThematicLootContainerType.PicnicCooler:
                    matName = "Mat_Signal_Yellow";
                    break;
                case ThematicLootContainerType.ElectricianToolbox:
                    matName = "Mat_Signal_Red";
                    break;
                case ThematicLootContainerType.HighVoltageLocker:
                    matName = "AAA_PBR/MAT_Metal_GalvanizedSteel";
                    break;
                case ThematicLootContainerType.MaintenanceVanStorage:
                    matName = "Mat_Armor_ScrapMetal";
                    break;
            }

            Material mat = Resources.Load<Material>(matName);
            if (mat == null) mat = Resources.Load<Material>("AAA_PBR/MAT_Metal_GalvanizedSteel");
            return mat;
        }
    }
}
