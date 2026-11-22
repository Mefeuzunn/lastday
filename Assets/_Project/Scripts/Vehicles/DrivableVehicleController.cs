using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.UI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.Vehicles
{
    public enum HeroVehicleType
    {
        SafeHouse_Pickup,
        Police_Interceptor,
        Forest_RangerJeep,
        RailYard_CargoVan,
        NorthHighway_ArmoredSUV
    }

    /// <summary>
    /// Micro-Step 1A: Studio-Grade Drivable Vehicle Physics Controller.
    /// Operates with 4 calibrated WheelColliders (WC_FL, WC_FR, WC_RL, WC_RR),
    /// speed-sensitive steering (32° down to 14°), anti-flip stabilization torque (> 55°),
    /// dynamic chassis roll & pitch, interior steering wheel rotation, procedural engine/skid audio,
    /// crimson rear brake lights, and seamless boarding/exiting handshakes with VehicleChaseCamera.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DrivableVehicleController : MonoBehaviour
    {
        [Header("Vehicle Identity")]
        [SerializeField] private string vehicleId = "vehicle_safehouse_pickup";
        [SerializeField] private string vehicleDisplayName = "Pikap Kamyonet";
        [SerializeField] private HeroVehicleType vehicleType = HeroVehicleType.SafeHouse_Pickup;

        [Header("Vehicle Integrity & Fuel")]
        [SerializeField] private float engineHP = 100f;
        [SerializeField] private float maxEngineHP = 100f;
        [SerializeField] private float fuel = 100f;
        [SerializeField] private float maxFuel = 100f;

        [Header("Speed & Handling Limits")]
        [SerializeField] private float topSpeedKmh = 56f;       // ~15.55 m/s forward limit
        [SerializeField] private float topReverseSpeedKmh = 20f; // ~5.55 m/s reverse limit
        [SerializeField] private float motorTorqueMax = 1850f;
        [SerializeField] private float brakeForce = 4500f;
        [SerializeField] private float lowSpeedSteerAngle = 32f;
        [SerializeField] private float highSpeedSteerAngle = 14f;
        [SerializeField] private float steerDampSpeed = 8.0f;

        [Header("Wheel Colliders (4-Wheel Independent Suspension)")]
        [SerializeField] private WheelCollider wheelColFL;
        [SerializeField] private WheelCollider wheelColFR;
        [SerializeField] private WheelCollider wheelColRL;
        [SerializeField] private WheelCollider wheelColRR;

        [Header("Visual Wheel Transforms")]
        [SerializeField] private Transform wheelMeshFL;
        [SerializeField] private Transform wheelMeshFR;
        [SerializeField] private Transform wheelMeshRL;
        [SerializeField] private Transform wheelMeshRR;

        [Header("Chassis & Interior Articulation")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private Transform steeringWheelMesh;

        [Header("Cabin Seats & Exit Anchors")]
        [SerializeField] private Transform driverSeatAnchor;
        [SerializeField] private Transform passengerSeatAnchor;
        [SerializeField] private Transform driverExitAnchor;
        [SerializeField] private Transform passengerExitAnchor;

        [Header("Lighting & VFX")]
        [SerializeField] private Light headlightL;
        [SerializeField] private Light headlightR;
        [SerializeField] private Renderer brakeLightL;
        [SerializeField] private Renderer brakeLightR;
        [SerializeField] private Light sirenLightRed;
        [SerializeField] private Light sirenLightBlue;
        [SerializeField] private ParticleSystem exhaustParticles;

        [Header("Mobile Trunk Stash")]
        [SerializeField] private int trunkSlotCapacity = 8;
        [SerializeField] private List<InventorySlot> trunkInventory = new List<InventorySlot>();

        [Header("NavMesh Obstacle Carving")]
        [SerializeField] private UnityEngine.AI.NavMeshObstacle navMeshObstacle;
        public UnityEngine.AI.NavMeshObstacle NavObstacle => navMeshObstacle;

        // Physics & State
        private Rigidbody rb;
        private bool isPlayerDriving = false;
        private GameObject currentDriver;
        private GameObject seatedCompanion;

        // Dynamic chassis tilt
        private float currentRoll = 0f;
        private float currentPitchTilt = 0f;
        private Vector3 prevVelocity;

        // Current Steer
        private float currentSteerAngle = 0f;

        // Inputs (-1 to 1)
        private float steerInput = 0f;
        private float throttleInput = 0f;
        private float brakeInput = 0f;

        // Skid Sound timer
        private float skidSoundTimer = 0f;

        // Brake Light state
        private bool isBrakingOrReversing = false;
        private MaterialPropertyBlock brakePropBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Audio
        private AudioSource engineAudioSource;
        private AudioSource hornAudioSource;
        private bool isHeadlightsOn = false;
        private bool isSirenActive = false;

        // Public Accessors
        public string VehicleId => vehicleId;
        public string VehicleDisplayName => vehicleDisplayName;
        public HeroVehicleType VehicleType => vehicleType;
        public float EngineHP => engineHP;
        public float MaxEngineHP => maxEngineHP;
        public float Fuel => fuel;
        public float MaxFuel => maxFuel;
        public bool IsPlayerDriving => isPlayerDriving;
        public static DrivableVehicleController ActiveVehicle { get; private set; }
        public bool IsHeadlightsOn => isHeadlightsOn;
        public bool IsSirenActive => isSirenActive;
        public int TrunkCapacity => trunkSlotCapacity;
        public List<InventorySlot> TrunkInventory => trunkInventory;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        public float ForwardSpeedKmh => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f : 0f;
        public float ThrottleInput => throttleInput;
        public float SteerInput => steerInput;
        public float BrakeInput => brakeInput;

        public WheelCollider WheelColFL => wheelColFL;
        public WheelCollider WheelColFR => wheelColFR;
        public WheelCollider WheelColRL => wheelColRL;
        public WheelCollider WheelColRR => wheelColRR;
        public Transform WheelMeshFL => wheelMeshFL;
        public Transform WheelMeshFR => wheelMeshFR;
        public Transform WheelMeshRL => wheelMeshRL;
        public Transform WheelMeshRR => wheelMeshRR;
        public Transform VisualModel => visualModel;
        public Transform DriverSeatAnchor => driverSeatAnchor;
        public Transform DriverExitAnchor => driverExitAnchor;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            brakePropBlock = new MaterialPropertyBlock();

            LocateAndConfigureHierarchy();
            ConfigureRigidbody();
            ConfigureNavMeshObstacle();
            ConfigureAllWheelColliders();
            InitializeAudioSources();
        }

        private void Start()
        {
            SetHeadlights(false);
            if (sirenLightRed != null) sirenLightRed.enabled = false;
            if (sirenLightBlue != null) sirenLightBlue.enabled = false;
            UpdateBrakeLightVisuals(false);

            if (engineAudioSource != null && !isPlayerDriving)
            {
                engineAudioSource.Stop();
                engineAudioSource.volume = 0f;
            engineAudioSource.Stop();
            }
        }

        public void LocateAndConfigureHierarchy()
        {
            if (visualModel == null) visualModel = transform.Find("Visual_Model");

            // Locate Wheel Colliders
            Transform colRoot = transform.Find("Wheel_Colliders");
            if (colRoot != null)
            {
                if (wheelColFL == null) wheelColFL = colRoot.Find("WC_FL")?.GetComponent<WheelCollider>();
                if (wheelColFR == null) wheelColFR = colRoot.Find("WC_FR")?.GetComponent<WheelCollider>();
                if (wheelColRL == null) wheelColRL = colRoot.Find("WC_RL")?.GetComponent<WheelCollider>();
                if (wheelColRR == null) wheelColRR = colRoot.Find("WC_RR")?.GetComponent<WheelCollider>();
            }

            // Locate Visual Wheels
            Transform meshRoot = transform.Find("Wheels_Visual") ?? (visualModel != null ? visualModel.Find("Wheels_Visual") : null);
            if (meshRoot != null)
            {
                if (wheelMeshFL == null) wheelMeshFL = meshRoot.Find("Wheel_FL");
                if (wheelMeshFR == null) wheelMeshFR = meshRoot.Find("Wheel_FR");
                if (wheelMeshRL == null) wheelMeshRL = meshRoot.Find("Wheel_RL");
                if (wheelMeshRR == null) wheelMeshRR = meshRoot.Find("Wheel_RR");
            }

            // Locate Steering Wheel
            if (steeringWheelMesh == null && visualModel != null)
            {
                steeringWheelMesh = visualModel.Find("Interior_SteeringWheel") ?? visualModel.Find("Cab/Interior_SteeringWheel");
            }

            // Locate Anchors
            if (driverSeatAnchor == null) driverSeatAnchor = transform.Find("DriverSeatAnchor");
            if (passengerSeatAnchor == null) passengerSeatAnchor = transform.Find("PassengerSeatAnchor");
            if (driverExitAnchor == null) driverExitAnchor = transform.Find("DriverExitAnchor");
            if (passengerExitAnchor == null) passengerExitAnchor = transform.Find("PassengerExitAnchor");

            // Locate Lights
            Transform fxRoot = transform.Find("Lighting_FX") ?? (visualModel != null ? visualModel.Find("Lighting_FX") : null);
            if (fxRoot != null)
            {
                if (headlightL == null) headlightL = fxRoot.Find("Headlight_L")?.GetComponent<Light>();
                if (headlightR == null) headlightR = fxRoot.Find("Headlight_R")?.GetComponent<Light>();
                if (brakeLightL == null) brakeLightL = fxRoot.Find("BrakeLight_L")?.GetComponent<Renderer>();
                if (brakeLightR == null) brakeLightR = fxRoot.Find("BrakeLight_R")?.GetComponent<Renderer>();
                if (exhaustParticles == null) exhaustParticles = fxRoot.Find("Exhaust_Emitter")?.GetComponent<ParticleSystem>();
            }
        }

        public void ConfigureNavMeshObstacle()
        {
            if (navMeshObstacle == null) navMeshObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (navMeshObstacle == null) navMeshObstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();

            navMeshObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                navMeshObstacle.center = boxCol.center;
                navMeshObstacle.size = boxCol.size;
            }
            else
            {
                navMeshObstacle.center = new Vector3(0f, 0.85f, 0.05f);
                navMeshObstacle.size = new Vector3(2.0f, 1.35f, 4.3f);
            }

            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = true;
            navMeshObstacle.carvingMoveThreshold = 0.25f;
            navMeshObstacle.carvingTimeToStationary = 0.4f;
            navMeshObstacle.enabled = !isPlayerDriving;
        }

        private void ConfigureRigidbody()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();

            rb.mass = 1450f;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 0.65f;
            rb.centerOfMass = new Vector3(0f, -0.48f, 0.15f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void ConfigureAllWheelColliders()
        {
            ConfigureSingleWheelCollider(wheelColFL);
            ConfigureSingleWheelCollider(wheelColFR);
            ConfigureSingleWheelCollider(wheelColRL);
            ConfigureSingleWheelCollider(wheelColRR);
        }

        private void ConfigureSingleWheelCollider(WheelCollider wc)
        {
            if (wc == null) return;

            wc.suspensionDistance = 0.22f;
            JointSpring spring = wc.suspensionSpring;
            spring.spring = 28000f;
            spring.damper = 3800f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;

            wc.radius = 0.36f;
            wc.mass = 35f;

            // Forward Friction (Asphalt Grip)
            WheelFrictionCurve fFric = wc.forwardFriction;
            fFric.extremumSlip = 0.2f;
            fFric.extremumValue = 1.0f;
            fFric.asymptoteSlip = 0.5f;
            fFric.asymptoteValue = 0.8f;
            fFric.stiffness = 1.5f;
            wc.forwardFriction = fFric;

            // Sideways Friction (Cornering Grip)
            WheelFrictionCurve sFric = wc.sidewaysFriction;
            sFric.extremumSlip = 0.2f;
            sFric.extremumValue = 1.0f;
            sFric.asymptoteSlip = 0.5f;
            sFric.asymptoteValue = 0.75f;
            sFric.stiffness = 1.6f;
            wc.sidewaysFriction = sFric;
        }

        private void InitializeAudioSources()
        {
            if (engineAudioSource == null)
            {
                engineAudioSource = GetComponent<AudioSource>();
                if (engineAudioSource == null)
                {
                    engineAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            engineAudioSource.loop = true;
            engineAudioSource.playOnAwake = false;
            engineAudioSource.spatialBlend = 1.0f;
            engineAudioSource.minDistance = 2.0f;
            engineAudioSource.maxDistance = 35.0f;
            engineAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            if (engineAudioSource.clip == null)
            {
                engineAudioSource.clip = GenerateProceduralEngineClip();
            }
            engineAudioSource.volume = 0f;
            engineAudioSource.Stop();

            if (hornAudioSource == null)
            {
                hornAudioSource = gameObject.AddComponent<AudioSource>();
            }

            hornAudioSource.loop = false;
            hornAudioSource.playOnAwake = false;
            hornAudioSource.spatialBlend = 1.0f;
            hornAudioSource.minDistance = 2.0f;
            hornAudioSource.maxDistance = 35.0f;
            hornAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            hornAudioSource.volume = 0.85f;
        }

        public void SetInputs(float steer, float throttle, float brake)
        {
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            brakeInput = Mathf.Clamp01(brake);
        }

        private void FixedUpdate()
        {
            SimulateDrivingPhysics();
            SimulateAntiFlipStabilization();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isPlayerDriving)
            {
                UpdateEngineAudio(dt);
                UpdateExhaustVFX();
                CheckTireSkidSound(dt);
            }
            else
            {
                FadeOutAudio(dt);
            }

            UpdateVisualWheels();
            UpdateInteriorSteeringWheel();
            UpdateDynamicChassisTilt(dt);
            UpdateBrakeLightVisuals(brakeInput > 0.05f || throttleInput < -0.1f);
        }

        // =========================================================================
        // DRIVING PHYSICS & WHEEL COLLIDERS
        // =========================================================================

        private void SimulateDrivingPhysics()
        {
            if (rb == null) return;

            float forwardSpeed = ForwardSpeedKmh;
            float absForwardSpeed = Mathf.Abs(forwardSpeed);

            // 1. Speed-Sensitive Steering
            float speedRatio = Mathf.Clamp01(absForwardSpeed / topSpeedKmh);
            float maxSteer = Mathf.Lerp(lowSpeedSteerAngle, highSpeedSteerAngle, speedRatio);
            float targetSteer = steerInput * maxSteer;

            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteer, Time.fixedDeltaTime * steerDampSpeed * 10f);

            if (wheelColFL != null) wheelColFL.steerAngle = currentSteerAngle;
            if (wheelColFR != null) wheelColFR.steerAngle = currentSteerAngle;

            // 2. Motor & Brake Torque
            float maxForwardMs = topSpeedKmh / 3.6f;
            float maxReverseMs = topReverseSpeedKmh / 3.6f;
            float curForwardMs = Vector3.Dot(rb.linearVelocity, transform.forward);

            var maint = GetComponent<VehicleMaintenanceModule>();
            if (maint != null)
            {
                if (maint.IsStalled)
                {
                    ApplyMotorTorque(0f);
                    ApplyBrakeTorque(40f);
                    return;
                }
                if (maint.IsLimpMode)
                {
                    maxForwardMs *= 0.55f; // Limp mode speed cap at 55%
                }
            }

            if (brakeInput > 0.05f)
            {
                float activeBrake = brakeForce * brakeInput;
                ApplyBrakeTorque(activeBrake);
                ApplyMotorTorque(0f);
            }
            else
            {
                ApplyBrakeTorque(0f);

                if (throttleInput > 0.05f)
                {
                    if (curForwardMs < maxForwardMs)
                    {
                        float torqueFalloff = Mathf.Clamp01((maxForwardMs - curForwardMs) / 3.0f);
                        float torque = motorTorqueMax * throttleInput * torqueFalloff;
                        ApplyMotorTorque(torque);
                    }
                    else
                    {
                        ApplyMotorTorque(0f);
                    }
                }
                else if (throttleInput < -0.05f)
                {
                    if (curForwardMs > -maxReverseMs)
                    {
                        float torqueFalloff = Mathf.Clamp01((maxReverseMs - (-curForwardMs)) / 2.0f);
                        float torque = motorTorqueMax * 0.75f * throttleInput * torqueFalloff;
                        ApplyMotorTorque(torque);
                    }
                    else
                    {
                        ApplyMotorTorque(0f);
                    }
                }
                else
                {
                    // Gentle rolling resistance when idling
                    ApplyMotorTorque(0f);
                    if (absForwardSpeed > 0.5f)
                    {
                        ApplyBrakeTorque(60f);
                    }
                }
            }
        }

        private void ApplyMotorTorque(float torque)
        {
            // 4WD split: 50% front, 50% rear
            float perWheel = torque * 0.25f;
            if (wheelColFL != null) wheelColFL.motorTorque = perWheel;
            if (wheelColFR != null) wheelColFR.motorTorque = perWheel;
            if (wheelColRL != null) wheelColRL.motorTorque = perWheel;
            if (wheelColRR != null) wheelColRR.motorTorque = perWheel;
        }

        private void ApplyBrakeTorque(float brake)
        {
            // 4-wheel braking
            float perWheel = brake * 0.25f;
            if (wheelColFL != null) wheelColFL.brakeTorque = perWheel;
            if (wheelColFR != null) wheelColFR.brakeTorque = perWheel;
            if (wheelColRL != null) wheelColRL.brakeTorque = perWheel;
            if (wheelColRR != null) wheelColRR.brakeTorque = perWheel;
        }

        private void SimulateAntiFlipStabilization()
        {
            if (rb == null) return;

            float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
            if (tiltAngle > 55f)
            {
                // Calculate self-righting torque toward world up
                Vector3 rightingTorque = Vector3.Cross(transform.up, Vector3.up);
                rb.AddTorque(rightingTorque * 5500f, ForceMode.Acceleration);
            }
        }

        // =========================================================================
        // VISUAL ARTICULATION & WHEEL SYNC
        // =========================================================================

        private void UpdateVisualWheels()
        {
            SyncWheelMesh(wheelColFL, wheelMeshFL);
            SyncWheelMesh(wheelColFR, wheelMeshFR);
            SyncWheelMesh(wheelColRL, wheelMeshRL);
            SyncWheelMesh(wheelColRR, wheelMeshRR);
        }

        private void SyncWheelMesh(WheelCollider col, Transform mesh)
        {
            if (col == null || mesh == null) return;

            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.position = pos;
            mesh.rotation = rot;
        }

        private void UpdateInteriorSteeringWheel()
        {
            if (steeringWheelMesh == null) return;

            float frontSteer = (wheelColFL != null) ? wheelColFL.steerAngle : currentSteerAngle;
            // Proportional cabin steering wheel rotation (±90°)
            steeringWheelMesh.localRotation = Quaternion.Euler(22f, 0f, -frontSteer * 2.8f);
        }

        private void UpdateDynamicChassisTilt(float dt)
        {
            if (visualModel == null || rb == null) return;

            // Lateral Body Roll: ±3.5° during sharp cornering
            float lateralAngularSpeed = Vector3.Dot(rb.angularVelocity, transform.up);
            float targetRoll = Mathf.Clamp(-lateralAngularSpeed * 2.2f, -3.5f, 3.5f);

            // Longitudinal Pitch: nose dive on hard braking (-2.0°), squat on acceleration (+1.5°)
            Vector3 velChange = (rb.linearVelocity - prevVelocity);
            float forwardAccel = Vector3.Dot(velChange, transform.forward) / Mathf.Max(dt, 0.001f);
            float targetPitch = Mathf.Clamp(-forwardAccel * 0.28f, -2.0f, 1.5f);

            currentRoll = Mathf.MoveTowards(currentRoll, targetRoll, dt * 14.0f);
            currentPitchTilt = Mathf.MoveTowards(currentPitchTilt, targetPitch, dt * 12.0f);

            visualModel.localRotation = Quaternion.Euler(currentPitchTilt, 0f, currentRoll);
            prevVelocity = rb.linearVelocity;
        }

        private void UpdateBrakeLightVisuals(bool braking)
        {
            if (isBrakingOrReversing == braking) return;
            isBrakingOrReversing = braking;

            Color crimson = new Color(1.0f, 0.12f, 0.15f, 1.0f);
            Color dimCrimson = new Color(0.25f, 0.03f, 0.04f, 1.0f);

            Color targetColor = braking ? crimson : dimCrimson;
            Color targetEmission = braking ? crimson * 2.5f : Color.black;

            ApplyBrakeLightMat(brakeLightL, targetColor, targetEmission);
            ApplyBrakeLightMat(brakeLightR, targetColor, targetEmission);
        }

        private void ApplyBrakeLightMat(Renderer rend, Color baseCol, Color emissionCol)
        {
            if (rend == null) return;

            if (brakePropBlock == null) brakePropBlock = new MaterialPropertyBlock();
            rend.GetPropertyBlock(brakePropBlock);
            brakePropBlock.SetColor(BaseColorId, baseCol);
            brakePropBlock.SetColor(EmissionColorId, emissionCol);
            rend.SetPropertyBlock(brakePropBlock);
        }

        // =========================================================================
        // AUDIO & LIGHTS
        // =========================================================================

        private void UpdateEngineAudio(float dt)
        {
            if (engineAudioSource == null) return;

            if (!engineAudioSource.isPlaying) engineAudioSource.Play();

            float absSpeed = Mathf.Abs(ForwardSpeedKmh);
            float speedRatio = Mathf.Clamp01(absSpeed / topSpeedKmh);
            float throttleRatio = Mathf.Abs(throttleInput);

            float targetPitch = Mathf.Lerp(0.75f, 1.85f, Mathf.Max(speedRatio, throttleRatio * 0.85f));
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, dt * 4.5f);
            engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, 0.75f, dt * 6.0f);
        }

        private void FadeOutAudio(float dt)
        {
            if (engineAudioSource != null && engineAudioSource.isPlaying)
            {
                engineAudioSource.volume = Mathf.MoveTowards(engineAudioSource.volume, 0f, dt * 2.5f);
                if (engineAudioSource.volume <= 0.01f) engineAudioSource.Stop();
            }
        }

        private void CheckTireSkidSound(float dt)
        {
            skidSoundTimer += dt;
            if (brakeInput > 0.5f && Mathf.Abs(ForwardSpeedKmh) > 25.0f && skidSoundTimer > 0.40f)
            {
                skidSoundTimer = 0f;
                ProceduralAudioManager.Instance?.PlayTireSkid();
            }
        }

        private void UpdateExhaustVFX()
        {
            if (exhaustParticles == null) return;

            if (isPlayerDriving)
            {
                if (!exhaustParticles.isPlaying) exhaustParticles.Play();
                var em = exhaustParticles.emission;
                em.rateOverTime = Mathf.Lerp(8f, 28f, Mathf.Abs(throttleInput));
            }
            else
            {
                if (exhaustParticles.isPlaying) exhaustParticles.Stop();
            }
        }

        public void ToggleHeadlights()
        {
            var lighting = GetComponent<VehicleLightingAndAcoustics>();
            if (lighting != null)
            {
                lighting.ToggleHeadlights();
                isHeadlightsOn = lighting.AreHeadlightsOn;
                return;
            }

            SetHeadlights(!isHeadlightsOn);
        }

        public void SetHeadlights(bool on)
        {
            isHeadlightsOn = on;
            var lighting = GetComponent<VehicleLightingAndAcoustics>();
            if (lighting != null)
            {
                lighting.SetHeadlights(on);
                return;
            }

            if (headlightL != null) headlightL.enabled = on;
            if (headlightR != null) headlightR.enabled = on;
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void HonkHorn()
        {
            var lighting = GetComponent<VehicleLightingAndAcoustics>();
            if (lighting != null)
            {
                if (vehicleType == HeroVehicleType.Police_Interceptor)
                {
                    lighting.ToggleSiren();
                }
                else
                {
                    lighting.HonkHorn();
                }
                return;
            }

            if (hornAudioSource != null)
            {
                hornAudioSource.Play();
            }
            ProceduralAudioManager.Instance?.PlayVehicleHorn();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void ToggleSiren()
        {
            var lighting = GetComponent<VehicleLightingAndAcoustics>();
            if (lighting != null)
            {
                lighting.ToggleSiren();
                isSirenActive = lighting.IsSirenActive;
            }
        }

        // =========================================================================
        // BOARDING & PASSENGER CABIN
        // =========================================================================

        public void EnterDriverSeat(GameObject player)
        {
            if (player == null || isPlayerDriving) return;

            isPlayerDriving = true;
            ActiveVehicle = this;
            currentDriver = player;

            // Disable NavMeshObstacle while driving so vehicle does not block its own path
            if (navMeshObstacle != null) navMeshObstacle.enabled = false;

            // 1. Hide Player Visuals and Disable Controls
            var visual = player.transform.Find("Visual_Model");
            if (visual != null) visual.gameObject.SetActive(false);

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            var ws = player.GetComponent<WeaponSystem>();
            if (ws != null) ws.HolsterWeapon();

            // Parent player to vehicle driver seat anchor
            player.transform.SetParent(driverSeatAnchor != null ? driverSeatAnchor : transform, false);
            player.transform.localPosition = Vector3.zero;

            // 2. Camera: Activate VehicleChaseCamera and notify ThirdPersonCamera
            if (VehicleChaseCamera.Instance != null)
            {
                VehicleChaseCamera.Instance.Activate(transform, rb);
            }
            ThirdPersonCamera.Instance?.EnterVehicleMode(transform);

            // 3. Open Mobile Cockpit HUD
            VehicleDrivingHUD.Instance?.Open(this);

            // 4. Sector Streamer Prewarming Lookahead
            WorldSectorStreamer.Instance?.SetDrivingLookAhead(transform.forward * 35.0f);

            GameEvents.TriggerObjectiveToastRequested($"{vehicleDisplayName.ToUpper()} ÇALIŞTIRILDI", 2.0f);
            MobileHapticManager.Instance?.TriggerMedium();

            // 5. Automatic Companion Boarding (if companion is active within 22m)
            BoardCompanionIfNearby();
        }

        private Vector3 originalCompanionScale = Vector3.one;

        private void BoardCompanionIfNearby()
        {
            var cm = ProjectSurvival.AI.CompanionManager.Instance;
            if (cm == null || !cm.HasActiveCompanion) return;

            var compAi = cm.ActiveCompanion;
            if (compAi == null) return;
            GameObject companion = compAi.gameObject;

            float dist = Vector3.Distance(transform.position, companion.transform.position);
            if (dist > 22f) return;

            var agent = companion.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            compAi.enabled = false;

            Transform seat = passengerSeatAnchor != null ? passengerSeatAnchor : transform;
            companion.transform.SetParent(seat, false);
            companion.transform.localPosition = new Vector3(0.45f, 0.35f, 0.15f);
            companion.transform.localRotation = Quaternion.identity;
            originalCompanionScale = companion.transform.localScale;
            companion.transform.localScale = Vector3.one * 0.85f;

            seatedCompanion = companion;
            GameEvents.TriggerObjectiveToastRequested("YOLDAŞ ARACA BİNDİ", 2.0f);
        }

        private void DisembarkCompanion()
        {
            if (seatedCompanion == null) return;

            Vector3 rightExit = passengerExitAnchor != null
                ? passengerExitAnchor.position
                : (transform.position + transform.right * 2.2f + Vector3.up * 0.15f);

            int obstacleMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI");
            if (Physics.CheckSphere(rightExit + Vector3.up * 0.5f, 0.45f, obstacleMask))
            {
                rightExit = transform.position - transform.forward * 2.8f + Vector3.up * 0.15f;
            }

            if (UnityEngine.AI.NavMesh.SamplePosition(rightExit, out UnityEngine.AI.NavMeshHit hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                rightExit = hit.position;
            }

            seatedCompanion.transform.SetParent(null);
            seatedCompanion.transform.position = rightExit;
            seatedCompanion.transform.localScale = originalCompanionScale;

            var agent = seatedCompanion.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(rightExit);
            }

            var ai = seatedCompanion.GetComponent<ProjectSurvival.AI.SurvivorNPC_AI>();
            if (ai != null)
            {
                ai.enabled = true;
                ai.OrderFollow();
            }

            seatedCompanion = null;
        }

        public void ExitDriverSeat()
        {
            if (!isPlayerDriving || currentDriver == null) return;

            // Decelerate vehicle
            SetInputs(0f, 0f, 1f);

            // Disembark passenger companion
            DisembarkCompanion();

            // Determine safe exit location
            Vector3 leftExitPos = driverExitAnchor != null
                ? driverExitAnchor.position
                : (transform.position - transform.right * 1.85f + Vector3.up * 0.15f);
            Vector3 rightExitPos = passengerExitAnchor != null
                ? passengerExitAnchor.position
                : (transform.position + transform.right * 1.85f + Vector3.up * 0.15f);
            Vector3 rearExitPos = transform.position - transform.forward * 2.8f + Vector3.up * 0.15f;

            Vector3 targetExit = leftExitPos;

            // Obstacle test for left door
            int obstacleMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI");
            if (Physics.CheckSphere(leftExitPos + Vector3.up * 0.5f, 0.45f, obstacleMask))
            {
                // Fallback to right side
                if (!Physics.CheckSphere(rightExitPos + Vector3.up * 0.5f, 0.45f, obstacleMask))
                {
                    targetExit = rightExitPos;
                }
                else
                {
                    targetExit = rearExitPos;
                }
            }

            // Sample NavMesh to guarantee stable footing
            if (UnityEngine.AI.NavMesh.SamplePosition(targetExit, out UnityEngine.AI.NavMeshHit hit, 2.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetExit = hit.position;
            }

            // Unparent and restore Player
            currentDriver.transform.SetParent(null);
            currentDriver.transform.position = targetExit;

            var visual = currentDriver.transform.Find("Visual_Model");
            if (visual != null) visual.gameObject.SetActive(true);

            var cc = currentDriver.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            var pc = currentDriver.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = true;

            // Restore Camera Mode
            VehicleChaseCamera.Instance?.Deactivate();
            ThirdPersonCamera.Instance?.ExitVehicleMode();

            // Close HUD
            VehicleDrivingHUD.Instance?.Close();

            // Clear sector streamer lookahead
            WorldSectorStreamer.Instance?.SetDrivingLookAhead(Vector3.zero);

            isPlayerDriving = false;
            if (ActiveVehicle == this) ActiveVehicle = null;
            currentDriver = null;

            // Re-enable NavMeshObstacle with carving upon exit
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
                navMeshObstacle.carving = true;
            }

            GameEvents.TriggerObjectiveToastRequested("ARAÇTAN İNİLDİ", 1.8f);
            MobileHapticManager.Instance?.TriggerLight();
        }

        // =========================================================================
        // PROCEDURAL AUDIO GENERATOR
        // =========================================================================

        private AudioClip GenerateProceduralEngineClip()
        {
            int sampleRate = 22050;
            float duration = 1.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Rich low-pass 4-cylinder engine loop
                float cyl1 = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.45f;
                float cyl2 = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.25f;
                float exhaustThump = Mathf.Sin(2f * Mathf.PI * 22.5f * t) * 0.30f;
                float rumble = (UnityEngine.Random.value * 2f - 1f) * 0.08f;

                samples[i] = Mathf.Clamp(cyl1 + cyl2 + exhaustThump + rumble, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Engine_Pickup_Loop", numSamples, 1, sampleRate, true);
            clip.SetData(samples, 0);
            return clip;
        }

        // =========================================================================
        // COMPONENT CONFIGURATION HOOK
        // =========================================================================

        public void ConfigureComponents(
            HeroVehicleType type,
            string id,
            string displayName,
            float initialHP,
            float initialFuel,
            Transform fl, Transform fr, Transform rl, Transform rr,
            Transform dSeat, Transform pSeat,
            Transform dExit, Transform pExit,
            Light headL, Light headR,
            Light sirenR, Light sirenB,
            ParticleSystem exhaust)
        {
            vehicleType = type;
            vehicleId = id;
            vehicleDisplayName = displayName;
            engineHP = initialHP;
            fuel = initialFuel;

            wheelMeshFL = fl;
            wheelMeshFR = fr;
            wheelMeshRL = rl;
            wheelMeshRR = rr;

            driverSeatAnchor = dSeat;
            passengerSeatAnchor = pSeat;
            driverExitAnchor = dExit;
            passengerExitAnchor = pExit;

            headlightL = headL;
            headlightR = headR;
            sirenLightRed = sirenR;
            sirenLightBlue = sirenB;
            exhaustParticles = exhaust;

            trunkSlotCapacity = (type == HeroVehicleType.RailYard_CargoVan) ? 16 : 8;
        }

        public void SetWheelColliders(WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr)
        {
            wheelColFL = fl;
            wheelColFR = fr;
            wheelColRL = rl;
            wheelColRR = rr;
            ConfigureAllWheelColliders();
        }

        public void SetVisualElements(Transform model, Transform steering)
        {
            visualModel = model;
            steeringWheelMesh = steering;
        }

        public void SetBrakeLights(Renderer l, Renderer r)
        {
            brakeLightL = l;
            brakeLightR = r;
            UpdateBrakeLightVisuals(false);
        }

        // =========================================================================
        // COMBAT & REPAIR / REFUEL HOOKS
        // =========================================================================

        public void ApplyImpactDamage(float damage)
        {
            engineHP = Mathf.Max(0f, engineHP - damage);
        }

        public void RepairEngine(float amount = 40f)
        {
            engineHP = Mathf.Clamp(engineHP + amount, 0f, maxEngineHP);
        }

        public void Refuel(float amount = 45f)
        {
            fuel = Mathf.Clamp(fuel + amount, 0f, maxFuel);
        }

        public void RestoreState(float x, float y, float z, float rotY, float hp, float f, List<string> itemIds, List<int> counts)
        {
            if (rb != null)
            {
                rb.position = new Vector3(x, y, z);
                rb.rotation = Quaternion.Euler(0f, rotY, 0f);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                transform.position = new Vector3(x, y, z);
                transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            }

            engineHP = hp;
            fuel = f;

            var maint = GetComponent<VehicleMaintenanceModule>();
            if (maint != null)
            {
                maint.SetState(f, hp);
            }

            var trunk = GetComponent<VehicleTrunkStorage>();
            if (trunk != null)
            {
                trunk.RestoreItems(itemIds, counts);
            }

            trunkInventory.Clear();
            if (itemIds != null && counts != null)
            {
                for (int i = 0; i < Mathf.Min(itemIds.Count, counts.Count); i++)
                {
                    ItemData it = InventorySystem.FindItemData(itemIds[i]);
                    if (it != null && counts[i] > 0)
                    {
                        trunkInventory.Add(new InventorySlot(it, counts[i]));
                    }
                }
            }
        }
    }
}
