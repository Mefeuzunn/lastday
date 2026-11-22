using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.UI;

namespace ProjectSurvival.Environment
{
    public enum SectorStreamingTier
    {
        Tier1_Immediate, // Full active: terrain, macro geometry, interior props, lights, particles
        Tier2_MidRange,  // Mid-range: terrain & macro silhouettes active; interior details/small lights culled
        Tier3_Culled     // Far: container completely deactivated
    }

    [Serializable]
    public class SectorBinding
    {
        public Vector2Int gridCoord;
        public GameObject sectorRoot;
        public GameObject detailsChild;
        public SectorStreamingTier currentTier;

        public SectorBinding(Vector2Int coord, GameObject root, GameObject details = null)
        {
            gridCoord = coord;
            sectorRoot = root;
            detailsChild = details;
            currentTier = SectorStreamingTier.Tier1_Immediate;
        }
    }

    /// <summary>
    /// Mobile 60 FPS Sector Streaming & AI Spatial Hibernation Controller.
    /// Evaluates player distance across the 720x720m 3x3 sector grid at 0.35s intervals,
    /// culling distant geometry and hibernating off-screen AI to maintain strict draw-call and CPU budgets.
    /// </summary>
    public class WorldSectorStreamer : MonoBehaviour
    {
        public static WorldSectorStreamer Instance { get; private set; }

        [Header("Distance Streaming Thresholds")]
        [Tooltip("Full active radius around player (Tier 1)")]
        [SerializeField] private float tier1Radius = 210f;
        [Tooltip("Mid-range horizon distance before sector is completely deactivated (Tier 2/3 cutoff)")]
        [SerializeField] private float tier2Radius = 360f;
        [SerializeField] private float streamingCheckInterval = 0.35f;

        [Header("AI Hibernation Thresholds")]
        [SerializeField] private float aiHibernationRadius = 155f;
        [SerializeField] private float aiWakeRadius = 145f;
        [SerializeField] private bool enableAiHibernation = true;

        [Header("Sector Root Hierarchy (3x3 Grid)")]
        [SerializeField] private List<SectorBinding> sectorBindings = new List<SectorBinding>();

        private Transform playerTransform;
        private Vector3 drivingLookAhead = Vector3.zero;
        private float nextStreamingCheckTime = 0f;
        private Vector2Int currentSectorCoord = new Vector2Int(99, 99);
        private readonly HashSet<ZombieAI> hibernatedZombies = new HashSet<ZombieAI>();
        private readonly HashSet<SurvivorNPC_AI> hibernatedSurvivors = new HashSet<SurvivorNPC_AI>();
        private readonly HashSet<HumanRaiderAI> hibernatedRaiders = new HashSet<HumanRaiderAI>();

        public Vector2Int CurrentSectorCoord => currentSectorCoord;
        public IReadOnlyList<SectorBinding> SectorBindings => sectorBindings;

        public void SetDrivingLookAhead(Vector3 lookAhead)
        {
            drivingLookAhead = lookAhead;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoDiscoverSectorRootsIfMissing();
        }

        private void Start()
        {
            FindPlayer();
            EvaluateStreamingSlices(true);
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        private void Update()
        {
            if (Time.time >= nextStreamingCheckTime)
            {
                nextStreamingCheckTime = Time.time + streamingCheckInterval;
                EvaluateStreamingSlices(false);
                if (enableAiHibernation)
                {
                    EvaluateAiHibernation();
                }
            }
        }

        public void AutoDiscoverSectorRootsIfMissing()
        {
            if (sectorBindings != null && sectorBindings.Count == 9) return;

            sectorBindings = new List<SectorBinding>();
            GameObject worldRoot = GameObject.Find("--- WORLD SECTORS ---") ?? GameObject.Find("Environment");

            var allSectors = WorldMapConfig.GetAllSectors();
            for (int i = 0; i < allSectors.Count; i++)
            {
                var s = allSectors[i];
                GameObject root = GameObject.Find(s.sectorId);
                if (root == null && worldRoot != null)
                {
                    Transform t = worldRoot.transform.Find(s.sectorId);
                    if (t != null) root = t.gameObject;
                }

                GameObject details = null;
                if (root != null)
                {
                    Transform dt = root.transform.Find("Details") ?? root.transform.Find("Sector_Details");
                    if (dt != null) details = dt.gameObject;
                }

                sectorBindings.Add(new SectorBinding(s.gridCoord, root, details));
            }
        }

        public void EvaluateStreamingSlices(bool force)
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 pPos = playerTransform.position + drivingLookAhead;
            Vector2Int newCoord = WorldMapConfig.GetSectorCoordAtPosition(pPos);

            // Sector Crossing Feedback
            if (newCoord != currentSectorCoord)
            {
                currentSectorCoord = newCoord;
                SectorData currentSector = WorldMapConfig.GetSectorData(currentSectorCoord);

                GameEvents.TriggerObjectiveToastRequested($"[BÖLGE: {currentSector.turkishDisplayName.ToUpper()}]", 3.2f);
                CompassBarUI.Instance?.SetActiveDistrict(currentSector.turkishDisplayName);

                Debug.Log($"[WorldSectorStreamer] Player entered sector {currentSectorCoord}: {currentSector.turkishDisplayName}");
            }

            // Evaluate all 9 sectors
            for (int i = 0; i < sectorBindings.Count; i++)
            {
                SectorBinding b = sectorBindings[i];
                if (b.sectorRoot == null) continue;

                SectorData sData = WorldMapConfig.GetSectorData(b.gridCoord);
                float distToCenter = Vector3.Distance(new Vector3(pPos.x, 0f, pPos.z), new Vector3(sData.worldCenter.x, 0f, sData.worldCenter.z));

                SectorStreamingTier targetTier;
                if (distToCenter <= tier1Radius || b.gridCoord == currentSectorCoord)
                {
                    targetTier = SectorStreamingTier.Tier1_Immediate;
                }
                else if (distToCenter <= tier2Radius)
                {
                    targetTier = SectorStreamingTier.Tier2_MidRange;
                }
                else
                {
                    targetTier = SectorStreamingTier.Tier3_Culled;
                }

                if (force || targetTier != b.currentTier)
                {
                    ApplyStreamingTier(b, targetTier);
                }
            }
        }

        private void ApplyStreamingTier(SectorBinding binding, SectorStreamingTier tier)
        {
            binding.currentTier = tier;

            switch (tier)
            {
                case SectorStreamingTier.Tier1_Immediate:
                    binding.sectorRoot.SetActive(true);
                    if (binding.detailsChild != null) binding.detailsChild.SetActive(true);
                    break;

                case SectorStreamingTier.Tier2_MidRange:
                    binding.sectorRoot.SetActive(true);
                    if (binding.detailsChild != null) binding.detailsChild.SetActive(false); // Cull small interior props
                    break;

                case SectorStreamingTier.Tier3_Culled:
                    binding.sectorRoot.SetActive(false); // Entire far sector culled
                    break;
            }
        }

        public void PrewarmSectorAtPosition(Vector3 worldPos)
        {
            Vector2Int coord = WorldMapConfig.GetSectorCoordAtPosition(worldPos);
            for (int i = 0; i < sectorBindings.Count; i++)
            {
                SectorBinding b = sectorBindings[i];
                if (b != null && b.sectorRoot != null && b.gridCoord == coord)
                {
                    b.sectorRoot.SetActive(true);
                    if (b.detailsChild != null) b.detailsChild.SetActive(true);
                    b.currentTier = SectorStreamingTier.Tier1_Immediate;
                }
            }
        }

        private void EvaluateAiHibernation()
        {
            if (playerTransform == null) return;
            Vector3 pPos = playerTransform.position;

            // 1. Zombie Hibernation
            var zombies = ZombieAI.ActiveZombies;
            if (zombies != null)
            {
                for (int i = 0; i < zombies.Count; i++)
                {
                    ZombieAI z = zombies[i];
                    if (z == null || z.IsDead) continue;

                    float dist = Vector3.Distance(pPos, z.transform.position);

                    if (dist > aiHibernationRadius && !hibernatedZombies.Contains(z))
                    {
                        HibernateZombie(z);
                    }
                    else if (dist <= aiWakeRadius && hibernatedZombies.Contains(z))
                    {
                        WakeZombie(z);
                    }
                }
            }

            // 2. NPC Hibernation (Companions NEVER hibernate)
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            if (survivors != null)
            {
                for (int i = 0; i < survivors.Count; i++)
                {
                    SurvivorNPC_AI npc = survivors[i];
                    if (npc == null || npc.IsDead || npc.IsCompanion) continue;

                    float dist = Vector3.Distance(pPos, npc.transform.position);

                    if (dist > aiHibernationRadius && !hibernatedSurvivors.Contains(npc))
                    {
                        HibernateSurvivor(npc);
                    }
                    else if (dist <= aiWakeRadius && hibernatedSurvivors.Contains(npc))
                    {
                        WakeSurvivor(npc);
                    }
                }
            }

            // 3. Armed Raider Hibernation
            var raiders = HumanRaiderAI.ActiveRaiders;
            if (raiders != null)
            {
                for (int i = 0; i < raiders.Count; i++)
                {
                    HumanRaiderAI raider = raiders[i];
                    if (raider == null || raider.IsDead) continue;

                    float dist = Vector3.Distance(pPos, raider.transform.position);

                    if (dist > aiHibernationRadius && !hibernatedRaiders.Contains(raider))
                    {
                        HibernateRaider(raider);
                    }
                    else if (dist <= aiWakeRadius && hibernatedRaiders.Contains(raider))
                    {
                        WakeRaider(raider);
                    }
                }
            }
        }

        private void HibernateZombie(ZombieAI z)
        {
            hibernatedZombies.Add(z);
            NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
            }

            ZombieSensorySystem sensor = z.GetComponent<ZombieSensorySystem>();
            if (sensor != null) sensor.enabled = false;

            ZombieBrain brain = z.GetComponent<ZombieBrain>();
            if (brain != null) brain.enabled = false;
        }

        private void WakeZombie(ZombieAI z)
        {
            hibernatedZombies.Remove(z);

            ZombieSensorySystem sensor = z.GetComponent<ZombieSensorySystem>();
            if (sensor != null) sensor.enabled = true;

            ZombieBrain brain = z.GetComponent<ZombieBrain>();
            if (brain != null) brain.enabled = true;

            NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
            }
        }

        private void HibernateSurvivor(SurvivorNPC_AI npc)
        {
            hibernatedSurvivors.Add(npc);
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
            }
        }

        private void WakeSurvivor(SurvivorNPC_AI npc)
        {
            hibernatedSurvivors.Remove(npc);
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
            }
        }

        private void HibernateRaider(HumanRaiderAI raider)
        {
            hibernatedRaiders.Add(raider);
            NavMeshAgent agent = raider.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            raider.enabled = false;
        }

        private void WakeRaider(HumanRaiderAI raider)
        {
            hibernatedRaiders.Remove(raider);
            raider.enabled = true;
            NavMeshAgent agent = raider.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }
    }
}
