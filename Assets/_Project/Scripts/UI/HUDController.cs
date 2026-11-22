using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Survival Bars")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private Slider thirstSlider;
        [SerializeField] private Slider infectionSlider;

        [Header("Survival Bar Texts")]
        [SerializeField] private Text healthText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text thirstText;
        [SerializeField] private Text infectionText;

        [Header("Quick Slot Bar")]
        [SerializeField] private Transform quickSlotParent;
        [SerializeField] private GameObject quickSlotButtonPrefab;
        [SerializeField] private List<Button> quickSlotButtons = new List<Button>();

        [Header("Action Buttons")]
        [SerializeField] private Button sprintButton;
        [SerializeField] private Button crouchButton;
        [SerializeField] private Button interactButton;
        [SerializeField] private Button dodgeButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button flashlightButton;
        [SerializeField] private Button takedownButton;
        [SerializeField] private Button pushButton;
        [SerializeField] private Button backpackButton;
        [SerializeField] private Button pauseButton;

        [Header("Minimalist Critical Warning Badges (<40%)")]
        [SerializeField] private GameObject hungerCriticalBadge;
        [SerializeField] private GameObject thirstCriticalBadge;
        [SerializeField] private GameObject infectionCriticalBadge;

        [Header("Flashlight & Battery")]
        [SerializeField] private Text flashlightText;
        [SerializeField] private Slider batterySlider;
        [SerializeField] private Text batteryText;
        [SerializeField] private Image flashlightRadialFill;

        [Header("Visual Feedback References")]
        [SerializeField] private Image crouchGlowImage;
        [SerializeField] private Text interactButtonText;

        [Header("Time & Atmosphere")]
        [SerializeField] private Text timeText;

        [Header("Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mapButton;

        [Header("Search Progress UI")]
        [SerializeField] private GameObject searchProgressRoot;
        [SerializeField] private Image searchRadialFill;
        [SerializeField] private Text searchProgressText;

        [Header("Combat & Weapon HUD")]
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private RectTransform crosshairTop;
        [SerializeField] private RectTransform crosshairBottom;
        [SerializeField] private RectTransform crosshairLeft;
        [SerializeField] private RectTransform crosshairRight;
        [SerializeField] private GameObject hitMarkerObj;
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text ammoCounterText;
        [SerializeField] private Button fireButton;
        [SerializeField] private Button aimButton;
        [SerializeField] private Button reloadButton;

        [Header("Objective Toast Banner")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastText;

        private PlayerController playerController;

        private void Awake()
        {
            CleanLeftEdgeArtifacts();

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (searchProgressRoot != null) searchProgressRoot.SetActive(false);

            if (sprintButton != null) sprintButton.onClick.AddListener(OnSprintClicked);
            if (crouchButton != null) crouchButton.onClick.AddListener(OnCrouchClicked);
            if (interactButton != null) interactButton.onClick.AddListener(OnInteractClicked);
            if (dodgeButton != null) dodgeButton.onClick.AddListener(OnDodgeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (craftButton != null) craftButton.onClick.AddListener(OnCraftClicked);
            if (flashlightButton != null) flashlightButton.onClick.AddListener(OnFlashlightClicked);
            if (mapButton != null) mapButton.onClick.AddListener(OnMapClicked);
            if (takedownButton != null)
            {
                takedownButton.gameObject.SetActive(false);
                takedownButton.onClick.AddListener(OnTakedownClicked);
            }
            if (pushButton != null) pushButton.onClick.AddListener(OnPushClicked);
            if (backpackButton != null) backpackButton.onClick.AddListener(OnBackpackClicked);
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);

            if (fireButton != null) fireButton.onClick.AddListener(() => ProjectSurvival.Player.WeaponSystem.Instance?.Fire());
            if (aimButton != null) aimButton.onClick.AddListener(() => ProjectSurvival.Player.WeaponSystem.Instance?.ToggleAim());
            if (reloadButton != null) reloadButton.onClick.AddListener(() => ProjectSurvival.Player.WeaponSystem.Instance?.Reload());

            if (hitMarkerObj != null) hitMarkerObj.SetActive(false);
            if (toastPanel != null) toastPanel.SetActive(false);
            if (interactButton != null) interactButton.gameObject.SetActive(false);
        }

        private void CleanLeftEdgeArtifacts()
        {
            Text[] allTexts = GetComponentsInChildren<Text>(true);
            foreach (var t in allTexts)
            {
                if (t == null) continue;
                string n = t.gameObject.name.ToLower();
                if (n.Contains("debug") || n.Contains("test") || n.Contains("temp") || n.Contains("label_old"))
                {
                    Destroy(t.gameObject);
                    continue;
                }

                RectTransform rt = t.rectTransform;
                if (rt != null && rt.anchorMin.x == 0f && rt.anchorMax.x == 0f && rt.anchoredPosition.x < -10f)
                {
                    Destroy(t.gameObject);
                }
            }
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }

            UpdateQuickSlots();

            // Initial Guidance Toast (Phase 8)
            HandleObjectiveToast("GÖREV: Polis aracının bagajından silahını al ve Dr. Aras'a ulaşmak için sokağı temizle.", 7.0f);
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += UpdateHealth;
            GameEvents.OnStaminaChanged += UpdateStamina;
            GameEvents.OnHungerChanged += UpdateHunger;
            GameEvents.OnThirstChanged += UpdateThirst;
            GameEvents.OnInfectionChanged += UpdateInfection;
            GameEvents.OnInventoryUpdated += UpdateQuickSlots;
            GameEvents.OnPlayerDied += HandlePlayerDeath;
            GameEvents.OnFlashlightToggled += UpdateFlashlightState;
            GameEvents.OnFlashlightBatteryChanged += UpdateBattery;
            GameEvents.OnDayNightChanged += UpdateDayNight;
            GameEvents.OnInteractionPromptChanged += HandleInteractionPrompt;
            GameEvents.OnSearchProgressUpdated += HandleSearchProgress;
            GameEvents.OnWeaponChanged += HandleWeaponChanged;
            GameEvents.OnHitConfirmed += HandleHitConfirmed;
            GameEvents.OnCrosshairSpreadChanged += HandleCrosshairSpread;
            GameEvents.OnObjectiveToastRequested += HandleObjectiveToast;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= UpdateHealth;
            GameEvents.OnStaminaChanged -= UpdateStamina;
            GameEvents.OnHungerChanged -= UpdateHunger;
            GameEvents.OnThirstChanged -= UpdateThirst;
            GameEvents.OnInfectionChanged -= UpdateInfection;
            GameEvents.OnInventoryUpdated -= UpdateQuickSlots;
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
            GameEvents.OnFlashlightToggled -= UpdateFlashlightState;
            GameEvents.OnFlashlightBatteryChanged -= UpdateBattery;
            GameEvents.OnDayNightChanged -= UpdateDayNight;
            GameEvents.OnInteractionPromptChanged -= HandleInteractionPrompt;
            GameEvents.OnSearchProgressUpdated -= HandleSearchProgress;
            GameEvents.OnWeaponChanged -= HandleWeaponChanged;
            GameEvents.OnHitConfirmed -= HandleHitConfirmed;
            GameEvents.OnCrosshairSpreadChanged -= HandleCrosshairSpread;
            GameEvents.OnObjectiveToastRequested -= HandleObjectiveToast;
        }

        private void HandleWeaponChanged(string weaponName, int clip, int reserve)
        {
            if (weaponNameText != null) weaponNameText.text = weaponName;
            if (ammoCounterText != null)
            {
                if (weaponName == "ELLER BOŞ (SİLAHSIZ)" || weaponName == "YUMRUK" || weaponName == "İTFAİYE BALTASI")
                {
                    ammoCounterText.text = "---";
                }
                else
                {
                    ammoCounterText.text = $"{clip} / {reserve}";
                }
            }

            if (crosshairRoot != null)
            {
                crosshairRoot.gameObject.SetActive(weaponName != "ELLER BOŞ (SİLAHSIZ)");
            }
        }

        private void HandleCrosshairSpread(float spread)
        {
            if (crosshairRoot == null) return;
            float dist = 14f * spread;
            if (crosshairTop != null) crosshairTop.anchoredPosition = new Vector2(0f, dist);
            if (crosshairBottom != null) crosshairBottom.anchoredPosition = new Vector2(0f, -dist);
            if (crosshairLeft != null) crosshairLeft.anchoredPosition = new Vector2(-dist, 0f);
            if (crosshairRight != null) crosshairRight.anchoredPosition = new Vector2(dist, 0f);
        }

        private void HandleHitConfirmed(bool isHeadshot, Vector3 hitPos)
        {
            if (hitMarkerObj != null)
            {
                StopCoroutine(nameof(HitMarkerRoutine));
                StartCoroutine(HitMarkerRoutine(isHeadshot));
            }
        }

        private System.Collections.IEnumerator HitMarkerRoutine(bool isHeadshot)
        {
            hitMarkerObj.SetActive(true);
            Image img = hitMarkerObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = isHeadshot ? new Color(1.0f, 0.16f, 0.16f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            }
            hitMarkerObj.transform.localScale = Vector3.one * (isHeadshot ? 1.4f : 1.15f);

            float elapsed = 0f;
            float duration = 0.13f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                hitMarkerObj.transform.localScale = Vector3.Lerp(Vector3.one * (isHeadshot ? 1.4f : 1.15f), Vector3.one, t);
                yield return null;
            }

            hitMarkerObj.SetActive(false);
        }

        private void HandleObjectiveToast(string message, float duration)
        {
            if (toastPanel != null && toastText != null)
            {
                StopCoroutine(nameof(ToastRoutine));
                StartCoroutine(ToastRoutine(message, duration));
            }
        }

        private System.Collections.IEnumerator ToastRoutine(string message, float duration)
        {
            toastPanel.SetActive(true);
            toastText.text = message;
            yield return new WaitForSeconds(duration);
            toastPanel.SetActive(false);
        }

        private void HandleSearchProgress(float progress)
        {
            if (progress < 0f)
            {
                if (searchProgressRoot != null) searchProgressRoot.SetActive(false);
            }
            else
            {
                if (searchProgressRoot != null) searchProgressRoot.SetActive(true);
                if (searchRadialFill != null) searchRadialFill.fillAmount = progress;
                if (searchProgressText != null) searchProgressText.text = $"ARANIYOR... %{Mathf.RoundToInt(progress * 100f)}";
            }
        }

        private void OnMapClicked()
        {
            GameEvents.TriggerMapToggleRequested();
        }

        private void Update()
        {
            // Keyboard shortcut for crafting
            if (Input.GetKeyDown(KeyCode.B))
            {
                OnCraftClicked();
            }

            // Keyboard shortcut for backpack
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                OnBackpackClicked();
            }

            // Keyboard shortcut for takedown (F) and push (V)
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnTakedownClicked();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                OnPushClicked();
            }

            if (DayNightManager.Instance != null && timeText != null)
            {
                float hour = DayNightManager.Instance.CurrentHour;
                int h = Mathf.FloorToInt(hour);
                int m = Mathf.FloorToInt((hour - h) * 60f);
                string tag = DayNightManager.Instance.IsNight ? "<color=#77AAFF>[GECE]</color>" : "<color=#FFAA33>[GÜNDÜZ]</color>";
                timeText.text = $"{tag} {h:00}:{m:00}";
            }

            if (takedownButton != null && ProjectSurvival.Player.CombatSystem.Instance != null)
            {
                bool canTakedown = ProjectSurvival.Player.CombatSystem.Instance.CanTakedown;
                if (takedownButton.gameObject.activeSelf != canTakedown)
                {
                    takedownButton.gameObject.SetActive(canTakedown);
                }
            }

            UpdateCrouchVisual();
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthSlider != null) healthSlider.value = max > 0 ? current / max : 0;
            if (healthText != null) healthText.text = $"CAN: {Mathf.CeilToInt(current)}";
        }

        private void UpdateStamina(float current, float max)
        {
            if (staminaSlider != null) staminaSlider.value = max > 0 ? current / max : 0;
            if (staminaText != null) staminaText.text = $"ENERJİ: {Mathf.CeilToInt(current)}";
        }

        private void UpdateHunger(float current, float max)
        {
            if (hungerSlider != null) hungerSlider.value = max > 0 ? current / max : 0;
            if (hungerText != null) hungerText.text = $"AÇLIK: {Mathf.CeilToInt(current)}%";
            if (hungerCriticalBadge != null)
            {
                bool isCritical = max > 0 && (current / max) < 0.40f;
                hungerCriticalBadge.SetActive(isCritical);
            }
        }

        private void UpdateThirst(float current, float max)
        {
            if (thirstSlider != null) thirstSlider.value = max > 0 ? current / max : 0;
            if (thirstText != null) thirstText.text = $"SUSUZLUK: {Mathf.CeilToInt(current)}%";
            if (thirstCriticalBadge != null)
            {
                bool isCritical = max > 0 && (current / max) < 0.40f;
                thirstCriticalBadge.SetActive(isCritical);
            }
        }

        private void UpdateInfection(float current, float max)
        {
            if (infectionSlider != null) infectionSlider.value = max > 0 ? current / max : 0;
            if (infectionText != null) infectionText.text = $"ENFEKSİYON: {Mathf.CeilToInt(current)}%";
            if (infectionCriticalBadge != null)
            {
                // Infection critical when elevated (> 40%)
                bool isCritical = max > 0 && (current / max) > 0.40f;
                infectionCriticalBadge.SetActive(isCritical);
            }
        }

        private void UpdateFlashlightState(bool isOn, float batteryPercent)
        {
            if (flashlightText != null)
            {
                flashlightText.text = isOn ? "FENER: AÇIK" : "FENER: KAPALI";
                flashlightText.color = isOn ? new Color(1f, 0.95f, 0.4f) : Color.white;
            }
        }

        private void UpdateBattery(float current, float max)
        {
            float fill = max > 0 ? (current / max) : 0f;
            if (flashlightRadialFill != null)
            {
                flashlightRadialFill.fillAmount = fill;
            }
            if (batterySlider != null)
            {
                batterySlider.value = fill;
            }
            if (batteryText != null)
            {
                batteryText.text = $"PİL: {Mathf.CeilToInt(fill * 100f)}%";
            }
        }

        private void HandleInteractionPrompt(bool isVisible, string promptText)
        {
            if (interactButton != null)
            {
                interactButton.gameObject.SetActive(isVisible);
            }

            if (interactButtonText != null)
            {
                if (isVisible && !string.IsNullOrEmpty(promptText))
                {
                    string shortPrompt = promptText.Replace("E - ", "").Trim();
                    interactButtonText.text = shortPrompt.Length > 15 ? shortPrompt.Substring(0, 13) + ".." : shortPrompt;
                }
                else
                {
                    interactButtonText.text = "ETKİLEŞ";
                }
            }
        }

        private void UpdateDayNight(bool isNight, float hour)
        {
            // Atmosphere callback
        }

        public void UpdateQuickSlots()
        {
            if (QuickSlotUI.Instance != null)
            {
                QuickSlotUI.Instance.UpdateAllBadges();
            }
        }

        private void OnQuickSlotClicked(int index)
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.UseItem(index);
            }
        }

        private void OnSprintClicked()
        {
            if (playerController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerController = player.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.SetSprint(!playerController.IsSprinting);
            }
        }

        private void OnCrouchClicked()
        {
            if (playerController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerController = player.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.ToggleCrouch();
            }

            UpdateCrouchVisual();
        }

        private void OnDodgeClicked()
        {
            if (playerController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerController = player.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.ExecuteDodge();
            }
        }

        private void UpdateCrouchVisual()
        {
            if (crouchGlowImage != null)
            {
                var stealth = FindFirstObjectByType<StealthSystem>();
                bool isCrouched = stealth != null && stealth.IsCrouched;
                crouchGlowImage.enabled = isCrouched;
            }
        }

        private void OnInteractClicked()
        {
            GameEvents.TriggerInteractPressed();
        }

        private void OnCraftClicked()
        {
            if (CraftingUI.Instance != null)
            {
                CraftingUI.Instance.TogglePanel();
            }
        }

        private void OnFlashlightClicked()
        {
            if (FlashlightController.Instance != null)
            {
                FlashlightController.Instance.ToggleFlashlight();
            }
        }

        private void HandlePlayerDeath()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnTakedownClicked()
        {
            ProjectSurvival.Player.CombatSystem.Instance?.ExecuteTakedown();
        }

        private void OnPushClicked()
        {
            ProjectSurvival.Player.CombatSystem.Instance?.ExecuteEmergencyPush();
        }

        private void OnBackpackClicked()
        {
            GameEvents.TriggerOpenBackpackRequested();
        }

        private void OnPauseClicked()
        {
            PauseMenuController.Instance?.TogglePause();
        }
    }
}
