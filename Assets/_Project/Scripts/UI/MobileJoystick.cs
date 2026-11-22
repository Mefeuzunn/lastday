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
        [SerializeField] private float handleRange = 78f;
        [SerializeField] private float deadZone = 0.08f;
        [SerializeField] private bool allowKeyboardFallbackInEditor = true;

        private Vector2 inputVector = Vector2.zero;
        private Canvas parentCanvas;
        private bool isDragging = false;

        public bool IsDragging => isDragging;
        public float DeadZone => deadZone;
        public float Horizontal => Direction.x;
        public float Vertical => Direction.y;

        public Vector2 Direction
        {
            get
            {
                if (isDragging)
                {
                    float rawMag = inputVector.magnitude;
                    if (rawMag < deadZone) return Vector2.zero;

                    float normalized = Mathf.Clamp01((rawMag - deadZone) / (1f - deadZone));
                    // Smooth ergonomic analog response curve: quadratic blend for fine control and snappy sprint
                    float curved = (0.7f * normalized * normalized) + (0.3f * normalized);
                    return inputVector.normalized * curved;
                }

                if (allowKeyboardFallbackInEditor)
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    Vector2 kDir = new Vector2(h, v);
                    if (kDir.sqrMagnitude > 1f) kDir.Normalize();
                    return kDir;
                }

                return Vector2.zero;
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
    }
}
