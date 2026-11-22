using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UnityEngine.Rendering;
using ProjectSurvival.AI;
using ProjectSurvival.Animation;
using ProjectSurvival.Core;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 2 (Step 2A): Raider Outpost Squad & Visual Rig Setup.
    /// Deploys 4 distinct 3-person Raider Squads (12 total raiders) across outer sectors:
    /// - Sector_1_0_East (East Rail Yard Ambush Crew)
    /// - Sector_0_1_North (North Highway Toll Blockade)
    /// - Sector_1_1_NorthEast (Northeast Harbor Smugglers)
    /// - Sector_1_-1_SouthEast (Southeast Substation Scavengers)
    /// Enforces zero raiders in Sector_0_0_Center.
    /// Snaps all positions to valid NavMesh, attaches 3D audio, HumanoidProceduralAnimator,
    /// and instanced URP materials with grounded contact shadows.
    /// </summary>
    public static class Step2A_RaiderSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string ContactShadowMatPath = "Assets/_Project/Materials/AAA_PBR/MAT_ContactShadow.mat";

        public struct RaiderDef
        {
            public string name;
            public string squadId;
            public string displayName;
            public RaiderWeaponType weaponType;
            public Vector3 initialPos;
            public List<Vector3> patrolWaypoints;
            public Color bandanaColor;
        }

        [MenuItem("ProjectSurvival/Phase 2/Setup Step 2A Raiders", false, 301)]
        public static void SetupStep2A()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> EXECUTING STEP 2A: ARMED HUMAN RAIDERS & SQUAD OUTPOST SETUP <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step2A_RaiderSetup] Failed to open scene at: {ScenePath}");
                return;
            }

            // 1. Setup RaiderSquadDirector
            GameObject directorObj = GameObject.Find("RaiderSquadDirector");
            if (directorObj == null)
            {
                directorObj = new GameObject("RaiderSquadDirector");
                GameObject controllers = GameObject.Find("--- GAME CONTROLLERS ---");
                if (controllers != null) directorObj.transform.SetParent(controllers.transform, true);
            }
            RaiderSquadDirector director = directorObj.GetComponent<RaiderSquadDirector>();
            if (director == null) director = directorObj.AddComponent<RaiderSquadDirector>();

            // 2. Load Contact Shadow Material
            Material shadowMat = AssetDatabase.LoadAssetAtPath<Material>(ContactShadowMatPath);

            // 3. Define the 4 Squads (12 Raiders)
            List<RaiderDef> raiderDefs = BuildRaiderDefinitions();

            // 4. Create or Update Raiders
            int createdCount = 0;
            int updatedCount = 0;

            for (int i = 0; i < raiderDefs.Count; i++)
            {
                RaiderDef def = raiderDefs[i];
                GameObject parentSector = GameObject.Find(GetSectorNameForSquad(def.squadId));
                Transform squadParent = null;

                if (parentSector != null)
                {
                    string outpostName = $"Raider_Outpost_{def.squadId.Replace("Squad_", "")}";
                    Transform existingOutpost = parentSector.transform.Find(outpostName);
                    if (existingOutpost == null)
                    {
                        GameObject opObj = new GameObject(outpostName);
                        opObj.transform.SetParent(parentSector.transform, true);
                        squadParent = opObj.transform;
                    }
                    else
                    {
                        squadParent = existingOutpost;
                    }
                }

                // Snap initial position to NavMesh
                Vector3 spawnPos = def.initialPos;
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 6.0f, NavMesh.AllAreas))
                {
                    spawnPos = navHit.position;
                }

                // Snap patrol waypoints to NavMesh
                List<Vector3> snappedWaypoints = new List<Vector3>();
                for (int w = 0; w < def.patrolWaypoints.Count; w++)
                {
                    Vector3 wp = def.patrolWaypoints[w];
                    if (NavMesh.SamplePosition(wp, out NavMeshHit wpHit, 6.0f, NavMesh.AllAreas))
                    {
                        snappedWaypoints.Add(wpHit.position);
                    }
                    else
                    {
                        snappedWaypoints.Add(wp);
                    }
                }

                GameObject raiderObj = GameObject.Find(def.name);
                if (raiderObj == null)
                {
                    raiderObj = new GameObject(def.name);
                    createdCount++;
                }
                else
                {
                    updatedCount++;
                }

                if (squadParent != null)
                {
                    raiderObj.transform.SetParent(squadParent, true);
                }
                raiderObj.transform.position = spawnPos;
                raiderObj.tag = "NPC";
                int npcLayer = LayerMask.NameToLayer("NPC");
                if (npcLayer != -1) raiderObj.layer = npcLayer;

                // NavMeshAgent
                NavMeshAgent agent = raiderObj.GetComponent<NavMeshAgent>();
                if (agent == null) agent = raiderObj.AddComponent<NavMeshAgent>();
                agent.speed = 2.2f;
                agent.angularSpeed = 240f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.8f;
                agent.radius = 0.4f;
                agent.height = 1.8f;

                // CapsuleCollider
                CapsuleCollider cc = raiderObj.GetComponent<CapsuleCollider>();
                if (cc == null) cc = raiderObj.AddComponent<CapsuleCollider>();
                cc.radius = 0.4f;
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);

                // AudioSource (3D Spatial Positional Audio)
                AudioSource audio = raiderObj.GetComponent<AudioSource>();
                if (audio == null) audio = raiderObj.AddComponent<AudioSource>();
                audio.spatialBlend = 1.0f;
                audio.minDistance = 2.0f;
                audio.maxDistance = 38.0f;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.playOnAwake = false;

                // HumanRaiderAI
                HumanRaiderAI raiderAi = raiderObj.GetComponent<HumanRaiderAI>();
                if (raiderAi == null) raiderAi = raiderObj.AddComponent<HumanRaiderAI>();

                SerializedObject soAi = new SerializedObject(raiderAi);
                soAi.FindProperty("squadId").stringValue = def.squadId;
                soAi.FindProperty("raiderName").stringValue = def.displayName;
                soAi.FindProperty("weaponType").enumValueIndex = (int)def.weaponType;
                soAi.FindProperty("maxHealth").floatValue = (def.weaponType == RaiderWeaponType.Shotgun) ? 100f : 80f;
                soAi.FindProperty("currentHealth").floatValue = (def.weaponType == RaiderWeaponType.Shotgun) ? 100f : 80f;

                SerializedProperty wpList = soAi.FindProperty("patrolWaypoints");
                wpList.ClearArray();
                for (int w = 0; w < snappedWaypoints.Count; w++)
                {
                    wpList.InsertArrayElementAtIndex(w);
                    wpList.GetArrayElementAtIndex(w).vector3Value = snappedWaypoints[w];
                }
                soAi.ApplyModifiedProperties();

                // Create or Rebuild Visual Model
                Transform existingVisual = raiderObj.transform.Find("Visual_Model");
                if (existingVisual != null)
                {
                    Object.DestroyImmediate(existingVisual.gameObject);
                }

                GameObject visualModel = BuildRaiderVisualModel(raiderObj.transform, def.bandanaColor, def.weaponType, shadowMat);

                // Attach & Auto-bind HumanoidProceduralAnimator
                HumanoidProceduralAnimator anim = raiderObj.GetComponent<HumanoidProceduralAnimator>();
                if (anim == null) anim = raiderObj.AddComponent<HumanoidProceduralAnimator>();
                anim.SetVisualModel(visualModel.transform);
                anim.AutoBindLimbs();
            }

            // 5. Auto-discover and register squads
            director.AutoDiscoverSquadsIfEmpty();

            // 6. Verify Starter Zone Protection
            int centerRaiders = 0;
            GameObject centerSector = GameObject.Find("Sector_0_0_Center");
            if (centerSector != null)
            {
                var cr = centerSector.GetComponentsInChildren<HumanRaiderAI>(true);
                centerRaiders = cr.Length;
            }

            Debug.Log($"[Step2A_RaiderSetup] Deployed {raiderDefs.Count} Raiders ({createdCount} created, {updatedCount} updated).");
            Debug.Log($"[Step2A_RaiderSetup] Starter Zone Check: Exactly {centerRaiders} raiders in Sector_0_0_Center (Should be 0).");

            EditorUtility.SetDirty(directorObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Step2A_RaiderSetup] Scene saved successfully!");
        }

        private static string GetSectorNameForSquad(string squadId)
        {
            switch (squadId)
            {
                case "Squad_RailYard_East": return "Sector_1_0_East";
                case "Squad_Highway_North": return "Sector_0_1_North";
                case "Squad_Harbor_NorthEast": return "Sector_1_1_NorthEast";
                case "Squad_Substation_SouthEast": return "Sector_1_-1_SouthEast";
                default: return "Sector_1_0_East";
            }
        }

        private static List<RaiderDef> BuildRaiderDefinitions()
        {
            Color redBandana = new Color(0.65f, 0.18f, 0.15f);
            Color darkBandana = new Color(0.12f, 0.14f, 0.16f);

            return new List<RaiderDef>
            {
                // SQUAD 1: East Rail Yard Ambush Crew (Sector_1_0_East)
                new RaiderDef
                {
                    name = "Raider_East_Pistol_1",
                    squadId = "Squad_RailYard_East",
                    displayName = "Demiryolu Yağmacı Gözcüsü",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(216f, 0f, 12f),
                    patrolWaypoints = new List<Vector3> { new Vector3(216f, 0f, 12f), new Vector3(226f, 0f, 22f), new Vector3(220f, 0f, 5f) },
                    bandanaColor = redBandana
                },
                new RaiderDef
                {
                    name = "Raider_East_Pistol_2",
                    squadId = "Squad_RailYard_East",
                    displayName = "Demiryolu Yağmacı Avcısı",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(224f, 0f, 20f),
                    patrolWaypoints = new List<Vector3> { new Vector3(224f, 0f, 20f), new Vector3(234f, 0f, 14f), new Vector3(228f, 0f, 28f) },
                    bandanaColor = darkBandana
                },
                new RaiderDef
                {
                    name = "Raider_East_Shotgun_3",
                    squadId = "Squad_RailYard_East",
                    displayName = "Demiryolu Yağmacı Hücumcusu",
                    weaponType = RaiderWeaponType.Shotgun,
                    initialPos = new Vector3(228f, 0f, 10f),
                    patrolWaypoints = new List<Vector3> { new Vector3(228f, 0f, 10f), new Vector3(218f, 0f, 18f), new Vector3(238f, 0f, 8f) },
                    bandanaColor = redBandana
                },

                // SQUAD 2: North Highway Toll Blockade (Sector_0_1_North)
                new RaiderDef
                {
                    name = "Raider_North_Pistol_1",
                    squadId = "Squad_Highway_North",
                    displayName = "Otoyol Gişe Gözcüsü",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(10f, 0f, 222f),
                    patrolWaypoints = new List<Vector3> { new Vector3(10f, 0f, 222f), new Vector3(20f, 0f, 230f), new Vector3(14f, 0f, 214f) },
                    bandanaColor = redBandana
                },
                new RaiderDef
                {
                    name = "Raider_North_Pistol_2",
                    squadId = "Squad_Highway_North",
                    displayName = "Otoyol Gişe Nişancısı",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(18f, 0f, 230f),
                    patrolWaypoints = new List<Vector3> { new Vector3(18f, 0f, 230f), new Vector3(28f, 0f, 224f), new Vector3(22f, 0f, 236f) },
                    bandanaColor = darkBandana
                },
                new RaiderDef
                {
                    name = "Raider_North_Shotgun_3",
                    squadId = "Squad_Highway_North",
                    displayName = "Otoyol Barikat Kırıcı",
                    weaponType = RaiderWeaponType.Shotgun,
                    initialPos = new Vector3(22f, 0f, 218f),
                    patrolWaypoints = new List<Vector3> { new Vector3(22f, 0f, 218f), new Vector3(12f, 0f, 226f), new Vector3(30f, 0f, 215f) },
                    bandanaColor = redBandana
                },

                // SQUAD 3: Northeast Harbor Smugglers (Sector_1_1_NorthEast)
                new RaiderDef
                {
                    name = "Raider_Harbor_Pistol_1",
                    squadId = "Squad_Harbor_NorthEast",
                    displayName = "Liman Kaçakçı Muhafızı",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(226f, 0f, 226f),
                    patrolWaypoints = new List<Vector3> { new Vector3(226f, 0f, 226f), new Vector3(236f, 0f, 236f), new Vector3(230f, 0f, 218f) },
                    bandanaColor = darkBandana
                },
                new RaiderDef
                {
                    name = "Raider_Harbor_Pistol_2",
                    squadId = "Squad_Harbor_NorthEast",
                    displayName = "Liman Vinç Gözcüsü",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(236f, 0f, 235f),
                    patrolWaypoints = new List<Vector3> { new Vector3(236f, 0f, 235f), new Vector3(244f, 0f, 228f), new Vector3(238f, 0f, 242f) },
                    bandanaColor = redBandana
                },
                new RaiderDef
                {
                    name = "Raider_Harbor_Shotgun_3",
                    squadId = "Squad_Harbor_NorthEast",
                    displayName = "Liman İskele Baskıncısı",
                    weaponType = RaiderWeaponType.Shotgun,
                    initialPos = new Vector3(232f, 0f, 220f),
                    patrolWaypoints = new List<Vector3> { new Vector3(232f, 0f, 220f), new Vector3(224f, 0f, 230f), new Vector3(242f, 0f, 222f) },
                    bandanaColor = darkBandana
                },

                // SQUAD 4: Southeast Substation Scavengers (Sector_1_-1_SouthEast)
                new RaiderDef
                {
                    name = "Raider_Substation_Pistol_1",
                    squadId = "Squad_Substation_SouthEast",
                    displayName = "Trafo Sahası Yağmacısı",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(220f, 0f, -220f),
                    patrolWaypoints = new List<Vector3> { new Vector3(220f, 0f, -220f), new Vector3(230f, 0f, -228f), new Vector3(224f, 0f, -212f) },
                    bandanaColor = redBandana
                },
                new RaiderDef
                {
                    name = "Raider_Substation_Pistol_2",
                    squadId = "Squad_Substation_SouthEast",
                    displayName = "Yüksek Gerilim Muhafızı",
                    weaponType = RaiderWeaponType.Pistol,
                    initialPos = new Vector3(230f, 0f, -228f),
                    patrolWaypoints = new List<Vector3> { new Vector3(230f, 0f, -228f), new Vector3(238f, 0f, -220f), new Vector3(232f, 0f, -236f) },
                    bandanaColor = darkBandana
                },
                new RaiderDef
                {
                    name = "Raider_Substation_Shotgun_3",
                    squadId = "Squad_Substation_SouthEast",
                    displayName = "Trafo Şalt Ağır Hücumcusu",
                    weaponType = RaiderWeaponType.Shotgun,
                    initialPos = new Vector3(225f, 0f, -235f),
                    patrolWaypoints = new List<Vector3> { new Vector3(225f, 0f, -235f), new Vector3(216f, 0f, -225f), new Vector3(235f, 0f, -240f) },
                    bandanaColor = redBandana
                }
            };
        }

        private static GameObject BuildRaiderVisualModel(Transform rootParent, Color bandanaColor, RaiderWeaponType weaponType, Material shadowMat)
        {
            GameObject visual = new GameObject("Visual_Model");
            visual.transform.SetParent(rootParent, false);

            // Material Caches (strictly URP Lit with GPU Instancing)
            Material skinMat = SceneBuilder.GetOrCreateMaterial("Mat_Skin_Human", new Color(0.88f, 0.74f, 0.63f));
            Material shirtMat = SceneBuilder.GetOrCreateMaterial("Mat_Raider_Shirt", new Color(0.24f, 0.22f, 0.20f));
            Material pantsMat = SceneBuilder.GetOrCreateMaterial("Mat_Raider_Pants", new Color(0.14f, 0.15f, 0.16f));
            Material vestMat = SceneBuilder.GetOrCreateMaterial("Mat_Raider_Vest", new Color(0.18f, 0.20f, 0.22f));
            Material bandanaMat = SceneBuilder.GetOrCreateMaterial($"Mat_Bandana_{bandanaColor.GetHashCode()}", bandanaColor);
            Material weaponSteelMat = SceneBuilder.GetOrCreateMaterial("Mat_Weapon_Steel", new Color(0.12f, 0.12f, 0.14f));

            skinMat.enableInstancing = true;
            shirtMat.enableInstancing = true;
            pantsMat.enableInstancing = true;
            vestMat.enableInstancing = true;
            bandanaMat.enableInstancing = true;
            weaponSteelMat.enableInstancing = true;

            // Container pivot for posture
            GameObject bodyHips = new GameObject("Body_Hips");
            bodyHips.transform.SetParent(visual.transform, false);
            bodyHips.transform.localPosition = new Vector3(0f, 0.88f, 0f);

            // Pelvis
            GameObject pelvis = CreatePart(PrimitiveType.Capsule, "Pelvis", bodyHips.transform,
                Vector3.zero, new Vector3(0.28f, 0.18f, 0.22f), pantsMat);
            pelvis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Torso
            GameObject torso = CreatePart(PrimitiveType.Capsule, "Torso", bodyHips.transform,
                new Vector3(0f, 0.32f, 0f), new Vector3(0.34f, 0.24f, 0.24f), shirtMat);

            // Tactical Vest Overlay
            CreatePart(PrimitiveType.Capsule, "Vest_Overlay", torso.transform,
                Vector3.zero, new Vector3(1.08f, 1.02f, 1.15f), vestMat);

            // Neck & Head
            GameObject neck = CreatePart(PrimitiveType.Cylinder, "Neck", torso.transform,
                new Vector3(0f, 0.54f, 0.02f), new Vector3(0.18f, 0.12f, 0.18f), skinMat);

            GameObject head = CreatePart(PrimitiveType.Sphere, "Head", neck.transform,
                new Vector3(0f, 1.15f, 0f), new Vector3(1.25f, 1.35f, 1.25f), skinMat);

            // Bandana around forehead / nose
            GameObject bandana = CreatePart(PrimitiveType.Cylinder, "Bandana", head.transform,
                new Vector3(0f, -0.05f, 0.05f), new Vector3(1.05f, 0.28f, 1.05f), bandanaMat);

            // Left Arm
            GameObject shoulderL = new GameObject("Shoulder_L");
            shoulderL.transform.SetParent(torso.transform, false);
            shoulderL.transform.localPosition = new Vector3(-0.35f, 0.24f, 0f);

            GameObject upperArmL = CreatePart(PrimitiveType.Capsule, "UpperArm_L", shoulderL.transform,
                new Vector3(0f, -0.16f, 0f), new Vector3(0.14f, 0.16f, 0.14f), shirtMat);

            GameObject forearmL = CreatePart(PrimitiveType.Capsule, "Forearm_L", upperArmL.transform,
                new Vector3(0f, -1.02f, 0f), new Vector3(0.85f, 0.85f, 0.85f), skinMat);

            CreatePart(PrimitiveType.Sphere, "Hand_L", forearmL.transform,
                new Vector3(0f, -1.05f, 0f), new Vector3(0.8f, 0.6f, 0.7f), skinMat);

            // Right Arm
            GameObject shoulderR = new GameObject("Shoulder_R");
            shoulderR.transform.SetParent(torso.transform, false);
            shoulderR.transform.localPosition = new Vector3(0.35f, 0.24f, 0f);

            GameObject upperArmR = CreatePart(PrimitiveType.Capsule, "UpperArm_R", shoulderR.transform,
                new Vector3(0f, -0.16f, 0f), new Vector3(0.14f, 0.16f, 0.14f), shirtMat);

            GameObject forearmR = CreatePart(PrimitiveType.Capsule, "Forearm_R", upperArmR.transform,
                new Vector3(0f, -1.02f, 0f), new Vector3(0.85f, 0.85f, 0.85f), skinMat);

            GameObject handR = CreatePart(PrimitiveType.Sphere, "Hand_R", forearmR.transform,
                new Vector3(0f, -1.05f, 0f), new Vector3(0.85f, 0.65f, 0.75f), skinMat);

            // Attach Weapon Mesh to Right Hand
            if (weaponType == RaiderWeaponType.Shotgun)
            {
                // Shotgun Model
                GameObject shotgunObj = new GameObject("Weapon_Shotgun");
                shotgunObj.transform.SetParent(handR.transform, false);
                shotgunObj.transform.localPosition = new Vector3(0f, -0.1f, 0.25f);
                shotgunObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

                CreatePart(PrimitiveType.Cube, "Shotgun_Body", shotgunObj.transform,
                    new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.12f, 0.75f), weaponSteelMat);
                CreatePart(PrimitiveType.Cylinder, "Shotgun_Barrel", shotgunObj.transform,
                    new Vector3(0f, 0.04f, 0.45f), new Vector3(0.05f, 0.45f, 0.05f), weaponSteelMat)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // Pistol Model
                GameObject pistolObj = new GameObject("Weapon_Pistol");
                pistolObj.transform.SetParent(handR.transform, false);
                pistolObj.transform.localPosition = new Vector3(0f, -0.05f, 0.18f);

                CreatePart(PrimitiveType.Cube, "Pistol_Slide", pistolObj.transform,
                    new Vector3(0f, 0.04f, 0.08f), new Vector3(0.06f, 0.08f, 0.28f), weaponSteelMat);
                CreatePart(PrimitiveType.Cube, "Pistol_Grip", pistolObj.transform,
                    new Vector3(0f, -0.06f, 0f), new Vector3(0.05f, 0.14f, 0.08f), weaponSteelMat);
            }

            // Legs
            GameObject thighL = CreatePart(PrimitiveType.Capsule, "Thigh_L", bodyHips.transform,
                new Vector3(-0.14f, -0.24f, 0f), new Vector3(0.14f, 0.22f, 0.14f), pantsMat);

            GameObject calfL = CreatePart(PrimitiveType.Capsule, "Calf_L", thighL.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.85f, 0.85f, 0.85f), pantsMat);

            CreatePart(PrimitiveType.Cube, "Foot_L", calfL.transform,
                new Vector3(0f, -1.05f, 0.1f), new Vector3(0.15f, 0.1f, 0.28f), pantsMat);

            GameObject thighR = CreatePart(PrimitiveType.Capsule, "Thigh_R", bodyHips.transform,
                new Vector3(0.14f, -0.24f, 0f), new Vector3(0.14f, 0.22f, 0.14f), pantsMat);

            GameObject calfR = CreatePart(PrimitiveType.Capsule, "Calf_R", thighR.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.85f, 0.85f, 0.85f), pantsMat);

            CreatePart(PrimitiveType.Cube, "Foot_R", calfR.transform,
                new Vector3(0f, -1.05f, 0.1f), new Vector3(0.15f, 0.1f, 0.28f), pantsMat);

            // Grounded Contact Shadow Quad
            if (shadowMat != null)
            {
                GameObject shadowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadowObj.name = "ContactShadow_Raider";
                shadowObj.transform.SetParent(visual.transform, false);
                shadowObj.transform.localPosition = new Vector3(0f, 0.015f, 0f);
                shadowObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shadowObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

                Collider col = shadowObj.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                Renderer sr = shadowObj.GetComponent<Renderer>();
                if (sr != null)
                {
                    sr.sharedMaterial = shadowMat;
                    sr.shadowCastingMode = ShadowCastingMode.Off;
                    sr.receiveShadows = false;
                }
            }

            return visual;
        }

        private static GameObject CreatePart(PrimitiveType primType, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(primType);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            // Remove primitive collider so only root CapsuleCollider handles physics
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            return obj;
        }
    }
}
