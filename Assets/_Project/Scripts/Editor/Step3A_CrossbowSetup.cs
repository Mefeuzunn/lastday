using System.IO;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class Step3A_CrossbowSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Phase 3/Step 3A - Setup Crossbow & Ranger Cache", false, 301)]
        public static void SetupStep3A()
        {
            Debug.Log("[Step3A_CrossbowSetup] Starting Phase 3 (Step 3A) Setup...");

            // 1. Open Scene
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // 2. Generate Weapon Sprites
            WeaponSpriteFactory.GenerateAllWeaponSprites();

            // 3. Setup Player Crossbow Visual
            SetupPlayerCrossbowVisual();

            // 4. Place Northwest Ranger Tower Crossbow Gun Rack
            PlaceRangerCrossbowCache();

            // 5. Ensure WorkbenchCraftingUI on HUD Canvas
            SetupWorkbenchUI();

            // 6. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[Step3A_CrossbowSetup] Step 3A Setup successfully completed and saved.");
        }

        private static void SetupPlayerCrossbowVisual()
        {
            GameObject player = GameObject.Find("Player_Root") ?? GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[Step3A_CrossbowSetup] Player_Root not found in scene!");
                return;
            }

            WeaponSystem ws = player.GetComponent<WeaponSystem>();
            if (ws == null) return;

            Transform handSocket = player.transform.Find("Visual_Model/Body_Hips/Torso/Chest/RightArm/RightForearm/RightHand/Weapon_Socket")
                ?? player.transform.Find("Visual_Model/Weapon_Socket");

            GameObject cbVisual = ws.EnsureCrossbowVisual(handSocket);
            if (cbVisual != null)
            {
                cbVisual.SetActive(ws.CurrentWeapon == WeaponType.Crossbow);
                EditorUtility.SetDirty(ws);
                Debug.Log("[Step3A_CrossbowSetup] Ensured Player_Root visual Crossbow is mounted and wired.");
            }
        }

        private static void PlaceRangerCrossbowCache()
        {
            const string RackName = "WeaponPickup_RangerTowerCrossbow";

            // If already exists, delete and replace for clean idempotent placement
            GameObject existing = GameObject.Find(RackName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Vector3 targetPos = new Vector3(-220f, 0.35f, 259f);
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }

            GameObject root = new GameObject(RackName);
            root.transform.position = targetPos;
            root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            // Wooden Gun Rack Frame
            GameObject rackBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rackBase.name = "Rack_WoodBase";
            rackBase.transform.SetParent(root.transform, false);
            rackBase.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            rackBase.transform.localScale = new Vector3(1.1f, 0.9f, 0.5f);
            Object.DestroyImmediate(rackBase.GetComponent<Collider>());

            Material woodMat = GetOrCreateMaterial("Mat_RangerRackWood", new Color(0.38f, 0.25f, 0.15f), 0.2f);
            rackBase.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Upright Gun Posts
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postL.name = "Post_L";
            postL.transform.SetParent(root.transform, false);
            postL.transform.localPosition = new Vector3(-0.45f, 0.95f, 0f);
            postL.transform.localScale = new Vector3(0.08f, 0.45f, 0.12f);
            Object.DestroyImmediate(postL.GetComponent<Collider>());
            postL.GetComponent<Renderer>().sharedMaterial = woodMat;

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postR.name = "Post_R";
            postR.transform.SetParent(root.transform, false);
            postR.transform.localPosition = new Vector3(0.45f, 0.95f, 0f);
            postR.transform.localScale = new Vector3(0.08f, 0.45f, 0.12f);
            Object.DestroyImmediate(postR.GetComponent<Collider>());
            postR.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Resting Crossbow 3D Visual
            Material steelMat = GetOrCreateMaterial("Mat_Weapon_Steel", new Color(0.20f, 0.22f, 0.25f), 0.8f);
            Material polymerMat = GetOrCreateMaterial("Mat_TacticalPolymer", new Color(0.12f, 0.13f, 0.15f), 0.4f);

            GameObject cbVisual = new GameObject("Visual_RangerCrossbow");
            cbVisual.transform.SetParent(root.transform, false);
            cbVisual.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            cbVisual.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(cbVisual.transform, false);
            stock.transform.localScale = new Vector3(0.05f, 0.06f, 0.48f);
            Object.DestroyImmediate(stock.GetComponent<Collider>());
            stock.GetComponent<Renderer>().sharedMaterial = polymerMat;

            GameObject prod = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prod.name = "Prod";
            prod.transform.SetParent(cbVisual.transform, false);
            prod.transform.localPosition = new Vector3(0f, 0.02f, 0.20f);
            prod.transform.localScale = new Vector3(0.42f, 0.035f, 0.025f);
            Object.DestroyImmediate(prod.GetComponent<Collider>());
            prod.GetComponent<Renderer>().sharedMaterial = steelMat;

            // 8 Quiver Bolts on side of rack
            Material fletchMat = GetOrCreateMaterial("Mat_Bolt_Fletching", new Color(0.20f, 0.85f, 0.95f), 0.5f);
            GameObject quiver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            quiver.name = "AmmoQuiver";
            quiver.transform.SetParent(root.transform, false);
            quiver.transform.localPosition = new Vector3(0.35f, 0.65f, 0.18f);
            quiver.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
            Object.DestroyImmediate(quiver.GetComponent<Collider>());
            quiver.GetComponent<Renderer>().sharedMaterial = polymerMat;

            for (int b = 0; b < 4; b++)
            {
                GameObject boltVane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boltVane.name = $"BoltVane_{b}";
                boltVane.transform.SetParent(quiver.transform, false);
                boltVane.transform.localPosition = new Vector3((b - 1.5f) * 0.18f, 0.55f, 0f);
                boltVane.transform.localScale = new Vector3(0.04f, 0.25f, 0.04f);
                Object.DestroyImmediate(boltVane.GetComponent<Collider>());
                boltVane.GetComponent<Renderer>().sharedMaterial = fletchMat;
            }

            // Interaction Trigger Collider (1.85m radius)
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.85f;
            sc.center = new Vector3(0f, 0.75f, 0f);

            // WeaponPickupInteractable
            WeaponPickupInteractable pickup = root.AddComponent<WeaponPickupInteractable>();
            pickup.Configure(
                WeaponType.Crossbow,
                "weapon_crossbow",
                "ammo_crossbow_bolt",
                8,
                "Taktik Tatar Yayı",
                "SESSİZ MENZİLLİ (+8 OK)"
            );
            pickup.SetPersistentId("world_cache_ranger_crossbow_nw");

            EditorUtility.SetDirty(root);
            Debug.Log($"[Step3A_CrossbowSetup] Placed Ranger Crossbow Gun Rack at {targetPos} on NavMesh.");
        }

        private static void SetupWorkbenchUI()
        {
            var wb = Object.FindFirstObjectByType<WorkbenchCraftingUI>(FindObjectsInactive.Include);
            if (wb == null)
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

                if (hudCanvas != null)
                {
                    GameObject uiObj = new GameObject("WorkbenchCrafting_UI");
                    uiObj.transform.SetParent(hudCanvas.transform, false);
                    wb = uiObj.AddComponent<WorkbenchCraftingUI>();
                    EditorUtility.SetDirty(uiObj);
                    EditorUtility.SetDirty(hudCanvas.gameObject);
                    Debug.Log("[Step3A_CrossbowSetup] Added WorkbenchCraftingUI to HUD_Canvas.");
                }
            }
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
