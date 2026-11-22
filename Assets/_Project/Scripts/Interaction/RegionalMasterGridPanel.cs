using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Master Regional Power Substation Grid Panel.
    /// Placed inside the Southeast Substation Control Room (Sector_1_-1_SouthEast).
    /// Requires 2 Alcohol/Fuel and 2 Scrap Metal to repair and energize.
    /// 
    /// Tactical Trade-off:
    /// - Emits a massive 60m transformer electrical hum and siren, triggering an immediate horde alert.
    /// - Permanently restores power and illuminates streetlamps, warehouse floodlights, and facility fixtures
    ///   across Sector_1_0_East, Sector_1_1_NorthEast, and Sector_1_-1_SouthEast!
    /// </summary>
    public class RegionalMasterGridPanel : WorldInteractableObject
    {
        [Header("Substation Configuration")]
        [SerializeField] private string stationName = "Bölgesel Ana Trafo Dağıtım Panosu";
        [SerializeField] private float hordeAlertRadius = 60.0f;
        [SerializeField] private AudioSource transformerHumSource;
        [SerializeField] private List<Light> managedEasternLights = new List<Light>();
        [SerializeField] private List<Renderer> managedEmissiveFixtures = new List<Renderer>();

        private Coroutine humNoiseRoutine;
        private static MaterialPropertyBlock sharedBlock;

        public override string ObjectSubtitle => isPowered
            ? "GÜÇ AKTİF (DOĞU ŞEBEKESİ ENERJİLENDİRİLDİ)"
            : "GÜÇ KESİK (2x Yakıt + 2x Hurda Gerekli)";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = stationName;
            SetupAudioSource();
            DiscoverEasternSectorLights();
        }

        private void Start()
        {
            ApplyPowerState(isPowered, false);
        }

        private void SetupAudioSource()
        {
            if (transformerHumSource == null)
            {
                transformerHumSource = GetComponent<AudioSource>();
                if (transformerHumSource == null) transformerHumSource = gameObject.AddComponent<AudioSource>();
                transformerHumSource.playOnAwake = false;
                transformerHumSource.loop = true;
                transformerHumSource.spatialBlend = 1.0f;
                transformerHumSource.minDistance = 3f;
                transformerHumSource.maxDistance = 65f;
                transformerHumSource.volume = 0.65f;
            }
        }

        public void DiscoverEasternSectorLights()
        {
            if (managedEasternLights.Count > 0) return;

            string[] easternSectors = new string[] { "Sector_1_0_East", "Sector_1_1_NorthEast", "Sector_1_-1_SouthEast" };
            for (int i = 0; i < easternSectors.Length; i++)
            {
                GameObject sectorObj = GameObject.Find(easternSectors[i]);
                if (sectorObj != null)
                {
                    Light[] lights = sectorObj.GetComponentsInChildren<Light>(true);
                    for (int l = 0; l < lights.Length; l++)
                    {
                        if (!managedEasternLights.Contains(lights[l]))
                        {
                            managedEasternLights.Add(lights[l]);
                        }
                    }
                }
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!isPowered)
            {
                bool hasResources = InventorySystem.Instance == null ||
                    (InventorySystem.Instance.HasItem("item_alcohol", 2) && InventorySystem.Instance.HasItem("item_scrap", 2));

                verbs.Add(new InteractionVerb(
                    "repair_and_energize",
                    hasResources ? "ŞEBEKEYİ ONAR VE ÇALIŞTIR" : "EKSİK MALZEME (2x Yakıt + 2x Hurda)",
                    3.0f,
                    "item_alcohol",
                    "2x Yakıt + 2x Hurda",
                    60f
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb("grid_status", "ŞEBEKE TAM KAPASİTE ÇALIŞIYOR", 0.5f));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (verbId == "repair_and_energize")
            {
                if (isPowered) return false;
                if (InventorySystem.Instance == null) return true;
                return InventorySystem.Instance.HasItem("item_alcohol", 2) &&
                       InventorySystem.Instance.HasItem("item_scrap", 2);
            }
            return base.CanInteract(verbId);
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "repair_and_energize")
            {
                ExecuteRepairAndEnergize();
            }
        }

        private void ExecuteRepairAndEnergize()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.RemoveItem("item_alcohol", 2);
                InventorySystem.Instance.RemoveItem("item_scrap", 2);
            }

            isPowered = true;
            ApplyPowerState(true, true);

            // Audio & Screen Feedback
            ProceduralAudioManager.Instance?.PlayAirRaidSiren();
            GameEvents.TriggerCameraShakeRequested(0.45f, 0.35f);

            // Tactical Trade-off: 60m horde alert attracts nearby industrial zombies
            GameEvents.TriggerNoiseEmitted(transform.position, hordeAlertRadius);
            GameEvents.TriggerHordeAlertTriggered(transform.position, hordeAlertRadius);

            GameEvents.TriggerObjectiveToastRequested("BÖLGESEL ELEKTRİK ŞEBEKESİ AKTİF EDİLDİ! (DOĞU ŞEHRİ IŞIKLANDIRILDI)", 3.5f);

            if (humNoiseRoutine != null) StopCoroutine(humNoiseRoutine);
            humNoiseRoutine = StartCoroutine(SubstationNoiseLoop());
        }

        private void ApplyPowerState(bool powered, bool playAudio)
        {
            DiscoverEasternSectorLights();

            for (int i = 0; i < managedEasternLights.Count; i++)
            {
                if (managedEasternLights[i] != null)
                {
                    managedEasternLights[i].enabled = powered;
                }
            }

            if (transformerHumSource != null)
            {
                if (powered)
                {
                    if (playAudio && !transformerHumSource.isPlaying) transformerHumSource.Play();
                }
                else
                {
                    transformerHumSource.Stop();
                }
            }
        }

        private IEnumerator SubstationNoiseLoop()
        {
            while (isPowered)
            {
                GameEvents.TriggerNoiseEmitted(transform.position, 28f);
                yield return new WaitForSeconds(3.0f);
            }
        }

        public void SetPoweredStateSilent(bool powered)
        {
            isPowered = powered;
            ApplyPowerState(powered, false);
            if (powered)
            {
                if (humNoiseRoutine != null) StopCoroutine(humNoiseRoutine);
                humNoiseRoutine = StartCoroutine(SubstationNoiseLoop());
            }
            else
            {
                if (humNoiseRoutine != null)
                {
                    StopCoroutine(humNoiseRoutine);
                    humNoiseRoutine = null;
                }
            }
        }

        public override string GetSerializedState()
        {
            return isPowered ? "1" : "0";
        }

        public override void LoadSerializedState(string state)
        {
            isPowered = state == "1";
            ApplyPowerState(isPowered, false);
        }
    }
}
