using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.Missions
{
    public class RandomEncounterManager : MonoBehaviour
    {
        public static RandomEncounterManager Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float minInterval = 75f;
        [SerializeField] private float maxInterval = 120f;
        [SerializeField] private bool enableEncounters = true;

        [Header("Prefabs & Templates")]
        [SerializeField] private GameObject airdropCratePrefab;

        private float nextEncounterTimer = 0f;
        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResetTimer();
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (!enableEncounters) return;

            nextEncounterTimer -= Time.deltaTime;
            if (nextEncounterTimer <= 0f)
            {
                TriggerRandomEncounter();
                ResetTimer();
            }
        }

        private void ResetTimer()
        {
            nextEncounterTimer = UnityEngine.Random.Range(minInterval, maxInterval);
        }

        public void TriggerRandomEncounter()
        {
            if (playerTransform == null) return;

            int roll = UnityEngine.Random.Range(0, 3);
            switch (roll)
            {
                case 0:
                    TriggerAirdrop();
                    break;
                case 1:
                    TriggerHordeMigration();
                    break;
                case 2:
                    TriggerSupplyCacheFound();
                    break;
            }
        }

        private void TriggerAirdrop()
        {
            Vector3 offset = UnityEngine.Random.insideUnitSphere * 18f;
            offset.y = 0;
            Vector3 dropPos = playerTransform.position + offset;

            // Spawn supply crate
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Airdrop_Supply_Crate";
            crate.transform.position = dropPos;
            crate.transform.localScale = new Vector3(1.2f, 1.0f, 1.0f);

            var col = crate.GetComponent<Collider>();
            col.isTrigger = true;

            var lc = crate.AddComponent<LootContainer>();

            // Notification
            GameEvents.TriggerInteractionPromptChanged(true, "⚠️ TELSİZ: Yakınlara Askeri Yardım Paketi Düştü!");
            GameEvents.TriggerCameraShakeRequested(0.2f, 0.3f);
            Debug.Log($"[RandomEncounter] Airdrop spawned at {dropPos}!");
        }

        private void TriggerHordeMigration()
        {
            GameEvents.TriggerInteractionPromptChanged(true, "⚠️ DİKKAT: Sokaktan Zombi Sürüsü Geçiyor! Gizlen!");
            GameEvents.TriggerNoiseEmitted(playerTransform.position + Vector3.forward * 20f, 22f);
            GameEvents.TriggerCameraShakeRequested(0.35f, 0.4f);

            // Spawn 3-4 runner zombies
            if (ObjectPooler.Instance != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = playerTransform.position + new Vector3(Random.Range(-15f, 15f), 0, Random.Range(15f, 25f));
                    ObjectPooler.Instance.SpawnFromPool("Zombie_Runner", pos, Quaternion.identity);
                }
            }
            Debug.Log("[RandomEncounter] Horde migration wave triggered!");
        }

        private void TriggerSupplyCacheFound()
        {
            GameEvents.TriggerInteractionPromptChanged(true, "ℹ️ Çevrede Terk Edilmiş Bir Erzak Zulası Keşfedildi.");
            if (InventorySystem.Instance != null)
            {
                // Give small random discovery
                ItemData scrap = Resources.Load<ItemData>("Data/Items/item_scrap");
                if (scrap != null) InventorySystem.Instance.AddItem(scrap, 2);
            }
        }
    }
}
