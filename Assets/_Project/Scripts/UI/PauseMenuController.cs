using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private SettingsUI settingsUI;

        [Header("Pause Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button skillsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToMainMenuButton;
        [SerializeField] private Text saveFeedbackText;

        [Header("Return to Main Menu Confirmation")]
        [SerializeField] private GameObject returnConfirmDialog;
        [SerializeField] private Button confirmReturnYesButton;
        [SerializeField] private Button confirmReturnNoButton;

        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        private Coroutine clearFeedbackCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureMainMenuButton();
            BindButtons();

            if (pausePanel != null) pausePanel.SetActive(false);
            if (returnConfirmDialog != null) returnConfirmDialog.SetActive(false);
            if (saveFeedbackText != null) saveFeedbackText.text = string.Empty;
        }

        private void BindButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(TogglePause);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(Resume);
            }
            if (saveGameButton != null)
            {
                saveGameButton.onClick.RemoveAllListeners();
                saveGameButton.onClick.AddListener(SaveGame);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OpenSettings);
            }
            if (skillsButton != null)
            {
                skillsButton.onClick.RemoveAllListeners();
                skillsButton.onClick.AddListener(OpenSkillTree);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGame);
            }
            if (returnToMainMenuButton != null)
            {
                returnToMainMenuButton.onClick.RemoveAllListeners();
                returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
            }
            if (confirmReturnYesButton != null)
            {
                confirmReturnYesButton.onClick.RemoveAllListeners();
                confirmReturnYesButton.onClick.AddListener(ConfirmReturnToMainMenu);
            }
            if (confirmReturnNoButton != null)
            {
                confirmReturnNoButton.onClick.RemoveAllListeners();
                confirmReturnNoButton.onClick.AddListener(CancelReturnToMainMenu);
            }
        }

        public void OpenSkillTree()
        {
            SkillTreeUI.Instance?.OpenSkillTree();
        }

        public void Configure(GameObject panel, SettingsUI settings, Button pBtn, Button rBtn, Button sBtn, Button setBtn, Button resBtn, Text fbText)
        {
            pausePanel = panel;
            settingsUI = settings;
            pauseButton = pBtn;
            resumeButton = rBtn;
            saveGameButton = sBtn;
            settingsButton = setBtn;
            restartButton = resBtn;
            saveFeedbackText = fbText;

            EnsureMainMenuButton();
            BindButtons();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (returnConfirmDialog != null && returnConfirmDialog.activeSelf)
                {
                    CancelReturnToMainMenu();
                    return;
                }

                // If settings is open, close settings and return to pause panel
                if (settingsUI != null && settingsUI.IsOpen)
                {
                    settingsUI.CloseSettings();
                    return;
                }

                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (pausePanel == null) return;

            if (pausePanel.activeSelf)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            if (returnConfirmDialog != null) returnConfirmDialog.SetActive(false);
            if (saveFeedbackText != null) saveFeedbackText.text = string.Empty;

            Time.timeScale = 0f;
            GameManager.Instance?.SetGameState(GameState.Paused);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void Resume()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (returnConfirmDialog != null) returnConfirmDialog.SetActive(false);
            if (settingsUI != null && settingsUI.IsOpen) settingsUI.CloseSettings();

            Time.timeScale = 1f;
            GameManager.Instance?.SetGameState(GameState.Gameplay);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void OpenSettings()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsUI != null) settingsUI.OpenSettings();
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void OnSettingsClosed()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void SaveGame()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
                if (saveFeedbackText != null)
                {
                    saveFeedbackText.text = "Oyun Başarıyla Kaydedildi";
                    if (clearFeedbackCoroutine != null) StopCoroutine(clearFeedbackCoroutine);
                    clearFeedbackCoroutine = StartCoroutine(ClearFeedbackRoutine());
                }
            }
        }

        private IEnumerator ClearFeedbackRoutine()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (saveFeedbackText != null) saveFeedbackText.text = string.Empty;
        }

        private void RestartGame()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerMedium();
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                GameEvents.ClearAll();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        private void OnReturnToMainMenuClicked()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            EnsureConfirmDialog();
            if (returnConfirmDialog != null)
            {
                returnConfirmDialog.SetActive(true);
            }
        }

        private void ConfirmReturnToMainMenu()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerMedium();

            if (returnConfirmDialog != null) returnConfirmDialog.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);

            Time.timeScale = 1f;

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.ShowMenu(true);
            }
            else
            {
                GameManager.Instance?.SetGameState(GameState.MainMenu);
            }

            CinematicMenuCamera.Instance?.StartCinematicOrbit();
        }

        private void CancelReturnToMainMenu()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            if (returnConfirmDialog != null)
            {
                returnConfirmDialog.SetActive(false);
            }
        }

        private void EnsureMainMenuButton()
        {
            if (returnToMainMenuButton != null) return;

            Transform container = null;
            if (restartButton != null) container = restartButton.transform.parent;
            else if (pausePanel != null) container = pausePanel.transform;

            if (container == null) return;

            Button refButton = restartButton ?? resumeButton ?? saveGameButton;
            if (refButton != null)
            {
                GameObject menuBtnGo = Instantiate(refButton.gameObject, container);
                menuBtnGo.name = "Button_ReturnToMainMenu";
                returnToMainMenuButton = menuBtnGo.GetComponent<Button>();
                Text txt = menuBtnGo.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = "<b>ANA MENÜYE DÖN</b>";
                    txt.color = new Color(0.95f, 0.45f, 0.45f);
                }
            }
        }

        private void EnsureConfirmDialog()
        {
            if (returnConfirmDialog != null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null && pausePanel != null) rootCanvas = pausePanel.GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

            GameObject dialogGo = new GameObject("Dialog_ConfirmReturnToMainMenu", typeof(RectTransform), typeof(Image));
            dialogGo.transform.SetParent(rootCanvas.transform, false);

            RectTransform rt = dialogGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(460f, 220f);

            Image bg = dialogGo.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            // Message
            GameObject msgGo = new GameObject("Text_Message", typeof(RectTransform), typeof(Text));
            msgGo.transform.SetParent(dialogGo.transform, false);
            RectTransform mrt = msgGo.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0f, 0.4f);
            mrt.anchorMax = new Vector2(1f, 1f);
            mrt.offsetMin = new Vector2(20f, 0f);
            mrt.offsetMax = new Vector2(-20f, -15f);

            Text mt = msgGo.GetComponent<Text>();
            mt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mt.fontSize = 15;
            mt.alignment = TextAnchor.MiddleCenter;
            mt.color = Color.white;
            mt.supportRichText = true;
            mt.text = "<b>Ana Menüye Dönmek İstiyor Musunuz?</b>\n\n<size=12><color=#F59E0B>Kaydedilmemiş tüm ilerlemeniz kaybolacaktır.</color></size>";

            // Button Yes
            GameObject yesGo = new GameObject("Button_Yes", typeof(RectTransform), typeof(Image), typeof(Button));
            yesGo.transform.SetParent(dialogGo.transform, false);
            RectTransform yrt = yesGo.GetComponent<RectTransform>();
            yrt.anchorMin = new Vector2(0.5f, 0f);
            yrt.anchorMax = new Vector2(0.5f, 0f);
            yrt.pivot = new Vector2(1f, 0f);
            yrt.sizeDelta = new Vector2(150f, 42f);
            yrt.anchoredPosition = new Vector2(-12f, 18f);

            Image yImg = yesGo.GetComponent<Image>();
            yImg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);

            GameObject yesTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            yesTextGo.transform.SetParent(yesGo.transform, false);
            RectTransform yt_rt = yesTextGo.GetComponent<RectTransform>();
            yt_rt.anchorMin = Vector2.zero;
            yt_rt.anchorMax = Vector2.one;
            yt_rt.offsetMin = Vector2.zero;
            yt_rt.offsetMax = Vector2.zero;

            Text yt = yesTextGo.GetComponent<Text>();
            yt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            yt.fontSize = 13;
            yt.fontStyle = FontStyle.Bold;
            yt.alignment = TextAnchor.MiddleCenter;
            yt.color = Color.white;
            yt.text = "EVET, DÖN";

            confirmReturnYesButton = yesGo.GetComponent<Button>();

            // Button No
            GameObject noGo = new GameObject("Button_No", typeof(RectTransform), typeof(Image), typeof(Button));
            noGo.transform.SetParent(dialogGo.transform, false);
            RectTransform nrt = noGo.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(0.5f, 0f);
            nrt.pivot = new Vector2(0f, 0f);
            nrt.sizeDelta = new Vector2(150f, 42f);
            nrt.anchoredPosition = new Vector2(12f, 18f);

            Image nImg = noGo.GetComponent<Image>();
            nImg.color = new Color(0.24f, 0.32f, 0.44f, 0.95f);

            GameObject noTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            noTextGo.transform.SetParent(noGo.transform, false);
            RectTransform nt_rt = noTextGo.GetComponent<RectTransform>();
            nt_rt.anchorMin = Vector2.zero;
            nt_rt.anchorMax = Vector2.one;
            nt_rt.offsetMin = Vector2.zero;
            nt_rt.offsetMax = Vector2.zero;

            Text nt = noTextGo.GetComponent<Text>();
            nt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nt.fontSize = 13;
            nt.fontStyle = FontStyle.Bold;
            nt.alignment = TextAnchor.MiddleCenter;
            nt.color = Color.white;
            nt.text = "İPTAL";

            confirmReturnNoButton = noGo.GetComponent<Button>();

            returnConfirmDialog = dialogGo;
            BindButtons();
        }
    }
}
