using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Narrative
{
    /// <summary>
    /// Step B: Furnished Starter House Bedroom Wake-Up Sequence.
    /// Manages the immersive opening sequence when starting a new game in the bedroom:
    /// - Guarantees unarmed player spawn (0 weapons equipped).
    /// - Sets calibrated survival stats: 100% Health, 85% Hunger, 80% Thirst, 0% Infection, 0 injuries.
    /// - Smooth 2.2s eyelid vignette fade transition with rustle audio and haptic feedback.
    /// - Orients camera toward the nightstand and prompts: "Başucundaki telefona bak".
    /// - Seamlessly transitions into Smartphone emergency broadcast.
    /// </summary>
    public class ApartmentWakeUpSequence : MonoBehaviour
    {
        public static ApartmentWakeUpSequence Instance { get; private set; }

        [Header("Sequence State")]
        [SerializeField] private bool hasCheckedIntroPhone = false;
        [SerializeField] private bool isSequenceRunning = false;
        [SerializeField] private float sequenceDuration = 1.2f;

        [Header("Target Look Orientation")]
        [SerializeField] private Vector3 nightstandPosition = new Vector3(-1.8f, 0.65f, -1.8f);

        [Header("UI Overlay Elements")]
        [SerializeField] private Canvas eyelidCanvas;
        [SerializeField] private Image eyelidImage;

        public bool HasCheckedPhone => hasCheckedIntroPhone;
        public bool IsSequenceRunning => isSequenceRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // If already checked or completed, skip sequence
            if (hasCheckedIntroPhone)
            {
                Debug.Log("[ApartmentWakeUpSequence] Intro phone already checked. Skipping wake-up sequence.");
                return;
            }

            StartCoroutine(WakeUpRoutine());
        }

        public void StartWakeUpManually()
        {
            StopAllCoroutines();
            StartCoroutine(WakeUpRoutine());
        }

        private IEnumerator WakeUpRoutine()
        {
            isSequenceRunning = true;

            // Wait until main menu transition completes and gameplay begins
            while ((GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu) ||
                   (MainMenuController.Instance != null && MainMenuController.Instance.IsMenuShowing) ||
                   (CinematicMenuCamera.Instance != null && CinematicMenuCamera.Instance.IsSwooping))
            {
                yield return null;
            }

            // 1. Force Unarmed Spawn & Stats Calibration
            CalibrateUnarmedPlayer();

            // 2. Setup Eyelid Canvas Overlay
            SetupEyelidOverlay();

            if (eyelidImage != null)
            {
                eyelidImage.color = new Color(0.01f, 0.01f, 0.02f, 1f); // Pure black eyelid
            }

            // Audio & Haptic Cue (Subtle waking rustle)
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();

            // 3. Non-intrusive Eyelid Vignette Fade (1.2s unscaled time, alpha 1.0 -> 0.0, zero camera override)
            float elapsed = 0f;
            float duration = sequenceDuration;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(1.0f - (elapsed / duration));
                if (eyelidImage != null)
                {
                    eyelidImage.color = new Color(0.01f, 0.01f, 0.02f, alpha);
                }
                yield return null;
            }

            if (eyelidImage != null)
            {
                eyelidImage.color = Color.clear;
            }

            if (eyelidCanvas != null)
            {
                eyelidCanvas.gameObject.SetActive(false);
            }

            // 4. HUD Prompt: "Başucundaki akıllı telefona bak"
            GameEvents.TriggerObjectiveToastRequested("Başucundaki akıllı telefona bak", 3.5f);

            isSequenceRunning = false;
            Debug.Log("[ApartmentWakeUpSequence] Bedroom wake-up sequence completed without camera override.");
        }

        public void CalibrateUnarmedPlayer()
        {
            // 1. Force Unarmed (WeaponType.None)
            if (WeaponSystem.Instance != null)
            {
                WeaponSystem.Instance.EquipWeapon(WeaponType.None);
            }

            // 2. Set Calibrated Survival Stats: 100 HP, 85 Hunger, 80 Thirst, 0 Infection
            SurvivalStats stats = FindFirstObjectByType<SurvivalStats>();
            if (stats != null)
            {
                stats.SetStats(100f, 85f, 80f, 0f);
            }

            // 3. Clear All Traumas & Injuries (0 Bleeding, 0 Fractures, 0 Hypothermia)
            if (TraumaAndInjurySystem.Instance != null)
            {
                TraumaAndInjurySystem.Instance.ClearAllInjuries();
            }

            Debug.Log("[ApartmentWakeUpSequence] Player calibrated: Unarmed (0 weapons), 100 HP, 85 Hunger, 80 Thirst, 0 Infection, 0 Injuries.");
        }

        private void OrientCameraTowardNightstand()
        {
            GameObject player = GameObject.FindWithTag("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            Vector3 dirToPhone = (nightstandPosition - playerPos).normalized;
            if (dirToPhone.sqrMagnitude > 0.01f)
            {
                float targetYaw = Mathf.Atan2(dirToPhone.x, dirToPhone.z) * Mathf.Rad2Deg;
                ThirdPersonCamera.Instance?.SetRotation(targetYaw, 14f);
            }
        }

        private void SetupEyelidOverlay()
        {
            if (eyelidCanvas != null && eyelidImage != null)
            {
                eyelidCanvas.gameObject.SetActive(true);
                return;
            }

            GameObject canvasObj = new GameObject("Eyelid_Vignette_Canvas");
            canvasObj.transform.SetParent(transform, false);

            eyelidCanvas = canvasObj.AddComponent<Canvas>();
            eyelidCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            eyelidCanvas.sortingOrder = 999; // Ensure on top of HUD during wake-up

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject imgObj = new GameObject("Eyelid_Overlay");
            imgObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = imgObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            eyelidImage = imgObj.AddComponent<Image>();
            eyelidImage.color = new Color(0.01f, 0.01f, 0.02f, 1f);
            eyelidImage.raycastTarget = false;
        }

        public void MarkPhoneChecked()
        {
            if (hasCheckedIntroPhone) return;

            hasCheckedIntroPhone = true;
            Debug.Log("[ApartmentWakeUpSequence] Intro smartphone marked as checked.");

            if (StarterPrologueDirector.Instance != null)
            {
                StarterPrologueDirector.Instance.OnSmartphoneClosed();
            }
            else
            {
                // Fallback direct update to QuestTrackerHUD
                QuestTrackerHUD.Instance?.RefreshObjectiveDisplay(
                    new StoryObjective
                    {
                        id = "prologue_2_scavenge",
                        chapter = 0,
                        chapterTitle = "BÖLÜM 0: İLK ERZAK",
                        title = "Mutfak dolabı ve buzdolabından yiyecek ve su bul (0/2)",
                        description = "Mutfak dolaplarından veya buzdolabından en az 1 Konserve ve 1 Su şişesi topla.",
                        targetPosition = new Vector3(-2.2f, 0.85f, 2.5f),
                        xpReward = 25
                    }
                );
            }

            // Persist save state
            GameManager.Instance?.SaveGame();
        }

        public void RestorePhoneChecked(bool checkedState)
        {
            hasCheckedIntroPhone = checkedState;
            Debug.Log($"[ApartmentWakeUpSequence] Restored phone checked state: {hasCheckedIntroPhone}");
        }
    }
}
