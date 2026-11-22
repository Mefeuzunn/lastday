using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    public class SurvivorBarterUI : MonoBehaviour
    {
        public static SurvivorBarterUI Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject barterRootPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text trustBadgeText;

        [Header("Left Pane: Player Items")]
        [SerializeField] private Transform playerContainer;
        [SerializeField] private Text playerTotalValueText;

        [Header("Right Pane: Merchant Items")]
        [SerializeField] private Transform merchantContainer;
        [SerializeField] private Text merchantTotalValueText;

        [Header("Center Control Panel")]
        [SerializeField] private Text balanceStatusText;
        [SerializeField] private Slider balanceSlider;
        [SerializeField] private Button confirmTradeButton;
        [SerializeField] private Button resetSelectionButton;
        [SerializeField] private Button closeButton;

        private string activeNpcId = "";
        private string activeNpcName = "";

        private readonly Dictionary<string, int> playerOfferedItems = new Dictionary<string, int>();
        private readonly Dictionary<string, int> requestedMerchantItems = new Dictionary<string, int>();

        private readonly List<GameObject> spawnedPlayerButtons = new List<GameObject>();
        private readonly List<GameObject> spawnedMerchantButtons = new List<GameObject>();

        public bool IsOpen => barterRootPanel != null && barterRootPanel.activeSelf;
        public bool IsBarterActive => IsOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();

            if (confirmTradeButton != null) confirmTradeButton.onClick.AddListener(OnConfirmTradeClicked);
            if (resetSelectionButton != null) resetSelectionButton.onClick.AddListener(OnResetSelectionClicked);
            if (closeButton != null) closeButton.onClick.AddListener(CloseBarter);

            if (barterRootPanel != null) barterRootPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnBarterRequested += OpenBarter;
        }

        private void OnDisable()
        {
            GameEvents.OnBarterRequested -= OpenBarter;
        }

        public void OpenBarter(string npcId, string displayName)
        {
            activeNpcId = npcId;
            activeNpcName = displayName;

            if (barterRootPanel != null) barterRootPanel.SetActive(true);

            playerOfferedItems.Clear();
            requestedMerchantItems.Clear();

            RefreshUI();
        }

        public void CloseBarter()
        {
            playerOfferedItems.Clear();
            requestedMerchantItems.Clear();

            if (barterRootPanel != null) barterRootPanel.SetActive(false);
        }

        private void OnResetSelectionClicked()
        {
            playerOfferedItems.Clear();
            requestedMerchantItems.Clear();
            RefreshUI();
        }

        private void OnConfirmTradeClicked()
        {
            if (BarterSystem.Instance == null) return;

            bool success = BarterSystem.Instance.ExecuteBarter(activeNpcId, playerOfferedItems, requestedMerchantItems);
            if (success)
            {
                playerOfferedItems.Clear();
                requestedMerchantItems.Clear();
                RefreshUI();
            }
        }

        public void RefreshUI()
        {
            if (BarterSystem.Instance == null) return;

            int trust = BarterSystem.Instance.GetTrust(activeNpcId);
            float discount = (1f - BarterSystem.Instance.GetTrustDiscountMultiplier(activeNpcId)) * 100f;

            if (titleText != null) titleText.text = $"[TAKAS] {activeNpcName}";
            if (trustBadgeText != null) trustBadgeText.text = $"GÜVEN: %{trust}  •  İNDİRİM: %{discount:F0}";

            PopulatePlayerInventory();
            PopulateMerchantStock();
            UpdateTradeCalculations();
        }

        private void PopulatePlayerInventory()
        {
            ClearChildren(spawnedPlayerButtons);
            InventorySystem inv = InventorySystem.Instance;
            if (inv == null || playerContainer == null) return;

            var slots = inv.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.item == null || slot.quantity <= 0) continue;

                string itemId = slot.item.itemId;
                int totalOwned = slot.quantity;
                playerOfferedItems.TryGetValue(itemId, out int offeredCount);

                int unitVal = BarterSystem.Instance.GetItemValue(itemId);

                GameObject btnObj = CreateTradeSlotButton(playerContainer, slot.item.itemName, unitVal, totalOwned, offeredCount, isPlayerSide: true, isLocked: false);
                Button b = btnObj.GetComponent<Button>();
                b.onClick.AddListener(() =>
                {
                    if (offeredCount < totalOwned)
                    {
                        playerOfferedItems[itemId] = offeredCount + 1;
                    }
                    else
                    {
                        playerOfferedItems[itemId] = 0;
                    }
                    UpdateTradeCalculations();
                    RefreshUI();
                });

                spawnedPlayerButtons.Add(btnObj);
            }
        }

        private void PopulateMerchantStock()
        {
            ClearChildren(spawnedMerchantButtons);
            if (BarterSystem.Instance == null || merchantContainer == null) return;

            var stock = BarterSystem.Instance.GetMerchantStock(activeNpcId);
            int trust = BarterSystem.Instance.GetTrust(activeNpcId);
            float discount = BarterSystem.Instance.GetTrustDiscountMultiplier(activeNpcId);

            for (int i = 0; i < stock.Count; i++)
            {
                var entry = stock[i];
                if (entry.availableQuantity <= 0) continue;

                ItemData itemData = InventorySystem.FindItemData(entry.itemId);
                string itemName = itemData != null ? itemData.itemName : entry.itemId;

                bool isLocked = trust < entry.minTrustRequired;
                int effectivePrice = Mathf.Max(1, Mathf.RoundToInt(entry.baseValue * discount));

                requestedMerchantItems.TryGetValue(entry.itemId, out int requestedCount);

                GameObject btnObj = CreateTradeSlotButton(merchantContainer, itemName, effectivePrice, entry.availableQuantity, requestedCount, isPlayerSide: false, isLocked: isLocked);
                Button b = btnObj.GetComponent<Button>();

                if (!isLocked)
                {
                    b.onClick.AddListener(() =>
                    {
                        if (requestedCount < entry.availableQuantity)
                        {
                            requestedMerchantItems[entry.itemId] = requestedCount + 1;
                        }
                        else
                        {
                            requestedMerchantItems[entry.itemId] = 0;
                        }
                        UpdateTradeCalculations();
                        RefreshUI();
                    });
                }

                spawnedMerchantButtons.Add(btnObj);
            }
        }

        private void UpdateTradeCalculations()
        {
            if (BarterSystem.Instance == null) return;

            int playerTotal = 0;
            foreach (var kvp in playerOfferedItems)
            {
                playerTotal += BarterSystem.Instance.GetItemValue(kvp.Key) * kvp.Value;
            }

            float discount = BarterSystem.Instance.GetTrustDiscountMultiplier(activeNpcId);
            int merchantTotal = 0;
            foreach (var kvp in requestedMerchantItems)
            {
                merchantTotal += Mathf.Max(1, Mathf.RoundToInt(BarterSystem.Instance.GetItemValue(kvp.Key) * kvp.Value * discount));
            }

            if (playerTotalValueText != null) playerTotalValueText.text = $"Teklif Değeri: {playerTotal}";
            if (merchantTotalValueText != null) merchantTotalValueText.text = $"İstenen Değer: {merchantTotal}";

            int diff = playerTotal - merchantTotal;
            bool canTrade = merchantTotal > 0 && diff >= 0;

            if (balanceStatusText != null)
            {
                if (merchantTotal == 0)
                {
                    balanceStatusText.text = "Tüccardan eşya seçiniz";
                    balanceStatusText.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else if (diff >= 0)
                {
                    balanceStatusText.text = $"DENGELİ TAKAS (+{diff} Fazlalık)";
                    balanceStatusText.color = new Color(0.35f, 0.85f, 0.45f);
                }
                else
                {
                    balanceStatusText.text = $"YETERSİZ TEKLİF ({diff} Değer Eksik)";
                    balanceStatusText.color = new Color(0.95f, 0.35f, 0.35f);
                }
            }

            if (balanceSlider != null)
            {
                balanceSlider.minValue = 0;
                balanceSlider.maxValue = Mathf.Max(1, merchantTotal * 2);
                balanceSlider.value = Mathf.Clamp(playerTotal, 0, balanceSlider.maxValue);
            }

            if (confirmTradeButton != null)
            {
                confirmTradeButton.interactable = canTrade;
            }
        }

        private GameObject CreateTradeSlotButton(Transform parent, string itemName, int unitValue, int totalQty, int selectedQty, bool isPlayerSide, bool isLocked)
        {
            GameObject slotObj = new GameObject($"Slot_{itemName}", typeof(RectTransform));
            slotObj.transform.SetParent(parent, false);

            RectTransform rect = slotObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 48f);

            Image img = slotObj.AddComponent<Image>();
            img.color = isLocked
                ? new Color(0.12f, 0.12f, 0.14f, 0.8f)
                : (selectedQty > 0 ? new Color(0.18f, 0.32f, 0.42f, 0.95f) : new Color(0.12f, 0.16f, 0.22f, 0.85f));

            Button btn = slotObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = !isLocked;

            GameObject textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(slotObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);

            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            t.fontSize = 13;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;

            if (isLocked)
            {
                t.text = $"<color=#888888>[KİLİTLİ] {itemName}</color>";
            }
            else
            {
                string tag = selectedQty > 0 ? $"<color=#2ECC71>[{selectedQty}/{totalQty}]</color>" : $"({totalQty})";
                t.text = $"{itemName} {tag}  <color=#F1C40F>{unitValue}P</color>";
            }

            return slotObj;
        }

        private void ClearChildren(List<GameObject> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) Destroy(list[i]);
            }
            list.Clear();
        }

        private void EnsureUIHierarchy()
        {
            if (barterRootPanel == null)
            {
                barterRootPanel = gameObject;
            }
        }
    }
}
