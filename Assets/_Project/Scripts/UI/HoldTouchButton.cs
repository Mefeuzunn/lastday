using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Micro-Step 1A: Continuous Hold-Input Touch & Keyboard Controller.
    /// Implements IPointerDownHandler, IPointerUpHandler, and IPointerExitHandler
    /// with smooth axis interpolation (0f -> 1f) and concurrent PC keyboard mapping.
    /// Prevents the button from getting stuck if finger drifts off the touch target.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class HoldTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Keyboard Bindings (PC Fallback)")]
        [SerializeField] private KeyCode primaryKey = KeyCode.None;
        [SerializeField] private KeyCode secondaryKey = KeyCode.None;

        [Header("Axis Dynamics")]
        [SerializeField] private float pressSpeed = 8.0f;
        [SerializeField] private float releaseSpeed = 10.0f;

        [Header("Visual Feedback")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color pressedColor = new Color(0.9f, 0.95f, 1f, 1f);
        [SerializeField] private float pressedScale = 0.95f;

        private bool isPointerDown = false;
        private float currentValue = 0f;
        private Vector3 originalScale;

        public float Value => currentValue;
        public bool IsPressed => isPointerDown || IsKeyHeld();

        public void ConfigureKeys(KeyCode primary, KeyCode secondary = KeyCode.None)
        {
            primaryKey = primary;
            secondaryKey = secondary;
        }

        private void Awake()
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
            originalScale = transform.localScale;
            if (targetGraphic != null) targetGraphic.color = normalColor;
        }

        private void OnDisable()
        {
            isPointerDown = false;
            currentValue = 0f;
            transform.localScale = originalScale;
            if (targetGraphic != null) targetGraphic.color = normalColor;
        }

        private void Update()
        {
            bool active = isPointerDown || IsKeyHeld();
            float targetVal = active ? 1.0f : 0.0f;
            float rate = active ? pressSpeed : releaseSpeed;

            currentValue = Mathf.MoveTowards(currentValue, targetVal, Time.unscaledDeltaTime * rate);

            // Visual feedback
            if (targetGraphic != null)
            {
                targetGraphic.color = Color.Lerp(normalColor, pressedColor, currentValue);
            }

            transform.localScale = Vector3.Lerp(originalScale, originalScale * pressedScale, currentValue);
        }

        private bool IsKeyHeld()
        {
            if (primaryKey != KeyCode.None && Input.GetKey(primaryKey)) return true;
            if (secondaryKey != KeyCode.None && Input.GetKey(secondaryKey)) return true;
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerDown = false;
        }
    }
}
