using UnityEngine;

namespace ProjectSurvival.Core
{
    public class DayNightManager : MonoBehaviour
    {
        public static DayNightManager Instance { get; private set; }

        [Header("Cycle Timing")]
        [Tooltip("Full 24-hour cycle in real-world seconds (default: 240s = 4 minutes)")]
        [SerializeField] private float fullDayDurationInSeconds = 240f;
        [Range(0f, 24f)]
        [SerializeField] private float startHour = 10f; // Start at 10:00 AM

        [Header("Sun & Lighting References")]
        [SerializeField] private Light directionalSunLight;

        [Header("Color Palettes")]
        [SerializeField] private Color daySunColor = new Color(1f, 0.95f, 0.88f);
        [SerializeField] private Color sunsetSunColor = new Color(1f, 0.55f, 0.25f);
        [SerializeField] private Color nightSunColor = new Color(0.2f, 0.35f, 0.65f);

        [SerializeField] private Color dayAmbientColor = new Color(0.5f, 0.5f, 0.55f);
        [SerializeField] private Color nightAmbientColor = new Color(0.08f, 0.1f, 0.15f);

        [Header("Intensity Settings")]
        [SerializeField] private float dayIntensity = 1.15f;
        [SerializeField] private float sunsetIntensity = 0.65f;
        [SerializeField] private float nightIntensity = 0.12f;

        private float currentHour;
        private bool isNight = false;
        private int dayCount = 1;

        public float CurrentHour => currentHour;
        public bool IsNight => isNight;
        public float NormalizedTime => currentHour / 24f;
        public int DayCount => dayCount;
        public string FormattedTime
        {
            get
            {
                int hours = Mathf.FloorToInt(currentHour);
                int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
                return $"{hours:D2}:{minutes:D2}";
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentHour = startHour;
            if (directionalSunLight == null)
            {
                GameObject sunObj = GameObject.Find("Directional Light");
                if (sunObj != null) directionalSunLight = sunObj.GetComponent<Light>();
            }
        }

        private void Update()
        {
            float hoursPerSecond = 24f / fullDayDurationInSeconds;
            currentHour += hoursPerSecond * Time.deltaTime;
            if (currentHour >= 24f)
            {
                currentHour -= 24f;
                dayCount++;
            }

            UpdateLighting();
            CheckDayNightState();
        }

        private void UpdateLighting()
        {
            if (directionalSunLight == null) return;

            // 0:00 = -90 (midnight), 6:00 = 0 (sunrise), 12:00 = 90 (noon), 18:00 = 180 (sunset)
            float sunRotationX = ((currentHour / 24f) * 360f) - 90f;
            directionalSunLight.transform.rotation = Quaternion.Euler(sunRotationX, -30f, 0f);

            // Interpolate color and intensity
            if (currentHour >= 6f && currentHour < 17f) // Day
            {
                directionalSunLight.color = daySunColor;
                directionalSunLight.intensity = dayIntensity;
                RenderSettings.ambientLight = dayAmbientColor;
            }
            else if (currentHour >= 17f && currentHour < 20f) // Sunset
            {
                float t = (currentHour - 17f) / 3f;
                directionalSunLight.color = Color.Lerp(daySunColor, sunsetSunColor, t);
                directionalSunLight.intensity = Mathf.Lerp(dayIntensity, sunsetIntensity, t);
                RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, sunsetSunColor * 0.5f, t);
            }
            else if (currentHour >= 20f || currentHour < 5f) // Night
            {
                directionalSunLight.color = nightSunColor;
                directionalSunLight.intensity = nightIntensity;
                RenderSettings.ambientLight = nightAmbientColor;
            }
            else // Sunrise (5:00 - 6:00)
            {
                float t = (currentHour - 5f) / 1f;
                directionalSunLight.color = Color.Lerp(sunsetSunColor, daySunColor, t);
                directionalSunLight.intensity = Mathf.Lerp(sunsetIntensity, dayIntensity, t);
                RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, t);
            }
        }

        private void CheckDayNightState()
        {
            bool newIsNight = (currentHour >= 20f || currentHour < 5.5f);
            if (newIsNight != isNight)
            {
                isNight = newIsNight;
                GameEvents.TriggerDayNightChanged(isNight, currentHour);
                Debug.Log($"[DayNightManager] Atmosphere changed! IsNight: {isNight} (Hour: {currentHour:F1})");
            }
        }

        public void SetTime(float hour)
        {
            currentHour = Mathf.Clamp(hour, 0f, 24f);
            UpdateLighting();
            CheckDayNightState();
        }
    }
}
