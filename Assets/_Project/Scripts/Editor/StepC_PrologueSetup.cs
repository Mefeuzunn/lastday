using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.UI;
using ProjectSurvival.Environment;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Step C: Scene Setup and World Injection for Starter House Scavenging,
    /// Front-Door Exit Sightline, Outdoor Starter Weapons, and 4-Stage Prologue Loop.
    /// </summary>
    public static class StepC_PrologueSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Step C Prologue & Starter House", false, 42)]
        public static void SetupStepC()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[StepC_Setup] Starting Step C Starter House & Prologue Setup...");

            // 1. Setup Starter House Root & Architectural Structure
            GameObject starterHouse = SetupStarterHouse();

            // 2. Setup Indoor Scavenging Containers
            SetupIndoorContainers(starterHouse);

            // 3. Setup Front Door & Exit Threshold
            SetupFrontDoor(starterHouse);

            // 4. Setup Outdoor Starter Weapons Along Exit Sightline
            SetupOutdoorWeapons();

            // 5. Setup Perimeter Safety & First Street Zombie
            SetupPerimeterAndFirstZombie();

            // 6. Setup Prologue Director and Smartphone UI
            SetupPrologueDirectorAndUI();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StepC_Setup] Step C Starter House & Prologue Setup completed successfully!");
        }

        private static GameObject SetupStarterHouse()
        {
            GameObject houseRoot = GameObject.Find("StarterHouse_Safehome");
            if (houseRoot == null)
            {
                houseRoot = new GameObject("StarterHouse_Safehome");
                houseRoot.transform.position = Vector3.zero;

                GameObject sectorCenter = GameObject.Find("Sector_0_0_Center");
                if (sectorCenter != null)
                {
                    houseRoot.transform.SetParent(sectorCenter.transform, true);
                }
            }

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Cover_Wood.mat") ?? woodMat;

            // Ensure Interior Floor
            Transform existingFloor = houseRoot.transform.Find("House_Floor") ?? houseRoot.transform.Find("Architecture/House_Floor_Main");
            if (existingFloor == null)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "House_Floor";
                floor.transform.SetParent(houseRoot.transform, false);
                floor.transform.localPosition = new Vector3(0f, -0.05f, 0.25f);
                floor.transform.localScale = new Vector3(7.0f, 0.1f, 7.5f);
                if (woodMat != null) floor.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // Interior Warm Ambient Lantern
            Transform existingLight = houseRoot.transform.Find("Interior_WarmLight");
            if (existingLight == null)
            {
                GameObject lightObj = new GameObject("Interior_WarmLight");
                lightObj.transform.SetParent(houseRoot.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1.0f, 0.88f, 0.68f);
                light.intensity = 1.8f;
                light.range = 8.5f;
            }

            return houseRoot;
        }

        private static void SetupIndoorContainers(GameObject houseRoot)
        {
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");

            Transform kitchen = houseRoot.transform.Find("Room_Kitchen") ?? houseRoot.transform;
            Transform bathroom = houseRoot.transform.Find("Room_Bathroom") ?? houseRoot.transform;
            Transform bedroom = houseRoot.transform.Find("Room_Bedroom") ?? houseRoot.transform;

            // 1. Kitchen Pantry Cabinet: 2x Canned Food, 1x Clean Cloth
            CreateOrUpdateContainer(
                kitchen,
                "Furniture_Starter_KitchenCabinet",
                "Mutfak Dolabı",
                "ERZAK DOLABI",
                FurnitureType.KitchenCupboard,
                new Vector3(-2.2f, 0.85f, 1.8f),
                new Vector3(0.85f, 0.9f, 0.55f),
                woodMat,
                new (string, int)[] { ("item_canned_food", 2), ("item_cloth", 1) }
            );

            // 2. Kitchen Refrigerator: 2x Water Bottle, 1x Canned Food
            CreateOrUpdateContainer(
                kitchen,
                "Furniture_Starter_Refrigerator",
                "Buzdolabı",
                "SOĞUTUCU",
                FurnitureType.Refrigerator,
                new Vector3(-2.2f, 0.95f, 3.2f),
                new Vector3(0.75f, 1.6f, 0.75f),
                woodMat,
                new (string, int)[] { ("item_water_bottle", 2), ("item_canned_food", 1) }
            );

            // 3. Bathroom Medicine Cabinet: 2x Bandage, 1x Painkillers
            CreateOrUpdateContainer(
                bathroom,
                "Furniture_Starter_MedicineCabinet",
                "Banyo Ecza Dolabı",
                "İLK YARDIM",
                FurnitureType.MedicalCabinet,
                new Vector3(5.1f, 1.4f, -1.8f),
                new Vector3(0.65f, 0.8f, 0.35f),
                woodMat,
                new (string, int)[] { ("item_bandage", 2), ("item_painkillers", 1) }
            );

            // 4. Bedroom Wardrobe / Closet: 1x Battery, 1x Distraction Bottle
            CreateOrUpdateContainer(
                bedroom,
                "Furniture_Starter_Wardrobe",
                "Yatak Odası Dolabı",
                "GARDROP VE TEÇHİZAT",
                FurnitureType.LockerOrWardrobe,
                new Vector3(-4.5f, 1.05f, -2.5f),
                new Vector3(0.9f, 1.8f, 0.65f),
                woodMat,
                new (string, int)[] { ("item_battery", 1), ("item_distraction_bottle", 1) }
            );

            Debug.Log("[StepC_Setup] Stocked 4 indoor containers in StarterHouse_Safehome with initial rations.");
        }

        private static void CreateOrUpdateContainer(
            Transform parent,
            string goName,
            string title,
            string subtitle,
            FurnitureType fType,
            Vector3 localPos,
            Vector3 localScale,
            Material mat,
            (string id, int count)[] loot)
        {
            Transform existing = parent.Find(goName);
            if (existing == null)
            {
                // Also search directly under StarterHouse_Safehome if re-parenting into room
                GameObject house = GameObject.Find("StarterHouse_Safehome");
                if (house != null) existing = house.transform.Find(goName);
            }

            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = goName;
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            if (mat != null)
            {
                var ren = go.GetComponent<MeshRenderer>();
                if (ren != null) ren.sharedMaterial = mat;
            }

            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();

            var furn = go.GetComponent<FurnitureInteractable>();
            if (furn == null) furn = go.AddComponent<FurnitureInteractable>();

            furn.SetObjectDetails(title, subtitle);
            furn.SetPersistentId($"starter_container_{goName.ToLower()}");
            furn.SetCustomLoot(loot);
        }

        private static void SetupFrontDoor(GameObject houseRoot)
        {
            Transform existingDoor = houseRoot.transform.Find("Door_Starter_Front") ?? houseRoot.transform.Find("Room_LivingRoom/Door_Starter_Front");
            GameObject doorObj;
            if (existingDoor != null)
            {
                doorObj = existingDoor.gameObject;
                doorObj.transform.SetParent(houseRoot.transform, true);
            }
            else
            {
                doorObj = new GameObject("Door_Starter_Front");
                doorObj.transform.SetParent(houseRoot.transform, false);
                doorObj.transform.localPosition = new Vector3(0f, 0f, 4.0f);
            }

            GameObjectUtility.SetStaticEditorFlags(doorObj, 0);

            // Door Leaf & Hinge
            Transform leafTf = doorObj.transform.Find("Door_Leaf");
            GameObject leafObj;
            if (leafTf != null)
            {
                leafObj = leafTf.gameObject;
            }
            else
            {
                leafObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leafObj.name = "Door_Leaf";
                leafObj.transform.SetParent(doorObj.transform, false);
                leafObj.transform.localPosition = new Vector3(0.5f, 1.05f, 0f);
                leafObj.transform.localScale = new Vector3(1.0f, 2.1f, 0.08f);

                Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Door_Wood.mat") ??
                                   AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wood_Timber.mat");
                if (doorMat != null) leafObj.GetComponent<MeshRenderer>().sharedMaterial = doorMat;
            }

            GameObjectUtility.SetStaticEditorFlags(leafObj, 0);
            var leafCol = leafObj.GetComponent<Collider>();
            if (leafCol != null) leafCol.isTrigger = true;

            // Ensure InteractiveDoor component
            var idoor = doorObj.GetComponent<InteractiveDoor>();
            if (idoor == null) idoor = doorObj.AddComponent<InteractiveDoor>();

            var so = new SerializedObject(idoor);
            so.FindProperty("doorHinge").objectReferenceValue = leafObj.transform;
            so.FindProperty("doorName").stringValue = "Ön Çıkış Kapısı";
            so.ApplyModifiedProperties();

            // Trigger BoxCollider for proximity interaction
            BoxCollider bc = doorObj.GetComponent<BoxCollider>();
            if (bc == null) bc = doorObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = new Vector3(0.5f, 1.0f, 0f);
            bc.size = new Vector3(2.4f, 2.4f, 2.4f);

            Debug.Log("[StepC_Setup] Interactive Front Door configured at (0, 0, 4.0).");
        }

        private static void SetupOutdoorWeapons()
        {
            // Ensure StarterArsenalPlacer places both weapons
            StarterArsenalPlacer.PlaceAllStarterWeapons();

            // 1. Weapon 1: Fire Axe at (0, 0, 6.5)
            GameObject axeObj = GameObject.Find("WeaponPickup_Starter_FireAxe");
            if (axeObj != null)
            {
                SphereCollider sc = axeObj.GetComponent<SphereCollider>();
                if (sc == null) sc = axeObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.75f;
                sc.center = new Vector3(0f, 0.6f, 0f);

                var pickup = axeObj.GetComponent<WeaponPickupInteractable>();
                if (pickup != null)
                {
                    pickup.Configure(WeaponType.Axe, "weapon_axe", "", 0, "Yangın Baltası", "YAKIN DÖVÜŞ");
                    pickup.SetPersistentId("starter_pickup_axe_boulevard");
                }
            }

            // 2. Weapon 2: 9mm Pistol on Police Cruiser at (2.2, 1.05, 15.5)
            GameObject pistolObj = GameObject.Find("WeaponPickup_Starter_Pistol");
            if (pistolObj != null)
            {
                SphereCollider sc = pistolObj.GetComponent<SphereCollider>();
                if (sc == null) sc = pistolObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.75f;
                sc.center = new Vector3(0f, 0.2f, 0f);

                var pickup = pistolObj.GetComponent<WeaponPickupInteractable>();
                if (pickup != null)
                {
                    pickup.Configure(WeaponType.Pistol, "weapon_pistol", "ammo_9mm", 24, "9mm Polis Tabancası", "ATEŞLİ SİLAH (+24 MERMİ)");
                    pickup.SetPersistentId("starter_pickup_pistol_cruiser");
                }
            }

            Debug.Log("[StepC_Setup] Outdoor starter weapons verified with 1.75m trigger colliders.");
        }

        private static void SetupPerimeterAndFirstZombie()
        {
            // 1. Ensure SpawnProtectionController on managers
            GameObject mgrRoot = GameObject.Find("--- MANAGERS ---") ?? GameObject.Find("--- STORY & RADIO MANAGERS ---");
            if (mgrRoot != null)
            {
                var spc = mgrRoot.GetComponent<SpawnProtectionController>();
                if (spc == null) spc = mgrRoot.AddComponent<SpawnProtectionController>();
                var so = new SerializedObject(spc);
                so.FindProperty("spawnSafeRadius").floatValue = 45.0f;
                so.FindProperty("minRelocationDistance").floatValue = 52.0f;
                so.FindProperty("maxRelocationDistance").floatValue = 95.0f;
                so.ApplyModifiedProperties();
            }

            // 2. Relocate all scene zombies currently within 52m of spawn to 55m - 75m
            ZombieBrain[] allZombies = UnityEngine.Object.FindObjectsByType<ZombieBrain>(FindObjectsSortMode.None);
            Vector3 center = Vector3.zero;
            int relocated = 0;
            foreach (var z in allZombies)
            {
                if (z == null) continue;
                if (z.gameObject.name == "Zombie_LoneStreet_Boulevard_FirstTarget") continue;

                float dist = Vector3.Distance(z.transform.position, center);
                if (dist < 52.0f)
                {
                    Vector3 dir = (z.transform.position - center);
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.1f) dir = new Vector3(1f, 0f, 0.4f);
                    dir.Normalize();

                    float newDist = UnityEngine.Random.Range(55.0f, 75.0f);
                    Vector3 newPos = center + dir * newDist;
                    newPos.y = z.transform.position.y;
                    z.transform.position = newPos;
                    relocated++;
                }
            }
            Debug.Log($"[StepC_Setup] Relocated {relocated} initial scene zombies outside 52m.");

            // 3. Verify or place first lone street zombie at (0, 0.5, 58.0) down the street
            GameObject loneZombie = GameObject.Find("Zombie_LoneStreet_Boulevard_FirstTarget");
            if (loneZombie == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab");
                if (prefab != null)
                {
                    loneZombie = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    loneZombie.name = "Zombie_LoneStreet_Boulevard_FirstTarget";
                    loneZombie.transform.position = new Vector3(0f, 0.5f, 58.0f);
                    loneZombie.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing away from player house

                    GameObject zombieParent = GameObject.Find("--- ZOMBIES ---");
                    if (zombieParent != null)
                    {
                        loneZombie.transform.SetParent(zombieParent.transform, true);
                    }
                }
            }
            else
            {
                loneZombie.transform.position = new Vector3(0f, 0.5f, 58.0f);
                loneZombie.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            Debug.Log("[StepC_Setup] 45m safe perimeter and first street zombie at 58m confirmed.");
        }

        private static void SetupPrologueDirectorAndUI()
        {
            // 1. StarterPrologueDirector under --- MANAGERS ---
            GameObject mgrRoot = GameObject.Find("--- MANAGERS ---") ?? GameObject.Find("--- STORY & RADIO MANAGERS ---");
            if (mgrRoot != null)
            {
                if (mgrRoot.GetComponent<StarterPrologueDirector>() == null)
                {
                    mgrRoot.AddComponent<StarterPrologueDirector>();
                }
            }
            else
            {
                if (UnityEngine.Object.FindAnyObjectByType<StarterPrologueDirector>() == null)
                {
                    GameObject go = new GameObject("StarterPrologueDirector");
                    go.AddComponent<StarterPrologueDirector>();
                }
            }

            // 2. SmartphoneScreenUI under Canvas
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                if (canvas.GetComponentInChildren<SmartphoneScreenUI>(true) == null)
                {
                    GameObject phoneObj = new GameObject("SmartphoneScreenUI");
                    phoneObj.transform.SetParent(canvas.transform, false);
                    phoneObj.AddComponent<SmartphoneScreenUI>();
                }
            }

            Debug.Log("[StepC_Setup] StarterPrologueDirector and SmartphoneScreenUI verified in scene hierarchy.");
        }
#endif
    }
}
