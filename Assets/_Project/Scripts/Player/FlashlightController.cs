using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    public class FlashlightController : MonoBehaviour
    {
        public static FlashlightController Instance { get; private set; }

        [Header("Light Component")]
        [SerializeField] private Light spotLight;

        [Header("Battery Parameters")]
        [SerializeField] private float maxBattery = 100f;
        [SerializeField] private float currentBattery = 100f;
        [SerializeField] private float batteryDrainRate = 1.25f; // % per second

        private bool isOn = false;

        public bool IsOn => isOn;
        public float BatteryPercent => maxBattery > 0 ? (currentBattery / maxBattery) : 0f;
        public float CurrentBattery => currentBattery;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>();
            }

            SetFlashlight(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu) return;

            // Keyboard shortcut for Editor testing
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleFlashlight();
            }

            if (isOn)
            {
                currentBattery = Mathf.Max(0f, currentBattery - (batteryDrainRate * Time.deltaTime));
                GameEvents.TriggerFlashlightBatteryChanged(currentBattery, maxBattery);

                if (currentBattery <= 0f)
                {
                    SetFlashlight(false);
                    Debug.Log("[FlashlightController] Battery fully depleted!");
                }
            }
        }

        public void ToggleFlashlight()
        {
            SetFlashlight(!isOn);
        }

        public void SetFlashlight(bool state)
        {
            if (state && currentBattery <= 0f)
            {
                Debug.LogWarning("[FlashlightController] Cannot turn on: battery is dead.");
                return;
            }

            isOn = state;
            if (spotLight != null)
            {
                spotLight.enabled = isOn;
            }

            GameEvents.TriggerFlashlightToggled(isOn, BatteryPercent);
            GameEvents.TriggerFlashlightBatteryChanged(currentBattery, maxBattery);
        }

        public void Recharge(float amount)
        {
            currentBattery = Mathf.Min(maxBattery, currentBattery + amount);
            GameEvents.TriggerFlashlightBatteryChanged(currentBattery, maxBattery);
            Debug.Log($"[FlashlightController] Recharged battery! Current: {currentBattery:F0}%");
        }
    }
}
