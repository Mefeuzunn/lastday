using System;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Micro-Step 1B: Vehicle Fuel, Engine Durability & Maintenance Module.
    /// Tracks dynamic fuel consumption, limp mode (speed cap at 55% + hood smoke),
    /// stall mechanics, and executes scrap metal repair and fuel/alcohol refueling.
    /// </summary>
    [RequireComponent(typeof(DrivableVehicleController))]
    public class VehicleMaintenanceModule : MonoBehaviour
    {
        [Header("Fuel System")]
        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private float currentFuel = 45f; // Initialized to 45% for immediate driving & refueling incentive
        [SerializeField] private float throttleFuelBurnRate = 0.55f; // Units/sec under active throttle (~1% per 1.8s)
        [SerializeField] private float idleFuelBurnRate = 0.05f;     // Units/sec while engine running at idle

        [Header("Engine Durability")]
        [SerializeField] private float maxEngineHP = 100f;
        [SerializeField] private float currentEngineHP = 40f; // Initialized to 40% for immediate repair testing
        [SerializeField] private float limpModeThreshold = 30f;
        [SerializeField] private float stallThreshold = 15f;

        [Header("Visual & Smoke VFX")]
        [SerializeField] private ParticleSystem hoodSmokeEmitter;
        [SerializeField] private Transform weldSparkOrigin;

        private DrivableVehicleController vehicleController;
        private float stallWarningTimer = 0f;

        public float MaxFuel => maxFuel;
        public float CurrentFuel => currentFuel;
        public float FuelRatio => Mathf.Clamp01(currentFuel / maxFuel);

        public float MaxEngineHP => maxEngineHP;
        public float CurrentEngineHP => currentEngineHP;
        public float EngineHPRatio => Mathf.Clamp01(currentEngineHP / maxEngineHP);

        public bool IsLimpMode => currentEngineHP < limpModeThreshold && currentEngineHP > stallThreshold;
        public bool IsStalled => currentFuel <= 0f || currentEngineHP <= stallThreshold;
        public bool CanRepair => currentEngineHP < maxEngineHP;
        public bool CanRefuel => currentFuel < maxFuel;

        private void Awake()
        {
            vehicleController = GetComponent<DrivableVehicleController>();
            EnsureHoodSmokeEmitter();
        }

        private void Start()
        {
            UpdateHoodSmoke();
        }

        private void Update()
        {
            if (vehicleController == null || !vehicleController.IsPlayerDriving)
            {
                if (hoodSmokeEmitter != null && hoodSmokeEmitter.isPlaying && !IsLimpMode)
                {
                    hoodSmokeEmitter.Stop();
                }
                return;
            }

            float dt = Time.deltaTime;
            ConsumeFuel(dt);
            UpdateHoodSmoke();
            CheckStallAlerts(dt);
        }

        private void ConsumeFuel(float dt)
        {
            if (currentFuel <= 0f)
            {
                currentFuel = 0f;
                return;
            }

            float throttle = Mathf.Abs(vehicleController.ThrottleInput);
            float burnRate = (throttle > 0.05f) ? (throttleFuelBurnRate * throttle) : idleFuelBurnRate;

            currentFuel = Mathf.Max(0f, currentFuel - burnRate * dt);
        }

        private void CheckStallAlerts(float dt)
        {
            if (!IsStalled) return;

            stallWarningTimer += dt;
            if (stallWarningTimer >= 4.0f)
            {
                stallWarningTimer = 0f;
                if (currentFuel <= 0f)
                {
                    GameEvents.TriggerObjectiveToastRequested("UYARI: YAKIT BİTTİ — YAKIT DOLDUR!", 2.5f);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("UYARI: MOTOR HASARLI — HURDA İLE TAMİR ET!", 2.5f);
                }
            }
        }

        private void UpdateHoodSmoke()
        {
            if (hoodSmokeEmitter == null) return;

            if (IsLimpMode || currentEngineHP <= stallThreshold)
            {
                if (!hoodSmokeEmitter.isPlaying) hoodSmokeEmitter.Play();
            }
            else
            {
                if (hoodSmokeEmitter.isPlaying) hoodSmokeEmitter.Stop();
            }
        }

        // =========================================================================
        // REPAIR & REFUEL INTERACTIONS
        // =========================================================================

        /// <summary>
        /// Consumes 2 Scrap Metal from inventory, restores +40 HP, plays wrench/spark audio.
        /// </summary>
        public bool TryRepair()
        {
            if (!CanRepair)
            {
                GameEvents.TriggerObjectiveToastRequested("Motor tam kondisyonda.", 1.8f);
                return false;
            }

            if (!InventoryManager.HasItem("item_scrap", 2))
            {
                GameEvents.TriggerObjectiveToastRequested("Tamir için 2 Hurda Metal gerekli!", 2.2f);
                return false;
            }

            InventoryManager.RemoveItem("item_scrap", 2);
            currentEngineHP = Mathf.Clamp(currentEngineHP + 40f, 0f, maxEngineHP);

            // Audio & Haptic Feedback
            ProceduralAudioManager.Instance?.PlayWrenchRepair();
            MobileHapticManager.Instance?.TriggerMedium();

            UpdateHoodSmoke();

            GameEvents.TriggerObjectiveToastRequested($"MOTOR ONARILDI: %{Mathf.RoundToInt(currentEngineHP)}", 2.0f);
            return true;
        }

        public void ApplyImpactWear(float wearAmount)
        {
            if (wearAmount <= 0f) return;
            currentEngineHP = Mathf.Max(0f, currentEngineHP - wearAmount);
            UpdateHoodSmoke();

            if (currentEngineHP <= stallThreshold)
            {
                GameEvents.TriggerObjectiveToastRequested("UYARI: MOTOR HASAR ALDI — ONARIM GEREKLİ!", 2.5f);
            }
        }

        public void SetState(float fuelAmount, float hpAmount)
        {
            currentFuel = Mathf.Clamp(fuelAmount, 0f, maxFuel);
            currentEngineHP = Mathf.Clamp(hpAmount, 0f, maxEngineHP);
            UpdateHoodSmoke();
        }

        /// <summary>
        /// Consumes 1 Fuel Canister or Alcohol, restores +45% fuel, plays liquid refuel audio.
        /// </summary>
        public bool TryRefuel()
        {
            if (!CanRefuel)
            {
                GameEvents.TriggerObjectiveToastRequested("Depo tam dolu.", 1.8f);
                return false;
            }

            bool hasFuelCan = InventoryManager.HasItem("item_fuel_can", 1) || InventoryManager.HasItem("item_fuel", 1);
            bool hasAlcohol = InventoryManager.HasItem("item_alcohol", 1);

            if (!hasFuelCan && !hasAlcohol)
            {
                GameEvents.TriggerObjectiveToastRequested("Yakıt Bidonu veya Alkol gerekli!", 2.2f);
                return false;
            }

            if (hasFuelCan)
            {
                if (InventoryManager.HasItem("item_fuel_can", 1))
                    InventoryManager.RemoveItem("item_fuel_can", 1);
                else
                    InventoryManager.RemoveItem("item_fuel", 1);

                InventoryManager.AddItemById("item_empty_jerrycan", 1);
                currentFuel = Mathf.Clamp(currentFuel + 35f, 0f, maxFuel);
                GameEvents.TriggerObjectiveToastRequested($"YAKIT DOLDURULDU: %{Mathf.RoundToInt(currentFuel)} (Boş Bidon Geri Alındı)", 2.0f);
            }
            else
            {
                InventoryManager.RemoveItem("item_alcohol", 1);
                currentFuel = Mathf.Clamp(currentFuel + 25f, 0f, maxFuel);
                GameEvents.TriggerObjectiveToastRequested($"ALKOL DOLDURULDU: %{Mathf.RoundToInt(currentFuel)}", 2.0f);
            }

            // Audio & Haptic Feedback
            ProceduralAudioManager.Instance?.PlayRefuelLiquid();
            MobileHapticManager.Instance?.TriggerMedium();

            return true;
        }

        public void SetFuelAndEngine(float fuelAmount, float engineAmount)
        {
            currentFuel = Mathf.Clamp(fuelAmount, 0f, maxFuel);
            currentEngineHP = Mathf.Clamp(engineAmount, 0f, maxEngineHP);
            UpdateHoodSmoke();
        }

        private void EnsureHoodSmokeEmitter()
        {
            if (hoodSmokeEmitter != null) return;

            Transform existing = transform.Find("Lighting_FX/Hood_Smoke_Emitter") ?? transform.Find("Hood_Smoke_Emitter");
            if (existing != null)
            {
                hoodSmokeEmitter = existing.GetComponent<ParticleSystem>();
                return;
            }

            GameObject smokeObj = new GameObject("Hood_Smoke_Emitter");
            smokeObj.transform.SetParent(transform, false);
            smokeObj.transform.localPosition = new Vector3(0f, 1.15f, 1.45f); // Above front hood
            smokeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            hoodSmokeEmitter = smokeObj.AddComponent<ParticleSystem>();
            var main = hoodSmokeEmitter.main;
            main.startSize = 0.55f;
            main.startLifetime = 1.2f;
            main.startSpeed = 1.8f;
            main.startColor = new Color(0.25f, 0.25f, 0.28f, 0.65f); // Dark gray engine smoke
            main.maxParticles = 40;

            var em = hoodSmokeEmitter.emission;
            em.rateOverTime = 16f;

            var shape = hoodSmokeEmitter.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.15f;

            ParticleSystemRenderer rend = smokeObj.GetComponent<ParticleSystemRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            rend.sharedMaterial = new Material(unlit);

            hoodSmokeEmitter.Stop();
        }
    }
}
