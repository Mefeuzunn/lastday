using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectSurvival.UI
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static MobileJoystick Instance { get; private set; }

        [Header("Joystick Visual Components")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Settings")]
        [SerializeField] private float handleRange = 85f;
        [SerializeField] private bool allowKeyboardFallbackInEditor = true;

        private Vector2 inputVector = Vector2.zero;
        private Canvas parentCanvas;
        private bool isDragging = false;

        public bool IsDragging => isDragging;
        public float Horizontal => GetAxis(inputVector.x, "Horizontal");
        public float Vertical => GetAxis(inputVector.y, "Vertical");
        public Vector2 Direction => new Vector2(Horizontal, Vertical);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            parentCanvas = GetComponentInParent<Canvas>();
            if (background == null) background = transform as RectTransform;
            if (handle == null && transform.childCount > 0)
                handle = transform.GetChild(0) as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            isDragging = true;
            Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera) 
                ? parentCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, cam, out Vector2 localPoint))
            {
                inputVector = Vector2.ClampMagnitude(localPoint / handleRange, 1f);

                if (handle != null)
                {
                    handle.anchoredPosition = inputVector * handleRange;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            inputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        private float GetAxis(float joystickVal, string axisName)
        {
            if (Mathf.Abs(joystickVal) > 0.05f)
            {
                return joystickVal;
            }

            if (allowKeyboardFallbackInEditor)
            {
                return Input.GetAxisRaw(axisName);
            }

            return 0f;
        }
    }
}
