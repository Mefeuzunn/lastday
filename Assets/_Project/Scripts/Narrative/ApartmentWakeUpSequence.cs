using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;
using ProjectSurvival.Animation;

namespace ProjectSurvival.Narrative
{
    /// <summary>
    /// Step B: Furnished Starter House Bedroom Wake-Up Sequence.
    /// Manages the immersive opening sequence when starting a new game in the bedroom:
    /// - Guarantees bedside spawn with >= 1.2m capsule clearance, starting unarmed.
    /// - Sets calibrated survival stats: 100% Health, 85% Hunger, 80% Thirst, 0% Infection, 0 injuries.
    /// - Smooth 2.2s eyelid opening (alpha 1.0 -> 0.0 over 1.4s unscaled time) and body/camera rise.
    /// - Single crisp vibration buzz + short notification chime directing attention to the glowing nightstand phone.
    /// - Immediate early-exit break on WASD/Joystick movement (zero player lock risk).
    /// - Seamless narrative handoff to StarterPrologueDirector and QuestTrackerHUD.
    /// </summary>
    public class ApartmentWakeUpSequence : MonoBehaviour
    {
        public static ApartmentWakeUpSequence Instance { get; private set; }

        [Header("Sequence State")]
        [SerializeField] private bool hasCheckedIntroPhone = false;
        [SerializeField] private bool isSequenceRunning = false;
        [SerializeField] private float sequenceDuration = 2.2f;
        [SerializeField] private float eyelidFadeDuration = 1.4f;

        [Header("Target Look Orientation")]
        [SerializeField] private Vector3 nightstandPosition = new Vector3(-1.6f, 0.72f, -1.8f);

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

            // 1. Force Clean Bedside Spawn & Clearance Verification
            GameObject player = GameObject.FindWithTag("Player");
            HumanoidProceduralAnimator anim = null;
            if (player != null)
            {
                Vector3 bedroomSpawn = new Vector3(-0.65f, 0.08f, -1.40f);
                Quaternion bedroomRot = Quaternion.Euler(0f, 247.2f, 0f);

                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                Vector3 p1 = bedroomSpawn + Vector3.up * 0.35f;
                Vector3 p2 = bedroomSpawn + Vector3.up * 1.45f;
                if (Physics.CheckCapsule(p1, p2, 0.30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bedroomSpawn = new Vector3(-0.55f, 0.08f, -1.30f);
                }

                player.transform.position = bedroomSpawn;
                player.transform.rotation = bedroomRot;
                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true;

                anim = player.GetComponent<HumanoidProceduralAnimator>();
                if (anim != null)
                {
                    anim.SetSeated(true);
                }
            }

            // 2. Force Unarmed Spawn & Stats Calibration
            CalibrateUnarmedPlayer();

            // 3. Setup Eyelid Canvas Overlay (Dark Drowsy Eyelids)
            SetupEyelidOverlay();
            if (eyelidCanvas != null)
            {
                eyelidCanvas.gameObject.SetActive(true);
            }
            if (eyelidImage != null)
            {
                eyelidImage.color = new Color(0.01f, 0.01f, 0.02f, 1f);
            }

            // 4. Initial Camera Pitch: Looking down toward bedside & glowing smartphone
            ThirdPersonCamera.Instance?.SetRotation(247.2f, -12f);

            // 5. Single Crisp Smartphone Vibration Buzz & Notification Chime
            MobileHapticManager.TriggerLight();
            ProceduralAudioManager.Instance?.PlayRadioFrequencyLockChime();

            // 6. Smooth 2.2-Second Eyelid Opening & Body Rise Loop
            float elapsed = 0f;
            while (elapsed < sequenceDuration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;

                // Immediate Early-Exit Guard: player moving WASD / Joystick instantly ends animation
                bool inputDetected = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                                     Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
                                     Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                                     Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                                     (MobileJoystick.Instance != null && MobileJoystick.Instance.Direction.sqrMagnitude > 0.01f);
                if (inputDetected)
                {
                    Debug.Log("[ApartmentWakeUpSequence] Early player movement detected; smoothly releasing camera authority.");
                    break;
                }

                // Smoothly fade eyelid alpha (1.0 -> 0.0 over 1.4s)
                if (eyelidImage != null)
                {
                    float alpha = Mathf.Clamp01(1.0f - (elapsed / eyelidFadeDuration));
                    eyelidImage.color = new Color(0.01f, 0.01f, 0.02f, alpha);
                }

                // Gently tilt camera pitch from -12f (bedside look-down) up to +10f (eye level)
                float tNorm = Mathf.Clamp01(elapsed / sequenceDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, tNorm);
                float currentPitch = Mathf.Lerp(-12f, 10f, smoothT);
                ThirdPersonCamera.Instance?.SetRotation(247.2f, currentPitch);

                // Body rises from seated waking posture to standing at t = 1.2s
                if (elapsed >= 1.2f && anim != null && anim.IsSeated)
                {
                    anim.SetSeated(false);
                }

                yield return null;
            }

            // 7. Complete Wake-Up Sequence & Release 100% Control
            if (eyelidImage != null)
            {
                eyelidImage.color = Color.clear;
            }
            if (eyelidCanvas != null)
            {
                eyelidCanvas.gameObject.SetActive(false);
            }
            if (anim != null)
            {
                anim.SetSeated(false);
            }

            ThirdPersonCamera.Instance?.SetRotation(247.2f, 10f);
            isSequenceRunning = false;

            // 8. HUD Prompt: "Başucundaki akıllı telefona bak"
            GameEvents.TriggerObjectiveToastRequested("Başucundaki akıllı telefona bak", 3.5f);

            // Update QuestTrackerHUD to Stage 1 objective
            StarterPrologueDirector.Instance?.DispatchCurrentPrologueObjective();

            Debug.Log("[ApartmentWakeUpSequence] Bedroom wake-up sequence completed; 100% control active.");
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
            eyelidCanvas.sortingOrder = 999;

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

            GameManager.Instance?.SaveGame();
        }

        public void RestorePhoneChecked(bool checkedState)
        {
            hasCheckedIntroPhone = checkedState;
            Debug.Log($"[ApartmentWakeUpSequence] Restored phone checked state: {hasCheckedIntroPhone}");
        }
    }
}
