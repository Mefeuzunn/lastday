using System;
using UnityEngine;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Represents a discrete, context-sensitive action verb that an interactable object exposes.
    /// Can be instant (holdDuration = 0) or physical/timed (holdDuration > 0).
    /// </summary>
    [Serializable]
    public class InteractionVerb
    {
        public string verbId;
        public string displayLabel;
        public Sprite verbIcon;
        public float holdDuration; // 0f = instant, > 0f = hold to complete
        public string requiredItemId; // e.g. "item_crowbar", "item_scrap", "item_alcohol"
        public string requiredItemName; // Turkish localized name for display, e.g. "Levye"
        public float acousticNoiseRadius;
        public bool isAvailable = true;
        public string disabledReason = string.Empty;

        public InteractionVerb(string id, string label, float duration = 0f, string reqItem = null, string reqItemName = null, float noise = 0f, bool available = true, string reason = "")
        {
            verbId = id;
            displayLabel = label;
            holdDuration = duration;
            requiredItemId = reqItem;
            requiredItemName = reqItemName;
            acousticNoiseRadius = noise;
            isAvailable = available;
            disabledReason = reason ?? string.Empty;
        }

        public InteractionVerb Clone()
        {
            return new InteractionVerb(verbId, displayLabel, holdDuration, requiredItemId, requiredItemName, acousticNoiseRadius)
            {
                verbIcon = this.verbIcon,
                isAvailable = this.isAvailable,
                disabledReason = this.disabledReason
            };
        }
    }
}
