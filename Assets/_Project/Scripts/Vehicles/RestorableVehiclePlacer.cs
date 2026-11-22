using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Phase 1: Studio-Grade Restorable Vehicle Placer & Multi-Archetype Rigging Engine.
    /// Spawns and configures 5 distinct hero vehicles across the 720x720m open world:
    /// 1. Vehicle_SafeHouse_Pickup: Outside SafeHouse (12.5, 0, -8), 12 slots, 45% Fuel, 40% HP.
    /// 2. Vehicle_Police_Interceptor: Police Station (-42, 0, 36), 10 slots, 40% Fuel, 50% HP, roof red/blue siren lure.
    /// 3. Vehicle_Forest_RangerJeep: West Park (-215, 0, 18), 10 slots, 45% Fuel, 55% HP, lifted 0.28m suspension.
    /// 4. Vehicle_RailYard_CargoVan: East Rail Yard (210, 0, -24), 16 slots, 50% Fuel, 35% HP, enclosed cargo van.
    /// 5. Vehicle_NorthHighway_ArmoredSUV: North FOB (14, 0, 195), 12 slots, 30% Fuel, 60% HP, reinforced steel bullbar.
    /// </summary>
    public static class RestorableVehiclePlacer
    {
        private static Material matTire;
        private static Material matRim;
        private static Material matGlass;
        private static Material matMetalDark;
        private static Material matPickupWeathered;
        private static Material matBrakeLightEmissive;
        private static Material matGrille;

        // Specialized Archetype Materials
        private static Material matPoliceBlack;
        private static Material matPoliceWhite;
        private static Material matRangerJeep;
        private static Material matCargoVan;
        private static Material matArmoredSUV;
        private static Material matSirenRed;
        private static Material matSirenBlue;
        private static Material matHeadlightLens;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureAllRestorableVehicles()
        {
            PlaceAllRestorableVehicles();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Project Survival/Vehicles/Place All 5 Restorable Hero Vehicles (Step 1C)")]
        public static void PlaceVehiclesInEditor()
        {
            PlaceAllRestorableVehicles();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            Debug.Log("[RestorableVehiclePlacer] All 5 Hero Vehicles placed and fully rigged in active scene.");
        }
#endif

        public static GameObject PlaceSafeHousePickup()
        {
            PlaceAllRestorableVehicles();
            return GameObject.Find("Vehicle_SafeHouse_Pickup");
        }

        public static void PlaceAllRestorableVehicles()
        {
            InitializeMaterials();

            GameObject vehiclesRoot = GameObject.Find("--- RESTORABLE HERO VEHICLES ---");
            if (vehiclesRoot == null)
            {
                vehiclesRoot = new GameObject("--- RESTORABLE HERO VEHICLES ---");
            }

            // 1. SafeHouse Utility Pickup
            CreateOrUpgradeHeroVehicle(
                vehiclesRoot.transform,
                "Vehicle_SafeHouse_Pickup",
                "Pikap Kamyonet",
                HeroVehicleType.SafeHouse_Pickup,
                new Vector3(12.5f, 0.0f, -8.0f),
                Quaternion.Euler(0f, 0f, 0f),
                initialHP: 40f,
                initialFuel: 45f,
                trunkSlots: 12,
                bodyMat: matPickupWeathered
            );

            // 2. Police Cruiser Interceptor
            CreateOrUpgradeHeroVehicle(
                vehiclesRoot.transform,
                "Vehicle_Police_Interceptor",
                "Polis Devriye Aracı",
                HeroVehicleType.Police_Interceptor,
                new Vector3(-42.0f, 0.0f, 36.0f),
                Quaternion.Euler(0f, 90f, 0f),
                initialHP: 50f,
                initialFuel: 40f,
                trunkSlots: 10,
                bodyMat: matPoliceBlack
            );

            // 3. West Park Forest Ranger Jeep
            CreateOrUpgradeHeroVehicle(
                vehiclesRoot.transform,
                "Vehicle_Forest_RangerJeep",
                "Orman Devriye Jipi",
                HeroVehicleType.Forest_RangerJeep,
                new Vector3(-215.0f, 0.0f, 18.0f),
                Quaternion.Euler(0f, 45f, 0f),
                initialHP: 55f,
                initialFuel: 45f,
                trunkSlots: 10,
                bodyMat: matRangerJeep
            );

            // 4. East Rail Yard Heavy Cargo Van
            CreateOrUpgradeHeroVehicle(
                vehiclesRoot.transform,
                "Vehicle_RailYard_CargoVan",
                "Ağır Kargo Minibüsü",
                HeroVehicleType.RailYard_CargoVan,
                new Vector3(210.0f, 0.0f, -24.0f),
                Quaternion.Euler(0f, -45f, 0f),
                initialHP: 35f,
                initialFuel: 50f,
                trunkSlots: 16,
                bodyMat: matCargoVan
            );

            // 5. North Highway Armored SUV
            CreateOrUpgradeHeroVehicle(
                vehiclesRoot.transform,
                "Vehicle_NorthHighway_ArmoredSUV",
                "Zırhlı Askeri SUV",
                HeroVehicleType.NorthHighway_ArmoredSUV,
                new Vector3(14.0f, 0.0f, 195.0f),
                Quaternion.Euler(0f, 180f, 0f),
                initialHP: 60f,
                initialFuel: 30f,
                trunkSlots: 12,
                bodyMat: matArmoredSUV
            );
        }

        private static void InitializeMaterials()
        {
            matTire = GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f), 0.20f, 0.05f);
            matRim = GetOrCreateMaterial("Mat_Vehicle_AlloyRim", new Color(0.72f, 0.74f, 0.76f), 0.85f, 0.85f);
            matGlass = GetOrCreateMaterial("Mat_Vehicle_Glass", new Color(0.12f, 0.18f, 0.25f, 0.80f), 0.95f, 0.10f);
            matMetalDark = GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.16f, 0.18f, 0.20f), 0.80f, 0.70f);
            matGrille = GetOrCreateMaterial("Mat_Grille_Black", new Color(0.10f, 0.10f, 0.10f), 0.40f, 0.30f);

            // Weathered utility pickup body material
#if UNITY_EDITOR
            matPickupWeathered = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AAA_PBR/MAT_Auto_RustMetal.mat");
#endif
            if (matPickupWeathered == null)
            {
                matPickupWeathered = GetOrCreateMaterial("Mat_Pickup_Weathered", new Color(0.35f, 0.26f, 0.22f), 0.55f, 0.35f);
            }

            // Crimson Brake Light Emissive Material (#FF1E27)
            matBrakeLightEmissive = GetOrCreateMaterial("Mat_BrakeLight_Emissive", new Color(1.0f, 0.12f, 0.15f), 0.80f, 0.10f);
            if (matBrakeLightEmissive.HasProperty("_EmissionColor"))
            {
                matBrakeLightEmissive.EnableKeyword("_EMISSION");
                matBrakeLightEmissive.SetColor("_EmissionColor", new Color(1.0f, 0.12f, 0.15f) * 2.2f);
            }

            // Headlight Lens Material
            matHeadlightLens = GetOrCreateMaterial("Mat_Headlight_Lens", new Color(1.0f, 0.96f, 0.84f), 0.90f, 0.10f);

            // Archetype materials
            matPoliceBlack = GetOrCreateMaterial("Mat_Police_Black", new Color(0.08f, 0.09f, 0.10f), 0.85f, 0.60f);
            matPoliceWhite = GetOrCreateMaterial("Mat_Police_White", new Color(0.92f, 0.94f, 0.96f), 0.80f, 0.15f);
            matRangerJeep = GetOrCreateMaterial("Mat_Ranger_Jeep", new Color(0.24f, 0.32f, 0.22f), 0.45f, 0.25f);
            matCargoVan = GetOrCreateMaterial("Mat_Cargo_Van", new Color(0.26f, 0.29f, 0.32f), 0.50f, 0.40f);
            matArmoredSUV = GetOrCreateMaterial("Mat_Armored_SUV", new Color(0.42f, 0.38f, 0.30f), 0.35f, 0.45f);

            matSirenRed = GetOrCreateMaterial("Mat_Siren_Red", new Color(0.95f, 0.15f, 0.18f), 0.85f, 0.10f);
            if (matSirenRed.HasProperty("_EmissionColor"))
            {
                matSirenRed.EnableKeyword("_EMISSION");
                matSirenRed.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.20f) * 3.0f);
            }

            matSirenBlue = GetOrCreateMaterial("Mat_Siren_Blue", new Color(0.18f, 0.45f, 0.98f), 0.85f, 0.10f);
            if (matSirenBlue.HasProperty("_EmissionColor"))
            {
                matSirenBlue.EnableKeyword("_EMISSION");
                matSirenBlue.SetColor("_EmissionColor", new Color(0.20f, 0.50f, 1.0f) * 3.0f);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color col, float smoothness, float metallic)
        {
            Material mat = Resources.Load<Material>(name);
            if (mat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(s);
                mat.name = name;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                else mat.color = col;

                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            }
            return mat;
        }

        public static GameObject CreateOrUpgradeHeroVehicle(
            Transform parent,
            string objectName,
            string displayName,
            HeroVehicleType type,
            Vector3 worldPos,
            Quaternion worldRot,
            float initialHP,
            float initialFuel,
            int trunkSlots,
            Material bodyMat)
        {
            GameObject root = GameObject.Find(objectName);
            if (root == null)
            {
                root = new GameObject(objectName);
                root.transform.SetParent(parent);
            }

            root.transform.position = worldPos;
            root.transform.rotation = worldRot;

            // 1. Rigidbody Setup
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();

            float vehicleMass = 1450f;
            if (type == HeroVehicleType.RailYard_CargoVan) vehicleMass = 2100f;
            else if (type == HeroVehicleType.NorthHighway_ArmoredSUV) vehicleMass = 2350f;

            rb.mass = vehicleMass;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 0.65f;
            rb.centerOfMass = new Vector3(0f, -0.48f, 0.15f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 2. Hull Collider
            BoxCollider hullCol = root.GetComponent<BoxCollider>();
            if (hullCol == null) hullCol = root.AddComponent<BoxCollider>();
            hullCol.center = new Vector3(0f, 0.85f, 0.05f);
            hullCol.size = new Vector3(2.0f, 1.25f, 4.3f);

            // 3. AudioSource
            AudioSource aSrc = root.GetComponent<AudioSource>();
            if (aSrc == null) aSrc = root.AddComponent<AudioSource>();

            // 4. Controller & Interactable
            DrivableVehicleController controller = root.GetComponent<DrivableVehicleController>();
            if (controller == null) controller = root.AddComponent<DrivableVehicleController>();

            DrivableVehicleInteractable interactable = root.GetComponent<DrivableVehicleInteractable>();
            if (interactable == null) interactable = root.AddComponent<DrivableVehicleInteractable>();

            // 5. Visual_Model Hierarchy
            Transform visualModel = root.transform.Find("Visual_Model");
            if (visualModel == null)
            {
                GameObject vmGo = new GameObject("Visual_Model");
                vmGo.transform.SetParent(root.transform, false);
                visualModel = vmGo.transform;
            }

            for (int i = visualModel.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(visualModel.GetChild(i).gameObject);
            }

            // Build specialized archetype visuals
            Renderer redBeaconLens = null;
            Renderer blueBeaconLens = null;

            switch (type)
            {
                case HeroVehicleType.Police_Interceptor:
                    BuildPoliceCruiserVisuals(visualModel, matPoliceBlack, matPoliceWhite, out redBeaconLens, out blueBeaconLens);
                    break;
                case HeroVehicleType.Forest_RangerJeep:
                    BuildRangerJeepVisuals(visualModel, matRangerJeep);
                    break;
                case HeroVehicleType.RailYard_CargoVan:
                    BuildCargoVanVisuals(visualModel, matCargoVan);
                    break;
                case HeroVehicleType.NorthHighway_ArmoredSUV:
                    BuildArmoredSUVVisuals(visualModel, matArmoredSUV);
                    break;
                default:
                    BuildPickupVisuals(visualModel, bodyMat);
                    break;
            }

            // 6. Wheel_Colliders Hierarchy
            Transform wheelColRoot = root.transform.Find("Wheel_Colliders");
            if (wheelColRoot == null)
            {
                GameObject wcGo = new GameObject("Wheel_Colliders");
                wcGo.transform.SetParent(root.transform, false);
                wheelColRoot = wcGo.transform;
            }

            float xWheel = 0.95f;
            float zWheel = 1.35f;
            float yWheelCol = 0.38f;
            float suspDist = (type == HeroVehicleType.Forest_RangerJeep) ? 0.28f : 0.22f;

            WheelCollider wcFL = CreateOrGetWheelCollider(wheelColRoot, "WC_FL", new Vector3(-xWheel, yWheelCol, zWheel), suspDist);
            WheelCollider wcFR = CreateOrGetWheelCollider(wheelColRoot, "WC_FR", new Vector3(xWheel, yWheelCol, zWheel), suspDist);
            WheelCollider wcRL = CreateOrGetWheelCollider(wheelColRoot, "WC_RL", new Vector3(-xWheel, yWheelCol, -zWheel), suspDist);
            WheelCollider wcRR = CreateOrGetWheelCollider(wheelColRoot, "WC_RR", new Vector3(xWheel, yWheelCol, -zWheel), suspDist);

            // 7. Wheels_Visual Hierarchy
            Transform wheelVisualRoot = root.transform.Find("Wheels_Visual");
            if (wheelVisualRoot == null)
            {
                GameObject wvGo = new GameObject("Wheels_Visual");
                wvGo.transform.SetParent(root.transform, false);
                wheelVisualRoot = wvGo.transform;
            }
            for (int i = wheelVisualRoot.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(wheelVisualRoot.GetChild(i).gameObject);
            }

            float wheelRadius = (type == HeroVehicleType.Forest_RangerJeep) ? 0.40f : 0.36f;
            Transform wMeshFL = CreateDetailedVisualWheel(wheelVisualRoot, "Wheel_FL", new Vector3(-xWheel, yWheelCol, zWheel), wheelRadius);
            Transform wMeshFR = CreateDetailedVisualWheel(wheelVisualRoot, "Wheel_FR", new Vector3(xWheel, yWheelCol, zWheel), wheelRadius);
            Transform wMeshRL = CreateDetailedVisualWheel(wheelVisualRoot, "Wheel_RL", new Vector3(-xWheel, yWheelCol, -zWheel), wheelRadius);
            Transform wMeshRR = CreateDetailedVisualWheel(wheelVisualRoot, "Wheel_RR", new Vector3(xWheel, yWheelCol, -zWheel), wheelRadius);

            // 8. Lighting_FX Hierarchy
            Transform lightingRoot = root.transform.Find("Lighting_FX");
            if (lightingRoot == null)
            {
                GameObject lGo = new GameObject("Lighting_FX");
                lGo.transform.SetParent(root.transform, false);
                lightingRoot = lGo.transform;
            }
            for (int i = lightingRoot.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(lightingRoot.GetChild(i).gameObject);
            }

            Light headlightL = CreateHeadlight(lightingRoot, "Headlight_L", new Vector3(-0.65f, 0.75f, 2.25f));
            Light headlightR = CreateHeadlight(lightingRoot, "Headlight_R", new Vector3(0.65f, 0.75f, 2.25f));

            Renderer lensL = CreateHeadlightLens(lightingRoot, "Lens_L", new Vector3(-0.65f, 0.75f, 2.20f));
            Renderer lensR = CreateHeadlightLens(lightingRoot, "Lens_R", new Vector3(0.65f, 0.75f, 2.20f));

            Renderer brakeLightL = CreateBrakeLightMesh(lightingRoot, "BrakeLight_L", new Vector3(-0.85f, 0.78f, -2.22f));
            Renderer brakeLightR = CreateBrakeLightMesh(lightingRoot, "BrakeLight_R", new Vector3(0.85f, 0.78f, -2.22f));

            ParticleSystem exhaust = CreateExhaust(lightingRoot, new Vector3(-0.75f, 0.28f, -2.25f));

            // Police siren light emitters
            Light sirenRed = null;
            Light sirenBlue = null;
            if (type == HeroVehicleType.Police_Interceptor)
            {
                sirenRed = CreateSirenLight(lightingRoot, "Siren_Red", new Vector3(-0.45f, 1.82f, 0.15f), new Color(1.0f, 0.15f, 0.20f));
                sirenBlue = CreateSirenLight(lightingRoot, "Siren_Blue", new Vector3(0.45f, 1.82f, 0.15f), new Color(0.20f, 0.50f, 1.0f));
            }

            // 9. Cabin Seats & Exit Anchors
            Transform dSeat = CreateAnchor(root.transform, "DriverSeatAnchor", new Vector3(-0.45f, 0.95f, 0.20f));
            Transform pSeat = CreateAnchor(root.transform, "PassengerSeatAnchor", new Vector3(0.45f, 0.95f, 0.20f));
            Transform dExit = CreateAnchor(root.transform, "DriverExitAnchor", new Vector3(-1.95f, 0.15f, 0.20f));
            Transform pExit = CreateAnchor(root.transform, "PassengerExitAnchor", new Vector3(1.95f, 0.15f, 0.20f));

            // 10. Wire Controller
            controller.ConfigureComponents(
                type,
                objectName.ToLower(),
                displayName,
                initialHP,
                initialFuel,
                wMeshFL, wMeshFR, wMeshRL, wMeshRR,
                dSeat, pSeat,
                dExit, pExit,
                headlightL, headlightR,
                null, null,
                exhaust
            );

            controller.SetWheelColliders(wcFL, wcFR, wcRL, wcRR);
            Transform steeringWheel = visualModel.Find("Interior_SteeringWheel");
            controller.SetVisualElements(visualModel, steeringWheel);
            controller.SetBrakeLights(brakeLightL, brakeLightR);

            interactable.Configure(controller);

            // 11. Lighting and Acoustics Component (Step 1C)
            VehicleLightingAndAcoustics lightingModule = root.GetComponent<VehicleLightingAndAcoustics>();
            if (lightingModule == null) lightingModule = root.AddComponent<VehicleLightingAndAcoustics>();
            lightingModule.ConfigureLights(headlightL, headlightR, sirenRed, sirenBlue);
            lightingModule.ConfigureRenderers(lensL, lensR, redBeaconLens, blueBeaconLens);

            // 12. Front Kinetic Impact Sensor (Step 1C)
            VehicleImpactSensor impactSensor = root.GetComponent<VehicleImpactSensor>();
            if (impactSensor == null) impactSensor = root.AddComponent<VehicleImpactSensor>();

            // Ensure bumper trigger collider exists
            Transform bumperTrigger = root.transform.Find("Bumper_Trigger");
            if (bumperTrigger == null)
            {
                GameObject btGo = new GameObject("Bumper_Trigger");
                btGo.transform.SetParent(root.transform, false);
                btGo.transform.localPosition = new Vector3(0f, 0.65f, 2.22f);
                BoxCollider bc = btGo.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(2.1f, 0.75f, 0.6f);
            }

            // 13. Maintenance & Cargo Trunk Modules (Step 1B)
            VehicleMaintenanceModule maint = root.GetComponent<VehicleMaintenanceModule>();
            if (maint == null) maint = root.AddComponent<VehicleMaintenanceModule>();
            maint.SetState(initialFuel, initialHP);

            VehicleTrunkStorage trunk = root.GetComponent<VehicleTrunkStorage>();
            if (trunk == null) trunk = root.AddComponent<VehicleTrunkStorage>();
            trunk.SetCapacity(trunkSlots);

            Transform tailgateTr = visualModel.Find("Tailgate");
            if (tailgateTr != null) trunk.SetTailgateTransform(tailgateTr);

            // 14. Register Map Markers
            FullTacticalMapUI.Instance?.RegisterVehicleMarker(objectName, displayName, new Vector2(worldPos.x, worldPos.z));
            MinimapHUDWidget.Instance?.RegisterVehicleMarker(objectName, displayName, new Vector2(worldPos.x, worldPos.z));

            return root;
        }

        // =========================================================================
        // VISUAL MODEL BUILDERS
        // =========================================================================

        private static void BuildPickupVisuals(Transform visualModel, Material bodyMat)
        {
            CreateSubBox(visualModel, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(1.95f, 0.40f, 4.3f), matMetalDark);
            CreateSubBox(visualModel, "Hood", new Vector3(0f, 0.85f, 1.45f), new Vector3(1.85f, 0.35f, 1.4f), bodyMat);
            CreateSubBox(visualModel, "Front_Grille", new Vector3(0f, 0.75f, 2.18f), new Vector3(1.65f, 0.30f, 0.08f), matGrille);
            CreateSubBox(visualModel, "Front_Bumper", new Vector3(0f, 0.52f, 2.22f), new Vector3(1.95f, 0.22f, 0.20f), matMetalDark);
            CreateSubBox(visualModel, "Cab", new Vector3(0f, 1.25f, 0.25f), new Vector3(1.75f, 0.75f, 1.8f), bodyMat);
            CreateSubBox(visualModel, "Windshield", new Vector3(0f, 1.30f, 1.15f), new Vector3(1.65f, 0.55f, 0.08f), matGlass, Quaternion.Euler(28f, 0f, 0f));
            CreateSubBox(visualModel, "RearWindow", new Vector3(0f, 1.32f, -0.62f), new Vector3(1.50f, 0.45f, 0.08f), matGlass);
            CreateSubBox(visualModel, "SideWindow_L", new Vector3(-0.86f, 1.30f, 0.25f), new Vector3(0.06f, 0.45f, 1.1f), matGlass);
            CreateSubBox(visualModel, "SideWindow_R", new Vector3(0.86f, 1.30f, 0.25f), new Vector3(0.06f, 0.45f, 1.1f), matGlass);
            CreateSubBox(visualModel, "Mirror_L", new Vector3(-1.05f, 1.15f, 1.05f), new Vector3(0.12f, 0.16f, 0.22f), matMetalDark);
            CreateSubBox(visualModel, "Mirror_R", new Vector3(1.05f, 1.15f, 1.05f), new Vector3(0.12f, 0.16f, 0.22f), matMetalDark);
            CreateSubBox(visualModel, "Bed_Floor", new Vector3(0f, 0.70f, -1.40f), new Vector3(1.75f, 0.12f, 1.9f), matMetalDark);
            CreateSubBox(visualModel, "Bed_Wall_L", new Vector3(-0.92f, 0.98f, -1.40f), new Vector3(0.12f, 0.48f, 1.9f), bodyMat);
            CreateSubBox(visualModel, "Bed_Wall_R", new Vector3(0.92f, 0.98f, -1.40f), new Vector3(0.12f, 0.48f, 1.9f), bodyMat);
            CreateSubBox(visualModel, "Tailgate", new Vector3(0f, 0.98f, -2.32f), new Vector3(1.85f, 0.48f, 0.12f), bodyMat);
            CreateSubBox(visualModel, "Rear_Bumper", new Vector3(0f, 0.52f, -2.25f), new Vector3(1.95f, 0.20f, 0.18f), matMetalDark);
            BuildSteeringWheel(visualModel);
        }

        private static void BuildPoliceCruiserVisuals(Transform visualModel, Material blackMat, Material whiteMat, out Renderer redBeacon, out Renderer blueBeacon)
        {
            CreateSubBox(visualModel, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(1.95f, 0.38f, 4.4f), blackMat);
            CreateSubBox(visualModel, "Hood", new Vector3(0f, 0.85f, 1.45f), new Vector3(1.85f, 0.30f, 1.5f), blackMat);
            CreateSubBox(visualModel, "Trunk_Lid", new Vector3(0f, 0.85f, -1.45f), new Vector3(1.85f, 0.30f, 1.3f), blackMat);
            CreateSubBox(visualModel, "Doors_White_L", new Vector3(-0.92f, 0.85f, 0f), new Vector3(0.12f, 0.45f, 1.8f), whiteMat);
            CreateSubBox(visualModel, "Doors_White_R", new Vector3(0.92f, 0.85f, 0f), new Vector3(0.12f, 0.45f, 1.8f), whiteMat);
            CreateSubBox(visualModel, "Roof_White", new Vector3(0f, 1.52f, 0f), new Vector3(1.60f, 0.10f, 1.7f), whiteMat);
            CreateSubBox(visualModel, "Front_Grille", new Vector3(0f, 0.72f, 2.22f), new Vector3(1.65f, 0.28f, 0.08f), matGrille);
            CreateSubBox(visualModel, "Push_Bumper", new Vector3(0f, 0.55f, 2.28f), new Vector3(1.95f, 0.30f, 0.22f), matMetalDark);

            CreateSubBox(visualModel, "Windshield", new Vector3(0f, 1.25f, 0.95f), new Vector3(1.65f, 0.50f, 0.08f), matGlass, Quaternion.Euler(32f, 0f, 0f));
            CreateSubBox(visualModel, "RearWindow", new Vector3(0f, 1.25f, -0.95f), new Vector3(1.55f, 0.48f, 0.08f), matGlass, Quaternion.Euler(-30f, 0f, 0f));
            CreateSubBox(visualModel, "SideWindow_L", new Vector3(-0.84f, 1.25f, 0f), new Vector3(0.06f, 0.42f, 1.7f), matGlass);
            CreateSubBox(visualModel, "SideWindow_R", new Vector3(0.84f, 1.25f, 0f), new Vector3(0.06f, 0.42f, 1.7f), matGlass);

            // Roof Lightbar Frame
            CreateSubBox(visualModel, "Lightbar_Base", new Vector3(0f, 1.62f, 0.15f), new Vector3(1.25f, 0.06f, 0.25f), matMetalDark);
            GameObject redB = CreateSubBox(visualModel, "Beacon_Red", new Vector3(-0.42f, 1.68f, 0.15f), new Vector3(0.35f, 0.10f, 0.22f), matSirenRed);
            GameObject blueB = CreateSubBox(visualModel, "Beacon_Blue", new Vector3(0.42f, 1.68f, 0.15f), new Vector3(0.35f, 0.10f, 0.22f), matSirenBlue);

            redBeacon = redB.GetComponent<Renderer>();
            blueBeacon = blueB.GetComponent<Renderer>();

            BuildSteeringWheel(visualModel);
        }

        private static void BuildRangerJeepVisuals(Transform visualModel, Material oliveMat)
        {
            CreateSubBox(visualModel, "Chassis", new Vector3(0f, 0.65f, 0f), new Vector3(1.85f, 0.42f, 3.8f), oliveMat);
            CreateSubBox(visualModel, "Hood", new Vector3(0f, 0.98f, 1.25f), new Vector3(1.75f, 0.38f, 1.4f), oliveMat);
            CreateSubBox(visualModel, "Front_Grille", new Vector3(0f, 0.90f, 1.96f), new Vector3(1.55f, 0.32f, 0.08f), matGrille);
            CreateSubBox(visualModel, "Front_Bumper", new Vector3(0f, 0.62f, 2.02f), new Vector3(1.90f, 0.24f, 0.22f), matMetalDark);
            CreateSubBox(visualModel, "Windshield", new Vector3(0f, 1.40f, 0.62f), new Vector3(1.65f, 0.55f, 0.08f), matGlass, Quaternion.Euler(15f, 0f, 0f));

            // Tubular Rollcage Frame
            CreateSubBox(visualModel, "Rollbar_L", new Vector3(-0.82f, 1.55f, -0.40f), new Vector3(0.08f, 0.85f, 0.08f), matMetalDark);
            CreateSubBox(visualModel, "Rollbar_R", new Vector3(0.82f, 1.55f, -0.40f), new Vector3(0.08f, 0.85f, 0.08f), matMetalDark);
            CreateSubBox(visualModel, "Rollbar_Top", new Vector3(0f, 1.95f, -0.40f), new Vector3(1.72f, 0.08f, 0.08f), matMetalDark);
            CreateSubBox(visualModel, "Rollbar_Strut_L", new Vector3(-0.82f, 1.45f, -1.20f), new Vector3(0.08f, 0.08f, 1.6f), matMetalDark, Quaternion.Euler(-28f, 0f, 0f));
            CreateSubBox(visualModel, "Rollbar_Strut_R", new Vector3(0.82f, 1.45f, -1.20f), new Vector3(0.08f, 0.08f, 1.6f), matMetalDark, Quaternion.Euler(-28f, 0f, 0f));

            // Spare tire on tailgate
            GameObject spareTire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spareTire.name = "Spare_Tire";
            spareTire.transform.SetParent(visualModel, false);
            spareTire.transform.localPosition = new Vector3(0f, 0.95f, -1.98f);
            spareTire.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spareTire.transform.localScale = new Vector3(0.72f, 0.18f, 0.72f);
            if (matTire != null) spareTire.GetComponent<Renderer>().sharedMaterial = matTire;
            Collider stc = spareTire.GetComponent<Collider>();
            if (stc != null) Object.DestroyImmediate(stc);

            BuildSteeringWheel(visualModel);
        }

        private static void BuildCargoVanVisuals(Transform visualModel, Material vanMat)
        {
            CreateSubBox(visualModel, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(2.05f, 0.38f, 4.8f), vanMat);
            CreateSubBox(visualModel, "Short_Hood", new Vector3(0f, 0.90f, 1.85f), new Vector3(1.95f, 0.40f, 0.9f), vanMat);
            CreateSubBox(visualModel, "Front_Grille", new Vector3(0f, 0.75f, 2.32f), new Vector3(1.75f, 0.32f, 0.08f), matGrille);
            CreateSubBox(visualModel, "Front_Bumper", new Vector3(0f, 0.52f, 2.38f), new Vector3(2.05f, 0.24f, 0.22f), matMetalDark);

            // Large Enclosed Cargo Box
            CreateSubBox(visualModel, "Cargo_Enclosure", new Vector3(0f, 1.45f, -0.45f), new Vector3(1.98f, 1.45f, 3.7f), vanMat);
            CreateSubBox(visualModel, "Cab_Windshield", new Vector3(0f, 1.40f, 1.42f), new Vector3(1.85f, 0.65f, 0.08f), matGlass, Quaternion.Euler(34f, 0f, 0f));
            CreateSubBox(visualModel, "SideWindow_L", new Vector3(-0.99f, 1.40f, 0.85f), new Vector3(0.06f, 0.55f, 0.9f), matGlass);
            CreateSubBox(visualModel, "SideWindow_R", new Vector3(0.99f, 1.40f, 0.85f), new Vector3(0.06f, 0.55f, 0.9f), matGlass);

            // Rear Cargo Doors
            CreateSubBox(visualModel, "Rear_Cargo_Door_L", new Vector3(-0.48f, 1.45f, -2.32f), new Vector3(0.95f, 1.35f, 0.10f), matMetalDark);
            CreateSubBox(visualModel, "Rear_Cargo_Door_R", new Vector3(0.48f, 1.45f, -2.32f), new Vector3(0.95f, 1.35f, 0.10f), matMetalDark);

            BuildSteeringWheel(visualModel);
        }

        private static void BuildArmoredSUVVisuals(Transform visualModel, Material suvMat)
        {
            CreateSubBox(visualModel, "Chassis", new Vector3(0f, 0.60f, 0f), new Vector3(2.10f, 0.42f, 4.6f), suvMat);
            CreateSubBox(visualModel, "Hood", new Vector3(0f, 0.98f, 1.40f), new Vector3(2.00f, 0.38f, 1.6f), suvMat);
            CreateSubBox(visualModel, "Cab_Armored", new Vector3(0f, 1.42f, -0.30f), new Vector3(1.98f, 1.05f, 3.2f), suvMat);

            // Armored Slit Windows
            CreateSubBox(visualModel, "Windshield", new Vector3(0f, 1.35f, 1.15f), new Vector3(1.75f, 0.35f, 0.12f), matGlass, Quaternion.Euler(24f, 0f, 0f));
            CreateSubBox(visualModel, "Window_L", new Vector3(-1.02f, 1.35f, 0f), new Vector3(0.08f, 0.32f, 2.2f), matGlass);
            CreateSubBox(visualModel, "Window_R", new Vector3(1.02f, 1.35f, 0f), new Vector3(0.08f, 0.32f, 2.2f), matGlass);

            // Heavy Steel Reinforced Front Bullbar (Protective ramming cage)
            CreateSubBox(visualModel, "Bullbar_Bottom", new Vector3(0f, 0.52f, 2.38f), new Vector3(2.15f, 0.16f, 0.28f), matMetalDark);
            CreateSubBox(visualModel, "Bullbar_Top", new Vector3(0f, 1.05f, 2.32f), new Vector3(1.85f, 0.12f, 0.18f), matMetalDark);
            CreateSubBox(visualModel, "Bullbar_Strut_L", new Vector3(-0.75f, 0.78f, 2.35f), new Vector3(0.12f, 0.55f, 0.16f), matMetalDark);
            CreateSubBox(visualModel, "Bullbar_Strut_R", new Vector3(0.75f, 0.78f, 2.35f), new Vector3(0.12f, 0.55f, 0.16f), matMetalDark);
            CreateSubBox(visualModel, "Bullbar_Mesh_Guard", new Vector3(0f, 0.78f, 2.32f), new Vector3(1.45f, 0.42f, 0.05f), matGrille);

            BuildSteeringWheel(visualModel);
        }

        private static void BuildSteeringWheel(Transform visualModel)
        {
            CreateSubBox(visualModel, "Interior_Dashboard", new Vector3(0f, 0.98f, 0.85f), new Vector3(1.55f, 0.25f, 0.45f), matMetalDark);

            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sw.name = "Interior_SteeringWheel";
            sw.transform.SetParent(visualModel, false);
            sw.transform.localPosition = new Vector3(-0.45f, 1.05f, 0.65f);
            sw.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            sw.transform.localScale = new Vector3(0.36f, 0.04f, 0.36f);
            if (matMetalDark != null) sw.GetComponent<Renderer>().sharedMaterial = matMetalDark;
            Collider swc = sw.GetComponent<Collider>();
            if (swc != null) Object.DestroyImmediate(swc);
        }

        // =========================================================================
        // WHEELS & SUSPENSION
        // =========================================================================

        private static WheelCollider CreateOrGetWheelCollider(Transform parent, string name, Vector3 pos, float suspDistance = 0.22f)
        {
            Transform existing = parent.Find(name);
            GameObject go = (existing != null) ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;

            WheelCollider wc = go.GetComponent<WheelCollider>();
            if (wc == null) wc = go.AddComponent<WheelCollider>();

            wc.suspensionDistance = suspDistance;
            JointSpring spring = wc.suspensionSpring;
            spring.spring = 28000f;
            spring.damper = 3800f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;
            wc.radius = 0.36f;
            wc.mass = 35f;

            WheelFrictionCurve ff = wc.forwardFriction;
            ff.extremumSlip = 0.2f;
            ff.extremumValue = 1.0f;
            ff.asymptoteSlip = 0.5f;
            ff.asymptoteValue = 0.8f;
            ff.stiffness = 1.5f;
            wc.forwardFriction = ff;

            WheelFrictionCurve sf = wc.sidewaysFriction;
            sf.extremumSlip = 0.2f;
            sf.extremumValue = 1.0f;
            sf.asymptoteSlip = 0.5f;
            sf.asymptoteValue = 0.75f;
            sf.stiffness = 1.6f;
            wc.sidewaysFriction = sf;

            return wc;
        }

        private static Transform CreateDetailedVisualWheel(Transform parent, string name, Vector3 pos, float radius)
        {
            GameObject wheelRoot = new GameObject(name);
            wheelRoot.transform.SetParent(parent, false);
            wheelRoot.transform.localPosition = pos;

            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Tire_Rubber";
            tire.transform.SetParent(wheelRoot.transform, false);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = new Vector3(radius * 2f, 0.18f, radius * 2f);
            if (matTire != null) tire.GetComponent<Renderer>().sharedMaterial = matTire;
            Collider tc = tire.GetComponent<Collider>();
            if (tc != null) Object.DestroyImmediate(tc);

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Alloy_Rim";
            rim.transform.SetParent(wheelRoot.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(radius * 1.35f, 0.19f, radius * 1.35f);
            if (matRim != null) rim.GetComponent<Renderer>().sharedMaterial = matRim;
            Collider rc = rim.GetComponent<Collider>();
            if (rc != null) Object.DestroyImmediate(rc);

            return wheelRoot.transform;
        }

        private static GameObject CreateSubBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = pos;
            cube.transform.localScale = size;
            cube.transform.localRotation = rot.HasValue ? rot.Value : Quaternion.identity;

            if (mat != null)
            {
                Renderer r = cube.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            Collider c = cube.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            return cube;
        }

        private static Light CreateHeadlight(Transform parent, string name, Vector3 pos)
        {
            GameObject lObj = new GameObject(name);
            lObj.transform.SetParent(parent, false);
            lObj.transform.localPosition = pos;
            lObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 34f;
            l.spotAngle = 58f;
            l.intensity = 4.5f;
            l.color = new Color(1.0f, 0.95f, 0.84f);
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private static Renderer CreateHeadlightLens(Transform parent, string name, Vector3 pos)
        {
            GameObject lensObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lensObj.name = name;
            lensObj.transform.SetParent(parent, false);
            lensObj.transform.localPosition = pos;
            lensObj.transform.localScale = new Vector3(0.24f, 0.16f, 0.05f);

            Collider c = lensObj.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            Renderer r = lensObj.GetComponent<Renderer>();
            if (matHeadlightLens != null) r.sharedMaterial = matHeadlightLens;
            return r;
        }

        private static Light CreateSirenLight(Transform parent, string name, Vector3 pos, Color col)
        {
            GameObject lObj = new GameObject(name);
            lObj.transform.SetParent(parent, false);
            lObj.transform.localPosition = pos;

            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 28f;
            l.spotAngle = 110f;
            l.intensity = 5.0f;
            l.color = col;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private static Renderer CreateBrakeLightMesh(Transform parent, string name, Vector3 pos)
        {
            GameObject bObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bObj.name = name;
            bObj.transform.SetParent(parent, false);
            bObj.transform.localPosition = pos;
            bObj.transform.localScale = new Vector3(0.24f, 0.18f, 0.08f);

            Collider c = bObj.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            Renderer r = bObj.GetComponent<Renderer>();
            if (matBrakeLightEmissive != null) r.sharedMaterial = matBrakeLightEmissive;
            return r;
        }

        private static ParticleSystem CreateExhaust(Transform parent, Vector3 pos)
        {
            GameObject exObj = new GameObject("Exhaust_Emitter");
            exObj.transform.SetParent(parent, false);
            exObj.transform.localPosition = pos;
            exObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            ParticleSystem ps = exObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSize = 0.35f;
            main.startLifetime = 0.65f;
            main.startSpeed = 2.5f;
            main.startColor = new Color(0.6f, 0.6f, 0.65f, 0.35f);
            main.maxParticles = 30;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;

            ParticleSystemRenderer rend = exObj.GetComponent<ParticleSystemRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            rend.sharedMaterial = new Material(unlit);

            ps.Stop();
            return ps;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 localPos)
        {
            Transform existing = parent.Find(name);
            GameObject a = existing != null ? existing.gameObject : new GameObject(name);
            a.transform.SetParent(parent, false);
            a.transform.localPosition = localPos;
            return a.transform;
        }
    }
}
