using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.AI;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class Step3B_HuntingFishingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Phase 3/Step 3B - Setup Wildlife & Lake Fishing", false, 303)]
        public static void SetupStep3B()
        {
            Debug.Log("[Step3B_HuntingFishingSetup] Starting Phase 3 (Step 3B) Setup...");

            // 1. Open Scene
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // 2. Setup Western Wildlife (4 Deer + 2 Boars)
            SetupWesternWildlife();

            // 3. Setup Northwest Lake Fishing Pier Spot
            SetupLakeFishingSpot();

            // 4. Ensure FishingMiniGameUI on HUD_Canvas
            SetupFishingUI();

            // 5. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[Step3B_HuntingFishingSetup] Step 3B Setup successfully completed and saved.");
        }

        private static void SetupWesternWildlife()
        {
            const string WildlifeGroupName = "--- PHASE3_WILDLIFE_ENTITIES ---";
            GameObject group = GameObject.Find(WildlifeGroupName);
            if (group != null)
            {
                Object.DestroyImmediate(group);
            }

            group = new GameObject(WildlifeGroupName);

            // Sector_-1_0_West (West Urban Forest) & Sector_-1_1_NorthWest (Lake Woods)
            // Exactly 6 wildlife entities: 4 Deer + 2 Boars. 0 in Sector_0_0_Center!
            var spawnConfigs = new (string name, WildlifeType type, Vector3 pos)[]
            {
                // Sector_-1_0_West: 2 Deer + 1 Boar
                ("Wildlife_Deer_WestPark_1", WildlifeType.ForestDeer, new Vector3(-210f, 0.2f, -15f)),
                ("Wildlife_Deer_WestPark_2", WildlifeType.ForestDeer, new Vector3(-255f, 0.2f, 25f)),
                ("Wildlife_Boar_WestWoods_1", WildlifeType.FeralBoar, new Vector3(-235f, 0.2f, -45f)),

                // Sector_-1_1_NorthWest: 2 Deer + 1 Boar
                ("Wildlife_Deer_Lakeside_3", WildlifeType.ForestDeer, new Vector3(-195f, 0.3f, 215f)),
                ("Wildlife_Deer_Lakeside_4", WildlifeType.ForestDeer, new Vector3(-260f, 0.3f, 220f)),
                ("Wildlife_Boar_LakesideWoods_2", WildlifeType.FeralBoar, new Vector3(-270f, 0.3f, 260f))
            };

            foreach (var cfg in spawnConfigs)
            {
                Vector3 spawnPos = cfg.pos;
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }

                GameObject animalObj = new GameObject(cfg.name);
                animalObj.transform.SetParent(group.transform);
                animalObj.transform.position = spawnPos;
                animalObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                // NavMeshAgent
                NavMeshAgent agent = animalObj.AddComponent<NavMeshAgent>();
                agent.speed = cfg.type == WildlifeType.ForestDeer ? 1.8f : 1.4f;
                agent.acceleration = 12f;
                agent.angularSpeed = 240f;
                agent.radius = cfg.type == WildlifeType.ForestDeer ? 0.65f : 0.75f;
                agent.height = cfg.type == WildlifeType.ForestDeer ? 1.5f : 1.1f;
                agent.stoppingDistance = 0.5f;

                // Hitbox CapsuleCollider
                CapsuleCollider col = animalObj.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, cfg.type == WildlifeType.ForestDeer ? 0.75f : 0.55f, 0f);
                col.radius = cfg.type == WildlifeType.ForestDeer ? 0.45f : 0.55f;
                col.height = cfg.type == WildlifeType.ForestDeer ? 1.4f : 1.1f;

                // Procedural 3D Visual Model
                BuildAnimalProceduralModel(animalObj.transform, cfg.type);

                // WildlifeAnimalAI
                WildlifeAnimalAI ai = animalObj.AddComponent<WildlifeAnimalAI>();
                var field = typeof(WildlifeAnimalAI).GetField("species", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(ai, cfg.type);

                ai.AutoBindVisualParts();
                EditorUtility.SetDirty(animalObj);
            }

            EditorUtility.SetDirty(group);
            Debug.Log("[Step3B_HuntingFishingSetup] Placed 6 Western Wildlife entities on NavMesh.");
        }

        private static void BuildAnimalProceduralModel(Transform parent, WildlifeType type)
        {
            GameObject visual = new GameObject("Visual_Model");
            visual.transform.SetParent(parent, false);

            if (type == WildlifeType.ForestDeer)
            {
                // Deer Coat: warm tawny chestnut brown
                Material coatMat = GetOrCreateMaterial("Mat_DeerCoat", new Color(0.55f, 0.32f, 0.18f), 0.35f);
                Material antlerMat = GetOrCreateMaterial("Mat_DeerAntler", new Color(0.85f, 0.82f, 0.75f), 0.5f);
                Material hoofMat = GetOrCreateMaterial("Mat_DeerHoof", new Color(0.12f, 0.10f, 0.08f), 0.2f);

                // Body Torso
                GameObject torso = CreateBox(visual.transform, "Body_Torso", new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.60f, 1.25f), coatMat);

                // Neck & Head
                GameObject neck = CreateBox(torso.transform, "Neck", new Vector3(0f, 0.35f, 0.55f), new Vector3(0.24f, 0.50f, 0.26f), coatMat);
                neck.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);

                GameObject head = CreateBox(neck.transform, "Head", new Vector3(0f, 0.28f, 0.18f), new Vector3(0.26f, 0.26f, 0.42f), coatMat);

                // Antlers
                GameObject antlerL = CreateBox(head.transform, "Antler_L", new Vector3(-0.16f, 0.28f, 0.05f), new Vector3(0.04f, 0.45f, 0.04f), antlerMat);
                antlerL.transform.localRotation = Quaternion.Euler(15f, -20f, -15f);

                GameObject antlerR = CreateBox(head.transform, "Antler_R", new Vector3(0.16f, 0.28f, 0.05f), new Vector3(0.04f, 0.45f, 0.04f), antlerMat);
                antlerR.transform.localRotation = Quaternion.Euler(15f, 20f, 15f);

                // 4 Slender Legs
                CreateBox(visual.transform, "Leg_FL", new Vector3(-0.22f, 0.38f, 0.45f), new Vector3(0.12f, 0.76f, 0.12f), hoofMat);
                CreateBox(visual.transform, "Leg_FR", new Vector3(0.22f, 0.38f, 0.45f), new Vector3(0.12f, 0.76f, 0.12f), hoofMat);
                CreateBox(visual.transform, "Leg_BL", new Vector3(-0.22f, 0.38f, -0.45f), new Vector3(0.12f, 0.76f, 0.12f), hoofMat);
                CreateBox(visual.transform, "Leg_BR", new Vector3(0.22f, 0.38f, -0.45f), new Vector3(0.12f, 0.76f, 0.12f), hoofMat);
            }
            else
            {
                // Boar Hide: dark bristly slate brown
                Material hideMat = GetOrCreateMaterial("Mat_BoarHide", new Color(0.25f, 0.20f, 0.18f), 0.25f);
                Material tuskMat = GetOrCreateMaterial("Mat_BoarTusk", new Color(0.92f, 0.90f, 0.85f), 0.6f);
                Material hoofMat = GetOrCreateMaterial("Mat_BoarHoof", new Color(0.10f, 0.08f, 0.08f), 0.2f);

                // Bulky Body Torso
                GameObject torso = CreateBox(visual.transform, "Body_Torso", new Vector3(0f, 0.62f, 0f), new Vector3(0.78f, 0.72f, 1.20f), hideMat);

                // Heavy Wedge Neck & Head
                GameObject neck = CreateBox(torso.transform, "Neck", new Vector3(0f, 0.05f, 0.55f), new Vector3(0.50f, 0.50f, 0.48f), hideMat);
                neck.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

                // Tusks protruding
                GameObject tuskL = CreateBox(neck.transform, "Tusk_L", new Vector3(-0.22f, -0.12f, 0.26f), new Vector3(0.04f, 0.14f, 0.06f), tuskMat);
                tuskL.transform.localRotation = Quaternion.Euler(-25f, -15f, 0f);

                GameObject tuskR = CreateBox(neck.transform, "Tusk_R", new Vector3(0.22f, -0.12f, 0.26f), new Vector3(0.04f, 0.14f, 0.06f), tuskMat);
                tuskR.transform.localRotation = Quaternion.Euler(-25f, 15f, 0f);

                // 4 Stout Legs
                CreateBox(visual.transform, "Leg_FL", new Vector3(-0.28f, 0.24f, 0.40f), new Vector3(0.18f, 0.48f, 0.18f), hoofMat);
                CreateBox(visual.transform, "Leg_FR", new Vector3(0.28f, 0.24f, 0.40f), new Vector3(0.18f, 0.48f, 0.18f), hoofMat);
                CreateBox(visual.transform, "Leg_BL", new Vector3(-0.28f, 0.24f, -0.40f), new Vector3(0.18f, 0.48f, 0.18f), hoofMat);
                CreateBox(visual.transform, "Leg_BR", new Vector3(0.28f, 0.24f, -0.40f), new Vector3(0.18f, 0.48f, 0.18f), hoofMat);
            }

            // Ground Contact Shadow Disk
            Material shadowMat = GetOrCreateMaterial("Mat_ContactShadow", new Color(0.04f, 0.05f, 0.06f, 0.65f), 0.1f);
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Contact_Shadow";
            shadow.transform.SetParent(visual.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localScale = new Vector3(1.1f, 0.01f, 1.4f);
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.GetComponent<Renderer>().sharedMaterial = shadowMat;
        }

        private static void SetupLakeFishingSpot()
        {
            const string SpotName = "FishingSpot_NorthwestLakePier";
            GameObject existing = GameObject.Find(SpotName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            // Pier end position at Sector_-1_1_NorthWest
            Vector3 pierPos = new Vector3(-220f, 0.40f, 276f);
            if (NavMesh.SamplePosition(pierPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                pierPos = hit.position;
            }

            GameObject spot = new GameObject(SpotName);
            spot.transform.position = pierPos;
            spot.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Material woodMat = GetOrCreateMaterial("Mat_FishingPierWood", new Color(0.35f, 0.25f, 0.16f), 0.2f);
            Material metalMat = GetOrCreateMaterial("Mat_Weapon_Steel", new Color(0.22f, 0.24f, 0.26f), 0.8f);

            // Tackle Crate
            GameObject tackle = CreateBox(spot.transform, "Tackle_Crate", new Vector3(0.75f, 0.35f, 0f), new Vector3(0.75f, 0.55f, 0.55f), woodMat);

            // Leaning Fishing Rod
            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "Prop_FishingRod";
            rod.transform.SetParent(spot.transform, false);
            rod.transform.localPosition = new Vector3(0.65f, 0.85f, 0.35f);
            rod.transform.localRotation = Quaternion.Euler(35f, 15f, -10f);
            rod.transform.localScale = new Vector3(0.035f, 1.25f, 0.035f);
            Object.DestroyImmediate(rod.GetComponent<Collider>());
            rod.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Trigger SphereCollider for interaction
            SphereCollider sc = spot.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.2f;
            sc.center = new Vector3(0f, 0.6f, 0f);

            // LakeFishingSpotInteractable
            LakeFishingSpotInteractable fis = spot.AddComponent<LakeFishingSpotInteractable>();
            fis.SetPersistentId("lake_fishing_spot_nw_pier");

            EditorUtility.SetDirty(spot);
            Debug.Log($"[Step3B_HuntingFishingSetup] Placed Northwest Lake Pier Fishing Spot at {pierPos}.");
        }

        private static void SetupFishingUI()
        {
            Canvas hudCanvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.gameObject.name == "HUD_Canvas")
                {
                    hudCanvas = c;
                    break;
                }
            }
            if (hudCanvas == null && canvases.Length > 0) hudCanvas = canvases[0];

            Transform oldRoot = hudCanvas != null ? hudCanvas.transform.Find("FishingMiniGame_PanelRoot") : null;
            if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);

            Transform existingUI = hudCanvas != null ? hudCanvas.transform.Find("FishingMiniGame_UI") : null;
            if (existingUI != null) Object.DestroyImmediate(existingUI.gameObject);

            FishingMiniGameUI ui = null;
            if (hudCanvas != null)
            {
                GameObject uiObj = new GameObject("FishingMiniGame_UI");
                uiObj.transform.SetParent(hudCanvas.transform, false);
                ui = uiObj.AddComponent<FishingMiniGameUI>();
                ui.BuildUIHierarchyIfEmpty();
                EditorUtility.SetDirty(uiObj);
                EditorUtility.SetDirty(hudCanvas.gameObject);
                Debug.Log("[Step3B_HuntingFishingSetup] Added FishingMiniGameUI to HUD_Canvas.");
            }

            // Also ensure SurvivalAlchemyUI is present on HUD_Canvas
            var alchemyUI = Object.FindFirstObjectByType<SurvivalAlchemyUI>(FindObjectsInactive.Include);
            if (alchemyUI == null && hudCanvas != null)
            {
                alchemyUI = hudCanvas.gameObject.AddComponent<SurvivalAlchemyUI>();
                EditorUtility.SetDirty(hudCanvas.gameObject);
                Debug.Log("[Step3B_HuntingFishingSetup] Added SurvivalAlchemyUI to HUD_Canvas.");
            }
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = localScale;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            return box;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness = 0.5f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader)
            {
                name = name,
                enableInstancing = true
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
#endif
    }
}
