using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;
using ProjectSurvival.Player;

namespace ProjectSurvival.Events
{
    [Serializable]
    public class MigrationCorridor
    {
        public string corridorId;
        public string displayName;
        public List<Vector3> waypoints = new List<Vector3>();

        public MigrationCorridor(string id, string name, List<Vector3> pts)
        {
            corridorId = id;
            displayName = name;
            waypoints = pts;
        }
    }

    /// <summary>
    /// Phase 2 (Step 2C): Night Horde Migration Director.
    /// Orchestrates cross-sector nocturnal horde movements across 3 curated outer corridors
    /// between 22:00 and 04:00 (DayNightManager).
    /// Enforces stealth evasion (crouched in shadows visibility < 0.35 allows pack to pass),
    /// flashlight/gunfire alert triggers, and strict 45m starter safehouse perimeter protection.
    /// </summary>
    public class NightHordeMigrationDirector : MonoBehaviour
    {
        public static NightHordeMigrationDirector Instance { get; private set; }

        [Header("Nocturnal Schedule (22:00 - 04:00)")]
        [SerializeField] private float nightStartHour = 22.0f;
        [SerializeField] private float nightEndHour = 4.0f;
        [SerializeField] private float dawnDespawnHour = 4.5f;

        [Header("Pack Kinematics")]
        [SerializeField] private float marchingSpeed = 1.6f;
        [SerializeField] private float packStealthAwarenessRadius = 20.0f;
        [SerializeField] private float stealthBypassThreshold = 0.35f;

        [Header("Starter Protection")]
        [SerializeField] private float minSafeDistanceFromCenter = 45.0f;

        [Header("Curated Migration Corridors (3 Outer Routes)")]
        [SerializeField] private List<MigrationCorridor> corridors = new List<MigrationCorridor>();

        private readonly List<ZombieAI> activePack = new List<ZombieAI>();
        private GameObject packLeaderObj;
        private AudioSource hordeAudioSource;
        private int currentCorridorIndex = 0;
        private int currentWaypointIndex = 0;
        private bool isFormationBroken = false;
        private bool hasMigratedTonight = false;
        private float lastCheckedHour = 12f;

        // Staggered formation offsets relative to pack lead position
        private readonly Vector3[] formationOffsets = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),          // Leader / Screamer
            new Vector3(-1.4f, 0f, -1.8f),    // Walker 1
            new Vector3(1.4f, 0f, -1.8f),     // Walker 2
            new Vector3(-2.4f, 0f, -3.6f),    // Runner 1
            new Vector3(0f, 0f, -3.8f),       // Walker 3
            new Vector3(2.4f, 0f, -3.6f),     // Runner 2
            new Vector3(-1.2f, 0f, -5.4f),    // Walker 4
            new Vector3(1.2f, 0f, -5.4f)      // Walker 5
        };

        public bool IsMigrationActive => activePack.Count > 0;
        public int ActivePackCount => activePack.Count;
        public IReadOnlyList<MigrationCorridor> Corridors => corridors;
        public bool IsFormationBroken => isFormationBroken;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultCorridorsIfEmpty();
        }

        private void Start()
        {
            GameEvents.OnNoiseEmitted += HandleNoiseEmitted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvents.OnNoiseEmitted -= HandleNoiseEmitted;
            CancelActiveMigration();
        }

        public void InitializeDefaultCorridorsIfEmpty()
        {
            if (corridors != null && corridors.Count >= 3) return;

            corridors = new List<MigrationCorridor>
            {
                // Route 1: North Highway Overpass -> East Rail Yard
                new MigrationCorridor(
                    "Corridor_North_To_East",
                    "Kuzey Otoyol -> Doğu Demiryolu",
                    new List<Vector3>
                    {
                        new Vector3(35f, 0f, 230f),
                        new Vector3(120f, 0f, 190f),
                        new Vector3(190f, 0f, 120f),
                        new Vector3(210f, 0f, 40f)
                    }
                ),

                // Route 2: West Urban Forest -> Southwest Flooded Suburb
                new MigrationCorridor(
                    "Corridor_West_To_Southwest",
                    "Batı Koruluğu -> Güneybatı Banliyö",
                    new List<Vector3>
                    {
                        new Vector3(-180f, 0f, 60f),
                        new Vector3(-190f, 0f, -50f),
                        new Vector3(-160f, 0f, -140f),
                        new Vector3(-130f, 0f, -200f)
                    }
                ),

                // Route 3: Northeast Harbor -> Southeast Substation
                new MigrationCorridor(
                    "Corridor_Northeast_To_Southeast",
                    "Kuzeydoğu Liman -> Güneydoğu Trafo",
                    new List<Vector3>
                    {
                        new Vector3(220f, 0f, 225f),
                        new Vector3(240f, 0f, 90f),
                        new Vector3(235f, 0f, -70f),
                        new Vector3(215f, 0f, -215f)
                    }
                )
            };

            ValidateCorridorSafety();
        }

        private void ValidateCorridorSafety()
        {
            for (int c = 0; c < corridors.Count; c++)
            {
                var corridor = corridors[c];
                for (int w = 0; w < corridor.waypoints.Count; w++)
                {
                    float d = Vector3.Distance(new Vector3(corridor.waypoints[w].x, 0, corridor.waypoints[w].z), Vector3.zero);
                    if (d < minSafeDistanceFromCenter)
                    {
                        Debug.LogWarning($"[NightHordeMigrationDirector] Waypoint in {corridor.corridorId} was {d:F1}m from center. Pushing to safe radius.");
                        corridor.waypoints[w] = corridor.waypoints[w].normalized * (minSafeDistanceFromCenter + 15f);
                    }
                }
            }
        }

        private void Update()
        {
            CheckNightSchedule();

            if (IsMigrationActive)
            {
                UpdatePackMovement();
                CheckStealthEvasion();
            }
        }

        private void CheckNightSchedule()
        {
            if (DayNightManager.Instance == null) return;

            float currentHour = DayNightManager.Instance.CurrentHour;

            // Reset nightly trigger flag when afternoon arrives
            if (currentHour >= 12f && currentHour < 18f)
            {
                hasMigratedTonight = false;
            }

            // Check dawn cleanup (after 04:30)
            if (currentHour >= dawnDespawnHour && currentHour < nightStartHour && IsMigrationActive)
            {
                Debug.Log("[NightHordeMigrationDirector] Dawn arrived: despawning nocturnal migration pack.");
                CancelActiveMigration();
                return;
            }

            // Check prologue completion: suppress until prologue completed
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                return;
            }

            // Check nocturnal window: 22:00 to 04:00
            bool isNightWindow = (currentHour >= nightStartHour || currentHour < nightEndHour);
            if (isNightWindow && !hasMigratedTonight && !IsMigrationActive)
            {
                hasMigratedTonight = true;
                TriggerNightMigration();
            }

            lastCheckedHour = currentHour;
        }

        /// <summary>
        /// Triggers a night horde migration along a specific corridor index, or cyclical next corridor.
        /// </summary>
        public void TriggerNightMigration(int corridorIdx = -1)
        {
            if (corridors == null || corridors.Count == 0)
            {
                InitializeDefaultCorridorsIfEmpty();
            }

            CancelActiveMigration();

            if (corridorIdx < 0 || corridorIdx >= corridors.Count)
            {
                currentCorridorIndex = (currentCorridorIndex + 1) % corridors.Count;
            }
            else
            {
                currentCorridorIndex = corridorIdx;
            }

            var corridor = corridors[currentCorridorIndex];
            if (corridor.waypoints.Count == 0) return;

            currentWaypointIndex = 0;
            isFormationBroken = false;

            Vector3 startPos = corridor.waypoints[0];
            if (NavMesh.SamplePosition(startPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                startPos = hit.position;
            }

            // Create Leader object that guides the pack
            packLeaderObj = new GameObject($"MigrationPackLead_{corridor.corridorId}");
            packLeaderObj.transform.position = startPos;
            var leadAgent = packLeaderObj.AddComponent<NavMeshAgent>();
            leadAgent.speed = marchingSpeed;
            leadAgent.stoppingDistance = 2.0f;
            leadAgent.acceleration = 6.0f;

            // Horde Ambient Groan Audio
            hordeAudioSource = packLeaderObj.AddComponent<AudioSource>();
            hordeAudioSource.spatialBlend = 1.0f;
            hordeAudioSource.minDistance = 4.0f;
            hordeAudioSource.maxDistance = 42.0f;
            hordeAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            hordeAudioSource.loop = true;
            hordeAudioSource.playOnAwake = false;

            if (ProceduralAudioManager.Instance != null)
            {
                hordeAudioSource.clip = ProceduralAudioManager.Instance.GenerateHordeGroanClip();
                hordeAudioSource.volume = 0.85f;
                hordeAudioSource.Play();
            }

            leadAgent.SetDestination(corridor.waypoints[currentWaypointIndex]);

            // Spawn 8 Formation Zombies (1 Screamer, 2 Runners, 5 Walkers)
            SpawnMigrationPack(startPos);

            Debug.Log($"[NightHordeMigrationDirector] Started Night Migration along '{corridor.displayName}' with 8 zombies.");
        }

        private void SpawnMigrationPack(Vector3 startOrigin)
        {
            activePack.Clear();
            ZombieAI sceneTemplate = FindFirstObjectByType<ZombieAI>();

            for (int i = 0; i < 8; i++)
            {
                Vector3 spawnPos = startOrigin + formationOffsets[i];
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }

                ZombieAI ai = null;
                if (sceneTemplate != null)
                {
                    GameObject zObj = Instantiate(sceneTemplate.gameObject, spawnPos, Quaternion.identity);
                    zObj.name = $"MigrationZombie_{i + 1}";
                    zObj.SetActive(true);
                    ai = zObj.GetComponent<ZombieAI>();
                }
                else
                {
                    ai = CreateFallbackZombie(spawnPos, i);
                }

                if (ai != null)
                {
                    // Configure types: 0 = Screamer, 3 & 5 = Runners, others = Standard Walkers
                    if (i == 0)
                    {
                        ai.SetZombieType(ZombieType.Screamer);
                        ai.ConfigureStats(45f, 8f, 3.8f);
                    }
                    else if (i == 3 || i == 5)
                    {
                        ai.SetZombieType(ZombieType.Runner);
                        ai.ConfigureStats(50f, 10f, 5.0f);
                    }
                    else
                    {
                        ai.SetZombieType(ZombieType.Standard);
                        ai.ConfigureStats(60f, 12f, 4.0f);
                    }

                    NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.speed = marchingSpeed;
                        agent.stoppingDistance = 1.0f;
                    }

                    activePack.Add(ai);
                }
            }
        }

        private ZombieAI CreateFallbackZombie(Vector3 pos, int index)
        {
            GameObject zObj = new GameObject($"MigrationZombie_{index + 1}");
            zObj.transform.position = pos;

            CapsuleCollider cc = zObj.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            NavMeshAgent agent = zObj.AddComponent<NavMeshAgent>();
            agent.speed = marchingSpeed;
            agent.radius = 0.35f;
            agent.height = 1.8f;

            ZombieAI ai = zObj.AddComponent<ZombieAI>();
            return ai;
        }

        private void UpdatePackMovement()
        {
            // Clean dead or destroyed zombies
            activePack.RemoveAll(z => z == null || z.IsDead || !z.gameObject.activeInHierarchy);
            if (activePack.Count == 0)
            {
                CancelActiveMigration();
                return;
            }

            if (isFormationBroken || packLeaderObj == null) return;

            var corridor = corridors[currentCorridorIndex];
            NavMeshAgent leadAgent = packLeaderObj.GetComponent<NavMeshAgent>();
            if (leadAgent == null || !leadAgent.isOnNavMesh) return;

            Vector3 currentTargetWp = corridor.waypoints[currentWaypointIndex];
            float distToWp = Vector3.Distance(packLeaderObj.transform.position, currentTargetWp);

            if (distToWp <= 3.8f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= corridor.waypoints.Count)
                {
                    Debug.Log($"[NightHordeMigrationDirector] Pack reached end of corridor '{corridor.displayName}'. Despawning pack.");
                    CancelActiveMigration();
                    return;
                }
                leadAgent.SetDestination(corridor.waypoints[currentWaypointIndex]);
            }

            // Keep followers in formation relative to leader
            Vector3 leadPos = packLeaderObj.transform.position;
            Vector3 forward = leadAgent.velocity.sqrMagnitude > 0.05f ? leadAgent.velocity.normalized : packLeaderObj.transform.forward;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            for (int i = 0; i < activePack.Count; i++)
            {
                var z = activePack[i];
                if (z == null || z.IsDead) continue;

                NavMeshAgent zAgent = z.GetComponent<NavMeshAgent>();
                if (zAgent == null || !zAgent.isOnNavMesh) continue;

                Vector3 targetSlot = leadPos + rot * formationOffsets[i % formationOffsets.Length];
                zAgent.speed = marchingSpeed;
                zAgent.SetDestination(targetSlot);
            }
        }

        private void CheckStealthEvasion()
        {
            if (isFormationBroken || packLeaderObj == null) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            float distToLeader = Vector3.Distance(packLeaderObj.transform.position, playerPos);

            if (distToLeader <= packStealthAwarenessRadius)
            {
                float visibility = StealthVisibilityController.Instance != null
                    ? StealthVisibilityController.Instance.CurrentVisibilityScore
                    : 0.8f;

                bool flashlightSweeping = StealthVisibilityController.Instance != null &&
                    StealthVisibilityController.Instance.IsFlashlightSweepingZombie;

                // If player is crouching in darkness with flashlight off (visibility < 0.35f), pack passes by!
                if (visibility < stealthBypassThreshold && !flashlightSweeping)
                {
                    return;
                }

                // Otherwise, sweep or higher visibility triggers break formation
                if (flashlightSweeping || visibility >= 0.70f)
                {
                    BreakFormationAndEngage(player.transform);
                }
            }
        }

        private void HandleNoiseEmitted(Vector3 noisePos, float radius)
        {
            if (!IsMigrationActive || isFormationBroken || packLeaderObj == null) return;

            float dist = Vector3.Distance(packLeaderObj.transform.position, noisePos);
            if (dist <= radius)
            {
                GameObject player = GameObject.FindWithTag("Player");
                BreakFormationAndEngage(player != null ? player.transform : null);
            }
        }

        public void BreakFormationAndEngage(Transform target)
        {
            if (isFormationBroken) return;
            isFormationBroken = true;

            ProceduralAudioManager.Instance?.PlayScreamerNoise();
            GameEvents.TriggerObjectiveToastRequested("DİKKAT: Göç eden zombi sürüsü seni fark etti!", 3.8f);

            if (hordeAudioSource != null && hordeAudioSource.isPlaying)
            {
                hordeAudioSource.Stop();
            }

            for (int i = 0; i < activePack.Count; i++)
            {
                var z = activePack[i];
                if (z == null || z.IsDead) continue;

                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = 4.6f;
                    if (target != null)
                    {
                        agent.SetDestination(target.position);
                    }
                }

                if (target != null)
                {
                    z.TriggerInvestigate(target.position);
                }
            }

            Debug.Log("[NightHordeMigrationDirector] Migration formation broken! Pack pursuing player.");
        }

        public void CancelActiveMigration()
        {
            for (int i = 0; i < activePack.Count; i++)
            {
                if (activePack[i] != null)
                {
                    Destroy(activePack[i].gameObject);
                }
            }
            activePack.Clear();

            if (packLeaderObj != null)
            {
                Destroy(packLeaderObj);
                packLeaderObj = null;
            }

            isFormationBroken = false;
        }
    }
}
