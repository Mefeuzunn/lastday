using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    public enum StationMode
    {
        Cooking,
        Chemistry
    }

    [System.Serializable]
    public class AlchemyIngredient
    {
        public string itemId;
        public string displayName;
        public int requiredCount;

        public AlchemyIngredient(string id, string name, int count)
        {
            itemId = id;
            displayName = name;
            requiredCount = count;
        }
    }

    [System.Serializable]
    public class AlchemyRecipe
    {
        public string recipeId;
        public string title;
        public string description;
        public string outputItemId;
        public int outputCount;
        public string statPreview;
        public StationMode mode;
        public List<AlchemyIngredient> ingredients;

        public AlchemyRecipe(string id, string t, string d, string outId, int outCount, string preview, StationMode m, List<AlchemyIngredient> ing)
        {
            recipeId = id;
            title = t;
            description = d;
            outputItemId = outId;
            outputCount = outCount;
            statPreview = preview;
            mode = m;
            ingredients = ing;
        }
    }

    /// <summary>
    /// Phase 2 Micro-Step 2B: Dedicated Mobile Cooking & Survival Chemistry Crafting Panel.
    /// Provides a split-screen modal for Camp Stove and Chemistry Lab stations with live ingredient tracking,
    /// animated 1.5s crafting progress, audio/haptic feedback, and bidirectional InventorySystem integration.
    /// </summary>
    public class SurvivalAlchemyUI : MonoBehaviour
    {
        public static SurvivalAlchemyUI Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private StationMode currentMode = StationMode.Cooking;
        [SerializeField] private bool isOpen = false;

        private GameObject panelRoot;
        private Text stationTitleText;
        private Text stationSubtitleText;
        private Transform recipeListContainer;
        private Text selectedTitleText;
        private Text selectedDescText;
        private Text selectedPreviewText;
        private Transform ingredientCardsContainer;
        private Button craftButton;
        private Text craftButtonText;
        private Image craftProgressBar;
        private GameObject progressContainer;

        private List<AlchemyRecipe> allRecipes = new List<AlchemyRecipe>();
        private AlchemyRecipe currentSelectedRecipe = null;
        private bool isCrafting = false;
        private Coroutine craftCoroutine = null;

        public bool IsOpen => isOpen;
        public StationMode CurrentMode => currentMode;
        public IReadOnlyList<AlchemyRecipe> AllRecipes => allRecipes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeRecipes();
            EnsurePanelHierarchy();
        }

        private void Start()
        {
            EnsurePanelHierarchy();
            ClosePanel();
        }

        private void InitializeRecipes()
        {
            allRecipes.Clear();

            // 1. CAMP STOVE RECIPES (Mode: Cooking)
            allRecipes.Add(new AlchemyRecipe(
                "recipe_hot_stew",
                "Sıcak Yahni",
                "Konserve et ve suyun tencerede kaynatılmasıyla hazırlanan sıcak, doyurucu öğün.",
                "item_hot_stew",
                1,
                "+55 TOKLUK  |  +25 SU  |  +15 CAN",
                StationMode.Cooking,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_canned_food", "Konserve Yiyecek", 1),
                    new AlchemyIngredient("item_water_bottle", "Su Şişesi", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_purified_water",
                "Arıtılmış İçme Suyu",
                "Ham suyun yüksek ateşte kaynatılarak parazit ve bakterilerden arındırılmış hali.",
                "item_purified_water",
                1,
                "+50 SU  |  +10 ENERJİ  |  -%5 ENFEKSİYON",
                StationMode.Cooking,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_water_bottle", "Su Şişesi", 1),
                    new AlchemyIngredient("item_scrap", "Hurda / Yakıt", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_herbal_tea",
                "Bitkisel Bağışıklık Çayı",
                "Şifalı bitkiler ve sıcak su ile demlenen metabolizma güçlendirici tonik.",
                "item_herbal_tea",
                1,
                "+30 SU  |  -%12 ENFEKSİYON  |  120 SN DİRENÇ",
                StationMode.Cooking,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_water_bottle", "Su Şişesi", 1),
                    new AlchemyIngredient("item_cloth", "Temiz Kumaş / Bitki", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_cooked_meat",
                "Izgara Av Eti",
                "Kamp ocağında odun ve hurda ateşiyle mühürlenmiş besleyici ve doyurucu ızgara av eti.",
                "item_cooked_meat",
                1,
                "+65 TOKLUK  |  +22 CAN  |  +15 ENERJİ  |  -%5 ENFEKSİYON",
                StationMode.Cooking,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_raw_meat", "Çiğ Av Eti", 1),
                    new AlchemyIngredient("item_scrap", "Hurda Metal / Yakıt", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_grilled_fish",
                "Közde Göl Balığı",
                "Ateşte nar gibi kızartılmış taze tatlı su balığı. Dayanıklılık ve enerji verir.",
                "item_grilled_fish",
                1,
                "+50 TOKLUK  |  +15 SU  |  +18 CAN  |  120 SN DAYANIKLILIK ARTIŞI",
                StationMode.Cooking,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_raw_fish", "Taze Göl Balığı", 1)
                }
            ));

            // 2. CHEMISTRY LAB RECIPES (Mode: Chemistry)
            allRecipes.Add(new AlchemyRecipe(
                "recipe_splint",
                "Atel (Kırık Sabitleyici)",
                "Kırık kemiği sabitlemek için sert destek çubukları ve sıkı steril sargı.",
                "item_splint",
                1,
                "BACAK KIRIĞINI ANINDA İYİLEŞTİRİR",
                StationMode.Chemistry,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_scrap", "Hurda Metal", 2),
                    new AlchemyIngredient("item_cloth", "Temiz Kumaş", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_painkillers",
                "Ağrı Kesici Tablet",
                "Alkol ve distile su senteziyle kimyasal olarak saflaştırılmış güçlü ağrı kesici.",
                "item_painkillers",
                1,
                "YÜKSEK ATEŞİ 180 SN BASKILAR  |  +12 CAN",
                StationMode.Chemistry,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_alcohol", "Alkol", 1),
                    new AlchemyIngredient("item_water_bottle", "Su Şişesi", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_antiviral",
                "Antiviral Serum",
                "Patojenik enfeksiyon yükünü nötralize eden konsantre biyolojik aşı.",
                "item_antiviral",
                1,
                "-%45 ENFEKSİYON  |  ATEŞİ KALICI ÇÖZER",
                StationMode.Chemistry,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_alcohol", "Alkol", 2),
                    new AlchemyIngredient("item_cloth", "Temiz Kumaş", 1),
                    new AlchemyIngredient("item_battery", "Pil / Kimyasal", 1)
                }
            ));

            allRecipes.Add(new AlchemyRecipe(
                "recipe_adrenaline",
                "Adrenalin İğnesi",
                "Aşırı fiziksel efor ve acil durum muharebesi için sentezlenmiş askeri stimülan.",
                "item_adrenaline",
                1,
                "+100 ENERJİ  |  25 SN SINIRSIZ KOŞMA  |  +%20 HIZ",
                StationMode.Chemistry,
                new List<AlchemyIngredient>
                {
                    new AlchemyIngredient("item_alcohol", "Alkol", 2),
                    new AlchemyIngredient("item_painkillers", "Ağrı Kesici", 1),
                    new AlchemyIngredient("item_scrap", "Hurda Metal", 1)
                }
            ));
        }

        public void OpenCookingStation()
        {
            currentMode = StationMode.Cooking;
            OpenStationInternal();
        }

        public void OpenChemistryStation()
        {
            currentMode = StationMode.Chemistry;
            OpenStationInternal();
        }

        private void OpenStationInternal()
        {
            EnsurePanelHierarchy();
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            isOpen = true;
            panelRoot.SetActive(true);

            if (currentMode == StationMode.Cooking)
            {
                stationTitleText.text = "SIĞINAK OCAĞI (YEMEK & SU)";
                stationSubtitleText.text = "BESLENME VE SU ARITMA İSTASYONU";
            }
            else
            {
                stationTitleText.text = "KİMYA & TIBBİ LABORATUVAR";
                stationSubtitleText.text = "İLAÇ VE BİYOLOJİK SENTEZ MASASI";
            }

            // Pick first recipe of this mode as initial selection
            List<AlchemyRecipe> modeRecipes = GetRecipesForMode(currentMode);
            currentSelectedRecipe = modeRecipes.Count > 0 ? modeRecipes[0] : null;

            RefreshUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ClosePanel()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            if (isCrafting && craftCoroutine != null)
            {
                StopCoroutine(craftCoroutine);
                isCrafting = false;
            }

            isOpen = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private List<AlchemyRecipe> GetRecipesForMode(StationMode mode)
        {
            List<AlchemyRecipe> list = new List<AlchemyRecipe>();
            for (int i = 0; i < allRecipes.Count; i++)
            {
                if (allRecipes[i].mode == mode)
                {
                    list.Add(allRecipes[i]);
                }
            }
            return list;
        }

        public bool CanCraftRecipe(AlchemyRecipe recipe)
        {
            if (recipe == null) return false;
            if (InventorySystem.Instance == null) return false;

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                var ing = recipe.ingredients[i];
                if (InventorySystem.Instance.GetItemCount(ing.itemId) < ing.requiredCount)
                {
                    return false;
                }
            }
            return true;
        }

        private void RefreshUI()
        {
            PopulateRecipeList();
            PopulateSelectedRecipeDetails();
        }

        private void PopulateRecipeList()
        {
            if (recipeListContainer == null) return;

            // Clear previous items
            for (int i = recipeListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(recipeListContainer.GetChild(i).gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            List<AlchemyRecipe> recipes = GetRecipesForMode(currentMode);

            for (int i = 0; i < recipes.Count; i++)
            {
                AlchemyRecipe r = recipes[i];
                bool canCraft = CanCraftRecipe(r);
                bool isSelected = (currentSelectedRecipe != null && currentSelectedRecipe.recipeId == r.recipeId);

                GameObject rowObj = new GameObject($"Recipe_{r.recipeId}");
                rowObj.transform.SetParent(recipeListContainer, false);

                RectTransform rtRow = rowObj.AddComponent<RectTransform>();
                rtRow.sizeDelta = new Vector2(340f, 62f);

                Image rowImg = rowObj.AddComponent<Image>();
                rowImg.color = isSelected ? new Color(0.18f, 0.28f, 0.38f, 0.95f) : new Color(0.12f, 0.15f, 0.19f, 0.85f);
                rowImg.raycastTarget = true;

                Button rowBtn = rowObj.AddComponent<Button>();
                rowBtn.targetGraphic = rowImg;
                var cb = rowBtn.colors;
                cb.highlightedColor = new Color(0.24f, 0.35f, 0.45f, 1f);
                cb.pressedColor = new Color(0.10f, 0.18f, 0.25f, 1f);
                rowBtn.colors = cb;
                rowBtn.onClick.AddListener(() =>
                {
                    if (isCrafting) return;
                    currentSelectedRecipe = r;
                    ProceduralAudioManager.Instance?.PlayButtonClick();
                    RefreshUI();
                });

                // Title Text
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(rowObj.transform, false);
                RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
                rtTitle.anchorMin = new Vector2(0f, 0.5f);
                rtTitle.anchorMax = new Vector2(0.68f, 0.5f);
                rtTitle.pivot = new Vector2(0f, 0.5f);
                rtTitle.anchoredPosition = new Vector2(16f, 0f);
                rtTitle.sizeDelta = new Vector2(0f, 32f);

                Text tText = titleObj.AddComponent<Text>();
                tText.font = font;
                tText.fontSize = 13;
                tText.fontStyle = FontStyle.Bold;
                tText.alignment = TextAnchor.MiddleLeft;
                tText.color = isSelected ? Color.white : new Color(0.9f, 0.9f, 0.92f);
                tText.text = r.title;
                tText.raycastTarget = false;

                // Status Badge
                GameObject badgeObj = new GameObject("Badge");
                badgeObj.transform.SetParent(rowObj.transform, false);
                RectTransform rtBadge = badgeObj.AddComponent<RectTransform>();
                rtBadge.anchorMin = new Vector2(1f, 0.5f);
                rtBadge.anchorMax = new Vector2(1f, 0.5f);
                rtBadge.pivot = new Vector2(1f, 0.5f);
                rtBadge.anchoredPosition = new Vector2(-12f, 0f);
                rtBadge.sizeDelta = new Vector2(92f, 24f);

                Image badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.color = canCraft ? new Color(0.12f, 0.45f, 0.25f, 0.90f) : new Color(0.22f, 0.24f, 0.28f, 0.85f);
                badgeImg.raycastTarget = false;

                GameObject badgeTxtObj = new GameObject("BadgeText");
                badgeTxtObj.transform.SetParent(badgeObj.transform, false);
                RectTransform rtBt = badgeTxtObj.AddComponent<RectTransform>();
                rtBt.anchorMin = Vector2.zero;
                rtBt.anchorMax = Vector2.one;
                rtBt.sizeDelta = Vector2.zero;

                Text bText = badgeTxtObj.AddComponent<Text>();
                bText.font = font;
                bText.fontSize = 10;
                bText.fontStyle = FontStyle.Bold;
                bText.alignment = TextAnchor.MiddleCenter;
                bText.color = canCraft ? new Color(0.40f, 0.95f, 0.55f) : new Color(0.70f, 0.72f, 0.75f);
                bText.text = canCraft ? "HAZIR" : "EKSİK";
                bText.raycastTarget = false;
            }
        }

        private void PopulateSelectedRecipeDetails()
        {
            if (currentSelectedRecipe == null)
            {
                if (selectedTitleText != null) selectedTitleText.text = "Tarif Seçilmedi";
                if (selectedDescText != null) selectedDescText.text = "";
                if (selectedPreviewText != null) selectedPreviewText.text = "";
                if (craftButton != null) craftButton.interactable = false;
                return;
            }

            selectedTitleText.text = currentSelectedRecipe.title;
            selectedDescText.text = currentSelectedRecipe.description;
            selectedPreviewText.text = currentSelectedRecipe.statPreview;

            // Clear ingredient cards
            for (int i = ingredientCardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ingredientCardsContainer.GetChild(i).gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            bool canCraftAll = true;

            for (int i = 0; i < currentSelectedRecipe.ingredients.Count; i++)
            {
                var ing = currentSelectedRecipe.ingredients[i];
                int owned = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(ing.itemId) : 0;
                bool hasEnough = owned >= ing.requiredCount;
                if (!hasEnough) canCraftAll = false;

                GameObject card = new GameObject($"Ingredient_{ing.itemId}");
                card.transform.SetParent(ingredientCardsContainer, false);

                RectTransform rtCard = card.AddComponent<RectTransform>();
                rtCard.sizeDelta = new Vector2(380f, 38f);

                Image cardBg = card.AddComponent<Image>();
                cardBg.color = hasEnough ? new Color(0.14f, 0.20f, 0.16f, 0.85f) : new Color(0.24f, 0.14f, 0.14f, 0.85f);
                cardBg.raycastTarget = false;

                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(card.transform, false);
                RectTransform rtName = nameObj.AddComponent<RectTransform>();
                rtName.anchorMin = new Vector2(0f, 0f);
                rtName.anchorMax = new Vector2(0.65f, 1f);
                rtName.anchoredPosition = new Vector2(14f, 0f);
                rtName.sizeDelta = Vector2.zero;

                Text nText = nameObj.AddComponent<Text>();
                nText.font = font;
                nText.fontSize = 12;
                nText.fontStyle = FontStyle.Normal;
                nText.alignment = TextAnchor.MiddleLeft;
                nText.color = Color.white;
                nText.text = ing.displayName;
                nText.raycastTarget = false;

                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(card.transform, false);
                RectTransform rtCount = countObj.AddComponent<RectTransform>();
                rtCount.anchorMin = new Vector2(0.65f, 0f);
                rtCount.anchorMax = new Vector2(1f, 1f);
                rtCount.anchoredPosition = new Vector2(-14f, 0f);
                rtCount.sizeDelta = Vector2.zero;

                Text cText = countObj.AddComponent<Text>();
                cText.font = font;
                cText.fontSize = 12;
                cText.fontStyle = FontStyle.Bold;
                cText.alignment = TextAnchor.MiddleRight;
                cText.color = hasEnough ? new Color(0.35f, 0.95f, 0.45f) : new Color(0.95f, 0.35f, 0.35f);
                cText.text = $"{owned} / {ing.requiredCount}";
                cText.raycastTarget = false;
            }

            if (!isCrafting)
            {
                craftButton.interactable = canCraftAll;
                craftButtonText.text = canCraftAll ? "ÜRET (1.5 sn)" : "YETERSİZ MALZEME";
                progressContainer.SetActive(false);
            }
        }

        private void OnCraftButtonClicked()
        {
            if (isCrafting || currentSelectedRecipe == null) return;
            if (!CanCraftRecipe(currentSelectedRecipe))
            {
                GameEvents.TriggerObjectiveToastRequested("Yetersiz Malzeme!", 2f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            craftCoroutine = StartCoroutine(CraftRoutine(currentSelectedRecipe));
        }

        private IEnumerator CraftRoutine(AlchemyRecipe recipe)
        {
            isCrafting = true;
            craftButton.interactable = false;
            craftButtonText.text = "ÜRETİLİYOR...";
            progressContainer.SetActive(true);
            craftProgressBar.fillAmount = 0f;

            if (currentMode == StationMode.Cooking)
            {
                ProceduralAudioManager.Instance?.PlayBoilingSteam();
            }
            else
            {
                ProceduralAudioManager.Instance?.PlayChemistryGlassware();
            }

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                craftProgressBar.fillAmount = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            // Deduct ingredients
            if (InventorySystem.Instance != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    InventorySystem.Instance.RemoveItem(recipe.ingredients[i].itemId, recipe.ingredients[i].requiredCount);
                }

                // Grant output item
                InventorySystem.Instance.AddItemById(recipe.outputItemId, recipe.outputCount);
            }

            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            MobileHapticManager.Instance?.TriggerMedium();
            GameEvents.TriggerObjectiveToastRequested($"{recipe.title} Başarıyla Üretildi!", 2.5f);

            isCrafting = false;
            progressContainer.SetActive(false);
            craftProgressBar.fillAmount = 0f;

            RefreshUI();
        }

        // =========================================================================
        // PROCEDURAL CANVAS / MODAL HIERARCHY SETUP
        // =========================================================================

        private void EnsurePanelHierarchy()
        {
            if (panelRoot != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("SurvivalAlchemy_Canvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
            }

            Transform existing = canvas.transform.Find("SurvivalAlchemy_Panel");
            if (existing != null)
            {
                panelRoot = existing.gameObject;
                WireExistingReferences();
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;

            // Modal Root Container
            panelRoot = new GameObject("SurvivalAlchemy_Panel");
            panelRoot.transform.SetParent(canvas.transform, false);

            RectTransform rtRoot = panelRoot.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.offsetMin = Vector2.zero;
            rtRoot.offsetMax = Vector2.zero;

            Image rootBg = panelRoot.AddComponent<Image>();
            rootBg.color = new Color(0.04f, 0.06f, 0.08f, 0.94f); // Dark tactical backdrop
            rootBg.raycastTarget = true;

            // Center Modal Window (900 x 580)
            GameObject modalWin = new GameObject("Modal_Window");
            modalWin.transform.SetParent(panelRoot.transform, false);
            RectTransform rtModal = modalWin.AddComponent<RectTransform>();
            rtModal.anchorMin = new Vector2(0.5f, 0.5f);
            rtModal.anchorMax = new Vector2(0.5f, 0.5f);
            rtModal.pivot = new Vector2(0.5f, 0.5f);
            rtModal.sizeDelta = new Vector2(880f, 540f);

            Image modalBg = modalWin.AddComponent<Image>();
            modalBg.color = new Color(0.09f, 0.12f, 0.16f, 0.98f);
            modalBg.raycastTarget = false;

            // --- HEADER ---
            GameObject header = new GameObject("Header");
            header.transform.SetParent(modalWin.transform, false);
            RectTransform rtH = header.AddComponent<RectTransform>();
            rtH.anchorMin = new Vector2(0f, 1f);
            rtH.anchorMax = new Vector2(1f, 1f);
            rtH.pivot = new Vector2(0.5f, 1f);
            rtH.anchoredPosition = new Vector2(0f, 0f);
            rtH.sizeDelta = new Vector2(880f, 65f);

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.13f, 0.17f, 0.22f, 1f);
            headerBg.raycastTarget = false;

            // Title
            GameObject titleObj = new GameObject("StationTitle");
            titleObj.transform.SetParent(header.transform, false);
            RectTransform rtSt = titleObj.AddComponent<RectTransform>();
            rtSt.anchorMin = new Vector2(0f, 0.5f);
            rtSt.anchorMax = new Vector2(0.8f, 0.5f);
            rtSt.pivot = new Vector2(0f, 0.5f);
            rtSt.anchoredPosition = new Vector2(24f, 8f);
            stationTitleText = titleObj.AddComponent<Text>();
            stationTitleText.font = font;
            stationTitleText.fontSize = 18;
            stationTitleText.fontStyle = FontStyle.Bold;
            stationTitleText.color = new Color(0.95f, 0.95f, 0.98f);
            stationTitleText.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("StationSubtitle");
            subObj.transform.SetParent(header.transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0.5f);
            rtSub.anchorMax = new Vector2(0.8f, 0.5f);
            rtSub.pivot = new Vector2(0f, 0.5f);
            rtSub.anchoredPosition = new Vector2(24f, -12f);
            stationSubtitleText = subObj.AddComponent<Text>();
            stationSubtitleText.font = font;
            stationSubtitleText.fontSize = 11;
            stationSubtitleText.color = new Color(0.60f, 0.70f, 0.80f);
            stationSubtitleText.raycastTarget = false;

            // Close Button (X)
            GameObject closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(header.transform, false);
            RectTransform rtClose = closeBtnObj.AddComponent<RectTransform>();
            rtClose.anchorMin = new Vector2(1f, 0.5f);
            rtClose.anchorMax = new Vector2(1f, 0.5f);
            rtClose.pivot = new Vector2(1f, 0.5f);
            rtClose.anchoredPosition = new Vector2(-16f, 0f);
            rtClose.sizeDelta = new Vector2(48f, 44f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.75f, 0.15f, 0.20f, 0.90f);
            closeImg.raycastTarget = true;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(ClosePanel);

            GameObject closeTextObj = new GameObject("X");
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform rtCt = closeTextObj.AddComponent<RectTransform>();
            rtCt.anchorMin = Vector2.zero;
            rtCt.anchorMax = Vector2.one;
            rtCt.sizeDelta = Vector2.zero;
            Text closeText = closeTextObj.AddComponent<Text>();
            closeText.font = font;
            closeText.fontSize = 16;
            closeText.fontStyle = FontStyle.Bold;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.color = Color.white;
            closeText.text = "X";
            closeText.raycastTarget = false;

            // --- LEFT COLUMN: RECIPE LIST ---
            GameObject leftCol = new GameObject("LeftColumn_RecipeList");
            leftCol.transform.SetParent(modalWin.transform, false);
            RectTransform rtLeft = leftCol.AddComponent<RectTransform>();
            rtLeft.anchorMin = new Vector2(0f, 0f);
            rtLeft.anchorMax = new Vector2(0.42f, 1f);
            rtLeft.offsetMin = new Vector2(16f, 16f);
            rtLeft.offsetMax = new Vector2(-8f, -75f);

            recipeListContainer = leftCol.transform;
            VerticalLayoutGroup vlgLeft = leftCol.AddComponent<VerticalLayoutGroup>();
            vlgLeft.spacing = 8f;
            vlgLeft.childAlignment = TextAnchor.UpperCenter;
            vlgLeft.childControlWidth = true;
            vlgLeft.childControlHeight = false;

            // --- RIGHT COLUMN: DETAILS & CRAFTING ---
            GameObject rightCol = new GameObject("RightColumn_Details");
            rightCol.transform.SetParent(modalWin.transform, false);
            RectTransform rtRight = rightCol.AddComponent<RectTransform>();
            rtRight.anchorMin = new Vector2(0.42f, 0f);
            rtRight.anchorMax = new Vector2(1f, 1f);
            rtRight.offsetMin = new Vector2(8f, 16f);
            rtRight.offsetMax = new Vector2(-16f, -75f);

            Image rightBg = rightCol.AddComponent<Image>();
            rightBg.color = new Color(0.12f, 0.15f, 0.19f, 0.90f);
            rightBg.raycastTarget = false;

            // Selected Title
            GameObject stObj = new GameObject("SelectedTitle");
            stObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtSt2 = stObj.AddComponent<RectTransform>();
            rtSt2.anchorMin = new Vector2(0f, 1f);
            rtSt2.anchorMax = new Vector2(1f, 1f);
            rtSt2.anchoredPosition = new Vector2(20f, -24f);
            rtSt2.sizeDelta = new Vector2(-40f, 30f);
            selectedTitleText = stObj.AddComponent<Text>();
            selectedTitleText.font = font;
            selectedTitleText.fontSize = 17;
            selectedTitleText.fontStyle = FontStyle.Bold;
            selectedTitleText.color = Color.white;
            selectedTitleText.raycastTarget = false;

            // Selected Description
            GameObject descObj = new GameObject("SelectedDesc");
            descObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtDesc = descObj.AddComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0f, 1f);
            rtDesc.anchorMax = new Vector2(1f, 1f);
            rtDesc.anchoredPosition = new Vector2(20f, -60f);
            rtDesc.sizeDelta = new Vector2(-40f, 36f);
            selectedDescText = descObj.AddComponent<Text>();
            selectedDescText.font = font;
            selectedDescText.fontSize = 11;
            selectedDescText.color = new Color(0.72f, 0.78f, 0.84f);
            selectedDescText.raycastTarget = false;

            // Stat Preview Badge
            GameObject prevObj = new GameObject("StatPreviewBadge");
            prevObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtPrev = prevObj.AddComponent<RectTransform>();
            rtPrev.anchorMin = new Vector2(0f, 1f);
            rtPrev.anchorMax = new Vector2(1f, 1f);
            rtPrev.anchoredPosition = new Vector2(20f, -100f);
            rtPrev.sizeDelta = new Vector2(-40f, 26f);

            Image prevBg = prevObj.AddComponent<Image>();
            prevBg.color = new Color(0.16f, 0.32f, 0.24f, 0.90f);
            prevBg.raycastTarget = false;

            GameObject ptObj = new GameObject("PreviewText");
            ptObj.transform.SetParent(prevObj.transform, false);
            RectTransform rtPt = ptObj.AddComponent<RectTransform>();
            rtPt.anchorMin = Vector2.zero;
            rtPt.anchorMax = Vector2.one;
            rtPt.sizeDelta = Vector2.zero;
            selectedPreviewText = ptObj.AddComponent<Text>();
            selectedPreviewText.font = font;
            selectedPreviewText.fontSize = 11;
            selectedPreviewText.fontStyle = FontStyle.Bold;
            selectedPreviewText.alignment = TextAnchor.MiddleCenter;
            selectedPreviewText.color = new Color(0.40f, 0.95f, 0.60f);
            selectedPreviewText.raycastTarget = false;

            // Ingredients Header
            GameObject ingHdrObj = new GameObject("IngredientsHeader");
            ingHdrObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtIh = ingHdrObj.AddComponent<RectTransform>();
            rtIh.anchorMin = new Vector2(0f, 1f);
            rtIh.anchorMax = new Vector2(1f, 1f);
            rtIh.anchoredPosition = new Vector2(20f, -135f);
            rtIh.sizeDelta = new Vector2(-40f, 22f);
            Text ihText = ingHdrObj.AddComponent<Text>();
            ihText.font = font;
            ihText.fontSize = 11;
            ihText.fontStyle = FontStyle.Bold;
            ihText.color = new Color(0.55f, 0.62f, 0.72f);
            ihText.text = "GEREKLİ BİLEŞENLER";
            ihText.raycastTarget = false;

            // Ingredients List Container
            GameObject ingListObj = new GameObject("IngredientsContainer");
            ingListObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtIl = ingListObj.AddComponent<RectTransform>();
            rtIl.anchorMin = new Vector2(0f, 1f);
            rtIl.anchorMax = new Vector2(1f, 1f);
            rtIl.anchoredPosition = new Vector2(20f, -165f);
            rtIl.sizeDelta = new Vector2(-40f, 160f);

            ingredientCardsContainer = ingListObj.transform;
            VerticalLayoutGroup vlgIng = ingListObj.AddComponent<VerticalLayoutGroup>();
            vlgIng.spacing = 6f;
            vlgIng.childControlWidth = true;
            vlgIng.childControlHeight = false;

            // Progress Bar Container
            progressContainer = new GameObject("ProgressContainer");
            progressContainer.transform.SetParent(rightCol.transform, false);
            RectTransform rtPc = progressContainer.AddComponent<RectTransform>();
            rtPc.anchorMin = new Vector2(0f, 0f);
            rtPc.anchorMax = new Vector2(1f, 0f);
            rtPc.anchoredPosition = new Vector2(20f, 75f);
            rtPc.sizeDelta = new Vector2(-40f, 12f);

            Image track = progressContainer.AddComponent<Image>();
            track.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);
            track.raycastTarget = false;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progressContainer.transform, false);
            RectTransform rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.sizeDelta = Vector2.zero;

            craftProgressBar = fillObj.AddComponent<Image>();
            craftProgressBar.type = Image.Type.Filled;
            craftProgressBar.fillMethod = Image.FillMethod.Horizontal;
            craftProgressBar.color = new Color(0.25f, 0.85f, 0.55f);
            craftProgressBar.raycastTarget = false;
            progressContainer.SetActive(false);

            // Craft Button
            GameObject craftBtnObj = new GameObject("Btn_Craft");
            craftBtnObj.transform.SetParent(rightCol.transform, false);
            RectTransform rtCb = craftBtnObj.AddComponent<RectTransform>();
            rtCb.anchorMin = new Vector2(0f, 0f);
            rtCb.anchorMax = new Vector2(1f, 0f);
            rtCb.anchoredPosition = new Vector2(20f, 24f);
            rtCb.sizeDelta = new Vector2(-40f, 44f);

            Image btnImg = craftBtnObj.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.65f, 0.38f, 0.95f);
            btnImg.raycastTarget = true;

            craftButton = craftBtnObj.AddComponent<Button>();
            craftButton.targetGraphic = btnImg;
            craftButton.onClick.AddListener(OnCraftButtonClicked);

            GameObject cbTxtObj = new GameObject("Text");
            cbTxtObj.transform.SetParent(craftBtnObj.transform, false);
            RectTransform rtCbt = cbTxtObj.AddComponent<RectTransform>();
            rtCbt.anchorMin = Vector2.zero;
            rtCbt.anchorMax = Vector2.one;
            rtCbt.sizeDelta = Vector2.zero;
            craftButtonText = cbTxtObj.AddComponent<Text>();
            craftButtonText.font = font;
            craftButtonText.fontSize = 14;
            craftButtonText.fontStyle = FontStyle.Bold;
            craftButtonText.alignment = TextAnchor.MiddleCenter;
            craftButtonText.color = Color.white;
            craftButtonText.text = "ÜRET (1.5 sn)";
            craftButtonText.raycastTarget = false;
        }

        private void WireExistingReferences()
        {
            if (panelRoot == null) return;

            stationTitleText = panelRoot.transform.Find("Modal_Window/Header/StationTitle")?.GetComponent<Text>();
            stationSubtitleText = panelRoot.transform.Find("Modal_Window/Header/StationSubtitle")?.GetComponent<Text>();
            recipeListContainer = panelRoot.transform.Find("Modal_Window/LeftColumn_RecipeList");

            Transform right = panelRoot.transform.Find("Modal_Window/RightColumn_Details");
            if (right != null)
            {
                selectedTitleText = right.Find("SelectedTitle")?.GetComponent<Text>();
                selectedDescText = right.Find("SelectedDesc")?.GetComponent<Text>();
                selectedPreviewText = right.Find("StatPreviewBadge/PreviewText")?.GetComponent<Text>();
                ingredientCardsContainer = right.Find("IngredientsContainer");
                progressContainer = right.Find("ProgressContainer")?.gameObject;
                craftProgressBar = progressContainer?.transform.Find("Fill")?.GetComponent<Image>();
                craftButton = right.Find("Btn_Craft")?.GetComponent<Button>();
                craftButtonText = right.Find("Btn_Craft/Text")?.GetComponent<Text>();
            }
        }
    }
}
