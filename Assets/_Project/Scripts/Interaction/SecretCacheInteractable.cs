using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;
using ProjectSurvival.Progression;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Active Phase 5: Radio-Revealed Secret Supply Cache Crate.
    /// Spawns in world sectors, activates when corresponding radio frequency is decoded,
    /// and grants high-tier survival supplies (ammo, antivirals, medkits, scrap) upon looting.
    /// </summary>
    public class SecretCacheInteractable : WorldInteractableObject
    {
        [Header("Cache Configuration")]
        [SerializeField] private string cacheId = "cache_west_forest";
        [SerializeField] private bool isRevealed = false;
        [SerializeField] private List<string> rewardItemIds = new List<string>();
        [SerializeField] private List<int> rewardItemCounts = new List<int>();

        [Header("Beacon Light")]
        [SerializeField] private Light beaconLight;
        [SerializeField] private Color beaconColor = new Color(0.2f, 0.98f, 0.5f, 1f);

        public string CacheId => cacheId;
        public bool IsRevealed => isRevealed;

        protected override void Awake()
        {
            objectTitle = "GİZLİ TELSİZ ZULASI";
            objectSubtitle = isRevealed ? "ŞİFRE ÇÖZÜLDÜ (AÇILABİLİR)" : "FREKANS KİLİTLİ";
            base.Awake();

            EnsureBeacon();
        }

        private void EnsureBeacon()
        {
            if (beaconLight == null)
            {
                beaconLight = GetComponentInChildren<Light>();
                if (beaconLight == null)
                {
                    GameObject lObj = new GameObject("Beacon_Light");
                    lObj.transform.SetParent(transform, false);
                    lObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    beaconLight = lObj.AddComponent<Light>();
                    beaconLight.type = LightType.Point;
                    beaconLight.range = 3.5f;
                    beaconLight.color = beaconColor;
                    beaconLight.intensity = isRevealed && !hasBeenSearched ? 1.0f : 0f;
                }
            }
        }

        private void Update()
        {
            if (beaconLight != null && isRevealed && !hasBeenSearched)
            {
                beaconLight.intensity = Mathf.Sin(Time.time * 3f) * 0.4f + 0.8f;
            }
        }

        public void RevealCache()
        {
            isRevealed = true;
            objectSubtitle = "ŞİFRE ÇÖZÜLDÜ (AÇILABİLİR)";
            if (beaconLight != null && !hasBeenSearched)
            {
                beaconLight.intensity = 1.0f;
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (hasBeenSearched)
            {
                verbs.Add(new InteractionVerb("looted", "ZULA BOŞALTILDI", 0f, null, null, 0f, false));
                return verbs;
            }

            if (!isRevealed)
            {
                verbs.Add(new InteractionVerb("locked", "TELSİZDEN ŞİFRE GEREKİYOR", 0f, null, null, 0f, false));
                return verbs;
            }

            verbs.Add(new InteractionVerb("loot_cache", "GİZLİ ZULAYI AÇ VE YAĞMALA", 1.0f, null, null, 12f, true));
            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "loot_cache" && isRevealed && !hasBeenSearched)
            {
                hasBeenSearched = true;
                objectSubtitle = "BOŞALTILDI";
                if (beaconLight != null) beaconLight.intensity = 0f;

                ProceduralAudioManager.Instance?.PlayVaultUnlock();
                MobileHapticManager.TriggerHeavy();

                // Transfer rewards
                for (int i = 0; i < rewardItemIds.Count; i++)
                {
                    string id = rewardItemIds[i];
                    int count = i < rewardItemCounts.Count ? rewardItemCounts[i] : 1;
                    InventoryManager.AddItem(id, count);
                }

                if (SurvivorProgressionManager.Instance != null)
                {
                    SurvivorProgressionManager.Instance.AddXP(50, $"Gizli Zula Açıldı: {objectTitle}");
                }

                // Remove marker
                FullTacticalMapUI.Instance?.UnregisterSecretCacheMarker(cacheId);
                MinimapHUDWidget.Instance?.UnregisterDynamicPoi(cacheId);

                Debug.Log($"[SecretCacheInteractable] Looted cache: {cacheId}");
            }
        }

        public void ConfigureCache(string id, string title, List<string> items, List<int> counts)
        {
            cacheId = id;
            objectTitle = title;
            rewardItemIds = items ?? new List<string>();
            rewardItemCounts = counts ?? new List<int>();
        }

        public override string GetSerializedState()
        {
            return $"{hasBeenSearched}|{isRevealed}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            string[] p = state.Split('|');
            if (p.Length >= 2)
            {
                bool.TryParse(p[0], out hasBeenSearched);
                bool.TryParse(p[1], out isRevealed);
                if (isRevealed && !hasBeenSearched) RevealCache();
            }
        }
    }
}
