using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;
using ProjectSurvival.Vehicles;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive roadside Gas Station Fuel Dispenser Landmark.
    /// Provides dual fueling modes:
    /// 1. Direct Vehicle Hose Refueling (ARACA YAKIT DOLDUR) within 4.8m radius.
    /// 2. Jerrycan Filling (BİDONA YAKIT DOLDUR) converting item_empty_jerrycan into item_fuel_can.
    /// Features 3D positional audio (spatialBlend = 1.0, 1.5m-22m logarithmic), NavMeshObstacle carving,
    /// and shadows-none indicator lights for mobile 60 FPS compliance.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GasStationPumpInteractable : WorldInteractableObject
    {
        [Header("Gas Station Configuration")]
        [SerializeField] private string stationId = "station_south_boulevard";
        [SerializeField] private string stationName = "Güney Bulvarı Benzin İstasyonu";
        [SerializeField] private float remainingFuelLiters = 65f;
        [SerializeField] private float maxReserveLiters = 100f;
        [SerializeField] private float vehicleDetectionRadius = 4.8f;

        [Header("Audio & Acoustics")]
        [SerializeField] private AudioSource pumpAudioSource;
        [SerializeField] private Light pumpIndicatorLight;

        private AudioClip cachedPumpMotorClip;
        private AudioClip cachedNozzleClickClip;
        private NavMeshObstacle navObstacle;

        public string StationId => stationId;
        public string StationName => stationName;
        public float RemainingFuelLiters => remainingFuelLiters;
        public float MaxReserveLiters => maxReserveLiters;

        public override string ObjectSubtitle
        {
            get
            {
                if (remainingFuelLiters <= 0.05f) return "İstasyon Deposu Boş (0L)";
                return $"Yakıt Rezervi: {Mathf.RoundToInt(remainingFuelLiters)}L / {Mathf.RoundToInt(maxReserveLiters)}L";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            objectTitle = stationName;
            objectSubtitle = $"Yakıt Rezervi: {Mathf.RoundToInt(remainingFuelLiters)}L";

            // Configure 3D Positional AudioSource
            if (pumpAudioSource == null) pumpAudioSource = GetComponent<AudioSource>();
            if (pumpAudioSource == null) pumpAudioSource = gameObject.AddComponent<AudioSource>();

            pumpAudioSource.spatialBlend = 1.0f; // Pure 3D positional
            pumpAudioSource.minDistance = 1.5f;
            pumpAudioSource.maxDistance = 22.0f;
            pumpAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            pumpAudioSource.playOnAwake = false;

            // Ensure Light has Shadows: None for mobile 60 FPS limit
            if (pumpIndicatorLight != null)
            {
                pumpIndicatorLight.shadows = LightShadows.None;
            }

            // Ensure NavMeshObstacle with carving
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.carveOnlyStationary = false;

            UpdateIndicatorVisuals();
        }

        private void Start()
        {
            CacheAudioClips();
            UpdateIndicatorVisuals();
        }

        private void CacheAudioClips()
        {
            if (ProceduralAudioManager.Instance != null)
            {
                cachedPumpMotorClip = ProceduralAudioManager.Instance.GeneratePumpMotorClip();
                cachedNozzleClickClip = ProceduralAudioManager.Instance.GenerateNozzleClickClip();
            }
        }

        public void SetRemainingFuelLiters(float liters)
        {
            remainingFuelLiters = Mathf.Clamp(liters, 0f, maxReserveLiters);
            UpdateIndicatorVisuals();
        }

        public void ConfigureStation(string id, string displayName, float initialLiters, float maxLiters)
        {
            stationId = id;
            stationName = displayName;
            objectTitle = displayName;
            remainingFuelLiters = initialLiters;
            maxReserveLiters = maxLiters;
            SetPersistentId(id);
            UpdateIndicatorVisuals();
        }

        private void UpdateIndicatorVisuals()
        {
            if (pumpIndicatorLight != null)
            {
                pumpIndicatorLight.shadows = LightShadows.None;
                if (remainingFuelLiters > 0.05f)
                {
                    pumpIndicatorLight.color = new Color(0.2f, 0.95f, 0.35f); // Green active
                    pumpIndicatorLight.intensity = 1.8f;
                }
                else
                {
                    pumpIndicatorLight.color = new Color(0.95f, 0.25f, 0.2f); // Red empty
                    pumpIndicatorLight.intensity = 0.8f;
                }
            }
        }

        public DrivableVehicleController FindNearbyVehicle(float radius)
        {
            Vector3 pos = transform.position;
            float closestDistSqr = radius * radius;
            DrivableVehicleController closest = null;

            var vehicles = FindObjectsByType<DrivableVehicleController>(FindObjectsSortMode.None);
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i] != null)
                {
                    float dSqr = (vehicles[i].transform.position - pos).sqrMagnitude;
                    if (dSqr <= closestDistSqr)
                    {
                        closestDistSqr = dSqr;
                        closest = vehicles[i];
                    }
                }
            }

            return closest;
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            DrivableVehicleController nearbyVehicle = FindNearbyVehicle(vehicleDetectionRadius);

            // Mode 1: Direct Vehicle Hose Refueling (ARACA YAKIT DOLDUR)
            if (nearbyVehicle != null)
            {
                bool needsFuel = nearbyVehicle.Fuel < nearbyVehicle.MaxFuel * 0.98f;
                bool hasStationFuel = remainingFuelLiters > 0.05f;

                string vehicleLabel;
                bool available;

                if (!hasStationFuel)
                {
                    vehicleLabel = "ARACA YAKIT DOLDUR (İSTASYON BOŞ • 0L)";
                    available = false;
                }
                else if (!needsFuel)
                {
                    vehicleLabel = "ARACA YAKIT DOLDUR (ARAÇ DEPOSU TAM DOLU)";
                    available = false;
                }
                else
                {
                    vehicleLabel = $"ARACA YAKIT DOLDUR (+%40 • İstasyon: {Mathf.RoundToInt(remainingFuelLiters)}L)";
                    available = true;
                }

                verbs.Add(new InteractionVerb(
                    "refuel_vehicle",
                    vehicleLabel,
                    1.8f, // 1.8s timed action
                    null,
                    null,
                    15f,
                    available
                ));
            }

            // Mode 2: Jerrycan Filling (BİDONA YAKIT DOLDUR)
            bool hasEmptyJerrycan = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_empty_jerrycan", 1);
            bool hasFuelForCan = remainingFuelLiters > 0.05f;

            string canLabel;
            bool canAvailable;

            if (!hasFuelForCan)
            {
                canLabel = "BİDONA YAKIT DOLDUR (İSTASYON BOŞ • 0L)";
                canAvailable = false;
            }
            else if (!hasEmptyJerrycan)
            {
                canLabel = "BİDONA YAKIT DOLDUR (BOŞ BİDON GEREKLİ)";
                canAvailable = false;
            }
            else
            {
                canLabel = $"BİDONA YAKIT DOLDUR (15L • İstasyon: {Mathf.RoundToInt(remainingFuelLiters)}L)";
                canAvailable = true;
            }

            verbs.Add(new InteractionVerb(
                "fill_jerrycan",
                canLabel,
                1.5f, // 1.5s timed action
                "item_empty_jerrycan",
                "Boş Yakıt Bidonu",
                10f,
                canAvailable
            ));

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (verbId == "refuel_vehicle")
            {
                DrivableVehicleController vehicle = FindNearbyVehicle(vehicleDetectionRadius);
                return vehicle != null && vehicle.Fuel < vehicle.MaxFuel * 0.98f && remainingFuelLiters > 0.05f;
            }
            if (verbId == "fill_jerrycan")
            {
                return InventorySystem.Instance != null &&
                       InventorySystem.Instance.HasItem("item_empty_jerrycan", 1) &&
                       remainingFuelLiters > 0.05f;
            }
            return false;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "refuel_vehicle")
            {
                DrivableVehicleController vehicle = FindNearbyVehicle(vehicleDetectionRadius);
                if (vehicle != null && remainingFuelLiters > 0.05f)
                {
                    float pumpLiters = Mathf.Min(25f, remainingFuelLiters);
                    remainingFuelLiters = Mathf.Max(0f, remainingFuelLiters - pumpLiters);

                    vehicle.Refuel(40f); // Up to +40% tank
                    PlayPumpAudio();
                    MobileHapticManager.Instance?.TriggerLight();
                    UpdateIndicatorVisuals();

                    GameEvents.TriggerObjectiveToastRequested(
                        $"ARACA YAKIT DOLDURULDU (+%40) | İstasyon Deposu: {Mathf.RoundToInt(remainingFuelLiters)}L Kaldı", 2.5f);
                    SurvivorProgressionManager.Instance?.AddXP(15);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("YAKIT DOLDURULAMADI! İstasyon boş veya araç uzakta.", 2.5f);
                }
            }
            else if (verbId == "fill_jerrycan")
            {
                if (InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_empty_jerrycan", 1) && remainingFuelLiters > 0.05f)
                {
                    float transferLiters = Mathf.Min(15f, remainingFuelLiters);
                    remainingFuelLiters = Mathf.Max(0f, remainingFuelLiters - transferLiters);

                    InventorySystem.Instance.RemoveItem("item_empty_jerrycan", 1);
                    InventorySystem.Instance.AddItemById("item_fuel_can", 1);

                    PlayPumpAudio();
                    MobileHapticManager.Instance?.TriggerLight();
                    UpdateIndicatorVisuals();

                    GameEvents.TriggerObjectiveToastRequested(
                        $"BİDON DOLDURULDU (+15L) | İstasyon Deposu: {Mathf.RoundToInt(remainingFuelLiters)}L Kaldı (+15 XP)", 2.5f);
                    SurvivorProgressionManager.Instance?.AddXP(15);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("BOŞ BİDON BULUNAMADI VEYA İSTASYON BOŞ!", 2.5f);
                }
            }
        }

        private void PlayPumpAudio()
        {
            if (pumpAudioSource != null)
            {
                if (cachedPumpMotorClip == null && ProceduralAudioManager.Instance != null)
                {
                    cachedPumpMotorClip = ProceduralAudioManager.Instance.GeneratePumpMotorClip();
                }
                if (cachedPumpMotorClip != null)
                {
                    pumpAudioSource.PlayOneShot(cachedPumpMotorClip, 0.85f);
                }

                if (cachedNozzleClickClip == null && ProceduralAudioManager.Instance != null)
                {
                    cachedNozzleClickClip = ProceduralAudioManager.Instance.GenerateNozzleClickClip();
                }
                if (cachedNozzleClickClip != null)
                {
                    pumpAudioSource.PlayOneShot(cachedNozzleClickClip, 0.90f);
                }
            }
            else if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlayPumpMotor(transform.position);
                ProceduralAudioManager.Instance.PlayNozzleClick(transform.position);
            }
        }

        public override string GetSerializedState()
        {
            return $"{remainingFuelLiters:F1}|{maxReserveLiters:F1}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            string[] parts = state.Split('|');
            if (parts.Length >= 1)
            {
                if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedFuel))
                {
                    remainingFuelLiters = parsedFuel;
                }
            }
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedMax))
                {
                    maxReserveLiters = parsedMax;
                }
            }
            UpdateIndicatorVisuals();
        }
    }
}
