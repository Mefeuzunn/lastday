using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }

    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Inventory Setup")]
        [SerializeField] private int maxSlots = 8;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        [Header("Starting Items")]
        [SerializeField] private List<ItemData> initialItems = new List<ItemData>();

        [Header("Throw Setup")]
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private float throwForce = 9f;

        private SurvivalStats survivalStats;

        public IReadOnlyList<InventorySlot> Slots => slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            survivalStats = GetComponent<SurvivalStats>();
            if (throwOrigin == null) throwOrigin = transform;

            foreach (var item in initialItems)
            {
                AddItem(item, 1);
            }
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            // Check if stackable with existing slot
            foreach (var slot in slots)
            {
                if (slot.item != null && slot.item.itemId == item.itemId && slot.quantity < item.maxStack)
                {
                    int space = item.maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, quantity);
                    slot.quantity += toAdd;
                    quantity -= toAdd;

                    if (quantity <= 0)
                    {
                        GameEvents.TriggerInventoryUpdated();
                        return true;
                    }
                }
            }

            // Add new slot if space exists
            if (slots.Count < maxSlots && quantity > 0)
            {
                slots.Add(new InventorySlot(item, quantity));
                GameEvents.TriggerInventoryUpdated();
                return true;
            }

            Debug.LogWarning("[InventorySystem] Inventory is full!");
            return false;
        }

        public static ItemData FindItemData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && string.Equals(allItems[i].itemId, itemId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return allItems[i];
                }
            }
            ItemData loaded = Resources.Load<ItemData>($"Items/{itemId}");
            if (loaded != null) return loaded;

            // Runtime fallback creation for new Phase 3 items if not yet loaded in Resources
            if (string.Equals(itemId, "weapon_crossbow", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData cb = ScriptableObject.CreateInstance<ItemData>();
                cb.itemId = "weapon_crossbow";
                cb.itemName = "Taktik Tatar Yayı";
                cb.description = "Sessiz, yüksek kinetik hasarlı ve okları geri toplanabilir taktik tatar yayı.";
                cb.itemType = ItemType.Weapon;
                cb.maxStack = 1;
                cb.weaponDamage = 95f;
                cb.maxDurability = 120;
                return cb;
            }
            if (string.Equals(itemId, "ammo_crossbow_bolt", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData bolt = ScriptableObject.CreateInstance<ItemData>();
                bolt.itemId = "ammo_crossbow_bolt";
                bolt.itemName = "Tatar Yayı Oku";
                bolt.description = "Çelik uçlu, sessiz uçuşlu ve vuruş sonrası %85 geri toplanabilir tatar yayı oku.";
                bolt.itemType = ItemType.Resource;
                bolt.maxStack = 30;
                bolt.weaponDamage = 95f;
                return bolt;
            }
            if (string.Equals(itemId, "item_raw_meat", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_raw_meat";
                it.itemName = "Çiğ Av Eti";
                it.description = "Avlanan yaban hayvanından elde edilen çiğ et. Pişirilmeden tüketilirse enfeksiyon riski taşır.";
                it.itemType = ItemType.Food;
                it.maxStack = 10;
                it.hungerEffect = 15f;
                return it;
            }
            if (string.Equals(itemId, "item_cooked_meat", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_cooked_meat";
                it.itemName = "Izgara Av Eti";
                it.description = "Kamp ocağında odun ve hurda ateşiyle mühürlenmiş besleyici ve doyurucu ızgara av eti.";
                it.itemType = ItemType.Food;
                it.maxStack = 10;
                it.hungerEffect = 65f;
                it.healthEffect = 22f;
                it.infectionEffect = -5f;
                return it;
            }
            if (string.Equals(itemId, "item_raw_fish", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_raw_fish";
                it.itemName = "Taze Göl Balığı";
                it.description = "Kuzeybatı gölünden tutulmuş taze tatlı su balığı. Çiğ tüketilirse parazit ve enfeksiyon riski taşır.";
                it.itemType = ItemType.Food;
                it.maxStack = 10;
                it.hungerEffect = 15f;
                return it;
            }
            if (string.Equals(itemId, "item_grilled_fish", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_grilled_fish";
                it.itemName = "Közde Göl Balığı";
                it.description = "Ateşte nar gibi kızartılmış közde göl balığı. Açlığı ve susuzluğu yatıştırır, 120 saniye enerji yenilenmesi sağlar.";
                it.itemType = ItemType.Food;
                it.maxStack = 10;
                it.hungerEffect = 50f;
                it.thirstEffect = 15f;
                it.healthEffect = 18f;
                return it;
            }
            if (string.Equals(itemId, "item_empty_jerrycan", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_empty_jerrycan";
                it.itemName = "Boş Yakıt Bidonu";
                it.description = "Benzin istasyonu pompalarından veya araç enkazlarından yakıt çekmek için kullanılan 15L kapasiteli boş metal bidon.";
                it.itemType = ItemType.Resource;
                it.maxStack = 5;
                return it;
            }
            if (string.Equals(itemId, "item_fuel_can", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_fuel_can";
                it.itemName = "Dolu Yakıt Bidonu";
                it.description = "Araç depolarını doldurmak veya jeneratörleri beslemek için 15L rafine yakıt içeren dolu bidon.";
                it.itemType = ItemType.Resource;
                it.maxStack = 5;
                it.weaponDamage = 15f;
                return it;
            }
            if (string.Equals(itemId, "item_bunker_keycard", System.StringComparison.OrdinalIgnoreCase))
            {
                ItemData it = ScriptableObject.CreateInstance<ItemData>();
                it.itemId = "item_bunker_keycard";
                it.itemName = "Seviye-2 Güvenlik Kartı";
                it.description = "Kuzeybatı araştırma sığınağının Biyolojik Deney Laboratuvarı blast kapısını açan Seviye-2 manyetik kart.";
                it.itemType = ItemType.QuestItem;
                it.maxStack = 1;
                return it;
            }

            return null;
        }

        public bool AddItemById(string itemId, int quantity = 1)
        {
            ItemData data = FindItemData(itemId);
            if (data != null)
            {
                return AddItem(data, quantity);
            }
            Debug.LogWarning($"[InventorySystem] ItemData for {itemId} could not be resolved.");
            return false;
        }

        public int GetItemCount(string itemId)
        {
            bool isFuelQuery = string.Equals(itemId, "item_fuel", System.StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(itemId, "item_fuel_can", System.StringComparison.OrdinalIgnoreCase);

            int total = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.item != null)
                {
                    if (isFuelQuery)
                    {
                        if (string.Equals(slot.item.itemId, "item_fuel", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(slot.item.itemId, "item_fuel_can", System.StringComparison.OrdinalIgnoreCase))
                        {
                            total += slot.quantity;
                        }
                    }
                    else if (string.Equals(slot.item.itemId, itemId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        total += slot.quantity;
                    }
                }
            }
            return total;
        }

        public bool HasItem(string itemId, int count = 1)
        {
            return GetItemCount(itemId) >= count;
        }

        public bool RemoveItem(string itemId, int count = 1)
        {
            if (!HasItem(itemId, count)) return false;

            bool isFuelRemoval = string.Equals(itemId, "item_fuel", System.StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(itemId, "item_fuel_can", System.StringComparison.OrdinalIgnoreCase);

            int remainingToRemove = count;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].item != null)
                {
                    bool match = isFuelRemoval
                        ? (string.Equals(slots[i].item.itemId, "item_fuel", System.StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(slots[i].item.itemId, "item_fuel_can", System.StringComparison.OrdinalIgnoreCase))
                        : string.Equals(slots[i].item.itemId, itemId, System.StringComparison.OrdinalIgnoreCase);

                    if (match)
                    {
                        if (slots[i].quantity > remainingToRemove)
                        {
                            slots[i].quantity -= remainingToRemove;
                            remainingToRemove = 0;
                            break;
                        }
                        else
                        {
                            remainingToRemove -= slots[i].quantity;
                            slots.RemoveAt(i);
                        }

                        if (remainingToRemove <= 0) break;
                    }
                }
            }

            GameEvents.TriggerInventoryUpdated();
            return true;
        }

        public void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.item == null) return;

            ItemData item = slot.item;

            bool isMedicalOrAlchemy = item.itemType == ItemType.Medicine ||
                                      item.itemId == "item_bandage" ||
                                      item.itemId == "item_splint" ||
                                      item.itemId == "item_painkillers" ||
                                      item.itemId == "item_medkit" ||
                                      item.itemId == "item_antiviral" ||
                                      item.itemId == "item_adrenaline" ||
                                      item.itemId == "item_herbal_tea" ||
                                      item.itemId == "item_purified_water" ||
                                      item.itemId == "item_hot_stew" ||
                                      item.itemId == "item_raw_meat" ||
                                      item.itemId == "item_cooked_meat" ||
                                      item.itemId == "item_raw_fish" ||
                                      item.itemId == "item_grilled_fish";

            if (isMedicalOrAlchemy && TraumaAndInjurySystem.Instance != null)
            {
                bool treated = TraumaAndInjurySystem.Instance.TryUseMedicalItem(item.itemId);
                if (!treated)
                {
                    // Treatment was unnecessary or invalid - do not consume item
                    return;
                }
            }
            else
            {
                // Apply item effects
                if (survivalStats != null)
                {
                    if (item.healthEffect != 0) survivalStats.Heal(item.healthEffect);
                    if (item.hungerEffect != 0) survivalStats.Feed(item.hungerEffect);
                    if (item.thirstEffect != 0) survivalStats.Hydrate(item.thirstEffect);
                    if (item.infectionEffect != 0) survivalStats.ModifyInfection(item.infectionEffect);
                }
            }

            // Distraction throwable
            if (item.itemType == ItemType.Distraction)
            {
                ThrowDistraction(item);
            }
            else if (item.itemId == "item_battery")
            {
                Player.FlashlightController.Instance?.Recharge(100f);
            }
            else if (item.itemId == "item_map_intel")
            {
                if (Environment.FogOfWarManager.Instance != null)
                {
                    Environment.FogOfWarManager.Instance.RevealRadius(new Vector3(45f, 0f, 45f), 60f);
                    Environment.FogOfWarManager.Instance.RevealRadius(new Vector3(-45f, 0f, 50f), 60f);
                }
                GameEvents.TriggerObjectiveToastRequested("Taktik Harita İncelendi: Kuzey Karantina ve Doğu Sanayi Bölgeleri açığa çıkarıldı!", 5f);
                ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            }

            slot.quantity--;
            if (slot.quantity <= 0)
            {
                slots.RemoveAt(slotIndex);
            }

            GameEvents.TriggerInventoryUpdated();
            Debug.Log($"[InventorySystem] Used item: {item.itemName}");
        }

        private void ThrowDistraction(ItemData item)
        {
            Vector3 spawnPos = throwOrigin.position + (transform.forward * 1f) + (Vector3.up * 1f);
            Vector3 throwVector = (transform.forward * throwForce) + (Vector3.up * 2.5f);

            Player.PlayerAnimationBridge animBridge = GetComponent<Player.PlayerAnimationBridge>();
            if (animBridge != null) animBridge.TriggerThrow();

            GameObject thrownObj = null;
            if (item.throwPrefab != null)
            {
                thrownObj = Instantiate(item.throwPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback procedural sphere if no prefab
                thrownObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                thrownObj.transform.position = spawnPos;
                thrownObj.transform.localScale = Vector3.one * 0.3f;
                Rigidbody rb = thrownObj.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
            }

            SoundBomb soundBomb = thrownObj.GetComponent<SoundBomb>();
            if (soundBomb != null)
            {
                soundBomb.Initialize(throwVector);
            }
            else
            {
                ThrownDistraction distraction = thrownObj.GetComponent<ThrownDistraction>();
                if (distraction == null)
                {
                    distraction = thrownObj.AddComponent<ThrownDistraction>();
                }
                distraction.Initialize(item.distractionNoiseRadius, throwVector);
            }
        }
    }
}
