using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Multi-Tab Gunsmith & Survival Workbench Station.
    /// Provides 3 organized functional tabs:
    /// Tab 1: SİLAH ATÖLYESİ (Gunsmith - Hasar, Ses Gizliliği, İsabet, Kapasite + Attachments)
    /// Tab 2: TUZAK & ZIRH (Tactical Gear - Spiked Barricade, Sound Bomb, Armor Vest, Armor Repair)
    /// Tab 3: SAĞLIK & CEPHANE (Supplies - Medkit, Bandage, 9mm Ammo x12, Shotgun Shells x6)
    /// Live header resource counters: Hurda, Kumaş, Alkol, Pil.
    /// </summary>
    public class WorkbenchCraftingUI : MonoBehaviour
    {
        private static WorkbenchCraftingUI _instance;
        public static WorkbenchCraftingUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<WorkbenchCraftingUI>();
                    if (_instance == null)
                    {
                        Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                        if (hudCanvas != null)
                        {
                            GameObject uiObj = new GameObject("WorkbenchCrafting_UI");
                            uiObj.transform.SetParent(hudCanvas.transform, false);
                            _instance = uiObj.AddComponent<WorkbenchCraftingUI>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public enum WorkbenchTab
        {
            Gunsmith = 0,
            TacticalGear = 1,
            Supplies = 2
        }

        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Resource Counters")]
        [SerializeField] private Text scrapCountText;
        [SerializeField] private Text clothCountText;
        [SerializeField] private Text alcoholCountText;
        [SerializeField] private Text batteryCountText;

        [Header("Tab Buttons")]
        [SerializeField] private Button tabGunsmithBtn;
        [SerializeField] private Button tabGearBtn;
        [SerializeField] private Button tabSuppliesBtn;

        [Header("Tab Content Views")]
        [SerializeField] private GameObject gunsmithView;
        [SerializeField] private GameObject gearView;
        [SerializeField] private GameObject suppliesView;

        // Gunsmith Sub-state
        private WeaponType selectedGunsmithWeapon = WeaponType.Pistol;
        private WorkbenchTab currentTab = WorkbenchTab.Gunsmith;

        // UI references dynamically built or bound
        private Text weaponTitleText;
        private Image statDamageFill;
        private Image statStealthFill;
        private Image statAccuracyFill;
        private Image statCapacityFill;
        private Text statDamageText;
        private Text statStealthText;
        private Text statAccuracyText;
        private Text statCapacityText;
        private Transform attachmentRowsParent;
        private Transform gearRowsParent;
        private Transform suppliesRowsParent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                {
                    GameObject uiObj = new GameObject("WorkbenchCrafting_UI");
                    uiObj.transform.SetParent(hudCanvas.transform, false);
                    Instance = uiObj.AddComponent<WorkbenchCraftingUI>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUIHierarchyIfEmpty();
        }

        private void OnEnable()
        {
            GameEvents.OnInventoryUpdated += RefreshAll;
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryUpdated -= RefreshAll;
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseWorkbench();
                }
            }
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void OpenWorkbench()
        {
            if (panelRoot != null)
            {
                ModalWindowManager.Instance?.NotifyModalOpened(this);
                panelRoot.SetActive(true);
                SwitchTab(currentTab);
                RefreshAll();
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }
        }

        public void CloseWorkbench()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }
        }

        public void ToggleWorkbench()
        {
            if (panelRoot != null && panelRoot.activeSelf) CloseWorkbench();
            else OpenWorkbench();
        }

        public void SwitchTab(WorkbenchTab tab)
        {
            currentTab = tab;
            if (gunsmithView != null) gunsmithView.SetActive(tab == WorkbenchTab.Gunsmith);
            if (gearView != null) gearView.SetActive(tab == WorkbenchTab.TacticalGear);
            if (suppliesView != null) suppliesView.SetActive(tab == WorkbenchTab.Supplies);

            HighlightActiveTabButton();
            RefreshAll();
        }

        private void HighlightActiveTabButton()
        {
            Color activeColor = new Color(0.95f, 0.65f, 0.15f);
            Color normalColor = new Color(0.20f, 0.25f, 0.32f);

            if (tabGunsmithBtn != null) tabGunsmithBtn.GetComponent<Image>().color = currentTab == WorkbenchTab.Gunsmith ? activeColor : normalColor;
            if (tabGearBtn != null) tabGearBtn.GetComponent<Image>().color = currentTab == WorkbenchTab.TacticalGear ? activeColor : normalColor;
            if (tabSuppliesBtn != null) tabSuppliesBtn.GetComponent<Image>().color = currentTab == WorkbenchTab.Supplies ? activeColor : normalColor;
        }

        public void RefreshAll()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;

            UpdateResourceCounters();

            switch (currentTab)
            {
                case WorkbenchTab.Gunsmith:
                    RefreshGunsmithTab();
                    break;
                case WorkbenchTab.TacticalGear:
                    RefreshGearTab();
                    break;
                case WorkbenchTab.Supplies:
                    RefreshSuppliesTab();
                    break;
            }
        }

        private void UpdateResourceCounters()
        {
            int scrap = GetResourceCount("item_scrap");
            int cloth = GetResourceCount("item_cloth");
            int alcohol = GetResourceCount("item_alcohol");
            int battery = GetResourceCount("item_battery");

            if (scrapCountText != null) scrapCountText.text = $"HURDA: {scrap}";
            if (clothCountText != null) clothCountText.text = $"KUMAŞ: {cloth}";
            if (alcoholCountText != null) alcoholCountText.text = $"ALKOL: {alcohol}";
            if (batteryCountText != null) batteryCountText.text = $"PİL: {battery}";
        }

        private int GetResourceCount(string id)
        {
            if (InventorySystem.Instance == null) return 0;
            int count = 0;
            foreach (var slot in InventorySystem.Instance.Slots)
            {
                if (slot.item != null && slot.item.itemId == id) count += slot.quantity;
            }
            return count;
        }

        // =========================================================================
        // TAB 1: GUNSMITH
        // =========================================================================
        private void RefreshGunsmithTab()
        {
            if (weaponTitleText == null) return;

            WeaponAttachmentManager wam = WeaponAttachmentManager.Instance;
            bool hasSup = wam != null && wam.HasSuppressor(selectedGunsmithWeapon);
            bool hasLaser = wam != null && wam.HasLaserSight(selectedGunsmithWeapon);
            bool hasExtMag = wam != null && wam.HasExtendedMag(selectedGunsmithWeapon);
            bool hasReinf = wam != null && wam.HasReinforcedAxe();

            float damage = 0f;
            float stealth = 0.2f;
            float accuracy = 0.5f;
            int capacity = 0;
            string title = "";

            if (selectedGunsmithWeapon == WeaponType.Pistol)
            {
                title = "9MM TAKTİK TABANCA";
                damage = 35f;
                stealth = hasSup ? 0.85f : 0.25f;
                accuracy = hasLaser ? 0.88f : 0.65f;
                capacity = hasExtMag ? 18 : 12;
            }
            else if (selectedGunsmithWeapon == WeaponType.Shotgun)
            {
                title = "POMPALI AV TÜFEĞİ";
                damage = 90f;
                stealth = hasSup ? 0.75f : 0.15f;
                accuracy = hasLaser ? 0.70f : 0.45f;
                capacity = hasExtMag ? 8 : 6;
            }
            else if (selectedGunsmithWeapon == WeaponType.Axe)
            {
                title = "YANGIN BALTASI";
                damage = hasReinf ? 74f : 55f;
                stealth = 0.95f; // Silent melee
                accuracy = 0.90f;
                capacity = 0;
            }

            weaponTitleText.text = title;
            if (statDamageFill != null) statDamageFill.fillAmount = damage / 100f;
            if (statDamageText != null) statDamageText.text = $"HASAR: {Mathf.CeilToInt(damage)}";

            if (statStealthFill != null) statStealthFill.fillAmount = stealth;
            if (statStealthText != null) statStealthText.text = $"SES GİZLİLİĞİ: {Mathf.CeilToInt(stealth * 100f)}%";

            if (statAccuracyFill != null) statAccuracyFill.fillAmount = accuracy;
            if (statAccuracyText != null) statAccuracyText.text = $"İSABET: {Mathf.CeilToInt(accuracy * 100f)}%";

            if (statCapacityFill != null) statCapacityFill.fillAmount = capacity > 0 ? (capacity / 20f) : 0f;
            if (statCapacityText != null) statCapacityText.text = capacity > 0 ? $"KAPASİTE: {capacity} MERMİ" : "KAPASİTE: ---";

            PopulateAttachmentRows();
        }

        private void PopulateAttachmentRows()
        {
            if (attachmentRowsParent == null) return;

            // Clear old rows
            for (int i = attachmentRowsParent.childCount - 1; i >= 0; i--)
            {
                Destroy(attachmentRowsParent.GetChild(i).gameObject);
            }

            WeaponAttachmentManager wam = WeaponAttachmentManager.Instance;

            if (selectedGunsmithWeapon == WeaponType.Pistol || selectedGunsmithWeapon == WeaponType.Shotgun)
            {
                CreateAttachmentRow(
                    WeaponAttachmentManager.MOD_SUPPRESSOR,
                    "El Yapımı Susturucu",
                    "Ateşleme ses yarıçapını %75 düşürür. Zombileri alarma geçirmez.",
                    "3 Hurda + 1 Kumaş",
                    () => CanAfford(3, 1, 0, 0),
                    () => ConsumeResources(3, 1, 0, 0)
                );

                CreateAttachmentRow(
                    WeaponAttachmentManager.MOD_LASER_SIGHT,
                    "Taktik Lazer Nişangah",
                    "Mermi dağılımını %35 daraltır, 45m kırmızı taktik lazer ışını verir.",
                    "3 Hurda + 1 Pil",
                    () => CanAfford(3, 0, 0, 1),
                    () => ConsumeResources(3, 0, 0, 1)
                );

                CreateAttachmentRow(
                    WeaponAttachmentManager.MOD_EXTENDED_MAG,
                    "Genişletilmiş Şarjör",
                    selectedGunsmithWeapon == WeaponType.Pistol ? "Şarjör kapasitesini 18 mermiye çıkarır, doldurmayı %15 hızlandırır." : "Şarjör kapasitesini 8 fişeğe çıkarır, doldurmayı %15 hızlandırır.",
                    "4 Hurda",
                    () => CanAfford(4, 0, 0, 0),
                    () => ConsumeResources(4, 0, 0, 0)
                );
            }
            else if (selectedGunsmithWeapon == WeaponType.Axe)
            {
                CreateAttachmentRow(
                    WeaponAttachmentManager.MOD_REINFORCED_AXE,
                    "Güçlendirilmiş Balta Başı",
                    "Balta hasarını %35 artırır, zombileri sersemletip geri fırlatır, -%20 stamina.",
                    "3 Hurda + 1 Kumaş",
                    () => CanAfford(3, 1, 0, 0),
                    () => ConsumeResources(3, 1, 0, 0)
                );
            }
        }

        private void CreateAttachmentRow(string modId, string name, string desc, string costStr, Func<bool> canAfford, Action consume)
        {
            WeaponAttachmentManager wam = WeaponAttachmentManager.Instance;
            bool unlocked = wam != null && wam.IsUnlocked(modId);
            bool equipped = wam != null && wam.IsEquipped(modId);

            GameObject row = CreateRowTemplate(attachmentRowsParent, 68f);

            // Title & Desc
            CreateLabel(row.transform, new Vector2(16f, 14f), new Vector2(380f, 24f), name, 15, FontStyle.Bold, new Color(0.95f, 0.95f, 0.95f));
            CreateLabel(row.transform, new Vector2(16f, -14f), new Vector2(400f, 22f), desc, 12, FontStyle.Normal, new Color(0.70f, 0.75f, 0.80f));

            // Button
            Button btn = CreateActionButton(row.transform, new Vector2(480f, 0f), new Vector2(150f, 40f));
            Text btnTxt = btn.GetComponentInChildren<Text>();

            if (!unlocked)
            {
                btnTxt.text = $"ÜRET\n({costStr})";
                btnTxt.fontSize = 11;
                btn.GetComponent<Image>().color = canAfford() ? new Color(0.85f, 0.55f, 0.12f) : new Color(0.35f, 0.35f, 0.35f);
                btn.onClick.AddListener(() =>
                {
                    if (canAfford())
                    {
                        consume();
                        wam?.UnlockAttachment(modId);
                        ProceduralAudioManager.Instance?.PlayCraftSuccess();
                        GameEvents.TriggerObjectiveToastRequested($"[YENİ EKLENTİ] {name} üretildi ve kuşanıldı!", 3.0f);
                        RefreshAll();
                    }
                    else
                    {
                        ProceduralAudioManager.Instance?.PlaySoftError();
                        GameEvents.TriggerObjectiveToastRequested("Yetersiz hammadde!", 2.0f);
                    }
                });
            }
            else if (equipped)
            {
                btnTxt.text = "KUŞANILDI\n(Çıkarmak için tıkla)";
                btnTxt.fontSize = 11;
                btn.GetComponent<Image>().color = new Color(0.18f, 0.65f, 0.35f);
                btn.onClick.AddListener(() =>
                {
                    wam?.UnequipAttachment(modId);
                    RefreshAll();
                });
            }
            else
            {
                btnTxt.text = "KUŞAN";
                btnTxt.fontSize = 13;
                btn.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f);
                btn.onClick.AddListener(() =>
                {
                    wam?.EquipAttachment(modId);
                    RefreshAll();
                });
            }
        }

        // =========================================================================
        // TAB 2: TACTICAL GEAR
        // =========================================================================
        private void RefreshGearTab()
        {
            if (gearRowsParent == null) return;
            for (int i = gearRowsParent.childCount - 1; i >= 0; i--) Destroy(gearRowsParent.GetChild(i).gameObject);

            // 1. Spiked Barricade Trap
            CreateCraftRow(gearRowsParent, "Çivili Barikat Tuzağı", "Zombileri yavaşlatan (%65) ve saniyede 22 hasar veren 160 HP'lik çivili barikat.", "3 Hurda + 1 Kumaş",
                () => CanAfford(3, 1, 0, 0),
                () =>
                {
                    ConsumeResources(3, 1, 0, 0);
                    InventorySystem.Instance?.AddItemById("trap_spiked_barricade", 1);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Çivili Barikat Tuzağı üretildi!", 3.0f);
                });

            // 2. Sound Bomb
            CreateCraftRow(gearRowsParent, "Ses Bombası", "5 sn boyunca zombileri çeken 48m siren yayar, ardından 6.5m alanda 140 hasarla patlar.", "2 Hurda + 1 Pil + 1 Alkol",
                () => CanAfford(2, 0, 1, 1),
                () =>
                {
                    ConsumeResources(2, 0, 1, 1);
                    InventorySystem.Instance?.AddItemById("trap_sound_bomb", 1);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Ses Bombası üretildi!", 3.0f);
                });

            // 3. Improvised Tactical Armor Vest
            CreateCraftRow(gearRowsParent, "El Yapımı Zırh Yeleği", "Hasarın %35'ini emer, enfeksiyon sıçramasını engeller (100 Zırh Canı).", "4 Hurda + 2 Kumaş",
                () => CanAfford(4, 2, 0, 0),
                () =>
                {
                    ConsumeResources(4, 2, 0, 0);
                    PlayerArmorController.Instance?.EquipArmor(100f);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                });

            // 4. Armor Repair Kit
            CreateCraftRow(gearRowsParent, "Zırh Tamir Kiti", "Hasar görmüş zırhı onarır (+50 Zırh Dayanıklılığı).", "1 Hurda + 1 Kumaş",
                () => CanAfford(1, 1, 0, 0),
                () =>
                {
                    ConsumeResources(1, 1, 0, 0);
                    if (PlayerArmorController.Instance != null && PlayerArmorController.Instance.HasArmorEquipped)
                    {
                        PlayerArmorController.Instance.RepairArmor(50f);
                    }
                    else
                    {
                        InventorySystem.Instance?.AddItemById("armor_repair_kit", 1);
                        ProceduralAudioManager.Instance?.PlayCraftSuccess();
                        GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Zırh Tamir Kiti üretildi!", 3.0f);
                    }
                });

            // 5. Tactical Crossbow Package (5 Scrap, 3 Cloth, 1 Battery -> Crossbow + 6 Bolts)
            CreateCraftRow(gearRowsParent, "Taktik Tatar Yayı Paketi", "Sessiz, yüksek kinetik hasarlı tatar yayı ve 6 adet geri toplanabilir çelik uçlu ok.", "5 Hurda + 3 Kumaş + 1 Pil",
                () => CanAfford(5, 3, 0, 1),
                () =>
                {
                    TryCraftCrossbow();
                });
        }

        // =========================================================================
        // TAB 3: SUPPLIES
        // =========================================================================
        private void RefreshSuppliesTab()
        {
            if (suppliesRowsParent == null) return;
            for (int i = suppliesRowsParent.childCount - 1; i >= 0; i--) Destroy(suppliesRowsParent.GetChild(i).gameObject);

            // 1. Medkit
            CreateCraftRow(suppliesRowsParent, "İlk Yardım Kiti", "+50 Can yeniler, enfeksiyonu düşürür.", "2 Kumaş + 1 Alkol",
                () => CanAfford(0, 2, 1, 0),
                () =>
                {
                    ConsumeResources(0, 2, 1, 0);
                    InventorySystem.Instance?.AddItemById("item_medkit", 1);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] İlk Yardım Kiti!", 2.5f);
                });

            // 2. Bandage
            CreateCraftRow(suppliesRowsParent, "Sargı Bezi", "+25 Can yeniler ve kanamayı durdurur.", "1 Kumaş",
                () => CanAfford(0, 1, 0, 0),
                () =>
                {
                    ConsumeResources(0, 1, 0, 0);
                    InventorySystem.Instance?.AddItemById("item_bandage", 1);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Sargı Bezi!", 2.5f);
                });

            // 3. 9mm Ammo x12
            CreateCraftRow(suppliesRowsParent, "9mm Tabanca Mermisi x12", "Standart 9x19mm tabanca mermisi.", "2 Hurda",
                () => CanAfford(2, 0, 0, 0),
                () =>
                {
                    ConsumeResources(2, 0, 0, 0);
                    InventorySystem.Instance?.AddItemById("ammo_9mm", 12);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] 12x 9mm Mermi!", 2.5f);
                });

            // 4. Shotgun Shells x6
            CreateCraftRow(suppliesRowsParent, "Pompalı Fişeği x6", "12 kalibre saçma fişeği.", "2 Hurda + 1 Alkol",
                () => CanAfford(2, 0, 1, 0),
                () =>
                {
                    ConsumeResources(2, 0, 1, 0);
                    InventorySystem.Instance?.AddItemById("ammo_shells", 6);
                    ProceduralAudioManager.Instance?.PlayCraftSuccess();
                    GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] 6x Pompalı Fişeği!", 2.5f);
                });

            // 5. Crossbow Bolts x5 (2 Scrap, 1 Cloth -> 5x ammo_crossbow_bolt)
            CreateCraftRow(suppliesRowsParent, "5x Tatar Yayı Oku", "Geri toplanabilir çelik uçlu sessiz tatar yayı mühimmatı.", "2 Hurda + 1 Kumaş",
                () => CanAfford(2, 1, 0, 0),
                () =>
                {
                    TryCraftCrossbowBolts();
                });

            // 6. Boş Yakıt Bidonu (3 Scrap -> 1x item_empty_jerrycan)
            CreateCraftRow(suppliesRowsParent, "Boş Yakıt Bidonu", "Yakıt çekmek ve depolamak için 15L boş metal bidon.", "3 Hurda",
                () => CanAfford(3, 0, 0, 0),
                () =>
                {
                    TryCraftEmptyJerrycan();
                });
        }

        public bool TryCraftCrossbow()
        {
            if (!CanAfford(5, 3, 0, 1)) return false;
            ConsumeResources(5, 3, 0, 1);
            InventorySystem.Instance?.AddItemById("weapon_crossbow", 1);
            InventorySystem.Instance?.AddItemById("ammo_crossbow_bolt", 6);
            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Taktik Tatar Yayı ve 6 Ok!", 3.0f);
            return true;
        }

        public bool TryCraftCrossbowBolts()
        {
            if (!CanAfford(2, 1, 0, 0)) return false;
            ConsumeResources(2, 1, 0, 0);
            InventorySystem.Instance?.AddItemById("ammo_crossbow_bolt", 5);
            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] 5x Tatar Yayı Oku!", 2.5f);
            return true;
        }

        public bool TryCraftEmptyJerrycan()
        {
            if (!CanAfford(3, 0, 0, 0)) return false;
            ConsumeResources(3, 0, 0, 0);
            InventorySystem.Instance?.AddItemById("item_empty_jerrycan", 1);
            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested("[ÜRETİLDİ] Boş Yakıt Bidonu!", 2.5f);
            return true;
        }

        private void CreateCraftRow(Transform parent, string name, string desc, string costStr, Func<bool> canAfford, Action onCraft)
        {
            GameObject row = CreateRowTemplate(parent, 68f);

            CreateLabel(row.transform, new Vector2(16f, 14f), new Vector2(380f, 24f), name, 15, FontStyle.Bold, new Color(0.95f, 0.95f, 0.95f));
            CreateLabel(row.transform, new Vector2(16f, -14f), new Vector2(400f, 22f), desc, 12, FontStyle.Normal, new Color(0.70f, 0.75f, 0.80f));

            Button btn = CreateActionButton(row.transform, new Vector2(480f, 0f), new Vector2(150f, 40f));
            Text btnTxt = btn.GetComponentInChildren<Text>();
            btnTxt.text = $"ÜRET\n({costStr})";
            btnTxt.fontSize = 11;

            bool afford = canAfford();
            btn.GetComponent<Image>().color = afford ? new Color(0.85f, 0.55f, 0.12f) : new Color(0.35f, 0.35f, 0.35f);

            btn.onClick.AddListener(() =>
            {
                if (canAfford())
                {
                    onCraft();
                    RefreshAll();
                }
                else
                {
                    ProceduralAudioManager.Instance?.PlaySoftError();
                    GameEvents.TriggerObjectiveToastRequested("Yetersiz hammadde!", 2.0f);
                }
            });
        }

        private int ApplyDiscount(int cost)
        {
            if (cost <= 1) return cost;
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null &&
                ProjectSurvival.Progression.SurvivorProgressionManager.Instance.HasPerk("survival_4_master_fabricator"))
            {
                return Mathf.Max(1, Mathf.FloorToInt(cost * 0.75f));
            }
            return cost;
        }

        private bool CanAfford(int scrap, int cloth, int alcohol, int battery)
        {
            scrap = ApplyDiscount(scrap);
            cloth = ApplyDiscount(cloth);
            alcohol = ApplyDiscount(alcohol);
            battery = ApplyDiscount(battery);
            return GetResourceCount("item_scrap") >= scrap &&
                   GetResourceCount("item_cloth") >= cloth &&
                   GetResourceCount("item_alcohol") >= alcohol &&
                   GetResourceCount("item_battery") >= battery;
        }

        private void ConsumeResources(int scrap, int cloth, int alcohol, int battery)
        {
            if (InventorySystem.Instance == null) return;
            scrap = ApplyDiscount(scrap);
            cloth = ApplyDiscount(cloth);
            alcohol = ApplyDiscount(alcohol);
            battery = ApplyDiscount(battery);
            if (scrap > 0) InventorySystem.Instance.RemoveItem("item_scrap", scrap);
            if (cloth > 0) InventorySystem.Instance.RemoveItem("item_cloth", cloth);
            if (alcohol > 0) InventorySystem.Instance.RemoveItem("item_alcohol", alcohol);
            if (battery > 0) InventorySystem.Instance.RemoveItem("item_battery", battery);
        }

        // =========================================================================
        // PROCEDURAL SAFEAREA UI BUILDER
        // =========================================================================
        private void BuildUIHierarchyIfEmpty()
        {
            if (panelRoot != null) return;

            // Fullscreen backdrop / block
            panelRoot = new GameObject("Workbench_PanelRoot");
            panelRoot.transform.SetParent(transform, false);

            RectTransform rtRoot = panelRoot.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            Image darkBg = panelRoot.AddComponent<Image>();
            darkBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            darkBg.raycastTarget = true; // Modal backdrop

            // Dialog Window Frame (1080 x 640)
            GameObject winObj = new GameObject("Window_Frame");
            winObj.transform.SetParent(panelRoot.transform, false);
            RectTransform rtWin = winObj.AddComponent<RectTransform>();
            rtWin.anchorMin = new Vector2(0.5f, 0.5f);
            rtWin.anchorMax = new Vector2(0.5f, 0.5f);
            rtWin.pivot = new Vector2(0.5f, 0.5f);
            rtWin.sizeDelta = new Vector2(1040f, 620f);

            Image winBg = winObj.AddComponent<Image>();
            winBg.color = new Color(0.10f, 0.12f, 0.16f, 0.98f);
            winBg.raycastTarget = false;

            // Window Header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(winObj.transform, false);
            RectTransform rtHead = headerObj.AddComponent<RectTransform>();
            rtHead.anchorMin = new Vector2(0f, 1f);
            rtHead.anchorMax = new Vector2(1f, 1f);
            rtHead.pivot = new Vector2(0.5f, 1f);
            rtHead.sizeDelta = new Vector2(0f, 54f);
            rtHead.anchoredPosition = Vector2.zero;

            Image headBg = headerObj.AddComponent<Image>();
            headBg.color = new Color(0.06f, 0.08f, 0.11f, 1f);
            headBg.raycastTarget = false;

            // Title
            CreateLabel(headerObj.transform, new Vector2(24f, 0f), new Vector2(360f, 40f), "ÇALIŞMA TEZGAHI & SİLAH ATÖLYESİ", 17, FontStyle.Bold, new Color(0.95f, 0.72f, 0.15f), TextAnchor.MiddleLeft);

            // Resource Counters
            GameObject resObj = new GameObject("ResourcePills");
            resObj.transform.SetParent(headerObj.transform, false);
            RectTransform rtRes = resObj.AddComponent<RectTransform>();
            rtRes.anchorMin = new Vector2(0.40f, 0f);
            rtRes.anchorMax = new Vector2(0.85f, 1f);
            rtRes.sizeDelta = Vector2.zero;

            scrapCountText = CreateLabel(resObj.transform, new Vector2(0f, 0f), new Vector2(95f, 30f), "HURDA: 0", 12, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f));
            clothCountText = CreateLabel(resObj.transform, new Vector2(105f, 0f), new Vector2(95f, 30f), "KUMAŞ: 0", 12, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f));
            alcoholCountText = CreateLabel(resObj.transform, new Vector2(210f, 0f), new Vector2(95f, 30f), "ALKOL: 0", 12, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f));
            batteryCountText = CreateLabel(resObj.transform, new Vector2(315f, 0f), new Vector2(95f, 30f), "PİL: 0", 12, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f));

            // Close Button
            closeButton = CreateActionButton(headerObj.transform, new Vector2(480f, 0f), new Vector2(44f, 36f));
            closeButton.GetComponentInChildren<Text>().text = "X";
            closeButton.GetComponentInChildren<Text>().fontSize = 18;
            closeButton.GetComponent<Image>().color = new Color(0.75f, 0.15f, 0.15f);
            closeButton.onClick.AddListener(CloseWorkbench);

            // Tab Navigation Bar (Left side: 3 tabs)
            GameObject tabsObj = new GameObject("TabBar");
            tabsObj.transform.SetParent(winObj.transform, false);
            RectTransform rtTabs = tabsObj.AddComponent<RectTransform>();
            rtTabs.anchorMin = new Vector2(0f, 0f);
            rtTabs.anchorMax = new Vector2(0f, 1f);
            rtTabs.pivot = new Vector2(0f, 0.5f);
            rtTabs.sizeDelta = new Vector2(240f, -60f);
            rtTabs.anchoredPosition = new Vector2(15f, -30f);

            tabGunsmithBtn = CreateTabButton(tabsObj.transform, 0, "SİLAH ATÖLYESİ", () => SwitchTab(WorkbenchTab.Gunsmith));
            tabGearBtn = CreateTabButton(tabsObj.transform, 1, "TUZAK & ZIRH", () => SwitchTab(WorkbenchTab.TacticalGear));
            tabSuppliesBtn = CreateTabButton(tabsObj.transform, 2, "SAĞLIK & CEPHANE", () => SwitchTab(WorkbenchTab.Supplies));

            // Content Area (Right side)
            GameObject contentObj = new GameObject("ContentArea");
            contentObj.transform.SetParent(winObj.transform, false);
            RectTransform rtContent = contentObj.AddComponent<RectTransform>();
            rtContent.anchorMin = new Vector2(0.26f, 0.03f);
            rtContent.anchorMax = new Vector2(0.98f, 0.88f);
            rtContent.sizeDelta = Vector2.zero;

            // 1. Gunsmith View
            gunsmithView = new GameObject("GunsmithView");
            gunsmithView.transform.SetParent(contentObj.transform, false);
            RectTransform rtGun = gunsmithView.AddComponent<RectTransform>();
            rtGun.anchorMin = Vector2.zero;
            rtGun.anchorMax = Vector2.one;
            rtGun.sizeDelta = Vector2.zero;

            BuildGunsmithHierarchy(gunsmithView.transform);

            // 2. Tactical Gear View
            gearView = new GameObject("GearView");
            gearView.transform.SetParent(contentObj.transform, false);
            RectTransform rtGear = gearView.AddComponent<RectTransform>();
            rtGear.anchorMin = Vector2.zero;
            rtGear.anchorMax = Vector2.one;
            rtGear.sizeDelta = Vector2.zero;

            gearRowsParent = BuildScrollViewHierarchy(gearView.transform);

            // 3. Supplies View
            suppliesView = new GameObject("SuppliesView");
            suppliesView.transform.SetParent(contentObj.transform, false);
            RectTransform rtSupp = suppliesView.AddComponent<RectTransform>();
            rtSupp.anchorMin = Vector2.zero;
            rtSupp.anchorMax = Vector2.one;
            rtSupp.sizeDelta = Vector2.zero;

            suppliesRowsParent = BuildScrollViewHierarchy(suppliesView.transform);

            panelRoot.SetActive(false);
        }

        private void BuildGunsmithHierarchy(Transform parent)
        {
            // Weapon Selectors (Pistol, Shotgun, Axe)
            GameObject selRow = new GameObject("WeaponSelectors");
            selRow.transform.SetParent(parent, false);
            RectTransform rtSel = selRow.AddComponent<RectTransform>();
            rtSel.anchorMin = new Vector2(0f, 1f);
            rtSel.anchorMax = new Vector2(1f, 1f);
            rtSel.pivot = new Vector2(0.5f, 1f);
            rtSel.sizeDelta = new Vector2(0f, 44f);
            rtSel.anchoredPosition = Vector2.zero;

            Button pBtn = CreateActionButton(selRow.transform, new Vector2(-180f, 0f), new Vector2(170f, 38f));
            pBtn.GetComponentInChildren<Text>().text = "9mm Tabanca";
            pBtn.onClick.AddListener(() => { selectedGunsmithWeapon = WeaponType.Pistol; RefreshGunsmithTab(); });

            Button sBtn = CreateActionButton(selRow.transform, new Vector2(0f, 0f), new Vector2(170f, 38f));
            sBtn.GetComponentInChildren<Text>().text = "Pompalı Tüfek";
            sBtn.onClick.AddListener(() => { selectedGunsmithWeapon = WeaponType.Shotgun; RefreshGunsmithTab(); });

            Button aBtn = CreateActionButton(selRow.transform, new Vector2(180f, 0f), new Vector2(170f, 38f));
            aBtn.GetComponentInChildren<Text>().text = "Yangın Baltası";
            aBtn.onClick.AddListener(() => { selectedGunsmithWeapon = WeaponType.Axe; RefreshGunsmithTab(); });

            // Weapon Title & Stat Bars Area
            GameObject statsObj = new GameObject("StatsArea");
            statsObj.transform.SetParent(parent, false);
            RectTransform rtStats = statsObj.AddComponent<RectTransform>();
            rtStats.anchorMin = new Vector2(0f, 0.72f);
            rtStats.anchorMax = new Vector2(1f, 0.90f);
            rtStats.sizeDelta = Vector2.zero;

            weaponTitleText = CreateLabel(statsObj.transform, new Vector2(15f, 12f), new Vector2(300f, 26f), "9MM TABANCA", 16, FontStyle.Bold, new Color(0.95f, 0.75f, 0.15f), TextAnchor.MiddleLeft);

            // 4 Stat Bars (Damage, Stealth, Accuracy, Capacity)
            statDamageFill = CreateStatBar(statsObj.transform, new Vector2(15f, -14f), "HASAR", out statDamageText);
            statStealthFill = CreateStatBar(statsObj.transform, new Vector2(185f, -14f), "SES GİZLİLİĞİ", out statStealthText);
            statAccuracyFill = CreateStatBar(statsObj.transform, new Vector2(355f, -14f), "İSABET", out statAccuracyText);
            statCapacityFill = CreateStatBar(statsObj.transform, new Vector2(525f, -14f), "KAPASİTE", out statCapacityText);

            // Attachments Scroll View Area
            GameObject listObj = new GameObject("AttachmentList");
            listObj.transform.SetParent(parent, false);
            RectTransform rtList = listObj.AddComponent<RectTransform>();
            rtList.anchorMin = Vector2.zero;
            rtList.anchorMax = new Vector2(1f, 0.70f);
            rtList.sizeDelta = Vector2.zero;

            attachmentRowsParent = BuildScrollViewHierarchy(listObj.transform);
        }

        private Image CreateStatBar(Transform parent, Vector2 pos, string label, out Text textLabel)
        {
            GameObject container = new GameObject($"Stat_{label}");
            container.transform.SetParent(parent, false);
            RectTransform rt = container.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(155f, 24f);

            // Track
            Image track = container.AddComponent<Image>();
            track.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            track.raycastTarget = false;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(container.transform, false);
            RectTransform rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.sizeDelta = Vector2.zero;

            Image fill = fillObj.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.20f, 0.65f, 0.90f);
            fill.raycastTarget = false;

            // Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(container.transform, false);
            RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            textLabel = txtObj.AddComponent<Text>();
            textLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            textLabel.fontSize = 10;
            textLabel.fontStyle = FontStyle.Bold;
            textLabel.alignment = TextAnchor.MiddleCenter;
            textLabel.color = Color.white;
            textLabel.raycastTarget = false;

            return fill;
        }

        private Transform BuildScrollViewHierarchy(Transform parent)
        {
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform rtScroll = scrollObj.AddComponent<RectTransform>();
            rtScroll.anchorMin = Vector2.zero;
            rtScroll.anchorMax = Vector2.one;
            rtScroll.sizeDelta = Vector2.zero;

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            GameObject contentObj = new GameObject("RowsContainer");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform rtContent = contentObj.AddComponent<RectTransform>();
            rtContent.anchorMin = new Vector2(0f, 1f);
            rtContent.anchorMax = new Vector2(1f, 1f);
            rtContent.pivot = new Vector2(0.5f, 1f);
            rtContent.sizeDelta = new Vector2(0f, 400f);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8f;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = rtContent;
            return contentObj.transform;
        }

        private GameObject CreateRowTemplate(Transform parent, float height)
        {
            GameObject row = new GameObject("Row_Item");
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(680f, height);

            Image img = row.AddComponent<Image>();
            img.color = new Color(0.14f, 0.17f, 0.22f, 0.95f);
            img.raycastTarget = false;

            return row;
        }

        private Button CreateTabButton(Transform parent, int index, string title, Action onClick)
        {
            GameObject btnObj = new GameObject($"Btn_Tab_{index}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(230f, 54f);
            rt.anchoredPosition = new Vector2(0f, -index * 62f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.20f, 0.25f, 0.32f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.text = title;

            return btn;
        }

        private Button CreateActionButton(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject btnObj = new GameObject("ActionButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.55f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }

        private Text CreateLabel(Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, FontStyle style, Color col, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(parent, false);

            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = anchor;
            txt.color = col;
            txt.text = text;
            txt.raycastTarget = false;

            return txt;
        }
    }
}
