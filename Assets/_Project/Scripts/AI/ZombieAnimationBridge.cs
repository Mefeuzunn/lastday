using UnityEngine;
using UnityEngine.AI;

namespace ProjectSurvival.AI
{
    public class ZombieAnimationBridge : MonoBehaviour
    {
        private Animator animator;
        private NavMeshAgent agent;
        private bool hasAnimator = false;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("TriggerAttack");
        private static readonly int ScreamHash = Animator.StringToHash("TriggerScream");
        private static readonly int DeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            RefreshAnimator();
        }

        public void RefreshAnimator()
        {
            animator = GetComponentInChildren<Animator>();
            hasAnimator = animator != null;
        }

        private void Update()
        {
            if (!hasAnimator) return;

            float speed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat(SpeedHash, speed);
        }

        public void TriggerAttack()
        {
            if (hasAnimator) animator.SetTrigger(AttackHash);
        }

        public void TriggerScream()
        {
            if (hasAnimator) animator.SetTrigger(ScreamHash);
        }

        public void SetDead(bool isDead)
        {
            if (hasAnimator) animator.SetBool(DeadHash, isDead);
        }
    }
}
