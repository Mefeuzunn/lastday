using UnityEngine;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Player
{
    public enum CameraPerspective
    {
        ThirdPerson = 0,
        FirstPerson = 1
    }

    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        [Header("Camera Perspective Switching")]
        [SerializeField] private CameraPerspective currentPerspective = CameraPerspective.ThirdPerson;
        [SerializeField] private float transitionDuration = 0.22f;
        [SerializeField] private float fpsEyeHeightStanding = 1.66f;
        [SerializeField] private float fpsEyeHeightCrouched = 1.18f;
        [SerializeField] private float fpsMinPitch = -78f;
        [SerializeField] private float fpsMaxPitch = 78f;

        private float perspectiveBlend = 0f;
        private CameraPerspective targetPerspective = CameraPerspective.ThirdPerson;
        private CameraPerspective userPreferredPerspective = CameraPerspective.ThirdPerson;

        public bool IsFirstPerson => targetPerspective == CameraPerspective.FirstPerson;
        public CameraPerspective CurrentPerspective => targetPerspective;

        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private float shoulderRightOffset = 0.65f;
        [SerializeField] private float shoulderHeightOffset = 1.45f;
        [SerializeField] private float defaultDistance = 3.0f;
        [SerializeField] private float minDistance = 0.35f;

        [Header("Rotation Settings")]
        [SerializeField] private float touchSensitivity = 0.16f;
        [SerializeField] private float mouseSensitivity = 1.35f;
        [SerializeField] private float aimSensitivityMultiplier = 0.8f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private bool cameraShakeEnabled = true;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float rotationSmoothTime = 0.08f;

        [Header("Collision & Occlusion")]
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private LayerMask collisionLayers = ~0; // Default all layers

        [Header("Dynamic FOV")]
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float sprintFOV = 68f;
        [SerializeField] private float crouchFOV = 52f;
        [SerializeField] private float aimFOV = 45f;
        [SerializeField] private float aimDistance = 1.85f;
        [SerializeField] private float fovSmoothSpeed = 7.0f;

        private Camera cam;
        private float currentYaw = 0f;
        private float currentPitch = 12f;
        private float targetYaw = 0f;
        private float targetPitch = 12f;
        private float yawVelocity = 0f;
        private float pitchVelocity = 0f;

        private float currentDistance = 3.2f;
        private float distanceVelocity = 0f;

        private PlayerController playerController;
        private StealthSystem stealthSystem;

        private bool isAiming = false;
        private bool isHiding = false;
        private Transform hidingPeekTransform;

        public void SetAiming(bool aiming) => isAiming = aiming;
        public bool IsAiming => isAiming;

        public void EnterHidingPeek(Transform peekTrans)
        {
            isHiding = true;
            hidingPeekTransform = peekTrans;
        }

        public void ExitHidingPeek()
        {
            isHiding = false;
            hidingPeekTransform = null;
        }

        [Header("Vehicle Chase Camera Mode")]
        [SerializeField] private float vehicleCameraDistance = 5.8f;
        [SerializeField] private float vehicleCameraHeight = 2.3f;
        [SerializeField] private float vehicleLookAheadMultiplier = 0.22f;
        private bool isDrivingVehicle = false;
        private Transform vehicleTarget;
        private Rigidbody vehicleRigidbody;
        private Vector3 vehicleCamVelocity = Vector3.zero;

        public bool IsDrivingVehicle => isDrivingVehicle;

        public void EnterVehicleMode(Transform vTransform)
        {
            isDrivingVehicle = true;
            vehicleTarget = vTransform;
            if (vTransform != null)
            {
                vehicleRigidbody = vTransform.GetComponent<Rigidbody>();
                targetYaw = vTransform.eulerAngles.y;
                currentYaw = targetYaw;
                targetPitch = 14f;
                currentPitch = 14f;
            }
        }

        public void ExitVehicleMode()
        {
            isDrivingVehicle = false;
            vehicleTarget = null;
            vehicleRigidbody = null;
            FindTarget();
            if (target != null)
            {
                targetYaw = target.eulerAngles.y;
                currentYaw = targetYaw;
                targetPitch = 12f;
            }
            SetPerspective(userPreferredPerspective);
        }

        public void TogglePerspective()
        {
            SetPerspective(targetPerspective == CameraPerspective.ThirdPerson ? CameraPerspective.FirstPerson : CameraPerspective.ThirdPerson);
        }

        public void SetPerspective(CameraPerspective perspective)
        {
            targetPerspective = perspective;
            userPreferredPerspective = perspective;
            currentPerspective = perspective;

            SetFirstPersonMeshVisibility(targetPerspective == CameraPerspective.FirstPerson);

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();

            GameEvents.TriggerCameraPerspectiveChanged(targetPerspective == CameraPerspective.FirstPerson);

            Debug.Log($"[ThirdPersonCamera] Perspective switched to: {targetPerspective}");
        }

        public void SetFirstPersonMeshVisibility(bool isFirstPerson)
        {
            if (target == null) FindTarget();
            if (target == null) return;

            Transform vm = target.Find("Visual_Model");
            if (vm == null) return;

            Renderer[] rends = vm.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                if (r == null) continue;
                string goName = r.gameObject.name.ToLower();

                // Check if weapon or weapon attachment
                bool isWeapon = goName.Contains("weapon") || goName.Contains("axe") || goName.Contains("pistol") ||
                                goName.Contains("shotgun") || goName.Contains("crossbow") || goName.Contains("gun") ||
                                goName.Contains("socket") || goName.Contains("muzzle");

                // Check if hands/arms
                bool isHandsOrArms = goName.Contains("hand") || goName.Contains("forearm") || goName.Contains("arm");

                // Hide head, neck, eyes, hair, upper torso/vest in FP
                bool isHeadOrUpperBody = goName.Contains("head") || goName.Contains("neck") || goName.Contains("eye") ||
                                         goName.Contains("hair") || goName.Contains("hat") || goName.Contains("torso") ||
                                         goName.Contains("vest") || goName.Contains("backpack");

                if (isFirstPerson)
                {
                    if (isHeadOrUpperBody && !isWeapon && !isHandsOrArms)
                    {
                        r.enabled = false;
                    }
                    else
                    {
                        r.enabled = true;
                    }
                }
                else
                {
                    r.enabled = true;
                }
            }
        }

        public void SetLookSensitivity(float sens)
        {
            mouseSensitivity = Mathf.Clamp(sens, 0.2f, 3.0f);
            touchSensitivity = mouseSensitivity * 0.12f;
        }

        public void SetAimSensitivity(float aimSens)
        {
            aimSensitivityMultiplier = Mathf.Clamp(aimSens, 0.2f, 2.5f);
        }

        public void ApplyTouchLookDelta(Vector2 delta)
        {
            float currentTouchSens = isAiming ? (touchSensitivity * aimSensitivityMultiplier) : touchSensitivity;
            float touchPitchDelta = invertY ? (-delta.y * currentTouchSens) : (delta.y * currentTouchSens);
            targetYaw += delta.x * currentTouchSens;
            targetPitch = Mathf.Clamp(targetPitch - touchPitchDelta, minPitch, maxPitch);
        }

        public void ApplyMouseLookDelta(Vector2 delta)
        {
            float currentMouseSens = isAiming ? (mouseSensitivity * aimSensitivityMultiplier) : mouseSensitivity;
            float mouseX = delta.x * 0.15f * currentMouseSens;
            float mouseY = delta.y * 0.15f * currentMouseSens;
            float pitchDelta = invertY ? mouseY : -mouseY;
            targetYaw += mouseX;
            targetPitch = Mathf.Clamp(targetPitch + pitchDelta, minPitch, maxPitch);
        }

        public void SetDefaultFOV(float fov)
        {
            defaultFOV = Mathf.Clamp(fov, 50f, 75f);
            if (cam != null && !isAiming && (playerController == null || !playerController.IsSprinting))
            {
                cam.fieldOfView = defaultFOV;
            }
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            cameraShakeEnabled = enabled;
        }

        // Touch tracking
        private int lookTouchId = -1;
        private Vector2 lastTouchPos;

        // Cinematic Dialogue Camera
        private bool isInDialogue = false;
        private Transform dialogueNpcTarget;
        private Vector3 dialogueCamPosVelocity = Vector3.zero;
        private Quaternion dialogueCamRotVelocity = Quaternion.identity;

        // Camera Shake
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;
        private float shakeTimer = 0f;
        private static readonly RaycastHit[] sphereCastBuffer = new RaycastHit[16];

        public float CurrentYaw => currentYaw;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.12f);
                cam.fieldOfView = defaultFOV;
            }

            // Remove Player layer from collisionLayers
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                collisionLayers &= ~(1 << playerLayer);
            }
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1)
            {
                collisionLayers &= ~(1 << ignoreRaycast);
            }
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1)
            {
                collisionLayers &= ~(1 << uiLayer);
            }

            var menu = FindFirstObjectByType<ProjectSurvival.UI.MainMenuController>();
            if (menu != null && menu.AutoShowMenuOnStart)
            {
                enabled = false;
            }
        }

        private void Start()
        {
            FindTarget();
            if (target != null)
            {
                targetYaw = target.eulerAngles.y;
                currentYaw = targetYaw;
            }
            currentDistance = defaultDistance;
        }

        public void SyncRotationToCurrent()
        {
            Vector3 euler = transform.eulerAngles;
            currentYaw = euler.y;
            targetYaw = euler.y;
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetPitch = currentPitch;
            yawVelocity = 0f;
            pitchVelocity = 0f;
        }

        public void SetRotation(float yaw, float pitch)
        {
            currentYaw = yaw;
            targetYaw = yaw;
            currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetPitch = currentPitch;
            yawVelocity = 0f;
            pitchVelocity = 0f;
        }

        private void OnEnable()
        {
            GameEvents.OnDialogueStarted += HandleDialogueStarted;
            GameEvents.OnDialogueEnded += HandleDialogueEnded;
            GameEvents.OnCameraShakeRequested += HandleCameraShake;
        }

        private void OnDisable()
        {
            GameEvents.OnDialogueStarted -= HandleDialogueStarted;
            GameEvents.OnDialogueEnded -= HandleDialogueEnded;
            GameEvents.OnCameraShakeRequested -= HandleCameraShake;
        }

        private void HandleCameraShake(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = shakeDuration;
        }

        public void AddRecoil(float pitchRecoil, float yawRecoil)
        {
            if (!cameraShakeEnabled) return;
            targetPitch = Mathf.Clamp(targetPitch - pitchRecoil, minPitch, maxPitch);
            targetYaw += yawRecoil;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            FindTarget();
        }

        private void FindTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                playerController = target.GetComponent<PlayerController>();
                stealthSystem = target.GetComponent<StealthSystem>();
            }
        }

        private void Update()
        {
            if (CinematicMenuCamera.Instance != null && (CinematicMenuCamera.Instance.IsCinematicActive || CinematicMenuCamera.Instance.IsSwooping)) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu) return;
            if (ProjectSurvival.UI.MainMenuController.Instance != null && ProjectSurvival.UI.MainMenuController.Instance.IsMenuShowing) return;
            if (isInDialogue || Time.timeScale <= 0f) return;

            if (Input.GetKeyDown(KeyCode.V))
            {
                TogglePerspective();
            }

            HandleTouchAndMouseLook();
        }

        private void LateUpdate()
        {
            if (CinematicMenuCamera.Instance != null && (CinematicMenuCamera.Instance.IsCinematicActive || CinematicMenuCamera.Instance.IsSwooping)) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu) return;
            if (ProjectSurvival.UI.MainMenuController.Instance != null && ProjectSurvival.UI.MainMenuController.Instance.IsMenuShowing) return;
            if (Time.timeScale <= 0f) return;

            if (target == null)
            {
                FindTarget();
                if (target == null) return;
            }

            if (isInDialogue && dialogueNpcTarget != null)
            {
                UpdateDialogueCamera();
                return;
            }

            if (isHiding && hidingPeekTransform != null)
            {
                transform.position = Vector3.Lerp(transform.position, hidingPeekTransform.position, 10f * Time.deltaTime);
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
                currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 55f, fovSmoothSpeed * Time.deltaTime);
                return;
            }

            if (isDrivingVehicle && vehicleTarget != null)
            {
                UpdateVehicleCamera();
                return;
            }

            // 1. Perspective Transition Blend (0.22s SmoothStep Ease Curve)
            float targetBlend = (targetPerspective == CameraPerspective.FirstPerson) ? 1.0f : 0.0f;
            if (Mathf.Abs(perspectiveBlend - targetBlend) > 0.001f)
            {
                perspectiveBlend = Mathf.MoveTowards(perspectiveBlend, targetBlend, Time.deltaTime / transitionDuration);
            }
            else
            {
                perspectiveBlend = targetBlend;
            }
            float t = Mathf.SmoothStep(0f, 1f, perspectiveBlend);

            // 2. Dynamic Pitch Clamping (-78° to +78° in FirstPerson, -30° to +55° in ThirdPerson)
            float activeMinPitch = Mathf.Lerp(minPitch, fpsMinPitch, t);
            float activeMaxPitch = Mathf.Lerp(maxPitch, fpsMaxPitch, t);
            targetPitch = Mathf.Clamp(targetPitch, activeMinPitch, activeMaxPitch);

            // 3. Rotation Smoothing (Strict Single Authority)
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

            if (Mathf.Abs(currentYaw - targetYaw) < 0.001f)
            {
                currentYaw = targetYaw;
                yawVelocity = 0f;
            }
            if (Mathf.Abs(currentPitch - targetPitch) < 0.001f)
            {
                currentPitch = targetPitch;
                pitchVelocity = 0f;
            }

            Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // 4. Anchor Eye Level vs Shoulder Interpolation
            float eyeHeight = (stealthSystem != null && stealthSystem.IsCrouched) ? fpsEyeHeightCrouched : fpsEyeHeightStanding;
            float activeHeightOffset = Mathf.Lerp(shoulderHeightOffset, eyeHeight, t);
            float activeRightOffset = Mathf.Lerp(shoulderRightOffset, 0f, t);

            Vector3 pivot = target.position + Vector3.up * activeHeightOffset;
            Vector3 rightOffsetVec = cameraRotation * (Vector3.right * activeRightOffset);
            Vector3 originWithShoulder = pivot + rightOffsetVec;

            // 5. Target Distance & FOV
            float targetDesiredDist = defaultDistance;
            float targetFOV = defaultFOV;

            if (isAiming)
            {
                targetDesiredDist = (targetPerspective == CameraPerspective.FirstPerson) ? 0f : aimDistance;
                targetFOV = (WeaponSystem.Instance != null && WeaponSystem.Instance.CurrentWeapon == WeaponType.Crossbow) ? 38f : aimFOV;
            }
            else if (playerController != null && playerController.IsSprinting)
            {
                targetDesiredDist = defaultDistance + 0.3f;
                targetFOV = sprintFOV;
            }
            else if (stealthSystem != null && stealthSystem.IsCrouched)
            {
                targetDesiredDist = defaultDistance - 0.5f;
                targetFOV = crouchFOV;
            }

            // 6. SphereCast Collision Avoidance (Avoid wall clipping in interior safehouses)
            Vector3 backwardsDir = cameraRotation * -Vector3.forward;
            Ray ray = new Ray(originWithShoulder, backwardsDir);

            float resolvedDist = targetDesiredDist;
            if (t < 0.99f)
            {
                int hitCount = Physics.SphereCastNonAlloc(ray, collisionRadius, sphereCastBuffer, targetDesiredDist, collisionLayers, QueryTriggerInteraction.Ignore);
                float closestHit = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit h = sphereCastBuffer[i];
                    if (h.collider == null) continue;
                    if (target != null && (h.collider.transform == target || h.collider.transform.IsChildOf(target))) continue;
                    if (h.collider.CompareTag("Player")) continue;

                    if (h.distance < closestHit)
                    {
                        closestHit = h.distance;
                    }
                }

                if (closestHit < float.MaxValue)
                {
                    resolvedDist = Mathf.Clamp(closestHit - 0.12f, minDistance, targetDesiredDist);
                }
            }

            // 7. Blend Distance to 0 at Eye Level in First Person
            float currentDistTarget = Mathf.Lerp(resolvedDist, 0f, t);
            currentDistance = Mathf.SmoothDamp(currentDistance, currentDistTarget, ref distanceVelocity, 0.08f);
            if (Mathf.Abs(currentDistance - currentDistTarget) < 0.001f)
            {
                currentDistance = currentDistTarget;
                distanceVelocity = 0f;
            }

            // Final Camera Position
            Vector3 desiredPosition = originWithShoulder + (backwardsDir * currentDistance);

            // Apply camera shake trauma (with strict zero snapping)
            if (cameraShakeEnabled && shakeTimer > 0f && shakeIntensity > 0.001f)
            {
                shakeTimer -= Time.deltaTime;
                if (shakeTimer <= 0f)
                {
                    shakeTimer = 0f;
                    shakeIntensity = 0f;
                    shakeDuration = 0f;
                }
                else
                {
                    float progress = shakeDuration > 0f ? (shakeTimer / shakeDuration) : 0f;
                    float currentShake = shakeIntensity * progress;
                    Vector3 shakeOffset = new Vector3(
                        (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * 2f * currentShake,
                        (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * 2f * currentShake,
                        0f
                    );
                    desiredPosition += cameraRotation * shakeOffset;
                }
            }
            else
            {
                shakeTimer = 0f;
                shakeIntensity = 0f;
                shakeDuration = 0f;
            }

            transform.position = desiredPosition;

            // Apply anatomical limp camera roll ONLY when actively moving with a leg fracture
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture &&
                playerController != null && playerController.IsMoving &&
                Mathf.Abs(TraumaAndInjurySystem.Instance.CurrentLimpRoll) > 0.05f)
            {
                cameraRotation *= Quaternion.Euler(0f, 0f, TraumaAndInjurySystem.Instance.CurrentLimpRoll);
            }

            transform.rotation = cameraRotation;

            // Dynamic FOV
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
            }
        }

        private void HandleTouchAndMouseLook()
        {
            float currentMouseSens = isAiming ? (mouseSensitivity * aimSensitivityMultiplier) : mouseSensitivity;
            float currentTouchSens = isAiming ? (touchSensitivity * aimSensitivityMultiplier) : touchSensitivity;

            // 1. Desktop / Mouse Look (Smooth and responsive)
            bool rightHeld = Input.GetMouseButton(1);
            bool leftHeld = Input.GetMouseButton(0);
            bool leftDown = Input.GetMouseButtonDown(0);
            bool escapeDown = Input.GetKeyDown(KeyCode.Escape);

#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed) rightHeld = true;
                if (mouse.leftButton.isPressed) leftHeld = true;
                if (mouse.leftButton.wasPressedThisFrame) leftDown = true;
            }
            if (kb != null && kb.escapeKey.wasPressedThisFrame) escapeDown = true;
#endif

            if (escapeDown && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            bool canLook = rightHeld || Cursor.lockState == CursorLockMode.Locked || (!IsPointerOverUI() && leftHeld);
            if (canLook)
            {
                float mouseX = 0f;
                float mouseY = 0f;
#if ENABLE_INPUT_SYSTEM
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    mouseX = delta.x * 0.15f * currentMouseSens;
                    mouseY = delta.y * 0.15f * currentMouseSens;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Mathf.Abs(mouseX) < 0.001f && Mathf.Abs(mouseY) < 0.001f)
                {
                    mouseX = Input.GetAxis("Mouse X") * currentMouseSens;
                    mouseY = Input.GetAxis("Mouse Y") * currentMouseSens;
                }
#endif
                float pitchDelta = invertY ? mouseY : -mouseY;
                targetYaw += mouseX;
                targetPitch = Mathf.Clamp(targetPitch + pitchDelta, minPitch, maxPitch);
            }

            // Click into game view to lock cursor
            if (leftDown && !IsPointerOverUI())
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // 2. Mobile Touch Look Area (Right side of screen: x > Screen.width * 0.40f)
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    // Touch on right side of screen that doesn't start over UI
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (touch.position.x > Screen.width * 0.38f)
                        {
                            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                            {
                                lookTouchId = touch.fingerId;
                                lastTouchPos = touch.position;
                            }
                        }
                    }
                    else if (touch.phase == TouchPhase.Moved && touch.fingerId == lookTouchId)
                    {
                        Vector2 delta = touch.position - lastTouchPos;
                        lastTouchPos = touch.position;

                        float touchPitchDelta = invertY ? (-delta.y * currentTouchSens) : (delta.y * currentTouchSens);
                        targetYaw += delta.x * currentTouchSens;
                        targetPitch = Mathf.Clamp(targetPitch - touchPitchDelta, minPitch, maxPitch);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (touch.fingerId == lookTouchId)
                        {
                            lookTouchId = -1;
                        }
                    }
                }
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleDialogueStarted(string storyResource, string startNodeId)
        {
            isInDialogue = true;
            // Find closest active NPC to frame
            var allNpcs = Object.FindObjectsByType<InteractableNPC>(FindObjectsSortMode.None);
            Transform closestNpc = null;
            float minDist = 12f;

            if (target != null)
            {
                for (int i = 0; i < allNpcs.Length; i++)
                {
                    float d = Vector3.Distance(target.position, allNpcs[i].transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestNpc = allNpcs[i].transform;
                    }
                }
            }

            dialogueNpcTarget = closestNpc;
        }

        private void HandleDialogueEnded()
        {
            isInDialogue = false;
            dialogueNpcTarget = null;
        }

        private void UpdateDialogueCamera()
        {
            if (dialogueNpcTarget == null || target == null)
            {
                isInDialogue = false;
                return;
            }

            // Cinematic over-the-player's-left-shoulder framing looking at the NPC
            Vector3 toNpc = dialogueNpcTarget.position - target.position;
            toNpc.y = 0;
            if (toNpc.sqrMagnitude < 0.01f) toNpc = target.forward;
            toNpc.Normalize();

            Vector3 npcHeadPos = dialogueNpcTarget.position + Vector3.up * 1.6f;
            Vector3 playerHeadPos = target.position + Vector3.up * 1.65f;

            // Position camera slightly behind and to the left of the player, elevated
            Vector3 camGoalPos = playerHeadPos - (toNpc * 1.8f) + (Vector3.Cross(toNpc, Vector3.up) * 0.75f) + (Vector3.up * 0.2f);
            Quaternion camGoalRot = Quaternion.LookRotation(npcHeadPos - camGoalPos, Vector3.up);

            transform.position = Vector3.SmoothDamp(transform.position, camGoalPos, ref dialogueCamPosVelocity, 0.25f);
            transform.rotation = Quaternion.Slerp(transform.rotation, camGoalRot, Time.deltaTime * 6f);

            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 46f, Time.deltaTime * 4f);
            }
        }

        private void UpdateVehicleCamera()
        {
            float dt = Time.deltaTime;

            Vector3 vel = vehicleRigidbody != null ? vehicleRigidbody.linearVelocity : Vector3.zero;
            float speedKmh = vel.magnitude * 3.6f;

            if (speedKmh > 3f)
            {
                float vehicleYaw = vehicleTarget.eulerAngles.y;
                targetYaw = Mathf.LerpAngle(targetYaw, vehicleYaw, dt * 2.5f);
            }

            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, 0.12f);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, 0.12f);

            Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 pivot = vehicleTarget.position + Vector3.up * vehicleCameraHeight;

            Vector3 lookAhead = vel * vehicleLookAheadMultiplier;
            lookAhead.y = 0f;

            Vector3 backDir = rot * -Vector3.forward;
            Vector3 desiredPos = (pivot + lookAhead) + (backDir * vehicleCameraDistance);

            Ray ray = new Ray(pivot, backDir);
            float resolvedDist = vehicleCameraDistance;
            if (Physics.SphereCast(ray, 0.28f, out RaycastHit hit, vehicleCameraDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                resolvedDist = Mathf.Clamp(hit.distance - 0.15f, 1.5f, vehicleCameraDistance);
                desiredPos = (pivot + lookAhead) + (backDir * resolvedDist);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref vehicleCamVelocity, 0.08f);
            transform.rotation = Quaternion.LookRotation((pivot + lookAhead + rot * (Vector3.forward * 4f)) - transform.position, Vector3.up);

            if (cam != null)
            {
                float targetFOV = Mathf.Lerp(60f, 68f, Mathf.Clamp01(speedKmh / 58f));
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, dt * 4.0f);
            }
        }
    }
}
