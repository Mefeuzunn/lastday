using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    [RequireComponent(typeof(Collider))]
    public class RestPoint : MonoBehaviour
    {
        [Header("Rest Parameters")]
        [SerializeField] private float hungerConsumed = 20f;
        [SerializeField] private float thirstConsumed = 25f;
        [SerializeField] private float healthRestored = 45f;
        [SerializeField] private float wakeUpHour = 7.0f; // 07:00 AM

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
                playerInRange = true;
                GameEvents.TriggerInteractionPromptChanged(true, "UYU / DİNLEN (Sabah 07:00'ye Geç)");
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
            if (!playerInRange) return;
            ExecuteRest();
        }

        public void ExecuteRest()
        {
            SurvivalStats stats = FindFirstObjectByType<SurvivalStats>();
            if (stats != null)
            {
                stats.TakeDamage(0f); // just refresh
                stats.Heal(healthRestored);

                // Drain hunger & thirst
                float newHunger = Mathf.Max(0f, stats.CurrentHunger - hungerConsumed);
                float newThirst = Mathf.Max(0f, stats.CurrentThirst - thirstConsumed);
                stats.Feed(-(stats.CurrentHunger - newHunger));
                stats.Hydrate(-(stats.CurrentThirst - newThirst));

                Debug.Log($"[RestPoint] Oyuncu dinlendi. +{healthRestored} Can, -{hungerConsumed} Açlık, -{thirstConsumed} Su.");
            }

            // Fast forward time to morning
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.SetTime(wakeUpHour);
            }

            // Camera shake / visual feedback
            GameEvents.TriggerCameraShakeRequested(0.15f, 0.4f);

            // Auto-save game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
            }

            Debug.Log("[RestPoint] Zaman sabah 07:00'ye ilerletildi. Oyun kaydedildi.");
        }
    }
}
