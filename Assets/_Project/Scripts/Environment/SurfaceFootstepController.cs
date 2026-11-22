using System;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Animation;
using ProjectSurvival.Player;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Phase 1 (Step 1C): Surface-Aware Footstep Audio, Particle VFX & Acoustic Propagation.
    /// Classifies ground under Player_Root into 5 distinct surfaces (AsphaltConcrete, GrassSoil,
    /// WoodInterior, MetalIndustrial, ShallowWater) via downward raycasting.
    /// Synchronizes footsteps with HumanoidProceduralAnimator stride cycles and TPSPlayerController speed,
    /// emits lightweight pooled URP step particles, scales acoustic stealth noise into HordeAlertSystem/NoiseEmitter,
    /// and handles heavy jump/vault landing impacts with camera dips.
    /// </summary>
    public class SurfaceFootstepController : MonoBehaviour
    {
        public static SurfaceFootstepController Instance { get; private set; }

        [Header("Surface Detection")]
        [SerializeField] private float rayDistance = 1.35f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float waterHeightThreshold = 0.08f;
        [SerializeField] private GroundSurfaceType currentSurface = GroundSurfaceType.AsphaltConcrete;

        [Header("Acoustic Noise Radii (Base in Meters)")]
        [SerializeField] private float crouchNoiseRadius = 1.8f;
        [SerializeField] private float walkNoiseRadius = 4.8f;
        [SerializeField] private float sprintNoiseRadius = 9.5f;
        [SerializeField] private float landingNoiseRadius = 12.0f;

        [Header("Cadence Tuning")]
        [SerializeField] private float walkStepInterval = 0.46f;
        [SerializeField] private float sprintStepInterval = 0.31f;
        [SerializeField] private float crouchStepInterval = 0.62f;

        [Header("Step Particle Pool (URP Unlit)")]
        [SerializeField] private ParticleSystem stepParticleSystem;
        [SerializeField] private int maxStepParticles = 80;

        // References
        private TPSPlayerController tpsController;
        private HumanoidProceduralAnimator humanoidAnimator;
        private NoiseEmitter noiseEmitter;
        private CharacterController characterController;

        private float stepTimer = 0f;
        private bool wasGrounded = true;
        private float peakAirborneY = 0f;
        private RaycastHit cachedHit;

        public GroundSurfaceType CurrentSurface => currentSurface;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Exclude Player and UI layers from ground probe
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) groundLayers &= ~(1 << playerLayer);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) groundLayers &= ~(1 << uiLayer);

            InitializeStepParticleEmitter();
        }

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (tpsController == null) tpsController = GetComponent<TPSPlayerController>() ?? FindFirstObjectByType<TPSPlayerController>();
            if (humanoidAnimator == null) humanoidAnimator = GetComponent<HumanoidProceduralAnimator>();
            if (noiseEmitter == null) noiseEmitter = GetComponent<NoiseEmitter>() ?? FindFirstObjectByType<NoiseEmitter>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        private void InitializeStepParticleEmitter()
        {
            if (stepParticleSystem != null) return;

            Transform existing = transform.Find("Step_Particle_Emitter");
            if (existing != null)
            {
                stepParticleSystem = existing.GetComponent<ParticleSystem>();
                return;
            }

            GameObject emitterObj = new GameObject("Step_Particle_Emitter");
            emitterObj.transform.SetParent(transform, false);
            emitterObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            stepParticleSystem = emitterObj.AddComponent<ParticleSystem>();
            var main = stepParticleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = maxStepParticles;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.8f;
            main.startSize = 0.18f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = stepParticleSystem.emission;
            emission.enabled = false;

            var shape = stepParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var renderer = stepParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");

            Material partMat = new Material(urpParticle);
            partMat.name = "Mat_StepDust_Instanced";
            partMat.enableInstancing = true;
            renderer.sharedMaterial = partMat;
        }

        private void Update()
        {
            FindReferences();

            bool isGrounded = characterController != null ? characterController.isGrounded : true;

            // Track airborne height for fall landing detection
            if (!isGrounded)
            {
                if (transform.position.y > peakAirborneY)
                {
                    peakAirborneY = transform.position.y;
                }
            }
            else
            {
                // Check if just landed
                if (!wasGrounded)
                {
                    float fallHeight = Mathf.Max(0f, peakAirborneY - transform.position.y);
                    if (fallHeight > 0.60f)
                    {
                        OnLanding(fallHeight, transform.position);
                    }
                    peakAirborneY = transform.position.y;
                }
            }
            wasGrounded = isGrounded;

            if (!isGrounded) return;

            // Movement footstep progression
            bool isMoving = tpsController != null && tpsController.IsMoving;
            if (isMoving)
            {
                bool isRunning = tpsController.IsSprinting;
                bool isCrouched = tpsController.IsCrouching;

                float currentInterval = isCrouched ? crouchStepInterval : (isRunning ? sprintStepInterval : walkStepInterval);
                stepTimer += Time.deltaTime;

                if (stepTimer >= currentInterval)
                {
                    stepTimer = 0f;
                    TriggerFootstep(isRunning, isCrouched);
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }

        /// <summary>
        /// Classifies the ground directly below the player into 5 distinct surfaces.
        /// </summary>
        public GroundSurfaceType DetectSurface(out RaycastHit hit)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.45f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                cachedHit = hit;
                string matName = "";
                Renderer r = hit.collider.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null) matName = r.sharedMaterial.name.ToLowerInvariant();
                string objName = hit.collider.gameObject.name.ToLowerInvariant();
                string parentName = hit.collider.transform.parent != null ? hit.collider.transform.parent.name.ToLowerInvariant() : "";

                // 1. Shallow Water (flooded district, puddles, stream)
                if (matName.Contains("water") || matName.Contains("puddle") || objName.Contains("water") || objName.Contains("puddle") ||
                    transform.position.y < waterHeightThreshold)
                {
                    currentSurface = GroundSurfaceType.ShallowWater;
                    return currentSurface;
                }

                // 2. Metal Industrial (train rail boxcars, cat walks, container docks, ladders)
                if (matName.Contains("metal") || matName.Contains("iron") || matName.Contains("steel") || matName.Contains("rail") ||
                    objName.Contains("metal") || objName.Contains("rail") || objName.Contains("train") || objName.Contains("boxcar") ||
                    objName.Contains("container") || parentName.Contains("rail") || parentName.Contains("train"))
                {
                    currentSurface = GroundSurfaceType.MetalIndustrial;
                    return currentSurface;
                }

                // 3. Wood Interior (StarterHouse_Safehome, SafeHouse, lake cabins)
                if (matName.Contains("wood") || matName.Contains("timber") || matName.Contains("plank") || matName.Contains("parquet") ||
                    objName.Contains("wood") || objName.Contains("floor") || parentName.Contains("starterhouse") || parentName.Contains("safehouse"))
                {
                    currentSurface = GroundSurfaceType.WoodInterior;
                    return currentSurface;
                }

                // 4. Grass & Soil (West forest, park lawns, lake dirt)
                if (matName.Contains("grass") || matName.Contains("soil") || matName.Contains("dirt") || matName.Contains("mud") ||
                    matName.Contains("terrain") || matName.Contains("foliage") || objName.Contains("grass") || objName.Contains("dirt") ||
                    objName.Contains("terrain") || objName.Contains("lawn"))
                {
                    currentSurface = GroundSurfaceType.GrassSoil;
                    return currentSurface;
                }

                // 5. Asphalt & Concrete (urban roads, sidewalks, highway, mall tiles)
                currentSurface = GroundSurfaceType.AsphaltConcrete;
                return currentSurface;
            }

            currentSurface = GroundSurfaceType.AsphaltConcrete;
            return currentSurface;
        }

        private void TriggerFootstep(bool isRunning, bool isCrouched)
        {
            GroundSurfaceType surface = DetectSurface(out RaycastHit hit);

            // 1. Audio Synthesis via ProceduralAudioManager
            ProceduralAudioManager.Instance?.PlaySurfaceFootstep(surface, isRunning, false);

            // 2. Step Particle VFX Puff
            Vector3 stepPos = hit.point != Vector3.zero ? hit.point : (transform.position + Vector3.up * 0.02f);
            EmitSurfaceStepParticles(surface, stepPos, isRunning, false);

            // 3. Acoustic Stealth Noise Radius
            float baseRadius = isCrouched ? crouchNoiseRadius : (isRunning ? sprintNoiseRadius : walkNoiseRadius);
            float surfaceMult = GetSurfaceAcousticMultiplier(surface);
            float effectiveRadius = baseRadius * surfaceMult;

            // Apply progression stealth perks & weather damping if active
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                effectiveRadius *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFootstepNoiseMultiplier();
            }
            if (ProjectSurvival.Environment.DynamicWeatherManager.Instance != null)
            {
                effectiveRadius *= ProjectSurvival.Environment.DynamicWeatherManager.Instance.GetWeatherAcousticMultiplier();
            }

            noiseEmitter?.EmitCustomImpulse(effectiveRadius);
            GameEvents.TriggerNoiseEmitted(transform.position, effectiveRadius);
        }

        /// <summary>
        /// Handles jump and vault landing impact scaling.
        /// </summary>
        public void OnLanding(float fallHeight, Vector3 landingPosition)
        {
            GroundSurfaceType surface = DetectSurface(out RaycastHit hit);

            // 1. Heavy landing audio
            ProceduralAudioManager.Instance?.PlaySurfaceFootstep(surface, false, true);

            // 2. Scaled particle impact burst
            Vector3 impactPos = hit.point != Vector3.zero ? hit.point : (landingPosition + Vector3.up * 0.02f);
            EmitSurfaceStepParticles(surface, impactPos, true, true);

            // 3. Acoustic noise shockwave into HordeAlertSystem
            float surfaceMult = GetSurfaceAcousticMultiplier(surface);
            float landingRadius = landingNoiseRadius * surfaceMult * (fallHeight > 1.2f ? 1.4f : 1.0f);
            noiseEmitter?.EmitCustomImpulse(landingRadius);
            GameEvents.TriggerNoiseEmitted(transform.position, landingRadius);

            // 4. Subtle camera landing dip (-0.06m pitch kick)
            ThirdPersonCamera.Instance?.AddRecoil(-0.06f, 0f);

            // 5. Tactile Haptics
            MobileHapticManager.TriggerLight();
        }

        public float GetSurfaceAcousticMultiplier(GroundSurfaceType surface)
        {
            switch (surface)
            {
                case GroundSurfaceType.GrassSoil: return 0.80f;
                case GroundSurfaceType.WoodInterior: return 0.90f;
                case GroundSurfaceType.MetalIndustrial: return 1.35f;
                case GroundSurfaceType.ShallowWater: return 1.30f;
                case GroundSurfaceType.AsphaltConcrete:
                default: return 1.00f;
            }
        }

        private void EmitSurfaceStepParticles(GroundSurfaceType surface, Vector3 pos, bool isRunning, bool isLanding)
        {
            if (stepParticleSystem == null) return;

            Color pColor = Color.white;
            int count = isLanding ? 10 : (isRunning ? 4 : 2);
            float size = isLanding ? 0.32f : (isRunning ? 0.22f : 0.14f);

            switch (surface)
            {
                case GroundSurfaceType.GrassSoil:
                    pColor = new Color(0.44f, 0.36f, 0.26f, 0.65f); // Earthy soil #705B42
                    break;
                case GroundSurfaceType.WoodInterior:
                    pColor = new Color(0.77f, 0.68f, 0.54f, 0.45f); // Fine timber dust #C4AE8A
                    break;
                case GroundSurfaceType.MetalIndustrial:
                    pColor = new Color(0.88f, 0.62f, 0.28f, 0.80f); // Micro spark/rust flecks #E09E47
                    break;
                case GroundSurfaceType.ShallowWater:
                    pColor = new Color(0.56f, 0.78f, 0.91f, 0.75f); // Water droplet splash #90C8E8
                    break;
                case GroundSurfaceType.AsphaltConcrete:
                default:
                    pColor = new Color(0.83f, 0.85f, 0.88f, 0.50f); // Concrete gray dust #D4D8E0
                    break;
            }

            var emitParams = new ParticleSystem.EmitParams
            {
                position = pos + Vector3.up * 0.05f,
                velocity = (Vector3.up * (isLanding ? 1.4f : 0.6f) + -transform.forward * 0.3f + UnityEngine.Random.insideUnitSphere * 0.25f),
                startColor = pColor,
                startSize = size,
                startLifetime = isLanding ? 0.55f : 0.38f
            };

            stepParticleSystem.Emit(emitParams, count);
        }
    }
}
