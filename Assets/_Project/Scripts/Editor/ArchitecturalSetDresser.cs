using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Interaction;
using ProjectSurvival.Environment;
using ProjectSurvival.Core;

namespace ProjectSurvival.EditorTools
{
    public static class ArchitecturalSetDresser
    {
        [MenuItem("ProjectSurvival/Dresser Architectural Facades & City Props", false, 22)]
        public static void DressAllMenuItem()
        {
            DressAll(true);
        }

        public static void DressAll(bool saveScene = true)
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING ARCHITECTURAL FACADE UPGRADE & INTERIOR SET-DRESSING <<<");
            Debug.Log("==================================================================");

            Transform env = (GameObject.Find("Environment") ?? GameObject.Find("--- ENVIRONMENT ---"))?.transform;
            if (env == null)
            {
                var envObj = new GameObject("Environment");
                env = envObj.transform;
            }

            // 1. Multi-Layered Building Facades & 3D Backlit Signs
            UpgradeBuildingFacades(env);

            // 2. Street-Level Storytelling & Curb Infrastructure
            UpgradeStreetInfrastructure(env);

            // 3. Dense Interior Set-Dressing (Linked to Stage 1 Interactables)
            DressFacilityInteriors(env);

            if (saveScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[ArchitecturalSetDresser] Scene saved successfully: {scene.path}");
            }

            Debug.Log("[ArchitecturalSetDresser] Architectural Set-Dressing Completed!");
        }

        #region 1. Multi-Layered Building Facades & 3D Signs

        public static void UpgradeBuildingFacades(Transform env)
        {
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Concrete_Spalling.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Spalling", new Color(0.42f, 0.43f, 0.44f));
            Material brickMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Brick_Weathered.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Brick_Weathered", new Color(0.55f, 0.24f, 0.18f));
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Station", new Color(0.28f, 0.30f, 0.34f));
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Wood_Planks.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Wood_Planks", new Color(0.48f, 0.35f, 0.22f));

            Transform facadeParent = env.Find("--- ARCHITECTURAL FACADES & DETAILS ---");
            if (facadeParent == null)
            {
                GameObject fp = new GameObject("--- ARCHITECTURAL FACADES & DETAILS ---");
                fp.transform.SetParent(env);
                facadeParent = fp.transform;
            }

            var buildings = new (string name, Vector3 pos, Vector3 size, string signText, Color signColor, Color signGlow)[]
            {
                ("Building_OldClinic", new Vector3(-28f, 0f, 32f), new Vector3(18f, 4.8f, 16f), "ST. JUDE CLINIC", new Color(0.12f, 0.45f, 0.48f), new Color(0.2f, 0.95f, 0.85f)),
                ("Building_PoliceStation", new Vector3(-18f, 0f, 6f), new Vector3(16f, 4.8f, 16f), "POLICE DEPT", new Color(0.10f, 0.14f, 0.22f), new Color(0.2f, 0.45f, 1.0f)),
                ("SafeHouse_Building", new Vector3(-32f, 0f, -28f), new Vector3(16f, 4.5f, 16f), "SAFE HOUSE", new Color(0.25f, 0.24f, 0.22f), new Color(1.0f, 0.65f, 0.15f)),
                ("Building_Supermarket", new Vector3(36f, 0f, 32f), new Vector3(22f, 5.2f, 20f), "SUPERMARKET", new Color(0.15f, 0.35f, 0.22f), new Color(1.0f, 0.85f, 0.25f)),
                ("Building_GasStation", new Vector3(58f, 0f, -26f), new Vector3(18f, 4.5f, 14f), "GAS MART", new Color(0.45f, 0.15f, 0.15f), new Color(1.0f, 0.25f, 0.25f)),
                ("Building_Pharmacy", new Vector3(30f, 0f, -44f), new Vector3(14f, 4.2f, 14f), "PHARMACY", new Color(0.18f, 0.45f, 0.28f), new Color(0.25f, 1.0f, 0.45f))
            };

            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                GameObject bldgObj = GameObject.Find(b.name);
                Vector3 basePos = bldgObj != null ? bldgObj.transform.position : b.pos;

                string groupName = $"FacadeDetails_{b.name}";
                if (facadeParent.Find(groupName) == null)
                {
                    GameObject group = new GameObject(groupName);
                    group.transform.SetParent(facadeParent);
                    group.transform.position = basePos;

                    float w = b.size.x;
                    float h = b.size.y;
                    float d = b.size.z;

                    // 1. Rusticated Foundation Plinth (0.7m high, 0.2m protrusion around base)
                    CreateFacadeBox(group.transform, "Foundation_Plinth", new Vector3(0f, 0.35f, 0f), new Vector3(w + 0.4f, 0.7f, d + 0.4f), concreteMat);

                    // 2. Vertical Corner Pilasters (4 corner columns)
                    float hw = w * 0.5f;
                    float hd = d * 0.5f;
                    CreateFacadeBox(group.transform, "Pilaster_FL", new Vector3(-hw, h * 0.5f, -hd), new Vector3(0.55f, h, 0.55f), concreteMat);
                    CreateFacadeBox(group.transform, "Pilaster_FR", new Vector3(hw, h * 0.5f, -hd), new Vector3(0.55f, h, 0.55f), concreteMat);
                    CreateFacadeBox(group.transform, "Pilaster_BL", new Vector3(-hw, h * 0.5f, hd), new Vector3(0.55f, h, 0.55f), concreteMat);
                    CreateFacadeBox(group.transform, "Pilaster_BR", new Vector3(hw, h * 0.5f, hd), new Vector3(0.55f, h, 0.55f), concreteMat);

                    // 3. Horizontal Cornice & Roof Parapet Wall
                    CreateFacadeBox(group.transform, "Roof_Cornice", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.5f, 0.30f, d + 0.5f), concreteMat);
                    CreateFacadeBox(group.transform, "Roof_Parapet_Front", new Vector3(0f, h + 0.55f, -hd), new Vector3(w, 0.70f, 0.25f), concreteMat);
                    CreateFacadeBox(group.transform, "Roof_Parapet_Back", new Vector3(0f, h + 0.55f, hd), new Vector3(w, 0.70f, 0.25f), concreteMat);
                    CreateFacadeBox(group.transform, "Roof_Parapet_Left", new Vector3(-hw, h + 0.55f, 0f), new Vector3(0.25f, 0.70f, d), concreteMat);
                    CreateFacadeBox(group.transform, "Roof_Parapet_Right", new Vector3(hw, h + 0.55f, 0f), new Vector3(0.25f, 0.70f, d), concreteMat);

                    // 4. Rooftop Mechanicals (HVAC Air Handler & Cylindrical Water Tank)
                    CreateRooftopMechanicals(group.transform, h, w, d, metalMat, woodMat);

                    // 5. Exterior Metal Fire Escape Balcony & Ladder (Side wall)
                    CreateFireEscapeBalcony(group.transform, new Vector3(-hw - 0.65f, 0f, 0f), h, metalMat);

                    // 6. Protruding Storefront Awning over Entrance
                    Material awningMat = SceneBuilder.GetOrCreateMaterial("Mat_Canopy_Red", new Color(0.65f, 0.18f, 0.18f));
                    CreateFacadeBox(group.transform, "Entrance_Awning", new Vector3(0f, 3.2f, -hd - 0.95f), new Vector3(4.5f, 0.22f, 1.9f), awningMat, Quaternion.Euler(12f, 0f, 0f));

                    // 7. 3D Backlit Neon / Signboard with Flickering Light
                    Create3DBacklitSign(group.transform, new Vector3(0f, 3.8f, -hd - 0.22f), b.signText, b.signColor, b.signGlow);
                }
            }

            Debug.Log("[ArchitecturalSetDresser] Upgraded Multi-Layered Building Facades & 3D Signboards.");
        }

        private static void CreateRooftopMechanicals(Transform parent, float roofY, float w, float d, Material metalMat, Material woodMat)
        {
            GameObject roofProps = new GameObject("Rooftop_Mechanicals");
            roofProps.transform.SetParent(parent, false);

            // HVAC Unit 1
            CreateFacadeBox(roofProps.transform, "HVAC_Unit_1", new Vector3(-w * 0.2f, roofY + 0.85f, d * 0.15f), new Vector3(2.4f, 1.4f, 1.8f), metalMat);
            // HVAC Fan Grille
            CreateFacadeCylinder(roofProps.transform, "HVAC_Fan_Grille", new Vector3(-w * 0.2f, roofY + 1.58f, d * 0.15f), new Vector3(1.2f, 0.08f, 1.2f), metalMat);

            // Cylindrical Water Tower on timber legs
            GameObject waterTower = new GameObject("Water_Tower");
            waterTower.transform.SetParent(roofProps.transform, false);
            waterTower.transform.localPosition = new Vector3(w * 0.22f, roofY + 0.05f, -d * 0.15f);

            // 4 Timber Stilts
            CreateFacadeBox(waterTower.transform, "Leg_FL", new Vector3(-0.6f, 0.9f, -0.6f), new Vector3(0.18f, 1.8f, 0.18f), woodMat);
            CreateFacadeBox(waterTower.transform, "Leg_FR", new Vector3(0.6f, 0.9f, -0.6f), new Vector3(0.18f, 1.8f, 0.18f), woodMat);
            CreateFacadeBox(waterTower.transform, "Leg_BL", new Vector3(-0.6f, 0.9f, 0.6f), new Vector3(0.18f, 1.8f, 0.18f), woodMat);
            CreateFacadeBox(waterTower.transform, "Leg_BR", new Vector3(0.6f, 0.9f, 0.6f), new Vector3(0.18f, 1.8f, 0.18f), woodMat);

            // Tank Cylinder
            CreateFacadeCylinder(waterTower.transform, "Tank_Drum", new Vector3(0f, 2.5f, 0f), new Vector3(1.8f, 1.4f, 1.8f), woodMat);
            // Conical Roof
            CreateFacadeCylinder(waterTower.transform, "Tank_Cap", new Vector3(0f, 3.3f, 0f), new Vector3(1.9f, 0.2f, 1.9f), metalMat);

            // Exhaust Vents
            CreateFacadeCylinder(roofProps.transform, "Exhaust_Vent_1", new Vector3(-w * 0.1f, roofY + 0.6f, -d * 0.2f), new Vector3(0.35f, 1.2f, 0.35f), metalMat);
            CreateFacadeCylinder(roofProps.transform, "Exhaust_Vent_2", new Vector3(w * 0.05f, roofY + 0.7f, d * 0.25f), new Vector3(0.40f, 1.4f, 0.40f), metalMat);
        }

        private static void CreateFireEscapeBalcony(Transform parent, Vector3 pos, float buildingHeight, Material metalMat)
        {
            GameObject fe = new GameObject("Fire_Escape");
            fe.transform.SetParent(parent, false);
            fe.transform.localPosition = pos;

            // Balcony Platform
            CreateFacadeBox(fe.transform, "Platform_Level1", new Vector3(0f, 2.8f, 0f), new Vector3(1.2f, 0.12f, 3.6f), metalMat);
            // Railings
            CreateFacadeBox(fe.transform, "Railing_Outer", new Vector3(-0.55f, 3.35f, 0f), new Vector3(0.08f, 0.95f, 3.6f), metalMat);
            CreateFacadeBox(fe.transform, "Railing_Front", new Vector3(0f, 3.35f, -1.75f), new Vector3(1.1f, 0.95f, 0.08f), metalMat);
            CreateFacadeBox(fe.transform, "Railing_Back", new Vector3(0f, 3.35f, 1.75f), new Vector3(1.1f, 0.95f, 0.08f), metalMat);

            // Drop ladder to ground
            CreateFacadeBox(fe.transform, "Ladder_Rail_L", new Vector3(0.2f, 1.4f, -1.6f), new Vector3(0.06f, 2.7f, 0.06f), metalMat);
            CreateFacadeBox(fe.transform, "Ladder_Rail_R", new Vector3(0.2f, 1.4f, -1.1f), new Vector3(0.06f, 2.7f, 0.06f), metalMat);
            for (int r = 0; r < 7; r++)
            {
                CreateFacadeBox(fe.transform, $"Rung_{r + 1}", new Vector3(0.2f, 0.35f + r * 0.35f, -1.35f), new Vector3(0.04f, 0.04f, 0.5f), metalMat);
            }
        }

        private static void Create3DBacklitSign(Transform parent, Vector3 pos, string text, Color boxColor, Color glowColor)
        {
            GameObject signRoot = new GameObject($"Sign3D_{text.Replace(" ", "_")}");
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = pos;

            // Sign Backing Box
            Material backingMat = SceneBuilder.GetOrCreateMaterial($"Mat_SignBack_{text.GetHashCode()}", boxColor);
            CreateFacadeBox(signRoot.transform, "Sign_Backing", Vector3.zero, new Vector3(4.8f, 0.95f, 0.16f), backingMat);

            // Sign Face / Frame
            Material frameMat = SceneBuilder.GetOrCreateMaterial("Mat_Sign_FrameMetal", new Color(0.18f, 0.18f, 0.20f));
            CreateFacadeBox(signRoot.transform, "Sign_Frame_Top", new Vector3(0f, 0.45f, 0.06f), new Vector3(4.9f, 0.08f, 0.08f), frameMat);
            CreateFacadeBox(signRoot.transform, "Sign_Frame_Bottom", new Vector3(0f, -0.45f, 0.06f), new Vector3(4.9f, 0.08f, 0.08f), frameMat);

            // Glowing Letters Plaque
            Material glowMat = SceneBuilder.GetOrCreateMaterial($"Mat_SignGlow_{text.GetHashCode()}", glowColor);
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", glowColor * 1.8f);

            GameObject textPlate = CreateFacadeBox(signRoot.transform, "Sign_GlowLetters", new Vector3(0f, 0f, -0.09f), new Vector3(4.4f, 0.70f, 0.04f), glowMat);

            // Backlit Point Light with Atmospheric Flickering
            GameObject lightObj = new GameObject("Sign_Backlight");
            lightObj.transform.SetParent(signRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, -0.45f);

            Light signLight = lightObj.AddComponent<Light>();
            signLight.type = LightType.Point;
            signLight.color = glowColor;
            signLight.range = 5.5f;
            signLight.intensity = 1.8f;
            signLight.shadows = LightShadows.None;

            FlickeringLight flicker = lightObj.AddComponent<FlickeringLight>();
            SerializedObject so = new SerializedObject(flicker);
            so.FindProperty("baseIntensity").floatValue = 1.8f;
            so.FindProperty("flickerAmount").floatValue = 0.45f;
            so.FindProperty("flickerSpeed").floatValue = 14f;
            so.ApplyModifiedProperties();
        }

        #endregion

        #region 2. Street Infrastructure & Curbs

        public static void UpgradeStreetInfrastructure(Transform env)
        {
            Transform infraParent = env.Find("--- STREET INFRASTRUCTURE & CURBS ---");
            if (infraParent == null)
            {
                GameObject ip = new GameObject("--- STREET INFRASTRUCTURE & CURBS ---");
                ip.transform.SetParent(env);
                infraParent = ip.transform;
            }

            Material curbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Concrete_Spalling.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Spalling", new Color(0.42f, 0.43f, 0.44f));
            Material grateMat = SceneBuilder.GetOrCreateMaterial("Mat_Grate_Iron", new Color(0.18f, 0.20f, 0.22f));
            Material signpostMat = SceneBuilder.GetOrCreateMaterial("Mat_StreetSign_Green", new Color(0.15f, 0.45f, 0.25f));
            Material poleMat = SceneBuilder.GetOrCreateMaterial("Mat_Chrome_Metal", new Color(0.65f, 0.67f, 0.70f));
            Material wireMat = SceneBuilder.GetOrCreateMaterial("Mat_Utility_Cable", new Color(0.10f, 0.10f, 0.10f));

            // 1. Concrete Curbs along Main Boulevard (X = -6.5 and X = +6.5, Z from -90 to +90)
            if (infraParent.Find("Curbs_Boulevard_Left") == null)
            {
                CreateFacadeBox(infraParent, "Curbs_Boulevard_Left", new Vector3(-6.85f, 0.10f, 0f), new Vector3(0.35f, 0.20f, 180f), curbMat);
                CreateFacadeBox(infraParent, "Curbs_Boulevard_Right", new Vector3(6.85f, 0.10f, 0f), new Vector3(0.35f, 0.20f, 180f), curbMat);
            }

            // 2. Storm Drain Grates in Road Gutters
            Vector3[] gratePositions = new Vector3[]
            {
                new Vector3(-6.45f, 0.02f, -60f),
                new Vector3(6.45f, 0.02f, -40f),
                new Vector3(-6.45f, 0.02f, -15f),
                new Vector3(6.45f, 0.02f, 15f),
                new Vector3(-6.45f, 0.02f, 40f),
                new Vector3(6.45f, 0.02f, 60f)
            };

            for (int i = 0; i < gratePositions.Length; i++)
            {
                string gName = $"StormDrain_Grate_{i + 1}";
                if (infraParent.Find(gName) == null)
                {
                    CreateFacadeBox(infraParent, gName, gratePositions[i], new Vector3(0.65f, 0.04f, 1.2f), grateMat);
                }
            }

            // 3. Street Name Signposts at Key Intersections
            var signposts = new (string name, Vector3 pos, string streetName, float rotY)[]
            {
                ("Signpost_RoundaboutNorth", new Vector3(-8.2f, 0.05f, 22f), "ATATÜRK BULVARI", 90f),
                ("Signpost_ClinicCorner", new Vector3(-12.5f, 0.05f, 24f), "HASTANE SOKAĞI", 0f),
                ("Signpost_PoliceStation", new Vector3(-12.5f, 0.05f, -6f), "KARAKOL CADDESİ", 0f),
                ("Signpost_Supermarket", new Vector3(12.5f, 0.05f, 24f), "TİCARET BULVARI", -90f),
                ("Signpost_SubwayPlaza", new Vector3(12.5f, 0.05f, -24f), "İSTASYON MEYDANI", -90f)
            };

            for (int i = 0; i < signposts.Length; i++)
            {
                var sp = signposts[i];
                if (infraParent.Find(sp.name) == null)
                {
                    GameObject postObj = new GameObject(sp.name);
                    postObj.transform.SetParent(infraParent);
                    postObj.transform.position = sp.pos;
                    postObj.transform.rotation = Quaternion.Euler(0f, sp.rotY, 0f);

                    // Vertical Pole
                    CreateFacadeCylinder(postObj.transform, "Metal_Pole", new Vector3(0f, 1.5f, 0f), new Vector3(0.08f, 1.5f, 0.08f), poleMat);
                    // Street Name Plate
                    CreateFacadeBox(postObj.transform, "Street_Name_Plate", new Vector3(0.35f, 2.75f, 0f), new Vector3(0.75f, 0.18f, 0.03f), signpostMat);
                    CreateFacadeBox(postObj.transform, "Cross_Street_Plate", new Vector3(0f, 2.55f, 0.35f), new Vector3(0.03f, 0.18f, 0.75f), signpostMat);
                }
            }

            // 4. Knocked-over Newspaper Vending Boxes on Sidewalks
            Material newsboxMat = SceneBuilder.GetOrCreateMaterial("Mat_Newsbox_Blue", new Color(0.18f, 0.32f, 0.55f));
            if (infraParent.Find("Newspaper_Box_Tumbled") == null)
            {
                GameObject box1 = CreateFacadeBox(infraParent, "Newspaper_Box_Tumbled", new Vector3(-8.5f, 0.22f, -12f), new Vector3(0.55f, 0.95f, 0.45f), newsboxMat, Quaternion.Euler(75f, 25f, -15f));
                GameObject box2 = CreateFacadeBox(infraParent, "Newspaper_Box_Standing", new Vector3(8.5f, 0.55f, 32f), new Vector3(0.55f, 1.1f, 0.45f), newsboxMat);
            }

            // 5. Tangled Overhead Utility Cables (Between Lamp Posts)
            if (infraParent.Find("Overhead_Cables_Boulevard") == null)
            {
                GameObject cableGroup = new GameObject("Overhead_Cables_Boulevard");
                cableGroup.transform.SetParent(infraParent);

                float[] cableZ = new float[] { -60f, -20f, 20f, 60f };
                for (int c = 0; c < cableZ.Length; c++)
                {
                    // Sagging catenary cable segment between left and right lamp posts
                    float z = cableZ[c];
                    CreateFacadeBox(cableGroup.transform, $"Cable_Span_{c + 1}_L", new Vector3(-3.5f, 4.8f, z), new Vector3(7.2f, 0.03f, 0.03f), wireMat, Quaternion.Euler(0f, 0f, -4f));
                    CreateFacadeBox(cableGroup.transform, $"Cable_Span_{c + 1}_R", new Vector3(3.5f, 4.8f, z), new Vector3(7.2f, 0.03f, 0.03f), wireMat, Quaternion.Euler(0f, 0f, 4f));
                }
            }

            // 6. Sandbag Fortifications & Concrete T-Barriers around Roundabout Checkpoint
            if (infraParent.Find("Checkpoint_Fortifications_Roundabout") == null)
            {
                GameObject fortGroup = new GameObject("Checkpoint_Fortifications_Roundabout");
                fortGroup.transform.SetParent(infraParent);

                Material sandbagMat = SceneBuilder.GetOrCreateMaterial("Mat_Sandbag_Khaki", new Color(0.55f, 0.48f, 0.36f));

                // Curved sandbag berms
                Vector3[] sandbagArcs = new Vector3[]
                {
                    new Vector3(-4.5f, 0.35f, -6f),
                    new Vector3(-3.2f, 0.35f, -7.5f),
                    new Vector3(3.2f, 0.35f, -7.5f),
                    new Vector3(4.5f, 0.35f, -6f),
                    new Vector3(-4.5f, 0.35f, 6f),
                    new Vector3(4.5f, 0.35f, 6f)
                };

                for (int s = 0; s < sandbagArcs.Length; s++)
                {
                    CreateFacadeBox(fortGroup.transform, $"Sandbag_Wall_{s + 1}", sandbagArcs[s], new Vector3(1.6f, 0.70f, 0.55f), sandbagMat);
                }
            }

            Debug.Log("[ArchitecturalSetDresser] Upgraded Street-Level Storytelling & Curb Infrastructure.");
        }

        #endregion

        #region 3. Dense Interior Set-Dressing

        public static void DressFacilityInteriors(Transform env)
        {
            Transform interiorParent = env.Find("--- FACILITY INTERIOR PROPS & DRESSING ---");
            if (interiorParent == null)
            {
                GameObject ip = new GameObject("--- FACILITY INTERIOR PROPS & DRESSING ---");
                ip.transform.SetParent(env);
                interiorParent = ip.transform;
            }

            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Concrete_Station", new Color(0.35f, 0.38f, 0.42f));
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Wood_Planks.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Wood_Planks", new Color(0.48f, 0.35f, 0.22f));
            Material whiteMat = SceneBuilder.GetOrCreateMaterial("Mat_Ambulance_White", new Color(0.92f, 0.94f, 0.96f));
            Material sheetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Tactical_Fabric.mat")
                ?? SceneBuilder.GetOrCreateMaterial("Mat_Tactical_Fabric", new Color(0.25f, 0.32f, 0.40f));
            Material screenMat = SceneBuilder.GetOrCreateMaterial("Mat_Computer_Screen", new Color(0.12f, 0.45f, 0.65f));
            screenMat.EnableKeyword("_EMISSION");
            screenMat.SetColor("_EmissionColor", new Color(0.12f, 0.45f, 0.65f) * 1.4f);

            // 1. Clinic: Hospital Beds with IV Stands & Waiting Room Chairs
            if (interiorParent.Find("Clinic_Interiors") == null)
            {
                GameObject clinicInt = new GameObject("Clinic_Interiors");
                clinicInt.transform.SetParent(interiorParent);
                clinicInt.transform.position = new Vector3(-28f, 0f, 32f);

                // Hospital Bed 1
                CreateHospitalBed(clinicInt.transform, new Vector3(-2f, 0.05f, 3f), metalMat, whiteMat, sheetMat);
                // Hospital Bed 2
                CreateHospitalBed(clinicInt.transform, new Vector3(-2f, 0.05f, -2f), metalMat, whiteMat, sheetMat);

                // Desktop Monitor & Keyboard on Reception Desk
                CreateFacadeBox(clinicInt.transform, "PC_Monitor", new Vector3(0f, 1.25f, -2.5f), new Vector3(0.45f, 0.35f, 0.05f), screenMat);
                CreateFacadeBox(clinicInt.transform, "PC_Base", new Vector3(0f, 1.05f, -2.5f), new Vector3(0.20f, 0.08f, 0.18f), metalMat);

                // Waiting Room Chairs (3 linked)
                for (int w = 0; w < 3; w++)
                {
                    CreateFacadeBox(clinicInt.transform, $"Waiting_Chair_{w + 1}", new Vector3(3f, 0.45f, -3f + w * 0.75f), new Vector3(0.55f, 0.85f, 0.55f), metalMat);
                }
            }

            // 2. Police Station: Booking Counter, Gun Locker, Interrogation Desk & Lamp
            if (interiorParent.Find("Police_Interiors") == null)
            {
                GameObject polInt = new GameObject("Police_Interiors");
                polInt.transform.SetParent(interiorParent);
                polInt.transform.position = new Vector3(-18f, 0f, 6f);

                // Duty Sergeant High Booking Counter
                CreateFacadeBox(polInt.transform, "Booking_Counter", new Vector3(0f, 0.65f, -3f), new Vector3(3.4f, 1.25f, 0.75f), woodMat);
                // Metal Evidence Locker
                CreateFacadeBox(polInt.transform, "Evidence_Locker_Row", new Vector3(-4f, 1.05f, 2f), new Vector3(0.65f, 2.1f, 2.8f), metalMat);
                // Interrogation Table & 2 Metal Chairs
                CreateFacadeBox(polInt.transform, "Interrogation_Table", new Vector3(2f, 0.45f, 2f), new Vector3(1.6f, 0.85f, 0.9f), metalMat);
                CreateFacadeBox(polInt.transform, "Chair_A", new Vector3(2f, 0.45f, 1.2f), new Vector3(0.5f, 0.85f, 0.5f), metalMat);
                CreateFacadeBox(polInt.transform, "Chair_B", new Vector3(2f, 0.45f, 2.8f), new Vector3(0.5f, 0.85f, 0.5f), metalMat);
            }

            // 3. SafeHouse: Survival Workbench, Ham Radio Desk & Food Stash Shelving
            if (interiorParent.Find("SafeHouse_Interiors") == null)
            {
                GameObject safeInt = new GameObject("SafeHouse_Interiors");
                safeInt.transform.SetParent(interiorParent);
                safeInt.transform.position = new Vector3(-32f, 0f, -28f);

                // Survival Workbench with Vise
                CreateFacadeBox(safeInt.transform, "Crafting_Workbench", new Vector3(2f, 0.50f, 3f), new Vector3(2.2f, 0.95f, 0.95f), woodMat);
                CreateFacadeBox(safeInt.transform, "Workbench_Vise", new Vector3(2.8f, 1.05f, 3.2f), new Vector3(0.22f, 0.22f, 0.18f), metalMat);

                // Ham Radio Communications Station
                CreateFacadeBox(safeInt.transform, "Ham_Radio_Rig", new Vector3(-3f, 1.05f, 0f), new Vector3(0.65f, 0.35f, 0.45f), metalMat);
                CreateFacadeBox(safeInt.transform, "Radio_Frequency_Dial", new Vector3(-2.65f, 1.08f, 0f), new Vector3(0.04f, 0.15f, 0.22f), screenMat);

                // Heavy Metal Weapon Safe
                CreateFacadeBox(safeInt.transform, "Weapon_Safe_Secure", new Vector3(-3.5f, 0.85f, 3f), new Vector3(0.85f, 1.7f, 0.75f), metalMat);
            }

            // 4. Supermarket: Checkout Conveyor Counter & Gondola Shelving Aisles
            if (interiorParent.Find("Supermarket_Interiors") == null)
            {
                GameObject supInt = new GameObject("Supermarket_Interiors");
                supInt.transform.SetParent(interiorParent);
                supInt.transform.position = new Vector3(36f, 0f, 32f);

                // 2 Checkout Conveyor Belts
                CreateFacadeBox(supInt.transform, "Checkout_Lane_1", new Vector3(-4f, 0.50f, -4f), new Vector3(1.1f, 0.95f, 3.2f), metalMat);
                CreateFacadeBox(supInt.transform, "Cash_Register_1", new Vector3(-4f, 1.10f, -3.2f), new Vector3(0.4f, 0.3f, 0.4f), screenMat);

                CreateFacadeBox(supInt.transform, "Checkout_Lane_2", new Vector3(-1.5f, 0.50f, -4f), new Vector3(1.1f, 0.95f, 3.2f), metalMat);
                CreateFacadeBox(supInt.transform, "Cash_Register_2", new Vector3(-1.5f, 1.10f, -3.2f), new Vector3(0.4f, 0.3f, 0.4f), screenMat);

                // Double-Sided Grocery Gondola Shelving Aisles
                for (int a = 0; a < 3; a++)
                {
                    CreateFacadeBox(supInt.transform, $"Gondola_Aisle_{a + 1}", new Vector3(2f + a * 2.5f, 1.05f, 1f), new Vector3(1.2f, 2.1f, 6.5f), metalMat);
                }
            }

            // 5. Gas Station: Service Bay Tool Chest & Cashier Island
            if (interiorParent.Find("GasStation_Interiors") == null)
            {
                GameObject gasInt = new GameObject("GasStation_Interiors");
                gasInt.transform.SetParent(interiorParent);
                gasInt.transform.position = new Vector3(58f, 0f, -26f);

                // Red Mechanics Tool Chest
                Material toolMat = SceneBuilder.GetOrCreateMaterial("Mat_ToolChest_Red", new Color(0.82f, 0.18f, 0.14f));
                CreateFacadeBox(gasInt.transform, "Tool_Chest_Rollaround", new Vector3(3f, 0.65f, 2f), new Vector3(1.4f, 1.25f, 0.75f), toolMat);

                // Oil Drum Pyramid
                CreateFacadeCylinder(gasInt.transform, "Oil_Drum_1", new Vector3(3f, 0.45f, -1f), new Vector3(0.65f, 0.85f, 0.65f), metalMat);
                CreateFacadeCylinder(gasInt.transform, "Oil_Drum_2", new Vector3(3.6f, 0.45f, -1f), new Vector3(0.65f, 0.85f, 0.65f), metalMat);
                CreateFacadeCylinder(gasInt.transform, "Oil_Drum_Top", new Vector3(3.3f, 1.25f, -1f), new Vector3(0.65f, 0.85f, 0.65f), metalMat);
            }

            Debug.Log("[ArchitecturalSetDresser] Populated Dense Interior Props across Key Facilities.");
        }

        private static void CreateHospitalBed(Transform parent, Vector3 pos, Material frameMat, Material mattressMat, Material sheetMat)
        {
            GameObject bed = new GameObject("Hospital_Bed_Rig");
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = pos;

            // Frame & Castors
            CreateFacadeBox(bed.transform, "Metal_Frame", new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.45f, 2.2f), frameMat);
            CreateFacadeBox(bed.transform, "Headboard", new Vector3(0f, 0.75f, 1.05f), new Vector3(1.1f, 0.75f, 0.08f), frameMat);
            CreateFacadeBox(bed.transform, "Footboard", new Vector3(0f, 0.65f, -1.05f), new Vector3(1.1f, 0.55f, 0.08f), frameMat);

            // Foam Mattress & Folded Sheet
            CreateFacadeBox(bed.transform, "Mattress", new Vector3(0f, 0.62f, 0f), new Vector3(1.0f, 0.22f, 2.05f), mattressMat);
            CreateFacadeBox(bed.transform, "Blanket_Fold", new Vector3(0f, 0.68f, -0.3f), new Vector3(1.02f, 0.12f, 1.3f), sheetMat);

            // Chrome IV Drip Stand
            GameObject ivStand = new GameObject("IV_Stand");
            ivStand.transform.SetParent(bed.transform, false);
            ivStand.transform.localPosition = new Vector3(-0.75f, 0f, 0.8f);

            CreateFacadeCylinder(ivStand.transform, "Pole", new Vector3(0f, 0.95f, 0f), new Vector3(0.04f, 1.9f, 0.04f), frameMat);
            CreateFacadeBox(ivStand.transform, "Hook_Cross", new Vector3(0f, 1.85f, 0f), new Vector3(0.35f, 0.04f, 0.04f), frameMat);
            CreateFacadeBox(ivStand.transform, "Saline_Bag", new Vector3(0.12f, 1.70f, 0f), new Vector3(0.08f, 0.18f, 0.04f), mattressMat);
        }

        #endregion

        #region Helpers

        private static GameObject CreateFacadeBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = size;
            if (rot.HasValue) obj.transform.localRotation = rot.Value;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.NavigationStatic);
            return obj;
        }

        private static GameObject CreateFacadeCylinder(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = size;
            if (rot.HasValue) obj.transform.localRotation = rot.Value;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.NavigationStatic);
            return obj;
        }

        #endregion
    }
}
