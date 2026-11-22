using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 4: Studio-Grade Real-Time Cinematic Main Menu Controller.
    /// Provides a left-aligned dark glassmorphism SafeArea UI stack, live urban background
    /// with real-time mist/weather, continuous camera orbit, Continue button with rich save
    /// metadata (Day, Time, Level, District), Controls Guide modal, Settings access,
    /// and a seamless 1.2s camera swoop transition to gameplay.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        [Header("Menu Panels")]
        [SerializeField] private GameObject menuRootPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button controlsGuideButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button audioToggleButton;
        [SerializeField] private Text audioButtonText;

        [Header("HUD Reference to Enable")]
        [SerializeField] private GameObject hudRootObject;

        [Header("Controls Guide Modal")]
        [SerializeField] private GameObject controlsGuideModal;
        [SerializeField] private Button closeGuideButton;

        [Header("Menu Launch Settings")]
        [SerializeField] private bool autoShowMenuOnStart = true;

        private bool isMenuShowing = true;
        private bool isTransitioning = false;
        private Transform playerTransform;

        public bool IsMenuShowing => isMenuShowing;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            Time.timeScale = 1.0f;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LocatePlayer();
            EnsureUIHierarchy();
            BindButtons();

            if (autoShowMenuOnStart)
            {
                // Disable ThirdPersonCamera immediately in Awake so it never positions the camera on frame 0
                var tpc = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : FindFirstObjectByType<ThirdPersonCamera>();
                if (tpc != null) tpc.enabled = false;

                // Ensure CinematicMenuCamera is present on Main Camera and starts orbit immediately
                if (Camera.main != null && Camera.main.GetComponent<CinematicMenuCamera>() == null)
                {
                    Camera.main.gameObject.AddComponent<CinematicMenuCamera>();
                }
                CinematicMenuCamera.Instance?.StartCinematicOrbit();

                // Immediately hide all gameplay HUD on Awake
                SetGameplayHUDActive(false);
            }
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            if (autoShowMenuOnStart)
            {
                ShowMenu(true);
            }
            else
            {
                ShowMenu(false);
                OnTransitionComplete();
            }
        }

        private void LocatePlayer()
        {
            if (playerTransform != null) return;
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void BindButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveAllListeners();
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            if (controlsGuideButton != null)
            {
                controlsGuideButton.onClick.RemoveAllListeners();
                controlsGuideButton.onClick.AddListener(OnControlsGuideClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
            }
            if (audioToggleButton != null)
            {
                audioToggleButton.onClick.RemoveAllListeners();
                audioToggleButton.onClick.AddListener(OnAudioToggleClicked);
            }
            if (closeGuideButton != null)
            {
                closeGuideButton.onClick.RemoveAllListeners();
                closeGuideButton.onClick.AddListener(CloseControlsGuide);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (isMenuShowing)
            {
                EnsureGameplayHUDHidden();
            }

            if (!isMenuShowing || isTransitioning) return;

            // Space or Enter hotkey to start
            if (controlsGuideModal == null || !controlsGuideModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    if (SaveSystem.HasSave()) OnContinueClicked();
                    else OnNewGameClicked();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseControlsGuide();
                }
            }
        }

        public bool AutoShowMenuOnStart => autoShowMenuOnStart;

        public void ShowMenu(bool show)
        {
            isMenuShowing = show;
            isTransitioning = false;

            if (menuRootPanel != null)
            {
                menuRootPanel.SetActive(show);
                if (show) menuRootPanel.transform.SetAsLastSibling();
            }

            SetGameplayHUDActive(!show);
            LocatePlayer();

            if (show)
            {
                Time.timeScale = 1f;
                GameManager.Instance?.SetGameState(GameState.MainMenu);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (playerTransform != null)
                {
                    var pc = playerTransform.GetComponent<PlayerController>();
                    if (pc != null) pc.enabled = false;
                }

                UpdateContinueButtonState();
                UpdateAudioText();

                // Kick off cinematic orbit
                CinematicMenuCamera.Instance?.StartCinematicOrbit();

                // Start safety coroutine to ensure any other UI scripts that enable themselves on Start() get hidden
                StartCoroutine(DelayedHideHUD());
            }
            else
            {
                if (playerTransform != null)
                {
                    var pc = playerTransform.GetComponent<PlayerController>();
                    if (pc != null) pc.enabled = true;
                }
            }
        }

        private IEnumerator DelayedHideHUD()
        {
            // Yield one frame so other UI components execute their Start() routines
            yield return null;
            if (isMenuShowing)
            {
                SetGameplayHUDActive(false);
                if (menuRootPanel != null) menuRootPanel.transform.SetAsLastSibling();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (isMenuShowing)
            {
                EnsureGameplayHUDHidden();
            }
        }

        private void SetGameplayHUDActive(bool active)
        {
            if (active && isMenuShowing) return;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int c = 0; c < allCanvases.Length; c++)
            {
                Canvas canvas = allCanvases[c];
                if (canvas == null) continue;

                if (menuRootPanel != null && menuRootPanel.transform.IsChildOf(canvas.transform))
                {
                    menuRootPanel.transform.SetAsLastSibling();
                }

                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    Transform child = canvas.transform.GetChild(i);
                    string childName = child.gameObject.name;

                    // Never hide or deactivate menu root panel or settings modal
                    if (childName.Contains("MainMenu") || childName.Contains("Modal") || 
                        childName.Contains("Settings") || childName.Contains("EventSystem"))
                    {
                        continue;
                    }

                    if (!active)
                    {
                        // While on Main Menu: unconditionally deactivate all gameplay HUD elements, prompt overlays, and dials
                        child.gameObject.SetActive(false);
                    }
                    else
                    {
                        // In Gameplay mode: only reactivate default on-screen gameplay widgets
                        bool isDefaultGameplayWidget =
                            childName.Equals("TouchLookArea", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("StealthIndicator_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("CompassBar_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("QuestTracker_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Btn_Backpack", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Btn_Pause", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("TacticalMinimap_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("TacticalVitalHUD_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Mobile_Joystick", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Action_Buttons_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Crosshair_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("WeaponHUD_Panel", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("QuickSlots_Bar", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Interaction_Prompt", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("ContextualInteraction_HUD", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Btn_JumpVault", System.StringComparison.OrdinalIgnoreCase) ||
                            childName.Equals("Btn_PrimaryInteract", System.StringComparison.OrdinalIgnoreCase);

                        child.gameObject.SetActive(isDefaultGameplayWidget);
                    }
                }
            }
        }

        private void EnsureGameplayHUDHidden()
        {
            if (!isMenuShowing) return;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int c = 0; c < allCanvases.Length; c++)
            {
                Canvas canvas = allCanvases[c];
                if (canvas == null) continue;

                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    Transform child = canvas.transform.GetChild(i);
                    string childName = child.gameObject.name;

                    if (childName.Contains("MainMenu") || childName.Contains("Modal") || 
                        childName.Contains("Settings") || childName.Contains("EventSystem"))
                    {
                        continue;
                    }

                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdateContinueButtonState()
        {
            if (continueButton == null) return;

            bool hasSave = SaveSystem.HasSave();
            continueButton.interactable = hasSave;

            Text btnText = continueButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.supportRichText = true;
                if (hasSave)
                {
                    SaveData save = SaveSystem.Load();
                    if (save != null)
                    {
                        int day = Mathf.Max(1, save.dayCount);
                        int hourInt = Mathf.FloorToInt(save.currentHour);
                        int minInt = Mathf.FloorToInt((save.currentHour % 1f) * 60f);
                        int level = Mathf.Max(1, save.playerLevel);
                        string district = GetDistrictName(save.playerPosX, save.playerPosZ);

                        btnText.text = $"<b>DEVAM ET</b>\n<size=11><color=#F59E0B>Gün {day} • {hourInt:00}:{minInt:00} • Sv. {level} • {district}</color></size>";
                    }
                    else
                    {
                        btnText.text = "<b>DEVAM ET</b>";
                    }
                }
                else
                {
                    btnText.text = "<b>DEVAM ET</b>\n<size=11><color=#6B7280>(Kayıt Bulunamadı)</color></size>";
                }
            }
        }

        private string GetDistrictName(float x, float z)
        {
            if (x >= -120f && x <= 120f && z >= -120f && z <= 120f) return "Merkez Sektör";
            if (z > 120f && Mathf.Abs(x) <= 120f) return "Kuzey Tahliye Yolu";
            if (x > 120f && z > 120f) return "Kuzeydoğu Konteyner";
            if (x > 120f && Mathf.Abs(z) <= 120f) return "Doğu Sanayi & Tren Garı";
            if (x > 120f && z < -120f) return "Güneydoğu Trafo";
            if (Mathf.Abs(x) <= 120f && z < -120f) return "Güney Metro & AVM";
            if (x < -120f && z < -120f) return "Güneybatı Banliyö";
            if (x < -120f && Mathf.Abs(z) <= 120f) return "Batı Şehir Parkı";
            if (x < -120f && z > 120f) return "Kuzeybatı Göl";
            return "Karantina Bölgesi";
        }

        private void OnNewGameClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            Time.timeScale = 1.0f;

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            // Hide menu UI cleanly
            if (menuRootPanel != null) menuRootPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }

            LocatePlayer();
            TeleportPlayerToBedroom();
            StartCameraTransition();
        }

        public void TeleportPlayerToBedroom()
        {
            LocatePlayer();
            if (playerTransform != null)
            {
                Vector3 bedroomSpawn = new Vector3(-0.65f, 0.08f, -1.40f);
                Quaternion bedroomRot = Quaternion.Euler(0f, 247.2f, 0f);

                var cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // Validate capsule clearance (radius 0.35m, height 1.8m)
                Vector3 p1 = bedroomSpawn + Vector3.up * 0.35f;
                Vector3 p2 = bedroomSpawn + Vector3.up * (1.8f - 0.35f);
                if (Physics.CheckCapsule(p1, p2, 0.30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Debug.LogWarning("[MainMenuController] Spawn point had minor collision, nudging slightly to clear space.");
                    bedroomSpawn = new Vector3(-0.55f, 0.08f, -1.30f);
                }

                playerTransform.position = bedroomSpawn;
                playerTransform.rotation = bedroomRot;
                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true;

                // Ensure player visual model is active and renderers are enabled
                Transform vm = playerTransform.Find("Visual_Model");
                if (vm != null)
                {
                    vm.gameObject.SetActive(true);
                    var rends = vm.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < rends.Length; i++)
                    {
                        if (rends[i] != null) rends[i].enabled = true;
                    }
                }

                // Ensure player starts unarmed (holstered, 0 weapons in inventory)
                var ws = playerTransform.GetComponent<WeaponSystem>();
                if (ws != null) ws.EquipWeapon(WeaponType.None);

                // Ensure survival stats and traumas are calibrated
                ProjectSurvival.Narrative.ApartmentWakeUpSequence.Instance?.CalibrateUnarmedPlayer();

                // Clear any nearby zombies within 45m safe perimeter
                SpawnProtectionController.Instance?.RelocateZombiesInsideSafePerimeter();

                Debug.Log("[MainMenuController] Player safely teleported into StarterHouse_Safehome Bedroom facing nightstand.");
            }
        }

        private void OnContinueClicked()
        {
            if (isTransitioning || !SaveSystem.HasSave()) return;
            isTransitioning = true;

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            if (menuRootPanel != null) menuRootPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadGame();
            }

            LocatePlayer();
            StartCameraTransition();
        }

        private void StartCameraTransition()
        {
            CinematicMenuCamera cam = CinematicMenuCamera.Instance;
            if (cam != null)
            {
                cam.SwoopToPlayer(playerTransform, 1.2f, () =>
                {
                    OnTransitionComplete();
                });
            }
            else
            {
                OnTransitionComplete();
            }
        }

        private void OnTransitionComplete()
        {
            isMenuShowing = false;
            isTransitioning = false;
            Time.timeScale = 1.0f;

            if (menuRootPanel != null)
            {
                menuRootPanel.SetActive(false);
            }

            SetGameplayHUDActive(true);

            LocatePlayer();
            if (playerTransform != null)
            {
                var pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;
            }

            CinematicMenuCamera cinematicCam = CinematicMenuCamera.Instance;
            if (cinematicCam != null)
            {
                cinematicCam.StopCinematicOrbit();
            }

            ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? (Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : FindFirstObjectByType<ThirdPersonCamera>());
            if (tpc != null)
            {
                tpc.enabled = true;
                tpc.SyncRotationToCurrent();
            }

            GameManager.Instance?.SetGameState(GameState.Gameplay);

            if (ProjectSurvival.Narrative.ApartmentWakeUpSequence.Instance != null && !ProjectSurvival.Narrative.ApartmentWakeUpSequence.Instance.HasCheckedPhone)
            {
                ProjectSurvival.Narrative.ApartmentWakeUpSequence.Instance.StartWakeUpManually();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnSettingsClicked()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            if (menuRootPanel != null) menuRootPanel.SetActive(false);
            SettingsUI.Instance?.OpenSettings();
        }

        public void OnSettingsClosed()
        {
            if (isMenuShowing)
            {
                if (menuRootPanel != null) menuRootPanel.SetActive(true);
                UpdateContinueButtonState();
            }
        }

        private void OnControlsGuideClicked()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            if (controlsGuideModal != null)
            {
                controlsGuideModal.SetActive(true);
            }
        }

        public void CloseControlsGuide()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            if (controlsGuideModal != null)
            {
                controlsGuideModal.SetActive(false);
            }
        }

        private void OnExitClicked()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerMedium();

            Debug.Log("[MainMenuController] Uygulamadan Çıkış Yapılıyor...");
            Application.Quit();
        }

        private void OnAudioToggleClicked()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();

            ProceduralAudioManager.Instance?.ToggleMute();
            UpdateAudioText();
        }

        private void UpdateAudioText()
        {
            if (audioButtonText != null && ProceduralAudioManager.Instance != null)
            {
                audioButtonText.text = ProceduralAudioManager.Instance.IsMuted ? "SES: KAPALI" : "SES: AÇIK";
            }
        }

        // =========================================================================
        // PROCEDURAL SAFE-AREA GLASSMORPHISM UI ENHANCEMENT
        // Ensures that even if the scene prefab only had basic buttons,
        // it transforms cleanly into a dark glassmorphic left-aligned panel
        // with Settings, Guide, Exit buttons and guide modal card.
        // =========================================================================

        private void EnsureUIHierarchy()
        {
            if (menuRootPanel == null)
            {
                menuRootPanel = gameObject;
            }

            // Find or enhance existing elements
            RectTransform rootRect = menuRootPanel.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                // Align stack to the left side (0 to 0.40 screen width) to reveal the 3D cinematic city
                rootRect.anchorMin = new Vector2(0f, 0f);
                rootRect.anchorMax = new Vector2(0.42f, 1f);
                rootRect.pivot = new Vector2(0f, 0.5f);
                rootRect.offsetMin = new Vector2(36f, 24f);
                rootRect.offsetMax = new Vector2(0f, -24f);

                Image rootImg = menuRootPanel.GetComponent<Image>();
                if (rootImg != null)
                {
                    // Dark Glassmorphism translucent background
                    rootImg.color = new Color(0.05f, 0.08f, 0.12f, 0.82f);
                }
            }

            // Locate Buttons inside panel if not serialized
            Button[] existingButtons = menuRootPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < existingButtons.Length; i++)
            {
                string bName = existingButtons[i].gameObject.name.ToLower();
                if (bName.Contains("continue") || bName.Contains("devam")) continueButton = existingButtons[i];
                else if (bName.Contains("new") || bName.Contains("yeni")) newGameButton = existingButtons[i];
                else if (bName.Contains("setting") || bName.Contains("ayar")) settingsButton = existingButtons[i];
                else if (bName.Contains("audio") || bName.Contains("ses")) audioToggleButton = existingButtons[i];
            }

            // Create Settings button if missing
            if (settingsButton == null && newGameButton != null)
            {
                GameObject setGo = Instantiate(newGameButton.gameObject, newGameButton.transform.parent);
                setGo.name = "Button_Settings";
                settingsButton = setGo.GetComponent<Button>();
                Text st = setGo.GetComponentInChildren<Text>();
                if (st != null) st.text = "<b>AYARLAR</b>";
            }

            // Create Controls Guide button if missing
            if (controlsGuideButton == null && newGameButton != null)
            {
                GameObject guideGo = Instantiate(newGameButton.gameObject, newGameButton.transform.parent);
                guideGo.name = "Button_ControlsGuide";
                controlsGuideButton = guideGo.GetComponent<Button>();
                Text gt = guideGo.GetComponentInChildren<Text>();
                if (gt != null) gt.text = "<b>KONTROLLER & REHBER</b>";
            }

            // Create Exit button if missing (Desktop/Editor)
            if (exitButton == null && newGameButton != null)
            {
                GameObject exitGo = Instantiate(newGameButton.gameObject, newGameButton.transform.parent);
                exitGo.name = "Button_Exit";
                exitButton = exitGo.GetComponent<Button>();
                Text et = exitGo.GetComponentInChildren<Text>();
                if (et != null) et.text = "<b>ÇIKIŞ</b>";
            }

            // Build Illustrated Controls Guide Modal Card
            EnsureControlsGuideModal();
        }

        private void EnsureControlsGuideModal()
        {
            if (controlsGuideModal != null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            GameObject modalGo = new GameObject("Modal_ControlsGuide", typeof(RectTransform), typeof(Image));
            modalGo.transform.SetParent(parentCanvas.transform, false);

            RectTransform rt = modalGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 440f);

            Image bg = modalGo.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.09f, 0.14f, 0.96f);

            // Header
            GameObject headerGo = new GameObject("Text_Header", typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(modalGo.transform, false);
            RectTransform hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.offsetMin = new Vector2(24f, -50f);
            hrt.offsetMax = new Vector2(-24f, -10f);

            Text ht = headerGo.GetComponent<Text>();
            ht.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ht.fontSize = 18;
            ht.fontStyle = FontStyle.Bold;
            ht.alignment = TextAnchor.MiddleCenter;
            ht.color = new Color(0.96f, 0.76f, 0.22f);
            ht.text = "HAYATTA KALMA REHBERİ & KONTROLLER";

            // Body
            GameObject bodyGo = new GameObject("Text_Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(modalGo.transform, false);
            RectTransform brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.offsetMin = new Vector2(28f, 65f);
            brt.offsetMax = new Vector2(-28f, -60f);

            Text bt = bodyGo.GetComponent<Text>();
            bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bt.fontSize = 13;
            bt.lineSpacing = 1.25f;
            bt.alignment = TextAnchor.UpperLeft;
            bt.color = new Color(0.88f, 0.90f, 0.94f);
            bt.supportRichText = true;
            bt.text =
                "<b>HAREKET & BAKIŞ:</b>\n" +
                "• Sol Sanal Joystick: Yürü / Koş (İleri itiş ile depar)\n" +
                "• Sağ Ekran: Serbest kamera bakışı ve hedef takibi\n\n" +
                "<b>ÇATIŞMA & SİLAHLAR:</b>\n" +
                "• [Ateş Butonu / Sol Tık]: Silahı ateşle veya yakın dövüş savur\n" +
                "• [Nişan Butonu / Sağ Tık]: Hassas omuz nişangahı aç\n" +
                "• [Q / Dokun]: Sonraki silaha geç  |  [H]: Kılıfa koy\n\n" +
                "<b>GİZLİLİK & HAVA ŞARTLARI:</b>\n" +
                "• [C / Çömel]: Zombi algılama mesafesini %50 azaltır\n" +
                "• Şiddetli Fırtınada ayak sesi mesafesi %35 kısalır, zombi görüşü zayıflar\n\n" +
                "<b>TEÇHİZAT & GELİŞİM:</b>\n" +
                "• Güvenli ev tezgahında zırh ve silah eklentileri (susturucu, lazer) üret\n" +
                "• Hızlı slotlardan medkit ve tuzakları anında konuşlandır";

            // Close Button
            GameObject closeGo = new GameObject("Button_CloseGuide", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(modalGo.transform, false);
            RectTransform crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.sizeDelta = new Vector2(180f, 42f);
            crt.anchoredPosition = new Vector2(0f, 15f);

            Image cImg = closeGo.GetComponent<Image>();
            cImg.color = new Color(0.85f, 0.22f, 0.22f, 0.9f);

            GameObject closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            RectTransform ctrt = closeTextGo.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.offsetMin = Vector2.zero;
            ctrt.offsetMax = Vector2.zero;

            Text closeText = closeTextGo.GetComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 14;
            closeText.fontStyle = FontStyle.Bold;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.color = Color.white;
            closeText.text = "ANLADIM (KAPAT)";

            closeGuideButton = closeGo.GetComponent<Button>();
            controlsGuideModal = modalGo;
            controlsGuideModal.SetActive(false);
        }
    }
}
