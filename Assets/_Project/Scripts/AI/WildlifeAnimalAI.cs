using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;

namespace ProjectSurvival.AI
{
    public enum WildlifeType
    {
        ForestDeer,
        FeralBoar
    }

    public enum WildlifeState
    {
        Grazing,
        Wandering,
        Warning,
        Charging,
        Fleeing,
        Dead
    }

    /// <summary>
    /// Phase 3 (Step 3B): Western Forest Wildlife AI.
    /// Implements distinct behaviors for Forest Deer (peaceful grazers, highly acoustic-sensitive,
    /// stalkable within 10m using Tactical Crossbow stealth) and Feral Forest Boars (territorial
    /// 11m warning grunters, 5.8 m/s charging combatants).
    /// Built with modular Root -> Visual_Model hierarchy, procedural leg/neck locomotion,
    /// NavMesh pathfinding, and non-allocating distance checks.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class WildlifeAnimalAI : MonoBehaviour
    {
        [Header("Species & Identity")]
        [SerializeField] private WildlifeType species = WildlifeType.ForestDeer;
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float currentHealth = 45f;

        [Header("Movement & Speeds")]
        [SerializeField] private float wanderSpeed = 1.8f;
        [SerializeField] private float runSpeed = 7.2f;
        [SerializeField] private float chargeSpeed = 5.8f;
        [SerializeField] private float wanderRadius = 24f;

        [Header("Senses & Combat")]
        [SerializeField] private float gunshotFleeRadius = 45f;
        [SerializeField] private float sprintDetectionRadius = 16f;
        [SerializeField] private float crouchStealthDetectionRadius = 9.5f;
        [SerializeField] private float boarWarningRadius = 11f;
        [SerializeField] private float boarChargeDamage = 18f;
        [SerializeField] private float attackCooldown = 1.8f;

        [Header("Procedural Visual Parts")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private Transform neckTransform;
        [SerializeField] private Transform legFL;
        [SerializeField] private Transform legFR;
        [SerializeField] private Transform legBL;
        [SerializeField] private Transform legBR;
        [SerializeField] private Transform contactShadow;

        private NavMeshAgent agent;
        private WildlifeState currentState = WildlifeState.Grazing;
        private Transform playerTransform;
        private Vector3 spawnOrigin;
        private float stateTimer = 0f;
        private float lastAttackTime = 0f;
        private float lastGruntTime = 0f;
        private float strideCycle = 0f;
        private bool isDead = false;

        public WildlifeType Species => species;
        public WildlifeState CurrentState => currentState;
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public float BoarWarningRadius => boarWarningRadius;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            spawnOrigin = transform.position;

            if (species == WildlifeType.ForestDeer)
            {
                maxHealth = 45f;
                wanderSpeed = 1.8f;
                runSpeed = 7.2f;
            }
            else
            {
                maxHealth = 85f;
                wanderSpeed = 1.4f;
                runSpeed = 5.8f;
            }
            currentHealth = maxHealth;

            AutoBindVisualParts();
        }

        private void Start()
        {
            FindPlayer();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = wanderSpeed;
                agent.stoppingDistance = 0.5f;
                agent.autoBraking = true;
            }

            stateTimer = UnityEngine.Random.Range(2.0f, 5.0f);
            GameEvents.OnWeaponFired += HandleWeaponFiredAcousticAlert;
        }

        private void OnDestroy()
        {
            GameEvents.OnWeaponFired -= HandleWeaponFiredAcousticAlert;
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (isDead) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Distance LOD throttling (> 75m idle tick reduction)
            if (distToPlayer > 75f)
            {
                return;
            }

            strideCycle += agent.velocity.magnitude * Time.deltaTime * 6.5f;
            AnimateProceduralLimbs();

            switch (currentState)
            {
                case WildlifeState.Grazing:
                    UpdateGrazingState(distToPlayer);
                    break;
                case WildlifeState.Wandering:
                    UpdateWanderingState(distToPlayer);
                    break;
                case WildlifeState.Warning:
                    UpdateWarningState(distToPlayer);
                    break;
                case WildlifeState.Charging:
                    UpdateChargingState(distToPlayer);
                    break;
                case WildlifeState.Fleeing:
                    UpdateFleeingState(distToPlayer);
                    break;
            }
        }

        // =========================================================================
        // STATE MACHINE
        // =========================================================================

        private void UpdateGrazingState(float distToPlayer)
        {
            CheckThreatDetection(distToPlayer);
            if (currentState != WildlifeState.Grazing) return;

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                ChooseNewWanderDestination();
            }
        }

        private void UpdateWanderingState(float distToPlayer)
        {
            CheckThreatDetection(distToPlayer);
            if (currentState != WildlifeState.Wandering) return;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                currentState = WildlifeState.Grazing;
                stateTimer = UnityEngine.Random.Range(3.5f, 7.0f);
            }
        }

        private void UpdateWarningState(float distToPlayer)
        {
            if (species != WildlifeType.FeralBoar) return;

            // Face player
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }

            if (Time.time >= lastGruntTime + 1.8f)
            {
                lastGruntTime = Time.time;
                ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, true);
            }

            // If player comes closer than 6.5m -> Initiate Charge!
            if (distToPlayer <= 6.5f)
            {
                StartCharge();
            }
            // If player retreats beyond 15m -> Return to grazing
            else if (distToPlayer >= 15f)
            {
                currentState = WildlifeState.Grazing;
                stateTimer = 4.0f;
            }
        }

        private void UpdateChargingState(float distToPlayer)
        {
            if (species != WildlifeType.FeralBoar) return;

            agent.speed = chargeSpeed;
            agent.SetDestination(playerTransform.position);

            if (distToPlayer <= 1.85f && Time.time >= lastAttackTime + attackCooldown)
            {
                // Execute gore charge attack
                lastAttackTime = Time.time;
                SurvivalStats.Instance?.TakeDamage(boarChargeDamage);
                GameEvents.TriggerCameraShakeRequested(0.35f, 0.15f);
                ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, true);
                MobileHapticManager.TriggerHeavy();
                GameEvents.TriggerObjectiveToastRequested("Yaban Domuzu Tosladı! (-18 Can)", 2.5f);
            }

            // Disengage if player climbs or gets too far (> 32m)
            if (distToPlayer > 32f)
            {
                currentState = WildlifeState.Wandering;
                agent.speed = wanderSpeed;
                ChooseNewWanderDestination();
            }
        }

        private void UpdateFleeingState(float distToPlayer)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f || distToPlayer > 35f)
            {
                currentState = WildlifeState.Grazing;
                agent.speed = wanderSpeed;
                stateTimer = UnityEngine.Random.Range(4.0f, 8.0f);
            }
        }

        // =========================================================================
        // SENSORY & THREAT EVALUATION
        // =========================================================================

        private void CheckThreatDetection(float distToPlayer)
        {
            if (playerTransform == null) return;

            float effectiveDetectionRange = sprintDetectionRadius;

            // Check stealth crouching & visibility score
            float visibility = StealthVisibilityController.Instance != null
                ? StealthVisibilityController.Instance.CurrentVisibilityScore
                : 0.85f;

            var cc = playerTransform.GetComponent<CharacterController>();
            bool isMovingFast = cc != null && cc.velocity.magnitude > 3.5f;

            if (visibility < 0.40f && !isMovingFast)
            {
                // Tactical Crossbow / Crouched Stalking Mode: player can approach within 9.5m
                effectiveDetectionRange = crouchStealthDetectionRadius;
            }
            else if (isMovingFast)
            {
                effectiveDetectionRange = sprintDetectionRadius;
            }

            if (distToPlayer <= effectiveDetectionRange)
            {
                if (species == WildlifeType.ForestDeer)
                {
                    StartFlee(playerTransform.position);
                }
                else if (species == WildlifeType.FeralBoar)
                {
                    if (distToPlayer <= 6.5f)
                    {
                        StartCharge();
                    }
                    else if (distToPlayer <= boarWarningRadius)
                    {
                        currentState = WildlifeState.Warning;
                        agent.ResetPath();
                        ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, true);
                    }
                }
            }
        }

        private void HandleWeaponFiredAcousticAlert()
        {
            if (isDead || playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= gunshotFleeRadius)
            {
                StartFlee(playerTransform.position);
            }
        }

        public void StartFlee(Vector3 threatPos)
        {
            if (isDead) return;

            currentState = WildlifeState.Fleeing;
            stateTimer = UnityEngine.Random.Range(6.0f, 9.0f);
            agent.speed = runSpeed;

            // Audio snort/grunt on startle
            ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, species == WildlifeType.FeralBoar);

            Vector3 fleeDir = (transform.position - threatPos).normalized;
            Vector3 targetFleePoint = transform.position + fleeDir * UnityEngine.Random.Range(20f, 35f);

            if (NavMesh.SamplePosition(targetFleePoint, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        private void StartCharge()
        {
            if (isDead) return;

            currentState = WildlifeState.Charging;
            agent.speed = chargeSpeed;
            agent.SetDestination(playerTransform.position);
            ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, true);
        }

        private void ChooseNewWanderDestination()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            Vector2 circle = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 dest = spawnOrigin + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                agent.speed = wanderSpeed;
                agent.SetDestination(hit.position);
                currentState = WildlifeState.Wandering;
            }
        }

        // =========================================================================
        // DAMAGE & DEATH
        // =========================================================================

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, bool isHeadshot = false)
        {
            if (isDead) return;

            currentHealth -= damage;

            // Hit feedback
            if (Combat.CombatJuiceController.Instance != null)
            {
                Combat.CombatJuiceController.Instance.TriggerHitmarker(isHeadshot, currentHealth <= 0f, hitPoint);
            }
            if (Combat.ImpactDecalPoolManager.Instance != null)
            {
                Combat.ImpactDecalPoolManager.Instance.SpawnImpact(hitPoint, -hitDirection, true);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                // If wounded and survived: Boar attacks, Deer flees!
                if (species == WildlifeType.FeralBoar)
                {
                    StartCharge();
                }
                else
                {
                    StartFlee(hitPoint);
                }
            }
        }

        private void Die()
        {
            isDead = true;
            currentState = WildlifeState.Dead;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Audio death sound
            ProceduralAudioManager.Instance?.PlayAnimalGrunt(transform.position, species == WildlifeType.FeralBoar);

            // Ragdoll side tilt
            if (visualModel != null)
            {
                visualModel.localRotation = Quaternion.Euler(0f, 0f, species == WildlifeType.ForestDeer ? 85f : 78f);
                visualModel.localPosition = new Vector3(0f, 0.15f, 0f);
            }

            // Attach harvest interactable
            AttachHarvestInteractable();
        }

        private void AttachHarvestInteractable()
        {
            // Remove existing triggers if any
            Collider[] existingCols = GetComponents<Collider>();
            foreach (var c in existingCols)
            {
                if (c.isTrigger) Destroy(c);
            }

            // Interaction SphereCollider
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.85f;
            sc.center = new Vector3(0f, 0.5f, 0f);

            AnimalHarvestInteractable harvest = gameObject.AddComponent<AnimalHarvestInteractable>();
            int meatCount = species == WildlifeType.ForestDeer ? 3 : 2;
            int clothCount = 1;
            string displayName = species == WildlifeType.ForestDeer ? "Orman Geyiği" : "Yaban Domuzu";
            harvest.Configure(displayName, meatCount, clothCount);
            harvest.SetPersistentId($"wildlife_harvest_{gameObject.name}");
        }

        // =========================================================================
        // PROCEDURAL ANIMATION & AUTO-BINDING
        // =========================================================================

        private void AnimateProceduralLimbs()
        {
            if (isDead) return;

            float speedRatio = Mathf.Clamp01(agent.velocity.magnitude / runSpeed);
            float swing = Mathf.Sin(strideCycle) * 28f * speedRatio;

            if (legFL != null) legFL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legFR != null) legFR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (legBL != null) legBL.localRotation = Quaternion.Euler(-swing * 0.9f, 0f, 0f);
            if (legBR != null) legBR.localRotation = Quaternion.Euler(swing * 0.9f, 0f, 0f);

            // Neck / Head bobbing
            if (neckTransform != null)
            {
                float neckBob = (currentState == WildlifeState.Grazing)
                    ? -25f + Mathf.Sin(Time.time * 1.5f) * 6f
                    : Mathf.Sin(strideCycle * 0.5f) * 8f * speedRatio;
                neckTransform.localRotation = Quaternion.Euler(neckBob, 0f, 0f);
            }
        }

        public void AutoBindVisualParts()
        {
            if (visualModel == null)
            {
                visualModel = transform.Find("Visual_Model") ?? transform;
            }

            if (visualModel != null)
            {
                if (neckTransform == null) neckTransform = visualModel.Find("Body_Torso/Neck") ?? visualModel.Find("Neck");
                if (legFL == null) legFL = visualModel.Find("Leg_FL");
                if (legFR == null) legFR = visualModel.Find("Leg_FR");
                if (legBL == null) legBL = visualModel.Find("Leg_BL");
                if (legBR == null) legBR = visualModel.Find("Leg_BR");
                if (contactShadow == null) contactShadow = visualModel.Find("Contact_Shadow");
            }
        }
    }
}
