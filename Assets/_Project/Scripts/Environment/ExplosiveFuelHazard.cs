using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Active Phase 6: Chain-Reaction Explosive Fuel Barrels & Gas Station Pumps.
    /// Has 45 HP; ignites on firearm shots, axe cleave, blast splash, or vehicular ramming.
    /// Emits a 0.35s pressurized hiss/spark jet before detonating in a 7.5m radial blast
    /// (220 damage, launches zombies into 480N ragdoll flight, 45 self-damage if player < 4.5m),
    /// triggers adjacent barrels within 6.5m (0.22s staggered delay) for chain reactions,
    /// and broadcasts a 55m acoustic lure via HordeAlertSystem.
    /// </summary>
    public class ExplosiveFuelHazard : MonoBehaviour
    {
        [Header("Hazard Identity")]
        [SerializeField] private string hazardName = "Yakıt Varili (Patlayıcı)";
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float currentHealth = 45f;

        public string HazardName => hazardName;

        [Header("Explosion Tuning")]
        [SerializeField] private float explosionRadius = 7.5f;
        [SerializeField] private float explosionDamage = 220f;
        [SerializeField] private float selfDamageRadius = 4.5f;
        [SerializeField] private float selfDamage = 45f;
        [SerializeField] private float ragdollBlastImpulse = 480f;
        [SerializeField] private float chainReactionRadius = 6.5f;
        [SerializeField] private float chainReactionDelay = 0.22f;
        [SerializeField] private float hordeAlertRadius = 55.0f;

        [Header("State")]
        [SerializeField] private bool isFuseLit = false;
        [SerializeField] private bool isDetonated = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsFuseLit => isFuseLit;
        public bool IsDetonated => isDetonated;
        public float ExplosionRadius => explosionRadius;

        private Collider mainCollider;
        private Renderer[] cachedRenderers;
        private MaterialPropertyBlock propBlock;

        private void Awake()
        {
            mainCollider = GetComponent<Collider>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            propBlock = new MaterialPropertyBlock();
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitDirection = default)
        {
            if (isDetonated || isFuseLit) return;

            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                IgniteFuseAndDetonate(0.35f);
            }
            else
            {
                // Subtle hit spark
                FlashWarningColor();
            }
        }

        private void FlashWarningColor()
        {
            if (cachedRenderers == null || propBlock == null) return;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == null) continue;
                cachedRenderers[i].GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.05f) * 1.5f);
                cachedRenderers[i].SetPropertyBlock(propBlock);
            }
        }

        public void TriggerChainReaction(float delay)
        {
            if (isDetonated || isFuseLit) return;
            IgniteFuseAndDetonate(delay);
        }

        public void IgniteFuseAndDetonate(float fuseDuration = 0.35f)
        {
            if (isDetonated || isFuseLit) return;
            isFuseLit = true;
            StartCoroutine(FuseSequence(fuseDuration));
        }

        private IEnumerator FuseSequence(float duration)
        {
            // Pressurized hiss audio + spark jet feedback
            ProceduralAudioManager.Instance?.PlayBoilingSteam();
            FlashWarningColor();

            yield return new WaitForSeconds(duration);

            DetonateImmediate();
        }

        public void DetonateImmediate()
        {
            if (isDetonated) return;
            isDetonated = true;
            isFuseLit = false;

            Vector3 blastCenter = transform.position + Vector3.up * 0.6f;

            // 1. Audio, Haptics & Camera Shake
            ProceduralAudioManager.Instance?.PlayExplosion();
            MobileHapticManager.TriggerHeavy();
            GameEvents.TriggerCameraShakeRequested(0.55f, 0.35f);

            // 2. 55m Acoustic Alert to Horde
            HordeAlertSystem.Instance?.BroadcastAcousticLure(blastCenter, hordeAlertRadius);
            GameEvents.TriggerNoiseEmitted(blastCenter, hordeAlertRadius);

            // 3. Spawn URP Fireball & Smoke VFX
            SpawnExplosionVFX(blastCenter);

            // 4. OverlapSphere Radial Blast Physics & Damage
            Collider[] hits = Physics.OverlapSphere(blastCenter, explosionRadius);
            List<ExplosiveFuelHazard> chainHazards = new List<ExplosiveFuelHazard>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;

                Vector3 targetPos = hit.transform.position;
                float dist = Vector3.Distance(blastCenter, targetPos);
                Vector3 blastDir = (targetPos - blastCenter).normalized;
                if (blastDir == Vector3.zero) blastDir = Vector3.up;

                // Falloff damage calculation
                float falloffPct = Mathf.Clamp01(1f - (dist / explosionRadius));
                float finalDamage = Mathf.Lerp(explosionDamage * 0.35f, explosionDamage, falloffPct);

                // A. Check Zombies -> Damage & Ragdoll Launch
                ZombieAI ai = hit.GetComponentInParent<ZombieAI>();
                if (ai != null && !ai.IsDead)
                {
                    ai.SetLastHitDetails(blastDir, targetPos, "Blast", false);
                    ai.TakeDamage(finalDamage, targetPos, -blastDir);

                    var ragdoll = ai.GetComponent<ZombieRagdollController>();
                    if (ragdoll != null && ragdoll.IsRagdollActive)
                    {
                        Vector3 launchForce = (blastDir + Vector3.up * 0.5f).normalized * ragdollBlastImpulse;
                        ragdoll.TriggerRagdoll(launchForce, targetPos);
                    }
                    continue;
                }

                // Check Mutant Boss
                MutantBossAI boss = hit.GetComponentInParent<MutantBossAI>();
                if (boss != null && !boss.IsDead)
                {
                    boss.TakeDamage(finalDamage * 0.85f, targetPos, blastDir);
                    continue;
                }

                // B. Check Player Proximity (< 4.5m) -> 45 Self-Damage
                if (dist <= selfDamageRadius && hit.CompareTag("Player"))
                {
                    SurvivalStats stats = hit.GetComponentInParent<SurvivalStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(selfDamage, transform.position);
                    }
                    continue;
                }

                // C. Check Destructible Props
                DestructibleProp prop = hit.GetComponentInParent<DestructibleProp>();
                if (prop != null && !prop.IsDestroyed)
                {
                    prop.TakeDamage(finalDamage, targetPos, blastDir);
                    continue;
                }

                // D. Check Adjacent Fuel Hazards (< 6.5m) -> Chain Reaction
                ExplosiveFuelHazard adj = hit.GetComponentInParent<ExplosiveFuelHazard>();
                if (adj != null && adj != this && !adj.IsDetonated && !adj.IsFuseLit)
                {
                    if (dist <= chainReactionRadius && !chainHazards.Contains(adj))
                    {
                        chainHazards.Add(adj);
                    }
                }
            }

            // Trigger chain reaction on adjacent hazards with staggered delay
            for (int c = 0; c < chainHazards.Count; c++)
            {
                float delay = chainReactionDelay * (c + 1);
                chainHazards[c].TriggerChainReaction(delay);
            }

            // 5. Visual Destruction & Collider Disable
            if (mainCollider != null) mainCollider.enabled = false;

            // Hide normal renderers or switch to scorched charred remains
            if (cachedRenderers != null)
            {
                for (int r = 0; r < cachedRenderers.Length; r++)
                {
                    if (cachedRenderers[r] != null) cachedRenderers[r].enabled = false;
                }
            }

            SpawnScorchedCylinder(blastCenter);
            Debug.Log($"[{gameObject.name}] {hazardName} detonated! 7.5m blast, 55m horde alert, triggered {chainHazards.Count} chain reactions.");
        }

        private void SpawnScorchedCylinder(Vector3 center)
        {
            GameObject scorched = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scorched.name = "Scorched_Barrel_Remains";
            scorched.transform.position = transform.position;
            scorched.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);

            var col = scorched.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = scorched.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material scorchedMat = new Material(s);
                scorchedMat.color = new Color(0.12f, 0.10f, 0.10f);
                scorchedMat.enableInstancing = true;
                rend.sharedMaterial = scorchedMat;
            }

            Destroy(scorched, 15f);
        }

        private void SpawnExplosionVFX(Vector3 pos)
        {
            GameObject fx = new GameObject("Explosion_Fireball_VFX");
            fx.transform.position = pos;

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 9f;
            main.startSize = 2.4f;
            main.startColor = new Color(1.0f, 0.42f, 0.08f, 0.95f); // Fiery orange/yellow
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            var renderer = fx.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null)
            {
                Material m = new Material(s);
                m.enableInstancing = true;
                renderer.sharedMaterial = m;
            }

            Destroy(fx, 2.5f);
        }
    }
}
