using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Backward-compatible wrapper that inherits all archetype sensory logic (LoneRoamer, Scavenger, DormantLurker).
    /// Any pre-existing prefabs or scripts referencing ZombieSensor seamlessly use the new ZombieSensorySystem.
    /// </summary>
    public class ZombieSensor : ZombieSensorySystem
    {
    }
}
