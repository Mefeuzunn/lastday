using UnityEngine;

namespace ProjectSurvival.AI
{
    public enum HitZone
    {
        Head,
        Torso,
        Legs
    }

    public struct HitZoneDamageResult
    {
        public HitZone Zone;
        public float BaseDamage;
        public float FinalDamage;
        public float DamageMultiplier;
        public bool IsCritical;
        public bool IsHeadshot;
        public bool IsLimbHit;
        public float HeightRatio;
    }

    /// <summary>
    /// Tactical Hit-Zone and Anatomical Detection Resolver.
    /// Uses 0 additional colliders, computing precise hit zones from capsule height ratios:
    /// - Head: ratio >= 0.82 (2.5x critical damage, head-snap stagger, Screamer shriek cancellation)
    /// - Torso/Arms: 0.38 <= ratio < 0.82 (1.0x base damage, 1.1m knockback impulse, melee timer reset)
    /// - Legs/Lower Body: ratio < 0.38 (0.85x base damage, accumulates leg trauma -> Cripple at 35 trauma)
    /// </summary>
    public static class ZombieHitZoneResolver
    {
        public const float HeadThreshold = 0.82f;
        public const float TorsoThreshold = 0.38f;

        public const float HeadDamageMultiplier = 2.5f;
        public const float TorsoDamageMultiplier = 1.0f;
        public const float LegsDamageMultiplier = 0.85f;

        public const float LegTraumaThreshold = 35.0f;
        public const float CrippleSpeedReductionFactor = 0.45f; // 55% speed reduction -> 45% remaining speed
        public const float HeadSnapStaggerDuration = 0.25f;
        public const float TorsoKnockbackDistance = 1.1f;

        /// <summary>
        /// Resolves the anatomical hit zone from the 3D contact point relative to the zombie root.
        /// </summary>
        public static HitZoneDamageResult ResolveHit(float rawDamage, Vector3 hitPoint, Vector3 rootPosition, float agentHeight)
        {
            if (agentHeight <= 0.1f) agentHeight = 1.8f;

            float localHitY = hitPoint.y - rootPosition.y;
            float hRatio = Mathf.Clamp01(localHitY / agentHeight);

            HitZoneDamageResult result = new HitZoneDamageResult
            {
                BaseDamage = rawDamage,
                HeightRatio = hRatio
            };

            if (hRatio >= HeadThreshold)
            {
                float headMult = HeadDamageMultiplier;
                if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
                {
                    headMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetHeadshotMultiplier();
                }
                result.Zone = HitZone.Head;
                result.DamageMultiplier = headMult;
                result.FinalDamage = rawDamage * headMult;
                result.IsCritical = true;
                result.IsHeadshot = true;
                result.IsLimbHit = false;
            }
            else if (hRatio >= TorsoThreshold)
            {
                result.Zone = HitZone.Torso;
                result.DamageMultiplier = TorsoDamageMultiplier;
                result.FinalDamage = rawDamage * TorsoDamageMultiplier;
                result.IsCritical = false;
                result.IsHeadshot = false;
                result.IsLimbHit = false;
            }
            else
            {
                result.Zone = HitZone.Legs;
                result.DamageMultiplier = LegsDamageMultiplier;
                result.FinalDamage = rawDamage * LegsDamageMultiplier;
                result.IsCritical = false;
                result.IsHeadshot = false;
                result.IsLimbHit = true;
            }

            return result;
        }
    }
}
