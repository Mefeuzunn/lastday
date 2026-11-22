using System.Collections;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.AI;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Atomic Micro-Step 1C: Night Headlights, Siren Lure & Continuous Acoustic Propagation.
    /// Manages twin halogen URP SpotLights, emissive headlight/taillight lens materials,
    /// police interceptor crimson/blue emergency roof beacons, and acoustic broadcasting
    /// into HordeAlertSystem for horns (50m), sirens (55m), and speed-scaled engine noise (16-30m).
    /// </summary>
    [RequireComponent(typeof(DrivableVehicleController))]
    public class VehicleLightingAndAcoustics : MonoBehaviour
    {
        [Header("Vehicle Controller")]
        [SerializeField] private DrivableVehicleController controller;
        [SerializeField] private VehicleMaintenanceModule maintenance;

        [Header("URP Halogen Headlights (34m Range, 58° Angle)")]
        [SerializeField] private Light headlightLeft;
        [SerializeField] private Light headlightRight;
        [SerializeField] private Renderer lensRendererLeft;
        [SerializeField] private Renderer lensRendererRight;
        [SerializeField] private bool areHeadlightsOn = false;

        [Header("Emergency Police Beacons (Crimson & Cyan-Blue)")]
        [SerializeField] private Light sirenLightRed;
        [SerializeField] private Light sirenLightBlue;
        [SerializeField] private Renderer beaconRendererRed;
        [SerializeField] private Renderer beaconRendererBlue;
        [SerializeField] private bool isSirenActive = false;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sirenAudioSource;

        private float engineAcousticTimer = 0f;
        private float sirenLureTimer = 0f;
        private float beaconFlashTimer = 0f;
        private bool beaconPhase = false;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock lensPropBlock;

        public bool AreHeadlightsOn => areHeadlightsOn;
        public bool IsSirenActive => isSirenActive;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<DrivableVehicleController>();
            if (maintenance == null) maintenance = GetComponent<VehicleMaintenanceModule>();
            lensPropBlock = new MaterialPropertyBlock();

            EnsureLightingComponents();
        }

        private void Start()
        {
            ApplyHeadlightState(areHeadlightsOn);
            ApplySirenState(isSirenActive);
        }

        private void Update()
        {
            if (controller == null) return;

            // 1. Auto turn off headlights/siren if vehicle is stalled or out of fuel
            if (maintenance != null && maintenance.IsStalled)
            {
                if (areHeadlightsOn) SetHeadlights(false);
                if (isSirenActive) SetSiren(false);
            }

            // 2. Continuous Engine Acoustic Propagation while driven
            if (controller.IsPlayerDriving)
            {
                engineAcousticTimer += Time.deltaTime;
                if (engineAcousticTimer >= 1.2f)
                {
                    engineAcousticTimer = 0f;
                    EmitEngineAcoustics();
                }
            }

            // 3. Siren Flash & Acoustic Lure
            if (isSirenActive)
            {
                UpdateSirenBeacons(Time.deltaTime);

                sirenLureTimer += Time.deltaTime;
                if (sirenLureTimer >= 1.0f)
                {
                    sirenLureTimer = 0f;
                    HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 55f);
                }
            }
        }

        // =========================================================================
        // HEADLIGHTS
        // =========================================================================

        public void ToggleHeadlights()
        {
            SetHeadlights(!areHeadlightsOn);
        }

        public void SetHeadlights(bool on)
        {
            if (on && maintenance != null && maintenance.IsStalled)
            {
                GameEvents.TriggerObjectiveToastRequested("FARLAR AÇILAMIYOR: MOTOR ÇALIŞMIYOR", 1.8f);
                return;
            }

            areHeadlightsOn = on;
            ApplyHeadlightState(areHeadlightsOn);

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        private void ApplyHeadlightState(bool on)
        {
            if (headlightLeft != null) headlightLeft.enabled = on;
            if (headlightRight != null) headlightRight.enabled = on;

            Color emissiveColor = on ? new Color(1f, 0.95f, 0.84f) * 2.8f : Color.black;

            SetRendererEmission(lensRendererLeft, emissiveColor);
            SetRendererEmission(lensRendererRight, emissiveColor);
        }

        // =========================================================================
        // HORN & SIREN
        // =========================================================================

        public void HonkHorn()
        {
            if (maintenance != null && maintenance.IsStalled)
            {
                GameEvents.TriggerObjectiveToastRequested("KORNA ÇALMIYOR: AKÜ / MOTOR BOŞ", 1.8f);
                return;
            }

            // 1. Play procedural vehicle horn
            ProceduralAudioManager.Instance?.PlayVehicleHorn();
            MobileHapticManager.Instance?.TriggerMedium();

            // 2. Broadcast 50m acoustic lure to attract / pull zombie packs
            HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 50.0f);
            GameEvents.TriggerObjectiveToastRequested("KORNA ÇALINDI (50M ALARM)", 1.5f);
        }

        public void ToggleSiren()
        {
            SetSiren(!isSirenActive);
        }

        public void SetSiren(bool active)
        {
            if (active && maintenance != null && maintenance.IsStalled)
            {
                GameEvents.TriggerObjectiveToastRequested("SİREN ÇALIŞMIYOR: MOTOR ÇALIŞMIYOR", 1.8f);
                return;
            }

            isSirenActive = active;
            ApplySirenState(isSirenActive);

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerMedium();

            if (isSirenActive)
            {
                GameEvents.TriggerObjectiveToastRequested("POLİS SİRENİ AKTİF (55M ALARM)", 2.0f);
                HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 55.0f);
            }
        }

        private void ApplySirenState(bool active)
        {
            if (sirenLightRed != null) sirenLightRed.enabled = active;
            if (sirenLightBlue != null) sirenLightBlue.enabled = active;

            if (sirenAudioSource != null)
            {
                if (active)
                {
                    if (!sirenAudioSource.isPlaying) sirenAudioSource.Play();
                }
                else
                {
                    if (sirenAudioSource.isPlaying) sirenAudioSource.Stop();
                }
            }

            if (!active)
            {
                SetRendererEmission(beaconRendererRed, Color.black);
                SetRendererEmission(beaconRendererBlue, Color.black);
            }
        }

        private void UpdateSirenBeacons(float dt)
        {
            beaconFlashTimer += dt;
            if (beaconFlashTimer >= 0.16f) // ~6 Hz strobing pulse
            {
                beaconFlashTimer = 0f;
                beaconPhase = !beaconPhase;

                if (sirenLightRed != null) sirenLightRed.enabled = beaconPhase;
                if (sirenLightBlue != null) sirenLightBlue.enabled = !beaconPhase;

                Color redEmissive = beaconPhase ? new Color(1f, 0.08f, 0.08f) * 4f : Color.black;
                Color blueEmissive = !beaconPhase ? new Color(0.1f, 0.45f, 1f) * 4f : Color.black;

                SetRendererEmission(beaconRendererRed, redEmissive);
                SetRendererEmission(beaconRendererBlue, blueEmissive);
            }
        }

        // =========================================================================
        // SPEED-SCALED ENGINE ACOUSTIC EMISSION
        // =========================================================================

        private void EmitEngineAcoustics()
        {
            float speedKmh = controller != null ? Mathf.Abs(controller.ForwardSpeedKmh) : 0f;
            float throttle = controller != null ? Mathf.Abs(controller.ThrottleInput) : 0f;

            // Speed & throttle scaled radius: 16m at slow crawl up to 30m at full throttle
            float speedFactor = Mathf.Clamp01(speedKmh / 50.0f);
            float loadFactor = Mathf.Max(speedFactor, throttle);
            float acousticRadius = Mathf.Lerp(16.0f, 30.0f, loadFactor);

            HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, acousticRadius);
        }

        // =========================================================================
        // SETUP & BINDINGS
        // =========================================================================

        public void ConfigureLights(Light leftHead, Light rightHead, Light redSiren = null, Light blueSiren = null)
        {
            headlightLeft = leftHead;
            headlightRight = rightHead;
            sirenLightRed = redSiren;
            sirenLightBlue = blueSiren;

            ApplyHeadlightState(areHeadlightsOn);
            ApplySirenState(isSirenActive);
        }

        public void ConfigureRenderers(Renderer leftLens, Renderer rightLens, Renderer redBeacon = null, Renderer blueBeacon = null)
        {
            lensRendererLeft = leftLens;
            lensRendererRight = rightLens;
            beaconRendererRed = redBeacon;
            beaconRendererBlue = blueBeacon;
        }

        private void SetRendererEmission(Renderer rend, Color emissiveCol)
        {
            if (rend == null) return;
            if (lensPropBlock == null) lensPropBlock = new MaterialPropertyBlock();

            rend.GetPropertyBlock(lensPropBlock);
            lensPropBlock.SetColor(EmissionColorId, emissiveCol);
            rend.SetPropertyBlock(lensPropBlock);
        }

        private void EnsureLightingComponents()
        {
            Transform lightingRoot = transform.Find("Lighting_FX");
            if (lightingRoot == null) return;

            if (headlightLeft == null)
            {
                Transform hl = lightingRoot.Find("Headlight_L");
                if (hl != null) headlightLeft = hl.GetComponent<Light>();
            }
            if (headlightRight == null)
            {
                Transform hr = lightingRoot.Find("Headlight_R");
                if (hr != null) headlightRight = hr.GetComponent<Light>();
            }

            if (sirenLightRed == null)
            {
                Transform slr = lightingRoot.Find("Siren_Red");
                if (slr != null) sirenLightRed = slr.GetComponent<Light>();
            }
            if (sirenLightBlue == null)
            {
                Transform slb = lightingRoot.Find("Siren_Blue");
                if (slb != null) sirenLightBlue = slb.GetComponent<Light>();
            }

            if (sirenAudioSource == null)
            {
                sirenAudioSource = lightingRoot.GetComponent<AudioSource>();
                if (sirenAudioSource == null && (sirenLightRed != null || sirenLightBlue != null))
                {
                    sirenAudioSource = lightingRoot.gameObject.AddComponent<AudioSource>();
                    sirenAudioSource.spatialBlend = 1f;
                    sirenAudioSource.minDistance = 4f;
                    sirenAudioSource.maxDistance = 55f;
                    sirenAudioSource.loop = true;
                    sirenAudioSource.playOnAwake = false;
                }
            }
        }
    }
}
