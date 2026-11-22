using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Events;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 2 (Step 2B): Physical interaction target for cornered civilian in SOS distress events.
    /// Locks interaction until all attacking zombies within 14m are eliminated,
    /// then presents the studio-grade 0.6s hold verb "SİVİLİ KURTAR".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CivilianRescueInteractable : WorldInteractableObject
    {
        [Header("Rescue State")]
        [SerializeField] private bool isRescued = false;
        [SerializeField] private bool canInteract = false;

        private CivilianSOSEvent parentEvent;

        public bool IsRescued => isRescued;
        public bool IsRescueInteractionAvailable => canInteract;

        public override bool CanInteract(string verbId)
        {
            if (isRescued) return false;
            if (verbId == "rescue") return canInteract;
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Kuşatılmış Sivil";
            objectSubtitle = "Zombiler Etrafını Sardı!";
        }

        public void BindEvent(CivilianSOSEvent sosEvent)
        {
            parentEvent = sosEvent;
        }

        public void SetDangerStatus(bool dangerCleared)
        {
            if (isRescued) return;

            canInteract = dangerCleared;
            if (canInteract)
            {
                objectTitle = "Kurtarılabilir Sivil";
                objectSubtitle = "Tehlike Geçti - Kurtarmak İçin Basılı Tut";
            }
            else
            {
                objectTitle = "Kuşatılmış Sivil";
                objectSubtitle = "Zombiler Etrafını Sardı!";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!isRescued)
            {
                if (canInteract)
                {
                    verbs.Add(new InteractionVerb(
                        "rescue",
                        "SİVİLİ KURTAR",
                        0.6f,
                        null,
                        null,
                        15.0f,
                        true
                    ));
                }
                else
                {
                    verbs.Add(new InteractionVerb(
                        "danger",
                        "ÖNCE SALDIRGANLARI TEMİZLE",
                        0.0f,
                        null,
                        null,
                        0.0f,
                        false
                    ));
                }
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "rescued",
                    "SİVİL KURTARILDI",
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
            if (verbId != "rescue" || isRescued || !canInteract) return;

            isRescued = true;
            canInteract = false;
            hasBeenSearched = true;

            SetFocused(false);

            if (parentEvent != null)
            {
                parentEvent.OnCivilianRescued();
            }
        }
    }
}
