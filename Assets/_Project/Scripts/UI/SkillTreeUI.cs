using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 3: Mobile-Friendly 3-Branch Survival Skill Tree UI.
    /// Built on a SafeArea-aligned 3-column dark glassmorphism layout:
    /// - Column 1: GİZLİLİK (Stealth & Recon)
    /// - Column 2: SAVAŞ (Combat & Ballistics)
    /// - Column 3: HAYATTA KALMA (Survival & Engineering)
    /// Shows active/locked state, prerequisite connections, available points,
    /// and instant perk unlock buttons with procedural audio feedback.
    /// </summary>
    public class SkillTreeUI : MonoBehaviour
    {
        private static SkillTreeUI _instance;
        public static SkillTreeUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SkillTreeUI>();
                    if (_instance == null)
                    {
                        Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                        if (hudCanvas != null)
                        {
                            GameObject uiObj = new GameObject("SkillTree_UI");
                            uiObj.transform.SetParent(hudCanvas.transform, false);
                            _instance = uiObj.AddComponent<SkillTreeUI>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text pointsCounterText;
        [SerializeField] private Button closeButton;

        // UI references for branch columns
        private Transform stealthColumnParent;
        private Transform combatColumnParent;
        private Transform survivalColumnParent;

        // Cached card elements for refresh
        private readonly List<SkillCardWidget> cardWidgets = new List<SkillCardWidget>();

        private class SkillCardWidget
        {
            public SkillNodeDef nodeDef;
            public GameObject cardObj;
            public Image background;
            public Text titleText;
            public Text tierText;
            public Text descText;
            public Button unlockBtn;
            public Text unlockBtnText;
            public Image lockIcon;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                {
                    GameObject uiObj = new GameObject("SkillTree_UI");
                    uiObj.transform.SetParent(hudCanvas.transform, false);
                    Instance = uiObj.AddComponent<SkillTreeUI>();
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIHierarchyIfEmpty();
        }

        private void OnEnable()
        {
            GameEvents.OnSkillPointsChanged += HandleSkillPointsChanged;
            GameEvents.OnPerkUnlocked += HandlePerkUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.OnSkillPointsChanged -= HandleSkillPointsChanged;
            GameEvents.OnPerkUnlocked -= HandlePerkUnlocked;
        }

        private void Update()
        {
            // Keyboard shortcut 'K' toggles Skill Tree
            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleSkillTree();
            }

            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseSkillTree();
                }
            }
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void OpenSkillTree()
        {
            if (panelRoot != null)
            {
                ModalWindowManager.Instance?.NotifyModalOpened(this);
                panelRoot.SetActive(true);
                RefreshAll();
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }
        }

        public void CloseSkillTree()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }
        }

        public void ToggleSkillTree()
        {
            if (panelRoot != null && panelRoot.activeSelf) CloseSkillTree();
            else OpenSkillTree();
        }

        private void HandleSkillPointsChanged(int points)
        {
            RefreshAll();
        }

        private void HandlePerkUnlocked(string perkId)
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            SurvivorProgressionManager pm = SurvivorProgressionManager.Instance;
            if (pm == null) return;

            if (pointsCounterText != null)
            {
                pointsCounterText.text = $"KULLANILABİLİR PUAN: <color=#FFD166><b>{pm.UnspentSkillPoints}</b></color>  |  SEVİYE: <b>{pm.PlayerLevel}</b>";
            }

            for (int i = 0; i < cardWidgets.Count; i++)
            {
                RefreshCard(cardWidgets[i], pm);
            }
        }

        private void RefreshCard(SkillCardWidget w, SurvivorProgressionManager pm)
        {
            bool isUnlocked = pm.HasPerk(w.nodeDef.id);
            bool canUnlock = pm.CanUnlockPerk(w.nodeDef.id);

            if (isUnlocked)
            {
                // Emerald Active State
                w.background.color = new Color(0.08f, 0.22f, 0.14f, 0.92f);
                w.titleText.color = new Color(0.35f, 0.95f, 0.55f);
                w.tierText.color = new Color(0.55f, 0.95f, 0.70f);
                w.unlockBtn.gameObject.SetActive(true);
                w.unlockBtn.interactable = false;
                w.unlockBtn.image.color = new Color(0.18f, 0.35f, 0.22f, 0.85f);
                w.unlockBtnText.text = "AKTİF";
                w.unlockBtnText.color = new Color(0.40f, 0.95f, 0.60f);
            }
            else if (canUnlock)
            {
                // Golden Available State
                w.background.color = new Color(0.24f, 0.18f, 0.08f, 0.92f);
                w.titleText.color = new Color(1.0f, 0.88f, 0.45f);
                w.tierText.color = new Color(0.95f, 0.80f, 0.35f);
                w.unlockBtn.gameObject.SetActive(true);
                w.unlockBtn.interactable = true;
                w.unlockBtn.image.color = new Color(0.96f, 0.65f, 0.12f, 0.95f);
                w.unlockBtnText.text = "YETENEĞİ AÇ (1 Puan)";
                w.unlockBtnText.color = Color.black;
            }
            else
            {
                // Dark Locked State
                w.background.color = new Color(0.10f, 0.12f, 0.15f, 0.88f);
                w.titleText.color = new Color(0.65f, 0.68f, 0.72f);
                w.tierText.color = new Color(0.48f, 0.52f, 0.56f);
                w.unlockBtn.gameObject.SetActive(true);
                w.unlockBtn.interactable = false;
                w.unlockBtn.image.color = new Color(0.15f, 0.17f, 0.20f, 0.65f);

                string prereqTitle = "";
                if (!string.IsNullOrEmpty(w.nodeDef.prerequisiteId))
                {
                    SkillNodeDef p = SurvivorProgressionManager.SkillTreeDefinitions.Find(x => x.id == w.nodeDef.prerequisiteId);
                    if (p != null) prereqTitle = p.titleTR;
                }

                w.unlockBtnText.text = string.IsNullOrEmpty(prereqTitle) ? "KİLİTLİ (Puan Yetersiz)" : $"KİLİTLİ ({prereqTitle} Gerekli)";
                w.unlockBtnText.color = new Color(0.55f, 0.58f, 0.62f);
            }
        }

        // =========================================================================
        // PROCEDURAL SAFEAREA UI HIERARCHY BUILDER
        // =========================================================================

        private void BuildUIHierarchyIfEmpty()
        {
            if (panelRoot != null) return;

            // Fullscreen backdrop
            panelRoot = new GameObject("SkillTree_PanelRoot");
            panelRoot.transform.SetParent(transform, false);

            RectTransform rtRoot = panelRoot.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            Image darkBackdrop = panelRoot.AddComponent<Image>();
            darkBackdrop.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);

            // Centered Modal Window
            GameObject window = new GameObject("SkillTree_ModalWindow");
            window.transform.SetParent(panelRoot.transform, false);

            RectTransform rtWin = window.AddComponent<RectTransform>();
            rtWin.anchorMin = new Vector2(0.03f, 0.04f);
            rtWin.anchorMax = new Vector2(0.97f, 0.96f);
            rtWin.sizeDelta = Vector2.zero;

            Image winBg = window.AddComponent<Image>();
            winBg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            // --- HEADER ---
            GameObject header = new GameObject("Header");
            header.transform.SetParent(window.transform, false);
            RectTransform rtHeader = header.AddComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0f, 1f);
            rtHeader.anchorMax = new Vector2(1f, 1f);
            rtHeader.pivot = new Vector2(0.5f, 1f);
            rtHeader.anchoredPosition = Vector2.zero;
            rtHeader.sizeDelta = new Vector2(0f, 65f);

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.15f, 0.20f, 0.90f);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(header.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0f);
            rtTitle.anchorMax = new Vector2(0.45f, 1f);
            rtTitle.offsetMin = new Vector2(25f, 0f);
            rtTitle.offsetMax = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.96f, 0.78f, 0.28f);
            titleText.text = "HAYATTA KALMA YETENEK AĞACI";

            // Points counter
            GameObject pointsObj = new GameObject("PointsText");
            pointsObj.transform.SetParent(header.transform, false);
            RectTransform rtPoints = pointsObj.AddComponent<RectTransform>();
            rtPoints.anchorMin = new Vector2(0.45f, 0f);
            rtPoints.anchorMax = new Vector2(0.85f, 1f);
            rtPoints.offsetMin = Vector2.zero;
            rtPoints.offsetMax = Vector2.zero;

            pointsCounterText = pointsObj.AddComponent<Text>();
            pointsCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pointsCounterText.fontSize = 15;
            pointsCounterText.alignment = TextAnchor.MiddleCenter;
            pointsCounterText.color = Color.white;
            pointsCounterText.text = "KULLANILABİLİR PUAN: 0";

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(header.transform, false);
            RectTransform rtClose = closeBtnObj.AddComponent<RectTransform>();
            rtClose.anchorMin = new Vector2(1f, 0.5f);
            rtClose.anchorMax = new Vector2(1f, 0.5f);
            rtClose.pivot = new Vector2(1f, 0.5f);
            rtClose.anchoredPosition = new Vector2(-15f, 0f);
            rtClose.sizeDelta = new Vector2(110f, 42f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.75f, 0.15f, 0.18f, 0.90f);

            closeButton = closeBtnObj.AddComponent<Button>();
            closeButton.onClick.AddListener(CloseSkillTree);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform rtCloseTxt = closeTxtObj.AddComponent<RectTransform>();
            rtCloseTxt.anchorMin = Vector2.zero;
            rtCloseTxt.anchorMax = Vector2.one;
            rtCloseTxt.sizeDelta = Vector2.zero;
            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 14;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.text = "KAPAT";

            // --- 3 BRANCH COLUMNS CONTAINER ---
            GameObject colsContainer = new GameObject("ColumnsContainer");
            colsContainer.transform.SetParent(window.transform, false);
            RectTransform rtCols = colsContainer.AddComponent<RectTransform>();
            rtCols.anchorMin = new Vector2(0f, 0f);
            rtCols.anchorMax = new Vector2(1f, 1f);
            rtCols.offsetMin = new Vector2(15f, 15f);
            rtCols.offsetMax = new Vector2(-15f, -75f);

            stealthColumnParent = CreateBranchColumn(colsContainer.transform, "GİZLİLİK & KEŞİF", new Color(0.25f, 0.65f, 0.85f), 0);
            combatColumnParent = CreateBranchColumn(colsContainer.transform, "SAVAŞ & BALİSTİK", new Color(0.92f, 0.32f, 0.28f), 1);
            survivalColumnParent = CreateBranchColumn(colsContainer.transform, "HAYATTA KALMA", new Color(0.35f, 0.82f, 0.48f), 2);

            // Populate 12 Node Cards
            cardWidgets.Clear();
            for (int i = 0; i < SurvivorProgressionManager.SkillTreeDefinitions.Count; i++)
            {
                SkillNodeDef def = SurvivorProgressionManager.SkillTreeDefinitions[i];
                Transform colParent = def.branch == SkillBranch.Stealth ? stealthColumnParent :
                                      def.branch == SkillBranch.Combat ? combatColumnParent : survivalColumnParent;

                SkillCardWidget widget = CreateSkillNodeCard(colParent, def);
                cardWidgets.Add(widget);
            }

            panelRoot.SetActive(false);
        }

        private Transform CreateBranchColumn(Transform parent, string headerTitle, Color accentColor, int colIndex)
        {
            GameObject col = new GameObject($"Column_{colIndex}_{headerTitle}");
            col.transform.SetParent(parent, false);

            RectTransform rt = col.AddComponent<RectTransform>();
            float colWidthNorm = 1f / 3f;
            rt.anchorMin = new Vector2(colIndex * colWidthNorm, 0f);
            rt.anchorMax = new Vector2((colIndex + 1) * colWidthNorm, 1f);
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, 0f);

            Image colBg = col.AddComponent<Image>();
            colBg.color = new Color(0.06f, 0.08f, 0.11f, 0.80f);

            // Header Banner
            GameObject hdr = new GameObject("ColumnHeader");
            hdr.transform.SetParent(col.transform, false);
            RectTransform rtHdr = hdr.AddComponent<RectTransform>();
            rtHdr.anchorMin = new Vector2(0f, 1f);
            rtHdr.anchorMax = new Vector2(1f, 1f);
            rtHdr.pivot = new Vector2(0.5f, 1f);
            rtHdr.sizeDelta = new Vector2(0f, 38f);

            Image hdrBg = hdr.AddComponent<Image>();
            hdrBg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.85f);

            GameObject txtObj = new GameObject("Title");
            txtObj.transform.SetParent(hdr.transform, false);
            RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = accentColor;
            txt.text = headerTitle;

            // Scroll / Content Parent for Cards
            GameObject cardsContainer = new GameObject("CardsContent");
            cardsContainer.transform.SetParent(col.transform, false);
            RectTransform rtCards = cardsContainer.AddComponent<RectTransform>();
            rtCards.anchorMin = Vector2.zero;
            rtCards.anchorMax = Vector2.one;
            rtCards.offsetMin = new Vector2(6f, 8f);
            rtCards.offsetMax = new Vector2(-6f, -44f);

            VerticalLayoutGroup vlg = cardsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = true;

            return cardsContainer.transform;
        }

        private SkillCardWidget CreateSkillNodeCard(Transform parent, SkillNodeDef def)
        {
            GameObject card = new GameObject($"Card_{def.id}");
            card.transform.SetParent(parent, false);

            RectTransform rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 105f);

            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.15f, 0.90f);

            // Tier & Title Header Row
            GameObject headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(card.transform, false);
            RectTransform rtHdr = headerRow.AddComponent<RectTransform>();
            rtHdr.anchorMin = new Vector2(0f, 1f);
            rtHdr.anchorMax = new Vector2(1f, 1f);
            rtHdr.pivot = new Vector2(0.5f, 1f);
            rtHdr.anchoredPosition = new Vector2(0f, -4f);
            rtHdr.sizeDelta = new Vector2(0f, 22f);

            GameObject tierObj = new GameObject("TierBadge");
            tierObj.transform.SetParent(headerRow.transform, false);
            RectTransform rtTier = tierObj.AddComponent<RectTransform>();
            rtTier.anchorMin = new Vector2(0f, 0f);
            rtTier.anchorMax = new Vector2(0.24f, 1f);
            rtTier.offsetMin = new Vector2(8f, 0f);
            rtTier.offsetMax = Vector2.zero;

            Text tierTxt = tierObj.AddComponent<Text>();
            tierTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tierTxt.fontSize = 11;
            tierTxt.fontStyle = FontStyle.Bold;
            tierTxt.alignment = TextAnchor.MiddleLeft;
            tierTxt.color = new Color(0.7f, 0.75f, 0.8f);
            tierTxt.text = $"AŞAMA {def.tier}";

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerRow.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.24f, 0f);
            rtTitle.anchorMax = new Vector2(1f, 1f);
            rtTitle.offsetMin = Vector2.zero;
            rtTitle.offsetMax = new Vector2(-8f, 0f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = Color.white;
            titleTxt.text = def.titleTR;

            // Description Text
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(card.transform, false);
            RectTransform rtDesc = descObj.AddComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0f, 0.28f);
            rtDesc.anchorMax = new Vector2(1f, 0.74f);
            rtDesc.offsetMin = new Vector2(10f, 0f);
            rtDesc.offsetMax = new Vector2(-10f, 0f);

            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descTxt.fontSize = 11;
            descTxt.alignment = TextAnchor.UpperLeft;
            descTxt.color = new Color(0.80f, 0.82f, 0.85f);
            descTxt.text = def.descriptionTR;

            // Unlock Button
            GameObject btnObj = new GameObject("UnlockButton");
            btnObj.transform.SetParent(card.transform, false);
            RectTransform rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0f, 0f);
            rtBtn.anchorMax = new Vector2(1f, 0.28f);
            rtBtn.offsetMin = new Vector2(8f, 4f);
            rtBtn.offsetMax = new Vector2(-8f, 0f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.96f, 0.65f, 0.12f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            string perkId = def.id;
            btn.onClick.AddListener(() =>
            {
                SurvivorProgressionManager.Instance?.UnlockPerk(perkId);
            });

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtBtnTxt = btnTxtObj.AddComponent<RectTransform>();
            rtBtnTxt.anchorMin = Vector2.zero;
            rtBtnTxt.anchorMax = Vector2.one;
            rtBtnTxt.sizeDelta = Vector2.zero;

            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 11;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.black;
            btnTxt.text = "YETENEĞİ AÇ";

            return new SkillCardWidget
            {
                nodeDef = def,
                cardObj = card,
                background = bg,
                titleText = titleTxt,
                tierText = tierTxt,
                descText = descTxt,
                unlockBtn = btn,
                unlockBtnText = btnTxt
            };
        }
    }
}
