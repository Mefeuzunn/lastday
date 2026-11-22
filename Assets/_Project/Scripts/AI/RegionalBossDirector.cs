using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Phase 3: Regional Apex Mutant Boss & Vault Cache Director.
    /// Manages the procedural placement, sector streaming hibernation,
    /// and lifecycle for the 3 signature regional bosses and their master-locked caches:
    /// 1. Juggernaut (Zırhlı Dev) in Sector_0_1_North (Elevated Highway Checkpoint)
    /// 2. Banshee (Çığlık Kraliçesi) in Sector_0_-1_South (Grand Shopping Mall Atrium)
    /// 3. Bio-Ravager (Biyolojik Mutasyon Lideri) in Sector_1_1_NorthEast (Harbor Container Plaza)
    /// </summary>
    public class RegionalBossDirector : MonoBehaviour
    {
        private static RegionalBossDirector _instance;
        public static RegionalBossDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RegionalBossDirector>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RegionalBossDirector");
                        _instance = go.AddComponent<RegionalBossDirector>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Serializable]
        public class BossArenaNode
        {
            public string bossId;
            public string displayName;
            public string subtitle;
            public MutantBossType bossType;
            public Vector3 arenaCenter;
            public Vector3 cachePosition;
            public string cacheId;
            public string blueprintId;
            public string blueprintName;
            public float maxHealth;
            public float maxArmor;

            [NonSerialized] public GameObject bossInstance;
            [NonSerialized] public GameObject cacheInstance;
            [NonSerialized] public bool isSpawned = false;
        }

        [Header("Arena Configurations")]
        [SerializeField] private List<BossArenaNode> arenaNodes = new List<BossArenaNode>
        {
            // 1. North Highway Elevated Checkpoint (Sector_0_1_North)
            new BossArenaNode
            {
                bossId = "boss_juggernaut",
                displayName = "ZIRHLI DEV",
                subtitle = "BÖLGE LİDERİ (KUZEY OTOYOLU)",
                bossType = MutantBossType.Juggernaut,
                arenaCenter = new Vector3(0f, 1.2f, 240f),
                cachePosition = new Vector3(0f, 1.2f, 252f),
                cacheId = "apex_cache_north_highway",
                blueprintId = SurvivorProgressionManager.BLUEPRINT_AP_ROUNDS,
                blueprintName = "Delici Mermi Şeması (AP Rounds)",
                maxHealth = 900f,
                maxArmor = 250f
            },

            // 2. Grand Mall Central Atrium (Sector_0_-1_South)
            new BossArenaNode
            {
                bossId = "boss_banshee",
                displayName = "ÇIĞLIK KRALİÇESİ",
                subtitle = "BÖLGE LİDERİ (AVM ATRİUMU)",
                bossType = MutantBossType.Banshee,
                arenaCenter = new Vector3(0f, 0.2f, -295f),
                cachePosition = new Vector3(0f, 0.2f, -305f),
                cacheId = "apex_cache_south_mall",
                blueprintId = SurvivorProgressionManager.BLUEPRINT_COMPENSATOR,
                blueprintName = "Gelişmiş Namlu Kompansatörü",
                maxHealth = 680f,
                maxArmor = 0f
            },

            // 3. Harbor Container Plaza (Sector_1_1_NorthEast)
            new BossArenaNode
            {
                bossId = "boss_bioravager",
                displayName = "BİYOLOJİK MUTASYON LİDERİ",
                subtitle = "BÖLGE LİDERİ (LİMAN BÖLGESİ)",
                bossType = MutantBossType.BioRavager,
                arenaCenter = new Vector3(240f, 1.0f, 240f),
                cachePosition = new Vector3(250f, 1.0f, 250f),
                cacheId = "apex_cache_ne_harbor",
                blueprintId = SurvivorProgressionManager.BLUEPRINT_COMPOSITE_ARMOR,
                blueprintName = "Güçlendirilmiş Kompozit Zırh Şeması",
                maxHealth = 800f,
                maxArmor = 0f
            }
        };

        [Header("Streaming Thresholds")]
        [SerializeField] private float bossActivationDistance = 120f;
        [SerializeField] private float bossHibernateDistance = 145f;

        public int ArenaNodesCount => arenaNodes.Count;
        public IReadOnlyList<BossArenaNode> ArenaNodes => arenaNodes;

        private Transform playerTransform;
        private float streamingCheckTimer = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("RegionalBossDirector");
                Instance = go.AddComponent<RegionalBossDirector>();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            FindPlayer();
            SpawnAllVaultCaches();
            EvaluateStreamingLifecycle();
        }

        private void Update()
        {
            streamingCheckTimer += Time.deltaTime;
            if (streamingCheckTimer >= 0.75f)
            {
                streamingCheckTimer = 0f;
                EvaluateStreamingLifecycle();
            }
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void SpawnAllVaultCaches()
        {
            for (int i = 0; i < arenaNodes.Count; i++)
            {
                BossArenaNode node = arenaNodes[i];
                if (node.cacheInstance != null) continue;

                GameObject cacheObj = new GameObject($"ApexVaultCache_{node.cacheId}", typeof(BoxCollider));
                cacheObj.transform.position = node.cachePosition;
                cacheObj.transform.rotation = Quaternion.identity;

                ApexBossCacheInteractable cache = cacheObj.AddComponent<ApexBossCacheInteractable>();
                if (cache != null)
                {
                    cache.Configure(node.cacheId, node.bossId, node.blueprintId, node.blueprintName);
                }

                node.cacheInstance = cacheObj;
            }
        }

        private void EvaluateStreamingLifecycle()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;
            SurvivorProgressionManager pm = SurvivorProgressionManager.Instance;

            for (int i = 0; i < arenaNodes.Count; i++)
            {
                BossArenaNode node = arenaNodes[i];

                // If boss was defeated in progression/save, ensure destroyed and skip
                if (pm != null && pm.IsBossDefeated(node.bossId))
                {
                    if (node.bossInstance != null)
                    {
                        Destroy(node.bossInstance);
                        node.bossInstance = null;
                        node.isSpawned = false;
                    }
                    continue;
                }

                float dist = Vector3.Distance(playerPos, node.arenaCenter);

                if (dist <= bossActivationDistance)
                {
                    // Activate or Spawn Boss
                    if (!node.isSpawned || node.bossInstance == null)
                    {
                        SpawnBossInstance(node);
                    }
                    else if (!node.bossInstance.activeSelf)
                    {
                        node.bossInstance.SetActive(true);
                    }
                }
                else if (dist > bossHibernateDistance)
                {
                    // Hibernate Boss to maintain 60 FPS
                    if (node.bossInstance != null && node.bossInstance.activeSelf)
                    {
                        node.bossInstance.SetActive(false);
                    }
                }
            }
        }

        private void SpawnBossInstance(BossArenaNode node)
        {
            // Sample NavMesh position
            Vector3 spawnPos = node.arenaCenter;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 6.0f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject bossObj = new GameObject($"ApexBoss_{node.bossId}");
            bossObj.transform.position = spawnPos;

            // Character Collider
            CapsuleCollider cc = bossObj.AddComponent<CapsuleCollider>();
            cc.height = node.bossType == MutantBossType.Juggernaut ? 2.65f : (node.bossType == MutantBossType.BioRavager ? 2.40f : 2.15f);
            cc.radius = node.bossType == MutantBossType.Juggernaut ? 0.75f : 0.55f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            // NavMeshAgent
            NavMeshAgent agent = bossObj.AddComponent<NavMeshAgent>();
            agent.speed = node.bossType == MutantBossType.Banshee ? 6.8f : (node.bossType == MutantBossType.Juggernaut ? 4.2f : 4.6f);
            agent.stoppingDistance = 2.4f;

            // MutantBossAI
            MutantBossAI bossAI = bossObj.AddComponent<MutantBossAI>();
            bossAI.Configure(node.bossId, node.displayName, node.subtitle, node.bossType, node.maxHealth, node.maxArmor);

            node.bossInstance = bossObj;
            node.isSpawned = true;

            Debug.Log($"[RegionalBossDirector] Spawned Apex Boss '{node.displayName}' at {spawnPos}");
        }
    }
}
