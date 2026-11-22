using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.AI;
using ProjectSurvival.Survival;
using ProjectSurvival.Narrative;
using ProjectSurvival.UI;

namespace ProjectSurvival.Animation
{
    public enum HumanoidState
    {
        Idle,
        Walk,
        Run,
        Crouch,
        CombatAim,
        CowerOrInjured,
        Seated
    }

    /// <summary>
    /// Phase 1 (Step 1A): Universal Humanoid Procedural Animation Rig.
    /// Drives smooth spring-damped procedural kinematics, limb strides, chest breathing,
    /// dynamic eye/head look-at, combat aiming, and dialogue gestures across Player and all 19 NPCs.
    /// Strictly preserves Root -> Visual_Model hierarchy with applyRootMotion = false.
    /// </summary>
    public class HumanoidProceduralAnimator : MonoBehaviour
    {
        [Header("Rig Transforms (Auto-Bound)")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private Transform hips;

        public Transform VisualModel => visualModel;
        public void SetVisualModel(Transform vm) => visualModel = vm;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform leftCalf;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform rightCalf;
        [SerializeField] private Transform weaponSocket;

        [Header("Kinematics Tuning")]
        [SerializeField] private float breathingFrequency = 0.35f;
        [SerializeField] private float breathingAmplitude = 0.008f;
        [SerializeField] private float breathingPitch = 1.2f;
        [SerializeField] private float idleScanAngle = 18.0f;
        [SerializeField] private float walkStrideAngle = 26.0f;
        [SerializeField] private float runStrideAngle = 42.0f;
        [SerializeField] private float pelvicBounce = 0.035f;
        [SerializeField] private float crouchHipsDrop = 0.38f;
        [SerializeField] private float crouchTorsoTilt = 16.0f;

        [Header("Social Head Look-At")]
        [SerializeField] private float headLookMaxDistance = 5.5f;
        [SerializeField] private float headLookMaxYaw = 55.0f;
        [SerializeField] private float headLookMaxPitch = 25.0f;
        [SerializeField] private float torsoLookFraction = 0.35f;
        [SerializeField] private float lookAtSlerpSpeed = 6.0f;

        [Header("Live State Monitor")]
        [SerializeField] private HumanoidState currentState = HumanoidState.Idle;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private bool isCrouched = false;
        [SerializeField] private bool isSeated = false;
        [SerializeField] private bool isAiming = false;
        [SerializeField] private bool isCowering = false;
        [SerializeField] private bool isInjured = false;
        [SerializeField] private Transform currentAimTarget;

        // Cached Rest Local Transforms
        private Vector3 restVisualPos;
        private Quaternion restVisualRot;
        private Vector3 restHipsPos;
        private Quaternion restHipsRot;
        private Vector3 restTorsoPos;
        private Quaternion restTorsoRot;
        private Quaternion restNeckRot;
        private Quaternion restHeadRot;
        private Quaternion restLeftArmRot;
        private Quaternion restRightArmRot;
        private Quaternion restLeftForearmRot;
        private Quaternion restRightForearmRot;
        private Quaternion restLeftLegRot;
        private Quaternion restRightLegRot;
        private Quaternion restLeftCalfRot;
        private Quaternion restRightCalfRot;
        private Vector3 restWeaponSocketPos;
        private Quaternion restWeaponSocketRot;
        private bool hasRestCached = false;

        // Locomotion & Animation Timers
        private float strideCycle = 0f;
        private float breathTimer = 0f;
        private float dialogueNodTimer = 0f;
        private Vector3 lastRootPosition;
        private Vector3 currentRecoilPos = Vector3.zero;
        private Vector3 currentRecoilRot = Vector3.zero;

        // Social Look-At state
        private Quaternion currentHeadLookRot = Quaternion.identity;
        private Quaternion currentTorsoLookRot = Quaternion.identity;
        private Transform cachedPlayerTransform;
        private Transform cachedPlayerHead;

        // Character controller references
        private CharacterController characterController;
        private NavMeshAgent navMeshAgent;
        private TPSPlayerController tpsPlayerController;
        private SurvivorNPC_AI survivorAi;
        private HumanRaiderAI raiderAi;
        private bool isPlayer = false;

        public HumanoidState CurrentState => currentState;
        public float CurrentSpeed => currentSpeed;
        public bool IsCrouched => isCrouched;
        public bool IsSeated => isSeated;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            tpsPlayerController = GetComponent<TPSPlayerController>();
            survivorAi = GetComponent<SurvivorNPC_AI>();
            raiderAi = GetComponent<HumanRaiderAI>();
            isPlayer = CompareTag("Player") || tpsPlayerController != null;

            AutoBindLimbs();
            CacheRestTransforms();
            lastRootPosition = transform.position;

            // Ensure Animator components never override NavMeshAgent or CharacterController
            var anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
            }
        }

        private void Start()
        {
            if (!hasRestCached) CacheRestTransforms();
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            if (isPlayer) return;

            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                cachedPlayerTransform = p.transform;
                cachedPlayerHead = p.transform.Find("Visual_Model/Body_Hips/Torso/Neck/Head")
                    ?? p.transform.Find("Visual_Model/Head")
                    ?? p.transform;
            }
        }

        public void CacheRestTransforms()
        {
            if (visualModel != null)
            {
                restVisualPos = visualModel.localPosition;
                restVisualRot = visualModel.localRotation;
            }
            if (hips != null)
            {
                restHipsPos = hips.localPosition;
                restHipsRot = hips.localRotation;
            }
            if (torso != null)
            {
                restTorsoPos = torso.localPosition;
                restTorsoRot = torso.localRotation;
            }
            if (neck != null) restNeckRot = neck.localRotation;
            if (head != null) restHeadRot = head.localRotation;
            if (leftArm != null) restLeftArmRot = leftArm.localRotation;
            if (rightArm != null) restRightArmRot = rightArm.localRotation;
            if (leftForearm != null) restLeftForearmRot = leftForearm.localRotation;
            if (rightForearm != null) restRightForearmRot = rightForearm.localRotation;
            if (leftLeg != null) restLeftLegRot = leftLeg.localRotation;
            if (rightLeg != null) restRightLegRot = rightLeg.localRotation;
            if (leftCalf != null) restLeftCalfRot = leftCalf.localRotation;
            if (rightCalf != null) restRightCalfRot = rightCalf.localRotation;
            if (weaponSocket != null)
            {
                restWeaponSocketPos = weaponSocket.localPosition;
                restWeaponSocketRot = weaponSocket.localRotation;
            }

            hasRestCached = true;
        }

        public void AutoBindLimbs()
        {
            if (visualModel == null)
            {
                visualModel = transform.Find("Visual_Model")
                    ?? transform.Find("Visual")
                    ?? transform.Find("Model");

                if (visualModel == null && transform.childCount > 0)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        var c = transform.GetChild(i);
                        if (c.GetComponentInChildren<Renderer>() != null)
                        {
                            visualModel = c;
                            break;
                        }
                    }
                }
            }

            if (visualModel == null) return;

            hips = FindNode(visualModel, new[] { "Body_Hips", "Hips", "Pelvis", "Root_Bone", "Hip" });
            torso = FindNode(visualModel, new[] { "Torso", "Spine", "Chest", "UpperBody" });
            neck = FindNode(visualModel, new[] { "Neck" });
            head = FindNode(visualModel, new[] { "Head", "head", "Head_Chin" });

            leftArm = FindNode(visualModel, new[] { "Shoulder_L", "UpperArm_L", "Arm_L", "LeftArm", "LArm", "LeftUpperArm", "Left_Arm" });
            leftForearm = FindNode(visualModel, new[] { "Forearm_L", "LowerArm_L", "LeftForearm", "LForeArm" });
            rightArm = FindNode(visualModel, new[] { "Shoulder_R", "UpperArm_R", "Arm_R", "RightArm", "RArm", "RightUpperArm", "Right_Arm" });
            rightForearm = FindNode(visualModel, new[] { "Forearm_R", "LowerArm_R", "RightForearm", "RForeArm" });

            leftLeg = FindNode(visualModel, new[] { "Hip_L", "Thigh_L", "Leg_L", "LeftLeg", "LLeg", "LeftUpperLeg", "Left_Leg" });
            leftCalf = FindNode(visualModel, new[] { "Calf_L", "Knee_L", "Foot_L", "LowerLeg_L", "LeftCalf" });
            rightLeg = FindNode(visualModel, new[] { "Hip_R", "Thigh_R", "Leg_R", "RightLeg", "RLeg", "RightUpperLeg", "Right_Leg" });
            rightCalf = FindNode(visualNode: visualModel, new[] { "Calf_R", "Knee_R", "Foot_R", "LowerLeg_R", "RightCalf" });

            weaponSocket = FindNode(visualModel, new[] { "Weapon_Socket", "WeaponSocket", "R_Weapon", "WeaponHolder" });
            if (weaponSocket == null && rightForearm != null)
            {
                weaponSocket = FindNode(rightForearm, new[] { "Hand_R", "Hand" });
            }
        }

        private Transform FindNode(Transform visualNode, string[] candidateNames)
        {
            if (visualNode == null) return null;

            foreach (var name in candidateNames)
            {
                Transform found = FindDeepChild(visualNode, name);
                if (found != null) return found;
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string nameFilter)
        {
            if (parent.name.Equals(nameFilter, StringComparison.OrdinalIgnoreCase) ||
                parent.name.Contains(nameFilter))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(nameFilter, StringComparison.OrdinalIgnoreCase) ||
                    child.name.Contains(nameFilter))
                {
                    return child;
                }
                Transform deep = FindDeepChild(child, nameFilter);
                if (deep != null) return deep;
            }
            return null;
        }

        // External State Mutators
        public void SetCrouched(bool crouch) => isCrouched = crouch;
        public void SetSeated(bool seated) => isSeated = seated;
        public void SetAimTarget(Transform target) => currentAimTarget = target;
        public void SetAiming(bool aiming) => isAiming = aiming;
        public void SetCowering(bool cower) => isCowering = cower;
        public void SetInjured(bool injured) => isInjured = injured;

        public void TriggerRecoil(float kick = 0.10f, float pitch = 10f)
        {
            currentRecoilPos = new Vector3(0f, 0.02f, -kick);
            currentRecoilRot = new Vector3(-pitch, UnityEngine.Random.Range(-2f, 2f), 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            EvaluateVelocityAndSpeed(dt);
            EvaluateActiveState();
            AnimateLocomotionAndPosture(dt);
            AnimateWeaponRecoil(dt);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            EvaluateSocialHeadLookAtAndGestures(dt);
        }

        private void EvaluateVelocityAndSpeed(float dt)
        {
            if (characterController != null)
            {
                Vector3 vel = characterController.velocity;
                vel.y = 0f;
                currentSpeed = vel.magnitude;
            }
            else if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                Vector3 vel = navMeshAgent.velocity;
                vel.y = 0f;
                currentSpeed = vel.magnitude;
            }
            else
            {
                Vector3 disp = transform.position - lastRootPosition;
                disp.y = 0f;
                currentSpeed = disp.magnitude / dt;
            }
            lastRootPosition = transform.position;
        }

        private void EvaluateActiveState()
        {
            // 1. Seated
            if (isSeated)
            {
                currentState = HumanoidState.Seated;
                return;
            }

            // 2. Check Player or NPC Injury & Cower
            if (isPlayer)
            {
                isInjured = TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture;
                isCrouched = PlayerIsCrouching();
                isAiming = WeaponSystem.Instance != null && WeaponSystem.Instance.IsAiming;
            }
            else if (survivorAi != null)
            {
                NPCSensor sensor = GetComponent<NPCSensor>();
                bool hasThreat = sensor != null && sensor.HasThreat;

                if (survivorAi.CurrentState == NPCState.FleeToSafeZone || (hasThreat && survivorAi.CurrentState == NPCState.Idle))
                {
                    isCowering = true;
                }
                else
                {
                    isCowering = false;
                }

                isInjured = survivorAi.CurrentHealth < (survivorAi.MaxHealth * 0.35f);
                isAiming = currentAimTarget != null || survivorAi.CurrentState == NPCState.Combat;
            }
            else if (raiderAi != null)
            {
                isCowering = false;
                isInjured = raiderAi.CurrentHealth < (raiderAi.MaxHealth * 0.35f);
                isAiming = raiderAi.IsAiming;
                isCrouched = raiderAi.IsCrouched;
                if (raiderAi.CurrentTarget != null) currentAimTarget = raiderAi.CurrentTarget;
            }

            if (isCowering || isInjured)
            {
                currentState = HumanoidState.CowerOrInjured;
                return;
            }

            // 3. Combat Aiming
            if (isAiming)
            {
                currentState = HumanoidState.CombatAim;
                return;
            }

            // 4. Crouch
            if (isCrouched)
            {
                currentState = HumanoidState.Crouch;
                return;
            }

            // 5. Locomotion vs Idle
            if (currentSpeed > 3.6f)
            {
                currentState = HumanoidState.Run;
            }
            else if (currentSpeed > 0.15f)
            {
                currentState = HumanoidState.Walk;
            }
            else
            {
                currentState = HumanoidState.Idle;
            }
        }

        private bool PlayerIsCrouching()
        {
            if (tpsPlayerController != null && tpsPlayerController.Controller != null)
            {
                return tpsPlayerController.Controller.IsCrouching;
            }
            var stealth = GetComponent<StealthSystem>();
            if (stealth != null) return stealth.IsCrouched;
            return false;
        }

        private void AnimateLocomotionAndPosture(float dt)
        {
            breathTimer += dt * breathingFrequency * Mathf.PI * 2f;

            // Advance Stride Cycle
            if (currentSpeed > 0.15f)
            {
                float strideFreq = (currentSpeed > 3.6f) ? 4.2f : 2.9f;
                strideCycle += dt * currentSpeed * strideFreq;
                if (strideCycle > Mathf.PI * 2f) strideCycle -= Mathf.PI * 2f;
            }
            else
            {
                strideCycle = Mathf.MoveTowards(strideCycle, 0f, dt * 5.0f);
            }

            // Fallback for single-mesh static models lacking limbs
            if (leftLeg == null || rightLeg == null)
            {
                AnimateFallbackVisualModel(dt);
                return;
            }

            // 1. Pelvis / Hips Posture
            float bounceAmount = (currentState == HumanoidState.Run) ? pelvicBounce * 1.3f : pelvicBounce;
            float verticalBounce = -Mathf.Abs(Mathf.Sin(strideCycle)) * bounceAmount * Mathf.Clamp01(currentSpeed / 2.5f);
            float lateralSway = Mathf.Cos(strideCycle) * 0.015f * Mathf.Clamp01(currentSpeed / 2.5f);

            Vector3 targetHipsPos = restHipsPos;
            Quaternion targetHipsRot = restHipsRot;

            if (currentState == HumanoidState.Crouch)
            {
                targetHipsPos += Vector3.down * crouchHipsDrop;
                targetHipsRot *= Quaternion.Euler(crouchTorsoTilt * 0.5f, 0f, 0f);
            }
            else if (currentState == HumanoidState.CowerOrInjured && isCowering)
            {
                targetHipsPos += Vector3.down * 0.20f;
                targetHipsRot *= Quaternion.Euler(15f, 0f, 0f);
            }
            else if (currentState == HumanoidState.Seated)
            {
                targetHipsPos += Vector3.down * 0.38f + Vector3.back * 0.12f;
                targetHipsRot *= Quaternion.identity;
            }
            else
            {
                // Idle breathing rise
                targetHipsPos += Vector3.up * (Mathf.Sin(breathTimer) * breathingAmplitude + verticalBounce);
                targetHipsPos += Vector3.right * lateralSway;
                float runLean = (currentState == HumanoidState.Run) ? 10f : (currentState == HumanoidState.Walk ? 3.5f : 0f);
                targetHipsRot *= Quaternion.Euler(runLean * 0.5f, Mathf.Sin(strideCycle) * 2.5f, -lateralSway * 40f);
            }

            if (hips != null)
            {
                hips.localPosition = Vector3.Lerp(hips.localPosition, targetHipsPos, dt * 10f);
                hips.localRotation = Quaternion.Slerp(hips.localRotation, targetHipsRot, dt * 10f);
            }

            // 2. Torso Posture & Breathing
            if (torso != null)
            {
                float breathPitch = Mathf.Sin(breathTimer) * breathingPitch;
                float sprintLean = (currentState == HumanoidState.Run) ? 12.0f : (currentState == HumanoidState.Walk ? 4.5f : 0f);
                float limpRoll = (isInjured && currentSpeed > 0.15f) ? (Mathf.Sin(strideCycle) > 0f ? 6.5f : -2.5f) : 0f;

                Quaternion targetTorsoRot = restTorsoRot * Quaternion.Euler(sprintLean + breathPitch, 0f, limpRoll);

                if (currentState == HumanoidState.Crouch)
                {
                    targetTorsoRot *= Quaternion.Euler(crouchTorsoTilt, 0f, 0f);
                }
                else if (currentState == HumanoidState.CowerOrInjured && isCowering)
                {
                    // Tremor jitter
                    float tremor = Mathf.Sin(Time.time * 26f) * 1.5f;
                    targetTorsoRot *= Quaternion.Euler(24f + tremor, tremor, 0f);
                }

                torso.localRotation = Quaternion.Slerp(torso.localRotation, targetTorsoRot, dt * 8f);
            }

            // 3. Legs & Calves Striding
            AnimateLegs(dt);

            // 4. Arms & Shoulders Posture
            AnimateArms(dt);
        }

        private void AnimateLegs(float dt)
        {
            if (currentState == HumanoidState.Seated)
            {
                // 90-degree bend at hips, 90-degree bend at knees
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, restLeftLegRot * Quaternion.Euler(-85f, 0f, 0f), dt * 8f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, restRightLegRot * Quaternion.Euler(-85f, 0f, 0f), dt * 8f);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, restLeftCalfRot * Quaternion.Euler(85f, 0f, 0f), dt * 8f);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, restRightCalfRot * Quaternion.Euler(85f, 0f, 0f), dt * 8f);
                return;
            }

            if (currentState == HumanoidState.Crouch && currentSpeed <= 0.15f)
            {
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, restLeftLegRot * Quaternion.Euler(30f, 0f, 0f), dt * 8f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, restRightLegRot * Quaternion.Euler(30f, 0f, 0f), dt * 8f);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, restLeftCalfRot * Quaternion.Euler(-38f, 0f, 0f), dt * 8f);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, restRightCalfRot * Quaternion.Euler(-38f, 0f, 0f), dt * 8f);
                return;
            }

            float maxAngle = (currentState == HumanoidState.Run) ? runStrideAngle : walkStrideAngle;
            if (currentState == HumanoidState.Crouch) maxAngle *= 0.65f;

            // Asymmetric limp cadence if injured
            float leftPhase = strideCycle;
            float rightPhase = strideCycle + Mathf.PI;
            float leftMult = 1f;
            float rightMult = 1f;

            if (isInjured)
            {
                // Injured right leg: shorter, hesitating stride
                rightMult = 0.55f;
                leftMult = 1.15f;
            }

            float leftPitch = Mathf.Sin(leftPhase) * maxAngle * leftMult;
            float rightPitch = Mathf.Sin(rightPhase) * maxAngle * rightMult;

            float leftKneeFlex = Mathf.Max(0f, -Mathf.Sin(leftPhase)) * (maxAngle * 0.95f);
            float rightKneeFlex = Mathf.Max(0f, -Mathf.Sin(rightPhase)) * (maxAngle * 0.95f);

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, restLeftLegRot * Quaternion.Euler(leftPitch, 0f, 0f), dt * 14f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, restRightLegRot * Quaternion.Euler(rightPitch, 0f, 0f), dt * 14f);
            if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, restLeftCalfRot * Quaternion.Euler(leftKneeFlex, 0f, 0f), dt * 14f);
            if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, restRightCalfRot * Quaternion.Euler(rightKneeFlex, 0f, 0f), dt * 14f);
        }

        private void AnimateArms(float dt)
        {
            if (currentState == HumanoidState.Seated)
            {
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, restLeftArmRot * Quaternion.Euler(-42f, 8f, 0f), dt * 8f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, restRightArmRot * Quaternion.Euler(-42f, -8f, 0f), dt * 8f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, restLeftForearmRot * Quaternion.Euler(30f, 0f, 0f), dt * 8f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, restRightForearmRot * Quaternion.Euler(30f, 0f, 0f), dt * 8f);
                return;
            }

            if (currentState == HumanoidState.CowerOrInjured && isCowering)
            {
                // Protective head cover
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, restLeftArmRot * Quaternion.Euler(-105f, 22f, 0f), dt * 10f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, restRightArmRot * Quaternion.Euler(-105f, -22f, 0f), dt * 10f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, restLeftForearmRot * Quaternion.Euler(-35f, 0f, 0f), dt * 10f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, restRightForearmRot * Quaternion.Euler(-35f, 0f, 0f), dt * 10f);
                return;
            }

            if (currentState == HumanoidState.CombatAim)
            {
                // Weapon Ready / Aim pose
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, restRightArmRot * Quaternion.Euler(-76f, 10f, 0f), dt * 12f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, restRightForearmRot * Quaternion.Euler(12f, 0f, 0f), dt * 12f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, restLeftArmRot * Quaternion.Euler(-64f, -24f, 0f), dt * 12f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, restLeftForearmRot * Quaternion.Euler(34f, 0f, 0f), dt * 12f);
                return;
            }

            // Locomotion Arm Counter-Swing
            float swingMax = (currentState == HumanoidState.Run) ? 34.0f : 18.0f;
            float leftSwing = Mathf.Sin(strideCycle + Mathf.PI) * swingMax * Mathf.Clamp01(currentSpeed / 2.0f);
            float rightSwing = Mathf.Sin(strideCycle) * swingMax * Mathf.Clamp01(currentSpeed / 2.0f);

            // Subtle breathing drape when stationary
            float breathDrape = Mathf.Sin(breathTimer) * 1.5f;

            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, restLeftArmRot * Quaternion.Euler(leftSwing + breathDrape, 0f, 0f), dt * 10f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, restRightArmRot * Quaternion.Euler(rightSwing + breathDrape, 0f, 0f), dt * 10f);

            if (leftForearm != null)
            {
                float flexL = Mathf.Max(0f, -leftSwing * 0.45f);
                leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, restLeftForearmRot * Quaternion.Euler(flexL, 0f, 0f), dt * 10f);
            }
            if (rightForearm != null)
            {
                float flexR = Mathf.Max(0f, -rightSwing * 0.45f);
                rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, restRightForearmRot * Quaternion.Euler(flexR, 0f, 0f), dt * 10f);
            }
        }

        private void AnimateFallbackVisualModel(float dt)
        {
            if (visualModel == null) return;

            float verticalBounce = Mathf.Sin(strideCycle * 2f) * pelvicBounce * Mathf.Clamp01(currentSpeed / 2.5f);
            float horizontalSway = Mathf.Cos(strideCycle) * 2.5f * Mathf.Clamp01(currentSpeed / 2.5f);
            float breathingBob = Mathf.Sin(breathTimer) * breathingAmplitude;

            Vector3 targetPos = restVisualPos + new Vector3(0f, verticalBounce + breathingBob, 0f);
            float sprintLean = (currentState == HumanoidState.Run) ? 8f : 0f;
            Quaternion targetRot = restVisualRot * Quaternion.Euler(sprintLean, horizontalSway, 0f);

            if (currentState == HumanoidState.Crouch)
            {
                targetPos += Vector3.down * crouchHipsDrop;
                targetRot *= Quaternion.Euler(crouchTorsoTilt, 0f, 0f);
            }

            visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetPos, dt * 10f);
            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRot, dt * 10f);
        }

        private void AnimateWeaponRecoil(float dt)
        {
            currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, dt * 10f);
            currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, dt * 10f);

            if (weaponSocket != null)
            {
                weaponSocket.localPosition = restWeaponSocketPos + currentRecoilPos;
                weaponSocket.localRotation = restWeaponSocketRot * Quaternion.Euler(currentRecoilRot);
            }
        }

        private void EvaluateSocialHeadLookAtAndGestures(float dt)
        {
            if (isPlayer) return; // Player head look is driven by camera

            if (cachedPlayerTransform == null) FindPlayerReferences();
            if (cachedPlayerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, cachedPlayerTransform.position);
            bool shouldLookAtPlayer = distToPlayer <= headLookMaxDistance &&
                                      currentState != HumanoidState.Run &&
                                      currentState != HumanoidState.CombatAim;

            if (shouldLookAtPlayer)
            {
                Vector3 targetLookPos = cachedPlayerHead != null ? cachedPlayerHead.position : (cachedPlayerTransform.position + Vector3.up * 1.65f);
                Vector3 dirToPlayer = targetLookPos - (head != null ? head.position : (transform.position + Vector3.up * 1.6f));

                if (dirToPlayer.sqrMagnitude > 0.05f)
                {
                    Vector3 localDir = transform.InverseTransformDirection(dirToPlayer.normalized);
                    float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                    float targetPitch = -Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

                    targetYaw = Mathf.Clamp(targetYaw, -headLookMaxYaw, headLookMaxYaw);
                    targetPitch = Mathf.Clamp(targetPitch, -headLookMaxPitch, headLookMaxPitch);

                    // Check for Active Dialogue or Barter gestures
                    bool isDialogueActive = (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueActive) ||
                                            (SurvivorBarterUI.Instance != null && SurvivorBarterUI.Instance.IsBarterActive);

                    if (isDialogueActive)
                    {
                        dialogueNodTimer += dt * 4.5f;
                        targetPitch += Mathf.Sin(dialogueNodTimer) * 5.5f;

                        // Conversational hand gesture
                        if (rightArm != null)
                        {
                            rightArm.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 2.8f) * 8f, 0f, 0f);
                        }
                    }
                    else
                    {
                        dialogueNodTimer = 0f;
                    }

                    Quaternion desiredHeadRot = restHeadRot * Quaternion.Euler(targetPitch, targetYaw * (1f - torsoLookFraction), 0f);
                    currentHeadLookRot = Quaternion.Slerp(currentHeadLookRot, desiredHeadRot, dt * lookAtSlerpSpeed);

                    if (head != null) head.localRotation = currentHeadLookRot;

                    if (torso != null)
                    {
                        Quaternion desiredTorsoRot = torso.localRotation * Quaternion.Euler(0f, targetYaw * torsoLookFraction, 0f);
                        currentTorsoLookRot = Quaternion.Slerp(currentTorsoLookRot, desiredTorsoRot, dt * lookAtSlerpSpeed);
                        torso.localRotation = currentTorsoLookRot;
                    }
                }
            }
            else
            {
                // Smooth return to rest orientation + subtle idle scan
                float scanYaw = (currentState == HumanoidState.Idle) ? Mathf.Sin(Time.time * 0.45f) * idleScanAngle : 0f;
                Quaternion desiredHeadRot = restHeadRot * Quaternion.Euler(0f, scanYaw, 0f);
                currentHeadLookRot = Quaternion.Slerp(currentHeadLookRot, desiredHeadRot, dt * lookAtSlerpSpeed);

                if (head != null) head.localRotation = currentHeadLookRot;
            }
        }
    }
}
