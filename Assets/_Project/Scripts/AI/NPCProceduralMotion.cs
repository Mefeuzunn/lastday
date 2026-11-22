using UnityEngine;
using UnityEngine.AI;

namespace ProjectSurvival.AI
{
    public class NPCProceduralMotion : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Transform rightArm;

        [Header("Locomotion Bob & Sway")]
        [SerializeField] private float bobFrequency = 2.8f;
        [SerializeField] private float bobAmount = 0.05f;
        [SerializeField] private float swayAmount = 2.5f;

        [Header("Crouch Posture")]
        [SerializeField] private float crouchYOffset = 0.35f;
        [SerializeField] private float crouchTiltAngle = 14f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Aim & Recoil")]
        [SerializeField] private float aimTurnSpeed = 12f;
        [SerializeField] private float recoilKick = 0.12f;
        [SerializeField] private float recoilPitch = 12f;
        [SerializeField] private float recoilRecoverySpeed = 10f;

        private NavMeshAgent agent;
        private Vector3 initialVisualPos;
        private Quaternion initialVisualRot;
        private Vector3 initialWeaponPos;
        private Quaternion initialWeaponRot;

        private float stepCycle = 0f;
        private bool isCrouched = false;
        private float currentCrouchWeight = 0f;

        private Transform currentAimTarget;
        private Vector3 currentRecoilPos = Vector3.zero;
        private Vector3 currentRecoilRot = Vector3.zero;
        private ProjectSurvival.Animation.HumanoidProceduralAnimator humanoidAnimator;

        public bool IsCrouched => isCrouched;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            humanoidAnimator = GetComponent<ProjectSurvival.Animation.HumanoidProceduralAnimator>();

            if (visualModel == null)
            {
                visualModel = transform.Find("Visual_Model");
            }

            if (visualModel != null)
            {
                initialVisualPos = visualModel.localPosition;
                initialVisualRot = visualModel.localRotation;
            }

            FindWeaponSocketIfMissing();
        }

        private void FindWeaponSocketIfMissing()
        {
            if (visualModel == null) return;

            if (weaponSocket == null)
            {
                // Search for Weapon_Socket or create one on right hand
                Transform ws = visualModel.Find("Weapon_Socket");
                if (ws != null)
                {
                    weaponSocket = ws;
                }
                else
                {
                    // Look for RightHand / RArm
                    Transform rh = FindDeepChild(visualModel, "RArm") ?? FindDeepChild(visualModel, "RightHand");
                    if (rh != null)
                    {
                        GameObject newWs = new GameObject("Weapon_Socket");
                        newWs.transform.SetParent(rh, false);
                        newWs.transform.localPosition = new Vector3(0f, -0.15f, 0.2f);
                        newWs.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        weaponSocket = newWs.transform;
                    }
                }
            }

            if (weaponSocket != null)
            {
                initialWeaponPos = weaponSocket.localPosition;
                initialWeaponRot = weaponSocket.localRotation;
            }
        }

        private Transform FindDeepChild(Transform parent, string nameFilter)
        {
            foreach (Transform c in parent)
            {
                if (c.name.Contains(nameFilter)) return c;
                Transform sub = FindDeepChild(c, nameFilter);
                if (sub != null) return sub;
            }
            return null;
        }

        public void SetCrouched(bool crouch)
        {
            isCrouched = crouch;
            if (humanoidAnimator != null) humanoidAnimator.SetCrouched(crouch);
        }

        public void SetAimTarget(Transform target)
        {
            currentAimTarget = target;
            if (humanoidAnimator != null) humanoidAnimator.SetAimTarget(target);
        }

        public void TriggerRecoil()
        {
            currentRecoilPos = new Vector3(0f, 0.02f, -recoilKick);
            currentRecoilRot = new Vector3(-recoilPitch, UnityEngine.Random.Range(-2f, 2f), 0f);
            if (humanoidAnimator != null) humanoidAnimator.TriggerRecoil(recoilKick, recoilPitch);
        }

        private void Update()
        {
            if (humanoidAnimator != null) return;

            float dt = Time.deltaTime;

            // 1. Crouch interpolation
            float targetCrouchWeight = isCrouched ? 1f : 0f;
            currentCrouchWeight = Mathf.MoveTowards(currentCrouchWeight, targetCrouchWeight, dt * crouchTransitionSpeed);

            // 2. Step Bob & Sway
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            if (speed > 0.15f)
            {
                stepCycle += dt * speed * bobFrequency;
                if (stepCycle > Mathf.PI * 2f) stepCycle -= Mathf.PI * 2f;
            }
            else
            {
                stepCycle = Mathf.MoveTowards(stepCycle, 0f, dt * 4f);
            }

            float verticalBob = Mathf.Sin(stepCycle * 2f) * bobAmount * Mathf.Clamp01(speed / 3f);
            float horizontalSway = Mathf.Cos(stepCycle) * swayAmount * Mathf.Clamp01(speed / 3f);

            if (visualModel != null)
            {
                Vector3 crouchOffset = Vector3.down * (currentCrouchWeight * crouchYOffset);
                visualModel.localPosition = initialVisualPos + crouchOffset + new Vector3(0f, verticalBob, 0f);

                Quaternion tiltRot = Quaternion.Euler(currentCrouchWeight * crouchTiltAngle, horizontalSway, 0f);
                visualModel.localRotation = initialVisualRot * tiltRot;
            }

            // 3. Weapon Recoil Recovery
            currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, dt * recoilRecoverySpeed);
            currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, dt * recoilRecoverySpeed);

            if (weaponSocket != null)
            {
                weaponSocket.localPosition = initialWeaponPos + currentRecoilPos;
                weaponSocket.localRotation = initialWeaponRot * Quaternion.Euler(currentRecoilRot);
            }
        }

        private void LateUpdate()
        {
            // 4. Procedural Aiming towards target zombie
            if (currentAimTarget != null && visualModel != null)
            {
                Vector3 targetPos = currentAimTarget.position + Vector3.up * 1.2f;
                Vector3 aimDir = (targetPos - transform.position);
                aimDir.y = 0f;

                if (aimDir.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * aimTurnSpeed);
                }
            }
        }
    }
}
