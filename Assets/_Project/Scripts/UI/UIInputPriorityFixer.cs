using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProjectSurvival.UI
{
    public class UIInputPriorityFixer : MonoBehaviour
    {
        public static UIInputPriorityFixer Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            FixInputPriorities();
        }

        public void FixInputPriorities()
        {
            Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Ensure GraphicRaycaster
            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            gr.enabled = true;

            // 2. Ensure EventSystem & InputModule
            if (EventSystem.current == null)
            {
                var existingEs = Object.FindFirstObjectByType<EventSystem>();
                if (existingEs == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<EventSystem>();
                    var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                    if (inputSystemModuleType != null)
                    {
                        esObj.AddComponent(inputSystemModuleType);
                    }
                    else
                    {
                        esObj.AddComponent<StandaloneInputModule>();
                    }
                }
            }

            // 3. TouchLookArea ordering (Must be SiblingIndex = 0 at the back of HUD)
            Transform touchLook = canvas.transform.Find("TouchLookArea");
            if (touchLook != null)
            {
                touchLook.SetSiblingIndex(0);
                Image img = touchLook.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }

            // 4. Audit & unblock all decorative full-screen overlays & HUD feedback
            Graphic[] allGraphics = canvas.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in allGraphics)
            {
                if (g == null) continue;

                string n = g.gameObject.name;

                // Buttons themselves must receive raycasts, but child text/icons must not
                Button parentBtn = g.GetComponentInParent<Button>();
                if (parentBtn != null)
                {
                    if (g.gameObject != parentBtn.gameObject)
                    {
                        g.raycastTarget = false;
                    }
                    else
                    {
                        g.raycastTarget = true;
                    }
                    continue;
                }

                // Decorative overlays that should NEVER block raycasts
                if (n == "DamageVignette" || n == "DamageFlash" || n == "DamageFlashOverlay" || n == "VignetteOverlay" ||
                    n == "StealthIndicator_Panel" || n == "PrimaryThreatArc" || n == "SecondaryThreatArc" ||
                    n == "Crosshair_Panel" || n.StartsWith("Tick_") || n == "HitMarker_Panel" ||
                    n == "Toast_Panel" || n == "ToastText" || n == "Interaction_Prompt" ||
                    n == "QuestTracker_Panel" || n == "CompassBar_Panel" || n == "CenterNotch" ||
                    n == "MarkersMask" || n == "CardinalMarkersParent" || n == "WaypointMarker" ||
                    n == "CrouchGlow")
                {
                    g.raycastTarget = false;
                    continue;
                }

                // Vital HUD Panel must not block touches
                Transform vitalTr = canvas.transform.Find("TacticalVitalHUD_Panel");
                if (vitalTr != null && (g.transform == vitalTr || g.transform.IsChildOf(vitalTr)))
                {
                    g.raycastTarget = false;
                    continue;
                }

                // QuickSlots background panel must not block; only the slot buttons should
                Transform qsTr = canvas.transform.Find("QuickSlots_Bar");
                if (qsTr != null && g.transform == qsTr)
                {
                    g.raycastTarget = false;
                    continue;
                }
            }

            // 5. Ensure Top-Right Pause button is responsive and top sibling
            Transform pauseBtnTr = canvas.transform.Find("Btn_Pause");
            if (pauseBtnTr != null)
            {
                pauseBtnTr.SetAsLastSibling();
                Button pb = pauseBtnTr.GetComponent<Button>();
                if (pb != null) pb.interactable = true;
                Image pImg = pauseBtnTr.GetComponent<Image>();
                if (pImg != null) pImg.raycastTarget = true;
                Transform icon = pauseBtnTr.Find("Icon");
                if (icon != null)
                {
                    Image iconImg = icon.GetComponent<Image>();
                    if (iconImg != null) iconImg.raycastTarget = false;
                }
            }

            // 6. Ensure QuickSlots buttons are responsive
            Transform qsBar = canvas.transform.Find("QuickSlots_Bar");
            if (qsBar != null)
            {
                Button[] qsBtns = qsBar.GetComponentsInChildren<Button>(true);
                foreach (var b in qsBtns)
                {
                    b.interactable = true;
                    Image bImg = b.GetComponent<Image>();
                    if (bImg != null) bImg.raycastTarget = true;
                    Graphic[] childGraphics = b.GetComponentsInChildren<Graphic>(true);
                    foreach (var cg in childGraphics)
                    {
                        if (cg.gameObject != b.gameObject) cg.raycastTarget = false;
                    }
                }
            }

            // 7. Ensure Weapon HUD card and mini buttons are responsive
            Transform wpHudTr = canvas.transform.Find("WeaponHUD_Panel");
            if (wpHudTr != null)
            {
                Button[] wpBtns = wpHudTr.GetComponentsInChildren<Button>(true);
                foreach (var b in wpBtns)
                {
                    b.interactable = true;
                    Image bImg = b.GetComponent<Image>();
                    if (bImg != null) bImg.raycastTarget = true;
                    Graphic[] childGraphics = b.GetComponentsInChildren<Graphic>(true);
                    foreach (var cg in childGraphics)
                    {
                        if (cg.gameObject != b.gameObject) cg.raycastTarget = false;
                    }
                }
            }

            // 8. Mobile Touch Hitbox Expansion (raycastPadding = -8px for buttons < 48px)
            ExpandSmallTouchHitboxes(canvas);
        }

        public static void ExpandSmallTouchHitboxes(Canvas canvas)
        {
            if (canvas == null) return;

            Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 size = rt.rect.size;
                    if (size.x > 0f && size.y > 0f && (size.x < 48f || size.y < 48f))
                    {
                        Graphic g = btn.targetGraphic != null ? btn.targetGraphic : btn.GetComponent<Graphic>();
                        if (g != null)
                        {
                            g.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                        }
                    }
                }
            }

            HoldTouchButton[] allHoldButtons = canvas.GetComponentsInChildren<HoldTouchButton>(true);
            foreach (var hBtn in allHoldButtons)
            {
                if (hBtn == null) continue;
                RectTransform rt = hBtn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 size = rt.rect.size;
                    if (size.x > 0f && size.y > 0f && (size.x < 48f || size.y < 48f))
                    {
                        Graphic g = hBtn.GetComponent<Graphic>();
                        if (g != null)
                        {
                            g.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                        }
                    }
                }
            }
        }
    }
}
