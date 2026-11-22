using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Vehicles;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Micro-Step 1A: Mobile Hold-Input Cockpit HUD & Telemetry Readout.
    /// Dedicated SafeArea-protected interface designed for 1920x1080 touch screens
    /// with concurrent PC keyboard controls. Hides combat UI while driving.
    /// </summary>
    public class VehicleDrivingHUD : MonoBehaviour
    {
        private static VehicleDrivingHUD _instance;
        public static VehicleDrivingHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VehicleDrivingHUD>();
                    if (_instance == null)
                    {
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            GameObject go = new GameObject("UI_VehicleDrivingHUD");
                            go.transform.SetParent(canvas.transform, false);
                            _instance = go.AddComponent<VehicleDrivingHUD>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("HUD Root Panel")]
        [SerializeField] private GameObject drivingHudRoot;
        [SerializeField] private RectTransform safeAreaContainer;

        [Header("Steering Touch Buttons (Bottom-Left)")]
        [SerializeField] private HoldTouchButton steerLeftBtn;
        [SerializeField] private HoldTouchButton steerRightBtn;

        [Header("Pedals (Bottom-Right)")]
        [SerializeField] private HoldTouchButton gasPedalBtn;
        [SerializeField] private HoldTouchButton brakePedalBtn;

        [Header("Auxiliary Controls")]
        [SerializeField] private Button exitBtn;
        [SerializeField] private Button headlightsBtn;
        [SerializeField] private Button hornBtn;

        [Header("Digital Speedometer & Telemetry")]
        [SerializeField] private Text speedometerText;
        [SerializeField] private Text gearIndicatorText;
        [SerializeField] private Slider fuelBarSlider;
        [SerializeField] private Image fuelBarFill;
        [SerializeField] private Text fuelText;
        [SerializeField] private Slider engineBarSlider;
        [SerializeField] private Image engineBarFill;
        [SerializeField] private Text engineText;

        [Header("Combat HUD to Toggle")]
        [SerializeField] private GameObject combatControlsContainer;

        private DrivableVehicleController activeVehicle;

        public bool IsDrivingActive => activeVehicle != null && activeVehicle.IsPlayerDriving;
        public HoldTouchButton SteerLeftBtn => steerLeftBtn;
        public HoldTouchButton SteerRightBtn => steerRightBtn;
        public HoldTouchButton GasPedalBtn => gasPedalBtn;
        public HoldTouchButton BrakePedalBtn => brakePedalBtn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                var inst = Instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIHierarchyIfMissing();
            if (drivingHudRoot != null) drivingHudRoot.SetActive(false);
        }

        public void Open(DrivableVehicleController vehicle)
        {
            activeVehicle = vehicle;
            if (drivingHudRoot == null) BuildUIHierarchyIfMissing();

            if (drivingHudRoot != null) drivingHudRoot.SetActive(true);

            // Update auxiliary button label for Police Siren vs Standard Horn
            if (hornBtn != null)
            {
                Text hornText = hornBtn.GetComponentInChildren<Text>();
                if (hornText != null)
                {
                    hornText.text = (vehicle != null && vehicle.VehicleType == HeroVehicleType.Police_Interceptor)
                        ? "SİREN [H]"
                        : "KORNA [H]";
                }
            }

            // Hide standard on-foot combat controls and action buttons
            FindAndToggleCombatControls(false);
        }

        public void Close()
        {
            if (drivingHudRoot != null) drivingHudRoot.SetActive(false);

            // Restore on-foot combat controls
            FindAndToggleCombatControls(true);

            activeVehicle = null;
        }

        private void Update()
        {
            if (activeVehicle == null || !activeVehicle.IsPlayerDriving) return;

            // 1. Calculate Continuous Inputs from HoldTouchButtons (handles both Touch + Keyboard)
            float steer = 0f;
            if (steerRightBtn != null && steerLeftBtn != null)
            {
                steer = steerRightBtn.Value - steerLeftBtn.Value;
            }

            float gas = gasPedalBtn != null ? gasPedalBtn.Value : 0f;
            float brake = brakePedalBtn != null ? brakePedalBtn.Value : 0f;

            float throttleInput = 0f;
            float brakeInput = 0f;

            float forwardSpeed = activeVehicle.ForwardSpeedKmh;

            if (gas > 0.05f)
            {
                throttleInput = gas;
            }

            if (brake > 0.05f)
            {
                // If moving forward > 2.0 km/h, apply brakes
                if (forwardSpeed > 2.0f)
                {
                    brakeInput = brake;
                }
                else
                {
                    // If stopped or reversing, pedal acts as reverse throttle
                    throttleInput = -brake;
                }
            }

            // Handbrake PC fallback
            if (Input.GetKey(KeyCode.Space))
            {
                brakeInput = 1.0f;
            }

            activeVehicle.SetInputs(steer, throttleInput, brakeInput);

            // 2. Auxiliaries & Exit Shortcut
            if (Input.GetKeyDown(KeyCode.F))
            {
                activeVehicle.ExitDriverSeat();
                return;
            }
            if (Input.GetKeyDown(KeyCode.L)) activeVehicle.ToggleHeadlights();
            if (Input.GetKeyDown(KeyCode.H)) activeVehicle.HonkHorn();

            // 3. Telemetry Readout
            UpdateTelemetry(forwardSpeed);

            // 4. Look-Ahead for Sector Streamer: 95m forward projection
            Vector3 forwardVec = activeVehicle.transform.forward;
            float lookRatio = Mathf.Clamp01(activeVehicle.CurrentSpeedKmh / 50.0f);
            Vector3 lookAhead = forwardVec * Mathf.Lerp(45.0f, 95.0f, lookRatio);
            WorldSectorStreamer.Instance?.SetDrivingLookAhead(lookAhead);
        }

        private void UpdateTelemetry(float forwardSpeed)
        {
            float absSpeed = Mathf.Abs(forwardSpeed);

            if (speedometerText != null)
            {
                speedometerText.text = $"<size=30><b>{absSpeed:F0}</b></size> <size=13>KM/S</size>";
            }

            if (gearIndicatorText != null)
            {
                string gear = "P";
                Color gearCol = Color.yellow;

                if (absSpeed < 1.0f && (gasPedalBtn == null || gasPedalBtn.Value < 0.1f) && (brakePedalBtn == null || brakePedalBtn.Value < 0.1f))
                {
                    gear = "P";
                    gearCol = new Color(0.9f, 0.7f, 0.2f);
                }
                else if (forwardSpeed >= -0.5f)
                {
                    gear = "D";
                    gearCol = new Color(0.2f, 0.9f, 0.4f);
                }
                else
                {
                    gear = "R";
                    gearCol = new Color(0.95f, 0.25f, 0.25f);
                }

                gearIndicatorText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(gearCol)}>[{gear}]</color>";
            }

            // Update Fuel and Engine durability telemetry bars
            if (activeVehicle != null)
            {
                var maint = activeVehicle.GetComponent<VehicleMaintenanceModule>();
                float fuel = (maint != null) ? maint.CurrentFuel : activeVehicle.Fuel;
                float maxFuel = (maint != null) ? maint.MaxFuel : 100f;
                float fuelRatio = Mathf.Clamp01(fuel / Mathf.Max(1f, maxFuel));

                if (fuelBarSlider != null) fuelBarSlider.value = fuelRatio;
                if (fuelText != null) fuelText.text = $"<color=#FFB703>YAKIT</color> %{fuelRatio * 100f:F0}";

                float hp = (maint != null) ? maint.CurrentEngineHP : activeVehicle.EngineHP;
                float maxHp = (maint != null) ? maint.MaxEngineHP : 100f;
                float hpRatio = Mathf.Clamp01(hp / Mathf.Max(1f, maxHp));

                if (engineBarSlider != null) engineBarSlider.value = hpRatio;
                Color hpCol = Color.Lerp(new Color(0.93f, 0.28f, 0.44f), new Color(0.02f, 0.84f, 0.63f), hpRatio);
                if (engineBarFill != null) engineBarFill.color = hpCol;
                if (engineText != null) engineText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(hpCol)}>MOTOR</color> %{hpRatio * 100f:F0}";
            }
        }

        private void FindAndToggleCombatControls(bool show)
        {
            if (combatControlsContainer == null)
            {
                combatControlsContainer = GameObject.Find("ActionButtons") ?? GameObject.Find("CombatControls") ?? GameObject.Find("RightControlCluster");
            }

            if (combatControlsContainer != null)
            {
                combatControlsContainer.SetActive(show);
            }

            if (QuickSlotUI.Instance != null)
            {
                QuickSlotUI.Instance.gameObject.SetActive(show);
            }
        }

        // =========================================================================
        // PROCEDURAL SAFEAREA UI BUILDER (1920x1080 Reference Layout)
        // =========================================================================

        public void BuildUIHierarchyIfMissing()
        {
            if (drivingHudRoot != null && steerLeftBtn != null && steerRightBtn != null && gasPedalBtn != null && brakePedalBtn != null)
            {
                return;
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas == null) return;

            // Ensure CanvasScaler is setup for mobile 1920x1080
            CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }

            // 1. Root Container
            GameObject rootGo = GameObject.Find("Panel_VehicleDrivingHUD");
            if (rootGo == null)
            {
                rootGo = new GameObject("Panel_VehicleDrivingHUD", typeof(RectTransform));
                rootGo.transform.SetParent(parentCanvas.transform, false);
            }

            RectTransform rrt = rootGo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            drivingHudRoot = rootGo;

            // 2. Safe Area Container
            GameObject saGo = rootGo.transform.Find("SafeAreaContainer")?.gameObject;
            if (saGo == null)
            {
                saGo = new GameObject("SafeAreaContainer", typeof(RectTransform));
                saGo.transform.SetParent(rootGo.transform, false);
            }
            safeAreaContainer = saGo.GetComponent<RectTransform>();
            safeAreaContainer.anchorMin = Vector2.zero;
            safeAreaContainer.anchorMax = Vector2.one;
            safeAreaContainer.offsetMin = Vector2.zero;
            safeAreaContainer.offsetMax = Vector2.zero;

            // 3. Bottom-Left Steering Cluster
            // SOL (<) button: AnchoredPosition: (44, 48), Size: 96x96
            steerLeftBtn = CreateHoldButton(safeAreaContainer, "Btn_SteerLeft", "◀",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 48f), new Vector2(96f, 96f),
                KeyCode.A, KeyCode.LeftArrow, new Color(0.12f, 0.16f, 0.24f, 0.88f));

            // SAĞ (>) button: AnchoredPosition: (154, 48), Size: 96x96
            steerRightBtn = CreateHoldButton(safeAreaContainer, "Btn_SteerRight", "▶",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(154f, 48f), new Vector2(96f, 96f),
                KeyCode.D, KeyCode.RightArrow, new Color(0.12f, 0.16f, 0.24f, 0.88f));

            // 4. Bottom-Right Pedal Cluster
            // GAZ pedal: AnchoredPosition: (-44, 48), Size: 100x120 (emerald-accented glass)
            gasPedalBtn = CreateHoldButton(safeAreaContainer, "Btn_GasPedal", "GAZ",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 48f), new Vector2(100f, 120f),
                KeyCode.W, KeyCode.UpArrow, new Color(0.12f, 0.45f, 0.28f, 0.90f));

            // FREN / GERİ pedal: AnchoredPosition: (-158, 48), Size: 88x104 (crimson-accented glass)
            brakePedalBtn = CreateHoldButton(safeAreaContainer, "Btn_BrakePedal", "FREN\nGERİ",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-158f, 48f), new Vector2(88f, 104f),
                KeyCode.S, KeyCode.DownArrow, new Color(0.60f, 0.16f, 0.16f, 0.90f));

            // 5. Top-Right / Mid-Right Exit Button
            // ARAÇTAN İN: AnchoredPosition: (-44, 186), Size: 140x48
            exitBtn = CreateActionButton(safeAreaContainer, "Btn_ExitVehicle", "ARAÇTAN İN [F]",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 186f), new Vector2(140f, 48f),
                () => activeVehicle?.ExitDriverSeat(), new Color(0.82f, 0.28f, 0.15f, 0.92f));

            // 6. Headlights & Horn Auxiliary Buttons (Size: 92x44 each)
            headlightsBtn = CreateActionButton(safeAreaContainer, "Btn_Headlights", "FARLAR [L]",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 246f), new Vector2(92f, 44f),
                () => activeVehicle?.ToggleHeadlights(), new Color(0.18f, 0.24f, 0.35f, 0.90f));

            hornBtn = CreateActionButton(safeAreaContainer, "Btn_Horn", "KORNA [H]",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-144f, 246f), new Vector2(92f, 44f),
                () => activeVehicle?.HonkHorn(), new Color(0.18f, 0.24f, 0.35f, 0.90f));

            // 7. Bottom-Center Digital Speedometer & Gear Readout
            CreateDigitalSpeedometer(safeAreaContainer);
        }

        private HoldTouchButton CreateHoldButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size,
            KeyCode primaryKey, KeyCode secondaryKey, Color bgCol)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : null;

            if (btnGo == null)
            {
                btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(HoldTouchButton));
                btnGo.transform.SetParent(parent, false);
            }

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin; // pivot aligns with corner
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = bgCol;

            HoldTouchButton htb = btnGo.GetComponent<HoldTouchButton>();
            if (htb == null) htb = btnGo.AddComponent<HoldTouchButton>();
            htb.ConfigureKeys(primaryKey, secondaryKey);

            // Label
            Transform txtChild = btnGo.transform.Find("Text");
            GameObject txtGo = txtChild != null ? txtChild.gameObject : null;
            if (txtGo == null)
            {
                txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(btnGo.transform, false);
            }

            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text t = txtGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = (size.y > 100f) ? 20 : 16;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;

            return htb;
        }

        private Button CreateActionButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size,
            System.Action onClick, Color bgCol)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : null;

            if (btnGo == null)
            {
                btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(parent, false);
            }

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = bgCol;

            Button btn = btnGo.GetComponent<Button>();
            if (btn == null) btn = btnGo.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());

            Transform txtChild = btnGo.transform.Find("Text");
            GameObject txtGo = txtChild != null ? txtChild.gameObject : null;
            if (txtGo == null)
            {
                txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(btnGo.transform, false);
            }

            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text t = txtGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;

            return btn;
        }

        private void CreateDigitalSpeedometer(Transform parent)
        {
            Transform existing = parent.Find("Panel_DigitalSpeedometer");
            GameObject speedoGo = existing != null ? existing.gameObject : null;

            if (speedoGo == null)
            {
                speedoGo = new GameObject("Panel_DigitalSpeedometer", typeof(RectTransform), typeof(Image));
                speedoGo.transform.SetParent(parent, false);
            }

            RectTransform srt = speedoGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 28f);
            srt.sizeDelta = new Vector2(280f, 68f);

            Image bg = speedoGo.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);

            // Left Section: Speedometer & Gear Text
            Transform speedChild = speedoGo.transform.Find("Text_Speed");
            GameObject txtGo = speedChild != null ? speedChild.gameObject : null;
            if (txtGo == null)
            {
                txtGo = new GameObject("Text_Speed", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(speedoGo.transform, false);
            }
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.35f);
            trt.anchorMax = new Vector2(0.50f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            speedometerText = txtGo.GetComponent<Text>();
            speedometerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speedometerText.fontSize = 24;
            speedometerText.fontStyle = FontStyle.Bold;
            speedometerText.alignment = TextAnchor.MiddleCenter;
            speedometerText.color = Color.white;
            speedometerText.text = "<b>0</b> <size=13>KM/S</size>";

            // Gear Indicator Text
            Transform gearChild = speedoGo.transform.Find("Text_Gear");
            GameObject gearGo = gearChild != null ? gearChild.gameObject : null;
            if (gearGo == null)
            {
                gearGo = new GameObject("Text_Gear", typeof(RectTransform), typeof(Text));
                gearGo.transform.SetParent(speedoGo.transform, false);
            }
            RectTransform grt = gearGo.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = new Vector2(0.50f, 0.38f);
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;

            gearIndicatorText = gearGo.GetComponent<Text>();
            gearIndicatorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gearIndicatorText.fontSize = 13;
            gearIndicatorText.fontStyle = FontStyle.Bold;
            gearIndicatorText.alignment = TextAnchor.MiddleCenter;
            gearIndicatorText.color = Color.yellow;
            gearIndicatorText.text = "[P]";

            // Right Section: Telemetry Bars (Fuel & Engine Durability)
            CreateTelemetryBar(speedoGo.transform, "Bar_Fuel", new Vector2(0.52f, 0.52f), new Vector2(0.96f, 0.88f),
                new Color(1.0f, 0.72f, 0.01f), "YAKIT %45", out fuelBarSlider, out fuelBarFill, out fuelText);

            CreateTelemetryBar(speedoGo.transform, "Bar_Engine", new Vector2(0.52f, 0.12f), new Vector2(0.96f, 0.48f),
                new Color(0.02f, 0.84f, 0.63f), "MOTOR %40", out engineBarSlider, out engineBarFill, out engineText);
        }

        private void CreateTelemetryBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Color fillColor, string defaultText, out Slider slider, out Image fillImg, out Text labelText)
        {
            Transform existing = parent.Find(name);
            GameObject barGo = existing != null ? existing.gameObject : null;
            if (barGo == null)
            {
                barGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
                barGo.transform.SetParent(parent, false);
            }

            RectTransform brt = barGo.GetComponent<RectTransform>();
            brt.anchorMin = anchorMin;
            brt.anchorMax = anchorMax;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            slider = barGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            // Background
            Transform bgChild = barGo.transform.Find("Background");
            GameObject bgGo = bgChild != null ? bgChild.gameObject : null;
            if (bgGo == null)
            {
                bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(barGo.transform, false);
            }
            RectTransform bgrt = bgGo.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero;
            bgrt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.20f, 0.95f);

            // Fill Area
            Transform fillArea = barGo.transform.Find("Fill Area");
            GameObject faGo = fillArea != null ? fillArea.gameObject : null;
            if (faGo == null)
            {
                faGo = new GameObject("Fill Area", typeof(RectTransform));
                faGo.transform.SetParent(barGo.transform, false);
            }
            RectTransform fart = faGo.GetComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = Vector2.zero;
            fart.offsetMax = Vector2.zero;

            Transform fillChild = faGo.transform.Find("Fill");
            GameObject fGo = fillChild != null ? fillChild.gameObject : null;
            if (fGo == null)
            {
                fGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fGo.transform.SetParent(faGo.transform, false);
            }
            RectTransform frt = fGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;

            fillImg = fGo.GetComponent<Image>();
            fillImg.color = fillColor;
            slider.fillRect = frt;

            // Text Label
            Transform txtChild = barGo.transform.Find("Text_Label");
            GameObject tlGo = txtChild != null ? txtChild.gameObject : null;
            if (tlGo == null)
            {
                tlGo = new GameObject("Text_Label", typeof(RectTransform), typeof(Text));
                tlGo.transform.SetParent(barGo.transform, false);
            }
            RectTransform tlrt = tlGo.GetComponent<RectTransform>();
            tlrt.anchorMin = Vector2.zero;
            tlrt.anchorMax = Vector2.one;
            tlrt.offsetMin = Vector2.zero;
            tlrt.offsetMax = Vector2.zero;

            labelText = tlGo.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 10;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = defaultText;
        }
    }
}
