using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 3 (Step 3B): Weapon, Survival Item & Yard Barricade 3D Model Upgrader.
    /// Replaces all primitive cubes, cylinders, and boxes on weapons, viewmodels,
    /// world pickups, and starter yard barricades with genuine CC0 3D models.
    /// </summary>
    public static class WeaponAndPropUpgrader
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string ModelsDir = "Assets/_Project/Models/CC0_Weapons_Items";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Upgrade Weapons & Props with CC0 3D Models", false, 49)]
        public static void UpgradeAllWeaponsAndPropsMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[WeaponAndPropUpgrader] Starting complete 3D Weapon, Item & Barricade Upgrade...");

            // 1. Upgrade Player Equipped Weapon Sockets (FPS & TPS Viewmodels)
            UpgradePlayerWeaponVisuals();

            // 2. Upgrade Starter World Pickups (Fire Axe on crate, 9mm Pistol on Cruiser, Shotgun racks)
            UpgradeWorldWeaponPickups();

            // 3. Upgrade Starter Yard Barricades & Wooden Crates
            UpgradeStarterYardBarricades();

            // 4. Convert all materials to Universal Render Pipeline (URP/Lit)
            ConvertAllWeaponMaterialsToURP();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[WeaponAndPropUpgrader] SUCCESS: All weapons, viewmodels, pickups, and yard crates upgraded with real 3D meshes!");
        }

        public static GameObject LoadOrInstantiateModel(string modelFileName, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            string assetPath = Path.Combine(ModelsDir, modelFileName);
            GameObject instance = null;

            // 1. Try AssetDatabase prefab load
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }

            // 2. Fallback to GLTFast direct import if prefab not yet baked
            if (instance == null)
            {
                string fullPath = Path.GetFullPath(assetPath);
                if (File.Exists(fullPath))
                {
                    var gltf = new GLTFast.GltfImport();
                    var task = gltf.LoadFile(fullPath);
                    task.Wait();
                    if (task.Result)
                    {
                        GameObject container = new GameObject(Path.GetFileNameWithoutExtension(modelFileName) + "_GLTF");
                        container.transform.SetParent(parent, false);
                        gltf.InstantiateMainScene(container.transform);
                        instance = container;
                    }
                }
            }

            if (instance != null)
            {
                instance.name = Path.GetFileNameWithoutExtension(modelFileName);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPos;
                instance.transform.localEulerAngles = localRot;
                instance.transform.localScale = localScale;
            }
            else
            {
                Debug.LogWarning($"[WeaponAndPropUpgrader] Could not instantiate 3D model '{modelFileName}'!");
            }

            return instance;
        }

        public static void UpgradePlayerWeaponVisuals()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[WeaponAndPropUpgrader] Player GameObject not found in active scene!");
                return;
            }

            WeaponSystem ws = player.GetComponent<WeaponSystem>();
            Transform handSocket = player.transform.Find("Visual_Model/Body_Hips/Torso/Chest/RightArm/RightForearm/RightHand/Weapon_Socket")
                                  ?? player.transform.Find("Visual_Model/Weapon_Socket");

            if (handSocket == null)
            {
                // Find any existing Weapon_Socket in children
                var allTfs = player.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < allTfs.Length; i++)
                {
                    if (allTfs[i].name == "Weapon_Socket")
                    {
                        handSocket = allTfs[i];
                        break;
                    }
                }
            }

            if (handSocket == null)
            {
                Debug.LogWarning("[WeaponAndPropUpgrader] Weapon_Socket not found on Player!");
                return;
            }

            // 1. Fire Axe Visual
            Transform axeTf = handSocket.Find("Visual_Weapon_Axe");
            if (axeTf == null)
            {
                axeTf = new GameObject("Visual_Weapon_Axe").transform;
                axeTf.SetParent(handSocket, false);
            }
            axeTf.localPosition = new Vector3(0f, 0.05f, 0.15f);
            axeTf.localEulerAngles = new Vector3(15f, 0f, -20f);
            StripPrimitiveChildren(axeTf, new[] { "Handle", "Blade_Steel", "Blade_RedBack" });

            Transform existingAxeModel = axeTf.Find("axeUpgraded") ?? axeTf.Find("axe");
            if (existingAxeModel != null) UnityEngine.Object.DestroyImmediate(existingAxeModel.gameObject);
            GameObject axeModel = LoadOrInstantiateModel("axeUpgraded.glb", axeTf, Vector3.zero, new Vector3(0f, 90f, 0f), new Vector3(1.15f, 1.15f, 1.15f));

            // 2. 9mm Pistol Visual
            Transform pistolTf = handSocket.Find("Visual_Weapon_Pistol");
            if (pistolTf == null)
            {
                pistolTf = new GameObject("Visual_Weapon_Pistol").transform;
                pistolTf.SetParent(handSocket, false);
            }
            pistolTf.localPosition = new Vector3(0f, -0.02f, 0.18f);
            pistolTf.localEulerAngles = new Vector3(0f, 90f, 0f);
            StripPrimitiveChildren(pistolTf, new[] { "Grip", "Slide" });

            Transform existingPistolModel = pistolTf.Find("pistol");
            if (existingPistolModel != null) UnityEngine.Object.DestroyImmediate(existingPistolModel.gameObject);
            GameObject pistolModel = LoadOrInstantiateModel("pistol.glb", pistolTf, Vector3.zero, Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f));

            // Ensure Muzzle Point and Light on Pistol
            Transform muzzlePoint = pistolTf.Find("Muzzle_Point");
            if (muzzlePoint == null)
            {
                muzzlePoint = new GameObject("Muzzle_Point").transform;
                muzzlePoint.SetParent(pistolTf, false);
            }
            muzzlePoint.localPosition = new Vector3(0f, 0.06f, 0.22f);

            Transform lightObj = muzzlePoint.Find("Muzzle_Light");
            Light mLight = lightObj != null ? lightObj.GetComponent<Light>() : null;
            if (mLight == null)
            {
                GameObject lo = new GameObject("Muzzle_Light");
                lo.transform.SetParent(muzzlePoint, false);
                lo.transform.localPosition = Vector3.zero;
                mLight = lo.AddComponent<Light>();
                mLight.type = LightType.Point;
                mLight.color = new Color(1.0f, 0.85f, 0.45f);
                mLight.range = 7f;
                mLight.intensity = 2.8f;
                mLight.enabled = false;
            }

            // 3. Pump Shotgun Visual
            Transform shotgunTf = handSocket.Find("Visual_Weapon_Shotgun");
            if (shotgunTf == null)
            {
                shotgunTf = new GameObject("Visual_Weapon_Shotgun").transform;
                shotgunTf.SetParent(handSocket, false);
            }
            shotgunTf.localPosition = new Vector3(0f, 0.02f, 0.22f);
            shotgunTf.localEulerAngles = new Vector3(0f, 90f, 0f);
            StripPrimitiveChildren(shotgunTf, new[] { "Stock", "Receiver", "Barrel" });

            Transform existingShotgunModel = shotgunTf.Find("shotgun");
            if (existingShotgunModel != null) UnityEngine.Object.DestroyImmediate(existingShotgunModel.gameObject);
            GameObject shotgunModel = LoadOrInstantiateModel("shotgun.glb", shotgunTf, Vector3.zero, Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));

            // 4. Tactical Crossbow Visual
            Transform crossbowTf = handSocket.Find("Visual_Weapon_Crossbow");
            if (crossbowTf == null)
            {
                crossbowTf = new GameObject("Visual_Weapon_Crossbow").transform;
                crossbowTf.SetParent(handSocket, false);
            }
            crossbowTf.localPosition = new Vector3(0f, 0.02f, 0.20f);
            crossbowTf.localEulerAngles = new Vector3(0f, 90f, 0f);
            StripPrimitiveChildren(crossbowTf, new[] { "Stock_Rail", "Prod_Limbs", "Stirrup", "Reflex_Optic" });

            Transform existingCrossbowModel = crossbowTf.Find("crossbow");
            if (existingCrossbowModel != null) UnityEngine.Object.DestroyImmediate(existingCrossbowModel.gameObject);
            GameObject crossbowModel = LoadOrInstantiateModel("crossbow.glb", crossbowTf, Vector3.zero, new Vector3(-90f, 0f, 0f), new Vector3(0.008f, 0.008f, 0.008f));

            if (ws != null)
            {
                ws.SetVisualReferences(axeTf.gameObject, pistolTf.gameObject, shotgunTf.gameObject, muzzlePoint, mLight, crossbowTf.gameObject);
            }

            Debug.Log("[WeaponAndPropUpgrader] Player equipped weapons (Axe, Pistol, Shotgun, Crossbow) upgraded with real 3D models.");
        }

        public static void UpgradeWorldWeaponPickups()
        {
            // 1. Starter Fire Axe at (0f, 0f, 6.5f) on Wooden Supply Crate
            GameObject axeRoot = GameObject.Find("WeaponPickup_Starter_FireAxe");
            if (axeRoot != null)
            {
                // Replace Crate_Mount primitive cube with real 3D Crate
                Transform crateMount = axeRoot.transform.Find("Crate_Mount");
                if (crateMount != null)
                {
                    StripMeshComponents(crateMount.gameObject);
                    Transform existingCrate = crateMount.Find("crateWood") ?? crateMount.Find("crate");
                    if (existingCrate != null) UnityEngine.Object.DestroyImmediate(existingCrate.gameObject);
                    LoadOrInstantiateModel("crateWood.glb", crateMount, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));

                    var cCol = crateMount.GetComponent<BoxCollider>();
                    if (cCol == null) cCol = crateMount.gameObject.AddComponent<BoxCollider>();
                    cCol.size = new Vector3(0.95f, 0.95f, 0.95f);
                    cCol.center = Vector3.zero;
                }

                // Replace Visual_Axe_Model primitive parts with real 3D Fire Axe
                Transform axeVisual = axeRoot.transform.Find("Visual_Axe_Model");
                if (axeVisual != null)
                {
                    StripPrimitiveChildren(axeVisual, new[] { "Handle", "Blade_Steel", "Blade_RedBack" });
                    Transform existingAxe = axeVisual.Find("axeUpgraded") ?? axeVisual.Find("axe");
                    if (existingAxe != null) UnityEngine.Object.DestroyImmediate(existingAxe.gameObject);
                    LoadOrInstantiateModel("axeUpgraded.glb", axeVisual, new Vector3(0f, 0.15f, 0f), new Vector3(0f, 90f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
                }

                // Preserve Sphere Trigger Collider and WeaponPickupInteractable
                SphereCollider sc = axeRoot.GetComponent<SphereCollider>();
                if (sc == null) sc = axeRoot.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.8f;
                sc.center = new Vector3(0f, 0.6f, 0f);

                var pickup = axeRoot.GetComponent<WeaponPickupInteractable>();
                if (pickup != null)
                {
                    pickup.Configure(WeaponType.Axe, "weapon_axe", "", 0, "Yangın Baltası", "YAKIN DÖVÜŞ");
                    pickup.SetPersistentId("starter_pickup_axe_boulevard");
                }
            }

            // 2. Starter 9mm Pistol at (2.2f, 1.05f, 15.5f) on Police Cruiser Hood
            GameObject pistolRoot = GameObject.Find("WeaponPickup_Starter_Pistol");
            if (pistolRoot != null)
            {
                Transform pistolVisual = pistolRoot.transform.Find("Visual_Pistol_Model");
                if (pistolVisual != null)
                {
                    StripPrimitiveChildren(pistolVisual, new[] { "Grip", "Slide" });
                    Transform existingPistol = pistolVisual.Find("pistol");
                    if (existingPistol != null) UnityEngine.Object.DestroyImmediate(existingPistol.gameObject);
                    LoadOrInstantiateModel("pistol.glb", pistolVisual, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
                }

                // Replace primitive AmmoBox_9mm_1 and AmmoBox_9mm_2 with real 3D Ammo Boxes
                Transform b1 = pistolRoot.transform.Find("AmmoBox_9mm_1");
                if (b1 != null)
                {
                    StripMeshComponents(b1.gameObject);
                    Transform ex = b1.Find("ammoBox");
                    if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);
                    LoadOrInstantiateModel("ammoBox.glb", b1, Vector3.zero, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f));
                }

                Transform b2 = pistolRoot.transform.Find("AmmoBox_9mm_2");
                if (b2 != null)
                {
                    StripMeshComponents(b2.gameObject);
                    Transform ex = b2.Find("ammoBox");
                    if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);
                    LoadOrInstantiateModel("ammoBox.glb", b2, Vector3.zero, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f));
                }

                SphereCollider sc = pistolRoot.GetComponent<SphereCollider>();
                if (sc == null) sc = pistolRoot.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.8f;
                sc.center = new Vector3(0f, 0.2f, 0f);

                var pickup = pistolRoot.GetComponent<WeaponPickupInteractable>();
                if (pickup != null)
                {
                    pickup.Configure(WeaponType.Pistol, "weapon_pistol", "ammo_9mm", 24, "9mm Polis Tabancası", "ATEŞLİ SİLAH (+24 MERMİ)");
                    pickup.SetPersistentId("starter_pickup_pistol_cruiser");
                }
            }

            // 3. Upgrade any Shotgun Pickups in scene
            WeaponPickupInteractable[] allPickups = UnityEngine.Object.FindObjectsByType<WeaponPickupInteractable>(FindObjectsSortMode.None);
            foreach (var p in allPickups)
            {
                if (p.WeaponType == WeaponType.Shotgun)
                {
                    Transform sgModel = p.transform.Find("Visual_Shotgun_Model") ?? p.transform.Find("Visual_Weapon_Shotgun");
                    if (sgModel != null)
                    {
                        StripPrimitiveChildren(sgModel, new[] { "Stock", "Receiver", "Barrel" });
                        Transform ex = sgModel.Find("shotgun");
                        if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);
                        LoadOrInstantiateModel("shotgun.glb", sgModel, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
                    }
                }
            }

            Debug.Log("[WeaponAndPropUpgrader] World weapon pickups (Axe on Crate, Pistol on Cruiser, Shotgun racks) upgraded with real 3D models.");
        }

        public static void UpgradeStarterYardBarricades()
        {
            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot == null) return;

            Transform yardTf = houseRoot.transform.Find("Starter_Yard_Fortifications");
            if (yardTf == null)
            {
                GameObject yObj = new GameObject("Starter_Yard_Fortifications");
                yObj.transform.SetParent(houseRoot.transform, false);
                yardTf = yObj.transform;
            }

            // 1. Fortified Wooden Barricades flanking front yard entrance path (Z = 6.2, X = -2.4 and +2.4)
            Transform bLeft = yardTf.Find("Barricade_Left");
            if (bLeft == null)
            {
                GameObject bl = new GameObject("Barricade_Left");
                bl.transform.SetParent(yardTf, false);
                bLeft = bl.transform;
            }
            bLeft.localPosition = new Vector3(-2.6f, 0.0f, 6.2f);
            bLeft.localEulerAngles = new Vector3(0f, 15f, 0f);
            Transform exBl = bLeft.Find("barricade");
            if (exBl != null) UnityEngine.Object.DestroyImmediate(exBl.gameObject);
            GameObject blModel = LoadOrInstantiateModel("barricade.glb", bLeft, Vector3.zero, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
            if (blModel != null) StarterHouseInteriorUpgrader.GroundModel(blModel, 0.0f);
            var colBl = bLeft.GetComponent<BoxCollider>();
            if (colBl == null) colBl = bLeft.gameObject.AddComponent<BoxCollider>();
            colBl.size = new Vector3(2.2f, 1.3f, 0.65f);
            colBl.center = new Vector3(0f, 0.65f, 0f);

            Transform bRight = yardTf.Find("Barricade_Right");
            if (bRight == null)
            {
                GameObject br = new GameObject("Barricade_Right");
                br.transform.SetParent(yardTf, false);
                bRight = br.transform;
            }
            bRight.localPosition = new Vector3(2.6f, 0.0f, 6.2f);
            bRight.localEulerAngles = new Vector3(0f, -15f, 0f);
            Transform exBr = bRight.Find("barricade");
            if (exBr != null) UnityEngine.Object.DestroyImmediate(exBr.gameObject);
            GameObject brModel = LoadOrInstantiateModel("barricade.glb", bRight, Vector3.zero, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
            if (brModel != null) StarterHouseInteriorUpgrader.GroundModel(brModel, 0.0f);
            var colBr = bRight.GetComponent<BoxCollider>();
            if (colBr == null) colBr = bRight.gameObject.AddComponent<BoxCollider>();
            colBr.size = new Vector3(2.2f, 1.3f, 0.65f);
            colBr.center = new Vector3(0f, 0.65f, 0f);

            // 2. Heavy Supply Crates and Fuel Barrels in Front Yard Corner
            Transform cStack = yardTf.Find("Yard_Supply_Stack");
            if (cStack == null)
            {
                GameObject cs = new GameObject("Yard_Supply_Stack");
                cs.transform.SetParent(yardTf, false);
                cStack = cs.transform;
            }
            cStack.localPosition = new Vector3(-3.8f, 0.0f, 5.2f);
            Transform exC1 = cStack.Find("crate");
            if (exC1 != null) UnityEngine.Object.DestroyImmediate(exC1.gameObject);
            GameObject c1 = LoadOrInstantiateModel("crate.glb", cStack, Vector3.zero, new Vector3(0f, 25f, 0f), new Vector3(1.35f, 1.35f, 1.35f));
            if (c1 != null) StarterHouseInteriorUpgrader.GroundModel(c1, 0.0f);

            Transform exBarrel = cStack.Find("barrel");
            if (exBarrel != null) UnityEngine.Object.DestroyImmediate(exBarrel.gameObject);
            GameObject b1 = LoadOrInstantiateModel("barrel.glb", cStack, new Vector3(0.9f, 0.0f, 0.2f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
            if (b1 != null) StarterHouseInteriorUpgrader.GroundModel(b1, 0.0f);

            var stackCol = cStack.GetComponent<BoxCollider>();
            if (stackCol == null) stackCol = cStack.gameObject.AddComponent<BoxCollider>();
            stackCol.size = new Vector3(2.0f, 1.1f, 1.1f);
            stackCol.center = new Vector3(0.45f, 0.55f, 0.1f);

            Debug.Log("[WeaponAndPropUpgrader] Starter yard barricades, wooden crates, and fuel barrels upgraded with real 3D models.");
        }

        public static void ConvertAllWeaponMaterialsToURP()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null) return;

            Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var rend in allRenderers)
            {
                string path = rend.gameObject.name.ToLower();
                if (!path.Contains("weapon") && !path.Contains("axe") && !path.Contains("pistol") &&
                    !path.Contains("shotgun") && !path.Contains("crossbow") && !path.Contains("crate") &&
                    !path.Contains("barricade") && !path.Contains("ammo") && !path.Contains("barrel"))
                {
                    continue;
                }

                Material[] mats = rend.sharedMaterials;
                bool mod = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;

                    string sName = m.shader != null ? m.shader.name : "";
                    if (m.shader == null || sName.Contains("InternalErrorShader") || sName.Contains("Error") || sName.Contains("glTF") || sName.Contains("Standard"))
                    {
                        Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.color : Color.white);
                        Texture tex = m.mainTexture;

                        Material newMat = new Material(urpLit);
                        newMat.name = m.name + "_URPLit";
                        newMat.SetColor("_BaseColor", c);
                        if (tex != null) newMat.SetTexture("_BaseMap", tex);
                        newMat.SetFloat("_Smoothness", 0.5f);
                        newMat.SetFloat("_Metallic", 0.25f);
                        newMat.enableInstancing = true;

                        mats[i] = newMat;
                        mod = true;
                        count++;
                    }
                    else
                    {
                        m.enableInstancing = true;
                    }
                }
                if (mod) rend.sharedMaterials = mats;
            }

            Debug.Log($"[WeaponAndPropUpgrader] Converted {count} weapon/prop materials to URP Lit (enableInstancing = true).");
        }

        private static void StripPrimitiveChildren(Transform parent, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform c = parent.Find(names[i]);
                if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
        }

        private static void StripMeshComponents(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) UnityEngine.Object.DestroyImmediate(mf);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class WeaponAndPropAutoUpgrader
    {
        private const string UpgradedPrefKey = "WeaponsAndProps_CC0_Step3B_Upgraded_v1";

        static WeaponAndPropAutoUpgrader()
        {
            EditorApplication.delayCall += CheckAndUpgrade;
        }

        private static void CheckAndUpgrade()
        {
            if (SessionState.GetBool(UpgradedPrefKey, false)) return;
            SessionState.SetBool(UpgradedPrefKey, true);

            Debug.Log("[WeaponAndPropAutoUpgrader] Automatically executing Step 3B CC0 3D Weapon & Prop Upgrade...");
            try
            {
                WeaponAndPropUpgrader.UpgradeAllWeaponsAndPropsMenu();
                Step3B_WeaponItem3DModelsAudit.RunAudit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeaponAndPropAutoUpgrader] Auto upgrade failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
#endif
}
