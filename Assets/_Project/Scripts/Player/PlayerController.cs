using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NoiseEmitter))]
    [RequireComponent(typeof(StealthSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float dodgeSpeed = 10.5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float accelerationRate = 28f;
        [SerializeField] private float decelerationRate = 34f;
        [SerializeField] private float gravity = 20f;

        [Header("References")]
        [SerializeField] private Transform visualModelTransform;

        private CharacterController characterController;
        private NoiseEmitter noiseEmitter;
        private StealthSystem stealthSystem;
        private SurvivalStats survivalStats;

        private Vector3 currentHorizontalVelocity = Vector3.zero;
        private bool isSprinting = false;
        private float verticalVelocity = 0f;

        // Dodge state
        private bool isDodging = false;
        private float dodgeTimer = 0f;
        private float nextDodgeTime = 0f;
        private Vector3 dodgeDirection = Vector3.zero;

        // Fall & Trauma tracking
        private bool wasGroundedLastFrame = true;
        private float peakAirborneY = 0f;
        private bool isTrackingFall = false;

        public bool IsMoving => characterController != null && characterController.velocity.magnitude > 0.1f;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => stealthSystem != null && stealthSystem.IsCrouched;
        public bool IsDodging => isDodging;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsVaulting { get; set; } = false;
        public float EnvironmentalSpeedMultiplier { get; set; } = 1.0f;
        public float VerticalVelocity { get => verticalVelocity; set => verticalVelocity = value; }

        public void ApplyJump(float jumpVel, float customGravity = 22.0f)
        {
            gravity = customGravity;
            verticalVelocity = jumpVel;
            wasGroundedLastFrame = false;
        }

        public void SetVerticalVelocity(float vel)
        {
            verticalVelocity = vel;
            wasGroundedLastFrame = false;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            noiseEmitter = GetComponent<NoiseEmitter>();
            stealthSystem = GetComponent<StealthSystem>();
            survivalStats = GetComponent<SurvivalStats>();

            if (visualModelTransform == null)
            {
                visualModelTransform = transform.Find("Visual_Model");
            }

            if (visualModelTransform != null)
            {
                visualModelTransform.gameObject.SetActive(true);
                var renderers = visualModelTransform.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].enabled = true;
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.OnStealthStateChanged += HandleStealthStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnStealthStateChanged -= HandleStealthStateChanged;
        }

        private void HandleStealthStateChanged(bool crouched)
        {
            if (characterController == null) return;

            if (crouched)
            {
                characterController.height = 1.35f;
                characterController.center = new Vector3(0f, 0.68f, 0f);
            }
            else
            {
                characterController.height = 2.0f;
                characterController.center = new Vector3(0f, 1.0f, 0f);
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.Dialogue || GameManager.Instance.CurrentState == GameState.MainMenu))
            {
                noiseEmitter.EmitNoise(NoiseState.Silent);
                return;
            }

            HandleInput();
            HandleMovement();
            HandleNoise();
        }

        private void HandleInput()
        {
            bool shiftDown = Input.GetKeyDown(KeyCode.LeftShift);
            bool shiftUp = Input.GetKeyUp(KeyCode.LeftShift);
            bool cPressed = Input.GetKeyDown(KeyCode.C);
            bool ePressed = Input.GetKeyDown(KeyCode.E);
            bool fPressed = Input.GetKeyDown(KeyCode.F);
            bool spacePressed = Input.GetKeyDown(KeyCode.Space);
            bool vPressed = Input.GetKeyDown(KeyCode.V);
            bool iPressed = Input.GetKeyDown(KeyCode.I);
            bool bPressed = Input.GetKeyDown(KeyCode.B);

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.leftShiftKey.wasPressedThisFrame) shiftDown = true;
                if (kb.leftShiftKey.wasReleasedThisFrame) shiftUp = true;
                if (kb.cKey.wasPressedThisFrame) cPressed = true;
                if (kb.eKey.wasPressedThisFrame) ePressed = true;
                if (kb.fKey.wasPressedThisFrame) fPressed = true;
                if (kb.spaceKey.wasPressedThisFrame) spacePressed = true;
                if (kb.vKey.wasPressedThisFrame) vPressed = true;
                if (kb.iKey.wasPressedThisFrame) iPressed = true;
                if (kb.bKey.wasPressedThisFrame) bPressed = true;
            }
#endif

            if (shiftDown) SetSprint(true);
            if (shiftUp) SetSprint(false);
            if (cPressed) stealthSystem?.ToggleCrouch();
            if (ePressed) TriggerInteract();
            if (fPressed) FlashlightController.Instance?.ToggleFlashlight();
            if (spacePressed)
            {
                if (ParkourVaultController.Instance != null)
                {
                    ParkourVaultController.Instance.TryJumpOrVault();
                }
                else
                {
                    ExecuteDodge();
                }
            }
            if (vPressed) CombatSystem.Instance?.ExecuteEmergencyPush();
            if (iPressed) GameEvents.TriggerOpenBackpackRequested();
            if (bPressed) CraftingUI.Instance?.TogglePanel();
        }

        public void SetSprint(bool sprinting)
        {
            if (sprinting && TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture && !TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                isSprinting = false;
                GameEvents.TriggerObjectiveToastRequested("Bacak kırık! Koşamazsın, atel veya ilk yardım kiti kullan.", 2.5f);
                return;
            }

            // Can only sprint if not crouching and stamina available
            if (sprinting && stealthSystem.IsCrouched)
            {
                stealthSystem.SetCrouch(false);
            }

            isSprinting = sprinting;
        }

        public void ToggleCrouch()
        {
            if (isSprinting) isSprinting = false;
            stealthSystem.ToggleCrouch();
        }

        public void TriggerInteract()
        {
            GameEvents.TriggerInteractPressed();
        }

        public void ExecuteDodge()
        {
            if (isDodging || Time.time < nextDodgeTime) return;

            // Check stamina (requires at least 16 stamina)
            if (survivalStats != null && survivalStats.CurrentStamina < 16f) return;

            if (survivalStats != null)
            {
                survivalStats.ConsumeStaminaForSprint(0.35f);
            }

            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            Vector2 joyInput = (MobileJoystick.Instance != null && MobileJoystick.Instance.IsDragging) 
                ? MobileJoystick.Instance.Direction 
                : Vector2.zero;
            Vector2 keyInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyInput.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyInput.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyInput.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyInput.x -= 1f;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyInput.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyInput.x -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keyInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keyInput.y -= 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (keyInput.sqrMagnitude < 0.01f)
            {
                keyInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
#endif
            Vector2 inputDir = joyInput.sqrMagnitude > keyInput.sqrMagnitude ? joyInput : keyInput;

            if (inputDir.sqrMagnitude > 0.05f && camTransform != null)
            {
                Vector3 camFwd = camTransform.forward;
                camFwd.y = 0;
                camFwd.Normalize();
                Vector3 camRight = camTransform.right;
                camRight.y = 0;
                camRight.Normalize();
                dodgeDirection = (camFwd * inputDir.y + camRight * inputDir.x).normalized;
            }
            else
            {
                dodgeDirection = -transform.forward; // Backstep if stationary
            }

            isDodging = true;
            dodgeTimer = 0.28f;
            nextDodgeTime = Time.time + 0.75f;
            ProceduralAudioManager.Instance?.PlayPush();
        }

        private void HandleMovement()
        {
            if (IsVaulting) return;

            Vector2 joyInput = Vector2.zero;
            if (MobileJoystick.Instance != null && MobileJoystick.Instance.IsDragging)
            {
                joyInput = MobileJoystick.Instance.Direction;
            }

            Vector2 keyInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyInput.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyInput.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyInput.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyInput.x -= 1f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyInput.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyInput.x -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keyInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keyInput.y -= 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (keyInput.sqrMagnitude < 0.01f)
            {
                keyInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
#endif
            if (keyInput.sqrMagnitude > 1f) keyInput.Normalize();

            // Select active input with larger magnitude
            Vector2 inputDir = joyInput.sqrMagnitude > keyInput.sqrMagnitude ? joyInput : keyInput;

            // Camera-Relative Movement (TPS Standard)
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            Vector3 moveDirection = Vector3.zero;

            if (camTransform != null)
            {
                Vector3 camFwd = camTransform.forward;
                camFwd.y = 0f;
                camFwd.Normalize();

                Vector3 camRight = camTransform.right;
                camRight.y = 0f;
                camRight.Normalize();

                moveDirection = (camFwd * inputDir.y) + (camRight * inputDir.x);
            }
            else
            {
                moveDirection = new Vector3(inputDir.x, 0f, inputDir.y);
            }

            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

            float targetSpeed = walkSpeed;

            if (stealthSystem.IsCrouched)
            {
                targetSpeed = crouchSpeed;
                isSprinting = false;
            }
            else if (isSprinting && moveDirection.sqrMagnitude > 0.01f)
            {
                if (survivalStats != null && survivalStats.CanSprint())
                {
                    targetSpeed = sprintSpeed;
                    survivalStats.ConsumeStaminaForSprint(Time.deltaTime);
                }
                else
                {
                    isSprinting = false;
                }
            }

            // Unencumbered movement bonus (+12% when unarmed)
            if (WeaponSystem.Instance != null && WeaponSystem.Instance.CurrentWeapon == WeaponType.None)
            {
                targetSpeed *= 1.12f;
            }

            // Anatomical Leg Fracture penalty: locks sprint and cuts walk speed by 42% (overridden during adrenaline rush)
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                targetSpeed *= 1.20f; // +20% movement speed during adrenaline rush
            }
            else if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture)
            {
                isSprinting = false;
                targetSpeed *= 0.58f;
            }

            // Corpse Dragging speed penalty: -35% speed and locks sprint
            if (BodyConcealmentSystem.Instance != null && BodyConcealmentSystem.Instance.IsDraggingCorpse)
            {
                isSprinting = false;
                targetSpeed *= 0.65f;
            }

            // Environmental terrain speed modifier (e.g., shallow floodwater -18%)
            targetSpeed *= EnvironmentalSpeedMultiplier;

            if (characterController.isGrounded)
            {
                if (!wasGroundedLastFrame && isTrackingFall)
                {
                    float fallDistance = peakAirborneY - transform.position.y;
                    if (fallDistance > 3.8f)
                    {
                        TraumaAndInjurySystem.Instance?.InflictLegFracture(fallDistance);
                    }
                    isTrackingFall = false;
                }
                wasGroundedLastFrame = true;
                verticalVelocity = -0.5f;
                peakAirborneY = transform.position.y;
            }
            else
            {
                if (wasGroundedLastFrame)
                {
                    wasGroundedLastFrame = false;
                    isTrackingFall = true;
                    peakAirborneY = transform.position.y;
                }
                else if (transform.position.y > peakAirborneY)
                {
                    peakAirborneY = transform.position.y;
                }
                verticalVelocity -= gravity * Time.deltaTime;
            }

            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0f) isDodging = false;
                currentHorizontalVelocity = dodgeDirection * dodgeSpeed;
            }
            else
            {
                // Smooth acceleration and deceleration
                Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;
                float rate = moveDirection.sqrMagnitude > 0.01f ? accelerationRate : decelerationRate;
                currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, rate * Time.deltaTime);
            }

            Vector3 finalMove = currentHorizontalVelocity + (Vector3.up * verticalVelocity);
            characterController.Move(finalMove * Time.deltaTime);

            // Smooth rotation towards movement direction or aim direction
            if (ThirdPersonCamera.Instance != null && ThirdPersonCamera.Instance.IsAiming && camTransform != null)
            {
                Vector3 aimLook = camTransform.forward;
                aimLook.y = 0;
                if (aimLook.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(aimLook.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 18f * Time.deltaTime);
                }
            }
            else if (currentHorizontalVelocity.sqrMagnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentHorizontalVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleNoise()
        {
            if (characterController.velocity.sqrMagnitude < 0.05f)
            {
                noiseEmitter.EmitNoise(NoiseState.Silent);
            }
            else if (stealthSystem.IsCrouched)
            {
                noiseEmitter.EmitNoise(NoiseState.Crouch);
            }
            else if (isSprinting)
            {
                noiseEmitter.EmitNoise(NoiseState.Sprint);
            }
            else
            {
                noiseEmitter.EmitNoise(NoiseState.Walk);
            }
        }
    }
}
