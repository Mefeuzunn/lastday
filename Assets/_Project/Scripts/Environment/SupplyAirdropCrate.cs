using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Phase 2 (Step 2B): Reinforced Military Supply Airdrop Crate.
    /// Descends from altitude, detaches parachute canopy upon touchdown,
    /// ignites an emerald-green signal flare (URP Unlit smoke + Shadows: None point light + 3D hiss audio),
    /// broadcasts acoustic attraction to nearby zombies and raiders, and grants high-tier supplies.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SupplyAirdropCrate : WorldInteractableObject
    {
        [Header("Airdrop Identification")]
        [SerializeField] private string airdropId = "Airdrop_Crate_1";

        [Header("Signal Flare & Parachute References")]
        [SerializeField] private GameObject parachuteCanopy;
        [SerializeField] private ParticleSystem flareParticleSystem;
        [SerializeField] private Light flareLight;
        [SerializeField] private AudioSource flareAudioSource;

        [Header("State")]
        [SerializeField] private bool hasLanded = false;

        public string AirdropId => airdropId;
        public bool HasLanded => hasLanded;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Askeri Yardım Sandığı";
            objectSubtitle = "Paraşütlü Hava İkmali";

            EnsureFlareComponents();
        }

        public void SetAirdropId(string id)
        {
            airdropId = id;
            SetPersistentId(id);
        }

        public void InitializeComponents(GameObject canopy, ParticleSystem flarePS, Light light = null, AudioSource audio = null)
        {
            parachuteCanopy = canopy;
            flareParticleSystem = flarePS;
            if (light != null) flareLight = light;
            if (audio != null) flareAudioSource = audio;
        }

        public void EnsureFlareComponents()
        {
            if (flareLight == null)
            {
                Transform fl = transform.Find("Flare_Light");
                if (fl != null) flareLight = fl.GetComponent<Light>();
                else
                {
                    GameObject flObj = new GameObject("Flare_Light");
                    flObj.transform.SetParent(transform, false);
                    flObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    flareLight = flObj.AddComponent<Light>();
                    flareLight.type = LightType.Point;
                    flareLight.color = new Color(0.18f, 0.85f, 0.44f); // Emerald green
                    flareLight.range = 9.0f;
                    flareLight.intensity = 2.5f;
                    flareLight.shadows = LightShadows.None; // Strict mobile 1-shadow budget
                }
            }

            if (flareAudioSource == null)
            {
                flareAudioSource = GetComponent<AudioSource>();
                if (flareAudioSource == null) flareAudioSource = gameObject.AddComponent<AudioSource>();

                flareAudioSource.spatialBlend = 1.0f; // 3D positional audio
                flareAudioSource.minDistance = 2.0f;
                flareAudioSource.maxDistance = 35.0f;
                flareAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                flareAudioSource.loop = true;
                flareAudioSource.playOnAwake = false;

                if (ProceduralAudioManager.Instance != null)
                {
                    flareAudioSource.clip = ProceduralAudioManager.Instance.GenerateSignalFlareHissClip();
                }
            }
        }

        /// <summary>
        /// Called when the crate touches down on the NavMesh terrain.
        /// </summary>
        public void OnTouchdown()
        {
            hasLanded = true;

            // 1. Detach or fold parachute canopy
            if (parachuteCanopy != null)
            {
                parachuteCanopy.transform.SetParent(null, true);
                // Collapse slightly or detach
                parachuteCanopy.transform.position += Vector3.forward * 1.5f + Vector3.down * 0.4f;
                parachuteCanopy.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);
            }

            // 2. Ignite signal flare
            if (flareLight != null) flareLight.enabled = true;
            if (flareParticleSystem != null) flareParticleSystem.Play();
            if (flareAudioSource != null && !flareAudioSource.isPlaying) flareAudioSource.Play();

            // 3. Register markers on Minimap and Full Tactical Map
            Vector2 mapPos = new Vector2(transform.position.x, transform.position.z);
            if (MinimapHUDWidget.Instance != null)
            {
                MinimapHUDWidget.Instance.RegisterDynamicPoi(airdropId, mapPos, new Color(0.18f, 0.85f, 0.44f, 0.95f));
            }
            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.RegisterAirdropMarker(airdropId, "Askeri Hava İkmali", mapPos);
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "loot",
                    "ASKERİ YARDIM SANDIĞI",
                    1.2f,
                    null,
                    null,
                    15.0f,
                    true
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "looted",
                    "İKMAL SANDIĞI BOŞALTILDI",
                    0.0f,
                    null,
                    null,
                    0.0f,
                    false
                ));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId != "loot" || hasBeenSearched) return;

            hasBeenSearched = true;

            // 1. Deliver High-Tier Combat Supplies
            if (InventorySystem.Instance != null)
            {
                // 18x 9mm Ammo (or 10x Shotgun Shells)
                bool isShotgunAmmo = Random.value > 0.5f;
                if (isShotgunAmmo)
                {
                    InventorySystem.Instance.AddItemById("ammo_shells", 10);
                }
                else
                {
                    InventorySystem.Instance.AddItemById("ammo_9mm", 18);
                }

                // 1x Antiviral Serum
                InventorySystem.Instance.AddItemById("item_antiviral", 1);

                // 1x Adrenaline Shot
                InventorySystem.Instance.AddItemById("item_adrenaline", 1);

                // 2x Canned Food
                InventorySystem.Instance.AddItemById("item_canned_food", 2);

                // 4x Scrap Metal
                InventorySystem.Instance.AddItemById("item_scrap", 4);
            }

            // 2. Award +75 Progression XP
            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.AddXP(75, "Askeri Hava İkmali Ele Geçirildi");
            }

            // 3. Audio & Haptic Feedback
            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerHeavy();

            // 4. Toast Notification
            GameEvents.TriggerObjectiveToastRequested("Askeri Hava İkmali Ele Geçirildi! (+75 XP & Yüksek Seviye Mühimmat)", 3.8f);

            // 5. Extinguish Signal Flare & Remove Map Markers
            if (flareLight != null) flareLight.enabled = false;
            if (flareParticleSystem != null) flareParticleSystem.Stop();
            if (flareAudioSource != null && flareAudioSource.isPlaying) flareAudioSource.Stop();

            if (MinimapHUDWidget.Instance != null)
            {
                MinimapHUDWidget.Instance.UnregisterDynamicPoi(airdropId);
            }
            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.UnregisterLandmarkMarker(airdropId);
            }

            // 6. Reset Visual Rim Highlight
            SetFocused(false);
        }

        private void OnDestroy()
        {
            if (MinimapHUDWidget.Instance != null)
            {
                MinimapHUDWidget.Instance.UnregisterDynamicPoi(airdropId);
            }
            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.UnregisterLandmarkMarker(airdropId);
            }
        }
    }
}
