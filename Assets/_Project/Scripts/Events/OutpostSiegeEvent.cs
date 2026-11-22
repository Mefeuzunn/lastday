using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.AI;
using ProjectSurvival.Building;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Managers;
using ProjectSurvival.Narrative;
using ProjectSurvival.OpenWorld;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.UI;

namespace ProjectSurvival.Events
{
    /// <summary>
    /// Phase 2 (Step 2C): Outpost & SafeHouse Defense Sieges.
    /// Manages high-stakes 2-wave perimeter sieges against player fortifications:
    /// - Wave 1 (6 Zombies): 4 Walkers + 2 Runners approaching from 36m perimeter.
    /// - Wave 2 (8 Zombies): 4 Walkers + 2 Runners + 1 Screamer + 1 Armored Tank.
    /// Drives tactical interaction with BuildableStructure defenses (DefenseWall, RazorWire, Floodlight).
    /// Awards +120 XP, +15 Morale, and spawns a Post-Siege Salvage Crate (4x Scrap, 2x Cloth, 12x Ammo).
    /// </summary>
    public class OutpostSiegeEvent : MonoBehaviour
    {
        public static OutpostSiegeEvent Instance { get; private set; }

        [Header("Siege Trigger & Intervals")]
        [SerializeField] private float siegeCooldownSeconds = 900.0f; // 15-minute cooldown
        [SerializeField] private float perimeterSpawnDistance = 36.0f;
        [SerializeField] private float waveBreatherDuration = 4.0f;

        [Header("State")]
        [SerializeField] private bool isSiegeActive = false;
        [SerializeField] private int currentWave = 0;
        [SerializeField] private string activeOutpostName = "Merkez Sığınak";
        [SerializeField] private Vector3 activeOutpostCenter = Vector3.zero;

        private readonly List<ZombieAI> activeEnemies = new List<ZombieAI>();
        private float lastSiegeTime = -9999f;
        private Coroutine siegeCoroutine;

        public bool IsSiegeActive => isSiegeActive;
        public int CurrentWave => currentWave;
        public int RemainingEnemies => activeEnemies.Count;
        public string ActiveOutpostName => activeOutpostName;
        public Vector3 ActiveOutpostCenter => activeOutpostCenter;
        public IReadOnlyList<ZombieAI> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            lastSiegeTime = -siegeCooldownSeconds * 0.7f; // Allow first siege after short initial play
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CancelActiveSiege();
        }

        private void Update()
        {
            if (isSiegeActive)
            {
                UpdateEnemyTrackingAndFortificationAttacks();
                return;
            }

            CheckAutomaticSiegeTrigger();
        }

        private void CheckAutomaticSiegeTrigger()
        {
            if (Time.time < lastSiegeTime + siegeCooldownSeconds) return;

            // Check prologue completion: suppress until prologue completed
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Vector3 playerPos = player.transform.position;

            // Check if player is inside a liberated Outpost
            if (OutpostLiberationManager.Instance != null)
            {
                var outposts = OutpostLiberationManager.Instance.Outposts;
                for (int i = 0; i < outposts.Count; i++)
                {
                    var op = outposts[i];
                    if (op.isLiberated && Vector3.Distance(playerPos, op.worldPosition) <= op.safeZoneRadius)
                    {
                        TriggerSiege(op.worldPosition, op.displayName);
                        return;
                    }
                }
            }

            // Check if player is in Central SafeHouse courtyard (12m - 32m radius from center)
            float distFromCenter = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), Vector3.zero);
            if (distFromCenter >= 10f && distFromCenter <= 32f)
            {
                TriggerSiege(Vector3.zero, "Merkez Güvenli Bölge Avlusu");
            }
        }

        /// <summary>
        /// Manually triggers a defensive training drill (SAVUNMA TATBİKATI / SİRENİ TETİKLE).
        /// </summary>
        public void TriggerDefenseDrill(string outpostId = "")
        {
            Vector3 center = Vector3.zero;
            string name = "Merkez Sığınak";

            if (!string.IsNullOrEmpty(outpostId) && OutpostLiberationManager.Instance != null)
            {
                var op = OutpostLiberationManager.Instance.FindOutpost(outpostId);
                if (op != null)
                {
                    center = op.worldPosition;
                    name = op.displayName;
                }
            }
            else
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    center = player.transform.position;
                    name = "Mevcut Karakol Konumu";
                }
            }

            TriggerSiege(center, name);
        }

        /// <summary>
        /// Initiates the full 2-Wave Outpost Defense Siege at the specified coordinate.
        /// </summary>
        public void TriggerSiege(Vector3 center, string locationName)
        {
            if (isSiegeActive) return;

            isSiegeActive = true;
            lastSiegeTime = Time.time;
            activeOutpostCenter = center;
            activeOutpostName = locationName;
            currentWave = 1;

            if (siegeCoroutine != null) StopCoroutine(siegeCoroutine);
            siegeCoroutine = StartCoroutine(SiegeSequenceRoutine());
        }

        private IEnumerator SiegeSequenceRoutine()
        {
            // 1. Sound 3D Positional Outpost Warning Siren
            ProceduralAudioManager.Instance?.PlayOutpostSiren(activeOutpostCenter);

            // 2. Radio Banner Toast
            GameEvents.TriggerObjectiveToastRequested(
                $"DİKKAT: {activeOutpostName} savunma alarmı devreye girdi! Zombi akını yaklaşıyor!",
                4.5f
            );

            yield return new WaitForSeconds(2.0f);

            // --- WAVE 1: 6 ZOMBIES (4 Walkers, 2 Runners) ---
            currentWave = 1;
            SpawnWave(6, 4, 2, 0, 0);

            if (SiegeWarningHUD.Instance != null)
            {
                SiegeWarningHUD.Instance.ShowSiegeHUD(1, 2, activeEnemies.Count);
            }

            while (activeEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Wave 1 Cleared Toast & Breather
            GameEvents.TriggerObjectiveToastRequested(
                "BİRİNCİ DALGA TEMİZLENDİ! İkinci ve daha güçlü akın yaklaşıyor!",
                3.5f
            );

            yield return new WaitForSeconds(waveBreatherDuration);

            // --- WAVE 2: 8 ZOMBIES (4 Walkers, 2 Runners, 1 Screamer, 1 Tank) ---
            currentWave = 2;
            ProceduralAudioManager.Instance?.PlayOutpostSiren(activeOutpostCenter);
            SpawnWave(8, 4, 2, 1, 1);

            if (SiegeWarningHUD.Instance != null)
            {
                SiegeWarningHUD.Instance.UpdateWave(2, 2, activeEnemies.Count);
            }

            while (activeEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // --- VICTORY & REWARDS ---
            OnSiegeVictory();
        }

        private void SpawnWave(int total, int walkers, int runners, int screamers, int tanks)
        {
            activeEnemies.Clear();
            ZombieAI sceneTemplate = FindFirstObjectByType<ZombieAI>();

            float angleStep = 360f / total;
            int spawnedW = 0, spawnedR = 0, spawnedS = 0, spawnedT = 0;

            for (int i = 0; i < total; i++)
            {
                float angle = (i * angleStep + UnityEngine.Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * perimeterSpawnDistance, 0f, Mathf.Sin(angle) * perimeterSpawnDistance);
                Vector3 spawnPos = activeOutpostCenter + offset;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }

                Quaternion lookRot = Quaternion.LookRotation((activeOutpostCenter - spawnPos).normalized);
                ZombieAI ai = null;

                if (sceneTemplate != null)
                {
                    GameObject zObj = Instantiate(sceneTemplate.gameObject, spawnPos, lookRot);
                    zObj.name = $"SiegeZombie_W{currentWave}_{i + 1}";
                    zObj.SetActive(true);
                    ai = zObj.GetComponent<ZombieAI>();
                }
                else
                {
                    ai = CreateFallbackSiegeZombie(spawnPos, i);
                }

                if (ai != null)
                {
                    // Assign archetype and stats
                    if (spawnedT < tanks)
                    {
                        ai.SetZombieType(ZombieType.Tank);
                        ai.ConfigureStats(180f, 22f, 2.8f);
                        spawnedT++;
                    }
                    else if (spawnedS < screamers)
                    {
                        ai.SetZombieType(ZombieType.Screamer);
                        ai.ConfigureStats(55f, 10f, 3.8f);
                        spawnedS++;
                    }
                    else if (spawnedR < runners)
                    {
                        ai.SetZombieType(ZombieType.Runner);
                        ai.ConfigureStats(55f, 12f, 4.8f);
                        spawnedR++;
                    }
                    else
                    {
                        ai.SetZombieType(ZombieType.Standard);
                        ai.ConfigureStats(65f, 14f, 3.8f);
                        spawnedW++;
                    }

                    // Direct toward outpost center / player
                    NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.speed = ai.EffectiveChaseSpeed;
                        agent.SetDestination(activeOutpostCenter);
                    }
                    ai.TriggerInvestigate(activeOutpostCenter);

                    activeEnemies.Add(ai);
                }
            }
        }

        private ZombieAI CreateFallbackSiegeZombie(Vector3 pos, int idx)
        {
            GameObject zObj = new GameObject($"SiegeZombie_W{currentWave}_{idx + 1}");
            zObj.transform.position = pos;

            CapsuleCollider cc = zObj.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            NavMeshAgent agent = zObj.AddComponent<NavMeshAgent>();
            agent.speed = 3.8f;
            agent.radius = 0.35f;
            agent.height = 1.8f;

            ZombieAI ai = zObj.AddComponent<ZombieAI>();
            return ai;
        }

        private void UpdateEnemyTrackingAndFortificationAttacks()
        {
            // Remove dead or destroyed enemies
            int prevCount = activeEnemies.Count;
            activeEnemies.RemoveAll(z => z == null || z.IsDead || !z.gameObject.activeInHierarchy);

            if (activeEnemies.Count != prevCount && SiegeWarningHUD.Instance != null)
            {
                SiegeWarningHUD.Instance.UpdateEnemyCount(activeEnemies.Count);
            }

            // Direct attackers & handle barricade/wall attacks
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 targetPos = player != null ? player.transform.position : activeOutpostCenter;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var z = activeEnemies[i];
                if (z == null || z.IsDead) continue;

                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    // Check if close to a player fortification
                    CheckBarricadeAttacks(z, agent);

                    if (!agent.pathPending && (agent.remainingDistance < 1.5f || !agent.hasPath))
                    {
                        agent.SetDestination(targetPos);
                    }
                }
            }
        }

        private void CheckBarricadeAttacks(ZombieAI zombie, NavMeshAgent agent)
        {
            var activeStructures = BuildableStructure.ActiveStructures;
            if (activeStructures == null || activeStructures.Count == 0) return;

            Vector3 zPos = zombie.transform.position;
            for (int s = 0; s < activeStructures.Count; s++)
            {
                var structure = activeStructures[s];
                if (structure == null) continue;

                if (structure.Type == StructureType.DefenseWall)
                {
                    float dist = Vector3.Distance(zPos, structure.transform.position);
                    if (dist <= 2.2f)
                    {
                        // Attack defense wall
                        structure.TakeStructuralDamage(14.0f * Time.deltaTime);
                        return;
                    }
                }
            }
        }

        private void OnSiegeVictory()
        {
            isSiegeActive = false;
            siegeCoroutine = null;

            // 1. Hide HUD
            if (SiegeWarningHUD.Instance != null)
            {
                SiegeWarningHUD.Instance.HideSiegeHUD();
            }

            // 2. Radio Toast
            GameEvents.TriggerObjectiveToastRequested(
                $"KARAKOL SAVUNMASI BAŞARILI! {activeOutpostName} güvence altına alındı.",
                4.5f
            );

            // 3. Award +120 Progression XP
            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.AddXP(120, "Karakol Kuşatması Püskürtüldü");
            }

            // 4. Award +15 Community Morale
            if (CommunityMoraleManager.Instance != null)
            {
                CommunityMoraleManager.Instance.AddMorale(15f);
            }

            // 5. Audio & Haptics
            ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            MobileHapticManager.TriggerSuccess();

            // 6. Spawn Post-Siege Salvage Crate
            SpawnPostSiegeCrate(activeOutpostCenter);

            Debug.Log($"[OutpostSiegeEvent] Siege at '{activeOutpostName}' completed successfully! Awarded +120 XP & +15 Morale.");
        }

        private void SpawnPostSiegeCrate(Vector3 outpostCenter)
        {
            Vector3 cratePos = outpostCenter + Vector3.forward * 8.5f + Vector3.right * 3.0f;
            if (NavMesh.SamplePosition(cratePos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                cratePos = hit.position;
            }

            GameObject crateObj = new GameObject("PostSiege_Salvage_Crate");
            crateObj.transform.position = cratePos;

            BoxCollider col = crateObj.AddComponent<BoxCollider>();
            col.size = new Vector3(1.1f, 0.8f, 1.1f);
            col.center = new Vector3(0f, 0.4f, 0f);

            Material oliveMat = GetOrCreateMaterial("Mat_Military_OliveDrab", new Color(0.22f, 0.28f, 0.20f));
            Material trimMat = GetOrCreateMaterial("Mat_Military_SteelTrim", new Color(0.12f, 0.14f, 0.15f));

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Crate_Body";
            body.transform.SetParent(crateObj.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.transform.localScale = new Vector3(1.05f, 0.75f, 1.05f);
            DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = oliveMat;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Crate_Trim";
            trim.transform.SetParent(crateObj.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            trim.transform.localScale = new Vector3(1.08f, 0.22f, 1.08f);
            DestroyImmediate(trim.GetComponent<Collider>());
            trim.GetComponent<Renderer>().sharedMaterial = trimMat;

            crateObj.AddComponent<PostSiegeSalvageCrate>();
        }

        public void CancelActiveSiege()
        {
            if (siegeCoroutine != null)
            {
                StopCoroutine(siegeCoroutine);
                siegeCoroutine = null;
            }

            isSiegeActive = false;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i].gameObject);
                }
            }
            activeEnemies.Clear();

            if (SiegeWarningHUD.Instance != null)
            {
                SiegeWarningHUD.Instance.HideSiegeHUD();
            }
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private static Material GetOrCreateMaterial(string name, Color color)
        {
            if (materialCache.TryGetValue(name, out Material mat) && mat != null) return mat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.enableInstancing = true;
            materialCache[name] = mat;
            return mat;
        }
    }
}
