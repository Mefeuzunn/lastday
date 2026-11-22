using UnityEngine;

namespace ProjectSurvival.Missions
{
    [RequireComponent(typeof(Collider))]
    public class MissionZone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private string zoneId = "Zone_Clinic";
        [SerializeField] private string zoneDisplayName = "Eski Klinik";
        [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.2f, 0.35f);

        public string ZoneId => zoneId;
        public string ZoneDisplayName => zoneDisplayName;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.OnPlayerEnteredZone(zoneId);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.OnPlayerExitedZone(zoneId);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
