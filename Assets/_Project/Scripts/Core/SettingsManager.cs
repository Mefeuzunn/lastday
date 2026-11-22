using UnityEngine;
using ProjectSurvival.Player;

namespace ProjectSurvival.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const string KeyMasterVol = "Settings_MasterVolume";
        private const string KeySfxVol = "Settings_SfxVolume";
        private const string KeyAmbientVol = "Settings_AmbientVolume";
        private const string KeyMuted = "Settings_IsMuted";

        private const string KeyLookSens = "Settings_LookSensitivity";
        private const string KeyAimSens = "Settings_AimSensitivity";
        private const string KeyFOV = "Settings_FOV";
        private const string KeyInvertY = "Settings_InvertY";

        private const string KeyQuality = "Settings_QualityLevel";
        private const string KeyCameraShake = "Settings_CameraShake";
        private const string KeyHaptics = "Settings_Haptics";

        // Runtime Cached Values
        public float MasterVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.8f;
        public float AmbientVolume { get; private set; } = 0.20f;
        public bool IsMuted { get; private set; } = false;

        public float LookSensitivity { get; private set; } = 1.2f;
        public float AimSensitivity { get; private set; } = 0.8f;
        public float FOV { get; private set; } = 60f;
        public bool InvertY { get; private set; } = false;

        public int QualityLevel { get; private set; } = 1; // 0: Düşük, 1: Orta, 2: Yüksek
        public bool CameraShake { get; private set; } = true;
        public bool HapticsEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSettings();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        public void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVol, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfxVol, 0.8f);
            AmbientVolume = PlayerPrefs.GetFloat(KeyAmbientVol, 0.20f);
            IsMuted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;

            LookSensitivity = PlayerPrefs.GetFloat(KeyLookSens, 1.2f);
            AimSensitivity = PlayerPrefs.GetFloat(KeyAimSens, 0.8f);
            FOV = PlayerPrefs.GetFloat(KeyFOV, 60f);
            InvertY = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;

            QualityLevel = PlayerPrefs.GetInt(KeyQuality, 1);
            CameraShake = PlayerPrefs.GetInt(KeyCameraShake, 1) == 1;
            HapticsEnabled = PlayerPrefs.GetInt(KeyHaptics, 1) == 1;
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
            PlayerPrefs.SetFloat(KeySfxVol, SfxVolume);
            PlayerPrefs.SetFloat(KeyAmbientVol, AmbientVolume);
            PlayerPrefs.SetInt(KeyMuted, IsMuted ? 1 : 0);

            PlayerPrefs.SetFloat(KeyLookSens, LookSensitivity);
            PlayerPrefs.SetFloat(KeyAimSens, AimSensitivity);
            PlayerPrefs.SetFloat(KeyFOV, FOV);
            PlayerPrefs.SetInt(KeyInvertY, InvertY ? 1 : 0);

            PlayerPrefs.SetInt(KeyQuality, QualityLevel);
            PlayerPrefs.SetInt(KeyCameraShake, CameraShake ? 1 : 0);
            PlayerPrefs.SetInt(KeyHaptics, HapticsEnabled ? 1 : 0);

            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            MasterVolume = 0.8f;
            SfxVolume = 0.8f;
            AmbientVolume = 0.6f;
            IsMuted = false;

            LookSensitivity = 1.2f;
            AimSensitivity = 0.8f;
            FOV = 60f;
            InvertY = false;

            QualityLevel = 1;
            CameraShake = true;
            HapticsEnabled = true;

            ApplyAllSettings();
            SaveSettings();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            HapticsEnabled = enabled;
            PlayerPrefs.SetInt(KeyHaptics, HapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float val)
        {
            MasterVolume = Mathf.Clamp01(val);
            if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.SetMasterVolume(MasterVolume);
            }
            else
            {
                AudioListener.volume = IsMuted ? 0f : MasterVolume;
            }
        }

        public void SetSfxVolume(float val)
        {
            SfxVolume = Mathf.Clamp01(val);
            ProceduralAudioManager.Instance?.SetSFXVolume(SfxVolume);
        }

        public void SetAmbientVolume(float val)
        {
            AmbientVolume = Mathf.Clamp01(val);
            ProceduralAudioManager.Instance?.SetAmbientVolume(AmbientVolume);
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.SetMuted(IsMuted);
            }
            else
            {
                AudioListener.volume = IsMuted ? 0f : MasterVolume;
            }
        }

        public void SetLookSensitivity(float val)
        {
            LookSensitivity = Mathf.Clamp(val, 0.2f, 3.0f);
            ThirdPersonCamera.Instance?.SetLookSensitivity(LookSensitivity);
        }

        public void SetAimSensitivity(float val)
        {
            AimSensitivity = Mathf.Clamp(val, 0.2f, 2.5f);
            ThirdPersonCamera.Instance?.SetAimSensitivity(AimSensitivity);
        }

        public void SetFOV(float val)
        {
            FOV = Mathf.Clamp(val, 50f, 75f);
            ThirdPersonCamera.Instance?.SetDefaultFOV(FOV);
        }

        public void SetInvertY(bool invert)
        {
            InvertY = invert;
            ThirdPersonCamera.Instance?.SetInvertY(InvertY);
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, 2);
            ApplyQualityLevel(QualityLevel);
        }

        public void SetCameraShake(bool enabled)
        {
            CameraShake = enabled;
            ThirdPersonCamera.Instance?.SetCameraShakeEnabled(CameraShake);
        }

        public void ApplyAllSettings()
        {
            SetMasterVolume(MasterVolume);
            SetSfxVolume(SfxVolume);
            SetAmbientVolume(AmbientVolume);
            SetMuted(IsMuted);

            SetLookSensitivity(LookSensitivity);
            SetAimSensitivity(AimSensitivity);
            SetFOV(FOV);
            SetInvertY(InvertY);

            ApplyQualityLevel(QualityLevel);
            SetCameraShake(CameraShake);
        }

        private void ApplyQualityLevel(int level)
        {
            int maxQuality = QualitySettings.names.Length - 1;
            int targetQuality = Mathf.Clamp(level, 0, maxQuality);
            QualitySettings.SetQualityLevel(targetQuality, true);
        }
    }
}
