using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.UI;

namespace ProjectSurvival.OpenWorld
{
    public enum CaravanState
    {
        Idle,
        Preparing,
        EnRoute,
        Ambushed,
        Completed
    }

    [Serializable]
    public class CaravanConvoy
    {
        public string caravanId;
        public string destinationOutpostId;
        public string destinationName;
        public Vector3 currentPosition;
        public Vector3 destinationPosition;
        public CaravanState state = CaravanState.Idle;
        public float travelProgress = 0f; // 0 to 1
        public float travelDuration = 90f; // 90 seconds
        public bool hasTriggeredAmbush = false;
    }

    /// <summary>
    /// Phase 4 Decoupled Inter-Sector Caravan Director.
    /// Coordinates autonomous 2-person armed supply convoys between SafeHouse and liberated outposts,
    /// live tactical map tracking, highway ambush radio distress events, and community stash delivery.
    /// </summary>
    public class InterSectorCaravanDirector : MonoBehaviour
    {
        public static InterSectorCaravanDirector Instance { get; private set; }

        [Header("Caravan Dispatch Config")]
        [SerializeField] private float dispatchIntervalSeconds = 180f; // Every 3 minutes
        [SerializeField] private Vector3 safeHouseOrigin = new Vector3(-35f, 0.8f, -30f);
        [SerializeField] private List<CaravanConvoy> activeCaravans = new List<CaravanConvoy>();

        private float nextDispatchTime = 0f;

        public IReadOnlyList<CaravanConvoy> ActiveCaravans => activeCaravans;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            nextDispatchTime = Time.time + 60f; // First dispatch 1 min into game
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            UpdateActiveCaravans();

            if (Time.time >= nextDispatchTime)
            {
                nextDispatchTime = Time.time + dispatchIntervalSeconds;
                TryAutoDispatchCaravan();
            }
        }

        public void TryAutoDispatchCaravan()
        {
            if (OutpostLiberationManager.Instance == null) return;

            List<string> liberated = OutpostLiberationManager.Instance.GetLiberatedOutpostIds();
            if (liberated == null || liberated.Count == 0) return;

            // Pick a random liberated outpost
            string targetId = liberated[UnityEngine.Random.Range(0, liberated.Count)];
            OutpostNode outpost = OutpostLiberationManager.Instance.FindOutpost(targetId);
            if (outpost != null)
            {
                DispatchCaravan(outpost);
            }
        }

        public void DispatchCaravan(OutpostNode targetOutpost)
        {
            if (targetOutpost == null) return;

            CaravanConvoy convoy = new CaravanConvoy
            {
                caravanId = $"caravan_{Guid.NewGuid().ToString().Substring(0, 6)}",
                destinationOutpostId = targetOutpost.outpostId,
                destinationName = targetOutpost.displayName,
                currentPosition = safeHouseOrigin,
                destinationPosition = targetOutpost.worldPosition,
                state = CaravanState.EnRoute,
                travelProgress = 0f,
                travelDuration = Mathf.Clamp(Vector3.Distance(safeHouseOrigin, targetOutpost.worldPosition) * 0.4f, 60f, 150f),
                hasTriggeredAmbush = false
            };

            activeCaravans.Add(convoy);

            GameEvents.TriggerObjectiveToastRequested($"[İKMAL KONVOYU] 2 muhafız {targetOutpost.displayName} için yola çıktı!", 4.0f);
            ProceduralAudioManager.Instance?.PlayItemPickup();
            Debug.Log($"[InterSectorCaravanDirector] Dispatched convoy {convoy.caravanId} to {targetOutpost.displayName}.");
        }

        private void UpdateActiveCaravans()
        {
            for (int i = activeCaravans.Count - 1; i >= 0; i--)
            {
                var c = activeCaravans[i];
                if (c.state == CaravanState.EnRoute)
                {
                    c.travelProgress += Time.deltaTime / c.travelDuration;
                    c.currentPosition = Vector3.Lerp(safeHouseOrigin, c.destinationPosition, c.travelProgress);

                    // Chance of highway ambush radio distress event around 50% travel
                    if (!c.hasTriggeredAmbush && c.travelProgress >= 0.45f && c.travelProgress <= 0.60f)
                    {
                        if (UnityEngine.Random.value < 0.40f) // 40% chance of distress
                        {
                            TriggerCaravanAmbush(c);
                        }
                        c.hasTriggeredAmbush = true;
                    }

                    if (c.travelProgress >= 1.0f)
                    {
                        CompleteCaravan(c);
                        activeCaravans.RemoveAt(i);
                    }
                }
            }
        }

        private void TriggerCaravanAmbush(CaravanConvoy c)
        {
            c.state = CaravanState.Ambushed;
            GameEvents.TriggerObjectiveToastRequested($"[TELSİZ ACİL DURUM] İkmal Konvoyu {c.destinationName} yolunda pusuya düştü!", 5.5f);
            ProceduralAudioManager.Instance?.PlayAirRaidSiren();
            GameEvents.TriggerNoiseEmitted(c.currentPosition, 40f);
            GameEvents.TriggerHordeAlertTriggered(c.currentPosition, 40f);

            // Resume after 25 seconds of standoff
            StartCoroutine(ResumeAmbushedCaravan(c, 25f));
        }

        private IEnumerator ResumeAmbushedCaravan(CaravanConvoy c, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (c != null && c.state == CaravanState.Ambushed)
            {
                c.state = CaravanState.EnRoute;
                GameEvents.TriggerObjectiveToastRequested($"[TELSİZ] İkmal konvoyu pusu bölgesini aştı, ilerlemeye devam ediyor.", 3.5f);
            }
        }

        private void CompleteCaravan(CaravanConvoy c)
        {
            c.state = CaravanState.Completed;

            // Deliver supplies to safe house stash
            StashContainer stash = StashContainer.Instance;
            if (stash != null)
            {
                ItemData food = InventorySystem.FindItemData("item_canned_food");
                ItemData water = InventorySystem.FindItemData("item_water_bottle");
                ItemData med = InventorySystem.FindItemData("item_medkit");
                ItemData scrap = InventorySystem.FindItemData("item_scrap");
                ItemData ammo = InventorySystem.FindItemData("ammo_9mm");

                if (food != null) stash.StoreItem(food, UnityEngine.Random.Range(2, 4));
                if (water != null) stash.StoreItem(water, UnityEngine.Random.Range(2, 4));
                if (med != null) stash.StoreItem(med, 1);
                if (scrap != null) stash.StoreItem(scrap, UnityEngine.Random.Range(3, 6));
                if (ammo != null) stash.StoreItem(ammo, UnityEngine.Random.Range(10, 20));
            }

            if (CommunityOutpostManager.Instance != null)
            {
                CommunityOutpostManager.Instance.ModifyMorale(8f);
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerObjectiveToastRequested($"[İKMAL TESLİM EDİLDİ] {c.destinationName} konvoyu üsse döndü! Sığınak Deposu yenilendi (+%8 Moral).", 5.0f);
            Debug.Log($"[InterSectorCaravanDirector] Convoy {c.caravanId} completed trip to {c.destinationName}.");
        }
    }
}
