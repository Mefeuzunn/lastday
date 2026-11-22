using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Core
{
    public enum DynamicEventType
    {
        SupplyDrop,
        HordeMigration,
        RadioSOS
    }

    public class DynamicEventDirector : MonoBehaviour
    {
        public static DynamicEventDirector Instance { get; private set; }

        [Header("Event Interval (2.5 - 3.5 minutes)")]
        [SerializeField] private float minIntervalSeconds = 150f;
        [SerializeField] private float maxIntervalSeconds = 210f;
        [SerializeField] private bool autoTriggerEvents = true;

        [Header("Prefabs & References")]
        [SerializeField] private GameObject lootCratePrefab;
        [SerializeField] private GameObject zombieStandardPrefab;
        [SerializeField] private GameObject zombieRunnerPrefab;

        private float eventTimer = 0f;
        private float nextEventDelay = 160f;
        private bool isEventActive = false;
        private Transform playerTransform;

        // Active Event Tracking
        private GameObject currentEventObject;
        private Coroutine activeEventCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            nextEventDelay = Random.Range(minIntervalSeconds, maxIntervalSeconds);
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (!autoTriggerEvents || isEventActive) return;

            eventTimer += Time.deltaTime;
            if (eventTimer >= nextEventDelay)
            {
                eventTimer = 0f;
                nextEventDelay = Random.Range(minIntervalSeconds, maxIntervalSeconds);
                TriggerRandomEvent();
            }
        }

        public void TriggerRandomEvent()
        {
            int r = Random.Range(0, 3);
            switch (r)
            {
                case 0:
                    TriggerSupplyDrop();
                    break;
                case 1:
                    TriggerHordeMigration();
                    break;
                case 2:
                    TriggerRadioSOS();
                    break;
            }
        }

        // =========================================================================
        // 1. HAVA YARDIM PAKETİ (SUPPLY DROP)
        // =========================================================================
        public void TriggerSupplyDrop()
        {
            Vector3[] dropLocations = new Vector3[]
            {
                new Vector3(0f, 0.1f, 0f),       // Central Metropolis Crossroads
                new Vector3(48f, 0.1f, 45f),     // Skyscraper Plaza
                new Vector3(-45f, 0.1f, 0f),     // Residential West Junction
                new Vector3(42f, 0.1f, -18f)     // South Highway Junction
            };

            Vector3 dropPos = dropLocations[Random.Range(0, dropLocations.Length)];
            if (activeEventCoroutine != null) StopCoroutine(activeEventCoroutine);
            activeEventCoroutine = StartCoroutine(SupplyDropRoutine(dropPos));
        }

        private IEnumerator SupplyDropRoutine(Vector3 dropPos)
        {
            isEventActive = true;
            Debug.Log($"[DynamicEventDirector] Supply Drop initiated at {dropPos}");

            GameEvents.TriggerObjectiveToastRequested("[HAVA YARDIMI] Şehre paraşütle acil yardım sandığı düştü!", 6f);
            GameEvents.TriggerWaypointChanged(dropPos, "Hava Yardımı");
            ProceduralAudioManager.Instance?.PlayAirRaidSiren();

            // Create Visual Supply Crate with Orange Smoke Flare
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "SupplyDrop_Crate";
            crate.transform.position = dropPos + Vector3.up * 0.45f;
            crate.transform.localScale = new Vector3(1.4f, 0.9f, 1.4f);

            Material crateMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            crateMat.color = new Color(0.18f, 0.32f, 0.20f); // Military green
            crate.GetComponent<Renderer>().sharedMaterial = crateMat;

            // Orange/Red Flare Beacon Column
            GameObject flareBeam = new GameObject("Flare_Light_Beam");
            flareBeam.transform.SetParent(crate.transform);
            flareBeam.transform.localPosition = Vector3.zero;

            Light flareLight = flareBeam.AddComponent<Light>();
            flareLight.type = LightType.Spot;
            flareLight.spotAngle = 60f;
            flareLight.range = 35f;
            flareLight.intensity = 4.5f;
            flareLight.color = new Color(1.0f, 0.35f, 0.05f); // Orange-red flare
            flareLight.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // Add Loot to the crate
            SphereCollider sc = crate.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.4f;
            var lc = crate.AddComponent<LootContainer>();

            ItemData ammo9 = null;
            ItemData shells = null;
            ItemData medkit = null;
            ItemData scrap = null;

#if UNITY_EDITOR
            ammo9 = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_9mm.asset");
            shells = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/ammo_shells.asset");
            medkit = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            scrap = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
#else
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                var it = allItems[i];
                if (it == null) continue;
                if (it.itemId == "ammo_9mm") ammo9 = it;
                else if (it.itemId == "ammo_shells") shells = it;
                else if (it.itemId == "item_medkit") medkit = it;
                else if (it.itemId == "item_scrap") scrap = it;
            }
#endif
            if (ammo9 != null) lc.AddItem(ammo9, 24);
            if (shells != null) lc.AddItem(shells, 12);
            if (medkit != null) lc.AddItem(medkit, 2);
            if (scrap != null) lc.AddItem(scrap, 5);

            // Emit loud sound attracting 6-8 nearby zombies
            GameEvents.TriggerNoiseEmitted(dropPos, 35f);

            // Wait until player loots or 120s timeout
            float timer = 0f;
            while (timer < 120f)
            {
                timer += 1f;
                yield return new WaitForSeconds(1f);
                if (crate == null) break;
            }

            if (crate != null) Destroy(crate, 30f);
            isEventActive = false;
        }

        // =========================================================================
        // 2. GEZİCİ ZOMBİ SÜRÜSÜ (HORDE MIGRATION)
        // =========================================================================
        public void TriggerHordeMigration()
        {
            if (activeEventCoroutine != null) StopCoroutine(activeEventCoroutine);
            activeEventCoroutine = StartCoroutine(HordeMigrationRoutine());
        }

        private IEnumerator HordeMigrationRoutine()
        {
            isEventActive = true;
            Debug.Log("[DynamicEventDirector] Horde Migration initiated along Main Avenue!");

            GameEvents.TriggerObjectiveToastRequested("[TEHLİKE] Gezici bir zombi sürüsü ana bulvardan geçiyor! Siper alın!", 6f);
            ProceduralAudioManager.Instance?.PlayScreamerNoise();

            Vector3 startPos = new Vector3(0f, 0.1f, 75f);
            Vector3 targetPos = new Vector3(0f, 0.1f, -75f);
            GameEvents.TriggerWaypointChanged(startPos, "Zombi Sürüsü");

            // Alert active zombies nearby to migrate
            var activeZombies = ZombieAI.ActiveZombies;
            int count = 0;
            for (int i = 0; i < activeZombies.Count && count < 8; i++)
            {
                var z = activeZombies[i];
                if (z != null && !z.IsDead)
                {
                    z.LoseTargetAndInvestigate(targetPos);
                    count++;
                }
            }

            yield return new WaitForSeconds(60f);
            isEventActive = false;
        }

        // =========================================================================
        // 3. TELSİZ SOS YARDIM ÇAĞRISI (RADIO SOS)
        // =========================================================================
        public void TriggerRadioSOS()
        {
            Vector3[] sosBuildings = new Vector3[]
            {
                new Vector3(32f, 0.1f, 30f),   // Supermarket
                new Vector3(30f, 0.1f, -48f),  // Pharmacy
                new Vector3(-68f, 0.1f, -12f)  // Victorian House 1
            };

            Vector3 sosPos = sosBuildings[Random.Range(0, sosBuildings.Length)];
            if (activeEventCoroutine != null) StopCoroutine(activeEventCoroutine);
            activeEventCoroutine = StartCoroutine(RadioSOSRoutine(sosPos));
        }

        private IEnumerator RadioSOSRoutine(Vector3 sosPos)
        {
            isEventActive = true;
            Debug.Log($"[DynamicEventDirector] Radio SOS Event at {sosPos}");

            GameEvents.TriggerObjectiveToastRequested("[TELSİZ SOS] 'Yardım edin! Binada sıkıştım, zombiler kapıyı zorluyor!' (90 sn)", 7f);
            GameEvents.TriggerWaypointChanged(sosPos, "SOS Çağrısı");

            // Alert nearby zombies towards the SOS building
            GameEvents.TriggerNoiseEmitted(sosPos, 28f);

            float countdown = 90f;
            bool rescued = false;

            while (countdown > 0f)
            {
                countdown -= 1f;
                yield return new WaitForSeconds(1f);

                if (playerTransform == null) FindPlayer();
                if (playerTransform != null && Vector3.Distance(playerTransform.position, sosPos) <= 6.0f)
                {
                    rescued = true;
                    break;
                }
            }

            if (rescued)
            {
                GameEvents.TriggerCivilianRescued();
                GameEvents.TriggerItemCollected("ammo_9mm", 12);
                GameEvents.TriggerItemCollected("item_canned_food", 2);
                GameEvents.TriggerObjectiveToastRequested("[KURTARILDI] Mahsur kalan sivil kurtarıldı! Teşekkür hediyesi envantere eklendi.", 6f);
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("[BAŞARISIZ] Telsiz sinyali kesildi... Sivil kurtarılamadı.", 5f);
            }

            yield return new WaitForSeconds(10f);
            isEventActive = false;
        }
    }
}
