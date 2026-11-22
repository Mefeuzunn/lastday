using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.Survival
{
    public class SurvivalStats : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaSprintDrainRate = 18f;
        [SerializeField] private float staminaRegenRate = 12f;
        [SerializeField] private float staminaRegenBuffTimer = 0f;
        [SerializeField] private float staminaRegenBuffMult = 1.6f;

        public bool HasStaminaRegenBuff => staminaRegenBuffTimer > 0f;

        public void ApplyStaminaRegenBuff(float duration, float mult = 1.6f)
        {
            staminaRegenBuffTimer = Mathf.Max(staminaRegenBuffTimer, duration);
            staminaRegenBuffMult = mult;
        }

        [Header("Hunger & Thirst Settings")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger = 100f;
        [SerializeField] private float hungerDrainRate = 0.4f; // per second

        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float currentThirst = 100f;
        [SerializeField] private float thirstDrainRate = 0.6f; // per second

        [SerializeField] private float starvationDamageRate = 1.5f;

        [Header("Infection Settings")]
        [SerializeField] private float maxInfection = 100f;
        [SerializeField] private float currentInfection = 0f;
        [SerializeField] private float infectionDamageRate = 2f;

        private bool isDead = false;

        public float Health => currentHealth;
        public float Stamina => currentStamina;
        public float Hunger => currentHunger;
        public float Thirst => currentThirst;
        public float Infection => currentInfection;

        public float MaxHealth => maxHealth;
        public float MaxStamina => maxStamina;
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;
        public float MaxInfection => maxInfection;

        public float CurrentHealth => currentHealth;
        public float CurrentStamina => currentStamina;
        public float CurrentHunger => currentHunger;
        public float CurrentThirst => currentThirst;
        public float CurrentInfection => currentInfection;

        public void SetMaxHealthBonus(float bonus)
        {
            maxHealth = 100f + bonus;
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            GameEvents.TriggerHealthChanged(currentHealth, maxHealth);
        }

        public void SetMaxStaminaBonus(float bonus)
        {
            maxStamina = 100f + bonus;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
            GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
        }

        public void DrainStamina(float amount)
        {
            ConsumeStamina(amount);
        }

        public static SurvivalStats Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (GetComponent<TraumaAndInjurySystem>() == null)
            {
                gameObject.AddComponent<TraumaAndInjurySystem>();
            }

            isDead = false;
            currentHealth = maxHealth > 0f ? maxHealth : 100f;
            currentStamina = maxStamina > 0f ? maxStamina : 100f;
            currentHunger = maxHunger > 0f ? maxHunger : 100f;
            currentThirst = maxThirst > 0f ? maxThirst : 100f;
            currentInfection = 0f;
        }

        private void Start()
        {
            isDead = false;
            currentHealth = maxHealth > 0f ? maxHealth : 100f;
            currentStamina = maxStamina > 0f ? maxStamina : 100f;
            currentHunger = maxHunger > 0f ? maxHunger : 100f;
            currentThirst = maxThirst > 0f ? maxThirst : 100f;
            currentInfection = 0f;

            PublishAllStats();
        }

        private void Update()
        {
            if (isDead) return;

            if (staminaRegenBuffTimer > 0f)
            {
                staminaRegenBuffTimer -= Time.deltaTime;
            }

            SimulateDecay(Time.deltaTime);
            RegenerateStamina(Time.deltaTime);
        }

        private void SimulateDecay(float dt)
        {
            float decayMult = 1.0f;
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                decayMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetHungerThirstDrainMultiplier();
            }

            // Drain Hunger & Thirst
            currentHunger = Mathf.Max(0f, currentHunger - (hungerDrainRate * decayMult * dt));
            currentThirst = Mathf.Max(0f, currentThirst - (thirstDrainRate * decayMult * dt));

            // Starvation & Dehydration damage only if depleted
            if (currentHunger <= 0f || currentThirst <= 0f)
            {
                TakeDamage(starvationDamageRate * dt);
            }

            // Infection damage if critical
            if (currentInfection >= 70f)
            {
                TakeDamage(infectionDamageRate * dt);
            }

            GameEvents.TriggerHungerChanged(currentHunger, maxHunger);
            GameEvents.TriggerThirstChanged(currentThirst, maxThirst);
        }

        private void RegenerateStamina(float dt)
        {
            if (currentStamina < maxStamina)
            {
                float feverMult = (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasAcuteFever) ? 0.50f : 1.0f;
                float buffMult = staminaRegenBuffTimer > 0f ? staminaRegenBuffMult : 1.0f;
                currentStamina = Mathf.Min(maxStamina, currentStamina + (staminaRegenRate * feverMult * buffMult * dt));
                GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
            }
        }

        public bool CanSprint()
        {
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                // Adrenaline rush temporarily overrides leg fracture sprint lock
                return true;
            }

            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasLegFracture)
            {
                return false;
            }
            return currentStamina > 5f;
        }

        public void ConsumeStaminaForSprint(float dt)
        {
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                // Unlimited stamina during adrenaline surge
                currentStamina = maxStamina;
                GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
                return;
            }

            float mult = 1.0f;
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.HasAcuteFever) mult *= 1.45f;
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.IsHerbalTeaActive) mult *= 0.80f;

            currentStamina = Mathf.Max(0f, currentStamina - (staminaSprintDrainRate * mult * dt));
            GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
        }

        public bool ConsumeStamina(float amount)
        {
            if (TraumaAndInjurySystem.Instance != null && TraumaAndInjurySystem.Instance.IsAdrenalineActive)
            {
                return true;
            }

            if (currentStamina < amount) return false;
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (isDead || amount <= 0f) return;
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f) return;

            // Tactical Armor absorbs 35% of incoming damage if active
            if (PlayerArmorController.Instance != null && PlayerArmorController.Instance.HasArmor)
            {
                amount = PlayerArmorController.Instance.MitigateDamage(amount);
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            GameEvents.TriggerHealthChanged(currentHealth, maxHealth);
            GameEvents.TriggerPlayerDamaged(amount);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void TakeDamage(float amount, Vector3 attackerPosition)
        {
            if (ProjectSurvival.UI.DirectionalDamageHUD.Instance != null)
            {
                ProjectSurvival.UI.DirectionalDamageHUD.Instance.ShowThreat(attackerPosition);
            }
            TakeDamage(amount);
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f) return;
            float healMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetHealingEfficiencyMultiplier() : 1.0f;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount * healMult);
            GameEvents.TriggerHealthChanged(currentHealth, maxHealth);
        }

        public void Feed(float amount)
        {
            float feedMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFoodWaterRestoreMultiplier() : 1.0f;
            currentHunger = Mathf.Min(maxHunger, currentHunger + amount * feedMult);
            GameEvents.TriggerHungerChanged(currentHunger, maxHunger);
        }

        public void Hydrate(float amount)
        {
            float drinkMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFoodWaterRestoreMultiplier() : 1.0f;
            currentThirst = Mathf.Min(maxThirst, currentThirst + amount * drinkMult);
            GameEvents.TriggerThirstChanged(currentThirst, maxThirst);
        }

        public void ModifyInfection(float amount)
        {
            // Tactical Armor blocks single-hit infection spikes while intact
            if (amount > 0f && PlayerArmorController.Instance != null && PlayerArmorController.Instance.HasArmor)
            {
                return;
            }

            if (amount > 0f && ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                amount *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetInfectionBuildupMultiplier();
            }

            currentInfection = Mathf.Clamp(currentInfection + amount, 0f, maxInfection);
            GameEvents.TriggerInfectionChanged(currentInfection, maxInfection);
        }

        private void Die()
        {
            if (isDead) return;
            if (currentHealth > 0f) return; // Guard against false death

            isDead = true;
            GameEvents.TriggerPlayerDied();
            Debug.Log("[SurvivalStats] Player has perished!");
        }

        public void SetStats(float health, float hunger, float thirst, float infection)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
            currentThirst = Mathf.Clamp(thirst, 0f, maxThirst);
            currentInfection = Mathf.Clamp(infection, 0f, maxInfection);
            PublishAllStats();
        }

        public void PublishAllStats()
        {
            GameEvents.TriggerHealthChanged(currentHealth, maxHealth);
            GameEvents.TriggerStaminaChanged(currentStamina, maxStamina);
            GameEvents.TriggerHungerChanged(currentHunger, maxHunger);
            GameEvents.TriggerThirstChanged(currentThirst, maxThirst);
            GameEvents.TriggerInfectionChanged(currentInfection, maxInfection);
        }
    }
}
