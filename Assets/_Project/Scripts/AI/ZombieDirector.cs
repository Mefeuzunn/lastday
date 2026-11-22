using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Manages the city-wide zombie population, maintaining archetype balance (12 Lone Roamers, 10 Scavengers, 6 Dormant Lurkers),
    /// enforcing NavMesh snapping, and handling object pool recycling / state resets.
    /// </summary>
    public class ZombieDirector : MonoBehaviour
    {
        public static ZombieDirector Instance { get; private set; }

        [Header("Population Targets (28 Total)")]
        [SerializeField] private int targetLoneRoamers = 12;
        [SerializeField] private int targetScavengers = 10;
        [SerializeField] private int targetDormantLurkers = 6;

        [Header("Spawn Prefabs")]
        [SerializeField] private GameObject standardZombiePrefab;
        [SerializeField] private GameObject runnerZombiePrefab;
        [SerializeField] private GameObject screamerZombiePrefab;
        [SerializeField] private GameObject tankZombiePrefab;

        [Header("Auto-Replenish Settings")]
        [SerializeField] private bool autoReplenish = false;
        [SerializeField] private float replenishCheckInterval = 30f;
        [SerializeField] private float minSpawnDistanceFromPlayer = 35f;

        private Transform playerTransform;
        private float replenishTimer = 0f;

        public int TargetTotalZombies => targetLoneRoamers + targetScavengers + targetDormantLurkers;

        public int ActiveLoneRoamersCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ZombieAI.ActiveZombies.Count; i++)
                {
                    var z = ZombieAI.ActiveZombies[i];
                    if (z != null && !z.IsDead && z.Archetype == ZombieArchetype.LoneRoamer) count++;
                }
                return count;
            }
        }

        public int ActiveScavengersCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ZombieAI.ActiveZombies.Count; i++)
                {
                    var z = ZombieAI.ActiveZombies[i];
                    if (z != null && !z.IsDead && z.Archetype == ZombieArchetype.Scavenger) count++;
                }
                return count;
            }
        }

        public int ActiveDormantLurkersCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ZombieAI.ActiveZombies.Count; i++)
                {
                    var z = ZombieAI.ActiveZombies[i];
                    if (z != null && !z.IsDead && z.Archetype == ZombieArchetype.DormantLurker) count++;
                }
                return count;
            }
        }

        public int TotalActiveZombies
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ZombieAI.ActiveZombies.Count; i++)
                {
                    var z = ZombieAI.ActiveZombies[i];
                    if (z != null && !z.IsDead) count++;
                }
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (GetComponent<FlankingCoordinator>() == null) gameObject.AddComponent<FlankingCoordinator>();
            if (GetComponent<ZombiePackManager>() == null) gameObject.AddComponent<ZombiePackManager>();
            if (GetComponent<HordeAlertSystem>() == null) gameObject.AddComponent<HordeAlertSystem>();
            if (GetComponent<DistrictZombieEcology>() == null) gameObject.AddComponent<DistrictZombieEcology>();
            if (GetComponent<NocturnalZombieEscalator>() == null) gameObject.AddComponent<NocturnalZombieEscalator>();
        }

        private void Start()
        {
            FindPlayer();
            SnapAllZombiesToNavMesh();

            ZombiePack[] packs = GetComponentsInChildren<ZombiePack>(true);
            for (int i = 0; i < packs.Length; i++)
            {
                ZombiePackManager.Instance?.RegisterPack(packs[i]);
            }
        }

        private void Update()
        {
            if (!autoReplenish) return;

            replenishTimer += Time.deltaTime;
            if (replenishTimer >= replenishCheckInterval)
            {
                replenishTimer = 0f;
                CheckAndReplenishZombies();
            }
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        /// <summary>
        /// Snaps every active and inactive zombie instance in the scene to the closest valid NavMesh surface.
        /// Prevents zombies from floating above roads or sinking below floor meshes.
        /// </summary>
        public void SnapAllZombiesToNavMesh()
        {
            ZombieAI[] sceneZombies = FindObjectsByType<ZombieAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int snappedCount = 0;

            for (int i = 0; i < sceneZombies.Length; i++)
            {
                ZombieAI z = sceneZombies[i];
                if (z == null) continue;

                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent == null) continue;

                if (NavMesh.SamplePosition(z.transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.Warp(hit.position);
                    }
                    else
                    {
                        z.transform.position = hit.position;
                    }
                    snappedCount++;
                }
            }

            Debug.Log($"[ZombieDirector] Snapped {snappedCount}/{sceneZombies.Length} zombies cleanly to NavMesh.");
        }

        /// <summary>
        /// Completely resets a zombie's state when spawning from pool or moving to a new patrol/scavenge post.
        /// </summary>
        public void ResetZombieState(ZombieAI zombie, ZombieArchetype archetype, Vector3 spawnPos, Quaternion spawnRot)
        {
            if (zombie == null) return;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 12f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            zombie.transform.position = spawnPos;
            zombie.transform.rotation = spawnRot;

            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh)
                {
                    agent.Warp(spawnPos);
                    agent.isStopped = false;
                }
            }

            zombie.SetArchetype(archetype);
            zombie.OnObjectSpawn();
        }

        private void CheckAndReplenishZombies()
        {
            if (playerTransform == null) FindPlayer();

            // Check deficit for each archetype
            int deficitRoamers = targetLoneRoamers - ActiveLoneRoamersCount;
            int deficitScavengers = targetScavengers - ActiveScavengersCount;
            int deficitLurkers = targetDormantLurkers - ActiveDormantLurkersCount;

            // Replenish roamers if pooler available
            if (deficitRoamers > 0 && ObjectPooler.Instance != null && standardZombiePrefab != null)
            {
                SpawnReplenishmentZombie(ZombieArchetype.LoneRoamer);
            }
        }

        private void SpawnReplenishmentZombie(ZombieArchetype archetype)
        {
            if (playerTransform == null) return;

            float safeMin = SpawnProtectionController.Instance != null
                ? Mathf.Max(minSpawnDistanceFromPlayer, SpawnProtectionController.Instance.SpawnSafeRadius)
                : minSpawnDistanceFromPlayer;

            // Pick point outside safe radius (42m - 65m away from player) on NavMesh
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(safeMin, safeMin + 20f);
            Vector3 candidatePos = playerTransform.position + new Vector3(circle.x, 0f, circle.y);

            // Enforce Safe House Buffer Zone constraint
            if (DistrictZombieEcology.Instance != null && !DistrictZombieEcology.Instance.CanSpawnZombieAt(candidatePos))
            {
                return;
            }

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 16f, NavMesh.AllAreas))
            {
                GameObject pooled = ObjectPooler.Instance.SpawnFromPool("Zombie_Standard", hit.position, Quaternion.identity);
                if (pooled != null)
                {
                    ZombieAI ai = pooled.GetComponent<ZombieAI>();
                    if (ai != null)
                    {
                        ResetZombieState(ai, archetype, hit.position, Quaternion.identity);
                    }
                }
            }
        }
    }
}
