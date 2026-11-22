using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    public class BackpackUI : MonoBehaviour
    {
        public static BackpackUI Instance { get; private set; }

        [Header("Main Panel")]
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private Button closeButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button notesTabButton;
        [SerializeField] private GameObject inventoryContentArea;
        [SerializeField] private GameObject notesContentArea;

        [Header("Inventory Elements")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Text selectedItemTitle;
        [SerializeField] private Text selectedItemDescription;
        [SerializeField] private Button useItemButton;
        [SerializeField] private Button dropItemButton;

        [Header("Notes Elements")]
        [SerializeField] private Transform notesContainer;

        private int selectedSlotIndex = -1;
        private readonly List<KeyValuePair<string, string>> collectedNotes = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Salgın Başlangıç Direktifi", "Tüm sivillerin acilen yerel sığınaklara çekilmesi emredildi. Enfekte olanlar sese ve ışığa koşuyor."),
            new KeyValuePair<string, string>("Dr. Aras'ın Not Defteri", "Kliniğin acil servisindeki tıbbi malzemeler enfeksiyonun yayılmasını yavaşlatabilecek tek şey...")
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(() => SwitchTab(true));
            if (notesTabButton != null) notesTabButton.onClick.AddListener(() => SwitchTab(false));
            if (useItemButton != null) useItemButton.onClick.AddListener(OnUseItemClicked);
            if (dropItemButton != null) dropItemButton.onClick.AddListener(OnDropItemClicked);

            if (backpackPanel != null) backpackPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnOpenBackpackRequested += TogglePanel;
            GameEvents.OnNoteCollected += AddNote;
            GameEvents.OnInventoryUpdated += RefreshUI;
        }

        private void OnDisable()
        {
            GameEvents.OnOpenBackpackRequested -= TogglePanel;
            GameEvents.OnNoteCollected -= AddNote;
            GameEvents.OnInventoryUpdated -= RefreshUI;
        }

        public bool IsOpen => backpackPanel != null && backpackPanel.activeSelf;

        public void TogglePanel()
        {
            bool nextState = !backpackPanel.activeSelf;
            if (nextState)
            {
                ModalWindowManager.Instance?.NotifyModalOpened(this);
            }
            else
            {
                ModalWindowManager.Instance?.NotifyModalClosed(this);
            }
            backpackPanel.SetActive(nextState);

            if (nextState)
            {
                ProceduralAudioManager.Instance?.PlayButtonClick();
                SwitchTab(true);
                RefreshUI();
            }
        }

        public void ClosePanel()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            ProceduralAudioManager.Instance?.PlayButtonClick();
            if (backpackPanel != null) backpackPanel.SetActive(false);
        }

        private void SwitchTab(bool isInventory)
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            if (inventoryContentArea != null) inventoryContentArea.SetActive(isInventory);
            if (notesContentArea != null) notesContentArea.SetActive(!isInventory);

            if (!isInventory)
            {
                RefreshNotesUI();
            }
            else
            {
                RefreshInventoryUI();
            }
        }

        public void RefreshUI()
        {
            if (backpackPanel != null && backpackPanel.activeSelf)
            {
                RefreshInventoryUI();
            }
        }

        private void RefreshInventoryUI()
        {
            if (InventorySystem.Instance == null) return;

            var slots = InventorySystem.Instance.Slots;

            // Clear old items
            if (slotsContainer != null)
            {
                for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(slotsContainer.GetChild(i).gameObject);
                }

                for (int i = 0; i < slots.Count; i++)
                {
                    int index = i;
                    var slot = slots[i];
                    GameObject slotObj = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    slotObj.transform.SetParent(slotsContainer, false);

                    Image img = slotObj.GetComponent<Image>();
                    img.color = (index == selectedSlotIndex) ? new Color(0.2f, 0.6f, 0.8f, 0.85f) : new Color(0.15f, 0.18f, 0.22f, 0.85f);

                    // Text
                    GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    textObj.transform.SetParent(slotObj.transform, false);
                    Text t = textObj.GetComponent<Text>();
                    t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    t.fontSize = 13;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.color = Color.white;
                    t.text = slot.item != null ? $"{slot.item.itemName}\nx{slot.quantity}" : "Boş";

                    RectTransform textRt = textObj.GetComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.sizeDelta = Vector2.zero;

                    Button btn = slotObj.GetComponent<Button>();
                    btn.onClick.AddListener(() => SelectSlot(index));
                }
            }

            UpdateSelectedItemDetails();
        }

        private void SelectSlot(int index)
        {
            selectedSlotIndex = index;
            ProceduralAudioManager.Instance?.PlayButtonClick();
            RefreshInventoryUI();
        }

        private void UpdateSelectedItemDetails()
        {
            if (InventorySystem.Instance == null || selectedSlotIndex < 0 || selectedSlotIndex >= InventorySystem.Instance.Slots.Count)
            {
                if (selectedItemTitle != null) selectedItemTitle.text = "Eşya Seçilmedi";
                if (selectedItemDescription != null) selectedItemDescription.text = "Ayrıntılarını görmek için bir eşyaya dokunun.";
                if (useItemButton != null) useItemButton.interactable = false;
                if (dropItemButton != null) dropItemButton.interactable = false;
                return;
            }

            var slot = InventorySystem.Instance.Slots[selectedSlotIndex];
            if (slot.item != null)
            {
                if (selectedItemTitle != null) selectedItemTitle.text = $"{slot.item.itemName} (x{slot.quantity})";
                if (selectedItemDescription != null) selectedItemDescription.text = slot.item.description;
                if (useItemButton != null) useItemButton.interactable = true;
                if (dropItemButton != null) dropItemButton.interactable = true;
            }
            else
            {
                if (selectedItemTitle != null) selectedItemTitle.text = "Boş Slot";
                if (selectedItemDescription != null) selectedItemDescription.text = string.Empty;
                if (useItemButton != null) useItemButton.interactable = false;
                if (dropItemButton != null) dropItemButton.interactable = false;
            }
        }

        private void OnUseItemClicked()
        {
            if (InventorySystem.Instance == null || selectedSlotIndex < 0) return;
            InventorySystem.Instance.UseItem(selectedSlotIndex);
            ProceduralAudioManager.Instance?.PlayButtonClick();
            RefreshInventoryUI();
        }

        private void OnDropItemClicked()
        {
            if (InventorySystem.Instance == null || selectedSlotIndex < 0) return;
            var slot = InventorySystem.Instance.Slots[selectedSlotIndex];
            if (slot.item != null)
            {
                InventorySystem.Instance.RemoveItem(slot.item.itemId, 1);
                ProceduralAudioManager.Instance?.PlayButtonClick();
                RefreshInventoryUI();
            }
        }

        private void AddNote(string title, string content)
        {
            collectedNotes.Add(new KeyValuePair<string, string>(title, content));
            if (backpackPanel != null && backpackPanel.activeSelf)
            {
                RefreshNotesUI();
            }
        }

        private void RefreshNotesUI()
        {
            if (notesContainer == null) return;

            for (int i = notesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(notesContainer.GetChild(i).gameObject);
            }

            foreach (var note in collectedNotes)
            {
                GameObject noteObj = new GameObject("NoteItem", typeof(RectTransform), typeof(Image));
                noteObj.transform.SetParent(notesContainer, false);
                noteObj.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 0.9f);

                RectTransform rt = noteObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 70);

                GameObject textObj = new GameObject("NoteText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(noteObj.transform, false);
                Text t = textObj.GetComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.fontSize = 13;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = Color.white;
                t.text = $"<b>{note.Key}</b>\n<color=#B0BEC5>{note.Value}</color>";

                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.05f, 0f);
                textRt.anchorMax = new Vector2(0.95f, 1f);
                textRt.sizeDelta = Vector2.zero;
            }
        }
    }
}
