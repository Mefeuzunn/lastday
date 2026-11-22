using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.AI
{
    public enum ZombieArchetype
    {
        LoneRoamer,
        Scavenger,
        DormantLurker
    }

    public enum AwarenessState
    {
        Unaware,
        Suspicious,
        Alerted
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieBrain : MonoBehaviour, IPooledObject
    {
        [Header("Zombie Archetype & Type")]
        [SerializeField] protected ZombieArchetype archetype = ZombieArchetype.LoneRoamer;
        [SerializeField] protected ZombieType zombieType = ZombieType.Standard;

        [Header("Awareness & State")]
        [SerializeField] protected AwarenessState awarenessState = AwarenessState.Unaware;
        [SerializeField] protected ZombieState currentState = ZombieState.Patrol;

        [Header("Health & Damage")]
        [SerializeField] protected float maxHealth = 60f;
        [SerializeField] protected float currentHealth = 60f;
        [SerializeField] protected float attackDamage = 14f;
        [SerializeField] protected float infectionInflicted = 8f;
        [SerializeField] protected float attackCooldown = 1.25f;
        [SerializeField] protected float attackRange = 1.65f;

        [Header("Speeds per Type")]
        [SerializeField] protected float patrolSpeed = 1.8f;
        [SerializeField] protected float investigateSpeed = 2.2f;
        [SerializeField] protected float chaseSpeed = 4.6f;

        [Header("Waypoints & Roaming")]
        [SerializeField] protected List<Transform> patrolWaypoints = new List<Transform>();
        [SerializeField] protected float waypointTolerance = 1.2f;
        [SerializeField] protected float patrolRadius = 28f;

        protected NavMeshAgent agent;
        protected ZombieLocomotionEngine locomotionEngine;
        protected ZombieSensorySystem sensorySystem;
        protected ZombieAnimationBridge animationBridge;
        protected Rigidbody rb;
        protected Collider col;

        protected Transform chaseTarget;
        protected Vector3 investigatePosition;
        protected Vector3 initialSpawnPosition;
        protected Quaternion initialSpawnRotation;

        protected ZombiePack assignedPack;
        protected bool isPackLeader = false;
        protected ProjectSurvival.Environment.InteractiveDoor currentDoorTarget;

        public NavMeshAgent NavAgent => agent != null ? agent : (agent = GetComponent<NavMeshAgent>());
        public ZombieLocomotionEngine LocomotionEngine => locomotionEngine != null ? locomotionEngine : (locomotionEngine = GetComponent<ZombieLocomotionEngine>());
        public ZombiePack AssignedPack => assignedPack;
        public bool IsPackLeader => isPackLeader;

        public void SetPack(ZombiePack pack)
        {
            assignedPack = pack;
        }

        public void SetPackLeader(bool isLeader)
        {
            isPackLeader = isLeader;
        }

        protected float stateTimer = 0f;
        protected float lastAttackTime = 0f;
        protected float chaseDestinationTimer = 0f;
        protected float stunTimer = 0f;
        protected bool isDead = false;

        [Header("Tactical Cripple & Trauma")]
        [SerializeField] protected bool isCrippled = false;
        [SerializeField] protected float legTrauma = 0f;
        protected ZombieEyeGlow eyeGlow;

        // Floating Overhead Health Bar
        protected GameObject healthBarRoot;
        protected Transform healthBarFill;
        protected float healthBarTimer = 0f;

        public static readonly List<ZombieAI> ActiveZombies = new List<ZombieAI>();

        public ZombieArchetype Archetype => archetype;
        public ZombieType Type => zombieType;
        public void SetZombieType(ZombieType type) => zombieType = type;
        public void ConfigureStats(float hp, float dmg, float spd)
        {
            maxHealth = hp;
            currentHealth = hp;
            attackDamage = dmg;
            chaseSpeed = spd;
        }
        public AwarenessState Awareness => awarenessState;
        public ZombieState CurrentState => currentState;
        public bool IsDead => isDead;
        public bool IsStunned => stunTimer > 0f;
        public bool IsCrippled => isCrippled;
        public float LegTrauma => legTrauma;
        public ZombieEyeGlow EyeGlow => eyeGlow;
        public float EnvironmentalSpeedMultiplier { get; set; } = 1.0f;

        public float EffectivePatrolSpeed => (isCrippled ? patrolSpeed * ZombieHitZoneResolver.CrippleSpeedReductionFactor : patrolSpeed) * EnvironmentalSpeedMultiplier;
        public float EffectiveInvestigateSpeed => (isCrippled ? investigateSpeed * ZombieHitZoneResolver.CrippleSpeedReductionFactor : investigateSpeed) * EnvironmentalSpeedMultiplier;
        public float EffectiveChaseSpeed
        {
            get
            {
                float spd = chaseSpeed;
                if (NocturnalZombieEscalator.Instance != null && NocturnalZombieEscalator.Instance.IsNightActive)
                {
                    spd *= 1.15f;
                }
                if (isCrippled)
                {
                    spd *= ZombieHitZoneResolver.CrippleSpeedReductionFactor;
                }
                spd *= EnvironmentalSpeedMultiplier;
                return spd;
            }
        }

        public void ApplyEnvironmentalSpeedModifier(float mult)
        {
            EnvironmentalSpeedMultiplier = mult;
            if (agent != null && agent.enabled && agent.isOnNavMesh && !isDead && !agent.isStopped)
            {
                if (currentState == ZombieState.Chase) agent.speed = EffectiveChaseSpeed;
                else if (currentState == ZombieState.Patrol) agent.speed = EffectivePatrolSpeed;
                else if (currentState == ZombieState.Investigate) agent.speed = EffectiveInvestigateSpeed;
            }
        }

        public virtual void SetCrippled(bool crippled)
        {
            isCrippled = crippled;
            if (locomotionEngine == null) locomotionEngine = GetComponent<ZombieLocomotionEngine>();
            if (locomotionEngine != null) locomotionEngine.SetCrippled(isCrippled);
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (currentState == ZombieState.Chase) agent.speed = EffectiveChaseSpeed;
                else if (currentState == ZombieState.Patrol) agent.speed = EffectivePatrolSpeed;
                else if (currentState == ZombieState.Investigate) agent.speed = EffectiveInvestigateSpeed;
            }
        }

        public void SetArchetype(ZombieArchetype newArchetype)
        {
            archetype = newArchetype;
            if (sensorySystem != null) sensorySystem.SetArchetype(newArchetype);
            if (locomotionEngine != null) locomotionEngine.SetState(archetype, awarenessState);
        }

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            locomotionEngine = GetComponent<ZombieLocomotionEngine>();
            if (locomotionEngine == null) locomotionEngine = gameObject.AddComponent<ZombieLocomotionEngine>();

            sensorySystem = GetComponent<ZombieSensorySystem>();
            if (sensorySystem == null) sensorySystem = GetComponent<ZombieSensor>();
            if (sensorySystem == null) sensorySystem = gameObject.AddComponent<ZombieSensor>();

            animationBridge = GetComponent<ZombieAnimationBridge>();
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;

            eyeGlow = GetComponent<ZombieEyeGlow>();
            if (eyeGlow == null) eyeGlow = gameObject.AddComponent<ZombieEyeGlow>();

            ConfigureSpeedsForType();
            SetupHealthBar();
        }

        protected virtual void OnEnable()
        {
            ZombieAI aiThis = this as ZombieAI;
            if (aiThis != null && !ActiveZombies.Contains(aiThis))
            {
                ActiveZombies.Add(aiThis);
            }

            SnapToNavMesh();
        }

        protected virtual void OnDisable()
        {
            ZombieAI aiThis = this as ZombieAI;
            if (aiThis != null)
            {
                ActiveZombies.Remove(aiThis);
                if (assignedPack != null) assignedPack.RemoveMember(aiThis);
                if (FlankingCoordinator.Instance != null) FlankingCoordinator.Instance.UnregisterAttacker(aiThis);
                if (currentDoorTarget != null) currentDoorTarget.UnregisterPounder(aiThis);
            }
        }

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            SnapToNavMesh();
            InitializeArchetypeState();
        }

        public virtual void OnObjectSpawn()
        {
            isDead = false;
            isCrippled = false;
            legTrauma = 0f;
            currentHealth = maxHealth;
            chaseTarget = null;
            stunTimer = 0f;

            if (col != null) col.enabled = true;
            SnapToNavMesh();

            if (locomotionEngine != null) locomotionEngine.ResetLocomotion();
            if (eyeGlow != null) eyeGlow.ResetGlow();
            ConfigureSpeedsForType();
            InitializeArchetypeState();
        }

        public virtual void AwakenToStreetPatrol()
        {
            if (isDead) return;
            if (archetype == ZombieArchetype.DormantLurker)
            {
                archetype = ZombieArchetype.LoneRoamer;
                awarenessState = AwarenessState.Suspicious;
                if (locomotionEngine != null) locomotionEngine.TriggerLurkerAwaken();
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = EffectivePatrolSpeed;
                }
                PickNextPatrolPoint();
                SetState(ZombieState.Patrol);
                ProceduralAudioManager.Instance?.PlayZombieAlertSnarl();
            }
        }

        public void ResetAlertState()
        {
            if (isDead) return;
            chaseTarget = null;
            awarenessState = AwarenessState.Unaware;
            if (eyeGlow != null) eyeGlow.ResetGlow();
            if (currentState == ZombieState.Chase || currentState == ZombieState.Attack || currentState == ZombieState.Investigate)
            {
                PickNextPatrolPoint();
                SetState(ZombieState.Patrol);
            }
        }

        private void ConfigureSpeedsForType()
        {
            switch (zombieType)
            {
                case ZombieType.Runner:
                    maxHealth = 45f;
                    chaseSpeed = 6.4f;
                    patrolSpeed = 2.4f;
                    investigateSpeed = 2.8f;
                    attackDamage = 16f;
                    break;
                case ZombieType.Tank:
                    maxHealth = 220f;
                    chaseSpeed = 3.6f;
                    patrolSpeed = 1.4f;
                    investigateSpeed = 1.8f;
                    attackDamage = 32f;
                    break;
                case ZombieType.Screamer:
                    maxHealth = 50f;
                    chaseSpeed = 4.2f;
                    patrolSpeed = 1.6f;
                    investigateSpeed = 2.0f;
                    attackDamage = 12f;
                    break;
                default: // Standard
                    maxHealth = 60f;
                    chaseSpeed = 4.6f;
                    patrolSpeed = 1.8f;
                    investigateSpeed = 2.2f;
                    attackDamage = 14f;
                    break;
            }
            currentHealth = maxHealth;
        }

        private void SnapToNavMesh()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null) return;

            // Sample nearest valid NavMesh surface within 12m
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 12f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            agent.enabled = true;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        public void InitializeArchetypeState()
        {
            awarenessState = AwarenessState.Unaware;
            chaseTarget = null;
            stateTimer = 0f;

            if (sensorySystem != null) sensorySystem.SetArchetype(archetype);
            if (locomotionEngine != null) locomotionEngine.SetState(archetype, awarenessState);

            switch (archetype)
            {
                case ZombieArchetype.LoneRoamer:
                    SetState(ZombieState.Patrol);
                    break;
                case ZombieArchetype.Scavenger:
                case ZombieArchetype.DormantLurker:
                    SetState(ZombieState.Idle);
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.speed = 0f;
                    }
                    break;
            }
        }

        protected virtual void Update()
        {
            if (isDead) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            // Health bar billboard
            if (healthBarTimer > 0f)
            {
                healthBarTimer -= Time.deltaTime;
                if (healthBarRoot != null)
                {
                    healthBarRoot.SetActive(healthBarTimer > 0f);
                    if (Camera.main != null)
                    {
                        healthBarRoot.transform.rotation = Camera.main.transform.rotation;
                    }
                }
            }

            // SENSORY SCAN: Graduated Awareness check (suppressed during initial spawn grace period)
            if (SpawnProtectionController.Instance != null && SpawnProtectionController.Instance.IsSpawnGraceActive)
            {
                // Orientation grace active: maintain calm patrol/idle
            }
            else if (sensorySystem != null && sensorySystem.CheckVision(out Transform spottedTarget, out AwarenessState detectedAwareness))
            {
                if (detectedAwareness == AwarenessState.Alerted)
                {
                    OnTargetAlerted(spottedTarget);
                }
            }

            // STATE MACHINE EXECUTION
            switch (currentState)
            {
                case ZombieState.Idle:
                    UpdateIdleArchetype();
                    break;
                case ZombieState.Patrol:
                    UpdatePatrol();
                    break;
                case ZombieState.Investigate:
                    UpdateInvestigate();
                    break;
                case ZombieState.Chase:
                    UpdateChase();
                    break;
                case ZombieState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void OnTargetAlerted(Transform target)
        {
            if (chaseTarget == target && currentState == ZombieState.Chase) return;

            chaseTarget = target;
            awarenessState = AwarenessState.Alerted;

            if (archetype == ZombieArchetype.DormantLurker && locomotionEngine != null)
            {
                locomotionEngine.TriggerLurkerAwaken();
            }

            if (assignedPack != null)
            {
                assignedPack.AlertPack(target);
            }

            ProceduralAudioManager.Instance?.PlayZombieAlertSnarl();
            SetState(ZombieState.Chase);
        }

        private void UpdateIdleArchetype()
        {
            if (archetype == ZombieArchetype.LoneRoamer)
            {
                stateTimer += Time.deltaTime;
                if (stateTimer >= Random.Range(1.5f, 3.0f))
                {
                    SetState(ZombieState.Patrol);
                }
            }
            else if (archetype == ZombieArchetype.Scavenger)
            {
                // Scavenger stays stationary facing vehicle/crate
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.speed = 0f;
                }
            }
            else if (archetype == ZombieArchetype.DormantLurker)
            {
                // Lurker stays slumped until proximity/flashlight/noise triggers it
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.speed = 0f;
                }
            }
        }

        private void UpdatePatrol()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh) return;

            if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                stateTimer = 0f;
                SetState(ZombieState.Idle);
            }
        }

        private void UpdateInvestigate()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh) return;

            if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                stateTimer += Time.deltaTime;
                // Slowly scan left and right
                transform.Rotate(0f, 45f * Time.deltaTime, 0f);

                if (stateTimer >= 5.0f)
                {
                    // Investigation concluded without finding target: resume archetype routine
                    InitializeArchetypeState();
                }
            }
        }

        private void UpdateChase()
        {
            if (chaseTarget == null)
            {
                LoseTargetAndInvestigate(transform.position);
                return;
            }

            // Check if player entered a hiding spot
            var playerStealth = chaseTarget.GetComponent<StealthSystem>();
            if (playerStealth != null && playerStealth.IsHidden)
            {
                if (assignedPack != null) assignedPack.BreakPursuit();
                LoseTargetAndInvestigate(chaseTarget.position);
                return;
            }

            // Check if target died
            var targetNPC = chaseTarget.GetComponent<SurvivorNPC_AI>();
            if (targetNPC != null && targetNPC.IsDead)
            {
                chaseTarget = null;
                if (assignedPack != null) assignedPack.BreakPursuit();
                InitializeArchetypeState();
                return;
            }

            var targetRaider = chaseTarget.GetComponent<HumanRaiderAI>();
            if (targetRaider != null && targetRaider.IsDead)
            {
                chaseTarget = null;
                if (assignedPack != null) assignedPack.BreakPursuit();
                InitializeArchetypeState();
                return;
            }

            // 1. Cooperative Door Breaching
            if (currentDoorTarget != null && !currentDoorTarget.IsOpen && !currentDoorTarget.IsBroken)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                Vector3 doorDir = (currentDoorTarget.transform.position - transform.position);
                doorDir.y = 0;
                if (doorDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(doorDir), Time.deltaTime * 8f);
                }
                currentDoorTarget.PoundDoor(this as ZombieAI, Time.deltaTime);
                if (locomotionEngine != null) locomotionEngine.TriggerAttackLunge();
                return;
            }
            else
            {
                if (currentDoorTarget != null && (currentDoorTarget.IsOpen || currentDoorTarget.IsBroken))
                {
                    currentDoorTarget = null;
                    if (agent.isOnNavMesh) agent.isStopped = false;
                }

                // Check forward obstacle raycast for closed door
                Vector3 rayOrigin = transform.position + Vector3.up * 1f;
                if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, 2.2f))
                {
                    ProjectSurvival.Environment.InteractiveDoor door = hit.collider.GetComponentInParent<ProjectSurvival.Environment.InteractiveDoor>();
                    if (door != null && !door.IsOpen && !door.IsBroken && door.CanAcceptPounder)
                    {
                        if (door.RegisterPounder(this as ZombieAI))
                        {
                            currentDoorTarget = door;
                            if (agent.isOnNavMesh) agent.isStopped = true;
                            return;
                        }
                    }
                }
            }

            // 2. Tactical Flanking Allocation
            chaseDestinationTimer += Time.deltaTime;
            if (chaseDestinationTimer >= 0.2f && agent.isOnNavMesh)
            {
                chaseDestinationTimer = 0f;
                Vector3 dest = chaseTarget.position;
                if (FlankingCoordinator.Instance != null)
                {
                    dest = FlankingCoordinator.Instance.GetAssignedDestination(this as ZombieAI, chaseTarget);
                }
                agent.SetDestination(dest);
            }

            float dist = Vector3.Distance(transform.position, chaseTarget.position);
            if (dist <= attackRange)
            {
                SetState(ZombieState.Attack);
            }
        }

        public void OnDoorBreached()
        {
            currentDoorTarget = null;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
            if (chaseTarget != null)
            {
                SetState(ZombieState.Chase);
            }
        }

        private void UpdateAttack()
        {
            if (chaseTarget == null)
            {
                InitializeArchetypeState();
                return;
            }

            // Face target smoothly
            Vector3 lookDir = (chaseTarget.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            float dist = Vector3.Distance(transform.position, chaseTarget.position);
            if (dist > attackRange + 0.4f)
            {
                SetState(ZombieState.Chase);
                return;
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                ExecuteMeleeAttack();
            }
        }

        protected virtual void ExecuteMeleeAttack()
        {
            if (locomotionEngine != null) locomotionEngine.TriggerAttackLunge();
            if (animationBridge != null) animationBridge.TriggerAttack();
            ProceduralAudioManager.Instance?.PlayZombieAttackSwipe();

            // Deal damage
            if (chaseTarget != null)
            {
                float dist = Vector3.Distance(transform.position, chaseTarget.position);
                if (dist <= attackRange + 0.5f)
                {
                    var playerStats = chaseTarget.GetComponent<SurvivalStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(attackDamage, transform.position);
                        playerStats.ModifyInfection(infectionInflicted);
                    }

                    var traumaSys = chaseTarget.GetComponent<TraumaAndInjurySystem>();
                    if (traumaSys != null)
                    {
                        traumaSys.EvaluateZombieAttack(zombieType, attackDamage);
                    }

                    var npc = chaseTarget.GetComponent<SurvivorNPC_AI>();
                    if (npc != null)
                    {
                        npc.TakeDamage(attackDamage);
                    }

                    var raider = chaseTarget.GetComponent<HumanRaiderAI>();
                    if (raider != null)
                    {
                        raider.TakeDamage(attackDamage);
                    }
                }
            }
        }

        public virtual void SetState(ZombieState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            ZombieAI aiThis = this as ZombieAI;
            if (aiThis != null)
            {
                if (newState == ZombieState.Chase && chaseTarget != null)
                {
                    FlankingCoordinator.Instance?.RegisterAttacker(aiThis, chaseTarget);
                }
                else if (newState != ZombieState.Chase && newState != ZombieState.Attack && newState != ZombieState.Scream)
                {
                    FlankingCoordinator.Instance?.UnregisterAttacker(aiThis);
                }
            }

            if (locomotionEngine != null) locomotionEngine.SetState(archetype, awarenessState);

            switch (newState)
            {
                case ZombieState.Idle:
                case ZombieState.Patrol:
                    GameEvents.TriggerZombieAlertStateChanged(gameObject, "");
                    break;
                case ZombieState.Investigate:
                    GameEvents.TriggerZombieAlertStateChanged(gameObject, "?");
                    break;
                case ZombieState.Chase:
                case ZombieState.Attack:
                    GameEvents.TriggerZombieAlertStateChanged(gameObject, "!");
                    break;
            }

            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh) return;

            switch (newState)
            {
                case ZombieState.Idle:
                    agent.isStopped = true;
                    agent.speed = 0f;
                    break;
                case ZombieState.Patrol:
                    agent.isStopped = false;
                    agent.speed = EffectivePatrolSpeed;
                    PickNextPatrolPoint();
                    break;
                case ZombieState.Investigate:
                    agent.isStopped = false;
                    agent.speed = EffectiveInvestigateSpeed;
                    agent.SetDestination(investigatePosition);
                    break;
                case ZombieState.Chase:
                    agent.isStopped = false;
                    agent.speed = EffectiveChaseSpeed;
                    break;
                case ZombieState.Attack:
                    agent.isStopped = true;
                    break;
            }
        }

        protected virtual void PickNextPatrolPoint()
        {
            if (patrolWaypoints.Count > 0)
            {
                Transform wp = patrolWaypoints[Random.Range(0, patrolWaypoints.Count)];
                if (wp != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(wp.position);
                    return;
                }
            }

            // Pick random reachable street/sidewalk NavMesh waypoint (20m - 35m)
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(20f, 35f);
            Vector3 candidatePos = initialSpawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 16f, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh) agent.SetDestination(hit.position);
            }
        }

        // NOISE HANDLING
        public void HearNoiseStimulus(Vector3 noiseSource)
        {
            if (isDead) return;
            if (SpawnProtectionController.Instance != null && SpawnProtectionController.Instance.IsSpawnGraceActive) return;
            if (currentState == ZombieState.Chase || currentState == ZombieState.Attack) return;

            investigatePosition = noiseSource;
            awarenessState = AwarenessState.Suspicious;

            ProceduralAudioManager.Instance?.PlayZombieQuestioningGroan();
            SetState(ZombieState.Investigate);
        }

        public void OnLurkerNoiseAwaken(Vector3 noiseSource)
        {
            if (isDead) return;
            if (SpawnProtectionController.Instance != null && SpawnProtectionController.Instance.IsSpawnGraceActive) return;
            investigatePosition = noiseSource;
            awarenessState = AwarenessState.Suspicious;

            if (locomotionEngine != null) locomotionEngine.TriggerLurkerAwaken();
            ProceduralAudioManager.Instance?.PlayZombieAlertSnarl();
            SetState(ZombieState.Investigate);
        }

        public void HearNoise(Vector3 noisePosition)
        {
            HearNoiseStimulus(noisePosition);
        }

        public void OnNoiseDetected(Vector3 noisePosition)
        {
            HearNoiseStimulus(noisePosition);
        }

        public void LoseTargetAndInvestigate(Vector3 point)
        {
            if (isDead) return;
            chaseTarget = null;
            investigatePosition = point;
            awarenessState = AwarenessState.Suspicious;
            SetState(ZombieState.Investigate);
        }

        public void TriggerInvestigate(Vector3 point)
        {
            LoseTargetAndInvestigate(point);
        }

        // DAMAGE & HEALTH
        public virtual void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead) return;

            currentHealth -= damage;
            ShowHealthBar();

            if (hitNormal != default && locomotionEngine != null)
            {
                locomotionEngine.TriggerHitRecoil(hitNormal);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
            else if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
            {
                // Damage immediately alerts zombie
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    OnTargetAlerted(player.transform);
                }
            }
        }

        public void Stun(float duration)
        {
            stunTimer = duration;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        }

        public void ApplyStaggerKnockback(Vector3 impulse)
        {
            if (isDead) return;
            Stun(1.0f);
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                Vector3 target = transform.position + impulse;
                if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, 2.5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            ZombieAI aiThis = this as ZombieAI;
            if (aiThis != null)
            {
                if (assignedPack != null) assignedPack.RemoveMember(aiThis);
                if (FlankingCoordinator.Instance != null) FlankingCoordinator.Instance.UnregisterAttacker(aiThis);
                if (currentDoorTarget != null) currentDoorTarget.UnregisterPounder(aiThis);
            }

            if (healthBarRoot != null) healthBarRoot.SetActive(false);
            if (locomotionEngine != null) locomotionEngine.TriggerDeathCollapse();
            if (animationBridge != null) animationBridge.SetDead(true);

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (col != null) col.enabled = false;

            GameEvents.TriggerZombieAlertStateChanged(gameObject, "");
            Debug.Log($"[{gameObject.name}] Perished!");
        }

        private void SetupHealthBar()
        {
            healthBarRoot = new GameObject("Zombie_HealthBar");
            healthBarRoot.transform.SetParent(transform);
            healthBarRoot.transform.localPosition = Vector3.up * 2.25f;

            // Background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(healthBarRoot.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(0.85f, 0.08f, 1f);
            Collider bgCol = bg.GetComponent<Collider>();
            if (bgCol != null) Destroy(bgCol);
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);
            bg.GetComponent<Renderer>().sharedMaterial = bgMat;

            // Fill
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(healthBarRoot.transform);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(0.83f, 0.065f, 1f);
            Collider fillCol = fill.GetComponent<Collider>();
            if (fillCol != null) Destroy(fillCol);
            Material fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            fillMat.color = new Color(0.92f, 0.22f, 0.22f, 0.95f);
            fill.GetComponent<Renderer>().sharedMaterial = fillMat;
            healthBarFill = fill.transform;

            healthBarRoot.SetActive(false);
        }

        private void ShowHealthBar()
        {
            if (healthBarRoot == null) return;
            healthBarTimer = 3.5f;
            healthBarRoot.SetActive(true);

            if (healthBarFill != null)
            {
                float pct = Mathf.Clamp01(currentHealth / maxHealth);
                healthBarFill.localScale = new Vector3(0.83f * pct, 0.065f, 1f);
            }
        }
    }
}
