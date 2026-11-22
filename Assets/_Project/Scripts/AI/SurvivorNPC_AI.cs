using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.Environment;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.AI
{
    public enum NPCArchetype
    {
        Armed,
        Medic,
        Civilian
    }

    public enum NPCState
    {
        Idle,
        FollowPlayer,
        HoldPosition,
        TakeCover,
        Combat,
        FleeToSafeZone,
        Dialogue,
        RoutineTask
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NPCSensor))]
    public class SurvivorNPC_AI : MonoBehaviour
    {
        [Header("Identity & Archetype")]
        [SerializeField] private string npcId = "kerem";
        [SerializeField] private string npcDisplayName = "Kerem";
        [SerializeField] private NPCArchetype archetype = NPCArchetype.Armed;

        [Header("Health & Survival")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float fleeHealthThresholdPercent = 0.30f;

        [Header("Combat Settings (Armed Archetype)")]
        [SerializeField] private float weaponDamage = 28f;
        [SerializeField] private float fireRate = 1.1f;
        [SerializeField] private float preferredCombatDistance = 9f;
        [SerializeField] private float minCombatDistance = 3.5f;

        [Header("Medical Settings (Medic Archetype)")]
        [SerializeField] private float healAmount = 30f;
        [SerializeField] private float healCooldown = 14f;
        [SerializeField] private float healDistance = 2.4f;

        [Header("Movement & Companion Follow")]
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float followOffsetAngle = 135f; // Rear left/right
        [SerializeField] private float followDistance = 3.0f;
        [SerializeField] private bool followLeft = true;

        public float FollowOffsetAngle => followOffsetAngle;

        [Header("State")]
        [SerializeField] private NPCState currentState = NPCState.Idle;
        [SerializeField] private bool isCompanion = false;
        [SerializeField] private bool isRescued = false;

        public static readonly List<SurvivorNPC_AI> ActiveSurvivors = new List<SurvivorNPC_AI>();

        private NavMeshAgent agent;
        private NPCSensor sensor;
        private NPCProceduralMotion proceduralMotion;
        private Transform playerTransform;
        private SurvivalStats playerStats;

        private NPCState previousStateBeforeDialogue = NPCState.Idle;
        private float lastFireTime = -99f;
        private float lastHealTime = -99f;
        private float stateTimer = 0f;
        private Vector3 holdPosition;
        private Vector3 coverDestination;
        private bool isCoverReached = false;

        [Header("Routine & Schedule Integration")]
        [SerializeField] private NPCTaskAnchor currentTaskAnchor;
        private float taskWorkTimer = 0f;
        private bool isPerformingTask = false;
        private float nextDoorCheckTime = 0f;

        // Visual weapon components
        private GameObject visualPistol;
        private Light muzzleFlashLight;

        public string NpcId => npcId;
        public string NpcDisplayName => npcDisplayName;
        public NPCArchetype Archetype => archetype;
        public NPCState CurrentState => currentState;
        public bool IsCompanion => isCompanion;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public NPCTaskAnchor CurrentTaskAnchor => currentTaskAnchor;
        public bool IsBusyWithSchedule => currentTaskAnchor != null;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensor = GetComponent<NPCSensor>();
            proceduralMotion = GetComponent<NPCProceduralMotion>();
            currentHealth = maxHealth;
            holdPosition = transform.position;

            SetupVisualWeapon();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void OnEnable()
        {
            if (!ActiveSurvivors.Contains(this)) ActiveSurvivors.Add(this);

            sensor.OnThreatSpotted += HandleThreatSpotted;
            sensor.OnThreatsCleared += HandleThreatsCleared;
            sensor.OnNoiseHeard += HandleNoiseHeard;
            GameEvents.OnStealthStateChanged += HandlePlayerStealthChanged;
            GameEvents.OnDialogueStarted += HandleDialogueStarted;
            GameEvents.OnDialogueClosed += HandleDialogueClosed;
        }

        private void OnDisable()
        {
            ClearTaskAnchor();
            ActiveSurvivors.Remove(this);

            sensor.OnThreatSpotted -= HandleThreatSpotted;
            sensor.OnThreatsCleared -= HandleThreatsCleared;
            sensor.OnNoiseHeard -= HandleNoiseHeard;
            GameEvents.OnStealthStateChanged -= HandlePlayerStealthChanged;
            GameEvents.OnDialogueStarted -= HandleDialogueStarted;
            GameEvents.OnDialogueClosed -= HandleDialogueClosed;
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerStats = p.GetComponent<SurvivalStats>();
            }
        }

        private void SetupVisualWeapon()
        {
            if (archetype != NPCArchetype.Armed) return;

            Transform visualModel = transform.Find("Visual_Model");
            if (visualModel == null) return;

            Transform ws = visualModel.Find("Weapon_Socket");
            if (ws == null)
            {
                // Find right hand
                foreach (Transform c in visualModel.GetComponentsInChildren<Transform>())
                {
                    if (c.name.Contains("RArm") || c.name.Contains("RightHand"))
                    {
                        GameObject wsObj = new GameObject("Weapon_Socket");
                        wsObj.transform.SetParent(c, false);
                        wsObj.transform.localPosition = new Vector3(0f, -0.15f, 0.22f);
                        wsObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        ws = wsObj.transform;
                        break;
                    }
                }
            }

            if (ws != null)
            {
                // Create compact procedural pistol
                visualPistol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualPistol.name = "NPC_Pistol";
                visualPistol.transform.SetParent(ws, false);
                visualPistol.transform.localPosition = Vector3.zero;
                visualPistol.transform.localRotation = Quaternion.identity;
                visualPistol.transform.localScale = new Vector3(0.06f, 0.12f, 0.25f);
                Collider col = visualPistol.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Material gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                gunMat.color = new Color(0.12f, 0.14f, 0.16f);
                visualPistol.GetComponent<Renderer>().sharedMaterial = gunMat;

                // Muzzle light
                GameObject lightObj = new GameObject("MuzzleFlashLight");
                lightObj.transform.SetParent(visualPistol.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                muzzleFlashLight = lightObj.AddComponent<Light>();
                muzzleFlashLight.type = LightType.Point;
                muzzleFlashLight.color = new Color(1f, 0.75f, 0.3f);
                muzzleFlashLight.intensity = 2.5f;
                muzzleFlashLight.range = 4f;
                muzzleFlashLight.enabled = false;
            }
        }

        private void Update()
        {
            if (IsDead) return;

            stateTimer += Time.deltaTime;

            switch (currentState)
            {
                case NPCState.Idle:
                    UpdateIdle();
                    break;
                case NPCState.FollowPlayer:
                    UpdateFollowPlayer();
                    break;
                case NPCState.HoldPosition:
                    UpdateHoldPosition();
                    break;
                case NPCState.TakeCover:
                    UpdateTakeCover();
                    break;
                case NPCState.Combat:
                    UpdateCombat();
                    break;
                case NPCState.FleeToSafeZone:
                    UpdateFleeToSafeZone();
                    break;
                case NPCState.Dialogue:
                    UpdateDialogue();
                    break;
                case NPCState.RoutineTask:
                    UpdateRoutineTask();
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (sensor.HasThreat)
            {
                DecideThreatResponse();
                return;
            }

            agent.isStopped = true;
            if (proceduralMotion != null)
            {
                proceduralMotion.SetCrouched(false);
                proceduralMotion.SetAimTarget(null);
            }
        }

        private void UpdateFollowPlayer()
        {
            if (sensor.HasThreat)
            {
                DecideThreatResponse();
                return;
            }

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            // Calculate tactical follow position (rear left or rear right based on followOffsetAngle)
            float angle = followLeft ? -followOffsetAngle : followOffsetAngle;
            Vector3 offsetDir = Quaternion.Euler(0f, angle, 0f) * playerTransform.forward;
            Vector3 targetPos = playerTransform.position + offsetDir * followDistance;

            float distToTarget = Vector3.Distance(transform.position, targetPos);
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToTarget > 1.2f)
            {
                agent.isStopped = false;
                agent.speed = distToPlayer > 6.0f ? runSpeed : walkSpeed;
                agent.SetDestination(targetPos);
            }
            else
            {
                agent.isStopped = true;
                // Match player facing orientation subtly when stopped
                transform.rotation = Quaternion.Slerp(transform.rotation, playerTransform.rotation, Time.deltaTime * 3f);
            }

            if (proceduralMotion != null)
            {
                proceduralMotion.SetAimTarget(null);
            }
        }

        private void UpdateHoldPosition()
        {
            if (sensor.HasThreat)
            {
                DecideThreatResponse();
                return;
            }

            float dist = Vector3.Distance(transform.position, holdPosition);
            if (dist > 1.0f)
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(holdPosition);
            }
            else
            {
                agent.isStopped = true;
            }

            if (proceduralMotion != null)
            {
                proceduralMotion.SetAimTarget(null);
            }
        }

        private void DecideThreatResponse()
        {
            if (currentHealth <= maxHealth * fleeHealthThresholdPercent)
            {
                TransitionToState(NPCState.FleeToSafeZone);
                return;
            }

            if (archetype == NPCArchetype.Armed)
            {
                // Armed NPCs: If already near cover or threat is close, engage
                float distToThreat = sensor.NearestThreat != null ? Vector3.Distance(transform.position, sensor.NearestThreat.transform.position) : 99f;
                if (distToThreat <= preferredCombatDistance)
                {
                    TransitionToState(NPCState.Combat);
                }
                else
                {
                    TransitionToState(NPCState.TakeCover);
                }
            }
            else
            {
                // Medics and civilians always seek cover first
                TransitionToState(NPCState.TakeCover);
            }
        }

        private void UpdateTakeCover()
        {
            if (sensor.NearestThreat == null)
            {
                // Threat cleared
                OnThreatsClearedAction();
                return;
            }

            if (!isCoverReached)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;

                float distToCover = Vector3.Distance(transform.position, coverDestination);
                if (distToCover <= 1.2f)
                {
                    isCoverReached = true;
                    agent.isStopped = true;
                    if (proceduralMotion != null) proceduralMotion.SetCrouched(true);
                }
            }
            else
            {
                // Crouched in cover
                if (proceduralMotion != null) proceduralMotion.SetCrouched(true);

                // If armed, peek and shoot
                if (archetype == NPCArchetype.Armed)
                {
                    TransitionToState(NPCState.Combat);
                }
                else if (archetype == NPCArchetype.Medic)
                {
                    CheckMedicOpportunities();
                }
            }
        }

        private void UpdateCombat()
        {
            ZombieAI target = sensor.NearestThreat;
            if (target == null || target.IsDead)
            {
                sensor.ScanForThreats();
                if (!sensor.HasThreat)
                {
                    OnThreatsClearedAction();
                    return;
                }
                target = sensor.NearestThreat;
            }

            if (proceduralMotion != null)
            {
                proceduralMotion.SetAimTarget(target.transform);
            }

            float distToTarget = Vector3.Distance(transform.position, target.transform.position);

            // Kiting / Repositioning if zombie gets too close
            if (distToTarget < minCombatDistance)
            {
                Vector3 fleeDir = (transform.position - target.transform.position).normalized;
                Vector3 retreatPos = transform.position + fleeDir * 4.5f;
                if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.speed = runSpeed;
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                agent.isStopped = true;
            }

            // Face target smoothly
            Vector3 lookDir = (target.transform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            // Shoot weapon
            if (Time.time >= lastFireTime + fireRate)
            {
                ShootAtZombie(target);
            }
        }

        private void ShootAtZombie(ZombieAI target)
        {
            if (SpawnProtectionController.Instance != null && SpawnProtectionController.Instance.IsSpawnGraceActive)
            {
                // Suppress gunfire during player initial spawn orientation grace
                return;
            }

            lastFireTime = Time.time;

            if (proceduralMotion != null)
            {
                proceduralMotion.TriggerRecoil();
            }

            // Audio & Noise
            ProceduralAudioManager.Instance?.PlayGunfire(false, 0.75f);
            GameEvents.TriggerNoiseEmitted(transform.position, 18f);

            // Flash Muzzle
            StartCoroutine(MuzzleFlashRoutine());

            // Bullet Tracer
            Vector3 origin = visualPistol != null ? visualPistol.transform.position : (transform.position + Vector3.up * 1.3f);
            Vector3 targetHead = target.transform.position + Vector3.up * 1.35f;
            SpawnTracer(origin, targetHead);

            // Damage Zombie
            target.TakeDamage(weaponDamage, transform);
            Debug.Log($"[{npcDisplayName}] Shot at {target.name} for {weaponDamage} damage!");
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = true;
                yield return new WaitForSeconds(0.06f);
                muzzleFlashLight.enabled = false;
            }
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            GameObject tracerObj = new GameObject("NPC_Tracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.04f;
            lr.endWidth = 0.02f;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.85f, 0.35f, 0.9f);
            lr.material = mat;

            Destroy(tracerObj, 0.08f);
        }

        private void CheckMedicOpportunities()
        {
            if (playerTransform == null || playerStats == null) return;

            if (Time.time >= lastHealTime + healCooldown && playerStats.CurrentHealth < playerStats.MaxHealth * 0.5f)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= 14f)
                {
                    if (distToPlayer > healDistance)
                    {
                        agent.isStopped = false;
                        agent.speed = runSpeed;
                        agent.SetDestination(playerTransform.position);
                        if (proceduralMotion != null) proceduralMotion.SetCrouched(false);
                    }
                    else
                    {
                        agent.isStopped = true;
                        ApplyMedicFirstAid();
                    }
                }
            }
        }

        private void ApplyMedicFirstAid()
        {
            lastHealTime = Time.time;
            playerStats.Heal(healAmount);
            ProceduralAudioManager.Instance?.PlayFirstAid();
            GameEvents.TriggerObjectiveToastRequested($"[İLK YARDIM] {npcDisplayName} sizi tedavi etti! (+{healAmount:F0} Can)");
            Debug.Log($"[{npcDisplayName}] Applied first aid to player (+{healAmount} HP)!");
        }

        private void UpdateFleeToSafeZone()
        {
            // If overwhelmed and a nearby hiding spot is closer than safe house, seek it!
            HidingSpot nearHiding = FindNearestEmptyHidingSpot(18f);
            GameObject safeZone = GameObject.Find("Zone_SafeHouse") ?? GameObject.Find("Zone_Clinic") ?? GameObject.FindGameObjectWithTag("SafeHouse");
            Vector3 fleeTarget = (nearHiding != null && (safeZone == null || Vector3.Distance(transform.position, nearHiding.transform.position) < Vector3.Distance(transform.position, safeZone.transform.position)))
                ? nearHiding.transform.position
                : (safeZone != null ? safeZone.transform.position : Vector3.zero);

            CheckAndOpenDoorsInPath();

            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(fleeTarget);

            if (proceduralMotion != null)
            {
                proceduralMotion.SetCrouched(false);
                proceduralMotion.SetAimTarget(null);
            }

            float dist = Vector3.Distance(transform.position, fleeTarget);
            if (dist <= 3.0f)
            {
                agent.isStopped = true;
                if (nearHiding != null && dist <= 1.8f && proceduralMotion != null)
                {
                    proceduralMotion.SetCrouched(true);
                }
                TransitionToState(NPCState.Idle);
            }
        }

        private void UpdateDialogue()
        {
            agent.isStopped = true;
            if (playerTransform != null)
            {
                Vector3 lookDir = (playerTransform.position - transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 6f);
                }
            }
        }

        public void AssignTaskAnchor(NPCTaskAnchor anchor)
        {
            if (isCompanion || currentState == NPCState.Combat || currentState == NPCState.Dialogue || IsDead) return;

            if (currentTaskAnchor != null && currentTaskAnchor != anchor)
            {
                currentTaskAnchor.Release(npcId);
            }

            currentTaskAnchor = anchor;
            if (currentTaskAnchor != null)
            {
                currentTaskAnchor.Occupy(npcId);
                isPerformingTask = false;
                taskWorkTimer = 0f;
                TransitionToState(NPCState.RoutineTask);
            }
        }

        public void ClearTaskAnchor()
        {
            if (currentTaskAnchor != null)
            {
                currentTaskAnchor.Release(npcId);
                currentTaskAnchor = null;
            }
            isPerformingTask = false;
            taskWorkTimer = 0f;
            if (currentState == NPCState.RoutineTask)
            {
                TransitionToState(NPCState.Idle);
            }
        }

        private void UpdateRoutineTask()
        {
            if (sensor != null && sensor.HasThreat)
            {
                DecideThreatResponse();
                return;
            }

            if (currentTaskAnchor == null)
            {
                TransitionToState(NPCState.Idle);
                return;
            }

            CheckAndOpenDoorsInPath();

            float dist = Vector3.Distance(transform.position, currentTaskAnchor.transform.position);
            if (dist > currentTaskAnchor.ArrivalRadius)
            {
                // Moving towards anchor
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(currentTaskAnchor.transform.position);

                if (proceduralMotion != null)
                {
                    proceduralMotion.SetCrouched(false);
                    proceduralMotion.SetAimTarget(null);
                }
            }
            else
            {
                // Arrived at anchor
                agent.isStopped = true;

                // Align rotation to anchor's preferred facing direction
                Vector3 desiredFacing = currentTaskAnchor.PreferredFacingDirection;
                if (desiredFacing.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredFacing), Time.deltaTime * 5f);
                }

                if (!isPerformingTask)
                {
                    isPerformingTask = true;
                    taskWorkTimer = 0f;

                    if (currentTaskAnchor.TaskType == NPCTaskType.ScavengeRoute || currentTaskAnchor.TaskType == NPCTaskType.WorkshopRepair)
                    {
                        if (proceduralMotion != null) proceduralMotion.SetCrouched(true);
                    }
                }

                taskWorkTimer += Time.deltaTime;

                // Periodic physical interaction execution (e.g. door repair)
                if (currentTaskAnchor.TargetDoor != null && currentTaskAnchor.TargetDoor.DoorHealth < currentTaskAnchor.TargetDoor.MaxDoorHealth)
                {
                    currentTaskAnchor.TargetDoor.RepairDoor(Time.deltaTime * 12f);
                }

                if (taskWorkTimer >= currentTaskAnchor.InteractionDuration)
                {
                    // Finished this task duration
                    if (currentTaskAnchor.TargetInteractable != null && !currentTaskAnchor.TargetInteractable.HasBeenSearched)
                    {
                        currentTaskAnchor.TargetInteractable.ExecuteVerb("search", gameObject);
                    }

                    ClearTaskAnchor();
                }
            }
        }

        private void CheckAndOpenDoorsInPath()
        {
            if (Time.time < nextDoorCheckTime) return;
            nextDoorCheckTime = Time.time + 0.35f;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 1f, transform.forward, out hit, 2.2f))
            {
                InteractiveDoor door = hit.collider.GetComponentInParent<InteractiveDoor>();
                if (door != null && !door.IsOpen && !door.IsBroken)
                {
                    door.ToggleDoor();
                }
            }
        }

        private HidingSpot FindNearestEmptyHidingSpot(float maxDist)
        {
            HidingSpot[] spots = Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);
            HidingSpot best = null;
            float bestDist = maxDist;

            for (int i = 0; i < spots.Length; i++)
            {
                HidingSpot s = spots[i];
                if (s == null || s.IsOccupied) continue;
                float d = Vector3.Distance(transform.position, s.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }
            return best;
        }

        private void HandleThreatSpotted(ZombieAI threat)
        {
            if (currentState == NPCState.Dialogue) return;
            DecideThreatResponse();
        }

        private void HandleNoiseHeard(Vector3 noisePos)
        {
            if (currentState == NPCState.Dialogue || currentState == NPCState.Combat) return;

            // Turn towards noise source
            Vector3 dir = (noisePos - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private void HandleThreatsCleared()
        {
            OnThreatsClearedAction();
        }

        private void OnThreatsClearedAction()
        {
            if (proceduralMotion != null)
            {
                proceduralMotion.SetCrouched(false);
                proceduralMotion.SetAimTarget(null);
            }

            // If scavenger and not yet rescued, reward player
            if (!isRescued && npcId.StartsWith("scavenger"))
            {
                isRescued = true;
                RewardPlayerFromRescue();
            }

            if (isCompanion)
            {
                TransitionToState(NPCState.FollowPlayer);
            }
            else
            {
                TransitionToState(NPCState.Idle);
            }
        }

        private void RewardPlayerFromRescue()
        {
            string rewardMsg;
            int r = UnityEngine.Random.Range(0, 3);
            if (r == 0)
            {
                GameEvents.TriggerItemCollected("ammo_9mm", 12);
                rewardMsg = "12x 9mm Mermi";
            }
            else if (r == 1)
            {
                GameEvents.TriggerItemCollected("item_food_canned", 1);
                rewardMsg = "1x Konserve Yiyecek";
            }
            else
            {
                GameEvents.TriggerItemCollected("item_scrap", 3);
                rewardMsg = "3x Hurda Metal";
            }

            GameEvents.TriggerScavengerRescued(npcDisplayName, rewardMsg);
            GameEvents.TriggerObjectiveToastRequested($"[KURTARILDI] {npcDisplayName}: 'Hayatımı kurtardın! Al bunlar senin olsun: {rewardMsg}'");
        }

        private void HandlePlayerStealthChanged(bool isCrouching)
        {
            if (isCompanion && currentState == NPCState.FollowPlayer)
            {
                if (proceduralMotion != null)
                {
                    proceduralMotion.SetCrouched(isCrouching);
                }
                agent.speed = isCrouching ? walkSpeed * 0.8f : walkSpeed;
            }
        }

        private void HandleDialogueStarted(string story, string node)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) < 3.5f)
            {
                previousStateBeforeDialogue = currentState;
                TransitionToState(NPCState.Dialogue);
            }
        }

        private void HandleDialogueClosed()
        {
            if (currentState == NPCState.Dialogue)
            {
                TransitionToState(previousStateBeforeDialogue);
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            currentHealth -= amount;
            ProceduralAudioManager.Instance?.PlayDamageSound();

            if (isCompanion)
            {
                GameEvents.TriggerCompanionHealthChanged(currentHealth, maxHealth);
            }

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else if (currentHealth <= maxHealth * fleeHealthThresholdPercent)
            {
                TransitionToState(NPCState.FleeToSafeZone);
            }
            else if (currentState != NPCState.Combat)
            {
                DecideThreatResponse();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            ProceduralAudioManager.Instance?.PlayFirstAid();

            if (isCompanion)
            {
                GameEvents.TriggerCompanionHealthChanged(currentHealth, maxHealth);
            }
        }

        private void Die()
        {
            ClearTaskAnchor();
            ActiveSurvivors.Remove(this);
            agent.isStopped = true;
            agent.enabled = false;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (isCompanion && CompanionManager.Instance != null)
            {
                CompanionManager.Instance.DismissCompanion();
            }

            GameEvents.TriggerObjectiveToastRequested($"[KAYIP] {npcDisplayName} hayatını kaybetti!");
            Debug.Log($"[{npcDisplayName}] Has died.");
        }

        public void TransitionToState(NPCState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            if (newState == NPCState.TakeCover)
            {
                FindCoverPosition();
                isCoverReached = false;
            }
        }

        private void FindCoverPosition()
        {
            Vector3 threatPos = sensor.NearestThreat != null ? sensor.NearestThreat.transform.position : (transform.position + transform.forward * 5f);
            Vector3 awayFromThreat = (transform.position - threatPos).normalized;

            // Search behind vehicles or sample navmesh away from threat
            Vector3 candidate = transform.position + awayFromThreat * 6.5f;

            // Raycast towards obstacles (cars/walls) to hide behind
            if (Physics.Raycast(transform.position, awayFromThreat, out RaycastHit hit, 8f))
            {
                candidate = hit.point + awayFromThreat * 1.5f; // Behind the obstacle
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                coverDestination = navHit.position;
            }
            else
            {
                coverDestination = transform.position;
            }
        }

        public void SetAsCompanion(bool companion)
        {
            isCompanion = companion;
            if (companion)
            {
                TransitionToState(NPCState.FollowPlayer);
                GameEvents.TriggerCompanionHealthChanged(currentHealth, maxHealth);
            }
            else
            {
                TransitionToState(NPCState.HoldPosition);
            }
        }

        public void OrderFollow()
        {
            if (!isCompanion) return;
            TransitionToState(NPCState.FollowPlayer);
            GameEvents.TriggerCompanionOrderChanged(true);
        }

        public void OrderHold()
        {
            if (!isCompanion) return;
            holdPosition = transform.position;
            TransitionToState(NPCState.HoldPosition);
            GameEvents.TriggerCompanionOrderChanged(false);
        }
    }
}
