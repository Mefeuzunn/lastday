using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 3 (Step 3B): Physical Animal Harvest Interaction Point.
    /// Enables 1.0s tactile knife harvesting of defeated forest wildlife (Deer and Boars),
    /// awarding raw meat, clean cloth/leather, and survival progression XP.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AnimalHarvestInteractable : WorldInteractableObject
    {
        [Header("Harvest Configuration")]
        [SerializeField] private string animalSpecies = "Av Hayvanı";
        [SerializeField] private int rawMeatCount = 2;
        [SerializeField] private int clothCount = 1;
        [SerializeField] private float holdTime = 1.0f;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = $"Av: {animalSpecies}";
            objectSubtitle = "Yaban Eti & Deri Toplama";
        }

        public void Configure(string species, int meat, int cloth)
        {
            animalSpecies = species;
            rawMeatCount = meat;
            clothCount = cloth;
            objectTitle = $"Av: {animalSpecies}";
            objectSubtitle = $"{rawMeatCount}x Çiğ Et, {clothCount}x Kumaş/Deri";
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "harvest_animal",
                    $"AVI TOPLA ({animalSpecies.ToUpper()})",
                    holdTime, // 1.0s tactile knife harvest hold
                    null,
                    null,
                    0f
                ));
            }
            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (hasBeenSearched) return false;
            return verbId == "harvest_animal";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "harvest_animal" && !hasBeenSearched)
            {
                ExecuteHarvest();
            }
        }

        private void ExecuteHarvest()
        {
            hasBeenSearched = true;

            // 1. Audio & Haptics
            ProceduralAudioManager.Instance?.PlayKnifeHarvest(transform.position);
            MobileHapticManager.TriggerLight();

            // 2. Add Harvest Loot
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItemById("item_raw_meat", rawMeatCount);
                InventorySystem.Instance.AddItemById("item_cloth", clothCount);
            }

            // 3. Award Survival XP (+35 XP)
            SurvivorProgressionManager.Instance?.AddXP(35);

            // 4. Toast Notification
            GameEvents.TriggerObjectiveToastRequested($"[AV TOPLANDI] {rawMeatCount}x Çiğ Av Eti, {clothCount}x Kumaş/Deri Alındı (+35 XP)", 3.0f);

            // 5. Shrink & Cleanup Visual
            Destroy(gameObject, 2.5f);
        }
    }
}
