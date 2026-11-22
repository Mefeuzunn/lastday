using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 2 (Step 2A): Ergonomic SafeArea Camera Perspective Switcher Button.
    /// Manages the top-right HUD toggle button 'Btn_CameraView' (48x48px) beside Btn_Pause and Btn_Backpack.
    /// Displays a clear badge ("3. ŞAHIS" / "1. ŞAHIS"), handles touch/clicks,
    /// triggers haptic feedback, and updates on keyboard 'V' input.
    /// Strictly adheres to: raycastTarget = true on Button Image, raycastTarget = false on child label.
    /// </summary>
    public class CameraPerspectiveSwitcher : MonoBehaviour
    {
        public static CameraPerspectiveSwitcher Instance { get; private set; }

        [Header("Button Components")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Text modeBadgeText;
        [SerializeField] private Outline buttonOutline;

        [Header("Visual Colors")]
        [SerializeField] private Color tpsBadgeColor = new Color(1.0f, 0.85f, 0.35f); // Amber warm
        [SerializeField] private Color fpsBadgeColor = new Color(0.35f, 0.85f, 1.0f); // Cyan cool

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
        }

        private void Start()
        {
            EnsureUIHierarchy();

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(OnToggleClicked);
            }

            GameEvents.OnCameraPerspectiveChanged += HandlePerspectiveChanged;

            // Initialize display to current camera state
            bool isFp = ThirdPersonCamera.Instance != null && ThirdPersonCamera.Instance.IsFirstPerson;
            UpdateBadgeText(isFp);
        }

        private void OnDestroy()
        {
            GameEvents.OnCameraPerspectiveChanged -= HandlePerspectiveChanged;
        }

        private void Update()
        {
            // Keyboard shortcut V handled by ThirdPersonCamera and PlayerController, but syncs badge here if needed
            if (Input.GetKeyDown(KeyCode.V))
            {
                // Visual update happens via GameEvents.OnCameraPerspectiveChanged
            }
        }

        public void OnToggleClicked()
        {
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.TogglePerspective();
            }
            else
            {
                Debug.LogWarning("[CameraPerspectiveSwitcher] ThirdPersonCamera instance not found!");
            }
        }

        private void HandlePerspectiveChanged(bool isFirstPerson)
        {
            UpdateBadgeText(isFirstPerson);
        }

        public void UpdateBadgeText(bool isFirstPerson)
        {
            if (modeBadgeText == null) EnsureUIHierarchy();

            if (modeBadgeText != null)
            {
                modeBadgeText.text = isFirstPerson ? "1. ŞAHIS" : "3. ŞAHIS";
                modeBadgeText.color = isFirstPerson ? fpsBadgeColor : tpsBadgeColor;
                modeBadgeText.raycastTarget = false;
            }

            if (buttonOutline != null)
            {
                buttonOutline.effectColor = isFirstPerson ? new Color(0.35f, 0.85f, 1.0f, 0.85f) : new Color(0.95f, 0.72f, 0.15f, 0.85f);
            }
        }

        public void EnsureUIHierarchy()
        {
            if (toggleButton == null) toggleButton = GetComponent<Button>();
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            if (modeBadgeText == null) modeBadgeText = GetComponentInChildren<Text>();
            if (buttonOutline == null) buttonOutline = GetComponent<Outline>();

            // If already complete, configure raycastTarget rules
            if (toggleButton != null && buttonImage != null && modeBadgeText != null)
            {
                buttonImage.raycastTarget = true;
                modeBadgeText.raycastTarget = false;
                return;
            }

            // Find parent Canvas / SafeArea
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            Transform parent = (canvas != null) ? canvas.transform : transform;
            Transform safeArea = parent.Find("SafeArea") ?? parent.Find("Safe_Area");
            if (safeArea != null) parent = safeArea;

            // Ensure GameObject name
            gameObject.name = "Btn_CameraView";
            transform.SetParent(parent, false);

            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-166f, -36f); // Top-right row beside Btn_Pause (-36) and Btn_Backpack (-104)
            rt.sizeDelta = new Vector2(48f, 48f);

            if (buttonImage == null)
            {
                buttonImage = gameObject.AddComponent<Image>();
                buttonImage.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            }
            buttonImage.raycastTarget = true;

            if (buttonOutline == null)
            {
                buttonOutline = gameObject.AddComponent<Outline>();
                buttonOutline.effectColor = new Color(0.95f, 0.72f, 0.15f, 0.85f);
                buttonOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            if (toggleButton == null)
            {
                toggleButton = gameObject.AddComponent<Button>();
                var colors = toggleButton.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 1f);
                colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                toggleButton.colors = colors;
            }

            Transform textTf = transform.Find("Badge_Text");
            GameObject textObj;
            if (textTf != null) textObj = textTf.gameObject;
            else
            {
                textObj = new GameObject("Badge_Text");
                textObj.transform.SetParent(transform, false);
            }

            RectTransform rtText = textObj.GetComponent<RectTransform>();
            if (rtText == null) rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            if (modeBadgeText == null) modeBadgeText = textObj.GetComponent<Text>();
            if (modeBadgeText == null) modeBadgeText = textObj.AddComponent<Text>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            modeBadgeText.font = font;
            modeBadgeText.fontSize = 11;
            modeBadgeText.fontStyle = FontStyle.Bold;
            modeBadgeText.alignment = TextAnchor.MiddleCenter;
            modeBadgeText.color = tpsBadgeColor;
            modeBadgeText.text = "3. ŞAHIS";
            modeBadgeText.raycastTarget = false;
        }
    }
}
