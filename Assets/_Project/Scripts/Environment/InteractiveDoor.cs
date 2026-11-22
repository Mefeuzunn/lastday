using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.Environment
{
    public class InteractiveDoor : MonoBehaviour
    {
        [Header("Door Configuration")]
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4.5f;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string requiredKeyId = "";

        [Header("Durability & Zombie Breaking")]
        [SerializeField] private float doorHealth = 100f;
        [SerializeField] private float maxDoorHealth = 100f;
        [SerializeField] private bool isBroken = false;

        [Header("Audio & Feedback")]
        [SerializeField] private string doorName = "Kapı";

        private Quaternion closedRotation;
        private Quaternion targetRotation;
        private NavMeshObstacle navObstacle;
        private bool isPlayerNearby = false;
        private float lastPoundSoundTime = 0f;

        public const int MaxPounders = 3;
        private readonly List<ZombieAI> registeredPounders = new List<ZombieAI>();

        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;
        public string RequiredKeyId => requiredKeyId;
        public bool IsBroken => isBroken;
        public float DoorHealth => doorHealth;
        public float MaxDoorHealth => maxDoorHealth;
        public bool CanAcceptPounder => !isOpen && !isBroken && registeredPounders.Count < MaxPounders;

        private void Awake()
        {
            if (doorHinge == null) doorHinge = transform;
            closedRotation = doorHinge.localRotation;
            targetRotation = closedRotation;
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null) navObstacle = doorHinge.GetComponent<NavMeshObstacle>();

            UpdateObstacle();
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += HandleInteractPressed;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= HandleInteractPressed;
        }

        private void Update()
        {
            // Smooth hinge rotation
            if (doorHinge != null && Quaternion.Angle(doorHinge.localRotation, targetRotation) > 0.1f)
            {
                doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRotation, Time.deltaTime * openSpeed);
            }
        }

        private void HandleInteractPressed()
        {
            if (!isPlayerNearby || isBroken) return;
            ToggleDoor();
        }

        public void ToggleDoor()
        {
            if (isBroken) return;

            if (isLocked)
            {
                string msg = string.IsNullOrEmpty(requiredKeyId) ? "Kapı kilitli!" : $"Kapı kilitli! ({requiredKeyId} gerekli)";
                GameEvents.TriggerObjectiveToastRequested(msg, 2.5f);
                return;
            }

            isOpen = !isOpen;
            float angle = isOpen ? openAngle : 0f;
            targetRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);

            UpdateObstacle();
            UpdatePrompt();
            Debug.Log($"[InteractiveDoor] {doorName} is now {(isOpen ? "OPEN" : "CLOSED")}");
        }

        public bool RegisterPounder(ZombieAI zombie)
        {
            if (isBroken || isOpen || zombie == null) return false;
            if (registeredPounders.Count >= MaxPounders) return false;
            if (!registeredPounders.Contains(zombie))
            {
                registeredPounders.Add(zombie);
            }
            return true;
        }

        public void UnregisterPounder(ZombieAI zombie)
        {
            if (zombie != null) registeredPounders.Remove(zombie);
        }

        /// <summary>
        /// Multi-attacker door pounding: Standard/Runner deals 12 damage/sec, Tank deals 45 damage/sec.
        /// </summary>
        public void PoundDoor(ZombieAI zombie, float dt)
        {
            if (isBroken || isOpen) return;

            float dps = (zombie != null && zombie.Type == ZombieType.Tank) ? 45f : 12f;
            doorHealth -= dps * dt;

            // Rhythmic impact sound & door shake
            if (Time.time >= lastPoundSoundTime + 0.38f)
            {
                lastPoundSoundTime = Time.time;
                ProceduralAudioManager.Instance?.PlayDoorPoundImpact(doorHinge.position, isMetal: false);
                GameEvents.TriggerCameraShakeRequested(0.18f, 0.12f);
                SpawnSplinters(doorHinge.position + Vector3.up * 1.1f);

                // Slight tactile door shudder
                doorHinge.localRotation = closedRotation * Quaternion.Euler(0f, Random.Range(-2.5f, 2.5f), 0f);
            }

            if (doorHealth <= 0f)
            {
                BreakDoor();
            }
        }

        public void TakeDoorDamage(float damage)
        {
            if (isBroken || isOpen) return;

            doorHealth -= damage;
            GameEvents.TriggerCameraShakeRequested(0.15f, 0.1f);

            if (doorHealth <= 0f)
            {
                BreakDoor();
            }
        }

        public void RepairDoor(float amount)
        {
            if (amount <= 0f) return;

            doorHealth = Mathf.Min(doorHealth + amount, maxDoorHealth);
            if (isBroken && doorHealth >= maxDoorHealth * 0.4f)
            {
                isBroken = false;
                isOpen = false;
                targetRotation = closedRotation;
                UpdateObstacle();
                UpdatePrompt();
            }

            Debug.Log($"[InteractiveDoor] {doorName} repaired (+{amount} HP). Current: {doorHealth}/{maxDoorHealth}");
        }

        private void BreakDoor()
        {
            isBroken = true;
            isOpen = true;
            doorHealth = 0f;

            // Violent inward swing
            targetRotation = closedRotation * Quaternion.Euler(0f, openAngle * 1.35f, 0f);
            UpdateObstacle();

            // Large splinter burst effect
            SpawnSplinters(doorHinge.position + Vector3.up * 1f, burst: true);
            GameEvents.TriggerCameraShakeRequested(0.4f, 0.25f);
            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(doorHinge.position, isMetal: false);

            if (isPlayerNearby)
            {
                GameEvents.TriggerInteractionPromptChanged(false, "");
            }

            // Notify all waiting pounders so they flood the interior
            for (int i = 0; i < registeredPounders.Count; i++)
            {
                ZombieAI z = registeredPounders[i];
                if (z != null) z.OnDoorBreached();
            }
            registeredPounders.Clear();

            Debug.Log($"[InteractiveDoor] {doorName} was completely breached by zombie horde!");
        }

        private void SpawnSplinters(Vector3 pos, bool burst = false)
        {
            int count = burst ? 4 : 1;
            for (int i = 0; i < count; i++)
            {
                GameObject splinters = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                splinters.transform.position = pos + Random.insideUnitSphere * (burst ? 0.4f : 0.15f);
                splinters.transform.localScale = Vector3.one * (burst ? 0.28f : 0.16f);
                Collider c = splinters.GetComponent<Collider>();
                if (c != null) Destroy(c);
                Renderer r = splinters.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    m.color = new Color(0.52f, 0.34f, 0.18f);
                    r.sharedMaterial = m;
                }
                Destroy(splinters, burst ? 0.45f : 0.25f);
            }
        }

        private void UpdateObstacle()
        {
            if (navObstacle != null)
            {
                navObstacle.carving = !isOpen && !isBroken;
                navObstacle.enabled = !isOpen && !isBroken;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
            {
                isPlayerNearby = true;
                UpdatePrompt();
            }
            else if ((other.GetComponentInParent<ZombieAI>() != null || other.GetComponentInParent<ZombieBrain>() != null || other.name.Contains("Zombie")) && !isOpen && !isBroken)
            {
                ZombieAI zombie = other.GetComponentInParent<ZombieAI>();
                if (zombie != null && zombie.CurrentState == ZombieState.Chase)
                {
                    RegisterPounder(zombie);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
            {
                isPlayerNearby = false;
                GameEvents.TriggerInteractionPromptChanged(false, "");
            }
            else if (other.GetComponentInParent<ZombieAI>() != null || other.GetComponentInParent<ZombieBrain>() != null || other.name.Contains("Zombie"))
            {
                ZombieAI zombie = other.GetComponentInParent<ZombieAI>();
                if (zombie != null)
                {
                    UnregisterPounder(zombie);
                }
            }
        }

        private void UpdatePrompt()
        {
            if (!isPlayerNearby || isBroken) return;
            string msg = isOpen ? "KAPIYI KAPAT (E)" : "KAPIYI AÇ (E)";
            GameEvents.TriggerInteractionPromptChanged(true, msg);
        }
    }
}
