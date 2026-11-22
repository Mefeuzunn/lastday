using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive 3D World Pickup for Starter Empty Jerrycans.
    /// Single-tap interaction (holdDuration = 0) adds 1x item_empty_jerrycan to player inventory,
    /// triggers tactile haptic + audio feedback, and persists state via SaveSystem.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class JerrycanPickupInteractable : WorldInteractableObject
    {
        [Header("Jerrycan Pickup Properties")]
        [SerializeField] private string pickupName = "Boş Yakıt Bidonu";
        [SerializeField] private string itemId = "item_empty_jerrycan";
        [SerializeField] private int quantity = 1;

        public string ItemId => itemId;
        public int Quantity => quantity;

        public override string ObjectSubtitle => hasBeenSearched ? "Toplandı" : "Eşya Toplama (Tek Dokunuş)";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = pickupName;
            objectSubtitle = hasBeenSearched ? "Toplandı" : "Eşya Toplama (Tek Dokunuş)";
        }

        private void Start()
        {
            if (hasBeenSearched)
            {
                gameObject.SetActive(false);
            }
        }

        public void ConfigurePickup(string id, string name = "Boş Yakıt Bidonu")
        {
            SetPersistentId(id);
            pickupName = name;
            objectTitle = name;
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "pickup_jerrycan",
                    $"AL: {pickupName}",
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
            return verbId == "pickup_jerrycan" && !hasBeenSearched;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "pickup_jerrycan" && !hasBeenSearched)
            {
                hasBeenSearched = true;
                InventorySystem.Instance?.AddItemById(itemId, quantity);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested($"[EŞYA ALINDI] {pickupName}", 2.2f);
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
