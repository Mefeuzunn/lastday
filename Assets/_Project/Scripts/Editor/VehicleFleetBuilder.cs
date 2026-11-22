using UnityEngine;
using UnityEditor;

namespace ProjectSurvival.EditorTools
{
    public static class VehicleFleetBuilder
    {
        public static void PlaceVehicleFleet(Transform parent)
        {
            GameObject fleetParent = new GameObject("--- VEHICLE FLEET ---");
            fleetParent.transform.SetParent(parent);

            // 1. Police Cars
            CreatePoliceCar(fleetParent.transform, "Vehicle_Police_Intersection", new Vector3(3.2f, 0.1f, -12f), 18f, flashingSiren: true);
            CreatePoliceCar(fleetParent.transform, "Vehicle_Police_Crashed", new Vector3(-8.5f, 0.1f, 48f), -35f, openDoor: true);
            CreatePoliceCar(fleetParent.transform, "Vehicle_Police_Barricade", new Vector3(28f, 0.1f, 15f), 82f, flashingSiren: false);

            // 2. Yellow Taxis
            CreateTaxi(fleetParent.transform, "Vehicle_Taxi_Avenue1", new Vector3(-2.8f, 0.1f, -42f), 5f);
            CreateTaxi(fleetParent.transform, "Vehicle_Taxi_Avenue2", new Vector3(3.5f, 0.1f, 18f), -12f);
            CreateTaxi(fleetParent.transform, "Vehicle_Taxi_Crossroad", new Vector3(-32f, 0.1f, 0f), 92f);
            CreateTaxi(fleetParent.transform, "Vehicle_Taxi_Subway", new Vector3(45f, 0.1f, -35f), 140f);

            // 3. Ambulances
            CreateAmbulance(fleetParent.transform, "Vehicle_Ambulance_Clinic", new Vector3(-32f, 0.1f, 32f), -45f, rearDoorsOpen: true);
            CreateAmbulance(fleetParent.transform, "Vehicle_Ambulance_Downtown", new Vector3(15f, 0.1f, -25f), 65f, rearDoorsOpen: false);

            // 4. Family Sedans (Variety of colors)
            Material matSedanNavy = SceneBuilder.GetOrCreateMaterial("Mat_Sedan_Navy", new Color(0.12f, 0.22f, 0.38f));
            Material matSedanWhite = SceneBuilder.GetOrCreateMaterial("Mat_Sedan_White", new Color(0.88f, 0.88f, 0.90f));
            Material matSedanGray = SceneBuilder.GetOrCreateMaterial("Mat_Sedan_Gray", new Color(0.35f, 0.36f, 0.38f));
            Material matSedanRust = SceneBuilder.GetOrCreateMaterial("Mat_Sedan_Rust", new Color(0.48f, 0.22f, 0.16f));

            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Navy", new Vector3(-3.2f, 0.1f, -75f), 12f, matSedanNavy);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_White", new Vector3(2.8f, 0.1f, 65f), -8f, matSedanWhite, doorOpen: true);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Gray", new Vector3(-58f, 0.1f, 22f), 85f, matSedanGray);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Rust", new Vector3(-18f, 0.1f, 85f), 110f, matSedanRust, hoodOpen: true);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Market", new Vector3(38f, 0.1f, 42f), -72f, matSedanNavy);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Driveway1", new Vector3(-72f, 0.1f, -15f), 30f, matSedanGray);
            CreateSedan(fleetParent.transform, "Vehicle_Sedan_Driveway2", new Vector3(-85f, 0.1f, 45f), -20f, matSedanWhite);

            // 5. SUVs / Off-roaders
            Material matSUVOlive = SceneBuilder.GetOrCreateMaterial("Mat_SUV_Olive", new Color(0.25f, 0.32f, 0.22f));
            Material matSUVBlack = SceneBuilder.GetOrCreateMaterial("Mat_SUV_Black", new Color(0.12f, 0.13f, 0.14f));
            Material matSUVSilver = SceneBuilder.GetOrCreateMaterial("Mat_SUV_Silver", new Color(0.65f, 0.67f, 0.70f));

            CreateSUV(fleetParent.transform, "Vehicle_SUV_Olive", new Vector3(4.2f, 0.1f, -2f), -22f, matSUVOlive);
            CreateSUV(fleetParent.transform, "Vehicle_SUV_Black", new Vector3(-68f, 0.1f, -48f), 45f, matSUVBlack);
            CreateSUV(fleetParent.transform, "Vehicle_SUV_Silver", new Vector3(55f, 0.1f, -65f), 165f, matSUVSilver);
            CreateSUV(fleetParent.transform, "Vehicle_SUV_Overturned", new Vector3(-45f, 0.1f, 75f), 40f, matSUVBlack, overturned: true);

            // 6. Cargo Delivery Vans
            CreateCargoVan(fleetParent.transform, "Vehicle_Van_Supermarket", new Vector3(48f, 0.1f, 28f), 90f);
            CreateCargoVan(fleetParent.transform, "Vehicle_Van_Alley", new Vector3(-18f, 0.1f, -32f), -85f);
            CreateCargoVan(fleetParent.transform, "Vehicle_Van_GasStation", new Vector3(68f, 0.1f, -22f), 15f);

            // 7. Heavy Semi-Truck & Trailer Barricades (Tactical Choke Points)
            CreateHeavyTruck(fleetParent.transform, "Vehicle_HeavyTruck_NorthBarricade", new Vector3(0f, 0.1f, 105f), 78f);
            CreateHeavyTruck(fleetParent.transform, "Vehicle_HeavyTruck_HighwayJackknife", new Vector3(75f, 0.1f, 8f), -35f);
        }

        // ==========================================
        // 1. POLICE CAR
        // ==========================================
        public static GameObject CreatePoliceCar(Transform parent, string name, Vector3 pos, float yRot, bool flashingSiren = false, bool openDoor = false)
        {
            GameObject car = new GameObject(name);
            car.transform.SetParent(parent);
            car.transform.position = pos;
            car.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material whiteMat = SceneBuilder.GetOrCreateMaterial("Mat_Police_White", new Color(0.92f, 0.94f, 0.96f));
            Material blackMat = SceneBuilder.GetOrCreateMaterial("Mat_Police_Black", new Color(0.12f, 0.13f, 0.15f));
            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Glass", new Color(0.15f, 0.20f, 0.28f, 0.75f), isTransparent: true);
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            // Chassis
            CreateBox(car.transform, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(2.0f, 0.65f, 4.5f), whiteMat);
            // Black front hood and trunk
            CreateBox(car.transform, "Hood_Black", new Vector3(0f, 0.75f, 1.35f), new Vector3(1.85f, 0.25f, 1.4f), blackMat);
            CreateBox(car.transform, "Trunk_Black", new Vector3(0f, 0.75f, -1.45f), new Vector3(1.85f, 0.25f, 1.1f), blackMat);
            // Black side door panels
            CreateBox(car.transform, "Door_Panel_L", new Vector3(-1.02f, 0.60f, 0.1f), new Vector3(0.06f, 0.55f, 1.8f), blackMat);
            CreateBox(car.transform, "Door_Panel_R", new Vector3(1.02f, 0.60f, 0.1f), new Vector3(0.06f, 0.55f, 1.8f), blackMat);

            // Cabin & Glass
            CreateBox(car.transform, "Cabin", new Vector3(0f, 1.15f, -0.15f), new Vector3(1.75f, 0.55f, 2.0f), whiteMat);
            CreateBox(car.transform, "Windshield", new Vector3(0f, 1.15f, 0.82f), new Vector3(1.65f, 0.50f, 0.1f), glassMat, Quaternion.Euler(32f, 0f, 0f));
            CreateBox(car.transform, "RearWindow", new Vector3(0f, 1.15f, -1.10f), new Vector3(1.65f, 0.50f, 0.1f), glassMat, Quaternion.Euler(-32f, 0f, 0f));

            // Push Bar
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.20f, 0.22f, 0.25f));
            CreateBox(car.transform, "PushBar", new Vector3(0f, 0.55f, 2.35f), new Vector3(1.7f, 0.55f, 0.18f), metalMat);

            // Emergency Light Bar
            Material redLightMat = SceneBuilder.GetOrCreateMaterial("Mat_Siren_Red", new Color(1.0f, 0.12f, 0.12f));
            Material blueLightMat = SceneBuilder.GetOrCreateMaterial("Mat_Siren_Blue", new Color(0.12f, 0.35f, 1.0f));

            CreateBox(car.transform, "Siren_Red", new Vector3(-0.45f, 1.52f, -0.15f), new Vector3(0.55f, 0.14f, 0.32f), redLightMat);
            CreateBox(car.transform, "Siren_Blue", new Vector3(0.45f, 1.52f, -0.15f), new Vector3(0.55f, 0.14f, 0.32f), blueLightMat);

            if (flashingSiren)
            {
                car.AddComponent<ProjectSurvival.Environment.EmergencyLightPulsar>();
            }

            // Open door variant
            if (openDoor)
            {
                CreateBox(car.transform, "Ajar_Door", new Vector3(-1.45f, 0.85f, 0.3f), new Vector3(0.08f, 0.85f, 1.0f), whiteMat, Quaternion.Euler(0f, -45f, 0f));
            }

            // Wheels
            AddWheels(car.transform, tireMat, 1.05f, 1.4f);
            AttachLootPoint(car, "Polis Aracı", ProjectSurvival.Survival.VehicleType.Police, new Vector3(0f, 0.8f, -1.2f), new Vector3(2.4f, 2f, 2.6f));
            return car;
        }

        // ==========================================
        // 2. YELLOW TAXI
        // ==========================================
        public static GameObject CreateTaxi(Transform parent, string name, Vector3 pos, float yRot)
        {
            GameObject taxi = new GameObject(name);
            taxi.transform.SetParent(parent);
            taxi.transform.position = pos;
            taxi.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material yellowMat = SceneBuilder.GetOrCreateMaterial("Mat_Taxi_Yellow", new Color(0.96f, 0.76f, 0.08f)); // Taxi #F5C214
            Material checkMat = SceneBuilder.GetOrCreateMaterial("Mat_Checkered_Black", new Color(0.12f, 0.12f, 0.12f));
            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Glass", new Color(0.15f, 0.20f, 0.28f, 0.75f), isTransparent: true);
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            CreateBox(taxi.transform, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(1.95f, 0.65f, 4.4f), yellowMat);
            CreateBox(taxi.transform, "Cabin", new Vector3(0f, 1.15f, -0.15f), new Vector3(1.70f, 0.55f, 2.0f), yellowMat);
            CreateBox(taxi.transform, "Windshield", new Vector3(0f, 1.15f, 0.82f), new Vector3(1.60f, 0.50f, 0.1f), glassMat, Quaternion.Euler(30f, 0f, 0f));
            CreateBox(taxi.transform, "RearWindow", new Vector3(0f, 1.15f, -1.10f), new Vector3(1.60f, 0.50f, 0.1f), glassMat, Quaternion.Euler(-30f, 0f, 0f));

            // Checkered side decal strips
            CreateBox(taxi.transform, "Checker_L", new Vector3(-0.99f, 0.72f, 0f), new Vector3(0.04f, 0.12f, 3.4f), checkMat);
            CreateBox(taxi.transform, "Checker_R", new Vector3(0.99f, 0.72f, 0f), new Vector3(0.04f, 0.12f, 3.4f), checkMat);

            // Taxi Roof Sign
            Material signLitMat = SceneBuilder.GetOrCreateMaterial("Mat_Taxi_SignLit", new Color(1.0f, 0.95f, 0.55f));
            CreateBox(taxi.transform, "Taxi_RoofSign", new Vector3(0f, 1.52f, -0.10f), new Vector3(0.65f, 0.16f, 0.28f), signLitMat);

            AddWheels(taxi.transform, tireMat, 1.0f, 1.35f);
            AttachLootPoint(taxi, "Taksi", ProjectSurvival.Survival.VehicleType.Taxi, new Vector3(0f, 0.8f, -1.2f), new Vector3(2.4f, 2f, 2.6f));
            return taxi;
        }

        // ==========================================
        // 3. AMBULANCE
        // ==========================================
        public static GameObject CreateAmbulance(Transform parent, string name, Vector3 pos, float yRot, bool rearDoorsOpen = false)
        {
            GameObject amb = new GameObject(name);
            amb.transform.SetParent(parent);
            amb.transform.position = pos;
            amb.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material whiteMat = SceneBuilder.GetOrCreateMaterial("Mat_Ambulance_White", new Color(0.95f, 0.95f, 0.96f));
            Material redCrossMat = SceneBuilder.GetOrCreateMaterial("Mat_Medical_Red", new Color(0.85f, 0.12f, 0.12f));
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            // Cab
            CreateBox(amb.transform, "Cab", new Vector3(0f, 0.95f, 1.4f), new Vector3(2.2f, 1.4f, 1.8f), whiteMat);
            // Box Body
            CreateBox(amb.transform, "Medical_Box", new Vector3(0f, 1.35f, -0.55f), new Vector3(2.35f, 1.85f, 3.4f), whiteMat);

            // Red Cross on Left & Right sides
            // Left Cross
            CreateBox(amb.transform, "Cross_L_V", new Vector3(-1.19f, 1.45f, -0.55f), new Vector3(0.04f, 0.85f, 0.28f), redCrossMat);
            CreateBox(amb.transform, "Cross_L_H", new Vector3(-1.19f, 1.45f, -0.55f), new Vector3(0.04f, 0.28f, 0.85f), redCrossMat);
            // Right Cross
            CreateBox(amb.transform, "Cross_R_V", new Vector3(1.19f, 1.45f, -0.55f), new Vector3(0.04f, 0.85f, 0.28f), redCrossMat);
            CreateBox(amb.transform, "Cross_R_H", new Vector3(1.19f, 1.45f, -0.55f), new Vector3(0.04f, 0.28f, 0.85f), redCrossMat);

            // Roof Emergency Beacon
            CreateBox(amb.transform, "Ambulance_Beacon", new Vector3(0f, 2.35f, 1.2f), new Vector3(0.95f, 0.18f, 0.35f), redCrossMat);

            // Rear Doors
            if (rearDoorsOpen)
            {
                CreateBox(amb.transform, "Door_Rear_L", new Vector3(-1.35f, 1.25f, -2.45f), new Vector3(0.08f, 1.6f, 0.85f), whiteMat, Quaternion.Euler(0f, -65f, 0f));
                CreateBox(amb.transform, "Door_Rear_R", new Vector3(1.35f, 1.25f, -2.45f), new Vector3(0.08f, 1.6f, 0.85f), whiteMat, Quaternion.Euler(0f, 65f, 0f));
            }
            else
            {
                CreateBox(amb.transform, "Door_Rear", new Vector3(0f, 1.25f, -2.26f), new Vector3(1.85f, 1.6f, 0.08f), whiteMat);
            }

            AddWheels(amb.transform, tireMat, 1.15f, 1.6f);
            AttachLootPoint(amb, "Ambulans", ProjectSurvival.Survival.VehicleType.Ambulance, new Vector3(0f, 1.2f, -1.8f), new Vector3(2.6f, 2.4f, 2.8f));
            return amb;
        }

        // ==========================================
        // 4. FAMILY SEDAN
        // ==========================================
        public static GameObject CreateSedan(Transform parent, string name, Vector3 pos, float yRot, Material bodyMat, bool doorOpen = false, bool hoodOpen = false)
        {
            GameObject sedan = new GameObject(name);
            sedan.transform.SetParent(parent);
            sedan.transform.position = pos;
            sedan.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Glass", new Color(0.15f, 0.20f, 0.28f, 0.75f), isTransparent: true);
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            CreateBox(sedan.transform, "Chassis", new Vector3(0f, 0.55f, 0f), new Vector3(1.90f, 0.65f, 4.3f), bodyMat);
            CreateBox(sedan.transform, "Cabin", new Vector3(0f, 1.15f, -0.15f), new Vector3(1.68f, 0.55f, 2.1f), bodyMat);
            CreateBox(sedan.transform, "Windshield", new Vector3(0f, 1.15f, 0.85f), new Vector3(1.58f, 0.48f, 0.08f), glassMat, Quaternion.Euler(32f, 0f, 0f));
            CreateBox(sedan.transform, "RearGlass", new Vector3(0f, 1.15f, -1.15f), new Vector3(1.58f, 0.48f, 0.08f), glassMat, Quaternion.Euler(-32f, 0f, 0f));

            if (doorOpen)
            {
                CreateBox(sedan.transform, "AjarDoor", new Vector3(-1.35f, 0.82f, 0.2f), new Vector3(0.08f, 0.80f, 0.95f), bodyMat, Quaternion.Euler(0f, -50f, 0f));
            }

            if (hoodOpen)
            {
                CreateBox(sedan.transform, "PoppedHood", new Vector3(0f, 1.15f, 1.45f), new Vector3(1.75f, 0.08f, 1.25f), bodyMat, Quaternion.Euler(-25f, 0f, 0f));
            }

            AddWheels(sedan.transform, tireMat, 0.98f, 1.35f);
            AttachLootPoint(sedan, "Sivil Otomobil", ProjectSurvival.Survival.VehicleType.Civilian, new Vector3(0f, 0.8f, -1.2f), new Vector3(2.4f, 2f, 2.6f));
            return sedan;
        }

        // ==========================================
        // 5. SUV / OFF-ROADER
        // ==========================================
        public static GameObject CreateSUV(Transform parent, string name, Vector3 pos, float yRot, Material bodyMat, bool overturned = false)
        {
            GameObject suv = new GameObject(name);
            suv.transform.SetParent(parent);
            suv.transform.position = pos;
            suv.transform.rotation = overturned ? Quaternion.Euler(0f, yRot, 90f) : Quaternion.Euler(0f, yRot, 0f);

            Material glassMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Glass", new Color(0.15f, 0.20f, 0.28f, 0.75f), isTransparent: true);
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.20f, 0.22f, 0.25f));
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            // Elevated body
            CreateBox(suv.transform, "Chassis", new Vector3(0f, 0.75f, 0f), new Vector3(2.15f, 0.85f, 4.6f), bodyMat);
            CreateBox(suv.transform, "Cabin", new Vector3(0f, 1.45f, -0.35f), new Vector3(1.95f, 0.75f, 2.6f), bodyMat);
            CreateBox(suv.transform, "Windshield", new Vector3(0f, 1.45f, 0.95f), new Vector3(1.85f, 0.65f, 0.08f), glassMat, Quaternion.Euler(28f, 0f, 0f));

            // Bullbar
            CreateBox(suv.transform, "Bullbar", new Vector3(0f, 0.65f, 2.45f), new Vector3(1.85f, 0.65f, 0.20f), metalMat);

            // Roof Luggage Rack with Spare Tire
            CreateBox(suv.transform, "RoofRack", new Vector3(0f, 1.88f, -0.35f), new Vector3(1.65f, 0.12f, 2.2f), metalMat);
            GameObject spareTire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spareTire.name = "SpareTire";
            spareTire.transform.SetParent(suv.transform);
            spareTire.transform.localPosition = new Vector3(0f, 2.05f, -0.35f);
            spareTire.transform.localScale = new Vector3(0.75f, 0.22f, 0.75f);
            Object.DestroyImmediate(spareTire.GetComponent<Collider>());
            spareTire.GetComponent<Renderer>().sharedMaterial = tireMat;

            AddWheels(suv.transform, tireMat, 1.15f, 1.55f, wheelScale: 0.88f, yOffset: 0.45f);
            AttachLootPoint(suv, "Arazi Aracı (SUV)", ProjectSurvival.Survival.VehicleType.Civilian, new Vector3(0f, 1.0f, -1.4f), new Vector3(2.6f, 2.2f, 2.8f));
            return suv;
        }

        // ==========================================
        // 6. CARGO VAN
        // ==========================================
        public static GameObject CreateCargoVan(Transform parent, string name, Vector3 pos, float yRot)
        {
            GameObject van = new GameObject(name);
            van.transform.SetParent(parent);
            van.transform.position = pos;
            van.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material vanMat = SceneBuilder.GetOrCreateMaterial("Mat_Van_White", new Color(0.85f, 0.86f, 0.88f));
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            // Driver Cab
            CreateBox(van.transform, "Cab", new Vector3(0f, 0.95f, 1.45f), new Vector3(2.15f, 1.25f, 1.6f), vanMat);
            // Cargo Box
            CreateBox(van.transform, "CargoArea", new Vector3(0f, 1.30f, -0.55f), new Vector3(2.25f, 1.80f, 3.2f), vanMat);

            AddWheels(van.transform, tireMat, 1.12f, 1.5f);
            AttachLootPoint(van, "Kargo Minibüsü", ProjectSurvival.Survival.VehicleType.Van, new Vector3(0f, 1.2f, -1.8f), new Vector3(2.6f, 2.5f, 3.2f));
            return van;
        }

        // ==========================================
        // 7. HEAVY TRUCK & TRAILER (TACTICAL BARRICADE)
        // ==========================================
        public static GameObject CreateHeavyTruck(Transform parent, string name, Vector3 pos, float yRot)
        {
            GameObject truck = new GameObject(name);
            truck.transform.SetParent(parent);
            truck.transform.position = pos;
            truck.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            Material cabMat = SceneBuilder.GetOrCreateMaterial("Mat_Truck_Red", new Color(0.75f, 0.18f, 0.15f));
            Material trailerMat = SceneBuilder.GetOrCreateMaterial("Mat_Trailer_Corrugated", new Color(0.45f, 0.48f, 0.52f));
            Material metalMat = SceneBuilder.GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.20f, 0.22f, 0.25f));
            Material tireMat = SceneBuilder.GetOrCreateMaterial("Mat_Vehicle_Tire", new Color(0.12f, 0.12f, 0.12f));

            // Massive Cab
            CreateBox(truck.transform, "Cab", new Vector3(0f, 1.75f, 4.2f), new Vector3(2.55f, 2.4f, 2.6f), cabMat);
            // Heavy Chrome/Metal Grille
            CreateBox(truck.transform, "Grille", new Vector3(0f, 1.25f, 5.55f), new Vector3(2.1f, 1.4f, 0.25f), metalMat);

            // Exhaust Stacks
            GameObject stackL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stackL.name = "Exhaust_L";
            stackL.transform.SetParent(truck.transform);
            stackL.transform.localPosition = new Vector3(-1.35f, 2.5f, 3.1f);
            stackL.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
            Object.DestroyImmediate(stackL.GetComponent<Collider>());
            stackL.GetComponent<Renderer>().sharedMaterial = metalMat;

            GameObject stackR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stackR.name = "Exhaust_R";
            stackR.transform.SetParent(truck.transform);
            stackR.transform.localPosition = new Vector3(1.35f, 2.5f, 3.1f);
            stackR.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
            Object.DestroyImmediate(stackR.GetComponent<Collider>());
            stackR.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Long Shipping Trailer (Jackknifed Barricade)
            CreateBox(truck.transform, "Trailer", new Vector3(0.2f, 1.95f, -1.8f), new Vector3(2.65f, 2.7f, 8.5f), trailerMat);

            // Cab Wheels
            AddWheels(truck.transform, tireMat, 1.30f, 4.2f, wheelScale: 0.95f, yOffset: 0.55f);
            // Trailer Dual Wheels
            AddWheels(truck.transform, tireMat, 1.30f, -4.2f, wheelScale: 0.95f, yOffset: 0.55f);
            AddWheels(truck.transform, tireMat, 1.30f, -5.3f, wheelScale: 0.95f, yOffset: 0.55f);

            return truck;
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private static void CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Quaternion? localRot = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.localPosition = localPos;
            box.transform.localRotation = localRot.HasValue ? localRot.Value : Quaternion.identity;
            box.transform.localScale = localScale;

            if (mat != null)
            {
                Renderer r = box.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.NavigationStatic);
        }

        private static void AddWheels(Transform parent, Material tireMat, float xOffset, float zOffset, float wheelScale = 0.72f, float yOffset = 0.35f)
        {
            Vector3[] wheelPos = new Vector3[]
            {
                new Vector3(-xOffset, yOffset, zOffset),
                new Vector3(xOffset, yOffset, zOffset),
                new Vector3(-xOffset, yOffset, -zOffset),
                new Vector3(xOffset, yOffset, -zOffset)
            };

            for (int i = 0; i < wheelPos.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i + 1}";
                wheel.transform.SetParent(parent);
                wheel.transform.localPosition = wheelPos[i];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(wheelScale, 0.16f, wheelScale);

                if (tireMat != null)
                {
                    Renderer r = wheel.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = tireMat;
                }

                GameObjectUtility.SetStaticEditorFlags(wheel, StaticEditorFlags.NavigationStatic);
            }
        }

        private static void AttachLootPoint(GameObject vehicle, string displayName, ProjectSurvival.Survival.VehicleType type, Vector3 triggerCenter, Vector3 triggerSize)
        {
            BoxCollider triggerCol = vehicle.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = triggerCenter;
            triggerCol.size = triggerSize;

            ProjectSurvival.Survival.VehicleLootPoint loot = vehicle.AddComponent<ProjectSurvival.Survival.VehicleLootPoint>();
            loot.Configure(displayName, type, 2.5f, 14f);
        }
    }
}
