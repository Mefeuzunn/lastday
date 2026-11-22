using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Step C: Interactive Emergency Smartphone Screen UI Modal.
    /// Displays urgent biohazard quarantine alert, civilian casualty telemetry (~340 survivors remaining),
    /// and survival instructions upon waking in StarterHouse_Safehome.
    /// Implements IModalWindow and integrates with ModalWindowManager and SafeAreaHandler.
    /// Strictly adheres to: raycastTarget = false on all texts/decorations, raycastTarget = true ONLY on close button.
    /// </summary>
    public class SmartphoneScreenUI : MonoBehaviour, IModalWindow
    {
        public static SmartphoneScreenUI Instance { get; private set; }

        [Header("UI Component Bindings")]
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text alertHeaderText;
        [SerializeField] private Text alertSubheaderText;
        [SerializeField] private Text alertBodyText;
        [SerializeField] private Text statusTimeText;

        [Header("State")]
        [SerializeField] private bool isOpen = false;

        public bool IsModalOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();

            if (screenRoot != null)
            {
                screenRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Keyboard shortcut support: Esc or Space closes the modal cleanly
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                CloseModal();
            }
        }

        public void ShowSmartphone()
        {
            EnsureUIHierarchy();

            // Refresh text contents cleanly
            if (statusTimeText != null)
            {
                statusTimeText.text = "ACİL DURUM YAYINI • UYDU YEDEK AĞI | Batarya: %14";
            }
            if (alertHeaderText != null)
            {
                alertHeaderText.text = "⚠️ ACİL DURUM UYARISI";
            }
            if (alertSubheaderText != null)
            {
                alertSubheaderText.text = "KÜRESEL SALGIN RAPORU • BİYOLOJİK TEHLİKE";
            }
            if (alertBodyText != null)
            {
                alertBodyText.text =
                    "KÜRESEL SALGIN RAPORU:\n" +
                    "Zombiler tüm büyük şehirleri ele geçirdi. Uydu taramalarına göre bölgede hayatta kalan sadece ~340 İNSAN tespit edildi.\n\n" +
                    "HAYATTA KALMA TALİMATLARI:\n" +
                    "1. Mutfak dolabı ve buzdolabından yiyecek ve su bul.\n" +
                    "2. Dışarı çıkarken ilk silahını bul.";
            }

            isOpen = true;
            if (screenRoot != null)
            {
                screenRoot.SetActive(true);
            }

            ModalWindowManager.Instance?.RegisterModal(this);
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            ProceduralAudioManager.Instance?.PlayRadioFrequencyLockChime();
            MobileHapticManager.TriggerLight();

            Debug.Log("[SmartphoneScreenUI] Emergency smartphone alert displayed.");
        }

        public void CloseModal()
        {
            if (!isOpen) return;
            isOpen = false;

            if (screenRoot != null)
            {
                screenRoot.SetActive(false);
            }

            ModalWindowManager.Instance?.NotifyModalClosed(this);
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();

            // Mark phone checked on wake-up sequence and narrative
            ApartmentWakeUpSequence.Instance?.MarkPhoneChecked();

            // Advance StarterPrologueDirector to Stage 2 (Scavenge Food & Water)
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
                        requiredFrequency = 0f,
                        isTerminalInteraction = false,
                        xpReward = 25
                    }
                );
            }

            GameManager.Instance?.SaveGame();
            Debug.Log("[SmartphoneScreenUI] Smartphone screen closed by user. Onboarding advanced to supply scavenging.");
        }

        private void EnsureUIHierarchy()
        {
            if (screenRoot != null) return;

            // Find canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            Transform parentTransform = canvas != null ? canvas.transform : transform;

            // Root Modal Container
            screenRoot = new GameObject("Smartphone_Screen_Modal");
            screenRoot.transform.SetParent(parentTransform, false);

            RectTransform rtRoot = screenRoot.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            // Semi-transparent backdrop (clicks do NOT block close button)
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(screenRoot.transform, false);
            RectTransform rtBackdrop = backdrop.AddComponent<RectTransform>();
            rtBackdrop.anchorMin = Vector2.zero;
            rtBackdrop.anchorMax = Vector2.one;
            rtBackdrop.sizeDelta = Vector2.zero;
            Image bgImg = backdrop.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
            bgImg.raycastTarget = false; // Prompt requirement: raycastTarget ONLY on close button

            // Phone Body Frame (Centered, 380x620)
            GameObject phoneBody = new GameObject("Phone_Body");
            phoneBody.transform.SetParent(screenRoot.transform, false);
            RectTransform rtPhone = phoneBody.AddComponent<RectTransform>();
            rtPhone.anchorMin = new Vector2(0.5f, 0.5f);
            rtPhone.anchorMax = new Vector2(0.5f, 0.5f);
            rtPhone.pivot = new Vector2(0.5f, 0.5f);
            rtPhone.sizeDelta = new Vector2(380f, 620f);

            Image phoneImg = phoneBody.AddComponent<Image>();
            phoneImg.color = new Color(0.07f, 0.09f, 0.12f, 0.98f);
            phoneImg.raycastTarget = false;

            Outline phoneOutline = phoneBody.AddComponent<Outline>();
            phoneOutline.effectColor = new Color(0.95f, 0.72f, 0.15f, 0.85f);
            phoneOutline.effectDistance = new Vector2(2f, -2f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Top Status Bar
            GameObject statusBar = new GameObject("Status_Bar");
            statusBar.transform.SetParent(phoneBody.transform, false);
            RectTransform rtStatus = statusBar.AddComponent<RectTransform>();
            rtStatus.anchorMin = new Vector2(0f, 1f);
            rtStatus.anchorMax = new Vector2(1f, 1f);
            rtStatus.pivot = new Vector2(0.5f, 1f);
            rtStatus.anchoredPosition = new Vector2(0f, -8f);
            rtStatus.sizeDelta = new Vector2(-24f, 26f);

            statusTimeText = statusBar.AddComponent<Text>();
            statusTimeText.font = font;
            statusTimeText.fontSize = 11;
            statusTimeText.fontStyle = FontStyle.Bold;
            statusTimeText.alignment = TextAnchor.MiddleLeft;
            statusTimeText.color = new Color(0.75f, 0.82f, 0.90f);
            statusTimeText.text = "ACİL DURUM YAYINI • UYDU YEDEK AĞI | Batarya: %14";
            statusTimeText.raycastTarget = false;

            // Alert Box Container
            GameObject alertBox = new GameObject("Alert_Box");
            alertBox.transform.SetParent(phoneBody.transform, false);
            RectTransform rtAlert = alertBox.AddComponent<RectTransform>();
            rtAlert.anchorMin = new Vector2(0f, 1f);
            rtAlert.anchorMax = new Vector2(1f, 1f);
            rtAlert.pivot = new Vector2(0.5f, 1f);
            rtAlert.anchoredPosition = new Vector2(0f, -44f);
            rtAlert.sizeDelta = new Vector2(-24f, 96f);

            Image alertBoxImg = alertBox.AddComponent<Image>();
            alertBoxImg.color = new Color(0.24f, 0.06f, 0.08f, 0.92f);
            alertBoxImg.raycastTarget = false;

            Outline alertOutline = alertBox.AddComponent<Outline>();
            alertOutline.effectColor = new Color(0.95f, 0.22f, 0.22f, 0.9f);
            alertOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Alert Header Text
            GameObject alertHeadObj = new GameObject("Alert_Header");
            alertHeadObj.transform.SetParent(alertBox.transform, false);
            RectTransform rtHead = alertHeadObj.AddComponent<RectTransform>();
            rtHead.anchorMin = new Vector2(0f, 0.5f);
            rtHead.anchorMax = new Vector2(1f, 1f);
            rtHead.sizeDelta = new Vector2(-16f, 0f);
            rtHead.anchoredPosition = new Vector2(0f, -6f);

            alertHeaderText = alertHeadObj.AddComponent<Text>();
            alertHeaderText.font = font;
            alertHeaderText.fontSize = 18;
            alertHeaderText.fontStyle = FontStyle.Bold;
            alertHeaderText.alignment = TextAnchor.MiddleCenter;
            alertHeaderText.color = new Color(1f, 0.92f, 0.35f);
            alertHeaderText.text = "⚠️ ACİL DURUM UYARISI";
            alertHeaderText.raycastTarget = false;

            // Alert Subheader Text
            GameObject alertSubObj = new GameObject("Alert_Subheader");
            alertSubObj.transform.SetParent(alertBox.transform, false);
            RectTransform rtSub = alertSubObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0f);
            rtSub.anchorMax = new Vector2(1f, 0.5f);
            rtSub.sizeDelta = new Vector2(-16f, 0f);
            rtSub.anchoredPosition = new Vector2(0f, 4f);

            alertSubheaderText = alertSubObj.AddComponent<Text>();
            alertSubheaderText.font = font;
            alertSubheaderText.fontSize = 12;
            alertSubheaderText.fontStyle = FontStyle.Bold;
            alertSubheaderText.alignment = TextAnchor.MiddleCenter;
            alertSubheaderText.color = new Color(1f, 0.45f, 0.45f);
            alertSubheaderText.text = "KÜRESEL SALGIN RAPORU • BİYOLOJİK TEHLİKE";
            alertSubheaderText.raycastTarget = false;

            // Body Message Container
            GameObject bodyObj = new GameObject("Message_Body");
            bodyObj.transform.SetParent(phoneBody.transform, false);
            RectTransform rtBody = bodyObj.AddComponent<RectTransform>();
            rtBody.anchorMin = new Vector2(0f, 0f);
            rtBody.anchorMax = new Vector2(1f, 1f);
            rtBody.anchoredPosition = new Vector2(0f, -30f);
            rtBody.sizeDelta = new Vector2(-28f, -240f);

            alertBodyText = bodyObj.AddComponent<Text>();
            alertBodyText.font = font;
            alertBodyText.fontSize = 14;
            alertBodyText.lineSpacing = 1.25f;
            alertBodyText.alignment = TextAnchor.UpperLeft;
            alertBodyText.color = new Color(0.92f, 0.94f, 0.98f);
            alertBodyText.text =
                "KÜRESEL SALGIN RAPORU:\n" +
                "Zombiler tüm büyük şehirleri ele geçirdi. Uydu taramalarına göre bölgede hayatta kalan sadece ~340 İNSAN tespit edildi.\n\n" +
                "HAYATTA KALMA TALİMATLARI:\n" +
                "1. Mutfak dolabı ve buzdolabından yiyecek ve su bul.\n" +
                "2. Dışarı çıkarken ilk silahını bul.";
            alertBodyText.raycastTarget = false;

            // Close Action Button: Strictly the only element with raycastTarget = true
            GameObject btnObj = new GameObject("Btn_CloseSmartphone");
            btnObj.transform.SetParent(phoneBody.transform, false);
            RectTransform rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0f);
            rtBtn.anchorMax = new Vector2(0.5f, 0f);
            rtBtn.pivot = new Vector2(0.5f, 0f);
            rtBtn.anchoredPosition = new Vector2(0f, 22f);
            rtBtn.sizeDelta = new Vector2(320f, 52f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.85f, 0.18f, 0.20f, 0.95f);
            btnImg.raycastTarget = true; // Prompt requirement: raycastTarget ONLY on close button

            closeButton = btnObj.AddComponent<Button>();
            var colors = closeButton.colors;
            colors.normalColor = new Color(0.85f, 0.18f, 0.20f, 0.95f);
            colors.highlightedColor = new Color(1.0f, 0.32f, 0.34f, 1f);
            colors.pressedColor = new Color(0.65f, 0.12f, 0.14f, 1f);
            closeButton.colors = colors;

            GameObject btnTextObj = new GameObject("Btn_Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtBtnText = btnTextObj.AddComponent<RectTransform>();
            rtBtnText.anchorMin = Vector2.zero;
            rtBtnText.anchorMax = Vector2.one;
            rtBtnText.sizeDelta = Vector2.zero;

            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 14;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "TELEFONU KAPAT VE HAREKETE GEÇ";
            btnText.raycastTarget = false;

            closeButton.onClick.AddListener(CloseModal);
        }
    }
}
