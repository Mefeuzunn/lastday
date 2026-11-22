using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.AI;

namespace ProjectSurvival.Survival
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrownDistraction : MonoBehaviour
    {
        [Header("Distraction Config")]
        [SerializeField] private float noiseRadius = 18f;
        [SerializeField] private float lifetime = 5f;

        private bool hasTriggered = false;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        public void Initialize(float radius, Vector3 throwForce)
        {
            noiseRadius = radius > 0f ? radius : 18f;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwForce, ForceMode.Impulse);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasTriggered) return;
            hasTriggered = true;

            // Audio & Event
            ProceduralAudioManager.Instance?.PlayGlassShatter();
            GameEvents.TriggerNoiseEmitted(transform.position, noiseRadius);

            // Alert all zombies in range
            Vector3 impactPos = transform.position;
            var activeZombies = ZombieAI.ActiveZombies;
            if (activeZombies != null)
            {
                for (int i = 0; i < activeZombies.Count; i++)
                {
                    ZombieAI z = activeZombies[i];
                    if (z != null && !z.IsDead)
                    {
                        if (Vector3.Distance(z.transform.position, impactPos) <= noiseRadius)
                        {
                            z.OnNoiseDetected(impactPos);
                        }
                    }
                }
            }

            Debug.Log($"[ThrownDistraction] Glass bottle shattered at {impactPos}! Alerted zombies within {noiseRadius}m.");
            Destroy(gameObject, 0.15f);
        }
    }
}
