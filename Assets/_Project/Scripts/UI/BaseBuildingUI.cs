using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Building;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Active Phase 3: Mobile & Desktop Base Building HUD & Card Selector.
    /// Implements IModalWindow for clean ModalWindowManager exclusivity.
    /// Provides responsive structure cards, live cost badges, 45° rotation controls,
    /// dismantle mode toggle, and a contextual HUD quick-pill in authorized zones.
    /// </summary>
    public class BaseBuildingUI : MonoBehaviour, IModalWindow
    {
        private static BaseBuildingUI _instance;
        public static BaseBuildingUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BaseBuildingUI>();
                    if (_instance == null)
                    {
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            GameObject go = new GameObject("UI_BaseBuilding");
                            go.transform.SetParent(canvas.transform, false);
                            _instance = go.AddComponent<BaseBuildingUI>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (_instance == null)
            {
                var inst = Instance;
            }
        }

        [Header("Root Panels")]
        [SerializeField] private GameObject buildUIRootPanel;
        [SerializeField] private GameObject quickHUDPill;

        [Header("Action Buttons")]
        [SerializeField] private Button btnRotateLeft;
        [SerializeField] private Button btnRotateRight;
        [SerializeField] private Button btnConfirmBuild;
        [SerializeField] private Button btnToggleDismantle;
        [SerializeField] private Button btnExitBuild;

        [Header("Feedback Texts")]
        [SerializeField] private Text txtModeTitle;
        [SerializeField] private Text txtValidationWarning;
        [SerializeField] private Text txtConfirmButton;

        private readonly List<StructureCardWidget> structureCards = new List<StructureCardWidget>();
        private bool isUIOpen = false;
        private Transform playerTransform;

        public bool IsModalOpen => isUIOpen;

        private class StructureCardWidget
        {
            public StructureType type;
            public GameObject cardRoot;
            public Button selectButton;
            public Image outlineImage;
            public Text titleText;
            public Text costText;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
            ModalWindowManager.Instance.RegisterModal(this);
            BindButtons();
        }

        private void OnDestroy()
        {
            if (ModalWindowManager.Instance != null)
            {
                ModalWindowManager.Instance.UnregisterModal(this);
            }
        }

        private void Start()
        {
            RefreshCostBadges();
            ShowUI(false);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            LocatePlayer();
            UpdateQuickHUDPillVisibility();

            if (isUIOpen)
            {
                UpdateValidationFeedback();
            }
        }

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
                if (p != null) playerTransform = p.transform;
            }
        }

        private void UpdateQuickHUDPillVisibility()
        {
            if (quickHUDPill == null) return;

            // Only show quick HUD pill when player is in authorized zone and not in menu / build mode
            if (isUIOpen || (MainMenuController.Instance != null && MainMenuController.Instance.IsMenuShowing))
            {
                if (quickHUDPill.activeSelf) quickHUDPill.SetActive(false);
                return;
            }

            if (playerTransform != null && BaseBuildingManager.Instance != null)
            {
                bool inZone = BaseBuildingManager.Instance.IsInAuthorizedBuildZone(playerTransform.position);
                if (quickHUDPill.activeSelf != inZone)
                {
                    quickHUDPill.SetActive(inZone);
                }
            }
        }

        public void ShowUI(bool show)
        {
            isUIOpen = show;

            if (buildUIRootPanel != null)
            {
                buildUIRootPanel.SetActive(show);
            }

            if (show)
            {
                ModalWindowManager.Instance.NotifyModalOpened(this);
                RefreshCostBadges();
                UpdateCardSelectionHighlight();
            }
            else
            {
                ModalWindowManager.Instance.NotifyModalClosed(this);
            }
        }

        public void CloseModal()
        {
            if (BaseBuildingManager.Instance != null && BaseBuildingManager.Instance.IsBuildModeActive)
            {
                BaseBuildingManager.Instance.ExitBuildMode();
            }
            else
            {
                ShowUI(false);
            }
        }

        private void BindButtons()
        {
            if (btnRotateLeft != null)
            {
                btnRotateLeft.onClick.RemoveAllListeners();
                btnRotateLeft.onClick.AddListener(() => BaseBuildingManager.Instance?.RotateStructure(-45f));
            }
            if (btnRotateRight != null)
            {
                btnRotateRight.onClick.RemoveAllListeners();
                btnRotateRight.onClick.AddListener(() => BaseBuildingManager.Instance?.RotateStructure(45f));
            }
            if (btnConfirmBuild != null)
            {
                btnConfirmBuild.onClick.RemoveAllListeners();
                btnConfirmBuild.onClick.AddListener(OnConfirmClicked);
            }
            if (btnToggleDismantle != null)
            {
                btnToggleDismantle.onClick.RemoveAllListeners();
                btnToggleDismantle.onClick.AddListener(OnDismantleToggleClicked);
            }
            if (btnExitBuild != null)
            {
                btnExitBuild.onClick.RemoveAllListeners();
                btnExitBuild.onClick.AddListener(CloseModal);
            }
        }

        private void OnConfirmClicked()
        {
            if (BaseBuildingManager.Instance == null) return;

            if (BaseBuildingManager.Instance.IsDismantleMode)
            {
                BaseBuildingManager.Instance.TryDismantleLookedAtStructure();
            }
            else
            {
                BaseBuildingManager.Instance.TryPlaceStructure();
            }
            RefreshCostBadges();
        }

        private void OnDismantleToggleClicked()
        {
            if (BaseBuildingManager.Instance == null) return;
            BaseBuildingManager.Instance.ToggleDismantleMode();
            UpdateModeVisuals();
        }

        private void UpdateModeVisuals()
        {
            if (BaseBuildingManager.Instance == null) return;
            bool isDismantle = BaseBuildingManager.Instance.IsDismantleMode;

            if (txtModeTitle != null)
            {
                txtModeTitle.text = isDismantle ? "SÖKÜM MODU (%50 İADE)" : "MODÜLER ÜS İNŞASI";
                txtModeTitle.color = isDismantle ? new Color(0.95f, 0.45f, 0.15f, 1f) : new Color(0.06f, 0.84f, 0.63f, 1f);
            }

            if (txtConfirmButton != null)
            {
                txtConfirmButton.text = isDismantle ? "SÖK VE İADE AL" : "İNŞA ET";
            }
        }

        public void RefreshCostBadges()
        {
            UpdateModeVisuals();

            for (int i = 0; i < structureCards.Count; i++)
            {
                var card = structureCards[i];
                if (card == null || card.costText == null) continue;

                ResourceCost[] costs = BuildableStructure.GetCosts(card.type);
                string costSummary = string.Empty;

                for (int c = 0; c < costs.Length; c++)
                {
                    int owned = (InventoryManager.Instance != null) ? InventoryManager.GetItemCount(costs[c].itemId) : 0;
                    bool canAfford = owned >= costs[c].amount;

                    string itemName = GetCleanItemName(costs[c].itemId);
                    string colorHex = canAfford ? "#06D6A0" : "#EF476F";
                    costSummary += $"<color={colorHex}>{itemName}: {owned}/{costs[c].amount}</color> ";
                }

                card.costText.text = costSummary.TrimEnd();
            }
        }

        private string GetCleanItemName(string itemId)
        {
            switch (itemId)
            {
                case "item_scrap": return "Hurda";
                case "item_cloth": return "Kumaş";
                case "item_battery": return "Pil";
                case "item_water_bottle": return "Su";
                default: return itemId;
            }
        }

        private void UpdateCardSelectionHighlight()
        {
            if (BaseBuildingManager.Instance == null) return;
            StructureType currentSelected = BaseBuildingManager.Instance.SelectedStructure;

            for (int i = 0; i < structureCards.Count; i++)
            {
                var card = structureCards[i];
                if (card == null || card.outlineImage == null) continue;

                bool isSelected = card.type == currentSelected;
                card.outlineImage.color = isSelected ? new Color(0.06f, 0.84f, 0.63f, 1f) : new Color(0.25f, 0.28f, 0.32f, 0.7f);
            }
        }

        private void UpdateValidationFeedback()
        {
            if (txtValidationWarning == null || BaseBuildingManager.Instance == null) return;

            if (BaseBuildingManager.Instance.IsDismantleMode)
            {
                txtValidationWarning.text = "Sökmek istediğiniz yapıya odaklanın ve onaylayın.";
                txtValidationWarning.color = new Color(0.95f, 0.45f, 0.15f, 1f);
            }
            else if (!BaseBuildingManager.Instance.IsPlacementValid)
            {
                txtValidationWarning.text = BaseBuildingManager.Instance.PlacementInvalidReason;
                txtValidationWarning.color = new Color(0.94f, 0.28f, 0.44f, 1f);
            }
            else
            {
                txtValidationWarning.text = "Yerleşim uygun. [İNŞA ET] butonuna basın.";
                txtValidationWarning.color = new Color(0.06f, 0.84f, 0.63f, 1f);
            }
        }

        private void EnsureUIHierarchy()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            if (canvas == null) return;

            // 1. Contextual Quick HUD Pill on Main HUD
            if (quickHUDPill == null)
            {
                quickHUDPill = new GameObject("Btn_QuickBuildPill");
                quickHUDPill.transform.SetParent(canvas.transform, false);

                RectTransform pillRt = quickHUDPill.AddComponent<RectTransform>();
                pillRt.anchorMin = new Vector2(0.5f, 0f);
                pillRt.anchorMax = new Vector2(0.5f, 0f);
                pillRt.pivot = new Vector2(0.5f, 0f);
                pillRt.anchoredPosition = new Vector2(0f, 95f);
                pillRt.sizeDelta = new Vector2(160f, 44f);

                Image pillBg = quickHUDPill.AddComponent<Image>();
                pillBg.color = new Color(0.06f, 0.10f, 0.15f, 0.90f);

                Outline pillOutline = quickHUDPill.AddComponent<Outline>();
                pillOutline.effectColor = new Color(0.06f, 0.84f, 0.63f, 0.8f);
                pillOutline.effectDistance = new Vector2(1.5f, 1.5f);

                Button pillBtn = quickHUDPill.AddComponent<Button>();
                pillBtn.onClick.AddListener(() => BaseBuildingManager.Instance?.EnterBuildMode());

                GameObject pillTxtGo = new GameObject("Text");
                pillTxtGo.transform.SetParent(quickHUDPill.transform, false);
                RectTransform txtRt = pillTxtGo.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                Text pillText = pillTxtGo.AddComponent<Text>();
                pillText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                pillText.text = "🏗️ İNŞA MODU";
                pillText.fontSize = 15;
                pillText.fontStyle = FontStyle.Bold;
                pillText.alignment = TextAnchor.MiddleCenter;
                pillText.color = Color.white;

                quickHUDPill.SetActive(false);
            }

            // 2. Build UI Root Panel
            if (buildUIRootPanel == null)
            {
                buildUIRootPanel = new GameObject("Panel_BaseBuildingHUD");
                buildUIRootPanel.transform.SetParent(canvas.transform, false);

                RectTransform rootRt = buildUIRootPanel.AddComponent<RectTransform>();
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.sizeDelta = Vector2.zero;

                buildUIRootPanel.AddComponent<SafeAreaHandler>();

                // Semi-transparent glass bottom dock
                GameObject bottomDock = new GameObject("Bottom_StructureCarousel");
                bottomDock.transform.SetParent(buildUIRootPanel.transform, false);
                RectTransform dockRt = bottomDock.AddComponent<RectTransform>();
                dockRt.anchorMin = new Vector2(0f, 0f);
                dockRt.anchorMax = new Vector2(1f, 0f);
                dockRt.pivot = new Vector2(0.5f, 0f);
                dockRt.anchoredPosition = new Vector2(0f, 10f);
                dockRt.sizeDelta = new Vector2(0f, 110f);

                Image dockBg = bottomDock.AddComponent<Image>();
                dockBg.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);

                // Title & Validation text bar
                GameObject infoBar = new GameObject("Top_FeedbackBar");
                infoBar.transform.SetParent(buildUIRootPanel.transform, false);
                RectTransform infoRt = infoBar.AddComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0.5f, 1f);
                infoRt.anchorMax = new Vector2(0.5f, 1f);
                infoRt.pivot = new Vector2(0.5f, 1f);
                infoRt.anchoredPosition = new Vector2(0f, -40f);
                infoRt.sizeDelta = new Vector2(460f, 60f);

                Image infoBg = infoBar.AddComponent<Image>();
                infoBg.color = new Color(0.05f, 0.08f, 0.12f, 0.90f);

                GameObject titleGo = new GameObject("Title");
                titleGo.transform.SetParent(infoBar.transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0.5f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.sizeDelta = Vector2.zero;

                txtModeTitle = titleGo.AddComponent<Text>();
                txtModeTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtModeTitle.text = "MODÜLER ÜS İNŞASI";
                txtModeTitle.fontSize = 17;
                txtModeTitle.fontStyle = FontStyle.Bold;
                txtModeTitle.alignment = TextAnchor.MiddleCenter;
                txtModeTitle.color = new Color(0.06f, 0.84f, 0.63f, 1f);

                GameObject warnGo = new GameObject("ValidationWarning");
                warnGo.transform.SetParent(infoBar.transform, false);
                RectTransform warnRt = warnGo.AddComponent<RectTransform>();
                warnRt.anchorMin = new Vector2(0f, 0f);
                warnRt.anchorMax = new Vector2(1f, 0.5f);
                warnRt.sizeDelta = Vector2.zero;

                txtValidationWarning = warnGo.AddComponent<Text>();
                txtValidationWarning.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtValidationWarning.text = "Yerleşim kontrol ediliyor...";
                txtValidationWarning.fontSize = 13;
                txtValidationWarning.alignment = TextAnchor.MiddleCenter;
                txtValidationWarning.color = Color.white;

                // Create the 5 Structure Cards
                CreateStructureCards(bottomDock.transform);

                // Right action controls panel
                CreateActionDock(buildUIRootPanel.transform);
            }
        }

        private void CreateStructureCards(Transform parent)
        {
            structureCards.Clear();
            StructureType[] types = new StructureType[]
            {
                StructureType.DefenseWall,
                StructureType.RazorWire,
                StructureType.Floodlight,
                StructureType.RainCollector,
                StructureType.HerbPlanter
            };

            float cardWidth = 130f;
            float spacing = 10f;
            float totalWidth = (types.Length * cardWidth) + ((types.Length - 1) * spacing);
            float startX = -totalWidth * 0.5f + (cardWidth * 0.5f);

            for (int i = 0; i < types.Length; i++)
            {
                StructureType st = types[i];
                GameObject cardGo = new GameObject($"Card_{st}");
                cardGo.transform.SetParent(parent, false);

                RectTransform cardRt = cardGo.AddComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = new Vector2(startX + (i * (cardWidth + spacing)), 0f);
                cardRt.sizeDelta = new Vector2(cardWidth, 90f);

                Image cardBg = cardGo.AddComponent<Image>();
                cardBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

                Outline cardOutline = cardGo.AddComponent<Outline>();
                cardOutline.effectDistance = new Vector2(1.5f, 1.5f);
                cardOutline.effectColor = new Color(0.25f, 0.28f, 0.32f, 0.7f);

                Button cardBtn = cardGo.AddComponent<Button>();
                cardBtn.onClick.AddListener(() =>
                {
                    BaseBuildingManager.Instance?.SelectStructure(st);
                    UpdateCardSelectionHighlight();
                });

                // Title
                GameObject titleGo = new GameObject("Title");
                titleGo.transform.SetParent(cardGo.transform, false);
                RectTransform tRt = titleGo.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.55f);
                tRt.anchorMax = new Vector2(1f, 1f);
                tRt.sizeDelta = Vector2.zero;

                Text titleTxt = titleGo.AddComponent<Text>();
                titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                titleTxt.text = BuildableStructure.GetStructureDisplayName(st);
                titleTxt.fontSize = 12;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.alignment = TextAnchor.MiddleCenter;
                titleTxt.color = Color.white;

                // Costs
                GameObject costGo = new GameObject("Cost");
                costGo.transform.SetParent(cardGo.transform, false);
                RectTransform cRt = costGo.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0f, 0f);
                cRt.anchorMax = new Vector2(1f, 0.55f);
                cRt.sizeDelta = Vector2.zero;

                Text costTxt = costGo.AddComponent<Text>();
                costTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                costTxt.supportRichText = true;
                costTxt.fontSize = 11;
                costTxt.alignment = TextAnchor.MiddleCenter;
                costTxt.color = Color.white;

                structureCards.Add(new StructureCardWidget
                {
                    type = st,
                    cardRoot = cardGo,
                    selectButton = cardBtn,
                    outlineImage = cardBg,
                    titleText = titleTxt,
                    costText = costTxt
                });
            }
        }

        private void CreateActionDock(Transform parent)
        {
            GameObject actionDock = new GameObject("Right_ActionControls");
            actionDock.transform.SetParent(parent, false);

            RectTransform dockRt = actionDock.AddComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(1f, 0.5f);
            dockRt.anchorMax = new Vector2(1f, 0.5f);
            dockRt.pivot = new Vector2(1f, 0.5f);
            dockRt.anchoredPosition = new Vector2(-15f, 0f);
            dockRt.sizeDelta = new Vector2(120f, 260f);

            // Rotate Left
            btnRotateLeft = CreateDockButton(actionDock.transform, "Btn_RotateLeft", "↺ -45°", new Vector2(0f, 90f), new Color(0.2f, 0.25f, 0.3f, 0.9f));

            // Rotate Right
            btnRotateRight = CreateDockButton(actionDock.transform, "Btn_RotateRight", "↻ +45°", new Vector2(0f, 40f), new Color(0.2f, 0.25f, 0.3f, 0.9f));

            // Confirm Placement
            btnConfirmBuild = CreateDockButton(actionDock.transform, "Btn_ConfirmBuild", "İNŞA ET", new Vector2(0f, -15f), new Color(0.06f, 0.65f, 0.45f, 0.95f));
            txtConfirmButton = btnConfirmBuild.GetComponentInChildren<Text>();

            // Toggle Dismantle
            btnToggleDismantle = CreateDockButton(actionDock.transform, "Btn_ToggleDismantle", "SÖKÜM", new Vector2(0f, -65f), new Color(0.75f, 0.35f, 0.15f, 0.9f));

            // Exit
            btnExitBuild = CreateDockButton(actionDock.transform, "Btn_ExitBuild", "✕ ÇIKIŞ", new Vector2(0f, -115f), new Color(0.5f, 0.15f, 0.15f, 0.9f));
        }

        private Button CreateDockButton(Transform parent, string name, string label, Vector2 anchoredPos, Color btnColor)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(110f, 40f);

            Image img = btnGo.AddComponent<Image>();
            img.color = btnColor;

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, 1f);
            outline.effectColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);

            Button btn = btnGo.AddComponent<Button>();

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform tRt = txtGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }
    }
}
