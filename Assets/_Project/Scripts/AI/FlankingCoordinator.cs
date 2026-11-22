using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Coordinates chasing zombies around their target (Player or NPC), allocating distinct angular slots
    /// (0°, ±40°, ±85°, ±140°, 180°) to prevent cluster stacking and execute strategic encirclement.
    /// </summary>
    public class FlankingCoordinator : MonoBehaviour
    {
        public static FlankingCoordinator Instance { get; private set; }

        private const float StagingRadius = 2.4f;
        private const float MeleeRadius = 1.45f;
        private const float ScreamerStandoffMin = 7.0f;
        private const float ScreamerStandoffMax = 9.5f;

        // 8 distinct angular sectors around target
        private static readonly float[] FrontalSlots = { 0f, 40f, -40f };
        private static readonly float[] FlankingSlots = { 85f, -85f, 140f, -140f, 180f };

        private class TargetEngagement
        {
            public Transform Target;
            public readonly List<ZombieAI> Attackers = new List<ZombieAI>();
            public readonly Dictionary<ZombieAI, float> AssignedAngles = new Dictionary<ZombieAI, float>();
            public float LastRebalanceTime;
        }

        private readonly Dictionary<Transform, TargetEngagement> activeEngagements = new Dictionary<Transform, TargetEngagement>();
        private readonly Dictionary<ZombieAI, Transform> zombieToTarget = new Dictionary<ZombieAI, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Registers a chasing zombie with the coordinator for slot-based flanking.
        /// </summary>
        public void RegisterAttacker(ZombieAI zombie, Transform target)
        {
            if (zombie == null || target == null || zombie.IsDead) return;

            // If already registered to a different target, remove first
            if (zombieToTarget.TryGetValue(zombie, out Transform currentTarget))
            {
                if (currentTarget == target) return;
                UnregisterAttacker(zombie);
            }

            if (!activeEngagements.TryGetValue(target, out TargetEngagement engagement))
            {
                engagement = new TargetEngagement { Target = target };
                activeEngagements[target] = engagement;
            }

            if (!engagement.Attackers.Contains(zombie))
            {
                engagement.Attackers.Add(zombie);
                zombieToTarget[zombie] = target;
                AssignSlotForZombie(engagement, zombie);
            }
        }

        /// <summary>
        /// Unregisters an attacker when it loses target, enters stun/death, or disables.
        /// </summary>
        public void UnregisterAttacker(ZombieAI zombie)
        {
            if (zombie == null) return;

            if (zombieToTarget.TryGetValue(zombie, out Transform target))
            {
                zombieToTarget.Remove(zombie);
                if (activeEngagements.TryGetValue(target, out TargetEngagement engagement))
                {
                    engagement.Attackers.Remove(zombie);
                    engagement.AssignedAngles.Remove(zombie);

                    if (engagement.Attackers.Count == 0)
                    {
                        activeEngagements.Remove(target);
                    }
                    else
                    {
                        RebalanceSlots(engagement);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the optimal tactical destination for this zombie around the target.
        /// </summary>
        public Vector3 GetAssignedDestination(ZombieAI zombie, Transform target)
        {
            if (zombie == null || target == null) return target != null ? target.position : Vector3.zero;

            Vector3 targetPos = target.position;
            Vector3 toZombie = zombie.transform.position - targetPos;
            toZombie.y = 0f;
            float currentDist = toZombie.magnitude;

            // Screamer keeps backline standoff distance (7m - 9.5m)
            if (zombie.Type == ZombieType.Screamer)
            {
                Vector3 standoffDir = toZombie.sqrMagnitude > 0.01f ? toZombie.normalized : -target.forward;
                Vector3 desiredStandoff = targetPos + standoffDir * Random.Range(ScreamerStandoffMin, ScreamerStandoffMax);
                if (NavMesh.SamplePosition(desiredStandoff, out NavMeshHit hitScreamer, 3.0f, NavMesh.AllAreas))
                {
                    return hitScreamer.position;
                }
                return desiredStandoff;
            }

            // Retrieve assigned angular slot
            float angleOffset = 0f;
            if (activeEngagements.TryGetValue(target, out TargetEngagement engagement))
            {
                if (!engagement.AssignedAngles.TryGetValue(zombie, out angleOffset))
                {
                    AssignSlotForZombie(engagement, zombie);
                    engagement.AssignedAngles.TryGetValue(zombie, out angleOffset);
                }
            }

            // Staging vs Melee radius: if already close and facing slot, close into melee radius
            float desiredRadius = (currentDist <= StagingRadius + 0.3f) ? MeleeRadius : StagingRadius;

            // Calculate slot position relative to target's forward facing
            Vector3 baseForward = target.forward;
            baseForward.y = 0f;
            if (baseForward.sqrMagnitude < 0.01f) baseForward = Vector3.forward;

            Quaternion rot = Quaternion.Euler(0f, angleOffset, 0f);
            Vector3 slotDirection = rot * baseForward.normalized;
            Vector3 desiredPoint = targetPos + slotDirection * desiredRadius;

            if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return targetPos;
        }

        private void AssignSlotForZombie(TargetEngagement engagement, ZombieAI zombie)
        {
            HashSet<float> occupiedAngles = new HashSet<float>(engagement.AssignedAngles.Values);

            // Runners prioritize flanking / rear angles to cut off retreat
            if (zombie.Type == ZombieType.Runner)
            {
                for (int i = 0; i < FlankingSlots.Length; i++)
                {
                    float angle = FlankingSlots[i];
                    if (!occupiedAngles.Contains(angle))
                    {
                        engagement.AssignedAngles[zombie] = angle;
                        return;
                    }
                }
            }
            // Tanks and Standards prioritize frontal pressure
            else
            {
                for (int i = 0; i < FrontalSlots.Length; i++)
                {
                    float angle = FrontalSlots[i];
                    if (!occupiedAngles.Contains(angle))
                    {
                        engagement.AssignedAngles[zombie] = angle;
                        return;
                    }
                }
            }

            // Fallback: pick any unassigned slot from remaining pool
            float[] allSlots = { 0f, 40f, -40f, 85f, -85f, 140f, -140f, 180f };
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (!occupiedAngles.Contains(allSlots[i]))
                {
                    engagement.AssignedAngles[zombie] = allSlots[i];
                    return;
                }
            }

            // If more than 8 attackers, distribute uniformly
            float uniformAngle = (engagement.Attackers.Count * 45f) % 360f;
            engagement.AssignedAngles[zombie] = uniformAngle;
        }

        private void RebalanceSlots(TargetEngagement engagement)
        {
            engagement.AssignedAngles.Clear();
            for (int i = 0; i < engagement.Attackers.Count; i++)
            {
                ZombieAI z = engagement.Attackers[i];
                if (z != null && !z.IsDead)
                {
                    AssignSlotForZombie(engagement, z);
                }
            }
        }

        private void Update()
        {
            // Time-sliced cleanup and slot maintenance (every 0.5s)
            if (Time.frameCount % 30 == 0)
            {
                CleanupDeadAttackers();
            }
        }

        private void CleanupDeadAttackers()
        {
            List<ZombieAI> toRemove = null;
            foreach (var kvp in zombieToTarget)
            {
                if (kvp.Key == null || kvp.Key.IsDead || !kvp.Key.gameObject.activeInHierarchy)
                {
                    if (toRemove == null) toRemove = new List<ZombieAI>();
                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    UnregisterAttacker(toRemove[i]);
                }
            }
        }
    }
}
