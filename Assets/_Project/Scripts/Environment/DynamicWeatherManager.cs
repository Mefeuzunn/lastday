using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.Environment
{
    public enum WeatherState
    {
        ClearOvercast = 0,   // Açık / Bulutlu (Baseline fog 0.010)
        LightDrizzle = 1,    // Hafif Yağmur (120 particles/sec, sheen +0.12)
        HeavyStorm = 2,      // Şiddetli Fırtına (300 particles/sec, lightning, sheen +0.22)
        DenseFog = 3         // Yoğun Karantina Sisi (Fog density 0.024)
    }

    /// <summary>
    /// Phase 4: Dynamic Weather & Wet-Surface Engine with Indoor Roof Occlusion.
    /// Manages 4 distinct atmospheric weather cycles, smooth 15s crossfading,
    /// upward roof raycasting (zero rain inside buildings), wet pavement PBR response,
    /// and tactical acoustic/vision stealth modifiers.
    /// </summary>
    public class DynamicWeatherManager : MonoBehaviour
    {
        private static DynamicWeatherManager _instance;
        public static DynamicWeatherManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DynamicWeatherManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DynamicWeatherManager");
                        _instance = go.AddComponent<DynamicWeatherManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Active Weather State")]
        [SerializeField] private WeatherState currentWeather = WeatherState.ClearOvercast;
        [SerializeField] private WeatherState targetWeather = WeatherState.ClearOvercast;
        [SerializeField] private float weatherCycleDuration = 240f; // Switch state every 4 minutes

        [Header("Indoor Roof Occlusion")]
        [SerializeField] private bool isIndoors = false;
        [SerializeField] private float roofCheckInterval = 0.25f;
        [SerializeField] private float maxRoofCheckDistance = 14f;

        [Header("Lighting & Thunder FX")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Color clearSkyColor = new Color(0.72f, 0.78f, 0.85f);
        [SerializeField] private Color stormSkyColor = new Color(0.24f, 0.28f, 0.35f);
        [SerializeField] private Color fogSkyColor = new Color(0.45f, 0.48f, 0.52f);

        // Particle System for Rain
        private ParticleSystem rainParticleSystem;
        private ParticleSystem.EmissionModule rainEmission;
        private Transform rainEmitterTransform;

        // Tactical Modifiers & PBR Materials
        private Material asphaltMaterial;
        private float currentSmoothnessOffset = 0f;
        private float targetSmoothnessOffset = 0f;

        private Transform playerTransform;
        private float weatherTimer = 0f;
        private float transitionProgress = 1.0f;
        private float roofCheckTimer = 0f;
        private float lightningTimer = 8.0f;

        // HUD Weather Badge
        private Text weatherHUDText;

        // Rain Audio Layers (Outdoor & Indoor Roof)
        private AudioSource rainOutdoorAudioSource;
        private AudioSource rainIndoorRoofAudioSource;
        private float outdoorRainTargetVol = 0f;
        private float indoorRoofTargetVol = 0f;

        public WeatherState CurrentWeather => currentWeather;
        public bool IsIndoors => isIndoors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("DynamicWeatherManager");
                Instance = go.AddComponent<DynamicWeatherManager>();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeAtmosphereSettings();
            InitializeRainParticleSystem();
            FindPBRRoadMaterials();
            InitializeRainAudioSources();
        }

        private void Start()
        {
            FindPlayerAndSun();
            EnsureHUDWeatherBadge();
            ApplyWeatherImmediate(currentWeather);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Weather Cycle Timer
            weatherTimer += dt;
            if (weatherTimer >= weatherCycleDuration)
            {
                weatherTimer = 0f;
                TransitionToRandomNextWeather();
            }

            // 2. Smooth 15s Crossfade Transitions
            if (transitionProgress < 1.0f)
            {
                transitionProgress += dt / 15.0f;
                UpdateAtmosphericCrossfade(transitionProgress);
                if (transitionProgress >= 1.0f)
                {
                    currentWeather = targetWeather;
                    UpdateHUDWeatherBadge();
                }
            }

            // 3. Indoor Roof Occlusion Raycast
            roofCheckTimer += dt;
            if (roofCheckTimer >= roofCheckInterval)
            {
                roofCheckTimer = 0f;
                CheckIndoorRoofOcclusion();
            }

            // 4. Update Rain Emitter Position (Follows Player/Camera)
            UpdateRainEmitterPosition();

            // 5. Thunder & Lightning during HeavyStorm
            if (currentWeather == WeatherState.HeavyStorm && !isIndoors)
            {
                lightningTimer -= dt;
                if (lightningTimer <= 0f)
                {
                    lightningTimer = UnityEngine.Random.Range(10f, 22f);
                    StartCoroutine(ExecuteLightningFlash());
                }
            }

            // 6. Smooth Wet Surface Smoothness Interpolation
            currentSmoothnessOffset = Mathf.MoveTowards(currentSmoothnessOffset, targetSmoothnessOffset, dt * 0.05f);
            ApplyRoadSmoothness(currentSmoothnessOffset);

            // 7. Dynamic Rain & Roof Audio Interpolation
            UpdateRainAudio(dt);
        }

        private void FindPlayerAndSun()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;

            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        sunLight = lights[i];
                        break;
                    }
                }
            }
        }

        private void InitializeAtmosphereSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.010f;
            RenderSettings.fogColor = clearSkyColor;
        }

        private void InitializeRainParticleSystem()
        {
            if (rainParticleSystem != null) return;

            GameObject emitterObj = new GameObject("Player_RainEmitter");
            emitterObj.transform.SetParent(transform, false);
            rainEmitterTransform = emitterObj.transform;

            rainParticleSystem = emitterObj.AddComponent<ParticleSystem>();

            var main = rainParticleSystem.main;
            main.maxParticles = 320; // Mobile 60 FPS budget
            main.startLifetime = 1.1f;
            main.startSpeed = 22f;
            main.startSize = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = 0f; // Controlled dynamically
            rainEmission = emission;

            var shape = rainParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 24f, 1f);
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Stretched Billboard URP Particle Material
            ParticleSystemRenderer rend = emitterObj.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.velocityScale = 0.08f;
            rend.lengthScale = 2.4f;

            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            Material pMat = new Material(pShader);
            Color dropCol = new Color(0.85f, 0.90f, 0.95f, 0.40f);
            if (pMat.HasProperty("_BaseColor")) pMat.SetColor("_BaseColor", dropCol);
            else pMat.color = dropCol;

            rend.sharedMaterial = pMat;
            rainParticleSystem.Stop();
        }

        private void FindPBRRoadMaterials()
        {
            asphaltMaterial = Resources.Load<Material>("AAA_PBR/MAT_Asphalt_WetPuddles");
            if (asphaltMaterial == null)
            {
                // Fallback scan
                Renderer[] allRends = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                for (int i = 0; i < allRends.Length; i++)
                {
                    if (allRends[i].sharedMaterial != null && allRends[i].sharedMaterial.name.Contains("Asphalt"))
                    {
                        asphaltMaterial = allRends[i].sharedMaterial;
                        break;
                    }
                }
            }
        }

        // =========================================================================
        // WEATHER CROSSFADE & TRANSITION LOGIC
        // =========================================================================

        public void SetWeather(WeatherState newWeather, bool instant = false)
        {
            if (instant)
            {
                currentWeather = newWeather;
                targetWeather = newWeather;
                transitionProgress = 1.0f;
                ApplyWeatherImmediate(newWeather);
            }
            else
            {
                targetWeather = newWeather;
                transitionProgress = 0f;
            }
            UpdateHUDWeatherBadge();
        }

        /// <summary>
        /// Phase 5 Chapter 4 Moral Finale: Shifts sky lighting toward a hopeful warm dawn.
        /// </summary>
        public void TransitionToHopefulDawn()
        {
            SetWeather(WeatherState.ClearOvercast, true);
            FindPlayerAndSun();
            if (sunLight != null)
            {
                sunLight.color = new Color(1.0f, 0.88f, 0.72f); // Warm dawn gold
                sunLight.intensity = 1.35f;
            }
            RenderSettings.fogColor = new Color(0.85f, 0.76f, 0.70f); // Warm rosy morning fog
            RenderSettings.fogDensity = 0.008f;
        }

        private void TransitionToRandomNextWeather()
        {
            int next = (int)currentWeather + 1;
            if (next > 3) next = 0;
            SetWeather((WeatherState)next, false);
            Debug.Log($"[DynamicWeatherManager] Transitioning weather from {currentWeather} to {targetWeather} (15s crossfade)");
        }

        private void ApplyWeatherImmediate(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.ClearOvercast:
                    RenderSettings.fogDensity = 0.010f;
                    RenderSettings.fogColor = clearSkyColor;
                    targetSmoothnessOffset = 0f;
                    SetRainEmission(0f);
                    break;

                case WeatherState.LightDrizzle:
                    RenderSettings.fogDensity = 0.014f;
                    RenderSettings.fogColor = clearSkyColor * 0.85f;
                    targetSmoothnessOffset = 0.12f;
                    SetRainEmission(120f);
                    break;

                case WeatherState.HeavyStorm:
                    RenderSettings.fogDensity = 0.018f;
                    RenderSettings.fogColor = stormSkyColor;
                    targetSmoothnessOffset = 0.22f;
                    SetRainEmission(300f);
                    break;

                case WeatherState.DenseFog:
                    RenderSettings.fogDensity = 0.024f;
                    RenderSettings.fogColor = fogSkyColor;
                    targetSmoothnessOffset = 0f;
                    SetRainEmission(0f);
                    break;
            }
            UpdateHUDWeatherBadge();
        }

        private void UpdateAtmosphericCrossfade(float t)
        {
            float startDensity = GetWeatherDensity(currentWeather);
            float endDensity = GetWeatherDensity(targetWeather);
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, endDensity, t);

            Color startCol = GetWeatherColor(currentWeather);
            Color endCol = GetWeatherColor(targetWeather);
            RenderSettings.fogColor = Color.Lerp(startCol, endCol, t);

            float startRain = GetWeatherRainRate(currentWeather);
            float endRain = GetWeatherRainRate(targetWeather);
            SetRainEmission(Mathf.Lerp(startRain, endRain, t));

            float startSmooth = GetWeatherSmoothnessOffset(currentWeather);
            float endSmooth = GetWeatherSmoothnessOffset(targetWeather);
            targetSmoothnessOffset = Mathf.Lerp(startSmooth, endSmooth, t);
        }

        private float GetWeatherDensity(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.ClearOvercast: return 0.010f;
                case WeatherState.LightDrizzle: return 0.014f;
                case WeatherState.HeavyStorm: return 0.018f;
                case WeatherState.DenseFog: return 0.024f;
                default: return 0.010f;
            }
        }

        private Color GetWeatherColor(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.ClearOvercast: return clearSkyColor;
                case WeatherState.LightDrizzle: return clearSkyColor * 0.85f;
                case WeatherState.HeavyStorm: return stormSkyColor;
                case WeatherState.DenseFog: return fogSkyColor;
                default: return clearSkyColor;
            }
        }

        private float GetWeatherRainRate(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.LightDrizzle: return 120f;
                case WeatherState.HeavyStorm: return 300f;
                default: return 0f;
            }
        }

        private float GetWeatherSmoothnessOffset(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.LightDrizzle: return 0.12f;
                case WeatherState.HeavyStorm: return 0.22f;
                default: return 0f;
            }
        }

        // =========================================================================
        // INDOOR ROOF OCCLUSION
        // =========================================================================

        private void CheckIndoorRoofOcclusion()
        {
            if (playerTransform == null)
            {
                FindPlayerAndSun();
                if (playerTransform == null) return;
            }

            Vector3 headPos = playerTransform.position + Vector3.up * 1.7f;
            bool roofAbove = false;

            // 1. Raycast upward looking for ceilings/roofs
            if (Physics.Raycast(headPos, Vector3.up, out RaycastHit hit, maxRoofCheckDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                roofAbove = true;
            }

            // 2. Elevation check: Subterranean subway concourses (Y < -1.0m) are always indoors
            if (playerTransform.position.y < -1.0f)
            {
                roofAbove = true;
            }

            isIndoors = roofAbove;

            // Adjust rain emission dynamically for indoor occlusion
            if (isIndoors)
            {
                if (rainParticleSystem != null && rainParticleSystem.isPlaying)
                {
                    rainEmission.rateOverTime = 0f;
                }
            }
            else
            {
                float targetRate = GetWeatherRainRate(currentWeather);
                SetRainEmission(targetRate);
            }
        }

        private void SetRainEmission(float rate)
        {
            if (rainParticleSystem == null) return;

            if (isIndoors || rate <= 0.1f)
            {
                rainEmission.rateOverTime = 0f;
                if (rainParticleSystem.isPlaying) rainParticleSystem.Stop();
            }
            else
            {
                rainEmission.rateOverTime = rate;
                if (!rainParticleSystem.isPlaying) rainParticleSystem.Play();
            }
        }

        private void UpdateRainEmitterPosition()
        {
            if (rainEmitterTransform == null) return;

            Vector3 targetPos = Vector3.zero;
            Camera cam = Camera.main;
            if (cam != null)
            {
                targetPos = cam.transform.position + Vector3.up * 11f + cam.transform.forward * 4f;
            }
            else if (playerTransform != null)
            {
                targetPos = playerTransform.position + Vector3.up * 12f;
            }

            rainEmitterTransform.position = targetPos;
        }

        private IEnumerator ExecuteLightningFlash()
        {
            if (sunLight == null) yield break;

            float origIntensity = sunLight.intensity;
            Color origColor = sunLight.color;

            // Sharp blue-white sky flash (0.12s)
            sunLight.color = new Color(0.85f, 0.95f, 1.0f);
            sunLight.intensity = origIntensity + 2.4f;

            yield return new WaitForSeconds(0.12f);

            sunLight.color = origColor;
            sunLight.intensity = origIntensity;

            // Distant rolling thunder sound (0.6s delay after lightning)
            yield return new WaitForSeconds(0.6f);
            ProceduralAudioManager.Instance?.PlayExplosion();
            GameEvents.TriggerCameraShakeRequested(0.25f, 0.20f);
        }

        private void ApplyRoadSmoothness(float offset)
        {
            if (asphaltMaterial != null && asphaltMaterial.HasProperty("_Smoothness"))
            {
                float baseSmoothness = 0.40f;
                asphaltMaterial.SetFloat("_Smoothness", Mathf.Clamp01(baseSmoothness + offset));
            }
        }

        // =========================================================================
        // TACTICAL STEALTH & AI MODIFIERS
        // =========================================================================

        public float GetWeatherAcousticMultiplier()
        {
            // HeavyStorm reduces footstep noise by 35% (masks sound)
            return (currentWeather == WeatherState.HeavyStorm && !isIndoors) ? 0.65f : 1.0f;
        }

        public float GetWeatherZombieSightMultiplier()
        {
            if (isIndoors) return 1.0f;

            if (currentWeather == WeatherState.HeavyStorm) return 0.85f; // -15% sight in heavy storm
            if (currentWeather == WeatherState.DenseFog) return 0.75f;    // -25% sight in dense fog
            return 1.0f;
        }

        // =========================================================================
        // HUD WEATHER BADGE
        // =========================================================================

        private void EnsureHUDWeatherBadge()
        {
            if (weatherHUDText != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject badgeObj = new GameObject("HUD_WeatherBadge");
            badgeObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = badgeObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(165f, -12f);
            rt.sizeDelta = new Vector2(95f, 24f);

            Image bg = badgeObj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.16f, 0.85f);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            weatherHUDText = txtObj.AddComponent<Text>();
            weatherHUDText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            weatherHUDText.fontSize = 10;
            weatherHUDText.fontStyle = FontStyle.Bold;
            weatherHUDText.alignment = TextAnchor.MiddleCenter;
            weatherHUDText.color = new Color(0.85f, 0.88f, 0.92f);

            UpdateHUDWeatherBadge();
        }

        private void UpdateHUDWeatherBadge()
        {
            if (weatherHUDText == null) return;

            switch (currentWeather)
            {
                case WeatherState.ClearOvercast:
                    weatherHUDText.text = "<color=#90E0EF>AÇIK</color>";
                    break;
                case WeatherState.LightDrizzle:
                    weatherHUDText.text = "<color=#48CAE4>YAĞMUR</color>";
                    break;
                case WeatherState.HeavyStorm:
                    weatherHUDText.text = "<color=#FF5A5F>FIRTINA</color>";
                    break;
                case WeatherState.DenseFog:
                    weatherHUDText.text = "<color=#B0C4DE>SİS</color>";
                    break;
            }
        }

        // =========================================================================
        // PROCEDURAL RAIN & INDOOR ROOF AUDIO
        // =========================================================================

        private void InitializeRainAudioSources()
        {
            if (rainOutdoorAudioSource == null)
            {
                rainOutdoorAudioSource = gameObject.AddComponent<AudioSource>();
                rainOutdoorAudioSource.loop = true;
                rainOutdoorAudioSource.playOnAwake = false;
                rainOutdoorAudioSource.priority = 160;

                rainOutdoorAudioSource.clip = GenerateProceduralRainClip(3.0f, false);
                rainOutdoorAudioSource.volume = 0f;
                rainOutdoorAudioSource.Play();
            }

            if (rainIndoorRoofAudioSource == null)
            {
                rainIndoorRoofAudioSource = gameObject.AddComponent<AudioSource>();
                rainIndoorRoofAudioSource.loop = true;
                rainIndoorRoofAudioSource.playOnAwake = false;
                rainIndoorRoofAudioSource.priority = 160;

                rainIndoorRoofAudioSource.clip = GenerateProceduralRainClip(3.0f, true);
                rainIndoorRoofAudioSource.volume = 0f;
                rainIndoorRoofAudioSource.Play();
            }
        }

        private void UpdateRainAudio(float dt)
        {
            float rainIntensity = 0f;
            if (currentWeather == WeatherState.LightDrizzle) rainIntensity = 0.40f;
            else if (currentWeather == WeatherState.HeavyStorm) rainIntensity = 0.85f;

            if (isIndoors)
            {
                outdoorRainTargetVol = 0f;
                indoorRoofTargetVol = rainIntensity * 0.70f;
            }
            else
            {
                outdoorRainTargetVol = rainIntensity;
                indoorRoofTargetVol = 0f;
            }

            float masterVol = 1.0f;
            float ambientVol = 0.7f;
            bool isMuted = false;

            if (SettingsManager.Instance != null)
            {
                masterVol = SettingsManager.Instance.MasterVolume;
                ambientVol = SettingsManager.Instance.AmbientVolume;
                isMuted = SettingsManager.Instance.IsMuted;
            }

            if (isMuted)
            {
                if (rainOutdoorAudioSource != null) rainOutdoorAudioSource.volume = 0f;
                if (rainIndoorRoofAudioSource != null) rainIndoorRoofAudioSource.volume = 0f;
                return;
            }

            float multiplier = masterVol * ambientVol;
            if (rainOutdoorAudioSource != null)
            {
                rainOutdoorAudioSource.volume = Mathf.MoveTowards(rainOutdoorAudioSource.volume, outdoorRainTargetVol * multiplier, dt * 0.45f);
            }
            if (rainIndoorRoofAudioSource != null)
            {
                rainIndoorRoofAudioSource.volume = Mathf.MoveTowards(rainIndoorRoofAudioSource.volume, indoorRoofTargetVol * multiplier, dt * 0.45f);
            }
        }

        private AudioClip GenerateProceduralRainClip(float duration, bool isMuffledRoof)
        {
            int sampleRate = 22050;
            int numSamples = (int)(sampleRate * duration);
            float[] data = new float[numSamples];
            float filterState = 0f;

            for (int i = 0; i < numSamples; i++)
            {
                float noise = (UnityEngine.Random.value * 2f - 1f);
                if (isMuffledRoof)
                {
                    // Heavy low-pass filtering for muffled sound inside buildings
                    filterState = Mathf.Lerp(filterState, noise, 0.045f);
                    float roofTap = Mathf.Sin(2f * Mathf.PI * 65f * ((float)i / sampleRate)) * 0.25f;
                    data[i] = Mathf.Clamp(filterState * 0.7f + roofTap * filterState, -1f, 1f);
                }
                else
                {
                    // Gentle band-pass / high-frequency hiss for outdoor rain
                    filterState = Mathf.Lerp(filterState, noise, 0.22f);
                    data[i] = Mathf.Clamp(filterState * 0.45f, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create("RainAudio_" + (isMuffledRoof ? "Roof" : "Outdoor"), numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
