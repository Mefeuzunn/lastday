using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    public class QuickSlotUI : MonoBehaviour
    {
        public static QuickSlotUI Instance { get; set; }

        [Header("Slot Components (0: Food, 1: Water, 2: Medkit, 3: Bottle)")]
        [SerializeField] private Button[] slotButtons = new Button[4];
        [SerializeField] private Image[] slotIcons = new Image[4];
        [SerializeField] private Text[] badgeTexts = new Text[4];

        [Header("Prefabs & References")]
        [SerializeField] private GameObject distractionBottlePrefab;

        private readonly string[] slotItemIds = new string[4]
        {
            "item_canned_food",
            "item_water_bottle",
            "item_medkit",
            "item_distraction_bottle"
        };

        private Text floatingFeedbackText;
        private Coroutine floatingCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => UseQuickSlot(index));
                }
            }

            CreateFloatingFeedbackUI();
        }

        private void Start()
        {
            UpdateAllBadges();
        }

        private void OnEnable()
        {
            GameEvents.OnInventoryUpdated += UpdateAllBadges;
            UpdateAllBadges();
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryUpdated -= UpdateAllBadges;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) UseQuickSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UseQuickSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UseQuickSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UseQuickSlot(3);
        }

        public void Configure(Button[] buttons, Image[] icons, Text[] badges, GameObject bottlePrefab)
        {
            slotButtons = buttons;
            slotIcons = icons;
            badgeTexts = badges;
            distractionBottlePrefab = bottlePrefab;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => UseQuickSlot(index));
                }
            }

            CreateFloatingFeedbackUI();
            UpdateAllBadges();
        }

        private void CreateFloatingFeedbackUI()
        {
            if (floatingFeedbackText != null) return;

            GameObject textObj = new GameObject("QuickSlot_FeedbackText");
            textObj.transform.SetParent(transform, false);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta = new Vector2(280f, 32f);

            floatingFeedbackText = textObj.AddComponent<Text>();
            floatingFeedbackText.alignment = TextAnchor.MiddleCenter;
            floatingFeedbackText.fontSize = 15;
            floatingFeedbackText.fontStyle = FontStyle.Bold;
            floatingFeedbackText.color = Color.white;
            floatingFeedbackText.raycastTarget = false;
            floatingFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            textObj.SetActive(false);
        }

        public int GetItemCount(string itemId)
        {
            if (InventorySystem.Instance == null) return 0;
            int count = 0;
            foreach (var slot in InventorySystem.Instance.Slots)
            {
                if (slot != null && slot.item != null && slot.item.itemId == itemId)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        public void UpdateAllBadges()
        {
            for (int i = 0; i < 4; i++)
            {
                int count = GetItemCount(slotItemIds[i]);
                if (badgeTexts != null && i < badgeTexts.Length && badgeTexts[i] != null)
                {
                    badgeTexts[i].text = $"x{count}";
                    badgeTexts[i].color = count > 0 ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.5f);
                }

                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                }
            }
        }

        public string[] GetSlotItemIds()
        {
            string[] copy = new string[4];
            for (int i = 0; i < 4; i++) copy[i] = slotItemIds[i];
            return copy;
        }

        public void RestoreSlotItemIds(string[] savedIds)
        {
            if (savedIds != null)
            {
                for (int i = 0; i < Mathf.Min(4, savedIds.Length); i++)
                {
                    if (!string.IsNullOrEmpty(savedIds[i]))
                    {
                        slotItemIds[i] = savedIds[i];
                    }
                }
            }
            UpdateAllBadges();
            Debug.Log("[QuickSlotUI] Restored QuickSlot item bindings and updated badges.");
        }

        public void SetSlotItem(int slotIndex, string newItemId)
        {
            if (slotIndex >= 0 && slotIndex < 4 && !string.IsNullOrEmpty(newItemId))
            {
                slotItemIds[slotIndex] = newItemId;
                UpdateAllBadges();
            }
        }

        public void AutoAssignStarterRation(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            bool assigned = false;
            if (itemId == "item_canned_food" || itemId == "item_hot_stew" || itemId == "item_cooked_meat" || itemId == "item_grilled_fish" || itemId == "item_raw_meat" || itemId == "item_raw_fish")
            {
                if (slotItemIds[0] != itemId || GetItemCount(slotItemIds[0]) == 0)
                {
                    slotItemIds[0] = itemId;
                    assigned = true;
                }
            }
            else if (itemId == "item_water_bottle" || itemId == "item_purified_water")
            {
                if (slotItemIds[1] != itemId || GetItemCount(slotItemIds[1]) == 0)
                {
                    slotItemIds[1] = itemId;
                    assigned = true;
                }
            }
            else if (itemId == "item_bandage" || itemId == "item_medkit" || itemId == "item_painkillers")
            {
                if (slotItemIds[2] != itemId && GetItemCount(slotItemIds[2]) == 0)
                {
                    slotItemIds[2] = itemId;
                    assigned = true;
                }
            }
            else if (itemId == "item_distraction_bottle" || itemId == "item_brick" || itemId == "item_sound_bomb" || itemId == "item_fuel_can" || itemId == "item_empty_jerrycan")
            {
                if (slotItemIds[3] != itemId && GetItemCount(slotItemIds[3]) == 0)
                {
                    slotItemIds[3] = itemId;
                    assigned = true;
                }
            }

            if (assigned)
            {
                UpdateAllBadges();
                GameEvents.TriggerObjectiveToastRequested("Erzak Hızlı Erişime Eklendi", 2.0f);
            }
        }

        public void UseQuickSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4) return;

            string itemId = slotItemIds[slotIndex];
            int count = GetItemCount(itemId);

            if (count <= 0)
            {
                GameEvents.TriggerObjectiveToastRequested("Eşya Yok", 2f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            // Tactical Traps & Armor Kits
            if (itemId == "trap_spiked_barricade")
            {
                if (TacticalTrapSystem.Instance != null && TacticalTrapSystem.Instance.DeploySpikedBarricade())
                {
                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    ShowFloatingFeedback("BARİKAT KURULDU", new Color(0.9f, 0.65f, 0.2f));
                    UpdateAllBadges();
                    return;
                }
            }
            else if (itemId == "trap_sound_bomb" || itemId == "item_sound_bomb")
            {
                if (TacticalTrapSystem.Instance != null && TacticalTrapSystem.Instance.ThrowSoundBomb())
                {
                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    ShowFloatingFeedback("SES BOMBASI ATILDI", new Color(1.0f, 0.5f, 0.1f));
                    UpdateAllBadges();
                    return;
                }
            }
            else if (itemId == "armor_repair_kit")
            {
                if (PlayerArmorController.Instance != null)
                {
                    PlayerArmorController.Instance.RepairArmor(50f);
                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    ShowFloatingFeedback("+50 ZIRH TAMİRİ", new Color(0.3f, 0.7f, 1.0f));
                    UpdateAllBadges();
                    return;
                }
            }

            // Tactical First-Aid & Trauma / Survival Alchemy Curative Routing
            bool isMedicalOrAlchemy = itemId == "item_bandage" || itemId == "item_splint" || itemId == "item_painkillers" ||
                                      itemId == "item_medkit" || itemId == "item_antiviral" || itemId == "item_adrenaline" ||
                                      itemId == "item_herbal_tea" || itemId == "item_purified_water" || itemId == "item_hot_stew" ||
                                      itemId == "item_raw_meat" || itemId == "item_cooked_meat" || itemId == "item_raw_fish" || itemId == "item_grilled_fish";
            if (isMedicalOrAlchemy && TraumaAndInjurySystem.Instance != null)
            {
                bool success = TraumaAndInjurySystem.Instance.TryUseMedicalItem(itemId);
                if (success)
                {
                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    UpdateAllBadges();
                }
                return;
            }

            // Reusable Fuel Container & Vehicle Refuel Routing
            if (itemId == "item_fuel_can" || itemId == "item_fuel")
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                Vector3 playerPos = (playerObj != null) ? playerObj.transform.position : transform.position;
                ProjectSurvival.Vehicles.DrivableVehicleController nearbyVehicle = null;
                float closestDistSqr = 3.8f * 3.8f;

                var allVehicles = Object.FindObjectsByType<ProjectSurvival.Vehicles.DrivableVehicleController>(FindObjectsSortMode.None);
                for (int v = 0; v < allVehicles.Length; v++)
                {
                    if (allVehicles[v] != null)
                    {
                        float distSqr = (allVehicles[v].transform.position - playerPos).sqrMagnitude;
                        if (distSqr <= closestDistSqr)
                        {
                            closestDistSqr = distSqr;
                            nearbyVehicle = allVehicles[v];
                        }
                    }
                }

                if (nearbyVehicle != null)
                {
                    if (nearbyVehicle.Fuel >= nearbyVehicle.MaxFuel * 0.99f)
                    {
                        GameEvents.TriggerObjectiveToastRequested("ARAÇ DEPOSU TAM DOLU!", 2.2f);
                        ProceduralAudioManager.Instance?.PlaySoftError();
                        return;
                    }

                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    nearbyVehicle.Refuel(35f);
                    InventorySystem.Instance?.AddItemById("item_empty_jerrycan", 1);
                    ProceduralAudioManager.Instance?.PlayRefuelLiquid();
                    MobileHapticManager.Instance?.TriggerMedium();
                    ShowFloatingFeedback("+%35 YAKIT (Boş Bidon)", new Color(1.0f, 0.75f, 0.2f));
                    GameEvents.TriggerObjectiveToastRequested("ARAÇ YAKITI DOLDURULDU (+%35) | Boş Bidon Envantere Eklendi", 2.5f);
                    UpdateAllBadges();
                    return;
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("YAKINDA ARAÇ YOK! (Yakıt doldurmak için araca 3.8m yaklaşın)", 2.5f);
                    ProceduralAudioManager.Instance?.PlaySoftError();
                    return;
                }
            }
            if (itemId == "item_empty_jerrycan")
            {
                GameEvents.TriggerObjectiveToastRequested("BOŞ YAKIT BİDONU! Benzin istasyonundan veya araç enkazından doldurun.", 2.5f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            SurvivalStats stats = SurvivalStats.Instance;

            switch (slotIndex)
            {
                case 0: // Konserve Yiyecek
                    if (stats != null && stats.Hunger >= 100f)
                    {
                        GameEvents.TriggerObjectiveToastRequested("Değer Zaten Dolu", 2f);
                        ProceduralAudioManager.Instance?.PlaySoftError();
                        return;
                    }

                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    stats?.Feed(35f);
                    stats?.Heal(5f);
                    ProceduralAudioManager.Instance?.PlayEat();
                    ShowFloatingFeedback("+35 TOKLUK", new Color(0.35f, 0.95f, 0.45f));
                    break;

                case 1: // Su Şişesi
                    if (stats != null && stats.Thirst >= 100f)
                    {
                        GameEvents.TriggerObjectiveToastRequested("Değer Zaten Dolu", 2f);
                        ProceduralAudioManager.Instance?.PlaySoftError();
                        return;
                    }

                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    stats?.Hydrate(40f);
                    stats?.RestoreStamina(15f);
                    ProceduralAudioManager.Instance?.PlayDrink();
                    ShowFloatingFeedback("+40 SUSUZLUK", new Color(0.30f, 0.79f, 1.0f));
                    break;

                case 2: // İlk Yardım Kiti
                    if (stats != null && stats.Health >= 100f && stats.Infection <= 0f)
                    {
                        GameEvents.TriggerObjectiveToastRequested("Değer Zaten Dolu", 2f);
                        ProceduralAudioManager.Instance?.PlaySoftError();
                        return;
                    }

                    InventorySystem.Instance?.RemoveItem(itemId, 1);
                    stats?.Heal(50f);
                    stats?.ModifyInfection(-15f);
                    ProceduralAudioManager.Instance?.PlayFirstAid();
                    ShowFloatingFeedback("+50 CAN", new Color(0.95f, 0.22f, 0.22f));
                    break;

                case 3: // Fırlatılabilir Dikkat Dağıtıcı (Şişe, Tuğla, Ses Bombası)
                    if (ParabolicThrowController.Instance != null)
                    {
                        ThrowableItemType tType = ThrowableItemType.DistractionBottle;
                        if (itemId == "item_brick") tType = ThrowableItemType.Brick;
                        else if (itemId == "trap_sound_bomb") tType = ThrowableItemType.SoundBomb;

                        ParabolicThrowController.Instance.StartAiming(tType);
                        ShowFloatingFeedback("ATIŞ YÖRÜNGESİ AKTİF", new Color(1.0f, 0.85f, 0.25f));
                    }
                    else
                    {
                        InventorySystem.Instance?.RemoveItem(itemId, 1);
                        ThrowDistractionBottle();
                        ShowFloatingFeedback("ŞİŞE FIRLATILDI", new Color(1.0f, 0.85f, 0.25f));
                    }
                    break;
            }

            UpdateAllBadges();
        }

        private void ThrowDistractionBottle()
        {
            GameObject player = GameObject.FindWithTag("Player");
            Transform throwOrigin = player != null ? player.transform : transform;
            Camera cam = Camera.main;
            Vector3 camFwd = cam != null ? cam.transform.forward : throwOrigin.forward;

            Vector3 spawnPos = throwOrigin.position + Vector3.up * 1.4f + camFwd * 0.75f;
            Vector3 throwVector = (camFwd * 14f) + (Vector3.up * 3.5f);

            PlayerAnimationBridge anim = player != null ? player.GetComponent<PlayerAnimationBridge>() : null;
            if (anim != null) anim.TriggerThrow();

            GameObject thrownObj = null;
            if (distractionBottlePrefab != null)
            {
                thrownObj = Instantiate(distractionBottlePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Procedural glass bottle fallback
                thrownObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                thrownObj.name = "DistractionBottle_Thrown";
                thrownObj.transform.position = spawnPos;
                thrownObj.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
                Rigidbody rb = thrownObj.AddComponent<Rigidbody>();
                rb.mass = 0.45f;
            }

            ThrownDistraction distraction = thrownObj.GetComponent<ThrownDistraction>();
            if (distraction == null) distraction = thrownObj.AddComponent<ThrownDistraction>();
            distraction.Initialize(18f, throwVector);
        }

        private void ShowFloatingFeedback(string message, Color col)
        {
            if (floatingFeedbackText == null) CreateFloatingFeedbackUI();
            if (floatingCoroutine != null) StopCoroutine(floatingCoroutine);
            floatingCoroutine = StartCoroutine(FloatingFeedbackRoutine(message, col));
        }

        private IEnumerator FloatingFeedbackRoutine(string message, Color col)
        {
            if (floatingFeedbackText == null) yield break;

            floatingFeedbackText.gameObject.SetActive(true);
            floatingFeedbackText.text = message;

            RectTransform rt = floatingFeedbackText.rectTransform;
            Vector2 startPos = new Vector2(0f, 16f);
            Vector2 endPos = new Vector2(0f, 54f);

            float elapsed = 0f;
            float duration = 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                Color c = col;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                floatingFeedbackText.color = c;
                yield return null;
            }

            floatingFeedbackText.gameObject.SetActive(false);
        }
    }
}
