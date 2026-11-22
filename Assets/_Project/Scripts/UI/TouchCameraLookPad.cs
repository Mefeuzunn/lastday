using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 2 (Step 2B): Dedicated Multi-Touch Camera Look Pad.
    /// Positioned behind the bottom-right action buttons (anchorMin 0.42, 0.0 to anchorMax 1.0, 0.85).
    /// Transparent with raycastTarget = true, tracking exact pointerId drags so
    /// players can simultaneously steer with the left joystick and rotate camera 360 degrees.
    /// Supports right-mouse drag / mouse look in Unity Editor.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TouchCameraLookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static TouchCameraLookPad Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float touchSensitivityMultiplier = 1.0f;
        [SerializeField] private bool allowEditorMouseLook = true;

        private int activePointerId = -1;
        private Vector2 lastPosition = Vector2.zero;
        private bool isDragging = false;
        private Image padImage;

        public bool IsDragging => isDragging;
        public int ActivePointerId => activePointerId;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            padImage = GetComponent<Image>();
            if (padImage != null)
            {
                // Must be transparent and raycastTarget = true
                padImage.color = new Color(0f, 0f, 0f, 0f);
                padImage.raycastTarget = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId == -1)
            {
                activePointerId = eventData.pointerId;
                lastPosition = eventData.position;
                isDragging = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                Vector2 currentPos = eventData.position;
                Vector2 delta = currentPos - lastPosition;
                lastPosition = currentPos;

                if (delta.sqrMagnitude > 0.0001f)
                {
                    ThirdPersonCamera.Instance?.ApplyTouchLookDelta(delta * touchSensitivityMultiplier);
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                activePointerId = -1;
                isDragging = false;
            }
        }

        private void Update()
        {
            // Reset if pointer is lost
            if (isDragging && activePointerId >= 0 && Input.touchCount == 0 && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                activePointerId = -1;
                isDragging = false;
            }
        }
    }
}
