using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Master Step A: Dedicated Mobile Jump & Obstacle Vault HUD Button (ZIPLA / ATLA).
    /// Positioned cleanly above combat controls in the bottom-right cluster (-130, 175, 68x68),
    /// inside SafeArea, verified with zero overlap against ATEŞ, NİŞAN, KOŞ, EĞİL, and AL/ETKİLEŞ.
    /// Dynamically swaps label between ZIPLA (ground jump) and ATLA (obstacle vault) based on front obstacle scanner.
    /// Implements IPointerDownHandler for zero-latency touch response.
    /// </summary>
    public class JumpVaultHUDButton : MonoBehaviour, IPointerDownHandler
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;

        [Header("Colors")]
        [SerializeField] private Color jumpColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
        [SerializeField] private Color vaultColor = new Color(0.15f, 0.55f, 0.65f, 0.95f); // Teal highlight for vault

        private bool wasObstacleAhead = false;
        private float lastTriggerTime = -1f;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (background == null) background = GetComponent<Image>();
            if (labelText == null) labelText = GetComponentInChildren<Text>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnJumpVaultClicked);
            }

            if (background != null)
            {
                background.raycastTarget = true;
                background.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
            }

            if (labelText != null)
            {
                labelText.raycastTarget = false;
            }
        }

        private void Start()
        {
            UpdateVisual(false);
        }

        private void Update()
        {
            bool obstacleAhead = ParkourVaultController.Instance != null && ParkourVaultController.Instance.IsObstacleAhead();
            if (obstacleAhead != wasObstacleAhead)
            {
                wasObstacleAhead = obstacleAhead;
                UpdateVisual(obstacleAhead);
            }
        }

        private void UpdateVisual(bool canVault)
        {
            if (labelText != null)
            {
                labelText.text = canVault ? "ATLA" : "ZIPLA";
            }

            if (background != null)
            {
                background.color = canVault ? vaultColor : jumpColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TriggerAction();
        }

        public void OnJumpVaultClicked()
        {
            TriggerAction();
        }

        private void TriggerAction()
        {
            // Debounce within 0.2s to prevent dual-triggering on platforms firing both pointer down and click
            if (Time.unscaledTime - lastTriggerTime < 0.2f) return;
            lastTriggerTime = Time.unscaledTime;

            if (ParkourVaultController.Instance != null)
            {
                ParkourVaultController.Instance.TryJumpOrVault();
            }
        }
    }
}
