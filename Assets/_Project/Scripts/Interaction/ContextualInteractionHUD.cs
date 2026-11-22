using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Contextual Multi-Verb Action Card HUD.
    /// Replaces the single generic interaction button with a diegetic, mobile-friendly action stack:
    /// - Displays object title & state subtitle.
    /// - Up to 4 contextual action verb buttons with icons, Turkish labels, required item badges,
    ///   and smooth progress fill bars for physical timed actions.
    /// - Full PC keyboard (E, 1, 2, 3, 4) and mobile direct touch/click support.
    /// - Non-blocking input (raycastTarget = false on frames/text/icons, true on buttons only).
    /// </summary>
    public class ContextualInteractionHUD : MonoBehaviour
    {
        public static ContextualInteractionHUD Instance { get; private set; }

        [Header("Root & Panels")]
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text headerSubtitleText;
        [SerializeField] private Image accentLine;
        [SerializeField] private Transform buttonsParent;

        [Header("Master Step A: Primary Interaction Button")]
        [SerializeField] private GameObject primaryButtonObj;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryLabelText;
        [SerializeField] private Text primaryKeybindText;
        [SerializeField] private Image primaryProgressFill;

        public bool HasActiveTarget => currentTarget != null;
        public GameObject PrimaryInteractButton => primaryButtonObj;
        public void TriggerPrimaryVerb() => StartHoldOrExecute(0);
        public void OnPrimaryPointerDown() => OnButtonPointerDown(0);
        public void OnPrimaryPointerUp() => OnButtonPointerUp(0);

        private WorldInteractableObject currentTarget;
        private IReadOnlyList<InteractionVerb> activeVerbs;
        private GameObject playerRef;

        // Up to 4 Verb Button Widgets
        private class VerbButtonWidget
        {
            public GameObject root;
            public Button button;
            public Image background;
            public Text shortcutBadge;
            public Text labelText;
            public Text requirementText;
            public Image progressFill;
            public InteractionVerb boundVerb;
            public bool isPointerHeld;
        }

        private readonly List<VerbButtonWidget> buttonWidgets = new List<VerbButtonWidget>();
        private int activeExecutingIndex = -1;
        private float holdProgress = 0f;
        private float activeHoldDuration = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCardHierarchy();
        }

        private void Start()
        {
            playerRef = GameObject.FindWithTag("Player");
            SetTarget(null);
        }

        private void EnsureCardHierarchy()
        {
            if (cardRoot == null) cardRoot = GetComponent<RectTransform>();
            if (cardRoot == null)
            {
                cardRoot = gameObject.AddComponent<RectTransform>();
                cardRoot.anchorMin = new Vector2(1f, 0.5f);
                cardRoot.anchorMax = new Vector2(1f, 0.5f);
                cardRoot.pivot = new Vector2(1f, 0.5f);
                cardRoot.anchoredPosition = new Vector2(-28f, 35f);
                cardRoot.sizeDelta = new Vector2(265f, 215f);
            }

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Background Card
            Transform bgTrans = transform.Find("Card_Background");
            if (bgTrans == null)
            {
                GameObject bgObj = new GameObject("Card_Background");
                bgObj.transform.SetParent(transform, false);
                RectTransform rtBg = bgObj.AddComponent<RectTransform>();
                rtBg.anchorMin = Vector2.zero;
                rtBg.anchorMax = Vector2.one;
                rtBg.sizeDelta = Vector2.zero;
                Image bgImg = bgObj.AddComponent<Image>();
                bgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.94f); // Sleek dark glass
                bgImg.raycastTarget = false;
            }

            // Accent Left/Top Line
            Transform lineTrans = transform.Find("Accent_Line");
            if (lineTrans == null)
            {
                GameObject lineObj = new GameObject("Accent_Line");
                lineObj.transform.SetParent(transform, false);
                RectTransform rtLine = lineObj.AddComponent<RectTransform>();
                rtLine.anchorMin = new Vector2(0f, 0f);
                rtLine.anchorMax = new Vector2(0f, 1f);
                rtLine.pivot = new Vector2(0f, 0.5f);
                rtLine.sizeDelta = new Vector2(3.5f, 0f);
                accentLine = lineObj.AddComponent<Image>();
                accentLine.color = new Color(1.0f, 0.82f, 0.4f, 0.95f); // Warm amber accent
                accentLine.raycastTarget = false;
            }

            // Header Container
            Transform headerTrans = transform.Find("Header_Container");
            if (headerTrans == null)
            {
                GameObject headObj = new GameObject("Header_Container");
                headObj.transform.SetParent(transform, false);
                RectTransform rtHead = headObj.AddComponent<RectTransform>();
                rtHead.anchorMin = new Vector2(0f, 1f);
                rtHead.anchorMax = new Vector2(1f, 1f);
                rtHead.pivot = new Vector2(0.5f, 1f);
                rtHead.anchoredPosition = new Vector2(0f, -6f);
                rtHead.sizeDelta = new Vector2(-20f, 40f);

                // Title
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(headObj.transform, false);
                RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
                rtTitle.anchorMin = new Vector2(0f, 0.5f);
                rtTitle.anchorMax = new Vector2(1f, 1f);
                rtTitle.sizeDelta = Vector2.zero;
                headerTitleText = titleObj.AddComponent<Text>();
                headerTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                headerTitleText.fontSize = 12;
                headerTitleText.fontStyle = FontStyle.Bold;
                headerTitleText.color = Color.white;
                headerTitleText.alignment = TextAnchor.MiddleLeft;
                headerTitleText.raycastTarget = false;

                // Subtitle
                GameObject subObj = new GameObject("Subtitle");
                subObj.transform.SetParent(headObj.transform, false);
                RectTransform rtSub = subObj.AddComponent<RectTransform>();
                rtSub.anchorMin = new Vector2(0f, 0f);
                rtSub.anchorMax = new Vector2(1f, 0.5f);
                rtSub.sizeDelta = Vector2.zero;
                headerSubtitleText = subObj.AddComponent<Text>();
                headerSubtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                headerSubtitleText.fontSize = 9;
                headerSubtitleText.color = new Color(0.8f, 0.84f, 0.9f, 0.85f);
                headerSubtitleText.alignment = TextAnchor.MiddleLeft;
                headerSubtitleText.raycastTarget = false;
            }

            // Buttons Parent
            if (buttonsParent == null)
            {
                Transform bp = transform.Find("Buttons_Stack");
                if (bp != null) buttonsParent = bp;
                else
                {
                    GameObject bpObj = new GameObject("Buttons_Stack");
                    bpObj.transform.SetParent(transform, false);
                    RectTransform rtBp = bpObj.AddComponent<RectTransform>();
                    rtBp.anchorMin = new Vector2(0f, 0f);
                    rtBp.anchorMax = new Vector2(1f, 1f);
                    rtBp.pivot = new Vector2(0.5f, 0.5f);
                    rtBp.anchoredPosition = new Vector2(0f, -22f);
                    rtBp.sizeDelta = new Vector2(-16f, -54f);
                    buttonsParent = bpObj.transform;
                }
            }

            // Preallocate 4 Verb Button Widgets
            if (buttonWidgets.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    buttonWidgets.Add(CreateVerbButtonWidget(i));
                }
            }

            EnsurePrimaryInteractButton();
        }

        public void EnsurePrimaryInteractButton()
        {
            if (primaryButtonObj != null) return;

            Transform parentCanvas = transform.parent != null ? transform.parent : transform;
            Transform safeArea = parentCanvas.Find("SafeArea") ?? parentCanvas.Find("Safe_Area");
            Transform targetParent = safeArea != null ? safeArea : parentCanvas;

            Transform existing = targetParent.Find("Btn_PrimaryInteract");
            if (existing != null)
            {
                primaryButtonObj = existing.gameObject;
                primaryButton = primaryButtonObj.GetComponent<Button>();
                if (primaryButton != null)
                {
                    primaryButton.onClick.RemoveAllListeners();
                    primaryButton.onClick.AddListener(() => OnButtonClick(0));
                }

                EventTrigger existingTrigger = primaryButtonObj.GetComponent<EventTrigger>();
                if (existingTrigger == null) existingTrigger = primaryButtonObj.AddComponent<EventTrigger>();
                existingTrigger.triggers.Clear();

                var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((d) => OnButtonPointerDown(0));
                existingTrigger.triggers.Add(entryDown);

                var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((d) => OnButtonPointerUp(0));
                existingTrigger.triggers.Add(entryUp);

                Image existingBg = primaryButtonObj.GetComponent<Image>();
                if (existingBg != null) existingBg.raycastTarget = true;

                primaryProgressFill = primaryButtonObj.transform.Find("ProgressFill")?.GetComponent<Image>();
                if (primaryProgressFill != null) primaryProgressFill.raycastTarget = false;

                primaryLabelText = primaryButtonObj.transform.Find("Label_Text")?.GetComponent<Text>();
                if (primaryLabelText != null) primaryLabelText.raycastTarget = false;

                primaryKeybindText = primaryButtonObj.transform.Find("Keybind_Badge")?.GetComponent<Text>();
                if (primaryKeybindText != null) primaryKeybindText.raycastTarget = false;
                return;
            }

            GameObject btnObj = new GameObject("Btn_PrimaryInteract", typeof(RectTransform));
            btnObj.transform.SetParent(targetParent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-56f, 265f);
            rt.sizeDelta = new Vector2(184f, 60f);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            bg.raycastTarget = true; // Crucial: ONLY root image has raycastTarget = true

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.72f, 0.01f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            primaryButton = btnObj.AddComponent<Button>();
            ColorBlock cb = primaryButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            primaryButton.colors = cb;
            primaryButton.onClick.AddListener(() => OnButtonClick(0));

            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            var pDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pDown.callback.AddListener((d) => OnButtonPointerDown(0));
            trigger.triggers.Add(pDown);

            var pUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pUp.callback.AddListener((d) => OnButtonPointerUp(0));
            trigger.triggers.Add(pUp);

            GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform));
            fillObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtFill = fillObj.GetComponent<RectTransform>();
            rtFill.anchorMin = new Vector2(0f, 0f);
            rtFill.anchorMax = new Vector2(0f, 1f);
            rtFill.pivot = new Vector2(0f, 0.5f);
            rtFill.sizeDelta = new Vector2(0f, 0f);
            primaryProgressFill = fillObj.AddComponent<Image>();
            primaryProgressFill.color = new Color(1.0f, 0.75f, 0.1f, 0.45f);
            primaryProgressFill.raycastTarget = false;

            GameObject badgeObj = new GameObject("Keybind_Badge", typeof(RectTransform));
            badgeObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
            rtBadge.anchorMin = new Vector2(0f, 0f);
            rtBadge.anchorMax = new Vector2(0f, 1f);
            rtBadge.pivot = new Vector2(0f, 0.5f);
            rtBadge.anchoredPosition = new Vector2(8f, 0f);
            rtBadge.sizeDelta = new Vector2(28f, 0f);
            primaryKeybindText = badgeObj.AddComponent<Text>();
            primaryKeybindText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            primaryKeybindText.fontSize = 12;
            primaryKeybindText.fontStyle = FontStyle.Bold;
            primaryKeybindText.alignment = TextAnchor.MiddleCenter;
            primaryKeybindText.color = new Color(1.0f, 0.82f, 0.35f, 0.95f);
            primaryKeybindText.raycastTarget = false;
            primaryKeybindText.text = "[E]";

            GameObject labelObj = new GameObject("Label_Text", typeof(RectTransform));
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtLabel = labelObj.GetComponent<RectTransform>();
            rtLabel.anchorMin = new Vector2(0f, 0f);
            rtLabel.anchorMax = new Vector2(1f, 1f);
            rtLabel.pivot = new Vector2(0.5f, 0.5f);
            rtLabel.anchoredPosition = new Vector2(14f, 0f);
            rtLabel.sizeDelta = new Vector2(-40f, 0f);
            primaryLabelText = labelObj.AddComponent<Text>();
            primaryLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            primaryLabelText.fontSize = 13;
            primaryLabelText.fontStyle = FontStyle.Bold;
            primaryLabelText.alignment = TextAnchor.MiddleCenter;
            primaryLabelText.color = Color.white;
            primaryLabelText.raycastTarget = false;
            primaryLabelText.text = "AL / ETKİLEŞ";

            primaryButtonObj = btnObj;
            primaryButtonObj.SetActive(false);
        }

        private VerbButtonWidget CreateVerbButtonWidget(int index)
        {
            GameObject btnObj = new GameObject($"VerbButton_{index}");
            btnObj.transform.SetParent(buttonsParent, false);

            RectTransform rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0f, 1f);
            rtBtn.anchorMax = new Vector2(1f, 1f);
            rtBtn.pivot = new Vector2(0.5f, 1f);
            rtBtn.anchoredPosition = new Vector2(0f, -(index * 38f));
            rtBtn.sizeDelta = new Vector2(0f, 34f);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.17f, 0.22f, 0.95f);
            bg.raycastTarget = true; // Button detects touch/clicks

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            // Progress Fill Bar (Horizontal)
            GameObject fillObj = new GameObject("ProgressFill");
            fillObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = new Vector2(0f, 0f);
            rtFill.anchorMax = new Vector2(0f, 1f);
            rtFill.pivot = new Vector2(0f, 0.5f);
            rtFill.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(1.0f, 0.82f, 0.4f, 0.45f); // Semi-transparent amber fill
            fillImg.raycastTarget = false;

            // Shortcut Badge: [E] or [1], [2], [3], [4]
            GameObject badgeObj = new GameObject("ShortcutBadge");
            badgeObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtBadge = badgeObj.AddComponent<RectTransform>();
            rtBadge.anchorMin = new Vector2(0f, 0f);
            rtBadge.anchorMax = new Vector2(0f, 1f);
            rtBadge.pivot = new Vector2(0f, 0.5f);
            rtBadge.anchoredPosition = new Vector2(6f, 0f);
            rtBadge.sizeDelta = new Vector2(24f, 0f);
            Text badgeTxt = badgeObj.AddComponent<Text>();
            badgeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            badgeTxt.fontSize = 10;
            badgeTxt.fontStyle = FontStyle.Bold;
            badgeTxt.color = new Color(1f, 0.85f, 0.35f, 0.95f);
            badgeTxt.alignment = TextAnchor.MiddleCenter;
            badgeTxt.raycastTarget = false;

            // Verb Label Text
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtLabel = labelObj.AddComponent<RectTransform>();
            rtLabel.anchorMin = new Vector2(0f, 0f);
            rtLabel.anchorMax = new Vector2(1f, 1f);
            rtLabel.pivot = new Vector2(0f, 0.5f);
            rtLabel.anchoredPosition = new Vector2(34f, 0f);
            rtLabel.sizeDelta = new Vector2(-36f, 0f);
            Text labelTxt = labelObj.AddComponent<Text>();
            labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            labelTxt.fontSize = 11;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.color = Color.white;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.raycastTarget = false;

            // Requirement Text (e.g. "Levye Gerekli")
            GameObject reqObj = new GameObject("Requirement");
            reqObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtReq = reqObj.AddComponent<RectTransform>();
            rtReq.anchorMin = new Vector2(0.5f, 0f);
            rtReq.anchorMax = new Vector2(1f, 1f);
            rtReq.pivot = new Vector2(1f, 0.5f);
            rtReq.anchoredPosition = new Vector2(-6f, 0f);
            rtReq.sizeDelta = new Vector2(0f, 0f);
            Text reqTxt = reqObj.AddComponent<Text>();
            reqTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            reqTxt.fontSize = 9;
            reqTxt.alignment = TextAnchor.MiddleRight;
            reqTxt.raycastTarget = false;

            VerbButtonWidget widget = new VerbButtonWidget
            {
                root = btnObj,
                button = btn,
                background = bg,
                shortcutBadge = badgeTxt,
                labelText = labelTxt,
                requirementText = reqTxt,
                progressFill = fillImg
            };

            // Hook EventTrigger for hold interactions
            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            int capturedIdx = index;

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { OnButtonPointerDown(capturedIdx); });
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { OnButtonPointerUp(capturedIdx); });
            trigger.triggers.Add(pointerUp);

            btn.onClick.AddListener(() => { OnButtonClick(capturedIdx); });

            btnObj.SetActive(false);
            return widget;
        }

        public void SetTarget(WorldInteractableObject target)
        {
            if (currentTarget == target) return;

            CancelActiveHold();
            currentTarget = target;

            if (primaryButtonObj == null) EnsurePrimaryInteractButton();
            if (primaryButtonObj != null)
            {
                primaryButtonObj.SetActive(currentTarget != null);
                if (currentTarget != null)
                {
                    WeaponPickupInteractable weapon = currentTarget as WeaponPickupInteractable;
                    if (weapon != null)
                    {
                        if (primaryLabelText != null) primaryLabelText.text = $"AL: {weapon.PickupDisplayName.ToUpper()}";
                    }
                    else
                    {
                        var verbs = currentTarget.GetAvailableVerbs();
                        string lbl = (verbs != null && verbs.Count > 0) ? verbs[0].displayLabel.ToUpper() : "ETKİLEŞ";
                        if (primaryLabelText != null) primaryLabelText.text = lbl;
                    }
                }
            }

            if (currentTarget == null)
            {
                activeVerbs = null;
                if (Application.isPlaying) StartCoroutine(FadeCard(0f));
                else if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
            else
            {
                RefreshVerbs();
                if (Application.isPlaying) StartCoroutine(FadeCard(1f));
                else if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
        }

        public void RefreshVerbs()
        {
            if (currentTarget == null) return;

            headerTitleText.text = currentTarget.ObjectTitle.ToUpper();
            headerSubtitleText.text = currentTarget.ObjectSubtitle;

            activeVerbs = currentTarget.GetAvailableVerbs();
            int count = activeVerbs != null ? activeVerbs.Count : 0;

            for (int i = 0; i < buttonWidgets.Count; i++)
            {
                if (i < count)
                {
                    InteractionVerb v = activeVerbs[i];
                    VerbButtonWidget w = buttonWidgets[i];
                    w.boundVerb = v;
                    w.root.SetActive(true);

                    w.shortcutBadge.text = i == 0 ? "[E]" : $"[{i + 1}]";
                    w.labelText.text = v.displayLabel.ToUpper();

                    // Check requirement
                    bool hasRequiredItem = true;
                    if (!string.IsNullOrEmpty(v.requiredItemId))
                    {
                        hasRequiredItem = InventorySystem.Instance != null && InventorySystem.Instance.HasItem(v.requiredItemId);
                        string itemName = !string.IsNullOrEmpty(v.requiredItemName) ? v.requiredItemName : v.requiredItemId;
                        w.requirementText.text = itemName;
                        w.requirementText.color = hasRequiredItem ? new Color(0.4f, 0.95f, 0.5f) : new Color(1f, 0.35f, 0.35f);
                    }
                    else
                    {
                        w.requirementText.text = string.Empty;
                    }

                    bool canInteract = v.isAvailable && hasRequiredItem;
                    w.button.interactable = canInteract;
                    w.background.color = canInteract ? new Color(0.14f, 0.17f, 0.22f, 0.95f) : new Color(0.10f, 0.11f, 0.13f, 0.65f);
                    w.progressFill.rectTransform.sizeDelta = new Vector2(0f, 0f);
                }
                else
                {
                    buttonWidgets[i].root.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (currentTarget == null || activeVerbs == null || activeVerbs.Count == 0) return;

            HandleKeyboardShortcuts();
            UpdateHoldProgress();
        }

        private void HandleKeyboardShortcuts()
        {
            // Primary verb: E key
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartHoldOrExecute(0);
            }
            else if (Input.GetKeyUp(KeyCode.E) && activeExecutingIndex == 0)
            {
                CancelActiveHold();
            }

            // Specific verbs: 1, 2, 3, 4 keys
            if (Input.GetKeyDown(KeyCode.Alpha1)) StartHoldOrExecute(0);
            else if (Input.GetKeyUp(KeyCode.Alpha1) && activeExecutingIndex == 0) CancelActiveHold();

            if (Input.GetKeyDown(KeyCode.Alpha2)) StartHoldOrExecute(1);
            else if (Input.GetKeyUp(KeyCode.Alpha2) && activeExecutingIndex == 1) CancelActiveHold();

            if (Input.GetKeyDown(KeyCode.Alpha3)) StartHoldOrExecute(2);
            else if (Input.GetKeyUp(KeyCode.Alpha3) && activeExecutingIndex == 2) CancelActiveHold();

            if (Input.GetKeyDown(KeyCode.Alpha4)) StartHoldOrExecute(3);
            else if (Input.GetKeyUp(KeyCode.Alpha4) && activeExecutingIndex == 3) CancelActiveHold();
        }

        private void OnButtonPointerDown(int index)
        {
            if (index < 0 || index >= buttonWidgets.Count) return;
            buttonWidgets[index].isPointerHeld = true;
            StartHoldOrExecute(index);
        }

        private void OnButtonPointerUp(int index)
        {
            if (index < 0 || index >= buttonWidgets.Count) return;
            buttonWidgets[index].isPointerHeld = false;
            if (activeExecutingIndex == index)
            {
                CancelActiveHold();
            }
        }

        private void OnButtonClick(int index)
        {
            if (index < 0 || activeVerbs == null || index >= activeVerbs.Count) return;
            InteractionVerb verb = activeVerbs[index];
            if (verb.holdDuration <= 0f)
            {
                ExecuteVerbAction(verb);
            }
        }

        private void StartHoldOrExecute(int index)
        {
            if (activeVerbs == null || index < 0 || index >= activeVerbs.Count) return;
            InteractionVerb verb = activeVerbs[index];

            if (!currentTarget.CanInteract(verb.verbId))
            {
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            if (verb.holdDuration <= 0.05f)
            {
                ExecuteVerbAction(verb);
            }
            else
            {
                activeExecutingIndex = index;
                float holdMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetScavengeHoldDurationMultiplier() : 1.0f;
                activeHoldDuration = verb.holdDuration * holdMult;
                holdProgress = 0f;
            }
        }

        private void UpdateHoldProgress()
        {
            if (activeExecutingIndex < 0 || activeExecutingIndex >= buttonWidgets.Count) return;

            holdProgress += Time.deltaTime;
            float pct = Mathf.Clamp01(holdProgress / activeHoldDuration);

            // Update button visual fill
            VerbButtonWidget w = buttonWidgets[activeExecutingIndex];
            float fullWidth = w.button.GetComponent<RectTransform>().rect.width;
            w.progressFill.rectTransform.sizeDelta = new Vector2(fullWidth * pct, 0f);

            if (activeExecutingIndex == 0 && primaryProgressFill != null)
            {
                primaryProgressFill.rectTransform.sizeDelta = new Vector2(184f * pct, 0f);
            }

            if (holdProgress >= activeHoldDuration)
            {
                InteractionVerb v = w.boundVerb;
                CancelActiveHold();
                ExecuteVerbAction(v);
            }
        }

        private void CancelActiveHold()
        {
            if (activeExecutingIndex >= 0 && activeExecutingIndex < buttonWidgets.Count)
            {
                buttonWidgets[activeExecutingIndex].progressFill.rectTransform.sizeDelta = new Vector2(0f, 0f);
                buttonWidgets[activeExecutingIndex].isPointerHeld = false;
            }
            if (primaryProgressFill != null)
            {
                primaryProgressFill.rectTransform.sizeDelta = Vector2.zero;
            }
            activeExecutingIndex = -1;
            holdProgress = 0f;
            activeHoldDuration = 0f;
        }

        private void ExecuteVerbAction(InteractionVerb verb)
        {
            if (currentTarget == null || verb == null) return;

            if (playerRef == null) playerRef = GameObject.FindWithTag("Player");

            currentTarget.ExecuteVerb(verb.verbId, playerRef);
            ProceduralAudioManager.Instance?.PlayItemPickup();

            if (verb.acousticNoiseRadius > 0f)
            {
                GameEvents.TriggerNoiseEmitted(currentTarget.InteractionTransform.position, verb.acousticNoiseRadius);
            }

            RefreshVerbs();
        }

        private IEnumerator FadeCard(float targetAlpha)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            float dur = 0.22f;

            canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
            canvasGroup.interactable = targetAlpha > 0.5f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / dur);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }
    }
}
