using UnityEngine;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Micro-Step 1A: Dedicated Anti-Clipping Vehicle Chase Camera.
    /// Smoothly trails 6.0m behind and 2.35m above the vehicle with velocity look-ahead,
    /// dynamic speed FOV (60-68), and a 0.25m SphereCast wall occlusion resolver.
    /// Handshakes seamlessly with ThirdPersonCamera upon boarding/disembarking.
    /// </summary>
    public class VehicleChaseCamera : MonoBehaviour
    {
        public static VehicleChaseCamera Instance { get; private set; }

        [Header("Chase Distance & Height")]
        [SerializeField] private float followDistance = 6.0f;
        [SerializeField] private float followHeight = 2.35f;
        [SerializeField] private float lookAheadMultiplier = 0.45f;
        [SerializeField] private float rotationDamping = 3.5f;
        [SerializeField] private float positionSmoothTime = 0.08f;

        [Header("Anti-Clipping SphereCast")]
        [SerializeField] private float sphereCastRadius = 0.25f;
        [SerializeField] private float minCameraDistance = 1.5f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Header("Dynamic FOV")]
        [SerializeField] private float minFOV = 60f;
        [SerializeField] private float maxFOV = 68f;
        [SerializeField] private float fovChangeSpeed = 4.0f;

        private Transform vehicleTarget;
        private Rigidbody vehicleRb;
        private Camera targetCamera;
        private bool isChaseActive = false;

        private Vector3 currentVelocity;
        private float currentYaw;
        private float currentPitch = 12f;
        private float yawVelocity;
        private float pitchVelocity;

        public bool IsActive => isChaseActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            // Exclude Player, Triggers, and UI from camera obstacle raycasts
            int playerLayer = LayerMask.NameToLayer("Player");
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            int uiLayer = LayerMask.NameToLayer("UI");

            if (playerLayer != -1) collisionLayers &= ~(1 << playerLayer);
            if (ignoreRaycast != -1) collisionLayers &= ~(1 << ignoreRaycast);
            if (uiLayer != -1) collisionLayers &= ~(1 << uiLayer);
        }

        public void Activate(Transform target, Rigidbody rb = null)
        {
            vehicleTarget = target;
            vehicleRb = rb != null ? rb : (target != null ? target.GetComponent<Rigidbody>() : null);
            isChaseActive = true;

            if (vehicleTarget != null)
            {
                currentYaw = vehicleTarget.eulerAngles.y;
            }

            if (targetCamera == null) targetCamera = Camera.main;

            // Notify ThirdPersonCamera to yield control
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.enabled = false;
            }
        }

        public void Deactivate()
        {
            isChaseActive = false;
            vehicleTarget = null;
            vehicleRb = null;

            // Restore ThirdPersonCamera
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.enabled = true;
                ThirdPersonCamera.Instance.ExitVehicleMode();
            }
        }

        private void LateUpdate()
        {
            if (!isChaseActive || vehicleTarget == null) return;

            UpdateCameraTransform(Time.deltaTime);
        }

        private void UpdateCameraTransform(float dt)
        {
            Vector3 vel = vehicleRb != null ? vehicleRb.linearVelocity : Vector3.zero;
            float speedKmh = vel.magnitude * 3.6f;

            // Rotate camera yaw toward vehicle heading if vehicle is moving
            if (speedKmh > 2.5f)
            {
                float vehicleYaw = vehicleTarget.eulerAngles.y;
                currentYaw = Mathf.LerpAngle(currentYaw, vehicleYaw, dt * rotationDamping);
            }

            Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 pivot = vehicleTarget.position + Vector3.up * followHeight;

            // Velocity look-ahead (flattened on Y)
            Vector3 lookAhead = vel * lookAheadMultiplier;
            lookAhead.y = 0f;

            Vector3 backDir = rot * -Vector3.forward;
            Vector3 desiredPosition = (pivot + lookAhead) + (backDir * followDistance);

            // Anti-clipping SphereCast against buildings and terrain
            Ray ray = new Ray(pivot, backDir);
            float resolvedDist = followDistance;
            if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, followDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                resolvedDist = Mathf.Clamp(hit.distance - 0.15f, minCameraDistance, followDistance);
                desiredPosition = (pivot + lookAhead) + (backDir * resolvedDist);
            }

            // Smooth position dampening
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

            // Look at pivot + forward target
            Vector3 lookTarget = pivot + lookAhead + rot * (Vector3.forward * 4.0f);
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

            // Speed-responsive dynamic FOV
            if (targetCamera != null)
            {
                float targetFOV = Mathf.Lerp(minFOV, maxFOV, Mathf.Clamp01(speedKmh / 56.0f));
                targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, dt * fovChangeSpeed);
            }
        }
    }
}
