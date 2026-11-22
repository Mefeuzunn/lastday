using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Player-Centric Focus & Interaction Scanner.
    /// Runs a proximity & cone-assisted scan at 10 Hz (every 0.1s) to determine the
    /// single best WorldInteractableObject in front of the player (within 3.2m and 75° cone).
    /// Provides TriggerPrimaryInteraction() for HUD buttons and direct screen tap support.
    /// </summary>
    public class PlayerInteractionScanner : MonoBehaviour
    {
        public static PlayerInteractionScanner Instance { get; private set; }

        [Header("Scan Parameters")]
        [SerializeField] private float scanRadius = 3.2f;
        [SerializeField] private float maxConeAngle = 85f;
        [SerializeField] private float scanInterval = 0.1f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private Camera mainCamera;
        private Transform playerRoot;
        private float scanTimer = 0f;
        private WorldInteractableObject currentFocused;
        private readonly Collider[] queryBuffer = new Collider[48];

        public WorldInteractableObject CurrentFocused => currentFocused;
        public float ScanRadius => scanRadius;
        public float MaxConeAngle => maxConeAngle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerRoot = transform;
            mainCamera = Camera.main;

            // Exclude Player and UI from obstacle mask
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) obstacleMask &= ~(1 << playerLayer);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) obstacleMask &= ~(1 << uiLayer);
        }

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Update()
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                PerformInteractionScan();
            }

            HandleDirectScreenTap();
        }

        public void PerformInteractionScan()
        {
            if (playerRoot == null) playerRoot = transform;
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 scanOrigin = playerRoot.position + Vector3.up * 1.0f;
            Vector3 camPos = mainCamera != null ? mainCamera.transform.position : scanOrigin;
            Vector3 camForward = mainCamera != null ? mainCamera.transform.forward : playerRoot.forward;

            // In edit mode or standalone test where camera doesn't follow player, align with player view
            if (!Application.isPlaying || mainCamera == null)
            {
                camPos = scanOrigin;
                camForward = playerRoot.forward;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(scanOrigin, scanRadius, queryBuffer, ~0, QueryTriggerInteraction.Collide);

            WorldInteractableObject bestCandidate = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = queryBuffer[i];
                if (hit == null || hit.transform == playerRoot || hit.transform.IsChildOf(playerRoot)) continue;

                WorldInteractableObject interactable = hit.GetComponentInParent<WorldInteractableObject>();
                if (interactable == null || !interactable.gameObject.activeInHierarchy) continue;

                Vector3 targetPos = interactable.InteractionTransform != null ? interactable.InteractionTransform.position : interactable.transform.position;
                float playerDist = Vector3.Distance(playerRoot.position, targetPos);
                if (playerDist > scanRadius + 0.8f) continue;

                Vector3 toTarget = targetPos - camPos;
                float camDist = toTarget.magnitude;
                Vector3 dir = camDist > 0.01f ? toTarget / camDist : camForward;

                float angle = Vector3.Angle(camForward, dir);

                // Also check angle from player's own forward facing
                Vector3 playerToTarget = (targetPos - playerRoot.position);
                playerToTarget.y = 0f;
                float playerAngle = Vector3.Angle(playerRoot.forward, playerToTarget.normalized);

                // Accept if within camera cone OR player forward cone
                if (angle <= maxConeAngle || playerAngle <= maxConeAngle)
                {
                    // Line-of-sight check to ensure object is not behind a thick solid wall
                    // If player is within close arm's reach (< 1.6m), do not obscure
                    bool isObscured = false;
                    if (playerDist > 1.6f)
                    {
                        Vector3 rayStart = scanOrigin;
                        Vector3 targetCenter = targetPos + Vector3.up * 0.25f;
                        Vector3 toTargetVec = targetCenter - rayStart;
                        float checkDist = toTargetVec.magnitude;
                        Vector3 rayDir = checkDist > 0.01f ? toTargetVec / checkDist : playerRoot.forward;

                        RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDir, Mathf.Max(0.01f, checkDist - 0.10f), ~0, QueryTriggerInteraction.Ignore);
                        if (hits != null && hits.Length > 0)
                        {
                            for (int h = 0; h < hits.Length; h++)
                            {
                                var wallHit = hits[h];
                                if (wallHit.collider == null || wallHit.collider.isTrigger) continue;
                                if (wallHit.transform == playerRoot || wallHit.transform.IsChildOf(playerRoot)) continue;
                                if (wallHit.transform == interactable.transform || wallHit.transform.IsChildOf(interactable.transform) || interactable.transform.IsChildOf(wallHit.transform)) continue;
                                // Ignore supporting surface, table, or crate directly beneath/beside target (within 0.45m)
                                if (Vector3.Distance(wallHit.point, targetPos) < 0.45f) continue;

                                isObscured = true;
                                break;
                            }
                        }
                    }

                    if (isObscured) continue;

                    // Score: lower is better (prioritizes screen center and close proximity)
                    float effectiveAngle = Mathf.Min(angle, playerAngle);
                    float score = (effectiveAngle * 0.35f) + (playerDist * 1.0f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCandidate = interactable;
                    }
                }
            }

            if (bestCandidate != currentFocused)
            {
                if (currentFocused != null)
                {
                    currentFocused.SetFocused(false);
                }

                currentFocused = bestCandidate;

                if (currentFocused != null)
                {
                    currentFocused.SetFocused(true);
                }

                ContextualInteractionHUD.Instance?.SetTarget(currentFocused);
            }
            else if (currentFocused != null)
            {
                Vector3 targetPos = currentFocused.InteractionTransform != null ? currentFocused.InteractionTransform.position : currentFocused.transform.position;
                float currentDist = Vector3.Distance(playerRoot.position, targetPos);
                if (currentDist > scanRadius + 1.0f)
                {
                    currentFocused.SetFocused(false);
                    currentFocused = null;
                    ContextualInteractionHUD.Instance?.SetTarget(null);
                }
            }
        }

        /// <summary>
        /// Direct Screen Tap Pickup: allows tapping directly on a 3D weapon or world loot item in screen space.
        /// </summary>
        private void HandleDirectScreenTap()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return; // Touched UI button, not world space
                }

                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera == null) return;

                Ray tapRay = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(tapRay, out RaycastHit hit, 12f, obstacleMask, QueryTriggerInteraction.Collide))
                {
                    WorldInteractableObject interactable = hit.collider.GetComponentInParent<WorldInteractableObject>();
                    if (interactable != null && interactable.gameObject.activeInHierarchy)
                    {
                        Vector3 targetPos = interactable.InteractionTransform != null ? interactable.InteractionTransform.position : interactable.transform.position;
                        float dist = Vector3.Distance(playerRoot.position, targetPos);

                        if (dist <= scanRadius + 1.0f)
                        {
                            if (currentFocused != null && currentFocused != interactable)
                            {
                                currentFocused.SetFocused(false);
                            }
                            currentFocused = interactable;
                            currentFocused.SetFocused(true);
                            ContextualInteractionHUD.Instance?.SetTarget(currentFocused);

                            TriggerPrimaryInteraction();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Triggers the primary interaction (e.g. AL / ETKİLEŞ) on the currently focused interactable.
        /// </summary>
        public void TriggerPrimaryInteraction()
        {
            if (playerRoot == null) playerRoot = transform;
            if (currentFocused == null) return;

            if (ContextualInteractionHUD.Instance != null && ContextualInteractionHUD.Instance.HasActiveTarget)
            {
                ContextualInteractionHUD.Instance.TriggerPrimaryVerb();
                return;
            }

            // Direct execution fallback if HUD is inactive
            var verbs = currentFocused.GetAvailableVerbs();
            if (verbs != null && verbs.Count > 0)
            {
                InteractionVerb primary = verbs[0];
                if (currentFocused.CanInteract(primary.verbId))
                {
                    currentFocused.ExecuteVerb(primary.verbId, playerRoot != null ? playerRoot.gameObject : gameObject);
                    PerformInteractionScan();
                }
            }
        }

        public void SetFocusedManual(WorldInteractableObject target)
        {
            if (currentFocused != null && currentFocused != target)
            {
                currentFocused.SetFocused(false);
            }
            currentFocused = target;
            if (currentFocused != null)
            {
                currentFocused.SetFocused(true);
            }
            ContextualInteractionHUD.Instance?.SetTarget(currentFocused);
        }

        private void OnDisable()
        {
            if (currentFocused != null)
            {
                currentFocused.SetFocused(false);
                currentFocused = null;
            }
            ContextualInteractionHUD.Instance?.SetTarget(null);
        }
    }
}
