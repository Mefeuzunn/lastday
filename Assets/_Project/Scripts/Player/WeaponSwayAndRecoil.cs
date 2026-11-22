using System;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Phase 1 (Step 1B): Procedural Weapon Sway & Physical Kickback Recoil.
    /// Drives smooth spring-damped localPosition and localRotation offsets on the equipped weapon socket:
    /// 1. Camera look velocity lag sway (turning rapidly induces natural physical inertia lag),
    /// 2. Walking & sprinting figure-8 bob cadence,
    /// 3. Physical backward (-Z) and upward pitch recoil kick on gunshots and melee strikes.
    /// </summary>
    public class WeaponSwayAndRecoil : MonoBehaviour
    {
        public static WeaponSwayAndRecoil Instance { get; private set; }

        [Header("Target Socket")]
        [SerializeField] private Transform weaponSocket;

        [Header("Look Sway (Lag Inertia)")]
        [SerializeField] private float swayAmount = 0.018f;
        [SerializeField] private float maxSwayDistance = 0.045f;
        [SerializeField] private float swayRotationAmount = 2.4f;
        [SerializeField] private float maxSwayRotation = 5.5f;
        [SerializeField] private float swaySmoothTime = 0.12f;

        [Header("Movement Bob Cadence")]
        [SerializeField] private float walkBobSpeed = 7.5f;
        [SerializeField] private float sprintBobSpeed = 11.5f;
        [SerializeField] private float bobAmountX = 0.009f;
        [SerializeField] private float bobAmountY = 0.007f;
        [SerializeField] private float bobSmoothTime = 0.08f;

        [Header("Spring Damping & Recoil Recovery")]
        [SerializeField] private float springStiffness = 38f;
        [SerializeField] private float springDamping = 12f;

        // Base transform cache
        private Vector3 baseSocketLocalPos = Vector3.zero;
        private Quaternion baseSocketLocalRot = Quaternion.identity;
        private bool hasBaseCached = false;

        // Sway State
        private Vector3 targetSwayPos = Vector3.zero;
        private Vector3 currentSwayPos = Vector3.zero;
        private Vector3 swayPosVelocity = Vector3.zero;
        private Vector3 targetSwayRot = Vector3.zero;
        private Vector3 currentSwayRot = Vector3.zero;
        private Vector3 swayRotVelocity = Vector3.zero;

        // Bob State
        private float bobTimer = 0f;
        private Vector3 targetBobPos = Vector3.zero;
        private Vector3 currentBobPos = Vector3.zero;
        private Vector3 bobPosVelocity = Vector3.zero;

        // Recoil Kick State
        private Vector3 targetRecoilPos = Vector3.zero;
        private Vector3 currentRecoilPos = Vector3.zero;
        private Vector3 recoilPosVelocity = Vector3.zero;
        private Vector3 targetRecoilRot = Vector3.zero;
        private Vector3 currentRecoilRot = Vector3.zero;
        private Vector3 recoilRotVelocity = Vector3.zero;

        // References
        private TPSPlayerController playerController;
        private WeaponSystem weaponSystem;
        private Camera mainCamera;
        private Vector3 lastCameraEuler = Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            AutoFindSocket();
        }

        private void Start()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            GameEvents.OnWeaponFired += HandleWeaponFired;
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponFired -= HandleWeaponFired;
            ResetToRest();
        }

        public void AutoFindSocket()
        {
            if (weaponSocket == null)
            {
                weaponSocket = transform.Find("Visual_Model/Weapon_Socket")
                    ?? transform.Find("Weapon_Socket")
                    ?? transform.Find("Visual_Model/Body_Hips/Torso/Arm_R/Forearm_R/Weapon_Socket")
                    ?? transform.Find("Visual_Model/WeaponSocket");
            }

            if (weaponSocket != null && !hasBaseCached)
            {
                baseSocketLocalPos = weaponSocket.localPosition;
                baseSocketLocalRot = weaponSocket.localRotation;
                hasBaseCached = true;
            }
        }

        public void SetWeaponSocket(Transform socket)
        {
            weaponSocket = socket;
            if (weaponSocket != null)
            {
                baseSocketLocalPos = weaponSocket.localPosition;
                baseSocketLocalRot = weaponSocket.localRotation;
                hasBaseCached = true;
            }
        }

        private void FindReferences()
        {
            if (playerController == null) playerController = GetComponent<TPSPlayerController>() ?? FindFirstObjectByType<TPSPlayerController>();
            if (weaponSystem == null) weaponSystem = WeaponSystem.Instance ?? GetComponent<WeaponSystem>() ?? FindFirstObjectByType<WeaponSystem>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera != null) lastCameraEuler = mainCamera.transform.eulerAngles;
            }
        }

        private void LateUpdate()
        {
            if (weaponSocket == null)
            {
                AutoFindSocket();
                if (weaponSocket == null) return;
            }

            UpdateLookSway();
            UpdateMovementBob();
            UpdateSpringRecoil();
            ApplyCompositeTransform();
        }

        private void UpdateLookSway()
        {
            float deltaX = 0f;
            float deltaY = 0f;

            if (mainCamera != null)
            {
                Vector3 currentCamEuler = mainCamera.transform.eulerAngles;
                deltaX = Mathf.DeltaAngle(lastCameraEuler.y, currentCamEuler.y);
                deltaY = Mathf.DeltaAngle(lastCameraEuler.x, currentCamEuler.x);
                lastCameraEuler = currentCamEuler;
            }
            else
            {
                deltaX = Input.GetAxis("Mouse X");
                deltaY = Input.GetAxis("Mouse Y");
            }

            // Target positional sway (inertia lag)
            targetSwayPos.x = Mathf.Clamp(-deltaX * swayAmount, -maxSwayDistance, maxSwayDistance);
            targetSwayPos.y = Mathf.Clamp(-deltaY * swayAmount, -maxSwayDistance, maxSwayDistance);
            targetSwayPos.z = 0f;

            // Target rotational sway
            targetSwayRot.y = Mathf.Clamp(-deltaX * swayRotationAmount, -maxSwayRotation, maxSwayRotation);
            targetSwayRot.x = Mathf.Clamp(deltaY * swayRotationAmount, -maxSwayRotation, maxSwayRotation);
            targetSwayRot.z = Mathf.Clamp(-deltaX * (swayRotationAmount * 0.45f), -maxSwayRotation, maxSwayRotation);

            // Smooth damping
            currentSwayPos = Vector3.SmoothDamp(currentSwayPos, targetSwayPos, ref swayPosVelocity, swaySmoothTime);
            currentSwayRot = Vector3.SmoothDamp(currentSwayRot, targetSwayRot, ref swayRotVelocity, swaySmoothTime);

            // Fade target back towards zero
            targetSwayPos = Vector3.MoveTowards(targetSwayPos, Vector3.zero, Time.deltaTime * 6.5f);
            targetSwayRot = Vector3.MoveTowards(targetSwayRot, Vector3.zero, Time.deltaTime * 24f);
        }

        private void UpdateMovementBob()
        {
            bool isMoving = playerController != null && playerController.IsMoving;
            bool isSprinting = playerController != null && playerController.IsSprinting;

            if (isMoving)
            {
                float speed = isSprinting ? sprintBobSpeed : walkBobSpeed;
                bobTimer += Time.deltaTime * speed;

                float mult = isSprinting ? 1.45f : 1.0f;
                targetBobPos.x = Mathf.Sin(bobTimer * 0.5f) * (bobAmountX * mult);
                targetBobPos.y = Mathf.Abs(Mathf.Cos(bobTimer)) * (bobAmountY * mult);
            }
            else
            {
                bobTimer = 0f;
                targetBobPos = Vector3.zero;
            }

            currentBobPos = Vector3.SmoothDamp(currentBobPos, targetBobPos, ref bobPosVelocity, bobSmoothTime);
        }

        private void UpdateSpringRecoil()
        {
            // Position recoil spring damper
            currentRecoilPos = Vector3.SmoothDamp(currentRecoilPos, targetRecoilPos, ref recoilPosVelocity, 1f / springStiffness);
            targetRecoilPos = Vector3.MoveTowards(targetRecoilPos, Vector3.zero, Time.deltaTime * springDamping);

            // Rotation recoil spring damper
            currentRecoilRot = Vector3.SmoothDamp(currentRecoilRot, targetRecoilRot, ref recoilRotVelocity, 1f / springStiffness);
            targetRecoilRot = Vector3.MoveTowards(targetRecoilRot, Vector3.zero, Time.deltaTime * springDamping * 18f);
        }

        private void ApplyCompositeTransform()
        {
            Vector3 finalLocalPos = baseSocketLocalPos + currentSwayPos + currentBobPos + currentRecoilPos;
            Quaternion finalLocalRot = baseSocketLocalRot * Quaternion.Euler(currentSwayRot + currentRecoilRot);

            weaponSocket.localPosition = finalLocalPos;
            weaponSocket.localRotation = finalLocalRot;
        }

        private void HandleWeaponFired()
        {
            WeaponType weapon = weaponSystem != null ? weaponSystem.CurrentWeapon : WeaponType.None;
            ApplyRecoilKick(weapon);
        }

        /// <summary>
        /// Applies sharp physical backward/upward kick on weapon fire.
        /// </summary>
        public void ApplyRecoilKick(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Pistol:
                    targetRecoilPos += new Vector3(0f, 0.022f, -0.065f);
                    targetRecoilRot += new Vector3(-4.5f, UnityEngine.Random.Range(-0.85f, 0.85f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    break;

                case WeaponType.Shotgun:
                    targetRecoilPos += new Vector3(0f, 0.048f, -0.155f);
                    targetRecoilRot += new Vector3(-9.2f, UnityEngine.Random.Range(-1.6f, 1.6f), UnityEngine.Random.Range(-1.0f, 1.0f));
                    break;

                case WeaponType.Axe:
                    targetRecoilPos += new Vector3(0.04f, -0.035f, 0.07f);
                    targetRecoilRot += new Vector3(4.8f, -7.2f, 5.5f);
                    break;

                case WeaponType.Crossbow:
                    targetRecoilPos += new Vector3(0f, 0.015f, -0.045f);
                    targetRecoilRot += new Vector3(-2.8f, UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(-0.2f, 0.2f));
                    break;
            }
        }

        public void ResetToRest()
        {
            if (weaponSocket != null && hasBaseCached)
            {
                weaponSocket.localPosition = baseSocketLocalPos;
                weaponSocket.localRotation = baseSocketLocalRot;
            }
            currentSwayPos = Vector3.zero;
            currentSwayRot = Vector3.zero;
            currentBobPos = Vector3.zero;
            currentRecoilPos = Vector3.zero;
            currentRecoilRot = Vector3.zero;
        }
    }
}
