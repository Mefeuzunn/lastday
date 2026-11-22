using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Phase 2 (Step 2C): Post-Siege Salvage Crate.
    /// Spawns at the outpost gate / entrance following a successful defense.
    /// Delivers critical fortification repair resources (4x Scrap, 2x Cloth) and 12x 9mm Ammo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PostSiegeSalvageCrate : WorldInteractableObject
    {
        [Header("Salvage Rewards")]
        [SerializeField] private int scrapAmount = 4;
        [SerializeField] private int clothAmount = 2;
        [SerializeField] private int ammoAmount = 12;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Kuşatma Ganimet Sandığı";
            objectSubtitle = "Savunma İkmal Malzemeleri";
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "loot",
                    "GANİMET SANDIĞINI AÇ",
                    0.8f,
                    null,
                    null,
                    15.0f,
                    true
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "looted",
                    "İKMAL SANDIĞI BOŞALTILDI",
                    0.0f,
                    null,
                    null,
                    0.0f,
                    false
                ));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId != "loot" || hasBeenSearched) return;

            hasBeenSearched = true;

            // Deliver repair supplies & ammo
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItemById("item_scrap", scrapAmount);
                InventorySystem.Instance.AddItemById("item_cloth", clothAmount);
                InventorySystem.Instance.AddItemById("ammo_9mm", ammoAmount);
            }

            // Audio & Haptic
            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerSuccess();

            // Objective Toast
            GameEvents.TriggerObjectiveToastRequested(
                $"Kuşatma Ganimet Sandığı Toplandı! (+{scrapAmount} Hurda, +{clothAmount} Kumaş, +{ammoAmount} Mühimmat)",
                4.0f
            );

            SetFocused(false);
        }
    }
}
