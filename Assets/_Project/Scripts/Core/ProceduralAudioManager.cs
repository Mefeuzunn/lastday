using System;
using UnityEngine;
using ProjectSurvival.AI;

namespace ProjectSurvival.Core
{
    public class ProceduralAudioManager : MonoBehaviour
    {
        private static ProceduralAudioManager _instance;
        public static ProceduralAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<ProceduralAudioManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource footstepsSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Settings")]
        [SerializeField] private bool isMuted = false;
        [Range(0f, 1f)][SerializeField] private float masterVolume = 0.8f;
        [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)][SerializeField] private float ambientVolume = 0.18f;

        private const int SampleRate = 44100;
        private float heartbeatTimer = 0f;
        private Transform playerTransform;

        public bool IsMuted => isMuted;
        public float MasterVolume => masterVolume;
        public float SFXVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (heartbeatSource == null) heartbeatSource = gameObject.AddComponent<AudioSource>();
            if (footstepsSource == null) footstepsSource = gameObject.AddComponent<AudioSource>();
            if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.loop = false;
            footstepsSource.playOnAwake = false;

            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.volume = isMuted ? 0f : ambientVolume;

            StartAmbientLoop();
        }

        private void StartAmbientLoop()
        {
            if (ambientSource == null || ambientSource.isPlaying) return;

            // Generate 4-second procedural gentle filtered air breeze (no harsh white noise static)
            float dur = 4.0f;
            float lpfState = 0f;
            AudioClip ambientClip = GenerateTone(dur, (t) =>
            {
                float slowLfo = Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float softAir = Mathf.Sin(2f * Mathf.PI * 52f * t) * 0.12f;
                float rawNoise = (UnityEngine.Random.value * 2f - 1f) * 0.08f;
                lpfState = Mathf.Lerp(lpfState, rawNoise, 0.05f); // 1-pole gentle low-pass filter
                return (softAir + lpfState * (0.12f + 0.04f * slowLfo));
            });

            ambientSource.clip = ambientClip;
            ambientSource.volume = isMuted ? 0f : Mathf.Min(ambientVolume, 0.20f);
            ambientSource.Play();
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            AudioListener.volume = isMuted ? 0f : masterVolume;
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
        }

        public void SetAmbientVolume(float vol)
        {
            ambientVolume = Mathf.Clamp01(vol);
            if (ambientSource != null)
            {
                ambientSource.volume = isMuted ? 0f : ambientVolume;
                if (!ambientSource.isPlaying && !isMuted && ambientVolume > 0.01f)
                {
                    StartAmbientLoop();
                }
            }
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            AudioListener.volume = isMuted ? 0f : masterVolume;
            if (ambientSource != null)
            {
                ambientSource.volume = isMuted ? 0f : ambientVolume;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnItemCollected += (_, __) => PlayItemPickup();
            GameEvents.OnCraftingCompleted += (_) => PlayCraftSuccess();
            GameEvents.OnStealthTakedownPerformed += (_) => PlayTakedown();
            GameEvents.OnEmergencyPushPerformed += () => PlayPush();
            GameEvents.OnPlayerDamaged += (_) => PlayDamageSound();
            GameEvents.OnHitConfirmed += HandleHitConfirmed;
        }

        private void OnDisable()
        {
            GameEvents.OnHitConfirmed -= HandleHitConfirmed;
        }

        private void HandleHitConfirmed(bool isHeadshot, Vector3 hitPoint)
        {
            PlayHitImpact(isHeadshot);
        }

        private void Update()
        {
            if (isMuted) return;

            UpdateHeartbeatMonitor();
        }

        private void UpdateHeartbeatMonitor()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
                else return;
            }

            float closestZombieDist = float.MaxValue;
            var zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            foreach (var z in zombies)
            {
                if (z.IsDead) continue;
                float dist = Vector3.Distance(playerTransform.position, z.transform.position);
                if (dist < closestZombieDist) closestZombieDist = dist;
            }

            if (closestZombieDist < 12f)
            {
                float t = Mathf.Clamp01((12f - closestZombieDist) / 10f);
                float interval = Mathf.Lerp(1.2f, 0.45f, t);
                float vol = Mathf.Lerp(0.2f, 0.85f, t) * masterVolume;

                heartbeatTimer += Time.deltaTime;
                if (heartbeatTimer >= interval)
                {
                    heartbeatTimer = 0f;
                    PlayHeartbeat(vol);
                }
            }
            else
            {
                heartbeatTimer = 0f;
            }
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            AudioListener.pause = isMuted;
            Debug.Log($"[ProceduralAudio] Mute: {isMuted}");
        }

        public void PlayFootstep(bool isRunning)
        {
            PlaySurfaceFootstep(ProjectSurvival.Environment.GroundSurfaceType.AsphaltConcrete, isRunning, false);
        }

        public void PlaySurfaceFootstep(ProjectSurvival.Environment.GroundSurfaceType surface, bool isRunning, bool isLanding = false)
        {
            if (isMuted || footstepsSource == null) return;

            float dur = isLanding ? 0.16f : (isRunning ? 0.09f : 0.07f);
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float normT = t / dur;
                float env = Mathf.Exp(-normT * 18f);

                switch (surface)
                {
                    case ProjectSurvival.Environment.GroundSurfaceType.GrassSoil:
                    {
                        float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                        float thump = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.40f;
                        return (noise + thump) * env;
                    }
                    case ProjectSurvival.Environment.GroundSurfaceType.WoodInterior:
                    {
                        float f1 = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.65f;
                        float f2 = Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.25f;
                        return (f1 + f2) * env;
                    }
                    case ProjectSurvival.Environment.GroundSurfaceType.MetalIndustrial:
                    {
                        float ping = Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.55f;
                        float clang = Mathf.Sin(2f * Mathf.PI * 1280f * t) * 0.35f;
                        float ringEnv = Mathf.Exp(-normT * 9f);
                        return (ping + clang) * ringEnv;
                    }
                    case ProjectSurvival.Environment.GroundSurfaceType.ShallowWater:
                    {
                        float noise = (UnityEngine.Random.value * 2f - 1f) * 0.40f;
                        float bubble = Mathf.Sin(2f * Mathf.PI * Mathf.Max(60f, 240f - t * 700f) * t) * 0.50f;
                        return (noise + bubble) * env;
                    }
                    case ProjectSurvival.Environment.GroundSurfaceType.AsphaltConcrete:
                    default:
                    {
                        float heel = Mathf.Sin(2f * Mathf.PI * (isRunning ? 180f : 140f) * t) * 0.55f;
                        float tap = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                        return (heel + tap) * env;
                    }
                }
            });

            float baseVol = isLanding ? 0.75f : (isRunning ? 0.48f : 0.28f);
            footstepsSource.PlayOneShot(clip, baseVol * sfxVolume);
        }

        public AudioClip GenerateElectricalHumClip()
        {
            float dur = 2.0f; // 2-second warm analog hum (no harsh 180Hz buzz or sputter)
            return GenerateTone(dur, (t) =>
            {
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.45f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.15f;
                return (hum60 + hum120) * 0.4f;
            });
        }

        public void PlayWaterSplashFootstep(Vector3 position, bool isRunning = false)
        {
            if (isMuted) return;
            float dur = 0.12f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-t * 22f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                float bubble = Mathf.Sin(2f * Mathf.PI * Mathf.Max(60f, 220f - t * 600f) * t) * 0.45f;
                return (noise + bubble) * env;
            });
            footstepsSource.PlayOneShot(clip, (isRunning ? 0.5f : 0.32f) * sfxVolume);
        }

        public void PlayHeartbeat(float volume)
        {
            if (isMuted) return;
            float dur = 0.22f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float f = t < 0.11f ? 65f : 55f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env;
            });
            heartbeatSource.PlayOneShot(clip, volume * sfxVolume);
        }

        public void PlayItemPickup()
        {
            MobileHapticManager.TriggerLight();
            if (isMuted) return;
            float dur = 0.12f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.5f;
            });
            sfxSource.PlayOneShot(clip, 0.5f * sfxVolume);
        }

        public void PlayWeaponEquip()
        {
            MobileHapticManager.TriggerLight();
            if (isMuted) return;
            float dur = 0.16f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                float f = Mathf.Lerp(420f, 960f, t / dur);
                return (Mathf.Sin(2f * Mathf.PI * f * t) + (Mathf.Sin(2f * Mathf.PI * (f * 1.5f) * t) * 0.4f)) * env * 0.5f;
            });
            sfxSource.PlayOneShot(clip, 0.65f * sfxVolume);
        }

        public void PlayCraftSuccess()
        {
            MobileHapticManager.TriggerMedium();
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float freq = t < 0.15f ? 523f : 659f; // C5 to E5
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            });
            sfxSource.PlayOneShot(clip, 0.6f * sfxVolume);
        }

        public void PlayTakedown()
        {
            MobileHapticManager.TriggerHeavy();
            if (isMuted) return;
            float dur = 0.25f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float lowSine = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.6f;
                return (noise + lowSine) * env;
            });
            sfxSource.PlayOneShot(clip, 0.8f * sfxVolume);
        }

        public void PlayPush()
        {
            if (isMuted) return;
            float dur = 0.2f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                float f = Mathf.Lerp(120f, 40f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.8f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayDamageSound()
        {
            MobileHapticManager.TriggerHeavy();
            if (isMuted) return;
            float dur = 0.2f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                float tone = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f;
                return (noise + tone) * env;
            });
            sfxSource.PlayOneShot(clip, 0.7f * sfxVolume);
        }

        public void PlayButtonClick()
        {
            MobileHapticManager.TriggerLight();
            if (isMuted) return;
            float dur = 0.05f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * 440f * t) * env * 0.35f;
            });
            sfxSource.PlayOneShot(clip, 0.4f * sfxVolume);
        }

        public void PlayHitImpact(bool isHeadshot = false)
        {
            if (isMuted) return;
            float dur = isHeadshot ? 0.20f : 0.15f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = 1f - (t / dur);
                float freq = isHeadshot ? Mathf.Lerp(280f, 55f, t / dur) : Mathf.Lerp(200f, 40f, t / dur);
                float thump = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.70f;
                float crunch = (UnityEngine.Random.value * 2f - 1f) * 0.40f;
                return (thump + crunch) * env;
            });
            sfxSource.PlayOneShot(clip, (isHeadshot ? 0.95f : 0.8f) * sfxVolume);
        }

        public void PlayHitMarker(bool isHeadshot)
        {
            if (isMuted) return;
            float dur = isHeadshot ? 0.065f : 0.045f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-28f * (t / dur));
                float freq = isHeadshot ? 2600f : 2050f;
                float click = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.9f;
                return click * env;
            });
            sfxSource.PlayOneShot(clip, (isHeadshot ? 0.95f : 0.85f) * sfxVolume);
        }

        public void PlayGunfire(bool isRifle = false, float volumeScale = 1f)
        {
            if (isRifle) MobileHapticManager.TriggerHeavy();
            else MobileHapticManager.TriggerMedium();
            if (isMuted) return;
            AudioClip clip = GenerateGunfireClip(isRifle);
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(0.85f * volumeScale) * sfxVolume);
        }

        public AudioClip GenerateGunfireClip(bool isShotgun = false)
        {
            float dur = isShotgun ? 0.32f : 0.22f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-12f * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.85f;
                float lowPunch = Mathf.Sin(2f * Mathf.PI * (isShotgun ? 60f : 85f) * t) * 0.95f;
                return (noise + lowPunch) * env;
            });
        }

        public void PlaySuppressedGunfire(bool isShotgun = false)
        {
            if (isShotgun) MobileHapticManager.TriggerHeavy();
            else MobileHapticManager.TriggerMedium();
            if (isMuted) return;
            float dur = 0.14f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-22f * (t / dur));
                float crack = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                float thump = Mathf.Sin(2f * Mathf.PI * (isShotgun ? 110f : 150f) * t) * 0.4f;
                return (crack + thump) * env;
            });
            sfxSource.PlayOneShot(clip, 0.48f * sfxVolume);
        }

        public void PlayExplosion()
        {
            MobileHapticManager.TriggerHeavy();
            if (isMuted) return;
            float dur = 0.65f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-6.5f * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.9f;
                float subBoom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(60f, 30f, t / dur) * t) * 0.95f;
                return (noise * 0.7f + subBoom) * env;
            });
            sfxSource.PlayOneShot(clip, 0.95f * sfxVolume);
        }

        public void PlayArmorHit()
        {
            if (isMuted) return;
            float dur = 0.16f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-18f * (t / dur));
                float metallicClang = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 1150f * t) * 0.35f;
                return metallicClang * env;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayTrapDeploy()
        {
            if (isMuted) return;
            float dur = 0.25f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.75f;
                float scrape = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                return (thud + scrape) * env;
            });
            sfxSource.PlayOneShot(clip, 0.7f * sfxVolume);
        }

        public void PlayFirstAid()
        {
            if (isMuted) return;
            float dur = 0.4f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float f = Mathf.Lerp(440f, 660f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.45f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayBandageWrap()
        {
            if (isMuted) return;
            float dur = 0.5f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.55f;
                float friction = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.25f;
                return (noise + friction) * env * 0.65f;
            });
            sfxSource.PlayOneShot(clip, 0.8f * sfxVolume);
        }

        public void PlayBoneFracture()
        {
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-t * 12f);
                float crack = (UnityEngine.Random.value * 2f - 1f) * 0.85f;
                float thud = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.75f;
                return (crack + thud) * env;
            });
            sfxSource.PlayOneShot(clip, 0.95f * sfxVolume);
        }

        public void PlayPillSwallow()
        {
            if (isMuted) return;
            float dur = 0.28f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float pop = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.6f;
                return pop * env * 0.7f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlaySplintEquip()
        {
            if (isMuted) return;
            float dur = 0.45f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float woodClick = Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.45f;
                float strap = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                return (woodClick + strap) * env * 0.75f;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayEat()
        {
            if (isMuted) return;
            float dur = 0.28f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.75f;
                float crunch = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.35f;
                return (noise + crunch) * env * 0.7f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayDrink()
        {
            if (isMuted) return;
            float dur = 0.32f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float f = Mathf.Lerp(320f, 210f, t / dur);
                float gulp = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f;
                float splash = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                return (gulp + splash) * env;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayGlassShatter()
        {
            if (isMuted) return;
            float dur = 0.45f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-9f * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.9f;
                float ring = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.35f;
                return (noise + ring) * env;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlaySoftError()
        {
            if (isMuted) return;
            float dur = 0.12f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-16f * (t / dur));
                float tone = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.5f;
                return tone * env;
            });
            sfxSource.PlayOneShot(clip, 0.65f * sfxVolume);
        }

        public void PlayAirRaidSiren()
        {
            if (isMuted) return;
            float dur = 2.5f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float freq = 450f + 250f * Mathf.Sin(2f * Mathf.PI * 0.8f * t);
                float env = Mathf.Clamp01(t * 3f) * Mathf.Clamp01((dur - t) * 2f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.65f;
            });
            sfxSource.PlayOneShot(clip, 0.8f * sfxVolume);
        }

        public void PlayScreamerNoise()
        {
            if (isMuted) return;
            float dur = 0.85f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.55f;
                float screech = Mathf.Sin(2f * Mathf.PI * (800f + Mathf.Sin(50f * t) * 300f) * t) * 0.7f;
                return (screech + noise) * env;
            });
            sfxSource.PlayOneShot(clip, 0.9f * sfxVolume);
        }

        public void PlayZombieQuestioningGroan()
        {
            if (isMuted) return;
            float dur = 0.65f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float f = Mathf.Lerp(110f, 150f, t / dur);
                float guttural = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f;
                float rasp = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                return (guttural + rasp) * env * 0.8f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayZombieAlertSnarl()
        {
            if (isMuted) return;
            float dur = 0.55f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                float f = Mathf.Lerp(260f, 190f, t / dur);
                float snarl = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f;
                float distortion = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                return (snarl + distortion) * env * 0.85f;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayZombieAttackSwipe()
        {
            if (isMuted) return;
            float dur = 0.28f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-7f * (t / dur));
                float f = Mathf.Lerp(300f, 90f, t / dur);
                float whoosh = Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f;
                float hitNoise = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                return (whoosh + hitNoise) * env;
            });
            sfxSource.PlayOneShot(clip, 0.8f * sfxVolume);
        }

        public void PlayScreamerShriek()
        {
            if (isMuted) return;
            float dur = 1.15f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
                float lfo = Mathf.Sin(2f * Mathf.PI * 18f * t);
                float f = Mathf.Lerp(650f, 1350f, t / dur) + lfo * 90f;
                float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.75f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                return (tone + noise) * env * 0.95f;
            });
            sfxSource.PlayOneShot(clip, 0.95f * sfxVolume);
        }

        public void PlayDoorPoundImpact(Vector3 position, bool isMetal = false)
        {
            if (isMuted) return;
            float dur = 0.22f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-9f * (t / dur));
                float baseFreq = isMetal ? 280f : 110f;
                float f = Mathf.Lerp(baseFreq, 45f, t / dur);
                float impact = Mathf.Sin(2f * Mathf.PI * f * t) * 0.85f;
                float rattle = (UnityEngine.Random.value * 2f - 1f) * (isMetal ? 0.6f : 0.35f);
                return (impact + rattle) * env;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayPackAlertBark()
        {
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-6f * (t / dur));
                float f = Mathf.Lerp(210f, 130f, t / dur);
                float bark = Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                return (bark + noise) * env * 0.8f;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayObjectiveSuccess()
        {
            if (isMuted) return;
            float dur = 0.6f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-4f * (t / dur));
                float f1 = t < 0.2f ? 523.25f : 659.25f; // C5 to E5
                float f2 = t < 0.2f ? 659.25f : 783.99f; // E5 to G5
                return (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.6f) * env * 0.5f;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayLevelUp()
        {
            if (isMuted) return;
            float dur = 0.85f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-3f * (t / dur));
                // Ascending harmonic fanfare: C5 -> E5 -> G5 -> C6
                float f = 523.25f;
                if (t > 0.45f) f = 1046.50f;
                else if (t > 0.30f) f = 783.99f;
                else if (t > 0.15f) f = 659.25f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.65f;
            });
            sfxSource.PlayOneShot(clip, 0.9f * sfxVolume);
        }

        public void PlayBossRoar()
        {
            if (isMuted) return;
            float dur = 1.2f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float f = 65f + Mathf.Sin(t * 18f) * 15f; // Deep gutter rumble
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) + noise) * env * 0.85f;
            });
            sfxSource.PlayOneShot(clip, 0.95f * sfxVolume);
        }

        public void PlayVaultUnlock()
        {
            if (isMuted) return;
            float dur = 0.9f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-3.5f * (t / dur));
                float f = t < 0.4f ? 440f : 880f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.7f;
            });
            sfxSource.PlayOneShot(clip, 0.9f * sfxVolume);
        }

        public void PlayTireSkid()
        {
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float f = Mathf.Lerp(850f, 620f, t / dur);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.65f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.35f + noise * 0.65f) * env * 0.5f;
            });
            sfxSource.PlayOneShot(clip, 0.65f * sfxVolume);
        }

        public void PlayWrenchRepair()
        {
            if (isMuted) return;
            float dur = 0.45f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-4f * (t / dur));
                float f = Mathf.Lerp(1200f, 600f, t / dur);
                float clank = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f;
                float spark = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                return (clank + spark) * env;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayRefuelLiquid()
        {
            if (isMuted) return;
            float dur = 0.5f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float f = 280f + Mathf.Sin(t * 22f) * 45f;
                float splash = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f + splash) * env;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayVehicleHorn()
        {
            if (isMuted) return;
            float dur = 0.45f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = (t < 0.04f) ? (t / 0.04f) : ((t > 0.40f) ? (0.45f - t) / 0.05f : 1f);
                float h1 = Mathf.Sin(2f * Mathf.PI * 435f * t);
                float h2 = Mathf.Sin(2f * Mathf.PI * 345f * t) * 0.85f;
                return (h1 + h2) * 0.5f * env;
            });
            sfxSource.PlayOneShot(clip, 0.95f * sfxVolume);
        }

        public void PlayVehicleCrash()
        {
            if (isMuted) return;
            float dur = 0.38f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-7f * (t / dur));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.7f;
                float bass = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.8f;
                float clank = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.4f * Mathf.Exp(-14f * (t / dur));
                return (bass + noise + clank) * env;
            });
            sfxSource.PlayOneShot(clip, 1.0f * sfxVolume);
        }

        public void PlayBoilingSteam()
        {
            if (isMuted) return;
            float dur = 0.55f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = (t < 0.08f) ? (t / 0.08f) : Mathf.Exp(-3.5f * (t / dur));
                float bubble = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.45f;
                float hiss = (UnityEngine.Random.value * 2f - 1f) * 0.55f;
                return (bubble + hiss) * env;
            });
            sfxSource.PlayOneShot(clip, 0.70f * sfxVolume);
        }

        public void PlayChemistryGlassware()
        {
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-9f * (t / dur));
                float clink1 = Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.6f;
                float clink2 = Mathf.Sin(2f * Mathf.PI * 2720f * t) * 0.4f;
                return (clink1 + clink2) * env;
            });
            sfxSource.PlayOneShot(clip, 0.65f * sfxVolume);
        }

        public void PlayAdrenalineHeartRush()
        {
            if (isMuted) return;
            float dur = 0.65f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-4.5f * (t / dur));
                float thump1 = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.8f;
                float thump2 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f;
                float surge = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                return (thump1 + thump2 + surge) * env;
            });
            sfxSource.PlayOneShot(clip, 0.90f * sfxVolume);
        }

        public void PlayRadioFrequencyLockChime()
        {
            if (isMuted) return;
            float dur = 0.35f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-7f * (t / dur));
                float f = t < 0.12f ? 1200f : 1850f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.75f;
            });
            sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
        }

        public void PlayRadioDecodeSuccess()
        {
            if (isMuted) return;
            float dur = 0.55f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-5f * (t / dur));
                float f = 880f;
                if (t > 0.32f) f = 1760f;
                else if (t > 0.16f) f = 1320f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.8f;
            });
            sfxSource.PlayOneShot(clip, 0.90f * sfxVolume);
        }

        public void PlayAudioLogPlaybackCue()
        {
            if (isMuted) return;
            float dur = 0.45f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                // Mechanical tape head engage click + short analog hum
                float click = t < 0.04f ? (UnityEngine.Random.value * 2f - 1f) * 0.8f : 0f;
                float hum = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.35f * Mathf.Exp(-4f * (t / dur));
                return click + hum;
            });
            sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
        }

        public void PlayRadioTransmissionBeep()
        {
            if (isMuted) return;
            float dur = 0.22f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-6f * (t / dur));
                return Mathf.Sin(2f * Mathf.PI * 1480f * t) * env * 0.65f;
            });
            sfxSource.PlayOneShot(clip, 0.70f * sfxVolume);
        }

        public void PlayRadioStaticBurst(float volumeScale = 1f)
        {
            if (isMuted) return;
            float dur = 0.25f;
            AudioClip clip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.6f;
                float whistle = Mathf.Sin(2f * Mathf.PI * (600f + t * 400f) * t) * 0.15f;
                return (noise + whistle) * env;
            });
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * 0.60f * sfxVolume);
        }

        // =========================================================================
        // Phase 2 (Step 2B): Dynamic World Events Audio (Helicopter, Flare, SOS)
        // =========================================================================

        public AudioClip GenerateHelicopterFlyoverClip()
        {
            float dur = 2.0f;
            return GenerateTone(dur, (t) =>
            {
                float rotorBeat = Mathf.Sin(2f * Mathf.PI * 14f * t); // 14 Hz rotor chop
                float rotorEnv = Mathf.Clamp01(rotorBeat * 1.5f);
                float turbine = Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.25f;
                float lowDrone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.65f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.18f;
                return (lowDrone * rotorEnv + turbine + noise) * 0.85f;
            });
        }

        public AudioClip GenerateSignalFlareHissClip()
        {
            float dur = 1.5f;
            return GenerateTone(dur, (t) =>
            {
                float whiteNoise = (UnityEngine.Random.value * 2f - 1f) * 0.65f;
                float crackle = (UnityEngine.Random.value > 0.94f) ? (UnityEngine.Random.value * 0.4f) : 0f;
                float hissTone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.15f;
                return (whiteNoise + crackle + hissTone) * 0.60f;
            });
        }

        public AudioClip GenerateCivilianDistressClip()
        {
            float dur = 0.65f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float freq = Mathf.Lerp(349.23f, 293.66f, t / dur); // F4 to D4 distress descent
                float vocal = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.70f;
                float tremolo = 1f + 0.18f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                return vocal * tremolo * env * 0.80f;
            });
        }

        public void PlayHelicopterFlyover(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateHelicopterFlyoverClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.95f * sfxVolume);
        }

        public void PlayCivilianDistress(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateCivilianDistressClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.85f * sfxVolume);
        }

        public AudioClip GenerateSurvivorReliefClip()
        {
            float dur = 0.85f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float freq = Mathf.Lerp(261.63f, 329.63f, Mathf.SmoothStep(0f, 1f, t / dur)); // C4 to E4 uplifting relief
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.20f;
                return tone * env * 0.75f;
            });
        }

        public void PlaySurvivorRelief(Vector3? worldPos = null)
        {
            if (isMuted) return;
            AudioClip clip = GenerateSurvivorReliefClip();
            if (worldPos.HasValue)
            {
                AudioSource.PlayClipAtPoint(clip, worldPos.Value, 0.85f * sfxVolume);
            }
            else if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, 0.85f * sfxVolume);
            }
        }

        public AudioClip GenerateOutpostSirenClip()
        {
            float dur = 3.2f;
            return GenerateTone(dur, t =>
            {
                float freq = 420f + 320f * Mathf.Sin(2f * Mathf.PI * 0.65f * t);
                float env = Mathf.Clamp01(t * 3f) * Mathf.Clamp01((dur - t) * 2f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.70f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f;
                return tone * env * 0.85f;
            });
        }

        public void PlayOutpostSiren(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateOutpostSirenClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 1.0f * sfxVolume);
        }

        public AudioClip GenerateHordeGroanClip()
        {
            float dur = 2.8f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float freq = 82f + 16f * Mathf.Sin(2f * Mathf.PI * 0.4f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f;
                float throat = Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f;
                float tremor = 1f + 0.25f * Mathf.Sin(2f * Mathf.PI * 4.2f * t);
                float gravel = (UnityEngine.Random.value * 2f - 1f) * 0.12f;
                return (sub + throat + gravel) * tremor * env * 0.85f;
            });
        }

        public void PlayDistantHordeGroan(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateHordeGroanClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.90f * sfxVolume);
        }

        public AudioClip GenerateBowstringReleaseClip()
        {
            float dur = 0.14f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Exp(-t * 28f);
                float snap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(340f, 140f, t / dur) * t) * 0.75f;
                float twang = Mathf.Sin(2f * Mathf.PI * 1350f * t) * 0.35f * Mathf.Exp(-t * 45f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f * env;
                return (snap + twang + noise) * env * 0.85f;
            });
        }

        public void PlayBowstringRelease(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateBowstringReleaseClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.75f * sfxVolume);
        }

        public AudioClip GenerateBoltWhooshClip()
        {
            float dur = 0.24f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float whistle = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(820f, 640f, t / dur) * t) * 0.45f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.30f;
                return (whistle + noise) * env * 0.60f;
            });
        }

        public void PlayBoltWhoosh(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateBoltWhooshClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.65f * sfxVolume);
        }

        public AudioClip GenerateBoltImpactThudClip(bool isFlesh = false)
        {
            float dur = 0.20f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Exp(-t * 22f);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(isFlesh ? 95f : 145f, 45f, t / dur) * t) * 0.80f;
                float crack = (UnityEngine.Random.value * 2f - 1f) * (isFlesh ? 0.20f : 0.45f) * Mathf.Exp(-t * 40f);
                return (thud + crack) * env * 0.90f;
            });
        }

        public void PlayBoltImpactThud(Vector3 worldPos, bool isFlesh = false)
        {
            if (isMuted) return;
            AudioClip clip = GenerateBoltImpactThudClip(isFlesh);
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.85f * sfxVolume);
        }

        public AudioClip GenerateQuiverClickClip()
        {
            float dur = 0.08f;
            return GenerateTone(dur, t =>
            {
                float env = Mathf.Exp(-t * 60f);
                float click = Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.70f;
                float rattle = (UnityEngine.Random.value * 2f - 1f) * 0.30f;
                return (click + rattle) * env * 0.80f;
            });
        }

        public void PlayQuiverClick(Vector3? worldPos = null)
        {
            if (isMuted) return;
            AudioClip clip = GenerateQuiverClickClip();
            if (worldPos.HasValue)
            {
                AudioSource.PlayClipAtPoint(clip, worldPos.Value, 0.75f * sfxVolume);
            }
            else if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, 0.75f * sfxVolume);
            }
        }

        // =========================================================================
        // PHASE 3 (STEP 3B): WILDLIFE, HARVESTING, LAKE FISHING & GRILLING
        // =========================================================================

        public AudioClip GenerateAnimalGruntClip(bool isBoar)
        {
            float dur = isBoar ? 0.42f : 0.28f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                if (isBoar)
                {
                    // Low resonant feral grunt & snort
                    float lowF = Mathf.Lerp(110f, 75f, t / dur);
                    float wave = Mathf.Sin(2f * Mathf.PI * lowF * t) * 0.7f;
                    float rumble = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                    return (wave + rumble) * env;
                }
                else
                {
                    // Deer alarm whistle / breath snort
                    float highF = Mathf.Lerp(620f, 480f, t / dur);
                    float breath = (UnityEngine.Random.value * 2f - 1f) * 0.6f;
                    float tone = Mathf.Sin(2f * Mathf.PI * highF * t) * 0.4f;
                    return (tone + breath) * env;
                }
            });
        }

        public void PlayAnimalGrunt(Vector3 worldPos, bool isBoar)
        {
            if (isMuted) return;
            AudioClip clip = GenerateAnimalGruntClip(isBoar);
            AudioSource.PlayClipAtPoint(clip, worldPos, (isBoar ? 0.95f : 0.85f) * sfxVolume);
        }

        public AudioClip GenerateKnifeHarvestClip()
        {
            float dur = 0.38f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-5f * (t / dur));
                float f = Mathf.Lerp(1400f, 750f, t / dur);
                float blade = Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f;
                float scrape = (UnityEngine.Random.value * 2f - 1f) * 0.55f;
                return (blade + scrape) * env;
            });
        }

        public void PlayKnifeHarvest(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateKnifeHarvestClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.90f * sfxVolume);
        }

        public AudioClip GenerateWaterSplashClip()
        {
            float dur = 0.48f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float f = Mathf.Lerp(280f, 120f, t / dur);
                float bubble = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f;
                float splashNoise = (UnityEngine.Random.value * 2f - 1f) * 0.55f;
                return (bubble + splashNoise) * env;
            });
        }

        public void PlayWaterSplash(Vector3 worldPos, float volume = 0.85f)
        {
            if (isMuted) return;
            AudioClip clip = GenerateWaterSplashClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, volume * sfxVolume);
        }

        public AudioClip GenerateReelClickClip()
        {
            float dur = 0.12f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-t * 45f);
                float click = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.65f;
                float snap = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                return (click + snap) * env;
            });
        }

        public void PlayReelClick(Vector3? worldPos = null)
        {
            if (isMuted) return;
            AudioClip clip = GenerateReelClickClip();
            if (worldPos.HasValue)
            {
                AudioSource.PlayClipAtPoint(clip, worldPos.Value, 0.70f * sfxVolume);
            }
            else if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, 0.70f * sfxVolume);
            }
        }

        public AudioClip GenerateGrillSizzleClip()
        {
            float dur = 0.65f;
            return GenerateTone(dur, (t) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float crackle = (UnityEngine.Random.value * 2f - 1f) * 0.85f;
                float hiss = Mathf.Sin(2f * Mathf.PI * 3400f * t) * 0.15f;
                return (crackle + hiss) * env * 0.7f;
            });
        }

        public void PlayGrillSizzle(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateGrillSizzleClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.90f * sfxVolume);
        }

        private AudioClip cachedPumpMotorClip;
        private AudioClip cachedNozzleClickClip;
        private AudioClip cachedSiphonGurgleClip;

        public AudioClip GeneratePumpMotorClip()
        {
            if (cachedPumpMotorClip != null) return cachedPumpMotorClip;
            float dur = 1.8f;
            cachedPumpMotorClip = GenerateTone(dur, (t) =>
            {
                float hum = Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.45f;
                float whir = Mathf.Sin(2f * Mathf.PI * 240f * t + Mathf.Sin(t * 15f)) * 0.25f;
                float fluid = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                return (hum + whir + fluid);
            });
            return cachedPumpMotorClip;
        }

        public void PlayPumpMotor(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GeneratePumpMotorClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.85f * sfxVolume);
        }

        public AudioClip GenerateNozzleClickClip()
        {
            if (cachedNozzleClickClip != null) return cachedNozzleClickClip;
            float dur = 0.15f;
            cachedNozzleClickClip = GenerateTone(dur, (t) =>
            {
                float env = Mathf.Exp(-35f * (t / dur));
                float click = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.7f;
                float thud = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.5f;
                return (click + thud) * env;
            });
            return cachedNozzleClickClip;
        }

        public void PlayNozzleClick(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateNozzleClickClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.90f * sfxVolume);
        }

        public AudioClip GenerateSiphonGurgleClip()
        {
            if (cachedSiphonGurgleClip != null) return cachedSiphonGurgleClip;
            float dur = 2.0f;
            cachedSiphonGurgleClip = GenerateTone(dur, (t) =>
            {
                float bubble = Mathf.Sin(2f * Mathf.PI * (160f + Mathf.Sin(t * 28f) * 70f) * t) * 0.5f;
                float hiss = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                float pulse = Mathf.Sin(t * 12f) * 0.5f + 0.5f;
                return (bubble * pulse + hiss * 0.35f);
            });
            return cachedSiphonGurgleClip;
        }

        public void PlaySiphonGurgle(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateSiphonGurgleClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.85f * sfxVolume);
        }

        private AudioClip cachedGeneratorRoarClip;
        private AudioClip cachedBlastDoorSlideClip;
        private AudioClip cachedFluorescentHumClip;
        private AudioClip cachedMutantRoarClip;

        public AudioClip GenerateGeneratorRoarClip()
        {
            if (cachedGeneratorRoarClip != null) return cachedGeneratorRoarClip;
            float dur = 2.2f;
            cachedGeneratorRoarClip = GenerateTone(dur, (t) =>
            {
                float crank = (t < 0.8f) ? Mathf.Sin(2f * Mathf.PI * 18f * t) * (1f - t / 0.8f) : 0f;
                float rumble = (t >= 0.6f) ? Mathf.Sin(2f * Mathf.PI * (48f + Mathf.Sin(t * 12f) * 6f) * t) * 0.7f : 0f;
                float exhaust = (UnityEngine.Random.value * 2f - 1f) * 0.35f * Mathf.Clamp01(t / 0.5f);
                return (crank + rumble + exhaust);
            });
            return cachedGeneratorRoarClip;
        }

        public void PlayGeneratorRoar(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateGeneratorRoarClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.95f * sfxVolume);
        }

        public AudioClip GenerateBlastDoorSlideClip()
        {
            if (cachedBlastDoorSlideClip != null) return cachedBlastDoorSlideClip;
            float dur = 1.8f;
            cachedBlastDoorSlideClip = GenerateTone(dur, (t) =>
            {
                float hydraulic = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f;
                float scrape = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                float motorWhine = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.2f;
                return (hydraulic + scrape + motorWhine);
            });
            return cachedBlastDoorSlideClip;
        }

        public void PlayBlastDoorSlide(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateBlastDoorSlideClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.90f * sfxVolume);
        }

        public AudioClip GenerateFluorescentHumClip()
        {
            if (cachedFluorescentHumClip != null) return cachedFluorescentHumClip;
            float dur = 1.5f;
            cachedFluorescentHumClip = GenerateTone(dur, (t) =>
            {
                float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.4f;
                float buzz = Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f;
                return (hum + buzz);
            });
            return cachedFluorescentHumClip;
        }

        public void PlayFluorescentHum(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateFluorescentHumClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.65f * sfxVolume);
        }

        public AudioClip GenerateMutantRoarClip()
        {
            if (cachedMutantRoarClip != null) return cachedMutantRoarClip;
            float dur = 1.6f;
            cachedMutantRoarClip = GenerateTone(dur, (t) =>
            {
                float freq = Mathf.Lerp(110f, 65f, t / dur);
                float growl = Mathf.Sin(2f * Mathf.PI * freq * t + Mathf.Sin(t * 45f) * 2f) * 0.75f;
                float rasp = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                return (growl + rasp) * env;
            });
            return cachedMutantRoarClip;
        }

        public void PlayMutantRoar(Vector3 worldPos)
        {
            if (isMuted) return;
            AudioClip clip = GenerateMutantRoarClip();
            AudioSource.PlayClipAtPoint(clip, worldPos, 0.95f * sfxVolume);
        }

        private AudioClip GenerateTone(float duration, Func<float, float> waveFunc)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] data = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProcAudio_" + Guid.NewGuid().ToString("N").Substring(0, 6), numSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
