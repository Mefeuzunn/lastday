using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Contract for all physical objects in the world supporting multi-verb context-sensitive interactions.
    /// </summary>
    public interface IUniversalInteractable
    {
        string ObjectTitle { get; }
        string ObjectSubtitle { get; }
        Transform InteractionTransform { get; }
        string PersistentId { get; }

        IReadOnlyList<InteractionVerb> GetAvailableVerbs();
        bool CanInteract(string verbId);
        void ExecuteVerb(string verbId, GameObject interactor);
        void SetFocused(bool focused);

        string GetSerializedState();
        void LoadSerializedState(string state);
    }
}
