using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Phase 1: Front Bumper Kinetic Impact Sensor.
    /// Detects vehicular collisions at forward speeds >= 22 km/h, executes kinetic roadkill
    /// (140-280 speed-scaled damage) on standard/runner/screamer zombies, inflicts heavy stagger
    /// on tanks/bosses, triggers heavy haptics, metallic crash SFX, and applies 4.0f engine wear
    /// (reduced by 60% to 1.6f on the Armored SUV bullbar).
    /// </summary>
    public class VehicleImpactSensor : MonoBehaviour
    {
        [Header("Vehicle Reference")]
        [SerializeField] private DrivableVehicleController vehicleController;

        [Header("Impact Thresholds")]
        [SerializeField] private float minRammingSpeedKmh = 22f;
        [SerializeField] private float minDamage = 140f; // at ~25 km/h
        [SerializeField] private float maxDamage = 280f; // at >= 55 km/h
        [SerializeField] private float baseRammingWear = 4.0f;

        private float lastImpactTime = 0f;
        private ParticleSystem impactVfx;

        private void Awake()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponentInParent<DrivableVehicleController>();
            }

            EnsureImpactVfx();
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleImpact(other.gameObject, other.ClosestPoint(transform.position));
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }

        private void HandleImpact(GameObject targetObj, Vector3 hitPoint)
        {
            if (vehicleController == null || !vehicleController.IsPlayerDriving) return;

            float speedKmh = vehicleController.CurrentSpeedKmh;
            if (speedKmh < minRammingSpeedKmh) return;

            // Debounce rapid multi-collider triggers on same frame
            if (Time.time - lastImpactTime < 0.22f) return;

            // Speed-scaled kinetic damage: 140 at 22-25 km/h to 280 at 55 km/h
            float t = Mathf.Clamp01((speedKmh - minRammingSpeedKmh) / (55f - minRammingSpeedKmh));
            float kineticDamage = Mathf.Lerp(minDamage, maxDamage, t);

            // 1. Check for Mutant Apex Boss
            MutantBossAI boss = targetObj.GetComponentInParent<MutantBossAI>();
            if (boss != null && !boss.IsDead)
            {
                lastImpactTime = Time.time;
                Vector3 knockbackDir = (boss.transform.position - transform.position).normalized + Vector3.up * 0.25f;
                boss.TakeDamage(kineticDamage * 0.75f, hitPoint, knockbackDir);

                ApplyRammingFeedback(isBoss: true, kineticDamage, hitPoint);
                return;
            }

            // 2. Check for Standard, Runner, Screamer or Tank Zombie (ZombieAI / ZombieBrain)
            ZombieAI ai = targetObj.GetComponentInParent<ZombieAI>();
            if (ai != null && !ai.IsDead)
            {
                lastImpactTime = Time.time;
                Vector3 hitDir = (ai.transform.position - transform.position).normalized;
                ai.SetLastHitDetails(vehicleController.transform.forward, hitPoint, "Vehicle", false);
                ai.TakeDamage(kineticDamage, hitPoint, -hitDir);
                ai.ApplyKnockback(hitDir, Mathf.Lerp(4f, 10f, t));

                ApplyRammingFeedback(isBoss: false, kineticDamage, hitPoint);
                return;
            }

            ZombieBrain zombie = targetObj.GetComponentInParent<ZombieBrain>();
            if (zombie != null && !zombie.IsDead)
            {
                lastImpactTime = Time.time;
                Vector3 hitDir = (zombie.transform.position - transform.position).normalized;
                zombie.TakeDamage(kineticDamage, hitPoint, -hitDir);

                ApplyRammingFeedback(isBoss: false, kineticDamage, hitPoint);
                return;
            }

            // 3. Check for DestructibleProp (fences, crates, glass)
            var prop = targetObj.GetComponentInParent<ProjectSurvival.Environment.DestructibleProp>();
            if (prop != null && !prop.IsDestroyed)
            {
                lastImpactTime = Time.time;
                prop.TakeDamage(kineticDamage * 1.5f, hitPoint, vehicleController.transform.forward);
                ApplyRammingFeedback(isBoss: false, kineticDamage, hitPoint);
                return;
            }

            // 4. Check for ExplosiveFuelHazard (fuel barrels, gas pumps)
            var hazard = targetObj.GetComponentInParent<ProjectSurvival.Environment.ExplosiveFuelHazard>();
            if (hazard != null && !hazard.IsDetonated)
            {
                lastImpactTime = Time.time;
                hazard.TakeDamage(kineticDamage, hitPoint, vehicleController.transform.forward);
                ApplyRammingFeedback(isBoss: false, kineticDamage, hitPoint);
                return;
            }
        }

        private void ApplyRammingFeedback(bool isBoss, float damage, Vector3 hitPoint)
        {
            // Tactile Combat Juice & Hitstop
            ProjectSurvival.Combat.CombatJuiceController.Instance?.TriggerVehicleRamJuice(hitPoint);

            // Tactile Haptics
            MobileHapticManager.Instance?.TriggerHeavy();

            // Camera Shake
            float shakeIntensity = isBoss ? 0.45f : 0.32f;
            GameEvents.TriggerCameraShakeRequested(shakeIntensity, 0.25f);

            // Heavy metallic crunch & impact audio
            ProceduralAudioManager.Instance?.PlayVehicleCrash();

            // Spawn Hood-Impact Sparks / Blood-Mist Particles
            SpawnImpactVfx(hitPoint);

            // Engine Wear: 4.0f base wear; Armored SUV with reinforced steel bullbar gets 60% reduction (1.6f)
            float wear = baseRammingWear;
            if (vehicleController.VehicleType == HeroVehicleType.NorthHighway_ArmoredSUV)
            {
                wear *= 0.40f; // 60% bullbar protection
            }

            vehicleController.ApplyImpactDamage(wear);
            var maint = vehicleController.GetComponent<VehicleMaintenanceModule>();
            if (maint != null)
            {
                maint.ApplyImpactWear(wear);
            }

            // Toast Feedback
            string msg = isBoss
                ? $"[AĞIR ÇARPMA: BOSS {damage:F0} KİNETİK HASAR]"
                : $"[ARAÇLA EZME: {damage:F0} KİNETİK HASAR]";
            GameEvents.TriggerObjectiveToastRequested(msg, 2.0f);
        }

        private void EnsureImpactVfx()
        {
            Transform existing = transform.Find("Impact_VFX");
            if (existing != null)
            {
                impactVfx = existing.GetComponent<ParticleSystem>();
                return;
            }

            GameObject go = new GameObject("Impact_VFX");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 1.8f);

            impactVfx = go.AddComponent<ParticleSystem>();
            var main = impactVfx.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 7f;
            main.startSize = 0.22f;
            main.startColor = new Color(0.85f, 0.2f, 0.15f, 0.9f); // Crimson impact mist
            main.maxParticles = 32;
            main.playOnAwake = false;

            var emission = impactVfx.emission;
            emission.enabled = false;

            var shape = impactVfx.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.4f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        }

        private void SpawnImpactVfx(Vector3 pos)
        {
            if (impactVfx != null)
            {
                impactVfx.transform.position = pos;
                impactVfx.Emit(18);
            }
        }

        public void Configure(DrivableVehicleController controller)
        {
            vehicleController = controller;
        }
    }
}
