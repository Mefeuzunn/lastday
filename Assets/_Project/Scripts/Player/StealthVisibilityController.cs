using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Active Phase 4: Real-Time Stealth Visibility & Flashlight Detection Engine.
    /// Calculates dynamic player exposure score (0.05 - 1.0) based on stance, velocity,
    /// foliage cover, day/night, dense fog, and active flashlight beam sweeps.
    /// Directly drives ZombieSensorySystem detection ranges and HUD Stealth Eye indicator.
    /// </summary>
    public class StealthVisibilityController : MonoBehaviour
    {
        private static StealthVisibilityController _instance;
        public static StealthVisibilityController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StealthVisibilityController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StealthVisibilityController");
                        _instance = go.AddComponent<StealthVisibilityController>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (_instance == null)
            {
                var inst = Instance;
            }
        }

        [Header("Live Exposure Score")]
        [Range(0f, 1f)]
        [SerializeField] private float currentVisibilityScore = 0.75f;
        [SerializeField] private bool inFoliageCover = false;
        [SerializeField] private bool isFlashlightSweepingZombie = false;

        [Header("Stance Visibility Baselines")]
        [SerializeField] private float sprintVisibility = 1.0f;
        [SerializeField] private float walkVisibility = 0.75f;
        [SerializeField] private float standStationaryVisibility = 0.60f;
        [SerializeField] private float crouchMoveVisibility = 0.42f;
        [SerializeField] private float crouchStationaryVisibility = 0.30f;

        [Header("Environmental Multipliers")]
        [SerializeField] private float foliageReductionMultiplier = 0.55f; // -45% exposure
        [SerializeField] private float darknessReductionMultiplier = 0.75f; // -25% baseline in night/fog
        [SerializeField] private float flashlightExposureBonus = 0.40f;     // +40% exposure

        private Transform playerTransform;
        private PlayerController playerController;
        private StealthSystem playerStealth;
        private CharacterController playerCC;
        private FlashlightController playerFlashlight;

        private float calculationTimer = 0f;
        private float flashlightSweepTimer = 0f;

        public float CurrentVisibilityScore => currentVisibilityScore;
        public bool InFoliageCover => inFoliageCover;
        public bool IsFlashlightSweepingZombie => isFlashlightSweepingZombie;

        public event Action<float> OnVisibilityChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            LocatePlayer();
            CalculateVisibility();
        }

        private void LocatePlayer()
        {
            if (playerTransform != null) return;

            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
            if (player != null)
            {
                playerTransform = player.transform;
                playerController = player.GetComponent<PlayerController>();
                playerStealth = player.GetComponent<StealthSystem>();
                playerCC = player.GetComponent<CharacterController>();
                playerFlashlight = player.GetComponentInChildren<FlashlightController>() ?? FlashlightController.Instance;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            LocatePlayer();
            if (playerTransform == null) return;

            calculationTimer += Time.deltaTime;
            if (calculationTimer >= 0.1f)
            {
                calculationTimer = 0f;
                CalculateVisibility();
            }

            flashlightSweepTimer += Time.deltaTime;
            if (flashlightSweepTimer >= 0.2f)
            {
                flashlightSweepTimer = 0f;
                PerformFlashlightSweep();
            }
        }

        public void CalculateVisibility()
        {
            LocatePlayer();
            if (playerTransform == null) return;

            // 1. Stance & Movement Speed
            bool isCrouched = (playerStealth != null && playerStealth.IsCrouched);
            bool isSprinting = (playerController != null && playerController.IsSprinting);
            float speedSqr = (playerCC != null) ? playerCC.velocity.sqrMagnitude : 0f;
            bool isMoving = speedSqr > 0.05f;

            float baseScore = walkVisibility;
            if (isSprinting)
            {
                baseScore = sprintVisibility;
            }
            else if (isCrouched)
            {
                baseScore = isMoving ? crouchMoveVisibility : crouchStationaryVisibility;
            }
            else
            {
                baseScore = isMoving ? walkVisibility : standStationaryVisibility;
            }

            // 2. Cover & Foliage check
            inFoliageCover = CheckFoliageCover(playerTransform.position);
            if (inFoliageCover || (playerStealth != null && playerStealth.IsHidden))
            {
                baseScore *= foliageReductionMultiplier; // -45%
            }

            // 3. Lighting & Weather check (Night & Dense Fog reduce visibility by -25%)
            bool isNight = DayNightManager.Instance != null && DayNightManager.Instance.IsNight;
            bool isDenseFog = DynamicWeatherManager.Instance != null && DynamicWeatherManager.Instance.CurrentWeather == WeatherState.DenseFog;

            if (isNight || isDenseFog)
            {
                baseScore *= darknessReductionMultiplier; // -25%
            }

            // 4. Flashlight Discipline: ON adds +40% visibility
            bool isFlashlightOn = (playerFlashlight != null && playerFlashlight.IsOn) ||
                                  (FlashlightController.Instance != null && FlashlightController.Instance.IsOn);

            if (isFlashlightOn)
            {
                baseScore += flashlightExposureBonus;
            }

            // Clamped exposure score
            float newScore = Mathf.Clamp(baseScore, 0.05f, 1.0f);
            if (!Mathf.Approximately(currentVisibilityScore, newScore))
            {
                currentVisibilityScore = newScore;
                OnVisibilityChanged?.Invoke(currentVisibilityScore);
            }
        }

        private bool CheckFoliageCover(Vector3 pos)
        {
            // Check proximity to park bushes and foliage hiding spots (< 2.5m)
            var spots = FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i] != null && spots[i].SpotType == HidingSpotType.FoliageBush)
                {
                    if ((spots[i].transform.position - pos).sqrMagnitude <= 6.25f) // 2.5m
                    {
                        return true;
                    }
                }
            }

            // Check overlap with foliage/bush layers or colliders
            Collider[] hits = Physics.OverlapSphere(pos + Vector3.up * 0.5f, 0.85f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].name.ToLower().Contains("bush"))
                {
                    return true;
                }
            }

            return false;
        }

        private void PerformFlashlightSweep()
        {
            bool isFlashlightOn = (playerFlashlight != null && playerFlashlight.IsOn) ||
                                  (FlashlightController.Instance != null && FlashlightController.Instance.IsOn);

            if (!isFlashlightOn || playerTransform == null)
            {
                isFlashlightSweepingZombie = false;
                return;
            }

            Vector3 origin = playerTransform.position + Vector3.up * 1.4f;
            Vector3 forward = playerTransform.forward;
            float sweepRange = 18.0f;
            isFlashlightSweepingZombie = false;

            var activeZombies = ZombieAI.ActiveZombies;
            if (activeZombies == null) return;

            for (int i = 0; i < activeZombies.Count; i++)
            {
                ZombieAI z = activeZombies[i];
                if (z == null || z.IsDead) continue;

                Vector3 zombieHead = z.transform.position + Vector3.up * 1.5f;
                Vector3 toZombie = zombieHead - origin;
                float dist = toZombie.magnitude;

                if (dist <= sweepRange)
                {
                    Vector3 dir = toZombie.normalized;
                    if (Vector3.Dot(forward, dir) > 0.75f) // Inside flashlight cone
                    {
                        // Check line of sight
                        int mask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
                        int pLayer = LayerMask.NameToLayer("Player");
                        if (pLayer != -1) mask &= ~(1 << pLayer);

                        if (!Physics.Raycast(origin, dir, dist - 0.2f, mask))
                        {
                            isFlashlightSweepingZombie = true;
                            z.SetTarget(playerTransform);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts the 0.05 - 1.0 visibility score into a vision range multiplier for ZombieSensorySystem.
        /// When visibility is low (0.30), zombies can only see player at 40% distance.
        /// </summary>
        public float GetVisionDistanceMultiplier()
        {
            // Normalize: 0.75 baseline visibility = 1.0x distance multiplier
            return Mathf.Clamp(currentVisibilityScore / 0.75f, 0.28f, 1.45f);
        }

        public void SetManualFoliageCover(bool inCover)
        {
            inFoliageCover = inCover;
            CalculateVisibility();
        }
    }
}
