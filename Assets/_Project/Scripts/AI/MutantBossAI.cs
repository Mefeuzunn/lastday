using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.AI
{
    public enum MutantBossType
    {
        Juggernaut = 0,    // ZIRHLI DEV (Sector_0_1_North)
        Banshee = 1,       // ÇIĞLIK KRALİÇESİ (Sector_0_-1_South)
        BioRavager = 2     // BİYOLOJİK MUTASYON LİDERİ (Sector_1_1_NorthEast)
    }

    /// <summary>
    /// Phase 3: Apex Regional Mutant Boss AI.
    /// Implements multi-phase boss combat with signature abilities:
    /// - Juggernaut: Armored plating shield, Bull-Rush charge, Seismic ground slam.
    /// - Banshee: Ultrasonic shatter shriek, Phase summon thresholds (65% & 30% HP).
    /// - BioRavager: Corrosive bile puddles, Toxic Nova at 40% HP & death burst.
    /// Strictly adheres to Root -> Visual_Model modular architecture and URP Lit shading.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MutantBossAI : MonoBehaviour
    {
        [Header("Boss Identity")]
        [SerializeField] private string bossId = "boss_juggernaut";
        [SerializeField] private string bossDisplayName = "ZIRHLI DEV";
        [SerializeField] private string bossSubtitle = "BÖLGE LİDERİ (KUZEY OTOYOLU)";
        [SerializeField] private MutantBossType bossType = MutantBossType.Juggernaut;

        [Header("Health & Shield Stats")]
        [SerializeField] private float maxHealth = 900f;
        [SerializeField] private float currentHealth = 900f;
        [SerializeField] private float maxArmorShield = 250f;
        [SerializeField] private float currentArmorShield = 250f;

        [Header("Combat & Movement")]
        [SerializeField] private float baseSpeed = 4.2f;
        [SerializeField] private float attackRange = 2.6f;
        [SerializeField] private float attackDamage = 35f;
        [SerializeField] private float attackCooldown = 1.8f;

        private NavMeshAgent agent;
        private Transform playerTransform;
        private SurvivalStats playerStats;
        private bool isDead = false;
        private float lastAttackTime = -10f;
        private float specialAbilityCooldownTimer = 4.0f;
        private bool isPerformingSpecial = false;

        // Visual and Telegraph objects
        private GameObject visualModelRoot;
        private LineRenderer telegraphRenderer;
        private Light bossEmissiveLight;

        // Phase Tracking
        private bool hasTriggeredPhase1 = false; // 65% for Banshee
        private bool hasTriggeredPhase2 = false; // 30% for Banshee, 40% for Ravager

        public string BossId => bossId;
        public string DisplayName => bossDisplayName;
        public string Subtitle => bossSubtitle;
        public MutantBossType BossType => bossType;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentArmorShield => currentArmorShield;
        public float MaxArmorShield => maxArmorShield;
        public bool HasArmorShield => bossType == MutantBossType.Juggernaut && currentArmorShield > 0f;
        public bool IsDead => isDead;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = baseSpeed;
            agent.stoppingDistance = attackRange * 0.8f;
            agent.acceleration = 14f;
            agent.angularSpeed = 240f;

            BuildVisualHierarchyIfEmpty();
        }

        private void Start()
        {
            FindPlayer();
            currentHealth = maxHealth;
            currentArmorShield = maxArmorShield;
        }

        private void Update()
        {
            if (isDead) return;

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (isPerformingSpecial) return;

            specialAbilityCooldownTimer -= Time.deltaTime;

            // Attack & Ability selection
            if (specialAbilityCooldownTimer <= 0f)
            {
                ExecuteSignatureAbility(distToPlayer);
            }
            else if (distToPlayer <= attackRange)
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformBasicMeleeAttack();
                }
            }
            else
            {
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTransform.position);
                }
            }
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

        // =========================================================================
        // SIGNATURE ABILITIES PER BOSS ARCHETYPE
        // =========================================================================

        private void ExecuteSignatureAbility(float distToPlayer)
        {
            switch (bossType)
            {
                case MutantBossType.Juggernaut:
                    if (distToPlayer >= 8f && distToPlayer <= 22f)
                    {
                        StartCoroutine(JuggernautBullRushCharge());
                    }
                    else if (distToPlayer <= 4.0f)
                    {
                        StartCoroutine(JuggernautSeismicGroundSlam());
                    }
                    else
                    {
                        specialAbilityCooldownTimer = 1.5f;
                    }
                    break;

                case MutantBossType.Banshee:
                    if (distToPlayer <= 18f)
                    {
                        StartCoroutine(BansheeShatterShriek());
                    }
                    else
                    {
                        specialAbilityCooldownTimer = 1.5f;
                    }
                    break;

                case MutantBossType.BioRavager:
                    if (distToPlayer >= 6f && distToPlayer <= 25f)
                    {
                        StartCoroutine(BioRavagerCorrosiveBileLob());
                    }
                    else
                    {
                        specialAbilityCooldownTimer = 1.5f;
                    }
                    break;
            }
        }

        // --- JUGGERNAUT ABILITIES ---

        private IEnumerator JuggernautBullRushCharge()
        {
            isPerformingSpecial = true;
            agent.isStopped = true;

            Vector3 chargeDir = (playerTransform.position - transform.position).normalized;
            chargeDir.y = 0f;
            transform.rotation = Quaternion.LookRotation(chargeDir);

            // 1. Telegraph Lane (1.1s Crimson warning line)
            if (telegraphRenderer != null)
            {
                telegraphRenderer.enabled = true;
                telegraphRenderer.startColor = new Color(1f, 0.1f, 0.2f, 0.85f);
                telegraphRenderer.endColor = new Color(1f, 0.1f, 0.2f, 0.85f);
                telegraphRenderer.SetPosition(0, transform.position + Vector3.up * 0.2f);
                telegraphRenderer.SetPosition(1, transform.position + chargeDir * 20f + Vector3.up * 0.2f);
            }

            ProceduralAudioManager.Instance?.PlayBossRoar();
            yield return new WaitForSeconds(1.1f);

            if (telegraphRenderer != null) telegraphRenderer.enabled = false;

            // 2. High-Speed Rush (12 m/s for up to 1.8 seconds)
            float rushTimer = 1.8f;
            agent.speed = 12.5f;
            agent.isStopped = false;

            Vector3 chargeTarget = transform.position + chargeDir * 22f;
            agent.SetDestination(chargeTarget);

            bool hitObstacle = false;

            while (rushTimer > 0f)
            {
                rushTimer -= Time.deltaTime;

                // Check player collision
                if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 2.2f)
                {
                    playerStats?.TakeDamage(45f, transform.position);
                    GameEvents.TriggerCameraShakeRequested(0.55f, 0.35f);
                    ProceduralAudioManager.Instance?.PlayPush();
                    break;
                }

                // Check wall/barricade collision
                if (Physics.Raycast(transform.position + Vector3.up * 1f, chargeDir, out RaycastHit hit, 1.4f))
                {
                    if (hit.collider.gameObject != gameObject && !hit.collider.CompareTag("Player"))
                    {
                        hitObstacle = true;
                        break;
                    }
                }

                yield return null;
            }

            agent.speed = baseSpeed;

            // 3. Stun if collided with obstacle/barricade
            if (hitObstacle)
            {
                ProceduralAudioManager.Instance?.PlayExplosion();
                GameEvents.TriggerCameraShakeRequested(0.45f, 0.25f);
                yield return new WaitForSeconds(2.5f); // 2.5s Stun duration
            }

            isPerformingSpecial = false;
            specialAbilityCooldownTimer = 6.0f;
        }

        private IEnumerator JuggernautSeismicGroundSlam()
        {
            isPerformingSpecial = true;
            agent.isStopped = true;

            // Windup (1.0s)
            yield return new WaitForSeconds(1.0f);

            // Ground slam detonation
            Vector3 slamPos = transform.position;
            ProceduralAudioManager.Instance?.PlayExplosion();
            GameEvents.TriggerCameraShakeRequested(0.6f, 0.4f);

            // 4m AoE shockwave
            Collider[] hits = Physics.OverlapSphere(slamPos, 4.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player") && playerStats != null)
                {
                    playerStats.TakeDamage(32f, transform.position);
                    TraumaAndInjurySystem.Instance?.EvaluateBossAttack(bossType, 32f, true);
                }
            }

            yield return new WaitForSeconds(0.6f);
            isPerformingSpecial = false;
            specialAbilityCooldownTimer = 5.0f;
        }

        // --- BANSHEE ABILITIES ---

        private IEnumerator BansheeShatterShriek()
        {
            isPerformingSpecial = true;
            agent.isStopped = true;

            // Telegraph 1.2s violet charge
            if (bossEmissiveLight != null)
            {
                bossEmissiveLight.color = new Color(0.7f, 0.2f, 1f);
                bossEmissiveLight.intensity = 5.0f;
            }

            ProceduralAudioManager.Instance?.PlayScreamerNoise();
            yield return new WaitForSeconds(1.2f);

            if (bossEmissiveLight != null) bossEmissiveLight.intensity = 1.0f;

            // Emit 16m sonic wave
            ProceduralAudioManager.Instance?.PlayScreamerShriek();
            GameEvents.TriggerCameraShakeRequested(0.5f, 0.5f);

            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= 16f)
            {
                playerStats?.TakeDamage(20f, transform.position);
                SurvivalStats.Instance?.DrainStamina(35f);
            }

            // Phase summon check
            CheckBansheeSummonThresholds();

            yield return new WaitForSeconds(0.8f);
            isPerformingSpecial = false;
            specialAbilityCooldownTimer = 6.5f;
        }

        private void CheckBansheeSummonThresholds()
        {
            float hpPct = currentHealth / maxHealth;

            if (hpPct <= 0.65f && !hasTriggeredPhase1)
            {
                hasTriggeredPhase1 = true;
                SpawnRunnerMinions(3);
            }
            else if (hpPct <= 0.30f && !hasTriggeredPhase2)
            {
                hasTriggeredPhase2 = true;
                SpawnRunnerMinions(3);
            }
        }

        private void SpawnRunnerMinions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnOffset = Quaternion.Euler(0f, i * (360f / count), 0f) * Vector3.forward * 4.5f;
                Vector3 spawnPos = transform.position + spawnOffset;

                GameObject minion = new GameObject($"Banshee_RunnerMinion_{i + 1}");
                minion.transform.position = spawnPos;

                CapsuleCollider cc = minion.AddComponent<CapsuleCollider>();
                cc.height = 1.8f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.9f, 0f);

                NavMeshAgent minionAgent = minion.AddComponent<NavMeshAgent>();
                minionAgent.speed = 5.8f;

                ZombieAI zAI = minion.AddComponent<ZombieAI>();
                zAI.SetZombieType(ZombieType.Runner);
                zAI.ConfigureStats(65f, 18f, 5.8f);

                // Quick visual representation
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(minion.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);
                Destroy(body.GetComponent<Collider>());
            }

            Debug.Log($"[MutantBossAI] Banshee summoned {count} Zombie_Runners!");
        }

        // --- BIO-RAVAGER ABILITIES ---

        private IEnumerator BioRavagerCorrosiveBileLob()
        {
            isPerformingSpecial = true;
            agent.isStopped = true;

            // Windup
            yield return new WaitForSeconds(0.8f);

            Vector3 targetPos = playerTransform.position;
            SpawnAcidPuddle(targetPos);

            yield return new WaitForSeconds(0.6f);
            isPerformingSpecial = false;
            specialAbilityCooldownTimer = 5.5f;
        }

        private void SpawnAcidPuddle(Vector3 pos)
        {
            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "BioRavager_AcidPuddle";
            puddle.transform.position = pos + Vector3.up * 0.05f;
            puddle.transform.localScale = new Vector3(4.0f, 0.05f, 4.0f);

            // Material
            Renderer rend = puddle.GetComponent<Renderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(unlit);
            Color acidCol = new Color(0.54f, 0.79f, 0.15f, 0.65f); // #8AC926
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", acidCol);
            else mat.color = acidCol;
            rend.material = mat;

            Collider col = puddle.GetComponent<Collider>();
            col.isTrigger = true;

            AcidPuddleHandler handler = puddle.AddComponent<AcidPuddleHandler>();
            handler.Initialize(8.0f, 14f);
        }

        private void CheckBioRavagerToxicNova()
        {
            float hpPct = currentHealth / maxHealth;
            if (hpPct <= 0.40f && !hasTriggeredPhase2)
            {
                hasTriggeredPhase2 = true;
                StartCoroutine(TriggerToxicNova(6.0f, 60f));
            }
        }

        private IEnumerator TriggerToxicNova(float radius, float damage)
        {
            // 2.0s telegraphed green warning ring
            if (telegraphRenderer != null)
            {
                telegraphRenderer.enabled = true;
                telegraphRenderer.startColor = new Color(0.54f, 0.79f, 0.15f, 0.9f);
                telegraphRenderer.endColor = new Color(0.54f, 0.79f, 0.15f, 0.9f);
            }

            yield return new WaitForSeconds(2.0f);

            if (telegraphRenderer != null) telegraphRenderer.enabled = false;

            ProceduralAudioManager.Instance?.PlayExplosion();
            GameEvents.TriggerCameraShakeRequested(0.55f, 0.35f);

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player") && playerStats != null)
                {
                    playerStats.TakeDamage(damage);
                    playerStats.ModifyInfection(20f);
                }
            }
        }

        // =========================================================================
        // DAMAGE RESOLUTION & DIRECTIONAL WEAKSPOTS
        // =========================================================================

        public void TakeDamage(float rawDamage, Vector3 hitPoint = default, Vector3 hitDirection = default)
        {
            if (isDead) return;

            float finalDamage = rawDamage;

            // Juggernaut Directional Mechanics
            if (bossType == MutantBossType.Juggernaut)
            {
                Vector3 toShooter = -hitDirection.normalized;
                float dotForward = Vector3.Dot(transform.forward, toShooter);

                if (dotForward < -0.35f)
                {
                    // Back hit -> Weakspot (2.2x damage!)
                    finalDamage = rawDamage * 2.2f;
                    ProceduralAudioManager.Instance?.PlayHitImpact(true);
                    GameEvents.TriggerCameraShakeRequested(0.35f, 0.15f);
                }
                else
                {
                    // Front/Side hit -> Armored Plating
                    if (currentArmorShield > 0f)
                    {
                        currentArmorShield = Mathf.Max(0f, currentArmorShield - rawDamage);
                        ProceduralAudioManager.Instance?.PlayArmorHit();
                        finalDamage = rawDamage * 0.25f; // Absorbed mostly by armor
                    }
                    else
                    {
                        finalDamage = rawDamage * 0.65f;
                    }
                }
            }
            else if (bossType == MutantBossType.BioRavager)
            {
                CheckBioRavagerToxicNova();
            }
            else if (bossType == MutantBossType.Banshee)
            {
                CheckBansheeSummonThresholds();
            }

            currentHealth -= finalDamage;

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void ProcessHitZoneDamage(float rawDamage, Vector3 hitPoint, Vector3 hitDirection)
        {
            TakeDamage(rawDamage, hitPoint, hitDirection);
        }

        private void PerformBasicMeleeAttack()
        {
            lastAttackTime = Time.time;
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

            ProceduralAudioManager.Instance?.PlayZombieAttackSwipe();

            if (playerStats != null && Vector3.Distance(transform.position, playerTransform.position) <= attackRange + 0.5f)
            {
                playerStats.TakeDamage(attackDamage, transform.position);
                TraumaAndInjurySystem.Instance?.EvaluateBossAttack(bossType, attackDamage, false);
                GameEvents.TriggerCameraShakeRequested(0.35f, 0.2f);
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            currentHealth = 0f;
            currentArmorShield = 0f;

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // BioRavager Death Burst
            if (bossType == MutantBossType.BioRavager)
            {
                StartCoroutine(TriggerToxicNova(6.0f, 40f));
            }

            ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            GameEvents.TriggerBossDefeated(bossId, bossDisplayName);

            Debug.Log($"[MutantBossAI] Boss '{bossDisplayName}' has been eradicated!");

            Destroy(gameObject, 5.0f);
        }

        // =========================================================================
        // PROCEDURAL VISUAL HIERARCHY (ROOT -> VISUAL_MODEL)
        // =========================================================================

        public void Configure(string id, string displayName, string subtitle, MutantBossType type, float hp, float armor)
        {
            bossId = id;
            bossDisplayName = displayName;
            bossSubtitle = subtitle;
            bossType = type;
            maxHealth = hp;
            currentHealth = hp;
            maxArmorShield = armor;
            currentArmorShield = armor;

            BuildVisualHierarchyIfEmpty();
        }

        private void BuildVisualHierarchyIfEmpty()
        {
            if (visualModelRoot != null) return;

            Transform existing = transform.Find("Visual_Model");
            if (existing != null)
            {
                visualModelRoot = existing.gameObject;
                return;
            }

            visualModelRoot = new GameObject("Visual_Model");
            visualModelRoot.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (bossType == MutantBossType.Juggernaut)
            {
                BuildJuggernautMesh(litShader);
            }
            else if (bossType == MutantBossType.Banshee)
            {
                BuildBansheeMesh(litShader);
            }
            else
            {
                BuildBioRavagerMesh(litShader);
            }

            // Telegraph LineRenderer
            GameObject lrObj = new GameObject("TelegraphRenderer");
            lrObj.transform.SetParent(transform, false);
            telegraphRenderer = lrObj.AddComponent<LineRenderer>();
            telegraphRenderer.useWorldSpace = true;
            telegraphRenderer.positionCount = 2;
            telegraphRenderer.startWidth = 0.6f;
            telegraphRenderer.endWidth = 0.6f;
            telegraphRenderer.enabled = false;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material lrMat = new Material(unlit);
            telegraphRenderer.material = lrMat;
        }

        private void BuildJuggernautMesh(Shader shader)
        {
            // 2.65m heavy mutated riot-enforcer
            Material steelMat = new Material(shader);
            SetMat(steelMat, new Color(0.20f, 0.22f, 0.25f), 0.7f, 0.8f);

            Material fleshMat = new Material(shader);
            SetMat(fleshMat, new Color(0.42f, 0.18f, 0.15f), 0.3f, 0.1f);

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(visualModelRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            torso.transform.localScale = new Vector3(1.3f, 1.2f, 0.85f);
            Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().sharedMaterial = fleshMat;

            // Chest Armor Plate
            GameObject chestPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestPlate.name = "ChestArmor";
            chestPlate.transform.SetParent(torso.transform, false);
            chestPlate.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            chestPlate.transform.localScale = new Vector3(1.05f, 0.95f, 0.35f);
            Destroy(chestPlate.GetComponent<Collider>());
            chestPlate.GetComponent<Renderer>().sharedMaterial = steelMat;

            // Riot Helmet
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "HelmetHead";
            head.transform.SetParent(visualModelRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            head.transform.localScale = new Vector3(0.65f, 0.70f, 0.65f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = steelMat;

            // Spiked Fists
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arm.name = $"Arm_{i}";
                arm.transform.SetParent(visualModelRoot.transform, false);
                arm.transform.localPosition = new Vector3(i * 0.95f, 1.35f, 0.2f);
                arm.transform.localScale = new Vector3(0.4f, 0.75f, 0.4f);
                Destroy(arm.GetComponent<Collider>());
                arm.GetComponent<Renderer>().sharedMaterial = fleshMat;
            }
        }

        private void BuildBansheeMesh(Shader shader)
        {
            // 2.15m slender bioluminescent stalker
            Material bioVioletMat = new Material(shader);
            SetMat(bioVioletMat, new Color(0.25f, 0.15f, 0.35f), 0.4f, 0.2f);
            if (bioVioletMat.HasProperty("_EmissionColor"))
            {
                bioVioletMat.EnableKeyword("_EMISSION");
                bioVioletMat.SetColor("_EmissionColor", new Color(0.65f, 0.25f, 0.95f) * 1.8f);
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "BansheeBody";
            body.transform.SetParent(visualModelRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            body.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = bioVioletMat;

            // Emissive Point Light
            GameObject lightObj = new GameObject("BioluminescentLight");
            lightObj.transform.SetParent(body.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            bossEmissiveLight = lightObj.AddComponent<Light>();
            bossEmissiveLight.type = LightType.Point;
            bossEmissiveLight.color = new Color(0.68f, 0.25f, 0.95f);
            bossEmissiveLight.range = 8.5f;
            bossEmissiveLight.intensity = 1.8f;
        }

        private void BuildBioRavagerMesh(Shader shader)
        {
            // 2.40m bloated acid mutant
            Material acidMat = new Material(shader);
            SetMat(acidMat, new Color(0.35f, 0.48f, 0.15f), 0.5f, 0.1f);
            if (acidMat.HasProperty("_EmissionColor"))
            {
                acidMat.EnableKeyword("_EMISSION");
                acidMat.SetColor("_EmissionColor", new Color(0.54f, 0.79f, 0.15f) * 1.5f);
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "BloatedTorso";
            body.transform.SetParent(visualModelRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            body.transform.localScale = new Vector3(1.35f, 1.55f, 1.25f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = acidMat;

            // Bile Cannon Arm
            GameObject cannon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cannon.name = "BileCannonArm";
            cannon.transform.SetParent(visualModelRoot.transform, false);
            cannon.transform.localPosition = new Vector3(0.95f, 1.35f, 0.45f);
            cannon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            cannon.transform.localScale = new Vector3(0.48f, 0.95f, 0.48f);
            Destroy(cannon.GetComponent<Collider>());
            cannon.GetComponent<Renderer>().sharedMaterial = acidMat;
        }

        private void SetMat(Material mat, Color col, float smoothness, float metallic)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else mat.color = col;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }
    }

    /// <summary>
    /// Temporary bubbling acid puddle created by BioRavager bile lob.
    /// </summary>
    public class AcidPuddleHandler : MonoBehaviour
    {
        private float duration = 8.0f;
        private float damagePerSecond = 14f;
        private float tickTimer = 0f;

        public void Initialize(float dur, float dps)
        {
            duration = dur;
            damagePerSecond = dps;
            Destroy(gameObject, duration);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                tickTimer += Time.deltaTime;
                if (tickTimer >= 0.5f)
                {
                    tickTimer = 0f;
                    SurvivalStats stats = other.GetComponent<SurvivalStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(damagePerSecond * 0.5f);
                        stats.ModifyInfection(4f);
                    }
                }
            }
        }
    }
}
