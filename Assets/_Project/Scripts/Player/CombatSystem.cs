using System;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.Player
{
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        [Header("Takedown Parameters")]
        [SerializeField] private float takedownRange = 2.0f;
        [SerializeField] private float behindDotThreshold = 0.35f;
        [SerializeField] private LayerMask zombieLayer = ~0;

        [Header("Emergency Push Parameters")]
        [SerializeField] private float pushRange = 2.2f;
        [SerializeField] private float pushAngle = 75f;
        [SerializeField] private float pushStaminaCost = 25f;
        [SerializeField] private float pushKnockbackDistance = 2.5f;
        [SerializeField] private float pushNoiseRadius = 15f;

        private StealthSystem stealthSystem;
        private SurvivalStats survivalStats;
        private ZombieAI currentTakedownTarget;
        private ProjectSurvival.AI.HumanRaiderAI currentRaiderTakedownTarget;
        private bool canTakedown = false;

        public bool CanTakedown => canTakedown;
        public ZombieAI TakedownTarget => currentTakedownTarget;

        public event Action<bool, ZombieAI> OnTakedownAvailabilityChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            stealthSystem = GetComponent<StealthSystem>();
            survivalStats = GetComponent<SurvivalStats>();
        }

        private void Update()
        {
            CheckStealthTakedown();
        }

        private void CheckStealthTakedown()
        {
            bool wasAvailable = canTakedown;
            canTakedown = false;
            currentTakedownTarget = null;
            currentRaiderTakedownTarget = null;

            if (stealthSystem == null || !stealthSystem.IsCrouched)
            {
                if (wasAvailable) OnTakedownAvailabilityChanged?.Invoke(false, null);
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, takedownRange, zombieLayer);
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                ZombieAI zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie == null || zombie.IsDead) continue;

                // Check if zombie is in active combat with player
                if (zombie.CurrentState == ZombieState.Chase || zombie.CurrentState == ZombieState.Attack)
                {
                    continue;
                }

                // Check angle: player must be behind the zombie
                Vector3 toZombie = (zombie.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(zombie.transform.forward, toZombie);

                if (dot > behindDotThreshold)
                {
                    float dist = Vector3.Distance(transform.position, zombie.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        currentTakedownTarget = zombie;
                        canTakedown = true;
                    }
                }
            }

            // Also check for unaware Human Raiders
            var raiders = ProjectSurvival.AI.HumanRaiderAI.ActiveRaiders;
            if (raiders != null)
            {
                for (int i = 0; i < raiders.Count; i++)
                {
                    var raider = raiders[i];
                    if (raider == null || raider.IsDead) continue;
                    if (raider.CurrentState != ProjectSurvival.AI.RaiderState.Patrol) continue;

                    float dist = Vector3.Distance(transform.position, raider.transform.position);
                    if (dist <= takedownRange && dist < closestDist)
                    {
                        Vector3 toRaider = (raider.transform.position - transform.position).normalized;
                        float dot = Vector3.Dot(raider.transform.forward, toRaider);
                        if (dot > behindDotThreshold)
                        {
                            closestDist = dist;
                            currentRaiderTakedownTarget = raider;
                            currentTakedownTarget = null;
                            canTakedown = true;
                        }
                    }
                }
            }

            if (canTakedown != wasAvailable)
            {
                OnTakedownAvailabilityChanged?.Invoke(canTakedown, currentTakedownTarget);
            }
        }

        public bool ExecuteTakedown()
        {
            if (!canTakedown) return false;

            // 1. Raider Takedown
            if (currentRaiderTakedownTarget != null && !currentRaiderTakedownTarget.IsDead)
            {
                var raiderTarget = currentRaiderTakedownTarget;
                raiderTarget.TakeDamage(9999f);
                GameEvents.TriggerStealthTakedownPerformed(raiderTarget.gameObject);
                canTakedown = false;
                currentRaiderTakedownTarget = null;
                OnTakedownAvailabilityChanged?.Invoke(false, null);
                return true;
            }

            // 2. Zombie Takedown
            if (currentTakedownTarget == null || currentTakedownTarget.IsDead)
            {
                return false;
            }

            ZombieAI target = currentTakedownTarget;

            // Check if player has shiv (uses 1 shiv if in inventory)
            bool hasShiv = false;
            if (InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_shiv", 1))
            {
                InventorySystem.Instance.RemoveItem("item_shiv", 1);
                hasShiv = true;
            }

            if (target.Type == ZombieType.Tank)
            {
                // Tank cannot be instant-killed from stealth; deal massive damage and stun
                float dmg = hasShiv ? 120f : 80f;
                target.TakeDamage(dmg);
                target.Stun(3.5f);
                GameEvents.TriggerCameraShakeRequested(0.45f, 0.35f);
                Debug.Log($"[CombatSystem] Stealth ambush on Tank Zombie! Dealt {dmg} damage and stunned!");
            }
            else
            {
                // Instant stealth takedown on Standard, Runner, Screamer
                target.TakeDamage(9999f);
                Debug.Log($"[CombatSystem] Stealth takedown executed on {target.name}! Silent elimination.");
            }

            GameEvents.TriggerStealthTakedownPerformed(target.gameObject);
            canTakedown = false;
            currentTakedownTarget = null;
            OnTakedownAvailabilityChanged?.Invoke(false, null);
            return true;
        }

        public bool ExecuteEmergencyPush()
        {
            if (survivalStats == null || !survivalStats.ConsumeStamina(pushStaminaCost))
            {
                Debug.LogWarning("[CombatSystem] Yetersiz stamina! İtme gerçekleştirilemedi.");
                return false;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, pushRange, zombieLayer);
            int pushedCount = 0;

            foreach (var hit in hits)
            {
                ZombieAI zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie == null || zombie.IsDead) continue;

                Vector3 toZombie = (zombie.transform.position - transform.position);
                toZombie.y = 0;
                float angle = Vector3.Angle(transform.forward, toZombie.normalized);

                if (angle <= pushAngle)
                {
                    zombie.ApplyKnockback(transform.forward, pushKnockbackDistance);
                    zombie.TakeDamage(12f);
                    pushedCount++;
                }
            }

            // Emits loud noise that alerts nearby horde
            GameEvents.TriggerNoiseEmitted(transform.position, pushNoiseRadius);
            GameEvents.TriggerCameraShakeRequested(0.3f, 0.2f);
            GameEvents.TriggerEmergencyPushPerformed();

            Debug.Log($"[CombatSystem] Acil durum itmesi yapıldı! {pushedCount} zombi savruldu, {pushNoiseRadius}m ses yayıldı.");
            return true;
        }
    }
}
