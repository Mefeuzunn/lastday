using UnityEngine;
using UnityEngine.AI;

namespace ProjectSurvival.AI
{
    public class ZombieLocomotionEngine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator childAnimator;

        [Header("Procedural Motion State")]
        [SerializeField] private ZombieArchetype currentArchetype = ZombieArchetype.LoneRoamer;
        [SerializeField] private AwarenessState currentAwareness = AwarenessState.Unaware;

        private Vector3 initialLocalPos = Vector3.zero;
        private Quaternion initialLocalRot = Quaternion.identity;
        private bool isInitialized = false;

        private float walkStepPhase = 0f;
        private float chaseStridePhase = 0f;
        private float headCheckTimer = 0f;
        private float headCheckDuration = 0f;
        private bool isHeadChecking = false;

        // Transitions & Combat
        private bool isAwakening = false;
        private float awakenTimer = 0f;
        private const float AwakenDuration = 0.55f;

        private float attackLungeTimer = 0f;
        private const float AttackLungeDuration = 0.25f;

        private float hitRecoilTimer = 0f;
        private Vector3 hitRecoilVector = Vector3.zero;
        private const float HitRecoilDuration = 0.20f;

        private bool isTelegraphingScream = false;
        private float telegraphScreamTimer = 0f;

        private bool isDead = false;
        private float deathProgress = 0f;
        private const float DeathDuration = 0.85f;

        // Cripple & Hit-Zone Physics
        [Header("Tactical Cripple & Hit Reaction")]
        [SerializeField] private bool isCrippled = false;
        private float headSnapTimer = 0f;
        private const float HeadSnapDuration = 0.25f;
        private ParticleSystem bloodTrailParticles;

        public bool IsCrippled => isCrippled;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsSearchingHash = Animator.StringToHash("IsSearching");
        private static readonly int IsDormantHash = Animator.StringToHash("IsDormant");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int DeathTriggerHash = Animator.StringToHash("DeathTrigger");
        private bool hasValidClips = false;
        private bool hasSpeedParam = false;
        private bool hasSearchingParam = false;
        private bool hasDormantParam = false;
        private bool hasAttackParam = false;
        private bool hasHitParam = false;
        private bool hasDeathParam = false;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (visualModel == null)
            {
                Transform vm = transform.Find("Visual_Model");
                if (vm != null) visualModel = vm;
            }

            if (visualModel != null)
            {
                initialLocalPos = visualModel.localPosition;
                initialLocalRot = visualModel.localRotation;
                childAnimator = visualModel.GetComponentInChildren<Animator>();
                isInitialized = true;
            }

            CheckAnimatorClips();
            headCheckTimer = Random.Range(3.5f, 6.0f);
        }

        private void CheckAnimatorClips()
        {
            if (childAnimator != null && childAnimator.runtimeAnimatorController != null)
            {
                var clips = childAnimator.runtimeAnimatorController.animationClips;
                hasValidClips = clips != null && clips.Length > 0;
                childAnimator.applyRootMotion = false;

                hasSpeedParam = false;
                hasSearchingParam = false;
                hasDormantParam = false;
                hasAttackParam = false;
                hasHitParam = false;
                hasDeathParam = false;
                var parameters = childAnimator.parameters;
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        int h = parameters[i].nameHash;
                        if (h == SpeedHash) hasSpeedParam = true;
                        else if (h == IsSearchingHash) hasSearchingParam = true;
                        else if (h == IsDormantHash) hasDormantParam = true;
                        else if (h == AttackTriggerHash) hasAttackParam = true;
                        else if (h == HitTriggerHash) hasHitParam = true;
                        else if (h == DeathTriggerHash) hasDeathParam = true;
                    }
                }
            }
            else
            {
                hasValidClips = false;
                hasSpeedParam = false;
                hasSearchingParam = false;
                hasDormantParam = false;
                hasAttackParam = false;
                hasHitParam = false;
                hasDeathParam = false;
            }
        }

        public void Initialize(Transform vm, NavMeshAgent ag)
        {
            visualModel = vm;
            agent = ag;
            if (visualModel != null)
            {
                initialLocalPos = visualModel.localPosition;
                initialLocalRot = visualModel.localRotation;
                childAnimator = visualModel.GetComponentInChildren<Animator>();
                isInitialized = true;
            }
            CheckAnimatorClips();
        }

        public void SetState(ZombieArchetype archetype, AwarenessState awareness)
        {
            currentArchetype = archetype;
            currentAwareness = awareness;

            if (currentArchetype == ZombieArchetype.DormantLurker && currentAwareness != AwarenessState.Unaware && !isAwakening)
            {
                TriggerLurkerAwaken();
            }
        }

        public void TriggerLurkerAwaken()
        {
            isAwakening = true;
            awakenTimer = 0f;
            if (hasValidClips && childAnimator != null)
            {
                childAnimator.SetBool(IsDormantHash, false);
            }
        }

        public void TriggerAttackLunge()
        {
            attackLungeTimer = AttackLungeDuration;
            if (hasValidClips && childAnimator != null && hasAttackParam)
            {
                childAnimator.SetTrigger(AttackTriggerHash);
            }
        }

        public void TriggerHitRecoil(Vector3 hitDir)
        {
            hitRecoilTimer = HitRecoilDuration;
            hitRecoilVector = -transform.InverseTransformDirection(hitDir).normalized;
            if (hasValidClips && childAnimator != null && hasHitParam)
            {
                childAnimator.SetTrigger(HitTriggerHash);
            }
        }

        public void TriggerScreamerTelegraph()
        {
            isTelegraphingScream = true;
            telegraphScreamTimer = 1.0f;
        }

        public void CancelScreamerTelegraph()
        {
            isTelegraphingScream = false;
            telegraphScreamTimer = 0f;
        }

        public void TriggerDeathCollapse()
        {
            isDead = true;
            deathProgress = 0f;
            if (bloodTrailParticles != null) bloodTrailParticles.Stop();
            if (hasValidClips && childAnimator != null && hasDeathParam)
            {
                childAnimator.SetTrigger(DeathTriggerHash);
            }
        }

        public void SetCrippled(bool crippled)
        {
            isCrippled = crippled;
            if (isCrippled)
            {
                if (Application.isPlaying)
                {
                    EnsureBloodParticles();
                    if (bloodTrailParticles != null) bloodTrailParticles.Play();
                }
            }
            else
            {
                if (bloodTrailParticles != null) bloodTrailParticles.Stop();
            }
        }

        public void TriggerHeadSnap(float duration = HeadSnapDuration)
        {
            headSnapTimer = duration;
        }

        private void EnsureBloodParticles()
        {
            if (bloodTrailParticles != null) return;

            Transform existing = transform.Find("Crippled_BloodTrail");
            if (existing != null)
            {
                bloodTrailParticles = existing.GetComponent<ParticleSystem>();
                if (bloodTrailParticles != null) return;
            }

            GameObject bloodObj = new GameObject("Crippled_BloodTrail");
            bloodObj.transform.SetParent(transform, false);
            bloodObj.transform.localPosition = new Vector3(0.18f, 0.15f, -0.1f);

            bloodTrailParticles = bloodObj.AddComponent<ParticleSystem>();
            var main = bloodTrailParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.4f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.25f, 0.02f, 0.04f, 0.85f); // Dark arterial coagulated blood
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = bloodTrailParticles.emission;
            emission.rateOverTime = 4f;

            var shape = bloodTrailParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = bloodObj.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                Material m = new Material(s);
                renderer.sharedMaterial = m;
            }
        }

        public void ResetLocomotion()
        {
            isDead = false;
            isAwakening = false;
            isCrippled = false;
            headSnapTimer = 0f;
            if (bloodTrailParticles != null) bloodTrailParticles.Stop();
            deathProgress = 0f;
            attackLungeTimer = 0f;
            hitRecoilTimer = 0f;
            walkStepPhase = Random.Range(0f, Mathf.PI * 2f);
            chaseStridePhase = Random.Range(0f, Mathf.PI * 2f);
            headCheckTimer = Random.Range(3.5f, 6.0f);
            isHeadChecking = false;

            if (visualModel != null && isInitialized)
            {
                visualModel.localPosition = initialLocalPos;
                visualModel.localRotation = initialLocalRot;
            }
        }

        private void Update()
        {
            if (visualModel == null) return;

            float speed = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;

            // Feed Animator if runtime clips exist
            if (hasValidClips && childAnimator != null)
            {
                if (hasSpeedParam) childAnimator.SetFloat(SpeedHash, speed);
                if (hasSearchingParam) childAnimator.SetBool(IsSearchingHash, currentArchetype == ZombieArchetype.Scavenger && currentAwareness == AwarenessState.Unaware);
                if (hasDormantParam) childAnimator.SetBool(IsDormantHash, currentArchetype == ZombieArchetype.DormantLurker && currentAwareness == AwarenessState.Unaware && !isAwakening);
            }

            // Procedural Motion Layer
            UpdateProceduralMotion(speed, Time.deltaTime);
        }

        private void UpdateProceduralMotion(float speed, float dt)
        {
            // 1. Death Collapse (Overrides other motion)
            if (isDead)
            {
                deathProgress = Mathf.Clamp01(deathProgress + (dt / DeathDuration));
                float easeOut = Mathf.Sin(deathProgress * Mathf.PI * 0.5f);
                float deathPitch = Mathf.Lerp(0f, 85f, easeOut);
                float deathY = Mathf.Lerp(0f, -0.72f, easeOut);

                visualModel.localPosition = initialLocalPos + new Vector3(0f, deathY, 0f);
                visualModel.localRotation = initialLocalRot * Quaternion.Euler(deathPitch, 0f, 0f);
                return;
            }

            // Screamer Telegraph Pose (Arched back, head tilted)
            if (isTelegraphingScream && telegraphScreamTimer > 0f)
            {
                telegraphScreamTimer -= dt;
                float progress = Mathf.Sin(telegraphScreamTimer * Mathf.PI);
                float tiltAngle = Mathf.Lerp(0f, -28f, progress);
                visualModel.localPosition = initialLocalPos + new Vector3(0f, 0.08f * progress, 0f);
                visualModel.localRotation = initialLocalRot * Quaternion.Euler(tiltAngle, 0f, 0f);
                return;
            }

            Vector3 procPosOffset = Vector3.zero;
            Vector3 procRotEuler = Vector3.zero;

            // 2. Awakening Animation from Dormant Slump
            if (isAwakening)
            {
                awakenTimer += dt;
                float progress = Mathf.Clamp01(awakenTimer / AwakenDuration);
                float easeSnap = Mathf.SmoothStep(0f, 1f, progress);

                float pitch = Mathf.Lerp(65f, 16f, easeSnap);
                float yOff = Mathf.Lerp(-0.35f, 0f, easeSnap);
                procPosOffset.y += yOff;
                procRotEuler.x += pitch;

                if (awakenTimer >= AwakenDuration)
                {
                    isAwakening = false;
                }
            }
            // 3. Dormant Lurker Rest Slump
            else if (currentArchetype == ZombieArchetype.DormantLurker && currentAwareness == AwarenessState.Unaware)
            {
                float breath = Mathf.Sin(Time.time * 1.6f);
                procPosOffset.y += -0.35f + (breath * 0.012f);
                procRotEuler.x += 65f + (breath * 1.8f);
            }
            // 4. Scavenger Rummaging Loop
            else if (currentArchetype == ZombieArchetype.Scavenger && currentAwareness == AwarenessState.Unaware)
            {
                headCheckTimer -= dt;
                if (!isHeadChecking && headCheckTimer <= 0f)
                {
                    isHeadChecking = true;
                    headCheckDuration = 1.35f;
                }

                if (isHeadChecking)
                {
                    headCheckDuration -= dt;
                    float t = Mathf.Sin((1.35f - headCheckDuration) / 1.35f * Mathf.PI);
                    // Lifts head up to scan (18°), turns yaw left and right
                    procRotEuler.x += Mathf.Lerp(48f, 18f, t);
                    procRotEuler.y += Mathf.Sin(Time.time * 4f) * 24f * t;
                    procPosOffset.y += Mathf.Lerp(-0.15f, 0f, t);

                    if (headCheckDuration <= 0f)
                    {
                        isHeadChecking = false;
                        headCheckTimer = Random.Range(4.0f, 6.5f);
                    }
                }
                else
                {
                    // Deep 48° forward bend, 1.8 Hz feeding bobbing, subtle shoulder twist
                    float rummageBob = Mathf.Sin(Time.time * 1.8f * Mathf.PI * 2f);
                    float twist = Mathf.Cos(Time.time * 1.8f * Mathf.PI);
                    procRotEuler.x += 48f + (rummageBob * 3.5f);
                    procRotEuler.z += twist * 4.5f;
                    procPosOffset.y += -0.15f + (rummageBob * 0.045f);
                }
            }
            // 5. Chase Sprint (Alerted)
            else if (currentAwareness == AwarenessState.Alerted || speed > 3.2f)
            {
                chaseStridePhase += dt * Mathf.Max(speed, 3.8f) * 7.5f;
                float verticalBounce = Mathf.Abs(Mathf.Sin(chaseStridePhase)) * 0.11f;
                float lateralRoll = Mathf.Sin(chaseStridePhase * 0.5f) * 11.5f;
                float lateralYaw = Mathf.Cos(chaseStridePhase * 0.5f) * 5.5f;

                procRotEuler.x += 26f; // Steep forward predator lean
                procRotEuler.z += lateralRoll;
                procRotEuler.y += lateralYaw;
                procPosOffset.y += verticalBounce;
            }
            // 6. Patrol Shamble (LoneRoamer or Suspicious Walking)
            else
            {
                float activeSpeed = Mathf.Max(speed, 0.4f);
                walkStepPhase += dt * activeSpeed * 4.4f;

                // Asymmetric bipedal limp
                float stepNorm = Mathf.Sin(walkStepPhase);
                float asymmetry = stepNorm > 0f ? 1.25f : 0.85f;
                float verticalBounce = Mathf.Abs(stepNorm) * 0.075f * asymmetry;
                float lateralRoll = Mathf.Sin(walkStepPhase * 0.5f) * 7.5f;
                float lateralYaw = Mathf.Cos(walkStepPhase * 0.5f) * 3.8f;

                float forwardHunch = (currentAwareness == AwarenessState.Suspicious) ? 20f : 15f;
                procRotEuler.x += forwardHunch;
                procRotEuler.z += lateralRoll;
                procRotEuler.y += lateralYaw;
                procPosOffset.y += verticalBounce;
            }

            // 7. Combat Reactions: Melee Attack Lunge
            if (attackLungeTimer > 0f)
            {
                attackLungeTimer -= dt;
                float progress = Mathf.Clamp01(1f - (attackLungeTimer / AttackLungeDuration));
                float lungeZ = Mathf.Sin(progress * Mathf.PI) * 0.42f;
                procPosOffset.z += lungeZ;
                procRotEuler.x += Mathf.Sin(progress * Mathf.PI) * 14f;
            }

            // 8. Combat Reactions: Hit Knockback Recoil
            if (hitRecoilTimer > 0f)
            {
                hitRecoilTimer -= dt;
                float progress = Mathf.Clamp01(1f - (hitRecoilTimer / HitRecoilDuration));
                float recoilIntensity = Mathf.Sin(progress * Mathf.PI);
                procPosOffset += hitRecoilVector * (recoilIntensity * 0.22f);
                procRotEuler.x -= recoilIntensity * 16f;
            }

            // 9. Head-Snap Stagger (Critical Head Hit Reaction)
            if (headSnapTimer > 0f)
            {
                headSnapTimer -= dt;
                float snapProgress = Mathf.Sin(Mathf.Clamp01(headSnapTimer / HeadSnapDuration) * Mathf.PI);
                procRotEuler.x -= snapProgress * 24f;
                procPosOffset.z -= snapProgress * 0.12f;
            }

            // 10. Crippled Gait & Torso Drag Physics (-0.32m Hip Drop, 22° Torso Lateral Drag)
            if (isCrippled)
            {
                procPosOffset.y -= 0.32f;
                procRotEuler.z += 22f;

                if (speed > 0.15f)
                {
                    // Asymmetric drag limp cadence
                    float dragDip = Mathf.Abs(Mathf.Sin(walkStepPhase * 0.8f)) * 0.08f;
                    procPosOffset.y -= dragDip;
                    procRotEuler.x += Mathf.Sin(walkStepPhase * 0.8f) * 8f;
                }
            }

            // Smoothly apply procedural offsets to Visual_Model
            visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, initialLocalPos + procPosOffset, dt * 14f);
            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, initialLocalRot * Quaternion.Euler(procRotEuler), dt * 14f);
        }
    }
}
