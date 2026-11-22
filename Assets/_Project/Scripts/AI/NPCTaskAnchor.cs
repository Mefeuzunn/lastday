using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Interaction;
using ProjectSurvival.Environment;

namespace ProjectSurvival.AI
{
    public enum NPCTaskType
    {
        GuardPost,       // Perimeter lookout, security patrol, engaging threats
        ScavengeRoute,   // Walking to vehicles/dumpsters/crates and looting
        MedicalDuty,     // Clinic bed rounds, patient care, antiseptic synthesis
        WorkshopRepair,  // Generator maintenance, workbench crafting, door fortification
        RestAndSocial    // Campfire seating, mess hall eating, sleeping cots
    }

    [SelectionBase]
    public class NPCTaskAnchor : MonoBehaviour
    {
        public static readonly List<NPCTaskAnchor> AllAnchors = new List<NPCTaskAnchor>();

        [Header("Anchor Identity")]
        [SerializeField] private NPCTaskType taskType = NPCTaskType.RestAndSocial;
        [SerializeField] private string anchorId = "anchor_01";
        [SerializeField] private string anchorName = "Sığınak Dinlenme Alanı";
        [Tooltip("Specific NPC id required (e.g. 'aras', 'kerem', 'zeynep', 'elif') or 'any' for general pool")]
        [SerializeField] private string assignedNpcId = "any";

        [Header("Interaction Parameters")]
        [SerializeField] private float interactionDuration = 10f;
        [SerializeField] private float arrivalRadius = 1.6f;
        [SerializeField] private Vector3 preferredFacingDirection = Vector3.forward;

        [Header("Optional Physical World Linkages")]
        [SerializeField] private WorldInteractableObject targetInteractable;
        [SerializeField] private InteractiveDoor targetDoor;

        [Header("Runtime State")]
        [SerializeField] private bool isOccupied = false;
        [SerializeField] private string currentOccupantId = string.Empty;

        public NPCTaskType TaskType => taskType;
        public string AnchorId => anchorId;
        public string AnchorName => anchorName;
        public string AssignedNpcId => assignedNpcId;
        public float InteractionDuration => interactionDuration;
        public float ArrivalRadius => arrivalRadius;
        public Vector3 PreferredFacingDirection => transform.TransformDirection(preferredFacingDirection.normalized);
        public WorldInteractableObject TargetInteractable => targetInteractable;
        public InteractiveDoor TargetDoor => targetDoor;
        public bool IsOccupied => isOccupied;
        public string CurrentOccupantId => currentOccupantId;

        private void OnEnable()
        {
            if (!AllAnchors.Contains(this))
            {
                AllAnchors.Add(this);
            }
        }

        private void OnDisable()
        {
            AllAnchors.Remove(this);
            isOccupied = false;
            currentOccupantId = string.Empty;
        }

        public bool CanAccept(string npcId)
        {
            if (isOccupied && !string.Equals(currentOccupantId, npcId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrEmpty(assignedNpcId) || string.Equals(assignedNpcId, "any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(assignedNpcId, npcId, StringComparison.OrdinalIgnoreCase);
        }

        public bool Occupy(string npcId)
        {
            if (isOccupied && !string.Equals(currentOccupantId, npcId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            isOccupied = true;
            currentOccupantId = npcId;
            return true;
        }

        public void Release(string npcId = null)
        {
            if (string.IsNullOrEmpty(npcId) || string.Equals(currentOccupantId, npcId, StringComparison.OrdinalIgnoreCase))
            {
                isOccupied = false;
                currentOccupantId = string.Empty;
            }
        }

        public void SetTargetInteractable(WorldInteractableObject interactable)
        {
            targetInteractable = interactable;
        }

        public void SetTargetDoor(InteractiveDoor door)
        {
            targetDoor = door;
        }

        public void Configure(NPCTaskType type, string id, string name, string npcId, float duration)
        {
            taskType = type;
            anchorId = id;
            anchorName = name;
            assignedNpcId = npcId;
            interactionDuration = duration;
        }

        private void OnDrawGizmosSelected()
        {
            Color c = taskType switch
            {
                NPCTaskType.GuardPost => Color.red,
                NPCTaskType.ScavengeRoute => Color.yellow,
                NPCTaskType.MedicalDuty => Color.green,
                NPCTaskType.WorkshopRepair => Color.cyan,
                _ => Color.blue
            };

            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, arrivalRadius);
            Gizmos.DrawLine(transform.position, transform.position + PreferredFacingDirection * 1.5f);
        }
    }
}
