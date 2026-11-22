using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    public class NPCSensor : MonoBehaviour
    {
        [Header("Detection Parameters")]
        [SerializeField] private float detectionRadius = 22f;
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float scanInterval = 0.2f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private readonly List<ZombieAI> visibleThreats = new List<ZombieAI>();
        private ZombieAI nearestThreat;
        private float scanTimer = 0f;
        private bool hadThreatLastFrame = false;

        public float DetectionRadius => detectionRadius;
        public ZombieAI NearestThreat => nearestThreat;
        public IReadOnlyList<ZombieAI> VisibleThreats => visibleThreats;
        public bool HasThreat => nearestThreat != null;
        public int ThreatCount => visibleThreats.Count;

        public event Action<ZombieAI> OnThreatSpotted;
        public event Action<Vector3> OnNoiseHeard;
        public event Action OnThreatsCleared;

        private void Awake()
        {
            // Mask out Player and Ignore Raycast from obstacle check
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) obstacleMask &= ~(1 << playerLayer);
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1) obstacleMask &= ~(1 << ignoreRaycast);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) obstacleMask &= ~(1 << uiLayer);
        }

        private void OnEnable()
        {
            GameEvents.OnNoiseEmitted += HandleNoiseEmitted;
            scanTimer = UnityEngine.Random.Range(0f, scanInterval); // Jitter scan start
        }

        private void OnDisable()
        {
            GameEvents.OnNoiseEmitted -= HandleNoiseEmitted;
            visibleThreats.Clear();
            nearestThreat = null;
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = scanInterval;
                ScanForThreats();
            }
        }

        public void ScanForThreats()
        {
            visibleThreats.Clear();
            nearestThreat = null;
            float closestDistSqr = float.MaxValue;
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

            // Efficiently query all active ZombieAI instances
            var allZombies = ZombieAI.ActiveZombies;
            if (allZombies != null)
            {
                for (int i = 0; i < allZombies.Count; i++)
                {
                    ZombieAI z = allZombies[i];
                    if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                    Vector3 zombieTargetPos = z.transform.position + Vector3.up * 1.2f;
                    float distSqr = (zombieTargetPos - eyePos).sqrMagnitude;
                    if (distSqr > detectionRadius * detectionRadius) continue;

                    // Line of sight check
                    Vector3 dir = (zombieTargetPos - eyePos).normalized;
                    float dist = Mathf.Sqrt(distSqr);

                    if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        // Check if hit is the zombie or part of the zombie
                        if (hit.transform == z.transform || hit.transform.IsChildOf(z.transform))
                        {
                            visibleThreats.Add(z);
                            if (distSqr < closestDistSqr)
                            {
                                closestDistSqr = distSqr;
                                nearestThreat = z;
                            }
                        }
                    }
                    else
                    {
                        // Direct unobstructed sight
                        visibleThreats.Add(z);
                        if (distSqr < closestDistSqr)
                        {
                            closestDistSqr = distSqr;
                            nearestThreat = z;
                        }
                    }
                }
            }
            else
            {
                // Fallback query if static active list not initialized
                ZombieAI[] fallbackZombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
                foreach (var z in fallbackZombies)
                {
                    if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                    Vector3 zombieTargetPos = z.transform.position + Vector3.up * 1.2f;
                    float distSqr = (zombieTargetPos - eyePos).sqrMagnitude;
                    if (distSqr > detectionRadius * detectionRadius) continue;

                    Vector3 dir = (zombieTargetPos - eyePos).normalized;
                    float dist = Mathf.Sqrt(distSqr);

                    if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform == z.transform || hit.transform.IsChildOf(z.transform))
                        {
                            visibleThreats.Add(z);
                            if (distSqr < closestDistSqr)
                            {
                                closestDistSqr = distSqr;
                                nearestThreat = z;
                            }
                        }
                    }
                }
            }

            if (nearestThreat != null)
            {
                if (!hadThreatLastFrame)
                {
                    hadThreatLastFrame = true;
                    OnThreatSpotted?.Invoke(nearestThreat);
                }
            }
            else if (hadThreatLastFrame)
            {
                hadThreatLastFrame = false;
                OnThreatsCleared?.Invoke();
            }
        }

        private void HandleNoiseEmitted(Vector3 noisePos, float noiseRadius)
        {
            float dist = Vector3.Distance(transform.position, noisePos);
            if (dist <= detectionRadius || dist <= noiseRadius)
            {
                OnNoiseHeard?.Invoke(noisePos);
                // Immediately force a scan
                ScanForThreats();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasThreat ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
