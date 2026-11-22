using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CivilianFollower : MonoBehaviour
    {
        public static CivilianFollower Instance { get; private set; }

        [Header("Follow Setup")]
        [SerializeField] private float followDistance = 2.5f;
        [SerializeField] private float runSpeed = 4.2f;
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private float currentHealth = 80f;

        [Header("State")]
        [SerializeField] private bool isFollowing = false;
        [SerializeField] private bool isRescued = false;
        [SerializeField] private bool isDead = false;

        private NavMeshAgent agent;
        private Transform playerTransform;

        public bool IsFollowing => isFollowing;
        public bool IsRescued => isRescued;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (isDead || isRescued || !isFollowing || playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > followDistance)
            {
                agent.isStopped = false;
                agent.speed = dist > 6f ? runSpeed : walkSpeed;
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                agent.isStopped = true;
            }
        }

        public void StartFollowing()
        {
            isFollowing = true;
            Debug.Log("[CivilianFollower] Sivil oyuncuyu takip etmeye başladı.");
        }

        public void TakeDamage(float amount)
        {
            if (isDead || isRescued) return;
            currentHealth -= amount;
            ProceduralAudioManager.Instance?.PlayDamageSound();

            if (currentHealth <= 0f)
            {
                isDead = true;
                agent.enabled = false;
                GameEvents.TriggerMissionFailed("mission_rescue_civilian", "node_civilian_died");
                Debug.Log("[CivilianFollower] Sivil hayatını kaybetti! Görev başarısız.");
            }
        }

        public void MarkRescued()
        {
            if (isRescued || isDead) return;
            isRescued = true;
            isFollowing = false;
            agent.isStopped = true;
            GameEvents.TriggerCivilianRescued();
            GameEvents.TriggerMissionCompleted("mission_rescue_civilian", "node_civilian_safe");
            Debug.Log("[CivilianFollower] Sivil güvenli sığınağa ulaştı!");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isRescued || isDead) return;

            if (other.name == "Zone_SafeHouse" || other.CompareTag("SafeHouse"))
            {
                MarkRescued();
            }
        }
    }
}
