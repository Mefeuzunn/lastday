using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 2 (Step 2B): Ergonomic Mobile & Editor Control Overhaul Setup Script.
    /// Configures:
    /// - Zero-overlap action arc at bottom-right (Btn_Fire, Btn_Aim, Btn_JumpVault, Btn_Crouch, Btn_Sprint, Btn_PrimaryInteract, WeaponHUD_Panel)
    /// - Dedicated Multi-Touch Camera Look Pad behind action buttons (TouchCameraLookPad)
    /// - Left Virtual Joystick at (135, 135) with 156x156 footprint and 0.08 deadzone
    /// - Centered QuickSlots_Bar at (0, 56)
    /// - Strict raycast hygiene across all gameplay HUD elements
    /// </summary>
    public static class Step2B_ControlsSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Step 2B Controls Overhaul", false, 46)]
        public static void SetupStep2B()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Step2B_ControlsSetup] Starting Step 2B Controls Overhaul...");

            Canvas hudCanvas = null;
            Canvas[] allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i].name.Contains("HUD") || allCanvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    hudCanvas = allCanvases[i];
                    break;
                }
            }

            if (hudCanvas == null)
            {
                Debug.LogError("[Step2B_ControlsSetup] HUD Canvas not found in scene!");
                return;
            }

            Transform safeArea = hudCanvas.transform.Find("SafeArea") ?? hudCanvas.transform.Find("Safe_Area");
            Transform targetParent = safeArea != null ? safeArea : hudCanvas.transform;

            SetupTouchCameraLookPad(targetParent);
            SetupVirtualJoystick(targetParent);
            SetupQuickSlotsBar(targetParent);
            SetupRightHandActionArc(targetParent);
            SetupWeaponHUDWidget(targetParent);
            EnforceStrictRaycastHygiene(hudCanvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Step2B_ControlsSetup] Successfully completed Step 2B Controls Overhaul in Main_Prototype.unity!");

            Step2B_ControlsAudit.RunAudit();
        }

        private static void SetupTouchCameraLookPad(Transform parent)
        {
            Transform existing = parent.Find("TouchCameraLookPad");
            GameObject padObj = existing != null ? existing.gameObject : new GameObject("TouchCameraLookPad", typeof(RectTransform));
            padObj.transform.SetParent(parent, false);
            padObj.transform.SetSiblingIndex(1); // Ordered behind all buttons

            RectTransform rt = padObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.42f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = padObj.GetComponent<Image>();
            if (img == null) img = padObj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            var pad = padObj.GetComponent<TouchCameraLookPad>();
            if (pad == null) pad = padObj.AddComponent<TouchCameraLookPad>();

            padObj.SetActive(true);
            Debug.Log("[Step2B_ControlsSetup] Configured TouchCameraLookPad (anchorMin: 0.42, 0.0 to 1.0, 0.85, sibling index 1).");
        }

        private static void SetupVirtualJoystick(Transform parent)
        {
            Transform existing = parent.Find("Mobile_Joystick");
            GameObject joyObj = existing != null ? existing.gameObject : new GameObject("Mobile_Joystick", typeof(RectTransform));
            joyObj.transform.SetParent(parent, false);

            RectTransform rt = joyObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(135f, 135f);
            rt.sizeDelta = new Vector2(156f, 156f);

            Image bgImg = joyObj.GetComponent<Image>();
            if (bgImg == null) bgImg = joyObj.AddComponent<Image>();
            bgImg.raycastTarget = true;

            var joy = joyObj.GetComponent<MobileJoystick>();
            if (joy == null) joy = joyObj.AddComponent<MobileJoystick>();

            var vJoy = joyObj.GetComponent<VirtualJoystick>();
            if (vJoy == null) vJoy = joyObj.AddComponent<VirtualJoystick>();

            // Handle
            Transform handleTf = joyObj.transform.Find("Handle");
            GameObject handleObj = handleTf != null ? handleTf.gameObject : new GameObject("Handle", typeof(RectTransform));
            handleObj.transform.SetParent(joyObj.transform, false);

            RectTransform rtHandle = handleObj.GetComponent<RectTransform>();
            rtHandle.anchorMin = new Vector2(0.5f, 0.5f);
            rtHandle.anchorMax = new Vector2(0.5f, 0.5f);
            rtHandle.pivot = new Vector2(0.5f, 0.5f);
            rtHandle.anchoredPosition = Vector2.zero;
            rtHandle.sizeDelta = new Vector2(68f, 68f);

            Image handleImg = handleObj.GetComponent<Image>();
            if (handleImg == null) handleImg = handleObj.AddComponent<Image>();
            handleImg.raycastTarget = false; // Only background catches pointer drags

            joyObj.SetActive(true);
            Debug.Log("[Step2B_ControlsSetup] Configured VirtualJoystick at (135, 135) with 156x156px footprint.");
        }

        private static void SetupQuickSlotsBar(Transform parent)
        {
            Transform existing = parent.Find("QuickSlots_Bar");
            if (existing == null) return;

            GameObject qsObj = existing.gameObject;
            RectTransform rt = qsObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 56f);
            rt.sizeDelta = new Vector2(280f, 68f);

            Image bgImg = qsObj.GetComponent<Image>();
            if (bgImg != null) bgImg.raycastTarget = false;

            // Re-align 4 slots cleanly: 58x58px, 10px spacing
            // Positions: -102, -34, 34, 102
            float[] slotX = new float[] { -102f, -34f, 34f, 102f };
            int slotIdx = 0;
            for (int i = 0; i < qsObj.transform.childCount; i++)
            {
                Transform child = qsObj.transform.GetChild(i);
                if (child.name.StartsWith("Slot_") || child.GetComponent<Button>() != null)
                {
                    if (slotIdx < slotX.Length)
                    {
                        RectTransform rtSlot = child.GetComponent<RectTransform>();
                        if (rtSlot != null)
                        {
                            rtSlot.anchorMin = new Vector2(0.5f, 0.5f);
                            rtSlot.anchorMax = new Vector2(0.5f, 0.5f);
                            rtSlot.pivot = new Vector2(0.5f, 0.5f);
                            rtSlot.anchoredPosition = new Vector2(slotX[slotIdx], 0f);
                            rtSlot.sizeDelta = new Vector2(58f, 58f);
                        }

                        Image slotImg = child.GetComponent<Image>();
                        if (slotImg != null) slotImg.raycastTarget = true;

                        // All children inside slot have raycastTarget = false
                        var childGraphics = child.GetComponentsInChildren<Graphic>(true);
                        for (int g = 0; g < childGraphics.Length; g++)
                        {
                            if (childGraphics[g] != slotImg) childGraphics[g].raycastTarget = false;
                        }
                        slotIdx++;
                    }
                }
            }

            qsObj.SetActive(true);
            Debug.Log("[Step2B_ControlsSetup] Configured QuickSlots_Bar centered at (0, 56) with 58x58px slots.");
        }

        private static void SetupRightHandActionArc(Transform parent)
        {
            Transform actionPanel = parent.Find("Action_Buttons_Panel");
            GameObject actionObj = actionPanel != null ? actionPanel.gameObject : new GameObject("Action_Buttons_Panel", typeof(RectTransform));
            actionObj.transform.SetParent(parent, false);

            RectTransform rtAction = actionObj.GetComponent<RectTransform>();
            rtAction.anchorMin = new Vector2(1f, 0f);
            rtAction.anchorMax = new Vector2(1f, 0f);
            rtAction.pivot = new Vector2(1f, 0f);
            rtAction.anchoredPosition = Vector2.zero;
            rtAction.sizeDelta = new Vector2(440f, 380f);

            var actionHUD = actionObj.GetComponent<ActionClusterHUD>();
            if (actionHUD == null) actionHUD = actionObj.AddComponent<ActionClusterHUD>();

            // 1. Btn_Fire: (-95, 95), 86x86, Crimson
            ConfigureActionButton(actionObj.transform, "Btn_Fire", "ATEŞ",
                new Vector2(-95f, 95f), new Vector2(86f, 86f),
                new Color(0.92f, 0.20f, 0.22f, 0.95f), new Color(1.0f, 0.45f, 0.45f, 0.90f));

            // 2. Btn_Aim: (-202, 88), 68x68, Blue
            ConfigureActionButton(actionObj.transform, "Btn_Aim", "NİŞAN",
                new Vector2(-202f, 88f), new Vector2(68f, 68f),
                new Color(0.15f, 0.55f, 0.92f, 0.95f), new Color(0.35f, 0.75f, 1.0f, 0.85f));

            // 3. Btn_JumpVault: (-92, 202), 68x68, Emerald/Teal
            Transform existingJump = parent.Find("Btn_JumpVault") ?? actionObj.transform.Find("Btn_JumpVault");
            GameObject jumpObj = existingJump != null ? existingJump.gameObject : new GameObject("Btn_JumpVault", typeof(RectTransform));
            jumpObj.transform.SetParent(parent, false); // Direct child of SafeArea / Canvas
            RectTransform rtJump = jumpObj.GetComponent<RectTransform>();
            rtJump.anchorMin = new Vector2(1f, 0f);
            rtJump.anchorMax = new Vector2(1f, 0f);
            rtJump.pivot = new Vector2(0.5f, 0.5f);
            rtJump.anchoredPosition = new Vector2(-92f, 202f);
            rtJump.sizeDelta = new Vector2(68f, 68f);

            Image jumpImg = jumpObj.GetComponent<Image>();
            if (jumpImg == null) jumpImg = jumpObj.AddComponent<Image>();
            jumpImg.color = new Color(0.12f, 0.78f, 0.60f, 0.95f);
            jumpImg.raycastTarget = true;

            Outline jumpOutline = jumpObj.GetComponent<Outline>();
            if (jumpOutline == null) jumpOutline = jumpObj.AddComponent<Outline>();
            jumpOutline.effectColor = new Color(0.25f, 0.95f, 0.75f, 0.85f);
            jumpOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var jvBtn = jumpObj.GetComponent<Button>();
            if (jvBtn == null) jvBtn = jumpObj.AddComponent<Button>();

            var jvScript = jumpObj.GetComponent<JumpVaultHUDButton>();
            if (jvScript == null) jvScript = jumpObj.AddComponent<JumpVaultHUDButton>();

            EnsureButtonLabel(jumpObj.transform, "ZIPLA / ATLA", 11);
            jumpObj.SetActive(true);

            // 4. Btn_Crouch: (-188, 184), 62x62, Slate Blue
            ConfigureActionButton(actionObj.transform, "Btn_Crouch", "EĞİL",
                new Vector2(-188f, 184f), new Vector2(62f, 62f),
                new Color(0.22f, 0.50f, 0.75f, 0.95f), new Color(0.35f, 0.75f, 1.0f, 0.75f));

            // 5. Btn_Sprint: (-274, 152), 62x62, Amber
            ConfigureActionButton(actionObj.transform, "Btn_Sprint", "KOŞ",
                new Vector2(-274f, 152f), new Vector2(62f, 62f),
                new Color(0.95f, 0.65f, 0.15f, 0.95f), new Color(1.0f, 0.80f, 0.25f, 0.85f));

            // 6. Btn_PrimaryInteract: (-145, 286), 196x54, Contextual Pill
            Transform existingInteract = parent.Find("Btn_PrimaryInteract");
            GameObject interactObj = existingInteract != null ? existingInteract.gameObject : new GameObject("Btn_PrimaryInteract", typeof(RectTransform));
            interactObj.transform.SetParent(parent, false);

            RectTransform rtInteract = interactObj.GetComponent<RectTransform>();
            rtInteract.anchorMin = new Vector2(1f, 0f);
            rtInteract.anchorMax = new Vector2(1f, 0f);
            rtInteract.pivot = new Vector2(0.5f, 0.5f);
            rtInteract.anchoredPosition = new Vector2(-145f, 286f);
            rtInteract.sizeDelta = new Vector2(196f, 54f);

            Image interactImg = interactObj.GetComponent<Image>();
            if (interactImg == null) interactImg = interactObj.AddComponent<Image>();
            interactImg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            interactImg.raycastTarget = true;

            Outline interactOutline = interactObj.GetComponent<Outline>();
            if (interactOutline == null) interactOutline = interactObj.AddComponent<Outline>();
            interactOutline.effectColor = new Color(1.0f, 0.72f, 0.01f, 0.85f);
            interactOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var interactBtn = interactObj.GetComponent<Button>();
            if (interactBtn == null) interactBtn = interactObj.AddComponent<Button>();

            EnsureButtonLabel(interactObj.transform, "AL / ETKİLEŞ", 12);
            interactObj.SetActive(true);

            actionObj.SetActive(true);
            Debug.Log("[Step2B_ControlsSetup] Configured Action Arc (Fire, Aim, Jump, Crouch, Sprint, PrimaryInteract).");
        }

        private static void SetupWeaponHUDWidget(Transform parent)
        {
            Transform existing = parent.Find("WeaponHUD_Panel");
            if (existing == null) return;

            GameObject wpHudObj = existing.gameObject;
            RectTransform rt = wpHudObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-145f, 360f);
            rt.sizeDelta = new Vector2(200f, 64f);

            Image bg = wpHudObj.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;

            // Ensure Btn_Reload mini button
            Transform reloadTf = wpHudObj.transform.Find("Btn_Reload");
            GameObject reloadObj = reloadTf != null ? reloadTf.gameObject : new GameObject("Btn_Reload", typeof(RectTransform));
            reloadObj.transform.SetParent(wpHudObj.transform, false);

            RectTransform rtReload = reloadObj.GetComponent<RectTransform>();
            rtReload.anchorMin = new Vector2(0f, 0.5f);
            rtReload.anchorMax = new Vector2(0f, 0.5f);
            rtReload.pivot = new Vector2(0.5f, 0.5f);
            rtReload.anchoredPosition = new Vector2(-24f, 0f);
            rtReload.sizeDelta = new Vector2(36f, 36f);

            Image reloadImg = reloadObj.GetComponent<Image>();
            if (reloadImg == null) reloadImg = reloadObj.AddComponent<Image>();
            reloadImg.color = new Color(0.14f, 0.18f, 0.24f, 0.95f);
            reloadImg.raycastTarget = true;

            var rBtn = reloadObj.GetComponent<Button>();
            if (rBtn == null) rBtn = reloadObj.AddComponent<Button>();
            EnsureButtonLabel(reloadObj.transform, "R", 11);
            reloadObj.SetActive(true);

            // Ensure Btn_Flashlight mini button
            Transform flashTf = wpHudObj.transform.Find("Btn_Flashlight");
            GameObject flashObj = flashTf != null ? flashTf.gameObject : new GameObject("Btn_Flashlight", typeof(RectTransform));
            flashObj.transform.SetParent(wpHudObj.transform, false);

            RectTransform rtFlash = flashObj.GetComponent<RectTransform>();
            rtFlash.anchorMin = new Vector2(0f, 0.5f);
            rtFlash.anchorMax = new Vector2(0f, 0.5f);
            rtFlash.pivot = new Vector2(0.5f, 0.5f);
            rtFlash.anchoredPosition = new Vector2(-66f, 0f);
            rtFlash.sizeDelta = new Vector2(36f, 36f);

            Image flashImg = flashObj.GetComponent<Image>();
            if (flashImg == null) flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(0.14f, 0.18f, 0.24f, 0.95f);
            flashImg.raycastTarget = true;

            var fBtn = flashObj.GetComponent<Button>();
            if (fBtn == null) fBtn = flashObj.AddComponent<Button>();
            EnsureButtonLabel(flashObj.transform, "F", 11);
            flashObj.SetActive(true);

            wpHudObj.SetActive(true);
            Debug.Log("[Step2B_ControlsSetup] Configured WeaponHUD_Panel at (-145, 360) with Btn_Reload and Btn_Flashlight.");
        }

        private static GameObject ConfigureActionButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, Color bgColor, Color outlineColor)
        {
            Transform existing = parent.Find(name);
            GameObject btnObj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            Outline outline = btnObj.GetComponent<Outline>();
            if (outline == null) outline = btnObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            EnsureButtonLabel(btnObj.transform, label, 12);
            btnObj.SetActive(true);
            return btnObj;
        }

        private static void EnsureButtonLabel(Transform parent, string text, int fontSize)
        {
            Transform labelTf = parent.Find("Label_Text") ?? parent.Find("Text") ?? parent.Find("Badge_Text");
            GameObject labelObj = labelTf != null ? labelTf.gameObject : new GameObject("Label_Text", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);

            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Text txt = labelObj.GetComponent<Text>();
            if (txt == null) txt = labelObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = text;
            txt.raycastTarget = false; // Strictly enforce raycastTarget = false on child text
        }

        private static void EnforceStrictRaycastHygiene(Transform root)
        {
            Graphic[] allGraphics = root.GetComponentsInChildren<Graphic>(true);
            int disabledCount = 0;

            for (int i = 0; i < allGraphics.Length; i++)
            {
                Graphic g = allGraphics[i];
                if (g == null) continue;

                // Check if this graphic is a child inside a Button or panel but not the interactive root image
                Button parentBtn = g.GetComponentInParent<Button>();
                if (parentBtn != null)
                {
                    // If this graphic is NOT the button's own targetGraphic, it must NEVER block raycasts!
                    if (g != parentBtn.targetGraphic && g.gameObject != parentBtn.gameObject)
                    {
                        if (g.raycastTarget)
                        {
                            g.raycastTarget = false;
                            disabledCount++;
                        }
                    }
                }
                else
                {
                    // If graphic is text or decorative outline without button, raycastTarget should be false
                    if (g is Text && g.raycastTarget)
                    {
                        g.raycastTarget = false;
                        disabledCount++;
                    }
                }
            }

            Debug.Log($"[Step2B_ControlsSetup] Enforced strict raycast hygiene: disabled {disabledCount} blocker graphics.");
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class Step2B_ControlsAutoSetup
    {
        private const string ExecutedPrefKey = "Phase2_Step2B_Controls_Setup_v1";

        static Step2B_ControlsAutoSetup()
        {
            EditorApplication.delayCall += CheckAndSetup;
        }

        private static void CheckAndSetup()
        {
            if (SessionState.GetBool(ExecutedPrefKey, false)) return;
            SessionState.SetBool(ExecutedPrefKey, true);

            Debug.Log("[Step2B_ControlsAutoSetup] Automatically executing Step 2B Controls Overhaul...");
            try
            {
                Step2B_ControlsSetup.SetupStep2B();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Step2B_ControlsAutoSetup] Controls setup failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
#endif
}
