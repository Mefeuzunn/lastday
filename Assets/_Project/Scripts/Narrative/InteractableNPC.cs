using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.Narrative
{
    public enum NPCStatusType
    {
        None,
        QuestAvailable, // '!'
        QuestActive,    // '?'
        Chat            // '...'
    }

    [RequireComponent(typeof(SphereCollider))]
    public class InteractableNPC : MonoBehaviour
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcId = "aras";
        [SerializeField] private string npcDisplayName = "Dr. Aras";

        [Header("Dialogue / Story Binding")]
        [SerializeField] private string storyResourceName = "chapter1_outbreak";
        [SerializeField] private string startNodeId = "node_doc_intro";

        [Header("Interaction Setup")]
        [SerializeField] private float interactionRadius = 2.6f;
        [SerializeField] private Vector3 overheadBadgeOffset = new Vector3(0f, 2.35f, 0f);
        [SerializeField] private NPCStatusType initialStatus = NPCStatusType.QuestAvailable;

        [Header("Overhead Visual")]
        [SerializeField] private GameObject overheadBadgeObj;
        [SerializeField] private Text overheadBadgeText;

        private bool isPlayerInRange = false;
        private Transform cameraTransform;
        private SphereCollider triggerCollider;

        public string NpcId => npcId;
        public string NpcDisplayName => npcDisplayName;

        private void Awake()
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = interactionRadius;
            }

            CreateOverheadMarkerIfMissing();
            SetStatus(initialStatus);
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnDialogueClosed += HandleDialogueClosed;
        }

        private void OnDisable()
        {
            if (isPlayerInRange)
            {
                GameEvents.OnInteractPressed -= HandleInteract;
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
                isPlayerInRange = false;
            }
            GameEvents.OnDialogueClosed -= HandleDialogueClosed;
        }

        private void LateUpdate()
        {
            // Update billboard orientation to face camera
            if (overheadBadgeObj != null && overheadBadgeObj.activeSelf)
            {
                if (cameraTransform == null && Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }

                if (cameraTransform != null)
                {
                    overheadBadgeObj.transform.rotation = cameraTransform.rotation;
                }
            }
        }

        public void SetStatus(NPCStatusType status)
        {
            if (overheadBadgeObj != null)
            {
                overheadBadgeObj.SetActive(false);
            }
        }

        public void SetStartNode(string nodeId)
        {
            startNodeId = nodeId;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isPlayerInRange)
            {
                isPlayerInRange = true;
                GameEvents.OnInteractPressed += HandleInteract;
                GameEvents.TriggerInteractionPromptChanged(true, $"KONUŞ: {npcDisplayName}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && isPlayerInRange)
            {
                isPlayerInRange = false;
                GameEvents.OnInteractPressed -= HandleInteract;
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void HandleInteract()
        {
            if (!isPlayerInRange) return;

            Debug.Log($"[InteractableNPC] Starting interaction with {npcDisplayName} ({npcId}) at node: {startNodeId}");

            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.StartStory(storyResourceName, startNodeId);
            }
        }

        private void HandleDialogueClosed()
        {
            // If still in range when dialogue ends, re-display interaction prompt
            if (isPlayerInRange)
            {
                GameEvents.TriggerInteractionPromptChanged(true, $"KONUŞ: {npcDisplayName}");
            }
        }

        private void CreateOverheadMarkerIfMissing()
        {
            // Fully disabled: Overhead '!' markers removed per AAA immersion standards
            if (overheadBadgeObj != null) overheadBadgeObj.SetActive(false);
        }
    }
}
