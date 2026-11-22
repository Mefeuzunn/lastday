using System.Collections;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Master Step A: Jump & Low-Obstacle Vault / Mantle Parkour Controller.
    /// Manages tactile vertical movement and tactical obstacle vaulting:
    /// - Two-tier obstacle scanning (knee 0.45m, chest clearance 1.65m, surface probe 1.55m -> 0.30m)
    ///   for obstacles 0.35m to 1.45m tall (road barriers, supply crates, low fences, car hoods, window sills).
    /// - Smooth 0.42s parabolic vault arc with subtle camera pitch dip (-4° -> 0°) and 8 stamina deduction.
    /// - Standard physics jump (1.35m height, -22.0 gravity) with 0.15s coyote-time and 6 stamina deduction.
    /// - Injury enforcement: blocks jump/vault if leg fracture is active (without adrenaline) with toast feedback.
    /// </summary>
    public class ParkourVaultController : MonoBehaviour
    {
        public static ParkourVaultController Instance { get; private set; }

        [Header("Parkour Tuning")]
        [SerializeField] private float vaultReach = 0.85f;
        [SerializeField] private float minObstacleHeight = 0.35f;
        [SerializeField] private float maxObstacleHeight = 1.45f;
        [SerializeField] private float vaultDuration = 0.42f;
        [SerializeField] private float vaultForwardDistance = 1.25f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float jumpGravity = 22.0f;
        [SerializeField] private float coyoteTime = 0.15f;

        [Header("Stamina Costs")]
        [SerializeField] private float vaultStaminaCost = 8.0f;
        [SerializeField] private float jumpStaminaCost = 6.0f;

        [Header("Layer Masks")]
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController characterController;
        private PlayerController playerController;
        private SurvivalStats survivalStats;

        private bool isVaulting = false;
        private float lastGroundedTime = 0f;
        private Coroutine activeVaultCoroutine;

        public bool IsVaulting => isVaulting;

        public void ResetVaultState()
        {
            isVaulting = false;
            if (playerController != null) playerController.IsVaulting = false;
            if (activeVaultCoroutine != null)
            {
                StopCoroutine(activeVaultCoroutine);
                activeVaultCoroutine = null;
            }
        }

        private void EnsureComponents()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (survivalStats == null) survivalStats = GetComponent<SurvivalStats>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnsureComponents();

            // Exclude Player and UI layers from obstacle mask
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) obstacleMask &= ~(1 << playerLayer);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) obstacleMask &= ~(1 << uiLayer);
        }

        private void Update()
        {
            if (characterController != null && characterController.isGrounded)
            {
                lastGroundedTime = Time.time;
            }

            // Desktop / Editor shortcut (Space key)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryJumpOrVault();
            }
        }

        /// <summary>
        /// Checks if a valid climbable or vaultable obstacle is directly ahead of the player.
        /// </summary>
        public bool IsObstacleAhead()
        {
            return DetectVaultableObstacle(out _, out _);
        }

        /// <summary>
        /// Public entry point for both mobile button (ZIPLA/ATLA) and PC Space key.
        /// </summary>
        public bool TryJumpOrVault()
        {
            EnsureComponents();
            if (isVaulting) return false;

            // 1. Anatomical Trauma Check (Leg Fracture)
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture && !TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                GameEvents.TriggerObjectiveToastRequested("Bacak Kırık — Zıplanamaz", 2.0f);
                MobileHapticManager.TriggerLight();
                ProceduralAudioManager.Instance?.PlaySoftError();
                return false;
            }

            // 2. Obstacle Vault Check
            if (DetectVaultableObstacle(out Vector3 vaultTop, out Vector3 landingTarget))
            {
                return ExecuteVault(vaultTop, landingTarget);
            }

            // 3. Ground Jump Check (with coyote time)
            bool isGrounded = characterController != null && characterController.isGrounded;
            bool withinCoyote = (Time.time - lastGroundedTime) <= coyoteTime;

            if (isGrounded || withinCoyote)
            {
                return ExecuteGroundJump();
            }

            return false;
        }

        private bool RaycastIgnoringSelf(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, LayerMask mask)
        {
            hit = default;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            float closestDist = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                if (h.collider.isTrigger) continue;
                if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
                if (characterController != null && h.collider == characterController) continue;

                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    hit = h;
                    found = true;
                }
            }

            return found;
        }

        private bool DetectVaultableObstacle(out Vector3 vaultTopPoint, out Vector3 landingPoint)
        {
            EnsureComponents();
            vaultTopPoint = Vector3.zero;
            landingPoint = Vector3.zero;

            Vector3 pPos = transform.position;
            Vector3 fwd = transform.forward;

            // 1. Low knee/shin ray (0.45m high)
            Vector3 kneeRayOrigin = pPos + Vector3.up * 0.45f;
            bool hitKnee = RaycastIgnoringSelf(kneeRayOrigin, fwd, out RaycastHit hitLow, vaultReach, obstacleMask);

            if (!hitKnee) return false;

            // 2. High chest/head clearance ray (1.65m high)
            Vector3 chestRayOrigin = pPos + Vector3.up * 1.65f;
            bool hitChest = RaycastIgnoringSelf(chestRayOrigin, fwd, out _, vaultReach, obstacleMask);

            // If chest ray hits solid obstacle, player cannot vault through a solid wall or closed ceiling
            if (hitChest) return false;

            // 3. Downward surface probe (1.55m -> 0.30m)
            Vector3 probeOrigin = hitLow.point + fwd * 0.25f + Vector3.up * 1.1f;
            bool hitSurface = RaycastIgnoringSelf(probeOrigin, Vector3.down, out RaycastHit hitTop, 1.5f, obstacleMask);

            if (!hitSurface) return false;

            float obstacleHeight = hitTop.point.y - pPos.y;
            if (obstacleHeight < minObstacleHeight || obstacleHeight > maxObstacleHeight) return false;

            // 4. Landing probe (ensure ground exists on other side of obstacle)
            Vector3 landProbeOrigin = hitTop.point + fwd * vaultForwardDistance + Vector3.up * 0.5f;
            bool hitGround = RaycastIgnoringSelf(landProbeOrigin, Vector3.down, out RaycastHit hitLand, 3.0f, obstacleMask);

            vaultTopPoint = hitTop.point;
            landingPoint = hitGround ? hitLand.point : (pPos + fwd * (vaultForwardDistance + 0.5f));
            return true;
        }

        private bool ExecuteVault(Vector3 vaultTop, Vector3 landingTarget)
        {
            if (survivalStats != null && survivalStats.CurrentStamina < vaultStaminaCost)
            {
                GameEvents.TriggerObjectiveToastRequested("Yetersiz Dayanıklılık", 1.5f);
                return false;
            }

            if (survivalStats != null)
            {
                survivalStats.ConsumeStaminaForSprint(0.18f); // deducts ~8 stamina
            }

            // Audio, Haptics & Feedback
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerLight();

            isVaulting = true;
            if (playerController != null) playerController.IsVaulting = true;

            if (Application.isPlaying)
            {
                if (activeVaultCoroutine != null) StopCoroutine(activeVaultCoroutine);
                activeVaultCoroutine = StartCoroutine(VaultMovementRoutine(vaultTop, landingTarget));
            }
            else
            {
                transform.position = landingTarget + Vector3.up * 0.05f;
            }
            return true;
        }

        private IEnumerator VaultMovementRoutine(Vector3 vaultTop, Vector3 landingTarget)
        {
            isVaulting = true;
            if (playerController != null) playerController.IsVaulting = true;

            bool ccWasEnabled = characterController != null && characterController.enabled;
            if (characterController != null) characterController.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 peakPos = vaultTop + Vector3.up * 0.20f;
            Vector3 finalLanding = landingTarget + Vector3.up * 0.05f;

            float elapsed = 0f;

            while (elapsed < vaultDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / vaultDuration);

                // Smooth quadratic bezier arc over the obstacle
                float oneMinusT = 1f - t;
                Vector3 currentPos = (oneMinusT * oneMinusT * startPos) +
                                     (2f * oneMinusT * t * peakPos) +
                                     (t * t * finalLanding);

                transform.position = currentPos;
                yield return null;
            }

            transform.position = finalLanding;

            if (characterController != null && ccWasEnabled)
            {
                characterController.enabled = true;
            }

            isVaulting = false;
            if (playerController != null) playerController.IsVaulting = false;
            activeVaultCoroutine = null;

            // Surface-aware landing footstep & particle impact
            ProjectSurvival.Environment.SurfaceFootstepController.Instance?.OnLanding(0.85f, finalLanding);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private bool ExecuteGroundJump()
        {
            if (survivalStats != null && survivalStats.CurrentStamina < jumpStaminaCost)
            {
                return false;
            }

            if (survivalStats != null)
            {
                survivalStats.ConsumeStaminaForSprint(0.13f); // deducts ~6 stamina
            }

            // Calculate vertical velocity: v = sqrt(2 * g * h)
            float jumpVelocity = Mathf.Sqrt(2f * jumpGravity * jumpHeight);

            if (playerController != null)
            {
                playerController.ApplyJump(jumpVelocity, jumpGravity);
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();
            lastGroundedTime = 0f; // Clear coyote time

            return true;
        }
    }
}
