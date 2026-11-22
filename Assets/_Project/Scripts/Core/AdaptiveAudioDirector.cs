using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.Core
{
    /// <summary>
    /// Phase 4: Adaptive Multi-State Ambient & Combat Audio Director.
    /// Manages an intelligent 3-Layer audio mixer driven by SettingsManager:
    /// - Layer 1: Ambient Environment Bed (wind, crickets, urban tone, rain/roof rain)
    /// - Layer 2: Proximity Tension Pulse (bass drone when unalerted zombies < 16m or inside dark subways)
    /// - Layer 3: High-Intensity Combat & Boss Stem (rhythmic pulse during chases, outpost defense, boss fights)
    /// </summary>
    public class AdaptiveAudioDirector : MonoBehaviour
    {
        private static AdaptiveAudioDirector _instance;
        public static AdaptiveAudioDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AdaptiveAudioDirector>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AdaptiveAudioDirector");
                        _instance = go.AddComponent<AdaptiveAudioDirector>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Audio Sources (3 Layers)")]
        [SerializeField] private AudioSource layer1AmbientBed;
        [SerializeField] private AudioSource layer2TensionPulse;
        [SerializeField] private AudioSource layer3CombatStem;

        [Header("Layer Volumes")]
        [SerializeField] private float layer2TargetVol = 0f;
        [SerializeField] private float layer3TargetVol = 0f;

        [Header("Combat Cooldown Timer")]
        [SerializeField] private float combatCalmDelay = 4.0f;
        private float combatTimer = 0f;

        private Transform playerTransform;
        private float scanTimer = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("AdaptiveAudioDirector");
                Instance = go.AddComponent<AdaptiveAudioDirector>();
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

            InitializeAudioLayers();
        }

        private void InitializeAudioLayers()
        {
            if (layer1AmbientBed == null)
            {
                layer1AmbientBed = gameObject.AddComponent<AudioSource>();
                layer1AmbientBed.loop = false;
                layer1AmbientBed.playOnAwake = false;
                layer1AmbientBed.volume = 0f;
            }

            if (layer2TensionPulse == null)
            {
                layer2TensionPulse = gameObject.AddComponent<AudioSource>();
                layer2TensionPulse.loop = false;
                layer2TensionPulse.playOnAwake = false;
                layer2TensionPulse.volume = 0f;
            }

            if (layer3CombatStem == null)
            {
                layer3CombatStem = gameObject.AddComponent<AudioSource>();
                layer3CombatStem.loop = false;
                layer3CombatStem.playOnAwake = false;
                layer3CombatStem.volume = 0f;
            }
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            scanTimer += dt;
            if (scanTimer >= 0.35f)
            {
                scanTimer = 0f;
                EvaluateAudioThreats();
            }

            UpdateLayerVolumes(dt);
        }

        private void Start()
        {
            FindPlayer();
            StopAllAmbientLoops();
        }

        public void StopAllAmbientLoops()
        {
            if (layer1AmbientBed != null)
            {
                layer1AmbientBed.loop = false;
                layer1AmbientBed.volume = 0f;
                if (layer1AmbientBed.isPlaying) layer1AmbientBed.Stop();
            }
            if (layer2TensionPulse != null)
            {
                layer2TensionPulse.loop = false;
                layer2TensionPulse.volume = 0f;
                if (layer2TensionPulse.isPlaying) layer2TensionPulse.Stop();
            }
            if (layer3CombatStem != null)
            {
                layer3CombatStem.loop = false;
                layer3CombatStem.volume = 0f;
                if (layer3CombatStem.isPlaying) layer3CombatStem.Stop();
            }
        }

        private void StartAmbientBed()
        {
            StopAllAmbientLoops();
        }

        // =========================================================================
        // THREAT EVALUATION & LAYER VOLUME MIXING
        // =========================================================================

        private void EvaluateAudioThreats()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;
            bool isInCombat = false;
            bool isInTension = false;

            // 1. Check for Active Boss Encounter (< 38m)
            MutantBossAI[] allBosses = FindObjectsByType<MutantBossAI>(FindObjectsSortMode.None);
            for (int i = 0; i < allBosses.Length; i++)
            {
                if (allBosses[i] != null && !allBosses[i].IsDead)
                {
                    float dist = Vector3.Distance(playerPos, allBosses[i].transform.position);
                    if (dist <= 38f)
                    {
                        isInCombat = true;
                        break;
                    }
                }
            }

            // 2. Check for Zombies in Chase / Attack state or in proximity
            if (!isInCombat)
            {
                ZombieBrain[] zombies = FindObjectsByType<ZombieBrain>(FindObjectsSortMode.None);
                for (int i = 0; i < zombies.Length; i++)
                {
                    ZombieBrain z = zombies[i];
                    if (z == null || z.IsDead) continue;

                    float dist = Vector3.Distance(playerPos, z.transform.position);

                    if (dist <= 22f)
                    {
                        // Check if actively chasing or attacking
                        if (z.CurrentState == ZombieState.Chase || z.CurrentState == ZombieState.Attack || z.CurrentState == ZombieState.Scream)
                        {
                            isInCombat = true;
                            break;
                        }

                        // Proximity tension (< 16m)
                        if (dist <= 16f)
                        {
                            isInTension = true;
                        }
                    }
                }
            }

            // 3. Subterranean Subway / Interior Tension
            if (playerPos.y < -1.0f)
            {
                isInTension = true;
            }

            // Calm indoor atmosphere for StarterHouse_Safehome and Main Menu
            bool isInsideStarterSafeHouse = Vector3.Distance(playerPos, Vector3.zero) < 45.0f;
            bool isMainMenu = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu;

            if (isMainMenu || isInsideStarterSafeHouse)
            {
                layer2TargetVol = 0f;
                layer3TargetVol = 0f;
                return;
            }

            // Update Target Volumes with 4-second combat calm delay
            if (isInCombat)
            {
                combatTimer = combatCalmDelay;
                layer3TargetVol = 0.85f;
                layer2TargetVol = 0.40f;
            }
            else
            {
                if (combatTimer > 0f)
                {
                    combatTimer -= 0.35f;
                    layer3TargetVol = 0.85f;
                }
                else
                {
                    layer3TargetVol = 0f;
                }

                layer2TargetVol = isInTension ? 0.45f : 0f;
            }
        }

        private void UpdateLayerVolumes(float dt)
        {
            if (layer1AmbientBed != null)
            {
                layer1AmbientBed.volume = 0f;
                if (layer1AmbientBed.isPlaying) layer1AmbientBed.Stop();
            }

            float masterVol = 1.0f;
            float ambientSetting = 0.7f;
            bool isMuted = false;

            if (SettingsManager.Instance != null)
            {
                masterVol = SettingsManager.Instance.MasterVolume;
                ambientSetting = SettingsManager.Instance.AmbientVolume;
                isMuted = SettingsManager.Instance.IsMuted;
            }

            if (isMuted)
            {
                if (layer2TensionPulse != null) layer2TensionPulse.volume = 0f;
                if (layer3CombatStem != null) layer3CombatStem.volume = 0f;
                return;
            }

            float finalMultiplier = masterVol * ambientSetting;

            if (layer2TensionPulse != null)
                layer2TensionPulse.volume = Mathf.MoveTowards(layer2TensionPulse.volume, layer2TargetVol * finalMultiplier, dt * 0.45f);
            if (layer3CombatStem != null)
                layer3CombatStem.volume = Mathf.MoveTowards(layer3CombatStem.volume, layer3TargetVol * finalMultiplier, dt * 0.65f);
        }

        private AudioClip GenerateProceduralTone(float duration, Func<float, float> waveFunc)
        {
            int sampleRate = 22050;
            int numSamples = (int)(sampleRate * duration);
            float[] data = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("AdaptiveTone_" + Guid.NewGuid().ToString("N").Substring(0, 6), numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
