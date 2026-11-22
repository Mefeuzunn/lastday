using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.Narrative
{
    /// <summary>
    /// Active Phase 5: Interactive Story Milestone Terminal.
    /// Manages physical consoles for Clinic research downloads, Military railcar cipher recovery,
    /// Bunker access protocols, and Relay Tower moral broadcasts.
    /// </summary>
    public class StoryTerminalInteractable : WorldInteractableObject
    {
        [Header("Terminal Identity")]
        [SerializeField] private string terminalId = "terminal_clinic";
        [SerializeField] private string terminalVerbLabel = "TERMİNALİ İNCELE";
        [SerializeField] private int requiredChapter = 1;

        public string TerminalId => terminalId;
        public int RequiredChapter => requiredChapter;

        protected override void Awake()
        {
            base.Awake();
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            bool isRelevant = MainStoryDirector.Instance != null && MainStoryDirector.Instance.CurrentChapter == requiredChapter;
            bool isCompleted = MainStoryDirector.Instance != null && MainStoryDirector.Instance.CurrentChapter > requiredChapter;

            string label = isCompleted ? $"{objectTitle} (VERİLER ALINDI)" : terminalVerbLabel;
            bool available = isRelevant && !hasBeenSearched;

            verbs.Add(new InteractionVerb("access_terminal", label, 1.2f, null, null, 10f, available));
            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "access_terminal")
            {
                if (MainStoryDirector.Instance != null)
                {
                    bool success = MainStoryDirector.Instance.TryInteractWithStoryTerminal(terminalId);
                    if (success)
                    {
                        hasBeenSearched = true;
                    }
                    else
                    {
                        ProceduralAudioManager.Instance?.PlaySoftError();
                    }
                }
            }
        }

        public void ConfigureTerminal(string id, string title, string subtitle, string verbLabel, int chapter)
        {
            terminalId = id;
            objectTitle = title;
            objectSubtitle = subtitle;
            terminalVerbLabel = verbLabel;
            requiredChapter = chapter;
        }
    }
}
