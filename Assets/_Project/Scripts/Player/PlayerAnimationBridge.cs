using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    public class PlayerAnimationBridge : MonoBehaviour
    {
        private Animator animator;
        private CharacterController characterController;
        private StealthSystem stealthSystem;
        private bool hasAnimator = false;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int CrouchHash = Animator.StringToHash("IsCrouching");
        private static readonly int ThrowHash = Animator.StringToHash("TriggerThrow");
        private static readonly int DeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stealthSystem = GetComponent<StealthSystem>();
            RefreshAnimator();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        public void RefreshAnimator()
        {
            animator = GetComponentInChildren<Animator>();
            hasAnimator = animator != null;
        }

        private void Update()
        {
            if (!hasAnimator) return;

            float speed = characterController != null ? characterController.velocity.magnitude : 0f;
            animator.SetFloat(SpeedHash, speed);

            if (stealthSystem != null)
            {
                animator.SetBool(CrouchHash, stealthSystem.IsCrouched);
            }
        }

        public void TriggerThrow()
        {
            if (hasAnimator) animator.SetTrigger(ThrowHash);
        }

        private void HandlePlayerDied()
        {
            if (hasAnimator) animator.SetBool(DeadHash, true);
        }
    }
}
