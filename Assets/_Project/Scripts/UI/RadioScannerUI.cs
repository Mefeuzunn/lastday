using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Active Phase 5: Mobile-Friendly Tactical Radio Receiver UI.
    /// Features smooth frequency slider, fine-tune -0.2 / +0.2 MHz buttons with -8px forgiving padding,
    /// dynamic animated waveform bars, signal lock status badges, and Turkish transmission transcript box.
    /// </summary>
    public class RadioScannerUI : MonoBehaviour, IModalWindow
    {
        public static RadioScannerUI Instance { get; private set; }

        [Header("UI Root & Panels")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button hudQuickPillButton;

        [Header("Frequency Tuning Controls")]
        [SerializeField] private Slider frequencySlider;
        [SerializeField] private Button btnTuneDown;
        [SerializeField] private Button btnTuneUp;
        [SerializeField] private Text frequencyDisplayText;
        [SerializeField] private Text statusBadgeText;
        [SerializeField] private Image statusDotImage;

        [Header("Animated Waveform Meter")]
        [SerializeField] private RectTransform waveformContainer;
        [SerializeField] private List<Image> waveformBars = new List<Image>();

        [Header("Decoding & Transcript")]
        [SerializeField] private Button btnDecodeSignal;
        [SerializeField] private Text btnDecodeText;
        [SerializeField] private GameObject transcriptBox;
        [SerializeField] private Text transcriptSenderText;
        [SerializeField] private Text transcriptBodyText;
        [SerializeField] private Text transcriptLocationText;

        public bool IsModalOpen => panelRoot != null && panelRoot.activeSelf;

        private float staticAudioTimer = 0f;
        private float waveformAnimTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            WireControls();
        }

        private void Start()
        {
            ModalWindowManager.Instance?.RegisterModal(this);

            if (panelRoot != null) panelRoot.SetActive(false);
            RefreshUI();
        }

        private void OnDestroy()
        {
            ModalWindowManager.Instance?.UnregisterModal(this);
        }

        private void WireControls()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }

            if (hudQuickPillButton != null)
            {
                hudQuickPillButton.onClick.RemoveAllListeners();
                hudQuickPillButton.onClick.AddListener(ToggleModal);
            }

            if (frequencySlider != null)
            {
                frequencySlider.minValue = TacticalRadioScanner.MIN_FREQUENCY;
                frequencySlider.maxValue = TacticalRadioScanner.MAX_FREQUENCY;
                frequencySlider.onValueChanged.RemoveAllListeners();
                frequencySlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (btnTuneDown != null)
            {
                if (btnTuneDown.targetGraphic != null) btnTuneDown.targetGraphic.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                btnTuneDown.onClick.RemoveAllListeners();
                btnTuneDown.onClick.AddListener(() =>
                {
                    TacticalRadioScanner.Instance?.StepFrequency(-TacticalRadioScanner.FREQ_STEP);
                    PlayTuningFeedback();
                });
            }

            if (btnTuneUp != null)
            {
                if (btnTuneUp.targetGraphic != null) btnTuneUp.targetGraphic.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                btnTuneUp.onClick.RemoveAllListeners();
                btnTuneUp.onClick.AddListener(() =>
                {
                    TacticalRadioScanner.Instance?.StepFrequency(TacticalRadioScanner.FREQ_STEP);
                    PlayTuningFeedback();
                });
            }

            if (btnDecodeSignal != null)
            {
                if (btnDecodeSignal.targetGraphic != null) btnDecodeSignal.targetGraphic.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                btnDecodeSignal.onClick.RemoveAllListeners();
                btnDecodeSignal.onClick.AddListener(OnDecodeClicked);
            }
        }

        private void Update()
        {
            // PC Shortcut 'T'
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleModal();
            }

            if (IsModalOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModal();
            }

            if (IsModalOpen)
            {
                AnimateWaveform();
            }
        }

        public void ToggleModal()
        {
            if (IsModalOpen) CloseModal();
            else OpenModal();
        }

        public void OpenModal()
        {
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            if (panelRoot != null) panelRoot.SetActive(true);

            if (frequencySlider != null && TacticalRadioScanner.Instance != null)
            {
                frequencySlider.SetValueWithoutNotify(TacticalRadioScanner.Instance.CurrentFrequency);
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();
            RefreshUI();
        }

        public void CloseModal()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void OnSliderValueChanged(float val)
        {
            TacticalRadioScanner.Instance?.SetFrequency(val);
            PlayTuningFeedback();
            RefreshUI();
        }

        private void PlayTuningFeedback()
        {
            if (Time.time - staticAudioTimer > 0.18f)
            {
                staticAudioTimer = Time.time;
                float noise = TacticalRadioScanner.Instance != null ? TacticalRadioScanner.Instance.StaticNoise : 0.8f;
                ProceduralAudioManager.Instance?.PlayRadioStaticBurst(noise);
            }
            MobileHapticManager.TriggerLight();
            RefreshUI();
        }

        private void OnDecodeClicked()
        {
            if (TacticalRadioScanner.Instance == null) return;

            if (TacticalRadioScanner.Instance.TryDecodeSignal(out RadioBroadcast broadcast))
            {
                RefreshUI();
            }
            else
            {
                ProceduralAudioManager.Instance?.PlaySoftError();
            }
        }

        public void RefreshUI()
        {
            if (TacticalRadioScanner.Instance == null) return;

            float freq = TacticalRadioScanner.Instance.CurrentFrequency;
            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.text = $"{freq:F1} MHz";
            }

            bool isLocked = TacticalRadioScanner.Instance.IsLockedOnSignal;
            RadioBroadcast broadcast = TacticalRadioScanner.Instance.ActiveBroadcast;
            bool isDecoded = broadcast != null && TacticalRadioScanner.Instance.IsFrequencyDecoded(broadcast.frequency);

            if (statusBadgeText != null && statusDotImage != null)
            {
                if (isLocked && broadcast != null)
                {
                    statusBadgeText.text = isDecoded ? "SİNYAL KİLİTLİ (ÇÖZÜLDÜ)" : "SİNYAL KİLİTLENDİ";
                    statusBadgeText.color = new Color(0.2f, 0.95f, 0.45f);
                    statusDotImage.color = new Color(0.2f, 0.95f, 0.45f);
                }
                else if (TacticalRadioScanner.Instance.SignalStrength > 0.2f)
                {
                    statusBadgeText.text = "ZAYIF YAYIN TESPİT EDİLDİ";
                    statusBadgeText.color = new Color(1f, 0.78f, 0.2f);
                    statusDotImage.color = new Color(1f, 0.78f, 0.2f);
                }
                else
                {
                    statusBadgeText.text = "STATİK PARAZİT";
                    statusBadgeText.color = new Color(0.6f, 0.65f, 0.75f);
                    statusDotImage.color = new Color(0.45f, 0.48f, 0.55f);
                }
            }

            // Decode Button State
            if (btnDecodeSignal != null)
            {
                btnDecodeSignal.interactable = isLocked;
                if (btnDecodeText != null)
                {
                    if (isDecoded)
                    {
                        btnDecodeText.text = "SİNYAL ÇÖZÜLDÜ";
                        btnDecodeText.color = new Color(0.3f, 0.95f, 0.5f);
                    }
                    else if (isLocked)
                    {
                        btnDecodeText.text = "SİNYALİ ÇÖZ";
                        btnDecodeText.color = new Color(1f, 0.95f, 0.95f);
                    }
                    else
                    {
                        btnDecodeText.text = "SİNYAL BEKLENİYOR...";
                        btnDecodeText.color = new Color(0.6f, 0.65f, 0.7f);
                    }
                }
            }

            // Transcript Box
            if (transcriptBox != null)
            {
                if (isDecoded && broadcast != null)
                {
                    transcriptBox.SetActive(true);
                    if (transcriptSenderText != null) transcriptSenderText.text = $"KAYNAK: {broadcast.sender} ({broadcast.title})";
                    if (transcriptBodyText != null) transcriptBodyText.text = $"\"{broadcast.transcript}\"";
                    if (transcriptLocationText != null)
                    {
                        Vector3 p = broadcast.worldPosition;
                        transcriptLocationText.text = $"KOORDİNAT: X:{p.x:F0} Y:{p.y:F0} Z:{p.z:F0} (HARİTADA İŞARETLENDİ)";
                    }
                }
                else
                {
                    transcriptBox.SetActive(false);
                }
            }
        }

        private void AnimateWaveform()
        {
            if (waveformBars == null || waveformBars.Count == 0) return;

            waveformAnimTimer += Time.deltaTime * 6f;
            float strength = TacticalRadioScanner.Instance != null ? TacticalRadioScanner.Instance.SignalStrength : 0f;
            bool isLocked = TacticalRadioScanner.Instance != null && TacticalRadioScanner.Instance.IsLockedOnSignal;

            for (int i = 0; i < waveformBars.Count; i++)
            {
                Image bar = waveformBars[i];
                if (bar == null) continue;

                float seed = i * 0.45f + waveformAnimTimer;
                float noiseHeight = Mathf.PerlinNoise(seed, 0f);

                float targetScaleY;
                Color barColor;

                if (isLocked)
                {
                    targetScaleY = Mathf.Clamp(0.35f + Mathf.Sin(seed * 2f) * 0.45f + 0.2f, 0.2f, 1.0f);
                    barColor = new Color(0.2f, 0.95f, 0.45f, 0.95f);
                }
                else if (strength > 0.1f)
                {
                    targetScaleY = Mathf.Clamp((noiseHeight * strength) + 0.15f, 0.1f, 0.85f);
                    barColor = new Color(1f, 0.75f, 0.25f, 0.85f);
                }
                else
                {
                    targetScaleY = Mathf.Clamp(noiseHeight * 0.22f + 0.05f, 0.05f, 0.35f);
                    barColor = new Color(0.45f, 0.52f, 0.62f, 0.5f);
                }

                RectTransform rt = bar.rectTransform;
                Vector3 s = rt.localScale;
                s.y = Mathf.Lerp(s.y, targetScaleY, Time.deltaTime * 12f);
                rt.localScale = s;
                bar.color = barColor;
            }
        }

        public void SetWaveformBars(List<Image> bars)
        {
            waveformBars = bars;
        }

        public void BindUIReferences(GameObject root, Button closeBtn, Slider slider, Button downBtn, Button upBtn,
            Text freqText, Text statusText, Image statusDot, Button decodeBtn, Text decodeTxt,
            GameObject transBox, Text transSender, Text transBody, Text transLoc, Button quickPill)
        {
            panelRoot = root;
            closeButton = closeBtn;
            frequencySlider = slider;
            btnTuneDown = downBtn;
            btnTuneUp = upBtn;
            frequencyDisplayText = freqText;
            statusBadgeText = statusText;
            statusDotImage = statusDot;
            btnDecodeSignal = decodeBtn;
            btnDecodeText = decodeTxt;
            transcriptBox = transBox;
            transcriptSenderText = transSender;
            transcriptBodyText = transBody;
            transcriptLocationText = transLoc;
            hudQuickPillButton = quickPill;

            WireControls();
        }
    }
}
