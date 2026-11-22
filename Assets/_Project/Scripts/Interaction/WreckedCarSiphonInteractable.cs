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
    /// Interactive weathered, non-drivable wrecked car fuel siphon point.
    /// Allows players carrying item_empty_jerrycan to siphon residual fuel (8L - 18L)
    /// into a usable item_fuel_can with a 2.0s physical hold action and 3D liquid audio.
    /// Equipped with NavMeshObstacle carving so zombies and NPCs navigate cleanly around wrecks.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WreckedCarSiphonInteractable : WorldInteractableObject
    {
        [Header("Wreck Configuration")]
        [SerializeField] private string wreckId = "wreck_01";
        [SerializeField] private float fuelReserveLiters = 14f;
        [SerializeField] private bool isSiphoned = false;

        [Header("Audio & Feedback")]
        [SerializeField] private AudioSource audioSource;

        private AudioClip cachedSiphonClip;
        private NavMeshObstacle navObstacle;

        public string WreckId => wreckId;
        public float FuelReserveLiters => fuelReserveLiters;
        public bool IsSiphoned => isSiphoned || fuelReserveLiters <= 0.05f;

        public override string ObjectSubtitle
        {
            get
            {
                if (IsSiphoned) return "Depo Boş";
                return $"Kalan Yakıt: ~{Mathf.RoundToInt(fuelReserveLiters)}L";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Terk Edilmiş Araç Enkazı";
            objectSubtitle = IsSiphoned ? "Depo Boş" : $"Kalan Yakıt: ~{Mathf.RoundToInt(fuelReserveLiters)}L";

            // Configure 3D Positional AudioSource
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1.0f; // Pure 3D positional
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 22.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.playOnAwake = false;

            // Ensure NavMeshObstacle with carving
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.carveOnlyStationary = false;
        }

        private void Start()
        {
            CacheAudioClip();
        }

        private void CacheAudioClip()
        {
            if (ProceduralAudioManager.Instance != null)
            {
                cachedSiphonClip = ProceduralAudioManager.Instance.GenerateSiphonGurgleClip();
            }
        }

        public void SetFuelReserveLiters(float liters)
        {
            fuelReserveLiters = Mathf.Max(0f, liters);
            isSiphoned = fuelReserveLiters <= 0.05f;
            hasBeenSearched = isSiphoned;
        }

        public void ConfigureWreck(string id, float liters)
        {
            wreckId = id;
            fuelReserveLiters = liters;
            isSiphoned = liters <= 0.05f;
            hasBeenSearched = isSiphoned;
            SetPersistentId(id);
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (IsSiphoned)
            {
                verbs.Add(new InteractionVerb(
                    "siphon_fuel",
                    "DEPO BOŞ (0L)",
                    0f,
                    null,
                    null,
                    0f,
                    false,
                    "Bu aracın deposundaki yakıt daha önce çekilmiş."
                ));
            }
            else
            {
                bool hasEmptyCan = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_empty_jerrycan", 1);
                string label = hasEmptyCan
                    ? $"YAKIT ÇEK [Hortumla Sifonla] ({Mathf.RoundToInt(fuelReserveLiters)}L)"
                    : "YAKIT ÇEK (BOŞ BİDON GEREKLİ)";

                verbs.Add(new InteractionVerb(
                    "siphon_fuel",
                    label,
                    2.0f, // 2.0s physical hold duration
                    "item_empty_jerrycan",
                    "Boş Yakıt Bidonu",
                    6f,
                    hasEmptyCan,
                    hasEmptyCan ? "" : "Yakıt çekmek için boş bir yakıt bidonu taşımalısınız."
                ));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (verbId != "siphon_fuel") return false;
            if (IsSiphoned) return false;
            return InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_empty_jerrycan", 1);
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "siphon_fuel" && !IsSiphoned)
            {
                if (InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_empty_jerrycan", 1))
                {
                    float drained = fuelReserveLiters;
                    fuelReserveLiters = 0f;
                    isSiphoned = true;
                    hasBeenSearched = true;

                    InventorySystem.Instance.RemoveItem("item_empty_jerrycan", 1);
                    InventorySystem.Instance.AddItemById("item_fuel_can", 1);

                    PlaySiphonAudio();
                    MobileHapticManager.Instance?.TriggerMedium();
                    SurvivorProgressionManager.Instance?.AddXP(20);

                    GameEvents.TriggerObjectiveToastRequested(
                        $"YAKIT SİFONLANDI (+{Mathf.RoundToInt(drained)}L) | 1x Dolu Yakıt Bidonu Elde Edildi! (+20 XP)", 2.5f);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("BOŞ BİDON BULUNAMADI!", 2.0f);
                }
            }
        }

        private void PlaySiphonAudio()
        {
            if (audioSource != null)
            {
                if (cachedSiphonClip == null && ProceduralAudioManager.Instance != null)
                {
                    cachedSiphonClip = ProceduralAudioManager.Instance.GenerateSiphonGurgleClip();
                }
                if (cachedSiphonClip != null)
                {
                    audioSource.PlayOneShot(cachedSiphonClip, 0.85f);
                }
            }
            else if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlaySiphonGurgle(transform.position);
            }
        }

        public override string GetSerializedState()
        {
            return $"{fuelReserveLiters:F1}|{isSiphoned}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            string[] parts = state.Split('|');
            if (parts.Length >= 1)
            {
                if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedFuel))
                {
                    fuelReserveLiters = parsedFuel;
                }
            }
            if (parts.Length >= 2)
            {
                if (bool.TryParse(parts[1], out bool parsedSiphoned))
                {
                    isSiphoned = parsedSiphoned;
                }
            }
            hasBeenSearched = isSiphoned || fuelReserveLiters <= 0.05f;
        }
    }
}
