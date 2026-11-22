using System;
using UnityEngine;
using ProjectSurvival.UI;

namespace ProjectSurvival.Core
{
    /// <summary>
    /// Master Mobile Runtime Hardware & Quality Bootstrapper.
    /// Runs BeforeSceneLoad to guarantee locked 60 FPS, zero screen dimming/sleep,
    /// strict landscape auto-rotation, multitouch concurrency, and immediate SafeArea enforcement.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class MobileDeviceBootstrap : MonoBehaviour
    {
        public static MobileDeviceBootstrap Instance { get; private set; }

        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenResolution = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;
        private float framePacingCheckTimer = 0f;

        public int TargetFrameRate => Application.targetFrameRate;
        public bool IsMultitouchEnabled => Input.multiTouchEnabled;
        public ScreenOrientation CurrentOrientation => Screen.orientation;
        public Rect CurrentSafeArea => Screen.safeArea;
        public Vector2Int CurrentResolution => new Vector2Int(Screen.width, Screen.height);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            ApplyMobileHardwareDefaults();
            EnsureManagerInstance();
        }

        private static void ApplyMobileHardwareDefaults()
        {
            // 1. Locked 60 FPS & Frame Pacing
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 2. Landscape Orientation Lock (Auto-Rotation restricted to Landscape Left & Right only)
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            // 3. Multitouch Concurrency (joystick movement + camera look + action buttons)
            Input.multiTouchEnabled = true;

            Debug.Log("[MobileDeviceBootstrap] Mobile hardware baseline applied: 60 FPS Locked, Sleep Disabled, Landscape Locked, Multitouch Active.");
        }

        private static void EnsureManagerInstance()
        {
            if (Instance != null) return;

            GameObject existing = GameObject.Find("MobileDeviceBootstrap");
            if (existing != null)
            {
                Instance = existing.GetComponent<MobileDeviceBootstrap>();
                if (Instance == null) Instance = existing.AddComponent<MobileDeviceBootstrap>();
                return;
            }

            GameObject bootstrapObj = new GameObject("MobileDeviceBootstrap");
            Instance = bootstrapObj.AddComponent<MobileDeviceBootstrap>();
            DontDestroyOnLoad(bootstrapObj);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ApplyMobileHardwareDefaults();
        }

        private void Start()
        {
            AttachUnderManagersHierarchy();
            ForceHardwareRefresh();
        }

        private void Update()
        {
            // Detect notch cutout, orientation flip, or screen resize and trigger SafeArea update
            if (lastSafeArea != Screen.safeArea ||
                lastScreenResolution.x != Screen.width ||
                lastScreenResolution.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                lastSafeArea = Screen.safeArea;
                lastScreenResolution = new Vector2Int(Screen.width, Screen.height);
                lastOrientation = Screen.orientation;

                SafeAreaHandler.RefreshAll();
            }

            // Periodic frame pacing guarantee (prevent background services or dialogues from resetting targetFrameRate)
            framePacingCheckTimer += Time.unscaledDeltaTime;
            if (framePacingCheckTimer >= 1.0f)
            {
                framePacingCheckTimer = 0f;
                if (Application.targetFrameRate != 60)
                {
                    Application.targetFrameRate = 60;
                }
                if (!Input.multiTouchEnabled)
                {
                    Input.multiTouchEnabled = true;
                }
            }
        }

        public void ForceHardwareRefresh()
        {
            lastSafeArea = Screen.safeArea;
            lastScreenResolution = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            SafeAreaHandler.RefreshAll();
        }

        private void AttachUnderManagersHierarchy()
        {
            GameObject managersRoot = GameObject.Find("--- MANAGERS ---");
            if (managersRoot != null && transform.parent == null)
            {
                transform.SetParent(managersRoot.transform, false);
            }
        }
    }
}
