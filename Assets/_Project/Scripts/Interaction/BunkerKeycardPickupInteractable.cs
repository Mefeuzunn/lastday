using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive 3D World Pickup for the Level-2 Security Keycard on the Head Virologist's desk in Chamber 3.
    /// Single-tap interaction adds 1x item_bunker_keycard, awards +35 XP, and persists across save/load.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BunkerKeycardPickupInteractable : WorldInteractableObject
    {
        [Header("Keycard Pickup Properties")]
        [SerializeField] private string pickupName = "Seviye-2 Güvenlik Kartı";
        [SerializeField] private string itemId = "item_bunker_keycard";

        public string ItemId => itemId;

        public override string ObjectSubtitle => hasBeenSearched ? "Toplandı" : "Araştırma Notu & Güvenlik Kartı";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = pickupName;
            objectSubtitle = hasBeenSearched ? "Toplandı" : "Araştırma Notu & Güvenlik Kartı";
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
                    "pickup_keycard",
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
            return verbId == "pickup_keycard" && !hasBeenSearched;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "pickup_keycard" && !hasBeenSearched)
            {
                hasBeenSearched = true;
                InventorySystem.Instance?.AddItemById(itemId, 1);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                SurvivorProgressionManager.Instance?.AddXP(35);

                GameEvents.TriggerObjectiveToastRequested(
                    $"[EŞYA ALINDI] {pickupName} & Şifreli Araştırma Günlüğü! (+35 XP)", 3.0f);
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
