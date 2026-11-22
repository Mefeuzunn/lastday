using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 2 (Step 2A): Lootable Corpse Component attached to defeated Human Raiders.
    /// Implements WorldInteractableObject with 'YAĞMACIYI ARA' (holdDuration = 0.5s).
    /// Drops guaranteed combat supplies (6–12x 9mm ammo or 4x Shotgun Shells, plus bandage/food/scrap),
    /// awards +45 Combat XP, and maintains full compatibility with BodyConcealmentSystem corpse dragging.
    /// </summary>
    public class RaiderLootCorpse : WorldInteractableObject
    {
        [Header("Corpse Loot Parameters")]
        [SerializeField] private bool isShotgunRaider = false;
        [SerializeField] private int ammoCount = 8;
        [SerializeField] private string bonusItemId = "item_bandage";
        [SerializeField] private int combatXpReward = 45;

        private static readonly string[] PossibleBonusItems = new string[]
        {
            "item_bandage",
            "item_canned_food",
            "item_scrap"
        };

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Etkisiz Yağmacı";
            objectSubtitle = "Yağmalanabilir Ceset";

            if (ammoCount <= 0)
            {
                ammoCount = isShotgunRaider ? 4 : Random.Range(6, 13);
            }

            if (string.IsNullOrEmpty(bonusItemId))
            {
                bonusItemId = PossibleBonusItems[Random.Range(0, PossibleBonusItems.Length)];
            }
        }

        public void ConfigureLoot(bool isShotgun)
        {
            isShotgunRaider = isShotgun;
            ammoCount = isShotgun ? 4 : Random.Range(6, 13);
            bonusItemId = PossibleBonusItems[Random.Range(0, PossibleBonusItems.Length)];
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "loot",
                    "YAĞMACIYI ARA",
                    0.5f,
                    null,
                    null,
                    8.0f,
                    true
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "looted",
                    "CESET BOŞALTILDI",
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

            // 1. Deliver Guaranteed Ammo
            string ammoId = isShotgunRaider ? "ammo_shells" : "ammo_9mm";
            string ammoName = isShotgunRaider ? "Fişek" : "9mm Mermi";

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItemById(ammoId, ammoCount);
                if (!string.IsNullOrEmpty(bonusItemId))
                {
                    InventorySystem.Instance.AddItemById(bonusItemId, 1);
                }
            }

            // 2. Award Combat XP (+45)
            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.AddXP(combatXpReward, "Savaş XP - Yağmacı Etkisiz Hale Getirildi");
            }

            // 3. Audio & Haptic Sensory Feedback
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerMedium();

            // 4. Toast Notification
            string bonusName = bonusItemId == "item_bandage" ? "Bandaj" : (bonusItemId == "item_canned_food" ? "Konserve" : "Hurda");
            GameEvents.TriggerObjectiveToastRequested($"+{ammoCount}x {ammoName}, +1x {bonusName} & +{combatXpReward} Savaş XP", 2.8f);

            // 5. Ensure corpse remains registered with BodyConcealmentSystem for dragging
            if (BodyConcealmentSystem.Instance != null)
            {
                BodyConcealmentSystem.Instance.RegisterCorpse(gameObject);
            }

            // 6. Update Visual Focus
            SetFocused(false);
        }
    }
}
