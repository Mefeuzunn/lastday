using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.Animation;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.AI
{
    public enum RaiderWeaponType
    {
        Pistol,
        Shotgun
    }

    public enum RaiderState
    {
        Patrol,
        AlertAndCallout,
        CoverAndPeek,
        ZombieDefense
    }

    /// <summary>
    /// Phase 2 (Step 2A): Cover-Seeking Armed Human Raider AI.
    /// Features 4 tactical states (Patrol, Alert & Callout, Cover & Peek-Fire, Cross-Faction Zombie Defense).
    /// Uses 3D positional audio (spatialBlend = 1.0, 2m-38m range), non-allocating physics buffers,
    /// HumanoidProceduralAnimator kinematics, and converts to a lootable corpse upon elimination.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class HumanRaiderAI : MonoBehaviour
    {
        public static readonly List<HumanRaiderAI> ActiveRaiders = new List<HumanRaiderAI>();

        [Header("Squad & Identity")]
        [SerializeField] private string squadId = "Squad_RailYard_East";
        [SerializeField] private string raiderName = "Yağmacı Nişancı";
        [SerializeField] private RaiderWeaponType weaponType = RaiderWeaponType.Pistol;

        [Header("Survival & Health")]
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private float currentHealth = 80f;
        [SerializeField] private bool isDead = false;

        [Header("Locomotion Speeds")]
        [SerializeField] private float patrolSpeed = 2.2f;
        [SerializeField] private float sprintSpeed = 4.6f;

        [Header("Vision & Perception")]
        [SerializeField] private float visionConeAngle = 65.0f;
        [SerializeField] private float visionRangeBase = 26.0f;
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float squadAlertRadius = 22.0f;

        [Header("Combat & Firepower")]
        [SerializeField] private float weaponDamage = 14.0f;
        [SerializeField] private float peekFireDuration = 1.2f;
        [SerializeField] private float crouchCoverMinTime = 1.8f;
        [SerializeField] private float crouchCoverMaxTime = 2.6f;
        [SerializeField] private float acousticGunfireRadius = 32.0f;
        [SerializeField] private float dispersionAngle = 5.0f;

        [Header("Patrol Waypoints")]
        [SerializeField] private List<Vector3> patrolWaypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;

        [Header("Live State Monitor")]
        [SerializeField] private RaiderState currentState = RaiderState.Patrol;
        [SerializeField] private bool isAiming = false;
        [SerializeField] private bool isCrouched = false;
        [SerializeField] private Transform currentThreatTarget;

        // Non-allocating physics buffers
        private static readonly RaycastHit[] s_VisionRayHits = new RaycastHit[16];
        private static readonly Collider[] s_ZombieScanBuffer = new Collider[16];
        private static readonly RaycastHit[] s_GunfireRayHits = new RaycastHit[8];

        // Component References
        private NavMeshAgent agent;
        private CapsuleCollider capsuleCollider;
        private HumanoidProceduralAnimator humanoidAnimator;
        private AudioSource audioSource;
        private WorldSpeechBubbleUI speechBubble;
        private Transform visualModel;
        private Light muzzleFlashLight;
        private LineRenderer tracerRenderer;

        // Timers & State Variables
        private float stateTimer = 0f;
        private float scanTimer = 0f;
        private float peekTimer = 0f;
        private float coverDuration = 2.0f;
        private bool isPeekingToFire = false;
        private Vector3 currentCoverPosition;
        private float lastShotTime = 0f;
        private int burstShotsRemaining = 0;
        private float burstTimer = 0f;

        // Properties
        public string SquadId => squadId;
        public string RaiderName => raiderName;
        public RaiderWeaponType WeaponType => weaponType;
        public RaiderState CurrentState => currentState;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsAiming => isAiming;
        public bool IsCrouched => isCrouched;
        public Transform CurrentTarget => currentThreatTarget;

        private static readonly string[] TacticalBarksAlert = new string[]
        {
            "Burada biri var! Siper alın!",
            "Ateş serbest! Hepsini indirin!",
            "Hedef görüldü! Siper bulun!"
        };

        private static readonly string[] TacticalBarksZombie = new string[]
        {
            "Zombiler yaklaştı! Önce onları vurun!",
            "Yaklaşma lanet olası yaratık!"
        };

        private static readonly string[] TacticalBarksCorpseFound = new string[]
        {
            "Biri bizim adamı indirmiş! Tetikte olun!",
            "Düşman yakında! Yoldaşımız vurulmuş!"
        };

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            humanoidAnimator = GetComponent<HumanoidProceduralAnimator>();

            SetupAudioSource();
            SetupTracerAndMuzzle();

            if (weaponType == RaiderWeaponType.Shotgun)
            {
                maxHealth = 100f;
                weaponDamage = 22f;
            }
            currentHealth = maxHealth;
        }

        private void OnEnable()
        {
            if (!ActiveRaiders.Contains(this))
            {
                ActiveRaiders.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveRaiders.Remove(this);
        }

        private void Start()
        {
            visualModel = transform.Find("Visual_Model");
            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }

            if (patrolWaypoints.Count == 0)
            {
                // Generate a local 3-point triangle patrol route around initial position
                Vector3 origin = transform.position;
                patrolWaypoints.Add(origin);
                patrolWaypoints.Add(origin + transform.forward * 8f + transform.right * 4f);
                patrolWaypoints.Add(origin + transform.forward * 8f - transform.right * 4f);
            }

            TransitionToState(RaiderState.Patrol);
        }

        private void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1.0f; // Pure 3D positional audio
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 38.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        private void SetupTracerAndMuzzle()
        {
            // Muzzle flash light
            GameObject flashObj = new GameObject("RaiderMuzzleLight");
            flashObj.transform.SetParent(transform, false);
            flashObj.transform.localPosition = new Vector3(0.35f, 1.4f, 0.7f);
            muzzleFlashLight = flashObj.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.range = 5.0f;
            muzzleFlashLight.color = new Color(1f, 0.75f, 0.35f);
            muzzleFlashLight.intensity = 0f;
            muzzleFlashLight.shadows = LightShadows.None; // Zero shadow cost for mobile
            muzzleFlashLight.enabled = false;

            // Tracer line
            GameObject tracerObj = new GameObject("RaiderTracerLine");
            tracerObj.transform.SetParent(transform, false);
            tracerRenderer = tracerObj.AddComponent<LineRenderer>();
            tracerRenderer.useWorldSpace = true;
            tracerRenderer.positionCount = 2;
            tracerRenderer.startWidth = 0.04f;
            tracerRenderer.endWidth = 0.02f;

            Shader tracerShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (tracerShader != null)
            {
                Material m = new Material(tracerShader);
                m.enableInstancing = true;
                Color col = new Color(1f, 0.85f, 0.35f, 0.85f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                tracerRenderer.material = m;
                tracerRenderer.startColor = col;
                tracerRenderer.endColor = col;
            }
            tracerRenderer.enabled = false;
        }

        public void SetPatrolWaypoints(List<Vector3> waypoints)
        {
            patrolWaypoints = new List<Vector3>(waypoints);
            currentWaypointIndex = 0;
        }

        private void Update()
        {
            if (isDead) return;

            float dt = Time.deltaTime;
            stateTimer += dt;
            scanTimer += dt;

            // Continuous Cross-Faction Zombie Detection (<10m)
            if (scanTimer >= 0.25f)
            {
                scanTimer = 0f;
                CheckZombieProximityAndSquadCorpses();
            }

            switch (currentState)
            {
                case RaiderState.Patrol:
                    UpdatePatrol(dt);
                    break;

                case RaiderState.AlertAndCallout:
                    UpdateAlertAndCallout(dt);
                    break;

                case RaiderState.CoverAndPeek:
                    UpdateCoverAndPeek(dt);
                    break;

                case RaiderState.ZombieDefense:
                    UpdateZombieDefense(dt);
                    break;
            }
        }

        #region Tactical States

        private void TransitionToState(RaiderState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case RaiderState.Patrol:
                    isAiming = false;
                    isCrouched = false;
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.speed = patrolSpeed;
                        if (patrolWaypoints.Count > 0)
                        {
                            agent.SetDestination(patrolWaypoints[currentWaypointIndex]);
                        }
                    }
                    break;

                case RaiderState.AlertAndCallout:
                    isAiming = true;
                    isCrouched = false;
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    break;

                case RaiderState.CoverAndPeek:
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.speed = sprintSpeed;
                        agent.SetDestination(currentCoverPosition);
                    }
                    isPeekingToFire = false;
                    isCrouched = false;
                    coverDuration = Random.Range(crouchCoverMinTime, crouchCoverMaxTime);
                    peekTimer = 0f;
                    break;

                case RaiderState.ZombieDefense:
                    isAiming = true;
                    isCrouched = false;
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                    break;
            }
        }

        private void UpdatePatrol(float dt)
        {
            // 1. Patrol navigation between waypoints
            if (patrolWaypoints.Count > 0 && agent.isOnNavMesh)
            {
                float dist = agent.remainingDistance;
                if (!agent.pathPending && dist <= agent.stoppingDistance + 0.4f)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
                    agent.SetDestination(patrolWaypoints[currentWaypointIndex]);
                }
            }

            // 2. Scan vision cone for player
            Transform player = GetPlayerTransform();
            if (player != null && CanSeeTarget(player))
            {
                AlertRaider(player, TacticalBarksAlert[Random.Range(0, TacticalBarksAlert.Length)]);
            }
        }

        private void UpdateAlertAndCallout(float dt)
        {
            // Face the threat briefly while calling out, then sprint to cover
            if (currentThreatTarget != null)
            {
                FacePosition(currentThreatTarget.position, dt * 10f);
            }

            if (stateTimer >= 0.65f)
            {
                Vector3 targetPos = currentThreatTarget != null ? currentThreatTarget.position : (transform.position + transform.forward * 12f);
                if (RaiderCoverEvaluator.TryFindCover(transform.position, targetPos, 14f, out Vector3 coverPos))
                {
                    currentCoverPosition = coverPos;
                }
                else
                {
                    currentCoverPosition = transform.position - (targetPos - transform.position).normalized * 5f;
                }

                TransitionToState(RaiderState.CoverAndPeek);
            }
        }

        private void UpdateCoverAndPeek(float dt)
        {
            if (currentThreatTarget == null)
            {
                currentThreatTarget = GetPlayerTransform();
                if (currentThreatTarget == null)
                {
                    TransitionToState(RaiderState.Patrol);
                    return;
                }
            }

            Vector3 threatPos = currentThreatTarget.position;
            float distToCover = Vector3.Distance(transform.position, currentCoverPosition);

            // Sprinting to cover
            if (agent.isOnNavMesh && agent.remainingDistance > agent.stoppingDistance + 0.35f && distToCover > 1.2f)
            {
                isCrouched = false;
                isAiming = false;
                return;
            }

            // Reached cover point — alternate between crouching and peek-firing
            peekTimer += dt;

            if (!isPeekingToFire)
            {
                // Crouched Behind Cover
                isCrouched = true;
                isAiming = false;

                if (peekTimer >= coverDuration)
                {
                    // Switch to Standing Peek-Fire
                    isPeekingToFire = true;
                    peekTimer = 0f;
                    isCrouched = false;
                    isAiming = true;
                    burstShotsRemaining = (weaponType == RaiderWeaponType.Shotgun) ? 1 : Random.Range(2, 4);
                    burstTimer = 0.25f; // Short aim telegraph before firing
                }
            }
            else
            {
                // Standing Peek-Fire
                FacePosition(threatPos, dt * 12f);

                burstTimer -= dt;
                if (burstTimer <= 0f && burstShotsRemaining > 0)
                {
                    burstTimer = (weaponType == RaiderWeaponType.Shotgun) ? 0.6f : 0.22f;
                    burstShotsRemaining--;
                    ExecuteGunfireShot(threatPos);
                }

                if (peekTimer >= peekFireDuration)
                {
                    // Switch back to Crouched Behind Cover
                    isPeekingToFire = false;
                    peekTimer = 0f;
                    isCrouched = true;
                    isAiming = false;
                    coverDuration = Random.Range(crouchCoverMinTime, crouchCoverMaxTime);

                    // Re-evaluate cover if target moved too close (<7m)
                    if (Vector3.Distance(transform.position, threatPos) < 7.0f)
                    {
                        if (RaiderCoverEvaluator.TryFindCover(transform.position, threatPos, 14f, out Vector3 newCover))
                        {
                            currentCoverPosition = newCover;
                            if (agent.isOnNavMesh)
                            {
                                agent.isStopped = false;
                                agent.speed = sprintSpeed;
                                agent.SetDestination(currentCoverPosition);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateZombieDefense(float dt)
        {
            if (currentThreatTarget == null)
            {
                TransitionToState(RaiderState.Patrol);
                return;
            }

            // Check if target zombie is still alive and nearby
            var zombieBrain = currentThreatTarget.GetComponent<ZombieBrain>();
            if (zombieBrain != null && zombieBrain.IsDead)
            {
                currentThreatTarget = null;
                Transform player = GetPlayerTransform();
                if (player != null && CanSeeTarget(player))
                {
                    AlertRaider(player, "Zombi geberdi! Hedefe dönün!");
                }
                else
                {
                    TransitionToState(RaiderState.Patrol);
                }
                return;
            }

            Vector3 zPos = currentThreatTarget.position;
            FacePosition(zPos, dt * 14f);

            // Fire at zombie
            burstTimer -= dt;
            if (burstTimer <= 0f)
            {
                burstTimer = (weaponType == RaiderWeaponType.Shotgun) ? 0.7f : 0.35f;
                ExecuteGunfireShot(zPos);
            }

            // If zombie reached beyond 14m, disengage
            if (Vector3.Distance(transform.position, zPos) > 14.0f)
            {
                currentThreatTarget = null;
                TransitionToState(RaiderState.Patrol);
            }
        }

        #endregion

        #region Cross-Faction Detection & Vision

        private void CheckZombieProximityAndSquadCorpses()
        {
            // 1. Cross-Faction Zombie Threat Check (<10m)
            int count = Physics.OverlapSphereNonAlloc(transform.position, 10.0f, s_ZombieScanBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = s_ZombieScanBuffer[i];
                if (col == null) continue;

                var zb = col.GetComponentInParent<ZombieBrain>();
                if (zb != null && !zb.IsDead)
                {
                    if (currentState != RaiderState.ZombieDefense)
                    {
                        currentThreatTarget = zb.transform;
                        SaySpeech(TacticalBarksZombie[Random.Range(0, TacticalBarksZombie.Length)]);
                        TransitionToState(RaiderState.ZombieDefense);
                        return;
                    }
                }
            }

            // 2. Discover Fallen Squadmates in line-of-sight (<12m)
            if (currentState == RaiderState.Patrol)
            {
                for (int i = 0; i < ActiveRaiders.Count; i++)
                {
                    var r = ActiveRaiders[i];
                    if (r != null && r != this && r.SquadId == squadId && r.IsDead)
                    {
                        float dist = Vector3.Distance(transform.position, r.transform.position);
                        if (dist <= 12.0f && CanSeePosition(r.transform.position + Vector3.up * 0.5f))
                        {
                            Transform player = GetPlayerTransform();
                            AlertRaider(player, TacticalBarksCorpseFound[Random.Range(0, TacticalBarksCorpseFound.Length)]);
                            break;
                        }
                    }
                }
            }
        }

        public bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 targetCenter = target.position + Vector3.up * 1.0f;
            Vector3 toTarget = targetCenter - eyePos;
            float dist = toTarget.magnitude;

            // Scale vision range with player's dynamic stealth exposure
            float effectiveRange = visionRangeBase;
            if (StealthVisibilityController.Instance != null)
            {
                effectiveRange *= StealthVisibilityController.Instance.GetVisionDistanceMultiplier();
            }

            if (dist > effectiveRange) return false;

            // Angle check
            Vector3 forward = transform.forward;
            Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            float angle = Vector3.Angle(forward, flatDir);

            if (angle > visionConeAngle * 0.5f) return false;

            // Line of sight raycast
            return CanSeePosition(targetCenter);
        }

        private bool CanSeePosition(Vector3 targetPos)
        {
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 dir = targetPos - eyePos;
            float dist = dir.magnitude;
            if (dist < 0.1f) return true;

            int mask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int npcLayer = LayerMask.NameToLayer("NPC");
            if (npcLayer != -1) mask &= ~(1 << npcLayer);

            int hits = Physics.RaycastNonAlloc(eyePos, dir.normalized, s_VisionRayHits, dist - 0.1f, mask, QueryTriggerInteraction.Ignore);
            return hits == 0;
        }

        #endregion

        #region Combat & Gunfire Execution

        private void ExecuteGunfireShot(Vector3 targetCenter)
        {
            lastShotTime = Time.time;

            Vector3 muzzlePos = transform.position + transform.forward * 0.45f + Vector3.up * 1.35f;

            // Apply accuracy dispersion
            Vector3 dirToTarget = (targetCenter - muzzlePos).normalized;
            Vector3 spread = Random.insideUnitSphere * Mathf.Tan(dispersionAngle * Mathf.Deg2Rad);
            Vector3 fireDir = (dirToTarget + spread).normalized;

            // 1. Audio: 3D Positional Gunfire
            PlayGunfireAudio();

            // 2. Visual: Muzzle Flash & Tracer
            StartCoroutine(FlashMuzzle());
            SpawnTracer(muzzlePos, muzzlePos + fireDir * 35f);

            // 3. Recoil Kick on Animator
            if (humanoidAnimator != null)
            {
                humanoidAnimator.TriggerRecoil(weaponType == RaiderWeaponType.Shotgun ? 0.16f : 0.09f, 10f);
            }

            // 4. Acoustic Noise Broadcast (32m)
            GameEvents.TriggerNoiseEmitted(transform.position, acousticGunfireRadius);
            HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, acousticGunfireRadius);

            // 5. Physics Raycast Damage
            int mask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int hits = Physics.RaycastNonAlloc(muzzlePos, fireDir, s_GunfireRayHits, 45f, mask, QueryTriggerInteraction.Ignore);

            if (hits > 0)
            {
                // Find closest hit
                float closestDist = float.MaxValue;
                RaycastHit closestHit = default;
                for (int h = 0; h < hits; h++)
                {
                    if (s_GunfireRayHits[h].collider != null && s_GunfireRayHits[h].distance < closestDist)
                    {
                        // Ignore self
                        if (s_GunfireRayHits[h].collider.transform.IsChildOf(transform)) continue;

                        closestDist = s_GunfireRayHits[h].distance;
                        closestHit = s_GunfireRayHits[h];
                    }
                }

                if (closestHit.collider != null)
                {
                    // Check Player Damage
                    var playerStats = closestHit.collider.GetComponentInParent<SurvivalStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(weaponDamage, transform.position);
                        ProceduralAudioManager.Instance?.PlayHitImpact(false);
                    }

                    // Check Zombie Damage (Cross-Faction)
                    var zb = closestHit.collider.GetComponentInParent<ZombieBrain>();
                    if (zb != null && !zb.IsDead)
                    {
                        zb.TakeDamage(weaponDamage, closestHit.point, closestHit.normal);
                        ProceduralAudioManager.Instance?.PlayHitImpact(false);
                    }
                }
            }
        }

        private void PlayGunfireAudio()
        {
            if (audioSource == null) return;

            float dur = (weaponType == RaiderWeaponType.Shotgun) ? 0.28f : 0.18f;
            AudioClip clip = ProceduralAudioManager.Instance != null
                ? ProceduralAudioManager.Instance.GenerateGunfireClip(weaponType == RaiderWeaponType.Shotgun)
                : null;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.95f);
            }
            else
            {
                ProceduralAudioManager.Instance?.PlayGunfire(weaponType == RaiderWeaponType.Shotgun, 0.85f);
            }
        }

        private IEnumerator FlashMuzzle()
        {
            if (muzzleFlashLight == null) yield break;

            muzzleFlashLight.enabled = true;
            muzzleFlashLight.intensity = (weaponType == RaiderWeaponType.Shotgun) ? 4.2f : 2.8f;
            yield return new WaitForSeconds(0.045f);
            muzzleFlashLight.intensity = 0f;
            muzzleFlashLight.enabled = false;
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            if (tracerRenderer == null) return;
            StartCoroutine(AnimateTracer(start, end));
        }

        private IEnumerator AnimateTracer(Vector3 start, Vector3 end)
        {
            tracerRenderer.SetPosition(0, start);
            tracerRenderer.SetPosition(1, end);
            tracerRenderer.enabled = true;
            yield return new WaitForSeconds(0.05f);
            tracerRenderer.enabled = false;
        }

        #endregion

        #region Alert, Damage & Defeat

        public void AlertRaider(Transform threat, string barkMessage = null)
        {
            if (isDead) return;

            currentThreatTarget = threat;

            if (!string.IsNullOrEmpty(barkMessage))
            {
                SaySpeech(barkMessage);
            }

            // Alert squadmates within squadAlertRadius (22m)
            if (RaiderSquadDirector.Instance != null)
            {
                RaiderSquadDirector.Instance.AlertSquad(squadId, threat, this, squadAlertRadius);
            }

            if (currentState == RaiderState.Patrol)
            {
                TransitionToState(RaiderState.AlertAndCallout);
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitDir = default)
        {
            if (isDead) return;

            currentHealth -= amount;
            ProceduralAudioManager.Instance?.PlayDamageSound();

            // Auto-alert if shot by player from stealth
            if (currentState == RaiderState.Patrol)
            {
                Transform player = GetPlayerTransform();
                AlertRaider(player, "Ateş altındayız! Karşılık verin!");
            }

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else
            {
                // Flinch recoil
                if (humanoidAnimator != null)
                {
                    humanoidAnimator.TriggerRecoil(0.12f, 8f);
                }
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            ActiveRaiders.Remove(this);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Disable standard capsule collider so player/bullets pass through freely,
            // but attach/enable RaiderLootCorpse trigger collider
            if (capsuleCollider != null)
            {
                capsuleCollider.isTrigger = true;
                capsuleCollider.height = 0.5f;
                capsuleCollider.center = new Vector3(0f, 0.25f, 0f);
            }

            // Attach & activate RaiderLootCorpse
            RaiderLootCorpse corpse = GetComponent<RaiderLootCorpse>();
            if (corpse == null)
            {
                corpse = gameObject.AddComponent<RaiderLootCorpse>();
            }
            corpse.ConfigureLoot(weaponType == RaiderWeaponType.Shotgun);

            // Register with BodyConcealmentSystem for dragging into foliage/dumpsters
            if (BodyConcealmentSystem.Instance != null)
            {
                BodyConcealmentSystem.Instance.RegisterCorpse(gameObject);
            }

            // Inform squad of fallen member
            if (RaiderSquadDirector.Instance != null)
            {
                RaiderSquadDirector.Instance.NotifySquadmateDown(squadId, transform.position);
            }

            // Relax visuals into corpse pose (collapse on back)
            if (visualModel != null)
            {
                visualModel.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                visualModel.localPosition = new Vector3(0f, 0.15f, -0.4f);
            }

            SaySpeech("Argh...", 1.2f);
        }

        private void SaySpeech(string text, float duration = 2.4f)
        {
            if (speechBubble == null)
            {
                GameObject bObj = new GameObject($"SpeechBubble_{name}");
                bObj.transform.position = transform.position + Vector3.up * 2.15f;
                speechBubble = bObj.AddComponent<WorldSpeechBubbleUI>();
                speechBubble.BindTarget(transform);
            }
            speechBubble.ShowSpeech(text, duration);
        }

        private void FacePosition(Vector3 worldPos, float turnSpeed)
        {
            Vector3 dir = worldPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        private Transform GetPlayerTransform()
        {
            GameObject p = GameObject.FindWithTag("Player");
            return p != null ? p.transform : null;
        }

        #endregion
    }
}
