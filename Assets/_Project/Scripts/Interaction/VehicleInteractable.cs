using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Interaction
{
    public enum VehicleArchetypeCategory
    {
        Civilian,
        Police,
        Ambulance,
        Taxi,
        Van,
        HeavyTruck
    }

    /// <summary>
    /// Interactive Vehicle Archetype.
    /// Provides 4 deep tactical verbs for all 25+ cars, police cruisers, ambulances, taxis, vans & trucks:
    /// 1. "search": Pop trunk with hinge Slerp, awards vehicle-specific survival loot (2.2s).
    /// 2. "salvage": Wrench off scrap metal & battery (2.8s, 1.4s with crowbar), 16m acoustic noise.
    /// 3. "decoy_horn": Blare 10s car horn / police siren with flashing lights, broadcasting a 42m acoustic zombie lure!
    /// 4. "hide": Low-profile backseat hiding spot, breaks zombie aggro and line-of-sight.
    /// </summary>
    public class VehicleInteractable : WorldInteractableObject
    {
        [Header("Vehicle Classification")]
        [SerializeField] private VehicleArchetypeCategory vehicleCategory = VehicleArchetypeCategory.Civilian;
        [SerializeField] private Transform trunkTransform;
        [SerializeField] private Transform doorTransform;
        [SerializeField] private Transform peekTransform;
        [SerializeField] private Transform exitTransform;

        [Header("Emergency Lights & Horn")]
        [SerializeField] private Light roofLightRed;
        [SerializeField] private Light roofLightBlue;
        [SerializeField] private bool hasBattery = true;
        [SerializeField] private bool isHornBlaring = false;

        private bool isPlayerHiddenInside = false;
        private Quaternion trunkClosedRot;
        private Quaternion trunkOpenRot;
        private Coroutine hornRoutine;
        private Coroutine trunkAnimRoutine;

        public override string ObjectSubtitle => hasBeenSearched ? "ARANDI" : (vehicleCategory == VehicleArchetypeCategory.Police ? "POLİS ARACI" : (vehicleCategory == VehicleArchetypeCategory.Ambulance ? "AMBULANS" : "TERK EDİLMİŞ ARAÇ"));

        protected override void Awake()
        {
            base.Awake();
            DetectVehicleCategory();
            SetupHingesAndPeeks();
        }

        private void DetectVehicleCategory()
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("police"))
            {
                vehicleCategory = VehicleArchetypeCategory.Police;
                objectTitle = "Terk Edilmiş Polis Aracı";
            }
            else if (n.Contains("ambulance"))
            {
                vehicleCategory = VehicleArchetypeCategory.Ambulance;
                objectTitle = "Saha Ambulansı";
            }
            else if (n.Contains("taxi"))
            {
                vehicleCategory = VehicleArchetypeCategory.Taxi;
                objectTitle = "Sarı Taksi";
            }
            else if (n.Contains("van"))
            {
                vehicleCategory = VehicleArchetypeCategory.Van;
                objectTitle = "Kargo Minibüsü";
            }
            else if (n.Contains("truck"))
            {
                vehicleCategory = VehicleArchetypeCategory.HeavyTruck;
                objectTitle = "Ağır Nakliye Kamyonu";
            }
            else
            {
                vehicleCategory = VehicleArchetypeCategory.Civilian;
                objectTitle = "Terk Edilmiş Sedan";
            }
        }

        private void SetupHingesAndPeeks()
        {
            if (trunkTransform == null)
            {
                trunkTransform = transform.Find("Trunk_Black") ?? transform.Find("Trunk") ?? transform.Find("RearDoors");
            }
            if (trunkTransform != null)
            {
                trunkClosedRot = trunkTransform.localRotation;
                trunkOpenRot = trunkClosedRot * Quaternion.Euler(-45f, 0f, 0f);
            }

            if (doorTransform == null)
            {
                doorTransform = transform.Find("Door_FL") ?? transform.Find("Door_Left") ?? transform.Find("Doors");
            }

            if (peekTransform == null)
            {
                GameObject peek = new GameObject("Vehicle_PeekPoint");
                peek.transform.SetParent(transform, false);
                peek.transform.localPosition = new Vector3(-0.35f, 1.1f, -0.2f);
                peekTransform = peek.transform;
            }

            if (exitTransform == null)
            {
                GameObject exit = new GameObject("Vehicle_ExitPoint");
                exit.transform.SetParent(transform, false);
                exit.transform.localPosition = new Vector3(-1.6f, 0f, -0.2f);
                exitTransform = exit.transform;
            }

            // Find emergency lights if police/ambulance
            if (roofLightRed == null)
            {
                Transform r = transform.Find("RoofLight_Red");
                if (r != null) roofLightRed = r.GetComponent<Light>();
            }
            if (roofLightBlue == null)
            {
                Transform b = transform.Find("RoofLight_Blue");
                if (b != null) roofLightBlue = b.GetComponent<Light>();
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isPlayerHiddenInside)
            {
                verbs.Add(new InteractionVerb("exit_vehicle", "ARAÇTAN DIŞARI ÇIK", 0f));
                return verbs;
            }

            // Verb 1: Search Trunk
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb("search", "BAGAJI ARA", 2.2f, null, null, 14f));
            }

            // Verb 2: Salvage Scrap & Battery
            if (!isDamaged)
            {
                bool hasCrowbar = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_crowbar");
                float salvageTime = hasCrowbar ? 1.4f : 2.8f;
                verbs.Add(new InteractionVerb("salvage", "HURDA VE AKÜ SÖK", salvageTime, null, null, 16f));
            }

            // Verb 3: Decoy Horn / Siren
            if (hasBattery)
            {
                string hornLabel = vehicleCategory == VehicleArchetypeCategory.Police ? "SİREN VE IŞIKLARI AÇ (YEM)" : "KORNADAN DİKKAT DAĞIT (YEM)";
                verbs.Add(new InteractionVerb("decoy_horn", hornLabel, 0f, null, null, 42f));
            }

            // Verb 4: Hide inside vehicle
            verbs.Add(new InteractionVerb("hide", "ARACIN İÇİNE SAKLAN", 0.6f));

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "search":
                    ExecuteSearch();
                    break;
                case "salvage":
                    ExecuteSalvage();
                    break;
                case "decoy_horn":
                    ExecuteDecoyHorn();
                    break;
                case "hide":
                    ExecuteHide(interactor);
                    break;
                case "exit_vehicle":
                    ExecuteExitVehicle(interactor);
                    break;
            }
        }

        private void ExecuteSearch()
        {
            hasBeenSearched = true;
            AnimateTrunkOpen();
            ProjectSurvival.Progression.SurvivorProgressionManager.Instance?.RegisterScavengeAction("Araç");

            // Vehicle-specific contextual loot
            if (InventorySystem.Instance != null)
            {
                switch (vehicleCategory)
                {
                    case VehicleArchetypeCategory.Ambulance:
                        InventorySystem.Instance.AddItem(LoadItem("item_medkit"), 1);
                        InventorySystem.Instance.AddItem(LoadItem("item_bandage"), 2);
                        InventorySystem.Instance.AddItem(LoadItem("item_alcohol"), 1);
                        break;
                    case VehicleArchetypeCategory.Police:
                        InventorySystem.Instance.AddItem(LoadItem("item_battery"), 2);
                        InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 3);
                        break;
                    case VehicleArchetypeCategory.Van:
                        InventorySystem.Instance.AddItem(LoadItem("item_canned_food"), 2);
                        InventorySystem.Instance.AddItem(LoadItem("item_water_bottle"), 2);
                        InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 2);
                        break;
                    default:
                        InventorySystem.Instance.AddItem(LoadItem("item_water_bottle"), 1);
                        InventorySystem.Instance.AddItem(LoadItem("item_cloth"), 2);
                        InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 1);
                        break;
                }
            }

            GameEvents.TriggerObjectiveToastRequested($"{objectTitle.ToUpper()} ARANDI — MALZEMELER ALINDI", 2.5f);
        }

        private void ExecuteSalvage()
        {
            isDamaged = true;
            hasBattery = false;

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 3);
                InventorySystem.Instance.AddItem(LoadItem("item_battery"), 1);
            }

            GameEvents.TriggerCameraShakeRequested(0.2f, 0.15f);
            GameEvents.TriggerObjectiveToastRequested("HURDA METAL (+3) VE AKÜ (+1) SÖKÜLDÜ", 2.5f);
        }

        private void ExecuteDecoyHorn()
        {
            if (isHornBlaring) return;
            hasBattery = false; // Discharges battery for emergency 10s decoy
            if (hornRoutine != null) StopCoroutine(hornRoutine);
            hornRoutine = StartCoroutine(HornBlareRoutine());
        }

        private IEnumerator HornBlareRoutine()
        {
            isHornBlaring = true;
            float elapsed = 0f;
            float duration = 10.0f;

            GameEvents.TriggerObjectiveToastRequested("DİKKAT DAĞITICI KORNA/SİREN AKTİF! (42m YEM)", 3.0f);

            while (elapsed < duration)
            {
                elapsed += 1.0f;
                // Emit continuous 42m acoustic shockwave lure
                GameEvents.TriggerNoiseEmitted(transform.position, 42f);
                ProceduralAudioManager.Instance?.PlayAirRaidSiren();

                // Flash emergency roof lights if present
                if (roofLightRed != null) roofLightRed.enabled = !roofLightRed.enabled;
                if (roofLightBlue != null) roofLightBlue.enabled = !roofLightRed.enabled;

                yield return new WaitForSeconds(1.0f);
            }

            if (roofLightRed != null) roofLightRed.enabled = false;
            if (roofLightBlue != null) roofLightBlue.enabled = false;
            isHornBlaring = false;
            hornRoutine = null;
        }

        private void ExecuteHide(GameObject player)
        {
            if (player == null) return;
            isPlayerHiddenInside = true;

            var cc = player.GetComponent<CharacterController>();
            var pc = player.GetComponent<PlayerController>();
            var visual = player.transform.Find("Visual_Model");

            if (cc != null) cc.enabled = false;
            if (pc != null) pc.enabled = false;
            if (visual != null) visual.gameObject.SetActive(false);

            player.transform.position = transform.position;
            ThirdPersonCamera.Instance?.EnterHidingPeek(peekTransform);
            GameEvents.TriggerPlayerHidingStateChanged(true);

            GameEvents.TriggerObjectiveToastRequested("ARAÇ İÇİNE SAKLANILDI (GÖRÜŞ KESİLDİ)", 2.5f);
        }

        private void ExecuteExitVehicle(GameObject player)
        {
            if (player == null) return;
            isPlayerHiddenInside = false;

            Vector3 exitPos = exitTransform != null ? exitTransform.position : (transform.position - transform.right * 1.5f);
            player.transform.position = exitPos;

            var visual = player.transform.Find("Visual_Model");
            var cc = player.GetComponent<CharacterController>();
            var pc = player.GetComponent<PlayerController>();

            if (visual != null) visual.gameObject.SetActive(true);
            if (cc != null) cc.enabled = true;
            if (pc != null) pc.enabled = true;

            ThirdPersonCamera.Instance?.ExitHidingPeek();
            GameEvents.TriggerPlayerHidingStateChanged(false);
        }

        private void AnimateTrunkOpen()
        {
            if (trunkTransform == null) return;
            if (trunkAnimRoutine != null) StopCoroutine(trunkAnimRoutine);
            trunkAnimRoutine = StartCoroutine(SmoothTrunkSlerp(trunkOpenRot));
        }

        private IEnumerator SmoothTrunkSlerp(Quaternion targetRot)
        {
            Quaternion start = trunkTransform.localRotation;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                trunkTransform.localRotation = Quaternion.Slerp(start, targetRot, t / 0.35f);
                yield return null;
            }
            trunkTransform.localRotation = targetRot;
            trunkAnimRoutine = null;
        }

        private ItemData LoadItem(string itemId)
        {
            ItemData it = Resources.Load<ItemData>($"Items/{itemId}") ?? Resources.Load<ItemData>($"Data/Items/{itemId}");
#if UNITY_EDITOR
            if (it == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{itemId} t:ItemData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    it = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                }
            }
#endif
            return it;
        }
    }
}
