using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive Power Grid & Generator Archetype.
    /// Placed inside SafeHouse, Zone_Clinic, Police_Station, Supermarket, and Gas Station.
    /// Manages:
    /// 1. "power_on": Adds fuel/alcohol (2.5s), starts generator with engine rumble audio,
    ///    illuminates building interior lights, and unlocks electronic security doors/lockers.
    /// 2. "power_off": Shuts down generator to silence the 22m engine noise when stealth is required.
    /// </summary>
    public class PowerGridInteractable : WorldInteractableObject
    {
        [Header("Power Grid Configuration")]
        [SerializeField] private string facilityName = "Bina Elektrik Panosu";
        [SerializeField] private List<Light> connectedInteriorLights = new List<Light>();
        [SerializeField] private List<GameObject> electronicDoorsOrLockers = new List<GameObject>();
        [SerializeField] private AudioSource generatorAudioSource;

        private Coroutine noiseRoutine;

        public override string ObjectSubtitle => isPowered ? "GÜÇ AKTİF (JENERATÖR ÇALIŞIYOR)" : "GÜÇ KESİK (YAKIT GEREKLİ)";

        protected override void Awake()
        {
            base.Awake();
            DetectFacilityName();
            FindConnectedInteriorLights();
            SetupAudioSource();
        }

        private void DetectFacilityName()
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("clinic")) facilityName = "Klinik Jeneratörü";
            else if (n.Contains("police")) facilityName = "Karakol Güç Ünitesi";
            else if (n.Contains("supermarket") || n.Contains("market")) facilityName = "Market Ana Şebekesi";
            else if (n.Contains("gas")) facilityName = "Benzinlik Jeneratörü";
            else if (n.Contains("safehouse")) facilityName = "Sığınak Güç Panosu";
            else if (n.Contains("mall") || n.Contains("avm")) facilityName = "AVM Yedek Güç Jeneratörü";
            else facilityName = "Sokak Jeneratörü";

            objectTitle = facilityName;
        }

        private void FindConnectedInteriorLights()
        {
            if (connectedInteriorLights.Count > 0) return;

            // Search lights within 22m radius in the same building
            Collider[] hits = Physics.OverlapSphere(transform.position, 22f);
            for (int i = 0; i < hits.Length; i++)
            {
                Light[] lights = hits[i].GetComponentsInChildren<Light>();
                for (int l = 0; l < lights.Length; l++)
                {
                    if (lights[l].type == LightType.Point || lights[l].type == LightType.Spot)
                    {
                        if (!connectedInteriorLights.Contains(lights[l]))
                        {
                            connectedInteriorLights.Add(lights[l]);
                            lights[l].enabled = isPowered;
                        }
                    }
                }
            }
        }

        private void SetupAudioSource()
        {
            if (generatorAudioSource == null)
            {
                generatorAudioSource = gameObject.AddComponent<AudioSource>();
                generatorAudioSource.playOnAwake = false;
                generatorAudioSource.loop = true;
                generatorAudioSource.spatialBlend = 1.0f;
                generatorAudioSource.minDistance = 2f;
                generatorAudioSource.maxDistance = 25f;
                generatorAudioSource.volume = 0.5f;
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!isPowered)
            {
                verbs.Add(new InteractionVerb("power_on", "YAKIT KOY VE ÇALIŞTIR", 2.5f, "item_alcohol", "Alkol / Yakıt", 22f));
            }
            else
            {
                verbs.Add(new InteractionVerb("power_off", "JENERATÖRÜ KAPAT (SESSİZ MOD)", 0.8f));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "power_on":
                    ExecutePowerOn();
                    break;
                case "power_off":
                    ExecutePowerOff();
                    break;
            }
        }

        private void ExecutePowerOn()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.RemoveItem("item_alcohol", 1);
            }

            isPowered = true;
            ApplyPowerState(true);

            if (noiseRoutine != null) StopCoroutine(noiseRoutine);
            noiseRoutine = StartCoroutine(GeneratorNoiseLoop());

            ProceduralAudioManager.Instance?.PlayAirRaidSiren();
            GameEvents.TriggerCameraShakeRequested(0.25f, 0.2f);
            GameEvents.TriggerObjectiveToastRequested($"{objectTitle.ToUpper()} AKTİF! (IŞIKLAR AÇILDI)", 3.0f);
        }

        private void ExecutePowerOff()
        {
            isPowered = false;
            ApplyPowerState(false);

            if (noiseRoutine != null)
            {
                StopCoroutine(noiseRoutine);
                noiseRoutine = null;
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
            GameEvents.TriggerObjectiveToastRequested($"{objectTitle.ToUpper()} KAPATILDI (SESSİZ)", 2.0f);
        }

        private void ApplyPowerState(bool powered)
        {
            for (int i = 0; i < connectedInteriorLights.Count; i++)
            {
                if (connectedInteriorLights[i] != null)
                {
                    connectedInteriorLights[i].enabled = powered;
                }
            }

            for (int i = 0; i < electronicDoorsOrLockers.Count; i++)
            {
                if (electronicDoorsOrLockers[i] != null)
                {
                    electronicDoorsOrLockers[i].SetActive(true);
                }
            }

            if (generatorAudioSource != null)
            {
                if (powered) generatorAudioSource.Play();
                else generatorAudioSource.Stop();
            }
        }

        private IEnumerator GeneratorNoiseLoop()
        {
            while (isPowered)
            {
                // Emits continuous 22m engine drone noise to nearby zombies
                GameEvents.TriggerNoiseEmitted(transform.position, 22f);
                yield return new WaitForSeconds(2.0f);
            }
        }
    }
}
