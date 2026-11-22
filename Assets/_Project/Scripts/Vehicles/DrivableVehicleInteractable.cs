using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Phase 1: Contextual Multi-Verb Interaction Handler for Restorable Hero Vehicles.
    /// Provides 4 tactile verbs:
    /// 1. "drive": ARACI SÜR (Enter driver seat & switch to driving HUD/Chase camera)
    /// 2. "repair": TAMİR ET (%40 Motor Onarımı, 2 Hurda Metal tüketir)
    /// 3. "refuel": YAKIT DOLDUR (%45 Yakıt İkmali, 1 Alkol / Yakıt Bidonu tüketir)
    /// 4. "trunk": ARAÇ BAGAJI (8 ila 16 slotluk taşınabilir mobil depo)
    /// </summary>
    public class DrivableVehicleInteractable : WorldInteractableObject
    {
        [Header("Vehicle Controller Reference")]
        [SerializeField] private DrivableVehicleController vehicleController;

        public override string ObjectSubtitle
        {
            get
            {
                if (vehicleController == null) return "ONARILABİLİR ARAÇ";
                int hp = Mathf.RoundToInt(vehicleController.EngineHP);
                int fl = Mathf.RoundToInt(vehicleController.Fuel);
                return $"MOTOR: %{hp} • YAKIT: %{fl}";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (vehicleController == null)
            {
                vehicleController = GetComponent<DrivableVehicleController>() ?? GetComponentInParent<DrivableVehicleController>();
            }

            if (vehicleController != null)
            {
                objectTitle = "SÜRÜLEBİLİR ARAÇ";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (vehicleController == null || vehicleController.IsPlayerDriving)
            {
                return verbs;
            }

            // Verb 1: ARACI SÜR (Bin)
            verbs.Add(new InteractionVerb("drive", "ARACI SÜR (Bin)", 0.4f, null, null, 12f, true));

            // Verb 2: TAMİR ET (Consumes 2 Scrap Metal)
            bool needsRepair = vehicleController.EngineHP < vehicleController.MaxEngineHP;
            bool hasScrap = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_scrap", 2);
            string repairLabel = needsRepair
                ? (hasScrap ? "TAMİR ET (+%40 MOTOR • 2 HURDA)" : "TAMİR ET (2 HURDA METAL GEREKLİ)")
                : "MOTOR TAM KONDİSYONDA";
            verbs.Add(new InteractionVerb("repair", repairLabel, 2.5f, "item_scrap", null, 14f, needsRepair && hasScrap));

            // Verb 3: YAKIT DOLDUR (Consumes 1 Fuel Can or Alcohol)
            bool needsFuel = vehicleController.Fuel < vehicleController.MaxFuel;
            bool hasFuelCan = InventorySystem.Instance != null &&
                (InventorySystem.Instance.HasItem("item_fuel_can", 1) || InventorySystem.Instance.HasItem("item_fuel", 1) || InventorySystem.Instance.HasItem("item_alcohol", 1));
            string refuelLabel = needsFuel
                ? (hasFuelCan ? "YAKIT DOLDUR (+%35 YAKIT • 1 BİDON)" : "YAKIT DOLDUR (YAKIT BİDONU/ALKOL GEREKLİ)")
                : "DEPO TAM DOLU";
            verbs.Add(new InteractionVerb("refuel", refuelLabel, 2.0f, null, null, 10f, needsFuel && hasFuelCan));

            // Verb 4: ARAÇ BAGAJI (Mobile Trunk Stash)
            verbs.Add(new InteractionVerb("trunk", "ARAÇ BAGAJI (MOBİL DEPO)", 0.6f, null, null, 10f, true));

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (vehicleController == null) return;

            switch (verbId)
            {
                case "drive":
                    ExecuteDrive(interactor);
                    break;
                case "repair":
                    ExecuteRepair();
                    break;
                case "refuel":
                    ExecuteRefuel();
                    break;
                case "trunk":
                    ExecuteOpenTrunk();
                    break;
            }
        }

        private void ExecuteDrive(GameObject interactor)
        {
            if (vehicleController.EngineHP <= 15f)
            {
                GameEvents.TriggerObjectiveToastRequested("MOTOR HASARLI! SÜRMEK İÇİN EN AZ %16 MOTOR GEREKLİ", 3.0f);
                ProceduralAudioManager.Instance?.PlayButtonClick();
                return;
            }

            if (vehicleController.Fuel <= 0f)
            {
                GameEvents.TriggerObjectiveToastRequested("YAKIT TÜKENDİ! SÜRMEK İÇİN YAKIT DOLDURUN", 3.0f);
                ProceduralAudioManager.Instance?.PlayButtonClick();
                return;
            }

            vehicleController.EnterDriverSeat(interactor);
        }

        private void ExecuteRepair()
        {
            if (InventorySystem.Instance == null) return;

            if (InventorySystem.Instance.RemoveItem("item_scrap", 2))
            {
                vehicleController.RepairEngine(40f);
                GameEvents.TriggerCameraShakeRequested(0.18f, 0.15f);
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("YETERSİZ MALZEME! 2 ADET HURDA METAL GEREKLİ", 2.5f);
            }
        }

        private void ExecuteRefuel()
        {
            if (InventorySystem.Instance == null) return;

            bool consumedFuelCan = false;
            bool consumedAlcohol = false;

            if (InventorySystem.Instance.HasItem("item_fuel_can", 1))
            {
                consumedFuelCan = InventorySystem.Instance.RemoveItem("item_fuel_can", 1);
            }
            else if (InventorySystem.Instance.HasItem("item_fuel", 1))
            {
                consumedFuelCan = InventorySystem.Instance.RemoveItem("item_fuel", 1);
            }
            else if (InventorySystem.Instance.HasItem("item_alcohol", 1))
            {
                consumedAlcohol = InventorySystem.Instance.RemoveItem("item_alcohol", 1);
            }

            if (consumedFuelCan)
            {
                vehicleController.Refuel(35f);
                InventorySystem.Instance.AddItemById("item_empty_jerrycan", 1);
                ProceduralAudioManager.Instance?.PlayRefuelLiquid();
                MobileHapticManager.Instance?.TriggerMedium();
                GameEvents.TriggerObjectiveToastRequested("YAKIT DOLDURULDU: +%35 | Boş Bidon Geri Alındı", 2.5f);
            }
            else if (consumedAlcohol)
            {
                vehicleController.Refuel(25f);
                ProceduralAudioManager.Instance?.PlayRefuelLiquid();
                MobileHapticManager.Instance?.TriggerMedium();
                GameEvents.TriggerObjectiveToastRequested("ALKOLLE YAKIT DOLDURULDU: +%25", 2.5f);
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("YAKIT BİDONU VEYA ALKOL BULUNAMADI!", 2.5f);
            }
        }

        private void ExecuteOpenTrunk()
        {
            VehicleTrunkUI.Instance?.OpenTrunk(vehicleController);
        }

        public void Configure(DrivableVehicleController controller)
        {
            vehicleController = controller;
            if (controller != null)
            {
                objectTitle = controller.VehicleDisplayName;
            }
        }
    }
}
