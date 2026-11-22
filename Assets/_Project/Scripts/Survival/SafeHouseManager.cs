using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    public class SafeHouseManager : MonoBehaviour
    {
        public static SafeHouseManager Instance { get; private set; }

        [Header("Zone Configuration")]
        [SerializeField] private string safeZoneId = "Zone_SafeHouse";
        [SerializeField] private Collider safeZoneTrigger;
        [SerializeField] private Light safeHouseInteriorLight;

        [Header("State")]
        [SerializeField] private bool isPlayerInside = false;

        public string SafeZoneId => safeZoneId;
        public bool IsPlayerInside => isPlayerInside;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (safeZoneTrigger == null)
            {
                safeZoneTrigger = GetComponent<Collider>();
            }
            if (safeZoneTrigger != null)
            {
                safeZoneTrigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = true;
                GameEvents.TriggerSafeHouseStateChanged(true);
                if (safeHouseInteriorLight != null) safeHouseInteriorLight.enabled = true;
                Debug.Log($"[SafeHouseManager] Oyuncu güvenli sığınağa girdi: {safeZoneId}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = false;
                GameEvents.TriggerSafeHouseStateChanged(false);
                Debug.Log("[SafeHouseManager] Oyuncu güvenli sığınaktan ayrıldı.");
            }
        }
    }
}
