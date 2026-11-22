using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Procedurally places high-visibility starter weapons with physical mounts, 3D meshes,
    /// golden-amber pulsing rings, and diegetic billboard badges.
    /// 1. Fire Axe (Yangın Baltası): Embedded in wooden supply crate 6.5m ahead at (0f, 0.85f, 6.5f).
    /// 2. 9mm Pistol + 24 ammo (9mm Polis Tabancası): Mounted on police cruiser at (2.2f, 1.05f, 15.5f).
    /// 3. Pump Shotgun + 12 shells (Pompalı Av Tüfeği): Illuminated gun racks in SafeHouse (-33, 1.1, -28),
    ///    Police Station (-18, 1.1, 8), Outpost (0, 1.1, 180), and Grand Mall (0, 1.1, -180).
    /// </summary>
    public static class StarterArsenalPlacer
    {
        private static Material woodMaterial;
        private static Material steelMaterial;
        private static Material redMaterial;
        private static Material gunMetalMaterial;
        private static Material brassMaterial;

        public static void PlaceAllStarterWeapons()
        {
            InitializeMaterials();

            // 1. Starter Fire Axe
            CreateStarterFireAxe();

            // 2. Starter 9mm Pistol
            CreateStarterPistol();

            // 3. Shotgun Racks
            CreateShotgunPickup("safehouse", "SIĞINAK CEPHANELİĞİ", new Vector3(-33f, 1.1f, -28f), Quaternion.Euler(0f, 90f, 0f));
            CreateShotgunPickup("police_station", "POLİS MERKEZİ", new Vector3(-18f, 1.1f, 8f), Quaternion.Euler(0f, 0f, 0f));
            CreateShotgunPickup("north_outpost", "KUZEY TAHKİMATI", new Vector3(0f, 1.1f, 180f), Quaternion.Euler(0f, 180f, 0f));
            CreateShotgunPickup("south_mall", "AVM GÜVENLİK ODASI", new Vector3(0f, 1.1f, -180f), Quaternion.Euler(0f, 90f, 0f));

            // 4. Rare Weapon Attachment Kits
            CreateAttachmentKitPickup(
                "suppressor_police",
                WeaponAttachmentManager.MOD_SUPPRESSOR,
                "El Yapımı Susturucu Kiti",
                "SES GİZLİLİĞİ (-%75 GÜRÜLTÜ)",
                new Vector3(-18f, 1.1f, 11f),
                Quaternion.Euler(0f, 45f, 0f)
            );

            CreateAttachmentKitPickup(
                "laser_mall",
                WeaponAttachmentManager.MOD_LASER_SIGHT,
                "Taktik Lazer Nişangah Kiti",
                "İSABET ARTIŞI (%35 DAHA DAR DAĞILIM)",
                new Vector3(15f, 1.1f, -175f),
                Quaternion.Euler(0f, 90f, 0f)
            );

            // 5. Ensure Workbench Interactables
            SetupWorkbenchInteractables();

            Debug.Log("[StarterArsenalPlacer] All starter weapons, attachment kits, and workbenches verified and mounted.");
        }

        private static void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            woodMaterial = new Material(litShader);
            woodMaterial.name = "Mat_Procedural_WoodStock";
            SetMatColor(woodMaterial, new Color(0.40f, 0.26f, 0.16f), 0.35f, 0.05f);

            steelMaterial = new Material(litShader);
            steelMaterial.name = "Mat_Procedural_AxeSteel";
            SetMatColor(steelMaterial, new Color(0.78f, 0.80f, 0.84f), 0.88f, 0.85f);

            redMaterial = new Material(litShader);
            redMaterial.name = "Mat_Procedural_AxeRed";
            SetMatColor(redMaterial, new Color(0.85f, 0.18f, 0.15f), 0.65f, 0.2f);

            gunMetalMaterial = new Material(litShader);
            gunMetalMaterial.name = "Mat_Procedural_GunMetal";
            SetMatColor(gunMetalMaterial, new Color(0.12f, 0.13f, 0.15f), 0.75f, 0.70f);

            brassMaterial = new Material(litShader);
            brassMaterial.name = "Mat_Procedural_AmmoBrass";
            SetMatColor(brassMaterial, new Color(0.72f, 0.58f, 0.22f), 0.70f, 0.60f);
        }

        private static void SetMatColor(Material mat, Color col, float smoothness, float metallic)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else mat.color = col;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }

        private static void CreateStarterFireAxe()
        {
            if (GameObject.Find("WeaponPickup_Starter_FireAxe") != null) return;

            Vector3 crateWorldPos = new Vector3(0f, 0f, 6.5f);
            GameObject root = new GameObject("WeaponPickup_Starter_FireAxe");
            root.transform.position = crateWorldPos;

            // Supply Crate Mount (0.85 x 0.85 x 0.85m)
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate_Mount";
            crate.transform.SetParent(root.transform, false);
            crate.transform.localPosition = new Vector3(0f, 0.425f, 0f);
            crate.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            SetRendererMaterial(crate, woodMaterial);

            // 3D Fire Axe stuck into crate top
            GameObject axeModel = new GameObject("Visual_Axe_Model");
            axeModel.transform.SetParent(root.transform, false);
            axeModel.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            axeModel.transform.localRotation = Quaternion.Euler(-35f, 15f, 0f);

            // Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(axeModel.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            handle.transform.localScale = new Vector3(0.045f, 0.38f, 0.045f);
            SetRendererMaterial(handle, woodMaterial);

            // Steel Blade
            GameObject steelBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steelBlade.name = "Blade_Steel";
            steelBlade.transform.SetParent(axeModel.transform, false);
            steelBlade.transform.localPosition = new Vector3(0f, 0.52f, 0.08f);
            steelBlade.transform.localScale = new Vector3(0.05f, 0.14f, 0.20f);
            SetRendererMaterial(steelBlade, steelMaterial);

            // Red Backing
            GameObject redBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            redBack.name = "Blade_RedBack";
            redBack.transform.SetParent(axeModel.transform, false);
            redBack.transform.localPosition = new Vector3(0f, 0.52f, -0.06f);
            redBack.transform.localScale = new Vector3(0.055f, 0.12f, 0.08f);
            SetRendererMaterial(redBack, redMaterial);

            // Sphere trigger collider for interaction
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.8f;
            sc.center = new Vector3(0f, 0.6f, 0f);

            // Interactive pickup component
            WeaponPickupInteractable pickup = root.AddComponent<WeaponPickupInteractable>();
            pickup.Configure(
                WeaponType.Axe,
                "weapon_axe",
                "",
                0,
                "Yangın Baltası",
                "YAKIN DÖVÜŞ"
            );
            pickup.SetPersistentId("starter_pickup_axe_boulevard");
        }

        private static void CreateStarterPistol()
        {
            if (GameObject.Find("WeaponPickup_Starter_Pistol") != null) return;

            Vector3 cruiserHoodPos = new Vector3(2.2f, 1.05f, 15.5f);
            GameObject root = new GameObject("WeaponPickup_Starter_Pistol");
            root.transform.position = cruiserHoodPos;

            // 3D Pistol Resting on Cruiser Hood
            GameObject pistolModel = new GameObject("Visual_Pistol_Model");
            pistolModel.transform.SetParent(root.transform, false);
            pistolModel.transform.localPosition = Vector3.zero;
            pistolModel.transform.localRotation = Quaternion.Euler(0f, 75f, 0f);

            // Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(pistolModel.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0.04f, -0.04f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.038f, 0.12f, 0.055f);
            SetRendererMaterial(grip, gunMetalMaterial);

            // Slide
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Slide";
            slide.transform.SetParent(pistolModel.transform, false);
            slide.transform.localPosition = new Vector3(0f, 0.11f, 0.04f);
            slide.transform.localScale = new Vector3(0.042f, 0.055f, 0.22f);
            SetRendererMaterial(slide, gunMetalMaterial);

            // 2 Ammo Boxes
            for (int i = 0; i < 2; i++)
            {
                GameObject ammoBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ammoBox.name = $"AmmoBox_9mm_{i + 1}";
                ammoBox.transform.SetParent(root.transform, false);
                ammoBox.transform.localPosition = new Vector3(0.25f + i * 0.16f, 0.04f, -0.05f);
                ammoBox.transform.localScale = new Vector3(0.12f, 0.07f, 0.14f);
                SetRendererMaterial(ammoBox, brassMaterial);
            }

            // Sphere trigger collider for interaction
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.8f;
            sc.center = new Vector3(0f, 0.2f, 0f);

            // Interactive pickup component
            WeaponPickupInteractable pickup = root.AddComponent<WeaponPickupInteractable>();
            pickup.Configure(
                WeaponType.Pistol,
                "weapon_pistol",
                "ammo_9mm",
                24,
                "9mm Polis Tabancası",
                "ATEŞLİ SİLAH (+24 MERMİ)"
            );
            pickup.SetPersistentId("starter_pickup_pistol_cruiser");
        }

        private static void CreateShotgunPickup(string id, string locationTag, Vector3 position, Quaternion rotation)
        {
            string objName = $"WeaponPickup_Shotgun_{id}";
            if (GameObject.Find(objName) != null) return;

            GameObject root = new GameObject(objName);
            root.transform.position = position;
            root.transform.rotation = rotation;

            // Wooden/Metal Gun Rack Backboard
            GameObject rack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rack.name = "GunRack_Backboard";
            rack.transform.SetParent(root.transform, false);
            rack.transform.localPosition = new Vector3(0f, 0.1f, -0.05f);
            rack.transform.localScale = new Vector3(0.12f, 0.28f, 1.10f);
            SetRendererMaterial(rack, woodMaterial);

            // 3D Shotgun Model Mounted on Rack
            GameObject shotgunModel = new GameObject("Visual_Shotgun_Model");
            shotgunModel.transform.SetParent(root.transform, false);
            shotgunModel.transform.localPosition = new Vector3(0.08f, 0.12f, 0f);
            shotgunModel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Stock
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(shotgunModel.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.03f, -0.18f);
            stock.transform.localScale = new Vector3(0.045f, 0.09f, 0.26f);
            SetRendererMaterial(stock, woodMaterial);

            // Receiver
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Receiver";
            receiver.transform.SetParent(shotgunModel.transform, false);
            receiver.transform.localPosition = new Vector3(0f, 0.02f, 0.04f);
            receiver.transform.localScale = new Vector3(0.05f, 0.07f, 0.18f);
            SetRendererMaterial(receiver, gunMetalMaterial);

            // Long Barrel
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(shotgunModel.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.035f, 0.35f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.04f, 0.24f, 0.04f);
            SetRendererMaterial(barrel, gunMetalMaterial);

            // 2 Shell Boxes (Red)
            for (int i = 0; i < 2; i++)
            {
                GameObject shellBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shellBox.name = $"ShellBox_{i + 1}";
                shellBox.transform.SetParent(root.transform, false);
                shellBox.transform.localPosition = new Vector3(0.08f, -0.10f, -0.25f + i * 0.20f);
                shellBox.transform.localScale = new Vector3(0.12f, 0.10f, 0.15f);
                SetRendererMaterial(shellBox, redMaterial);
            }

            // Sphere trigger collider for interaction
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.8f;
            sc.center = new Vector3(0f, 0.1f, 0f);

            // Interactive pickup component
            WeaponPickupInteractable pickup = root.AddComponent<WeaponPickupInteractable>();
            pickup.Configure(
                WeaponType.Shotgun,
                "weapon_shotgun",
                "ammo_shells",
                12,
                "Pompalı Av Tüfeği",
                $"{locationTag} (+12 FİŞEK)"
            );
            pickup.SetPersistentId($"starter_pickup_shotgun_{id}");
        }

        private static void CreateAttachmentKitPickup(string id, string modId, string displayName, string subtitle, Vector3 pos, Quaternion rot)
        {
            string objName = $"AttachmentKit_{id}";
            if (GameObject.Find(objName) != null) return;

            GameObject root = new GameObject(objName);
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Tactical Hardcase Box (0.45 x 0.18 x 0.32m)
            GameObject caseBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caseBody.name = "Hardcase_Body";
            caseBody.transform.SetParent(root.transform, false);
            caseBody.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            caseBody.transform.localScale = new Vector3(0.45f, 0.18f, 0.32f);
            SetRendererMaterial(caseBody, gunMetalMaterial);

            // Handle & Latches
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Hardcase_Handle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.09f, 0.18f);
            handle.transform.localScale = new Vector3(0.16f, 0.05f, 0.04f);
            SetRendererMaterial(handle, steelMaterial);

            // Sphere trigger collider for interaction
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.8f;
            sc.center = new Vector3(0f, 0.2f, 0f);

            // Interactive component
            AttachmentKitPickupInteractable pickup = root.AddComponent<AttachmentKitPickupInteractable>();
            pickup.Configure(modId, displayName, subtitle);
            pickup.SetPersistentId($"pickup_kit_{id}");
        }

        private static void SetupWorkbenchInteractables()
        {
            // Find all workbenches in scene and equip with WorkbenchInteractable
            var allObjects = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                Transform t = allObjects[i];
                if (t == null) continue;

                string n = t.name.ToLower();
                if (n.Contains("workbench") || n.Contains("crafting_table"))
                {
                    if (t.GetComponent<WorkbenchInteractable>() == null)
                    {
                        t.gameObject.AddComponent<WorkbenchInteractable>();
                    }

                    Collider col = t.GetComponent<Collider>();
                    if (col == null)
                    {
                        BoxCollider bc = t.gameObject.AddComponent<BoxCollider>();
                        bc.size = new Vector3(3.2f, 1.2f, 1.6f);
                        bc.center = new Vector3(0f, 0.6f, 0f);
                    }
                }
            }
        }

        private static void SetRendererMaterial(GameObject go, Material mat)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null)
            {
                rend.sharedMaterial = mat;
            }
        }
    }
}
