using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Vehicles;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 1: Mobile-Optimized Vehicle Trunk Stash Interface.
    /// Manages an 8-to-16 slot mobile storage container that travels with the vehicle,
    /// enabling players to haul bulk resources back to the Safe House.
    /// </summary>
    public class VehicleTrunkUI : MonoBehaviour
    {
        private static VehicleTrunkUI _instance;
        public static VehicleTrunkUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VehicleTrunkUI>();
                    if (_instance == null)
                    {
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            GameObject go = new GameObject("UI_VehicleTrunk");
                            go.transform.SetParent(canvas.transform, false);
                            _instance = go.AddComponent<VehicleTrunkUI>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Root Panel")]
        [SerializeField] private GameObject trunkPanelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text capacityText;

        [Header("Grids")]
        [SerializeField] private Transform trunkGridContainer;
        [SerializeField] private Transform playerGridContainer;

        [Header("Buttons")]
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button storeAllButton;
        [SerializeField] private Button closeButton;

        private DrivableVehicleController activeVehicle;
        private VehicleTrunkStorage activeStorage;
        private readonly List<GameObject> spawnedTrunkSlotObjs = new List<GameObject>();
        private readonly List<GameObject> spawnedPlayerSlotObjs = new List<GameObject>();

        public bool IsOpen => trunkPanelRoot != null && trunkPanelRoot.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                // Trigger getter
                var inst = Instance;
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

            BuildUIHierarchyIfMissing();
            if (trunkPanelRoot != null) trunkPanelRoot.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseTrunk();
            }
        }

        public void OpenTrunkWithStorage(VehicleTrunkStorage storage)
        {
            if (storage == null) return;
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            activeStorage = storage;
            activeVehicle = storage.GetComponent<DrivableVehicleController>();

            if (trunkPanelRoot == null) BuildUIHierarchyIfMissing();
            if (trunkPanelRoot != null) trunkPanelRoot.SetActive(true);

            if (titleText != null)
            {
                string vName = activeVehicle != null ? activeVehicle.VehicleDisplayName.ToUpper() : "HERO PICKUP";
                titleText.text = $"<b>ARAÇ BAGAJI — {vName}</b>";
            }

            RefreshUI();
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void OpenTrunk(DrivableVehicleController vehicle)
        {
            if (vehicle == null) return;
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            activeVehicle = vehicle;
            activeStorage = vehicle.GetComponent<VehicleTrunkStorage>();

            if (trunkPanelRoot == null) BuildUIHierarchyIfMissing();

            if (trunkPanelRoot != null) trunkPanelRoot.SetActive(true);

            if (titleText != null)
            {
                titleText.text = $"<b>ARAÇ BAGAJI — {vehicle.VehicleDisplayName.ToUpper()}</b>";
            }

            RefreshUI();
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void CloseTrunk()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            if (trunkPanelRoot != null) trunkPanelRoot.SetActive(false);
            if (activeStorage != null && activeStorage.IsTrunkOpen)
            {
                activeStorage.CloseTrunk();
            }
            activeVehicle = null;
            activeStorage = null;

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void RefreshUI()
        {
            if (activeVehicle == null && activeStorage == null) return;

            // Update Capacity Text
            int currentCount = (activeStorage != null) ? activeStorage.OccupiedSlots : activeVehicle.TrunkInventory.Count;
            int maxCap = (activeStorage != null) ? activeStorage.MaxSlots : activeVehicle.TrunkCapacity;
            if (capacityText != null)
            {
                capacityText.text = $"KAPASİTE: {currentCount} / {maxCap} SLOT";
            }

            // 1. Populate Trunk Slots
            PopulateTrunkSlots();

            // 2. Populate Player Slots
            PopulatePlayerSlots();
        }

        private void PopulateTrunkSlots()
        {
            for (int i = 0; i < spawnedTrunkSlotObjs.Count; i++)
            {
                Destroy(spawnedTrunkSlotObjs[i]);
            }
            spawnedTrunkSlotObjs.Clear();

            if (trunkGridContainer == null) return;
            if (activeVehicle == null && activeStorage == null) return;

            int totalSlots = (activeStorage != null) ? activeStorage.MaxSlots : activeVehicle.TrunkCapacity;
            IReadOnlyList<InventorySlot> trunkList = (activeStorage != null)
                ? activeStorage.Items
                : (IReadOnlyList<InventorySlot>)activeVehicle.TrunkInventory;

            for (int i = 0; i < totalSlots; i++)
            {
                int slotIndex = i;
                InventorySlot slotData = (i < trunkList.Count) ? trunkList[i] : null;

                GameObject slotObj = CreateSlotElement(trunkGridContainer, slotData, () =>
                {
                    OnTrunkSlotClicked(slotIndex);
                });

                spawnedTrunkSlotObjs.Add(slotObj);
            }
        }

        private void PopulatePlayerSlots()
        {
            for (int i = 0; i < spawnedPlayerSlotObjs.Count; i++)
            {
                Destroy(spawnedPlayerSlotObjs[i]);
            }
            spawnedPlayerSlotObjs.Clear();

            if (playerGridContainer == null || InventorySystem.Instance == null) return;

            var playerSlots = InventorySystem.Instance.Slots;
            for (int i = 0; i < playerSlots.Count; i++)
            {
                int playerSlotIndex = i;
                InventorySlot slotData = playerSlots[i];

                GameObject slotObj = CreateSlotElement(playerGridContainer, slotData, () =>
                {
                    OnPlayerSlotClicked(playerSlotIndex);
                });

                spawnedPlayerSlotObjs.Add(slotObj);
            }
        }

        private GameObject CreateSlotElement(Transform parent, InventorySlot slot, System.Action onClick)
        {
            GameObject slotObj = new GameObject("Slot_Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObj.transform.SetParent(parent, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(58f, 58f);

            Image bg = slotObj.GetComponent<Image>();
            bg.color = (slot != null && slot.item != null)
                ? new Color(0.15f, 0.20f, 0.28f, 0.95f)
                : new Color(0.08f, 0.10f, 0.14f, 0.75f);

            Button btn = slotObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            if (slot != null && slot.item != null)
            {
                // Item Name / Count text
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(slotObj.transform, false);
                RectTransform trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(3f, 3f);
                trt.offsetMax = new Vector2(-3f, -3f);

                Text t = textObj.GetComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 10;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.supportRichText = true;

                string shortName = slot.item.itemName;
                if (shortName.Length > 9) shortName = shortName.Substring(0, 8) + ".";
                t.text = $"<b>{shortName}</b>\n<color=#F59E0B>x{slot.quantity}</color>";
            }

            return slotObj;
        }

        private void OnTrunkSlotClicked(int index)
        {
            if (activeStorage != null)
            {
                activeStorage.TransferToPlayer(index);
                RefreshUI();
                return;
            }

            if (activeVehicle == null || InventorySystem.Instance == null) return;
            var trunk = activeVehicle.TrunkInventory;
            if (index < 0 || index >= trunk.Count) return;

            InventorySlot slot = trunk[index];
            if (slot == null || slot.item == null || slot.quantity <= 0) return;

            // Transfer from trunk to player
            if (InventorySystem.Instance.AddItem(slot.item, slot.quantity))
            {
                trunk.RemoveAt(index);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                RefreshUI();
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("ÇANTA DOLU!", 2.0f);
            }
        }

        private void OnPlayerSlotClicked(int index)
        {
            if (activeStorage != null)
            {
                var slots = InventorySystem.Instance != null ? InventorySystem.Instance.Slots : null;
                if (slots == null || index < 0 || index >= slots.Count) return;
                var s = slots[index];
                if (s == null || s.item == null || s.quantity <= 0) return;
                activeStorage.StoreItem(s.item, s.quantity);
                RefreshUI();
                return;
            }

            if (activeVehicle == null || InventorySystem.Instance == null) return;
            var pSlots = InventorySystem.Instance.Slots;
            if (index < 0 || index >= pSlots.Count) return;

            InventorySlot slot = pSlots[index];
            if (slot == null || slot.item == null || slot.quantity <= 0) return;

            var trunk = activeVehicle.TrunkInventory;

            // Check if stackable in trunk
            bool stacked = false;
            for (int i = 0; i < trunk.Count; i++)
            {
                if (trunk[i].item != null && trunk[i].item.itemId == slot.item.itemId && trunk[i].quantity < slot.item.maxStack)
                {
                    int space = slot.item.maxStack - trunk[i].quantity;
                    int toAdd = Mathf.Min(space, slot.quantity);
                    trunk[i].quantity += toAdd;

                    InventorySystem.Instance.RemoveItem(slot.item.itemId, toAdd);
                    stacked = true;
                    break;
                }
            }

            if (!stacked)
            {
                // Add new slot in trunk if space exists
                if (trunk.Count < activeVehicle.TrunkCapacity)
                {
                    trunk.Add(new InventorySlot(slot.item, slot.quantity));
                    InventorySystem.Instance.RemoveItem(slot.item.itemId, slot.quantity);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("ARAÇ BAGAJI TAMAMEN DOLU!", 2.0f);
                    return;
                }
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.Instance?.TriggerLight();
            RefreshUI();
        }

        private void TakeAllItems()
        {
            if (activeStorage != null)
            {
                activeStorage.TransferAllToPlayer();
                RefreshUI();
                return;
            }

            if (activeVehicle == null || InventorySystem.Instance == null) return;

            var trunk = activeVehicle.TrunkInventory;
            for (int i = trunk.Count - 1; i >= 0; i--)
            {
                var s = trunk[i];
                if (s.item != null && s.quantity > 0)
                {
                    if (InventorySystem.Instance.AddItem(s.item, s.quantity))
                    {
                        trunk.RemoveAt(i);
                    }
                }
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.Instance?.TriggerMedium();
            RefreshUI();
        }

        private void StoreAllItems()
        {
            if (activeStorage != null)
            {
                activeStorage.StoreAllFromPlayer();
                RefreshUI();
                return;
            }

            if (activeVehicle == null || InventorySystem.Instance == null) return;

            var pSlots = InventorySystem.Instance.Slots;
            for (int i = pSlots.Count - 1; i >= 0; i--)
            {
                if (activeVehicle.TrunkInventory.Count >= activeVehicle.TrunkCapacity) break;

                var s = pSlots[i];
                if (s != null && s.item != null && s.quantity > 0)
                {
                    // Don't auto-store weapons or medkits in quickslots
                    if (s.item.itemType == ItemType.Weapon) continue;

                    activeVehicle.TrunkInventory.Add(new InventorySlot(s.item, s.quantity));
                    InventorySystem.Instance.RemoveItem(s.item.itemId, s.quantity);
                }
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.Instance?.TriggerMedium();
            RefreshUI();
        }

        // =========================================================================
        // PROCEDURAL UI GENERATION (URP Glassmorphism)
        // =========================================================================

        private void BuildUIHierarchyIfMissing()
        {
            if (trunkPanelRoot != null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas == null) return;

            // 1. Root Modal Panel
            GameObject panelGo = new GameObject("Panel_VehicleTrunkModal", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parentCanvas.transform, false);

            RectTransform rootRt = panelGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(640f, 480f);

            Image panelBg = panelGo.GetComponent<Image>();
            panelBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            trunkPanelRoot = panelGo;

            // 2. Header Title
            GameObject titleGo = new GameObject("Text_HeaderTitle", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelGo.transform, false);
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(20f, -44f);
            trt.offsetMax = new Vector2(-20f, -10f);

            titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.96f, 0.72f, 0.18f);
            titleText.supportRichText = true;

            // 3. Capacity Text
            GameObject capGo = new GameObject("Text_Capacity", typeof(RectTransform), typeof(Text));
            capGo.transform.SetParent(panelGo.transform, false);
            RectTransform crt = capGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.offsetMin = new Vector2(0f, -44f);
            crt.offsetMax = new Vector2(-50f, -10f);

            capacityText = capGo.GetComponent<Text>();
            capacityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            capacityText.fontSize = 12;
            capacityText.alignment = TextAnchor.MiddleRight;
            capacityText.color = new Color(0.75f, 0.80f, 0.85f);

            // 4. Trunk Grid Container
            GameObject trunkGridGo = new GameObject("Grid_Trunk", typeof(RectTransform), typeof(GridLayoutGroup));
            trunkGridGo.transform.SetParent(panelGo.transform, false);
            RectTransform tgrt = trunkGridGo.GetComponent<RectTransform>();
            tgrt.anchorMin = new Vector2(0.05f, 0.52f);
            tgrt.anchorMax = new Vector2(0.95f, 0.88f);
            tgrt.offsetMin = Vector2.zero;
            tgrt.offsetMax = Vector2.zero;

            GridLayoutGroup tgl = trunkGridGo.GetComponent<GridLayoutGroup>();
            tgl.cellSize = new Vector2(64f, 64f);
            tgl.spacing = new Vector2(8f, 8f);
            tgl.childAlignment = TextAnchor.UpperLeft;
            trunkGridContainer = trunkGridGo.transform;

            // 5. Player Grid Container
            GameObject playerGridGo = new GameObject("Grid_Player", typeof(RectTransform), typeof(GridLayoutGroup));
            playerGridGo.transform.SetParent(panelGo.transform, false);
            RectTransform pgrt = playerGridGo.GetComponent<RectTransform>();
            pgrt.anchorMin = new Vector2(0.05f, 0.14f);
            pgrt.anchorMax = new Vector2(0.95f, 0.44f);
            pgrt.offsetMin = Vector2.zero;
            pgrt.offsetMax = Vector2.zero;

            GridLayoutGroup pgl = playerGridGo.GetComponent<GridLayoutGroup>();
            pgl.cellSize = new Vector2(64f, 64f);
            pgl.spacing = new Vector2(8f, 8f);
            pgl.childAlignment = TextAnchor.UpperLeft;
            playerGridContainer = playerGridGo.transform;

            // 6. Action Buttons Bar
            takeAllButton = CreateActionButton(panelGo.transform, "Btn_TakeAll", "TÜMÜNÜ AL", new Vector2(-120f, 24f), TakeAllItems);
            storeAllButton = CreateActionButton(panelGo.transform, "Btn_StoreAll", "ÇANTAYI DEPOLA", new Vector2(40f, 24f), StoreAllItems);
            closeButton = CreateActionButton(panelGo.transform, "Btn_Close", "KAPAT", new Vector2(200f, 24f), CloseTrunk, new Color(0.85f, 0.25f, 0.25f));
        }

        private Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, System.Action onClick, Color? col = null)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(140f, 38f);

            Image img = btnGo.GetComponent<Image>();
            img.color = col.HasValue ? col.Value : new Color(0.20f, 0.28f, 0.42f, 0.95f);

            Button btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text t = txtGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;

            return btn;
        }
    }
}
