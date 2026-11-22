using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.AI;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.Core
{
    /// <summary>
    /// Guarantees a safe perimeter (42m) around player spawn, relocates initial scene zombies,
    /// enforces an initial 6-second orientation grace period, removes low-hanging overhead sightline obstructions,
    /// and instantiates the high-visibility starter weapons.
    /// </summary>
    public class SpawnProtectionController : MonoBehaviour
    {
        public static SpawnProtectionController Instance { get; set; }

        [Header("Spawn Perimeter & Grace Settings")]
        [SerializeField] private float spawnSafeRadius = 45.0f;
        [SerializeField] private float gracePeriodDuration = 6.0f;
        [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(-1.6f, 0.08f, -0.9f);
        public static readonly Vector3 BedroomSpawnPosition = new Vector3(-1.6f, 0.08f, -0.9f);
        public static readonly Quaternion BedroomSpawnRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Relocation Distances")]
        [SerializeField] private float minRelocationDistance = 52.0f;
        [SerializeField] private float maxRelocationDistance = 95.0f;

        private float graceTimer = 0f;
        private bool hasInitialized = false;
        private Transform playerTransform;

        public float SpawnSafeRadius => spawnSafeRadius;
        public float GracePeriodDuration => gracePeriodDuration;
        public bool IsSpawnGraceActive => graceTimer > 0f;
        public float GraceTimeRemaining => Mathf.Max(0f, graceTimer);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SpawnProtectionController");
                Instance = go.AddComponent<SpawnProtectionController>();
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
            graceTimer = gracePeriodDuration;
        }

        private void Start()
        {
            InitializeSpawnPerimeter();
        }

        private void Update()
        {
            // Hold grace period while in Main Menu so cinematic camera orbit is peaceful
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                graceTimer = gracePeriodDuration;
                return;
            }

            if (graceTimer > 0f)
            {
                graceTimer -= Time.deltaTime;
                if (graceTimer <= 0f)
                {
                    graceTimer = 0f;
                    Debug.Log("[SpawnProtectionController] Initial spawn grace period ended. Full ambient AI active.");
                }
            }
        }

        public void InitializeSpawnPerimeter()
        {
            if (hasInitialized) return;
            hasInitialized = true;

            // 1. Locate and calibrate player position
            LocateAndCalibratePlayer();

            // 2. Relocate all zombies inside 42m safe perimeter
            RelocateZombiesInsideSafePerimeter();

            // 3. Clear low-hanging overhead obstacles obstructing sky sightlines
            CleanupSpawnSightlines();

            // 4. Place starter arsenal
            StarterArsenalPlacer.PlaceAllStarterWeapons();

            // 5. Place restorable hero vehicles across districts
            Vehicles.RestorableVehiclePlacer.PlaceAllRestorableVehicles();

            Debug.Log($"[SpawnProtectionController] Initialized: 42m Safe Perimeter enforced, Grace Period: {gracePeriodDuration}s active.");
        }

        private void LocateAndCalibratePlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                // If player was placed at legacy -28 position, relocate to (0, 0.1, 0)
                if (playerTransform.position.z < -20f && playerTransform.position.z > -35f)
                {
                    playerTransform.position = defaultSpawnPosition;
                    Debug.Log($"[SpawnProtectionController] Calibrated Player starting position to {defaultSpawnPosition}");
                }
            }
            else
            {
                Debug.LogWarning("[SpawnProtectionController] Player tag not found during spawn calibration!");
            }
        }

        public void RelocateZombiesInsideSafePerimeter()
        {
            Vector3 center = playerTransform != null ? playerTransform.position : defaultSpawnPosition;
            var zombies = ZombieAI.ActiveZombies;
            List<ZombieBrain> toRelocate = new List<ZombieBrain>();

            // Check ActiveZombies list
            if (zombies != null)
            {
                for (int i = 0; i < zombies.Count; i++)
                {
                    var z = zombies[i];
                    if (z == null || z.IsDead) continue;
                    float dist = Vector3.Distance(z.transform.position, center);
                    if (dist < spawnSafeRadius)
                    {
                        toRelocate.Add(z);
                    }
                }
            }

            // Also check FindObjectsByType to catch any zombies initialized before ActiveZombies registration
            var allSceneZombies = FindObjectsByType<ZombieBrain>(FindObjectsSortMode.None);
            for (int i = 0; i < allSceneZombies.Length; i++)
            {
                var z = allSceneZombies[i];
                if (z == null || z.IsDead || toRelocate.Contains(z)) continue;
                float dist = Vector3.Distance(z.transform.position, center);
                if (dist < spawnSafeRadius)
                {
                    toRelocate.Add(z);
                }
            }

            // Relocate each
            int count = 0;
            for (int i = 0; i < toRelocate.Count; i++)
            {
                var z = toRelocate[i];
                if (RelocateZombieOutsidePerimeter(z, center))
                {
                    count++;
                }
            }

            Debug.Log($"[SpawnProtectionController] Relocated {count} zombies outside the {spawnSafeRadius}m safe perimeter.");
        }

        private bool RelocateZombieOutsidePerimeter(ZombieBrain zombie, Vector3 center)
        {
            if (zombie == null) return false;

            // Pick a direction radiating away from center, avoiding the forward +Z road sightline if possible
            Vector3 oldPos = zombie.transform.position;
            Vector3 dir = (oldPos - center);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            }
            else
            {
                dir.Normalize();
            }

            // Target distance: 48m to 95m
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angleOffset = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
                float currentAngle = Mathf.Atan2(dir.z, dir.x) + angleOffset;
                Vector3 sampleDir = new Vector3(Mathf.Cos(currentAngle), 0f, Mathf.Sin(currentAngle));
                float dist = Random.Range(minRelocationDistance, maxRelocationDistance);
                Vector3 candidate = center + sampleDir * dist;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
                {
                    NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled)
                    {
                        agent.Warp(hit.position);
                    }
                    else
                    {
                        zombie.transform.position = hit.position;
                    }

                    // Reset alert state
                    zombie.ResetAlertState();
                    return true;
                }
            }

            return false;
        }

        public void CleanupSpawnSightlines()
        {
            // Disable or elevate any low-hanging overhead meshes blocking sky view in spawn corridor (X: [-7, 7], Y: [2.5, 13.5], Z: [-5, 25])
            GameObject canopyObj = GameObject.Find("Overhead_Canopy");
            if (canopyObj != null)
            {
                canopyObj.SetActive(false);
                Debug.Log("[SpawnProtectionController] Disabled Overhead_Canopy for clean spawn sightline.");
            }

            GameObject cablesObj = GameObject.Find("Overhead_Cables_Boulevard");
            if (cablesObj != null)
            {
                cablesObj.SetActive(false);
                Debug.Log("[SpawnProtectionController] Disabled Overhead_Cables_Boulevard for clean spawn sightline.");
            }

            // General bounds scan for any other low-hanging overhead arches
            Bounds spawnCorridor = new Bounds(new Vector3(0f, 8f, 10f), new Vector3(14f, 11f, 30f));
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                var r = allRenderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;

                string objName = r.gameObject.name.ToLower();
                if (objName.Contains("canopy") || objName.Contains("overhead") || objName.Contains("arch") || objName.Contains("cable"))
                {
                    if (spawnCorridor.Contains(r.bounds.center))
                    {
                        r.gameObject.SetActive(false);
                        Debug.Log($"[SpawnProtectionController] Deactivated low-hanging overhead obstacle: {r.gameObject.name}");
                    }
                }
            }
        }
    }
}
