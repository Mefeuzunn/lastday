using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Auxiliary Diesel Backup Generator inside Chamber 2 of the Underground Research Bunker.
    /// Initially unpowered, leaving Chambers 3-5 in pitch darkness.
    /// Interacting consumes 1x item_fuel_can (returns 1x item_empty_jerrycan), roars to life
    /// with 3D diesel audio, switches on all fluorescent lights, energizes security doors, and awards +50 XP.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BunkerGeneratorInteractable : WorldInteractableObject
    {
        [Header("Generator Properties")]
        [SerializeField] private bool isGeneratorRunning = false;

        [Header("Audio & Acoustics")]
        [SerializeField] private AudioSource generatorAudioSource;

        private AudioClip cachedRoarClip;
        private NavMeshObstacle navObstacle;

        public bool IsGeneratorRunning => isGeneratorRunning;

        public override string ObjectSubtitle => isGeneratorRunning ? "Durum: Aktif (Şebeke Besleniyor)" : "Durum: Devre Dışı (Yakıt Gerekli)";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Endüstriyel Yedek Jeneratör";
            objectSubtitle = isGeneratorRunning ? "Durum: Aktif" : "Durum: Devre Dışı";

            // Configure 3D Positional AudioSource
            if (generatorAudioSource == null) generatorAudioSource = GetComponent<AudioSource>();
            if (generatorAudioSource == null) generatorAudioSource = gameObject.AddComponent<AudioSource>();

            generatorAudioSource.spatialBlend = 1.0f; // Pure 3D positional
            generatorAudioSource.minDistance = 2.0f;
            generatorAudioSource.maxDistance = 25.0f;
            generatorAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            generatorAudioSource.playOnAwake = false;
            generatorAudioSource.loop = true;
            generatorAudioSource.volume = 0f;
            generatorAudioSource.Stop();

            // NavMeshObstacle with carving
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.carveOnlyStationary = false;
        }

        private void Start()
        {
            if (ProceduralAudioManager.Instance != null)
            {
                cachedRoarClip = ProceduralAudioManager.Instance.GenerateGeneratorRoarClip();
            }

            if (isGeneratorRunning)
            {
                StartRunningAudio();
            }
            else if (generatorAudioSource != null)
            {
                generatorAudioSource.Stop();
                generatorAudioSource.volume = 0f;
            }
        }

        public void SetPoweredSilent(bool running)
        {
            isGeneratorRunning = running;
            if (isGeneratorRunning)
            {
                StartRunningAudio();
            }
            else if (generatorAudioSource != null)
            {
                generatorAudioSource.Stop();
                generatorAudioSource.volume = 0f;
            }
            else if (generatorAudioSource != null && generatorAudioSource.isPlaying)
            {
                generatorAudioSource.Stop();
            }
        }

        private void StartRunningAudio()
        {
            if (generatorAudioSource != null)
            {
                if (cachedRoarClip == null && ProceduralAudioManager.Instance != null)
                {
                    cachedRoarClip = ProceduralAudioManager.Instance.GenerateGeneratorRoarClip();
                }
                if (cachedRoarClip != null)
                {
                    generatorAudioSource.clip = cachedRoarClip;
                    generatorAudioSource.volume = 0.85f;
                    generatorAudioSource.Play();
                }
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isGeneratorRunning)
            {
                verbs.Add(new InteractionVerb(
                    "start_generator",
                    "YEDEK JENERATÖR AKTİF (Şebeke Besleniyor)",
                    0f,
                    null,
                    null,
                    0f,
                    false,
                    "Jeneratör zaten çalışıyor ve sığınağa güç veriyor."
                ));
            }
            else
            {
                bool hasFuel = InventorySystem.Instance != null &&
                    (InventorySystem.Instance.HasItem("item_fuel_can", 1) || InventorySystem.Instance.HasItem("item_fuel", 1));

                string label = hasFuel
                    ? "YEDEK JENERATÖRÜ ÇALIŞTIR (1 Yakıt Bidonu)"
                    : "YEDEK JENERATÖRÜ ÇALIŞTIR (YAKIT BİDONU GEREKLİ)";

                verbs.Add(new InteractionVerb(
                    "start_generator",
                    label,
                    1.5f, // 1.5s timed action
                    "item_fuel_can",
                    "Dolu Yakıt Bidonu",
                    22f,
                    hasFuel,
                    hasFuel ? "" : "Jeneratörü çalıştırmak için 1x Dolu Yakıt Bidonu gereklidir."
                ));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (verbId != "start_generator") return false;
            if (isGeneratorRunning) return false;
            return InventorySystem.Instance != null &&
                (InventorySystem.Instance.HasItem("item_fuel_can", 1) || InventorySystem.Instance.HasItem("item_fuel", 1));
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "start_generator" && !isGeneratorRunning)
            {
                bool consumed = false;
                if (InventorySystem.Instance != null)
                {
                    if (InventorySystem.Instance.HasItem("item_fuel_can", 1))
                    {
                        consumed = InventorySystem.Instance.RemoveItem("item_fuel_can", 1);
                    }
                    else if (InventorySystem.Instance.HasItem("item_fuel", 1))
                    {
                        consumed = InventorySystem.Instance.RemoveItem("item_fuel", 1);
                    }
                }

                if (consumed)
                {
                    // Return empty jerrycan for reusable loop
                    InventorySystem.Instance?.AddItemById("item_empty_jerrycan", 1);

                    isGeneratorRunning = true;
                    StartRunningAudio();
                    MobileHapticManager.TriggerHeavy();
                    SurvivorProgressionManager.Instance?.AddXP(50);

                    // Notify bunker director to ignite fluorescent lights and unlock doors
                    UndergroundBunkerDirector.Instance?.OnGeneratorPowered();

                    GameEvents.TriggerObjectiveToastRequested(
                        "[GÜÇ VERİLDİ] Yedek Jeneratör Devrede! Sığınak Işıkları ve Kapı Manyetikleri Aktif (+50 XP)", 3.0f);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("YAKIT BULUNAMADI!", 2.0f);
                }
            }
        }

        public override string GetSerializedState()
        {
            return $"{isGeneratorRunning}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            if (bool.TryParse(state, out bool running))
            {
                SetPoweredSilent(running);
            }
        }
    }
}
