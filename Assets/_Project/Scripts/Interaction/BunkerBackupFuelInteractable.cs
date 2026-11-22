using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Wall rack in Chamber 2 holding 1x Dolu Yakıt Bidonu (item_fuel_can).
    /// Prevents player soft-locking if arriving at the bunker without fuel for the generator.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BunkerBackupFuelInteractable : WorldInteractableObject
    {
        [Header("Fuel Rack State")]
        [SerializeField] private string itemId = "item_fuel_can";
        [SerializeField] private int quantity = 1;

        public override string ObjectSubtitle => hasBeenSearched ? "Boş" : "Yedek Yakıt Bidonu (Dolu)";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Dolu Yakıt Bidonu";
            objectSubtitle = hasBeenSearched ? "Boş" : "Yedek Yakıt Bidonu (Dolu)";
        }

        private void Start()
        {
            if (hasBeenSearched)
            {
                gameObject.SetActive(false);
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "pickup_fuel_can",
                    "AL: Dolu Yakıt Bidonu",
                    0f, // Instant Single-Tap
                    null,
                    null,
                    2f,
                    true
                ));
            }
            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "pickup_fuel_can" && !hasBeenSearched;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "pickup_fuel_can" && !hasBeenSearched)
            {
                hasBeenSearched = true;
                InventorySystem.Instance?.AddItemById(itemId, quantity);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("[EŞYA ALINDI] Dolu Yakıt Bidonu", 2.2f);
                gameObject.SetActive(false);
            }
        }

        public override string GetSerializedState()
        {
            return $"{hasBeenSearched}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            if (bool.TryParse(state, out bool searched))
            {
                hasBeenSearched = searched;
                if (hasBeenSearched)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
