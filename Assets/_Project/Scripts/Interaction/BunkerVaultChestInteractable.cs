using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// High-tier military/medical vault chest inside Chamber 5 (Antiviral Synthesis Vault).
    /// Awards 2x Antiviral Serum, 2x Adrenaline Shot, 18x 9mm Ammo, 8x Crossbow Bolts, 5x Scrap Metal, and +150 XP.
    /// Persists looted state via SaveSystem.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BunkerVaultChestInteractable : WorldInteractableObject
    {
        [Header("Vault Loot State")]
        [SerializeField] private bool isLooted = false;

        public bool IsLooted => isLooted;

        public override string ObjectSubtitle => isLooted ? "Durum: Yağmalandı (Boş)" : "Askeri / Biyolojik Malzeme Kasası";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Antiviral Araştırma Kasası";
            objectSubtitle = isLooted ? "Durum: Yağmalandı" : "Askeri / Biyolojik Malzeme Kasası";
        }

        public void SetLootedSilent(bool looted)
        {
            isLooted = looted;
            hasBeenSearched = looted;
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isLooted)
            {
                verbs.Add(new InteractionVerb(
                    "open_vault_chest",
                    "KASA YAĞMALANDI (Boş)",
                    0f,
                    null,
                    null,
                    0f,
                    false,
                    "Bu kasadaki malzemeler daha önce alındı."
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "open_vault_chest",
                    "KASAYI AÇ & YAĞMALA",
                    1.5f, // 1.5s timed action
                    null,
                    null,
                    8f,
                    true
                ));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "open_vault_chest" && !isLooted;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "open_vault_chest" && !isLooted)
            {
                isLooted = true;
                hasBeenSearched = true;

                // Award Vault Loot
                if (InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.AddItemById("item_antiviral", 2);
                    InventorySystem.Instance.AddItemById("item_adrenaline", 2);
                    InventorySystem.Instance.AddItemById("ammo_9mm", 18);
                    InventorySystem.Instance.AddItemById("ammo_crossbow_bolt", 8);
                    InventorySystem.Instance.AddItemById("item_scrap", 5);
                }

                ProceduralAudioManager.Instance?.PlayCraftSuccess();
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.TriggerHeavy();
                SurvivorProgressionManager.Instance?.AddXP(150);

                UndergroundBunkerDirector.Instance?.OnVaultLooted();

                GameEvents.TriggerObjectiveToastRequested(
                    "[KASA AÇILDI] 2x Antiviral, 2x Adrenalin, 18x 9mm, 8x Ok, 5x Hurda Alındı! (+150 XP)", 3.5f);
            }
        }

        public override string GetSerializedState()
        {
            return $"{isLooted}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            if (bool.TryParse(state, out bool looted))
            {
                SetLootedSilent(looted);
            }
        }
    }
}
