using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Narrative;
using ProjectSurvival.Survival;

namespace ProjectSurvival.EditorTools
{
    public static class Step4A_UndergroundBunkerSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Setup Step 4A Underground Bunker", false, 70)]
        public static void SetupMenuItem()
        {
            SetupStep4A();
        }

        public static void SetupStep4A()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> EXECUTING STEP 4A SETUP: SUBTERRANEAN RESEARCH BUNKER DUNGEON <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step4A_Setup] Could not open scene at {ScenePath}");
                return;
            }

            // Load Materials
            Material matConcrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Concrete_Spalling.mat");
            Material matRust = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_RustMetal.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Vehicle_Rust.mat");
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Glass_DustyReflective.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Vehicle_Glass.mat");
            Material matDrumRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Fuel_Drum_Red.mat");
            Material matShadow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_ContactShadow.mat");

            // Load Zombie Prefabs
            GameObject standardZombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab");
            GameObject runnerZombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab");
            GameObject tankZombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab");

            // Remove existing bunker root if any
            GameObject oldRoot = GameObject.Find("--- SUBTERRANEAN RESEARCH BUNKER ---");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            GameObject bunkerRoot = new GameObject("--- SUBTERRANEAN RESEARCH BUNKER ---");

            // Director component on master root
            UndergroundBunkerDirector director = bunkerRoot.AddComponent<UndergroundBunkerDirector>();

            // Surface Entrance Coordinates
            Vector3 surfacePos = new Vector3(-235f, 0.45f, 255f);
            float floorY = -12.0f;
            float ceilingHeight = 3.6f;

            // =========================================================================
            // SURFACE ENTRANCE STRUCTURE (Hatch & Stairwell Enclosure)
            // =========================================================================
            GameObject surfaceEntrance = new GameObject("Surface_Bunker_Entrance");
            surfaceEntrance.transform.SetParent(bunkerRoot.transform, false);
            surfaceEntrance.transform.position = surfacePos;

            // Concrete Pillbox Entrance Block
            GameObject pillbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillbox.name = "Entrance_Concrete_Pillbox";
            pillbox.transform.SetParent(surfaceEntrance.transform, false);
            pillbox.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            pillbox.transform.localScale = new Vector3(4.5f, 2.8f, 5.0f);
            if (matConcrete != null) pillbox.GetComponent<Renderer>().sharedMaterial = matConcrete;

            // Entrance Doorframe Hatch
            GameObject hatchDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hatchDoor.name = "Bunker_Heavy_Hatch";
            hatchDoor.transform.SetParent(surfaceEntrance.transform, false);
            hatchDoor.transform.localPosition = new Vector3(0f, 1.1f, -2.4f);
            hatchDoor.transform.localScale = new Vector3(1.8f, 2.2f, 0.35f);
            if (matRust != null) hatchDoor.GetComponent<Renderer>().sharedMaterial = matRust;

            // Stairwell Shaft connecting Surface (y=0.45) to Subterranean Floor (y=-12.0)
            GameObject stairShaft = new GameObject("Stairwell_Shaft");
            stairShaft.transform.SetParent(bunkerRoot.transform, false);
            stairShaft.transform.position = new Vector3(-235f, -5.75f, 260f);

            // Stepped Ramp for smooth continuous NavMesh walkability
            GameObject stairRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stairRamp.name = "Stairwell_Walkable_Ramp";
            stairRamp.transform.SetParent(stairShaft.transform, false);
            stairRamp.transform.localPosition = Vector3.zero;
            stairRamp.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            stairRamp.transform.localScale = new Vector3(3.2f, 0.4f, 20.0f);
            if (matConcrete != null) stairRamp.GetComponent<Renderer>().sharedMaterial = matConcrete;
            GameObjectUtility.SetStaticEditorFlags(stairRamp, StaticEditorFlags.NavigationStatic);

            // =========================================================================
            // SUBTERRANEAN CHAMBERS HIERARCHY ROOT (y = -12.0m)
            // =========================================================================
            GameObject subterraneanRoot = new GameObject("Subterranean_Chambers_Root");
            subterraneanRoot.transform.SetParent(bunkerRoot.transform, false);

            List<Light> bunkerFluorescentLights = new List<Light>();

            // -------------------------------------------------------------------------
            // CHAMBER 1: Entry Airlock & Reception (Giriş Güvenlik Holü)
            // Center: (-235, -12, 255), 10m x 12m
            // -------------------------------------------------------------------------
            GameObject ch1 = BuildChamber(subterraneanRoot.transform, "Chamber1_EntryAirlock",
                new Vector3(-235f, floorY, 255f), new Vector3(10f, ceilingHeight, 12f), matConcrete);

            // Emergency Airlock Light (Red Flickering, Shadows: None)
            GameObject airlockLightObj = new GameObject("Airlock_Emergency_RedLight");
            airlockLightObj.transform.SetParent(ch1.transform, false);
            airlockLightObj.transform.localPosition = new Vector3(0f, ceilingHeight - 0.3f, 0f);
            Light airlockLight = airlockLightObj.AddComponent<Light>();
            airlockLight.type = LightType.Point;
            airlockLight.color = new Color(0.95f, 0.15f, 0.12f);
            airlockLight.range = 8.5f;
            airlockLight.intensity = 1.6f;
            airlockLight.shadows = LightShadows.None;

            // Supply Locker (Contains Battery + Bandage)
            GameObject locker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locker.name = "Airlock_Supply_Locker";
            locker.transform.SetParent(ch1.transform, false);
            locker.transform.localPosition = new Vector3(-3.8f, 1.1f, 2.5f);
            locker.transform.localScale = new Vector3(0.85f, 2.2f, 1.4f);
            if (matRust != null) locker.GetComponent<Renderer>().sharedMaterial = matRust;
            ThematicLootContainerInteractable lockerLoot = locker.AddComponent<ThematicLootContainerInteractable>();
            lockerLoot.SetContainerType(ThematicLootContainerType.HighVoltageLocker);
            lockerLoot.SetObjectDetails("Acil Durum Malzeme Dolabı", "ARANABİLİR");

            // -------------------------------------------------------------------------
            // CHAMBER 2: Auxiliary Power & Generator Room (Yedek Jeneratör Odası)
            // Center: (-252, -12, 255), 12m x 10m
            // -------------------------------------------------------------------------
            GameObject ch2 = BuildChamber(subterraneanRoot.transform, "Chamber2_GeneratorRoom",
                new Vector3(-252f, floorY, 255f), new Vector3(12f, ceilingHeight, 10f), matConcrete);

            // Corridor Ch1 -> Ch2
            BuildCorridor(subterraneanRoot.transform, "Corridor_Ch1_Ch2",
                new Vector3(-243.5f, floorY, 255f), new Vector3(7f, ceilingHeight, 3.2f), matConcrete);

            // Heavy Industrial Diesel Backup Generator
            GameObject generatorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            generatorObj.name = "Bunker_Backup_Generator";
            generatorObj.transform.SetParent(ch2.transform, false);
            generatorObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            generatorObj.transform.localScale = new Vector3(2.4f, 2.5f, 3.2f);
            if (matRust != null) generatorObj.GetComponent<Renderer>().sharedMaterial = matRust;

            // AudioSource on Generator
            AudioSource genAudio = generatorObj.AddComponent<AudioSource>();
            genAudio.spatialBlend = 1.0f;
            genAudio.minDistance = 2.0f;
            genAudio.maxDistance = 25.0f;
            genAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            genAudio.playOnAwake = false;
            genAudio.loop = true;

            // NavMeshObstacle with carving
            NavMeshObstacle genNav = generatorObj.AddComponent<NavMeshObstacle>();
            genNav.shape = NavMeshObstacleShape.Box;
            genNav.center = Vector3.zero;
            genNav.size = new Vector3(2.5f, 2.5f, 3.3f);
            genNav.carving = true;
            genNav.carveOnlyStationary = false;

            BunkerGeneratorInteractable generatorComp = generatorObj.AddComponent<BunkerGeneratorInteractable>();
            SerializedObject genSo = new SerializedObject(generatorComp);
            genSo.FindProperty("generatorAudioSource").objectReferenceValue = genAudio;
            genSo.ApplyModifiedPropertiesWithoutUndo();

            // Generator Room Light
            Light genLight = CreateCeilingLight(ch2.transform, new Vector3(0f, ceilingHeight - 0.3f, 0f));
            bunkerFluorescentLights.Add(genLight);

            // Wall Rack holding Dolu Yakıt Bidonu (item_fuel_can)
            GameObject rackFuel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rackFuel.name = "WallRack_Backup_FuelCan";
            rackFuel.transform.SetParent(ch2.transform, false);
            rackFuel.transform.localPosition = new Vector3(4.5f, 0.6f, -2.8f);
            rackFuel.transform.localScale = new Vector3(0.5f, 0.65f, 0.45f);
            if (matDrumRed != null) rackFuel.GetComponent<Renderer>().sharedMaterial = matDrumRed;
            SphereCollider rackCol = rackFuel.AddComponent<SphereCollider>();
            rackCol.isTrigger = true;
            rackCol.radius = 1.35f;
            rackFuel.AddComponent<BunkerBackupFuelInteractable>();

            // -------------------------------------------------------------------------
            // CHAMBER 3: Dark Archive & Research Offices (Karanlık Arşiv Koridoru)
            // Center: (-235, -12, 235), 10m x 16m
            // -------------------------------------------------------------------------
            GameObject ch3 = BuildChamber(subterraneanRoot.transform, "Chamber3_DarkArchive",
                new Vector3(-235f, floorY, 235f), new Vector3(10f, ceilingHeight, 16f), matConcrete);

            // Corridor Ch1 -> Ch3
            BuildCorridor(subterraneanRoot.transform, "Corridor_Ch1_Ch3",
                new Vector3(-235f, floorY, 246f), new Vector3(3.2f, ceilingHeight, 6f), matConcrete);

            // Head Virologist's Desk holding Level-2 Keycard
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Virologist_Research_Desk";
            desk.transform.SetParent(ch3.transform, false);
            desk.transform.localPosition = new Vector3(2.5f, 0.45f, 3.0f);
            desk.transform.localScale = new Vector3(2.2f, 0.9f, 1.2f);
            if (matRust != null) desk.GetComponent<Renderer>().sharedMaterial = matRust;

            // Keycard Pickup on Desk
            GameObject keycardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keycardObj.name = "Pickup_item_bunker_keycard";
            keycardObj.transform.SetParent(desk.transform, false);
            keycardObj.transform.localPosition = new Vector3(0.2f, 0.55f, 0f);
            keycardObj.transform.localScale = new Vector3(0.18f, 0.05f, 0.28f);
            if (matDrumRed != null) keycardObj.GetComponent<Renderer>().sharedMaterial = matDrumRed;
            SphereCollider kcCol = keycardObj.AddComponent<SphereCollider>();
            kcCol.isTrigger = true;
            kcCol.radius = 1.4f;
            keycardObj.AddComponent<BunkerKeycardPickupInteractable>();

            // Chamber 3 Ceiling Lights
            bunkerFluorescentLights.Add(CreateCeilingLight(ch3.transform, new Vector3(0f, ceilingHeight - 0.3f, 4.0f)));
            bunkerFluorescentLights.Add(CreateCeilingLight(ch3.transform, new Vector3(0f, ceilingHeight - 0.3f, -4.0f)));

            // 2 Lab Zombies in Chamber 3
            SpawnZombie(standardZombiePrefab, ch3.transform, new Vector3(-236f, floorY + 0.1f, 238f), "Lab_Zombie_01");
            SpawnZombie(standardZombiePrefab, ch3.transform, new Vector3(-233f, floorY + 0.1f, 231f), "Lab_Zombie_02");

            // -------------------------------------------------------------------------
            // CHAMBER 4: Bio-Containment Lab (Biyolojik Deney Laboratuvarı)
            // Center: (-235, -12, 212), 14m x 16m
            // -------------------------------------------------------------------------
            GameObject ch4 = BuildChamber(subterraneanRoot.transform, "Chamber4_BioContainmentLab",
                new Vector3(-235f, floorY, 212f), new Vector3(14f, ceilingHeight, 16f), matConcrete);

            // Corridor Ch3 -> Ch4 with KeycardSecurityDoor Blast Door
            GameObject blastDoorFrame = BuildCorridor(subterraneanRoot.transform, "Corridor_Ch3_Ch4_BlastDoor",
                new Vector3(-235f, floorY, 223.5f), new Vector3(3.4f, ceilingHeight, 7f), matConcrete);

            // KeycardSecurityDoor Motorized Blast Door
            GameObject doorRoot = new GameObject("Keycard_Security_BlastDoor");
            doorRoot.transform.SetParent(blastDoorFrame.transform, false);
            doorRoot.transform.localPosition = Vector3.zero;

            GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "BlastDoor_SteelLeaf";
            doorLeaf.transform.SetParent(doorRoot.transform, false);
            doorLeaf.transform.localPosition = new Vector3(0f, ceilingHeight * 0.5f, 0f);
            doorLeaf.transform.localScale = new Vector3(2.6f, ceilingHeight, 0.45f);
            if (matRust != null) doorLeaf.GetComponent<Renderer>().sharedMaterial = matRust;
            UnityEngine.Object.DestroyImmediate(doorLeaf.GetComponent<Collider>());

            BoxCollider doorTriggerCol = doorRoot.AddComponent<BoxCollider>();
            doorTriggerCol.center = new Vector3(0f, ceilingHeight * 0.5f, 0f);
            doorTriggerCol.size = new Vector3(3.0f, ceilingHeight, 2.0f);

            // AudioSource on Security Door
            AudioSource doorAudio = doorRoot.AddComponent<AudioSource>();
            doorAudio.spatialBlend = 1.0f;
            doorAudio.minDistance = 2.0f;
            doorAudio.maxDistance = 22.0f;
            doorAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            doorAudio.playOnAwake = false;

            NavMeshObstacle doorNav = doorRoot.AddComponent<NavMeshObstacle>();
            doorNav.shape = NavMeshObstacleShape.Box;
            doorNav.center = new Vector3(0f, ceilingHeight * 0.5f, 0f);
            doorNav.size = new Vector3(2.8f, ceilingHeight, 0.6f);
            doorNav.carving = true;

            KeycardSecurityDoor securityDoorComp = doorRoot.AddComponent<KeycardSecurityDoor>();
            SerializedObject doorSo = new SerializedObject(securityDoorComp);
            doorSo.FindProperty("doorLeafTransform").objectReferenceValue = doorLeaf.transform;
            doorSo.FindProperty("doorAudioSource").objectReferenceValue = doorAudio;
            doorSo.ApplyModifiedPropertiesWithoutUndo();

            // Cryo-pods inside Chamber 4
            Vector3[] podOffsets = new Vector3[] { new Vector3(-4.5f, 1.4f, 2f), new Vector3(-4.5f, 1.4f, -2f), new Vector3(4.5f, 1.4f, 0f) };
            for (int p = 0; p < podOffsets.Length; p++)
            {
                GameObject pod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pod.name = $"Cryo_Pod_{p + 1}";
                pod.transform.SetParent(ch4.transform, false);
                pod.transform.localPosition = podOffsets[p];
                pod.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);
                if (matGlass != null) pod.GetComponent<Renderer>().sharedMaterial = matGlass;
            }

            // Chamber 4 Ceiling Lights
            bunkerFluorescentLights.Add(CreateCeilingLight(ch4.transform, new Vector3(-3.5f, ceilingHeight - 0.3f, 0f)));
            bunkerFluorescentLights.Add(CreateCeilingLight(ch4.transform, new Vector3(3.5f, ceilingHeight - 0.3f, 0f)));

            // 3 Mutated Lab Specimens in Chamber 4 (2 Runners + 1 Tank)
            SpawnZombie(runnerZombiePrefab ?? standardZombiePrefab, ch4.transform, new Vector3(-238f, floorY + 0.1f, 214f), "Specimen_Runner_01");
            SpawnZombie(runnerZombiePrefab ?? standardZombiePrefab, ch4.transform, new Vector3(-232f, floorY + 0.1f, 214f), "Specimen_Runner_02");
            SpawnZombie(tankZombiePrefab ?? standardZombiePrefab, ch4.transform, new Vector3(-235f, floorY + 0.1f, 208f), "Specimen_Lab_Tank");

            // -------------------------------------------------------------------------
            // CHAMBER 5: Antiviral Synthesis Vault (Antiviral Çekirdek Kasası)
            // Center: (-235, -12, 190), 12m x 12m
            // -------------------------------------------------------------------------
            GameObject ch5 = BuildChamber(subterraneanRoot.transform, "Chamber5_SynthesisVault",
                new Vector3(-235f, floorY, 190f), new Vector3(12f, ceilingHeight, 12f), matConcrete);

            // Corridor Ch4 -> Ch5
            BuildCorridor(subterraneanRoot.transform, "Corridor_Ch4_Ch5",
                new Vector3(-235f, floorY, 200.5f), new Vector3(3.2f, ceilingHeight, 7f), matConcrete);

            // Antiviral Synthesis Core Terminal (Story Milestone Objective 3B)
            GameObject terminalObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terminalObj.name = "Antiviral_Synthesis_Terminal_bunker";
            terminalObj.transform.SetParent(ch5.transform, false);
            terminalObj.transform.localPosition = new Vector3(0f, 0.9f, 2.5f);
            terminalObj.transform.localScale = new Vector3(1.8f, 1.8f, 1.2f);
            if (matRust != null) terminalObj.GetComponent<Renderer>().sharedMaterial = matRust;
            StoryTerminalInteractable terminalComp = terminalObj.AddComponent<StoryTerminalInteractable>();
            terminalComp.ConfigureTerminal("terminal_bunker", "Antiviral Sentez Çekirdek Konsolu", "Dr. Kaya Araştırma Veritabanı", "ANTİVİRAL VERİSİNİ İNDİR", 3);

            // Vault Chest (Antiviral Chest with High-Tier Loot)
            GameObject chestObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestObj.name = "Bunker_Vault_Chest";
            chestObj.transform.SetParent(ch5.transform, false);
            chestObj.transform.localPosition = new Vector3(0f, 0.5f, -2.5f);
            chestObj.transform.localScale = new Vector3(1.8f, 1.0f, 1.1f);
            if (matRust != null) chestObj.GetComponent<Renderer>().sharedMaterial = matRust;
            BunkerVaultChestInteractable chestComp = chestObj.AddComponent<BunkerVaultChestInteractable>();

            // Chamber 5 Ceiling Light
            bunkerFluorescentLights.Add(CreateCeilingLight(ch5.transform, new Vector3(0f, ceilingHeight - 0.3f, 0f)));

            // -------------------------------------------------------------------------
            // CONNECT OFFMESHLINK FOR STAIRWELL TRANSITION
            // -------------------------------------------------------------------------
            GameObject navLinkGo = new GameObject("OffMeshLink_Surface_To_Bunker");
            navLinkGo.transform.SetParent(bunkerRoot.transform, false);
            navLinkGo.transform.position = new Vector3(-235f, 0f, 256f);
            OffMeshLink link = navLinkGo.AddComponent<OffMeshLink>();
            link.startTransform = surfaceEntrance.transform;
            link.endTransform = ch1.transform;
            link.biDirectional = true;
            link.activated = true;

            // Configure Director References
            director.ConfigureReferences(
                subterraneanRoot,
                airlockLightObj,
                bunkerFluorescentLights,
                generatorComp,
                securityDoorComp,
                chestComp,
                terminalComp
            );

            // Bake NavMesh
            Debug.Log("[Step4A_Setup] Rebuilding NavMesh to include all 5 subterranean chambers...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[Step4A_Setup] NavMesh rebuild complete.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Step4A_Setup] Successfully created 5-Chamber Subterranean Research Bunker Dungeon!");
        }

        private static GameObject BuildChamber(Transform parent, string name, Vector3 center, Vector3 size, Material wallMat)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent, false);
            room.transform.position = center;

            // Floor Plane
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(size.x, 0.2f, size.z);
            if (wallMat != null) floor.GetComponent<Renderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

            // Solid Ceiling (Blocks rain/wetness)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Solid_Ceiling";
            ceiling.transform.SetParent(room.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, size.y + 0.1f, 0f);
            ceiling.transform.localScale = new Vector3(size.x, 0.2f, size.z);
            if (wallMat != null) ceiling.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Perimeter Walls
            CreateWall(room.transform, "Wall_North", new Vector3(0f, size.y * 0.5f, size.z * 0.5f), new Vector3(size.x, size.y, 0.3f), wallMat);
            CreateWall(room.transform, "Wall_South", new Vector3(0f, size.y * 0.5f, -size.z * 0.5f), new Vector3(size.x, size.y, 0.3f), wallMat);
            CreateWall(room.transform, "Wall_West", new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.3f, size.y, size.z), wallMat);
            CreateWall(room.transform, "Wall_East", new Vector3(size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.3f, size.y, size.z), wallMat);

            return room;
        }

        private static GameObject BuildCorridor(Transform parent, string name, Vector3 center, Vector3 size, Material wallMat)
        {
            GameObject corridor = new GameObject(name);
            corridor.transform.SetParent(parent, false);
            corridor.transform.position = center;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(corridor.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(size.x, 0.2f, size.z);
            if (wallMat != null) floor.GetComponent<Renderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Solid_Ceiling";
            ceiling.transform.SetParent(corridor.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, size.y + 0.1f, 0f);
            ceiling.transform.localScale = new Vector3(size.x, 0.2f, size.z);
            if (wallMat != null) ceiling.GetComponent<Renderer>().sharedMaterial = wallMat;

            return corridor;
        }

        private static void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            if (mat != null) wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Light CreateCeilingLight(Transform parent, Vector3 localPos)
        {
            GameObject lightObj = new GameObject("Ceiling_Fluorescent_Light");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.localPosition = localPos;

            Light lt = lightObj.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.range = 10f;
            lt.intensity = 1.7f;
            lt.color = new Color(0.95f, 0.92f, 0.85f); // Warm-white fluorescent
            lt.shadows = LightShadows.None; // 60 FPS mobile limit
            lt.enabled = false; // Initially unpowered until generator starts
            return lt;
        }

        private static void SpawnZombie(GameObject prefab, Transform parent, Vector3 worldPos, string name)
        {
            GameObject zObj;
            if (prefab != null)
            {
                zObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                zObj.transform.position = worldPos;
            }
            else
            {
                zObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                zObj.transform.SetParent(parent, false);
                zObj.transform.position = worldPos;
            }
            zObj.name = name;

            NavMeshAgent agent = zObj.GetComponent<NavMeshAgent>();
            if (agent == null) agent = zObj.AddComponent<NavMeshAgent>();
            agent.speed = 2.4f;

            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                zObj.transform.position = hit.position;
            }
        }
    }
}
