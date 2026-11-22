using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 2 Micro-Step 2B: World Station Placer & Manager.
    /// Ensures that all 7 Camp Stoves and 4 Chemistry Labs are placed, grounded, and wired
    /// with proper interaction colliders across Main_Prototype.unity.
    /// </summary>
    public class SurvivalStationPlacer : MonoBehaviour
    {
        public static SurvivalStationPlacer Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnsureAllStationsPlaced();
        }

        public static void EnsureAllStationsPlaced()
        {
            Transform root = GetOrCreateStationRoot();

            // 1. PLACE 7 CAMP STOVES
            EnsureStove(root, "Stove_SafeHouse_Kitchen", new Vector3(-40.0f, 0.05f, -26.5f), 0f);
            EnsureStove(root, "Stove_LakeCabin", new Vector3(-185.0f, 0.05f, 220.0f), -45f);
            EnsureStove(root, "Stove_SouthMall_FoodCourt", new Vector3(28.0f, 4.45f, -264.0f), 90f);
            EnsureStove(root, "Stove_Outpost_NorthHighway", new Vector3(0.0f, 6.55f, 202.0f), 0f);
            EnsureStove(root, "Stove_Outpost_EastRailStation", new Vector3(303.0f, 1.35f, 66.0f), 90f);
            EnsureStove(root, "Stove_Outpost_NorthwestRanger", new Vector3(-218.0f, 0.40f, 260.0f), -30f);
            EnsureStove(root, "Stove_Outpost_SouthMetroHub", new Vector3(-2.5f, -4.15f, -188.0f), 180f);

            // 2. PLACE 4 CHEMISTRY LABS
            EnsureChemLab(root, "ChemLab_SafeHouse_Medical", new Vector3(-36.5f, 0.05f, -26.5f), 0f);
            EnsureChemLab(root, "ChemLab_Zone_Clinic", new Vector3(-35.0f, 0.05f, 36.5f), -90f);
            EnsureChemLab(root, "ChemLab_Pharmacy", new Vector3(30.0f, 0.05f, -48.0f), 90f);
            EnsureChemLab(root, "ChemLab_Outpost_SouthMetroHub", new Vector3(2.5f, -4.15f, -188.0f), 180f);
        }

        private static Transform GetOrCreateStationRoot()
        {
            GameObject existing = GameObject.Find("--- SURVIVAL_STATIONS ---");
            if (existing != null) return existing.transform;

            GameObject root = new GameObject("--- SURVIVAL_STATIONS ---");
            return root.transform;
        }

        private static GameObject EnsureStove(Transform parent, string name, Vector3 pos, float rotY)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(1.3f, 1.1f, 0.95f);

            CookingStoveInteractable stove = obj.AddComponent<CookingStoveInteractable>();
            return obj;
        }

        private static GameObject EnsureChemLab(Transform parent, string name, Vector3 pos, float rotY)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(1.6f, 1.1f, 1.0f);

            ChemistryLabInteractable lab = obj.AddComponent<ChemistryLabInteractable>();
            return obj;
        }
    }
}
