using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.Survival
{
    public enum ThrowableItemType
    {
        DistractionBottle, // item_distraction_bottle
        Brick,             // item_brick
        SoundBomb          // trap_sound_bomb
    }

    /// <summary>
    /// Active Phase 4: Kinematic Parabolic Trajectory Arc & Throwable Distraction Physics.
    /// Projects a 28-segment live trajectory preview, spawns physical projectiles with
    /// URP instanced materials, triggers acoustic lures (28m), and delivers head-hit stuns (2.2s).
    /// </summary>
    public class ParabolicThrowController : MonoBehaviour
    {
        private static ParabolicThrowController _instance;
        public static ParabolicThrowController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ParabolicThrowController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ParabolicThrowController");
                        _instance = go.AddComponent<ParabolicThrowController>();
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

        [Header("Trajectory Parameters")]
        [SerializeField] private int segmentCount = 28;
        [SerializeField] private float timeStep = 0.045f; // Flight time simulation step
        [SerializeField] private float throwForce = 14.5f;
        [SerializeField] private float upwardForce = 4.2f;

        [Header("State")]
        [SerializeField] private bool isAimingThrow = false;
        [SerializeField] private ThrowableItemType activeThrowable = ThrowableItemType.DistractionBottle;

        private LineRenderer trajectoryLine;
        private GameObject impactMarker;
        private Renderer impactMarkerRenderer;

        private Transform playerTransform;
        private Camera mainCamera;

        public bool IsAimingThrow => isAimingThrow;
        public ThrowableItemType ActiveThrowable => activeThrowable;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            CreateTrajectoryVisuals();
        }

        private void Start()
        {
            LocatePlayerAndCamera();
            SetTrajectoryActive(false);
        }

        private void LocatePlayerAndCamera()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
                if (p != null) playerTransform = p.transform;
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void CreateTrajectoryVisuals()
        {
            // 1. LineRenderer for Parabolic Trajectory
            trajectoryLine = GetComponent<LineRenderer>();
            if (trajectoryLine == null) trajectoryLine = gameObject.AddComponent<LineRenderer>();

            trajectoryLine.positionCount = segmentCount;
            trajectoryLine.startWidth = 0.06f;
            trajectoryLine.endWidth = 0.025f;
            trajectoryLine.useWorldSpace = true;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material lineMat = new Material(unlitShader);
            lineMat.name = "Mat_Trajectory_Arc_Instance";
            lineMat.color = new Color(1.0f, 0.72f, 0.01f, 0.85f); // Amber glow
            lineMat.enableInstancing = true;
            trajectoryLine.sharedMaterial = lineMat;

            // 2. Impact Ground Ring Marker
            if (impactMarker == null)
            {
                impactMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                impactMarker.name = "Impact_Ground_Ring";
                impactMarker.transform.SetParent(transform);
                impactMarker.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);

                Collider col = impactMarker.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Material ringMat = new Material(unlitShader);
                ringMat.name = "Mat_Impact_Ring_Instance";
                ringMat.color = new Color(1.0f, 0.72f, 0.01f, 0.90f);
                ringMat.enableInstancing = true;

                impactMarkerRenderer = impactMarker.GetComponent<Renderer>();
                if (impactMarkerRenderer != null)
                {
                    impactMarkerRenderer.sharedMaterial = ringMat;
                    impactMarkerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        public void StartAiming(ThrowableItemType type)
        {
            LocatePlayerAndCamera();
            if (playerTransform == null) return;

            // Check if player has item
            string itemId = GetItemId(type);
            if (!InventoryManager.HasItem(itemId, 1))
            {
                ProceduralAudioManager.Instance?.PlaySoftError();
                GameEvents.TriggerObjectiveToastRequested($"Yetersiz malzeme: {GetItemDisplayName(type)} kalmadı!", 2.5f);
                return;
            }

            activeThrowable = type;
            isAimingThrow = true;
            SetTrajectoryActive(true);
            UpdateVisualColors();
        }

        public void CancelAiming()
        {
            isAimingThrow = false;
            SetTrajectoryActive(false);
        }

        public void ExecuteThrow()
        {
            if (!isAimingThrow) return;

            LocatePlayerAndCamera();
            if (playerTransform == null)
            {
                CancelAiming();
                return;
            }

            string itemId = GetItemId(activeThrowable);
            if (!InventoryManager.RemoveItem(itemId, 1))
            {
                CancelAiming();
                return;
            }

            Vector3 startPos = GetLaunchOrigin();
            Vector3 velocity = CalculateLaunchVelocity();

            // Trigger player throw animation
            PlayerAnimationBridge anim = playerTransform.GetComponent<PlayerAnimationBridge>();
            if (anim != null) anim.TriggerThrow();

            // Spawn 3D projectile
            SpawnThrowableProjectile(activeThrowable, startPos, velocity);

            ProceduralAudioManager.Instance?.PlayPush();
            MobileHapticManager.TriggerLight();

            CancelAiming();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // PC Hotkey Shortcuts for quick testing: Key 4 = Bottle, 5 = Brick, 6 = SoundBomb
            if (Input.GetKeyDown(KeyCode.Alpha4)) StartAiming(ThrowableItemType.DistractionBottle);
            if (Input.GetKeyDown(KeyCode.Alpha5)) StartAiming(ThrowableItemType.Brick);
            if (Input.GetKeyDown(KeyCode.Alpha6)) StartAiming(ThrowableItemType.SoundBomb);

            if (isAimingThrow)
            {
                SimulateAndRenderTrajectory();

                if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.Return))
                {
                    ExecuteThrow();
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    CancelAiming();
                }
            }
        }

        private void SimulateAndRenderTrajectory()
        {
            LocatePlayerAndCamera();
            if (playerTransform == null || trajectoryLine == null) return;

            Vector3 startPos = GetLaunchOrigin();
            Vector3 velocity = CalculateLaunchVelocity();

            Vector3 currentPos = startPos;
            int groundMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int pLayer = LayerMask.NameToLayer("Player");
            if (pLayer != -1) groundMask &= ~(1 << pLayer);

            Vector3 impactPoint = startPos + velocity;
            Vector3 impactNormal = Vector3.up;
            bool hitObstacle = false;
            int pointsDrawn = segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                trajectoryLine.SetPosition(i, currentPos);

                if (hitObstacle)
                {
                    // Collapse remaining points to impact point
                    trajectoryLine.SetPosition(i, impactPoint);
                    continue;
                }

                float t = (i + 1) * timeStep;
                Vector3 nextPos = startPos + (velocity * t) + (0.5f * Physics.gravity * t * t);
                Vector3 stepDir = nextPos - currentPos;
                float stepDist = stepDir.magnitude;

                if (stepDist > 0.001f && Physics.SphereCast(currentPos, 0.10f, stepDir.normalized, out RaycastHit hit, stepDist, groundMask))
                {
                    hitObstacle = true;
                    impactPoint = hit.point;
                    impactNormal = hit.normal;
                    pointsDrawn = i + 1;
                    trajectoryLine.SetPosition(i, impactPoint);
                }
                else
                {
                    currentPos = nextPos;
                }
            }

            // Position Ground Impact Ring
            if (impactMarker != null)
            {
                impactMarker.SetActive(hitObstacle);
                if (hitObstacle)
                {
                    impactMarker.transform.position = impactPoint + (impactNormal * 0.03f);
                    impactMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, impactNormal);
                }
            }
        }

        private Vector3 GetLaunchOrigin()
        {
            if (playerTransform == null) return Vector3.zero;

            Vector3 camFwd = (mainCamera != null) ? mainCamera.transform.forward : playerTransform.forward;
            return playerTransform.position + (Vector3.up * 1.35f) + (playerTransform.right * 0.35f) + (camFwd * 0.45f);
        }

        private Vector3 CalculateLaunchVelocity()
        {
            Vector3 forwardDir = (mainCamera != null) ? mainCamera.transform.forward : playerTransform.forward;
            return (forwardDir * throwForce) + (Vector3.up * upwardForce);
        }

        private void SetTrajectoryActive(bool active)
        {
            if (trajectoryLine != null) trajectoryLine.enabled = active;
            if (impactMarker != null) impactMarker.SetActive(active);
        }

        private void UpdateVisualColors()
        {
            Color targetColor = (activeThrowable == ThrowableItemType.SoundBomb)
                ? new Color(0.94f, 0.28f, 0.44f, 0.90f)  // Crimson Red
                : new Color(1.0f, 0.72f, 0.01f, 0.90f);  // Amber Gold

            if (trajectoryLine != null && trajectoryLine.sharedMaterial != null)
            {
                trajectoryLine.sharedMaterial.color = targetColor;
            }
            if (impactMarkerRenderer != null && impactMarkerRenderer.sharedMaterial != null)
            {
                impactMarkerRenderer.sharedMaterial.color = targetColor;
            }
        }

        private void SpawnThrowableProjectile(ThrowableItemType type, Vector3 pos, Vector3 vel)
        {
            GameObject proj = new GameObject($"Thrown_{type}");
            proj.transform.position = pos;

            SphereCollider col = proj.AddComponent<SphereCollider>();
            col.radius = 0.16f;

            Rigidbody rb = proj.AddComponent<Rigidbody>();
            rb.mass = (type == ThrowableItemType.Brick) ? 1.2f : 0.45f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearVelocity = vel;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;

            // Visual mesh
            BuildProjectileVisual(proj.transform, type);

            // Projectile script
            ThrowableDistractionProjectile pScript = proj.AddComponent<ThrowableDistractionProjectile>();
            pScript.Initialize(type);
        }

        private void BuildProjectileVisual(Transform parent, ThrowableItemType type)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(litShader);
            mat.enableInstancing = true;

            if (type == ThrowableItemType.Brick)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Visual_Brick";
                cube.transform.SetParent(parent, false);
                cube.transform.localScale = new Vector3(0.24f, 0.12f, 0.12f);
                mat.color = new Color(0.72f, 0.28f, 0.18f); // Red terracotta brick
                Collider c = cube.GetComponent<Collider>();
                if (c != null) Destroy(c);
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else if (type == ThrowableItemType.SoundBomb)
            {
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Visual_SoundBomb";
                cyl.transform.SetParent(parent, false);
                cyl.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
                mat.color = new Color(0.18f, 0.20f, 0.24f); // Matte dark casing with red band
                Collider c = cyl.GetComponent<Collider>();
                if (c != null) Destroy(c);
                cyl.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else
            {
                // Glass Bottle
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Visual_Bottle";
                cyl.transform.SetParent(parent, false);
                cyl.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
                mat.color = new Color(0.22f, 0.65f, 0.38f, 0.85f); // Green glass
                Collider c = cyl.GetComponent<Collider>();
                if (c != null) Destroy(c);
                cyl.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        public static string GetItemId(ThrowableItemType type)
        {
            switch (type)
            {
                case ThrowableItemType.Brick: return "item_brick";
                case ThrowableItemType.SoundBomb: return "trap_sound_bomb";
                default: return "item_distraction_bottle";
            }
        }

        public static string GetItemDisplayName(ThrowableItemType type)
        {
            switch (type)
            {
                case ThrowableItemType.Brick: return "Tuğla";
                case ThrowableItemType.SoundBomb: return "Ses Bombası";
                default: return "Dikkat Dağıtıcı Şişe";
            }
        }
    }

    /// <summary>
    /// Active Phase 4: Collision Handler for physical throwable distraction items.
    /// Handles zombie headshot stuns (2.2s), blunt damage, acoustic lure emission (28m),
    /// glass/brick particles, and MobileHaptic feedback.
    /// </summary>
    public class ThrowableDistractionProjectile : MonoBehaviour
    {
        private ThrowableItemType itemType;
        private bool hasImpacted = false;
        private float noiseRadius = 28.0f;

        public void Initialize(ThrowableItemType type)
        {
            itemType = type;
            noiseRadius = 28.0f;
            Destroy(gameObject, 6.0f); // Safety cleanup
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasImpacted) return;
            hasImpacted = true;

            Vector3 impactPos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            // 1. Direct Hit on Zombie
            ZombieAI hitZombie = collision.collider.GetComponentInParent<ZombieAI>();
            if (hitZombie != null && !hitZombie.IsDead)
            {
                float damage = (itemType == ThrowableItemType.Brick) ? 35f : 25f;
                hitZombie.TakeDamage(damage, impactPos, -collision.contacts[0].normal);

                // Direct Head / Upper Torso Hit Stun Bonus (2.2s)
                Vector3 zombieHead = hitZombie.transform.position + Vector3.up * 1.4f;
                if (Vector3.Distance(impactPos, zombieHead) <= 0.65f)
                {
                    hitZombie.Stun(2.2f);
                    GameEvents.TriggerObjectiveToastRequested("[KRİTİK İSABET] Zombi kafadan vuruldu ve 2.2 sn sersemledi!", 3.0f);
                }

                ProceduralAudioManager.Instance?.PlayArmorHit();
                MobileHapticManager.TriggerLight();
            }
            else
            {
                // 2. Impact on Ground / Surface
                if (itemType == ThrowableItemType.DistractionBottle)
                {
                    ProceduralAudioManager.Instance?.PlayGlassShatter();
                }
                else
                {
                    ProceduralAudioManager.Instance?.PlayArmorHit();
                }
                MobileHapticManager.TriggerLight();
            }

            // 3. Broadcast 28m Acoustic Lure
            HordeAlertSystem.Instance?.BroadcastAcousticLure(impactPos, noiseRadius);
            GameEvents.TriggerNoiseEmitted(impactPos, noiseRadius);

            // Turn nearby zombies towards noise source to investigate for 6 seconds
            var activeZombies = ZombieAI.ActiveZombies;
            if (activeZombies != null)
            {
                for (int i = 0; i < activeZombies.Count; i++)
                {
                    ZombieAI z = activeZombies[i];
                    if (z != null && !z.IsDead)
                    {
                        if (Vector3.Distance(z.transform.position, impactPos) <= noiseRadius)
                        {
                            z.OnNoiseDetected(impactPos);
                        }
                    }
                }
            }

            Destroy(gameObject, 0.25f);
        }
    }
}
