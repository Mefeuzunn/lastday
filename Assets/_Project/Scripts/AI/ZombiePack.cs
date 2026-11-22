using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Runtime container managing a cohesive group of 4 to 6 zombies roaming together.
    /// Handles macro leader navigation, follower formation offsets, dynamic leader succession,
    /// and staggered acoustic/visual pack alert propagation.
    /// </summary>
    public class ZombiePack : MonoBehaviour
    {
        [Header("Pack Identification")]
        [SerializeField] private string packId = "Pack_Alpha";
        [SerializeField] private ZombieAI packLeader;
        [SerializeField] private List<ZombieAI> packMembers = new List<ZombieAI>();

        [Header("Formation Settings")]
        [SerializeField] private float formationRadius = 2.2f;
        [SerializeField] private float speedSyncJitter = 0.08f;

        private static readonly Vector3[] FormationOffsets =
        {
            new Vector3(-2.2f, 0f, -2.2f),
            new Vector3(2.2f, 0f, -2.2f),
            new Vector3(-1.8f, 0f, -4.2f),
            new Vector3(1.8f, 0f, -4.2f),
            new Vector3(0f, 0f, -4.8f)
        };

        private bool isAlerted = false;
        private Transform currentPackTarget;
        private float leaderSuccessionTimer = 0f;
        private const float MaxSuccessionDelay = 0.4f;

        // Follower staggered alert timers
        private readonly Dictionary<ZombieAI, float> pendingAlertDelays = new Dictionary<ZombieAI, float>();
        private float alertDelayMultiplier = 1.0f;

        public string PackId => packId;
        public ZombieAI Leader => packLeader;
        public IReadOnlyList<ZombieAI> Members => packMembers;
        public bool IsAlerted => isAlerted;
        public float FormationRadius { get => formationRadius; set => formationRadius = value; }
        public float AlertDelayMultiplier { get => alertDelayMultiplier; set => alertDelayMultiplier = value; }
        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < packMembers.Count; i++)
                {
                    if (packMembers[i] != null && !packMembers[i].IsDead) count++;
                }
                return count;
            }
        }

        public void InitializePack(string id, ZombieAI leader, List<ZombieAI> followers)
        {
            packId = id;
            packLeader = leader;
            packMembers.Clear();

            if (leader != null)
            {
                packMembers.Add(leader);
                leader.SetPack(this);
            }

            if (followers != null)
            {
                for (int i = 0; i < followers.Count; i++)
                {
                    ZombieAI f = followers[i];
                    if (f != null && !packMembers.Contains(f))
                    {
                        packMembers.Add(f);
                        f.SetPack(this);
                    }
                }
            }

            SyncFollowerSpeeds();
        }

        public void AddMember(ZombieAI zombie)
        {
            if (zombie == null || packMembers.Contains(zombie)) return;
            packMembers.Add(zombie);
            zombie.SetPack(this);

            if (packLeader == null || packLeader.IsDead)
            {
                packLeader = zombie;
            }
        }

        public void RemoveMember(ZombieAI zombie)
        {
            if (zombie == null) return;
            packMembers.Remove(zombie);
            pendingAlertDelays.Remove(zombie);

            if (packLeader == zombie)
            {
                packLeader = null;
                leaderSuccessionTimer = 0f;
                PromoteNearestLeader();
            }
        }

        /// <summary>
        /// Dynamic Leader Succession: If leader dies or is lost, immediately promotes
        /// the nearest living pack member within <= 0.4s.
        /// </summary>
        public void PromoteNearestLeader()
        {
            ZombieAI bestCandidate = null;
            float nearestDist = float.MaxValue;
            Vector3 lastLeaderPos = (packLeader != null) ? packLeader.transform.position : transform.position;

            // Prioritize Tanks, then Screamers, then Standards/Runners
            for (int i = 0; i < packMembers.Count; i++)
            {
                ZombieAI candidate = packMembers[i];
                if (candidate == null || candidate.IsDead || !candidate.gameObject.activeInHierarchy) continue;

                // Priority bonus for resilient archetypes
                float priorityBonus = 0f;
                if (candidate.Type == ZombieType.Tank) priorityBonus = 20f;
                else if (candidate.Type == ZombieType.Screamer) priorityBonus = 12f;

                float dist = Vector3.Distance(lastLeaderPos, candidate.transform.position) - priorityBonus;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    bestCandidate = candidate;
                }
            }

            packLeader = bestCandidate;
            if (packLeader != null)
            {
                packLeader.SetPackLeader(true);
                Debug.Log($"[ZombiePack:{packId}] Leader succession executed: {packLeader.gameObject.name} is now Pack Leader!");
            }
        }

        /// <summary>
        /// Time-sliced pack tick called by ZombiePackManager.
        /// </summary>
        public void UpdatePack(float dt)
        {
            // 1. Validate Pack Leader
            if (packLeader == null || packLeader.IsDead || !packLeader.gameObject.activeInHierarchy)
            {
                leaderSuccessionTimer += dt;
                if (leaderSuccessionTimer >= MaxSuccessionDelay)
                {
                    leaderSuccessionTimer = 0f;
                    PromoteNearestLeader();
                }
                if (packLeader == null) return;
            }

            // 2. Process Staggered Reaction Delays
            if (pendingAlertDelays.Count > 0)
            {
                List<ZombieAI> readyZombies = null;
                var keys = new List<ZombieAI>(pendingAlertDelays.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    ZombieAI z = keys[i];
                    if (z == null || z.IsDead)
                    {
                        if (readyZombies == null) readyZombies = new List<ZombieAI>();
                        readyZombies.Add(z);
                        continue;
                    }

                    pendingAlertDelays[z] -= dt;
                    if (pendingAlertDelays[z] <= 0f)
                    {
                        if (readyZombies == null) readyZombies = new List<ZombieAI>();
                        readyZombies.Add(z);

                        // Trigger alerted state for follower
                        if (currentPackTarget != null)
                        {
                            z.SetTarget(currentPackTarget);
                            z.SetState(ZombieState.Chase);
                            ProceduralAudioManager.Instance?.PlayPackAlertBark();
                        }
                    }
                }

                if (readyZombies != null)
                {
                    for (int i = 0; i < readyZombies.Count; i++)
                    {
                        pendingAlertDelays.Remove(readyZombies[i]);
                    }
                }
            }

            // 3. Update Follower Formation in Patrol / Roam
            if (!isAlerted && packLeader.CurrentState == ZombieState.Patrol)
            {
                UpdateFollowerFormation();
            }
        }

        private void UpdateFollowerFormation()
        {
            Vector3 leaderPos = packLeader.transform.position;
            Quaternion leaderRot = packLeader.transform.rotation;
            float leaderSpeed = packLeader.NavAgent != null ? packLeader.NavAgent.speed : 1.8f;

            int followerIndex = 0;
            for (int i = 0; i < packMembers.Count; i++)
            {
                ZombieAI member = packMembers[i];
                if (member == null || member == packLeader || member.IsDead) continue;

                Vector3 rawOffset = FormationOffsets[followerIndex % FormationOffsets.Length] * (formationRadius / 2.2f);
                Vector3 desiredPos = leaderPos + (leaderRot * rawOffset);

                NavMeshAgent ag = member.NavAgent;
                if (ag != null && ag.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                    {
                        ag.SetDestination(hit.position);
                    }
                    else
                    {
                        ag.SetDestination(desiredPos);
                    }

                    // Dynamically match speed with slight organic variance
                    float variance = 1f + Random.Range(-speedSyncJitter, speedSyncJitter);
                    ag.speed = leaderSpeed * variance;
                }

                followerIndex++;
            }
        }

        private void SyncFollowerSpeeds()
        {
            if (packLeader == null) return;
            float leaderSpeed = packLeader.NavAgent != null ? packLeader.NavAgent.speed : 1.8f;

            for (int i = 0; i < packMembers.Count; i++)
            {
                ZombieAI m = packMembers[i];
                if (m != null && m != packLeader && m.NavAgent != null)
                {
                    m.NavAgent.speed = leaderSpeed * (1f + Random.Range(-speedSyncJitter, speedSyncJitter));
                }
            }
        }

        /// <summary>
        /// When any pack member spots or hears the player, triggers internal alert
        /// spreading to all packmates with 0.25s - 0.55s staggered delays.
        /// </summary>
        public void AlertPack(Transform target)
        {
            if (target == null) return;
            currentPackTarget = target;
            isAlerted = true;

            for (int i = 0; i < packMembers.Count; i++)
            {
                ZombieAI member = packMembers[i];
                if (member == null || member.IsDead) continue;

                // If not already chasing, queue staggered reaction delay
                if (member.CurrentState != ZombieState.Chase && member.CurrentState != ZombieState.Attack)
                {
                    if (!pendingAlertDelays.ContainsKey(member))
                    {
                        pendingAlertDelays[member] = Random.Range(0.25f, 0.55f) * alertDelayMultiplier;
                    }
                }
            }
        }

        /// <summary>
        /// Breaks pursuit and calms pack back into patrol if line of sight is broken.
        /// </summary>
        public void BreakPursuit()
        {
            isAlerted = false;
            currentPackTarget = null;
            pendingAlertDelays.Clear();

            for (int i = 0; i < packMembers.Count; i++)
            {
                ZombieAI member = packMembers[i];
                if (member != null && !member.IsDead)
                {
                    member.InitializeArchetypeState();
                }
            }
        }
    }
}
