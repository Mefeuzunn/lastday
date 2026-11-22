using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Narrative
{
    [RequireComponent(typeof(Collider))]
    public class StoryTriggerZone : MonoBehaviour
    {
        [Header("Story Trigger Settings")]
        [SerializeField] private string storyResourceName = "chapter1_outbreak";
        [SerializeField] private string startNodeId = "node_doc_intro";
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private string interactionPromptText = "Dr. Aras ile Konuş";

        private bool hasTriggered = false;
        private bool playerInRange = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += HandleInteract;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= HandleInteract;
            if (playerInRange)
            {
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (hasTriggered && triggerOnce) return;

                playerInRange = true;

                if (triggerOnEnter)
                {
                    ExecuteTrigger();
                }
                else
                {
                    GameEvents.TriggerInteractionPromptChanged(true, interactionPromptText);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void HandleInteract()
        {
            if (playerInRange && !triggerOnEnter)
            {
                if (hasTriggered && triggerOnce) return;
                ExecuteTrigger();
            }
        }

        private void ExecuteTrigger()
        {
            hasTriggered = true;
            GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            GameEvents.TriggerStoryRequested(storyResourceName, startNodeId);
        }
    }
}
