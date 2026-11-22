using UnityEngine;

namespace ProjectSurvival.Core
{
    /// <summary>
    /// Phase 4: Cross-Platform Mobile Tactile Haptics Controller.
    /// Provides low-latency tactile feedback on Android and iOS devices.
    /// Safely handles Unity Editor and desktop platforms without throwing exceptions.
    /// </summary>
    public class MobileHapticManager
    {
        public class HapticProxy
        {
            public void TriggerLight() => MobileHapticManager.TriggerLight();
            public void TriggerMedium() => MobileHapticManager.TriggerMedium();
            public void TriggerHeavy() => MobileHapticManager.TriggerHeavy();
            public void TriggerSuccess() => MobileHapticManager.TriggerSuccess();
        }

        public static HapticProxy Instance { get; } = new HapticProxy();
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static bool isInitialized = false;

        private static void InitializeAndroid()
        {
            if (isInitialized) return;
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    isInitialized = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobileHapticManager] Android Vibrator init failed: {ex.Message}");
            }
        }
#endif

        private static bool IsHapticsAllowed()
        {
            if (SettingsManager.Instance != null)
            {
                return SettingsManager.Instance.HapticsEnabled;
            }
            return PlayerPrefs.GetInt("Settings_Haptics", 1) == 1;
        }

        /// <summary>
        /// Light tactile tap (15ms): UI clicks, item pickups, inventory swaps, reload clicks.
        /// </summary>
        public static void TriggerLight()
        {
            if (!IsHapticsAllowed()) return;
            Vibrate(15, 60);
        }

        /// <summary>
        /// Medium tactile pulse (35ms): 9mm pistol shots, melee axe impact, skill tree unlock.
        /// </summary>
        public static void TriggerMedium()
        {
            if (!IsHapticsAllowed()) return;
            Vibrate(35, 150);
        }

        /// <summary>
        /// Heavy tactile thump (75ms): Shotgun blasts, explosions, boss seismic slam, damage taken.
        /// </summary>
        public static void TriggerHeavy()
        {
            if (!IsHapticsAllowed()) return;
            Vibrate(75, 255);
        }

        /// <summary>
        /// Success tactile celebration: Objective completed, civilian rescued, supply drop secured.
        /// </summary>
        public static void TriggerSuccess()
        {
            if (!IsHapticsAllowed()) return;
            Vibrate(45, 180);
        }

        private static void Vibrate(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            InitializeAndroid();
            if (vibrator != null)
            {
                try
                {
                    using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        int sdkInt = version.GetStatic<int>("SDK_INT");
                        if (sdkInt >= 26)
                        {
                            using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                            {
                                AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                                vibrator.Call("vibrate", effect);
                            }
                        }
                        else
                        {
                            vibrator.Call("vibrate", milliseconds);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MobileHapticManager] Vibrate failed: {ex.Message}");
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            // Editor / Desktop: Graceful no-op
#endif
        }
    }
}
