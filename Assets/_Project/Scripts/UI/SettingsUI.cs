using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.UI
{
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance { get; private set; }

        [Header("Root Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Audio Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeText;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private Text ambientVolumeText;
        [SerializeField] private Toggle muteToggle;

        [Header("Camera Controls")]
        [SerializeField] private Slider lookSensitivitySlider;
        [SerializeField] private Text lookSensitivityText;
        [SerializeField] private Slider aimSensitivitySlider;
        [SerializeField] private Text aimSensitivityText;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Text fovText;
        [SerializeField] private Toggle invertYToggle;

        [Header("Graphics Controls")]
        [SerializeField] private Button qualityLowButton;
        [SerializeField] private Button qualityMedButton;
        [SerializeField] private Button qualityHighButton;
        [SerializeField] private Text qualityCurrentText;
        [SerializeField] private Toggle cameraShakeToggle;
        [SerializeField] private Toggle hapticsToggle;

        [Header("Action Buttons")]
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button saveAndBackButton;
        [SerializeField] private Button closeButton;

        public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

        private bool isInitializing = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupListeners();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void SetupListeners()
        {
            // Audio listeners
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (ambientVolumeSlider != null)
                ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteChanged);

            // Camera listeners
            if (lookSensitivitySlider != null)
                lookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivityChanged);
            if (aimSensitivitySlider != null)
                aimSensitivitySlider.onValueChanged.AddListener(OnAimSensitivityChanged);
            if (fovSlider != null)
                fovSlider.onValueChanged.AddListener(OnFOVChanged);
            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);

            // Graphics listeners
            if (qualityLowButton != null)
                qualityLowButton.onClick.AddListener(() => OnQualitySelected(0));
            if (qualityMedButton != null)
                qualityMedButton.onClick.AddListener(() => OnQualitySelected(1));
            if (qualityHighButton != null)
                qualityHighButton.onClick.AddListener(() => OnQualitySelected(2));
            if (cameraShakeToggle != null)
                cameraShakeToggle.onValueChanged.AddListener(OnCameraShakeChanged);
            if (hapticsToggle != null)
                hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);

            // Actions
            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
            if (saveAndBackButton != null)
                saveAndBackButton.onClick.AddListener(OnSaveAndBackClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnSaveAndBackClicked);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            RefreshUI();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            SettingsManager.Instance?.SaveSettings();
            PauseMenuController.Instance?.OnSettingsClosed();
            MainMenuController.Instance?.OnSettingsClosed();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void RefreshUI()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;

            isInitializing = true;

            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = sm.MasterVolume;
                if (masterVolumeText != null) masterVolumeText.text = $"%{(int)(sm.MasterVolume * 100f)}";
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = sm.SfxVolume;
                if (sfxVolumeText != null) sfxVolumeText.text = $"%{(int)(sm.SfxVolume * 100f)}";
            }
            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.value = sm.AmbientVolume;
                if (ambientVolumeText != null) ambientVolumeText.text = $"%{(int)(sm.AmbientVolume * 100f)}";
            }
            if (muteToggle != null)
            {
                muteToggle.isOn = sm.IsMuted;
            }

            // Camera
            if (lookSensitivitySlider != null)
            {
                lookSensitivitySlider.value = sm.LookSensitivity;
                if (lookSensitivityText != null) lookSensitivityText.text = $"{sm.LookSensitivity:F1}x";
            }
            if (aimSensitivitySlider != null)
            {
                aimSensitivitySlider.value = sm.AimSensitivity;
                if (aimSensitivityText != null) aimSensitivityText.text = $"{sm.AimSensitivity:F1}x";
            }
            if (fovSlider != null)
            {
                fovSlider.value = sm.FOV;
                if (fovText != null) fovText.text = $"{(int)sm.FOV}°";
            }
            if (invertYToggle != null)
            {
                invertYToggle.isOn = sm.InvertY;
            }

            // Graphics
            UpdateQualityButtonsUI(sm.QualityLevel);
            if (cameraShakeToggle != null)
            {
                cameraShakeToggle.isOn = sm.CameraShake;
            }
            if (hapticsToggle != null)
            {
                hapticsToggle.isOn = sm.HapticsEnabled;
            }

            isInitializing = false;
        }

        private void OnMasterVolumeChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetMasterVolume(val);
            if (masterVolumeText != null) masterVolumeText.text = $"%{(int)(val * 100f)}";
        }

        private void OnSfxVolumeChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetSfxVolume(val);
            if (sfxVolumeText != null) sfxVolumeText.text = $"%{(int)(val * 100f)}";
        }

        private void OnAmbientVolumeChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetAmbientVolume(val);
            if (ambientVolumeText != null) ambientVolumeText.text = $"%{(int)(val * 100f)}";
        }

        private void OnMuteChanged(bool muted)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetMuted(muted);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnLookSensitivityChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetLookSensitivity(val);
            if (lookSensitivityText != null) lookSensitivityText.text = $"{val:F1}x";
        }

        private void OnAimSensitivityChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetAimSensitivity(val);
            if (aimSensitivityText != null) aimSensitivityText.text = $"{val:F1}x";
        }

        private void OnFOVChanged(float val)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetFOV(val);
            if (fovText != null) fovText.text = $"{(int)val}°";
        }

        private void OnInvertYChanged(bool invert)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetInvertY(invert);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnQualitySelected(int level)
        {
            SettingsManager.Instance?.SetQualityLevel(level);
            UpdateQualityButtonsUI(level);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void UpdateQualityButtonsUI(int activeLevel)
        {
            Color activeColor = new Color(0.12f, 0.52f, 0.9f, 0.95f);
            Color inactiveColor = new Color(0.12f, 0.16f, 0.22f, 0.85f);

            if (qualityLowButton != null)
            {
                var img = qualityLowButton.GetComponent<Image>();
                if (img != null) img.color = activeLevel == 0 ? activeColor : inactiveColor;
            }
            if (qualityMedButton != null)
            {
                var img = qualityMedButton.GetComponent<Image>();
                if (img != null) img.color = activeLevel == 1 ? activeColor : inactiveColor;
            }
            if (qualityHighButton != null)
            {
                var img = qualityHighButton.GetComponent<Image>();
                if (img != null) img.color = activeLevel == 2 ? activeColor : inactiveColor;
            }

            if (qualityCurrentText != null)
            {
                string[] names = new string[] { "Düşük (Yüksek Performans)", "Orta (Dengeli)", "Yüksek (Tam Görsel)" };
                qualityCurrentText.text = names[Mathf.Clamp(activeLevel, 0, 2)];
            }
        }

        private void OnCameraShakeChanged(bool enabled)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetCameraShake(enabled);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnHapticsChanged(bool enabled)
        {
            if (isInitializing) return;
            SettingsManager.Instance?.SetHapticsEnabled(enabled);
            if (enabled) MobileHapticManager.TriggerLight();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnResetDefaultsClicked()
        {
            SettingsManager.Instance?.ResetToDefaults();
            RefreshUI();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnSaveAndBackClicked()
        {
            CloseSettings();
        }
    }
}
