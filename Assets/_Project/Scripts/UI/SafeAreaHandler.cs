using UnityEngine;

namespace ProjectSurvival.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public static void RefreshAll()
        {
            var handlers = Object.FindObjectsByType<SafeAreaHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] != null) handlers[i].ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // If attached to root Canvas, distribute safe area anchors to full-screen child panels
            if (GetComponent<Canvas>() != null)
            {
                ApplySafeAreaToChildPanels(anchorMin, anchorMax);
                EnforceRightEdgeSafeArea(safeArea);
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            EnforceRightEdgeSafeArea(safeArea);
        }

        private void ApplySafeAreaToChildPanels(Vector2 anchorMin, Vector2 anchorMax)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child == null) continue;

                string n = child.gameObject.name.ToLower();
                bool isPanel = n.Contains("panel") || n.Contains("card") || n.Contains("hud") || n.Contains("menu") || n.Contains("dialogue") || n.Contains("summary");
                bool isFullScreen = (child.anchorMin == Vector2.zero && child.anchorMax == Vector2.one);

                if (isPanel && isFullScreen)
                {
                    child.anchorMin = anchorMin;
                    child.anchorMax = anchorMax;
                    child.offsetMin = Vector2.zero;
                    child.offsetMax = Vector2.zero;
                }
            }
        }

        public void EnforceRightEdgeSafeArea(Rect safeArea)
        {
            float rightInset = Screen.width - (safeArea.x + safeArea.width);
            float minRightMargin = Mathf.Max(24f, rightInset + 16f);

            // Audit and enforce right-edge margin for HUD widgets
            RectTransform[] allRects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < allRects.Length; i++)
            {
                RectTransform rt = allRects[i];
                if (rt == null || rt == rectTransform) continue;

                string n = rt.gameObject.name.ToLower();
                bool isTargetWidget = n.Contains("minimap") ||
                                      n.Contains("btn_pause") ||
                                      n.Contains("btn_backpack") ||
                                      n.Contains("contextualinteraction");

                if (isTargetWidget)
                {
                    // If anchored to right edge
                    if (rt.anchorMax.x >= 0.8f && rt.pivot.x >= 0.9f)
                    {
                        if (rt.anchoredPosition.x > -minRightMargin)
                        {
                            Vector2 pos = rt.anchoredPosition;
                            pos.x = -minRightMargin;
                            rt.anchoredPosition = pos;
                        }
                    }
                }
            }
        }
    }
}
