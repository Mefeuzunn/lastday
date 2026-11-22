using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Phase 1 (Step 1C): Environmental Atmosphere & Ambience Enhancer.
    /// Drives organic voltage-flickering across 12 exterior streetlights and neon signs
    /// (strictly Shadows: None for mobile 60 FPS, with 3D spatialized 60Hz electrical buzz),
    /// and 5 proximity-culled indoor sunbeam dust mote particle volumes (<22m culling).
    /// </summary>
    public class AtmosphericAmbienceEnhancer : MonoBehaviour
    {
        public static AtmosphericAmbienceEnhancer Instance { get; private set; }

        [System.Serializable]
        public class FlickeringLight
        {
            public string Name;
            public Light LightComponent;
            public Renderer EmissiveRenderer;
            public AudioSource BuzzSource;
            public float BaseIntensity = 2.4f;
            public float Seed = 0f;
            public float NoiseFrequency = 7.5f;
            public bool EnableSparks = true;
            public float LastSparkTime = 0f;
        }

        [System.Serializable]
        public class IndoorDustVolume
        {
            public string LocationName;
            public Transform Anchor;
            public ParticleSystem DustParticleSystem;
            public float MaxDistance = 22.0f;
            public bool IsActive = true;
        }

        [Header("Flickering Lights (12 Target Landmark Lights)")]
        [SerializeField] private List<FlickeringLight> flickeringLights = new List<FlickeringLight>();

        [Header("Indoor Dust Volumes (5 Key Buildings)")]
        [SerializeField] private List<IndoorDustVolume> indoorDustVolumes = new List<IndoorDustVolume>();

        [Header("Spark Particle System")]
        [SerializeField] private ParticleSystem sparkParticleSystem;

        private Transform playerTransform;
        private float proximityTimer = 0f;

        public int FlickeringLightCount => flickeringLights.Count;
        public int IndoorDustVolumeCount => indoorDustVolumes.Count;
        public List<FlickeringLight> FlickeringLights => flickeringLights;
        public List<IndoorDustVolume> IndoorDustVolumes => indoorDustVolumes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSparkParticles();
            ConfigureFlickeringLights();
            ConfigureIndoorDustVolumes();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void InitializeSparkParticles()
        {
            if (sparkParticleSystem != null) return;

            GameObject sparkObj = new GameObject("Atmospheric_Spark_Pool");
            sparkObj.transform.SetParent(transform, false);

            sparkParticleSystem = sparkObj.AddComponent<ParticleSystem>();
            var main = sparkParticleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 32;
            main.startLifetime = 0.22f;
            main.startSpeed = 1.8f;
            main.startSize = 0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = sparkParticleSystem.emission;
            emission.enabled = false;

            var shape = sparkParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = sparkParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            Shader unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");

            Material sparkMat = new Material(unlit);
            sparkMat.name = "Mat_AtmosphericSparks_Instanced";
            sparkMat.color = new Color(1.0f, 0.88f, 0.45f, 1.0f);
            sparkMat.enableInstancing = true;
            renderer.sharedMaterial = sparkMat;
        }

        public void ConfigureFlickeringLights()
        {
            AudioClip humClip = ProceduralAudioManager.Instance != null ? ProceduralAudioManager.Instance.GenerateElectricalHumClip() : null;

            foreach (var fl in flickeringLights)
            {
                if (fl.LightComponent != null)
                {
                    // Strict mobile 60 FPS rule: Shadows MUST be None on secondary flickering lights
                    fl.LightComponent.shadows = LightShadows.None;

                    if (fl.BaseIntensity <= 0.01f)
                    {
                        fl.BaseIntensity = fl.LightComponent.intensity > 0.1f ? fl.LightComponent.intensity : 2.5f;
                    }

                    if (fl.Seed <= 0.001f)
                    {
                        fl.Seed = UnityEngine.Random.Range(10f, 999f);
                    }

                    // Check if light is inside StarterHouse_Safehome (45m radius) or non-electrical (Lantern/Fire)
                    bool isSafeZone = fl.Name.Contains("SafeHouse") ||
                                     fl.Name.Contains("FireLight") ||
                                     fl.Name.Contains("Lantern") ||
                                     Vector3.Distance(fl.LightComponent.transform.position, Vector3.zero) < 45.0f;

                    if (isSafeZone)
                    {
                        if (fl.BuzzSource != null)
                        {
                            fl.BuzzSource.volume = 0f;
                            fl.BuzzSource.playOnAwake = false;
                            fl.BuzzSource.loop = false;
                            fl.BuzzSource.mute = true;
                            if (fl.BuzzSource.isPlaying) fl.BuzzSource.Stop();
                        }
                        continue;
                    }

                    // Setup gentle 3D spatialized electrical hum for distant exterior streetlights
                    if (fl.BuzzSource == null)
                    {
                        fl.BuzzSource = fl.LightComponent.GetComponent<AudioSource>();
                        if (fl.BuzzSource == null)
                        {
                            fl.BuzzSource = fl.LightComponent.gameObject.AddComponent<AudioSource>();
                        }
                    }

                    if (fl.BuzzSource != null)
                    {
                        fl.BuzzSource.spatialBlend = 1.0f;
                        fl.BuzzSource.loop = false;
                        fl.BuzzSource.volume = 0f;
                        fl.BuzzSource.playOnAwake = false;
                        if (fl.BuzzSource.isPlaying) fl.BuzzSource.Stop();
                    }
                }
            }
        }

        public void ConfigureIndoorDustVolumes()
        {
            // Remove/destroy all blocky square yellow particle systems inside interiors
            var allDustObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allDustObjects.Length; i++)
            {
                if (allDustObjects[i] != null && allDustObjects[i].name == "IndoorDust_Motes")
                {
                    allDustObjects[i].SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(allDustObjects[i]);
                    }
                }
            }

            foreach (var vol in indoorDustVolumes)
            {
                if (vol.Anchor != null)
                {
                    Transform existing = vol.Anchor.Find("IndoorDust_Motes");
                    if (existing != null)
                    {
                        existing.gameObject.SetActive(false);
                        if (Application.isPlaying) Destroy(existing.gameObject);
                    }
                    vol.DustParticleSystem = null;
                }
            }
        }

        private void Update()
        {
            UpdateVoltageFlicker();

            // Periodic proximity culling for dust motes (every 0.4s to conserve CPU)
            proximityTimer += Time.deltaTime;
            if (proximityTimer >= 0.40f)
            {
                proximityTimer = 0f;
                UpdateDustProximityCulling();
            }
        }

        private void UpdateVoltageFlicker()
        {
            float time = Time.time;

            for (int i = 0; i < flickeringLights.Count; i++)
            {
                var fl = flickeringLights[i];
                if (fl.LightComponent == null || !fl.LightComponent.gameObject.activeInHierarchy) continue;

                // Deterministic Perlin voltage wave
                float p = Mathf.PerlinNoise(fl.Seed, time * fl.NoiseFrequency);
                float wave = Mathf.Lerp(0.72f, 1.18f, p);

                // Voltage dip jitter (occasional sharp dips mimicking degraded power grid)
                float jitter = 1.0f;
                float r = Mathf.PerlinNoise(fl.Seed + 84.2f, time * 18.0f);
                if (r > 0.82f)
                {
                    jitter = Mathf.Lerp(0.12f, 0.45f, (r - 0.82f) / 0.18f);

                    // Trigger occasional micro spark on sharp voltage sag
                    if (fl.EnableSparks && time > fl.LastSparkTime + 2.8f)
                    {
                        fl.LastSparkTime = time;
                        EmitSparkPuff(fl.LightComponent.transform.position);
                    }
                }

                fl.LightComponent.intensity = fl.BaseIntensity * wave * jitter;

                // Ensure electrical hum is completely silenced
                if (fl.BuzzSource != null)
                {
                    fl.BuzzSource.volume = 0f;
                    if (fl.BuzzSource.isPlaying) fl.BuzzSource.Stop();
                }
            }
        }

        private void UpdateDustProximityCulling()
        {
            FindPlayer();
            if (playerTransform == null) return;

            Vector3 playerPos = playerTransform.position;

            for (int i = 0; i < indoorDustVolumes.Count; i++)
            {
                var vol = indoorDustVolumes[i];
                if (vol.DustParticleSystem == null) continue;

                Vector3 targetPos = vol.Anchor != null ? vol.Anchor.position : vol.DustParticleSystem.transform.position;
                float dist = Vector3.Distance(playerPos, targetPos);

                if (dist <= vol.MaxDistance)
                {
                    if (vol.DustParticleSystem.isPaused)
                    {
                        vol.DustParticleSystem.Play();
                    }
                    vol.IsActive = true;
                }
                else
                {
                    if (vol.DustParticleSystem.isPlaying)
                    {
                        vol.DustParticleSystem.Pause();
                    }
                    vol.IsActive = false;
                }
            }
        }

        private void EmitSparkPuff(Vector3 pos)
        {
            if (sparkParticleSystem == null) return;

            var emitParams = new ParticleSystem.EmitParams
            {
                position = pos + Vector3.down * 0.15f,
                velocity = (Vector3.down * 1.5f + UnityEngine.Random.insideUnitSphere * 0.6f),
                startColor = new Color(1.0f, 0.85f, 0.45f, 1.0f),
                startSize = 0.07f,
                startLifetime = 0.25f
            };

            sparkParticleSystem.Emit(emitParams, UnityEngine.Random.Range(2, 5));
        }

        /// <summary>
        /// Registers a flickering light into the managed array.
        /// </summary>
        public void RegisterFlickeringLight(string name, Light lightComp, float baseIntensity)
        {
            if (lightComp == null) return;
            lightComp.shadows = LightShadows.None;

            flickeringLights.Add(new FlickeringLight
            {
                Name = name,
                LightComponent = lightComp,
                BaseIntensity = baseIntensity,
                Seed = UnityEngine.Random.Range(10f, 999f)
            });
        }

        /// <summary>
        /// Registers an indoor dust volume into the managed array.
        /// </summary>
        public void RegisterDustVolume(string name, Transform anchor)
        {
            if (anchor == null) return;

            indoorDustVolumes.Add(new IndoorDustVolume
            {
                LocationName = name,
                Anchor = anchor
            });
        }
    }
}
