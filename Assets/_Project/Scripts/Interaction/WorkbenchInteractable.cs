using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive Gunsmith & Crafting Workbench.
    /// Interacting with this fixture opens the 3-Tab WorkbenchCraftingUI:
    /// - Gunsmith modifications & attachment crafting
    /// - Tactical gear & deployable traps
    /// - Essential medical & ammo supplies
    /// </summary>
    public class WorkbenchInteractable : WorldInteractableObject
    {
        protected override void Awake()
        {
            base.Awake();
            objectTitle = "ÇALIŞMA TEZGAHI & SİLAH ATÖLYESİ";
            objectSubtitle = "MODİFİKASYON VE ÜRETİM";
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>
            {
                new InteractionVerb(
                    "open_workbench",
                    "TEZGÂHI AÇ (SİLAH & TUZAK ATÖLYESİ)",
                    0f, // Instant Single-Tap
                    null,
                    null,
                    0f
                )
            };
            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "open_workbench";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "open_workbench")
            {
                if (WorkbenchCraftingUI.Instance != null)
                {
                    WorkbenchCraftingUI.Instance.OpenWorkbench();
                }
                else
                {
                    Debug.LogWarning("[WorkbenchInteractable] WorkbenchCraftingUI.Instance not found!");
                }
            }
        }
    }
}
