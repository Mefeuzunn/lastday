using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    public class StashUI : MonoBehaviour
    {
        public static StashUI Instance { get; private set; }

        [Header("Panel Root")]
        [SerializeField] private GameObject stashPanel;
        [SerializeField] private Button closeButton;

        [Header("Columns")]
        [SerializeField] private Transform playerSlotsContainer;
        [SerializeField] private Transform stashSlotsContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (stashPanel != null) stashPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnOpenStashRequested += OpenPanel;
            if (StashContainer.Instance != null) StashContainer.Instance.OnStashUpdated += RefreshUI;
            GameEvents.OnInventoryUpdated += RefreshUI;
        }

        private void OnDisable()
        {
            GameEvents.OnOpenStashRequested -= OpenPanel;
            if (StashContainer.Instance != null) StashContainer.Instance.OnStashUpdated -= RefreshUI;
            GameEvents.OnInventoryUpdated -= RefreshUI;
        }

        public bool IsOpen => stashPanel != null && stashPanel.activeSelf;

        public void OpenPanel()
        {
            if (stashPanel != null)
            {
                ModalWindowManager.Instance?.NotifyModalOpened(this);
                stashPanel.SetActive(true);
                ProceduralAudioManager.Instance?.PlayButtonClick();
                RefreshUI();
            }
        }

        public void ClosePanel()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            if (stashPanel != null)
            {
                stashPanel.SetActive(false);
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }
        }

        public void RefreshUI()
        {
            if (stashPanel == null || !stashPanel.activeSelf) return;

            RefreshPlayerInventoryColumn();
            RefreshStashColumn();
        }

        private void RefreshPlayerInventoryColumn()
        {
            if (playerSlotsContainer == null || InventorySystem.Instance == null) return;

            for (int i = playerSlotsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(playerSlotsContainer.GetChild(i).gameObject);
            }

            var slots = InventorySystem.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.item == null || slot.quantity <= 0) continue;

                int index = i;
                GameObject itemObj = CreateSlotButton(playerSlotsContainer, $"{slot.item.itemName} (x{slot.quantity})\n[DEPOYA KOY]", () =>
                {
                    if (StashContainer.Instance != null && StashContainer.Instance.StoreItem(slot.item, 1))
                    {
                        InventorySystem.Instance.RemoveItem(slot.item.itemId, 1);
                        ProceduralAudioManager.Instance?.PlayButtonClick();
                        RefreshUI();
                    }
                });
            }
        }

        private void RefreshStashColumn()
        {
            if (stashSlotsContainer == null || StashContainer.Instance == null) return;

            for (int i = stashSlotsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(stashSlotsContainer.GetChild(i).gameObject);
            }

            var slots = StashContainer.Instance.StoredSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.item == null || slot.quantity <= 0) continue;

                string itemId = slot.item.itemId;
                GameObject itemObj = CreateSlotButton(stashSlotsContainer, $"{slot.item.itemName} (x{slot.quantity})\n[ÇANTAYA AL]", () =>
                {
                    if (StashContainer.Instance != null && StashContainer.Instance.WithdrawItem(itemId, 1))
                    {
                        ProceduralAudioManager.Instance?.PlayButtonClick();
                        RefreshUI();
                    }
                });
            }
        }

        private GameObject CreateSlotButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject("SlotBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            obj.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(obj.transform, false);
            Text t = textObj.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            obj.GetComponent<Button>().onClick.AddListener(onClick);
            return obj;
        }
    }
}
