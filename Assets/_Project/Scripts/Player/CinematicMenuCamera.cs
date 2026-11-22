using System;
using System.Collections;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Phase 4: Seamless Real-Time Cinematic Main Menu & Camera Swoop Transition.
    /// Drives a cinematic multi-waypoint spline orbit across the urban backdrop (Safe House neon lantern,
    /// misty central avenue, emergency cruisers, skyscraper skyline) during GameState.MainMenu,
    /// then executes a 1.2s smooth swoop behind the player's shoulder on game start/continue.
    /// </summary>
    public class CinematicMenuCamera : MonoBehaviour
    {
        private static CinematicMenuCamera _instance;
        public static CinematicMenuCamera Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CinematicMenuCamera>();
                    if (_instance == null)
                    {
                        Camera mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            _instance = mainCam.gameObject.AddComponent<CinematicMenuCamera>();
                        }
                        else
                        {
                            GameObject go = new GameObject("CinematicMenuCamera");
                            _instance = go.AddComponent<CinematicMenuCamera>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [System.Serializable]
        public struct CameraWaypoint
        {
            public Vector3 position;
            public Vector3 lookTarget;
            public float fieldOfView;

            public CameraWaypoint(Vector3 pos, Vector3 look, float fov)
            {
                position = pos;
                lookTarget = look;
                fieldOfView = fov;
            }
        }

        [Header("Cinematic Orbit Waypoints")]
        [SerializeField] private CameraWaypoint[] waypoints;
        [SerializeField] private float orbitLoopSpeed = 0.035f; // Full loop cycle duration ~28s

        [Header("Swoop Settings")]
        [SerializeField] private float defaultSwoopDuration = 1.2f;

        private Camera targetCamera;
        private bool isCinematicActive = false;
        private bool isSwooping = false;
        private float loopProgress = 0f;
        private Coroutine swoopCoroutine;

        public bool IsCinematicActive => isCinematicActive;
        public bool IsSwooping => isSwooping;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            InitializeDefaultWaypoints();
            StartCinematicOrbit();
        }

        private void InitializeDefaultWaypoints()
        {
            waypoints = new CameraWaypoint[]
            {
                // WP 0: Hero Vehicle & SafeHouse Vista (Framing pickup truck with warm SafeHouse lantern in background)
                new CameraWaypoint(new Vector3(8f, 2.8f, -13.5f), new Vector3(12.5f, 1.2f, -8f), 52f),

                // WP 1: SafeHouse Front Porch Vista (Framing fortified compound, barricades, warm lantern and entrance)
                new CameraWaypoint(new Vector3(-6f, 2.6f, -12f), new Vector3(0f, 1.8f, -2f), 55f),

                // WP 2: Elevated Boulevard Sweep (Cinematic slow panning vista across the central quarantine street)
                new CameraWaypoint(new Vector3(2f, 3.8f, -13f), new Vector3(14f, 1.5f, -6f), 56f),

                // WP 3: Compound Courtyard Angle (Atmospheric low angle looking past streetlights toward SafeHouse)
                new CameraWaypoint(new Vector3(10f, 3.2f, -4f), new Vector3(1f, 1.6f, -1f), 54f)
            };
        }

        private void LateUpdate()
        {
            if (!isCinematicActive || isSwooping || targetCamera == null) return;

            // Advance orbital spline progress using unscaled delta time
            loopProgress = (loopProgress + Time.unscaledDeltaTime * orbitLoopSpeed) % 1.0f;
            UpdateSplinePose(loopProgress);
        }

        private void UpdateSplinePose(float progress)
        {
            if (waypoints == null || waypoints.Length == 0) return;

            int count = waypoints.Length;
            float total = progress * count;
            int idx0 = Mathf.FloorToInt(total) % count;
            int idx1 = (idx0 + 1) % count;
            float t = total - Mathf.Floor(total);

            // Smooth ease between waypoints
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Catmull-Rom or smooth Hermite interpolation across 4 nodes
            int p0 = (idx0 - 1 + count) % count;
            int p1 = idx0;
            int p2 = idx1;
            int p3 = (idx1 + 1) % count;

            Vector3 camPos = CatmullRom(waypoints[p0].position, waypoints[p1].position, waypoints[p2].position, waypoints[p3].position, t);
            Vector3 lookTarget = CatmullRom(waypoints[p0].lookTarget, waypoints[p1].lookTarget, waypoints[p2].lookTarget, waypoints[p3].lookTarget, t);
            float fov = Mathf.Lerp(waypoints[p1].fieldOfView, waypoints[p2].fieldOfView, smoothT);

            // Subtle gentle breathing micro-oscillation for alive cinematic feel
            float swayX = Mathf.Sin(Time.unscaledTime * 0.45f) * 0.12f;
            float swayY = Mathf.Cos(Time.unscaledTime * 0.35f) * 0.08f;
            camPos += new Vector3(swayX, swayY, 0f);

            transform.position = camPos;
            if ((lookTarget - camPos).sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookTarget - camPos, Vector3.up);
            }

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = fov;
            }
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * (t * t) +
                (-p0 + 3f * p1 - 3f * p2 + p3) * (t * t * t)
            );
        }

        public void StartCinematicOrbit()
        {
            enabled = true;
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;

            ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? FindFirstObjectByType<ThirdPersonCamera>();
            if (tpc != null)
            {
                tpc.enabled = false;
            }

            isCinematicActive = true;
            isSwooping = false;
            if (swoopCoroutine != null)
            {
                StopCoroutine(swoopCoroutine);
                swoopCoroutine = null;
            }

            if (waypoints == null || waypoints.Length == 0)
            {
                InitializeDefaultWaypoints();
            }

            UpdateSplinePose(loopProgress);
        }

        public void StopCinematicOrbit()
        {
            isCinematicActive = false;
            isSwooping = false;
            if (swoopCoroutine != null)
            {
                StopCoroutine(swoopCoroutine);
                swoopCoroutine = null;
            }
            enabled = false;
        }

        public void SwoopToPlayer(Transform targetPlayer, float duration = -1f, Action onComplete = null)
        {
            if (duration <= 0f) duration = defaultSwoopDuration;

            if (targetPlayer == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) targetPlayer = player.transform;
            }

            if (swoopCoroutine != null) StopCoroutine(swoopCoroutine);
            swoopCoroutine = StartCoroutine(SwoopRoutine(targetPlayer, duration, onComplete));
        }

        private IEnumerator SwoopRoutine(Transform targetPlayer, float duration, Action onComplete)
        {
            isSwooping = true;
            isCinematicActive = false;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFOV = targetCamera != null ? targetCamera.fieldOfView : 60f;
            float targetFOV = 60f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);

                // Smooth cinematic cubic ease-in-out curve
                float t = Mathf.SmoothStep(0f, 1f, rawT);

                // Compute player shoulder target pose at current frame (even if player rotates/settles)
                Vector3 playerPivot = (targetPlayer != null) ? targetPlayer.position : Vector3.zero;
                Vector3 forward = (targetPlayer != null) ? targetPlayer.forward : Vector3.forward;
                Vector3 right = (targetPlayer != null) ? targetPlayer.right : Vector3.right;

                Vector3 headPos = playerPivot + Vector3.up * 1.65f;
                Vector3 camOffset = -(forward * 3.2f) + (right * 0.65f);
                float desiredDist = camOffset.magnitude;
                Vector3 camDir = camOffset.normalized;
                if (Physics.SphereCast(headPos, 0.2f, camDir, out RaycastHit wallHit, desiredDist, ~LayerMask.GetMask("Player", "UI", "Ignore Raycast"), QueryTriggerInteraction.Ignore))
                {
                    camOffset = camDir * Mathf.Max(0.35f, wallHit.distance - 0.15f);
                }
                Vector3 endPos = headPos + camOffset;
                Quaternion endRot = Quaternion.LookRotation(headPos + (forward * 3f) - endPos, Vector3.up);

                // Subtle arc curve (lift camera up slightly during the flight)
                float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.2f;
                Vector3 currentTargetPos = Vector3.Lerp(startPos, endPos, t) + Vector3.up * arcHeight;

                transform.position = currentTargetPos;
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);

                if (targetCamera != null)
                {
                    targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                }

                yield return null;
            }

            // Final snap to exact shoulder position
            if (targetPlayer != null)
            {
                Vector3 headPos = targetPlayer.position + Vector3.up * 1.65f;
                Vector3 camOffset = -(targetPlayer.forward * 3.2f) + (targetPlayer.right * 0.65f);
                float desiredDist = camOffset.magnitude;
                Vector3 camDir = camOffset.normalized;
                if (Physics.SphereCast(headPos, 0.2f, camDir, out RaycastHit wallHit, desiredDist, ~LayerMask.GetMask("Player", "UI", "Ignore Raycast"), QueryTriggerInteraction.Ignore))
                {
                    camOffset = camDir * Mathf.Max(0.35f, wallHit.distance - 0.15f);
                }
                Vector3 endPos = headPos + camOffset;
                transform.position = endPos;
                transform.rotation = Quaternion.LookRotation(headPos + (targetPlayer.forward * 3f) - endPos, Vector3.up);
            }

            if (targetCamera != null) targetCamera.fieldOfView = targetFOV;

            isSwooping = false;
            isCinematicActive = false;

            // Seamless handover to ThirdPersonCamera
            ThirdPersonCamera tpc = ThirdPersonCamera.Instance;
            if (tpc != null)
            {
                tpc.SyncRotationToCurrent();
                tpc.enabled = true;
            }

            // Explicitly disable CinematicMenuCamera so it never updates Camera.main during gameplay
            enabled = false;

            onComplete?.Invoke();
            swoopCoroutine = null;
        }
    }
}
