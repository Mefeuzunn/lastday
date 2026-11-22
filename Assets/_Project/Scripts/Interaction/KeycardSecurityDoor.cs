using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Motorized Heavy Blast Security Door guarding Chamber 4 (Bio-Containment Lab).
    /// Requires both Generator Power ON and Level-2 Security Keycard (item_bunker_keycard).
    /// Slides open with hydraulic 3D audio (spatialBlend = 1.0) and unblocks NavMesh pathing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KeycardSecurityDoor : WorldInteractableObject
    {
        [Header("Security Door Properties")]
        [SerializeField] private bool isDoorUnlocked = false;
        [SerializeField] private bool isDoorOpen = false;
        [SerializeField] private Transform doorLeafTransform;
        [SerializeField] private AudioSource doorAudioSource;
        [SerializeField] private Vector3 openOffset = new Vector3(-2.8f, 0f, 0f);

        private Vector3 closedLocalPos;
        private AudioClip cachedSlideClip;
        private NavMeshObstacle navObstacle;

        public bool IsDoorUnlocked => isDoorUnlocked;
        public bool IsDoorOpen => isDoorOpen;

        public override string ObjectSubtitle
        {
            get
            {
                if (isDoorOpen) return "Durum: Açık (Erişim Serbest)";
                bool isPowerOn = UndergroundBunkerDirector.Instance != null && UndergroundBunkerDirector.Instance.IsGeneratorPowered;
                if (!isPowerOn) return "Durum: Kilitli (Güç Yok • Jeneratör Gerekli)";
                return "Durum: Kilitli (Seviye-2 Güvenlik Kartı Gerekli)";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Biyolojik Laboratuvar Blast Kapısı";
            objectSubtitle = isDoorOpen ? "Durum: Açık" : "Durum: Kilitli (Manyetik Kilit)";

            if (doorLeafTransform != null)
            {
                closedLocalPos = doorLeafTransform.localPosition;
            }

            // Configure 3D Positional AudioSource
            if (doorAudioSource == null) doorAudioSource = GetComponent<AudioSource>();
            if (doorAudioSource == null) doorAudioSource = gameObject.AddComponent<AudioSource>();

            doorAudioSource.spatialBlend = 1.0f; // Pure 3D positional
            doorAudioSource.minDistance = 2.0f;
            doorAudioSource.maxDistance = 22.0f;
            doorAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            doorAudioSource.playOnAwake = false;

            // NavMeshObstacle with carving
            navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            navObstacle.carving = !isDoorOpen;
            navObstacle.enabled = !isDoorOpen;
        }

        private void Start()
        {
            if (ProceduralAudioManager.Instance != null)
            {
                cachedSlideClip = ProceduralAudioManager.Instance.GenerateBlastDoorSlideClip();
            }

            if (isDoorOpen && doorLeafTransform != null)
            {
                doorLeafTransform.localPosition = closedLocalPos + openOffset;
                if (navObstacle != null) navObstacle.enabled = false;
            }
        }

        public void SetUnlockedSilent(bool unlocked)
        {
            isDoorUnlocked = unlocked;
            isDoorOpen = unlocked;
            if (doorLeafTransform != null)
            {
                doorLeafTransform.localPosition = isDoorOpen ? (closedLocalPos + openOffset) : closedLocalPos;
            }
            if (navObstacle != null)
            {
                navObstacle.enabled = !isDoorOpen;
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isDoorOpen)
            {
                verbs.Add(new InteractionVerb(
                    "open_blast_door",
                    "GÜVENLİK KAPISI AÇIK",
                    0f,
                    null,
                    null,
                    0f,
                    false,
                    "Kapı zaten açılmış."
                ));
            }
            else
            {
                bool isPowerOn = UndergroundBunkerDirector.Instance != null && UndergroundBunkerDirector.Instance.IsGeneratorPowered;
                bool hasKeycard = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_bunker_keycard", 1);
                bool canOpen = isPowerOn && (hasKeycard || isDoorUnlocked);

                string label;
                string reason = "";

                if (!isPowerOn)
                {
                    label = "GÜVENLİK KARTI İLE AÇ (GÜÇ YOK • Jeneratörü Çalıştır)";
                    reason = "Manyetik kilit panelinde elektrik yok. Önce jeneratörü çalıştırın.";
                }
                else if (!hasKeycard && !isDoorUnlocked)
                {
                    label = "GÜVENLİK KARTI İLE AÇ (Seviye-2 Kart Gerekli)";
                    reason = "Seviye-2 Güvenlik Kartı gereklidir. Arşiv odasındaki masayı arayın.";
                }
                else
                {
                    label = "GÜVENLİK KARTI İLE AÇ [Seviye-2]";
                }

                verbs.Add(new InteractionVerb(
                    "open_blast_door",
                    label,
                    1.2f, // 1.2s timed action
                    "item_bunker_keycard",
                    "Seviye-2 Güvenlik Kartı",
                    12f,
                    canOpen,
                    reason
                ));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (verbId != "open_blast_door") return false;
            if (isDoorOpen) return false;
            bool isPowerOn = UndergroundBunkerDirector.Instance != null && UndergroundBunkerDirector.Instance.IsGeneratorPowered;
            bool hasKeycard = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_bunker_keycard", 1);
            return isPowerOn && (hasKeycard || isDoorUnlocked);
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "open_blast_door" && !isDoorOpen)
            {
                bool isPowerOn = UndergroundBunkerDirector.Instance != null && UndergroundBunkerDirector.Instance.IsGeneratorPowered;
                bool hasKeycard = InventorySystem.Instance != null && InventorySystem.Instance.HasItem("item_bunker_keycard", 1);

                if (isPowerOn && (hasKeycard || isDoorUnlocked))
                {
                    isDoorUnlocked = true;
                    isDoorOpen = true;

                    PlaySlideAudio();
                    MobileHapticManager.TriggerHeavy();
                    SurvivorProgressionManager.Instance?.AddXP(40);

                    // Disable NavMeshObstacle so zombies and player path cleanly through
                    if (navObstacle != null) navObstacle.enabled = false;

                    // Animate door slide
                    StopAllCoroutines();
                    StartCoroutine(AnimateDoorSlide());

                    UndergroundBunkerDirector.Instance?.OnSecurityDoorUnlocked();

                    GameEvents.TriggerObjectiveToastRequested(
                        "[ERİŞİM ONAYLANDI] Biyolojik Deney Laboratuvarı Blast Kapısı Açıldı (+40 XP)", 3.0f);
                }
                else if (!isPowerOn)
                {
                    GameEvents.TriggerObjectiveToastRequested("KAPI ÇALIŞMIYOR! Önce Jeneratör Odasına güç verin.", 2.5f);
                    ProceduralAudioManager.Instance?.PlaySoftError();
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("SEVİYE-2 GÜVENLİK KARTI GEREKLİ!", 2.5f);
                    ProceduralAudioManager.Instance?.PlaySoftError();
                }
            }
        }

        private IEnumerator AnimateDoorSlide()
        {
            if (doorLeafTransform == null) yield break;

            Vector3 startPos = doorLeafTransform.localPosition;
            Vector3 targetPos = closedLocalPos + openOffset;
            float elapsed = 0f;
            float duration = 1.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                doorLeafTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            doorLeafTransform.localPosition = targetPos;
        }

        private void PlaySlideAudio()
        {
            if (doorAudioSource != null)
            {
                if (cachedSlideClip == null && ProceduralAudioManager.Instance != null)
                {
                    cachedSlideClip = ProceduralAudioManager.Instance.GenerateBlastDoorSlideClip();
                }
                if (cachedSlideClip != null)
                {
                    doorAudioSource.PlayOneShot(cachedSlideClip, 0.90f);
                }
            }
            else if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlayBlastDoorSlide(transform.position);
            }
        }

        public override string GetSerializedState()
        {
            return $"{isDoorUnlocked}|{isDoorOpen}";
        }

        public override void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            string[] parts = state.Split('|');
            if (parts.Length >= 1)
            {
                bool.TryParse(parts[0], out isDoorUnlocked);
            }
            if (parts.Length >= 2)
            {
                bool.TryParse(parts[1], out isDoorOpen);
            }
            SetUnlockedSilent(isDoorOpen);
        }
    }
}
